using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Schema dei log diagnostici prodotti da un backtest locale.
///
/// Sono due artefatti complementari, entrambi pensati per essere letti in modo automatico
/// (anche da un agente) dopo l'esecuzione:
///
/// - <see cref="LogFileName"/>: JSON Lines append-only, una riga per evento rilevante
///   (segnale, ingresso, uscita, anomalia). Non contiene le valutazioni che restituiscono
///   Hold né gli skip: sarebbero milioni di righe e nasconderebbero il segnale utile.
/// - <see cref="SummaryFileName"/>: un solo oggetto con i contatori aggregati per strategia
///   (incluse valutazioni e skip per motivo) e una lista di diagnosi generate automaticamente.
///   È il file da leggere per capire perché un backtest non ha prodotto trade.
/// </summary>
public static class BacktestDiagnosticsSchema
{
    public const int Version = 1;
    public const string LogFileName = "backtest-log.jsonl";
    public const string SummaryFileName = "backtest-summary.json";
}

/// <summary>Tipo di evento registrato in <see cref="BacktestDiagnosticsSchema.LogFileName"/>.</summary>
public enum BacktestLogEventType
{
    /// <summary>Inizio o fine del job, con la configurazione effettiva.</summary>
    Run,

    /// <summary>Esito del pre-caricamento di un datasource (symbol + timeframe).</summary>
    DataSource,

    /// <summary>Una strategia ha emesso un segnale diverso da Hold.</summary>
    Signal,

    /// <summary>L'engine ha aperto una posizione.</summary>
    Entry,

    /// <summary>L'engine ha chiuso una posizione.</summary>
    Exit,

    /// <summary>Incoerenza rilevata dall'engine o dalla diagnostica.</summary>
    Anomaly
}

/// <summary>Motivo per cui l'engine ha chiuso una posizione.</summary>
public enum TradeExitReason
{
    Unknown,

    /// <summary>Stop loss (o break even attivato) raggiunto.</summary>
    StopLoss,

    /// <summary>Take profit raggiunto.</summary>
    TakeProfit,

    /// <summary>Orario assoluto di flat richiesto dalla strategia (CloseAtUtc).</summary>
    TimeExit,

    /// <summary>Superato il numero massimo di barre in posizione.</summary>
    MaxBars,

    /// <summary>Segnale opposto della stessa strategia sullo stesso simbolo.</summary>
    OppositeSignal,

    /// <summary>
    /// Non più prodotto: le strategie non emettono segnali di chiusura. Lo slot resta per non
    /// rinumerare i valori successivi nei log e nei backtest già archiviati.
    /// </summary>
    [Obsolete("Meccanismo CloseOnly rimosso: l'uscita è descritta nel segnale di ingresso.")]
    CloseOnly,

    /// <summary>Chiusura forzata all'ultima barra della settimana di trading.</summary>
    WeekEnd,

    /// <summary>Chiusura forzata a fine backtest.</summary>
    EndOfRun
}

/// <summary>
/// Riga del log JSONL. I campi non pertinenti al tipo di evento restano null: lo schema è
/// volutamente piatto e uniforme, così una riga si legge senza conoscere il tipo in anticipo.
/// </summary>
public sealed class BacktestLogEvent
{
    public int SchemaVersion { get; init; } = BacktestDiagnosticsSchema.Version;

    /// <summary>Progressivo di scrittura, ricostruisce l'ordine anche dopo un merge di file.</summary>
    public long Sequence { get; set; }

    public required BacktestLogEventType Type { get; init; }
    public string? JobId { get; init; }

    /// <summary>Timestamp della barra in elaborazione (non l'ora di sistema).</summary>
    public DateTime? BarTimeUtc { get; init; }

    public string? StrategyCode { get; init; }
    public string? StrategyName { get; init; }
    public string? Symbol { get; init; }
    public int? TimeframeMinutes { get; init; }

    public SignalType? Side { get; init; }
    public TradeOrderType? OrderType { get; init; }
    public decimal? Price { get; init; }
    public decimal? Quantity { get; init; }

    /// <summary>Stop loss in punti dal prezzo di ingresso, come lo vede l'engine.</summary>
    public decimal? StopLossPoints { get; init; }

    /// <summary>Take profit in punti dal prezzo di ingresso, come lo vede l'engine.</summary>
    public decimal? TakeProfitPoints { get; init; }

    public DateTime? EntryTimeUtc { get; init; }
    public decimal? EntryPrice { get; init; }
    public int? BarsInPosition { get; init; }

    public TradeExitReason? ExitReason { get; init; }
    public decimal? GrossProfit { get; init; }
    public decimal? NetProfit { get; init; }
    public decimal? Commission { get; init; }

