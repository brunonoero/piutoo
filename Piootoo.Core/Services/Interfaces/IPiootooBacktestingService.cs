using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Servizio per l'esecuzione del backtesting
/// </summary>
public interface IPiootooBacktestingService
{
    /// <summary>
    /// Avvia un job di backtesting
    /// </summary>
    string StartBacktesting(BacktestingRequest request);
    
    /// <summary>
    /// Ottiene lo stato di un job di backtesting
    /// </summary>
    BacktestingJob? GetJobStatus(string jobId);

    /// <summary>Richiede in modo idempotente la cancellazione di un job.</summary>
    BacktestingJob? CancelBacktesting(string jobId);
    
    /// <summary>
    /// Ottiene il risultato completo di un backtesting completato
    /// </summary>
    BacktestingResult? GetResult(string jobId);
    
    /// <summary>
    /// Ottiene la lista di tutti i backtesting completati
    /// </summary>
    List<BacktestingResult> GetCompletedBacktestings();

    /// <summary>
    /// Ottiene una lista leggera dei backtesting completati senza caricare le serie complete.
    /// </summary>
    List<BacktestingResult> GetCompletedBacktestingSummaries();
    
    /// <summary>
    /// Elimina un backtesting completato
    /// </summary>
    bool DeleteBacktesting(string jobId);
}
