using Piootoo.Shared.Models;

namespace Piootoo.Core;

/// <summary>
/// Gestisce la rotazione delle strategie basata su scoring avanzato
/// </summary>
public class StrategyRotationManager
{
    private readonly List<StrategyPerformance> _performanceHistory = new();
    private readonly Dictionary<string, bool> _strategyStatus = new();
    private ScoringConfiguration _config;
    
    public int EvaluationWeeks { get; set; } = 4;
    public int TopStrategiesToEnable { get; set; } = 3;

    public StrategyRotationManager(ScoringConfiguration? config = null)
    {
        _config = config ?? new ScoringConfiguration();
        _config.ValidateWeights();
    }

    /// <summary>
    /// Ottiene la configurazione corrente
    /// </summary>
    public ScoringConfiguration GetCurrentConfiguration()
    {
        return _config;
    }

    /// <summary>
    /// Aggiorna la configurazione dello scoring
    /// </summary>
    public void UpdateScoringConfiguration(ScoringConfiguration config)
    {
        config.ValidateWeights();
        _config = config;
    }

    /// <summary>
    /// Registra la performance di una strategia per una settimana
    /// </summary>
    public void RecordWeeklyPerformance(StrategyPerformance performance)
    {
        _performanceHistory.Add(performance);
    }

