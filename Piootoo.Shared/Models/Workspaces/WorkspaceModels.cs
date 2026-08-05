using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Models.Workspaces;

public sealed class WorkspaceMasterFilter
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}

public sealed class WorkspaceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StrategiesCount { get; set; }
}

/// <summary>
/// Chi ha prodotto una cartella di backtest. Da quando le sessioni di backtest aperte da piano
/// scrivono anch'esse sotto <c>backtests/</c>, i due tipi convivono nello stesso albero: l'origine
/// va dichiarata alla creazione, non dedotta a posteriori dai file presenti.
/// </summary>
public enum BacktestOrigin
{
    /// <summary>Cartella precedente all'introduzione del marcatore.</summary>
    Unknown,

    /// <summary>Motore di backtesting del server (<c>PiootooBacktestingService</c>).</summary>
    Internal,

    /// <summary>Sessione di trading eseguita da un engine esterno, tipicamente un cBot cTrader.</summary>
    ExternalBroker
}

/// <summary>
/// Marcatore scritto nella cartella del backtest alla creazione (<c>origin.json</c>). Serve a
/// distinguere un run interno da uno prodotto dall'engine esterno senza euristiche sui file
/// presenti: <c>backtest-summary.json</c> manca anche in un run interno interrotto, quindi usarlo
/// come indizio etichetterebbe come esterno un backtest che non lo è.
/// </summary>
public sealed class BacktestOriginInfo
{
    public const string FileName = "origin.json";

    public BacktestOrigin Origin { get; set; } = BacktestOrigin.Unknown;
    public DateTime CreatedUtc { get; set; }

    /// <summary>Valorizzati solo per l'origine esterna.</summary>
    public string? PlanCode { get; set; }
    public string? ExecutionKey { get; set; }
    public string? SessionId { get; set; }
}

public sealed class WorkspaceBacktestInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
    public int ResultsCount { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool HasResults => ResultsCount > 0;

    public BacktestOrigin Origin { get; set; } = BacktestOrigin.Unknown;

    /// <summary>Piano che ha prodotto il run, per l'origine esterna.</summary>
    public string? PlanCode { get; set; }
}

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}

/// <summary>
/// Riga della tabella di conversione di un account: traduce il simbolo Piootoo nel simbolo
/// usato dal broker e scala la size del contratto.
/// </summary>
public sealed class AccountSymbolMapping
{
    /// <summary>Simbolo Piootoo, come compare nelle strategie del catalogo (es. <c>@NQ</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Simbolo equivalente sull'account/broker (es. <c>USDTEC</c>).</summary>
    public string AccountSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Fattore moltiplicativo del contratto: 1 contratto Piootoo vale
    /// <c>ContractMultiplier</c> contratti sull'account. Es. 0.1 se il contratto del broker
    /// vale 100k contro 1M del contratto Piootoo. Deve essere maggiore di zero.
    /// </summary>
    public decimal ContractMultiplier { get; set; } = 1m;

    /// <summary>
    /// Quantità minima eseguibile su questo simbolo presso il broker dell'account, espressa nei
    /// contratti del broker. Sotto questa soglia l'intent vale zero e non viene consegnato: meglio
    /// nessun ordine che un ordine di taglia non eseguibile.
    /// </summary>
    public decimal MinimumQuantity { get; set; } = 1m;

    /// <summary>Passo di volume del broker; la quantità viene arrotondata per difetto a un suo multiplo.</summary>
    public decimal QuantityStep { get; set; } = 1m;

    /// <summary>
    /// Granularità del volume: contratti interi per i future, passo di volume per i CFD.
    ///
    /// <para>Vive qui e non sul piano perché è una proprietà della coppia <b>broker/strumento</b>, e
    /// solo al claim — quando il conto è noto e la quantità è già stata convertita nei contratti del
    /// broker — arrotondare significa qualcosa. Vedi <c>docs/decisioni.md</c> (2026-08-05).</para>
    /// </summary>
    public QuantityRoundingMode RoundingMode { get; set; } = QuantityRoundingMode.BrokerVolumeStep;

    /// <summary>Se false il simbolo resta configurato ma non è operativo sull'account.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>Account di trading globale, condiviso da tutti i workspace.</summary>
public sealed class WorkspaceAccount
{
    /// <summary>Slug derivato dal nome; identifica univocamente l'account nel registro globale.</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Codice account del broker, lo stesso usato in <c>AccountGroupMapping</c>.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Gruppo anti copy-trading (tipicamente la prop firm) usato dalle trading session.</summary>
    public string GroupId { get; set; } = string.Empty;

    public string Broker { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    /// <summary>Balance iniziale dell'account nella valuta indicata.</summary>
    public decimal InitialBalance { get; set; }

    public bool Enabled { get; set; } = true;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Codice della tabella di conversione simboli associata (<see cref="SymbolConversion.Code"/>),
    /// dal registro globale. Vuoto = nessuna conversione, l'account opera 1 a 1.
    /// </summary>
    public string SymbolConversionCode { get; set; } = string.Empty;
}

/// <summary>
/// Tabella di conversione simboli nominata, definita nel registro globale (fuori da workspace e
/// account): un account la referenzia per <see cref="Code"/> invece di portarne una copia propria,
/// così più account possono condividere la stessa tabella.
/// </summary>
public sealed class SymbolConversion
{
    /// <summary>Identificativo univoco scelto dall'utente, stabile: è ciò che gli account referenziano.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Tabella di conversione, una riga per simbolo.</summary>
    public List<AccountSymbolMapping> Mappings { get; set; } = new();

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

/// <summary>Contenuto del registro globale <c>accounts/symbol-conversions.json</c>.</summary>
public sealed class SymbolConversionsFile
{
    public List<SymbolConversion> Conversions { get; set; } = new();
}

/// <summary>Contenuto del registro globale <c>accounts/accounts.json</c>.</summary>
public sealed class WorkspaceAccountsFile
{
    public List<string> Groups { get; set; } = new();
    public List<WorkspaceAccount> Accounts { get; set; } = new();
}

/// <summary>Richiesta di creazione di un gruppo account globale.</summary>
public sealed class CreateAccountGroupRequest
{
    public string GroupId { get; set; } = string.Empty;
}
