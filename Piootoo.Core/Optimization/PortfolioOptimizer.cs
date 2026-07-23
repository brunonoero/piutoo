namespace Piootoo.Core.Optimization;

/// <summary>
/// Ottimizzatore di portafoglio con algoritmi avanzati
/// </summary>
public class PortfolioOptimizer
{
    /// <summary>
    /// Ottimizzazione Risk Parity
    /// Alloca in modo che ogni strategia contribuisca equamente al rischio totale
    /// </summary>
    public static decimal[] RiskParityOptimization(
        Dictionary<string, decimal[]> strategyReturns,
        int maxIterations = 100,
        decimal tolerance = 0.0001m)
    {
        var strategies = strategyReturns.Keys.ToList();
        var n = strategies.Count;
        
        if (n == 0) return Array.Empty<decimal>();
        if (n == 1) return new[] { 1m };

        // Calcola volatilità di ogni strategia
        var volatilities = strategies
            .Select(s => Math.Max(0.0001m, AdvancedMetrics.CalculateStdDev(strategyReturns[s])))
            .ToArray();

        // Calcola matrice di correlazione
        var corrMatrix = CorrelationAnalyzer.CalculateCorrelationMatrix(strategyReturns);

        // Inizializza con pesi uguali
        var weights = Enumerable.Repeat(1m / n, n).ToArray();

        // Iterazione per convergenza
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Calcola contributo al rischio marginale di ogni strategia
            var riskContributions = new decimal[n];
            var totalRisk = 0m;

            for (int i = 0; i < n; i++)
            {
                decimal marginalRisk = 0;
                for (int j = 0; j < n; j++)
                {
                    marginalRisk += weights[j] * volatilities[j] * corrMatrix[i, j];
                }
                riskContributions[i] = weights[i] * volatilities[i] * marginalRisk;
                totalRisk += riskContributions[i];
            }

            if (totalRisk == 0) break;

            // Target: rischio uguale per ogni strategia
            var targetRisk = totalRisk / n;
            var newWeights = new decimal[n];
            var sumWeights = 0m;

            for (int i = 0; i < n; i++)
            {
                // Aggiusta i pesi in base al gap dal target
                var ratio = riskContributions[i] > 0 
                    ? targetRisk / riskContributions[i] 
                    : 1m;
                newWeights[i] = weights[i] * (decimal)Math.Sqrt((double)ratio);
                sumWeights += newWeights[i];
            }

            // Normalizza
            for (int i = 0; i < n; i++)
            {
                newWeights[i] /= sumWeights;
            }

            // Verifica convergenza
            var maxChange = weights.Zip(newWeights, (w1, w2) => Math.Abs(w1 - w2)).Max();
            weights = newWeights;

            if (maxChange < tolerance) break;
        }

