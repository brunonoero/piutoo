using System.Diagnostics;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Core.Optimization;

/// <summary>
/// Filtro base delle strategie con parametri di rischio
/// </summary>
public class BasicStrategyFilter
{
    private readonly RiskParameters _riskParams;

    public BasicStrategyFilter(RiskParameters riskParams)
    {
        _riskParams = riskParams;
    }

    /// <summary>
    /// Filtra il backtesting applicando i parametri di rischio settimana per settimana
    /// </summary>
    public FilteredBacktestingResult FilterBacktesting(
        BacktestingResult backtesting,
        int lookbackWeeks,
        string setupName)
    {
        var result = new FilteredBacktestingResult
        {
            OriginalBacktestingId = "",
            SetupName = setupName,
            OptimizationDate = DateTime.UtcNow,
            StartDate = backtesting.StartDate,
            EndDate = backtesting.EndDate,
            InitialCapital = backtesting.InitialCapital,
            FilterParameters = _riskParams
        };

        // Raggruppa i dati per strategia e settimana
        var strategyWeeklyData = GroupStrategyDataByWeek(backtesting);
        var allWeeks = backtesting.WeeklyResults.OrderBy(w => w.Year).ThenBy(w => w.Week).ToList();

        // Debug logging
        Debug.WriteLine($"[BasicFilter] StrategyResults count: {backtesting.StrategyResults?.Count ?? 0}");
        Debug.WriteLine($"[BasicFilter] WeeklyResults count: {allWeeks.Count}");
        Debug.WriteLine($"[BasicFilter] StrategiesUsed: {string.Join(", ", backtesting.StrategiesUsed ?? new List<string>())}");
        Debug.WriteLine($"[BasicFilter] StrategyWeeklyData keys: {strategyWeeklyData.Count}");
        
        foreach (var (stratName, weekData) in strategyWeeklyData.Take(2))
        {
            Debug.WriteLine($"[BasicFilter] Strategy '{stratName}' has {weekData.Count} weeks of data");
            foreach (var (weekKey, data) in weekData.Take(3))
            {
                Debug.WriteLine($"[BasicFilter]   Week {weekKey.Year}-W{weekKey.Week}: Profit={data.Profit}, Trades={data.Trades}");
            }
        }
        
        if (allWeeks.Any())
        {
            var firstWeek = allWeeks.First();
            Debug.WriteLine($"[BasicFilter] First WeeklyResult: Year={firstWeek.Year}, Week={firstWeek.Week}");
        }

        var filteredHourlyResults = new List<HourlyResult>();
        var filteredWeeklyResults = new List<FilteredWeeklyResult>();
        var strategyStatuses = InitializeStrategyStatuses(backtesting);

        decimal runningEquity = backtesting.InitialCapital;
        decimal peakEquity = backtesting.InitialCapital;
        decimal maxDrawdown = 0;

        for (int i = 0; i < allWeeks.Count; i++)
        {
            var currentWeek = allWeeks[i];
            var weekKey = (currentWeek.Year, currentWeek.Week);

            if (i < 3)
            {
                Debug.WriteLine($"[BasicFilter] Processing week {i}: Year={currentWeek.Year}, Week={currentWeek.Week}, WeekKey={weekKey}");
            }

            // Determina strategie attive con allocazioni
            var allocations = DetermineActiveStrategiesWithAllocations(
                strategyWeeklyData,
                allWeeks.Take(i).ToList(),
                lookbackWeeks,
                backtesting.StrategiesInfo);

            var activeStrategies = allocations.Select(a => a.StrategyName).ToHashSet();
            var disabledStrategies = new List<StrategyDisqualification>();

            // Calcola metriche filtrate
            decimal weeklyProfit = 0;
            int weeklyTrades = 0, winningTrades = 0;

            foreach (var strategyName in backtesting.StrategiesUsed)
            {
                var isActive = activeStrategies.Contains(strategyName);
                var strategyStatus = strategyStatuses[strategyName];
                var allocation = allocations.FirstOrDefault(a => a.StrategyName == strategyName);
                var multiplier = allocation?.SizeMultiplier ?? 1.0m;

                if (strategyWeeklyData.TryGetValue(strategyName, out var weeklyData) &&
                    weeklyData.TryGetValue(weekKey, out var strategyWeekData))
                {
                    if (isActive)
                    {
                        var weightedProfit = strategyWeekData.Profit * multiplier;
                        weeklyProfit += weightedProfit;
                        weeklyTrades += strategyWeekData.Trades;
                        winningTrades += strategyWeekData.WinningTrades;
                        strategyStatus.TotalProfitWhenActive += weightedProfit;
                        strategyStatus.ActiveWeeks.Add(currentWeek.Week);
                    }
                    else
                    {
                        strategyStatus.DisabledWeeks.Add(currentWeek.Week);
                        disabledStrategies.Add(new StrategyDisqualification
                        {
                            StrategyName = strategyName,
                            Reasons = GetDisqualificationReasons(strategyWeeklyData, strategyName,
                                allWeeks.Take(i).ToList(), lookbackWeeks),
                            Score = 0
                        });
                    }
                    strategyStatus.TotalProfitIfAlwaysActive += strategyWeekData.Profit;
                }
            }

            if (i < 3)
            {
                Debug.WriteLine($"[BasicFilter] Week {i}: weeklyProfit={weeklyProfit}, weeklyTrades={weeklyTrades}, activeStrategies={activeStrategies.Count}");
            }

            runningEquity += weeklyProfit;
            peakEquity = Math.Max(peakEquity, runningEquity);
            var currentDrawdown = peakEquity > 0 ? (runningEquity - peakEquity) / peakEquity : 0;
            maxDrawdown = Math.Min(maxDrawdown, currentDrawdown);

            if (i < 3)
            {
                Debug.WriteLine($"[BasicFilter] Week {i}: runningEquity={runningEquity}");
            }

            var filteredWeekResult = new FilteredWeeklyResult
            {
                Year = currentWeek.Year,
                Week = currentWeek.Week,
                WeekStart = currentWeek.WeekStart,
                WeekEnd = currentWeek.WeekEnd,
                WeeklyProfit = weeklyProfit,
                WeeklyEquity = runningEquity,
                WeeklyDrawdown = currentDrawdown,
                TotalTrades = weeklyTrades,
                WinningTrades = winningTrades,
                WinRate = weeklyTrades > 0 ? (decimal)winningTrades / weeklyTrades : 0,
                ActiveStrategies = activeStrategies.ToList(),
                AllocationsForNextWeek = DetermineActiveStrategiesWithAllocations(
                    strategyWeeklyData,
                    allWeeks.Take(i + 1).ToList(),
                    lookbackWeeks,
                    backtesting.StrategiesInfo),
                DisabledStrategies = disabledStrategies
            };

            filteredWeeklyResults.Add(filteredWeekResult);
        }

        // Strategie per prossima settimana
        var enabledForNextWeek = DetermineActiveStrategiesWithAllocations(
            strategyWeeklyData,
            allWeeks,
            lookbackWeeks,
            backtesting.StrategiesInfo);

        foreach (var allocation in enabledForNextWeek)
        {
            if (strategyStatuses.ContainsKey(allocation.StrategyName))
            {
                strategyStatuses[allocation.StrategyName].IsEnabledForNextWeek = true;
                strategyStatuses[allocation.StrategyName].SizeMultiplier = allocation.SizeMultiplier;
            }
        }

        // Genera hourly results filtrati
        filteredHourlyResults = GenerateFilteredHourlyResults(
            backtesting.StrategyResults,
            filteredWeeklyResults,
            backtesting.InitialCapital);

        // Compila risultato finale
        result.HourlyResults = filteredHourlyResults;
        result.WeeklyResults = filteredWeeklyResults;
        result.FinalEquity = runningEquity;
        result.TotalProfit = runningEquity - backtesting.InitialCapital;
        result.MaxDrawdown = maxDrawdown;
        result.TotalReturn = backtesting.InitialCapital > 0
            ? (runningEquity - backtesting.InitialCapital) / backtesting.InitialCapital * 100
            : 0;
        result.TotalTrades = filteredWeeklyResults.Sum(w => w.TotalTrades);
        result.WinRate = result.TotalTrades > 0
            ? (decimal)filteredWeeklyResults.Sum(w => w.WinningTrades) / result.TotalTrades
            : 0;
        result.EnabledStrategiesForNextWeek = enabledForNextWeek;
        result.StrategyStatuses = strategyStatuses.Values.ToList();

        result.Stats = new FilteredOptimizationStats
        {
            TotalStrategiesInBacktesting = backtesting.StrategiesUsed.Count,
            AverageActiveStrategiesPerWeek = filteredWeeklyResults.Any()
                ? (decimal)filteredWeeklyResults.Average(w => w.ActiveStrategies.Count)
                : 0,
            WeeksAnalyzed = filteredWeeklyResults.Count,
            LookbackWeeks = lookbackWeeks,
            OriginalTotalProfit = backtesting.TotalProfit,
            FilteredTotalProfit = result.TotalProfit,
            ProfitDifferencePercent = backtesting.TotalProfit != 0
                ? ((result.TotalProfit - backtesting.TotalProfit) / Math.Abs(backtesting.TotalProfit)) * 100
                : 0,
            OriginalMaxDrawdown = backtesting.MaxDrawdown,
            FilteredMaxDrawdown = maxDrawdown
        };

        return result;
    }

