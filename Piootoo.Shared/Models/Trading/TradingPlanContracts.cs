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
    public string? RotationSetupId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico
    /// (attivo ovunque tranne nel backtest senza filtro Titano, che deve produrre il campione
    /// sorgente completo). Esplicito per poter variare concorrenza e rotazione in modo indipendente:
    /// vedi <c>docs/domini/distribuzione-multi-account.md</c> §4.
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
    /// Quando il conto deve essere piatto per il fine settimana. Sta sul piano perche' e' una
    /// proprieta' di come si opera, non del singolo run: da qui scende nella sessione, nel
    /// descriptor e infine nel cBot, che lo esegue al posto del proprio parametro. Lo stesso
    /// numero va nella <c>BacktestingRequest</c>, altrimenti backtest e conto vero chiudono il
    /// venerdi' in due istanti diversi. Vedi <see cref="WeekEndFlatPolicy"/>.
    /// </summary>
    public WeekEndFlatPolicy WeekEndFlat { get; init; } = WeekEndFlatPolicy.Default;

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
    public string? RotationSetupId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico.
    /// Vedi <see cref="TradingPlan.EnforceConcurrencyLimits"/>.
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

    /// <summary>Cosa conta <c>MaxConcurrentTrades</c>. Vedi <see cref="TradingPlan.ConcurrencyCountMode"/>.</summary>
    public ConcurrencyCountMode ConcurrencyCountMode { get; init; }

    public decimal CommissionPerContract { get; init; } = 2m;

    /// <summary>Vedi <see cref="TradingPlan.WeekEndFlat"/>.</summary>
    public WeekEndFlatPolicy WeekEndFlat { get; init; } = WeekEndFlatPolicy.Default;

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
/// Che tipo di run sta aprendo il cBot. È l'unico interruttore fra i due backtest che il progetto
/// distingue, e li nomina invece di farli dedurre da una combinazione di flag: <c>ApplyTitanoFilters</c>
/// nel piano più <c>EnforceConcurrencyLimits</c> descrivono la stessa scelta in due posti, e due
/// dichiarazioni della stessa cosa prima o poi divergono.
/// </summary>
public enum TradingRunProfile
{
    /// <summary>
    /// Comportamento storico: decide il piano con <c>ApplyTitanoFilters</c> e
    /// <c>EnforceConcurrencyLimits</c>. È il default e non cambia nulla per le configurazioni
    /// esistenti.
    /// </summary>
    DalPiano = 0,

    /// <summary>
    /// Backtest sorgente: nessun filtro Titano (tutte le strategie del masterfilter del workspace)
    /// e nessun lucchetto di concorrenza, così ogni segnale diventa un intent. È il run che produce
    /// il <c>trades.json</c> su cui Titano calcola le rotazioni: applicargli vincoli operativi
    /// falserebbe la sorgente. Vedi <c>docs/domini/titano-rotation.md</c>.
    /// </summary>
    BacktestSorgente = 1,

    /// <summary>
    /// Backtest filtrato: rotazioni storiche già generate da Titano
    /// (<c>TitanoFilterMode.BacktestRotationFile</c>) e lucchetti di distribuzione attivi. Serve a
    /// misurare cosa avrebbe fatto il sistema con il filtro, quindi i vincoli operativi ci vogliono.
    /// Richiede che il piano indichi la cartella del run Titano.
    /// </summary>
    BacktestTitano = 2,

    /// <summary>
    /// Backtest a filtro statico: le strategie sono quelle del masterfilter del workspace — nessuna
    /// rotazione Titano — ma i lucchetti di concorrenza e distribuzione sono attivi.
    ///
    /// <para>È il termine di paragone fra gli altri due. <see cref="BacktestSorgente"/> risponde a
    /// "quanto rende ogni strategia da sola", <see cref="BacktestTitano"/> a "quanto rende il
    /// sistema con il filtro dinamico": in mezzo manca "quanto rende lo stesso insieme di strategie
    /// con i soli vincoli operativi", cioè quanta parte della differenza è merito della rotazione e
    /// quanta è soltanto l'effetto del tetto di concorrenza. Senza questo profilo quella domanda si
    /// risponde solo cambiando a mano due flag del piano fra un run e l'altro, e la differenza fra
    /// i due run non resta scritta da nessuna parte.</para>
    ///
    /// <para>Il nome dice la differenza vera con <see cref="BacktestTitano"/>: filtro
    /// <b>statico</b> (il masterfilter, fisso per tutto il run) contro filtro <b>dinamico</b> (le
    /// rotazioni, che cambiano nel tempo). I lucchetti sono uguali nei due, quindi nominarli non
    /// distinguerebbe niente.</para>
    ///
    /// <para>Non richiede la cartella del run Titano: non ne legge nessuna.</para>
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
    /// il claim; il piano continua a fornire workspace, sizing, capitale, Titano e metadata
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
