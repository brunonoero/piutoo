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

public sealed class WorkspaceBacktestInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
    public int ResultsCount { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool HasResults => ResultsCount > 0;
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

    /// <summary>Tabella di conversione simboli, una riga per simbolo.</summary>
    public List<AccountSymbolMapping> SymbolMappings { get; set; } = new();
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
