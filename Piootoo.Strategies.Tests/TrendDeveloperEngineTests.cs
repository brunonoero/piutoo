using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Fixture minima per TrendDeveloper: ingresso stop next-bar su estremi d0,
/// rifiuto fuori finestra e uscite monetarie dichiarate sull'ingresso.
/// </summary>
public sealed class TrendDeveloperEngineTests
{
    [Fact]
    public void EmitsStopNextBarOnCurrentSessionExtremesWithMoneyExits()
    {
        var strategy = new TestTrendDeveloper();
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);
        var shortSignal = Assert.Single(signal.CompanionSignals!);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(205m, signal.Price); // highd0 aggregato della sessione corrente
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        // lowd0 include la barra di apertura sessione (17:05 del giorno precedente).
        Assert.Equal(156m, shortSignal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1000, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(3000, signal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(500, signal.BreakEvenMoneyPerFutureContract);
        Assert.Equal(200, signal.TrailingStopMoneyPerFutureContract);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        Assert.NotNull(signal.EntrySessionStartUtc);
    }

    [Fact]
    public void HoldsOutsideTradingWindow()
    {
        var strategy = new TestTrendDeveloper { Start = 1200, End = 1400 };
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);
        Assert.Equal(SignalType.Hold, signal.Type);
    }

    [Fact]
    public void MarketEntrySkipsSessionLevelRequirement()
    {
        var strategy = new TestTrendDeveloper { UseMarket = true };
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].Close, signal.Price);
    }

    private static OhlcvData[] BuildSessions(DateTime current)
    {
        var bars = new List<OhlcvData>();
        for (var day = -7; day <= -1; day++)
        {
            var sessionDate = current.Date.AddDays(day);
            var basePrice = 100m + (day + 7) * 10m;
            var start = sessionDate.AddHours(17).AddMinutes(5);
            var end = sessionDate.AddDays(1).AddHours(16);
            if (start <= current)
                bars.Add(Bar(start, basePrice, basePrice + 8m, basePrice - 4m));
            if (end <= current)
                bars.Add(Bar(end, basePrice + 2m, basePrice + 6m, basePrice - 2m));
        }

        bars.Add(Bar(current.AddHours(-1), 199m, 204m, 194m));
        bars.Add(Bar(current, 200m, 205m, 195m));
        return bars.OrderBy(bar => bar.DateTime).ToArray();
    }

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = high,
            Low = low,
            Close = open + 1m,
            Volume = 1m
        };

    private sealed class TestTrendDeveloper : TrendDeveloperEngine
    {
        public int Start { set => StartTrade = value; }
        public int End { set => EndTrade = value; }
        public bool UseMarket { set => MarketEntry = value; }

        public TestTrendDeveloper()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1659;
            Trigger = TrendTrigger.CurrentSessionOhlc;
            StartTrade = 0;
            EndTrade = 2359;
            InclusiveWindowEnd = true;
            NeutralYes = 55;
            NeutralNo = 56;
            DirectionalYes = 52;
            DirectionalNo = 53;
            StopMoney = 1000;
            ProfitMoney = 3000;
            BreakEvenMoney = 500;
            TrailingStopMoney = 200;
            MaxEntriesPerSession = 1;
        }

        public override string Name => "TEST_TD";
        public override string Description => "TrendDeveloper fixture";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;

        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
            EvaluateCore(data, currentDate);
    }
}
