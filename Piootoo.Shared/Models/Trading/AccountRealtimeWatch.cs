using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Gravità di un rilievo del presidio. Serve a ordinare, non a decorare: la console mostra prima
/// ciò che chiede un intervento a mano su cTrader.
/// </summary>
public enum RealtimeWatchSeverity
{
    /// <summary>Niente da fare.</summary>
    Ok,

    /// <summary>Da guardare: il server potrebbe non essere allineato, ma nessuna posizione è scoperta.</summary>
    Attenzione,

    /// <summary>Aprire cTrader: c'è una posizione o un ordine che il server non sta più governando.</summary>
    Intervento
}

/// <summary>
/// Cosa il presidio ha rilevato. I nomi sono stabili: la console ci appende testo, non li traduce.
/// Vedi <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c> §8 per la tabella completa.
/// </summary>
public enum RealtimeWatchFinding
{
    /// <summary>Sessione viva, flusso di barre recente, nessuna anomalia.</summary>
    Presidiata,

    /// <summary>Nessun piano nomina questo conto: non c'è niente da presidiare.</summary>
    NessunPianoPerIlConto,

    /// <summary>Il conto è in un piano ma non ha nessuna sessione realtime viva sul server.</summary>
    SessioneAssente,

    /// <summary>La sessione esiste ma non è in esecuzione, e ha posizioni aperte.</summary>
    SessioneNonInEsecuzione,

    /// <summary>Il <c>CloseAtUtc</c> dell'intent è passato e per il server la posizione è ancora aperta.</summary>
    ChiusuraAttesaNonAvvenuta,

    /// <summary>La posizione supera il flat di sessione o di fine settimana che il piano impone.</summary>
    OltreIlFlatDiConto,

    /// <summary>Nessuna barra chiusa da un multiplo del timeframe più fitto della sessione.</summary>
    FlussoFermo,

    /// <summary>Intent ancora <c>Pending</c> oltre la barra su cui era valido, senza execution report.</summary>
    PendingScaduto,

    /// <summary>
    /// Sessione in esecuzione diretta: il client non manda mai lo stato del broker, quindi ciò che
    /// il server crede non è mai stato verificato contro cTrader.
    /// </summary>
    StatoBrokerMaiVerificato,

    /// <summary>Posizione che il broker non ha mai confermato in uno snapshot di poll.</summary>
    PosizioneMaiConfermata,

    /// <summary>
    /// La sessione è stata ripresa da un dump dopo un riavvio del server e da allora non ha
    /// ricevuto una sola barra: il cBot non si è ancora riagganciato.
    /// </summary>
    SessioneRipresaSenzaFlusso
}

/// <summary>
/// Un rilievo del presidio: cosa il server crede, e cosa conviene controllare su cTrader.
///
/// <para><b>Nessun rilievo afferma che una posizione è aperta.</b> La console parla solo HTTP con
/// l'API e non vede cTrader: può dire cosa il server crede e da quanto non lo verifica, non cosa
/// c'è davvero sul conto. Finché la riconciliazione (fase 3 del documento) non esiste, "per il
/// server" è la sola formulazione onesta, ed è quella che <see cref="Message"/> usa.</para>
/// </summary>
public sealed class RealtimeWatchItem
{
    public required RealtimeWatchFinding Finding { get; init; }

    public required RealtimeWatchSeverity Severity { get; init; }

    /// <summary>Sessione a cui il rilievo si riferisce; vuoto quando il rilievo riguarda il conto.</summary>
    public string SessionId { get; init; } = string.Empty;

    public string StrategyCode { get; init; } = string.Empty;

    public string Symbol { get; init; } = string.Empty;

    public string IntentId { get; init; } = string.Empty;

    /// <summary>Cosa risulta al server, con i numeri su cui il rilievo poggia.</summary>
    public required string Message { get; init; }

    /// <summary>Cosa fare, in una riga. Vuoto quando non c'è niente da fare.</summary>
    public string Action { get; init; } = string.Empty;
}

/// <summary>Posizione che il server crede aperta per il conto, con ciò che ne governa l'uscita.</summary>
public sealed class RealtimeWatchPosition
{
    public required string StrategyCode { get; init; }

    /// <summary>Simbolo Piootoo (<c>@NQ</c>), come lo indicizza il server.</summary>
    public required string Symbol { get; init; }

    /// <summary>Simbolo con cui lo stesso strumento compare su cTrader, per poterlo cercare a mano.</summary>
    public string AccountSymbol { get; init; } = string.Empty;

    public required SignalType Direction { get; init; }

    public decimal Quantity { get; init; }

    public decimal EntryPrice { get; init; }

    /// <summary>Istante del report di fill che l'ha aperta.</summary>
    public DateTime EntryTimeUtc { get; init; }

