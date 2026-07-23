namespace Piootoo.Core.Optimization;

/// <summary>
/// Analisi di correlazione tra strategie
/// Fondamentale per diversificazione del portafoglio
/// </summary>
public class CorrelationAnalyzer
{
    /// <summary>
    /// Calcola la matrice di correlazione tra strategie
    /// </summary>
    public static decimal[,] CalculateCorrelationMatrix(Dictionary<string, decimal[]> strategyReturns)
    {
        var strategies = strategyReturns.Keys.ToList();
        var n = strategies.Count;
        var matrix = new decimal[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    matrix[i, j] = 1m;
                }
                else if (j > i)
                {
                    var corr = CalculatePearsonCorrelation(
                        strategyReturns[strategies[i]], 
                        strategyReturns[strategies[j]]);
                    matrix[i, j] = corr;
                    matrix[j, i] = corr;
                }
            }
        }

        return matrix;
    }

    /// <summary>
    /// Calcola la correlazione di Pearson tra due serie
    /// </summary>
    public static decimal CalculatePearsonCorrelation(decimal[] x, decimal[] y)
    {
        var n = Math.Min(x.Length, y.Length);
        if (n < 3) return 0;

        // Allinea le serie
        var xAligned = x.Take(n).ToArray();
        var yAligned = y.Take(n).ToArray();

        var xMean = xAligned.Average();
        var yMean = yAligned.Average();

        decimal sumXY = 0, sumX2 = 0, sumY2 = 0;

        for (int i = 0; i < n; i++)
        {
            var dx = xAligned[i] - xMean;
            var dy = yAligned[i] - yMean;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }

        var denominator = (decimal)Math.Sqrt((double)(sumX2 * sumY2));
        
        return denominator > 0 ? sumXY / denominator : 0;
    }

    /// <summary>
    /// Identifica cluster di strategie correlate
    /// Usa clustering gerarchico semplificato
    /// </summary>
    public static List<List<string>> IdentifyCorrelatedClusters(
        Dictionary<string, decimal[]> strategyReturns,
        decimal correlationThreshold = 0.7m)
    {
        var strategies = strategyReturns.Keys.ToList();
        var matrix = CalculateCorrelationMatrix(strategyReturns);
        
        var visited = new HashSet<int>();
        var clusters = new List<List<string>>();

        for (int i = 0; i < strategies.Count; i++)
        {
            if (visited.Contains(i)) continue;

            var cluster = new List<string> { strategies[i] };
            visited.Add(i);

            for (int j = i + 1; j < strategies.Count; j++)
            {
                if (visited.Contains(j)) continue;
                
                if (Math.Abs(matrix[i, j]) >= correlationThreshold)
                {
                    cluster.Add(strategies[j]);
                    visited.Add(j);
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    /// <summary>
    /// Seleziona la migliore strategia da ogni cluster correlato
    /// Riduce la ridondanza nel portafoglio
    /// </summary>
    public static List<string> SelectBestFromClusters(
        List<List<string>> clusters,
        Dictionary<string, decimal> strategyScores)
    {
        var selected = new List<string>();

        foreach (var cluster in clusters)
        {
            // Seleziona la strategia con score più alto nel cluster
            var best = cluster
                .Where(s => strategyScores.ContainsKey(s))
                .OrderByDescending(s => strategyScores[s])
                .FirstOrDefault();

            if (best != null)
                selected.Add(best);
        }

        return selected;
    }

    /// <summary>
    /// Calcola la correlazione media del portafoglio
    /// Valori più bassi = migliore diversificazione
    /// </summary>
    public static decimal CalculateAverageCorrelation(decimal[,] correlationMatrix)
    {
        var n = correlationMatrix.GetLength(0);
        if (n < 2) return 0;

        decimal sum = 0;
        int count = 0;

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                sum += Math.Abs(correlationMatrix[i, j]);
                count++;
            }
        }

        return count > 0 ? sum / count : 0;
    }

    /// <summary>
    /// Calcola il Diversification Ratio
    /// DR = (somma volatilità individuali) / (volatilità portafoglio)
    /// Valori > 1 indicano beneficio dalla diversificazione
    /// </summary>
    public static decimal CalculateDiversificationRatio(
        Dictionary<string, decimal[]> strategyReturns,
        decimal[] weights)
    {
        var strategies = strategyReturns.Keys.ToList();
        if (strategies.Count != weights.Length) return 1;

        // Calcola volatilità individuali
        var individualVols = strategies
            .Select(s => AdvancedMetrics.CalculateStdDev(strategyReturns[s]))
            .ToArray();

        // Somma pesata delle volatilità individuali
        decimal weightedVolSum = 0;
        for (int i = 0; i < strategies.Count; i++)
        {
            weightedVolSum += weights[i] * individualVols[i];
        }

        // Calcola volatilità del portafoglio
        var portfolioReturns = CalculatePortfolioReturns(strategyReturns, weights);
        var portfolioVol = AdvancedMetrics.CalculateStdDev(portfolioReturns);

        return portfolioVol > 0 ? weightedVolSum / portfolioVol : 1;
    }

    /// <summary>
    /// Calcola i rendimenti del portafoglio dato i pesi
    /// </summary>
    public static decimal[] CalculatePortfolioReturns(
        Dictionary<string, decimal[]> strategyReturns,
        decimal[] weights)
    {
        var strategies = strategyReturns.Keys.ToList();
        var minLength = strategyReturns.Values.Min(r => r.Length);
        
        var portfolioReturns = new decimal[minLength];

        for (int t = 0; t < minLength; t++)
        {
            decimal weightedReturn = 0;
            for (int i = 0; i < strategies.Count; i++)
            {
                var returns = strategyReturns[strategies[i]];
                if (t < returns.Length)
                {
                    weightedReturn += weights[i] * returns[t];
                }
            }
            portfolioReturns[t] = weightedReturn;
        }

        return portfolioReturns;
    }
}
