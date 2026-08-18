using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore sottile per ingressi su incrocio Aroon calcolato su un timeframe superiore (data2).
///
/// <para>Replica <c>s_TOP_UA_123_CL_5____120__7.txt</c>: crossover bullish/bearish su data2 con
/// ingresso <c>next bar at market</c> e gate <c>PtnBaseSA2</c> sul grafico di esecuzione.</para>
/// </summary>
public abstract class AroonCrossoverEngine : EasyEngineBase, IMultiTimeframeTradingStrategy
{
    /// <summary>Periodo Aroon (<c>mylenght</c>).</summary>
    protected int AroonLength = 22;

    /// <summary>Timeframe aggiuntivo in minuti (data2).</summary>
    protected int HigherTimeframeMinutes = 120;

    /// <summary>Inizio finestra operativa HHMM.</summary>
    protected int StartTrade;

    /// <summary>Fine finestra operativa HHMM.</summary>
    protected int EndTrade = 2359;

    /// <summary>Giorno EasyLanguage escluso per il long. -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno EasyLanguage escluso per lo short. -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Gate PtnBaseSA2 long.</summary>
    protected int BaseYesLong = 41;

    /// <summary>Gate PtnBaseSA2 che blocca il long.</summary>
    protected int BaseNoLong = 42;

    /// <summary>Gate PtnBaseSA2 short.</summary>
    protected int BaseYesShort = 41;

    /// <summary>Gate PtnBaseSA2 che blocca lo short.</summary>
    protected int BaseNoShort = 42;

    public IReadOnlyCollection<int> AdditionalTimeframes => new[] { HigherTimeframeMinutes };

    public TradeSignal GenerateSignal(
        OhlcvData[] data,
        IReadOnlyDictionary<int, OhlcvData[]> additionalData,
        DateTime currentDate) =>
        EvaluateCore(data, additionalData, currentDate);

    protected TradeSignal EvaluateCore(
        OhlcvData[] data,
        IReadOnlyDictionary<int, OhlcvData[]>? additionalData,
        DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        if (additionalData is null ||
            !additionalData.TryGetValue(HigherTimeframeMinutes, out var higherTf) ||
            higherTf.Length < AroonLength + 1)
        {
            return Hold(data[^1].Close, currentDate, $"Serie {HigherTimeframeMinutes}m non disponibile");
        }

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (!EasyLib.TimeWindow(Clock, StartTrade, EndTrade, barTime))
            return Hold(bar.Close, barTime);

        BuildSessionOhlc(data, barTime, out var ohlc);

        var end = higherTf.Length - 1;
        var aroonUp = CalculateAroonUp(higherTf, end);
        var aroonDown = CalculateAroonDown(higherTf, end);
        var prevAroonUp = CalculateAroonUp(higherTf, end - 1);
        var prevAroonDown = CalculateAroonDown(higherTf, end - 1);

        var bullishCross = prevAroonUp <= prevAroonDown && aroonUp > aroonDown;
        var bearishCross = prevAroonUp >= prevAroonDown && aroonUp < aroonDown;

        var entries = new List<TradeSignal>(2);

        if (bullishCross &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            EasyLib.PtnBaseSA2(BaseYesLong, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoLong, ohlc))
        {
            entries.Add(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE Aroon"));
        }

        if (bearishCross &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            EasyLib.PtnBaseSA2(BaseYesShort, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoShort, ohlc))
        {
            entries.Add(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE Aroon"));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private decimal CalculateAroonUp(OhlcvData[] data, int endIndex)
    {
        if (endIndex < 0) return 0m;
        var barsAgo = HighestBarIndex(data, endIndex, AroonLength);
        return (AroonLength - barsAgo) / (decimal)AroonLength * 100m;
    }

    private decimal CalculateAroonDown(OhlcvData[] data, int endIndex)
    {
        if (endIndex < 0) return 0m;
        var barsAgo = LowestBarIndex(data, endIndex, AroonLength);
        return (AroonLength - barsAgo) / (decimal)AroonLength * 100m;
    }

    private static int HighestBarIndex(OhlcvData[] data, int endIndex, int length)
    {
        var start = Math.Max(0, endIndex - length + 1);
        var max = decimal.MinValue;
        var barsAgo = 0;
        for (var index = endIndex; index >= start; index--)
        {
            if (data[index].High < max) continue;
            max = data[index].High;
            barsAgo = endIndex - index;
        }

        return barsAgo;
    }

    private static int LowestBarIndex(OhlcvData[] data, int endIndex, int length)
    {
        var start = Math.Max(0, endIndex - length + 1);
        var min = decimal.MaxValue;
        var barsAgo = 0;
        for (var index = endIndex; index >= start; index--)
        {
            if (data[index].Low > min) continue;
            min = data[index].Low;
            barsAgo = endIndex - index;
        }

        return barsAgo;
    }
}
