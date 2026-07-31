using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>Tipo di media mobile usata dal motore MAC.</summary>
public enum MovingAverageType
{
    Simple,
    Exponential
}

/// <summary>Versi che il motore può aprire.</summary>
public enum MovingAverageCrossoverDirection
{
    Both,
    LongOnly,
    ShortOnly
}

/// <summary>
/// Motore riusabile per crossover fra due medie mobili.
///
/// <para>Traduce <c>s_UA_MC_SMA_CROSS</c> e <c>TOP_UA_101</c>: il cross è osservato alla
/// chiusura della barra e l'ingresso è un market sulla barra successiva. I filtri opzionali
/// coprono inoltre <c>TOP_UA_772</c>: setup daily della sessione precedente, controllo della
/// pendenza relativa e ingresso solo flat.</para>
///
/// <para>Oltre alle uscite dichiarate da <see cref="EasyEngineBase"/>, MAC chiude una posizione
/// esistente sul crossover inverso (next bar) e alla chiusura della sessione di venerdì.</para>
/// </summary>
public abstract class MovingAverageCrossoverEngine : EasyEngineBase
{
    /// <summary>Tipo di entrambe le medie (Average/AverageFC = <see cref="MovingAverageType.Simple"/>).</summary>
    protected MovingAverageType AverageType = MovingAverageType.Simple;

    /// <summary>Periodo della media veloce.</summary>
    protected int FastPeriod = 20;

    /// <summary>Periodo della media lenta.</summary>
    protected int SlowPeriod = 50;

    /// <summary>Versi abilitati.</summary>
    protected MovingAverageCrossoverDirection Direction = MovingAverageCrossoverDirection.Both;

    /// <summary>
    /// Richiede posizione flat prima dell'ingresso, come <c>TOP_UA_772</c>. Quando è false,
    /// il cross opposto può essere inviato all'engine per l'eventuale inversione.
    /// </summary>
    protected bool RequireFlatPosition;

    /// <summary>Abilita la finestra oraria di ingresso.</summary>
    protected bool UseTradingWindow;

    /// <summary>Inizio finestra HHMM, inclusa.</summary>
    protected int StartTradeTime;

    /// <summary>Fine finestra HHMM, esclusa, coerente con <c>tw()</c>.</summary>
    protected int EndTradeTime = 2359;

    /// <summary>Numero massimo di ingressi giornalieri; 0 significa illimitato.</summary>
    protected int MaxEntriesPerDay;

    /// <summary>
    /// Richiede che la sessione giornaliera precedente sia positiva per il long o negativa per lo
    /// short, con corpo/range non oltre <see cref="DailyBodyFactor"/>.
    /// </summary>
    protected bool UseDailyFilter;

    /// <summary>Massimo rapporto assoluto corpo/range della sessione precedente.</summary>
    protected decimal DailyBodyFactor = 0.5m;

    /// <summary>
    /// Barre fra le due letture della media veloce e lenta per il controllo pendenza; 0 disattiva
    /// il filtro. Corrisponde a <c>myGradientLength</c> di 772.
    /// </summary>
    protected int GradientPeriod;

    /// <summary>Rapporto minimo fra pendenza veloce e pendenza lenta.</summary>
    protected decimal GradientFactor;

    /// <summary>Abilita i gate PatternNeutralFast/PatternDirectionalFast di TOP_UA_101.</summary>
    protected bool UsePatternFilter;

    /// <summary>Pattern neutro richiesto.</summary>
    protected int NeutralPatternYes = 31;

    /// <summary>Pattern neutro che impedisce l'ingresso.</summary>
    protected int NeutralPatternNo = 56;

    /// <summary>Pattern direzionale richiesto, con segno applicato al verso.</summary>
    protected int DirectionalPatternYes = 52;

    /// <summary>Pattern direzionale che impedisce l'ingresso, con segno applicato al verso.</summary>
    protected int DirectionalPatternNo = 29;

    /// <summary>
    /// 772 non valuta il setup daily prima di almeno un giorno di barre. Il requisito cresce
    /// automaticamente quando tale filtro è attivo, oltre ai periodi delle medie e del gradiente.
    /// </summary>
    public override int RequiredCandles
    {
        get
        {
            var averageLookback = Math.Max(1, Math.Max(FastPeriod, SlowPeriod));
            var gradientLookback = GradientPeriod > 0 ? GradientPeriod + averageLookback : averageLookback;
            var dailyLookback = UseDailyFilter ? Math.Max(1, 1440 / Math.Max(1, TimeframeMinutes)) + 1 : 0;
            return Math.Max(gradientLookback + 1, dailyLookback);
        }
    }

    /// <summary>Valuta cross, ingressi market next-bar e le uscite MAC.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        if (FastPeriod <= 0 || SlowPeriod <= 0)
            return Hold(data[^1].Close, data[^1].DateTime, "Periodi media non validi");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (UseTradingWindow && !EasyLib.TimeWindow(StartTradeTime, EndTradeTime, barTime))
            return Hold(bar.Close, barTime);

        if (MaxEntriesPerDay > 0 && EntriesTodayCount >= MaxEntriesPerDay)
            return Hold(bar.Close, barTime, "Tetto ingressi giornalieri raggiunto");

        if (RequireFlatPosition && CurrentMP != 0)
            return Hold(bar.Close, barTime, "Posizione non flat");