    /// <summary>
    /// Valuta e ruota le strategie con sistema di scoring avanzato
    /// </summary>
    public List<StrategyEvaluationResult> EvaluateAndRotateStrategies(DateTime currentDate)
    {
        var currentWeek = GetWeekNumber(currentDate);
        var currentYear = currentDate.Year;
        
        // Filtra le performance delle ultime N settimane
        var recentPerformances = _performanceHistory
            .Where(p => IsWithinLastNWeeks(p.Year, p.Week, currentYear, currentWeek, EvaluationWeeks))
            .ToList();

        if (recentPerformances.Count == 0)
        {
            // Nessuna performance disponibile, abilita tutte
            return _strategyStatus.Keys.Select(strategyName => new StrategyEvaluationResult
            {
                StrategyName = strategyName,
                FinalScore = 0,
                IsEnabled = true,
                Rank = 1,
                QualificationReasons = new List<string> { "Nessuna performance storica disponibile" }
            }).ToList();
        }

        // Calcola metriche aggregate per strategia
        var strategyMetrics = recentPerformances
            .GroupBy(p => p.StrategyName)
            .Select(g => new StrategyMetrics
            {
                StrategyName = g.Key,
                AvgReturn = g.Average(p => p.Return),
                AvgSharpeRatio = g.Average(p => p.SharpeRatio),
                AvgDrawdown = g.Average(p => p.MaxDrawdown),
                AvgWinRate = g.Average(p => p.WinRate),
                AvgProfitFactor = g.Average(p => p.ProfitFactor),
                AvgCalmarRatio = g.Average(p => p.CalmarRatio),
                AvgVolatility = g.Average(p => p.Volatility),
                TotalTrades = g.Sum(p => p.TotalTrades),
                MaxConsecutiveLosses = g.Max(p => p.ConsecutiveLosses),
                WeeksCount = g.Count(),
                
                // Metriche basate su balance
                AvgInitialBalance = g.Average(p => p.InitialBalance),
                AvgFinalBalance = g.Average(p => p.FinalBalance),
                AvgPeakBalance = g.Average(p => p.PeakBalance),
                AvgNetProfit = g.Average(p => p.NetProfit),
                AvgNetProfitPercent = g.Average(p => p.NetProfitPercent),
                MinBalance = g.Min(p => p.MinBalance),
                AvgMaxDrawdownValue = g.Average(p => p.MaxDrawdownValue),
                TotalNetProfit = g.Sum(p => p.NetProfit)
            })
            .ToList();

        // Valori per normalizzazione
        var maxReturn = strategyMetrics.Max(s => s.AvgReturn);
        var minReturn = strategyMetrics.Min(s => s.AvgReturn);
        var maxSharpe = strategyMetrics.Max(s => s.AvgSharpeRatio);
        var maxProfitFactor = strategyMetrics.Max(s => s.AvgProfitFactor);
        var maxCalmar = strategyMetrics.Max(s => s.AvgCalmarRatio);
        var minDrawdown = strategyMetrics.Min(s => s.AvgDrawdown);

        // Calcola score per ogni strategia
        var evaluationResults = new List<StrategyEvaluationResult>();

        foreach (var metric in strategyMetrics)
        {
            var result = new StrategyEvaluationResult
            {
                StrategyName = metric.StrategyName,
                AvgReturn = metric.AvgReturn,
                AvgSharpeRatio = metric.AvgSharpeRatio,
                AvgDrawdown = metric.AvgDrawdown,
                AvgWinRate = metric.AvgWinRate,
                AvgProfitFactor = metric.AvgProfitFactor,
                TotalTrades = metric.TotalTrades
            };

            // Verifica soglie minime
            var qualifies = CheckQualificationThresholds(metric, result);
            
            if (!qualifies)
            {
                result.FinalScore = 0;
                result.IsEnabled = false;
                evaluationResults.Add(result);
                continue;
            }

            // Calcola score componenti
            decimal returnScore, sharpeScore, drawdownScore;
            decimal winRateScore, profitFactorScore, consistencyScore, calmarScore;

            if (_config.UseNormalization)
            {
                // Normalizza i valori tra 0 e 1
                returnScore = Normalize(metric.AvgReturn, minReturn, maxReturn);
                sharpeScore = Normalize(metric.AvgSharpeRatio, 0, maxSharpe);
                drawdownScore = 1 - Normalize(Math.Abs(metric.AvgDrawdown), 0, Math.Abs(minDrawdown));
                winRateScore = metric.AvgWinRate;
                profitFactorScore = Normalize(metric.AvgProfitFactor, 0, maxProfitFactor);
                calmarScore = Normalize(metric.AvgCalmarRatio, 0, maxCalmar);
                
                // Consistency: combinazione di win rate e basse perdite consecutive
                consistencyScore = metric.AvgWinRate * (1 - (metric.MaxConsecutiveLosses / 10m));
                consistencyScore = Math.Max(0, Math.Min(1, consistencyScore));
            }
            else
            {
                // Usa valori raw
                returnScore = metric.AvgReturn * 10; // Scala per rendere più significativo
                sharpeScore = metric.AvgSharpeRatio;
                drawdownScore = -metric.AvgDrawdown * 10;
                winRateScore = metric.AvgWinRate * 100;
                profitFactorScore = metric.AvgProfitFactor;
                calmarScore = metric.AvgCalmarRatio;
                consistencyScore = metric.AvgWinRate * 50;
            }

            // Applica penalità
            if (_config.PenalizeHighVolatility && metric.AvgVolatility > 0.03m)
            {
                var volatilityPenalty = (metric.AvgVolatility - 0.03m) * 2;
                sharpeScore -= volatilityPenalty;
                result.QualificationReasons.Add($"Penalità volatilità: {volatilityPenalty:F2}");
            }

            if (_config.PenalizeConsecutiveLosses && metric.MaxConsecutiveLosses > 3)
            {
                var lossesOverThreshold = metric.MaxConsecutiveLosses - 3;
                var lossPenalty = lossesOverThreshold * _config.ConsecutiveLossesPenalty;
                consistencyScore -= lossPenalty;
                result.QualificationReasons.Add($"Penalità perdite consecutive: {lossPenalty:F2}");
            }

            // Salva score componenti
            result.ComponentScores["Return"] = returnScore;
            result.ComponentScores["Sharpe"] = sharpeScore;
            result.ComponentScores["Drawdown"] = drawdownScore;
            result.ComponentScores["WinRate"] = winRateScore;
            result.ComponentScores["ProfitFactor"] = profitFactorScore;
            result.ComponentScores["Consistency"] = consistencyScore;
            result.ComponentScores["Calmar"] = calmarScore;

            // Calcola score finale pesato
            result.FinalScore = 
                returnScore * _config.ReturnWeight +
                sharpeScore * _config.SharpeRatioWeight +
                drawdownScore * _config.DrawdownWeight +
                winRateScore * _config.WinRateWeight +
                profitFactorScore * _config.ProfitFactorWeight +
                consistencyScore * _config.ConsistencyWeight +
                calmarScore * _config.CalmarRatioWeight;

            evaluationResults.Add(result);
        }

        // Ranking
        if (_config.UseRankBasedScoring)
        {
            evaluationResults = ApplyRankBasedScoring(evaluationResults);
        }

        // Ordina per score e assegna rank
        evaluationResults = evaluationResults.OrderByDescending(r => r.FinalScore).ToList();
        
        for (int i = 0; i < evaluationResults.Count; i++)
        {
            evaluationResults[i].Rank = i + 1;
            evaluationResults[i].IsEnabled = i < TopStrategiesToEnable && 
                                            evaluationResults[i].DisqualificationReasons.Count == 0;
        }

        // Aggiorna stato strategie
        foreach (var result in evaluationResults)
        {
            _strategyStatus[result.StrategyName] = result.IsEnabled;
        }

        return evaluationResults;
    }

