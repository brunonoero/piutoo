using Piootoo.Core.Services;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Planning;

/// <summary>
/// Un trade chiuso ridotto a cio' che serve per comporre i piani: chi, su cosa, quando e' uscito,
/// quanto ha fatto. Si costruisce dagli artefatti di un run (<see cref="PersistedTrade"/>) o dai trade in
/// memoria di una sweep (<see cref="TradingResult"/>).
/// </summary>
public sealed record PlanTrade(string StrategyCode, string Symbol, DateTime ExitUtc, decimal NetProfit)
{
    public static PlanTrade From(PersistedTrade trade) =>
        new(trade.StrategyCode, trade.Symbol, trade.ExitTimeUtc, trade.NetProfit);

    public static PlanTrade From(TradingResult trade) =>
        new(trade.StrategyCode, trade.Symbol, trade.ExitDate, trade.NetProfit);
}

/// <summary>I vincoli con cui si compongono i piani. I default sono un punto di partenza, non una soglia del metodo.</summary>
public sealed record PlanBuilderOptions
{
    /// <summary>Quanti piani costruire. Sono <b>disgiunti</b>: una strategia sta in un piano solo.</summary>
    public int Plans { get; init; } = 3;

    /// <summary>Strategie al massimo per piano.</summary>
    public int MaxStrategiesPerPlan { get; init; } = 8;

    /// <summary>Strategie al massimo per simbolo dentro un piano.</summary>
    public int MaxPerSymbol { get; init; } = 2;

    /// <summary>Strategie al massimo per famiglia di motore (la sigla del nome) dentro un piano.</summary>
    public int MaxPerFamily { get; init; } = 2;

    /// <summary>Correlazione giornaliera massima con ciascuna strategia gia' nel piano. Le negative passano sempre.</summary>
    public double MaxCorrelation { get; init; } = 0.3;

    /// <summary>Correlazione massima nelle code (vedi <see cref="PlanBuilder.TailCorrelation"/>).</summary>
    public double MaxTailCorrelation { get; init; } = 0.3;

    /// <summary>Quota dei giorni peggiori di ciascuna strategia che fa la coda. 0,10 = il peggior decimo.</summary>
    public double TailQuantile { get; init; } = 0.10;

    /// <summary>Drawdown massimo del piano sull'equity giornaliera, in denaro. Null = nessun vincolo.</summary>
    public decimal? MaxPlanDrawdown { get; init; }

    /// <summary>Perdita massima del piano in un giorno, in denaro: la regola giornaliera del conto. Null = nessun vincolo.</summary>
    public decimal? MaxDailyLoss { get; init; }

    /// <summary>Trade minimi perche' una strategia entri fra le candidate: sotto, la correlazione e' rumore.</summary>
    public int MinTrades { get; init; } = 30;
}

/// <summary>Una strategia candidata, sull'asse dei giorni comune a tutte.</summary>
public sealed record StrategyProfile(
    string Code, string Symbol, string Family, int Trades, decimal Net, decimal MaxDrawdown, decimal WorstDay, decimal[] Daily)
{
    /// <summary>Netto su drawdown giornaliero: il criterio con cui si sceglie il seme di un piano.</summary>
    public decimal Score => MaxDrawdown > 0m ? Net / MaxDrawdown : Net > 0m ? decimal.MaxValue : 0m;
}

/// <summary>Un piano costruito: le sue strategie e le metriche della loro somma giornaliera.</summary>
public sealed record BuiltPlan(
    int Number,
    IReadOnlyList<StrategyProfile> Members,
    decimal Net,
    decimal MaxDrawdown,
    decimal WorstDay,
    decimal[] Daily,
    double MaxPairCorrelation,
    double MaxPairTailCorrelation);

/// <summary>Una strategia che non e' entrata fra le candidate, con il motivo.</summary>
public sealed record ExcludedStrategy(string Code, string Reason);

/// <summary>Tutto cio' che il resoconto mostra.</summary>
public sealed record PlanBuilderResult(
    IReadOnlyList<DateTime> Days,
    IReadOnlyList<StrategyProfile> Candidates,
    double[,] Correlation,
    double[,] TailCorrelation,
    IReadOnlyList<BuiltPlan> Plans,
    double[,] PlanCorrelation,
    IReadOnlyList<ExcludedStrategy> Excluded,
    IReadOnlyList<string> Unassigned);

