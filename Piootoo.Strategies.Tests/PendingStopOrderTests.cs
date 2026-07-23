using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

public class PendingStopOrderTests
{
    [Fact]
    public void ProcessSignals_DefersStopUntilNextBarTouch()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m);

        var signalTime = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        var nextBar = signalTime.AddMinutes(5);
        var signal = new TradeSignal
        {
            Date = signalTime,
            Type = SignalType.Buy,
            Price = 15010m,
            Symbol = "NQ",
            StrategyName = "TOP_UA_152",
            StrategyCode = "TOP_UA_152",
            Quantity = 1,
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = nextBar,
            StopLossMoneyPerFutureContract = 1200m
        };

        // Barra di emissione: nessun fill
        service.ProcessSignals(
            [signal],
            new Dictionary<string, decimal> { ["NQ"] = 15000m },
            new Dictionary<string, OhlcvData>
            {
                ["NQ"] = new OhlcvData
                {
                    DateTime = signalTime,
                    Open = 15000,
                    High = 15005,
                    Low = 14995,
                    Close = 15000
                }
            },
            signalTime);

        var snapBefore = service.GetExecutionSnapshot("TOP_UA_152", "NQ", signalTime);
        Assert.Null(snapBefore.Position);

        // Barra successiva: high tocca lo stop
        service.UpdateMarketPrices(
            new Dictionary<string, decimal> { ["NQ"] = 15012m },
            new Dictionary<string, OhlcvData>
            {
                ["NQ"] = new OhlcvData
                {
                    DateTime = nextBar,
                    Open = 15000,
                    High = 15015,
                    Low = 14998,
                    Close = 15012
                }
            },
            nextBar);

        var snapAfter = service.GetExecutionSnapshot("TOP_UA_152", "NQ", nextBar);
        Assert.NotNull(snapAfter.Position);
        Assert.Equal(SignalType.Buy, snapAfter.Position!.Direction);
        Assert.Equal(1, snapAfter.EntriesToday);
        Assert.Equal(20m, snapAfter.DollarsPerPoint); // NQ
    }
}
