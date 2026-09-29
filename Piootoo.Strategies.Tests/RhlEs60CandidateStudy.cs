using System.Collections.Concurrent;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>RHL sull'S&amp;P a 1 ora con il flat prima del rollover</b> (29/09/2026): la configurazione trovata dal percorso
/// con la tenuta dei piani PT3B (<c>ricerca/percorso/es-60-ricerca-ftmo-flat.md</c>). Long a limite 10 tick sotto il
/// minimo di ieri, a qualunque ora, pattern direzionale 1 vietato, domenica esclusa, stop 0,8 ATR, nessun target,
/// uscita dopo 4 barre o alle 21, intraday. Passa la prova sul broker e gli altri mercati; fallisce anni e outlier,
/// come la RHL del DAX a 4 ore prima della sua promozione.
///
/// <para>Le verifiche prima di una classe: il caso (100 ingressi long casuali con le stesse uscite) sulla prova e
/// sulla storia interna dell'S&amp;P, gli anni sulla storia interna, e la sovrapposizione con
/// <c>PT3B_ES_RHL_001_240</c>, che compra lo stesso minimo di ieri sullo stesso mercato ed e' gia' nel piano USA.
/// La storia interna ha solo le barre orarie: li' l'orologio e' a 60 minuti e stop e uscite si valutano sulla barra,
/// non sul minuto. Resoconto in <c>ricerca/percorso/es-60-rhl-flat-verifiche.md</c>.</para>
/// </summary>
public sealed class RhlEs60CandidateStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;

    private static readonly AccountHoldingPolicy Holding = AccountHoldingPolicy.Default with
    {
        AllowOvernight = false, AllowOverweek = false, SessionFlatUtc = new TimeOnly(20, 45), SessionFlatWindowMinutes = 30
    };

    private static Dictionary<string, object> Rule() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = "@ES", ["TimeframeMinutes"] = 60,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 0.8m, ["TargetAtr"] = 0m, ["MaxBars"] = 4, ["IntradayOnly"] = 1, ["ExitHour"] = 21,
        ["StartHour"] = -1, ["EndHour"] = -1,
        ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 1,
        ["SkipDay"] = 0, ["LevelOffsetTicks"] = 10, ["Direction"] = 1
    };

    /// <summary>La regola di PT3B_ES_RHL_001_240, nelle chiavi del contenitore.</summary>
    private static Dictionary<string, object> RuleOnFourHours() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = "@ES", ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 1.0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = 10,
        ["PtnNeutYes"] = 34, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
        ["SkipDay"] = -1, ["LevelOffsetTicks"] = 20, ["Direction"] = 1
    };

    private static Dictionary<string, object> RandomExits() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = "@ES", ["TimeframeMinutes"] = 60,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 0.8m, ["TargetAtr"] = 0m, ["MaxBars"] = 4, ["IntradayOnly"] = 1, ["ExitHour"] = 21,
        ["StartHour"] = -1, ["EndHour"] = -1, ["MaxEntriesPerSession"] = 1, ["Direction"] = 1
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task RhlEs60AgainstChanceLongHistoryAndTheFourHourRule()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points["ES"];
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs["ES"];
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        SweepJob Job(string container, IReadOnlyDictionary<string, object> parameters, int clock) => new(container, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = clock,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["ES"] = spread },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { ["ES"] = swap },
            Holding = Holding
        };

        var broker = await SweepSeries.LoadAsync(dataFeed, "@ES", [60, 240, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
        var holdout = broker.Between(Utc(2024, 9, 1), broker.EndUtc);
        var internalHistory = await SweepSeries.LoadAsync(dataFeed, "@ES", [60], Utc(2006, 3, 1), Utc(2020, 11, 9), null, warmupDays: 30d);

        var report = new StringBuilder("# RHL S&P 1 ora con il flat: le verifiche\n\n");
        report.AppendLine("Regola identica alla finalista del percorso (`es-60-ricerca-ftmo-flat.md`): long a limite 10 tick sotto il minimo di ieri, " +
                          "pattern direzionale 1 vietato, domenica esclusa, stop 0,8 ATR, nessun target, uscita dopo 4 barre o alle 21, intraday. " +
                          "Costi FTMO, niente overnight, flat alle 20:45 UTC.\n");

        var periods = new List<(string Name, SweepSeries Series, int Clock)>
        {
            ("ricerca FTMO 11/2020-09/2024", broker.Between(broker.StartUtc, Utc(2024, 9, 1)), 1),
            ("prova FTMO 09/2024-09/2026", holdout, 1),
            ("interno 2006-11/2020, mai visto (orologio a 60 minuti)", internalHistory, 60)
        };

        report.AppendLine("## 1. Sui periodi\n");
        report.AppendLine("| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
        var results = new List<(string Name, SweepSeries Series, int Clock, SweepOutcome Outcome)>();
        foreach (var (name, series, clock) in periods)
        {
            var outcome = new SweepRunner(series).Run(Job("RC_RHL", Rule(), clock));
            results.Add((name, series, clock, outcome));
            var years = outcome.ClosedTrades.GroupBy(t => t.ExitDate.Year).OrderBy(g => g.Key)
                .Select(g => (Year: g.Key, Net: g.Sum(t => t.NetProfit))).ToList();
            report.AppendLine($"| {name} | {outcome.Trades} | {outcome.NetProfit:N0} | {outcome.MaxClosedTradeDrawdown:N0} | {Ratio(outcome):0.00} | " +
                              $"{outcome.AverageTrade:N0} | {years.Count(y => y.Net > 0)}/{years.Count} | " +
                              string.Join(" ", years.Select(y => $"{y.Year}:{y.Net / 1000m:0.#}")) + " |");
        }
        output.WriteLine(report.ToString());

        // La sovrapposizione con la RHL a 4 ore dello stesso mercato, sulla prova.
        report.AppendLine("\n## 2. Sovrapposizione con PT3B_ES_RHL_001_240 (prova FTMO)\n");
        var hourly = results[1].Outcome.ClosedTrades;
        var fourHours = new SweepRunner(holdout).Run(Job("RC_RHL", RuleOnFourHours(), 1)).ClosedTrades;
        var sameDay = hourly.Count(t => fourHours.Any(f => f.EntryDate.Date == t.EntryDate.Date));
        var within = hourly.Count(t => fourHours.Any(f => Math.Abs((f.EntryDate - t.EntryDate).TotalMinutes) <= 5));
        report.AppendLine($"RHL 1 ora {hourly.Count} trade, RHL 4 ore {fourHours.Count} trade. Trade della 1 ora con un trade della 4 ore " +
                          $"lo stesso giorno: {sameDay} ({Share(sameDay, hourly.Count)}); ingresso entro 5 minuti: {within} ({Share(within, hourly.Count)}). " +
                          $"Correlazione del P&L giornaliero: {DailyCorrelation(hourly, fourHours):0.00}.\n");

        // Contro il caso.
        report.AppendLine("## 3. Contro 100 ingressi long casuali con le stesse uscite\n");
        report.AppendLine("RC_RAN riceve stop 0,8 ATR, uscita dopo 4 barre o alle 21, un ingresso per sessione, solo long, e sceglie a caso il quando. " +
                          "Percentile = quota di semi che fa peggio della strategia.\n");
        foreach (var (name, series, clock, real) in results.Skip(1))
        {
            var p = Calibrate(series, real.Trades, parameters => Job("RC_RAN", parameters, clock), name);
            var random = RunSeeds(series, p, parameters => Job("RC_RAN", parameters, clock)).Select(r => r.Result).ToList();
            report.AppendLine($"### {name}\n");
            report.AppendLine($"RHL {real.Trades} trade, netto {real.NetProfit:N0}. RAN p = {p:0.#####}, trade medi {random.Average(r => r.Trades):N0}.\n");
            report.AppendLine("| metrica | RHL | RAN p5 | mediana | p95 | percentile |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            report.AppendLine(Row("netto", real.NetProfit, random.Select(r => r.NetProfit)));
            report.AppendLine(Row("average trade", real.AverageTrade, random.Select(r => r.AverageTrade)));
            report.AppendLine(Row("netto / DD", Ratio(real), random.Select(Ratio)));
            report.AppendLine();
        }

        File.WriteAllText(Path.Combine(RepositoryPath, "ricerca", "percorso", "es-60-rhl-flat-verifiche.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
    }

    private static string Share(int part, int total) => total > 0 ? $"{100.0 * part / total:0}%" : "-";

    private static double DailyCorrelation(IReadOnlyList<TradingResult> a, IReadOnlyList<TradingResult> b)
    {
        var da = a.GroupBy(t => t.ExitDate.Date).ToDictionary(g => g.Key, g => (double)g.Sum(t => t.NetProfit));
        var db = b.GroupBy(t => t.ExitDate.Date).ToDictionary(g => g.Key, g => (double)g.Sum(t => t.NetProfit));
        var days = da.Keys.Union(db.Keys).ToList();
        if (days.Count < 3) return 0;
        var x = days.Select(d => da.GetValueOrDefault(d)).ToArray();
        var y = days.Select(d => db.GetValueOrDefault(d)).ToArray();
        double mx = x.Average(), my = y.Average();
        var cov = x.Zip(y, (u, v) => (u - mx) * (v - my)).Sum();
        var sx = Math.Sqrt(x.Sum(u => (u - mx) * (u - mx)));
        var sy = Math.Sqrt(y.Sum(v => (v - my) * (v - my)));
        return sx > 0 && sy > 0 ? cov / (sx * sy) : 0;
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
                var parameters = RandomExits();
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