/// <summary>
/// <b>Compone piani di strategie scorrelate</b>, uno per conto, a partire dai trade chiusi dei loro run.
/// E' il passo che chiude la serie PT6EXO: famiglie diverse non bastano, la scorrelazione <b>si
/// misura</b>. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §"Dalle strategie ai piani".
///
/// <para><b>Il P&amp;L giornaliero.</b> Ogni trade conta nel giorno UTC della sua uscita. L'asse dei giorni
/// e' l'insieme dei giorni in cui <b>almeno una</b> candidata ha chiuso un trade: i fine settimana e i
/// festivi in cui nessuno opera non entrano, perche' una fila di zeri comuni a tutte gonfierebbe ogni
/// correlazione verso quella di due serie ferme. Una strategia che quel giorno non ha chiuso nulla vale
/// zero, ed e' giusto: un piano, quel giorno, da lei non riceve niente.</para>
///
/// <para><b>Due correlazioni.</b> Quella di Pearson sull'intero asse, e quella <b>nelle code</b>: la stessa
/// misura sui soli giorni che sono fra i peggiori dell'una o dell'altra. Strategie scorrelate in media
/// perdono spesso insieme nei giorni di crash, ed e' li' che un conto salta; la seconda lo vede, la prima
/// no. Una correlazione negativa non e' mai un problema: e' cio' che si cerca.</para>
///
/// <para><b>La costruzione e' golosa.</b> Il seme di ogni piano e' la candidata rimasta con il miglior
/// netto su drawdown. Poi, finche' il piano ha posto, si aggiunge la candidata che porta il piano al miglior
/// netto su drawdown fra quelle che rispettano tutti i vincoli: correlazione e correlazione nelle code
/// con ciascun membro sotto soglia, tetti per simbolo e per famiglia, drawdown e perdita giornaliera del
/// piano. I piani sono disgiunti: il secondo si costruisce con cio' che il primo ha lasciato, cosi' ogni
/// conto segue regole diverse e una famiglia che smette di funzionare ne tocca uno, non tutti.</para>
///
/// <para><b>Cosa non fa.</b> Non giudica le strategie: le candidate arrivano gia' passate dal metodo
/// (<c>lettura-risultati</c>). Non ridimensiona: somma il denaro dei trade come lo trova, quindi i run da
/// cui vengono devono avere le stesse size — un run neutro, <c>BalanceScale</c> a 1.</para>
/// </summary>
public static class PlanBuilder
{
    /// <summary>
    /// Legge i trade chiusi di un run: una cartella di backtest o di sessione, oppure il suo
    /// <c>trades.json</c>. Passa da <see cref="TradingJsonStore"/>, che fonde un journal rimasto aperto
    /// invece di leggere un array a meta'.
    /// </summary>
    public static IReadOnlyList<PlanTrade> ReadRun(string path)
    {
        var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is null || !File.Exists(Path.Combine(directory, TradingPersistenceSchema.TradesFileName)))
            throw new FileNotFoundException($"Nessun {TradingPersistenceSchema.TradesFileName} in '{path}'.");

