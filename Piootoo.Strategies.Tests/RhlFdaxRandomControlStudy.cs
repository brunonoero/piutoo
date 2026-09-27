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
/// <b>RHL su FDAX 4h contro il caso.</b> La configurazione trovata dal percorso cercando sul feed FTMO
/// (<c>ricerca/percorso/fdax-240-ricerca-ftmo.md</c>, 26/09/2026): reversal sul minimo di ieri meno 20 tick,
/// solo long, ingressi fino alle 10, pattern neutro 34, stop e target a 1 ATR, intraday.
///
/// <para><c>RC_RAN</c> riceve le sue uscite e i suoi vincoli — stop e target in ATR, intraday, finestra
/// fino alle 10, un ingresso per sessione, <b>solo long</b> — e sceglie a caso il quando. Solo long anche
/// il caso: la domanda e' se il segnale batte un long qualunque nelle stesse ore, non se il DAX e' salito.</para>
///
/// <para>Tre periodi: la ricerca (FTMO nov 2020 → set 2024), la prova (FTMO set 2024 → set 2026) e la
/// storia interna 2008 → nov 2020 che la ricerca non ha visto. Contano gli ultimi due. Costi FTMO, orologio
/// al minuto. CSV per seme in <c>ricerca/percorso/fdax-240-rhl-controllo-ran.csv</c>.</para>
/// </summary>
public sealed class RhlFdaxRandomControlStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;
    private const int CalibrationSeeds = 8;

    /// <summary>
    /// Simbolo del controllo. Default <c>@FDAX</c>, dove la regola e' stata trovata; con
    /// <c>PIOOTOO_RAN_SIMBOLO</c> la stessa regola, identica, su un altro indice (27/09/2026): li' tutto il
    /// feed FTMO e' fuori campione per la regola, che nessuno ha scelto su quel mercato.
    /// </summary>
    private static string Symbol => "@" + (Environment.GetEnvironmentVariable("PIOOTOO_RAN_SIMBOLO") ?? "@FDAX").TrimStart('@').ToUpperInvariant();

    /// <summary>La strategia nelle chiavi di RC_RHL.</summary>
    private static Dictionary<string, object> Strategy() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = Symbol, ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 1.0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = 10,
        ["PtnNeutYes"] = 34, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
        ["SkipDay"] = -1, ["LevelOffsetTicks"] = 20, ["Direction"] = 1
    };

    /// <summary>Le uscite e i vincoli della strategia nelle chiavi di RC_RAN: il segnale e' l'unica cosa che manca.</summary>
    private static Dictionary<string, object> Exits() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = Symbol, ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
        ["StopAtr"] = 1.0m, ["TargetAtr"] = 1.0m, ["MaxBars"] = 0, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
        ["StartHour"] = -1, ["EndHour"] = 10, ["MaxEntriesPerSession"] = 1, ["Direction"] = 1
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task RhlAgainstAHundredRandomLongEntriesWithTheSameExits()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = Symbol.TrimStart('@');
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key];
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs[key];
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        var broker = await SweepSeries.LoadAsync(dataFeed, Symbol, [240, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), "FTMO", warmupDays: 30d);
        var periods = new List<(string Name, SweepSeries Series)>();
        if (key == "FDAX")
        {
            // Il mercato su cui la regola e' stata trovata: la ricerca, la prova e la storia mai vista.
            periods.Add(("ricerca FTMO 2020-2024", broker.Between(broker.StartUtc, Utc(2024, 9, 1))));
            periods.Add(("prova FTMO 2024-2026", broker.Between(Utc(2024, 9, 1), broker.EndUtc)));
        }
        else
        {
            // Un altro mercato: la regola non e' stata scelta qui, tutto il feed FTMO e' fuori campione.
            periods.Add(("FTMO dal 2020, fuori campione per la regola", broker));
        }

        if (File.Exists(Path.Combine(RepositoryPath, "datafeed", $"{Symbol}_1.json")))
            periods.Add(("interno 2008-2020, mai visto",
                await SweepSeries.LoadAsync(dataFeed, Symbol, [240, 1], Utc(2008, 1, 1), Utc(2020, 11, 9), null, warmupDays: 30d)));

        SweepJob Job(string id, IReadOnlyDictionary<string, object> parameters) => new(id, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        var csv = new StringBuilder("periodo,seme,p,trade,netto,dd,pf,avg\n");
        var report = new StringBuilder($"# RHL {key} 4h contro 100 ingressi long casuali con le stesse uscite\n\n");
        report.AppendLine("Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.\n");
        report.AppendLine($"Costi FTMO (spread {spread} punti, swap), orologio al minuto, semi {FirstSeed}-{FirstSeed + Seeds - 1}. Percentile = quota di semi che fa peggio della strategia.\n");

        foreach (var (name, series) in periods)
        {
            var real = new SweepRunner(series).Run(Job("RC_RHL", Strategy()));
            var p = Calibrate(series, real.Trades, parameters => Job("RC_RAN", parameters), name);
            var random = RunSeeds(series, p, parameters => Job("RC_RAN", parameters));
            foreach (var (seed, result) in random.OrderBy(entry => entry.Seed))
                csv.AppendLine(string.Join(',', name, seed, p.ToString(CultureInfo.InvariantCulture), result.Trades,
                    Num(result.NetProfit), Num(result.MaxClosedTradeDrawdown), result.ProfitFactor is { } pf ? Num(pf) : "", Num(result.AverageTrade)));

            var results = random.Select(entry => entry.Result).ToList();
            report.AppendLine($"## {name}\n");
            report.AppendLine($"RHL {real.Trades} trade, netto {real.NetProfit:N0}. RAN p = {p:0.#####}, trade medi {results.Average(r => r.Trades):N0}.\n");
            report.AppendLine("| metrica | RHL | RAN p5 | mediana | p95 | percentile |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            report.AppendLine(Row("netto", real.NetProfit, results.Select(r => r.NetProfit)));
            report.AppendLine(Row("average trade", real.AverageTrade, results.Select(r => r.AverageTrade)));
            report.AppendLine(Row("profit factor", real.ProfitFactor ?? 0m, results.Select(r => r.ProfitFactor ?? 0m)));
            report.AppendLine(Row("netto / DD", Ratio(real), results.Select(Ratio)));
            report.AppendLine();
            output.WriteLine(report.ToString());
        }

        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        File.WriteAllText(Path.Combine(folder, $"{key.ToLowerInvariant()}-240-rhl-controllo-ran.csv"), csv.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folder, $"{key.ToLowerInvariant()}-240-rhl-controllo-ran.md"), report.ToString(), Encoding.UTF8);
    }

    private decimal Calibrate(SweepSeries series, int target, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string name)
    {
        var p = 0.05m;
        for (var iteration = 1; iteration <= 8; iteration++)
        {
            var mean = RunSeeds(series, p, job, firstSeed: 1, count: CalibrationSeeds).Average(entry => entry.Result.Trades);
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
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => new SweepRunner(series),
            (seed, _, runner) =>
            {
                var parameters = Exits();
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

    private static string Num(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
