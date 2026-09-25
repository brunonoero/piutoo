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
    /// Nome del motore, per le intestazioni del CSV e del resoconto. Non seleziona nulla — il motore
    /// e' quello della classe di partenza — ma un file che non lo dichiara diventa illeggibile
    /// appena esistono due griglie sulla stessa cella con motori diversi, che e' precisamente il
    /// confronto per cui la griglia e' stata generalizzata.
    /// </summary>
    string EngineName = "Price Channel",
    /// <summary>
    /// La chiave della <b>prima leva</b>, quella strutturale del motore: <c>ChannelBars</c> per il
    /// Price Channel (quante barre fa il canale), <c>MaxBars</c> per il trend following (dopo quante
    /// barre la posizione muore, visto che il livello e' l'estremo del giorno prima e non si sceglie).
    /// I valori restano in <see cref="Channels"/>.
    /// </summary>
    string FirstLeverKey = "ChannelBars",
    /// <summary>Come la prima leva si chiama nel CSV e nel resoconto.</summary>
    string FirstLeverLabel = "channelBars",
    /// <summary>
    /// Divisore applicato ai valori di <see cref="Channels"/> prima di passarli alla classe: la
    /// griglia e' di interi, ma la leva strutturale di un motore puo' essere un moltiplicatore
    /// (il <c>k</c> del volatility breakout, 0,5…4,0). Con 10 i valori sono decimi: 5 = 0,5. Nel CSV
    /// resta l'intero, e l'intestazione dichiara l'unita'.
    /// </summary>
    decimal FirstLeverDivisor = 1m,
    /// <summary>
    /// Se il motore ha una leva <c>Direction</c>. Il Price Channel si', il trend following no —
    /// emette entrambi i lati e la direzione la decidono i gate di pattern. Quando e' falso
    /// <see cref="Directions"/> va lasciato a un valore solo: passare la chiave a una classe che non
    /// la legge farebbe girare tre volte la stessa combinazione senza che nulla lo dica.
    /// </summary>
    bool VariesDirection = true,
    /// <summary>
    /// Se vero, <see cref="Stops"/> e <see cref="Targets"/> sono multipli dell'ATR delle sessioni
    /// chiuse in <b>decimi</b> (10 = 1,0 ATR, 15 = 1,5) invece che dollari per contratto: la
    /// griglia passa <c>StopAtr</c>/<c>TargetAtr</c> e azzera il denaro fisso. Stesse colonne nel
    /// CSV, con l'unita' dichiarata nell'intestazione.
    /// </summary>
    bool AtrStops = false,
    /// <summary>
    /// Giornate di tenuta: 0 = intraday (chiude a fine sessione, com'era sempre stata la griglia),
    /// N = puo' restare aperta fino a N sessioni. La v5.0 le cercava tutte (1, 2, 3, 5, 10) e la
    /// griglia le teneva ferme a zero, quindi un motore che vive di movimenti di piu' giorni qui
    /// risultava senza edge per costruzione. Una tenuta oltre la notte esclude l'ora di uscita: le
    /// due insieme sarebbero la stessa combinazione intraday. Null = solo intraday.
    /// </summary>
    int[]? HoldDays = null,
    /// <summary>
    /// Parametri fissi in piu', per ogni combinazione: la variante del motore (il breakout della
    /// sessione in corso invece di N sessioni) e, per i contenitori generici, simbolo e timeframe.
    /// </summary>
    IReadOnlyDictionary<string, object>? ExtraParameters = null,
    /// <summary>
    /// Altri simboli da caricare per le strategie tra mercati (XMK), dallo stesso feed e sullo stesso
    /// periodo, sul timeframe della cella. Si leggono e basta: costi, trade e resoconto restano quelli
    /// di <see cref="Symbol"/>. Null = cella su un simbolo solo, come tutte le altre.
    /// </summary>
    string[]? ReferenceSymbols = null);

