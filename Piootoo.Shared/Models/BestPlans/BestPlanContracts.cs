using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Shared.Models.BestPlans;

/// <summary>
/// Un piano messo in evidenza: la fotografia di un backtest che ha funzionato bene, presa al momento
/// della promozione e tenuta fuori dai workspace (<c>[BasePath]\best-plans\{Id}\</c>).
///
/// <para><b>E' una copia, non un riferimento.</b> Tutto cio' che la lista e il dettaglio mostrano —
/// cifre per anno, curva di equity, strategie — sta in questo oggetto e nella cartella accanto, cosi'
/// la pulizia delle cartelle di backtest non lo tocca. Per la stessa ragione i campi non si
/// ricalcolano dopo: una promozione ripetuta sullo stesso backtest sostituisce la fotografia.</para>
///
/// <para>Trasversale ai workspace: <see cref="WorkspaceId"/> e <see cref="WorkspaceName"/> dicono da
/// dove viene, ma l'elenco dei best plan non dipende dal workspace scelto nella console.</para>
/// </summary>
public sealed class BestPlan
{
    public const string FileName = "best-plan.json";

    /// <summary>Nome della sottocartella con gli artefatti copiati dal backtest.</summary>
    public const string ArtifactsDirectoryName = "artifacts";

    /// <summary>
    /// Frase del <c>409</c> di una promozione ripetuta. Il client la riconosce per chiedere se
    /// sostituire: gli errori arrivano come testo, e un codice di stato solo non distingue questo
    /// caso dal backtest senza niente da fotografare.
    /// </summary>
    public const string AlreadyPromotedMessage = "e' gia' fra i best plan";

    /// <summary><c>{workspaceId}__{cartella di backtest}</c>: una promozione per backtest.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Codice del piano che il backtest ha eseguito; vuoto per un run neutro sul masterfilter.</summary>
    public string PlanCode { get; set; } = string.Empty;

    /// <summary>Nome del piano al momento della promozione, se il piano esiste ancora nel workspace.</summary>
    public string PlanName { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = string.Empty;

    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>La cartella di backtest da cui e' stata presa la fotografia.</summary>
    public string BacktestFolder { get; set; } = string.Empty;

    public BacktestOrigin Origin { get; set; } = BacktestOrigin.Unknown;

    /// <summary>Su quali prezzi e' girato il run (<c>RunPriceSource.FeedLabel</c>), vuoto se ignoto.</summary>
    public string PriceSource { get; set; } = string.Empty;

    /// <summary>Versione di chi ha generato i fill: il server per un run interno, il cBot per uno esterno.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Quando e' stato eseguito il backtest.</summary>
    public DateTime? ExecutedUtc { get; set; }

    /// <summary>Primo e ultimo istante del periodo simulato.</summary>
    public DateTime? StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    /// <summary>Quando il backtest e' stato promosso.</summary>
    public DateTime PromotedUtc { get; set; }

    public decimal InitialCapital { get; set; }

    public decimal FinalEquity { get; set; }

    public decimal NetProfit { get; set; }

    public decimal NetProfitPercent { get; set; }

    /// <summary>Drawdown massimo in valuta, dal picco di equity.</summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>Drawdown massimo in percentuale dal picco di equity.</summary>
    public decimal MaxDrawdownPercent { get; set; }

    public int TotalTrades { get; set; }

    public int WinningTrades { get; set; }

    /// <summary>
    /// Come e' costruita la curva: <c>mark-to-market</c> per un run interno, <c>realizzata</c> quando
    /// viene dai soli trade chiusi (run del cBot, run interni interrotti). Il drawdown della seconda
    /// e' misurato fra chiusure e risulta piu' basso: due best plan con curve diverse non si
    /// confrontano sul drawdown senza guardare questa colonna.
    /// </summary>
    public string EquitySource { get; set; } = string.Empty;

    public List<BestPlanYear> Years { get; set; } = new();

    public List<BestPlanStrategy> Strategies { get; set; } = new();

    /// <summary>
    /// Curva di equity ridotta alla risoluzione che un grafico puo' disegnare, con gli estremi e il
    /// drawdown peggiore di ogni colonna conservati. Nell'elenco arriva ancora piu' ridotta.
    /// </summary>
    public List<BestPlanEquityPoint> Equity { get; set; } = new();

    /// <summary>File copiati nella cartella <see cref="ArtifactsDirectoryName"/>.</summary>
    public List<string> Artifacts { get; set; } = new();

    /// <summary>True quando fra gli artefatti c'e' il report HTML del run.</summary>
    public bool HasHtmlReport { get; set; }
}

/// <summary>Resoconto di un anno solare, con la stessa aritmetica del report HTML.</summary>
public sealed class BestPlanYear
{
    public int Year { get; set; }

    public decimal StartEquity { get; set; }

    public decimal EndEquity { get; set; }

    public decimal NetProfit { get; set; }

    /// <summary>Profit in percentuale dell'equity di inizio anno.</summary>
    public decimal ReturnPercent { get; set; }

    public decimal MaxDrawdown { get; set; }

    public decimal MaxDrawdownPercent { get; set; }

    public int Trades { get; set; }

    public int WinningTrades { get; set; }

    public int LosingTrades { get; set; }
}

/// <summary>Una strategia del piano, con i trade che ha chiuso nel backtest.</summary>
public sealed class BestPlanStrategy
{
    public string StrategyCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    public int Trades { get; set; }

    public int WinningTrades { get; set; }

    public decimal NetProfit { get; set; }
}

public sealed class BestPlanEquityPoint
{
    public DateTime TimeUtc { get; set; }

    public decimal Equity { get; set; }

    /// <summary>Percentuale dal picco, positiva.</summary>
    public decimal DrawdownPercent { get; set; }
}

public sealed class PromoteBestPlanRequest
{
    public string WorkspaceId { get; set; } = string.Empty;

    public string BacktestFolder { get; set; } = string.Empty;

    /// <summary>
    /// Sostituisce una fotografia gia' presente dello stesso backtest. Senza, la promozione ripetuta
    /// e' un <c>409</c>: la console chiede prima di sovrascrivere.
    /// </summary>
    public bool Overwrite { get; set; }
}
