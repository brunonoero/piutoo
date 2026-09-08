namespace Piootoo.Shared.Models.Datafeed;

/// <summary>
/// Contratti dell'ingestione datafeed esterno: un cBot raccoglitore spinge barre e tick a pezzi,
/// il server li accoda, deduplica e compatta in <c>datafeed-external/@SYM_{minuti}.json</c>.
///
/// <para><b>Perche' a pezzi.</b> Lo storico di un simbolo e' decine di migliaia di barre e il
/// broker le consegna a blocchi: una sola chiamata che le carichi tutte va in timeout, e se va in
/// timeframe a meta' non lascia niente di riutilizzabile. Qui ogni blocco e' un'unita' autonoma —
/// idempotente per costruzione, perche' la chiave e' l'istante della barra — quindi il feed si puo'
/// completare in cento invii, in ordine qualsiasi, su piu' sessioni.</para>
/// </summary>
public sealed class ExternalCandleDto
{
    /// <summary>Istante di APERTURA della barra, in UTC. Kind != Utc viene rifiutato.</summary>
    public DateTime DateTime { get; set; }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

/// <summary>Un blocco di barre di UNO stream (simbolo + timeframe). L'unita' di invio.</summary>
public sealed class ExternalBarChunkDto
{
    /// <summary>
    /// Codice del broker che ha prodotto queste barre (es. <c>ICMARKETS</c>). E' la sottocartella
    /// in cui il feed viene scritto, ed e' obbligatorio: barre dello stesso simbolo prese da due
    /// broker diversi NON sono la stessa serie — cambiano sessione, bucket e volume — e mescolarle
    /// in un unico file produce un feed che non corrisponde a nessuno dei due.
    /// </summary>
    public string Broker { get; set; } = string.Empty;

    /// <summary>Simbolo Piootoo, con o senza "@" (viene normalizzato a <c>@NQ</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    /// <summary>Chi ha raccolto le barre (nome bot, broker, account). Finisce nel manifest.</summary>
    public string? Source { get; set; }

    /// <summary>
    /// Etichetta libera del blocco (es. <c>@NQ_60_20240101-20240108</c>). Serve solo ai log: la
    /// deduplica non la usa, perche' la chiave e' l'istante della barra.
    /// </summary>
    public string? ChunkId { get; set; }

    public List<ExternalCandleDto> Candles { get; set; } = new();
}

public sealed class IngestBarsRequestDto
{
    public List<ExternalBarChunkDto> Chunks { get; set; } = new();

    /// <summary>
    /// Forza la compattazione del journal nel file piatto alla fine dell'invio. Di norma il server
    /// decide da solo (soglia sul journal); il bot lo chiede quando ha finito il backfill di uno
    /// stream, cosi' il file su disco e' subito quello definitivo.
    /// </summary>
    public bool Compact { get; set; }
}

/// <summary>Esito dell'ingestione di uno stream: quanto e' entrato, quanto era gia' li'.</summary>
public sealed class ExternalStreamIngestResultDto
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }

    /// <summary>Barre arrivate nel blocco.</summary>
    public int Received { get; set; }

    /// <summary>Barre nuove, mai viste prima per quell'istante.</summary>
    public int Accepted { get; set; }

    /// <summary>Barre gia' presenti con valori diversi: l'ultima arrivata vince.</summary>
    public int Updated { get; set; }

    /// <summary>Barre gia' presenti identiche: la sovrapposizione fra due blocchi.</summary>
    public int Duplicates { get; set; }

    /// <summary>Barre scartate perche' malformate.</summary>
    public int Rejected { get; set; }

    /// <summary>Le prime ragioni di scarto, per capire il perche' senza aprire i log.</summary>
    public List<string> RejectReasons { get; set; } = new();

    /// <summary>Barre in attesa nel journal, non ancora compattate nel file piatto.</summary>
    public int PendingJournalCandles { get; set; }

    /// <summary>true se questo invio ha innescato la compattazione.</summary>
    public bool Compacted { get; set; }

    /// <summary>Copertura dello stream dopo l'invio (valorizzata solo se si e' compattato).</summary>
    public ExternalFeedCoverageDto? Coverage { get; set; }
}

