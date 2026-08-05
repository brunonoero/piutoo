using Piootoo.Core.Services;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Tests;

public sealed class PositionSizingTests
{
    [Fact]
    public void FormulaClampsEachCoefficientAndNeverExceedsBase()
    {
        var result = Service().Calculate(Request(baseQuantity: 10, strategy: 2));
        Assert.Equal(10, result.FinalQuantity);
        Assert.Equal(1, result.StrategyEquityMultiplier);
    }

    [Fact]
    public void AtrNeverUsesFutureBars()
    {
        var bars = new[]
        {
            Bar(1, 100, 101, 99), Bar(2, 100, 102, 98), Bar(3, 100, 1000, 1)
        };
        Assert.Equal(4m, PositionSizingService.CalculateAtr(bars, Utc(2), 14));
    }

    /// <summary>
    /// Con l'overlay CPPI rimosso (docs/decisioni.md 2026-08-05), il freno di portafoglio che resta
    /// è la riduzione proporzionale sul drawdown dal picco: a metà della soglia il moltiplicatore
    /// vale 0,5 e su un contratto singolo la quantità arrotondata scende sotto il minimo.
    /// </summary>
    [Fact]
    public void DrawdownFromPeakScalesDownTheQuantity()
    {
        var config = new PositionSizingConfig
        {
            PortfolioRisk = new PortfolioRiskSizingConfig { Enabled = true, MaximumDrawdown = 0.5m }
        };
        var result = Service().Calculate(Request(config: config, equity: 75, initial: 100));
        Assert.Equal(0.5m, result.PortfolioRiskMultiplier);
        Assert.Equal(0, result.FinalQuantity);
        Assert.Equal("BelowMinimumQuantity", result.Reason);
    }

    /// <summary>Oltre la soglia di drawdown il sizing si azzera del tutto, non si limita a ridursi.</summary>
    [Fact]
    public void DrawdownBeyondTheThresholdStopsSizingCompletely()
    {
        var config = new PositionSizingConfig
        {
            PortfolioRisk = new PortfolioRiskSizingConfig { Enabled = true, MaximumDrawdown = 0.2m }
        };
        var result = Service().Calculate(Request(baseQuantity: 10, config: config, equity: 70, initial: 100));
        Assert.Equal(0m, result.PortfolioRiskMultiplier);
        Assert.Equal(0, result.FinalQuantity);
    }

    [Theory]
    [InlineData(QuantityRoundingMode.FuturesContracts, 1, 3.9, 3)]
    [InlineData(QuantityRoundingMode.BrokerVolumeStep, 0.1, 3.99, 3.9)]
    public void BoundaryRoundsExactlyOnce(
        QuantityRoundingMode mode, decimal step, decimal quantity, decimal expected)
    {
        var result = Service().Calculate(Request(baseQuantity: quantity,
            instrument: new InstrumentMetadata
            {
                Symbol = "X", DollarsPerPoint = 1, MinimumQuantity = step,
                QuantityStep = step, RoundingMode = mode
            }));
        Assert.Equal(expected, result.FinalQuantity);
    }

    [Fact]
    public void BelowMinimumProducesAuditableReason()
    {
        var result = Service().Calculate(Request(baseQuantity: 0.5m));
        Assert.Equal(0, result.FinalQuantity);
        Assert.Equal("BelowMinimumQuantity", result.Reason);
    }

    [Fact]
    public void CoefficientsRoundTripInPersistedSignal()
    {
        var signal = new PersistedSignal
        {
            SignalId = "s", TimestampUtc = Utc(2), StrategyCode = "A",
            StrategyName = "A", Symbol = "X", BaseQuantity = 10,
            StrategyEquityMultiplier = 0.5m, MarketVolatilityMultiplier = 0.8m,
            PortfolioRiskMultiplier = 0.5m, FinalQuantity = 2
        };
        var json = System.Text.Json.JsonSerializer.Serialize(signal);
        var copy = System.Text.Json.JsonSerializer.Deserialize<PersistedSignal>(json)!;
        Assert.Equal(10, copy.BaseQuantity);
        Assert.Equal(0.5m, copy.StrategyEquityMultiplier);
        Assert.Equal(0.8m, copy.MarketVolatilityMultiplier);
        Assert.Equal(0.5m, copy.PortfolioRiskMultiplier);
        Assert.Equal(2, copy.FinalQuantity);
    }

    private static PositionSizingService Service() => new();

    private static PositionSizingRequest Request(
        decimal baseQuantity = 1, decimal strategy = 1, decimal equity = 100,
        decimal initial = 100, InstrumentMetadata? instrument = null,
        PositionSizingConfig? config = null) => new()
    {
        BaseQuantity = baseQuantity, StrategyEquityMultiplier = strategy,
        Instrument = instrument ?? new InstrumentMetadata
        {
            Symbol = "X", DollarsPerPoint = 1, MinimumQuantity = 1,
            QuantityStep = 1, RoundingMode = QuantityRoundingMode.FuturesContracts
        },
        Config = config ?? new PositionSizingConfig(), TimestampUtc = Utc(2),
        InitialCapital = initial, Equity = equity, PeakEquity = initial
    };

    private static OhlcvData Bar(int day, decimal close, decimal high, decimal low) => new()
    {
        DateTime = Utc(day), Open = close, Close = close, High = high, Low = low
    };

    private static DateTime Utc(int day) => new(2026, 1, day, 0, 0, 0, DateTimeKind.Utc);
}
