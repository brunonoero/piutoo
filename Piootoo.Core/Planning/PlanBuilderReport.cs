using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Piootoo.Core.Planning;

/// <summary>
/// Gli artefatti del costruttore di piani: il resoconto da leggere (<c>plan-builder.md</c>), la matrice
/// delle correlazioni (<c>correlazioni.csv</c>, <c>correlazioni-code.csv</c>) e i piani in forma
/// riusabile (<c>plans.json</c>), da cui si compone il <c>TradingPlan</c> di ogni conto.
/// </summary>
public static class PlanBuilderReport
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    public static void WriteAll(PlanBuilderResult result, PlanBuilderOptions options, IReadOnlyList<string> sources, string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "plan-builder.md"), Markdown(result, options, sources), Encoding.UTF8);
        File.WriteAllText(Path.Combine(directory, "correlazioni.csv"), MatrixCsv(result.Candidates, result.Correlation), Encoding.UTF8);
        File.WriteAllText(Path.Combine(directory, "correlazioni-code.csv"), MatrixCsv(result.Candidates, result.TailCorrelation), Encoding.UTF8);
        File.WriteAllText(Path.Combine(directory, "plans.json"), PlansJson(result), Encoding.UTF8);
    }

    public static string Markdown(PlanBuilderResult result, PlanBuilderOptions options, IReadOnlyList<string> sources)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Piani di strategie scorrelate");
        sb.AppendLine();
        sb.AppendLine($"Run letti: {string.Join(", ", sources.Select(s => $"`{s}`"))}.");
        if (result.Days.Count > 0)
            sb.AppendLine($"Asse dei giorni: {result.Days.Count} giorni con almeno un trade, dal {result.Days[0]:yyyy-MM-dd} al {result.Days[^1]:yyyy-MM-dd} (UTC, giorno di uscita).");
        sb.AppendLine();
        sb.AppendLine(
            $"Vincoli: fino a {options.Plans} piani disgiunti da {options.MaxStrategiesPerPlan} strategie; al massimo " +
            $"{options.MaxPerSymbol} per simbolo e {options.MaxPerFamily} per famiglia; correlazione <= {F(options.MaxCorrelation)}, " +
            $"nelle code (peggior {options.TailQuantile:P0} dei giorni) <= {F(options.MaxTailCorrelation)}; " +
            $"drawdown del piano {(options.MaxPlanDrawdown is { } dd ? M(dd) : "libero")}, perdita giornaliera " +
            $"{(options.MaxDailyLoss is { } loss ? M(loss) : "libera")}; candidate da {options.MinTrades} trade in su.");
        sb.AppendLine();
        sb.AppendLine("Le candidate arrivano gia' giudicate dal metodo: qui si misura solo come stanno insieme. I run devono avere le stesse size.");
        sb.AppendLine();

        sb.AppendLine("## Piani");
        sb.AppendLine();
        if (result.Plans.Count == 0)
            sb.AppendLine("Nessun piano: nessuna candidata rispetta da sola i vincoli del conto.");

        foreach (var plan in result.Plans)
        {
            sb.AppendLine($"### Piano {plan.Number}");
            sb.AppendLine();
            sb.AppendLine(
                $"{plan.Members.Count} strategie · netto {M(plan.Net)} · drawdown giornaliero {M(plan.MaxDrawdown)} · " +
                $"netto/DD {F((double)(plan.MaxDrawdown > 0 ? plan.Net / plan.MaxDrawdown : 0))} · giorno peggiore {M(plan.WorstDay)} · " +
                $"correlazione massima fra membri {F(plan.MaxPairCorrelation)}, nelle code {F(plan.MaxPairTailCorrelation)}");
            sb.AppendLine();
            sb.AppendLine("| strategia | simbolo | famiglia | trade | netto | DD | netto/DD | giorno peggiore |");
            sb.AppendLine("|---|---|---|---:|---:|---:|---:|---:|");
            foreach (var member in plan.Members)
            {
                sb.AppendLine(
                    $"| `{member.Code}` | {member.Symbol} | {member.Family} | {member.Trades} | {M(member.Net)} | {M(member.MaxDrawdown)} | " +
                    $"{F((double)(member.MaxDrawdown > 0 ? member.Net / member.MaxDrawdown : 0))} | {M(member.WorstDay)} |");
            }

            sb.AppendLine();
        }

        if (result.Plans.Count > 1)
        {
            sb.AppendLine("## Correlazione fra i piani");
            sb.AppendLine();
            sb.AppendLine("|  | " + string.Join(" | ", result.Plans.Select(p => $"piano {p.Number}")) + " |");
            sb.AppendLine("|---|" + string.Concat(result.Plans.Select(_ => "---:|")));
            for (var i = 0; i < result.Plans.Count; i++)
            {
                sb.AppendLine($"| piano {result.Plans[i].Number} | " +
                              string.Join(" | ", Enumerable.Range(0, result.Plans.Count).Select(j => F(result.PlanCorrelation[i, j]))) + " |");
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Coppie piu' correlate fra le candidate");
        sb.AppendLine();
        sb.AppendLine("Le prime venti per correlazione nelle code: sono le strategie che perdono insieme nei giorni peggiori. Matrici complete in `correlazioni.csv` e `correlazioni-code.csv`.");
        sb.AppendLine();
        sb.AppendLine("| strategia | strategia | correlazione | nelle code |");
        sb.AppendLine("|---|---|---:|---:|");
        var pairs = new List<(string A, string B, double C, double T)>();
        for (var i = 0; i < result.Candidates.Count; i++)
        {
            for (var j = i + 1; j < result.Candidates.Count; j++)
                pairs.Add((result.Candidates[i].Code, result.Candidates[j].Code, result.Correlation[i, j], result.TailCorrelation[i, j]));
        }

        foreach (var pair in pairs.OrderByDescending(p => double.IsNaN(p.T) ? double.MinValue : p.T).Take(20))
            sb.AppendLine($"| `{pair.A}` | `{pair.B}` | {F(pair.C)} | {F(pair.T)} |");
        sb.AppendLine();

        if (result.Unassigned.Count > 0)
        {
            sb.AppendLine("## Candidate rimaste fuori dai piani");
            sb.AppendLine();
            sb.AppendLine(string.Join(", ", result.Unassigned.Select(c => $"`{c}`")) + ".");
            sb.AppendLine();
        }

        if (result.Excluded.Count > 0)
        {
            sb.AppendLine("## Strategie escluse dalle candidate");
            sb.AppendLine();
            foreach (var excluded in result.Excluded)
                sb.AppendLine($"- `{excluded.Code}`: {excluded.Reason}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string MatrixCsv(IReadOnlyList<StrategyProfile> candidates, double[,] matrix)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strategia;" + string.Join(';', candidates.Select(c => c.Code)));
        for (var i = 0; i < candidates.Count; i++)
        {
            sb.Append(candidates[i].Code);
            for (var j = 0; j < candidates.Count; j++)
                sb.Append(';').Append(double.IsNaN(matrix[i, j]) ? string.Empty : matrix[i, j].ToString("0.000", CultureInfo.InvariantCulture));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string PlansJson(PlanBuilderResult result) =>
        JsonSerializer.Serialize(
            result.Plans.Select(plan => new
            {
                plan = plan.Number,
                strategies = plan.Members.Select(m => m.Code).ToArray(),
                net = plan.Net,
                maxDrawdown = plan.MaxDrawdown,
                worstDay = plan.WorstDay,
                maxPairCorrelation = double.IsNaN(plan.MaxPairCorrelation) ? (double?)null : plan.MaxPairCorrelation,
                maxPairTailCorrelation = double.IsNaN(plan.MaxPairTailCorrelation) ? (double?)null : plan.MaxPairTailCorrelation
            }),
            new JsonSerializerOptions { WriteIndented = true });

    private static string M(decimal value) => value.ToString("N0", It);

    private static string F(double value) => double.IsNaN(value) ? "n/d" : value.ToString("0.00", It);
}
