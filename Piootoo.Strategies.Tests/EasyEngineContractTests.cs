using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Verifica il contratto comune di <see cref="EasyEngineBase"/>: next-bar di una barra,
/// rischio monetario e limite fill per sessione dichiarati sul segnale.
/// </summary>
public sealed class EasyEngineContractTests
{
    [Fact]
    public void EntryHelpersDeclareNextBarMoneyRiskAndSessionCap()
    {
        var strategy = new ContractProbe();
        var t0 = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc);
        var bars = new[]
        {
            Bar(t0.AddHours(-1), 100m),
            Bar(t0, 101m)
        };

        var stop = strategy.ProbeStop(bars, t0);
        var limit = strategy.ProbeLimit(bars, t0);
        var market = strategy.ProbeMarket(bars, t0);

        Assert.Equal(TradeOrderType.Stop, stop.OrderType);
        Assert.Equal(TradeOrderType.Limit, limit.OrderType);
        Assert.Equal(TradeOrderType.Market, market.OrderType);

        foreach (var signal in new[] { stop, limit, market })
        {
            Assert.Equal(t0.AddHours(1), signal.ValidFromUtc);
            Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
            Assert.Equal(1500, signal.StopLossMoneyPerFutureContract);
            Assert.Equal(4500, signal.TakeProfitMoneyPerFutureContract);
            Assert.Equal(800, signal.BreakEvenMoneyPerFutureContract);
            Assert.Equal(600, signal.TrailingStopMoneyPerFutureContract);
            Assert.Equal(2, signal.MaxEntriesPerSession);
            Assert.Equal(new DateTime(2024, 1, 7, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
            Assert.Null(signal.StopLoss);
            Assert.Null(signal.TakeProfit);
            Assert.Null(signal.BreakEven);
        }
    }

    private static OhlcvData Bar(DateTime time, decimal close) =>
        new()
        {
            DateTime = time,
            Open = close,
            High = close + 1m,
            Low = close - 1m,
            Close = close,
            Volume = 1m
        };

    private sealed class ContractProbe : EasyEngineBase
    {
        public ContractProbe()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            StopMoney = 1500;
            ProfitMoney = 4500;
            BreakEvenMoney = 800;
            TrailingStopMoney = 600;
            MaxEntriesPerSession = 2;
        }

        public override string Name => "CONTRACT_PROBE";
        public override string Description => "contract probe";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;

        public TradeSignal ProbeStop(OhlcvData[] data, DateTime barTime) =>
            EntryStopNextBar(SignalType.Buy, 110m, data, barTime, "stop");

        public TradeSignal ProbeLimit(OhlcvData[] data, DateTime barTime) =>
            EntryLimitNextBar(SignalType.Sell, 90m, data, barTime, "limit");

        public TradeSignal ProbeMarket(OhlcvData[] data, DateTime barTime) =>
            EntryMarketNextBar(SignalType.Buy, 101m, data, barTime, "market");
    }
}
