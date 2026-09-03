using System.Text.Json.Serialization;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Configurazione operativa riutilizzabile, salvata nel workspace. Una sessione ne acquisisce uno
/// snapshot alla creazione: modificare il piano non cambia le sessioni già esistenti.
///
/// <para>La collezione <see cref="Groups"/> è la fonte autorevole di gruppo/account. I campi
/// singoli (<see cref="GroupId"/>, <see cref="AccountNumber"/>, ecc.) restano come mirror della
/// prima riga per compatibilità con i <c>plans.json</c> legacy e con i client che li leggono
/// ancora in modo diretto.</para>
/// </summary>
public sealed class TradingPlan
{
    public required string WorkspaceId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }

    /// <summary>Righe gruppo/account del piano. Almeno una. È la configurazione canonica.</summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];

    /// <summary>Mirror legacy della prima riga: usato nei file vecchi e come default di sessione.</summary>
    public string GroupId { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public int MaxConcurrentTrades { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico
    /// (attivo ovunque tranne nel backtest sorgente, che deve produrre il campione completo).
    /// Vedi <c>docs/domini/distribuzione-multi-account.md</c> §4.
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

    /// <summary>
    /// Cosa conta <c>MaxConcurrentTrades</c>. Mirror legacy della prima riga, come gli altri campi
    /// singoli: la fonte autorevole è <see cref="TradingGroupRow.ConcurrencyCountMode"/>.
    /// </summary>
    public ConcurrencyCountMode ConcurrencyCountMode { get; init; }

    // Nessun InitialCapital sul piano (docs/decisioni.md 2026-08-05): le sessioni aperte da un piano
    // sono sempre ExternalBroker, dove l'equity non è del server e ogni account porta il proprio
    // InitialBalance — che diventa BalanceScale ed è ciò che dimensiona davvero. Il capitale iniziale
    // resta un parametro del singolo run di backtest (BacktestingRequest.InitialCapital).
    //
    // Nessun Instruments (docs/decisioni.md 2026-08-05): DollarsPerPoint viene dal registro
    // strumenti (InstrumentRegistry), la granularità di volume (minimo/passo/arrotondamento) dalla
    // riga della tabella di conversione dell'account — è una proprietà del broker, non del piano.
    public decimal CommissionPerContract { get; init; } = 2m;

    /// <summary>
    /// Cosa il conto permette di tenere — la notte, il fine settimana — e a che ora taglia quando
    /// non lo permette. Sta sul piano perche' e' una proprieta' di come si opera, non del singolo
    /// run: da qui scende nella sessione, nel descriptor e infine nel cBot, che la esegue al posto
    /// di qualsiasi parametro locale. La stessa policy va nella <c>BacktestingRequest</c>,
    /// altrimenti backtest e conto vero chiudono in istanti diversi.
    ///
    /// <para><b>Il piano ha l'ultima parola.</b> Un conto prop che impone il flat di sessione taglia
    /// a prescindere da cosa la strategia vorrebbe; solo se il piano concede di tenere, decidono
    /// motore e strategia. Vedi <see cref="AccountHoldingPolicy"/>.</para>
    /// </summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    /// <summary>
    /// Solo per leggere i <c>plans.json</c> scritti prima che la finestra del fine settimana
    /// entrasse in <see cref="Holding"/>. <c>TradingPlanService.NormalizeLoadedPlan</c> la travasa e
    /// la azzera, cosi' non viene mai riscritta: due posti che dichiarano lo stesso orario sono la
    /// premessa della divergenza che questa gerarchia esiste per chiudere.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeekEndFlatPolicy? WeekEndFlat { get; init; }

    public PositionSizingConfig PositionSizing { get; init; } = new();
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed class SaveTradingPlanRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Righe gruppo/account da salvare. Se vuota, il server ricostruisce una riga dai campi
    /// legacy singoli (compatibilità con i client e i test già esistenti).
    /// </summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];

    public string? GroupId { get; init; }
    public string? AccountNumber { get; init; }
    public int MaxConcurrentTrades { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico.
    /// Vedi <see cref="TradingPlan.EnforceConcurrencyLimits"/>.
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

    /// <summary>Cosa conta <c>MaxConcurrentTrades</c>. Vedi <see cref="TradingPlan.ConcurrencyCountMode"/>.</summary>
    public ConcurrencyCountMode ConcurrencyCountMode { get; init; }

    public decimal CommissionPerContract { get; init; } = 2m;

    /// <summary>Vedi <see cref="TradingPlan.Holding"/>.</summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    public PositionSizingConfig PositionSizing { get; init; } = new();
}

/// <summary>
/// Cosa conta <c>MaxConcurrentTrades</c>. È un parametro del piano perché la risposta giusta
/// dipende dal tipo di motore: chi entra a mercato non ha ordini in attesa da contare, chi entra
/// in breakout ne ha uno per strategia per tutta la barra.
///
/// <para>Il limite è comunque <b>per account e trasversale ai simboli</b>: non esiste più un
/// vincolo che leghi un account a un solo ingresso per simbolo. Resta invece sempre attiva la
/// guardia di identità (stessa strategia, stesso simbolo), che non è concorrenza ma doppione.
/// Vedi <c>docs/domini/distribuzione-multi-account.md</c> §2.</para>
/// </summary>
public enum ConcurrencyCountMode
{
    /// <summary>
    /// Posizioni riempite <b>più</b> ordini pendenti presso il broker. È il default e il
    /// comportamento storico: tetto rigido, nessuno sfondamento possibile, ma un ordine stop mai
    /// riempito occupa uno slot per tutta la barra in cui vive.
    /// </summary>
    PositionsAndPendingOrders = 0,

    /// <summary>
    /// Solo posizioni riempite. Gli ordini pendenti non consumano budget, quindi tutti gli intent
    /// della barra arrivano a mercato: su motori breakout è ciò che evita di perdere l'unico
    /// livello che sarebbe stato toccato, perché a priori non si sa quale sarà.
    ///
    /// <para>Il prezzo da pagare è che il tetto è garantito solo <i>a valle del fill</i>: quando i
    /// fill lo raggiungono il client cancella gli ordini rimasti (comportamento OCO), e in quella
    /// finestra due stop possono riempirsi insieme. Da valutare quando le regole del conto —
    /// FTMO e simili — puniscono l'esposizione istantanea.</para>
    /// </summary>
    PositionsOnly = 1
}

/// <summary>
/// Che tipo di run sta aprendo il cBot. È l'unico interruttore fra i backtest che il progetto
/// distingue, e li nomina invece di farli dedurre da una combinazione di flag.
/// </summary>
public enum TradingRunProfile
{
    /// <summary>
    /// Comportamento storico: decide il piano con <c>EnforceConcurrencyLimits</c>. È il default e
    /// non cambia nulla per le configurazioni esistenti.
    /// </summary>
    DalPiano = 0,

    /// <summary>
    /// Backtest sorgente: tutte le strategie del masterfilter del workspace e nessun lucchetto di
    /// concorrenza, così ogni segnale diventa un intent. È il run che produce il campione completo:
    /// applicargli vincoli operativi falserebbe la sorgente.
    /// </summary>
    BacktestSorgente = 1,

    // Il valore 2 era BacktestTitano (rotazioni storiche). Rimosso con Titano: non si riusa, così i
    // run già salvati con quel profilo non vengono riletti come qualcos'altro.

    /// <summary>
    /// Backtest a filtro statico: le strategie sono quelle del masterfilter del workspace e i
    /// lucchetti di concorrenza e distribuzione sono attivi.
    ///
    /// <para>È il termine di paragone di <see cref="BacktestSorgente"/>, che risponde a "quanto
    /// rende ogni strategia da sola": qui si misura quanto rende lo stesso insieme di strategie con
    /// i vincoli operativi addosso, cioè quanta parte della differenza è soltanto l'effetto del
    /// tetto di concorrenza.</para>
    /// </summary>
    BacktestStaticFilter = 3
}

/// <summary>
/// Richiesta idempotente del cBot. La chiave (piano, modalità client, execution key) identifica
/// un'esecuzione: la stessa richiesta riprende la sessione, una chiave nuova ne crea una nuova.
/// </summary>
public sealed class OpenTradingPlanSessionRequest
{
    public required string PlanCode { get; init; }
    public required ClientRunMode ClientRunMode { get; init; }
    public required string ExecutionKey { get; init; }

    /// <summary>
    /// Account cTrader che apre la sessione. Deve appartenere alle righe del piano. Se omesso,
    /// si usa il primo account del piano.
    /// </summary>
    public string? AccountNumber { get; init; }

    /// <summary>
    /// True (default) = distribuzione multi-account: le righe del piano diventano i gruppi della
    /// sessione, <c>POST /bars</c> restituisce template non assegnati e ogni account li reclama da
    /// <c>GET /accounts/{n}/signals</c>.
    ///
    /// <para>False = esecuzione diretta: la sessione non ha gruppi, quindi <c>POST /bars</c>
    /// restituisce intent già assegnati e il client li esegue. Serve ai cBot che non implementano
    /// il claim; il piano continua a fornire workspace, sizing, capitale e metadata
    /// strumenti. La sessione è per singolo account e non è condivisibile.</para>
    /// </summary>
    public bool DistributeToAccounts { get; init; } = true;

    /// <summary>
    /// Profilo del run. <c>null</c> o <see cref="TradingRunProfile.DalPiano"/> conservano il
    /// comportamento storico. I profili <c>Backtest*</c> valgono solo con
    /// <see cref="ClientRunMode.Backtest"/>: aprirli in realtime è rifiutato all'apertura invece di
    /// produrre in silenzio un run che non è quello che dichiara di essere.
    /// </summary>
    public TradingRunProfile? RunProfile { get; init; }
}
