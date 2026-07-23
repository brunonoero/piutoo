namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Stato di un job di backtesting
/// </summary>
public enum BacktestingJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
