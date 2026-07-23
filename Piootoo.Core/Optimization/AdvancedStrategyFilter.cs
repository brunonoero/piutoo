using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Core.Optimization;

/// <summary>
/// Filtro avanzato delle strategie con metriche sofisticate
/// </summary>
public class AdvancedStrategyFilter
{
    private readonly AdvancedFilterConfig _config;

    public AdvancedStrategyFilter(AdvancedFilterConfig? config = null)
    {
        _config = config ?? new AdvancedFilterConfig();
    }

    /// <summary>
    /// Filtra e ottimizza le strategie da un backtesting
    /// </summary>
    public AdvancedFilterResult FilterAndOptimize(
        BacktestingResult backtesting,
        int lookbackWeeks)
    {
        var result = new AdvancedFilterResult
        {
            OriginalStrategiesCount = backtesting.StrategiesUsed.Count
        };

        // 1. Estrai i rendimenti settimanali per ogni strategia
        var strategyReturns = ExtractStrategyReturns(backtesting, lookbackWeeks);
        
        if (strategyReturns.Count == 0)
        {
            result.FilteredStrategies = new List<FilteredStrategy>();
            return result;
        }

        // 2. Calcola metriche avanzate per ogni strategia
        var strategyMetrics = CalculateAdvancedMetrics(strategyReturns, backtesting);

        // 3. Applica filtri base
        var passedBaseFilter = ApplyBaseFilters(strategyMetrics);

        // 4. Analisi correlazione e rimozione ridondanza
        var decorrelatedStrategies = RemoveCorrelatedStrategies(
            passedBaseFilter, 
            strategyReturns);

        // 5. Ottimizzazione pesi con algoritmi avanzati
        var optimizedWeights = OptimizeWeights(decorrelatedStrategies, strategyReturns);

        // 6. Costruisci risultato
        result.FilteredStrategies = BuildFilteredStrategies(
            decorrelatedStrategies, 
            optimizedWeights,
            backtesting.StrategiesInfo);

        result.CorrelationMatrix = BuildCorrelationInfo(strategyReturns, decorrelatedStrategies);
        result.PortfolioMetrics = CalculatePortfolioMetrics(strategyReturns, optimizedWeights);

        return result;
    }

