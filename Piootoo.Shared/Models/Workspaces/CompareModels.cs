using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Shared.Models.Workspaces;

/// <summary>
/// Richiesta di confronto fra due run dello stesso workspace. L'ordine non conta: il server
/// riconosce quale dei due e' il run del cBot e quale il backtest interno leggendo
/// <c>origin.json</c>, non fidandosi della posizione nella selezione.
/// </summary>
public sealed class StartCompareRequest
{
    public string WorkspaceId { get; set; } = string.Empty;

    public string FirstBacktest { get; set; } = string.Empty;

    public string SecondBacktest { get; set; } = string.Empty;
}

/// <summary>
/// Un confronto avviato dalla console: la cartella <c>compare-NNNN</c> e' gia' creata e riempita
/// quando il job nasce, l'analisi dello strumento gira dopo. Lo stato riusa quello dei backtest
/// perche' il client lo interroga allo stesso modo.
/// </summary>
public sealed class CompareJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();

    public BacktestingJobStatus Status { get; set; } = BacktestingJobStatus.Pending;

    /// <summary>Nome della cartella, es. <c>compare-0041</c>.</summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>Percorso della cartella sul server.</summary>
    public string FolderPath { get; set; } = string.Empty;

    public string CbotBacktest { get; set; } = string.Empty;

    public string InternalBacktest { get; set; } = string.Empty;

    /// <summary>L'ultima riga scritta dallo strumento, o la fase del job.</summary>
    public string? ProgressMessage { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
