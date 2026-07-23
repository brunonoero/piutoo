using Piootoo.Shared.Models.Sapioo;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Servizio per l'ottimizzazione Sapioo
/// </summary>
public interface IPiootooSapiooService
{
    /// <summary>
    /// Avvia un job di ottimizzazione Sapioo
    /// </summary>
    string StartOptimization(SapiooRequest request);
    
    /// <summary>
    /// Ottiene lo stato di un job di ottimizzazione
    /// </summary>
    SapiooJob? GetJobStatus(string jobId);
    
    /// <summary>
    /// Ottiene il risultato completo di un'ottimizzazione completata
    /// </summary>
    SapiooResult? GetResult(string jobId);
    
    /// <summary>
    /// Ottiene la lista dei nomi dei backtesting disponibili per l'ottimizzazione
    /// </summary>
    List<string> GetAvailableBacktestings();
    
    /// <summary>
    /// Ottiene la lista di tutte le ottimizzazioni completate
    /// </summary>
    List<SapiooResult> GetCompletedOptimizations();
    
    /// <summary>
    /// Elimina un'ottimizzazione completata
    /// </summary>
    bool DeleteOptimization(string jobId);
}
