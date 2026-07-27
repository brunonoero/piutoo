using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core;

/// <summary>
/// Engine di trading con gestione automatica del setup settimanale
/// </summary>
public class TradingEngine
{
    private readonly List<ITradingStrategy> _strategies = new();
    private readonly StrategyRotationManager _rotationManager;
    private readonly WeeklyRotationScheduler _scheduler;
    private readonly PerformanceCalculator _performanceCalculator;
    private readonly Dictionary<string, List<TradingResult>> _weeklyResults = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _strategyRuntimeStates = new(StringComparer.OrdinalIgnoreCase);

    public TradingEngine(StrategyRotationManager rotationManager, 
                        decimal initialBalance = 10000m,
                        decimal commissionPerTrade = 2m)
    {
        _rotationManager = rotationManager;
        _scheduler = new WeeklyRotationScheduler(rotationManager);
        _performanceCalculator = new PerformanceCalculator(initialBalance, commissionPerTrade);
    }

    public void AddStrategy(ITradingStrategy strategy)
    {
        _strategies.Add(strategy);
        _rotationManager.RegisterStrategy(strategy.Name);
    }

    /// <summary>
    /// MODALITÀ BACKTESTING: Esegue backtest con rotazione automatica settimanale
    /// </summary>
    public BacktestResult RunBacktestWithWeeklyRotation(
        OhlcvData[] data, 
        DateTime startDate, 
        DateTime endDate)
    {
        var result = new BacktestResult
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Serie completa ordinata una volta sola: serve come base per la finestra storica
        // passata alle strategie, che avanza insieme al tempo del backtest.
        var sortedData = data.OrderBy(d => d.DateTime).ToArray();
        var historyCursor = new Services.CandleWindowCursor(sortedData);

        // Raggruppa dati per settimana
        var dataByWeek = data
            .Where(d => d.DateTime >= startDate && d.DateTime <= endDate)
            .GroupBy(d => GetWeekStartDate(d.DateTime))
            .OrderBy(g => g.Key)
            .ToList();

        Console.WriteLine($"\n=== BACKTEST CON ROTAZIONE SETTIMANALE ===");
        Console.WriteLine($"Periodo: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}");
        Console.WriteLine($"Numero settimane: {dataByWeek.Count}\n");

        foreach (var weekData in dataByWeek)
        {
            var weekStart = weekData.Key;
            var weekEnd = weekStart.AddDays(6);
            var weekend = weekEnd; // Domenica

            Console.WriteLine($"\n--- Settimana {GetWeekNumber(weekStart)}/{weekStart.Year} ---");

            // 1. WEEKEND: Esegue rotazione e genera setup
            Console.WriteLine($"[Weekend {weekend:dd/MM}] Esecuzione rotazione...");
            var setup = _scheduler.ExecuteWeeklyRotation(weekend);
            result.WeeklySetups.Add(setup);

            Console.WriteLine($"Strategie abilitate: {string.Join(", ", setup.EnabledStrategies)}");

            // 2. SETTIMANA: Trading con le strategie abilitate
            foreach (var date in weekData.Select(d => d.DateTime).OrderBy(d => d))
            {
                // Cursore invece di un Where+ToArray su tutta la serie a ogni barra: quello era
                // quadratico nel numero di candele.
                var historicalData = historyCursor.Window(date, sortedData.Length);


                // Esegue solo le strategie abilitate dal setup
                foreach (var strategy in _strategies.Where(s => setup.EnabledStrategies.Contains(s.Name)))
                {
                    var signal = Evaluate(strategy, historicalData, date);
                    
                    if (signal.Type != SignalType.Hold)
                    {
                        result.AllSignals.Add(signal);
                    }
                }
            }

            // 3. FINE SETTIMANA: Calcola performance
            foreach (var strategyName in setup.EnabledStrategies)
            {
                if (_weeklyResults.TryGetValue(strategyName, out var trades) && trades.Any())
                {
                    var performance = _performanceCalculator.CalculatePerformance(
                        strategyName,
                        trades,
                        GetWeekNumber(weekStart),
                        weekStart.Year
                    );

                    _rotationManager.RecordWeeklyPerformance(performance);
                    result.WeeklyPerformances.Add(performance);

                    Console.WriteLine($"  {strategyName}: {performance.NetProfitPercent:F2}% " +
                        $"({performance.WinningTrades}/{performance.TotalTrades} win)");
                }
            }
        }

        result.CalculateSummary();
        return result;
    }

