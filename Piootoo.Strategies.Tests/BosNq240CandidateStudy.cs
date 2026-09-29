using System.Collections.Concurrent;
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
/// <b>BOS sul Nasdaq a 4 ore con il flat prima del rollover</b> (30/09/2026): la configurazione trovata dal percorso
/// con la tenuta dei piani PT3B (<c>ricerca/percorso/nq-240-ricerca-ftmo-flat.md</c>). Passa tutti i cancelli della
/// ricerca e si ferma sulla prova per l'average trade (450 contro 483).
///
/// <para>Le tre verifiche che hanno deciso la PCH del DAX a 1 ora e la RHL dell'S&amp;P a 1 ora: il Nasdaq 2008 →
/// 05/2022 sul feed interno con il minuto, mai visto; la regola identica sugli altri indici FTMO; 100 ingressi
/// casuali con le stesse uscite, sulla prova e sulla storia interna. Costi FTMO, orologio al minuto, niente
/// overnight e flat alle 20:45 UTC. Resoconto in <c>ricerca/percorso/nq-240-bos-flat-verifiche.md</c>.</para>
/// </summary>
public sealed class BosNq240CandidateStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;
    private static readonly string[] OtherIndices = ["@FDAX", "@ES", "@YM", "@FESX", "@FCE", "@Z", "@NIY"];

    private static readonly AccountHoldingPolicy Holding = AccountHoldingPolicy.Default with
    {
        AllowOvernight = false, AllowOverweek = false, SessionFlatUtc = new TimeOnly(20, 45), SessionFlatWindowMinutes = 30
    };

    private static Dictionary<string, object> Rule(string symbol) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = symbol, ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = 1, ["LevelSource"] = 1,
        ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 6, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53, ["SkipDay"] = -1,
        ["BreakoutOffsetTicks"] = 10
    };

    /// <summary>Le uscite e i vincoli della BOS: stop 1 ATR, intraday, finestra fino all'01, entrambi i lati.</summary>
    private static Dictionary<string, object> RandomExits(string symbol) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = symbol, ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = 1, ["MaxEntriesPerSession"] = 1, ["Direction"] = 0
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task BosNq240AgainstLongHistoryOtherIndicesAndChance()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spreads = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points;
        var swaps = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs;
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        SweepJob Job(string container, IReadOnlyDictionary<string, object> parameters)
        {
            var key = ((string)parameters["Symbol"]).TrimStart('@');
            return new SweepJob(container, parameters)
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = 1,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spreads[key] },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swaps[key] },
                Holding = Holding
            };
        }

        var report = new StringBuilder("# BOS Nasdaq 4 ore con il flat: le tre verifiche\n\n");
        report.AppendLine("Regola identica alla finalista del percorso (`nq-240-ricerca-ftmo-flat.md`): breakout della sessione in corso con " +
                          "LevelSource 1, offset 10 tick, finestra fino all'01, pattern neutro NO 6, stop 1 ATR, nessun target, intraday. " +
                          "Costi FTMO, orologio al minuto, niente overnight, flat alle 20:45 UTC. Nessun parametro ritoccato.\n");

        var broker = await SweepSeries.LoadAsync(dataFeed, "@NQ", [240, 1], Utc(2022, 5, 16), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
        var internalHistory = await SweepSeries.LoadAsync(dataFeed, "@NQ", [240, 1], Utc(2008, 1, 1), Utc(2022, 5, 16), null, warmupDays: 30d);
        var periods = new[]
        {
            ("ricerca FTMO 05/2022-09/2024", broker.Between(broker.StartUtc, Utc(2024, 9, 1))),
            ("prova FTMO 09/2024-09/2026", broker.Between(Utc(2024, 9, 1), broker.EndUtc)),
            ("interno 2008-05/2022, mai visto", internalHistory)
        };

        report.AppendLine("## 1. Nasdaq: ricerca, prova e storia interna mai vista\n");
        report.AppendLine("| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
        var nq = new List<(string Name, SweepSeries Series, SweepOutcome Outcome)>();
        foreach (var (name, series) in periods)
        {
            var outcome = new SweepRunner(series).Run(Job("RC_SBO", Rule("@NQ")));
            nq.Add((name, series, outcome));
            var years = outcome.ClosedTrades.GroupBy(t => t.ExitDate.Year).OrderBy(g => g.Key)
                .Select(g => (Year: g.Key, Net: g.Sum(t => t.NetProfit))).ToList();
            report.AppendLine($"| {name} | {outcome.Trades} | {outcome.NetProfit:N0} | {outcome.MaxClosedTradeDrawdown:N0} | {Ratio(outcome):0.00} | " +
                              $"{outcome.AverageTrade:N0} | {years.Count(y => y.Net > 0)}/{years.Count} | " +
                              string.Join(" ", years.Select(y => $"{y.Year}:{y.Net / 1000m:0}")) + " |");
        }

        report.AppendLine("\n## 2. La regola identica sugli altri indici (FTMO 11/2020-09/2026)\n");
        report.AppendLine("| indice | trade | netto | DD chiuso | net/DD | average trade |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");
        var good = 0;
        var measured = 0;
        foreach (var index in OtherIndices)
        {
            var key = index.TrimStart('@');
            if (!spreads.ContainsKey(key) || !swaps.ContainsKey(key) ||
                !File.Exists(Path.Combine(RepositoryPath, "datafeed-external", "FTMO", $"{index}_1.json")))
            {
                report.AppendLine($"| {index} | - | - | - | - | manca feed o costo |");
                continue;
            }

            var series = await SweepSeries.LoadAsync(dataFeed, index, [240, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
            var outcome = new SweepRunner(series).Run(Job("RC_SBO", Rule(index)));
            measured++;
            if (outcome.NetProfit > 0m && Ratio(outcome) >= 1m) good++;
            report.AppendLine($"| {index} | {outcome.Trades} | {outcome.NetProfit:N0} | {outcome.MaxClosedTradeDrawdown:N0} | {Ratio(outcome):0.00} | {outcome.AverageTrade:N0} |");
        }
        report.AppendLine($"\n{good} su {measured} con net/DD ≥ 1 (il percorso chiede almeno la meta').\n");
        output.WriteLine(report.ToString());

        report.AppendLine("## 3. Contro 100 ingressi casuali con le stesse uscite\n");
        report.AppendLine("RC_RAN riceve finestra fino all'01, un ingresso per sessione, entrambi i lati, stop 1 ATR, intraday, e sceglie a caso il quando " +
                          "e il lato. Percentile = quota di semi che fa peggio della strategia.\n");
        foreach (var (name, series, real) in nq.Skip(1))
        {
            var p = Calibrate(series, real.Trades, parameters => Job("RC_RAN", parameters), name);
            var random = RunSeeds(series, p, parameters => Job("RC_RAN", parameters)).Select(r => r.Result).ToList();
            report.AppendLine($"### {name}\n");
            report.AppendLine($"BOS {real.Trades} trade, netto {real.NetProfit:N0}. RAN p = {p:0.#####}, trade medi {random.Average(r => r.Trades):N0}.\n");
            report.AppendLine("| metrica | BOS | RAN p5 | mediana | p95 | percentile |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            report.AppendLine(Row("netto", real.NetProfit, random.Select(r => r.NetProfit)));
            report.AppendLine(Row("average trade", real.AverageTrade, random.Select(r => r.AverageTrade)));
            report.AppendLine(Row("netto / DD", Ratio(real), random.Select(Ratio)));
            report.AppendLine();
        }

        File.WriteAllText(Path.Combine(RepositoryPath, "ricerca", "percorso", "nq-240-bos-flat-verifiche.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
    }

    private decimal Calibrate(SweepSeries series, int target, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string name)
    {
        var p = 0.05m;
        for (var iteration = 1; iteration <= 8; iteration++)
        {
            var mean = RunSeeds(series, p, job, firstSeed: 1, count: 8).Average(entry => entry.Result.Trades);
            output.WriteLine($"  taratura {name} #{iteration}: p = {p:0.#####} → {mean:N1} trade (obiettivo {target})");
            if (mean <= 0 || Math.Abs(mean - target) / target < 0.03)
                break;
            p = Math.Min(1m, Math.Round(p * (decimal)(target / mean), 5));
        }

        return p;
    }

    private static List<(int Seed, SweepOutcome Result)> RunSeeds(
        SweepSeries series, decimal p, Func<IReadOnlyDictionary<string, object>, SweepJob> job, int firstSeed = FirstSeed, int count = Seeds)
    {
        var results = new ConcurrentBag<(int, SweepOutcome)>();
        Parallel.For(firstSeed, firstSeed + count,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
            () => new SweepRunner(series),
            (seed, _, runner) =>
            {
                var parameters = RandomExits("@NQ");
                parameters["Seed"] = seed;
                parameters["EntryProbability"] = p;
                results.Add((seed, runner.Run(job(parameters))));
                return runner;
            },
            _ => { });
        return results.ToList();
    }

    private static string Row(string metric, decimal strategy, IEnumerable<decimal> seeds)
    {
        var sorted = seeds.OrderBy(v => v).ToList();
        var below = sorted.Count(v => v < strategy) / (double)sorted.Count;
        return $"| {metric} | {strategy:N2} | {Quantile(sorted, 0.05):N2} | {Quantile(sorted, 0.5):N2} | {Quantile(sorted, 0.95):N2} | {below:P0} |";
    }

    private static decimal Quantile(List<decimal> sorted, double q) =>
        sorted[Math.Clamp((int)Math.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static decimal Ratio(SweepOutcome o) => o.MaxClosedTradeDrawdown > 0 ? o.NetProfit / o.MaxClosedTradeDrawdown : 0m;
}
