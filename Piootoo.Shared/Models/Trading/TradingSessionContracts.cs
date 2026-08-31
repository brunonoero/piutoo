using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Shared.Models.Trading;

public enum ExecutionMode { ServerSimulated, ExternalBroker }
public enum TradingSessionStatus { Created, Running, Stopped }
public enum ExecutionReportStatus { Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
public enum OrderIntentStatus { Pending, Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
/// <summary>
/// Granularità con cui arrotondare una quantità in contratti Piootoo.
///
/// <para><see cref="FuturesContracts"/> — contratti interi (passo minimo 1), la size dichiarata
/// dalle strategie. <see cref="BrokerVolumeStep"/> — passo di volume esplicito, per i CFD dove può
/// essere frazionario. <see cref="Deferred"/> — non arrotondare qui: la granularità è quella del
/// broker e si applica una sola volta, al claim, con la tabella di conversione dell'account
/// (<see cref="Piootoo.Core.Services.AccountSymbolConversion.RoundQuantity"/> — vedi
/// <c>docs/decisioni.md</c> 2026-08-05). Serve alle sessioni <see cref="ExecutionMode.ExternalBroker"/>
/// per evitare di arrotondare due volte: una prima sui contratti Piootoo, una seconda sui contratti
/// del broker dopo la conversione.</para>
/// </summary>
public enum QuantityRoundingMode { FuturesContracts, BrokerVolumeStep, Deferred }

/// <summary>
/// Natura di un <see cref="OrderIntent"/>.
///
/// <para><see cref="Entry"/> — ingresso deciso dal server a partire da un segnale di strategia. Porta
/// con sé l'intera specifica di uscita (SL, TP, uscita a tempo, massimo di barre): il client la
/// applica e chiude in autonomia.</para>
///
/// <para><see cref="Close"/> — chiusura di una posizione esistente. Può registrare una chiusura già
/// decisa dal client (SL/TP nativi del broker, uscita a tempo, limite barre) oppure essere emesso dal
/// server per un segnale <see cref="TradeSignal.ExitOnly"/>. Il client esegue l'intent e vi aggancia
/// l'execution report per generare il <c>PersistedTrade</c>.</para>
/// </summary>
public enum OrderIntentKind { Entry, Close }

/// <summary>
/// Le tre modalità operative dell'engine rispetto al filtro Titano. Valgono identiche per l'engine
/// interno (backtest in-process, <c>ServerSimulated</c>) e per l'engine esterno cTrader
/// (<c>ExternalBroker</c>): il cBot invia le barre e riceve i segnali già filtrati, quindi non
/// conosce Titano e non cambia comportamento tra una modalità e l'altra.
/// </summary>
/// <summary>
/// Contesto in cui sta girando il client che ha creato la sessione. Non è una preferenza: il cBot
/// lo ricava dalla piattaforma (<c>Robot.IsBacktesting</c>), non da un parametro, quindi non può
/// sbagliarlo per distrazione.
///
/// <para>Serve a rendere verificabile lato server una coerenza che altrimenti resterebbe negli
/// occhi dell'operatore: una rotazione storica applicata a una sessione live, o una modalità
/// <see cref="TitanoFilterMode.Realtime"/> usata in backtest, sono errori silenziosi che si notano
/// solo dai risultati sbagliati. Vedi <c>CreateTradingSessionRequest.ClientRunMode</c>.</para>
/// </summary>
public enum ClientRunMode
{
    /// <summary>
    /// Client che non sa dichiararlo (console WinForms, test, integrazioni terze). Le combinazioni
    /// non vengono verificate: la responsabilità resta di chi configura.
    /// </summary>
    Unknown,

    /// <summary>Replay di dati storici: cTrader in backtesting/ottimizzazione, o backtest interno.</summary>
    Backtest,

    /// <summary>Mercato reale, barre che arrivano man mano che si chiudono.</summary>
    Realtime
}

public enum TitanoFilterMode
{
    /// <summary>
    /// Nessun filtro: vengono valutate tutte le strategie del masterfilter. È la modalità con cui si
    /// produce il <c>trades.json</c> su cui l'analisi Titano calcola offline le rotazioni.
    /// </summary>
    Disabled,

    /// <summary>
    /// Backtest con filtro: le rotazioni sono lette dal manifest calcolato <b>offline</b> sui trade
    /// di tutte le strategie del workspace. Per ogni barra vale la decisione del periodo che la
    /// contiene. Una barra fuori dai periodi del manifest è un errore di configurazione (manifest
    /// non allineato all'intervallo di backtest), non un "tutto abilitato".
    /// </summary>
    BacktestRotationFile,

