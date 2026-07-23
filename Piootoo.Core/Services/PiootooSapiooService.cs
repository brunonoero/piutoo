using System.Collections.Concurrent;
using System.Text.Json;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Sapioo;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per l'ottimizzazione Sapioo
/// </summary>
public class PiootooSapiooService : IPiootooSapiooService
{
    private readonly ConcurrentDictionary<string, SapiooJob> _jobs = new();
    private readonly IPiootooBacktestingService _backtestingService;
    private readonly IPiootooTradingService _tradingService;
    private readonly PiootooSettings _settings;
    private readonly string _resultsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PiootooSapiooService(
        IPiootooBacktestingService backtestingService,
        IPiootooTradingService tradingService,
        PiootooSettings settings)
    {
        _backtestingService = backtestingService;
        _tradingService = tradingService;
        _settings = settings;
        
        _resultsPath = Path.Combine(settings.GetSettingsPath(), "results");
        if (!Directory.Exists(_resultsPath))
        {
            Directory.CreateDirectory(_resultsPath);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public string StartOptimization(SapiooRequest request)
    {
        var job = new SapiooJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = SapiooJobStatus.Pending
        };

        _jobs[job.JobId] = job;

        // Avvia l'ottimizzazione in background
        Task.Run(async () => await ExecuteOptimization(job, request));

        return job.JobId;
    }

    public SapiooJob? GetJobStatus(string jobId)
    {
        Console.WriteLine($"GetJobStatus chiamato per JobId: {jobId}");
        
        // Cerca prima nel dizionario dei job attivi
        if (_jobs.TryGetValue(jobId, out var job))
        {
            Console.WriteLine($"Job trovato nel dizionario attivo - Status: {job.Status}, HasResult: {job.Result != null}");
            return job;
        }
        
        Console.WriteLine($"Job non trovato nel dizionario attivo, cercando nei file completati per JobId: {jobId}");
        // Se non trovato, cerca nei file completati
        var result = GetResult(jobId);
        if (result != null)
        {
            Console.WriteLine($"Risultato trovato nei file completati per JobId: {jobId}, creando job completato");
            // Restituisci un job completato basato sul risultato salvato
            var completedJob = new SapiooJob
            {
                JobId = jobId,
                Status = SapiooJobStatus.Completed,
                ProgressPercent = 100,
                Result = result,
                CompletedAt = result.FinalResult?.DateTime ?? DateTime.UtcNow
            };
            Console.WriteLine($"Job completato creato - HasResult: {completedJob.Result != null}");
            return completedJob;
        }
        
        Console.WriteLine($"Nessun risultato trovato per JobId: {jobId}");
        return null;
    }

    public SapiooResult? GetResult(string jobId)
    {
        Console.WriteLine($"GetResult chiamato per JobId: {jobId}");
        
        // Prima controlla se c'è un job attivo con risultato
        if (_jobs.TryGetValue(jobId, out var job) && job.Result != null)
        {
            Console.WriteLine($"Risultato trovato nel job attivo per JobId: {jobId}");
            return job.Result;
        }
        
        // Se non trovato, cerca nei file completati
        if (!Directory.Exists(_resultsPath))
        {
            Console.WriteLine($"Directory risultati non esiste: {_resultsPath}");
            return null;
        }

        Console.WriteLine($"Cercando file in: {_resultsPath}");
        var files = Directory.GetFiles(_resultsPath, "sapioo_*.json");
        Console.WriteLine($"Trovati {files.Length} file di ottimizzazione");
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<SapiooResult>(json, _jsonOptions);
                if (result != null)
                {
                    Console.WriteLine($"File {file} - JobId nel file: '{result.JobId}', JobId cercato: '{jobId}'");
                    if (string.Equals(result.JobId, jobId, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Risultato trovato nel file {file} per JobId: {jobId}");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la lettura del file {file}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Risultato non trovato per JobId: {jobId}");
        return null;
    }

    public List<string> GetAvailableBacktestings()
    {
        var backtestings = _backtestingService.GetCompletedBacktestings();
        return backtestings.Select(b => b.SetupName).Distinct().ToList();
    }

    public List<SapiooResult> GetCompletedOptimizations()
    {
        var results = new List<SapiooResult>();
        
        if (!Directory.Exists(_resultsPath))
        {
            Console.WriteLine($"Directory risultati non esiste: {_resultsPath}");
            return results;
        }

        Console.WriteLine($"Cercando file in: {_resultsPath}");
        var files = Directory.GetFiles(_resultsPath, "sapioo_*.json");
        Console.WriteLine($"Trovati {files.Length} file di ottimizzazione");
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<SapiooResult>(json, _jsonOptions);
                if (result != null)
                {
                    if (string.IsNullOrEmpty(result.JobId))
                    {
                        Console.WriteLine($"Attenzione: JobId vuoto nel file {file}");
                        // Prova a estrarre il JobId dal nome del file o dal contenuto JSON
                        // Per ora salta questo file
                        continue;
                    }
                    Console.WriteLine($"File {file} - JobId: '{result.JobId}', BacktestingName: '{result.BacktestingName}'");
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la deserializzazione del file {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Restituiti {results.Count} risultati di ottimizzazione");
        return results.OrderByDescending(r => r.FinalResult?.DateTime ?? DateTime.MinValue).ToList();
    }

    public bool DeleteOptimization(string jobId)
    {
        try
        {
            // Cerca il file del risultato
            if (!Directory.Exists(_resultsPath))
            {
                Console.WriteLine($"Directory risultati non esiste: {_resultsPath}");
                return false;
            }

            var files = Directory.GetFiles(_resultsPath, "sapioo_*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var result = JsonSerializer.Deserialize<SapiooResult>(json, _jsonOptions);
                    if (result != null && result.JobId == jobId)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Ottimizzazione eliminata: {file}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante la lettura del file {file}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Ottimizzazione con JobId {jobId} non trovata");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'eliminazione dell'ottimizzazione: {ex.Message}");
            return false;
        }
    }

    private async Task ExecuteOptimization(SapiooJob job, SapiooRequest request)
    {
        try
        {
            job.Status = SapiooJobStatus.Running;
            job.StartedAt = DateTime.UtcNow;

            // Carica il risultato del backtesting
            var backtestings = _backtestingService.GetCompletedBacktestings();
            var backtesting = backtestings.FirstOrDefault(b => b.SetupName == request.BacktestingName);
            
            if (backtesting == null)
            {
                throw new ArgumentException($"Backtesting '{request.BacktestingName}' non trovato");
            }

            var result = new SapiooResult
            {
                JobId = job.JobId,
                BacktestingName = request.BacktestingName,
                Parameters = request.RiskParams
            };

            // Analizza per settimana
            var weeks = backtesting.WeeklyResults.OrderBy(w => w.WeekStart).ToList();
            var totalWeeks = weeks.Count;
            var processedWeeks = 0;

            for (int i = 0; i < weeks.Count; i++)
            {
                var currentWeek = weeks[i];
                
                // Determina le settimane da analizzare (lookback)
                // Usa EvaluationPeriodWeeks se specificato, altrimenti usa WeeksLookback
                var evaluationWeeks = request.EvaluationPeriodWeeks > 0 
                    ? request.EvaluationPeriodWeeks 
                    : request.RiskParams.WeeksLookback;
                var lookbackStart = Math.Max(0, i - evaluationWeeks + 1);
                var lookbackWeeks = weeks.Skip(lookbackStart).Take(evaluationWeeks).ToList();

                // Analizza performance delle strategie nelle settimane di lookback
                var strategyMetrics = CalculateStrategyMetrics(
                    backtesting, 
                    lookbackWeeks, 
                    request.RiskParams);

                // Filtra strategie e calcola moltiplicatori
                var enabledStrategies = FilterAndWeightStrategies(strategyMetrics, request.RiskParams);

                // Crea risultato settimanale
                var weeklyResult = new WeeklyOptimizationResult
                {
                    Year = currentWeek.Year,
                    Week = currentWeek.Week,
                    WeekStart = currentWeek.WeekStart,
                    WeekEnd = currentWeek.WeekEnd,
                    WeeklyProfit = currentWeek.WeeklyProfit,
                    WeeklyDrawdown = currentWeek.WeeklyDrawdown,
                    WeeklyEquity = currentWeek.WeeklyEquity,
                    EnabledStrategies = enabledStrategies
                };

                result.WeeklyResults.Add(weeklyResult);

                processedWeeks++;
                job.ProgressPercent = (int)((processedWeeks * 100.0) / totalWeeks);
            }

            // Applica filtri ai risultati del backtesting originale
            var enabledStrategyNames = result.WeeklyResults
                .SelectMany(wr => wr.EnabledStrategies.Where(s => s.IsEnabled).Select(s => s.StrategyName))
                .Distinct()
                .ToList();

            var multipliers = result.WeeklyResults
                .SelectMany(wr => wr.EnabledStrategies)
                .GroupBy(sw => sw.StrategyName)
                .ToDictionary(g => g.Key, g => g.Average(sw => sw.Multiplier));

            var filteredBacktesting = _tradingService.ApplyStrategyFilter(
                backtesting, 
                enabledStrategyNames, 
                multipliers);

            // Crea equity curve filtrata
            result.FilteredEquityCurve = filteredBacktesting.HourlyResults
                .Select(hr => new EquityPoint
                {
                    Date = hr.DateTime,
                    Balance = hr.Equity
                })
                .ToList();

            result.FinalResult = new TradingSnapshot
            {
                DateTime = filteredBacktesting.EndDate,
                Equity = filteredBacktesting.FinalEquity,
                Balance = filteredBacktesting.FinalEquity,
                Drawdown = filteredBacktesting.MaxDrawdown,
                Profit = filteredBacktesting.TotalProfit
            };

            // Assicurati che il JobId sia impostato prima di salvare
            if (string.IsNullOrEmpty(result.JobId))
            {
                result.JobId = job.JobId;
                Console.WriteLine($"JobId impostato nel risultato prima del salvataggio: {job.JobId}");
            }
            
            // Salva risultato su file
            var fileName = $"sapioo_{request.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var filePath = Path.Combine(_resultsPath, fileName);
            
            Console.WriteLine($"Salvando risultato ottimizzazione per JobId: {result.JobId} in file: {filePath}");
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            File.WriteAllText(filePath, json);
            result.ResultFilePath = filePath;
            
            // Verifica che il file sia stato salvato correttamente
            if (File.Exists(filePath))
            {
                Console.WriteLine($"File salvato correttamente: {filePath}");
                // Verifica che il JobId sia presente nel file salvato
                var savedJson = File.ReadAllText(filePath);
                if (savedJson.Contains(result.JobId))
                {
                    Console.WriteLine($"JobId '{result.JobId}' verificato nel file salvato");
                }
                else
                {
                    Console.WriteLine($"ATTENZIONE: JobId '{result.JobId}' NON trovato nel file salvato!");
                }
            }
            else
            {
                Console.WriteLine($"ERRORE: File non salvato correttamente: {filePath}");
            }

            job.Result = result;
            job.Status = SapiooJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercent = 100;
        }
        catch (Exception ex)
        {
            job.Status = SapiooJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    private Dictionary<string, StrategyMetrics> CalculateStrategyMetrics(
        BacktestingResult backtesting,
        List<WeeklyResult> lookbackWeeks,
        RiskManagementParams riskParams)
    {
        var metrics = new Dictionary<string, StrategyMetrics>();

        // Raggruppa risultati per strategia
        var strategyNames = backtesting.StrategyResults.Select(sr => sr.StrategyName).Distinct();

        foreach (var strategyName in strategyNames)
        {
            var strategyResults = backtesting.StrategyResults
                .Where(sr => sr.StrategyName == strategyName)
                .Where(sr => lookbackWeeks.Any(w => sr.DateTime >= w.WeekStart && sr.DateTime <= w.WeekEnd))
                .ToList();

            if (!strategyResults.Any())
                continue;

            var weeklyProfits = lookbackWeeks.Select(week =>
            {
                var weekResults = strategyResults
                    .Where(sr => sr.DateTime >= week.WeekStart && sr.DateTime <= week.WeekEnd)
                    .ToList();
                return weekResults.Sum(sr => sr.Profit);
            }).ToList();

            var profitableWeeks = weeklyProfits.Count(p => p > 0);
            var totalWeeks = weeklyProfits.Count;
            var winRate = totalWeeks > 0 ? (decimal)profitableWeeks / totalWeeks : 0;

            var totalProfit = weeklyProfits.Sum();
            var avgProfit = weeklyProfits.Average();
            var stdDev = CalculateStandardDeviation(weeklyProfits.Select(p => (double)p).ToArray());
            var volatility = avgProfit != 0 ? (decimal)(stdDev / Math.Abs((double)avgProfit)) * 100m : 0;

            var maxDrawdown = CalculateMaxDrawdown(strategyResults, lookbackWeeks);
            var sharpeRatio = CalculateSharpeRatio(weeklyProfits);

            var trades = strategyResults.Where(sr => sr.Signal.HasValue && sr.Signal != SignalType.Hold).ToList();
            var winningTrades = trades.Count(t => t.Profit > 0);
            var tradeWinRate = trades.Count > 0 ? (decimal)winningTrades / trades.Count : 0;

            var avgWin = trades.Where(t => t.Profit > 0).Select(t => t.Profit).DefaultIfEmpty(0).Average();
            var avgLoss = Math.Abs(trades.Where(t => t.Profit < 0).Select(t => t.Profit).DefaultIfEmpty(0).Average());
            var profitFactor = avgLoss > 0 ? avgWin / avgLoss : 0;

            var consecutiveLosses = CalculateMaxConsecutiveLosses(strategyResults);

            metrics[strategyName] = new StrategyMetrics
            {
                StrategyName = strategyName,
                WinRate = winRate,
                TradeWinRate = tradeWinRate,
                TotalProfit = totalProfit,
                AvgProfit = avgProfit,
                Volatility = volatility,
                MaxDrawdown = maxDrawdown,
                SharpeRatio = sharpeRatio,
                ProfitFactor = profitFactor,
                ConsecutiveLosses = consecutiveLosses,
                TotalTrades = trades.Count,
                WinningTrades = winningTrades
            };
        }

        return metrics;
    }

    private List<StrategyWeight> FilterAndWeightStrategies(
        Dictionary<string, StrategyMetrics> metrics,
        RiskManagementParams riskParams)
    {
        var weights = new List<StrategyWeight>();

        foreach (var (strategyName, metric) in metrics)
        {
            var weight = new StrategyWeight
            {
                StrategyName = strategyName,
                IsEnabled = true,
                WinRate = metric.TradeWinRate,
                ProfitFactor = metric.ProfitFactor,
                SharpeRatio = metric.SharpeRatio,
                MaxDrawdown = metric.MaxDrawdown
            };

            // Filtri eliminatori
            if (metric.MaxDrawdown > riskParams.MaxDrawdownPercent)
            {
                weight.IsEnabled = false;
                weight.DisabledReason = $"Drawdown troppo alto: {metric.MaxDrawdown:F2}% > {riskParams.MaxDrawdownPercent}%";
            }
            else if (metric.TradeWinRate * 100 < riskParams.MinWinRate)
            {
                weight.IsEnabled = false;
                weight.DisabledReason = $"Win rate troppo basso: {metric.TradeWinRate * 100:F2}% < {riskParams.MinWinRate}%";
            }
            else if (metric.ProfitFactor < riskParams.MinProfitFactor)
            {
                weight.IsEnabled = false;
                weight.DisabledReason = $"Profit factor troppo basso: {metric.ProfitFactor:F2} < {riskParams.MinProfitFactor}";
            }
            else if (metric.ConsecutiveLosses > riskParams.MaxConsecutiveLosses)
            {
                weight.IsEnabled = false;
                weight.DisabledReason = $"Troppe perdite consecutive: {metric.ConsecutiveLosses} > {riskParams.MaxConsecutiveLosses}";
            }

            // Calcola moltiplicatore solo se abilitata
            if (weight.IsEnabled)
            {
                weight.Multiplier = CalculateMultiplier(metric, riskParams);
            }
            else
            {
                weight.Multiplier = 0;
            }

            weights.Add(weight);
        }

        return weights;
    }

    private decimal CalculateMultiplier(StrategyMetrics metric, RiskManagementParams riskParams)
    {
        // Kelly Criterion modificato
        var avgWin = metric.WinningTrades > 0 ? metric.TotalProfit / metric.WinningTrades : 0;
        var avgLoss = metric.TotalTrades - metric.WinningTrades > 0 
            ? Math.Abs(metric.TotalProfit / (metric.TotalTrades - metric.WinningTrades)) 
            : 1;

        var kellyBase = metric.TradeWinRate - ((1 - metric.TradeWinRate) / (avgWin / avgLoss));
        kellyBase = Math.Max(0.1m, Math.Min(1.0m, kellyBase)); // Limita tra 0.1 e 1.0

        // Fattore Sharpe (normalizzato 0-1)
        var fSharpe = Math.Min(1.0m, metric.SharpeRatio / 2.0m);
        fSharpe = Math.Max(0.1m, fSharpe);

        // Fattore Drawdown (penalità)
        var fDrawdown = 1.0m - (metric.MaxDrawdown / riskParams.MaxDrawdownPercent);
        fDrawdown = Math.Max(0.1m, fDrawdown);

        // Fattore Consistenza (stabilità rendimenti)
        var fConsistency = metric.Volatility > 0 
            ? Math.Max(0.1m, 1.0m - (metric.Volatility / riskParams.MaxVolatility))
            : 1.0m;

        // Moltiplicatore finale
        var multiplier = kellyBase * fSharpe * fDrawdown * fConsistency;
        return Math.Max(0.1m, Math.Min(3.0m, multiplier)); // Limita tra 0.1 e 3.0
    }

    private decimal CalculateMaxDrawdown(List<StrategyHourlyResult> results, List<WeeklyResult> weeks)
    {
        if (!results.Any())
            return 0;

        decimal maxEquity = 0;
        decimal maxDrawdown = 0;

        foreach (var week in weeks.OrderBy(w => w.WeekStart))
        {
            var weekResults = results
                .Where(r => r.DateTime >= week.WeekStart && r.DateTime <= week.WeekEnd)
                .OrderBy(r => r.DateTime)
                .ToList();

            decimal runningEquity = 0;
            foreach (var result in weekResults)
            {
                runningEquity += result.Profit;
                if (runningEquity > maxEquity)
                    maxEquity = runningEquity;

                var drawdown = maxEquity > 0 ? ((maxEquity - runningEquity) / maxEquity) * 100m : 0;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;
            }
        }

        return maxDrawdown;
    }

    private decimal CalculateSharpeRatio(List<decimal> weeklyReturns)
    {
        if (!weeklyReturns.Any())
            return 0;

        var avgReturn = weeklyReturns.Average();
        var stdDev = CalculateStandardDeviation(weeklyReturns.Select(r => (double)r).ToArray());
        
        if (stdDev == 0)
            return 0;

        // Assumiamo risk-free rate = 0
        return (decimal)((double)avgReturn / stdDev);
    }

    private double CalculateStandardDeviation(double[] values)
    {
        if (values.Length == 0)
            return 0;

        var avg = values.Average();
        var sumSquaredDiff = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumSquaredDiff / values.Length);
    }

    private int CalculateMaxConsecutiveLosses(List<StrategyHourlyResult> results)
    {
        int maxConsecutive = 0;
        int currentConsecutive = 0;

        foreach (var result in results.OrderBy(r => r.DateTime))
        {
            if (result.Profit < 0)
            {
                currentConsecutive++;
                if (currentConsecutive > maxConsecutive)
                    maxConsecutive = currentConsecutive;
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }

    private class StrategyMetrics
    {
        public string StrategyName { get; set; } = string.Empty;
        public decimal WinRate { get; set; }
        public decimal TradeWinRate { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal AvgProfit { get; set; }
        public decimal Volatility { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal ProfitFactor { get; set; }
        public int ConsecutiveLosses { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
    }
}
