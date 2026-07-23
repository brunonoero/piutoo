using System.Collections.Concurrent;
using System.Text.Json;
using Piootoo.Core.Optimization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per l'ottimizzazione delle strategie
/// </summary>
public class PiootooOptimizationService
{
    private readonly PiootooBacktestingService _backtestingService;
    private readonly ConcurrentDictionary<string, OptimizationJob> _jobs = new();
    private readonly string _resultsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PiootooOptimizationService(PiootooBacktestingService backtestingService, PiootooSettings settings)
    {
        _backtestingService = backtestingService;
        _resultsPath = Path.Combine(settings.GetSettingsPath(), "results", "optimizations");
        
        // Crea la cartella se non esiste
        if (!Directory.Exists(_resultsPath))
        {
            Directory.CreateDirectory(_resultsPath);
        }
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Async Job Management

    /// <summary>
    /// Avvia un'ottimizzazione BASE in background
    /// </summary>
    public string StartBasicOptimization(
        string backtestingId,
        string setupName,
        int lookbackWeeks,
        RiskParameters riskParams)
    {
        var job = new OptimizationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = OptimizationJobStatus.Pending,
            Type = OptimizationType.Basic,
            CurrentStep = "Inizializzazione..."
        };

        _jobs[job.JobId] = job;

        // Avvia l'ottimizzazione in background
        Task.Run(async () => await ExecuteBasicOptimization(job, backtestingId, setupName, lookbackWeeks, riskParams));

        return job.JobId;
    }

    /// <summary>
    /// Avvia un'ottimizzazione AVANZATA in background
    /// </summary>
    public string StartAdvancedOptimization(
        string backtestingId,
        int lookbackWeeks,
        AdvancedFilterConfig? filterConfig = null)
    {
        var job = new OptimizationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = OptimizationJobStatus.Pending,
            Type = OptimizationType.Advanced,
            CurrentStep = "Inizializzazione..."
        };

        _jobs[job.JobId] = job;

        // Avvia l'ottimizzazione in background
        Task.Run(async () => await ExecuteAdvancedOptimization(job, backtestingId, lookbackWeeks, filterConfig));