    /// <summary>
    /// Realtime: vale la decisione del periodo corrente prodotto dall'ultima analisi Titano. Le barre
    /// successive alla fine del manifest ricadono sull'ultimo periodo disponibile — è la rotazione
    /// in vigore finché non se ne calcola una nuova — e la cosa viene dichiarata nel rotation-log.
    /// </summary>
    Realtime
}

public sealed class InstrumentMetadata
{
    public required string Symbol { get; init; }
    public decimal DollarsPerPoint { get; init; } = 1m;
    public decimal MinimumQuantity { get; init; } = 1m;
    public decimal QuantityStep { get; init; } = 1m;
    public QuantityRoundingMode RoundingMode { get; init; } = QuantityRoundingMode.FuturesContracts;
}

public sealed class MarketVolatilitySizingConfig
{
    public bool Enabled { get; init; }
    public int AtrPeriods { get; init; } = 14;
    public decimal TargetRiskDollars { get; init; } = 1_000m;
}

public sealed class PortfolioRiskSizingConfig
{
    public bool Enabled { get; init; }
    public decimal MaximumDrawdown { get; init; } = 0.20m;
    public decimal MaximumGrossExposure { get; init; } = 1m;
    // L'overlay CPPI è stato rimosso (docs/decisioni.md 2026-08-05): entrava solo come tetto sul
    // moltiplicatore già calcolato e, con l'equity ferma in ExternalBroker, era un taglio costante.
    public bool EnableAggressiveModules { get; init; }
    public decimal FractionalFactor { get; init; } = 0.25m;
    public decimal MaximumMultiplier { get; init; } = 1m;
}

public sealed class PositionSizingConfig
{
    public bool ClampMultipliersToUnitInterval { get; init; } = true;
    public MarketVolatilitySizingConfig MarketVolatility { get; init; } = new();
    public PortfolioRiskSizingConfig PortfolioRisk { get; init; } = new();
}

public sealed class CreateTradingSessionRequest
{
    public required string WorkspaceId { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public decimal InitialCapital { get; init; } = 100_000m;
    public decimal CommissionPerContract { get; init; } = 2m;
    public string? ClientSessionToken { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }

    /// <summary>
    /// Modalità operativa rispetto al filtro Titano. Vedi <see cref="TitanoFilterMode"/>.
    ///
    /// <para><see cref="TitanoFilterMode.Disabled"/> non richiede un <see cref="TitanoRunId"/>. Se il
    /// RunId è comunque indicato, la rotazione viene risolta e registrata nel rotation-log senza
    /// essere applicata: serve a confrontare "cosa avrebbe fatto Titano" senza subirne gli effetti.</para>
    ///
    /// <para>Le altre due modalità richiedono <see cref="TitanoRunId"/> e
    /// <see cref="TitanoBacktestFolder"/>.</para>
    /// </summary>
    public TitanoFilterMode TitanoMode { get; init; } = TitanoFilterMode.Disabled;

    /// <summary>
    /// Contesto dichiarato dal client (vedi <see cref="ClientRunMode"/>). Il server lo incrocia con
    /// <see cref="TitanoMode"/> e rifiuta le combinazioni incoerenti:
    ///
    /// <list type="bullet">
    /// <item><see cref="TitanoFilterMode.Realtime"/> in <see cref="ClientRunMode.Backtest"/>: la
    /// rotazione "corrente" verrebbe applicata a barre storiche, e oltre la fine del manifest
    /// resterebbe congelata sull'ultimo periodo — cioè look-ahead mascherato da fallback.</item>
    /// <item><see cref="TitanoFilterMode.BacktestRotationFile"/> in
    /// <see cref="ClientRunMode.Realtime"/>: il tempo live esce quasi subito dall'intervallo del
    /// manifest e la sessione si fermerebbe alla prima barra scoperta.</item>
    /// </list>
    ///
    /// <see cref="TitanoFilterMode.Disabled"/> è legittimo in entrambi i contesti.
    /// </summary>
    public ClientRunMode ClientRunMode { get; init; } = ClientRunMode.Unknown;

    /// <summary>
    /// Applica il limite <c>MaxConcurrentTrades</c> per account nella distribuzione multi-account.
    ///
    /// <para>Null = default: attivo ovunque tranne nel run che genera il campione sorgente, cioè
    /// <see cref="ClientRunMode.Backtest"/> con <see cref="TitanoFilterMode.Disabled"/>. Quel run
    /// deve produrre tutti i trade del master filter, perché è da lì che Titano calcola le
    /// rotazioni: limitare la concorrenza eliminerebbe segnali e falserebbe la sorgente.</para>
    ///
    /// <para>Il flag esiste perché prima quella regola era cablata su <see cref="TitanoMode"/>, e
    /// così concorrenza e rotazione si accendevano insieme: confrontare un run <c>Disabled</c> con
    /// uno <c>BacktestRotationFile</c> significava muovere due variabili e attribuire a Titano una
    /// differenza che veniva in parte dal limite di trade. Valorizzarlo esplicitamente permette di
    /// isolare i due effetti.</para>
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

    /// <summary>
    /// Cosa il conto permette di tenere e quando taglia. Il server e' l'unico proprietario di questi
    /// numeri: li pubblica nel descriptor, il cBot li esegue e il backtest riceve la stessa policy
    /// nella propria richiesta. Vedi <see cref="AccountHoldingPolicy"/>.
    /// </summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    public PositionSizingConfig PositionSizing { get; init; } = new();
}

public sealed class TradingSessionDescriptor
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required string WorkspaceId { get; init; }
    public string? PlanCode { get; init; }
    public string? ExecutionKey { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public string? TitanoRunId { get; init; }

    /// <summary>Modalità operativa rispetto al filtro Titano. Vedi <see cref="TitanoFilterMode"/>.</summary>
    public TitanoFilterMode TitanoMode { get; init; }

    /// <summary>Contesto dichiarato dal client alla creazione. Vedi <see cref="Trading.ClientRunMode"/>.</summary>
    public ClientRunMode ClientRunMode { get; init; }

    /// <summary>Profilo con cui il cBot ha aperto il run. Vedi <c>TradingRunProfile</c>.</summary>
    public TradingRunProfile RunProfile { get; init; }

    /// <summary>
    /// Se i lucchetti di concorrenza sono attivi in questa sessione (<c>MaxConcurrentTrades</c> e
    /// slot gruppo/strategia/simbolo). Falso nel backtest sorgente.
    /// </summary>
    public bool EnforceConcurrencyLimits { get; init; }

    /// <summary>
    /// <c>MaxConcurrentTrades</c> dell'account destinatario del descriptor. 0 = illimitato. Vale
    /// solo se <see cref="EnforceConcurrencyLimits"/> è vero.
    /// </summary>
    public int MaxConcurrentTrades { get; init; }

    /// <summary>
    /// Cosa conta <see cref="MaxConcurrentTrades"/>. Il client lo legge per sapere se deve
    /// cancellare gli ordini pendenti rimasti quando i fill raggiungono il tetto
    /// (<see cref="Trading.ConcurrencyCountMode.PositionsOnly"/>).
    /// </summary>
    public ConcurrencyCountMode ConcurrencyCountMode { get; init; }

    /// <summary>
    /// Cosa il conto permette di tenere e a che ora taglia, deciso dal server. Il cBot non ha piu'
    /// alcun parametro proprio su questo: due regole diverse fra chi simula e chi esegue sono due
    /// sistemi diversi, ed e' esattamente cio' che il confronto del 26/08/2026 ha trovato (23:30
    /// contro 20:45). Vedi <see cref="AccountHoldingPolicy"/>.
    /// </summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> InstrumentMetadata { get; init; } = [];
    public IReadOnlyList<TradingInstrument> Instruments { get; init; } = [];

    /// <summary>
    /// Le strategie che la sessione valuterà, con simbolo e timeframe. Serve al client per
    /// mostrare a chart *cosa* sta girando: un cBot che dichiara un piano e ne esegue un altro è
    /// indistinguibile da uno che funziona, finché non si leggono i trade.
    /// </summary>
    public IReadOnlyList<TradingSessionStrategyInfo> Strategies { get; init; } = [];
}

/// <summary>Identità operativa di una strategia in sessione: codice, simbolo, timeframe.</summary>
public sealed class TradingSessionStrategyInfo
{
    /// <summary>
    /// Il <c>Name</c>/<c>StrategyCode</c>, non l'<c>Id</c> della classe: è il codice che compare in
    /// <c>signals.json</c>, <c>trades.json</c> e negli stati Titano. Vedi CLAUDE.md, «Id ≠ Name».
    /// </summary>
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>
    /// Cosa questa strategia vuole tenere. Sul chart si legge accanto al codice: e' l'unico modo
    /// per vedere, senza aprire i sorgenti, quali strategie il piano sta troncando e quali no.
    /// </summary>
    public StrategyHolding Holding { get; init; } = StrategyHolding.Multiday;
}

/// <summary>
/// Riga leggera per l'elenco delle sessioni vive (<c>GET /api/v1/trading-sessions</c>): niente
/// artefatti pesanti (strumenti, sizing), solo l'identità e lo stato che servono a una griglia.
///
/// <para>Include <see cref="SessionToken"/> perché senza non si potrebbe gestire da console una
/// sessione aperta da un cBot: l'unica alternativa sarebbe un'API di sola lettura, che vanifica lo
/// scopo dell'elenco. Non esiste ancora un provider di autenticazione sull'API (vedi
/// <c>trading-sessions-api.md</c>): il token resta il solo confine fra sessioni, non un confine
/// verso chi ha già accesso al server.</para>
/// </summary>
public sealed class TradingSessionSummary
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required string WorkspaceId { get; init; }
    public string? PlanCode { get; init; }
    public string? ExecutionKey { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public required ClientRunMode ClientRunMode { get; init; }
    public required TitanoFilterMode TitanoMode { get; init; }
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Barra più recente valutata dalla sessione. Null se non ha ancora ricevuto barre.</summary>
    public DateTime? LastBarTimeUtc { get; init; }
}

public sealed class TradingInstrument
{
    public required string Symbol { get; init; }

