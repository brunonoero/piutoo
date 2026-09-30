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
/// <b>BOS sul Dow a 1 ora con il flat prima del rollover</b> (30/09/2026): la configurazione trovata dal percorso con
/// la tenuta dei piani PT3B (<c>ricerca/percorso/ym-60-ricerca-ftmo-flat.md</c>). Passa tutti i cancelli della ricerca
/// e la prova sul broker (net/DD 5,61, 4/4 finestre); fallisce gli altri mercati (1 su 7: solo l'S&amp;P, 3,05).
///
/// <para>Le verifiche: il caso (100 ingressi casuali con le stesse uscite) sulla prova del Dow; il Dow interno non ha
/// barre orarie ne' il minuto, quindi la storia lunga si misura sull'S&amp;P 2006 → 11/2020 (orologio a 60 minuti), il
/// solo mercato dove la regola regge, con il caso anche li'. Costi FTMO, niente overnight e flat alle 20:45 UTC.
/// Resoconto in <c>ricerca/percorso/ym-60-bos-flat-verifiche.md</c>.</para>
/// </summary>
public sealed class BosYm60CandidateStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;

    private static readonly AccountHoldingPolicy Holding = AccountHoldingPolicy.Default with
    {
        AllowOvernight = false, AllowOverweek = false, SessionFlatUtc = new TimeOnly(20, 45), SessionFlatWindowMinutes = 30
    };

    private static Dictionary<string, object> Rule(string symbol) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = symbol, ["TimeframeMinutes"] = 60,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 0m, ["MaxBars"] = 23, ["IntradayOnly"] = 0, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = -1, ["LevelSource"] = 1,
        ["PtnNeutYes"] = 23, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = -45, ["SkipDay"] = -1,
        ["BreakoutOffsetTicks"] = 2
    };

    /// <summary>Le uscite e i vincoli della BOS: stop 1 ATR, 23 barre, un ingresso per sessione, entrambi i lati.</summary>
    private static Dictionary<string, object> RandomExits(string symbol) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = symbol, ["TimeframeMinutes"] = 60,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 0m, ["MaxBars"] = 23, ["IntradayOnly"] = 0, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = -1, ["MaxEntriesPerSession"] = 1, ["Direction"] = 0
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task BosYm60AgainstChanceAndTheSpLongHistory()
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

        SweepJob Job(string container, IReadOnlyDictionary<string, object> parameters, int clock)
        {
            var key = ((string)parameters["Symbol"]).TrimStart('@');
            return new SweepJob(container, parameters)
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = clock,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spreads[key] },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swaps[key] },
                Holding = Holding
            };
        }

        var ym = await SweepSeries.LoadAsync(dataFeed, "@YM", [60, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
        var es = await SweepSeries.LoadAsync(dataFeed, "@ES", [60, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
        var esInternal = await SweepSeries.LoadAsync(dataFeed, "@ES", [60], Utc(2006, 3, 1), Utc(2020, 11, 9), null, warmupDays: 30d);
        var cases = new (string Name, string Symbol, SweepSeries Series, int Clock, bool Chance)[]
        {
            ("Dow, ricerca FTMO 11/2020-09/2024", "@YM", ym.Between(ym.StartUtc, Utc(2024, 9, 1)), 1, false),
            ("Dow, prova FTMO 09/2024-09/2026", "@YM", ym.Between(Utc(2024, 9, 1), ym.EndUtc), 1, true),
            ("S&P, FTMO 11/2020-09/2026", "@ES", es, 1, false),
            ("S&P, interno 2006-11/2020, mai visto (orologio a 60 minuti)", "@ES", esInternal, 60, true)
        };

        var report = new StringBuilder("# BOS Dow 1 ora con il flat: le verifiche\n\n");
        report.AppendLine("Regola identica alla finalista del percorso (`ym-60-ricerca-ftmo-flat.md`): breakout della sessione in corso, LevelSource 1, " +
                          "offset 2 tick, pattern neutro YES 23, direzionale NO -45, stop 1 ATR, nessun target, al massimo 23 barre. Costi FTMO, " +
                          "niente overnight, flat alle 20:45 UTC. Nessun parametro ritoccato.\n");
        report.AppendLine("## 1. Sui periodi\n");
        report.AppendLine("| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
        var outcomes = new List<(string Name, string Symbol, SweepSeries Series, int Clock, bool Chance, SweepOutcome Outcome)>();
        foreach (var c in cases)
        {
            var outcome = new SweepRunner(c.Series).Run(Job("RC_SBO", Rule(c.Symbol), c.Clock));
            outcomes.Add((c.Name, c.Symbol, c.Series, c.Clock, c.Chance, outcome));
            var years = outcome.ClosedTrades.GroupBy(t => t.ExitDate.Year).OrderBy(g => g.Key)
                .Select(g => (Year: g.Key, Net: g.Sum(t => t.NetProfit))).ToList();
            report.AppendLine($"| {c.Name} | {outcome.Trades} | {outcome.NetProfit:N0} | {outcome.MaxClosedTradeDrawdown:N0} | {Ratio(outcome):0.00} | " +
                              $"{outcome.AverageTrade:N0} | {years.Count(y => y.Net > 0)}/{years.Count} | " +
                              string.Join(" ", years.Select(y => $"{y.Year}:{y.Net / 1000m:0.#}")) + " |");
        }
        output.WriteLine(report.ToString());

        report.AppendLine("\n## 2. Contro 100 ingressi casuali con le stesse uscite\n");
        report.AppendLine("RC_RAN riceve stop 1 ATR, al massimo 23 barre, un ingresso per sessione, entrambi i lati, e sceglie a caso il quando e il lato. " +
                          "Percentile = quota di semi che fa peggio della strategia.\n");
        foreach (var (name, symbol, series, clock, _, real) in outcomes.Where(o => o.Chance))
        {
            var p = Calibrate(series, real.Trades, parameters => Job("RC_RAN", parameters, clock), name, symbol);
            var random = RunSeeds(series, p, parameters => Job("RC_RAN", parameters, clock), symbol).Select(r => r.Result).ToList();
            report.AppendLine($"### {name}\n");
            report.AppendLine($"BOS {real.Trades} trade, netto {real.NetProfit:N0}. RAN p = {p:0.#####}, trade medi {random.Average(r => r.Trades):N0}.\n");
            report.AppendLine("| metrica | BOS | RAN p5 | mediana | p95 | percentile |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            report.AppendLine(Row("netto", real.NetProfit, random.Select(r => r.NetProfit)));
            report.AppendLine(Row("average trade", real.AverageTrade, random.Select(r => r.AverageTrade)));
            report.AppendLine(Row("netto / DD", Ratio(real), random.Select(Ratio)));
            report.AppendLine();
        }

        File.WriteAllText(Path.Combine(RepositoryPath, "ricerca", "percorso", "ym-60-bos-flat-verifiche.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
    }

    private decimal Calibrate(SweepSeries series, int target, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string name, string symbol)
    {
        var p = 0.05m;
        for (var iteration = 1; iteration <= 8; iteration++)
        {
            var mean = RunSeeds(series, p, job, symbol, firstSeed: 1, count: 8).Average(entry => entry.Result.Trades);
            output.WriteLine($"  taratura {name} #{iteration}: p = {p:0.#####} → {mean:N1} trade (obiettivo {target})");
            if (mean <= 0 || Math.Abs(mean - target) / target < 0.03)
                break;
            p = Math.Min(1m, Math.Round(p * (decimal)(target / mean), 5));
        }

        return p;
    }

    private static List<(int Seed, SweepOutcome Result)> RunSeeds(
        SweepSeries series, decimal p, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string symbol, int firstSeed = FirstSeed, int count = Seeds)
    {
        var results = new ConcurrentBag<(int, SweepOutcome)>();
        Parallel.For(firstSeed, firstSeed + count,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
            () => new SweepRunner(series),
            (seed, _, runner) =>
            {
                var parameters = RandomExits(symbol);
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
