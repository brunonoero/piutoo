using Piootoo.Shared.Models.Trading;

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

    /// <summary>
    /// Modalità rispetto al filtro Titano. Identica a quella delle sessioni
    /// (<see cref="TitanoFilterMode"/>), così backtest interno ed engine esterno cTrader si
    /// comportano allo stesso modo.
    ///
    /// <para><see cref="TitanoFilterMode.Disabled"/> — nessun filtro: è il run che produce il
    /// <c>trades.json</c> su cui l'analisi Titano calcola offline le rotazioni.</para>
    /// <para><see cref="TitanoFilterMode.BacktestRotationFile"/> — per ogni barra vengono valutate
    /// solo le strategie abilitate dal periodo di rotazione che la contiene. Richiede
    /// <see cref="TitanoRunId"/> e <see cref="TitanoBacktestFolder"/>.</para>
    /// <para><see cref="TitanoFilterMode.Realtime"/> non ha senso in backtest e viene rifiutata.</para>
    /// </summary>
    public TitanoFilterMode TitanoMode { get; set; } = TitanoFilterMode.Disabled;

    /// <summary>Run Titano da applicare. Obbligatorio con <see cref="TitanoFilterMode.BacktestRotationFile"/>.</summary>
    public string? TitanoRunId { get; set; }

    /// <summary>Cartella di backtest che contiene il run Titano indicato.</summary>
    public string? TitanoBacktestFolder { get; set; }

    /// <summary>
    /// Account del workspace la cui tabella di conversione viene applicata al run: il moltiplicatore
    /// contratto scala la size dei segnali e il simbolo account viene riportato in
    /// <c>signals.json</c>. Null o vuoto = nessuna conversione (1 a 1).
    /// </summary>
    public string? AccountId { get; set; }
}
