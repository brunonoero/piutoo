using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

public static class SessionStateSchema
{
    public const int Version = 1;
    public const string FileName = "session-state.json";
}

/// <summary>
/// Lo stato di una sessione realtime che il client non può ricostruire da sé, scritto accanto a
/// <c>signals.json</c> perché il processo server possa riprendere la sessione dopo un riavvio
/// invece di aprirne una nuova.
///
/// <para><b>Cosa non c'è, e non è una dimenticanza.</b> Non c'è la storia delle candele: in
/// <c>ExternalBroker</c> è del client, vive in RAM per progetto, e reidratarne una copia stantia
/// sarebbe peggio di non averla — il server si crederebbe caldo e valuterebbe su barre vecchie.
/// La rimanda il cBot con il proprio riscaldamento. Non c'è la configurazione del run (strategie,
/// sizing, holding, moltiplicatore): si ricava dal piano, che è la sua fonte autorevole; qui resta
/// solo l'impronta con cui verificare che quel piano non sia cambiato nel frattempo. Non ci sono i
/// trade chiusi: stanno già in <c>trades.json</c>.</para>
///
/// <para><b>Cosa c'è.</b> Identità (il <see cref="SessionId"/> e il <see cref="SessionToken"/> sono
/// il pezzo che vale di più: il file di stato locale del cBot è ancorato al session id, e senza di
/// essi il bot butta via break-even, trailing e uscite a tempo di ogni posizione aperta), ordini
/// ancora in volo, posizioni aperte, contatori di rischio e chiavi di deduplica recenti.</para>
///
/// <para>Regole complete in <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c>.</para>
/// </summary>
public sealed class SessionStateFile
{
    public int SchemaVersion { get; init; } = SessionStateSchema.Version;

    public required string SessionId { get; init; }

    public required string SessionToken { get; init; }

    public required string WorkspaceId { get; init; }

    /// <summary>Il piano da cui la sessione si ricostruisce. Senza piano non c'è ripresa.</summary>
    public required string PlanCode { get; init; }

    /// <summary>La execution key dichiarata dal cBot, non la chiave composta dell'indice.</summary>
    public required string ExecutionKey { get; init; }

    /// <summary>
    /// La chiave con cui la sessione è indicizzata in <c>_planExecutions</c>. Si salva già composta
    /// perché è ciò su cui il cBot si riaggancia con <c>open-plan</c>: ricomporla al riavvio
    /// significherebbe rifare — e dover tenere allineata — la stessa concatenazione in due punti.
    /// </summary>
    public required string ExecutionIndexKey { get; init; }

    public ExecutionMode ExecutionMode { get; init; }

    public ClientRunMode ClientRunMode { get; init; }

    public TradingRunProfile RunProfile { get; init; }

    public TradingSessionStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime SavedAtUtc { get; init; }

    /// <summary>Conto che esegue direttamente; vuoto nelle sessioni distribuite.</summary>
    public string DirectAccountNumber { get; init; } = string.Empty;

    /// <summary>Conti che hanno aperto questa sessione con <c>open-plan</c>.</summary>
    public IReadOnlyList<string> JoinedAccounts { get; init; } = [];

    /// <summary>Vero se la sessione aveva i gruppi configurati, cioè se distribuisce.</summary>
    public bool Distributed { get; init; }

    /// <summary>
    /// Impronta della configurazione sotto cui le posizioni aperte sono nate: codici strategia
    /// risolti e policy di holding del conto. Se al riavvio il piano non produce la stessa
    /// impronta la sessione <b>non</b> si riprende — sorvegliare una posizione con regole diverse
    /// da quelle con cui è stata aperta non dà errore, dà un'uscita a un orario che nessuno ha
    /// deciso.
    /// </summary>
    public required string ConfigurationFingerprint { get; init; }

    // --- Ordini e posizioni ------------------------------------------------------------------

    /// <summary>
    /// Gli intent ancora capaci di cambiare, più quelli referenziati da una posizione aperta.
    /// Non è la storia della sessione — quella sta in <c>signals.json</c> — ma il minimo per
    /// sapere cosa c'è a mercato e con quale specifica di uscita.
    /// </summary>
    public IReadOnlyList<OrderIntent> Intents { get; init; } = [];

    /// <summary>Template di ingresso non ancora reclamati (solo sessioni distribuite).</summary>
    public IReadOnlyList<OrderIntent> EntryTemplates { get; init; } = [];

    public IReadOnlyList<SessionStatePosition> Positions { get; init; } = [];