    private bool CheckQualificationThresholds(StrategyMetrics metric, StrategyEvaluationResult result)
    {
        bool qualifies = true;

        // Soglie tradizionali
        if (metric.AvgWinRate < _config.MinWinRate)
        {
            result.DisqualificationReasons.Add(
                $"Win rate troppo basso: {metric.AvgWinRate:P2} < {_config.MinWinRate:P2}");
            qualifies = false;
        }

        if (metric.TotalTrades < _config.MinTotalTrades)
        {
            result.DisqualificationReasons.Add(
                $"Trade insufficienti: {metric.TotalTrades} < {_config.MinTotalTrades}");
            qualifies = false;
        }

        if (metric.AvgSharpeRatio < _config.MinSharpeRatio)
        {
            result.DisqualificationReasons.Add(
                $"Sharpe ratio troppo basso: {metric.AvgSharpeRatio:F2} < {_config.MinSharpeRatio:F2}");
            qualifies = false;
        }

        if (metric.AvgDrawdown < _config.MaxDrawdown)
        {
            result.DisqualificationReasons.Add(
                $"Drawdown eccessivo: {metric.AvgDrawdown:P2} < {_config.MaxDrawdown:P2}");
            qualifies = false;
        }

        // Soglie basate su balance e profit
        if (metric.AvgFinalBalance < _config.MinFinalBalance)
        {
            result.DisqualificationReasons.Add(
                $"Balance finale insufficiente: ${metric.AvgFinalBalance:F2} < ${_config.MinFinalBalance:F2}");
            qualifies = false;
        }

        if (metric.AvgNetProfit < _config.MinNetProfit)
        {
            result.DisqualificationReasons.Add(
                $"Profit netto insufficiente: ${metric.AvgNetProfit:F2} < ${_config.MinNetProfit:F2}");
            qualifies = false;
        }

        if (metric.AvgNetProfitPercent < _config.MinNetProfitPercent)
        {
            result.DisqualificationReasons.Add(
                $"Profit % insufficiente: {metric.AvgNetProfitPercent:F2}% < {_config.MinNetProfitPercent:F2}%");
            qualifies = false;
        }

        // Verifica balance sempre positivo
        if (_config.RequirePositiveBalance && metric.MinBalance < 0)
        {
            result.DisqualificationReasons.Add(
                $"Balance sceso sotto zero: ${metric.MinBalance:F2}");
            qualifies = false;
        }

        // Verifica stop loss sul balance
        if (metric.AvgMaxDrawdownValue > 0 && metric.AvgInitialBalance > 0)
        {
            var drawdownFromInitial = -metric.AvgMaxDrawdownValue / metric.AvgInitialBalance;
            if (drawdownFromInitial < _config.StopLossPercent)
            {
                result.DisqualificationReasons.Add(
                    $"Hit stop loss: {drawdownFromInitial:P2} < {_config.StopLossPercent:P2}");
                qualifies = false;
            }
        }

        if (qualifies)
        {
            result.QualificationReasons.Add("✓ Supera tutte le soglie minime");
            
            result.QualificationReasons.Add(
                $"✓ Balance: ${metric.AvgInitialBalance:F0} → ${metric.AvgFinalBalance:F0} " +
                $"(+{metric.AvgNetProfitPercent:F1}%)");
            
            result.QualificationReasons.Add(
                $"✓ Max DD: {metric.AvgDrawdown:P2} (${metric.AvgMaxDrawdownValue:F2})");
        }

        return qualifies;
    }