    private Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>> GroupStrategyDataByWeek(
        BacktestingResult backtesting)
    {
        var result = new Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>>();

        // Debug: mostra primi 5 StrategyResults raw
        Debug.WriteLine($"[GroupByWeek] Raw StrategyResults sample:");
        foreach (var sr in backtesting.StrategyResults.Take(5))
        {
            Debug.WriteLine($"[GroupByWeek]   Strategy={sr.StrategyName}, DateTime={sr.DateTime}, Profit={sr.Profit}, Signal={sr.Signal}");
        }

        var grouped = backtesting.StrategyResults
            .GroupBy(sr => new { sr.StrategyName, Year = sr.DateTime.Year, Week = GetWeekNumber(sr.DateTime) });

        foreach (var group in grouped)
        {
            if (!result.ContainsKey(group.Key.StrategyName))
            {
                result[group.Key.StrategyName] = new Dictionary<(int Year, int Week), StrategyWeekData>();
            }

            var profit = group.Sum(g => g.Profit);
            var trades = group.Count(g => g.Signal.HasValue);
            var winningTrades = group.Count(g => g.Profit > 0);

            result[group.Key.StrategyName][(group.Key.Year, group.Key.Week)] = new StrategyWeekData
            {
                Profit = profit,
                Trades = trades,
                WinningTrades = winningTrades,
                WinRate = trades > 0 ? (decimal)winningTrades / trades : 0
            };
        }

        return result;
    }

