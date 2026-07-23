using Piootoo.Shared.Models;

namespace Piootoo.Core;

/// <summary>
/// Calcola le metriche di performance dai risultati di trading
/// </summary>
public class PerformanceCalculator
{
    private readonly decimal _initialBalance;
    private readonly decimal _commissionPerTrade;

    public PerformanceCalculator(decimal initialBalance = 10000m, decimal commissionPerTrade = 0m)
    {
        _initialBalance = initialBalance;
        _commissionPerTrade = commissionPerTrade;
    }

    /// <summary>
    /// Calcola tutte le metriche di performance da una lista di TradingResult
    /// </summary>
    public StrategyPerformance CalculatePerformance(
        string strategyName, 
        List<TradingResult> results, 
        int week, 
        int year)
    {
        if (results.Count == 0)
        {
            return new StrategyPerformance
            {
                StrategyName = strategyName,
                Week = week,
                Year = year,
                InitialBalance = _initialBalance,
                FinalBalance = _initialBalance
            };
        }

        // Calcola equity curve e drawdown
        var equityCurve = CalculateEquityCurve(results);
        var drawdownData = CalculateDrawdown(equityCurve);
        
        // Separa trade vincenti e perdenti
        var winners = results.Where(r => r.IsWinner).ToList();
        var losers = results.Where(r => !r.IsWinner).ToList();

        // Calcola metriche base
        var totalReturn = equityCurve.Last().Balance - _initialBalance;
        var returnPercent = (_initialBalance != 0) ? (totalReturn / _initialBalance) * 100 : 0;
        
        var winRate = results.Count > 0 ? (decimal)winners.Count / results.Count : 0;
        var avgWin = winners.Any() ? winners.Average(w => w.NetProfit) : 0;
        var avgLoss = losers.Any() ? losers.Average(l => l.NetProfit) : 0;

        // Calcola volatilità e Sharpe
        var returns = results.Select(r => r.ReturnPercent).ToList();
        var volatility = CalculateStandardDeviation(returns);
        var sharpeRatio = CalculateSharpeRatio(returns, volatility);
        var sortinoRatio = CalculateSortinoRatio(returns);

        // Calcola perdite consecutive
        var (maxConsecutiveWins, maxConsecutiveLosses) = CalculateConsecutiveStats(results);

        return new StrategyPerformance
        {
            StrategyName = strategyName,
            Week = week,
            Year = year,
            
            // Rendimento
            Return = returnPercent,
            CumulativeReturn = returnPercent,
            
            // Rischio
            MaxDrawdown = drawdownData.MaxDrawdownPercent,
            MaxDrawdownValue = drawdownData.MaxDrawdownValue,
            Volatility = volatility,
            SharpeRatio = sharpeRatio,
            SortinoRatio = sortinoRatio,
            
            // Trading
            TotalTrades = results.Count,
            WinningTrades = winners.Count,
            LosingTrades = losers.Count,
            AverageWin = avgWin,
            AverageLoss = avgLoss,
            
            // Consistenza
            ConsecutiveWins = maxConsecutiveWins,
            ConsecutiveLosses = maxConsecutiveLosses,
            
            // Balance
            InitialBalance = _initialBalance,
            FinalBalance = equityCurve.Last().Balance,
            PeakBalance = equityCurve.Max(e => e.Balance),
            
            // Metriche aggiuntive
            MaxConsecutiveDrawdown = drawdownData.MaxConsecutiveDrawdown,
            DaysInMarket = (int)(results.Sum(r => r.Duration.TotalDays)),
            LargestWin = winners.Any() ? winners.Max(w => w.NetProfit) : 0,
            LargestLoss = losers.Any() ? losers.Min(l => l.NetProfit) : 0,
            
            // Equity curve per analisi dettagliate
            EquityCurve = equityCurve
        };
    }

    private List<EquityPoint> CalculateEquityCurve(List<TradingResult> results)
    {
        var equity = new List<EquityPoint>();
        var balance = _initialBalance;
        
        // Ordina per data di uscita
        var sortedResults = results.OrderBy(r => r.ExitDate).ToList();

        foreach (var trade in sortedResults)
        {
            balance += trade.NetProfit;
            equity.Add(new EquityPoint
            {
                Date = trade.ExitDate,
                Balance = balance,
                Trade = trade
            });
        }

        return equity;
    }

    private DrawdownData CalculateDrawdown(List<EquityPoint> equityCurve)
    {
        decimal maxDrawdown = 0;
        decimal maxDrawdownValue = 0;
        decimal peak = _initialBalance;
        int consecutiveDrawdown = 0;
        int maxConsecutiveDrawdown = 0;

        foreach (var point in equityCurve)
        {
            if (point.Balance > peak)
            {
                peak = point.Balance;
                consecutiveDrawdown = 0;
            }
            else
            {
                consecutiveDrawdown++;
                maxConsecutiveDrawdown = Math.Max(maxConsecutiveDrawdown, consecutiveDrawdown);
            }

            var drawdownValue = peak - point.Balance;
            var drawdownPercent = peak != 0 ? (drawdownValue / peak) : 0;

            if (drawdownPercent > maxDrawdown)
            {
                maxDrawdown = drawdownPercent;
                maxDrawdownValue = drawdownValue;
            }
        }

        return new DrawdownData
        {
            MaxDrawdownPercent = -maxDrawdown,
            MaxDrawdownValue = maxDrawdownValue,
            MaxConsecutiveDrawdown = maxConsecutiveDrawdown
        };
    }

    private decimal CalculateStandardDeviation(List<decimal> returns)
    {
        if (returns.Count < 2) return 0;

        var avg = returns.Average();
        var sumOfSquares = returns.Sum(r => (r - avg) * (r - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / (returns.Count - 1)));
    }

    private decimal CalculateSharpeRatio(List<decimal> returns, decimal volatility, decimal riskFreeRate = 0)
    {
        if (volatility == 0 || returns.Count == 0) return 0;
        
        var avgReturn = returns.Average();
        var excessReturn = avgReturn - riskFreeRate;
        
        // Annualizza assumendo ~52 settimane/anno
        var annualizedReturn = excessReturn * (decimal)Math.Sqrt(52);
        var annualizedVolatility = volatility * (decimal)Math.Sqrt(52);
        
        return annualizedVolatility != 0 ? annualizedReturn / annualizedVolatility : 0;
    }

    private decimal CalculateSortinoRatio(List<decimal> returns, decimal riskFreeRate = 0)
    {
        if (returns.Count < 2) return 0;

        var avgReturn = returns.Average();
        var negativeReturns = returns.Where(r => r < riskFreeRate).ToList();
        
        if (negativeReturns.Count == 0) return avgReturn > 0 ? 100 : 0;

        var downsideDeviation = CalculateStandardDeviation(negativeReturns);
        
        if (downsideDeviation == 0) return 0;
        
        return (avgReturn - riskFreeRate) / downsideDeviation;
    }

    private (int MaxWins, int MaxLosses) CalculateConsecutiveStats(List<TradingResult> results)
    {
        int currentWins = 0, maxWins = 0;
        int currentLosses = 0, maxLosses = 0;

        var sortedResults = results.OrderBy(r => r.ExitDate).ToList();

        foreach (var trade in sortedResults)
        {
            if (trade.IsWinner)
            {
                currentWins++;
                maxWins = Math.Max(maxWins, currentWins);
                currentLosses = 0;
            }
            else
            {
                currentLosses++;
                maxLosses = Math.Max(maxLosses, currentLosses);
                currentWins = 0;
            }
        }

        return (maxWins, maxLosses);
    }
}