        return new TradingJsonStore(directory).ReadTrades().Select(PlanTrade.From).ToList();
    }

    /// <summary>
    /// La famiglia di una strategia: la sigla del motore nel nome di catalogo
    /// (<c>PT6EXO_NQ_FBO_001_60</c> → <c>FBO</c>). Un nome fuori convenzione e' una famiglia a se'.
    /// </summary>
    public static string Family(string code)
    {
        var parts = code.Split('_');
        return parts.Length >= 5 && parts[2].Length == 3 ? parts[2] : code;
    }

    public static PlanBuilderResult Build(IEnumerable<PlanTrade> trades, PlanBuilderOptions options)
    {
        if (options.Plans < 1 || options.MaxStrategiesPerPlan < 1 || options.MaxPerSymbol < 1 || options.MaxPerFamily < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "piani, strategie per piano e tetti devono valere almeno 1.");
        if (options.TailQuantile is <= 0 or > 0.5)
            throw new ArgumentOutOfRangeException(nameof(options), "la coda e' una quota fra 0 e 0,5 dei giorni.");

        var all = trades.ToList();
        foreach (var trade in all)
        {
            // Come i contratti delle sessioni: un istante che non si dichiara UTC si rifiuta, non si
            // "aggiusta". Il giorno del trade dipende da questo.
            if (trade.ExitUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"{trade.StrategyCode}: uscita {trade.ExitUtc:o} con Kind {trade.ExitUtc.Kind}; i trade sono in UTC.");
        }

        var byStrategy = all.GroupBy(t => t.StrategyCode, StringComparer.Ordinal).ToList();
        var excluded = new List<ExcludedStrategy>();
        var eligible = new List<IGrouping<string, PlanTrade>>();
        foreach (var group in byStrategy)
        {
            var count = group.Count();
            var net = group.Sum(t => t.NetProfit);
            if (count < options.MinTrades)
                excluded.Add(new ExcludedStrategy(group.Key, $"{count} trade, ne servono {options.MinTrades}"));
            else if (net <= 0m)
                excluded.Add(new ExcludedStrategy(group.Key, $"netto {net:N0}: non e' in utile"));
            else
                eligible.Add(group);
        }

        var days = eligible.SelectMany(g => g).Select(t => t.ExitUtc.Date).Distinct().OrderBy(d => d).ToList();
        var dayIndex = new Dictionary<DateTime, int>(days.Count);
        for (var index = 0; index < days.Count; index++)
            dayIndex[days[index]] = index;

        var candidates = new List<StrategyProfile>(eligible.Count);
        foreach (var group in eligible)
        {
            var daily = new decimal[days.Count];
            foreach (var trade in group)
                daily[dayIndex[trade.ExitUtc.Date]] += trade.NetProfit;

            var (drawdown, worst) = DrawdownAndWorstDay(daily);
            candidates.Add(new StrategyProfile(
                group.Key, group.First().Symbol, Family(group.Key), group.Count(), daily.Sum(), drawdown, worst, daily));
        }

        candidates = candidates.OrderByDescending(c => c.Score).ThenBy(c => c.Code, StringComparer.Ordinal).ToList();

        var n = candidates.Count;
        var correlation = new double[n, n];
        var tail = new double[n, n];
        for (var i = 0; i < n; i++)
        {
            correlation[i, i] = 1;
            tail[i, i] = 1;
            for (var j = i + 1; j < n; j++)
            {
                correlation[i, j] = correlation[j, i] = Correlation(candidates[i].Daily, candidates[j].Daily);
                tail[i, j] = tail[j, i] = TailCorrelation(candidates[i].Daily, candidates[j].Daily, options.TailQuantile);
            }
        }

        var plans = BuildPlans(candidates, correlation, tail, options);

        var planCorrelation = new double[plans.Count, plans.Count];
        for (var i = 0; i < plans.Count; i++)
        {
            for (var j = 0; j < plans.Count; j++)
                planCorrelation[i, j] = i == j ? 1 : Correlation(plans[i].Daily, plans[j].Daily);
        }

        var assigned = plans.SelectMany(p => p.Members).Select(m => m.Code).ToHashSet(StringComparer.Ordinal);
        var unassigned = candidates.Where(c => !assigned.Contains(c.Code)).Select(c => c.Code).ToList();

        return new PlanBuilderResult(days, candidates, correlation, tail, plans, planCorrelation, excluded, unassigned);
    }

    private static List<BuiltPlan> BuildPlans(
        List<StrategyProfile> candidates, double[,] correlation, double[,] tail, PlanBuilderOptions options)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < candidates.Count; i++)
            index[candidates[i].Code] = i;

        var remaining = new List<StrategyProfile>(candidates);
        var plans = new List<BuiltPlan>();

        while (plans.Count < options.Plans && remaining.Count > 0)
        {
            // Il seme: la migliore rimasta che da sola rispetta i vincoli del conto.
            var seed = remaining.FirstOrDefault(c => FitsAccount(c.Daily, options));
            if (seed is null)
                break;

            var members = new List<StrategyProfile> { seed };
            var daily = (decimal[])seed.Daily.Clone();

            while (members.Count < options.MaxStrategiesPerPlan)
            {
                StrategyProfile? best = null;
                decimal[]? bestDaily = null;
                var bestRatio = decimal.MinValue;

                foreach (var candidate in remaining)
                {
                    if (members.Contains(candidate) || !FitsCaps(members, candidate, options))
                        continue;

                    var tooCorrelated = false;
                    foreach (var member in members)
                    {
                        var c = correlation[index[member.Code], index[candidate.Code]];
                        var t = tail[index[member.Code], index[candidate.Code]];
                        if (c > options.MaxCorrelation || (!double.IsNaN(t) && t > options.MaxTailCorrelation))
                        {
                            tooCorrelated = true;
                            break;
                        }
                    }

                    if (tooCorrelated)
                        continue;

                    var combined = Add(daily, candidate.Daily);
                    if (!FitsAccount(combined, options))
                        continue;

                    var (drawdown, _) = DrawdownAndWorstDay(combined);
                    var net = combined.Sum();
                    var ratio = drawdown > 0m ? net / drawdown : net > 0m ? decimal.MaxValue : 0m;
                    if (ratio > bestRatio)
                    {
                        best = candidate;
                        bestDaily = combined;
                        bestRatio = ratio;
                    }
                }

                if (best is null)
                    break;

                members.Add(best);
                daily = bestDaily!;
            }

            foreach (var member in members)
                remaining.Remove(member);

            var (planDrawdown, worstDay) = DrawdownAndWorstDay(daily);
            var maxCorrelation = double.NaN;
            var maxTail = double.NaN;
            for (var a = 0; a < members.Count; a++)
            {
                for (var b = a + 1; b < members.Count; b++)
                {
                    maxCorrelation = MaxIgnoringNaN(maxCorrelation, correlation[index[members[a].Code], index[members[b].Code]]);
                    maxTail = MaxIgnoringNaN(maxTail, tail[index[members[a].Code], index[members[b].Code]]);
                }
            }

            plans.Add(new BuiltPlan(plans.Count + 1, members, daily.Sum(), planDrawdown, worstDay, daily, maxCorrelation, maxTail));
        }

        return plans;
    }

    private static bool FitsCaps(List<StrategyProfile> members, StrategyProfile candidate, PlanBuilderOptions options)
    {
        var sameSymbol = 0;
        var sameFamily = 0;
        foreach (var member in members)
        {
            if (string.Equals(member.Symbol, candidate.Symbol, StringComparison.OrdinalIgnoreCase)) sameSymbol++;
            if (string.Equals(member.Family, candidate.Family, StringComparison.Ordinal)) sameFamily++;
        }

        return sameSymbol < options.MaxPerSymbol && sameFamily < options.MaxPerFamily;
    }

    private static bool FitsAccount(decimal[] daily, PlanBuilderOptions options)
    {
        var (drawdown, worst) = DrawdownAndWorstDay(daily);
        return (options.MaxPlanDrawdown is not { } maxDrawdown || drawdown <= maxDrawdown) &&
               (options.MaxDailyLoss is not { } maxLoss || -worst <= maxLoss);
    }

    /// <summary>Drawdown massimo dell'equity giornaliera cumulata e il giorno peggiore (negativo se in perdita).</summary>
    public static (decimal Drawdown, decimal WorstDay) DrawdownAndWorstDay(decimal[] daily)
    {
        decimal equity = 0m, peak = 0m, drawdown = 0m, worst = 0m;
        foreach (var value in daily)
        {
            equity += value;
            if (equity > peak) peak = equity;
            if (peak - equity > drawdown) drawdown = peak - equity;
            if (value < worst) worst = value;
        }

        return (drawdown, worst);
    }

    /// <summary>Pearson fra due serie della stessa lunghezza. NaN se una delle due e' costante.</summary>
    public static double Correlation(decimal[] a, decimal[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("le due serie devono stare sullo stesso asse dei giorni.");

        return Pearson(a, b, null);
    }

    /// <summary>
    /// Correlazione <b>nelle code</b>: Pearson sui soli giorni che sono fra i peggiori
    /// <paramref name="quantile"/> dell'una <b>o</b> dell'altra serie (solo giorni in perdita). NaN se i
    /// giorni sono meno di cinque o una delle due e' costante li': coda troppo corta per dire qualcosa, e
    /// il costruttore la lascia passare invece di inventarla.
    /// </summary>
    public static double TailCorrelation(decimal[] a, decimal[] b, double quantile)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("le due serie devono stare sullo stesso asse dei giorni.");

        var days = new HashSet<int>(WorstDays(a, quantile));
        days.UnionWith(WorstDays(b, quantile));
        return days.Count < 5 ? double.NaN : Pearson(a, b, days);
    }

    private static IEnumerable<int> WorstDays(decimal[] series, double quantile)
    {
        var take = Math.Max(1, (int)Math.Ceiling(series.Length * quantile));
        return Enumerable.Range(0, series.Length)
            .Where(i => series[i] < 0m)
            .OrderBy(i => series[i])
            .Take(take);
    }

    private static double Pearson(decimal[] a, decimal[] b, HashSet<int>? only)
    {
        double sumA = 0, sumB = 0;
        var count = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (only is not null && !only.Contains(i)) continue;
            sumA += (double)a[i];
            sumB += (double)b[i];
            count++;
        }

        if (count < 2) return double.NaN;
        var meanA = sumA / count;
        var meanB = sumB / count;

        double covariance = 0, varianceA = 0, varianceB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (only is not null && !only.Contains(i)) continue;
            var da = (double)a[i] - meanA;
            var db = (double)b[i] - meanB;
            covariance += da * db;
            varianceA += da * da;
            varianceB += db * db;
        }

        return varianceA <= 0 || varianceB <= 0 ? double.NaN : covariance / Math.Sqrt(varianceA * varianceB);
    }

    private static decimal[] Add(decimal[] a, decimal[] b)
    {
        var sum = new decimal[a.Length];
        for (var i = 0; i < a.Length; i++)
            sum[i] = a[i] + b[i];
        return sum;
    }

    private static double MaxIgnoringNaN(double current, double value) =>
        double.IsNaN(value) ? current : double.IsNaN(current) ? value : Math.Max(current, value);
}
