using System.Globalization;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>Quanto si somigliano, sullo stesso simbolo, strategie di motori diversi?</b>
///
/// <para>Una misura, non un test: gira tutte le strategie <c>@NQ</c> del catalogo con l'orologio al
/// minuto sul feed del vendor, ricava il P&amp;L giornaliero di ciascuna e calcola la matrice di
/// correlazione di Pearson fra le serie, poi la media dentro e fra le famiglie di motori. Serve a
/// decidere quali motori vale la pena portare in sweep per primi: un motore che entra nella stessa
/// direzione dello stesso movimento (i trend following e i breakout fra loro) diversifica poco; un
/// reversal, che entra contro, diversifica molto. L'aspettativa e' quella, ma e' una regola del
/// pollice: qui diventa un numero.</para>
///
/// <para>Il P&amp;L giornaliero e' la somma dei netti dei trade <i>chiusi</i> in quel giorno UTC;
/// nei giorni senza chiusure vale zero. E' la convenzione piu' semplice e sottostima leggermente
/// la correlazione delle posizioni aperte insieme, che qui non interessa: interessa se perdono
/// negli stessi giorni.</para>
///
/// <para>Scrive la matrice in <c>piootoo-repository/ricerca/nq-correlazione-motori.csv</c> e il
/// riassunto per famiglia nell'output. Legge il feed vero: senza, salta.</para>
/// </summary>
public sealed class NqEngineCorrelationTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private static readonly DateTime StartUtc = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task DailyPnlCorrelationAcrossEnginesOnNq()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var nq = StrategyFactory.GetRegisteredStrategies()
            .Where(d => string.Equals(d.Symbol?.Trim().TrimStart('@'), "NQ", StringComparison.OrdinalIgnoreCase))
            .Where(d => d.TimeframeMinutes > 0)
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(nq);

        var timeframes = nq.Select(d => d.TimeframeMinutes).Append(1).Distinct().Order().ToArray();
        var series = await LoadAsync(timeframes);
        if (series is null)
        {
            output.WriteLine("feed assente: saltato.");
            return;
        }

        output.WriteLine($"{nq.Count} strategie @NQ, timeframe {string.Join("/", timeframes)}, {StartUtc:yyyy-MM-dd} → {EndUtc:yyyy-MM-dd}, orologio al minuto\n");

        var runner = new SweepRunner(series);
        var daily = new Dictionary<string, Dictionary<DateOnly, decimal>>(StringComparer.Ordinal);
        var summary = new List<string>();

        foreach (var definition in nq)
        {
            var job = new SweepJob(definition.Id)
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 4m,
                ClockTimeframeMinutes = 1,
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
            };

            SweepOutcome outcome;
            try
            {
                outcome = runner.Run(job);
            }
            catch (Exception error)
            {
                output.WriteLine($"{definition.Id,-24} SALTATA: {error.Message}");
                continue;
            }

            var byDay = outcome.ClosedTrades
                .GroupBy(t => DateOnly.FromDateTime(t.ExitDate))
                .ToDictionary(g => g.Key, g => g.Sum(t => t.NetProfit));
            daily[definition.Id] = byDay;

            summary.Add($"{definition.Id,-24} {outcome.Trades,5} trade  netto {outcome.NetProfit,11:N0}  giorni con chiusure {byDay.Count,4}");
            output.WriteLine(summary[^1]);
        }

        // Solo strategie con abbastanza giorni: con dieci chiusure in quattro anni la correlazione
        // e' rumore qualunque sia.
        var ids = daily.Where(kv => kv.Value.Count >= 40).Select(kv => kv.Key).ToList();
        output.WriteLine($"\n{ids.Count} strategie con almeno 40 giorni di chiusure entrano nella matrice.");
        if (ids.Count < 2) return;

        var days = daily.Values.SelectMany(d => d.Keys).Distinct().Order().ToList();
        var vectors = ids.ToDictionary(
            id => id,
            id => days.Select(day => (double)daily[id].GetValueOrDefault(day, 0m)).ToArray(),
            StringComparer.Ordinal);

        var matrix = new double[ids.Count, ids.Count];
        for (var i = 0; i < ids.Count; i++)
        for (var j = 0; j < ids.Count; j++)
            matrix[i, j] = Pearson(vectors[ids[i]], vectors[ids[j]]);

        WriteCsv(ids, matrix);

        // Famiglie: il terzo token del nome (PTS_NQ_TFM_001_60 → TFM).
        static string Family(string id) => id.Split('_').ElementAtOrDefault(2) ?? "?";
        var families = ids.Select(Family).Distinct().Order().ToList();

        output.WriteLine("\nCorrelazione media del P&L giornaliero, per coppia di famiglie (diagonale = dentro la famiglia):");
        output.WriteLine("        " + string.Join("", families.Select(f => $"{f,8}")));
        foreach (var a in families)
        {
            var row = new StringBuilder($"{a,8}");
            foreach (var b in families)
            {
                var values = new List<double>();
                for (var i = 0; i < ids.Count; i++)
                for (var j = 0; j < ids.Count; j++)
                {
                    if (i == j || Family(ids[i]) != a || Family(ids[j]) != b) continue;
                    values.Add(matrix[i, j]);
                }
                row.Append(values.Count == 0 ? $"{"n/d",8}" : $"{values.Average(),8:N2}");
            }
            output.WriteLine(row.ToString());
        }

        var offDiagonal = new List<double>();
        for (var i = 0; i < ids.Count; i++)
        for (var j = i + 1; j < ids.Count; j++)
            offDiagonal.Add(matrix[i, j]);
        output.WriteLine($"\ncorrelazione media fra tutte le coppie: {offDiagonal.Average():N3}   " +
                         $"massima: {offDiagonal.Max():N3}   minima: {offDiagonal.Min():N3}");

        var top = new List<(string A, string B, double R)>();
        for (var i = 0; i < ids.Count; i++)
        for (var j = i + 1; j < ids.Count; j++)
            top.Add((ids[i], ids[j], matrix[i, j]));
        output.WriteLine("\nle 8 coppie piu' correlate:");
        foreach (var (a, b, r) in top.OrderByDescending(t => t.R).Take(8))
            output.WriteLine($"  {r,6:N2}  {a}  ~  {b}");
        output.WriteLine("\nle 8 coppie meno correlate:");
        foreach (var (a, b, r) in top.OrderBy(t => t.R).Take(8))
            output.WriteLine($"  {r,6:N2}  {a}  ~  {b}");
    }

    private static void WriteCsv(List<string> ids, double[,] matrix)
    {
        var path = Path.Combine(RepositoryPath, "ricerca", "nq-correlazione-motori.csv");
        var sb = new StringBuilder();
        sb.AppendLine("# Correlazione di Pearson del P&L giornaliero (netto dei trade chiusi nel giorno UTC, 0 nei giorni senza chiusure).");
        sb.AppendLine($"# Strategie @NQ del catalogo, feed del vendor, orologio al minuto, {StartUtc:yyyy-MM-dd} -> {EndUtc:yyyy-MM-dd}. Generato da NqEngineCorrelationTests.");
        sb.AppendLine("strategia;" + string.Join(";", ids));
        for (var i = 0; i < ids.Count; i++)
        {
            sb.Append(ids[i]);
            for (var j = 0; j < ids.Count; j++)
                sb.Append(';').Append(matrix[i, j].ToString("0.000", CultureInfo.InvariantCulture));
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static double Pearson(double[] a, double[] b)
    {
        var ma = a.Average();
        var mb = b.Average();
        double num = 0, da = 0, db = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var x = a[i] - ma;
            var y = b[i] - mb;
            num += x * y;
            da += x * x;
            db += y * y;
        }
        return da == 0 || db == 0 ? 0 : num / Math.Sqrt(da * db);
    }

    private static async Task<SweepSeries?> LoadAsync(int[] timeframes)
    {
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed")))
            return null;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        };
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        return await SweepSeries.LoadAsync(dataFeed, "@NQ", timeframes, StartUtc, EndUtc, warmupDays: 30d);
    }
}