        var fast = MovingAverage(data, FastPeriod, 0);
        var slow = MovingAverage(data, SlowPeriod, 0);
        var previousFast = MovingAverage(data, FastPeriod, 1);
        var previousSlow = MovingAverage(data, SlowPeriod, 1);
        var crossesOver = fast > slow && previousFast <= previousSlow;
        var crossesUnder = fast < slow && previousFast >= previousSlow;

        // Il Python valuta il cross alla close k ed esegue il reverse alla barra k+1.
        // Il reverse non è un ingresso opposto: chiude soltanto la posizione corrente.
        if (IsFridaySessionEnd(barTime))
        {
            if (CurrentMP == 1)
                return ExitMarketNow(SignalType.Sell, bar.Close, barTime, "LX_FRIDAY_EOD");
            if (CurrentMP == -1)
                return ExitMarketNow(SignalType.Buy, bar.Close, barTime, "SX_FRIDAY_EOD");
        }

        if (CurrentMP == 1 && crossesUnder)
            return ExitMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "LX_REVERSE_CROSS");
        if (CurrentMP == -1 && crossesOver)
            return ExitMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "SX_REVERSE_CROSS");

        if ((!crossesOver && !crossesUnder) || !PassesGradient(data, fast, slow))
            return Hold(bar.Close, barTime);

        decimal[]? ohlc = null;
        if (UsePatternFilter)
        {
            BuildSessionOhlc(data, barTime, out var sessionOhlc);
            ohlc = sessionOhlc;
            if (!EasyLib.PatternNeutralFast(NeutralPatternYes, ohlc) ||
                EasyLib.PatternNeutralFast(NeutralPatternNo, ohlc))
            {
                return Hold(bar.Close, barTime);
            }
        }

        var entries = new List<TradeSignal>(2);
        if (crossesOver &&
            Direction is not MovingAverageCrossoverDirection.ShortOnly &&
            PassesDailyFilter(data, barTime, SignalType.Buy) &&
            PassesDirectionalPattern(ohlc, SignalType.Buy))
        {
            entries.Add(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE_CROSS"));
        }

        if (crossesUnder &&
            Direction is not MovingAverageCrossoverDirection.LongOnly &&
            PassesDailyFilter(data, barTime, SignalType.Sell) &&
            PassesDirectionalPattern(ohlc, SignalType.Sell))
        {
            entries.Add(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE_CROSS"));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool IsFridaySessionEnd(DateTime barTime)
    {
        if (barTime.DayOfWeek != DayOfWeek.Friday)
            return false;

        var barEnd = barTime.AddMinutes(TimeframeMinutes);
        return Hhmm(barEnd) == SessionEndTime;
    }

    private TradeSignal ExitMarketNextBar(
        SignalType side, decimal referencePrice, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryMarketNextBar(side, referencePrice, data, barTime, reason);
        signal.ExitOnly = true;
        return signal;
    }

    private TradeSignal ExitMarketNow(SignalType side, decimal price, DateTime barTime, string reason) =>
        new()
        {
            Date = barTime,
            Type = side,
            Price = price,
            StrategyName = Name,
            OrderType = TradeOrderType.Market,
            ExitOnly = true,
            Reason = reason
        };

    private bool PassesGradient(OhlcvData[] data, decimal fast, decimal slow)
    {
        if (GradientPeriod <= 0)
            return true;

        var priorFast = MovingAverage(data, FastPeriod, GradientPeriod);
        var priorSlow = MovingAverage(data, SlowPeriod, GradientPeriod);
        return Math.Abs(fast - priorFast) >= GradientFactor * Math.Abs(slow - priorSlow);
    }

    private bool PassesDailyFilter(OhlcvData[] data, DateTime barTime, SignalType side)
    {
        if (!UseDailyFilter)
            return true;

        var open = EasyLib.GetDailyOpen(data, barTime, 1);
        var high = EasyLib.GetDailyHigh(data, barTime, 1);
        var low = EasyLib.GetDailyLow(data, barTime, 1);
        var close = EasyLib.GetDailyClose(data, barTime, 1);
        var range = high - low;
        if (range <= 0m || Math.Abs(close - open) / range > DailyBodyFactor)
            return false;

        return side == SignalType.Buy ? close > open : close < open;
    }

    private bool PassesDirectionalPattern(decimal[]? ohlc, SignalType side)
    {
        if (!UsePatternFilter || ohlc is null)
            return true;

        var sign = side == SignalType.Buy ? 1 : -1;
        return EasyLib.PatternDirectionalFast(sign * DirectionalPatternYes, ohlc) &&
               !EasyLib.PatternDirectionalFast(sign * DirectionalPatternNo, ohlc);
    }

    private decimal MovingAverage(OhlcvData[] data, int period, int barsAgo)
    {
        var end = data.Length - 1 - barsAgo;
        var start = end - period + 1;
        if (start < 0)
            return 0m;

        if (AverageType == MovingAverageType.Simple)
        {
            decimal total = 0m;
            for (var i = start; i <= end; i++)
                total += data[i].Close;
            return total / period;
        }

        var ema = data[0].Close;
        var multiplier = 2m / (period + 1m);
        for (var i = 1; i <= end; i++)
            ema += multiplier * (data[i].Close - ema);
        return ema;
    }
}