    private List<StrategyEvaluationResult> ApplyRankBasedScoring(List<StrategyEvaluationResult> results)
    {
        var metrics = new[] { "Return", "Sharpe", "Drawdown", "WinRate", "ProfitFactor", "Consistency", "Calmar" };
        
        foreach (var metric in metrics)
        {
            var ranked = results.OrderByDescending(r => r.ComponentScores.GetValueOrDefault(metric, 0)).ToList();
            for (int i = 0; i < ranked.Count; i++)
            {
                ranked[i].ComponentScores[metric] = ranked.Count - i;
            }
        }

        foreach (var result in results)
        {
            result.FinalScore = 
                result.ComponentScores.GetValueOrDefault("Return", 0) * _config.ReturnWeight +
                result.ComponentScores.GetValueOrDefault("Sharpe", 0) * _config.SharpeRatioWeight +
                result.ComponentScores.GetValueOrDefault("Drawdown", 0) * _config.DrawdownWeight +
                result.ComponentScores.GetValueOrDefault("WinRate", 0) * _config.WinRateWeight +
                result.ComponentScores.GetValueOrDefault("ProfitFactor", 0) * _config.ProfitFactorWeight +
                result.ComponentScores.GetValueOrDefault("Consistency", 0) * _config.ConsistencyWeight +
                result.ComponentScores.GetValueOrDefault("Calmar", 0) * _config.CalmarRatioWeight;
        }

        return results;
    }

    private decimal Normalize(decimal value, decimal min, decimal max)
    {
        if (max == min) return 0.5m;
        return (value - min) / (max - min);
    }

    /// <summary>
    /// Ottimizza i pesi della configurazione testando diverse combinazioni
    /// </summary>
    public OptimizationResult OptimizeWeights(DateTime startDate, DateTime endDate, int optimizationRuns = 100)
    {
        var random = new Random();
        var bestScore = decimal.MinValue;
        ScoringConfiguration? bestConfig = null;
        var results = new List<(ScoringConfiguration Config, decimal Score)>();

        for (int run = 0; run < optimizationRuns; run++)
        {
            // Genera pesi casuali che sommano a 1.0
            var weights = GenerateRandomWeights(random, 7);
            
            var testConfig = new ScoringConfiguration
            {
                ReturnWeight = weights[0],
                SharpeRatioWeight = weights[1],
                DrawdownWeight = weights[2],
                WinRateWeight = weights[3],
                ProfitFactorWeight = weights[4],
                ConsistencyWeight = weights[5],
                CalmarRatioWeight = weights[6],
                MinWinRate = _config.MinWinRate,
                MinTotalTrades = _config.MinTotalTrades,
                MinSharpeRatio = _config.MinSharpeRatio,
                MaxDrawdown = _config.MaxDrawdown
            };

            UpdateScoringConfiguration(testConfig);
            
            // Simula il periodo e calcola performance
            var score = SimulatePerformance(startDate, endDate);
            
            results.Add((testConfig, score));

            if (score > bestScore)
            {
                bestScore = score;
                bestConfig = testConfig;
            }
        }

        return new OptimizationResult
        {
            BestConfiguration = bestConfig!,
            BestScore = bestScore,
            AllResults = results.OrderByDescending(r => r.Score).ToList()
        };
    }

    private decimal[] GenerateRandomWeights(Random random, int count)
    {
        var weights = new decimal[count];
        var sum = 0m;

        for (int i = 0; i < count; i++)
        {
            weights[i] = (decimal)random.NextDouble();
            sum += weights[i];
        }

        // Normalizza per sommare a 1.0
        for (int i = 0; i < count; i++)
        {
            weights[i] /= sum;
        }

        return weights;
    }