    /// <summary>
    /// Nome dello stesso strumento sull'account che esegue la sessione. È il nome che il client
    /// vede sul proprio grafico e con cui deve riconoscere lo strumento; nelle barre e negli intent
    /// di chiusura va invece usato <see cref="Symbol"/>, che è la chiave del dominio Piootoo.
    /// Coincidono quando l'account non mappa quel simbolo o non è noto.
    /// </summary>
    public string AccountSymbol { get; init; } = string.Empty;

    public required IReadOnlyList<int> TimeframesMinutes { get; init; }

    /// <summary>
    /// Per ogni timeframe di <see cref="TimeframesMinutes"/>, quante candele servono al server per
    /// poter valutare almeno una strategia di questa coppia: è il massimo di
    /// <c>ITradingStrategy.RequiredCandles</c> fra le strategie del masterfilter su quello stream.
    ///
    /// <para>Serve al client per sapere quanta storia caricare dal broker e quanto profonda deve
    /// essere la finestra che invia a ogni barra: sotto questa soglia
    /// <c>StrategyEvaluationService</c> salta la valutazione, e la sessione resta muta senza che
    /// nulla lo segnali. Non è un parametro del client di proposito — duplicherebbe il
    /// masterfilter e le due cifre divergerebbero in silenzio.</para>
    /// </summary>
    public IReadOnlyDictionary<int, int> RequiredCandlesByTimeframe { get; init; } =
        new Dictionary<int, int>();
}

public sealed class ClosedBar
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }
    public required DateTime BarTimeUtc { get; init; }
    public required long Sequence { get; init; }
    public required string IdempotencyKey { get; init; }
    public required OhlcvData Bar { get; init; }
}

public sealed class PushBarsRequest
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required IReadOnlyList<ClosedBar> Bars { get; init; }
}

public sealed class PushBarsResponse
{
    public int AcceptedBars { get; init; }
    public int DuplicateBars { get; init; }
    public IReadOnlyList<OrderIntent> Intents { get; init; } = [];
}

/// <summary>
/// Finestra di candele di uno stream: le ultime N barre chiuse, in ordine cronologico, di cui
/// l'ultima è quella appena chiusa e da valutare.
///
/// <para>Esiste perché il server non ha un datafeed proprio nelle sessioni ExternalBroker: la storia
/// di uno stream è solo ciò che il client gli ha spinto. Inviando una barra per volta le prime
/// <c>RequiredCandles</c> barre di ogni run venivano scartate in silenzio da
/// <c>StrategyEvaluationService</c> (per una strategia a 15 minuti sono sei sessioni piene, 576
/// barre) e un backtest più corto di quella soglia non produceva un solo segnale.</para>
///
/// <para><b>Solo l'ultima candela viene valutata.</b> Le precedenti servono a ricostruire la storia:
/// il server accoda quelle che non ha già e ignora le altre. Così la prima finestra di un run fa da
/// riscaldamento senza generare intent sul passato.</para>
/// </summary>
public sealed class ClosedBarWindow
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>Candele in ordine cronologico crescente; l'ultima è la barra appena chiusa.</summary>
    public required IReadOnlyList<OhlcvData> Candles { get; init; }

    /// <summary>Idempotenza e ordinamento si applicano alla sola barra da valutare, cioè l'ultima.</summary>
    public required long Sequence { get; init; }
    public required string IdempotencyKey { get; init; }

    /// <summary>
    /// false = finestra di solo riscaldamento: il server accoda le candele e non valuta nulla, quindi
    /// non consuma <see cref="IdempotencyKey"/> né avanza <see cref="Sequence"/>.
    ///
    /// <para>Serve all'avvio del client, che deve consegnare al server tutta la storia richiesta dalle
    /// strategie senza che le barre già passate producano intent: sono ordini sul passato, che il bot
    /// eseguirebbe al prezzo di adesso. A regime il client manda finestre corte con questo flag a
    /// true.</para>
    /// </summary>
    public bool EvaluateLastCandle { get; init; } = true;
}

