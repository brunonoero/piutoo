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
        if (!string.IsNullOrWhiteSpace(options.SpreadBroker))
        {
            var table = SpreadTable.Load(settings.GetSpreadPath(), options.SpreadBroker, SpreadStatistic.Median);
            Console.WriteLine($"[sweep] spread: {table.Describe()}");
            foreach (var warning in table.Warnings)
                Console.WriteLine($"[sweep] spread, attenzione: {warning}");

            var key = StrategyKeys.NormalizeSymbol(options.Symbol);
            if (!table.Points.TryGetValue(key, out var points))
            {
                Console.Error.WriteLine(
                    $"La tabella spread di {options.SpreadBroker} non misura {key}. Un costo mancante " +
                    "non si sostituisce con zero: la ricerca girerebbe senza il costo che poi paghera'.");
                return 3;
            }

            spread[key] = points;
            Console.WriteLine($"[sweep] spread {key}: {points} punti (mediana)");
        }

        Console.WriteLine($"[sweep] carico {options.Symbol} {options.Timeframe}m" +
                          (options.Broker is null ? " dal feed interno" : $" da {options.Broker}") +
                          $", {options.FromUtc:yyyy-MM-dd} → {options.ToUtc:yyyy-MM-dd}, piu' il minuto per le fasi di rischio...");

        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var series = await SweepSeries.LoadAsync(
            dataFeed, options.Symbol, [options.Timeframe, 1], options.FromUtc, options.ToUtc, options.Broker);

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

        foreach (var phase in space.Phases)
        {
            Console.WriteLine($"[sweep] fase {phase.Name}: {space.CombinationCount(phase):N0} combinazioni" +
                              (phase.RequiresAccurateClock ? " (orologio al minuto)" : string.Empty));
        }

        var template = new SweepJob(options.Strategy)
        {
            InitialCapital = options.InitialCapital,
            CommissionPerContract = options.Commission,
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true },
            SpreadPoints = spread.Count > 0 ? spread : null
        };

        var result = SweepSearch.Run(
            series,
            options.SplitUtc,
            space,
            template,
            new NetOverDrawdownObjective(options.MinTrades),
            new SweepOptimizerOptions
            {
                BeamWidth = options.BeamWidth,
                MaxCombinationsPerPhase = options.MaxCombinationsPerPhase,
                Verbose = true
            },
            new SweepValidationOptions(),
            options.TopCandidates);

        started.Stop();
        var report = Report(options, series, result, spread, started.Elapsed);
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

    private static string Report(
        Options options,
        SweepSeries series,
        SweepSearchResult result,
        IReadOnlyDictionary<string, decimal> spread,
        TimeSpan elapsed)
    {
        var lines = new List<string>
        {
            $"# Sweep {options.Symbol} {options.Timeframe}m — {options.Engine}",
            string.Empty,
            $"- Strategia di partenza: `{options.Strategy}`",
            $"- Datafeed: {options.Broker ?? "interno (vendor)"}",
            $"- Spread: {(spread.Count > 0 ? $"{options.SpreadBroker}, mediana, {string.Join(", ", spread.Select(entry => $"{entry.Key} {entry.Value} pt"))}" : "nessuno")}",
            $"- Commissione: ${options.Commission} per contratto e per lato",
            $"- Campione di ricerca: {series.StartUtc:yyyy-MM-dd} → {options.SplitUtc:yyyy-MM-dd}",
            $"- Validazione: {options.SplitUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}",
            $"- Obiettivo: {result.Optimization.Objective}",
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

    private sealed record Options
    {
        public const string Usage = """
            piootoo-sweep --strategy <Id> --symbol <@SYM> --timeframe <minuti> --split <yyyy-MM-dd>
                          [--engine PC|BIASW] [--broker <BROKER>] [--spread-broker <BROKER>]
                          [--from <yyyy-MM-dd>] [--to <yyyy-MM-dd>] [--beam N] [--top N]
                          [--min-trades N] [--commission N] [--max-combinations N] [--out <file.md>]
            """;

        public required string Strategy { get; init; }
        public required string Symbol { get; init; }
        public required int Timeframe { get; init; }
        public string Engine { get; init; } = "PC";
        public string? Broker { get; init; }
        public string? SpreadBroker { get; init; }
        public DateTime FromUtc { get; init; } = new(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public required DateTime SplitUtc { get; init; }
        public DateTime ToUtc { get; init; } = DateTime.UtcNow.Date;
        public int BeamWidth { get; init; } = 2;
        public int TopCandidates { get; init; } = 5;
        public int MinTrades { get; init; } = 30;
        public decimal Commission { get; init; } = 4m;
        public decimal InitialCapital { get; init; } = 1_000_000m;
        public long MaxCombinationsPerPhase { get; init; } = 50_000;
        public string RepositoryPath { get; init; } = @"C:\piootoo-dev\piootoo-repository";
        public string? OutputPath { get; init; }

        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < args.Length; index++)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"argomento inatteso: {args[index]}");
                if (index + 1 >= args.Length)
                    throw new ArgumentException($"manca il valore di {args[index]}");

                values[args[index][2..]] = args[++index];
            }

            string Required(string key) => values.TryGetValue(key, out var value)
                ? value
                : throw new ArgumentException($"manca --{key}");

            DateTime Date(string key, DateTime fallback) => values.TryGetValue(key, out var value)
                ? DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc)
                : fallback;

            int Number(string key, int fallback) =>
                values.TryGetValue(key, out var value) ? int.Parse(value) : fallback;

            return new Options
            {
                Strategy = Required("strategy"),
                Symbol = Required("symbol"),
                Timeframe = int.Parse(Required("timeframe")),
                SplitUtc = DateTime.SpecifyKind(DateTime.Parse(Required("split")), DateTimeKind.Utc),
                Engine = values.TryGetValue("engine", out var engine) ? engine.ToUpperInvariant() : "PC",
                Broker = values.GetValueOrDefault("broker"),
                SpreadBroker = values.GetValueOrDefault("spread-broker"),
                FromUtc = Date("from", new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                ToUtc = Date("to", DateTime.UtcNow.Date),
                BeamWidth = Number("beam", 2),
                TopCandidates = Number("top", 5),
                MinTrades = Number("min-trades", 30),
                Commission = Number("commission", 4),
                MaxCombinationsPerPhase = Number("max-combinations", 50_000),
                OutputPath = values.GetValueOrDefault("out")
            };
        }
    }
}
