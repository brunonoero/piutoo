namespace Piootoo.Core.Optimization;

/// <summary>
/// Metriche avanzate per valutazione strategie
/// </summary>
public static class AdvancedMetrics
{
    /// <summary>
    /// Calcola il Sortino Ratio (penalizza solo volatilità negativa)
    /// </summary>
    public static decimal CalculateSortinoRatio(decimal[] returns, decimal riskFreeRate = 0)
    {
        if (returns.Length < 2) return 0;

        var avgReturn = returns.Average();
        var negativeReturns = returns.Where(r => r < riskFreeRate).ToArray();
        
        if (negativeReturns.Length == 0) 
            return avgReturn > 0 ? 10m : 0;

        var downsideDeviation = CalculateStdDev(negativeReturns);
        
        if (downsideDeviation == 0) return 0;
        
        // Annualizza (52 settimane)
        var annualizedReturn = (avgReturn - riskFreeRate) * 52;
        var annualizedDownside = downsideDeviation * (decimal)Math.Sqrt(52);
        
        return annualizedDownside != 0 ? annualizedReturn / annualizedDownside : 0;
    }

    /// <summary>
    /// Calcola il Calmar Ratio (rendimento annualizzato / max drawdown)
    /// </summary>
    public static decimal CalculateCalmarRatio(decimal[] returns, decimal maxDrawdown)
    {
        if (returns.Length == 0 || maxDrawdown >= 0) return 0;
        
        var totalReturn = returns.Sum();
        var annualizedReturn = totalReturn * (52m / returns.Length);
        
        return Math.Abs(maxDrawdown) > 0 ? annualizedReturn / Math.Abs(maxDrawdown) : 0;
    }

    /// <summary>
    /// Calcola l'Omega Ratio (probabilità pesata di guadagno vs perdita)
    /// threshold = rendimento minimo accettabile
    /// </summary>
    public static decimal CalculateOmegaRatio(decimal[] returns, decimal threshold = 0)
    {
        if (returns.Length == 0) return 1;

        decimal sumAbove = 0;
        decimal sumBelow = 0;

        foreach (var r in returns)
        {
            if (r > threshold)
                sumAbove += r - threshold;
            else
                sumBelow += threshold - r;
        }

        return sumBelow > 0 ? sumAbove / sumBelow : (sumAbove > 0 ? 10m : 1m);
    }

    /// <summary>
    /// Calcola il Recovery Factor (profit totale / max drawdown)
    /// Misura quanto velocemente si recupera dai drawdown
    /// </summary>
    public static decimal CalculateRecoveryFactor(decimal totalProfit, decimal maxDrawdown)
    {
        if (maxDrawdown >= 0) return totalProfit > 0 ? 10m : 0;
        return Math.Abs(maxDrawdown) > 0 ? totalProfit / Math.Abs(maxDrawdown) : 0;
    }

    /// <summary>
    /// Calcola l'Ulcer Index (misura profondità e durata dei drawdown)
    /// Valori più bassi = meno "dolore" per l'investitore
    /// </summary>
    public static decimal CalculateUlcerIndex(decimal[] equityCurve)
    {
        if (equityCurve.Length < 2) return 0;

        var peak = equityCurve[0];
        var sumSquaredDrawdown = 0m;

        foreach (var equity in equityCurve)
        {
            peak = Math.Max(peak, equity);
            var drawdown = peak > 0 ? (equity - peak) / peak * 100 : 0;
            sumSquaredDrawdown += drawdown * drawdown;
        }

        return (decimal)Math.Sqrt((double)(sumSquaredDrawdown / equityCurve.Length));
    }

    /// <summary>
    /// Calcola il Tail Ratio (comportamento nelle code della distribuzione)
    /// Rapporto tra guadagni estremi e perdite estreme (95° percentile)
    /// </summary>
    public static decimal CalculateTailRatio(decimal[] returns)
    {
        if (returns.Length < 20) return 1;

        var sorted = returns.OrderBy(r => r).ToArray();
        var n = sorted.Length;
        
        // 5° percentile (perdite estreme)
        var lowerTail = sorted[(int)(n * 0.05)];
        // 95° percentile (guadagni estremi)
        var upperTail = sorted[(int)(n * 0.95)];

        return lowerTail != 0 ? Math.Abs(upperTail / lowerTail) : (upperTail > 0 ? 10m : 1m);
    }