public sealed class PushBarWindowRequest
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required IReadOnlyList<ClosedBarWindow> Windows { get; init; }
}

/// <summary>
/// Stato della storia di uno stream dopo l'invio: dice al client se il server è già in grado di
/// valutare o quante barre gli mancano ancora. Senza questo il silenzio della sessione è
/// indistinguibile da "nessuna strategia ha prodotto un segnale".
/// </summary>
public sealed class StreamHistoryStatus
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>Candele che il server ha in memoria per questo stream dopo l'unione della finestra.</summary>
    public required int HistoryBars { get; init; }

    /// <summary>Massimo <c>RequiredCandles</c> fra le strategie del masterfilter su questo stream.</summary>
    public required int RequiredCandles { get; init; }

    /// <summary>Strategie del masterfilter su questo stream che sono state davvero valutate.</summary>
    public required int EvaluatedStrategies { get; init; }

    /// <summary>Strategie saltate perché la storia disponibile è più corta del loro RequiredCandles.</summary>
    public required int SkippedForInsufficientHistory { get; init; }
}

public sealed class PushBarWindowResponse
{
    public int AcceptedBars { get; init; }
    public int DuplicateBars { get; init; }

    /// <summary>Candele accodate alla storia perché il server non le aveva (riscaldamento).</summary>
    public int BackfilledBars { get; init; }
    public IReadOnlyList<OrderIntent> Intents { get; init; } = [];
    public IReadOnlyList<StreamHistoryStatus> Streams { get; init; } = [];

    /// <summary>
    /// Quante cose la sessione ha, in questo istante, che un claim potrebbe consegnare a qualcuno:
    /// template di ingresso ancora <c>Pending</c> e non scaduti, più gli intent già assegnati e
    /// ancora pendenti (ingressi non confermati e chiusure da eseguire).
    ///
    /// <para>Serve al client come guardia sul poll. Se vale <c>0</c>,
    /// <c>GetNextSignalForAccount</c> non può restituire nulla <b>per nessun account</b>: il passo 1
    /// non trova intent assegnati e la lista dei template è vuota. Il cBot può quindi saltare il
    /// poll, che in backtest è una chiamata HTTP sincrona per barra e per stream — e dai log reali
    /// la grande maggioranza delle barre non produce nessun segnale.</para>
    ///
    /// <para>È il server a contarlo, non il client: solo lui sa dei template di barre precedenti
    /// ancora vivi e degli intent assegnati da un altro giro. Dedurlo lato client da
    /// <see cref="Intents"/> — cioè dai soli segnali di <i>questa</i> barra — salterebbe un poll che
    /// aveva qualcosa da consegnare.</para>
    /// </summary>
    public int ClaimableIntents { get; init; }
}

public sealed class OrderIntent
{
    public required string IntentId { get; init; }
    public required string SessionId { get; init; }
    public required string StrategyCode { get; init; }
    public string StrategyName { get; init; } = string.Empty;
    public required string Symbol { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public SignalType Side { get; init; }
    public TradeOrderType OrderType { get; init; }
    /// <summary>
    /// Simbolo con cui inoltrare l'ordine sull'account assegnato. È il simbolo su cui il client
    /// deve fare match: <see cref="Symbol"/> resta quello Piootoo, con cui il server indicizza
    /// posizioni e valutazioni. Coincidono quando l'account non mappa quel simbolo.
    /// </summary>
    public string AccountSymbol { get; init; } = string.Empty;

    /// <summary>Account della tabella di conversione applicata. Vuoto = nessuna conversione.</summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>Rapporto contratto broker / contratto Piootoo, già applicato alle quantità.</summary>
    public decimal ContractMultiplier { get; init; } = 1m;

    /// <summary>Rapporto capitale del conto / milione di riferimento, già applicato alle quantità.</summary>
    public decimal AccountBalanceScale { get; init; } = 1m;

    /// <summary>
    /// Fattore con cui le distanze di prezzo (<see cref="StopLoss"/>, <see cref="TakeProfit"/>,
    /// <see cref="BreakEven"/>, <see cref="TrailingStop"/>) sono state convertite nei punti dello
    /// strumento dell'account: quei campi arrivano al client GIÀ scalati, questo numero serve solo
    /// a ricostruire la distanza Piootoo originale in diagnostica. Vale 1 quando l'account quota
    /// lo strumento nella stessa unità delle strategie, che è il caso normale.
    /// </summary>
    public decimal PriceScale { get; init; } = 1m;

    public decimal Quantity { get; init; }

    /// <summary>Quantità prima della conversione dell'account, per tracciabilità: non va eseguita.</summary>
    public decimal QuantityBeforeAccountConversion { get; init; }

    public decimal AllocationMultiplier { get; init; } = 1m;
    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; } = 1m;
    public decimal MarketVolatilityMultiplier { get; init; } = 1m;
    public decimal PortfolioRiskMultiplier { get; init; } = 1m;
    public decimal FinalQuantity { get; init; }
    public string? SizingReason { get; init; }
    public decimal Price { get; init; }