        return weights;
    }

    /// <summary>
    /// Kelly Criterion per dimensionamento ottimale
    /// Calcola la frazione ottimale di capitale da allocare
    /// </summary>
    public static decimal[] KellyOptimization(
        Dictionary<string, (decimal WinRate, decimal AvgWin, decimal AvgLoss)> strategyStats,
        decimal kellyFraction = 0.5m) // Half-Kelly per sicurezza
    {
        var strategies = strategyStats.Keys.ToList();
        var n = strategies.Count;
        
        if (n == 0) return Array.Empty<decimal>();

        var kellyWeights = new decimal[n];
        var totalKelly = 0m;

        for (int i = 0; i < n; i++)
        {
            var stats = strategyStats[strategies[i]];
            
            // Formula Kelly: f* = (bp - q) / b
            // dove b = odds (avgWin/avgLoss), p = win rate, q = 1-p
            var b = stats.AvgLoss != 0 ? Math.Abs(stats.AvgWin / stats.AvgLoss) : 1m;
            var p = stats.WinRate;
            var q = 1 - p;

            var kelly = b > 0 ? (b * p - q) / b : 0;
            
            // Applica fraction e limita
            kelly = Math.Max(0, kelly * kellyFraction);
            kelly = Math.Min(0.5m, kelly); // Max 50% per strategia
            
            kellyWeights[i] = kelly;
            totalKelly += kelly;
        }

        // Normalizza se totale > 1
        if (totalKelly > 1)
        {
            for (int i = 0; i < n; i++)
            {
                kellyWeights[i] /= totalKelly;
            }
        }
        else if (totalKelly > 0)
        {
            // Scala per usare tutto il capitale se desiderato
            for (int i = 0; i < n; i++)
            {
                kellyWeights[i] /= totalKelly;
            }
        }
        else
        {
            // Fallback a pesi uguali
            return Enumerable.Repeat(1m / n, n).ToArray();
        }

        return kellyWeights;
    }

    /// <summary>
    /// Hierarchical Risk Parity (HRP)
    /// Combina clustering gerarchico con risk parity
    /// </summary>
    public static decimal[] HierarchicalRiskParity(
        Dictionary<string, decimal[]> strategyReturns)
    {
        var strategies = strategyReturns.Keys.ToList();
        var n = strategies.Count;
        
        if (n == 0) return Array.Empty<decimal>();
        if (n == 1) return new[] { 1m };

        // 1. Calcola matrice di distanza (1 - |correlazione|)
        var corrMatrix = CorrelationAnalyzer.CalculateCorrelationMatrix(strategyReturns);
        var distMatrix = new decimal[n, n];
        
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                distMatrix[i, j] = 1 - Math.Abs(corrMatrix[i, j]);
            }
        }

        // 2. Clustering gerarchico semplificato (single-linkage)
        var clusters = PerformHierarchicalClustering(strategies, distMatrix);

        // 3. Calcola volatilità
        var volatilities = strategies
            .ToDictionary(s => s, s => AdvancedMetrics.CalculateStdDev(strategyReturns[s]));

        // 4. Alloca ricorsivamente basandosi sul clustering
        var weights = AllocateHRP(clusters, volatilities, corrMatrix, strategies);

        return weights;
    }

    /// <summary>
    /// Clustering gerarchico semplificato
    /// </summary>
    private static List<object> PerformHierarchicalClustering(
        List<string> strategies, 
        decimal[,] distMatrix)
    {
        var n = strategies.Count;
        
        // Inizializza ogni strategia come cluster
        var clusters = strategies.Cast<object>().ToList();
        var clusterIndices = Enumerable.Range(0, n).ToList();

        while (clusters.Count > 1)
        {
            // Trova la coppia più vicina
            decimal minDist = decimal.MaxValue;
            int minI = 0, minJ = 1;

            for (int i = 0; i < clusterIndices.Count; i++)
            {
                for (int j = i + 1; j < clusterIndices.Count; j++)
                {
                    var dist = GetClusterDistance(
                        clusterIndices[i], clusterIndices[j], distMatrix, n);
                    
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minI = i;
                        minJ = j;
                    }
                }
            }

            // Unisci i cluster
            var newCluster = new List<object> { clusters[minI], clusters[minJ] };
            clusters.RemoveAt(minJ);
            clusters.RemoveAt(minI);
            clusters.Add(newCluster);

            // Aggiorna indici (usa il minore dei due)
            var newIndex = Math.Min(clusterIndices[minI], clusterIndices[minJ]);
            clusterIndices.RemoveAt(minJ);
            clusterIndices.RemoveAt(minI);
            clusterIndices.Add(newIndex);
        }

        return clusters;
    }

    private static decimal GetClusterDistance(int i, int j, decimal[,] distMatrix, int n)
    {
        if (i >= n || j >= n) return decimal.MaxValue;
        return distMatrix[i, j];
    }

    /// <summary>
    /// Allocazione ricorsiva HRP
    /// </summary>
    private static decimal[] AllocateHRP(
        List<object> clusters,
        Dictionary<string, decimal> volatilities,
        decimal[,] corrMatrix,
        List<string> strategies)
    {
        var weights = new decimal[strategies.Count];
        
        // Inizializza tutti a 1 e poi scala
        for (int i = 0; i < weights.Length; i++)
            weights[i] = 1m;

        AllocateRecursive(clusters[0], weights, volatilities, strategies, 1m);

        // Normalizza
        var sum = weights.Sum();
        if (sum > 0)
        {
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= sum;
        }

        return weights;
    }

    private static void AllocateRecursive(
        object cluster,
        decimal[] weights,
        Dictionary<string, decimal> volatilities,
        List<string> strategies,
        decimal allocation)
    {
        if (cluster is string strategyName)
        {
            var idx = strategies.IndexOf(strategyName);
            if (idx >= 0)
                weights[idx] = allocation;
            return;
        }

        if (cluster is List<object> subClusters && subClusters.Count >= 2)
        {
            // Calcola volatilità di ogni sotto-cluster
            var vol1 = GetClusterVolatility(subClusters[0], volatilities, strategies);
            var vol2 = GetClusterVolatility(subClusters[1], volatilities, strategies);

            var totalVol = vol1 + vol2;
            if (totalVol == 0) totalVol = 1;

            // Inverse volatility weighting
            var w1 = vol2 / totalVol;
            var w2 = vol1 / totalVol;

            AllocateRecursive(subClusters[0], weights, volatilities, strategies, allocation * w1);
            AllocateRecursive(subClusters[1], weights, volatilities, strategies, allocation * w2);
        }
    }

    private static decimal GetClusterVolatility(
        object cluster,
        Dictionary<string, decimal> volatilities,
        List<string> strategies)
    {
        if (cluster is string strategyName)
        {
            return volatilities.GetValueOrDefault(strategyName, 0.01m);
        }

        if (cluster is List<object> subClusters)
        {
            // Media delle volatilità nel cluster
            var vols = subClusters
                .Select(c => GetClusterVolatility(c, volatilities, strategies))
                .ToList();
            return vols.Count > 0 ? vols.Average() : 0.01m;
        }

        return 0.01m;
    }

    /// <summary>
    /// Combina multiple ottimizzazioni con pesi
    /// </summary>
    public static decimal[] CombinedOptimization(
        Dictionary<string, decimal[]> strategyReturns,
        Dictionary<string, (decimal WinRate, decimal AvgWin, decimal AvgLoss)> strategyStats,
        decimal riskParityWeight = 0.4m,
        decimal kellyWeight = 0.3m,
        decimal hrpWeight = 0.3m)
    {
        var strategies = strategyReturns.Keys.ToList();
        var n = strategies.Count;
        
        if (n == 0) return Array.Empty<decimal>();

        // Calcola pesi con ogni metodo
        var rpWeights = RiskParityOptimization(strategyReturns);
        var kellyWeights = KellyOptimization(strategyStats);
        var hrpWeights = HierarchicalRiskParity(strategyReturns);

        // Combina
        var combined = new decimal[n];
        for (int i = 0; i < n; i++)
        {
            combined[i] = riskParityWeight * rpWeights[i] +
                          kellyWeight * kellyWeights[i] +
                          hrpWeight * hrpWeights[i];
        }

        // Normalizza
        var sum = combined.Sum();
        if (sum > 0)
        {
            for (int i = 0; i < n; i++)
                combined[i] /= sum;
        }

        return combined;
    }

    /// <summary>
    /// Converte pesi in moltiplicatori di size (0.5x - 2.0x)
    /// </summary>
    public static decimal[] WeightsToSizeMultipliers(decimal[] weights, decimal baseMultiplier = 1.0m)
    {
        if (weights.Length == 0) return Array.Empty<decimal>();

        var avgWeight = 1m / weights.Length;
        var multipliers = new decimal[weights.Length];

        for (int i = 0; i < weights.Length; i++)
        {
            // Rapporto rispetto al peso medio
            var ratio = avgWeight > 0 ? weights[i] / avgWeight : 1m;
            
            // Scala nel range 0.5 - 2.0
            var multiplier = baseMultiplier * ratio;
            multiplier = Math.Max(0.5m, Math.Min(2.0m, multiplier));
            
            // Arrotonda a 0.25
            multipliers[i] = Math.Round(multiplier * 4) / 4;
        }

        return multipliers;
    }
}
