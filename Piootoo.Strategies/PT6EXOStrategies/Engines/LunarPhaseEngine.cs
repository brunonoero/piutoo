using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore LUN: <b>fasi lunari</b>. Long dalla luna nuova alla piena e short dalla piena alla nuova
/// (<see cref="Mode"/> 0), o il contrario (1). E' una delle idee <b>bizzarre</b> della serie PT6EXO:
/// nessun legame con nulla del catalogo, quindi scorrelata per definizione, e nessuna ipotesi
/// economica. Si giudica solo contro il controllo a ingresso casuale RAN con le stesse uscite, e su
/// piu' simboli: se regge fuori campione su uno solo e' rumore. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §LUN.
///
/// <para><b>La fase</b> e' una funzione pura dell'istante: la frazione del mese sinodico medio
/// (29,530588853 giorni) trascorsa dalla luna nuova di riferimento del 06/01/2000 18:14 UTC, in
/// [0, 1), con 0 luna nuova e 0,5 piena. E' la lunazione <b>media</b>: la luna vera se ne scosta di
/// qualche ora, ed e' voluto — un'effemeride vera sarebbe una tabella da mantenere, questa e' una
/// formula. Il calcolo e' in <b>tick interi</b>, non in double: 29,530588853 giorni sono esattamente
/// 25.514.428.768.992 tick e mezzo mese 12.757.214.384.496, quindi i confini sono istanti esatti e
/// identici su ogni macchina, senza arrotondamenti che possano spostare un confine sulla barra
/// accanto.</para>
///
/// <para><b>Quando nasce l'ordine.</b> Come l'ora di ingresso di <c>HourOfDayEngine</c>, sulla barra
/// <b>prima</b>: il confine (fase 0 o 0,5) cade fra l'apertura della barra appena chiusa (esclusa) e
/// l'apertura della barra dopo (inclusa), e l'ingresso e' a mercato all'apertura di quella barra, la
/// prima che apre sul mezzo ciclo nuovo. Un confine che cade esattamente su un'apertura entra su
/// quella barra.</para>
///
/// <para><b>Se la barra dopo non esiste</b> — un giorno senza sessione, o fuori dalla finestra di
/// negoziazione del simbolo (<c>SessionMask</c>) — l'ingresso di quel mezzo ciclo <b>si salta</b>. Non
/// si recupera alla prima barra vera: l'ordine aprirebbe il lunedi' a un istante che nessuno ha
/// dichiarato, e non piu' "alla luna nuova" ma "alla prima apertura dopo la luna nuova", un'altra
/// regola. E' una perdita grossa e va messa in conto leggendo i numeri: su NQ il mercato e' chiuso
/// quasi un terzo della settimana (fine settimana e pause), e quasi un mezzo ciclo su tre non entra;
/// sui simboli con la finestra di negoziazione corta (FDAX) di piu'.</para>
///
/// <para><b>L'uscita.</b> La posizione vive fino al confine successivo, mezzo mese sinodico dopo, ed e'
/// quindi <b>multiday per natura</b>: il contenitore nasce con <c>IntradayOnly = false</c>. Con
/// <c>IntradayOnly</c> acceso l'uscita di sessione, piu' stretta, vince, e il motore diventa un trade
/// intraday ogni due settimane.</para>
///
/// <para><b>Perche' la chiusura cade all'apertura della barra che contiene il confine</b>, e non
/// sull'istante del confine. Il confine e' anche l'ingresso del mezzo ciclo dopo, e quell'ingresso
/// nasce valutando la barra che lo contiene: nel backtest, dentro il tick, la valutazione viene
/// <b>prima</b> delle uscite a tempo, quindi una posizione che chiude sull'istante del confine e' ancora
/// aperta quando la strategia decide il mezzo ciclo nuovo, e l'ingresso non nasce. Con <see cref="Mode"/>
/// 0 i due mezzi cicli hanno verso opposto, e il rollover di <c>BiasWeeklyEngine</c> — stesso verso,
/// stessa barra — non si applica: la strategia entrerebbe un mezzo ciclo si' e uno no. Chiudendo
/// all'apertura della barra del confine la posizione e' chiusa quando quella barra si valuta, in
/// backtest come in sessione live. Il prezzo e' una barra flat fra un mezzo ciclo e l'altro.
/// Resta vero che un'uscita a tempo del piano o di sessione non allineata puo' lasciare la posizione
/// aperta alla valutazione del confine: in quel caso quel mezzo ciclo si perde, come ogni ingresso a
/// posizione aperta.</para>
/// </summary>
public abstract class LunarPhaseEngine : EasyEngineBase
{
    /// <summary>Mese sinodico medio, in giorni.</summary>
    public const double SynodicMonthDays = 29.530588853;