    private Dictionary<string, StrategyWeeklyStatus> InitializeStrategyStatuses(BacktestingResult backtesting)
    {
        var statuses = new Dictionary<string, StrategyWeeklyStatus>();
        foreach (var strategyName in backtesting.StrategiesUsed)
        {
            var info = backtesting.StrategiesInfo?.FirstOrDefault(s => s.Name == strategyName);
            statuses[strategyName] = new StrategyWeeklyStatus
            {
                StrategyName = strategyName,
                Symbol = info?.Symbol ?? "",
                TimeframeMinutes = info?.TimeframeMinutes ?? 0
            };
        }
        return statuses;
    }

    private List<StrategyAllocation> DetermineActiveStrategiesWithAllocations(
        Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>> strategyWeeklyData,
        List<WeeklyResult> previousWeeks,
        int lookbackWeeks,
        List<Piootoo.Shared.Models.Backtesting.StrategyInfo>? strategiesInfo)
    {
        var allocations = new List<StrategyAllocation>();

        if (!previousWeeks.Any())
        {
            // Prima settimana: tutte attive con multiplier 1.0
            return strategyWeeklyData.Keys.Select(name =>
            {
                var info = strategiesInfo?.FirstOrDefault(s => s.Name == name);
                return new StrategyAllocation
                {
                    StrategyName = name,
                    Symbol = info?.Symbol ?? "",
                    TimeframeMinutes = info?.TimeframeMinutes ?? 0,
                    SizeMultiplier = 1.0m,
                    AllocationPercent = 100m / strategyWeeklyData.Count,
                    Score = 0,
                    Rank = 1
                };
            }).ToList();
        }

        var recentWeeks = previousWeeks
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Week)
            .Take(lookbackWeeks)
            .Select(w => (w.Year, w.Week))
            .ToHashSet();

        var strategyMetrics = new List<(string Name, decimal Score, decimal Profit, decimal WinRate, decimal MaxDrawdown, int Trades)>();

