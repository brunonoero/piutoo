using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore MOD: <b>modulo del tempo</b>. Si entra solo sulle barre il cui indice temporale, diviso per
/// <see cref="ModuloBars"/>, da' resto <see cref="Remainder"/>, nella direzione della barra appena
/// chiusa (<see cref="Mode"/> 0) o contro (1). E' una delle idee <b>bizzarre</b> della serie PT6EXO,
/// forse la piu' pura: non ha alcuna ipotesi economica, e' un campionamento a passo fisso di un
/// segnale di momentum a una barra. Si valuta solo contro il controllo a ingresso casuale RAN con le
/// stesse uscite e lo stesso numero di trade: se non lo batte con chiarezza e' rumore, e l'atteso e'
/// che lo sia. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §MOD.
///
/// <para><b>L'indice e' tempo, non un conteggio di barre.</b> Indice = minuti UTC dall'epoca Unix
/// all'<b>apertura</b> della barra, diviso (intero) per <see cref="EasyEngineBase.TimeframeMinutes"/>.
/// Mai la posizione della barra nella serie: il feed interno, quello del broker e la storia che il cBot
/// spinge in sessione live hanno buchi diversi (festivi, pause, barre mancanti, riscaldamento di
/// lunghezza variabile), e contare le barre darebbe a ciascuno un indice diverso per la stessa barra.
/// Il tempo e' lo stesso per tutti. Oltre l'ora l'indice non e' allineato all'ancoraggio della
/// sessione (una 4h che apre alle 23:00 UTC ha indice ⌊minuti/240⌋ come ogni altra), e al cambio d'ora
/// un indice puo' saltare o ripetersi: e' una proprieta' dell'idea, non un difetto, perche' la regola
/// resta una funzione dell'istante.</para>
///
/// <para><b>Quale barra.</b> L'indice e' quello della barra <b>su cui si entra</b>, la dopo quella
/// appena chiusa; il verso lo da' la barra chiusa (chiusura sopra l'apertura = rialzo; uguali = nessun
/// verso, nessun ingresso). Come in <c>HourOfDayEngine</c>, la barra su cui si entra deve esistere —
/// giorno di sessione e finestra di negoziazione del simbolo — altrimenti l'ordine aspetterebbe la
/// prima barra vera, che ha un altro indice.</para>
/// </summary>
public abstract class TimeModuloEngine : EasyEngineBase
{
    /// <summary>Il modulo: ogni quante barre di tempo si entra. Almeno 2.</summary>
    protected int ModuloBars = 7;

    /// <summary>Il resto che fa scattare l'ingresso, fra 0 e <see cref="ModuloBars"/> - 1.</summary>
    protected int Remainder;

    /// <summary>0 = nella direzione della barra chiusa; 1 = contro.</summary>
    protected int Mode;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (ModuloBars < 2 || Remainder < 0 || Remainder >= ModuloBars)
        {
            throw new ArgumentOutOfRangeException(nameof(Remainder),
                $"{Name}: ModuloBars {ModuloBars} deve valere almeno 2 e Remainder {Remainder} stare fra 0 e ModuloBars - 1.");
        }

        if (Mode is not (0 or 1) || Direction is < 0 or > 2 || TimeframeMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: Mode {Mode} (0 o 1), Direction {Direction} (0, 1, 2) o timeframe {TimeframeMinutes} non validi.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var entryOpen = barTime.AddMinutes(TimeframeMinutes);
        if (FloorMod(TimeIndex(entryOpen), ModuloBars) != Remainder)
            return Hold(bar.Close, barTime);

        if (bar.Close == bar.Open)
            return Hold(bar.Close, barTime);

        // La barra su cui si entra deve esistere: si controlla solo sulle barre del modulo.
        if (Grid.IsSessionDay(Grid.SessionDayOf(entryOpen)) == false ||
            SessionMask.For(Symbol).Overlaps(entryOpen, TimeframeMinutes) == false)
        {
            return Hold(bar.Close, barTime);
        }

        var up = bar.Close > bar.Open;
        var side = (Mode == 0) == up ? SignalType.Buy : SignalType.Sell;

        if ((Direction == 1 && side != SignalType.Buy) || (Direction == 2 && side != SignalType.Sell))
            return Hold(bar.Close, barTime);

        var signal = WithSessionExit(EntryMarketNextBar(side, bar.Close, data, barTime,
            side == SignalType.Buy ? "LE MOD" : "SE MOD"));

        return signal ?? Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Indice temporale della barra che apre in <paramref name="barOpenUtc"/>: minuti UTC dall'epoca
    /// Unix diviso il timeframe, per difetto. Pubblico perche' test e studi devono poter dire quali
    /// barre il modulo sceglie senza far girare un backtest.
    /// </summary>
    public long TimeIndex(DateTime barOpenUtc)
    {
        var minutes = FloorDiv(barOpenUtc.Ticks - DateTime.UnixEpoch.Ticks, TimeSpan.TicksPerMinute);
        return FloorDiv(minutes, TimeframeMinutes);
    }

    private static long FloorDiv(long value, long divisor)
    {
        var quotient = value / divisor;
        return value % divisor < 0 ? quotient - 1 : quotient;
    }

    private static long FloorMod(long value, long divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}
