using System.ComponentModel;

namespace Piootoo.Shared.Models.Optimization;

public enum TitanoRotationPeriod { Weekly, Biweekly, Monthly }
public enum TitanoRunStatus { Completed, Failed }
public enum TitanoStrategyStatus { Enabled, Reduced, Disabled, HardStopped }
public enum TitanoWalkForwardMode { Rolling, Expanding }

/// <summary>
/// Configurazione di un run di rotazione. È un <c>record</c> perché è un oggetto di sola
/// configurazione: consente <c>with</c> per derivarne varianti senza ripetere tutti i parametri, e
/// dà uguaglianza per valore — utile quando si confrontano due configurazioni.
/// </summary>
public sealed record TitanoRotationRequest
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
    // set e non init: l'editor di collezioni del PropertyGrid costruisce la voce vuota e poi ne
    // assegna le proprietà. Con init-only gli scaglioni sarebbero visibili ma non modificabili.
    [DisplayName("Score minimo")]
    public decimal MinimumScore { get; set; }

    [DisplayName("Moltiplicatore")]
    public decimal AllocationMultiplier { get; set; }

    public override string ToString() => $"score ≥ {MinimumScore:0.##} → ×{AllocationMultiplier:0.##}";
}

/// <summary>
/// Parametri riutilizzabili di una rotazione Titano. Il contesto del run
/// (workspace, backtest e intervallo date) resta escluso dal setup.
/// </summary>
public sealed class TitanoRotationSetup
{
    // Le annotazioni System.ComponentModel servono al PropertyGrid del dettaglio setup nella
    // console. Stanno qui e non in una classe adattatrice del client perché un adattatore sarebbe
    // una seconda dichiarazione del modello: al primo parametro aggiunto resterebbe indietro, e un
    // parametro assente dalla UI viene salvato al proprio default senza che nulla lo segnali.
    // Sono metadati, non logica: Piootoo.Shared resta senza dipendenze verso gli altri progetti.

    [Browsable(false)]
    public string Id { get; set; } = string.Empty;

    [Browsable(false)]
    public string Name { get; set; } = string.Empty;

    [Browsable(false)]
    public string Description { get; set; } = string.Empty;

    [Browsable(false)]
    public DateTime? UpdatedAt { get; set; }

    [Category("1. Calendario")]
    [DisplayName("Periodo di rotazione")]
    [Description("Ogni quanto si ricalcola quali strategie sono abilitate.")]
    public TitanoRotationPeriod RotationPeriod { get; set; } = TitanoRotationPeriod.Weekly;

    [Category("1. Calendario")]
    [DisplayName("Trade minimi")]
    [Description("Trade minimi nella finestra breve perché la strategia sia valutabile.")]
    public int MinimumTrades { get; set; } = 1;

    [Category("2. Finestre di misura")]
    [DisplayName("Finestra breve (giorni)")]
    [Description("Orizzonte della performance di breve e della volatilità.")]
    public int ShortWindowDays { get; set; } = 90;

    [Category("2. Finestre di misura")]
    [DisplayName("Finestra lunga (giorni)")]
    [Description("Orizzonte della performance di lungo.")]
    public int LongWindowDays { get; set; } = 365;

    [Category("2. Finestre di misura")]
    [DisplayName("Finestra media mobile (giorni)")]
    [Description("Ampiezza della media mobile dell'equity.")]
    public int MovingAverageWindowDays { get; set; } = 90;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Rendimento breve minimo")]
    [Description("Frazione, non percentuale: 0,05 significa 5%.")]
    public decimal MinimumShortReturn { get; set; }

    [Category("3. Soglie di ammissione")]
    [DisplayName("Rendimento lungo minimo")]
    [Description("Frazione, non percentuale: 0,05 significa 5%.")]
    public decimal MinimumLongReturn { get; set; }

    [Category("3. Soglie di ammissione")]
    [DisplayName("Z-score minimo")]
    [Description("Distanza minima dell'equity dalla propria media mobile.")]
    public decimal MinimumZScore { get; set; } = -1.5m;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Z-score massimo")]
    [Description("Anche l'eccesso positivo disabilita: è surriscaldamento, non forza.")]
    public decimal MaximumZScore { get; set; } = 2.5m;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Drawdown corrente massimo")]
    [Description("Frazione dal picco. 0,15 significa 15%.")]
    public decimal MaximumCurrentDrawdown { get; set; } = 0.15m;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Drawdown storico massimo")]
    [Description("Massimo drawdown osservato sull'intera storia disponibile.")]
    public decimal MaximumObservedDrawdown { get; set; } = 0.25m;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Volatilità massima")]
    [Description("Deviazione standard dei rendimenti trade nella finestra breve.")]
    public decimal MaximumReturnVolatility { get; set; } = 0.10m;