    /// <summary>Ingresso, oppure registrazione di una chiusura decisa dal client. Vedi <see cref="OrderIntentKind"/>.</summary>
    public OrderIntentKind Kind { get; init; } = OrderIntentKind.Entry;

    /// <summary>Vero per gli intent che chiudono una posizione. Comodità di lettura su <see cref="Kind"/>.</summary>
    public bool IsClose => Kind == OrderIntentKind.Close;

    // --- Specifica di uscita completa, copiata dal segnale di ingresso ---
    // Il client DEVE applicarla per intero per le uscite locali; il server può inoltre emettere un
    // intent Close quando una strategia produce un segnale ExitOnly.

    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }

    /// <summary>Livello di break even in punti: raggiunto il profitto, lo stop va spostato all'entry.</summary>
    public decimal? BreakEven { get; init; }

    /// <summary>
    /// Distanza in punti dal picco favorevole che il client deve mantenere come
    /// trailing stop. Null = nessun trailing stop.
    /// </summary>
    public decimal? TrailingStop { get; init; }

    // --- Rischio in denaro, come dichiarato dalla strategia ---
    // I punti qui sopra restano la grandezza da eseguire; questi campi sono la stessa specifica
    // nell'unità in cui la ricerca l'ha scritta, denaro per contratto future di riferimento.
    // Viaggiano fino al client per una ragione sola: permettergli di verificare che il rischio che
    // sta per mettere a mercato sia quello dichiarato. La conversione denaro → punti è avvenuta una
    // volta sola sul server, dividendo per ReferenceDollarsPerPoint; rifarla sul client, con il
    // valore punto del SUO strumento, è l'errore che questi campi servono a rendere visibile.

    /// <summary>Stop in denaro per contratto future di riferimento, se la strategia l'ha dichiarato così.</summary>
    public decimal? StopLossMoneyPerFutureContract { get; init; }

    /// <summary>Target in denaro per contratto future di riferimento.</summary>
    public decimal? TakeProfitMoneyPerFutureContract { get; init; }

    /// <summary>Trailing stop in denaro per contratto future di riferimento.</summary>
    public decimal? TrailingStopMoneyPerFutureContract { get; init; }

    /// <summary>Soglia di break even in denaro per contratto future di riferimento.</summary>
    public decimal? BreakEvenMoneyPerFutureContract { get; init; }

    /// <summary>
    /// Valore in denaro di un punto del contratto future di riferimento
    /// (<c>InstrumentRegistry.PointValue</c> del simbolo Piootoo), cioè il divisore con cui i campi
    /// in denaro qui sopra sono diventati i punti dell'intent. È del contratto di riferimento, non
    /// di quello dell'account: quest'ultimo entra solo nella quantità, via
    /// <see cref="ContractMultiplier"/>.
    /// </summary>
    public decimal ReferenceDollarsPerPoint { get; init; } = 1m;

    /// <summary>
    /// Timeframe della strategia che ha emesso l'intent. Il client usa questa
    /// informazione per contare <see cref="MaxBarsInPosition"/> sulle barre del
    /// relativo stream, non sul timeframe del grafico che ospita il cBot.
    /// </summary>
    public int TimeframeMinutes { get; init; }

    /// <summary>Numero massimo di barre in posizione. Null o 0 = nessun limite.</summary>
    public int? MaxBarsInPosition { get; init; }

    /// <summary>
    /// Massimo numero di fill consentiti nella sessione che inizia a
    /// <see cref="EntrySessionStartUtc"/>. Null o 0 = nessun limite. Il vincolo è applicato dal
    /// server sui fill confermati, non sugli ordini inviati: uno stop non eseguito viene
    /// riemesso. Viaggia sull'intent perché il client possa mostrarlo e diagnosticarlo.
    /// </summary>
    public int? MaxEntriesPerSession { get; init; }

    /// <summary>
    /// Inizio della sessione di trading a cui si riferisce <see cref="MaxEntriesPerSession"/>,
    /// secondo il calendario della strategia che ha emesso il segnale.
    /// </summary>
    public DateTime? EntrySessionStartUtc { get; init; }

    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }

    /// <summary>Istante entro cui la posizione va chiusa (uscita a tempo). Null = nessuna scadenza.</summary>
    public DateTime? CloseAtUtc { get; init; }

    /// <summary>
    /// Condiziona la chiusura a <see cref="CloseAtUtc"/> all'utile aperto per contratto: alla
    /// deadline il client chiude solo se l'utile è inferiore a questa soglia. Null = chiusura
    /// incondizionata.
    /// </summary>
    public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; init; }

    /// <summary>
    /// Da questo istante il client sorveglia l'utile aperto e chiude alla prima barra in cui non
    /// supera il massimo osservato dopo la deadline. Null = nessuna uscita per stallo.
    /// </summary>
    public DateTime? ProfitStallAfterUtc { get; init; }

    public string? Reason { get; init; }
    public OrderIntentStatus Status { get; set; } = OrderIntentStatus.Pending;
    public decimal FilledQuantity { get; set; }
    public string? ExternalOrderId { get; set; }
    /// <summary>Account cTrader a cui questo intent concreto è stato assegnato. Null per i template non ancora reclamati da nessun gruppo.</summary>
    public string? AssignedAccountNumber { get; set; }
    /// <summary>Gruppo (prop firm) a cui appartiene l'account assegnato.</summary>
    public string? AssignedGroupId { get; set; }
}

public sealed class ExternalExecutionReport
{
    public required string ReportId { get; init; }
    public required string IntentId { get; init; }
    public string? ExternalOrderId { get; init; }
    public required ExecutionReportStatus Status { get; init; }
    public decimal CumulativeFilledQuantity { get; init; }
    public decimal? FillPrice { get; init; }
    public decimal Commission { get; init; }

