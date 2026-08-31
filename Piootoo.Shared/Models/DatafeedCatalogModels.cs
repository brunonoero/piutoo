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

/// <summary>
/// Un singolo feed a barre, cioe' un file piatto <c>@SYM_{minuti}.json</c> dentro un archivio.
///
/// <para>Serve a rispondere alla domanda che si fa prima di ogni backtest: <b>quali coppie
/// (simbolo, timeframe) esistono, e che periodo coprono</b>. Un run che chiede date fuori dal
/// range di un feed non fallisce, produce semplicemente meno barre del previsto: vedere il
/// range prima costa meno che diagnosticarlo dopo dal summary.</para>
///
/// <para>Il range e' ricavato dalla <b>prima e dall'ultima barra del file</b>, non dal
/// filesystem: <see cref="LastWriteUtc"/> dice quando l'archivio e' stato toccato, non fin dove
/// arrivano i dati, e sui feed esterni i due valori divergono di giorni.</para>
/// </summary>
public sealed class DatafeedFeedInfo
{
    /// <summary>Broker dell'archivio esterno, null per il datafeed interno.</summary>
    public string? Broker { get; init; }

    /// <summary>Etichetta della sorgente come compare negli artefatti: <c>interno</c> o <c>esterno/{BROKER}</c>.</summary>
    public required string Source { get; init; }

    /// <summary>Simbolo con il prefisso, es. <c>@NQ</c>.</summary>
    public required string Symbol { get; init; }

    /// <summary>Timeframe in minuti, la stessa unita' del nome file.</summary>
    public int TimeframeMinutes { get; init; }

    /// <summary>Prima barra del file, gia' convertita a UTC vero con l'orologio dichiarato dal feed.</summary>
    public DateTime? FirstBarUtc { get; init; }

    /// <summary>Ultima barra del file, stessa conversione di <see cref="FirstBarUtc"/>.</summary>
    public DateTime? LastBarUtc { get; init; }

    /// <summary>
    /// Numero di barre dichiarato dal file (<c>candleCount</c>). Null se il file non lo dichiara:
    /// contarle davvero vorrebbe dire leggere l'intero archivio per popolare un elenco.
    /// </summary>
    public int? CandleCount { get; init; }

    /// <summary>
    /// Fuso in cui sono stampati i timestamp del file, da <c>feed-clocks.json</c>. Null quando il
    /// feed non lo dichiara: in quel caso il range qui sopra e' l'etichetta grezza del file, non
    /// un istante vero, e un backtest su quel simbolo si rifiuterebbe di partire.
    /// </summary>
    public string? FeedClock { get; init; }

    /// <summary>Ultima scrittura del file. Tempo del filesystem, non del feed.</summary>
    public DateTime LastWriteUtc { get; init; }

    /// <summary>Dimensione del file in byte.</summary>
    public long SizeBytes { get; init; }

    /// <summary>Perche' il range non e' stato letto, quando non lo e'. Null se tutto e' andato bene.</summary>
    public string? Problem { get; init; }
}
