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
    public void HoldsOutsideTradingWindow()
    {
        var strategy = new TestTrendDeveloper { Start = 1200, End = 1400 };

        // La finestra si confronta con l'etichetta di CHIUSURA della barra, che e' il nome con cui
        // la ricerca la chiama. Questa apre alle 09:00 UTC — le 10:00 locali d'inverno — e chiude
        // alle 11:00 locali: fuori da 1200-1400. La barra che apre alle 10:00 UTC chiude alle 12:00
        // ed e' la PRIMA dentro, non l'ultima fuori.
        var bars = BuildSessions(new DateTime(2024, 1, 8, 9, 0, 0, DateTimeKind.Utc));

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