    /// <summary>Intent di ingresso, se il server ce l'ha ancora in memoria.</summary>
    public string IntentId { get; init; } = string.Empty;

    public decimal? StopLoss { get; init; }

    public decimal? TakeProfit { get; init; }

    /// <summary>Uscita a tempo dichiarata dall'intent: la applica il client, non il server.</summary>
    public DateTime? CloseAtUtc { get; init; }

    public int? MaxBarsInPosition { get; init; }

    /// <summary>
    /// Il broker l'ha confermata almeno una volta in uno snapshot di poll. Falso non significa
    /// "non esiste": nelle sessioni dirette gli snapshot non arrivano mai — vedi
    /// <see cref="RealtimeWatchSession.RiceveStatoBroker"/>.
    /// </summary>
    public bool BrokerConfermata { get; init; }
}

/// <summary>Ordine che per il server è ancora in volo sul conto.</summary>
public sealed class RealtimeWatchPending
{
    public required string IntentId { get; init; }

    public required string StrategyCode { get; init; }

    public required string Symbol { get; init; }

    public string AccountSymbol { get; init; } = string.Empty;

    public required SignalType Side { get; init; }

    public required OrderIntentStatus Status { get; init; }

    public decimal Price { get; init; }

    public decimal Quantity { get; init; }

    public int TimeframeMinutes { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Inizio dell'<b>ultima</b> barra su cui l'ordine è valido: la scadenza vera è
    /// <c>ExpiresAtUtc + TimeframeMinutes</c>. Vedi <c>docs/domini/orologio-barre-e-fill.md</c>.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; init; }
}

/// <summary>Una sessione realtime del conto, come il server la vede in questo istante.</summary>
public sealed class RealtimeWatchSession
{
    public required string SessionId { get; init; }

    public string PlanCode { get; init; } = string.Empty;

    public string ExecutionKey { get; init; } = string.Empty;

    public required string WorkspaceId { get; init; }

    public required TradingSessionStatus Status { get; init; }

    public required ExecutionMode ExecutionMode { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Ultima barra chiusa ricevuta su qualunque stream.</summary>
    public DateTime? LastBarUtc { get; init; }

    /// <summary>Ultima barra su cui le strategie sono state valutate.</summary>
    public DateTime? LastEvaluatedBarUtc { get; init; }

    /// <summary>Timeframe più fitto del portafoglio: è la scala su cui si misura un flusso fermo.</summary>
    public int MinTimeframeMinutes { get; init; }

    /// <summary>Da quanti minuti il server non riceve una barra. Null se non ne ha mai ricevuta una.</summary>
    public double? MinutiDallUltimaBarra { get; init; }

    /// <summary>
    /// Cosa il piano concede di tenere. È il dato su cui poggiano i rilievi di flat, non solo
    /// un'etichetta: <c>Describe()</c> la rende leggibile, <c>WeekEnd.IsInsideWindow</c> dice se in
    /// questo istante il conto dovrebbe essere piatto.
    /// </summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    /// <summary>
    /// Il client manda al server lo stato del broker (posizioni, ordini, trade) a ogni poll di
    /// claim. Vero nelle sessioni distribuite, falso in esecuzione diretta: lì il server non
    /// verifica mai contro cTrader ciò che crede.
    /// </summary>
    public bool RiceveStatoBroker { get; init; }

    /// <summary>
    /// Quando la sessione è stata ripresa da <c>session-state.json</c> dopo un riavvio del server.
    /// Null se è stata aperta normalmente. Finché non arriva una barra successiva a questo istante,
    /// posizioni e ordini elencati vengono da un dump e nessun client li ha ancora confermati.
    /// </summary>
    public DateTime? RipresaDaDumpAtUtc { get; init; }

    public IReadOnlyList<RealtimeWatchPosition> Posizioni { get; init; } = [];

    public IReadOnlyList<RealtimeWatchPending> Pendenti { get; init; } = [];
}

/// <summary>
/// Presidio di un conto: cosa il server sta governando adesso e dove serve un intervento a mano su
/// cTrader. Risposta di <c>GET /api/v1/trading-sessions/accounts/{accountNumber}/watch</c>.
/// </summary>
public sealed class AccountRealtimeWatch
{
    public required string AccountNumber { get; init; }

    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>Codici dei piani che nominano questo conto: senza, non ci si aspetta una sessione.</summary>
    public IReadOnlyList<string> Piani { get; init; } = [];

    public IReadOnlyList<RealtimeWatchSession> Sessioni { get; init; } = [];

    /// <summary>Rilievi già ordinati per gravità decrescente.</summary>
    public IReadOnlyList<RealtimeWatchItem> Rilievi { get; init; } = [];

    /// <summary>La gravità massima fra i rilievi: è il semaforo della schermata.</summary>
    public RealtimeWatchSeverity Severity { get; init; }
}
