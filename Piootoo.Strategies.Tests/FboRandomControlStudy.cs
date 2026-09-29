using System.Collections.Concurrent;
using System.Diagnostics;
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
/// <b>Il falso breakout contro il caso</b>, sulle celle della matrice PT6EXO che hanno dato configurazioni
/// robuste (<c>ricerca/matrice/pt6exo-prime-7-celle.md</c>): FDAX 1h e NQ 1h. Entrambe sono solo long e
/// fuori campione cadono nel rialzo 2025-26: il controllo RAN dice se il segnale porta qualcosa o se basta
/// comprare a caso con le stesse uscite.
///
/// <para><b>Come.</b> La configurazione ricostruita con gli stessi parametri, costi, feed e split della
/// matrice (<see cref="CoarseGridMatrixTests"/>). <c>RC_RAN</c> riceve le stesse uscite e lo stesso lato
/// (<c>Direction = 1</c>): cambia solo il <i>quando</i>. Un RAN con il verso a caso risponderebbe a
/// un'altra domanda, perche' in un rialzo perde per costruzione meta' delle volte.</para>
///
/// <para>Non asserisce una soglia: stampa i percentili e scrive il CSV per seme in
/// <c>ricerca/matrice/{cella}-controllo-ran.csv</c>.</para>
/// </summary>
public sealed class FboRandomControlStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;
    private const int CalibrationSeeds = 8;
    private static readonly DateTime StartUtc = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Una configurazione della griglia. <paramref name="HoldDays"/> come nella matrice: 0 = intraday,
    /// N = fino a N sessioni da 23 ore (<c>MaxBars</c> = N × 1380 / timeframe).
    /// </summary>
    private sealed record Config(
        string Cell, string Symbol, int TimeframeMinutes, int ChannelBars,
        decimal StopAtr, decimal TargetAtr, int ExitHour, int HoldDays);

    /// <summary>Canale 5, stop 2,5 ATR, target 1,5 ATR, uscita alle 21, intraday: la prima delle robuste per netto in campione.</summary>
    private static readonly Config FdaxHourly = new("fdax-60-fbo", "@FDAX", 60, 5, 2.5m, 1.5m, 21, 0);

    /// <summary>Canale 20, stop 2,5 ATR, target 1,5 ATR, nessuna ora di uscita, un giorno: la prima delle robuste per UngerFit.</summary>
    private static readonly Config NqHourly = new("nq-60-fbo", "@NQ", 60, 20, 2.5m, 1.5m, -1, 1);

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public Task FdaxHourlyAgainstAHundredRandomLongEntriesWithTheSameExits() => RunAsync(FdaxHourly);

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public Task NqHourlyAgainstAHundredRandomLongEntriesWithTheSameExits() => RunAsync(NqHourly);

    /// <summary>Le uscite e il lato della configurazione, comuni alla strategia e al controllo.</summary>
    private static Dictionary<string, object> Exits(Config config) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = config.Symbol,
        ["TimeframeMinutes"] = config.TimeframeMinutes,
        ["StopLoss"] = 0,
        ["TakeProfit"] = 0,
        ["StopAtr"] = config.StopAtr,
        ["TargetAtr"] = config.TargetAtr,
        ["TrailingStop"] = 0,
        ["BreakEven"] = 0,
        ["MaxBars"] = config.HoldDays * Math.Max(1, 1380 / config.TimeframeMinutes),
        ["IntradayOnly"] = config.HoldDays > 0 ? 0 : 1,
        ["ExitHour"] = config.ExitHour,
        ["StartHour"] = -1,
        ["EndHour"] = -1,
        ["Direction"] = 1
    };

    /// <summary>La strategia: le uscite piu' il segnale del falso breakout, come nella griglia (motore nudo).</summary>
    private static Dictionary<string, object> Strategy(Config config)
    {
        var parameters = Exits(config);
        parameters["ChannelBars"] = config.ChannelBars;
        parameters["ReentryBars"] = 1;
        parameters["PtnNeutYes"] = 55; parameters["PtnNeutNo"] = 56; parameters["PtnDirYes"] = 52; parameters["PtnDirNo"] = 53;
        parameters["DvolMin"] = 0; parameters["SkipDay"] = -1; parameters["OffsetTicks"] = 0;
        return parameters;
    }

    private async Task RunAsync(Config config)
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };

        var key = StrategyKeys.NormalizeSymbol(config.Symbol);
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol);
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO");

        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), config.Symbol, [config.TimeframeMinutes, 1],
            StartUtc, EndUtc, warmupDays: 30d, broker: "FTMO");

        SweepJob Job(string id, IReadOnlyDictionary<string, object> parameters) => new(id, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread.Points[key] },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap.For(key) },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        output.WriteLine($"{config}");
        output.WriteLine($"feed {series.StartUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}, split {SplitUtc:yyyy-MM-dd}, {Environment.ProcessorCount} core");
        output.WriteLine($"spread {spread.Points[key]} pt, commissione 0, {Seeds} semi da {FirstSeed}, RAN solo long\n");

        var halves = new[]
        {
            (Name: "campione", Series: series.Between(series.StartUtc, SplitUtc)),
            (Name: "validazione", Series: series.Between(SplitUtc, series.EndUtc))
        };

        var csv = new StringBuilder("half,seed,p,trades,net,dd,pf,avg\n");

        foreach (var (name, half) in halves)
        {
            var clock = Stopwatch.StartNew();
            var real = new SweepRunner(half).Run(Job("RC_FBO", Strategy(config)));
            output.WriteLine($"=== {name}: FBO {real.Trades} trade, netto {real.NetProfit:N0}, DD {real.MaxClosedTradeDrawdown:N0}, PF {Fmt(real.ProfitFactor)}, avg {real.AverageTrade:N0}");

            var p = Calibrate(config, half, real.Trades, parameters => Job("RC_RAN", parameters), name);
            var random = RunSeeds(config, half, p, parameters => Job("RC_RAN", parameters));

            foreach (var outcome in random.OrderBy(o => o.Seed))
            {
                csv.AppendLine(string.Join(',', name, outcome.Seed, p.ToString(CultureInfo.InvariantCulture),
                    outcome.Result.Trades, Num(outcome.Result.NetProfit), Num(outcome.Result.MaxClosedTradeDrawdown),
                    outcome.Result.ProfitFactor is { } pf ? Num(pf) : "", Num(outcome.Result.AverageTrade)));
            }

            var results = random.Select(o => o.Result).ToList();
            output.WriteLine($"  RAN p = {p:0.#####}, trade medi {results.Average(r => r.Trades):N0} (min {results.Min(r => r.Trades)}, max {results.Max(r => r.Trades)}), {clock.Elapsed:hh\\:mm\\:ss}");
            output.WriteLine($"  {"metrica",-14}{"FBO",12}{"RAN p5",12}{"mediana",12}{"p95",12}{"percentile",12}");
            Report("netto", real.NetProfit, results.Select(r => r.NetProfit));
            Report("average trade", real.AverageTrade, results.Select(r => r.AverageTrade));
            Report("profit factor", real.ProfitFactor ?? 0m, results.Select(r => r.ProfitFactor ?? 0m));
            Report("netto / DD", Ratio(real), results.Select(Ratio));
            output.WriteLine("");
        }

        var csvName = Path.Combine("matrice", $"{config.Cell}-controllo-ran.csv");
        var path = Path.Combine(RepositoryPath, "ricerca", csvName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        output.WriteLine($"CSV: ricerca/{csvName}");
    }

    /// <summary>La probabilita' per barra che da' in media, sui semi di taratura, i trade della strategia (scarto sotto il 3%).</summary>
    private decimal Calibrate(Config config, SweepSeries half, int target, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string name)
    {
        var p = 0.05m;
        for (var iteration = 1; iteration <= 8; iteration++)
        {
            var mean = RunSeeds(config, half, p, job, firstSeed: 1, count: CalibrationSeeds).Average(o => o.Result.Trades);
            output.WriteLine($"  taratura {name} #{iteration}: p = {p:0.#####} → {mean:N1} trade (obiettivo {target})");
            if (mean <= 0 || Math.Abs(mean - target) / target < 0.03)
                break;

            p = Math.Min(1m, Math.Round(p * (decimal)(target / mean), 5));
        }

        return p;
    }

    /// <summary>I semi in parallelo, un runner per thread: <c>PiootooTradingService</c> non e' thread-safe, le serie si condividono.</summary>
    private static List<(int Seed, SweepOutcome Result)> RunSeeds(
        Config config, SweepSeries half, decimal p, Func<IReadOnlyDictionary<string, object>, SweepJob> job,
        int firstSeed = FirstSeed, int count = Seeds)
    {
        var results = new ConcurrentBag<(int, SweepOutcome)>();
        Parallel.For(
            firstSeed, firstSeed + count,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => new SweepRunner(half),
            (seed, _, runner) =>
            {
                var parameters = Exits(config);
                parameters["Seed"] = seed;
                parameters["EntryProbability"] = p;
                results.Add((seed, runner.Run(job(parameters))));
                return runner;
            },
            _ => { });

        return results.ToList();
    }

    private void Report(string metric, decimal strategy, IEnumerable<decimal> seeds)
    {
        var sorted = seeds.OrderBy(v => v).ToList();
        var below = sorted.Count(v => v < strategy) / (double)sorted.Count;
        output.WriteLine($"  {metric,-14}{strategy,12:N2}{Quantile(sorted, 0.05),12:N2}{Quantile(sorted, 0.5),12:N2}{Quantile(sorted, 0.95),12:N2}{below,12:P0}");
    }

    private static decimal Quantile(List<decimal> sorted, double q) =>
        sorted[Math.Clamp((int)Math.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static decimal Ratio(SweepOutcome o) =>
        o.MaxClosedTradeDrawdown > 0 ? o.NetProfit / o.MaxClosedTradeDrawdown : 0m;

    private static string Fmt(decimal? v) => v.HasValue ? v.Value.ToString("N2") : "n/d";

    private static string Num(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