    /// <summary>
    /// Calcola il Value at Risk (VaR) al livello di confidenza specificato
    /// Restituisce la massima perdita attesa con la probabilità data
    /// </summary>
    public static decimal CalculateVaR(decimal[] returns, decimal confidenceLevel = 0.95m)
    {
        if (returns.Length < 10) return 0;

        var sorted = returns.OrderBy(r => r).ToArray();
        var index = (int)((1 - confidenceLevel) * sorted.Length);
        
        return sorted[Math.Max(0, index)];
    }

    /// <summary>
    /// Calcola il Conditional VaR (CVaR) - Expected Shortfall
    /// Media delle perdite oltre il VaR
    /// </summary>
    public static decimal CalculateCVaR(decimal[] returns, decimal confidenceLevel = 0.95m)
    {
        if (returns.Length < 10) return 0;

        var var = CalculateVaR(returns, confidenceLevel);
        var tailReturns = returns.Where(r => r <= var).ToArray();
        
        return tailReturns.Length > 0 ? tailReturns.Average() : var;
    }

    /// <summary>
    /// Calcola il Gain-to-Pain Ratio
    /// Somma dei rendimenti positivi / valore assoluto somma negativi
    /// </summary>
    public static decimal CalculateGainToPainRatio(decimal[] returns)
    {
        if (returns.Length == 0) return 0;

        var gains = returns.Where(r => r > 0).Sum();
        var pains = Math.Abs(returns.Where(r => r < 0).Sum());

        return pains > 0 ? gains / pains : (gains > 0 ? 10m : 0);
    }

    /// <summary>
    /// Calcola lo Sharpe Ratio rolling per valutare stabilità
    /// Restituisce media e deviazione standard dello Sharpe nel tempo
    /// </summary>
    public static (decimal Mean, decimal StdDev) CalculateRollingSharpeStats(
        decimal[] returns, 
        int windowSize = 4)
    {
        if (returns.Length < windowSize * 2) return (0, 0);

        var rollingSharpes = new List<decimal>();
        
        for (int i = windowSize; i <= returns.Length; i++)
        {
            var window = returns.Skip(i - windowSize).Take(windowSize).ToArray();
            var sharpe = CalculateSharpeRatio(window);
            rollingSharpes.Add(sharpe);
        }

        if (rollingSharpes.Count == 0) return (0, 0);

        var mean = rollingSharpes.Average();
        var stdDev = CalculateStdDev(rollingSharpes.ToArray());
        
        return (mean, stdDev);
    }

    /// <summary>
    /// Calcola lo Sharpe Ratio
    /// </summary>
    public static decimal CalculateSharpeRatio(decimal[] returns, decimal riskFreeRate = 0)
    {
        if (returns.Length < 2) return 0;

        var avgReturn = returns.Average();
        var stdDev = CalculateStdDev(returns);
        
        if (stdDev == 0) return 0;
        
        // Annualizza
        var annualizedReturn = (avgReturn - riskFreeRate) * 52;
        var annualizedStdDev = stdDev * (decimal)Math.Sqrt(52);
        
        return annualizedStdDev != 0 ? annualizedReturn / annualizedStdDev : 0;
    }

    /// <summary>
    /// Calcola la deviazione standard
    /// </summary>
    public static decimal CalculateStdDev(decimal[] values)
    {
        if (values.Length < 2) return 0;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        
        return (decimal)Math.Sqrt((double)(sumOfSquares / (values.Length - 1)));
    }

    /// <summary>
    /// Calcola il Max Drawdown da una serie di equity
    /// </summary>
    public static decimal CalculateMaxDrawdown(decimal[] equityCurve)
    {
        if (equityCurve.Length < 2) return 0;

        decimal maxDrawdown = 0;
        decimal peak = equityCurve[0];

        foreach (var equity in equityCurve)
        {
            peak = Math.Max(peak, equity);
            var drawdown = peak > 0 ? (equity - peak) / peak : 0;
            maxDrawdown = Math.Min(maxDrawdown, drawdown);
        }

        return maxDrawdown;
    }

    /// <summary>
    /// Calcola i rendimenti da una curva equity
    /// </summary>
    public static decimal[] CalculateReturns(decimal[] equityCurve)
    {
        if (equityCurve.Length < 2) return Array.Empty<decimal>();

        var returns = new decimal[equityCurve.Length - 1];
        
        for (int i = 1; i < equityCurve.Length; i++)
        {
            returns[i - 1] = equityCurve[i - 1] != 0 
                ? (equityCurve[i] - equityCurve[i - 1]) / equityCurve[i - 1] 
                : 0;
        }

        return returns;
    }
}