        foreach (var (strategyName, weeklyData) in strategyWeeklyData)
        {
            var relevantData = weeklyData
                .Where(kv => recentWeeks.Contains(kv.Key))
                .Select(kv => kv.Value)
                .ToList();

            if (!relevantData.Any())
            {
                strategyMetrics.Add((strategyName, 0, 0, 0.5m, 0, 0));
                continue;
            }

            var totalProfit = relevantData.Sum(d => d.Profit);
            var totalTrades = relevantData.Sum(d => d.Trades);
            var winningTrades = relevantData.Sum(d => d.WinningTrades);
            var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades : 0;

            decimal runningProfit = 0, peak = 0, maxDrawdown = 0;
            foreach (var data in relevantData)
            {
                runningProfit += data.Profit;
                peak = Math.Max(peak, runningProfit);
                var dd = peak > 0 ? (runningProfit - peak) / peak : 0;
                maxDrawdown = Math.Min(maxDrawdown, dd);
            }

            // Applica filtri
            bool passesFilters = true;
            if (winRate < _riskParams.MinWinRate) passesFilters = false;
            if (maxDrawdown < _riskParams.MaxDrawdown) passesFilters = false;
            if (totalTrades < _riskParams.MinTrades) passesFilters = false;
            if (_riskParams.RequirePositiveBalance && totalProfit < 0) passesFilters = false;

            if (passesFilters)
            {
                var profitScore = totalProfit > 0 ? Math.Log10((double)totalProfit + 1) : -Math.Log10((double)Math.Abs(totalProfit) + 1);
                var winRateScore = (double)winRate * 100;
                var ddScore = (1 + (double)maxDrawdown) * 50;
                var tradeScore = Math.Min(totalTrades / 10.0, 10);
                var score = (decimal)(profitScore * 0.4 + winRateScore * 0.3 + ddScore * 0.2 + tradeScore * 0.1);

                strategyMetrics.Add((strategyName, score, totalProfit, winRate, maxDrawdown, totalTrades));
            }
        }

        if (!strategyMetrics.Any()) return allocations;

        var rankedStrategies = strategyMetrics.OrderByDescending(s => s.Score).ToList();
        var maxScore = rankedStrategies.Max(s => s.Score);
        var minScore = rankedStrategies.Min(s => s.Score);
        var scoreRange = maxScore - minScore;

        for (int i = 0; i < rankedStrategies.Count; i++)
        {
            var strategy = rankedStrategies[i];
            var info = strategiesInfo?.FirstOrDefault(s => s.Name == strategy.Name);

            decimal multiplier = 1.0m;
            if (scoreRange > 0)
            {
                var normalizedScore = (strategy.Score - minScore) / scoreRange;
                multiplier = 0.5m + (normalizedScore * 1.5m);
            }

            var percentile = (decimal)i / rankedStrategies.Count;
            if (percentile < 0.25m) multiplier = Math.Max(multiplier, 2.0m);
            else if (percentile < 0.50m) multiplier = Math.Max(multiplier, 1.5m);
            else if (percentile < 0.75m) multiplier = Math.Max(multiplier, 1.0m);
            else multiplier = Math.Min(multiplier, 0.75m);

            multiplier = Math.Round(multiplier * 4) / 4;

            allocations.Add(new StrategyAllocation
            {
                StrategyName = strategy.Name,
                Symbol = info?.Symbol ?? "",
                TimeframeMinutes = info?.TimeframeMinutes ?? 0,
                SizeMultiplier = multiplier,
                Score = strategy.Score,
                Rank = i + 1,
                Metrics = new StrategyMetricsSummary
                {
                    WinRate = strategy.WinRate,
                    TotalProfit = strategy.Profit,
                    MaxDrawdown = strategy.MaxDrawdown,
                    TotalTrades = strategy.Trades,
                    ProfitFactor = CalculateProfitFactor(strategyWeeklyData[strategy.Name], recentWeeks)
                }
            });
        }

        var totalMultiplier = allocations.Sum(a => a.SizeMultiplier);
        foreach (var allocation in allocations)
        {
            allocation.AllocationPercent = totalMultiplier > 0
                ? (allocation.SizeMultiplier / totalMultiplier) * 100
                : 100m / allocations.Count;
        }