    /// <summary>
    /// Estrae i rendimenti settimanali per strategia
    /// </summary>
    private Dictionary<string, decimal[]> ExtractStrategyReturns(
        BacktestingResult backtesting,
        int lookbackWeeks)
    {
        var result = new Dictionary<string, decimal[]>();

        // Raggruppa i risultati per strategia e settimana
        var grouped = backtesting.StrategyResults
            .GroupBy(sr => sr.StrategyName)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => GetWeekKey(r.DateTime))
                      .OrderBy(wg => wg.Key)
                      .Select(wg => wg.Sum(r => r.Profit))
                      .TakeLast(lookbackWeeks)
                      .ToArray()
            );

        foreach (var (strategy, returns) in grouped)
        {
            if (returns.Length >= _config.MinWeeksRequired)
            {
                result[strategy] = returns;
            }
        }

        return result;
    }

    /// <summary>
    /// Calcola metriche avanzate per ogni strategia
    /// </summary>
    private Dictionary<string, StrategyAdvancedMetrics> CalculateAdvancedMetrics(
        Dictionary<string, decimal[]> strategyReturns,
        BacktestingResult backtesting)
    {
        var result = new Dictionary<string, StrategyAdvancedMetrics>();

        foreach (var (strategy, returns) in strategyReturns)
        {
            // Costruisci curva equity
            var equity = new List<decimal> { _config.InitialCapital };
            foreach (var r in returns)
            {
                equity.Add(equity.Last() + r);
            }
            var equityCurve = equity.ToArray();

            // Calcola statistiche di trading
            var trades = backtesting.StrategyResults
                .Where(r => r.StrategyName == strategy && r.Signal.HasValue)
                .ToList();
            
            var winningTrades = trades.Count(t => t.Profit > 0);
            var losingTrades = trades.Count(t => t.Profit < 0);
            var winRate = trades.Count > 0 ? (decimal)winningTrades / trades.Count : 0;
            var avgWin = trades.Where(t => t.Profit > 0).Select(t => t.Profit).DefaultIfEmpty(0).Average();
            var avgLoss = trades.Where(t => t.Profit < 0).Select(t => t.Profit).DefaultIfEmpty(0).Average();

            var metrics = new StrategyAdvancedMetrics
            {
                StrategyName = strategy,
                
                // Metriche base
                TotalReturn = returns.Sum(),
                WinRate = winRate,
                TotalTrades = trades.Count,
                AvgWin = avgWin,
                AvgLoss = avgLoss,
                
                // Metriche avanzate
                SharpeRatio = AdvancedMetrics.CalculateSharpeRatio(returns),
                SortinoRatio = AdvancedMetrics.CalculateSortinoRatio(returns),
                CalmarRatio = AdvancedMetrics.CalculateCalmarRatio(
                    returns, 
                    AdvancedMetrics.CalculateMaxDrawdown(equityCurve)),
                OmegaRatio = AdvancedMetrics.CalculateOmegaRatio(returns),
                MaxDrawdown = AdvancedMetrics.CalculateMaxDrawdown(equityCurve),
                RecoveryFactor = AdvancedMetrics.CalculateRecoveryFactor(
                    returns.Sum(),
                    AdvancedMetrics.CalculateMaxDrawdown(equityCurve)),
                UlcerIndex = AdvancedMetrics.CalculateUlcerIndex(equityCurve),
                TailRatio = AdvancedMetrics.CalculateTailRatio(returns),
                VaR95 = AdvancedMetrics.CalculateVaR(returns, 0.95m),
                CVaR95 = AdvancedMetrics.CalculateCVaR(returns, 0.95m),
                GainToPainRatio = AdvancedMetrics.CalculateGainToPainRatio(returns),
                
                // Rolling Sharpe
                RollingSharpeStats = AdvancedMetrics.CalculateRollingSharpeStats(returns)
            };

            // Calcola score composito
            metrics.CompositeScore = CalculateCompositeScore(metrics);

            result[strategy] = metrics;
        }

        return result;
    }

    /// <summary>
    /// Calcola uno score composito dalle metriche
    /// </summary>
    private decimal CalculateCompositeScore(StrategyAdvancedMetrics m)
    {
        decimal score = 0;

        // Sharpe e Sortino (normalizzati, tipicamente -2 a +3)
        score += NormalizeMetric(m.SharpeRatio, -1, 3) * _config.SharpeWeight;
        score += NormalizeMetric(m.SortinoRatio, -1, 4) * _config.SortinoWeight;
        
        // Calmar (tipicamente 0 a 5)
        score += NormalizeMetric(m.CalmarRatio, 0, 5) * _config.CalmarWeight;
        
        // Omega (tipicamente 0.5 a 3)
        score += NormalizeMetric(m.OmegaRatio, 0.5m, 3) * _config.OmegaWeight;
        
        // Recovery Factor (tipicamente 0 a 10)
        score += NormalizeMetric(m.RecoveryFactor, 0, 10) * _config.RecoveryWeight;
        
        // Win Rate (0 a 1)
        score += m.WinRate * _config.WinRateWeight;
        
        // Tail Ratio (tipicamente 0.5 a 2)
        score += NormalizeMetric(m.TailRatio, 0.5m, 2) * _config.TailRatioWeight;
        
        // Gain to Pain (tipicamente 0 a 5)
        score += NormalizeMetric(m.GainToPainRatio, 0, 5) * _config.GainToPainWeight;
        
        // Penalità per Ulcer Index alto (tipicamente 0 a 20, invertito)
        score -= NormalizeMetric(m.UlcerIndex, 0, 20) * _config.UlcerPenalty;
        
        // Penalità per drawdown (già negativo)
        score += m.MaxDrawdown * _config.DrawdownPenalty; // MaxDD è negativo
        
        // Bonus per stabilità (rolling sharpe con bassa varianza)
        var sharpeStability = m.RollingSharpeStats.StdDev > 0 
            ? m.RollingSharpeStats.Mean / m.RollingSharpeStats.StdDev 
            : m.RollingSharpeStats.Mean;
        score += NormalizeMetric(sharpeStability, -2, 5) * _config.StabilityBonus;

        return score;
    }

    private decimal NormalizeMetric(decimal value, decimal min, decimal max)
    {
        if (max == min) return 0.5m;
        var normalized = (value - min) / (max - min);
        return Math.Max(0, Math.Min(1, normalized));
    }

    /// <summary>
    /// Applica i filtri base
    /// </summary>
    private List<StrategyAdvancedMetrics> ApplyBaseFilters(
        Dictionary<string, StrategyAdvancedMetrics> metrics)
    {
        return metrics.Values
            .Where(m => 
                m.WinRate >= _config.MinWinRate &&
                m.MaxDrawdown >= _config.MaxDrawdownLimit && // MaxDD è negativo
                m.SharpeRatio >= _config.MinSharpeRatio &&
                m.TotalTrades >= _config.MinTrades &&
                m.CompositeScore >= _config.MinCompositeScore)
            .OrderByDescending(m => m.CompositeScore)
            .ToList();
    }

    /// <summary>
    /// Rimuove strategie troppo correlate
    /// </summary>
    private List<StrategyAdvancedMetrics> RemoveCorrelatedStrategies(
        List<StrategyAdvancedMetrics> strategies,
        Dictionary<string, decimal[]> strategyReturns)
    {
        if (strategies.Count <= 1) return strategies;

        var filtered = strategyReturns
            .Where(kv => strategies.Any(s => s.StrategyName == kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Identifica cluster correlati
        var clusters = CorrelationAnalyzer.IdentifyCorrelatedClusters(
            filtered, 
            _config.MaxCorrelation);

        // Seleziona la migliore da ogni cluster
        var scores = strategies.ToDictionary(s => s.StrategyName, s => s.CompositeScore);
        var selected = CorrelationAnalyzer.SelectBestFromClusters(clusters, scores);

        return strategies.Where(s => selected.Contains(s.StrategyName)).ToList();
    }

    /// <summary>
    /// Ottimizza i pesi con algoritmi avanzati
    /// </summary>
    private Dictionary<string, decimal> OptimizeWeights(
        List<StrategyAdvancedMetrics> strategies,
        Dictionary<string, decimal[]> allReturns)
    {
        if (strategies.Count == 0) return new Dictionary<string, decimal>();

        var filtered = allReturns
            .Where(kv => strategies.Any(s => s.StrategyName == kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var strategyStats = strategies.ToDictionary(
            s => s.StrategyName,
            s => (s.WinRate, s.AvgWin, Math.Abs(s.AvgLoss)));

        // Ottimizzazione combinata
        var weights = PortfolioOptimizer.CombinedOptimization(
            filtered,
            strategyStats,
            _config.RiskParityWeight,
            _config.KellyWeight,
            _config.HRPWeight);

        var strategyNames = filtered.Keys.ToList();
        var result = new Dictionary<string, decimal>();
        
        for (int i = 0; i < strategyNames.Count && i < weights.Length; i++)
        {
            result[strategyNames[i]] = weights[i];
        }

        return result;
    }

    /// <summary>
    /// Costruisce il risultato finale
    /// </summary>
    private List<FilteredStrategy> BuildFilteredStrategies(
        List<StrategyAdvancedMetrics> strategies,
        Dictionary<string, decimal> weights,
        List<Piootoo.Shared.Models.Backtesting.StrategyInfo>? strategiesInfo)
    {
        var multipliers = weights.Count > 0
            ? PortfolioOptimizer.WeightsToSizeMultipliers(weights.Values.ToArray())
            : Array.Empty<decimal>();

        var strategyNames = weights.Keys.ToList();
        var result = new List<FilteredStrategy>();

        for (int i = 0; i < strategies.Count; i++)
        {
            var s = strategies[i];
            var info = strategiesInfo?.FirstOrDefault(si => si.Name == s.StrategyName);
            var weightIdx = strategyNames.IndexOf(s.StrategyName);

            result.Add(new FilteredStrategy
            {
                StrategyName = s.StrategyName,
                Symbol = info?.Symbol ?? "",
                TimeframeMinutes = info?.TimeframeMinutes ?? 0,
                Weight = weightIdx >= 0 ? weights[s.StrategyName] : 0,
                SizeMultiplier = weightIdx >= 0 && weightIdx < multipliers.Length 
                    ? multipliers[weightIdx] : 1m,
                Rank = i + 1,
                Metrics = s
            });
        }

        return result;
    }

    private CorrelationInfo BuildCorrelationInfo(
        Dictionary<string, decimal[]> returns,
        List<StrategyAdvancedMetrics> filtered)
    {
        var filteredReturns = returns
            .Where(kv => filtered.Any(s => s.StrategyName == kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (filteredReturns.Count < 2)
        {
            return new CorrelationInfo();
        }

        var matrix = CorrelationAnalyzer.CalculateCorrelationMatrix(filteredReturns);
        
        return new CorrelationInfo
        {
            AverageCorrelation = CorrelationAnalyzer.CalculateAverageCorrelation(matrix),
            StrategyNames = filteredReturns.Keys.ToList(),
            Matrix = ConvertMatrixToList(matrix)
        };
    }

    private List<List<decimal>> ConvertMatrixToList(decimal[,] matrix)
    {
        var result = new List<List<decimal>>();
        var n = matrix.GetLength(0);
        
        for (int i = 0; i < n; i++)
        {
            var row = new List<decimal>();
            for (int j = 0; j < n; j++)
            {
                row.Add(Math.Round(matrix[i, j], 3));
            }
            result.Add(row);
        }
        
        return result;
    }

    private PortfolioMetrics CalculatePortfolioMetrics(
        Dictionary<string, decimal[]> returns,
        Dictionary<string, decimal> weights)
    {
        if (weights.Count == 0) return new PortfolioMetrics();

        var filteredReturns = returns
            .Where(kv => weights.ContainsKey(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var weightArray = weights.Values.ToArray();
        var portfolioReturns = CorrelationAnalyzer.CalculatePortfolioReturns(
            filteredReturns, weightArray);

        var equity = new List<decimal> { _config.InitialCapital };
        foreach (var r in portfolioReturns)
        {
            equity.Add(equity.Last() + r);
        }
        var equityCurve = equity.ToArray();

        return new PortfolioMetrics
        {
            ExpectedReturn = portfolioReturns.Length > 0 ? portfolioReturns.Average() * 52 : 0,
            Volatility = AdvancedMetrics.CalculateStdDev(portfolioReturns) * (decimal)Math.Sqrt(52),
            SharpeRatio = AdvancedMetrics.CalculateSharpeRatio(portfolioReturns),
            MaxDrawdown = AdvancedMetrics.CalculateMaxDrawdown(equityCurve),
            DiversificationRatio = CorrelationAnalyzer.CalculateDiversificationRatio(
                filteredReturns, weightArray)
        };
    }

    private string GetWeekKey(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var week = cal.GetWeekOfYear(date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
        return $"{date.Year}-W{week:D2}";
    }
}

/// <summary>
/// Configurazione filtro avanzato
/// </summary>
public class AdvancedFilterConfig
{
    // Filtri base
    public decimal MinWinRate { get; set; } = 0.40m;
    public decimal MaxDrawdownLimit { get; set; } = -0.25m;
    public decimal MinSharpeRatio { get; set; } = 0.3m;
    public int MinTrades { get; set; } = 5;
    public decimal MinCompositeScore { get; set; } = 0.3m;
    public int MinWeeksRequired { get; set; } = 3;
    public decimal InitialCapital { get; set; } = 100000m;

    // Correlazione
    public decimal MaxCorrelation { get; set; } = 0.7m;

    // Pesi per score composito
    public decimal SharpeWeight { get; set; } = 0.15m;
    public decimal SortinoWeight { get; set; } = 0.15m;
    public decimal CalmarWeight { get; set; } = 0.10m;
    public decimal OmegaWeight { get; set; } = 0.10m;
    public decimal RecoveryWeight { get; set; } = 0.10m;
    public decimal WinRateWeight { get; set; } = 0.10m;
    public decimal TailRatioWeight { get; set; } = 0.05m;
    public decimal GainToPainWeight { get; set; } = 0.10m;
    public decimal UlcerPenalty { get; set; } = 0.05m;
    public decimal DrawdownPenalty { get; set; } = 0.5m;
    public decimal StabilityBonus { get; set; } = 0.10m;

    // Pesi ottimizzazione portafoglio
    public decimal RiskParityWeight { get; set; } = 0.4m;
    public decimal KellyWeight { get; set; } = 0.3m;
    public decimal HRPWeight { get; set; } = 0.3m;
}

/// <summary>
/// Risultato del filtro avanzato
/// </summary>
public class AdvancedFilterResult
{
    public int OriginalStrategiesCount { get; set; }
    public List<FilteredStrategy> FilteredStrategies { get; set; } = new();
    public CorrelationInfo CorrelationMatrix { get; set; } = new();
    public PortfolioMetrics PortfolioMetrics { get; set; } = new();
}

/// <summary>
/// Strategia filtrata con metriche
/// </summary>
public class FilteredStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }
    public decimal Weight { get; set; }
    public decimal SizeMultiplier { get; set; }
    public int Rank { get; set; }
    public StrategyAdvancedMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Metriche avanzate per strategia
/// </summary>
public class StrategyAdvancedMetrics
{
    public string StrategyName { get; set; } = string.Empty;
    
    // Base
    public decimal TotalReturn { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }
    
    // Avanzate
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal CalmarRatio { get; set; }
    public decimal OmegaRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal RecoveryFactor { get; set; }
    public decimal UlcerIndex { get; set; }
    public decimal TailRatio { get; set; }
    public decimal VaR95 { get; set; }
    public decimal CVaR95 { get; set; }
    public decimal GainToPainRatio { get; set; }
    
    // Stabilità
    public (decimal Mean, decimal StdDev) RollingSharpeStats { get; set; }
    
    // Score finale
    public decimal CompositeScore { get; set; }
}

/// <summary>
/// Info correlazione
/// </summary>
public class CorrelationInfo
{
    public decimal AverageCorrelation { get; set; }
    public List<string> StrategyNames { get; set; } = new();
    public List<List<decimal>> Matrix { get; set; } = new();
}

/// <summary>
/// Metriche portafoglio
/// </summary>
public class PortfolioMetrics
{
    public decimal ExpectedReturn { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal DiversificationRatio { get; set; }
}
