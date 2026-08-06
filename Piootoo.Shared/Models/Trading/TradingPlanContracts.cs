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

    // Nessun InitialCapital sul piano (docs/decisioni.md 2026-08-05): le sessioni aperte da un piano
    // sono sempre ExternalBroker, dove l'equity non è del server e ogni account porta il proprio
    // InitialBalance — che diventa BalanceScale ed è ciò che dimensiona davvero. Il capitale iniziale
    // resta un parametro del singolo run di backtest (BacktestingRequest.InitialCapital).
    //
    // Nessun Instruments (docs/decisioni.md 2026-08-05): DollarsPerPoint viene dal registro
    // strumenti (InstrumentRegistry), la granularità di volume (minimo/passo/arrotondamento) dalla
    // riga della tabella di conversione dell'account — è una proprietà del broker, non del piano.
    public decimal CommissionPerContract { get; init; } = 2m;
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

    public decimal CommissionPerContract { get; init; } = 2m;
    public PositionSizingConfig PositionSizing { get; init; } = new();
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
    BacktestTitano = 2
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
