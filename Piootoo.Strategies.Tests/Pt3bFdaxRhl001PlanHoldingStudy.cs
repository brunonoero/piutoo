using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <c>PT3B_FDAX_RHL_001_240</c> con la tenuta con cui e' stata cercata (overnight libero, chiusura a fine
/// sessione della ricerca, l'01:00 di Roma) contro quella del piano FTMO del DAX (flat alle 20:45 UTC per
/// 30 minuti, niente overnight): prima di metterla in un piano con quella tenuta, quanto cambia.
/// </summary>
public sealed class Pt3bFdaxRhl001PlanHoldingStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    /// <summary>
    /// Il periodo del primo backtest cBot (12/08/2025 → 26/09/2026) con il filtro dei livelli gia'
    /// scavalcati spento (come la ricerca) e acceso (come il cBot): nel cBot RHL ha emesso 54 intent,
    /// 25 rifiutati, 2 riempiti.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task WrongSideLevelsOnTheCbotPeriod()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points["FDAX"];
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs["FDAX"];
        var series = await SweepSeries.LoadAsync(new PiootooDataFeedService(new DatafeedCatalog(settings)), "@FDAX", [240, 1],
            new DateTime(2025, 8, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc), "FTMO", warmupDays: 60d);

        foreach (var reject in new[] { false, true })
        {
            var job = new SweepJob("PT3B_FDAX_RHL_001_240")
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = 1,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = spread },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = swap },
                RejectWrongSideLevels = reject,
                Holding = AccountHoldingPolicy.Default with
                {
                    AllowOvernight = false, AllowOverweek = false,
                    SessionFlatUtc = new TimeOnly(20, 45), SessionFlatWindowMinutes = 30
                }
            };
            var outcome = new SweepRunner(series).Run(job);
            var line = $"livelli scavalcati {(reject ? "SCARTATI (cBot)" : "eseguiti (ricerca)")}: {outcome.Trades} trade, netto {outcome.NetProfit:N0}";
            output.WriteLine(line);
            Console.WriteLine(line);
            foreach (var trade in outcome.ClosedTrades)
            {
                var detail = $"  {trade.EntryDate:yyyy-MM-dd HH:mm} → {trade.ExitDate:yyyy-MM-dd HH:mm} {trade.EntryPrice} → {trade.ExitPrice} {trade.NetProfit:N0} {trade.ExitReason}";
                output.WriteLine(detail);
                Console.WriteLine(detail);
            }
        }
    }

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task TheResearchHoldingAgainstThePlanHolding()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points["FDAX"];
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs["FDAX"];
        var series = await SweepSeries.LoadAsync(new PiootooDataFeedService(new DatafeedCatalog(settings)), "@FDAX", [240, 1],
            new DateTime(2020, 11, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), "FTMO", warmupDays: 30d);

        var holdings = new[]
        {
            (Name: "ricerca: overnight libero", Holding: AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }),
            (Name: "piano: flat 20:45 UTC, 30 minuti", Holding: AccountHoldingPolicy.Default with
            {
                AllowOvernight = false,
                AllowOverweek = false,
                SessionFlatUtc = new TimeOnly(20, 45),
                SessionFlatWindowMinutes = 30
            })
        };

        foreach (var (name, holding) in holdings)
        {
            foreach (var id in new[] { "PT3B_FDAX_RHL_001_240", "PT3B_FDAX_PCH_002_240" })
            {
                var job = new SweepJob(id)
                {
                    InitialCapital = 1_000_000m,
                    CommissionPerContract = 0m,
                    ClockTimeframeMinutes = 1,
                    SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = spread },
                    Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = swap },
                    Holding = holding
                };
                var outcome = new SweepRunner(series).Run(job);
                var ratio = outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
                var line = $"{name} · {id}: {outcome.Trades} trade, netto {outcome.NetProfit:N0}, DD {outcome.MaxClosedTradeDrawdown:N0}, " +
                           $"net/DD {ratio:N2}, avg {outcome.AverageTrade:N0}";
                output.WriteLine(line);
                Console.WriteLine(line);
            }
        }
    }
}
