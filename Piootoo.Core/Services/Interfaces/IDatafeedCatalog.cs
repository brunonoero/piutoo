using Piootoo.Shared.Models;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Sa quali archivi di barre esistono e dove stanno. E' l'unico punto che traduce "broker" in un
/// path: chi legge candele riceve gia' una radice risolta, e un nome di broker che arriva da un
/// client non puo' diventare un percorso arbitrario.
/// </summary>
public interface IDatafeedCatalog
{
    /// <summary>Broker disponibili sotto la radice esterna, in ordine alfabetico.</summary>
    IReadOnlyList<DatafeedBrokerInfo> GetBrokers();

    /// <summary>
    /// Radice da cui leggere le barre. <paramref name="broker"/> null o vuoto = datafeed interno.
    /// </summary>
    /// <exception cref="ArgumentException">Il nome del broker non e' un nome di cartella semplice.</exception>
    /// <exception cref="DirectoryNotFoundException">Il broker indicato non esiste.</exception>
    string ResolveRoot(string? broker);

    /// <summary>Etichetta leggibile della sorgente, per log e artefatti.</summary>
    string Describe(string? broker);
}
