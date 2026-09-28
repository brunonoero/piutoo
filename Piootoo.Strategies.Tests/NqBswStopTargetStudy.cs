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
/// <b>PT5DAV_NQ_BSW_001_240 con uno stop largo e un target ampio</b> (28/09/2026). La strategia compra il
/// Nasdaq lunedi' alle 04:00 e vende giovedi' alle 04:00 senza stop ne' target: il 29/07/2026 la sua perdita
/// aperta ha portato <c>PT5DAV-O4</c> a size 1 oltre il limite giornaliero FTMO. La domanda e' se uno stop
/// largo e un target ampio tolgono le code senza togliere la strategia.
///
/// <para><b>Campione e fuori campione.</b> La regola di base e' della ricerca PT5DAV (CFD 2012 → 30/05/2025),
/// quindi fino al 05/2025 e' tutto in campione per la base. Lo stop e il target si scelgono sul feed
/// interno 2012 → 2020 con una regola scritta prima di guardare; il feed interno 2021 → 05/2025 e' fuori
/// campione per la scelta (non per la base), il feed FTMO 06/2025 → 09/2026 e' fuori campione per tutto.</para>
///
/// <para><b>Lo swap sul feed interno.</b> FTMO lo dichiara in punti per notte (6,71 long sul Nasdaq, scheda
/// del 21/09/2026, con l'indice a circa 30.400). Applicato identico al Nasdaq del 2012, che valeva un
/// dodicesimo, e' un finanziamento di quasi il 100% l'anno e fa perdere anche il long tutta la settimana
/// in un decennio di rialzo: il primo giro dello studio e' uscito cosi'. Sui periodi interni lo swap di ogni
/// trade si riporta quindi al suo prezzo d'ingresso (<see cref="SwapReferencePrice"/>), come fa la ricerca
/// PT5DAV riportando i trade al prezzo di oggi. Sul feed FTMO resta quello misurato.</para>
///
/// <para><b>Il calendario.</b> La stessa tenuta di tre sessioni negli altri giorni della settimana, e la
/// settimana intera: se il lunedi'-giovedi' non fa meglio degli altri giorni, la strategia e' la deriva
/// del Nasdaq e nient'altro.</para>
///
/// <para>Costi FTMO (spread mediano, swap), orologio al minuto, un contratto. Resoconto in
/// <c>ricerca/pt5dav/nq-bsw-stop-target.md</c>.</para>
/// </summary>
public sealed class NqBswStopTargetStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    /// <summary>Chiusura di @NQ sul feed FTMO il 21/09/2026, giorno della scheda swap US100.cash.</summary>
    private const decimal SwapReferencePrice = 30_400m;

    private static readonly decimal[] Stops = [0m, 1m, 1.5m, 2m, 2.5m, 3m, 4m];
    private static readonly decimal[] Targets = [0m, 3m, 4m, 5m, 6m, 8m];

    /// <summary>La strategia nelle chiavi della consegna, identica a PT5DAV_NQ_BSW_001_240 a stop e target spenti.</summary>
    private static Dictionary<string, object> Strategy(int entryDay, int exitDay, decimal stopAtr, decimal targetAtr) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Symbol"] = "@NQ", ["TimeframeMinutes"] = 240,
            ["le_day"] = entryDay, ["le_time"] = 400, ["lx_day"] = exitDay, ["lx_time"] = 400,
            ["se_day"] = -1, ["se_time"] = 400, ["sx_day"] = 0, ["sx_time"] = 400,
            ["ptn_ly_yes"] = 152, ["ptn_ly_no"] = 153, ["ptn_sy_yes"] = 152, ["ptn_sy_no"] = 153,
            ["stop_atr"] = stopAtr, ["take_profit_atr"] = targetAtr
        };

    /// <summary>I numeri di un run, dai trade chiusi, con lo swap eventualmente riportato al prezzo d'ingresso.</summary>
    private sealed record Metrics(int Trades, decimal Net, decimal Drawdown, decimal Worst, decimal Swap, int Stopped, int Targeted)
    {
        public decimal Ratio => Drawdown > 0 ? Net / Drawdown : 0m;
    }

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task WideStopAndTargetOnTheWeeklyNasdaqLong()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol).Points["NQ"];
        var swap = SwapTable.Load(settings.GetSwapPath(), "FTMO").Specs["NQ"];
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        var periods = new List<(string Name, string Role, bool ScaleSwap, SweepSeries Series)>
        {
            ("interno 2012-2020", "scelta di stop e target", true,
                await SweepSeries.LoadAsync(dataFeed, "@NQ", [240, 1], Utc(2012, 1, 10), Utc(2021, 1, 1), null, warmupDays: 120d)),
            ("interno 2021-05/2025", "fuori campione per la scelta, in campione per la base", true,
                await SweepSeries.LoadAsync(dataFeed, "@NQ", [240, 1], Utc(2021, 1, 1), Utc(2025, 5, 30), null, warmupDays: 120d)),
            ("FTMO 06/2025-09/2026", "fuori campione per tutto", false,
                await SweepSeries.LoadAsync(dataFeed, "@NQ", [240, 1], Utc(2025, 6, 1), Utc(2026, 9, 21), "FTMO", warmupDays: 120d))
        };

        SweepJob Job(IReadOnlyDictionary<string, object> parameters) => new("RC5_BSW", parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["NQ"] = spread },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { ["NQ"] = swap },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        var grid = (from s in Stops from t in Targets select (Stop: s, Target: t)).ToList();
        var calendar = new (string Name, int Entry, int Exit)[]
        {
            ("lun-gio (la strategia)", 0, 3), ("mar-ven", 1, 4), ("mer-lun", 2, 0), ("gio-mar", 3, 1),
            ("ven-mer", 4, 2), ("settimana intera lun-lun", 0, 0)
        };

        var report = new StringBuilder("# NQ BSW: stop largo, target ampio e calendario\n\n");
        report.AppendLine("Base: `PT5DAV_NQ_BSW_001_240` (long lunedi' 04:00 → giovedi' 04:00, ora della ricerca, nessuno stop ne' target). " +
                          $"Stop e target in ATR50 delle sessioni dal prezzo d'ingresso. Costi FTMO: spread {spread} punti, swap long {swap.LongPointsPerNight} punti a notte. " +
                          "Orologio al minuto, un contratto.\n");
        report.AppendLine($"**Swap sui periodi interni** riportato al prezzo d'ingresso di ogni trade (× prezzo / {SwapReferencePrice:N0}, il Nasdaq del giorno della scheda FTMO): " +
                          "applicato in punti fissi al Nasdaq del 2012 sarebbe un finanziamento di quasi il 100% l'anno. Sul feed FTMO resta quello misurato. " +
                          "Lo spread resta in punti fissi anche sui periodi interni (pesa meno di 30 dollari a trade).\n");
        report.AppendLine("**Regola di scelta, scritta prima dei risultati**, sul solo periodo 2012-2020: fra le configurazioni con stop acceso, " +
                          "quelle con netto almeno l'85% della base e net/DD almeno quello della base; fra queste la migliore per net/DD; " +
                          "plateau: la media net/DD dei vicini (stop e target adiacenti) almeno l'80% della scelta. Nessuna che passi = la base resta com'e'. " +
                          "Gli altri due periodi non entrano nella scelta.\n");

        var csv = new StringBuilder("periodo,stop_atr,target_atr,trade,netto,dd,netdd,peggiore,swap,stop_usciti,target_usciti\n");
        var results = new Dictionary<(string, decimal, decimal), Metrics>();
        foreach (var (name, _, scaleSwap, series) in periods)
        {
            foreach (var (key, outcome) in RunAll(series, grid.Select(g => (g, Strategy(0, 3, g.Stop, g.Target))), Job))
            {
                var m = Measure(outcome, scaleSwap);
                results[(name, key.Stop, key.Target)] = m;
                csv.AppendLine(string.Join(',', name, Num(key.Stop), Num(key.Target), m.Trades, Num(m.Net), Num(m.Drawdown),
                    Num(m.Ratio), Num(m.Worst), Num(m.Swap), m.Stopped, m.Targeted));
            }
        }

        // La scelta, sul solo primo periodo.
        var selection = periods[0].Name;
        var baseline = results[(selection, 0m, 0m)];
        var eligible = grid.Where(g => g.Stop > 0m)
            .Select(g => (g.Stop, g.Target, M: results[(selection, g.Stop, g.Target)]))
            .Where(c => c.M.Net >= 0.85m * baseline.Net && c.M.Ratio >= baseline.Ratio)
            .OrderByDescending(c => c.M.Ratio).ThenByDescending(c => c.M.Worst)
            .ToList();

        (decimal Stop, decimal Target)? chosen = null;
        var decision = new StringBuilder();
        foreach (var candidate in eligible)
        {
            var neighbours = grid.Where(g => IsNeighbour(g, (candidate.Stop, candidate.Target)) && g.Stop > 0m)
                .Select(g => results[(selection, g.Stop, g.Target)].Ratio).ToList();
            var plateau = neighbours.Count > 0 ? neighbours.Average() : 0m;
            var passes = baseline.Net > 0m && plateau >= 0.8m * candidate.M.Ratio;
            decision.AppendLine($"- stop {Num(candidate.Stop)} target {Num(candidate.Target)}: net/DD {candidate.M.Ratio:0.00}, " +
                                $"plateau {plateau:0.00} su {neighbours.Count} vicini → {(passes ? "**scelta**" : "non passa")}");
            if (passes) { chosen = (candidate.Stop, candidate.Target); break; }
        }

        report.AppendLine("## La scelta (interno 2012-2020)\n");
        report.AppendLine($"Base: {Line(baseline)}. Configurazioni ammesse dalla regola: {eligible.Count} su {grid.Count(g => g.Stop > 0m)}.\n");
        if (baseline.Net <= 0m)
            report.AppendLine("La base non guadagna sul periodo di scelta: la regola non si applica.\n");
        report.AppendLine(decision.Length > 0 ? decision.ToString() : "- nessuna configurazione passa la regola\n");
        report.AppendLine(chosen is { } c0
            ? $"\n**Scelta: stop {Num(c0.Stop)} ATR, target {Num(c0.Target)} ATR** ({(c0.Target == 0m ? "target spento" : "target acceso")}).\n"
            : "\n**Nessuna scelta: la base resta senza stop.**\n");

        report.AppendLine("## Base e scelta sui tre periodi\n");
        report.AppendLine("| periodo | ruolo | configurazione | trade | netto | DD chiuso | net/DD | peggior trade | swap | usciti a stop | usciti a target |");
        report.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var (name, role, _, _) in periods)
        {
            report.AppendLine(Row(name, role, "base", results[(name, 0m, 0m)]));
            if (chosen is { } c1)
                report.AppendLine(Row(name, role, $"stop {Num(c1.Stop)} / target {Num(c1.Target)}", results[(name, c1.Stop, c1.Target)]));
        }

        foreach (var (name, role, _, _) in periods)
        {
            report.AppendLine($"\n## Griglia completa: {name} ({role})\n");
            report.AppendLine("net/DD, e fra parentesi il netto in migliaia. Righe = stop in ATR (0 = nessuno), colonne = target in ATR (0 = nessuno).\n");
            report.AppendLine("| stop \\ target | " + string.Join(" | ", Targets.Select(Num)) + " |");
            report.AppendLine("|---|" + string.Concat(Targets.Select(_ => "---:|")));
            foreach (var s in Stops)
                report.AppendLine($"| {Num(s)} | " + string.Join(" | ", Targets.Select(t =>
                {
                    var m = results[(name, s, t)];
                    return $"{m.Ratio:0.00} ({m.Net / 1000m:0})";
                })) + " |");
        }

        report.AppendLine("\n## Il calendario: la stessa tenuta negli altri giorni, senza stop ne' target\n");
        report.AppendLine("| periodo | finestra | trade | netto | DD chiuso | net/DD | peggior trade | swap |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
        foreach (var (name, _, scaleSwap, series) in periods)
        {
            var runs = RunAll(series, calendar.Select(w => (w, Strategy(w.Entry, w.Exit, 0m, 0m))), Job);
            foreach (var w in calendar)
            {
                var m = Measure(runs.Single(r => r.Key == w).Outcome, scaleSwap);
                report.AppendLine($"| {name} | {w.Name} | {m.Trades} | {m.Net:N0} | {m.Drawdown:N0} | {m.Ratio:0.00} | {m.Worst:N0} | {m.Swap:N0} |");
            }
        }

        var folder = Path.Combine(RepositoryPath, "ricerca", "pt5dav");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "nq-bsw-stop-target.md"), report.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folder, "nq-bsw-stop-target.csv"), csv.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
    }

    /// <summary>
    /// Netto, drawdown dei trade chiusi e peggior trade, con lo swap di ogni trade riportato al suo prezzo
    /// d'ingresso quando <paramref name="scaleSwap"/>. Lo swap del run e' in punti fissi ai prezzi di oggi.
    /// </summary>
    private static Metrics Measure(SweepOutcome outcome, bool scaleSwap)
    {
        decimal equity = 0m, peak = 0m, drawdown = 0m, worst = 0m, swapTotal = 0m;
        int stopped = 0, targeted = 0;
        foreach (var trade in outcome.ClosedTrades.OrderBy(t => t.ExitDate))
        {
            var swapCost = scaleSwap ? trade.Swap * trade.EntryPrice / SwapReferencePrice : trade.Swap;
            var net = trade.NetProfit + trade.Swap - swapCost;
            swapTotal += swapCost;
            equity += net;
            peak = Math.Max(peak, equity);
            drawdown = Math.Max(drawdown, peak - equity);
            worst = Math.Min(worst, net);
            var reason = trade.ExitReason.ToString();
            if (reason.Contains("Stop", StringComparison.OrdinalIgnoreCase)) stopped++;
            if (reason.Contains("Target", StringComparison.OrdinalIgnoreCase) || reason.Contains("Profit", StringComparison.OrdinalIgnoreCase)) targeted++;
        }

        return new Metrics(outcome.ClosedTrades.Count, equity, drawdown, worst, swapTotal, stopped, targeted);
    }

    private static List<(TKey Key, SweepOutcome Outcome)> RunAll<TKey>(
        SweepSeries series, IEnumerable<(TKey Key, Dictionary<string, object> Parameters)> configurations,
        Func<IReadOnlyDictionary<string, object>, SweepJob> job)
    {
        var items = configurations.ToList();
        var results = new ConcurrentBag<(TKey, SweepOutcome)>();
        Parallel.ForEach(items,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
            () => new SweepRunner(series),
            (item, _, runner) =>
            {
                results.Add((item.Key, runner.Run(job(item.Parameters))));
                return runner;
            },
            _ => { });
        return results.ToList();
    }

    private static bool IsNeighbour((decimal Stop, decimal Target) g, (decimal Stop, decimal Target) c)
    {
        var ds = Math.Abs(Array.IndexOf(Stops, g.Stop) - Array.IndexOf(Stops, c.Stop));
        var dt = Math.Abs(Array.IndexOf(Targets, g.Target) - Array.IndexOf(Targets, c.Target));
        return ds + dt == 1;
    }

    private static string Row(string period, string role, string configuration, Metrics m) =>
        $"| {period} | {role} | {configuration} | {m.Trades} | {m.Net:N0} | {m.Drawdown:N0} | {m.Ratio:0.00} | " +
        $"{m.Worst:N0} | {m.Swap:N0} | {m.Stopped} | {m.Targeted} |";

    private static string Line(Metrics m) =>
        $"{m.Trades} trade, netto {m.Net:N0}, DD {m.Drawdown:N0}, net/DD {m.Ratio:0.00}, peggior trade {m.Worst:N0}, swap {m.Swap:N0}";

    private static string Num(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
