using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La <b>griglia grossa</b>: il motore nudo (pattern alle sentinelle, nessun filtro orario) e le sole
/// leve grosse, tutte insieme in un giro solo, ogni combinazione misurata dentro <i>e</i> fuori
/// campione sull'orologio al minuto. Nata su GC il 22/09/2026 come controllo della sweep — e ha
/// detto in due ore che il fuori campione della sweep era il rally dell'oro, non i pattern.
///
/// <para>Una cella = un simbolo, un timeframe, un motore. La classe di partenza e' solo un
/// contenitore di parametri: deve avere il simbolo e il timeframe giusti e un <c>Initialize</c> che
/// legga tutte le chiavi, e nient'altro di suo sopravvive perche' la griglia sovrascrive tutto.</para>
///
/// <para>I costi si caricano dalle stesse tabelle della sweep, mai a mano; con piu' broker si prende
/// il peggiore voce per voce, come fa <c>piootoo-sweep</c>.</para>
/// </summary>
public sealed record CoarseGridSpec(
    string StrategyId,
    string Symbol,
    int TimeframeMinutes,
    string FeedBroker,
    string[] SpreadBrokers,
    string[] SwapBrokers,
    decimal CommissionPerSide,
    DateTime StartUtc,
    DateTime SplitUtc,
    DateTime EndUtc,
    int[] Channels,
    int[] Stops,
    int[] Targets,
    int[] ExitHours,
    int[] Directions,
    string CsvName,
    int MinInSampleTrades = 250,
    /// <summary>
    /// Se vero, <see cref="Stops"/> e <see cref="Targets"/> sono multipli dell'ATR delle sessioni
    /// chiuse in <b>decimi</b> (10 = 1,0 ATR, 15 = 1,5) invece che dollari per contratto: la
    /// griglia passa <c>StopAtr</c>/<c>TargetAtr</c> e azzera il denaro fisso. Stesse colonne nel
    /// CSV, con l'unita' dichiarata nell'intestazione.
    /// </summary>
    bool AtrStops = false);

