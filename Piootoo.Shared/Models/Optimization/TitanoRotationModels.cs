namespace Piootoo.Shared.Models.Optimization;

public enum TitanoRotationPeriod { Weekly, Biweekly, Monthly }
public enum TitanoRunStatus { Completed, Failed }
public enum TitanoStrategyStatus { Enabled, Reduced, Disabled, HardStopped }
public enum TitanoWalkForwardMode { Rolling, Expanding }

public sealed class TitanoRotationRequest
{
    public required string WorkspaceId { get; init; }
    public required string BacktestFolder { get; init; }
    public string SetupName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TitanoRotationPeriod RotationPeriod { get; init; } = TitanoRotationPeriod.Weekly;
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
    public DateTime? BiweeklyAnchorUtc { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public decimal InitialCapital { get; init; } = 100_000m;
    public int MinimumTrades { get; init; } = 1;
    public int ShortWindowDays { get; init; } = 90;
    public int LongWindowDays { get; init; } = 365;
    public int MovingAverageWindowDays { get; init; } = 90;
    public decimal MinimumShortReturn { get; init; } = 0m;
    public decimal MinimumLongReturn { get; init; } = 0m;
    public decimal MinimumZScore { get; init; } = -1.5m;
    public decimal MaximumZScore { get; init; } = 2.5m;
    public decimal MaximumCurrentDrawdown { get; init; } = 0.15m;
    public decimal MaximumObservedDrawdown { get; init; } = 0.25m;
    public decimal MaximumReturnVolatility { get; init; } = 0.10m;
    public bool RequireEquityAboveMovingAverage { get; init; } = true;
    public decimal ReenableMaximumCurrentDrawdown { get; init; } = 0.10m;
    public decimal DisableCompositeScore { get; init; } = 0.40m;
    public decimal ReenableCompositeScore { get; init; } = 0.60m;
    public int MinimumPassingFilters { get; init; } = 4;
    public int CooldownPeriodsAfterOff { get; init; } = 2;
    public int MinimumOnPeriods { get; init; } = 1;
    public decimal HardStopDrawdown { get; init; } = 0.35m;
    /// <summary>
    /// Sizing per percentile invece che per soglie assolute.
    ///
    /// <para>true (default): lo score usato per il sizing è il <b>rango</b> della strategia rispetto
    /// alle altre dello stesso periodo, e l'allocazione è una curva continua tra
    /// <see cref="MinimumAllocationMultiplier"/> e <see cref="MaximumAllocationMultiplier"/>. È ciò
    /// che serve a una rotazione: ordinare le strategie, non giudicarle in assoluto.</para>
    ///
    /// <para>false: comportamento storico, media dei voti assoluti mappata sui
    /// <see cref="SizingTiers"/>. Con le scale attuali i voti sono quasi costanti — drawdown e
    /// volatilità restano sopra 0.9, la performance lunga è inchiodata a 0.5 — quindi lo score si
    /// concentra in una banda strettissima e finisce quasi sempre nello stesso scaglione.</para>
    ///
    /// <para><b>L'accensione e lo spegnimento non passano da qui.</b> Restano governati dai cancelli
    /// assoluti (filtri minimi superati, drawdown, hard stop, isteresi, cooldown): il percentile
    /// decide solo <i>quanto</i> allocare a una strategia già ritenuta eleggibile. Altrimenti in un
    /// portafoglio piccolo la peggiore verrebbe spenta sempre, per definizione di rango.</para>
    /// </summary>
    public bool CrossSectionalSizing { get; init; } = true;

    /// <summary>
    /// Allocazione della strategia eleggibile peggiore del periodo. Non è zero: una strategia che
    /// supera i cancelli assoluti resta operativa, solo con size ridotta.
    /// </summary>
    public decimal MinimumAllocationMultiplier { get; init; } = 0.25m;

    /// <summary>Allocazione della strategia migliore del periodo.</summary>
    public decimal MaximumAllocationMultiplier { get; init; } = 1m;

    /// <summary>
    /// Granularità dell'allocazione: la curva continua viene arrotondata a questo passo, così i
    /// moltiplicatori restano leggibili e confrontabili tra periodi. 0 = nessun arrotondamento.
    /// </summary>
    public decimal AllocationStep { get; init; } = 0.05m;

    public decimal CommissionPerUnit { get; init; }
    public decimal SlippagePerUnit { get; init; }
    public decimal MinimumIntentQuantity { get; init; } = 1m;
    public decimal QuantityStep { get; init; } = 1m;
    public IReadOnlyList<TitanoSizingTier> SizingTiers { get; init; } =
    [
        new() { MinimumScore = 0.80m, AllocationMultiplier = 1m },
        new() { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
        new() { MinimumScore = 0.40m, AllocationMultiplier = 0.25m },
        new() { MinimumScore = 0m, AllocationMultiplier = 0m }
    ];
    public int CalibrationPeriods { get; init; } = 8;
    public int EvaluationPeriods { get; init; } = 4;
    public TitanoWalkForwardMode WalkForwardMode { get; init; } = TitanoWalkForwardMode.Rolling;
}

public sealed class TitanoSizingTier
{
    public decimal MinimumScore { get; init; }
    public decimal AllocationMultiplier { get; init; }
}

/// <summary>
/// Parametri riutilizzabili di una rotazione Titano. Il contesto del run
/// (workspace, backtest e intervallo date) resta escluso dal setup.
/// </summary>
public sealed class TitanoRotationSetup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public TitanoRotationPeriod RotationPeriod { get; set; } = TitanoRotationPeriod.Weekly;
    public int MinimumTrades { get; set; } = 1;
    public int ShortWindowDays { get; set; } = 90;
    public int LongWindowDays { get; set; } = 365;
    public int MovingAverageWindowDays { get; set; } = 90;
    public decimal MinimumShortReturn { get; set; }
    public decimal MinimumLongReturn { get; set; }
    public decimal MinimumZScore { get; set; } = -1.5m;
    public decimal MaximumZScore { get; set; } = 2.5m;
    public decimal MaximumCurrentDrawdown { get; set; } = 0.15m;
    public decimal MaximumObservedDrawdown { get; set; } = 0.25m;
    public decimal MaximumReturnVolatility { get; set; } = 0.10m;
    public bool RequireEquityAboveMovingAverage { get; set; } = true;
    public decimal ReenableMaximumCurrentDrawdown { get; set; } = 0.10m;
    public decimal DisableCompositeScore { get; set; } = 0.40m;
    public decimal ReenableCompositeScore { get; set; } = 0.60m;
    public int MinimumPassingFilters { get; set; } = 4;
    public int CooldownPeriodsAfterOff { get; set; } = 2;
    public int MinimumOnPeriods { get; set; } = 1;
    public decimal HardStopDrawdown { get; set; } = 0.35m;
    public decimal CommissionPerUnit { get; set; }
    public decimal SlippagePerUnit { get; set; }
    public List<TitanoSizingTier> SizingTiers { get; set; } =
    [
        new() { MinimumScore = 0.80m, AllocationMultiplier = 1m },
        new() { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
        new() { MinimumScore = 0.40m, AllocationMultiplier = 0.25m },
        new() { MinimumScore = 0m, AllocationMultiplier = 0m }
    ];
    public int CalibrationPeriods { get; set; } = 8;
    public int EvaluationPeriods { get; set; } = 4;
    public TitanoWalkForwardMode WalkForwardMode { get; set; } = TitanoWalkForwardMode.Rolling;
}

public sealed class TitanoRunInfo
{
    public required string RunId { get; init; }
    public required string WorkspaceId { get; init; }
    public required string BacktestFolder { get; init; }
    public TitanoRunStatus Status { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public required string ManifestPath { get; init; }
    public int PeriodCount { get; init; }
}

public sealed class TitanoRotationManifest
{
    public const int CurrentSchemaVersion = 2;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string RunId { get; init; }
    public required TitanoRotationRequest Config { get; init; }
    public required string SourceTradesSha256 { get; init; }
    public required string MasterFilterHash { get; init; }
    public required string ConfigSha256 { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public List<TitanoRotationDecision> Periods { get; init; } = [];
    /// <summary>Equity di portafoglio sui soli trade master, senza filtro Titano né costi simulati.</summary>
    public List<TitanoEquityPoint> OriginalEquity { get; init; } = [];
    public List<TitanoEquityPoint> FilteredEquity { get; init; } = [];
    public List<TitanoWalkForwardResult> WalkForward { get; init; } = [];
    public List<TitanoHardStopReset> HardStopResets { get; init; } = [];
}

public sealed class TitanoRotationDecision
{
    public required string PeriodId { get; init; }
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public DateTime EffectiveToUtc { get; init; }
    public required string SourceBacktestFolder { get; init; }
    public required string MasterFilterHash { get; init; }
    public List<TitanoStrategyState> Strategies { get; init; } = [];
}

public sealed class TitanoStrategyState
{
    public required string StrategyCode { get; init; }
    public bool Enabled { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public TitanoStrategyStatus State { get; init; }
    public int CooldownRemaining { get; init; }
    public int ConsecutiveOnPeriods { get; init; }
    public bool HardStopped { get; init; }
    public int PassingFilters { get; init; }
    public int TotalFilters { get; init; }
    public IReadOnlyList<TitanoFilterVote> Votes { get; init; } = [];

    /// <summary>
    /// Score usato per il sizing. Con <c>CrossSectionalSizing</c> è il percentile della strategia
    /// nel periodo (0 = peggiore, 1 = migliore); altrimenti è la media dei voti assoluti.
    /// </summary>
    public decimal Score { get; init; }

    /// <summary>
    /// Media dei voti assoluti, sempre valorizzata anche quando il sizing usa il percentile.
    /// Serve a distinguere "è la peggiore del gruppo" da "va male": un percentile basso in un
    /// periodo in cui vanno tutte bene non è un allarme, un RawScore basso sì.
    /// </summary>
    public decimal RawScore { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public TitanoPeriodMetrics Metrics { get; init; } = new();
    /// <summary>Stato dello stesso periodo precedente per questa strategia; null se è il primo periodo osservato.</summary>
    public TitanoStrategyStatus? PreviousState { get; init; }
    /// <summary>
    /// Descrive il cambio di stato rispetto al periodo precedente: "NewlyTracked", "Unchanged",
    /// "EnabledToDisabled", "DisabledToEnabled", "HardStopTriggered", "HardStopReleased", "AllocationChanged".
    /// Pensato per individuare a colpo d'occhio (o via script) le rotazioni dove una strategia ha cambiato
    /// comportamento, senza dover fare il diff manuale di due periodi.
    /// </summary>
    public required string TransitionType { get; init; }
    /// <summary>
    /// Incongruenze rilevate automaticamente in questo stato (es. Enabled=true con AllocationMultiplier=0,
    /// oppure HardStopped=true con Enabled=true). Vuoto se nessuna anomalia è stata rilevata; utile per
    /// individuare rapidamente bug nel calcolo della rotazione senza rileggere tutta la logica.
    /// </summary>
    public IReadOnlyList<string> AnomalyFlags { get; init; } = [];
}

public sealed class TitanoFilterVote
{
    public required string Filter { get; init; }
    public bool Passed { get; init; }
    public decimal Score { get; init; }
    public required string Reason { get; init; }
}

public sealed class TitanoWalkForwardResult
{
    public required string EvaluationPeriodId { get; init; }
    public DateTime CalibrationFromUtc { get; init; }
    public DateTime CalibrationToUtc { get; init; }
    public DateTime EvaluationFromUtc { get; init; }
    public DateTime EvaluationToUtc { get; init; }
    public decimal InSampleNetProfit { get; init; }
    public decimal OutOfSampleNetProfit { get; init; }
    public bool InSampleOnlyImprovementWarning { get; init; }
}

public sealed class TitanoHardStopReset
{
    public required string ResetId { get; init; }
    public required string StrategyCode { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public required string RequestedBy { get; init; }
    public required string Reason { get; init; }
}

public sealed class TitanoHardStopResetRequest
{
    public required string StrategyCode { get; init; }
    public required string RequestedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTime RequestedAtUtc { get; init; }
}

public sealed class TitanoPeriodMetrics
{
    public int Trades { get; init; }
    public int WinningTrades { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal Commission { get; init; }
    public decimal CurrentEquity { get; init; }
    public decimal ShortStartEquity { get; init; }
    public decimal LongStartEquity { get; init; }
    public decimal ShortReturn { get; init; }
    public decimal LongReturn { get; init; }
    public decimal MovingAverageEquity { get; init; }
    public decimal EquityStandardDeviation { get; init; }
    public decimal ZScore { get; init; }
    public decimal CurrentDrawdown { get; init; }
    public decimal MaximumDrawdown { get; init; }
    public decimal ReturnVolatility { get; init; }
}

public sealed class TitanoEquityPoint
{
    public DateTime TimestampUtc { get; init; }
    public required string TradeId { get; init; }
    public required string StrategyCode { get; init; }
    public decimal NetProfit { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public decimal Costs { get; init; }
    public decimal Balance { get; init; }
    public decimal Equity { get; init; }
}

public sealed class TitanoEffectiveStrategy
{
    public required string StrategyCode { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public TitanoStrategyStatus State { get; init; }
    public int CooldownRemaining { get; init; }
    public bool HardStopped { get; init; }
    /// <summary>Motivo sintetico della decisione nel periodo corrente (copiato da TitanoStrategyState.Reason).</summary>
    public string? Reason { get; init; }
    public decimal Score { get; init; }
    public int PassingFilters { get; init; }
    public int TotalFilters { get; init; }
    public int ConsecutiveOnPeriods { get; init; }
}

public sealed class TitanoEffectiveStrategies
{
    public required string RunId { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string? PeriodId { get; init; }
    public IReadOnlyList<string> MasterStrategies { get; init; } = [];
    public IReadOnlyList<string> TitanoEnabledStrategies { get; init; } = [];
    public IReadOnlyList<string> EffectiveStrategies { get; init; } = [];
    public IReadOnlyList<TitanoEffectiveStrategy> StrategyStates { get; init; } = [];

    /// <summary>
    /// true quando l'istante richiesto cade dentro un periodo del manifest, cioè quando esiste
    /// davvero una decisione di rotazione. false significa "Titano non ha nulla da dire adesso":
    /// tipicamente perché il manifest è stato costruito su un backtest storico e il tempo live è
    /// oltre l'ultimo periodo, oppure perché si è nel primo periodo, che non ha storia su cui
    /// calibrare. Va distinto da "tutte le strategie disabilitate": senza questo flag una sessione
    /// live smetteva silenziosamente di valutare qualsiasi strategia.
    /// </summary>
    public bool HasActivePeriod { get; init; }

    /// <summary>
    /// True quando la decisione non viene dal periodo che contiene il timestamp — che non esiste — ma
    /// dall'ultimo periodo del manifest. Succede solo in <c>TitanoFilterMode.Realtime</c>: la
    /// rotazione più recente resta in vigore finché non se ne calcola una nuova. Va registrato,
    /// perché indica che l'analisi Titano andrebbe rigenerata.
    /// </summary>
    public bool UsedLatestPeriod { get; init; }

    /// <summary>
    /// Estremi [inizio, fine) del periodo di rotazione applicato. Permettono a un chiamante che
    /// itera nel tempo (il loop di backtest) di sapere fino a quando la decisione resta valida,
    /// invece di ririsolvere la rotazione a ogni barra.
    /// </summary>
    public DateTime? PeriodFromUtc { get; init; }

    /// <summary>Fine esclusiva del periodo applicato. Vedi <see cref="PeriodFromUtc"/>.</summary>
    public DateTime? PeriodToUtc { get; init; }

    /// <summary>Intervallo coperto dal manifest, per spiegare un <see cref="HasActivePeriod"/> false.</summary>
    public DateTime? ManifestFromUtc { get; init; }

    /// <summary>Intervallo coperto dal manifest, per spiegare un <see cref="HasActivePeriod"/> false.</summary>
    public DateTime? ManifestToUtc { get; init; }
}