    private decimal SimulatePerformance(DateTime startDate, DateTime endDate)
    {
        var relevantPerformances = _performanceHistory
            .Where(p => 
            {
                var date = GetDateFromWeek(p.Year, p.Week);
                return date >= startDate && date <= endDate;
            })
            .ToList();

        if (relevantPerformances.Count == 0) return 0;

        var totalReturn = 0m;
        var weeksProcessed = relevantPerformances.Select(p => (p.Year, p.Week)).Distinct().Count();

        foreach (var weekGroup in relevantPerformances.GroupBy(p => (p.Year, p.Week)))
        {
            var weekDate = GetDateFromWeek(weekGroup.Key.Year, weekGroup.Key.Week);
            var evaluation = EvaluateAndRotateStrategies(weekDate);
            
            var enabledStrategies = evaluation.Where(e => e.IsEnabled).Select(e => e.StrategyName).ToHashSet();
            var enabledPerformances = weekGroup.Where(p => enabledStrategies.Contains(p.StrategyName)).ToList();
            
            if (enabledPerformances.Any())
            {
                var weekReturn = enabledPerformances.Average(p => p.Return);
                totalReturn += weekReturn;
            }
        }

        return weeksProcessed > 0 ? totalReturn / weeksProcessed : 0;
    }

    /// <summary>
    /// Registra una nuova strategia nel sistema
    /// </summary>
    public void RegisterStrategy(string strategyName, bool initiallyEnabled = true)
    {
        if (!_strategyStatus.ContainsKey(strategyName))
        {
            _strategyStatus[strategyName] = initiallyEnabled;
        }
    }

    /// <summary>
    /// Verifica se una strategia è abilitata
    /// </summary>
    public bool IsStrategyEnabled(string strategyName)
    {
        return _strategyStatus.TryGetValue(strategyName, out var enabled) && enabled;
    }

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
    }

    private bool IsWithinLastNWeeks(int perfYear, int perfWeek, int currentYear, int currentWeek, int nWeeks)
    {
        var perfDate = GetDateFromWeek(perfYear, perfWeek);
        var currentDate = GetDateFromWeek(currentYear, currentWeek);
        var weeksDiff = (currentDate - perfDate).Days / 7;
        
        return weeksDiff >= 0 && weeksDiff < nWeeks;
    }

    private DateTime GetDateFromWeek(int year, int week)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        return firstMonday.AddDays((week - 1) * 7);
    }

    public List<StrategyPerformance> GetPerformanceHistory(string? strategyName = null, int? lastNWeeks = null)
    {
        var query = _performanceHistory.AsEnumerable();
        
        if (strategyName != null)
            query = query.Where(p => p.StrategyName == strategyName);
            
        if (lastNWeeks.HasValue)
        {
            var now = DateTime.Now;
            var currentWeek = GetWeekNumber(now);
            var currentYear = now.Year;
            query = query.Where(p => IsWithinLastNWeeks(p.Year, p.Week, 
                currentYear, currentWeek, lastNWeeks.Value));
        }
        
        return query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Week).ToList();
    }

    /// <summary>
    /// Classe interna per le metriche aggregate di una strategia
    /// </summary>
    private class StrategyMetrics
    {
        public string StrategyName { get; set; } = string.Empty;
        public decimal AvgReturn { get; set; }
        public decimal AvgSharpeRatio { get; set; }
        public decimal AvgDrawdown { get; set; }
        public decimal AvgWinRate { get; set; }
        public decimal AvgProfitFactor { get; set; }
        public decimal AvgCalmarRatio { get; set; }
        public decimal AvgVolatility { get; set; }
        public int TotalTrades { get; set; }
        public decimal MaxConsecutiveLosses { get; set; }
        public int WeeksCount { get; set; }
        public decimal AvgInitialBalance { get; set; }
        public decimal AvgFinalBalance { get; set; }
        public decimal AvgPeakBalance { get; set; }
        public decimal AvgNetProfit { get; set; }
        public decimal AvgNetProfitPercent { get; set; }
        public decimal MinBalance { get; set; }
        public decimal AvgMaxDrawdownValue { get; set; }
        public decimal TotalNetProfit { get; set; }
    }
}
