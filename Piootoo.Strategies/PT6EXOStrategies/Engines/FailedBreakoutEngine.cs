using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore FBO: <b>falso breakout</b> ("turtle soup"). Il prezzo rompe il canale delle ultime barre e
/// rientra: si entra <b>contro</b> la rottura. E' la famiglia della serie PT6EXO costruita per essere
/// anti-correlata con i breakout del catalogo (PCH, SBO, VBO), che su quella stessa rottura sono
/// entrati e ora perdono. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §FBO.
///
/// <para><b>Il livello.</b> Il canale e' quello di <see cref="ChannelBars"/> barre che finiscono
/// <b>prima</b> della finestra di rottura: massimo degli alti per lo short, minimo dei bassi per il
/// long. La finestra di rottura sono le ultime <see cref="ReentryBars"/> barre, compresa quella appena
/// chiusa. Tenere il canale fuori dalla finestra e' cio' che rende il livello fisso mentre la rottura
/// si consuma: un canale che la includesse si alzerebbe con la rottura stessa e il rientro non
/// esisterebbe mai.</para>
///
/// <para><b>Il segnale</b> (short; il long e' lo specchio sui bassi):</para>
/// <list type="number">
///   <item>nella finestra c'e' una rottura: un alto oltre il livello di almeno
///   <see cref="MinBreakAtr"/> volte l'ATR delle sessioni chiuse (0 = basta superarlo);</item>
///   <item>la barra appena chiusa chiude <b>sotto</b> il livello;</item>
///   <item>ed e' il primo rientro: o la barra stessa ha bucato il livello (rottura e rientro nella
///   stessa barra, il caso classico con <see cref="ReentryBars"/> = 1), o la barra prima aveva chiuso
///   sopra. Senza questa condizione una rottura produrrebbe un segnale per ogni barra rimasta sotto.</item>
/// </list>
///
/// <para><b>L'ingresso</b> e' a mercato sulla barra dopo: il rientro si conosce alla chiusura, e un
/// ordine al livello sarebbe gia' scavalcato (il prezzo e' sotto) — un <c>CrossedLevel</c> per
/// costruzione. Lo stop e' quello comune della base (denaro o ATR) oppure, con
/// <see cref="StopAtExtreme"/>, <b>oltre l'estremo della rottura</b> di <see cref="ExtremeBufferTicks"/>:
/// e' lo stop naturale del falso breakout, perche' se il prezzo torna oltre l'estremo la rottura non
/// era falsa. La distanza si misura dalla chiusura della barra di segnale, l'unico prezzo noto quando
/// l'ordine nasce; il fill all'apertura dopo la sposta di quanto e' il gap.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla, come nel Price Channel.</para>
/// </summary>
public abstract class FailedBreakoutEngine : EasyEngineBase
{
    /// <summary>Barre del canale, prima della finestra di rottura.</summary>
    protected int ChannelBars = 20;

    /// <summary>Barre entro cui la rottura deve rientrare, compresa quella di segnale. 1 = stessa barra.</summary>
    protected int ReentryBars = 1;

    /// <summary>Profondita' minima della rottura oltre il livello, in multipli dell'ATR delle sessioni chiuse. 0 = spento.</summary>
    protected decimal MinBreakAtr;

    /// <summary>1 = stop oltre l'estremo della rottura; 0 = lo stop comune della base (denaro o ATR).</summary>
    protected int StopAtExtreme;

    /// <summary>Margine oltre l'estremo della rottura, in tick, quando <see cref="StopAtExtreme"/> e' acceso.</summary>
    protected int ExtremeBufferTicks;

    /// <summary>Dimensione del tick dello strumento, per <see cref="ExtremeBufferTicks"/>.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// Canale, finestra di rottura, la barra prima della finestra e, se la profondita' minima e' in
    /// ATR, le sessioni dell'ATR.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(
            Math.Max(1, ChannelBars) + Math.Max(1, ReentryBars) + 1,
            MinBreakAtr > 0m ? SessionsToCandles(AtrSessions + 2) : 0));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles || ChannelBars <= 0 || ReentryBars <= 0)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var last = data.Length - 1;
        var windowStart = last - ReentryBars + 1;
        var channelStart = windowStart - ChannelBars;

        // Canale e finestra in un giro solo, senza LINQ: e' il percorso caldo.
        var channelHigh = data[channelStart].High;
        var channelLow = data[channelStart].Low;
        for (var index = channelStart + 1; index < windowStart; index++)
        {
            if (data[index].High > channelHigh) channelHigh = data[index].High;
            if (data[index].Low < channelLow) channelLow = data[index].Low;
        }

        var breakHigh = data[windowStart].High;
        var breakLow = data[windowStart].Low;
        for (var index = windowStart + 1; index <= last; index++)
        {
            if (data[index].High > breakHigh) breakHigh = data[index].High;
            if (data[index].Low < breakLow) breakLow = data[index].Low;
        }

        var minBreak = 0m;
        if (MinBreakAtr > 0m)
        {
            var atr = ClosedSessionAtrPoints(data, barTime);
            if (atr is not { } points || points <= 0m)
                return Hold(bar.Close, barTime);
            minBreak = MinBreakAtr * points;
        }

        var previousClose = data[last - 1].Close;
        var entries = new List<TradeSignal>(2);

        // Short: rottura sopra il canale, prima chiusura di nuovo sotto.
        if (Direction != 1 &&
            breakHigh > channelHigh + minBreak &&
            bar.Close < channelHigh &&
            (bar.High > channelHigh || previousClose >= channelHigh))
        {
            AddEntry(entries, Entry(SignalType.Sell, breakHigh, data, barTime, "SE FBO"));
        }

        // Long: rottura sotto il canale, prima chiusura di nuovo sopra.
        if (Direction != 2 &&
            breakLow < channelLow - minBreak &&
            bar.Close > channelLow &&
            (bar.Low < channelLow || previousClose <= channelLow))
        {
            AddEntry(entries, Entry(SignalType.Buy, breakLow, data, barTime, "LE FBO"));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal? Entry(SignalType side, decimal extreme, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryMarketNextBar(side, data[^1].Close, data, barTime, reason);

        if (StopAtExtreme != 0)
        {
            var buffer = ExtremeBufferTicks * TickSize;
            var distance = side == SignalType.Sell
                ? extreme + buffer - data[^1].Close
                : data[^1].Close - (extreme - buffer);

            // Una distanza nulla non e' uno stop: l'estremo coincide con la chiusura solo su una barra
            // piatta, e li' vale lo stop comune.
            if (distance > 0m)
                signal.StopLossMoneyPerFutureContract =
                    Math.Round(distance * InstrumentRegistry.PointValue(Symbol), 2);
        }

        return WithSessionExit(signal);
    }
}