public sealed class IngestBarsResponseDto
{
    public List<ExternalStreamIngestResultDto> Streams { get; set; } = new();
    public int TotalAccepted { get; set; }
    public int TotalDuplicates { get; set; }
    public int TotalRejected { get; set; }
}

/// <summary>Riassunto di copertura di uno stream: quanto c'e' e da quando a quando.</summary>
public sealed class ExternalFeedCoverageDto
{
    public int TotalCandles { get; set; }
    public DateTime? FirstCandleUtc { get; set; }
    public DateTime? LastCandleUtc { get; set; }

    /// <summary>
    /// Passo osservato piu' frequente fra due barre consecutive. Non e' il timeframe dichiarato:
    /// e' quello che i dati mostrano davvero, ed e' la base per dire cos'e' un buco. Un feed
    /// giornaliero di broker apre alle 22:00 o alle 23:00 UTC, non a mezzanotte: dedurre il passo
    /// dai dati invece di assumerlo evita di dichiarare buchi che non esistono.
    /// </summary>
    public int? DominantStepMinutes { get; set; }
}

/// <summary>Un buco nella serie: fra queste due barre manca del tempo.</summary>
public sealed class ExternalFeedGapDto
{
    /// <summary>Istante dell'ultima barra prima del buco.</summary>
    public DateTime FromUtc { get; set; }

    /// <summary>Istante della prima barra dopo il buco.</summary>
    public DateTime ToUtc { get; set; }

    public int MinutesMissing { get; set; }

    /// <summary>Quante barre mancherebbero al passo dominante. Stima, non verita'.</summary>
    public int EstimatedMissingCandles { get; set; }

    /// <summary>
    /// true se il buco contiene un sabato o una domenica: quasi sempre e' la chiusura del mercato,
    /// non un pezzo di storia mancante. Il bot lo usa per non richiedere all'infinito un periodo
    /// che il broker non ha.
    /// </summary>
    public bool SpansWeekend { get; set; }
}

public sealed class ExternalFeedStatusDto
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExternalFeedCoverageDto Coverage { get; set; } = new();
    public DateTime? LastUpdateUtc { get; set; }
    public string? Source { get; set; }
    public int PendingJournalCandles { get; set; }

    /// <summary>Soglia oltre la quale un salto fra due barre e' stato considerato un buco.</summary>
    public int GapToleranceMinutes { get; set; }

    public int GapCount { get; set; }

    /// <summary>I buchi piu' grandi, ordinati per durata. Troncati a un tetto ragionevole.</summary>
    public List<ExternalFeedGapDto> Gaps { get; set; } = new();

    /// <summary>true se <see cref="Gaps"/> e' stato troncato.</summary>
    public bool GapsTruncated { get; set; }
}

public sealed class ExternalFeedIndexDto
{
    public string RootPath { get; set; } = string.Empty;
    public List<ExternalFeedStatusDto> Feeds { get; set; } = new();
}

public sealed class ExternalTickDto
{
    /// <summary>Istante del tick, in UTC.</summary>
    public DateTime TimeUtc { get; set; }

    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
}

public sealed class IngestTicksRequestDto
{
    /// <summary>Codice broker: stessa regola delle barre, i tick finiscono nella sua cartella.</summary>
    public string Broker { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ChunkId { get; set; }
    public List<ExternalTickDto> Ticks { get; set; } = new();
}

public sealed class IngestTicksResponseDto
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Received { get; set; }
    public int Accepted { get; set; }

    /// <summary>Tick scartati perche' non piu' recenti dell'ultimo scritto: la sovrapposizione.</summary>
    public int Stale { get; set; }

    public int Rejected { get; set; }
    public List<string> RejectReasons { get; set; } = new();

    /// <summary>Ultimo tick memorizzato: il punto da cui il bot puo' riprendere.</summary>
    public DateTime? LastTickUtc { get; set; }

