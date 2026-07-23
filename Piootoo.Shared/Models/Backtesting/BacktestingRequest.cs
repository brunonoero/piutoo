namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Richiesta di avvio backtesting.
/// StartDate e EndDate devono essere espressi in UTC (allineati al feed JSON).
/// </summary>
public class BacktestingRequest
{
    /// <summary>Workspace obbligatorio che determina il masterfilter e gli output.</summary>
    public string WorkspaceId { get; set; } = string.Empty;
    /// <summary>Nome normalizzato della sottocartella in workspace/backtests.</summary>
    public string BacktestFolderName { get; set; } = string.Empty;
    /// <summary>Consente di sostituire una cartella esistente solo dopo conferma esplicita.</summary>
    public bool OverwriteExistingBacktest { get; set; }
    public List<string> SelectedSymbols { get; set; } = new();
    public List<string> SelectedStrategyIds { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CommissionPerContract { get; set; } = 2.0m;
    public string Name { get; set; } = string.Empty;
    /// <summary>Chiude tutte le posizioni aperte all'ultima barra della settimana di trading (UTC).</summary>
    public bool CloseAllPositionsAtWeekEnd { get; set; } = true;
}
