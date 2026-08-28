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
    /// <summary>
    /// Cosa il conto simulato permette di tenere — la notte, il fine settimana — e a che ora taglia
    /// quando non lo permette.
    ///
    /// <para>E' <b>lo stesso tipo</b> che il piano porta in sessione e nel descriptor, e non un
    /// interruttore parallelo: un run e il live dello stesso piano sono confrontabili per
    /// costruzione, invece che per disciplina di chi compila la richiesta. Il precedente
    /// <c>CloseAllPositionsAtWeekEnd</c> e' oggi <see cref="AccountHoldingPolicy.AllowOverweek"/>
    /// rovesciato. Vedi <see cref="AccountHoldingPolicy"/>.</para>
    /// </summary>
    public AccountHoldingPolicy Holding { get; set; } = AccountHoldingPolicy.Default;

    /// <summary>
    /// Scarta i pending il cui livello e' gia' oltrepassato quando l'ordine nasce, come fa il cBot
    /// con <c>RejectWrongSideLevels</c>. Il perche' sta su
    /// <c>PiootooTradingService.RejectWrongSideLevels</c>; spegnerlo serve solo a misurare la
    /// fedelta' del porting rispetto al motore di ricerca, che quei livelli li riempie
    /// all'apertura.
    /// </summary>
    public bool RejectWrongSideLevels { get; set; } = true;

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

    // Nessun account: il backtest interno è neutro rispetto ai conti. Un run è capitale iniziale
    // + strategie del masterfilter + datafeed, con conversione simbolo e moltiplicatori fissi a 1.
    // Il motivo sta in docs/decisioni.md (2026-08-05): questo run è il campione sorgente di Titano,
    // e una size legata al conto farebbe dipendere le rotazioni dal capitale invece che dalle
    // strategie. Conversione e scala per conto restano sulle sessioni ExternalBroker, dove il
    // segnale deve diventare un ordine eseguibile su un conto reale.
}