    /// <summary>File giornalieri toccati da questo invio.</summary>
    public List<string> Files { get; set; } = new();
}

public sealed class CompactExternalFeedsResponseDto
{
    public List<ExternalStreamIngestResultDto> Streams { get; set; } = new();
}

/// <summary>
/// Uno strumento del piano visto da un raccoglitore di datafeed: il simbolo Piootoo, come si chiama
/// sul conto che raccoglie, e i timeframe che il piano usa davvero.
/// </summary>
public sealed class PlanDatafeedInstrumentDto
{
    /// <summary>Simbolo Piootoo (<c>@NQ</c>): la chiave con cui il feed viene salvato.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Lo stesso strumento sul conto che esegue: il nome che il bot deve chiedere al broker. Viene
    /// dalla tabella di conversione dell'account, quindi il bot non deve mappare niente a mano.
    /// Coincide con <see cref="Symbol"/> quando l'account non mappa quel simbolo.
    /// </summary>
    public string AccountSymbol { get; set; } = string.Empty;

    public List<int> TimeframesMinutes { get; set; } = new();
}

/// <summary>
/// Gli strumenti che un piano tocca, per chi deve raccoglierne il datafeed.
///
/// <para><b>Vengono dal masterfilter</b>, ed e' la differenza che conta: le strategie attive
/// possono cambiare nel tempo, ma il datafeed di uno strumento serve <i>sempre</i> — anche mentre
/// e' spento, perche' quando torna attivo la sua storia deve esserci gia'. Seguendo le strategie
/// accese, il feed si interromperebbe a ogni pausa e lascerebbe un buco lungo quanto la pausa.</para>
/// </summary>
public sealed class PlanDatafeedInstrumentsDto
{
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Conto di cui si e' usata la tabella di conversione per <c>AccountSymbol</c>.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// La cartella di <c>datafeed-external/</c> in cui va il feed raccolto per questo conto: il
    /// <c>DatafeedFolder</c> del broker se c'e', altrimenti il suo <c>Code</c>.
    ///
    /// <para><b>Perche' lo dice il server.</b> E' lo stesso nome con cui il server poi rilegge
    /// l'archivio — riscaldamento di sessione, sorgente dei prezzi di un run — quindi deve essere
    /// deciso una volta sola. Un raccoglitore che se lo costruisce da <c>Account.BrokerName</c>
    /// produce un nome diverso ("FTMO Platform" diventa FTMOPLATFORM mentre il registro dice FTMO),
    /// e nessuno se ne accorge finche' non ci sono due archivi a meta' per lo stesso broker: e'
    /// successo l'08/09/2026. Vuoto quando il conto non ha un broker: in quel caso il raccoglitore
    /// non ha una cartella in cui scrivere e non parte.</para>
    /// </summary>
    public string DatafeedBroker { get; set; } = string.Empty;

    public List<PlanDatafeedInstrumentDto> Instruments { get; set; } = new();
}

