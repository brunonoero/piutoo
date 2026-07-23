using Piootoo.Shared.Models.Settings;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Servizio per la gestione dei settings Piootoo
/// </summary>
public interface IPiootooSettingsService
{
    /// <summary>
    /// Ottiene l'elenco dei simboli disponibili da appsettings
    /// </summary>
    List<string> GetAvailableSymbols();
    
    /// <summary>
    /// Ottiene tutti i setup salvati
    /// </summary>
    List<PiootooSetup> GetAllSetups();
    
    /// <summary>
    /// Ottiene un setup per ID
    /// </summary>
    PiootooSetup? GetSetupById(string id);
    
    /// <summary>
    /// Crea un nuovo setup
    /// </summary>
    PiootooSetup CreateSetup(PiootooSetup setup);
    
    /// <summary>
    /// Aggiorna un setup esistente
    /// </summary>
    PiootooSetup UpdateSetup(PiootooSetup setup);
    
    /// <summary>
    /// Elimina un setup
    /// </summary>
    bool DeleteSetup(string id);
}