public static class CoarseGridStudy
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    public sealed record Cell(
        int ChannelBars, int StopLoss, int TakeProfit, int ExitHour, int Direction, int HoldDays,
        SweepOutcome InSample, SweepOutcome OutOfSample, int OosWindowsInProfit);

    /// <summary>
    /// I criteri di una cella, calcolati sull'ammissibile: costanza negli anni su tutto il periodo,
    /// quota del trade migliore dentro e fuori, average trade e UngerFit nel campione.
    /// </summary>
    private sealed record Judged(Cell Cell, YearConsistency Years, decimal? BestShareIn, decimal? BestShareOut,
        decimal AverageTrade, decimal? UngerFit)
    {
        public bool OutlierPasses =>
            BestShareIn is { } inside && inside <= ResearchCriteria.MaxBestTradeShare &&
            (BestShareOut is null || BestShareOut <= ResearchCriteria.MaxBestTradeShare);
    }

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

        // Le serie degli altri mercati, per le strategie tra mercati: stesso feed, stesso periodo,
        // tagliate allo stesso split. Il minuto non serve, perche' su quei simboli non si opera.
        var referencesIn = new Dictionary<string, SweepSeries>(StringComparer.OrdinalIgnoreCase);
        var referencesOut = new Dictionary<string, SweepSeries>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in spec.ReferenceSymbols ?? [])
        {
            var loaded = await SweepSeries.LoadAsync(
                dataFeed, reference, [spec.TimeframeMinutes], spec.StartUtc, spec.EndUtc, warmupDays: 30d, broker: spec.FeedBroker);
            referencesIn[reference] = loaded.Between(loaded.StartUtc, spec.SplitUtc);
            referencesOut[reference] = loaded.Between(spec.SplitUtc, loaded.EndUtc);
            output.WriteLine($"riferimento {reference}: {loaded.Bars(spec.TimeframeMinutes).Length} barre da {spec.TimeframeMinutes}m");
        }

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
        // Le chiavi che la classe di partenza non legge vengono ignorate: OffsetTicks e DvolMin
        // esistono solo sul Price Channel, MaxBars su entrambi ma come leva solo sul trend following.
        var fixedParameters = new Dictionary<string, object>
        {
            ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
            ["StartHour"] = -1, ["EndHour"] = -1, ["DvolMin"] = 0, ["SkipDay"] = -1,
            ["IntradayOnly"] = 1, ["OffsetTicks"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0
        };

        // MaxBars resta fisso a zero solo quando non e' la prima leva: altrimenti lo scrive il combo.
        if (!string.Equals(spec.FirstLeverKey, "MaxBars", StringComparison.Ordinal))
            fixedParameters["MaxBars"] = 0;
        foreach (var (name, value) in spec.ExtraParameters ?? new Dictionary<string, object>())
            fixedParameters[name] = value;

        // Una tenuta oltre la notte con un'ora di uscita sarebbe la stessa combinazione intraday.
        var holds = spec.HoldDays is { Length: > 0 } ? spec.HoldDays : [0];
        var combos = (from c in spec.Channels from s in spec.Stops from t in spec.Targets from e in spec.ExitHours from d in spec.Directions
                      from h in holds
                      where h == 0 || e == -1
                      select (c, s, t, e, d, h)).ToList();
        // Barre di una sessione (23 ore, come la ricerca Python): la tenuta di N giornate e' N volte tanto.
        var barsPerSession = Math.Max(1, 1380 / spec.TimeframeMinutes);
        output.WriteLine($"{combos.Count} combinazioni, orologio al minuto, {Environment.ProcessorCount} core\n");

        var cells = new ConcurrentBag<Cell>();
        var started = System.Diagnostics.Stopwatch.StartNew();
        var done = 0;

        // Un runner per thread: PiootooTradingService non e' thread-safe, le serie si condividono.
        Parallel.ForEach(
            combos,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
            () => (In: new SweepRunner(inSample, referencesIn), Out: new SweepRunner(outOfSample, referencesOut)),
            (combo, _, runners) =>
            {
                var parameters = new Dictionary<string, object>(fixedParameters)
                {
                    [spec.FirstLeverKey] = spec.FirstLeverDivisor == 1m ? combo.c : combo.c / spec.FirstLeverDivisor,
                    ["ExitHour"] = combo.e
                };
                if (spec.VariesDirection) parameters["Direction"] = combo.d;
                if (combo.h > 0)
                {
                    parameters["IntradayOnly"] = 0;
                    // Con MaxBars come prima leva la durata la decide gia' la leva: si apre solo la notte.
                    if (!string.Equals(spec.FirstLeverKey, "MaxBars", StringComparison.Ordinal))
                        parameters["MaxBars"] = combo.h * barsPerSession;
                }
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
                cells.Add(new Cell(combo.c, combo.s, combo.t, combo.e, combo.d, combo.h, isOutcome, oosOutcome,
                    WindowsInProfit(oosOutcome, spec.SplitUtc, series.EndUtc, 4)));

                var n = Interlocked.Increment(ref done);
                if (n % 100 == 0) output.WriteLine($"  {n}/{combos.Count} in {started.Elapsed.TotalMinutes:N1} min");
                return runners;
            },
            _ => { });

        output.WriteLine($"\nfinito in {started.Elapsed.TotalMinutes:N1} minuti");
        var list = cells.ToList();

        // La soglia si misura sulle barre del campione della cella, non si eredita.
        var instrument = InstrumentRegistry.Get(spec.Symbol);
        var threshold = ResearchCriteria.AverageTradeThreshold(
            inSample.Bars(spec.TimeframeMinutes), instrument.PointValue, instrument.TickSize);
        var judged = list.ToDictionary(c => c, c => Judge(c, spec, series.StartUtc, series.EndUtc, threshold));

        WriteCsv(spec, list, judged);
        Report(spec, list, judged, threshold, output);
        return list;
    }

    private static Judged Judge(Cell c, CoarseGridSpec spec, DateTime fromUtc, DateTime toUtc, decimal threshold)
    {
        var all = c.InSample.ClosedTrades.Concat(c.OutOfSample.ClosedTrades).ToList();
        var average = c.InSample.Trades > 0 ? c.InSample.NetProfit / c.InSample.Trades : 0m;
        return new Judged(
            c,
            ResearchCriteria.Years(all, fromUtc, toUtc),
            ResearchCriteria.BestTradeShare(c.InSample.ClosedTrades),
            ResearchCriteria.BestTradeShare(c.OutOfSample.ClosedTrades),
            average,
            ResearchCriteria.UngerFit(average, threshold, c.InSample.MaxClosedTradeDrawdown));
    }

    private static void Report(CoarseGridSpec spec, List<Cell> cells, Dictionary<Cell, Judged> judged, decimal threshold,
        ITestOutputHelper output)
    {
        var admissible = cells
            .Where(c => c.InSample.Trades >= spec.MinInSampleTrades && c.InSample.NetProfit > 0m)
            .ToList();
        var robust = admissible.Where(c => c.OutOfSample.NetProfit > 0m && c.OosWindowsInProfit >= 3).ToList();

        output.WriteLine($"\nammissibili (IS ≥ {spec.MinInSampleTrades} trade e IS > 0): {admissible.Count} su {cells.Count}");
        output.WriteLine($"di cui fuori campione in utile con ≥ 3 finestre su 4: {robust.Count}");
        if (admissible.Count == 0) return;

        output.WriteLine("\nle 15 migliori fuori campione fra le ammissibili (netto OOS / DD OOS):");
        output.WriteLine($"  {spec.FirstLeverLabel,-4} stop  targ  exit dir ten |   IS n    IS netto    IS DD |  OOS n   OOS netto   OOS DD  fin");
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

        // I criteri della ricerca Python v4/v5 sopra le equilibrate: regolarita' negli anni e nessun
        // trade che faccia da solo il risultato. Poi le soglie del metodo, dentro il campione.
        var regular = balanced.Where(c => judged[c].Years.Passes && judged[c].OutlierPasses).ToList();
        output.WriteLine(
            $"\nrobuste (equilibrate + anni: ≥ {ResearchCriteria.MinTradesPerYearWithTrades} trade ogni anno, " +
            $"≥ {ResearchCriteria.MinAverageTradesPerYear} all'anno, ≥ meta' anni in utile; " +
            $"trade migliore ≤ {ResearchCriteria.MaxBestTradeShare:P0} del netto dentro e fuori): {regular.Count}");
        var failedYears = balanced.Count(c => !judged[c].Years.Passes);
        var failedOutlier = balanced.Count(c => !judged[c].OutlierPasses);
        if (balanced.Count > 0)
            output.WriteLine($"  delle equilibrate: {failedYears} bocciate sugli anni, {failedOutlier} sull'outlier");

        output.WriteLine(
            $"\nsoglia di average trade nel campione: {threshold:N2} per contratto " +
            $"({ResearchCriteria.RangeShareThreshold:P0} del range medio della barra, minimo {ResearchCriteria.MinTicksThreshold} tick)");
        var passing = regular
            .Where(c => judged[c].AverageTrade >= threshold && judged[c].UngerFit is >= 1m)
            .ToList();
        output.WriteLine($"sopra soglia (robuste + average trade ≥ soglia + UngerFit ≥ 1): {passing.Count}");
        foreach (var c in regular.OrderByDescending(c => judged[c].UngerFit ?? 0m).Take(10))
            output.WriteLine($"{Row(c)}  avg {judged[c].AverageTrade,8:N0}  UF {judged[c].UngerFit ?? 0m,5:N2}");

        if (spec.HoldDays is { Length: > 1 })
        {
            output.WriteLine("\ntenuta, media del netto sulle ammissibili:");
            foreach (var g in admissible.GroupBy(c => c.HoldDays).OrderBy(g => g.Key))
                output.WriteLine($"  giorni={g.Key,3}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        }

        output.WriteLine("\nora di uscita, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.ExitHour).OrderBy(g => g.Key))
            output.WriteLine($"  ExitHour={g.Key,3}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        if (spec.VariesDirection)
        {
            output.WriteLine("\ndirezione, media del netto sulle ammissibili:");
            foreach (var g in admissible.GroupBy(c => c.Direction).OrderBy(g => g.Key))
                output.WriteLine($"  Direction={g.Key}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        }

        output.WriteLine($"\n{spec.FirstLeverLabel}, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.ChannelBars).OrderBy(g => g.Key))
            output.WriteLine($"  {spec.FirstLeverLabel}={g.Key,4}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
        output.WriteLine("\nstop, media del netto sulle ammissibili:");
        foreach (var g in admissible.GroupBy(c => c.StopLoss).OrderBy(g => g.Key))
            output.WriteLine($"  Stop={g.Key,5}: {g.Count(),3} celle, IS medio {g.Average(c => c.InSample.NetProfit),10:N0}, OOS medio {g.Average(c => c.OutOfSample.NetProfit),10:N0}");
    }

    private static decimal Ratio(SweepOutcome o) =>
        o.MaxClosedTradeDrawdown > 0m ? o.NetProfit / o.MaxClosedTradeDrawdown : (o.NetProfit > 0m ? 999m : -999m);

    private static string Row(Cell c) =>
        $"  {c.ChannelBars,3} {c.StopLoss,5} {c.TakeProfit,5} {c.ExitHour,5} {c.Direction,3} {c.HoldDays,2}g | " +
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

    private static void WriteCsv(CoarseGridSpec spec, List<Cell> cells, Dictionary<Cell, Judged> judged)
    {
        var path = Path.Combine(RepositoryPath, "ricerca", spec.CsvName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sb = new StringBuilder();
        sb.AppendLine($"# Griglia grossa {spec.Symbol} {spec.TimeframeMinutes}m {spec.EngineName}, motore nudo (pattern spenti, nessun filtro orario), {cells.Count} combinazioni.");
        if (spec.AtrStops)
            sb.AppendLine("# stopLoss e takeProfit sono DECIMI di ATR delle sessioni chiuse (10 = 1,0 ATR), non dollari.");
        if (spec.FirstLeverDivisor != 1m)
            sb.AppendLine($"# {spec.FirstLeverLabel} e' un intero da dividere per {spec.FirstLeverDivisor}: il valore passato alla classe e' {spec.FirstLeverLabel}/{spec.FirstLeverDivisor}.");
        sb.AppendLine($"# Feed {spec.FeedBroker}, spread peggiore fra {string.Join("/", spec.SpreadBrokers)}, swap peggiore fra {string.Join("/", spec.SwapBrokers)}, commissione {spec.CommissionPerSide}/lato, orologio al minuto. Campione {spec.StartUtc:yyyy-MM-dd} -> {spec.SplitUtc:yyyy-MM-dd}, fuori campione -> {spec.EndUtc:yyyy-MM-dd}.");
        sb.AppendLine("# holdDays: 0 = intraday, N = fino a N sessioni. yearsOk: costanza negli anni su tutto il periodo. bestShare*: trade migliore / netto. avgIS e ungerFit: nel campione, contro la soglia della cella.");
        sb.AppendLine($"{spec.FirstLeverLabel};stopLoss;takeProfit;exitHour;direction;holdDays;isTrades;isNet;isDD;isPF;oosTrades;oosNet;oosDD;oosPF;oosWindowsInProfit;yearsOk;minTradesYear;tradesPerYear;profitableYears;yearsWithTrades;bestShareIS;bestShareOOS;avgIS;ungerFit");
        foreach (var c in cells.OrderBy(c => c.ChannelBars).ThenBy(c => c.StopLoss).ThenBy(c => c.TakeProfit).ThenBy(c => c.ExitHour).ThenBy(c => c.Direction).ThenBy(c => c.HoldDays))
        {
            var j = judged[c];
            sb.Append(string.Join(';',
                c.ChannelBars, c.StopLoss, c.TakeProfit, c.ExitHour, c.Direction, c.HoldDays,
                c.InSample.Trades, F(c.InSample.NetProfit), F(c.InSample.MaxClosedTradeDrawdown), F(c.InSample.ProfitFactor),
                c.OutOfSample.Trades, F(c.OutOfSample.NetProfit), F(c.OutOfSample.MaxClosedTradeDrawdown), F(c.OutOfSample.ProfitFactor),
                c.OosWindowsInProfit,
                j.Years.Passes ? 1 : 0, j.Years.MinTradesInYear, F(j.Years.TradesPerYear), j.Years.ProfitableYears, j.Years.YearsWithTrades,
                F(j.BestShareIn), F(j.BestShareOut), F(j.AverageTrade), F(j.UngerFit)));
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string F(decimal? v) => v.HasValue ? v.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
}