/// <summary>
/// Esito della ricostruzione di uno stream aggregato a partire dalle sue barre da un minuto.
///
/// <para><b>A cosa serve.</b> Il minuto e' il dato autorevole; tutto cio' che sta sopra e' derivato
/// e rigenerabile. Serve quando un aggregato e' nato su una griglia sbagliata — o su due griglie
/// insieme, che e' il difetto che la deduplica per istante di apertura rende inevitabile quando due
/// raccolte usano ancoraggi diversi: le etichette non si sovrascrivono, si sommano. Vedi
/// <c>docs/domini/layer-barre-e-calendario.md</c> §7bis.</para>
/// </summary>
public sealed class RebuildStreamResultDto
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }

    /// <summary>Il file e' stato riscritto.</summary>
    public bool Rebuilt { get; set; }

    /// <summary>Perche' lo stream e' stato saltato. Nullo quando <see cref="Rebuilt"/> e' vero.</summary>
    public string? Skipped { get; set; }

    /// <summary>Barre da un minuto lette come sorgente.</summary>
    public int MinuteBars { get; set; }

    /// <summary>Barre nel file prima della ricostruzione.</summary>
    public int BarsBefore { get; set; }

    /// <summary>Barre nel file dopo.</summary>
    public int BarsAfter { get; set; }

    /// <summary>
    /// Quante barre del file precedente <b>non</b> stavano su un confine di bucket della griglia
    /// dichiarata dal simbolo. E' la misura diretta del difetto che la ricostruzione ripara: se e'
    /// zero il file era gia' sano e la differenza viene solo dalla copertura del minuto.
    /// </summary>
    public int OffGridBefore { get; set; }

    /// <summary>
    /// Bucket scartati perche' l'input non ne copriva tutto l'arco: il primo, troncato dall'inizio
    /// del journal a un minuto, e l'ultimo, ancora in formazione. Un bucket a meta' nel feed
    /// sarebbe un dato falso che poi nessuno distingue da uno vero.
    /// </summary>
    public int IncompleteDropped { get; set; }

    /// <summary>La griglia dichiarata, come finisce nel campo <c>source</c> del file.</summary>
    public string Grid { get; set; } = string.Empty;

    public ExternalFeedCoverageDto? Coverage { get; set; }
}

/// <summary>Esito complessivo di una ricostruzione dal minuto.</summary>
public sealed class RebuildFromMinutesResponseDto
{
    public List<RebuildStreamResultDto> Streams { get; set; } = new();

    public int RebuiltCount => Streams.Count(stream => stream.Rebuilt);
    public int SkippedCount => Streams.Count(stream => !stream.Rebuilt);
}

/// <summary>
/// Riscaldamento di uno stream letto dall'archivio del broker: la storia che il server si carica da
/// solo all'apertura di una sessione <c>ExternalBroker</c>, invece di aspettare che il client gliela
/// spinga barra per barra.
///
/// <para><b>Perche' esiste.</b> Il problema e' aritmetico: <c>PTS_NQ_VBO_002_240</c> chiede 606
/// barre a 240 minuti, cioe' 145.440 barre da un minuto. Spedirle dal client all'avvio ricrea
/// esattamente il problema che il journal a blocchi del raccoglitore ha gia' risolto. Il disco tiene
/// la storia, il client porta la coda.</para>
///
/// <para>La sorgente autorevole e' sempre <c>@SYM_1.json</c>: gli aggregati su disco sono cache, e
/// riscaldarsi da una cache sarebbe riscaldarsi dal derivato. Sopra il minuto l'aggregazione passa
/// dallo stesso <c>BarAggregator</c> che costruisce il feed, quindi la storia con cui la sessione
/// parte e quella su cui gira sono sulla stessa griglia per costruzione.</para>
/// </summary>
public sealed class WarmUpSeriesDto
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }

    /// <summary>Barre da un minuto lette dall'archivio, journal compreso.</summary>
    public int MinuteBars { get; set; }

    /// <summary>Bucket completi che l'archivio poteva offrire, prima del taglio a quante ne servono.</summary>
    public int AvailableBars { get; set; }

    /// <summary>Le candele consegnate, in ordine cronologico. Vuoto quando <see cref="Skipped"/> parla.</summary>
    public List<Piootoo.Shared.Models.OhlcvData> Candles { get; set; } = new();

    /// <summary>Apertura dell'ultima candela consegnata: e' da li' che il client deve continuare.</summary>
    public DateTime? LastBarUtc { get; set; }

    /// <summary>
    /// Perche' non c'e' riscaldamento, in parole. Null = e' andato bene. Non e' un errore di per se':
    /// il client ha ancora la propria strada, ma la sessione deve poterlo <b>dire</b> invece di
    /// partire muta.
    /// </summary>
    public string? Skipped { get; set; }

    /// <summary>La griglia con cui e' stato costruito, nella stessa forma del campo <c>source</c> del feed.</summary>
    public string Grid { get; set; } = string.Empty;
}
