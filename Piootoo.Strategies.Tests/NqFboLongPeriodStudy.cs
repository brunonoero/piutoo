using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>FBO NQ 1h sul periodo lungo.</b> La configurazione della matrice che ha superato il controllo RAN
/// (<c>ricerca/matrice/pt6exo-prime-7-celle.md</c>: 88° percentile in campione, 96° fuori) misurata
/// <b>fissa</b> sul feed del vendor dal 2008, come <see cref="Pt3b002LongPeriodTests"/> per la 002. Il
/// tratto 2008-2021 non l'ha mai visto nessuna ricerca di questa cella: e' la prova che conta.
///
/// <para>Canale 20, rientro in una barra, stop 2,5 ATR, target 1,5 ATR, nessuna ora di uscita, tenuta fino
/// a una sessione, solo long; spread e swap FTMO, commissione 0 come la matrice. Il feed e' quello del
/// future (vendor), non il CFD: prezzi e sessioni sono quelli del CME, quindi i numeri si confrontano con
/// la matrice per segno e ordine di grandezza, non al centesimo.</para>
///
/// <para>Stampa il risultato per periodo e per anno, e sul tratto 2008-2021 il percentile contro 100 semi
/// RAN solo long con le stesse uscite. CSV dei trade in <c>ricerca/matrice/nq-60-fbo-periodo-lungo-trade.csv</c>,
/// per la correlazione con i piani.</para>
/// </summary>
public sealed class NqFboLongPeriodStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;
    private static readonly DateTime StartUtc = new(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UnseenEndUtc = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Dictionary<string, object> Exits() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = "@NQ",
        ["TimeframeMinutes"] = 60,
        ["StopLoss"] = 0,
        ["TakeProfit"] = 0,
        ["StopAtr"] = 2.5m,
        ["TargetAtr"] = 1.5m,
        ["TrailingStop"] = 0,
        ["BreakEven"] = 0,
        ["MaxBars"] = 1380 / 60,
        ["IntradayOnly"] = 0,
        ["ExitHour"] = -1,
        ["StartHour"] = -1,
        ["EndHour"] = -1,
        ["Direction"] = 1
    };

    private static Dictionary<string, object> Strategy()
    {
        var parameters = Exits();
        parameters["ChannelBars"] = 20;
        parameters["ReentryBars"] = 1;
        parameters["PtnNeutYes"] = 55; parameters["PtnNeutNo"] = 56; parameters["PtnDirYes"] = 52; parameters["PtnDirNo"] = 53;
        parameters["DvolMin"] = 0; parameters["SkipDay"] = -1; parameters["OffsetTicks"] = 0;
        return parameters;
    }

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task TheFixedConfigurationOverEighteenYearsOfVendorFeed()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };

        var key = StrategyKeys.NormalizeSymbol("@NQ");
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol);
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO");

        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), "@NQ", [60, 1],
            StartUtc, EndUtc, broker: null, warmupDays: 30d);

        SweepJob Job(string id, IReadOnlyDictionary<string, object> parameters) => new(id, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread.Points[key] },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap.For(key) },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        output.WriteLine($"feed vendor {series.StartUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}, spread {spread.Points[key]} pt, commissione 0\n");

        var periods = new[]
        {
            (Name: "2008-2021 (mai visto)", From: series.StartUtc, To: UnseenEndUtc),
            (Name: "2022-2024 (campione)", From: UnseenEndUtc, To: SplitUtc),
            (Name: "2025-2026 (validazione)", From: SplitUtc, To: series.EndUtc),
            (Name: "tutto", From: series.StartUtc, To: series.EndUtc)
        };

        SweepOutcome? unseen = null;
        foreach (var (name, from, to) in periods)
        {
            var outcome = new SweepRunner(series.Between(from, to)).Run(Job("RC_FBO", Strategy()));
            output.WriteLine($"{name,-26} {outcome.Trades,5} trade  netto {outcome.NetProfit,12:N0}  DD {outcome.MaxClosedTradeDrawdown,10:N0}  " +
                             $"PF {Fmt(outcome.ProfitFactor),5}  avg {outcome.AverageTrade,7:N0}  netto/DD {Ratio(outcome),5:N2}");
            if (unseen is null) unseen = outcome;

            if (name == "tutto")
            {
                output.WriteLine("\nper anno (anno di uscita):");
                foreach (var year in outcome.ClosedTrades.GroupBy(t => t.ExitDate.Year).OrderBy(g => g.Key))
                    output.WriteLine($"  {year.Key}  {year.Count(),4} trade  netto {year.Sum(t => t.NetProfit),10:N0}");

                var csv = new StringBuilder("entry_utc,exit_utc,net\n");
                foreach (var t in outcome.ClosedTrades)
                    csv.AppendLine(string.Join(',', t.EntryDate.ToString("o"), t.ExitDate.ToString("o"), t.NetProfit.ToString("0.##", CultureInfo.InvariantCulture)));
                var path = Path.Combine(RepositoryPath, "ricerca", "matrice", "nq-60-fbo-periodo-lungo-trade.csv");
                File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
                output.WriteLine($"\nCSV: ricerca/matrice/nq-60-fbo-periodo-lungo-trade.csv");
            }
        }

        // Il controllo RAN sul tratto mai visto: stesse uscite, stesso lato, ingresso a caso.
        var half = series.Between(series.StartUtc, UnseenEndUtc);
        var p = 0.035m;
        for (var iteration = 1; iteration <= 6; iteration++)
        {
            var mean = RunSeeds(half, p, parameters => Job("RC_RAN", parameters), 1, 8).Average(o => o.Trades);
            output.WriteLine($"\n  taratura RAN #{iteration}: p = {p:0.#####} → {mean:N1} trade (obiettivo {unseen!.Trades})");
            if (mean <= 0 || Math.Abs(mean - unseen.Trades) / unseen.Trades < 0.03) break;
            p = Math.Min(1m, Math.Round(p * (decimal)(unseen.Trades / mean), 5));
        }

        var random = RunSeeds(half, p, parameters => Job("RC_RAN", parameters), FirstSeed, Seeds);
        output.WriteLine($"\nRAN 2008-2021, p = {p:0.#####}, trade medi {random.Average(r => r.Trades):N0}");
        output.WriteLine($"  {"metrica",-14}{"FBO",14}{"RAN p5",14}{"mediana",14}{"p95",14}{"percentile",12}");
        Report("netto", unseen!.NetProfit, random.Select(r => r.NetProfit));
        Report("average trade", unseen.AverageTrade, random.Select(r => r.AverageTrade));
        Report("profit factor", unseen.ProfitFactor ?? 0m, random.Select(r => r.ProfitFactor ?? 0m));
        Report("netto / DD", Ratio(unseen), random.Select(Ratio));
    }

    private static List<SweepOutcome> RunSeeds(SweepSeries half, decimal p, Func<IReadOnlyDictionary<string, object>, SweepJob> job, int firstSeed, int count)
    {
        var results = new ConcurrentBag<SweepOutcome>();
        Parallel.For(
            firstSeed, firstSeed + count,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => new SweepRunner(half),
            (seed, _, runner) =>
            {
                var parameters = Exits();
                parameters["Seed"] = seed;
                parameters["EntryProbability"] = p;
                results.Add(runner.Run(job(parameters)));
                return runner;
            },
            _ => { });
        return results.ToList();
    }

    private void Report(string metric, decimal strategy, IEnumerable<decimal> seeds)
    {
        var sorted = seeds.OrderBy(v => v).ToList();
        var below = sorted.Count(v => v < strategy) / (double)sorted.Count;
        output.WriteLine($"  {metric,-14}{strategy,14:N2}{Quantile(sorted, 0.05),14:N2}{Quantile(sorted, 0.5),14:N2}{Quantile(sorted, 0.95),14:N2}{below,12:P0}");
    }

    private static decimal Quantile(List<decimal> sorted, double q) =>
        sorted[Math.Clamp((int)Math.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static decimal Ratio(SweepOutcome o) =>
        o.MaxClosedTradeDrawdown > 0 ? o.NetProfit / o.MaxClosedTradeDrawdown : 0m;

    private static string Fmt(decimal? v) => v.HasValue ? v.Value.ToString("N2") : "n/d";
}