    /// <summary>Mese sinodico medio in tick: 29,530588853 × 864.000.000.000, esatto.</summary>
    public const long SynodicMonthTicks = 25_514_428_768_992L;

    /// <summary>Mezzo mese sinodico in tick: la distanza fra luna nuova e piena, esatta.</summary>
    public const long HalfCycleTicks = SynodicMonthTicks / 2;

    /// <summary>La luna nuova di riferimento: 06/01/2000 18:14 UTC.</summary>
    public static readonly DateTime ReferenceNewMoonUtc = new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    /// <summary>0 = long dalla nuova alla piena e short dalla piena alla nuova; 1 = il contrario.</summary>
    protected int Mode;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (Mode is not (0 or 1) || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: Mode {Mode} (0 o 1) o Direction {Direction} (0, 1, 2) non validi.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Il confine deve cadere in (apertura di questa barra, apertura della barra dopo].
        var entryOpen = barTime.AddMinutes(TimeframeMinutes);
        var half = HalfCycleIndex(entryOpen);
        if (half == HalfCycleIndex(barTime))
            return Hold(bar.Close, barTime);

        // La barra su cui si entra deve esistere: giorno di sessione e finestra di negoziazione.
        // Se non c'e', il mezzo ciclo si salta (vedi il sommario).
        if (Grid.IsSessionDay(Grid.SessionDayOf(entryOpen)) == false ||
            SessionMask.For(Symbol).Overlaps(entryOpen, TimeframeMinutes) == false)
        {
            return Hold(bar.Close, barTime);
        }

        // Mezzo ciclo pari = dalla nuova alla piena (fase in [0; 0,5)).
        var waxing = FloorMod(half, 2) == 0;
        var side = (Mode == 0) == waxing ? SignalType.Buy : SignalType.Sell;

        if ((Direction == 1 && side != SignalType.Buy) || (Direction == 2 && side != SignalType.Sell))
            return Hold(bar.Close, barTime);

        var signal = WithSessionExit(EntryMarketNextBar(side, bar.Close, data, barTime,
            side == SignalType.Buy ? "LE LUN" : "SE LUN"));
        if (signal is null)
            return Hold(bar.Close, barTime);

        // Vince la scadenza piu' stretta fra quella di sessione e il confine successivo.
        var exit = CloseBeforeBoundaryUtc(BoundaryUtc(half + 1));
        if (signal.CloseAtUtc is not { } earlier || exit < earlier)
            signal.CloseAtUtc = exit;

        return signal;
    }

    /// <summary>
    /// Fase lunare media in [0, 1): 0 luna nuova, 0,5 piena. Pubblica perche' test e studi devono
    /// poterla leggere senza far girare un backtest.
    /// </summary>
    public static double Phase(DateTime instantUtc) =>
        FloorMod(instantUtc.Ticks - ReferenceNewMoonUtc.Ticks, SynodicMonthTicks) / (double)SynodicMonthTicks;

    /// <summary>
    /// Indice del mezzo ciclo che contiene l'istante: pari dalla nuova alla piena, dispari dalla piena
    /// alla nuova. 0 e' il mezzo ciclo che parte dalla luna nuova di riferimento.
    /// </summary>
    public static long HalfCycleIndex(DateTime instantUtc) =>
        FloorDiv(instantUtc.Ticks - ReferenceNewMoonUtc.Ticks, HalfCycleTicks);

    /// <summary>Istante esatto in cui comincia il mezzo ciclo <paramref name="index"/>.</summary>
    public static DateTime BoundaryUtc(long index) =>
        new(ReferenceNewMoonUtc.Ticks + index * HalfCycleTicks, DateTimeKind.Utc);

    /// <summary>
    /// L'apertura della barra che contiene il confine, sulla griglia del simbolo: e' li' che chiude la
    /// posizione del mezzo ciclo che finisce (vedi il sommario). Un timeframe che non divide il giorno
    /// non ha una griglia di bucket, e chiude sul confine.
    /// </summary>
    private DateTime CloseBeforeBoundaryUtc(DateTime boundaryUtc) =>
        SessionGrid.DividesTheDay(TimeframeMinutes)
            ? Grid.BucketStartUtc(boundaryUtc.AddTicks(-1), TimeframeMinutes)
            : boundaryUtc;

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
