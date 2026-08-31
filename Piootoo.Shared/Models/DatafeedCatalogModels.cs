namespace Piootoo.Shared.Models;

/// <summary>
/// Un archivio di barre esterno, cioe' una sottocartella di <c>piootoo-repository/datafeed-external</c>.
///
/// <para>L'esterno e' organizzato <b>per broker</b> perche' le barre non sono un dato neutro: due
/// broker sullo stesso future chiudono le candele su prezzi diversi, e mescolarle produrrebbe un
/// backtest che non corrisponde a nessun conto reale. La cartella e' quindi la chiave, e il nome
/// del broker viaggia con la richiesta di backtest fino al summary.</para>
///
/// <para>Il datafeed interno (<c>datafeed/</c>) non compare in questo elenco: e' l'assenza di
/// broker, non un broker chiamato "interno".</para>
/// </summary>
public sealed class DatafeedBrokerInfo
{
    /// <summary>Nome della cartella sotto <c>datafeed-external</c>, es. <c>RAWTRADINGLTD</c>.</summary>
    public required string Broker { get; init; }

    /// <summary>Simboli distinti per cui esiste almeno un file piatto <c>@SYM_{minuti}.json</c>.</summary>
    public int SymbolCount { get; init; }

    /// <summary>Coppie (simbolo, timeframe) disponibili.</summary>
    public int FeedCount { get; init; }

    /// <summary>
    /// Ultima scrittura fra i file del broker. E' il tempo del filesystem, non l'ultima barra:
    /// serve a capire a colpo d'occhio se l'archivio e' fermo, non a datare il feed.
    /// </summary>
    public DateTime? LastWriteUtc { get; init; }
}
