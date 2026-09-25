using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore GAP: <b>gap di apertura della sessione della ricerca</b>. Quando la sessione apre lontano
/// dalla chiusura della precedente — piu' di <see cref="GapAtr"/> volte l'ATR delle sessioni chiuse — si
/// entra contro il gap (fade, la scommessa che si richiuda) o a favore (go). Esiste solo nei giorni con
/// gap e non guarda il trend: e' il motivo per cui sta nella serie PT6EXO. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §GAP.
///
/// <para><b>Quando.</b> Alla chiusura della <b>prima</b> barra della sessione (quella per cui
/// <see cref="EasyEngineBase.BuildSessionOhlc"/> dice che apre una sessione nuova), quindi una volta per
/// sessione e mai piu' tardi: il gap e' <c>apertura della sessione − chiusura della precedente</c>, e
/// l'apertura della sessione e' l'apertura di questa barra. Ingresso a mercato sulla barra dopo. La
/// barra di segnale e' la prima perche' il gap si conosce solo quando la sessione ha aperto, e un ordine
/// all'apertura stessa richiederebbe di conoscerla prima.</para>
///
/// <para><b>La soglia</b> e' in ATR delle sessioni chiuse (<see cref="EasyEngineBase.ClosedSessionAtrPoints"/>,
/// la sessione in corso non entra): un gap di 50 punti e' enorme in un regime e rumore in un altro. Se
/// l'ATR non e' calcolabile non nasce nulla. <see cref="GapAtr"/> = 0 toglie la soglia: basta un gap
/// diverso da zero.</para>
///
/// <para><b>Il target al riempimento del gap.</b> Con <see cref="TargetAtGapFill"/> = 1, solo in fade,
/// il target in denaro e' la distanza fra la chiusura della barra di segnale — l'unico prezzo noto quando
/// l'ordine nasce, come per lo stop all'estremo del FBO — e la chiusura della sessione precedente, per il
/// valore del punto. Se la barra di segnale ha gia' chiuso oltre quel livello il gap e' gia' chiuso e non
/// c'e' niente da sfumare: nessun ingresso. Il fill all'apertura dopo sposta il target di quanto e' il
/// salto fra le due barre. In go il livello del gap non e' un target, e la leva accesa e' un errore.</para>
///
/// <para><b>Trappola: il gap di rollover.</b> Sul feed interno, serie continua aggiustata, i salti di
/// rollover sono gia' tolti. Sul feed di un broker no: il giorno in cui il CFD passa al contratto
/// successivo la prima barra apre lontano dalla chiusura di ieri per la sola differenza fra le due
/// scadenze, e per questo motore e' un gap vero. Il motore <b>non</b> lo filtra — dal prezzo non si
/// distingue, andrebbe riconosciuto dal calendario delle scadenze, che oggi non c'e' — quindi una cella
/// ricercata sul feed di un broker contiene qualche trade finto al mese di rollover, e il confronto con
/// lo stesso run sul feed interno e' il modo di misurarli.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla.</para>
/// </summary>
public abstract class SessionGapEngine : EasyEngineBase
{
    /// <summary>Gap minimo in multipli dell'ATR delle sessioni chiuse. 0 = basta un gap diverso da zero.</summary>
    protected decimal GapAtr = 0.3m;

    /// <summary>0 = fade (contro il gap), 1 = go (a favore).</summary>
    protected int Mode;

    /// <summary>1 = in fade il target e' il riempimento del gap (chiusura della sessione precedente); 0 = target comune.</summary>
    protected int TargetAtGapFill;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>Le sessioni dell'ATR della soglia, piu' la sessione in corso e una di margine.</summary>
    public override int RequiredCandles => Math.Max(base.RequiredCandles, SessionsToCandles(AtrSessions + 2));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (Mode is not (0 or 1) || Direction is < 0 or > 2 || GapAtr < 0m || TargetAtGapFill is not (0 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: configurazione GAP non valida (Mode {Mode}, Direction {Direction}, GapAtr {GapAtr}, " +
                $"TargetAtGapFill {TargetAtGapFill}); servono Mode 0/1, Direction 0..2, GapAtr >= 0, TargetAtGapFill 0/1.");
        }

        if (TargetAtGapFill == 1 && Mode != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetAtGapFill), TargetAtGapFill,
                $"{Name}: il target al riempimento del gap vale solo in fade (Mode 0); in go sarebbe una leva inerte.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Solo sulla prima barra della sessione: ohlc[0] e' l'apertura della sessione corrente (quella
        // di questa barra), ohlc[7] la chiusura della sessione precedente.
        if (!BuildSessionOhlc(data, barTime, out var ohlc))
            return Hold(bar.Close, barTime);

        var sessionOpen = ohlc[0];
        var previousClose = ohlc[7];
        var gap = sessionOpen - previousClose;
        if (previousClose <= 0m || gap == 0m)
            return Hold(bar.Close, barTime);

        if (GapAtr > 0m)
        {
            var atr = ClosedSessionAtrPoints(data, barTime);
            if (atr is not { } points || points <= 0m || Math.Abs(gap) < GapAtr * points)
                return Hold(bar.Close, barTime);
        }

        var gapUp = gap > 0m;
        var side = (Mode == 1) == gapUp ? SignalType.Buy : SignalType.Sell;
        if ((Direction == 1 && side != SignalType.Buy) || (Direction == 2 && side != SignalType.Sell))
            return Hold(bar.Close, barTime);

        var signal = EntryMarketNextBar(side, bar.Close, data, barTime,
            (side == SignalType.Buy ? "LE GAP " : "SE GAP ") + (Mode == 0 ? "fade" : "go"));

        if (TargetAtGapFill == 1)
        {
            // Il fade vende un gap al rialzo: il target e' la chiusura di ieri, sotto la chiusura
            // della barra di segnale. Una distanza non positiva e' un gap gia' chiuso.
            var distance = side == SignalType.Sell ? bar.Close - previousClose : previousClose - bar.Close;
            if (distance <= 0m)
                return Hold(bar.Close, barTime);

            signal.TakeProfitMoneyPerFutureContract = Math.Round(distance * InstrumentRegistry.PointValue(Symbol), 2);
        }

        return WithSessionExit(signal) ?? Hold(bar.Close, barTime);
    }
}
