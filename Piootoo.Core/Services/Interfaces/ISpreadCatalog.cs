using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Sa quali misure di spread esistono sotto <c>piootoo-repository/spread/</c> e cosa contengono.
/// Sta accanto a <see cref="IDatafeedCatalog"/> e per la stessa ragione: e' l'unico punto che
/// traduce un nome di broker in un percorso, e la console non apre le cartelle del repository.
/// </summary>
public interface ISpreadCatalog
{
    /// <summary>
    /// Broker con almeno un file <c>spread-by-symbol</c>, in ordine alfabetico. Elenco vuoto =
    /// nessuna misura ancora raccolta: e' un'assenza legittima, non un errore — un run senza spread
    /// e' un run valido.
    /// </summary>
    IReadOnlyList<SpreadBrokerInfo> GetBrokers();

    /// <summary>
    /// Gli spread che un run applicherebbe con questa combinazione, letti adesso dal disco. E' il
    /// preventivo che la schermata mostra prima di lanciare.
    /// </summary>
    /// <exception cref="ArgumentException">Il nome del broker non e' un nome di cartella semplice.</exception>
    /// <exception cref="DirectoryNotFoundException">Il broker non ha una cartella di misura.</exception>
    /// <exception cref="FileNotFoundException">Manca il CSV, o il gemello per ora quando si chiede <c>PerHour</c>.</exception>
    /// <exception cref="InvalidDataException">Il file non ha le colonne attese, o porta valori non validi.</exception>
    SpreadTableInfo GetTable(string broker, SpreadStatistic statistic, SpreadResolution resolution);
}
