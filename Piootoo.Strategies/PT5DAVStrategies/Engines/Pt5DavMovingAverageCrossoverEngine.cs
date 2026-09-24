using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// MAC della ricerca PT5DAV: incrocio di due medie semplici sulla close. Stessa logica del motore
/// condiviso <c>MovingAverageCrossoverEngine</c> — incrocio sulla barra corrente contro la precedente,
/// filtro gradiente, filtro sulla sessione precedente, ingresso market alla barra dopo — sulla base
/// comune PT5DAV.
///
/// <para><b>Uscite.</b> Sull'incrocio inverso, a mercato alla barra dopo (segnale <c>ExitOnly</c>,
/// come il motore condiviso); e all'<b>ultima barra del venerdi'</b>, che la scheda dichiara e che
/// qui si scrive all'ingresso come <c>CloseAtUtc</c>: e' l'uscita al limite del CFD del venerdi' della
/// settimana d'ingresso (17:00 NY su BP), cosi' vale anche a server muto. Nessuna uscita a fine
/// sessione negli altri giorni: la MAC tiene la notte per costruzione.</para>
/// </summary>
public abstract class Pt5DavMovingAverageCrossoverEngine : Pt5DavEngineBase
{
    /// <summary><c>fast</c>.</summary>
    protected int FastPeriod;

    /// <summary><c>slow</c>.</summary>
    protected int SlowPeriod;

    /// <summary><c>gradient_length</c>: barre su cui si misura lo spostamento delle medie.</summary>
    protected int GradientPeriod;

    /// <summary><c>gradient_factor</c>: la veloce deve essersi mossa almeno questo multiplo della lenta.</summary>
    protected decimal GradientFactor;

    /// <summary><c>daily_factor</c>: la sessione precedente e' di indecisione se |C−O| ≤ fattore × (H−L).</summary>
    protected decimal DailyFactor;

    /// <summary><c>direction</c>: 0 entrambe, 1 solo long, 2 solo short.</summary>
    protected int Direction;

    protected Pt5DavMovingAverageCrossoverEngine()
    {
        IntradayOnly = false;
    }

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        var lookback = Math.Max(FastPeriod, SlowPeriod) + Math.Max(2, GradientPeriod);
        if (FastPeriod <= 0 || SlowPeriod <= 0 || RecentMarketBars(data, lookback + 1) is not { } bars)
            return Hold(bar.Close, barTime, "Storia insufficiente per le medie");

        var fast = Average(bars, FastPeriod, 0);
        var slow = Average(bars, SlowPeriod, 0);
        var previousFast = Average(bars, FastPeriod, 1);
        var previousSlow = Average(bars, SlowPeriod, 1);
        var crossesOver = fast > slow && previousFast <= previousSlow;
        var crossesUnder = fast < slow && previousFast >= previousSlow;

        // L'uscita per incrocio inverso la ricerca la esegue alla CHIUSURA della barra dopo quella
        // dell'incrocio, una barra piu' tardi dell'ingresso sull'incrocio (che e' all'apertura).
        // Misurato su NQ-15M-MAC, periodo broker FTMO: 74 uscite su 74 al close della barra dopo il
        // segnale, ingressi invece al tick. Quindi l'uscita guarda l'incrocio della barra PRECEDENTE
        // ed esce all'apertura della prossima, cioe' alla chiusura di questa.
        var crossedUnderBefore = previousFast < previousSlow &&
                                 Average(bars, FastPeriod, 2) >= Average(bars, SlowPeriod, 2);
        var crossedOverBefore = previousFast > previousSlow &&
                                Average(bars, FastPeriod, 2) <= Average(bars, SlowPeriod, 2);

        if (CurrentMP == 1 && crossedUnderBefore)
            return ExitNextBar(SignalType.Sell, bar, data, "LX MAC incrocio inverso");
        if (CurrentMP == -1 && crossedOverBefore)
            return ExitNextBar(SignalType.Buy, bar, data, "SX MAC incrocio inverso");

        if ((!crossesOver && !crossesUnder) || !PassesGradient(bars, fast, slow))
            return Hold(bar.Close, barTime);

        var (open1, high1, low1, close1) = (ohlc[4], ohlc[5], ohlc[6], ohlc[7]);
        var undecided = Math.Abs(close1 - open1) <= DailyFactor * (high1 - low1);

        var entries = new List<TradeSignal>(1);
        if (crossesOver && Direction != 2 && undecided && close1 > open1)
            AddEntry(entries, WithWeekendExit(Finish(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE MAC"), oneEntryPerSessionPerSide: false)));

        if (crossesUnder && Direction != 1 && undecided && close1 < open1)
            AddEntry(entries, WithWeekendExit(Finish(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE MAC"), oneEntryPerSessionPerSide: false)));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>
    /// L'uscita di fine settimana, dichiarata all'ingresso. Misurata sui trade di tutte le 23 MAC:
    /// <list type="bullet">
    ///   <item>fino a 60 minuti, alla chiusura dell'ultima barra del venerdi' entro il limite del CFD
    ///   (17:00 NY);</item>
    ///   <item>a 4 ore, alla chiusura della barra di giovedi' 20:00-24:00 di Roma, cioe' all'apertura
    ///   della sessione di venerdi' — su tutte e sette le MAC a 4 ore (BP 140 uscite forzate su 266, GC
    ///   106 su 122). La scheda non lo spiega: e' la regola misurata, non dedotta.</item>
    /// </list>
    /// Un ingresso che nasce dopo l'uscita della propria settimana prende quella della settimana dopo.
    /// </summary>
    private TradeSignal? WithWeekendExit(TradeSignal? signal)
    {
        if (signal is null)
            return null;

        var fill = signal.ValidFromUtc!.Value;
        var friday = ResearchSessionDay(fill);
        while (DaysSinceMonday(friday) != 4)
            friday = friday.AddDays(1);

        var weekend = WeekendExitUtc(friday);
        if (weekend <= fill)
            weekend = WeekendExitUtc(friday.AddDays(7));

        weekend = weekend.AddMinutes(-1);
        if (signal.CloseAtUtc is not { } declared || weekend < declared)
            signal.CloseAtUtc = weekend;
        return signal;
    }

    private DateTime WeekendExitUtc(DateTime friday) =>
        TimeframeMinutes >= 240 ? SessionOpenUtc(friday) : IntradayExitUtc(friday);

    private TradeSignal ExitNextBar(SignalType side, OhlcvData bar, OhlcvData[] data, string reason)
    {
        var signal = EntryMarketNextBar(side, bar.Close, data, bar.DateTime, reason);
        signal.ExitOnly = true;
        return signal;
    }

    private bool PassesGradient(OhlcvData[] bars, decimal fast, decimal slow)
    {
        if (GradientPeriod <= 0)
            return true;

        var priorFast = Average(bars, FastPeriod, GradientPeriod);
        var priorSlow = Average(bars, SlowPeriod, GradientPeriod);
        return Math.Abs(fast - priorFast) >= GradientFactor * Math.Abs(slow - priorSlow);
    }

    /// <summary>Media semplice delle close di <paramref name="period"/> barre, <paramref name="barsAgo"/> barre fa.</summary>
    private static decimal Average(OhlcvData[] bars, int period, int barsAgo)
    {
        var end = bars.Length - 1 - barsAgo;
        decimal sum = 0m;
        for (var index = end - period + 1; index <= end; index++)
            sum += bars[index].Close;
        return sum / period;
    }
}
