using Piootoo.Shared.Models;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// I criteri con cui una configurazione trovata dalla ricerca si giudica, come funzioni pure sui
/// trade: li usano la griglia grossa e la sweep, e si provano da soli.
///
/// <para><b>Da dove vengono.</b> Soglia di average trade e UngerFit sono del metodo (skill
/// <c>lettura-risultati</c>). Costanza negli anni e outlier sono i filtri minimi e il cancello
/// "outlier" della ricerca Python (<c>metodo/Mat_didattico/SISTEMA_RICERCA_v4.0.md</c> §10.1 e §10.8),
/// portati il 24/09/2026 dopo la lettura della consegna v5.0: dei 224 della v5.0 ne reggono 41 sul
/// periodo mai visto, e i criteri che separano quelli che reggono non sono il netto ma la regolarita'
/// — un trade che fa il 40% del netto, un anno che fa tutto.</para>
/// </summary>
public static class ResearchCriteria
{
    /// <summary>Trade minimi in ogni anno che ne ha almeno uno (v4.0 §10.1).</summary>
    public const int MinTradesPerYearWithTrades = 5;

    /// <summary>Trade medi all'anno sull'intero periodo (v4.0 §10.1).</summary>
    public const decimal MinAverageTradesPerYear = 12m;

    /// <summary>Quota massima del netto che puo' venire dal trade migliore (v4.0 §10.8).</summary>
    public const decimal MaxBestTradeShare = 0.30m;

    /// <summary>Quota del range medio della barra che l'average trade deve superare (metodo).</summary>
    public const decimal RangeShareThreshold = 0.15m;

    /// <summary>Tick minimi dell'average trade, qualunque sia il range (metodo: 6-7 tick).</summary>
    public const int MinTicksThreshold = 6;

    /// <summary>
    /// La costanza negli anni: ogni anno con trade ne ha almeno cinque, in media almeno dodici
    /// all'anno sul periodo, e almeno meta' degli anni con trade e' in utile. Gli anni si contano
    /// sull'uscita del trade, che e' quando il netto entra nel conto.
    /// </summary>
    public static YearConsistency Years(IEnumerable<TradingResult> trades, DateTime fromUtc, DateTime toUtc)
    {
        var byYear = trades
            .GroupBy(trade => trade.ExitDate.Year)
            .Select(group => (Count: group.Count(), Net: group.Sum(trade => trade.NetProfit)))
            .ToList();

        var span = (decimal)Math.Max(1d, (toUtc - fromUtc).TotalDays) / 365.25m;
        var total = byYear.Sum(year => year.Count);
        var result = new YearConsistency(
            YearsWithTrades: byYear.Count,
            MinTradesInYear: byYear.Count == 0 ? 0 : byYear.Min(year => year.Count),
            TradesPerYear: total / span,
            ProfitableYears: byYear.Count(year => year.Net > 0m));

        return result;
    }

    /// <summary>
    /// Quanto del netto viene dal trade migliore. Null se il netto non e' positivo: la domanda
    /// ha senso solo per una configurazione in utile.
    /// </summary>
    public static decimal? BestTradeShare(IEnumerable<TradingResult> trades)
    {
        var list = trades as IReadOnlyCollection<TradingResult> ?? trades.ToList();
        var net = list.Sum(trade => trade.NetProfit);
        if (list.Count == 0 || net <= 0m)
            return null;

        return list.Max(trade => trade.NetProfit) / net;
    }

    /// <summary>
    /// La soglia di average trade di una cella, in denaro per contratto: il 15% del range medio della
    /// barra, e mai meno di sei tick. Si misura sulle barre del campione, non si eredita da un'altra
    /// cella.
    /// </summary>
    public static decimal AverageTradeThreshold(IReadOnlyList<OhlcvData> bars, decimal pointValue, decimal tickSize)
    {
        var range = bars.Count == 0 ? 0m : bars.Average(bar => bar.High - bar.Low);
        return Math.Max(MinTicksThreshold * tickSize * pointValue, RangeShareThreshold * range * pointValue);
    }

