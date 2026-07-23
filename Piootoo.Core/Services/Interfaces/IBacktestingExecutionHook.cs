namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Seam osservabile per coordinare l'avvio effettivo di un job.
/// L'implementazione di produzione non introduce attese.
/// </summary>
public interface IBacktestingExecutionHook
{
    Task OnJobRunningAsync(string jobId, CancellationToken cancellationToken);
}

public sealed class NoOpBacktestingExecutionHook : IBacktestingExecutionHook
{
    public Task OnJobRunningAsync(string jobId, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