    public decimal? Equity { get; init; }
    public decimal? Balance { get; init; }
    public int? OpenPositionsCount { get; init; }

    /// <summary>Motivo dichiarato dalla strategia (campo Reason del segnale) o descrizione dell'evento.</summary>
    public string? Message { get; init; }

    /// <summary>Coppie chiave/valore libere per contesto aggiuntivo, senza allargare lo schema.</summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

/// <summary>Esito del pre-caricamento di un datasource, riportato nel riepilogo.</summary>
public sealed class BacktestDataSourceSummary
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }
    public int CandleCount { get; init; }
    public DateTime? FirstBarUtc { get; init; }
    public DateTime? LastBarUtc { get; init; }

    /// <summary>False quando il feed non copre l'intero intervallo richiesto dal backtest.</summary>
    public bool CoversRequestedRange { get; init; }

    /// <summary>Descrizione del problema quando il datasource è vuoto o incompleto.</summary>
    public string? Warning { get; init; }
}

/// <summary>
/// Contatori aggregati di una singola strategia. Gli skip non finiscono nel log evento per
/// evento: si contano qui, ed è qui che si vede se una strategia non ha mai avuto dati a
/// sufficienza per essere valutata davvero.
/// </summary>
public sealed class BacktestStrategySummary
{
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>Barre in cui la strategia era allineata al proprio timeframe e quindi candidata alla valutazione.</summary>
    public long Scheduled { get; set; }

    /// <summary>Valutazioni realmente eseguite (Evaluate chiamata).</summary>
    public long Evaluations { get; set; }

    /// <summary>Skip perché il datasource non esiste o è vuoto.</summary>
    public long SkippedNoData { get; set; }

    /// <summary>Skip perché le candele disponibili sono meno di RequiredCandles.</summary>
    public long SkippedNotEnoughCandles { get; set; }

    /// <summary>Skip perché l'ultima candela disponibile è troppo vecchia rispetto alla barra corrente.</summary>
    public long SkippedStaleCandle { get; set; }

    /// <summary>Eccezioni catturate durante la valutazione.</summary>
    public long Errors { get; set; }

    public long HoldSignals { get; set; }
    public long BuySignals { get; set; }
    public long SellSignals { get; set; }

    /// <summary>
    /// Segnali di ingresso privi di qualsiasi condizione di uscita (StopLoss, TakeProfit,
    /// CloseAtUtc, MaxBarsInPosition). L'engine non può chiuderli: la posizione resta aperta fino
    /// alla chiusura tecnica di fine settimana o fine run. Va letto come un difetto della strategia.
    /// </summary>
    public long SignalsWithoutExitSpec { get; set; }

    public int Trades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal Commission { get; set; }

    /// <summary>Conteggio delle uscite per motivo (chiave = <see cref="TradeExitReason"/>).</summary>
    public Dictionary<string, int> ExitReasons { get; init; } = new(StringComparer.Ordinal);

    public DateTime? FirstSignalUtc { get; set; }
    public DateTime? LastSignalUtc { get; set; }

    /// <summary>Diagnosi automatica quando i contatori indicano un problema; null se tutto coerente.</summary>
    public string? Diagnosis { get; set; }
}

/// <summary>Riepilogo completo di un run di backtest.</summary>
public sealed class BacktestRunSummary
{
    public int SchemaVersion { get; init; } = BacktestDiagnosticsSchema.Version;
    public required string JobId { get; init; }
    public string SetupName { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public string BacktestFolder { get; init; } = string.Empty;

    public DateTime RequestedStartUtc { get; init; }
    public DateTime RequestedEndUtc { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public double DurationSeconds { get; set; }

    public int MinTimeframeMinutes { get; init; }
    public long PlannedIterations { get; init; }
    public long ProcessedIterations { get; set; }

    /// <summary>Barre in cui almeno un prezzo era disponibile e l'engine ha aggiornato il mark-to-market.</summary>
    public long MarkedToMarketBars { get; set; }

    public decimal InitialCapital { get; init; }
    public decimal FinalEquity { get; set; }
    public decimal TotalNetProfit { get; set; }
    public decimal MaxDrawdown { get; set; }

    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public int OpenPositionsAtEnd { get; set; }

    public string Outcome { get; set; } = "Unknown";
    public string? ErrorMessage { get; set; }

    public IReadOnlyList<BacktestDataSourceSummary> DataSources { get; set; } = [];
    public IReadOnlyList<BacktestStrategySummary> Strategies { get; set; } = [];

    /// <summary>
    /// Problemi rilevati automaticamente confrontando i contatori: datasource vuoti, strategie
    /// mai valutate, segnali che non si trasformano mai in trade, e simili.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}