    [Category("3. Soglie di ammissione")]
    [DisplayName("Equity sopra la media mobile")]
    [Description("Se attivo, un'equity sotto la propria media mobile non è ammessa.")]
    public bool RequireEquityAboveMovingAverage { get; set; } = true;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Drawdown per riattivare")]
    [Description("Soglia più severa di quella di disattivazione: serve a non rientrare troppo presto.")]
    public decimal ReenableMaximumCurrentDrawdown { get; set; } = 0.10m;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Score di disattivazione")]
    [Description("Sotto questo score composito la strategia passa OFF.")]
    public decimal DisableCompositeScore { get; set; } = 0.40m;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Score di riattivazione")]
    [Description("Sopra questo score la strategia torna ON, se il cooldown è esaurito.")]
    public decimal ReenableCompositeScore { get; set; } = 0.60m;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Voti minimi da superare")]
    [Description("Quanti dei cinque voti devono passare perché la strategia sia ammessa.")]
    public int MinimumPassingFilters { get; set; } = 4;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Periodi di cooldown dopo OFF")]
    [Description("Periodi da attendere prima di poter riattivare.")]
    public int CooldownPeriodsAfterOff { get; set; } = 2;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Periodi minimi in ON")]
    [Description("Impedisce un OFF precoce, ma non prevale sull'hard stop.")]
    public int MinimumOnPeriods { get; set; } = 1;

    [Category("4. Anti-whipsaw")]
    [DisplayName("Hard stop drawdown")]
    [Description("Disattivazione immediata, che prevale su periodi minimi e cooldown.")]
    public decimal HardStopDrawdown { get; set; } = 0.35m;

    /// <summary>Vedi <see cref="TitanoRotationRequest.CrossSectionalSizing"/>.</summary>
    [Category("5. Allocazione")]
    [DisplayName("Sizing per percentile")]
    [Description("Se attivo l'allocazione viene dal rango fra le strategie del periodo e gli scaglioni sono ignorati.")]
    public bool CrossSectionalSizing { get; set; } = true;

    /// <summary>Vedi <see cref="TitanoRotationRequest.MinimumAllocationMultiplier"/>.</summary>
    [Category("5. Allocazione")]
    [DisplayName("Moltiplicatore minimo")]
    [Description("Pavimento della curva di allocazione. Solo con il sizing per percentile.")]
    public decimal MinimumAllocationMultiplier { get; set; } = 0.25m;

    /// <summary>Vedi <see cref="TitanoRotationRequest.MaximumAllocationMultiplier"/>.</summary>
    [Category("5. Allocazione")]
    [DisplayName("Moltiplicatore massimo")]
    [Description("Tetto della curva. Lo stato Abilitato significa 'al tetto', non 'moltiplicatore 1'.")]
    public decimal MaximumAllocationMultiplier { get; set; } = 1m;

    /// <summary>Vedi <see cref="TitanoRotationRequest.AllocationStep"/>.</summary>
    [Category("5. Allocazione")]
    [DisplayName("Passo di arrotondamento")]
    [Description("Granularità del moltiplicatore di allocazione.")]
    public decimal AllocationStep { get; set; } = 0.05m;

    [Category("6. Costi")]
    [DisplayName("Commissione per unità")]
    public decimal CommissionPerUnit { get; set; }

    [Category("6. Costi")]
    [DisplayName("Slippage per unità")]
    public decimal SlippagePerUnit { get; set; }

    [Category("5. Allocazione")]
    [DisplayName("Scaglioni di sizing")]
    [Description("Usati solo quando il sizing per percentile è spento.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public List<TitanoSizingTier> SizingTiers { get; set; } =
    [
        new() { MinimumScore = 0.80m, AllocationMultiplier = 1m },
        new() { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
        new() { MinimumScore = 0.40m, AllocationMultiplier = 0.25m },
        new() { MinimumScore = 0m, AllocationMultiplier = 0m }
    ];
    [Category("7. Walk-forward")]
    [DisplayName("Periodi di calibrazione")]
    public int CalibrationPeriods { get; set; } = 8;

    [Category("7. Walk-forward")]
    [DisplayName("Periodi di valutazione")]
    public int EvaluationPeriods { get; set; } = 4;

    [Category("7. Walk-forward")]
    [DisplayName("Modalità")]
    [Description("Rolling sposta la finestra, Expanding la allunga tenendo fermo l'inizio.")]
    public TitanoWalkForwardMode WalkForwardMode { get; set; } = TitanoWalkForwardMode.Rolling;
}

/// <summary>
/// Voce di elenco di un setup di rotazione salvato: quanto basta a popolare una combo senza
/// deserializzare l'intero <see cref="TitanoRotationSetup"/>.
/// </summary>
public class TitanoSetupInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
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

    /// <summary>
    /// Spiega un <see cref="WalkForward"/> vuoto o parziale. Vuoto quando la validazione è completa.
    /// Una tabella vuota nel report era indistinguibile da "nessun problema rilevato".
    /// </summary>
    public string WalkForwardNote { get; init; } = string.Empty;

    /// <summary>
    /// Trade del master filter la cui <c>EntryTimeUtc</c> cade fuori dai periodi efficaci del run —
    /// il primo periodo è solo osservazione e l'ultimo non produce decisione. Sono presenti in
    /// <see cref="OriginalEquity"/> e assenti da <see cref="FilteredEquity"/>: senza dichiararlo, il
    /// confronto fra le due curve sembra a parità di campione e non lo è.
    /// </summary>
    public int TradesOutsideCoverage { get; init; }
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

    /// <summary>
    /// true quando la finestra di valutazione è più corta di <c>EvaluationPeriods</c> perché il run
    /// finisce prima. Il confronto IS/OOS resta valido ma è su un campione ridotto: senza questo
    /// flag l'ultima riga sembrava confrontabile con le precedenti.
    /// </summary>
    public bool EvaluationTruncated { get; init; }
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