    /// <summary>
    /// UngerFit = radice di (E × 100 / R), con E = average trade / soglia ed R = drawdown / average
    /// trade, cioe' quanti trade medi servono a recuperare il drawdown peggiore. Null senza utile.
    /// </summary>
    public static decimal? UngerFit(decimal averageTrade, decimal threshold, decimal maxDrawdown)
    {
        if (averageTrade <= 0m || threshold <= 0m)
            return null;

        var e = averageTrade / threshold;
        var r = Math.Max(maxDrawdown, averageTrade) / averageTrade;
        return (decimal)Math.Sqrt((double)(e * 100m / r));
    }

    /// <summary>Soglia del test dei pattern casuali sull'estremo inferiore di Wilson (v4.0 §10.8).</summary>
    public const double MaxRandomPatternP = 0.20;

    /// <summary>z del Wilson al 90% a una coda, come in Python.</summary>
    public const double WilsonZ = 1.2816;

    /// <summary>
    /// Trade minimi di un'estrazione perche' conti nel test: max(10, meta' dei trade della candidata).
    /// Un pattern casuale che apre tre trade ha un average trade qualunque, e confrontarlo non dice niente.
    /// </summary>
    public static int MinRandomDrawTrades(int candidateTrades) => Math.Max(10, (candidateTrades + 1) / 2);

    /// <summary>
    /// Il test dei pattern casuali: la candidata contro estrazioni in cui ogni pattern prende un valore
    /// a caso della propria griglia, a parita' di tutto il resto. <c>p = (1 + estrazioni con average
    /// trade ≥ candidata) / (1 + estrazioni valide)</c>; la candidata <b>non</b> passa se l'estremo
    /// inferiore di Wilson di p supera 0,20, cioe' se un pattern qualunque fa altrettanto piu' spesso
    /// di una volta su cinque. Senza estrazioni valide passa: non c'e' niente con cui confrontare.
    /// </summary>
    public static RandomPatternTest RandomPatterns(
        decimal candidateAverageTrade, int candidateTrades, IEnumerable<(int Trades, decimal AverageTrade)> draws)
    {
        var minimum = MinRandomDrawTrades(candidateTrades);
        var valid = draws.Where(draw => draw.Trades >= minimum).ToList();
        var better = valid.Count(draw => draw.AverageTrade >= candidateAverageTrade);
        var p = (1d + better) / (1d + valid.Count);
        var lower = valid.Count == 0 ? 0d : WilsonLower(p, valid.Count, WilsonZ);
        return new RandomPatternTest(valid.Count, better, p, lower, valid.Count == 0 || lower <= MaxRandomPatternP);
    }

    /// <summary>Estremo inferiore dell'intervallo di Wilson di una proporzione p su n prove.</summary>
    public static double WilsonLower(double p, int n, double z)
    {
        if (n <= 0) return 0d;
        var z2 = z * z;
        var denominator = 1d + z2 / n;
        var centre = (p + z2 / (2d * n)) / denominator;
        var half = z / denominator * Math.Sqrt(p * (1d - p) / n + z2 / (4d * n * n));
        return Math.Max(0d, centre - half);
    }
}

/// <summary>Esito del test dei pattern casuali. Vedi <see cref="ResearchCriteria.RandomPatterns"/>.</summary>
public sealed record RandomPatternTest(int ValidDraws, int DrawsAtLeastAsGood, double P, double WilsonLower, bool Passes);

/// <summary>Come si distribuiscono trade e utile negli anni. Vedi <see cref="ResearchCriteria.Years"/>.</summary>
public sealed record YearConsistency(int YearsWithTrades, int MinTradesInYear, decimal TradesPerYear, int ProfitableYears)
{
    public bool Passes =>
        YearsWithTrades > 0 &&
        MinTradesInYear >= ResearchCriteria.MinTradesPerYearWithTrades &&
        TradesPerYear >= ResearchCriteria.MinAverageTradesPerYear &&
        ProfitableYears * 2 >= YearsWithTrades;
}
