using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: filtro range OK_L/OK_S + ingresso a mercato sul cross Bollinger (RBB_U usa limit).

/// <summary>
/// TOP_UA_506 — Bollinger cross con filtro di range, GC 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_506_GC_30__7.txt</c>. Dopo un setup
/// di compressione (<c>okl</c>/<c>oks</c>) entra a mercato sul cross della banda, non con
/// limit sulla banda come <see cref="RbbUnmirroredEngine"/>.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.100, target long $3.800 /
/// short $3.500, breakeven $2.000, max 5 sessioni in posizione.</para>
/// </summary>
public sealed class Easy_506_GC_30 : EasyEngineBase
{
    private const int HighestPeriodLong = 61;
    private const int LowestPeriodLong = 61;
    private const int HighestPeriodShort = 48;
    private const int LowestPeriodShort = 64;

    private bool _okLong;
    private bool _okShort;
    private decimal _prevClose;

    public override string Name => "Easy_506_GC_30";
    public override string Description => "Bollinger cross con filtro range, GC 30m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 30;

    private int PeriodLong { get; set; } = 38;
    private int PeriodShort { get; set; } = 32;
    private int FastYesLong { get; set; } = 152;
    private int FastNoLong { get; set; } = 8;
    private int FastYesShort { get; set; } = 4;
    private int FastNoShort { get; set; } = 98;
    private int StartTrade { get; set; } = 0;
    private int EndTrade { get; set; } = 2300;
    private int BollingerLength { get; set; } = 20;
    private decimal BollingerNumDevs { get; set; } = 2m;
    private decimal RangeMultiplierLong { get; set; } = 0.8m;
    private decimal RangeMultiplierShort { get; set; } = 0.8m;

    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, Math.Max(BollingerLength + 1, HighestPeriodLong + 1));

    public Easy_506_GC_30()
    {
        SessionStartTime = 1800;  // sessionStartTimeC
        SessionEndTime = 1700;    // sessionEndTimeC
        Contracts = 1;

        StopMoney = 1100;       // MyStopl / MyStops
        ProfitMoney = 3800;     // MyProfitl (short 3500 — approssimato al long)
        BreakEvenMoney = 2000;  // MyBreakeven
        MaxDaysInTrade = 5;     // maxdaysintrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        UpdateRangeFlags(data, bar);

        if (CurrentMP != 0)
        {
            _okLong = false;
            _okShort = false;
        }

        GetBands(data, BollingerLength, BollingerNumDevs, out var upperBand, out var lowerBand);

        if (!EasyLib.TimeWindow(StartTrade, EndTrade, barTime))
        {
            _prevClose = bar.Close;
            return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);

        if (_okShort && _prevClose > upperBand && bar.Close <= upperBand &&
            EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc))
        {
            _okShort = false;
            entries.Add(WithExitSettings(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE BB cross")));
        }

        if (_okLong && _prevClose < lowerBand && bar.Close >= lowerBand &&
            EasyLib.PatternFast(FastYesLong, ohlc) && !EasyLib.PatternFast(FastNoLong, ohlc))
        {
            _okLong = false;
            entries.Add(WithExitSettings(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE BB cross")));
        }

        _prevClose = bar.Close;
        return Combine(entries, Hold(bar.Close, barTime));
    }

    private void UpdateRangeFlags(OhlcvData[] data, OhlcvData bar)
    {
        var highestLong = EasyLib.Highest(data, HighestPeriodLong, d => d.High);
        var lowestLong = EasyLib.Lowest(data, LowestPeriodLong, d => d.Low);
        var highestShort = EasyLib.Highest(data, HighestPeriodShort, d => d.High);
        var lowestShort = EasyLib.Lowest(data, LowestPeriodShort, d => d.Low);
        var rangeLong = highestLong - lowestLong;
        var rangeShort = highestShort - lowestShort;

        var highestPeriodLong = EasyLib.Highest(data, PeriodLong, d => d.High);
        var lowestPeriodShort = EasyLib.Lowest(data, PeriodShort, d => d.Low);
        var prevBar = data[^2];

        if (prevBar.Low > highestPeriodLong - rangeLong * RangeMultiplierLong &&
            bar.Low < highestPeriodLong - rangeLong * RangeMultiplierLong)
        {
            _okLong = true;
        }

        if (prevBar.High < lowestPeriodShort + rangeShort * RangeMultiplierShort &&
            bar.High > lowestPeriodShort + rangeShort * RangeMultiplierShort)
        {
            _okShort = true;
        }
    }

    private static void GetBands(OhlcvData[] data, int length, decimal numDevs, out decimal upper, out decimal lower)
    {
        var end = data.Length - 1;
        var start = end - length + 1;
        decimal sum = 0m;
        for (var index = start; index <= end; index++)
            sum += data[index].Close;

        var average = sum / length;
        decimal squaredDifferenceSum = 0m;
        for (var index = start; index <= end; index++)
        {
            var difference = data[index].Close - average;
            squaredDifferenceSum += difference * difference;
        }

        var standardDeviation = (decimal)Math.Sqrt((double)(squaredDifferenceSum / length));
        upper = average + numDevs * standardDeviation;
        lower = average - numDevs * standardDeviation;
    }

    private TradeSignal WithExitSettings(TradeSignal signal)
    {
        if (MaxDaysInTrade > 0)
            signal.CloseAtUtc = signal.ValidFromUtc!.Value.Date.AddDays(MaxDaysInTrade);
        return signal;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("Mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
    }
}