    /// <summary>
    /// Interessi di finanziamento addebitati o accreditati dal broker sulla posizione, in valuta
    /// del conto. È <b>con segno</b>, perché a differenza della commissione può essere un credito:
    /// negativo è un costo, positivo un accredito.
    ///
    /// <para>Non è un dettaglio contabile trascurabile su posizioni multigiorno: un TFU tenuto
    /// aperto 4694 minuti ha pagato 314,19 di swap su 1422,45 di perdita netta, il 22% del
    /// risultato. Finché il campo non esisteva quella quota spariva dal trade persistito e il
    /// P&amp;L riportato non tornava con il saldo del conto.</para>
    /// </summary>
    public decimal Swap { get; init; }

    /// <summary>
    /// Utile lordo del trade come lo conta il broker, <b>nella valuta del conto</b>. Null quando il
    /// client non lo dichiara: in quel caso il server lo ricava dai prezzi, come faceva prima.
    ///
    /// <para><b>Perche' non basta ricavarlo dai prezzi.</b> <c>punti × valore punto</c> da' un
    /// importo nella valuta dello <i>strumento</i>, mentre commissione e swap arrivano gia' nella
    /// valuta del <i>conto</i>: il netto che ne usciva sommava due valute diverse. Su un conto in EUR
    /// che opera XAUUSD l'errore e' il cambio EURUSD — su un run 2022-2023 il lordo persistito era
    /// del 7% mediano sopra quello del broker, con uno scarto che va da −4% a +15% seguendo il
    /// cambio, quindi non correggibile a posteriori con un fattore fisso.</para>
    /// </summary>
    public decimal? GrossProfit { get; init; }

    /// <summary>
    /// Utile netto del broker, valuta del conto, commissioni e swap gia' dentro. Vedi
    /// <see cref="GrossProfit"/>. Null se il client non lo dichiara.
    /// </summary>
    public decimal? NetProfit { get; init; }

    public required DateTime EventTimeUtc { get; init; }

    /// <summary>
    /// Spread dello strumento sul broker nell'istante del fill, in unità di prezzo. Null se il
    /// client non lo dichiara.
    ///
    /// <para>Non serve alla contabilità — il P&amp;L viene dai prezzi di apertura e chiusura — ma è
    /// l'unico modo per misurare quanto costa davvero eseguire su questo strumento. Su un CFD long
    /// si entra sull'<b>Ask</b> e lo stop è valutato sul <b>Bid</b>: la perdita in denaro quando lo
    /// stop salta resta quella dichiarata, ma il Bid deve scendere solo di
    /// <c>(distanza stop − spread)</c> per farlo saltare. Il rapporto <c>spread / distanza stop</c>
    /// è quindi quanto margine operativo lo strumento si mangia, e cambia per strategia: su uno stop
    /// da 12,5 punti uno spread di 2 vale il 16%, su uno da 50 punti il 4%.</para>
    ///
    /// <para>Senza questo campo quel rapporto non è misurabile da nessuna parte del sistema.</para>
    /// </summary>
    public decimal? SpreadAtFill { get; init; }
}

public sealed class ExecutionReportRequest
{
    public required string SessionToken { get; init; }
    public required ExternalExecutionReport Report { get; init; }
}

public sealed class TradingPositionSnapshot
{
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    public required SignalType Direction { get; init; }
    public decimal Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    /// <summary>Account cTrader proprietario della posizione. Vuoto in modalità legacy senza gruppi.</summary>
    public string AccountNumber { get; init; } = string.Empty;
}

/// <summary>
/// Un evento della sessione, per il monitor della console.
///
/// <para><b>Perche' non basta lo snapshot.</b> <see cref="TradingSessionSnapshot"/> dice
/// <i>cos'e' aperto adesso</i>: posizioni, intent pendenti, equity. Non puo' dire <i>perche'</i>
/// mezz'ora fa non e' entrato niente, perche' quel "perche'" non e' uno stato — e' una decisione
/// che nasce e muore dentro un claim. Un monitor costruito sul solo snapshot mostra a lungo
/// "0 posizioni, 0 pending" senza distinguere "nessuna strategia ha prodotto un segnale" da
/// "li stava scartando tutti", che sono i due casi opposti da cui parte ogni indagine.</para>
///
/// <para>Vive in memoria in un buffer circolare per sessione: e' diagnostica viva, non
/// contabilita'. Il registro durevole restano <c>signals.json</c> e <c>trades.json</c>.</para>
/// </summary>
public sealed class SessionActivityEntry
{
    /// <summary>
    /// Progressivo crescente e senza buchi dentro la sessione. Il client rilegge con
    /// <c>?since=</c> e riceve solo il nuovo: senza, un monitor che polla ogni due secondi
    /// riscarica tutto il buffer ogni volta.
    /// </summary>
    public required long Sequence { get; init; }

    public required DateTime TimestampUtc { get; init; }
    public required SessionActivityKind Kind { get; init; }

