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

    public List<PlanDatafeedInstrumentDto> Instruments { get; set; } = new();
}
