using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <c>PT3B_FDAX_RHL_001_240</c> riproduce trade per trade il contenitore <c>RC_RHL</c> con i parametri
/// della finalista, sul feed FTMO della ricerca e della prova e sul feed interno 2008-2020: e' il passo
/// della procedura di promozione che separa una classe sbagliata da una ricerca sbagliata.
/// </summary>
public sealed class Pt3bFdaxRhl001ReproductionStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task TheClassReproducesTheContainerTradeByTrade()
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
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        var container = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Symbol"] = "@FDAX", ["TimeframeMinutes"] = 240,
            ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
            ["StopAtr"] = 1.0m, ["TargetAtr"] = 1.0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = 10, ["PtnNeutYes"] = 34, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52,
            ["PtnDirNo"] = 53, ["SkipDay"] = -1, ["LevelOffsetTicks"] = 20, ["Direction"] = 1
        };

        SweepJob Job(string id, IReadOnlyDictionary<string, object>? parameters) => new(id, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = spread },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = swap },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        var feeds = new[]
        {
            (Name: "FTMO 2020-2026", Series: await SweepSeries.LoadAsync(dataFeed, "@FDAX", [240, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d)),
            (Name: "interno 2008-2020", Series: await SweepSeries.LoadAsync(dataFeed, "@FDAX", [240, 1], Utc(2008, 1, 1), Utc(2020, 11, 9), null, warmupDays: 30d))
        };

        foreach (var (name, series) in feeds)
        {
            var reference = new SweepRunner(series).Run(Job("RC_RHL", container));
            var promoted = new SweepRunner(series).Run(Job("PT3B_FDAX_RHL_001_240", null));
            output.WriteLine($"{name}: contenitore {reference.Trades} trade {reference.NetProfit:N2}, classe {promoted.Trades} trade {promoted.NetProfit:N2}");

            Assert.Equal(reference.Trades, promoted.Trades);
            Assert.Equal(reference.NetProfit, promoted.NetProfit);
            Assert.Equal(
                reference.ClosedTrades.Select(t => (t.EntryDate, t.EntryPrice, t.ExitDate, t.ExitPrice)),
                promoted.ClosedTrades.Select(t => (t.EntryDate, t.EntryPrice, t.ExitDate, t.ExitPrice)));
        }
    }
}