    /// <summary>Account a cui l'evento si riferisce. Vuoto per gli eventi che non nascono da un claim.</summary>
    public string AccountNumber { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;
    public string StrategyCode { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Il testo che spiega l'evento: per un claim negato e' lo stadio del filtro che ha svuotato
    /// la lista, cioe' esattamente cio' che il cBot stampa come "Nessun intent per l'account".
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Intent coinvolto, quando ce n'e' uno. Permette di correlare l'evento con la griglia degli intent.</summary>
    public string IntentId { get; init; } = string.Empty;
}

/// <summary>
/// Tipo di evento. Deliberatamente pochi valori e grossolani: servono a colorare e filtrare una
/// griglia, non a classificare il dominio. Il dettaglio sta in <see cref="SessionActivityEntry.Detail"/>.
/// </summary>
public enum SessionActivityKind
{
    /// <summary>Una barra ha prodotto un template di ingresso.</summary>
    IntentCreato,

    /// <summary>Un account ha reclamato un intent e se l'e' portato via.</summary>
    ClaimServito,

    /// <summary>Un claim non ha restituito niente, e <see cref="SessionActivityEntry.Detail"/> dice quale filtro lo ha svuotato.</summary>
    ClaimNegato,

    /// <summary>Il client ha riportato l'esito di un ordine (riempito, rifiutato, annullato).</summary>
    EsitoEsecuzione,

    /// <summary>Una posizione si e' chiusa.</summary>
    PosizioneChiusa,

    /// <summary>Cambio di stato della sessione (avvio, stop, ripresa).</summary>
    Sessione,

    /// <summary>
    /// Un ingresso reclamato ha superato la propria finestra di validita' senza che il client ne
    /// riportasse mai l'esito, e il server lo ha annullato da se'.
    ///
    /// <para>Ha un tipo suo e non ricade in <see cref="EsitoEsecuzione"/> proprio perche' l'esito
    /// NON e' arrivato: e' il server che decide al posto di un client che non ha parlato, e
    /// confonderlo con un report vero renderebbe invisibile l'unica traccia di un client che ha
    /// smesso di riportare. Aggiunto in coda per non spostare gli ordinali dei valori esistenti.</para>
    /// </summary>
    IntentScaduto
}

/// <summary>
/// Risposta di <c>GET /activity</c>: la coda degli eventi dopo <c>since</c>, piu' il progressivo
/// corrente perche' il client sappia da dove ripartire anche quando la pagina e' vuota.
/// </summary>
public sealed class SessionActivityResponse
{
    public required long LastSequence { get; init; }

    /// <summary>
    /// Vero quando il buffer circolare ha gia' buttato eventi che il client non ha mai visto —
    /// cioe' quando ha pollato troppo lentamente rispetto al ritmo della sessione. Va detto:
    /// una griglia con un buco silenzioso e' peggio di una che dichiara di averlo.
    /// </summary>
    public bool Troncato { get; init; }

    public IReadOnlyList<SessionActivityEntry> Entries { get; init; } = [];
}

public sealed class TradingSessionSnapshot
{
    public required string SessionId { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal Equity { get; init; }
    public int Entries { get; init; }
    public int Fills { get; init; }
    public IReadOnlyList<TradingPositionSnapshot> Positions { get; init; } = [];
    public IReadOnlyList<OrderIntent> PendingIntents { get; init; } = [];
    /// <summary>Mappa account -> gruppo configurata per la distribuzione dei segnali (anti copy-trading).</summary>
    public IReadOnlyList<AccountGroupMapping> AccountGroups { get; init; } = [];

    /// <summary>Profilo completo gruppo/account/Titano quando configurato via PUT /groups.</summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];
}

/// <summary>
/// Associa un account cTrader a un gruppo (tipicamente una prop firm): gli account dello stesso
/// gruppo non ricevono mai lo stesso segnale di ingresso, account di gruppi diversi sì.
/// </summary>
public sealed class AccountGroupMapping
{
    public required string AccountNumber { get; init; }
    public required string GroupId { get; init; }
}

public sealed class SetAccountGroupsRequest
{
    public required string SessionToken { get; init; }
    public required IReadOnlyList<AccountGroupMapping> Accounts { get; init; }
}

/// <summary>
/// Riga di configurazione gruppo/account con profilo Titano opzionale. Più righe con lo stesso
/// <see cref="GroupId"/> condividono lo stesso profilo Titano del gruppo.
/// </summary>
public sealed class TradingGroupRow
{
    public required string GroupId { get; init; }
    public required string AccountNumber { get; init; }

    /// <summary>
    /// Massimo numero di ingressi contemporanei per questo account, <b>sull'insieme delle
    /// strategie e trasversale ai simboli</b>: dieci significa dieci, che siano su un simbolo solo
    /// o su dieci diversi. Zero significa illimitato. Ignorato nel backtest senza Titano.
    ///
    /// <para>Cosa venga contato — solo posizioni, oppure posizioni più ordini pendenti — lo dice
    /// <see cref="ConcurrencyCountMode"/>.</para>
    /// </summary>
    public int MaxConcurrentTrades { get; init; }

    /// <summary>
    /// Cosa conta <see cref="MaxConcurrentTrades"/> per questo account. Vedi
    /// <see cref="Trading.ConcurrencyCountMode"/>.
    /// </summary>
    public ConcurrencyCountMode ConcurrencyCountMode { get; init; }

    /// <summary>Riferimento al setup salvato (rotation-setups); metadata per il client, non usato a runtime.</summary>
    public string? RotationSetupId { get; init; }

    /// <summary>
    /// Cartella di backtest da cui derivano i run Titano del gruppo. Obbligatoria se si applicano
    /// filtri Titano al gruppo. Il run non si indica più: si usa sempre l'ultimo generato in questa
    /// cartella, risolto al momento (<c>TitanoRotationService.ResolveLatestRun</c>) invece che
    /// congelato sulla riga.
    /// </summary>
    public string? TitanoBacktestFolder { get; init; }

    /// <summary>
    /// true: al polling account il manifest del gruppo filtra i template e scala la quantità.
    /// false: il gruppo riceve tutti i template compatibili con l'anti copy-trading, indipendentemente dal manifest.
    ///
    /// <para>Non va confuso con <see cref="CreateTradingSessionRequest.TitanoMode"/>: la modalità dice
    /// <i>dove</i> si sta girando ed è della sessione, questo flag dice soltanto se <i>questo</i> gruppo
    /// subisce il filtro del proprio run. Un gruppo senza <see cref="TitanoBacktestFolder"/> proprio eredita
    /// la decisione della sessione — filtrata in tutte le modalità tranne
    /// <see cref="TitanoFilterMode.Disabled"/>.</para>
    /// </summary>
    public bool ApplyTitanoFilters { get; init; } = true;
}

public sealed class SetTradingGroupsRequest
{
    public required string SessionToken { get; init; }
    public required IReadOnlyList<TradingGroupRow> Rows { get; init; }
}

/// <summary>Risposta al polling di un account per il prossimo segnale da eseguire.</summary>
public sealed class AccountSignalResponse
{
    /// <summary>Intent da eseguire ora, oppure null se non c'è nulla per questo account.</summary>
    public OrderIntent? Intent { get; init; }
    /// <summary>
    /// Motivo diagnostico quando Intent è null: "NoSignal" (nessun segnale libero per i simboli non
    /// occupati dall'account) o "SessionNotRunning" (la sessione non è in esecuzione).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Perché, esattamente, non c'è un intent: quale filtro ha scartato i template e quanti ne
    /// restavano a quel punto. <see cref="Reason"/> resta il codice stabile su cui fare match,
    /// questo è la spiegazione per un umano.
    ///
    /// <para>Esiste perché "NoSignal" copre situazioni molto diverse — nessun template, simbolo
    /// disabilitato sul conto, slot di gruppo occupato, quantità azzerata dalla conversione
    /// dell'account — che dal lato client sono lo stesso identico silenzio.</para>
    /// </summary>
    public string? ReasonDetail { get; init; }
    public int OpenPositions { get; init; }
    public int PendingOrders { get; init; }
    public int MaxConcurrentTrades { get; init; }

    /// <summary>
    /// Il numero effettivamente confrontato con <see cref="MaxConcurrentTrades"/>: gli ingressi in
    /// volo dell'account, deduplicati per IntentId.
    ///
    /// <para>Non è la somma di <see cref="OpenPositions"/> e <see cref="PendingOrders"/>, e la
    /// differenza è il punto: un intent reclamato conta una volta sola, che sia ancora solo un
    /// claim, già un ordine sul broker, o entrambe le cose secondo chi guarda. Sommare i due
    /// conteggi grezzi contava due volte ogni ordine piazzato.</para>
    /// </summary>
    public int InFlight { get; init; }
}

/// <summary>Posizione Piootoo attualmente presente sulla piattaforma del broker.</summary>
public sealed class BrokerPositionSnapshot
{
    public string PositionId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string StrategyCode { get; init; } = string.Empty;

    /// <summary>
    /// Intent di ingresso che ha generato la posizione, letto dalla label del broker
    /// (<c>PiootooLive:{StrategyCode}:{IntentId}</c>). Vuoto per posizioni con label di formato
    /// precedente, che portavano solo il codice strategia.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;
}

/// <summary>Ordine Piootoo ancora pendente sulla piattaforma del broker.</summary>
public sealed class BrokerOrderSnapshot
{
    public string OrderId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string StrategyCode { get; init; } = string.Empty;

    /// <summary>Intent che ha piazzato l'ordine, letto dalla label del broker. Vedi <see cref="BrokerPositionSnapshot.IntentId"/>.</summary>
    public string IntentId { get; init; } = string.Empty;
}

/// <summary>Trade Piootoo presente nello storico della piattaforma broker.</summary>
public sealed class BrokerTradeSnapshot
{
    public string PositionId { get; init; } = string.Empty;
    public DateTime ClosingTimeUtc { get; init; }
}

/// <summary>Stato broker inviato dal cBot insieme a ogni richiesta del prossimo segnale.</summary>
public sealed class AccountSignalPollRequest
{
    public required string SessionToken { get; init; }
    public IReadOnlyList<BrokerPositionSnapshot> Positions { get; init; } = [];
    public IReadOnlyList<BrokerOrderSnapshot> Orders { get; init; } = [];
    public IReadOnlyList<BrokerTradeSnapshot> Trades { get; init; } = [];
}

/// <summary>
/// Registrazione di una chiusura che un client ExternalBroker ha già eseguito applicando la specifica
/// di uscita ricevuta con l'intent di ingresso: Stop Loss/Take Profit nativi del broker, uscita a
/// tempo (<c>CloseAtUtc</c>) o limite di barre (<c>MaxBarsInPosition</c>).
///
/// <para>Registra un intent <see cref="OrderIntentKind.Close"/> per la posizione aperta
/// corrispondente, che il client referenzia nel normale POST /execution-reports per completare la
/// chiusura e generare il PersistedTrade (input delle rotazioni Titano). Gli intent Close emessi
/// dal server per un segnale <see cref="TradeSignal.ExitOnly"/> seguono invece il normale canale
/// di consegna al client.</para>
/// </summary>
/// <summary>
/// Promuove i trade di una sessione a campione sorgente per le rotazioni Titano.
///
/// <para>Serve perché le due metà della catena scrivono e leggono in posti diversi: una sessione
/// persiste in <c>&lt;workspace&gt;/sessions/&lt;id&gt;/</c>, mentre <c>TitanoRotationService</c>
/// legge <c>&lt;workspace&gt;/backtests/&lt;cartella&gt;/trades.json</c>. Senza questo passaggio un
/// backtest eseguito dall'engine cTrader non può alimentare Titano, pur avendone prodotto i
/// trade.</para>
/// </summary>
public sealed class PromoteSessionToBacktestRequest
{
    public required string SessionToken { get; init; }

    /// <summary>Cartella di destinazione sotto <c>&lt;workspace&gt;/backtests/</c>.</summary>
    public required string BacktestFolderName { get; init; }

    /// <summary>
    /// Senza conferma esplicita una cartella esistente non viene toccata: sovrascriverla
    /// cambierebbe la sorgente di run Titano già calcolati, che di quella sorgente portano l'hash.
    /// </summary>
    public bool OverwriteExisting { get; init; }
}

/// <summary>Esito della promozione: dove sono finiti i trade e quanti erano.</summary>
public sealed class PromoteSessionToBacktestResult
{
    public required string WorkspaceId { get; init; }
    public required string BacktestFolderName { get; init; }
    public required int TradeCount { get; init; }
    public required int SignalCount { get; init; }
}

public sealed class CreateExternalCloseIntentRequest
{
    public required string SessionToken { get; init; }
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    /// <summary>Account cTrader che ha deciso la chiusura in locale. Obbligatorio se la sessione ha gruppi account configurati.</summary>
    public string? AccountNumber { get; init; }
    /// <summary>Quantità da chiudere; se omessa o &lt;= 0 il server usa l'intera quantità della posizione aperta.</summary>
    public decimal Quantity { get; init; }
    public string? Reason { get; init; }
}
