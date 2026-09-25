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
/// <b>La 002 contro il caso.</b> Primo uso del controllo RAN (serie PT6EXO, vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c>): quanto del risultato di
/// <c>PT3B_FDAX_PCH_002_240</c> viene dal suo segnale, e quanto dalla forma delle uscite e dal mercato.
///
/// <para><b>Come.</b> <c>RC_RAN</c> riceve tutto cio' che della 002 non e' il segnale: stop 5.000 e
/// target 4.500 in denaro, 12 barre, intraday con uscita alle 21:00, finestra 03-18 nell'orologio
/// della ricerca, un ingresso per sessione e per lato, etichetta sull'apertura. Cambia solo il
/// <i>quando</i> e il <i>verso</i> dell'ingresso, che sono a caso. La probabilita' per barra si tara,
/// separatamente dentro e fuori campione, in modo che i semi facciano in media quanti trade fa la 002;
/// poi cento semi diversi da quelli della taratura danno la distribuzione.</para>
///
/// <para><b>Cosa si legge.</b> Il percentile della 002 nella distribuzione dei semi, per netto,
/// average trade, profit factor e netto su drawdown. Una strategia che vale per il proprio segnale sta
/// sopra quasi tutti i semi <b>fuori campione</b>; dentro il campione conta meno, perche' li' la 002 e'
/// stata scelta. Se fuori campione sta nel mezzo, il suo guadagno lo farebbe anche una moneta con le
/// stesse uscite.</para>
///
/// <para>Stesso feed, periodo, split e costi di <see cref="Pt3b002LongPeriodTests"/> (ICS, 2014-07 →
/// 2021-01 → 2026-09, spread mediano, swap, 19,23 per lato, orologio al minuto), cosi' i due
/// resoconti si leggono insieme. Non asserisce una soglia: stampa i numeri e scrive il CSV per seme in
/// <c>ricerca/fdax-4h-pch-002-controllo-ran.csv</c>.</para>
/// </summary>
public sealed class Pt3b002RandomControlStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const string StrategyId = "PT3B_FDAX_PCH_002_240";
    private const string CsvName = "fdax-4h-pch-002-controllo-ran.csv";
    private const int Seeds = 100;
    private const int FirstSeed = 1001;
    private const int CalibrationSeeds = 8;
    private static readonly DateTime StartUtc = new(2014, 7, 18, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Le uscite e i vincoli della 002, nelle chiavi del contenitore. Il segnale — canale, pattern,
    /// livello — e' l'unica cosa che manca, ed e' il punto.
    /// </summary>
    private static Dictionary<string, object> Exits() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Symbol"] = "FDAX",
        ["TimeframeMinutes"] = 240,
        ["StopLoss"] = 5000,
        ["TakeProfit"] = 4500,
        ["TrailingStop"] = 0,
        ["BreakEven"] = 0,
        ["MaxBars"] = 12,
        ["IntradayOnly"] = 1,
        ["ExitHour"] = 21,
        ["StartHour"] = 3,
        ["EndHour"] = 18,
        ["MaxEntriesPerSession"] = 1,
        ["Direction"] = 0
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task The002AgainstAHundredRandomEntriesWithTheSameExits()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed-external", "ICS")))
        {
            output.WriteLine("feed assente: saltato.");
            return;
        }

        var spread = SpreadTable.Load(settings.GetSpreadPath(), "ICS", SpreadStatistic.Median, SpreadResolution.PerSymbol);
        var swap = SwapTable.Load(settings.GetSwapPath(), "ICS");
        var key = StrategyKeys.NormalizeSymbol("@FDAX");

        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), "@FDAX", [240, 1],
            StartUtc, EndUtc, warmupDays: 30d, broker: "ICS");

        SweepJob Job(string id, IReadOnlyDictionary<string, object>? parameters = null) => new(id, parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 19.23m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread.Points[key] },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap.For(key) },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        output.WriteLine($"feed {series.StartUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}, split {SplitUtc:yyyy-MM-dd}, {Environment.ProcessorCount} core");
        output.WriteLine($"spread {spread.Points[key]} pt, commissione 19,23/lato, {Seeds} semi da {FirstSeed}\n");

        var halves = new[]
        {
            (Name: "campione", Series: series.Between(series.StartUtc, SplitUtc)),
            (Name: "validazione", Series: series.Between(SplitUtc, series.EndUtc))
        };

        var csv = new StringBuilder("half,seed,p,trades,net,dd,pf,avg\n");

        foreach (var (name, half) in halves)
        {
            var clock = Stopwatch.StartNew();
            var real = new SweepRunner(half).Run(Job(StrategyId));
            output.WriteLine($"=== {name}: {StrategyId} {real.Trades} trade, netto {real.NetProfit:N0}, DD {real.MaxClosedTradeDrawdown:N0}, PF {Fmt(real.ProfitFactor)}, avg {real.AverageTrade:N0}");

            var p = Calibrate(half, real.Trades, parameters => Job("RC_RAN", parameters), name);
            var random = RunSeeds(half, p, parameters => Job("RC_RAN", parameters));

            foreach (var outcome in random.OrderBy(o => o.Seed))
            {
                csv.AppendLine(string.Join(',', name, outcome.Seed, p.ToString(CultureInfo.InvariantCulture),
                    outcome.Result.Trades, Num(outcome.Result.NetProfit), Num(outcome.Result.MaxClosedTradeDrawdown),
                    outcome.Result.ProfitFactor is { } pf ? Num(pf) : "", Num(outcome.Result.AverageTrade)));
            }

            var results = random.Select(o => o.Result).ToList();
            output.WriteLine($"  RAN p = {p:0.#####}, trade medi {results.Average(r => r.Trades):N0} (min {results.Min(r => r.Trades)}, max {results.Max(r => r.Trades)}), {clock.Elapsed:hh\\:mm\\:ss}");
            output.WriteLine($"  {"metrica",-14}{"002",12}{"RAN p5",12}{"mediana",12}{"p95",12}{"percentile",12}");
            Report("netto", real.NetProfit, results.Select(r => r.NetProfit));
            Report("average trade", real.AverageTrade, results.Select(r => r.AverageTrade));
            Report("profit factor", real.ProfitFactor ?? 0m, results.Select(r => r.ProfitFactor ?? 0m));
            Report("netto / DD", Ratio(real), results.Select(Ratio));
            output.WriteLine("");
        }

        var path = Path.Combine(RepositoryPath, "ricerca", CsvName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        output.WriteLine($"CSV: ricerca/{CsvName}");
    }

    /// <summary>
    /// La probabilita' per barra che da' in media, sui semi di taratura, lo stesso numero di trade della
    /// strategia. Non e' lineare — piu' trade vuol dire meno barre flat — quindi si corregge qualche
    /// volta in proporzione finche' lo scarto sta sotto il 3%.
    /// </summary>
    private decimal Calibrate(SweepSeries half, int target, Func<IReadOnlyDictionary<string, object>, SweepJob> job, string name)
    {
        var p = 0.1m;
        for (var iteration = 1; iteration <= 6; iteration++)
        {
            var mean = RunSeeds(half, p, job, firstSeed: 1, count: CalibrationSeeds).Average(o => o.Result.Trades);
            output.WriteLine($"  taratura {name} #{iteration}: p = {p:0.#####} → {mean:N1} trade (obiettivo {target})");
            if (mean <= 0 || Math.Abs(mean - target) / target < 0.03)
                break;

            p = Math.Min(1m, Math.Round(p * (decimal)(target / mean), 5));
        }

        return p;
    }

    /// <summary>I semi in parallelo, un runner per thread: <c>PiootooTradingService</c> non e' thread-safe, le serie si condividono.</summary>
    private static List<(int Seed, SweepOutcome Result)> RunSeeds(
        SweepSeries half, decimal p, Func<IReadOnlyDictionary<string, object>, SweepJob> job,
        int firstSeed = FirstSeed, int count = Seeds)
    {
        var results = new ConcurrentBag<(int, SweepOutcome)>();
        Parallel.For(
            firstSeed, firstSeed + count,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => new SweepRunner(half),
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

    /// <summary>Una riga: il valore della strategia, la distribuzione dei semi e la quota di semi che fa peggio.</summary>
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