public static class CoarseGridStudy
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    public sealed record Cell(
        int ChannelBars, int StopLoss, int TakeProfit, int ExitHour, int Direction,
        SweepOutcome InSample, SweepOutcome OutOfSample, int OosWindowsInProfit);

    public static async Task<List<Cell>?> RunAsync(CoarseGridSpec spec, ITestOutputHelper output)
    {
        // L'interruttore sta QUI e non nelle singole celle: una cella nuova e' un file di dieci
        // righe, e chi lo scrive non deve ricordarsi di spegnerla. Vedi ResearchStudy.
        if (ResearchStudy.IsSkipped(output)) return null;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed-external", spec.FeedBroker)))
        {
            output.WriteLine("feed assente: saltato.");
            return null;
        }

        // Chiavi delle tabelle: senza '@' e in maiuscolo, come StrategyKeys.NormalizeSymbol.
        var key = spec.Symbol.Trim().TrimStart('@').ToUpperInvariant();

        var spreadValue = spec.SpreadBrokers
            .Select(b => SpreadTable.Load(settings.GetSpreadPath(), b, SpreadStatistic.Median, SpreadResolution.PerSymbol))
            .Where(t => t.Points.ContainsKey(key))
            .Select(t => t.Points[key])
            .Max();
        var spread = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spreadValue };

        var swapTables = spec.SwapBrokers.Select(b => SwapTable.Load(settings.GetSwapPath(), b)).ToList();
        var swapSpecs = swapTables.Count == 1 ? swapTables[0].Specs : SwapTable.Worst(swapTables);
        var swapSpec = swapSpecs[key];
        var swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swapSpec };

        output.WriteLine(
            $"{spec.Symbol} {spec.TimeframeMinutes}m · feed {spec.FeedBroker} · spread {spreadValue} pt (peggiore fra {string.Join(", ", spec.SpreadBrokers)}) · " +
            $"swap long {swapSpec.LongPointsPerNight} short {swapSpec.ShortPointsPerNight} pt/notte (peggiore fra {string.Join(", ", spec.SwapBrokers)}) · " +
            $"commissione {spec.CommissionPerSide} per lato");

        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var series = await SweepSeries.LoadAsync(
            dataFeed, spec.Symbol, [spec.TimeframeMinutes, 1], spec.StartUtc, spec.EndUtc, warmupDays: 30d, broker: spec.FeedBroker);
        var inSample = series.Between(series.StartUtc, spec.SplitUtc);
        var outOfSample = series.Between(spec.SplitUtc, series.EndUtc);
        output.WriteLine($"feed {series.StartUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}, split {spec.SplitUtc:yyyy-MM-dd}\n");

        var template = new SweepJob(spec.StrategyId)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = spec.CommissionPerSide,
            ClockTimeframeMinutes = 1,
            SpreadPoints = spread,
            Swap = swap,
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        // Il motore nudo: pattern alle sentinelle, nessun filtro, tutto il giorno, niente trailing.
        var fixedParameters = new Dictionary<string, object>
        {
            ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
            ["StartHour"] = -1, ["EndHour"] = -1, ["DvolMin"] = 0, ["SkipDay"] = -1,
            ["IntradayOnly"] = 1, ["OffsetTicks"] = 0, ["MaxBars"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0
        };

        var combos = (from c in spec.Channels from s in spec.Stops from t in spec.Targets from e in spec.ExitHours from d in spec.Directions
                      select (c, s, t, e, d)).ToList();
        output.WriteLine($"{combos.Count} combinazioni, orologio al minuto, {Environment.ProcessorCount} core\n");

        var cells = new ConcurrentBag<Cell>();
        var started = System.Diagnostics.Stopwatch.StartNew();
        var done = 0;

        // Un runner per thread: PiootooTradingService non e' thread-safe, le serie si condividono.
        Parallel.ForEach(
            combos,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => (In: new SweepRunner(inSample), Out: new SweepRunner(outOfSample)),
            (combo, _, runners) =>
            {
                var parameters = new Dictionary<string, object>(fixedParameters)
                {
                    ["ChannelBars"] = combo.c, ["ExitHour"] = combo.e, ["Direction"] = combo.d
                };
                if (spec.AtrStops)
                {
                    // Decimi di ATR: 10 = 1,0. Il denaro fisso va a zero, cosi' un target a 0 ATR
                    // e' davvero "nessun target" e non il ProfitMoney della classe.
                    parameters["StopLoss"] = 0; parameters["TakeProfit"] = 0;
                    parameters["StopAtr"] = combo.s / 10m; parameters["TargetAtr"] = combo.t / 10m;
                }
                else
                {
                    parameters["StopLoss"] = combo.s; parameters["TakeProfit"] = combo.t;
                }
                var job = template with { Parameters = parameters };
                var isOutcome = runners.In.Run(job);
                var oosOutcome = runners.Out.Run(job);
                cells.Add(new Cell(combo.c, combo.s, combo.t, combo.e, combo.d, isOutcome, oosOutcome,
                    WindowsInProfit(oosOutcome, spec.SplitUtc, series.EndUtc, 4)));

                var n = Interlocked.Increment(ref done);
                if (n % 100 == 0) output.WriteLine($"  {n}/{combos.Count} in {started.Elapsed.TotalMinutes:N1} min");
                return runners;
            },
            _ => { });

        output.WriteLine($"\nfinito in {started.Elapsed.TotalMinutes:N1} minuti");
        var list = cells.ToList();
        WriteCsv(spec, list);
        Report(spec, list, output);
        return list;
    }

    private static void Report(CoarseGridSpec spec, List<Cell> cells, ITestOutputHelper output)
    {
        var admissible = cells
            .Where(c => c.InSample.Trades >= spec.MinInSampleTrades && c.InSample.NetProfit > 0m)
            .ToList();
        var robust = admissible.Where(c => c.OutOfSample.NetProfit > 0m && c.OosWindowsInProfit >= 3).ToList();

        output.WriteLine($"\nammissibili (IS ≥ {spec.MinInSampleTrades} trade e IS > 0): {admissible.Count} su {cells.Count}");
        output.WriteLine($"di cui fuori campione in utile con ≥ 3 finestre su 4: {robust.Count}");
        if (admissible.Count == 0) return;

        output.WriteLine("\nle 15 migliori fuori campione fra le ammissibili (netto OOS / DD OOS):");
        output.WriteLine("  can  stop  targ  exit dir |   IS n    IS netto    IS DD |  OOS n   OOS netto   OOS DD  fin");
        foreach (var c in admissible.OrderByDescending(c => Ratio(c.OutOfSample)).Take(15))
            output.WriteLine(Row(c));

        output.WriteLine("\nle 10 migliori IN campione (per confronto: e' qui che la fortuna si nasconde):");
        foreach (var c in admissible.OrderByDescending(c => Ratio(c.InSample)).Take(10))
            output.WriteLine(Row(c));

        // Le celle equilibrate: utile dentro E fuori con rapporto netto/DD sopra 1 da entrambe le parti.
        var balanced = admissible.Where(c => Ratio(c.InSample) >= 1m && Ratio(c.OutOfSample) >= 1m && c.OosWindowsInProfit >= 3).ToList();
        output.WriteLine($"\nequilibrate (netto/DD ≥ 1 sia dentro sia fuori, ≥ 3 finestre): {balanced.Count}");
        foreach (var c in balanced.OrderByDescending(c => Math.Min(Ratio(c.InSample), Ratio(c.OutOfSample))).Take(10))
            output.WriteLine(Row(c));

        output.WriteLine("\nora di uscita, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.ExitHour).OrderBy(g => g.Key))
            output.WriteLine($"  ExitHour={g.Key,3}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        output.WriteLine("\ndirezione, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.Direction).OrderBy(g => g.Key))
            output.WriteLine($"  Direction={g.Key}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        output.WriteLine("\nstop, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.StopLoss).OrderBy(g => g.Key))
            output.WriteLine($"  Stop={g.Key,5}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
    }

    private static decimal Ratio(SweepOutcome o) =>
        o.MaxClosedTradeDrawdown > 0m ? o.NetProfit / o.MaxClosedTradeDrawdown : (o.NetProfit > 0m ? 999m : -999m);

    private static string Row(Cell c) =>
        $"  {c.ChannelBars,3} {c.StopLoss,5} {c.TakeProfit,5} {c.ExitHour,5} {c.Direction,3} | " +
        $"{c.InSample.Trades,5} {c.InSample.NetProfit,11:N0} {c.InSample.MaxClosedTradeDrawdown,8:N0} | " +
        $"{c.OutOfSample.Trades,5} {c.OutOfSample.NetProfit,11:N0} {c.OutOfSample.MaxClosedTradeDrawdown,8:N0}  {c.OosWindowsInProfit}/4";

    private static int WindowsInProfit(SweepOutcome o, DateTime from, DateTime to, int windows)
    {
        var span = (to - from) / windows;
        var count = 0;
        for (var i = 0; i < windows; i++)
        {
            var a = from + span * i;
            var b = i == windows - 1 ? to.AddTicks(1) : a + span;
            if (o.ClosedTrades.Where(t => t.ExitDate >= a && t.ExitDate < b).Sum(t => t.NetProfit) > 0m) count++;
        }
        return count;
    }

    private static void WriteCsv(CoarseGridSpec spec, List<Cell> cells)
    {
        var path = Path.Combine(RepositoryPath, "ricerca", spec.CsvName);
        var sb = new StringBuilder();
        sb.AppendLine($"# Griglia grossa {spec.Symbol} {spec.TimeframeMinutes}m Price Channel, motore nudo (pattern spenti, nessun filtro orario), {cells.Count} combinazioni.");
        if (spec.AtrStops)
            sb.AppendLine("# stopLoss e takeProfit sono DECIMI di ATR delle sessioni chiuse (10 = 1,0 ATR), non dollari.");
        sb.AppendLine($"# Feed {spec.FeedBroker}, spread peggiore fra {string.Join("/", spec.SpreadBrokers)}, swap peggiore fra {string.Join("/", spec.SwapBrokers)}, commissione {spec.CommissionPerSide}/lato, orologio al minuto. Campione {spec.StartUtc:yyyy-MM-dd} -> {spec.SplitUtc:yyyy-MM-dd}, fuori campione -> {spec.EndUtc:yyyy-MM-dd}.");
        sb.AppendLine("channelBars;stopLoss;takeProfit;exitHour;direction;isTrades;isNet;isDD;isPF;oosTrades;oosNet;oosDD;oosPF;oosWindowsInProfit");
        foreach (var c in cells.OrderBy(c => c.ChannelBars).ThenBy(c => c.StopLoss).ThenBy(c => c.TakeProfit).ThenBy(c => c.ExitHour).ThenBy(c => c.Direction))
        {
            sb.Append(string.Join(';',
                c.ChannelBars, c.StopLoss, c.TakeProfit, c.ExitHour, c.Direction,
                c.InSample.Trades, F(c.InSample.NetProfit), F(c.InSample.MaxClosedTradeDrawdown), F(c.InSample.ProfitFactor),
                c.OutOfSample.Trades, F(c.OutOfSample.NetProfit), F(c.OutOfSample.MaxClosedTradeDrawdown), F(c.OutOfSample.ProfitFactor),
                c.OosWindowsInProfit));
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string F(decimal? v) => v.HasValue ? v.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
}
