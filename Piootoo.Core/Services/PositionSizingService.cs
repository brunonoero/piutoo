using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

public sealed class PositionSizingRequest
{
    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; } = 1m;
    public required InstrumentMetadata Instrument { get; init; }
    public required PositionSizingConfig Config { get; init; }
    public IReadOnlyList<OhlcvData> AvailableBars { get; init; } = [];
    public DateTime TimestampUtc { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal Equity { get; init; }
    public decimal PeakEquity { get; init; }
    public decimal GrossExposureFraction { get; init; }
}

public sealed class PositionSizingResult
{
    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; }
    public decimal MarketVolatilityMultiplier { get; init; }
    public decimal PortfolioRiskMultiplier { get; init; }
    public decimal UnroundedQuantity { get; init; }
    public decimal FinalQuantity { get; init; }
    public string? Reason { get; init; }
}

public interface IPositionSizingService
{
    PositionSizingResult Calculate(PositionSizingRequest request);
}

public sealed class PositionSizingService : IPositionSizingService
{
    public PositionSizingResult Calculate(PositionSizingRequest request)
    {
        Validate(request);
        var strategy = Clamp(request.StrategyEquityMultiplier, request.Config);
        var market = Clamp(MarketMultiplier(request), request.Config);
        var portfolio = Clamp(PortfolioMultiplier(request), request.Config);
        var unrounded = request.BaseQuantity * strategy * market * portfolio;
        var step = request.Instrument.RoundingMode == QuantityRoundingMode.FuturesContracts
            ? Math.Max(1m, request.Instrument.QuantityStep)
            : request.Instrument.QuantityStep;
        var final = Math.Floor(unrounded / step) * step;
        var reason = final < request.Instrument.MinimumQuantity ? "BelowMinimumQuantity" : null;
        if (reason is not null) final = 0;
        return new PositionSizingResult
        {
            BaseQuantity = request.BaseQuantity,
            StrategyEquityMultiplier = strategy,
            MarketVolatilityMultiplier = market,
            PortfolioRiskMultiplier = portfolio,
            UnroundedQuantity = unrounded,
            FinalQuantity = final,
            Reason = reason
        };
    }

    public static decimal CalculateAtr(
        IReadOnlyList<OhlcvData> bars, DateTime timestampUtc, int periods)
    {
        var eligible = bars.Where(x => x.DateTime <= timestampUtc)
            .OrderBy(x => x.DateTime).TakeLast(periods + 1).ToArray();
        if (eligible.Length < 2) return 0;
        return eligible.Skip(1).Select((bar, index) =>
        {
            var priorClose = eligible[index].Close;
            return Math.Max(bar.High - bar.Low,
                Math.Max(Math.Abs(bar.High - priorClose), Math.Abs(bar.Low - priorClose)));
        }).Average();
    }

    private static decimal MarketMultiplier(PositionSizingRequest request)
    {
        var config = request.Config.MarketVolatility;
        if (!config.Enabled) return 1m;
        var atr = CalculateAtr(request.AvailableBars, request.TimestampUtc, config.AtrPeriods);
        var riskPerUnit = atr * request.Instrument.DollarsPerPoint;
        return riskPerUnit <= 0 ? 0m : config.TargetRiskDollars / riskPerUnit / request.BaseQuantity;
    }

    private static decimal PortfolioMultiplier(PositionSizingRequest request)
    {
        var config = request.Config.PortfolioRisk;
        if (!config.Enabled) return 1m;
        var peak = Math.Max(request.PeakEquity, request.InitialCapital);
        var drawdown = peak <= 0 ? 1m : Math.Max(0, (peak - request.Equity) / peak);
        if (drawdown >= config.MaximumDrawdown ||
            request.GrossExposureFraction >= config.MaximumGrossExposure) return 0m;
        var multiplier = Math.Min(
            1m - drawdown / Math.Max(0.000001m, config.MaximumDrawdown),
            1m - request.GrossExposureFraction / Math.Max(0.000001m, config.MaximumGrossExposure));
        if (config.EnableCppi)
        {
            var floor = request.InitialCapital * config.CppiFloorFraction;
            var cushion = Math.Max(0, request.Equity - floor);
            var cppiBudget = cushion * config.CppiMultiplier;
            multiplier = Math.Min(multiplier, request.Equity <= 0 ? 0 : cppiBudget / request.Equity);
        }
        if (config.EnableAggressiveModules)
            multiplier = Math.Min(config.MaximumMultiplier, multiplier * config.FractionalFactor);
        return multiplier;
    }

    private static decimal Clamp(decimal value, PositionSizingConfig config) =>
        config.ClampMultipliersToUnitInterval ? Math.Clamp(value, 0m, 1m) : Math.Max(0m, value);

    private static void Validate(PositionSizingRequest request)
    {
        if (request.BaseQuantity < 0 || request.Instrument.DollarsPerPoint <= 0 ||
            request.Instrument.MinimumQuantity <= 0 || request.Instrument.QuantityStep <= 0 ||
            request.Config.MarketVolatility.AtrPeriods <= 0 ||
            request.Config.MarketVolatility.TargetRiskDollars <= 0 ||
            request.Config.PortfolioRisk.MaximumDrawdown is <= 0 or > 1 ||
            request.Config.PortfolioRisk.MaximumGrossExposure is <= 0 or > 1 ||
            request.Config.PortfolioRisk.CppiFloorFraction is < 0 or > 1 ||
            request.Config.PortfolioRisk.CppiMultiplier < 0 ||
            request.Config.PortfolioRisk.FractionalFactor is < 0 or > 1)
            throw new ArgumentException("Configurazione position sizing non valida.");
        if (!request.Config.PortfolioRisk.EnableAggressiveModules &&
            request.Config.PortfolioRisk.MaximumMultiplier > 1)
            throw new ArgumentException("Un cap superiore a 1 richiede moduli aggressivi esplicitamente abilitati.");
    }
}
