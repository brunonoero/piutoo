using System.Globalization;
using Piootoo.Core.Planning;

namespace Piootoo.PlanBuilder;

/// <summary>
/// Compone piani di strategie scorrelate dai trade chiusi di uno o piu' run e scrive il resoconto.
/// La logica sta in <see cref="Core.Planning.PlanBuilder"/>; qui solo argomenti, lettura e scrittura.
///
/// <example>
/// piootoo-plan-builder --run piootoo-repository\workspaces\catalogo\backtests\run-a ^
///                      --run piootoo-repository\workspaces\pt6exo\backtests\run-b ^
///                      --plans 3 --max-strategies 8 --max-daily-loss 5000 ^
///                      --out piootoo-repository\ricerca\piani-2026-09
/// </example>
/// </summary>
public static class Program
{
    private const string Usage = """
        uso: piootoo-plan-builder --run <cartella o trades.json> [--run ...] --out <cartella>
          --plans N              piani da costruire, disgiunti (default 3)
          --max-strategies N     strategie per piano (default 8)
          --max-per-symbol N     strategie per simbolo in un piano (default 2)
          --max-per-family N     strategie per famiglia di motore in un piano (default 2)
          --max-corr X           correlazione giornaliera massima con ogni membro (default 0.3)
          --max-tail-corr X      correlazione massima nelle code (default 0.3)
          --tail X               quota dei giorni peggiori che fa la coda (default 0.10)
          --max-dd X             drawdown massimo del piano, in denaro (default nessuno)
          --max-daily-loss X     perdita massima del piano in un giorno, in denaro (default nessuna)
          --min-trades N         trade minimi per una candidata (default 30)
          --from AAAA-MM-GG      solo i trade usciti da questa data (UTC)
          --to AAAA-MM-GG        solo i trade usciti prima di questa data (UTC)
        """;

    public static int Main(string[] args)
    {
        try
        {
            var (runs, output, options, from, to) = Parse(args);

            var trades = new List<PlanTrade>();
            var owner = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var run in runs)
            {
                var read = Core.Planning.PlanBuilder.ReadRun(run);
                foreach (var code in read.Select(t => t.StrategyCode).Distinct(StringComparer.Ordinal))
                {
                    // La stessa strategia in due run sarebbe contata due volte: il P&L raddoppierebbe e
                    // la correlazione con se stessa sparirebbe dal conto. Si sceglie il run, non si somma.
                    if (owner.TryGetValue(code, out var previous))
                        throw new ArgumentException($"{code} compare sia in '{previous}' sia in '{run}': scegli un run solo per strategia.");
                    owner[code] = run;
                }

                var kept = read.Where(t => (from is null || t.ExitUtc >= from) && (to is null || t.ExitUtc < to)).ToList();
                Console.WriteLine($"[plan-builder] {run}: {read.Count} trade, {kept.Count} nel periodo, {owner.Count(o => o.Value == run)} strategie");
                trades.AddRange(kept);
            }

            var result = Core.Planning.PlanBuilder.Build(trades, options);
            PlanBuilderReport.WriteAll(result, options, runs, output);

            Console.WriteLine($"[plan-builder] {result.Candidates.Count} candidate, {result.Excluded.Count} escluse, {result.Plans.Count} piani");
            foreach (var plan in result.Plans)
                Console.WriteLine($"[plan-builder] piano {plan.Number}: {plan.Members.Count} strategie, netto {plan.Net:N0}, DD {plan.MaxDrawdown:N0}, giorno peggiore {plan.WorstDay:N0}");
            Console.WriteLine($"[plan-builder] resoconto in {Path.Combine(output, "plan-builder.md")}");
            return 0;
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine(error.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Usage);
            return 2;
        }
        catch (FileNotFoundException error)
        {
            Console.Error.WriteLine(error.Message);
            return 3;
        }
    }

    private static (List<string> Runs, string Output, PlanBuilderOptions Options, DateTime? From, DateTime? To) Parse(string[] args)
    {
        var runs = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
                throw new ArgumentException($"argomento inatteso o senza valore: {args[index]}");

            var key = args[index][2..];
            var value = args[++index];
            if (string.Equals(key, "run", StringComparison.OrdinalIgnoreCase))
                runs.Add(value);
            else
                values[key] = value;
        }

        if (runs.Count == 0) throw new ArgumentException("manca --run");
        if (!values.TryGetValue("out", out var output)) throw new ArgumentException("manca --out");

        int Number(string key, int fallback) =>
            values.TryGetValue(key, out var value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
        double Real(string key, double fallback) =>
            values.TryGetValue(key, out var value) ? double.Parse(value, CultureInfo.InvariantCulture) : fallback;
        decimal? Money(string key) =>
            values.TryGetValue(key, out var value) ? decimal.Parse(value, CultureInfo.InvariantCulture) : null;
        DateTime? Date(string key) =>
            values.TryGetValue(key, out var value)
                ? DateTime.SpecifyKind(DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Utc)
                : null;

        var options = new PlanBuilderOptions
        {
            Plans = Number("plans", 3),
            MaxStrategiesPerPlan = Number("max-strategies", 8),
            MaxPerSymbol = Number("max-per-symbol", 2),
            MaxPerFamily = Number("max-per-family", 2),
            MaxCorrelation = Real("max-corr", 0.3),
            MaxTailCorrelation = Real("max-tail-corr", 0.3),
            TailQuantile = Real("tail", 0.10),
            MaxPlanDrawdown = Money("max-dd"),
            MaxDailyLoss = Money("max-daily-loss"),
            MinTrades = Number("min-trades", 30)
        };

        return (runs, output, options, Date("from"), Date("to"));
    }
}