        return allocations;
    }

    private decimal CalculateProfitFactor(
        Dictionary<(int Year, int Week), StrategyWeekData> weeklyData,
        HashSet<(int Year, int Week)> recentWeeks)
    {
        var relevantData = weeklyData
            .Where(kv => recentWeeks.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        if (!relevantData.Any()) return 1.0m;

        var totalProfit = relevantData.Where(d => d.Profit > 0).Sum(d => d.Profit);
        var totalLoss = Math.Abs(relevantData.Where(d => d.Profit < 0).Sum(d => d.Profit));

        return totalLoss > 0 ? totalProfit / totalLoss : totalProfit > 0 ? 10.0m : 1.0m;
    }

    private List<string> GetDisqualificationReasons(
        Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>> strategyWeeklyData,
        string strategyName,
        List<WeeklyResult> previousWeeks,
        int lookbackWeeks)
    {
        var reasons = new List<string>();

        if (!strategyWeeklyData.TryGetValue(strategyName, out var weeklyData))
        {
            reasons.Add("Nessun dato disponibile");
            return reasons;
        }

        var recentWeeks = previousWeeks
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Week)
            .Take(lookbackWeeks)
            .Select(w => (w.Year, w.Week))
            .ToHashSet();

        var relevantData = weeklyData
            .Where(kv => recentWeeks.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        if (!relevantData.Any())
        {
            reasons.Add("Dati insufficienti nel periodo di lookback");
            return reasons;
        }

        var totalProfit = relevantData.Sum(d => d.Profit);
        var totalTrades = relevantData.Sum(d => d.Trades);
        var winningTrades = relevantData.Sum(d => d.WinningTrades);
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades : 0;

        decimal runningProfit = 0, peak = 0, maxDrawdown = 0;
        foreach (var data in relevantData)
        {
            runningProfit += data.Profit;
            peak = Math.Max(peak, runningProfit);
            var dd = peak > 0 ? (runningProfit - peak) / peak : 0;
            maxDrawdown = Math.Min(maxDrawdown, dd);
        }

        if (winRate < _riskParams.MinWinRate)
            reasons.Add($"Win rate {winRate:P1} < {_riskParams.MinWinRate:P1}");
        if (maxDrawdown < _riskParams.MaxDrawdown)
            reasons.Add($"Drawdown {maxDrawdown:P1} < {_riskParams.MaxDrawdown:P1}");
        if (totalTrades < _riskParams.MinTrades)
            reasons.Add($"Trade {totalTrades} < {_riskParams.MinTrades}");
        if (_riskParams.RequirePositiveBalance && totalProfit < 0)
            reasons.Add($"Profit negativo: {totalProfit:C}");

        return reasons;
    }

    private List<HourlyResult> GenerateFilteredHourlyResults(
        List<StrategyHourlyResult> strategyResults,
        List<FilteredWeeklyResult> filteredWeeklyResults,
        decimal initialCapital)
    {
        var result = new List<HourlyResult>();

        var activeStrategiesByWeek = filteredWeeklyResults
            .ToDictionary(
                w => (w.Year, w.Week),
                w => w.ActiveStrategies.ToHashSet());

        var hourlyGroups = strategyResults
            .GroupBy(sr => sr.DateTime)
            .OrderBy(g => g.Key);

        decimal equity = initialCapital;
        decimal peak = initialCapital;

        foreach (var hourGroup in hourlyGroups)
        {
            var dateTime = hourGroup.Key;
            var year = dateTime.Year;
            var week = GetWeekNumber(dateTime);
            var weekKey = (year, week);

            if (!activeStrategiesByWeek.TryGetValue(weekKey, out var activeStrategies))
            {
                activeStrategies = new HashSet<string>();
            }

            var hourlyProfit = hourGroup
                .Where(sr => activeStrategies.Contains(sr.StrategyName))
                .Sum(sr => sr.Profit);

            equity += hourlyProfit;
            peak = Math.Max(peak, equity);
            var drawdown = peak > 0 ? (equity - peak) / peak : 0;

            result.Add(new HourlyResult
            {
                DateTime = dateTime,
                Equity = equity,
                Profit = hourlyProfit,
                Drawdown = drawdown
            });
        }

        return result;
    }

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    private class StrategyWeekData
    {
        public decimal Profit { get; set; }
        public int Trades { get; set; }
        public int WinningTrades { get; set; }
        public decimal WinRate { get; set; }
    }
}