    /// <summary>Template già consegnati a un gruppo: <c>IntentId</c> → gruppi che ne hanno copia.</summary>
    public IReadOnlyDictionary<string, List<string>> TemplateClaimedGroups { get; init; } =
        new Dictionary<string, List<string>>();

    /// <summary>Slot (gruppo, strategia, simbolo, direzione) occupati: chiave → conto e intent.</summary>
    public IReadOnlyDictionary<string, SessionStateSlot> GroupStrategySlots { get; init; } =
        new Dictionary<string, SessionStateSlot>();

    // --- Contatori ---------------------------------------------------------------------------

    public int Entries { get; init; }

    public int Fills { get; init; }

    /// <summary>Progressivo degli IntentId: non riparte da zero, o due intent avrebbero lo stesso id.</summary>
    public int IntentSequence { get; init; }

    public decimal PeakEquity { get; init; }

    public DateTime? FirstBarUtc { get; init; }

    public DateTime? LastBarUtc { get; init; }

    public DateTime? LastEvaluatedBarTimeUtc { get; init; }

    /// <summary>PnL netto per strategia: è la priorità di consegna dei template.</summary>
    public IReadOnlyDictionary<string, decimal> StrategyNetPnl { get; init; } =
        new Dictionary<string, decimal>();

    /// <summary>
    /// Fill di ingresso per (strategia|simbolo, giorno, conto). Perderli azzera
    /// <c>MaxEntriesPerSession</c> a metà giornata, cioè fa riaprire trade che il limite aveva già
    /// escluso — l'unica perdita che fa <i>aprire</i> qualcosa invece di ometterlo.
    /// </summary>
    public IReadOnlyList<SessionStateEntryFill> EntryFills { get; init; } = [];

    /// <summary>Ultima sequence accettata per stream.</summary>
    public IReadOnlyDictionary<string, long> LastSequence { get; init; } = new Dictionary<string, long>();

    /// <summary>Massimo storico di barre per stream: senza, la diagnosi "mai valutata" mente.</summary>
    public IReadOnlyDictionary<string, int> HistoryHighWater { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Id degli execution report già applicati. Si salvano tutti: crescono con gli eventi del
    /// broker — qualche centinaio al giorno — non con le barre, e perderli significa riapplicare
    /// un fill che un client riprova dopo il riavvio, cioè contare due volte un ingresso.
    ///
    /// <para>Le chiavi di idempotenza delle <b>barre</b> non ci sono, di proposito: crescono per
    /// barra e per stream senza limite, e a proteggere dalla rivalutazione basta già
    /// <see cref="LastSequence"/>, che è per stream e non per barra.</para>
    /// </summary>
    public IReadOnlyList<string> ReportIds { get; init; } = [];
}

/// <summary>Posizione aperta, con la chiave con cui la sessione la indicizza.</summary>
public sealed class SessionStatePosition
{
    /// <summary>Chiave di <c>ExternalPositions</c>: <c>simbolo|strategia</c>, col conto davanti in multi-account.</summary>
    public required string Key { get; init; }

    public required TradingPositionSnapshot Snapshot { get; init; }

    public DateTime EntryTimeUtc { get; init; }

    public string IntentId { get; init; } = string.Empty;

    public decimal? StopLoss { get; init; }

    public decimal? TakeProfit { get; init; }

    /// <summary>Il broker l'ha confermata almeno una volta nei propri snapshot di poll.</summary>
    public bool BrokerConfirmed { get; init; }

    // La posizione "canonica" e il conteggio dei detentori per strategia non si salvano: sono
    // derivabili da queste righe (una canonica per simbolo|strategia, il conteggio è quanti conti
    // la detengono) e salvarli significherebbe poter reidratare due verità che si contraddicono.
}

/// <summary>Esito della ripresa di una singola cartella di sessione, per il log di avvio.</summary>
public sealed record SessionRestoreOutcome(string SessionId, string PlanCode, bool Restored, string Reason);

/// <summary>Occupazione di uno slot di gruppo.</summary>
public sealed class SessionStateSlot
{
    public required string AccountNumber { get; init; }

    public required string IntentId { get; init; }
}

/// <summary>Una riga del conteggio dei fill di ingresso.</summary>
public sealed class SessionStateEntryFill
{
    /// <summary>Chiave <c>strategia|simbolo</c>.</summary>
    public required string StrategyKey { get; init; }

    /// <summary>Inizio della sessione di trading a cui il conteggio si riferisce.</summary>
    public DateTime SessionStartUtc { get; init; }

    /// <summary>Conto, oppure <c>*</c> per il totale su tutti i conti.</summary>
    public required string AccountNumber { get; init; }

    public int Count { get; init; }
}
