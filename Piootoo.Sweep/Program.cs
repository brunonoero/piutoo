using System.Diagnostics;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Sweep;

/// <summary>
/// Lancia una ricerca completa su una cella (simbolo, timeframe) e scrive il resoconto.
///
/// <para><b>Perche' un eseguibile e non un test.</b> Una sweep vera dura ore: non appartiene a una
/// suite, va lanciata a mano, deve poter essere interrotta e ripetuta con gli stessi argomenti, e il
/// suo esito e' un documento da leggere e archiviare, non un verde o un rosso.</para>
///
/// <example>
/// piootoo-sweep --strategy PT2_NQ_PCH_001_240 --symbol @NQ --timeframe 240 ^
///               --broker ICS --spread-broker FTMOPLATFORM ^
///               --from 2014-07-17 --split 2022-01-01 --to 2026-09-17 ^
///               --out C:\ricerca\nq-4h.md
/// </example>
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            return await RunAsync(options);
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine(error.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Options.Usage);
            return 2;
        }
    }

    private static async Task<int> RunAsync(Options options)
    {
        var started = Stopwatch.StartNew();
        var settings = new PiootooSettings
        {
            BasePath = options.RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };

        // Lo spread e' una MISURA, e la si carica dalla stessa tabella che userebbe il backtest:
        // scriverne il numero a mano qui significherebbe avere due verita' che nessuno riconcilia.
        var spread = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var spreadByHour = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(options.SpreadBroker))
        {
            // Per ora e non per simbolo, quando la ricerca puo' scegliere gli orari: con una costante
            // giornaliera le fasce a spread largo sembrano economiche quanto le altre e la sweep ci
            // si infila. Vedi SweepJob.SpreadPointsByHour.
            // Anche qui piu' broker separati da virgola: per ogni simbolo, e per ogni ora, vince il
            // piu' caro. Su @FDAX e' FTMO (1,23 contro 0,50 di mediana), mentre sullo swap e' ICS:
            // il costo peggiore si costruisce voce per voce, non scegliendo un listino.
            var spreadBrokers = options.SpreadBroker
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var resolution = options.SpreadPerHour ? SpreadResolution.PerHour : SpreadResolution.PerSymbol;
            var tavole = spreadBrokers
                .Select(broker => SpreadTable.Load(
                    settings.GetSpreadPath(), broker, options.SpreadStatistic, resolution))
                .ToList();

            var table = tavole[0];
            Console.WriteLine($"[sweep] spread: {table.Describe()}");
            foreach (var warning in table.Warnings)
                Console.WriteLine($"[sweep] spread, attenzione: {warning}");

            var key = StrategyKeys.NormalizeSymbol(options.Symbol);
            var costanti = tavole
                .Where(t => t.Points.ContainsKey(key))
                .Select(t => t.Points[key])
                .ToList();

            if (costanti.Count == 0)
            {
                Console.Error.WriteLine(
                    $"Nessuna fra le tabelle di {options.SpreadBroker} misura {key}. Un costo mancante " +
                    "non si sostituisce con zero: la ricerca girerebbe senza il costo che poi paghera'.");
                return 3;
            }

            spread[key] = costanti.Max();
            Console.WriteLine($"[sweep] spread {key}: {spread[key]} punti" +
                              (spreadBrokers.Length > 1 ? $" — PEGGIORE fra {string.Join(", ", spreadBrokers)}" : string.Empty));

            if (options.SpreadPerHour)
            {
                // Ora per ora: la fascia cara di un broker puo' non essere quella dell'altro, e una
                // strategia che sceglie gli orari deve trovarle entrambe care.
                var orarie = tavole.Where(t => t.PointsByHour.ContainsKey(key)).Select(t => t.PointsByHour[key]).ToList();
                if (orarie.Count > 0)
                {
                    var peggiori = new decimal[24];
                    for (var ora = 0; ora < 24; ora++)
                        peggiori[ora] = orarie.Max(valori => ora < valori.Length ? valori[ora] : 0m);

                    spreadByHour[key] = peggiori;
                    var quotate = peggiori.Where(valore => valore > 0m).ToArray();
                    Console.WriteLine(
                        $"[sweep] spread {key} per ora UTC: da {quotate.Min()} a {quotate.Max()} punti " +
                        $"({quotate.Length} ore quotate; le altre usano la costante)");
                }
                else
                {
                    Console.WriteLine($"[sweep] attenzione: nessuna misura oraria per {key}, si usa la costante.");
                }
            }
        }

        // Il finanziamento oltre il rollover. Senza, tenere una posizione fino a mezzanotte e'
        // gratis e la ricerca ci si infila, esattamente come faceva con le ore a spread largo.
        // Piu' broker separati da virgola: si prende il costo PEGGIORE voce per voce. Vedi
        // SwapTable.Worst — non esiste "il broker piu' caro", e sceglierne uno lascia fuori meta'
        // del costo.
        var swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(options.SwapBroker))
        {
            var brokers = options.SwapBroker
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var tables = brokers.Select(broker => SwapTable.Load(settings.GetSwapPath(), broker)).ToList();
            var specs = brokers.Length == 1 ? tables[0].Specs : SwapTable.Worst(tables);

            var key = StrategyKeys.NormalizeSymbol(options.Symbol);
            if (!specs.TryGetValue(key, out var spec))
            {
                Console.Error.WriteLine(
                    $"Nessuno fra {string.Join(", ", brokers)} misura lo swap di {key}: un costo che " +
                    "non si conosce non vale zero.");
                return 3;
            }

            swap[key] = spec;
            Console.WriteLine(
                $"[sweep] swap {key}: long {spec.LongPointsPerNight} pt/notte, " +
                $"short {spec.ShortPointsPerNight} pt/notte, rollover {spec.RolloverUtc:HH\\:mm} UTC, " +
                $"triplo {(spec.TripleDay?.ToString() ?? "nessuno")}" +
                (brokers.Length > 1 ? $" — PEGGIORE fra {string.Join(", ", brokers)}" : $" ({tables[0].Describe()})"));
        }

        Console.WriteLine($"[sweep] carico {options.Symbol} {options.Timeframe}m" +
                          (options.Broker is null ? " dal feed interno" : $" da {options.Broker}") +
                          $", {options.FromUtc:yyyy-MM-dd} → {options.ToUtc:yyyy-MM-dd}, piu' l'orologio a {options.ClockMinutes}m per tutte le fasi...");

        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var series = await SweepSeries.LoadAsync(
            dataFeed, options.Symbol, [options.Timeframe, options.ClockMinutes], options.FromUtc, options.ToUtc, options.Broker);

        foreach (var timeframe in series.Timeframes.OrderBy(tf => tf))
        {
            var bars = series.Bars(timeframe);
            var dropped = series.Dropped[timeframe];
            Console.WriteLine(
                $"[sweep] {timeframe,5}m: {bars.Length,9:N0} barre, {bars[0].DateTime:yyyy-MM-dd} → " +
                $"{bars[^1].DateTime:yyyy-MM-dd} (scartate {dropped.NonSessionDays:N0} fuori sessione, " +
                $"{dropped.OutsideWindow:N0} fuori finestra)");
        }

        var space = options.Engine switch
        {
            "PC" => SweepSpaces.PriceChannel(options.Timeframe),
            "BIASW" => SweepSpaces.BiasWeekly(),
            _ => throw new ArgumentException($"motore sconosciuto: {options.Engine}")
        };

        if (options.SplitPatternPhases)
        {
            space = space.SplitPatternPhases();
            Console.WriteLine("[sweep] fasi pattern spezzate: il prodotto fra pattern richiesto e vietato " +
                              "diventa una somma. E' una DEVIAZIONE dal motore di ricerca, che li ottimizza " +
                              "insieme: un pattern che rende solo in coppia con un certo divieto non verra' trovato.");
        }

        var optimizerOptions = new SweepOptimizerOptions
        {
            BeamWidth = options.BeamWidth,
            MaxCombinationsPerPhase = options.MaxCombinationsPerPhase,
            AccurateClockMinutes = options.ClockMinutes,
            Verbose = true
        };

        // Dal 21/09/2026 ogni fase gira al minuto: l'etichetta dice quale orologio ha misurato quel
        // numero, perche' fasi misurate su orologi diversi non sono confrontabili fra loro e il
        // resoconto le stampava nella stessa colonna senza dirlo.
        foreach (var phase in space.Phases)
        {
            var orologio = phase.RequiresAccurateClock || !optimizerOptions.UseFastClockForOrderingPhases
                ? $" (orologio a {options.ClockMinutes}m)"
                : $" (orologio veloce, {options.Timeframe}m)";
            Console.WriteLine($"[sweep] fase {phase.Name}: {space.CombinationCount(phase):N0} combinazioni{orologio}");
        }

        // La tenuta con cui la strategia verra' operata. Senza --flat-utc la ricerca gira come il
        // motore Python, overnight e overweek liberi; con il flat gira come il piano che vieta
        // l'overnight, cioe' con la deadline al flat e senza ingressi dentro la finestra. Cercare con
        // una tenuta e operare con un'altra valida una strategia diversa da quella che si opera.
        var holding = options.FlatUtc is { } flat
            ? AccountHoldingPolicy.Default with
            {
                AllowOvernight = false,
                AllowOverweek = false,
                SessionFlatUtc = flat,
                SessionFlatWindowMinutes = options.FlatWindowMinutes
            }
            : AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true };
        holding.Validate();
        Console.WriteLine($"[sweep] tenuta: {holding.Describe()}");

        // Il flat promette di non attraversare il rollover: vale solo se il rollover cade dentro la
        // finestra. Si avvisa e si prosegue, perche' misurare un flat DOPO il rollover puo' essere
        // proprio l'esperimento — ma il resoconto deve dirlo.
        foreach (var fuori in HoldingResolver.RolloversOutsideSessionFlatWindow(holding, swap.Values))
            Console.WriteLine($"[sweep] ATTENZIONE: il flat ({holding.DescribeSessionFlat()}) non copre {fuori}: " +
                              "le posizioni pagano il finanziamento lo stesso, o un ingresso appena dopo la finestra lo attraversa.");

        var template = new SweepJob(options.Strategy)
        {
            InitialCapital = options.InitialCapital,
            CommissionPerContract = options.Commission,
            Holding = holding,
            SpreadPoints = spread.Count > 0 ? spread : null,
            SpreadPointsByHour = spreadByHour.Count > 0 ? spreadByHour : null,
            Swap = swap.Count > 0 ? swap : null
        };

        // Sola validazione: si salta la ricerca e si misura una configurazione gia' scelta, dentro e
        // fuori campione. Serve a rimisurare una finalista quando cambia un'ipotesi di costo — e a
        // misurare su un feed nuovo una strategia che esiste gia', che e' la stessa operazione.
        if (options.Parameters is not null)
        {
            var validator = new SweepValidator(
                series.Between(series.StartUtc, options.SplitUtc),
                series.Between(options.SplitUtc, series.EndUtc),
                new NetOverDrawdownObjective(options.MinTrades),
                new SweepValidationOptions(),
                options.ClockMinutes);

            var single = validator.Validate(template, options.Parameters);
            Console.WriteLine();
            Console.WriteLine(single.ToString());

            // Con --out la modalita' di sola misura scrive anche i trade: senza la lista, un totale
            // che non torna con quello del broker non si puo' diagnosticare.
            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                var righe = new List<string>
                {
                    "entry;exit;side;entryPrice;exitPrice;gross;commission;swap;net;reason"
                };
                foreach (var trade in single.OutOfSample.ClosedTrades)
                {
                    righe.Add(string.Join(';',
                        trade.EntryDate.ToString("yyyy-MM-dd HH:mm"),
                        trade.ExitDate.ToString("yyyy-MM-dd HH:mm"),
                        trade.Direction,
                        trade.EntryPrice, trade.ExitPrice,
                        trade.GrossProfit, trade.Commission, trade.Swap, trade.NetProfit,
                        trade.ExitReason));
                }

                await File.WriteAllLinesAsync(options.OutputPath, righe);
                Console.WriteLine($"[sweep] {righe.Count - 1} trade fuori campione scritti in {options.OutputPath}");
            }
            foreach (var window in single.StabilityWindows)
                Console.WriteLine($"  {window.FromUtc:yyyy-MM-dd} → {window.ToUtc:yyyy-MM-dd}: {window.Trades} trade, {window.NetProfit:N0}");

            return single.Passed ? 0 : 1;
        }

        // Due criteri per due domande: quello della RICERCA sceglie fra decine di migliaia di
        // combinazioni e deve punire la fortuna; quello della VALIDAZIONE dice quanto ha reso
        // rispetto a quanto ha rischiato, e resta semplice perche' deve voler dire la stessa cosa
        // qualunque sia il criterio con cui si e' cercato.
        ISweepObjective objective = options.Objective switch
        {
            "net-over-dd" => new NetOverDrawdownObjective(options.MinTrades, options.MinAverageTrade),
            _ => new WorstSubPeriodObjective(
                options.MinTrades,
                MinAverageTrade: options.MinAverageTrade,
                MinProfitFactor: options.MinProfitFactor,
                MinLosingTrades: options.MinLosingTrades)
        };
        Console.WriteLine($"[sweep] criterio di ricerca: {objective.Describe()}");

        var result = SweepSearch.Run(
            series,
            options.SplitUtc,
            space,
            template,
            objective,
            optimizerOptions,
            new SweepValidationOptions(),
            options.TopCandidates);

        started.Stop();
        var report = Report(options, series, result, spread, spreadByHour, swap, started.Elapsed);
        Console.WriteLine();
        Console.WriteLine(report);

        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))!);
            await File.WriteAllTextAsync(options.OutputPath, report);
            Console.WriteLine($"[sweep] resoconto scritto in {options.OutputPath}");
        }

        return result.Survivor is null ? 1 : 0;
    }

    private static string DescribeSpread(
        Options options,
        IReadOnlyDictionary<string, decimal> spread,
        IReadOnlyDictionary<string, decimal[]> byHour)
    {
        if (spread.Count == 0)
            return "**nessuno** — la ricerca non paga alcun costo di transazione";

        var statistica = options.SpreadStatistic switch
        {
            SpreadStatistic.Mean => "media",
            SpreadStatistic.P90 => "**p90** (il caso brutto)",
            _ => "mediana"
        };

        var costante = string.Join(", ", spread.Select(entry => $"{entry.Key} {entry.Value} pt"));
        if (byHour.Count == 0)
            return $"{options.SpreadBroker}, {statistica} **costante** per simbolo: {costante}";

        var estremi = string.Join(", ", byHour.Select(entry =>
        {
            var quotate = entry.Value.Where(value => value > 0m).ToArray();
            return quotate.Length > 0
                ? $"{entry.Key} da {quotate.Min()} a {quotate.Max()} pt"
                : $"{entry.Key} nessuna ora quotata";
        }));

        return $"{options.SpreadBroker}, {statistica} **per ora UTC** ({estremi}); costante di riserva: {costante}";
    }

    private static string Report(
        Options options,
        SweepSeries series,
        SweepSearchResult result,
        IReadOnlyDictionary<string, decimal> spread,
        IReadOnlyDictionary<string, decimal[]> spreadByHour,
        IReadOnlyDictionary<string, SwapSpec> swap,
        TimeSpan elapsed)
    {
        var lines = new List<string>
        {
            $"# Sweep {options.Symbol} {options.Timeframe}m — {options.Engine}",
            string.Empty,
            $"- Strategia di partenza: `{options.Strategy}`",
            $"- Datafeed: {options.Broker ?? "interno (vendor)"}",
            // Il modello di costo va dichiarato nel resoconto e non solo nel nome del file: due
            // ricerche sulla stessa cella con spread costante e spread orario scelgono orari diversi
            // — misurato su FDAX 4h, che con la costante sceglie 00:00-06:00 e con le ore vere
            // 03:00-18:00 — e un resoconto che non lo dice e' un numero senza la sua ipotesi.
            $"- Spread: {DescribeSpread(options, spread, spreadByHour)}",
            $"- Swap: {(swap.Count > 0 ? string.Join(", ", swap.Select(entry => $"**{entry.Key}** long {entry.Value.LongPointsPerNight} pt/notte, short {entry.Value.ShortPointsPerNight} pt/notte, rollover {entry.Value.RolloverUtc:HH\\:mm} UTC ({options.SwapBroker})")) : "**nessuno** — tenere una posizione oltre il rollover non costa niente")}",
            $"- Commissione: ${options.Commission} per contratto e per lato, cioe' ${options.Commission * 2} per trade",
            // La tenuta cambia quali trade esistono e quanto durano: un resoconto che non la dichiara
            // e' confrontabile solo per caso con uno cercato a tenuta diversa.
            $"- Tenuta: {(options.FlatUtc is null ? "**overnight e overweek liberi** (parita' con il motore di ricerca)" : $"**flat di sessione {options.FlatUtc:HH\\:mm} UTC per {options.FlatWindowMinutes} minuti** — niente posizioni ne' ingressi nella finestra" + DescribeRolloverCoverage(options, swap))}",
            $"- Campione di ricerca: {series.StartUtc:yyyy-MM-dd} → {options.SplitUtc:yyyy-MM-dd}",
            $"- Validazione: {options.SplitUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}",
            $"- Obiettivo: {result.Optimization.Objective}",
            $"- Beam: {options.BeamWidth}" +
            (options.SplitPatternPhases
                ? " · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)"
                : string.Empty),
            $"- Durata: {elapsed.TotalMinutes:N1} minuti",
            string.Empty,
            "## Fasi",
            string.Empty,
            "| fase | combinazioni | ammissibili | minuti | migliore in campione |",
            "|---|---:|---:|---:|---|"
        };

        foreach (var phase in result.Optimization.Phases)
        {
            var best = phase.Seeds.Count > 0 ? phase.Seeds[0].Outcome.ToString() : "nessuna ammissibile";
            lines.Add($"| {phase.Phase} | {phase.Combinations:N0} | {phase.Admissible:N0} | " +
                      $"{phase.Elapsed.TotalMinutes:N1} | {best} |");
        }

        lines.Add(string.Empty);
        if (result.Optimization.PatternsDroppedByAblation.Count > 0)
        {
            lines.Add("**Ablation**: " + string.Join("; ", result.Optimization.PatternsDroppedByAblation) +
                      " — pattern che non battono la propria sentinella, quindi tolti.");
            lines.Add(string.Empty);
        }

        lines.Add("## Finaliste");
        lines.Add(string.Empty);
        lines.Add("| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |");
        lines.Add("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|");

        for (var index = 0; index < result.Validations.Count; index++)
        {
            var validation = result.Validations[index];
            lines.Add(
                $"| {index + 1} | {(validation.Passed ? "**passa**" : "scartata")} " +
                $"| {validation.InSample.Trades:N0} | {validation.InSample.NetProfit:N0} | {validation.InSampleScore:N2} " +
                $"| {validation.OutOfSample.Trades:N0} | {validation.OutOfSample.NetProfit:N0} | {validation.OutOfSampleScore:N2} " +
                $"| {validation.ScoreRetention:P0} | {validation.ProfitableWindows}/{validation.StabilityWindows.Count} |");
        }

        lines.Add(string.Empty);
        for (var index = 0; index < result.Validations.Count; index++)
        {
            var validation = result.Validations[index];
            lines.Add($"**Finalista {index + 1}** — {validation.Verdict}");
            lines.Add(string.Empty);
            lines.Add("```");
            foreach (var entry in validation.Parameters.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                lines.Add($"{entry.Key} = {entry.Value}");
            lines.Add("```");
            lines.Add(string.Empty);
            foreach (var window in validation.StabilityWindows)
                lines.Add($"- {window.FromUtc:yyyy-MM-dd} → {window.ToUtc:yyyy-MM-dd}: {window.Trades} trade, {window.NetProfit:N0}");
            lines.Add(string.Empty);
        }

        lines.Add(result.Survivor is null
            ? "## Esito: nessuna finalista sopravvive alla validazione fuori campione."
            : "## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Se il flat dichiarato copre il rollover degli swap caricati. E' la riga che distingue un flat
    /// che evita il finanziamento da uno che lo paga lo stesso.
    /// </summary>
    private static string DescribeRolloverCoverage(Options options, IReadOnlyDictionary<string, SwapSpec> swap)
    {
        if (options.FlatUtc is not { } flat || swap.Count == 0) return string.Empty;

        var holding = AccountHoldingPolicy.Default with
        {
            AllowOvernight = false, SessionFlatUtc = flat, SessionFlatWindowMinutes = options.FlatWindowMinutes
        };
        var fuori = HoldingResolver.RolloversOutsideSessionFlatWindow(holding, swap.Values);
        return fuori.Count == 0
            ? "; il rollover cade dentro la finestra"
            : $"; **ATTENZIONE**: la finestra non copre {string.Join(", ", fuori)}";
    }

    private sealed record Options
    {
        public const string Usage = """
            piootoo-sweep --strategy <Id> --symbol <@SYM> --timeframe <minuti> --split <yyyy-MM-dd>
                          [--engine PC|BIASW] [--broker <BROKER>] [--spread-broker <BROKER>]
                          [--from <yyyy-MM-dd>] [--to <yyyy-MM-dd>] [--beam N] [--top N]
                          [--min-trades N] [--commission N] [--max-combinations N] [--out <file.md>]
                          [--split-pattern-phases] [--clock <minuti, default 1>]
                          [--min-losing-trades N, default 10]  (--min-trades default 250)
                          [--flat-utc <HH:mm>] [--flat-window <minuti, default 30>]
            """;

        public required string Strategy { get; init; }
        public required string Symbol { get; init; }
        public required int Timeframe { get; init; }

        /// <summary>
        /// L'orologio con cui gira OGNI fase, in minuti. Default 1. Dal 21/09/2026 il veloce e'
        /// spento (ordinava rumore, Spearman 0,021 sul punteggio su FDAX); un orologio intermedio
        /// pero' ordina: su NQ, 15m contro 1m vale 0,980. Serve per i simboli di cui il vendor ha i
        /// 15 minuti ma non il minuto — ES, BP, EC — che altrimenti non sarebbero cercabili.
        /// Va caricato nelle serie, quindi il feed deve avere quel timeframe.
        /// </summary>
        public int ClockMinutes { get; init; } = 1;

        public string Engine { get; init; } = "PC";
        public string? Broker { get; init; }
        public string? SpreadBroker { get; init; }
        public DateTime FromUtc { get; init; } = new(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public required DateTime SplitUtc { get; init; }
        public DateTime ToUtc { get; init; } = DateTime.UtcNow.Date;
        public int BeamWidth { get; init; } = 2;
        public int TopCandidates { get; init; } = 5;
        /// <summary>
        /// Trade minimi in campione perche' una configurazione sia giudicata. Era 30 e il criterio
        /// del peggior tratto convergeva su 42-52 trade in tre anni (FDAX e NQ al minuto, 22/09):
        /// chi fa pochissimi trade non ha un tratto brutto per costruzione. 250 su tre anni a 4 ore
        /// sono meno di due a settimana. Vedi <see cref="WorstSubPeriodObjective"/>.
        /// </summary>
        public int MinTrades { get; init; } = 250;

        /// <summary>
        /// Trade in perdita minimi in campione: sotto, la coda non e' stata vista e la configurazione
        /// non e' misurata. NQ al minuto: 42 trade, zero perdite, punteggio 19.115.
        /// </summary>
        public int MinLosingTrades { get; init; } = 10;
        /// <summary>
        /// Commissione per contratto e <b>per lato</b>: il motore la addebita due volte, all'entrata
        /// e all'uscita. La scheda di un broker stampa di solito il <i>round turn</i> — su ICS/DE40
        /// sono $38,46 a trade, cioe' 19,23 qui. Sbagliarlo raddoppia il costo senza che si veda: la
        /// strategia sembra semplicemente peggiore.
        /// </summary>
        public decimal Commission { get; init; } = 4m;
        public decimal InitialCapital { get; init; } = 1_000_000m;
        public long MaxCombinationsPerPhase { get; init; } = 50_000;

        /// <summary>Vedi <see cref="SweepSpace.SplitPatternPhases"/>: si guadagna tempo, si perde l'interazione.</summary>
        public bool SplitPatternPhases { get; init; }

        /// <summary>Spread per ora UTC invece che costante. Vedi <see cref="SweepJob.SpreadPointsByHour"/>.</summary>
        public bool SpreadPerHour { get; init; }

        /// <summary>
        /// Quale numero della distribuzione misurata diventa il costo: <c>median</c> (il costo
        /// tipico, default), <c>mean</c>, oppure <c>p90</c> — il caso brutto, che serve a chiedersi
        /// quanto resta di una strategia quando entra nel momento sbagliato. Una candidata che con
        /// il p90 evapora non aveva un margine: stava dentro il costo.
        /// </summary>
        public SpreadStatistic SpreadStatistic { get; init; } = SpreadStatistic.Median;

        /// <summary>
        /// Broker di cui applicare il finanziamento overnight, da <c>piootoo-repository/swap/</c>.
        /// Vuoto = nessuno swap. Un simbolo che la tabella non misura fa fallire l'avvio.
        /// </summary>
        public string? SwapBroker { get; init; }

        /// <summary>
        /// Ora UTC del flat di sessione con cui la strategia verra' operata. Vuoto = overnight e
        /// overweek liberi, la parita' con il motore di ricerca. Valorizzato, la ricerca gira con la
        /// stessa <see cref="AccountHoldingPolicy"/> del piano che vieta l'overnight: deadline al
        /// flat e nessun ingresso nella finestra <c>[flat, flat + FlatWindowMinutes)</c>.
        /// </summary>
        public TimeOnly? FlatUtc { get; init; }

        /// <summary>Durata della finestra di flat, in minuti. Vedi <see cref="AccountHoldingPolicy.SessionFlatWindowMinutes"/>.</summary>
        public int FlatWindowMinutes { get; init; } = TradingConventions.SessionFlatWindowMinutes;

        /// <summary>
        /// Il criterio di ricerca: <c>worst-period</c> (default, giudica sul peggiore dei tratti del
        /// campione) o <c>net-over-dd</c> (il totale, che e' quello che premiava la fortuna).
        /// </summary>
        public string Objective { get; init; } = "worst-period";

        /// <summary>
        /// Profit factor minimo perche' una configurazione sia ammissibile. Sotto, non entra in
        /// classifica: un margine dell'1% lo mangia il primo costo dimenticato.
        /// </summary>
        public decimal MinProfitFactor { get; init; }

        /// <summary>
        /// Utile medio per trade minimo, in denaro. Va tarato sul <b>costo</b> per trade: su
        /// ICS/DE40 sono ~$50 fra commissione e spread, quindi 150 chiede tre volte il costo.
        /// </summary>
        public decimal MinAverageTrade { get; init; }

        /// <summary>
        /// Con <c>--params "Chiave=valore;Altra=valore"</c> non si cerca niente: si misura questa
        /// configurazione dentro e fuori campione. Vuoto = ricerca completa.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Parameters { get; init; }
        public string RepositoryPath { get; init; } = @"C:\piootoo-dev\piootoo-repository";
        public string? OutputPath { get; init; }

        /// <summary>
        /// <c>"ChannelBars=1;StopLoss=750"</c>. I valori sono interi perche' lo sono tutti i
        /// parametri dei motori portati: un valore non numerico e' quasi sempre un errore di
        /// battitura, e farlo passare come stringa lo trasformerebbe in un parametro ignorato.
        /// </summary>
        private static IReadOnlyDictionary<string, object>? ParseParameters(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                    throw new ArgumentException($"parametro malformato: '{entry}' (serve Chiave=valore)");
                if (!int.TryParse(parts[1], out var value))
                    throw new ArgumentException($"il valore di {parts[0]} non e' un intero: '{parts[1]}'");

                parameters[parts[0]] = value;
            }

            return parameters;
        }

        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < args.Length; index++)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"argomento inatteso: {args[index]}");

                var key = args[index][2..];

                // Un'opzione senza valore e' un interruttore: --split-pattern-phases da solo vale
                // "acceso". Senza questo ramo andrebbe scritto con un valore finto, che e' il genere
                // di dettaglio che si sbaglia lanciando una corsa di nove ore.
                var isSwitch = index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal);
                values[key] = isSwitch ? "true" : args[++index];
            }

            string Required(string key) => values.TryGetValue(key, out var value)
                ? value
                : throw new ArgumentException($"manca --{key}");

            DateTime Date(string key, DateTime fallback) => values.TryGetValue(key, out var value)
                ? DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc)
                : fallback;

            int Number(string key, int fallback) =>
                values.TryGetValue(key, out var value) ? int.Parse(value) : fallback;

            decimal Decimale(string key, decimal fallback) =>
                values.TryGetValue(key, out var value)
                    ? decimal.Parse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture)
                    : fallback;

            return new Options
            {
                Strategy = Required("strategy"),
                Symbol = Required("symbol"),
                Timeframe = int.Parse(Required("timeframe")),
                ClockMinutes = Number("clock", 1),
                SplitUtc = DateTime.SpecifyKind(DateTime.Parse(Required("split")), DateTimeKind.Utc),
                Engine = values.TryGetValue("engine", out var engine) ? engine.ToUpperInvariant() : "PC",
                Broker = values.GetValueOrDefault("broker"),
                SpreadBroker = values.GetValueOrDefault("spread-broker"),
                SwapBroker = values.GetValueOrDefault("swap-broker"),
                FromUtc = Date("from", new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                ToUtc = Date("to", DateTime.UtcNow.Date),
                BeamWidth = Number("beam", 2),
                TopCandidates = Number("top", 5),
                MinTrades = Number("min-trades", 250),
                MinLosingTrades = Number("min-losing-trades", 10),
                // Decimale: la meta' di un round turn raramente e' un intero.
                Commission = values.TryGetValue("commission", out var commissione)
                    ? decimal.Parse(commissione, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture)
                    : 4m,
                MaxCombinationsPerPhase = Number("max-combinations", 50_000),
                SplitPatternPhases = values.ContainsKey("split-pattern-phases"),
                SpreadPerHour = values.ContainsKey("spread-per-hour"),
                SpreadStatistic = values.TryGetValue("spread-statistic", out var statistic)
                    ? statistic.ToLowerInvariant() switch
                    {
                        "median" or "p50" => SpreadStatistic.Median,
                        "mean" or "avg" => SpreadStatistic.Mean,
                        "p90" => SpreadStatistic.P90,
                        _ => throw new ArgumentException($"statistica di spread sconosciuta: '{statistic}' (median, mean, p90)")
                    }
                    : SpreadStatistic.Median,
                Objective = values.TryGetValue("objective", out var objective)
                    ? objective.ToLowerInvariant()
                    : "worst-period",
                MinProfitFactor = Decimale("min-profit-factor", 0m),
                MinAverageTrade = Decimale("min-average-trade", 0m),
                FlatUtc = values.TryGetValue("flat-utc", out var flatUtc)
                    ? TimeOnly.TryParseExact(flatUtc, "HH:mm", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedFlat)
                        ? parsedFlat
                        : throw new ArgumentException($"--flat-utc vuole un orario HH:mm UTC, ricevuto '{flatUtc}'")
                    : null,
                FlatWindowMinutes = Number("flat-window", TradingConventions.SessionFlatWindowMinutes),
                Parameters = ParseParameters(values.GetValueOrDefault("params")),
                OutputPath = values.GetValueOrDefault("out")
            };
        }
    }
}