    /// <summary>
    /// MODALITÀ REAL-TIME: Esegue strategie con setup corrente
    /// </summary>
    public List<TradeSignal> ExecuteRealTime(OhlcvData[] data, DateTime currentDate)
    {
        var signals = new List<TradeSignal>();

        // Verifica se è weekend e serve nuova rotazione
        if (_scheduler.IsWeekend(currentDate) && _scheduler.NeedsRotation(currentDate))
        {
            Console.WriteLine($"\n[WEEKEND {currentDate:dd/MM/yyyy}] Esecuzione rotazione settimanale...");
            var setup = _scheduler.ExecuteWeeklyRotation(currentDate);
            setup.PrintReport();
        }

        // Ottiene setup corrente
        var currentSetup = _scheduler.GetCurrentSetup();
        
        Console.WriteLine($"\n[{currentDate:dd/MM/yyyy HH:mm}] Trading con setup settimana {currentSetup.Week}/{currentSetup.Year}");
        Console.WriteLine($"Strategie attive: {string.Join(", ", currentSetup.EnabledStrategies)}");

        // Esegue solo le strategie abilitate
        foreach (var strategy in _strategies.Where(s => currentSetup.EnabledStrategies.Contains(s.Name)))
        {
            var signal = Evaluate(strategy, data, currentDate);
            
            if (signal.Type != SignalType.Hold)
            {
                if (string.IsNullOrWhiteSpace(signal.Symbol))
                {
                    signal.Symbol = strategy.Symbol;
                }

                if (string.IsNullOrWhiteSpace(signal.StrategyCode))
                {
                    signal.StrategyCode = strategy.Name;
                }

                signals.Add(signal);
                Console.WriteLine($"  {signal.StrategyName}: {signal.Type} @ {signal.Price}");
                if (signal.CompanionSignals is not null)
                {
                    foreach (var companion in signal.CompanionSignals)
                    {
                        if (string.IsNullOrWhiteSpace(companion.Symbol))
                        {
                            companion.Symbol = strategy.Symbol;
                        }

                        if (string.IsNullOrWhiteSpace(companion.StrategyCode))
                        {
                            companion.StrategyCode = strategy.Name;
                        }

                        signals.Add(companion);
                        Console.WriteLine($"  {companion.StrategyName}: {companion.Type} @ {companion.Price}");
                    }
                }
            }
        }

        return signals;
    }

    private TradeSignal Evaluate(ITradingStrategy strategy, OhlcvData[] data, DateTime barTime)
    {
        var key = $"{strategy.Symbol}|{strategy.Name}";
        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = data,
            BarTimeUtc = barTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = strategy.Symbol,
                BarTimeUtc = barTime,
                RuntimeState = _strategyRuntimeStates.TryGetValue(key, out var state)
                    ? state
                    : new Dictionary<string, object?>(StringComparer.Ordinal)
            }
        });

        if (signal.RuntimeState is not null)
        {
            _strategyRuntimeStates[key] = signal.RuntimeState;
        }

        return signal;
    }

    /// <summary>
    /// Registra un trade completato per tracking performance
    /// </summary>
    public void RecordTrade(TradingResult trade)
    {
        if (!_weeklyResults.ContainsKey(trade.StrategyName))
        {
            _weeklyResults[trade.StrategyName] = new List<TradingResult>();
        }
        
        _weeklyResults[trade.StrategyName].Add(trade);
    }

    /// <summary>
    /// Finalizza la settimana e calcola performance (chiamato alla fine di ogni settimana)
    /// </summary>
    public void FinalizeWeek(DateTime weekEnd)
    {
        var weekStart = GetWeekStartDate(weekEnd);
        var week = GetWeekNumber(weekStart);
        var year = weekStart.Year;

        Console.WriteLine($"\n=== FINALIZZAZIONE SETTIMANA {week}/{year} ===\n");

        foreach (var (strategyName, trades) in _weeklyResults)
        {
            if (trades.Any())
            {
                var performance = _performanceCalculator.CalculatePerformance(
                    strategyName,
                    trades,
                    week,
                    year
                );

                _rotationManager.RecordWeeklyPerformance(performance);

                Console.WriteLine($"{strategyName}:");
                Console.WriteLine($"  Trades: {performance.TotalTrades}");
                Console.WriteLine($"  Net Profit: ${performance.NetProfit:F2} ({performance.NetProfitPercent:F2}%)");
                Console.WriteLine($"  Win Rate: {performance.WinRate:P2}");
                Console.WriteLine($"  Max DD: {performance.MaxDrawdown:P2}\n");
            }
        }

        // Pulisce i risultati per la settimana successiva
        _weeklyResults.Clear();
    }

    /// <summary>
    /// Forza una rotazione manuale (per testing o situazioni eccezionali)
    /// </summary>
    public WeeklySetup ForceRotation(DateTime date)
    {
        Console.WriteLine($"\n[ROTAZIONE FORZATA] {date:dd/MM/yyyy}");
        return _scheduler.ExecuteWeeklyRotation(date);
    }

    /// <summary>
    /// Ottiene il setup corrente
    /// </summary>
    public WeeklySetup GetCurrentSetup()
    {
        return _scheduler.GetCurrentSetup();
    }

    /// <summary>
    /// Ottiene lo storico dei setup
    /// </summary>
    public List<WeeklySetup> GetSetupHistory(int? lastNWeeks = null)
    {
        return _scheduler.GetSetupHistory(lastNWeeks);
    }

    /// <summary>
    /// Ottiene tutte le strategie registrate
    /// </summary>
    public IReadOnlyList<ITradingStrategy> GetStrategies()
    {
        return _strategies.AsReadOnly();
    }

    private DateTime GetWeekStartDate(DateTime date)
    {
        var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0) daysToSubtract += 7;
        return date.AddDays(-daysToSubtract).Date;
    }

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }
}
