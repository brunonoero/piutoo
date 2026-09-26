using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services.BestPlans;

/// <summary>
/// Le cifre di un best plan a partire dalla curva di equity e dai trade chiusi.
/// </summary>
/// <remarks>
/// L'aritmetica per anno e' quella di <c>BacktestHtmlReport.AppendYearlySummaryHtml</c>: equity di
/// inizio anno = fine dell'anno prima (il capitale iniziale per il primo), drawdown misurato da un
/// picco che parte da li', rendimento sull'equity di inizio anno, trade contati sull'anno di uscita.
/// Cosi' la riga della lista e la tabella del report dello stesso run dicono lo stesso numero.
/// </remarks>
public static class BestPlanPerformance
{
    /// <summary>Colonne della curva salvata: la larghezza di un grafico a schermo intero.</summary>
    public const int StoredEquityColumns = 1400;

    /// <summary>Colonne della curva che arriva nell'elenco, per il grafico in miniatura.</summary>
    public const int SparklineColumns = 120;

    /// <summary>Curva realizzata dai soli trade chiusi: un gradino a ogni uscita.</summary>
    public static List<BestPlanEquityPoint> EquityFromTrades(
        IReadOnlyList<PersistedTrade> trades,
        decimal initialCapital)
    {
        var ordered = trades
            .OrderBy(trade => trade.ExitTimeUtc)
            .ThenBy(trade => trade.TradeId, StringComparer.Ordinal)
            .ToList();
        var points = new List<BestPlanEquityPoint>(ordered.Count + 1);
        if (ordered.Count == 0)
            return points;

        points.Add(new BestPlanEquityPoint
        {
            TimeUtc = ordered.Min(trade => trade.EntryTimeUtc),
            Equity = initialCapital
        });

        var equity = initialCapital;
        var peak = initialCapital;
        foreach (var trade in ordered)
        {
            equity += trade.NetProfit;
            peak = Math.Max(peak, equity);
            points.Add(new BestPlanEquityPoint
            {
                TimeUtc = trade.ExitTimeUtc,
                Equity = equity,
                DrawdownPercent = peak > 0 ? (peak - equity) / peak * 100m : 0m
            });
        }

        return points;
    }

    /// <summary>
    /// Riempie cifre globali e anni. <paramref name="points"/> deve essere in ordine di tempo.
    /// </summary>
    public static void Fill(
        BestPlan plan,
        IReadOnlyList<BestPlanEquityPoint> points,
        decimal initialCapital,
        IReadOnlyList<PersistedTrade> trades)
    {
        plan.InitialCapital = initialCapital;
        plan.FinalEquity = points.Count > 0 ? points[^1].Equity : initialCapital;
        plan.NetProfit = plan.FinalEquity - initialCapital;
        plan.NetProfitPercent = initialCapital != 0 ? plan.NetProfit / initialCapital * 100m : 0m;
        (plan.MaxDrawdown, plan.MaxDrawdownPercent) = MaxDrawdown(points, 0, points.Count, initialCapital);
        plan.TotalTrades = trades.Count;
        plan.WinningTrades = trades.Count(trade => trade.NetProfit > 0);

        var tradesByYear = trades
            .GroupBy(trade => trade.ExitTimeUtc.Year)
            .ToDictionary(group => group.Key, group => group.ToList());

        plan.Years.Clear();
        var yearStartEquity = initialCapital;
        var index = 0;
        while (index < points.Count)
        {
            var year = points[index].TimeUtc.Year;
            var end = index;
            while (end < points.Count && points[end].TimeUtc.Year == year)
                end++;

            var endEquity = points[end - 1].Equity;
            var (drawdown, drawdownPercent) = MaxDrawdown(points, index, end, yearStartEquity);
            var yearTrades = tradesByYear.GetValueOrDefault(year) ?? new List<PersistedTrade>();
            plan.Years.Add(new BestPlanYear
            {
                Year = year,
                StartEquity = yearStartEquity,
                EndEquity = endEquity,
                NetProfit = endEquity - yearStartEquity,
                ReturnPercent = yearStartEquity != 0 ? (endEquity - yearStartEquity) / yearStartEquity * 100m : 0m,
                MaxDrawdown = drawdown,
                MaxDrawdownPercent = drawdownPercent,
                Trades = yearTrades.Count,
                WinningTrades = yearTrades.Count(trade => trade.NetProfit > 0),
                LosingTrades = yearTrades.Count(trade => trade.NetProfit < 0)
            });

            yearStartEquity = endEquity;
            index = end;
        }
    }

    /// <summary>
    /// Drawdown massimo in valuta e in percentuale sul tratto <c>[from, to)</c>, con il picco che
    /// parte da <paramref name="initialPeak"/>. Le due cifre possono riferirsi a punti diversi della
    /// curva, come nel report.
    /// </summary>
    private static (decimal Money, decimal Percent) MaxDrawdown(
        IReadOnlyList<BestPlanEquityPoint> points, int from, int to, decimal initialPeak)
    {
        var peak = initialPeak;
        var money = 0m;
        var fraction = 0m;
        for (var index = from; index < to; index++)
        {
            var equity = points[index].Equity;
            peak = Math.Max(peak, equity);
            money = Math.Max(money, peak - equity);
            if (peak != 0)
                fraction = Math.Max(fraction, (peak - equity) / Math.Abs(peak));
        }

        return (money, fraction * 100m);
    }

    /// <summary>
    /// Riduce la curva a <paramref name="columns"/> colonne contigue tenendo di ognuna il primo,
    /// l'ultimo, il minimo, il massimo e il drawdown peggiore, in ordine di tempo: il tracciato resta
    /// quello della serie intera, con gli estremi al posto giusto. E' la riduzione del grafico
    /// globale del report HTML.
    /// </summary>
    public static List<BestPlanEquityPoint> Downsample(IReadOnlyList<BestPlanEquityPoint> points, int columns)
    {
        if (points.Count <= columns * 5)
            return points.ToList();

        var kept = new List<BestPlanEquityPoint>(columns * 5);
        var lastKept = -1;
        Span<int> candidates = stackalloc int[5];
        for (var column = 0; column < columns; column++)
        {
            var from = (int)((long)column * points.Count / columns);
            var to = (int)((long)(column + 1) * points.Count / columns) - 1;
            if (to < from)
                continue;

            int min = from, max = from, worst = from;
            for (var index = from + 1; index <= to; index++)
            {
                if (points[index].Equity < points[min].Equity) min = index;
                if (points[index].Equity > points[max].Equity) max = index;
                if (points[index].DrawdownPercent > points[worst].DrawdownPercent) worst = index;
            }

            candidates[0] = from;
            candidates[1] = min;
            candidates[2] = max;
            candidates[3] = worst;
            candidates[4] = to;
            candidates.Sort();
            foreach (var index in candidates)
            {
                if (index > lastKept)
                {
                    kept.Add(points[index]);
                    lastKept = index;
                }
            }
        }

        return kept;
    }
}
