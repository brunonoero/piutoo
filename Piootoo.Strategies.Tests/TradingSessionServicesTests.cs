using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Tests;

public sealed class TradingSessionServicesTests
{
    [Theory]
    [InlineData(5, 5, 1)]
    [InlineData(15, 5, 0)]
    [InlineData(15, 15, 1)]
    [InlineData(7, 7, 1)]
    [InlineData(15, 7, 0)]
    public void Evaluation_IsDrivenByExplicitBarClose(int strategyTimeframe, int eventTimeframe, int expected)
    {
        var strategy = new RecordingStrategy(strategyTimeframe);
        var service = new StrategyEvaluationService();
        var time = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var result = service.Evaluate(
            [strategy],
            Bar(eventTimeframe, time),
            [Ohlcv(time)],
            _ => Execution(time));

        Assert.Equal(expected, strategy.Evaluations);
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void ExternalReportStatuses_AreMutuallyExplicit()
    {
        Assert.Equal(
            ["Accepted", "PartiallyFilled", "Filled", "Rejected", "Cancelled"],
            Enum.GetNames<ExecutionReportStatus>());
        Assert.NotEqual(ExecutionMode.ServerSimulated, ExecutionMode.ExternalBroker);
    }

    [Fact]
    public void IntentIdsAndReportIds_AreSeparateIdempotencyBoundaries()
    {
        var report = new ExternalExecutionReport
        {
            ReportId = "report-1",
            IntentId = "intent-1",
            Status = ExecutionReportStatus.PartiallyFilled,
            CumulativeFilledQuantity = 0.5m,
            EventTimeUtc = DateTime.UtcNow
        };
        Assert.NotEqual(report.ReportId, report.IntentId);
        Assert.Equal(0.5m, report.CumulativeFilledQuantity);
    }

    private static ClosedBar Bar(int timeframe, DateTime time) => new()
    {
        Symbol = "NQ",
        TimeframeMinutes = timeframe,
        BarTimeUtc = time,
        Sequence = 1,
        IdempotencyKey = $"NQ-{timeframe}-1",
        Bar = Ohlcv(time)
    };

    private static OhlcvData Ohlcv(DateTime time) => new()
    {
        DateTime = time,
        Open = 100,
        High = 102,
        Low = 99,
        Close = 101,
        Volume = 1
    };

    private static StrategyExecutionSnapshot Execution(DateTime time) => new()
    {
        StrategyCode = "test",
        Symbol = "NQ",
        BarTimeUtc = time
    };

    private sealed class RecordingStrategy(int timeframe) : ITradingStrategy
    {
        public int Evaluations { get; private set; }
        public string Name => $"test-{timeframe}";
        public string Description => "test";
        public string Symbol => "NQ";
        public int TimeframeMinutes => timeframe;
        public int RequiredCandles => 1;
        public TradeSignal Evaluate(StrategyEvaluationRequest request)
        {
            Evaluations++;
            return new TradeSignal { Type = SignalType.Buy, Price = request.Ohlcv[^1].Close };
        }
        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) => Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = data,
            BarTimeUtc = currentDate,
            Execution = Execution(currentDate)
        });
        public void Initialize(Dictionary<string, object>? parameters = null) { }
    }
}