        return job.JobId;
    }

    /// <summary>
    /// Ottiene lo stato di un job
    /// </summary>
    public OptimizationJob? GetJobStatus(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    /// <summary>
    /// Ottiene il risultato BASE di un job completato
    /// </summary>
    public FilteredBacktestingResult? GetBasicResult(string jobId)
    {
        var job = GetJobStatus(jobId);
        return job?.BasicResult;
    }

    /// <summary>
    /// Ottiene il risultato AVANZATO di un job completato
    /// </summary>
    public AdvancedOptimizationResult? GetAdvancedResult(string jobId)
    {
        var job = GetJobStatus(jobId);
        return job?.AdvancedResult;
    }

    /// <summary>
    /// Ottiene tutte le ottimizzazioni salvate (BASE)
    /// </summary>
    public List<FilteredBacktestingResult> GetSavedOptimizations()
    {
        var results = new List<FilteredBacktestingResult>();
        
        if (!Directory.Exists(_resultsPath))
        {
            return results;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_*.json");
        Console.WriteLine($"[Optimization] Trovati {files.Length} file di ottimizzazione in {_resultsPath}");
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<FilteredBacktestingResult>(json, _jsonOptions);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Optimization] Errore deserializzazione {file}: {ex.Message}");
            }
        }

        return results.OrderByDescending(r => r.OptimizationDate).ToList();
    }

    /// <summary>
    /// Ottiene un'ottimizzazione salvata per ID
    /// </summary>
    public FilteredBacktestingResult? GetSavedOptimization(string optimizationId)
    {
        // Prima cerca nei job in memoria
        if (_jobs.TryGetValue(optimizationId, out var job) && job.BasicResult != null)
        {
            return job.BasicResult;
        }

        // Poi cerca nei file salvati
        if (!Directory.Exists(_resultsPath))
        {
            return null;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<FilteredBacktestingResult>(json, _jsonOptions);
                if (result != null && result.OriginalBacktestingId == optimizationId)
                {
                    return result;
                }
            }
            catch
            {
                // Ignora file corrotti
            }
        }

        return null;
    }

    /// <summary>
    /// Elimina un'ottimizzazione salvata
    /// </summary>
    public bool DeleteOptimization(string optimizationId)
    {
        if (!Directory.Exists(_resultsPath))
        {
            return false;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<FilteredBacktestingResult>(json, _jsonOptions);
                if (result != null && result.OriginalBacktestingId == optimizationId)
                {
                    File.Delete(file);
                    Console.WriteLine($"[Optimization] Eliminato: {file}");
                    return true;
                }
            }
            catch
            {
                // Ignora file corrotti
            }
        }

        return false;
    }

    /// <summary>
    /// Salva il risultato BASE su file
    /// </summary>
    private void SaveOptimizationResult(FilteredBacktestingResult result, string jobId)
    {
        try
        {
            // Usa il jobId come identificatore univoco
            result.OriginalBacktestingId = jobId;
            
            var fileName = $"optimization_{result.SetupName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var filePath = Path.Combine(_resultsPath, fileName);
            
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            File.WriteAllText(filePath, json);
            
            Console.WriteLine($"[Optimization] Salvato risultato BASE: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Optimization] Errore salvataggio BASE: {ex.Message}");
        }
    }

    /// <summary>
    /// Salva il risultato AVANZATO su file
    /// </summary>
    private void SaveAdvancedOptimizationResult(AdvancedOptimizationResult result, string jobId)
    {
        try
        {
            // Usa il jobId come identificatore univoco
            result.BacktestingId = jobId;
            
            var fileName = $"optimization_adv_{result.SetupName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var filePath = Path.Combine(_resultsPath, fileName);
            
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            File.WriteAllText(filePath, json);
            
            Console.WriteLine($"[Optimization] Salvato risultato AVANZATO: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Optimization] Errore salvataggio AVANZATO: {ex.Message}");
        }
    }

    /// <summary>
    /// Ottiene tutte le ottimizzazioni AVANZATE salvate
    /// </summary>
    public List<AdvancedOptimizationResult> GetSavedAdvancedOptimizations()
    {
        var results = new List<AdvancedOptimizationResult>();
        
        if (!Directory.Exists(_resultsPath))
        {
            return results;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_adv_*.json");
        Console.WriteLine($"[Optimization] Trovati {files.Length} file di ottimizzazione avanzata in {_resultsPath}");
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<AdvancedOptimizationResult>(json, _jsonOptions);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Optimization] Errore deserializzazione {file}: {ex.Message}");
            }
        }

        return results.OrderByDescending(r => r.OptimizationDate).ToList();
    }

    /// <summary>
    /// Ottiene un'ottimizzazione AVANZATA salvata per ID
    /// </summary>
    public AdvancedOptimizationResult? GetSavedAdvancedOptimization(string optimizationId)
    {
        // Prima cerca nei job in memoria
        if (_jobs.TryGetValue(optimizationId, out var job) && job.AdvancedResult != null)
        {
            return job.AdvancedResult;
        }

        // Poi cerca nei file salvati
        if (!Directory.Exists(_resultsPath))
        {
            return null;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_adv_*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<AdvancedOptimizationResult>(json, _jsonOptions);
                if (result != null && result.BacktestingId == optimizationId)
                {
                    return result;
                }
            }
            catch
            {
                // Ignora file corrotti
            }
        }

        return null;
    }

    /// <summary>
    /// Elimina un'ottimizzazione AVANZATA salvata
    /// </summary>
    public bool DeleteAdvancedOptimization(string optimizationId)
    {
        if (!Directory.Exists(_resultsPath))
        {
            return false;
        }

        var files = Directory.GetFiles(_resultsPath, "optimization_adv_*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<AdvancedOptimizationResult>(json, _jsonOptions);
                if (result != null && result.BacktestingId == optimizationId)
                {
                    File.Delete(file);
                    Console.WriteLine($"[Optimization] Eliminato avanzato: {file}");
                    return true;
                }
            }
            catch
            {
                // Ignora file corrotti
            }
        }

        return false;
    }

    /// <summary>
    /// Esegue l'ottimizzazione base in background
    /// </summary>
    private async Task ExecuteBasicOptimization(
        OptimizationJob job,
        string backtestingId,
        string setupName,
        int lookbackWeeks,
        RiskParameters riskParams)
    {
        try
        {
            Console.WriteLine($"[Optimization] Avvio job BASE {job.JobId} per backtesting {backtestingId}");
            
            job.Status = OptimizationJobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            job.CurrentStep = "Caricamento backtesting...";
            job.ProgressPercent = 10;

            await Task.Delay(100); // Yielding per aggiornare UI

            var backtesting = _backtestingService.GetResult(backtestingId);
            if (backtesting == null)
            {
                throw new KeyNotFoundException($"Backtesting con ID '{backtestingId}' non trovato");
            }
            
            Console.WriteLine($"[Optimization] Backtesting trovato: {backtesting.SetupName}, strategie: {backtesting.StrategiesUsed?.Count ?? 0}");

            job.CurrentStep = "Applicazione filtri...";
            job.ProgressPercent = 30;

            await Task.Delay(100);

            var filter = new BasicStrategyFilter(riskParams);
            
            job.CurrentStep = "Calcolo strategie attive per settimana...";
            job.ProgressPercent = 50;

            await Task.Delay(100);

            Console.WriteLine($"[Optimization] Esecuzione filtro con lookbackWeeks={lookbackWeeks}");
            var result = filter.FilterBacktesting(backtesting, lookbackWeeks, setupName);
            Console.WriteLine($"[Optimization] Filtro completato: TotalProfit={result.TotalProfit}, Strategie attive={result.EnabledStrategiesForNextWeek?.Count ?? 0}");

            job.CurrentStep = "Generazione risultati...";
            job.ProgressPercent = 80;

            await Task.Delay(100);

            // Salva su file
            job.CurrentStep = "Salvataggio risultati...";
            job.ProgressPercent = 90;
            SaveOptimizationResult(result, job.JobId);

            job.BasicResult = result;
            job.Status = OptimizationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercent = 100;
            job.CurrentStep = "Completato";
            
            Console.WriteLine($"[Optimization] Job BASE {job.JobId} completato con successo");
        }
        catch (Exception ex)
        {
            job.Status = OptimizationJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.CurrentStep = "Errore";
            Console.WriteLine($"[Optimization] Errore job BASE {job.JobId}: {ex.Message}");
            Console.WriteLine($"[Optimization] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Esegue l'ottimizzazione avanzata in background
    /// </summary>
    private async Task ExecuteAdvancedOptimization(
        OptimizationJob job,
        string backtestingId,
        int lookbackWeeks,
        AdvancedFilterConfig? filterConfig)
    {
        try
        {
            job.Status = OptimizationJobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            job.CurrentStep = "Caricamento backtesting...";
            job.ProgressPercent = 5;

            await Task.Delay(100);

            var backtesting = _backtestingService.GetResult(backtestingId);
            if (backtesting == null)
            {
                throw new KeyNotFoundException($"Backtesting con ID '{backtestingId}' non trovato");
            }

            job.CurrentStep = "Configurazione filtri avanzati...";
            job.ProgressPercent = 10;

            await Task.Delay(100);

            var config = filterConfig ?? new AdvancedFilterConfig();
            config.InitialCapital = backtesting.InitialCapital;

            job.CurrentStep = "Calcolo metriche avanzate (Sharpe, Sortino, Calmar, Omega)...";
            job.ProgressPercent = 20;

            await Task.Delay(100);

            var filter = new AdvancedStrategyFilter(config);

            job.CurrentStep = "Analisi correlazione strategie...";
            job.ProgressPercent = 35;

            await Task.Delay(100);

            job.CurrentStep = "Rimozione strategie correlate...";
            job.ProgressPercent = 50;

            await Task.Delay(100);

            var filterResult = filter.FilterAndOptimize(backtesting, lookbackWeeks);

            job.CurrentStep = "Ottimizzazione portfolio (Risk Parity, Kelly, HRP)...";
            job.ProgressPercent = 65;

            await Task.Delay(100);

            job.CurrentStep = "Applicazione filtri al backtesting...";
            job.ProgressPercent = 80;

            await Task.Delay(100);

            var filteredBacktesting = ApplyFilterToBacktesting(
                backtesting,
                filterResult.FilteredStrategies,
                lookbackWeeks);

            job.CurrentStep = "Generazione risultati finali...";
            job.ProgressPercent = 90;

            await Task.Delay(100);

            var result = new AdvancedOptimizationResult
            {
                BacktestingId = backtestingId,
                SetupName = backtesting.SetupName,
                OptimizationDate = DateTime.UtcNow,
                OriginalStrategiesCount = filterResult.OriginalStrategiesCount,
                FilteredStrategiesCount = filterResult.FilteredStrategies.Count,
                FilteredStrategies = MapToDto(filterResult.FilteredStrategies),
                Correlation = MapCorrelationToDto(filterResult.CorrelationMatrix),
                PortfolioMetrics = MapPortfolioToDto(filterResult.PortfolioMetrics),
                FilteredBacktesting = filteredBacktesting,
                FilterConfigUsed = config
            };

            // Salva su file
            job.CurrentStep = "Salvataggio risultati...";
            job.ProgressPercent = 95;
            SaveAdvancedOptimizationResult(result, job.JobId);

            job.AdvancedResult = result;
            job.Status = OptimizationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercent = 100;
            job.CurrentStep = "Completato";
            
            Console.WriteLine($"[Optimization] Job AVANZATO {job.JobId} completato con successo");
        }
        catch (Exception ex)
        {
            job.Status = OptimizationJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.CurrentStep = "Errore";
            Console.WriteLine($"[Optimization] Errore job avanzato {job.JobId}: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Esegue l'ottimizzazione base filtrando il backtesting
    /// </summary>
    public FilteredBacktestingResult OptimizeBasic(
        string backtestingId,
        string setupName,
        int lookbackWeeks,
        RiskParameters riskParams)
    {
        var backtesting = _backtestingService.GetResult(backtestingId);
        if (backtesting == null)
        {
            throw new KeyNotFoundException($"Backtesting con ID '{backtestingId}' non trovato");
        }

        var filter = new BasicStrategyFilter(riskParams);
        return filter.FilterBacktesting(backtesting, lookbackWeeks, setupName);
    }

    /// <summary>
    /// Esegue l'ottimizzazione AVANZATA con algoritmi sofisticati
    /// </summary>
    public AdvancedOptimizationResult OptimizeAdvanced(
        string backtestingId,
        int lookbackWeeks,
        AdvancedFilterConfig? filterConfig = null)
    {
        var backtesting = _backtestingService.GetResult(backtestingId);
        if (backtesting == null)
        {
            throw new KeyNotFoundException($"Backtesting con ID '{backtestingId}' non trovato");
        }

        // Configura il filtro
        var config = filterConfig ?? new AdvancedFilterConfig();
        config.InitialCapital = backtesting.InitialCapital;

        // Esegui filtro avanzato
        var filter = new AdvancedStrategyFilter(config);
        var filterResult = filter.FilterAndOptimize(backtesting, lookbackWeeks);

        // Applica le strategie filtrate
        var filteredBacktesting = ApplyFilterToBacktesting(
            backtesting,
            filterResult.FilteredStrategies,
            lookbackWeeks);

        // Costruisci risultato
        return new AdvancedOptimizationResult
        {
            BacktestingId = backtestingId,
            SetupName = backtesting.SetupName,
            OptimizationDate = DateTime.UtcNow,
            OriginalStrategiesCount = filterResult.OriginalStrategiesCount,
            FilteredStrategiesCount = filterResult.FilteredStrategies.Count,
            FilteredStrategies = MapToDto(filterResult.FilteredStrategies),
            Correlation = MapCorrelationToDto(filterResult.CorrelationMatrix),
            PortfolioMetrics = MapPortfolioToDto(filterResult.PortfolioMetrics),
            FilteredBacktesting = filteredBacktesting,
            FilterConfigUsed = config
        };
    }

    /// <summary>
    /// Applica il filtro al backtesting
    /// </summary>
    private FilteredBacktestingResult ApplyFilterToBacktesting(
        BacktestingResult backtesting,
        List<FilteredStrategy> filteredStrategies,
        int lookbackWeeks)
    {
        var activeStrategies = filteredStrategies
            .ToDictionary(s => s.StrategyName, s => s.SizeMultiplier);

        var result = new FilteredBacktestingResult
        {
            OriginalBacktestingId = "",
            SetupName = backtesting.SetupName,
            OptimizationDate = DateTime.UtcNow,
            StartDate = backtesting.StartDate,
            EndDate = backtesting.EndDate,
            InitialCapital = backtesting.InitialCapital
        };

        // Raggruppa dati per strategia e settimana
        var strategyWeeklyData = GroupStrategyDataByWeek(backtesting);
        var allWeeks = backtesting.WeeklyResults.OrderBy(w => w.Year).ThenBy(w => w.Week).ToList();

        decimal runningEquity = backtesting.InitialCapital;
        decimal peakEquity = backtesting.InitialCapital;
        decimal maxDrawdown = 0;
        var weeklyResults = new List<FilteredWeeklyResult>();

        foreach (var week in allWeeks)
        {
            var weekKey = (week.Year, week.Week);
            decimal weeklyProfit = 0;
            int trades = 0, winningTrades = 0;

            foreach (var (strategyName, multiplier) in activeStrategies)
            {
                if (strategyWeeklyData.TryGetValue(strategyName, out var weekData) &&
                    weekData.TryGetValue(weekKey, out var data))
                {
                    weeklyProfit += data.Profit * multiplier;
                    trades += data.Trades;
                    winningTrades += data.WinningTrades;
                }
            }

            runningEquity += weeklyProfit;
            peakEquity = Math.Max(peakEquity, runningEquity);
            var drawdown = peakEquity > 0 ? (runningEquity - peakEquity) / peakEquity : 0;
            maxDrawdown = Math.Min(maxDrawdown, drawdown);

            weeklyResults.Add(new FilteredWeeklyResult
            {
                Year = week.Year,
                Week = week.Week,
                WeekStart = week.WeekStart,
                WeekEnd = week.WeekEnd,
                WeeklyProfit = weeklyProfit,
                WeeklyEquity = runningEquity,
                WeeklyDrawdown = drawdown,
                TotalTrades = trades,
                WinningTrades = winningTrades,
                WinRate = trades > 0 ? (decimal)winningTrades / trades : 0,
                ActiveStrategies = activeStrategies.Keys.ToList(),
                AllocationsForNextWeek = filteredStrategies.Select(s => new StrategyAllocation
                {
                    StrategyName = s.StrategyName,
                    Symbol = s.Symbol,
                    TimeframeMinutes = s.TimeframeMinutes,
                    SizeMultiplier = s.SizeMultiplier,
                    AllocationPercent = s.Weight * 100,
                    Score = s.Metrics.CompositeScore,
                    Rank = s.Rank
                }).ToList()
            });
        }

        result.WeeklyResults = weeklyResults;
        result.FinalEquity = runningEquity;
        result.TotalProfit = runningEquity - backtesting.InitialCapital;
        result.MaxDrawdown = maxDrawdown;
        result.TotalReturn = backtesting.InitialCapital > 0
            ? (runningEquity - backtesting.InitialCapital) / backtesting.InitialCapital * 100
            : 0;
        result.TotalTrades = weeklyResults.Sum(w => w.TotalTrades);
        result.WinRate = result.TotalTrades > 0
            ? (decimal)weeklyResults.Sum(w => w.WinningTrades) / result.TotalTrades
            : 0;

        result.EnabledStrategiesForNextWeek = filteredStrategies.Select(s => new StrategyAllocation
        {
            StrategyName = s.StrategyName,
            Symbol = s.Symbol,
            TimeframeMinutes = s.TimeframeMinutes,
            SizeMultiplier = s.SizeMultiplier,
            AllocationPercent = s.Weight * 100,
            Score = s.Metrics.CompositeScore,
            Rank = s.Rank,
            Metrics = new StrategyMetricsSummary
            {
                WinRate = s.Metrics.WinRate,
                TotalProfit = s.Metrics.TotalReturn,
                MaxDrawdown = s.Metrics.MaxDrawdown,
                TotalTrades = s.Metrics.TotalTrades,
                ProfitFactor = 0
            }
        }).ToList();

        result.Stats = new FilteredOptimizationStats
        {
            TotalStrategiesInBacktesting = backtesting.StrategiesUsed.Count,
            AverageActiveStrategiesPerWeek = filteredStrategies.Count,
            WeeksAnalyzed = weeklyResults.Count,
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

    /// <summary>
    /// Raggruppa i dati per strategia e settimana
    /// </summary>
    private Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>> GroupStrategyDataByWeek(
        BacktestingResult backtesting)
    {
        var result = new Dictionary<string, Dictionary<(int Year, int Week), StrategyWeekData>>();

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

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    #region Mapping DTO

    private List<FilteredStrategyDto> MapToDto(List<FilteredStrategy> strategies)
    {
        return strategies.Select(s => new FilteredStrategyDto
        {
            StrategyName = s.StrategyName,
            Symbol = s.Symbol,
            TimeframeMinutes = s.TimeframeMinutes,
            Weight = s.Weight,
            SizeMultiplier = s.SizeMultiplier,
            Rank = s.Rank,
            Metrics = new StrategyAdvancedMetricsDto
            {
                TotalReturn = s.Metrics.TotalReturn,
                WinRate = s.Metrics.WinRate,
                TotalTrades = s.Metrics.TotalTrades,
                AvgWin = s.Metrics.AvgWin,
                AvgLoss = s.Metrics.AvgLoss,
                SharpeRatio = s.Metrics.SharpeRatio,
                SortinoRatio = s.Metrics.SortinoRatio,
                CalmarRatio = s.Metrics.CalmarRatio,
                OmegaRatio = s.Metrics.OmegaRatio,
                MaxDrawdown = s.Metrics.MaxDrawdown,
                RecoveryFactor = s.Metrics.RecoveryFactor,
                UlcerIndex = s.Metrics.UlcerIndex,
                TailRatio = s.Metrics.TailRatio,
                VaR95 = s.Metrics.VaR95,
                CVaR95 = s.Metrics.CVaR95,
                GainToPainRatio = s.Metrics.GainToPainRatio,
                CompositeScore = s.Metrics.CompositeScore
            }
        }).ToList();
    }

    private CorrelationInfoDto MapCorrelationToDto(CorrelationInfo info)
    {
        return new CorrelationInfoDto
        {
            AverageCorrelation = info.AverageCorrelation,
            StrategyNames = info.StrategyNames,
            Matrix = info.Matrix
        };
    }

    private PortfolioMetricsDto MapPortfolioToDto(PortfolioMetrics metrics)
    {
        return new PortfolioMetricsDto
        {
            ExpectedReturn = metrics.ExpectedReturn,
            Volatility = metrics.Volatility,
            SharpeRatio = metrics.SharpeRatio,
            MaxDrawdown = metrics.MaxDrawdown,
            DiversificationRatio = metrics.DiversificationRatio
        };
    }

    #endregion

    /// <summary>
    /// Dati settimanali per strategia
    /// </summary>
    private class StrategyWeekData
    {
        public decimal Profit { get; set; }
        public int Trades { get; set; }
        public int WinningTrades { get; set; }
        public decimal WinRate { get; set; }
    }
}
