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
    //
    // Tre convenzioni, tutte verificate da TitanoParameterMetadataTests:
    //
    // 1. Ogni proprietà visibile porta un [TitanoLevel]. La console usa BrowsableAttributes per
    //    mostrare la sola vista Base; una proprietà senza livello sparirebbe da quella vista senza
    //    che nulla lo segnali, quindi l'assenza è un errore e non un default.
    // 2. Le categorie sono numerate per imporre l'ordine di lettura del PropertyGrid, che altrimenti
    //    è alfabetico. La numerazione segue la sequenza logica della decisione: quando ruoto, su
    //    cosa misuro, chi ammetto, quanto lo tengo, quanto gli alloco.
    // 3. Ogni frazione porta [TypeConverter(typeof(PercentTypeConverter))]. Il modello resta in
    //    frazioni — è il contratto verso il server — ma non esiste un campo in cui si debba
    //    indovinare se 15 significhi 15% o 1500%.

    [Browsable(false)]
    public string Id { get; set; } = string.Empty;

    [Browsable(false)]
    public string Name { get; set; } = string.Empty;

    [Browsable(false)]
    public string Description { get; set; } = string.Empty;

    [Browsable(false)]
    public DateTime? UpdatedAt { get; set; }

    [Category("1. Quando ruotare")]
    [DisplayName("Ogni quanto ricalcolare")]
    [Description("Ogni quanto Titano rifà i conti e decide quali strategie sono accese. " +
                 "Settimanale = ogni lunedì alle 00:00 UTC.")]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public TitanoRotationPeriod RotationPeriod { get; set; } = TitanoRotationPeriod.Weekly;

    [Category("2. Su cosa misurare")]
    [DisplayName("Reattività: giorni osservati (breve)")]
    [Description("Quanti giorni di storia recente guarda il giudizio di breve. " +
                 "È il parametro che decide quanto Titano è reattivo: 90 giorni con rotazione " +
                 "settimanale significa che l'ultima settimana pesa un tredicesimo della misura, " +
                 "quindi una settimana pessima non spegne nulla. Per una reattività davvero " +
                 "settimanale servono 21-35 giorni, ma controlla che restino abbastanza trade.")]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public int ShortWindowDays { get; set; } = 90;

    [Category("2. Su cosa misurare")]
    [DisplayName("Giorni osservati (lungo)")]
    [Description("Orizzonte del giudizio di fondo. Non può essere inferiore alla finestra breve.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int LongWindowDays { get; set; } = 365;

    [Category("2. Su cosa misurare")]
    [DisplayName("Giorni della media mobile")]
    [Description("Ampiezza della media mobile dell'equity, usata dal filtro di trend e come " +
                 "riferimento dello z-score.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int MovingAverageWindowDays { get; set; } = 90;

    [Category("2. Su cosa misurare")]
    [DisplayName("Trade minimi per esprimere un giudizio")]
    [Description("Sotto questo numero di trade nella finestra breve la strategia è considerata " +
                 "non valutabile e non passa il voto di performance recente.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int MinimumTrades { get; set; } = 1;

    [Category("3. Chi ammettere")]
    [DisplayName("Voti da superare (su 5)")]
    [Description("Titano dà cinque voti a ogni strategia: performance breve, performance lunga, " +
                 "z-score, drawdown, volatilità. Questo è il numero che deve passarne per essere " +
                 "ammessa. 5 = severissimo, 3 = permissivo.")]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public int MinimumPassingFilters { get; set; } = 4;

    [Category("3. Chi ammettere")]
    [DisplayName("Spegni sopra questo drawdown")]
    [Description("Perdita massima dal picco oltre la quale la strategia si spegne. È il cancello " +
                 "che agisce più spesso.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public decimal MaximumCurrentDrawdown { get; set; } = 0.15m;

    [Category("3. Chi ammettere")]
    [DisplayName("Drawdown storico massimo tollerato")]
    [Description("Peggior drawdown mai visto sull'intera storia. Serve a escludere strategie che " +
                 "adesso stanno bene ma sono già andate molto male una volta.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MaximumObservedDrawdown { get; set; } = 0.25m;

    [Category("3. Chi ammettere")]
    [DisplayName("Volatilità massima dei rendimenti")]
    [Description("Deviazione standard dei rendimenti dei singoli trade nella finestra breve. " +
                 "Esclude chi guadagna a strappi.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MaximumReturnVolatility { get; set; } = 0.10m;

    [Category("3. Chi ammettere")]
    [DisplayName("Rendimento minimo di breve")]
    [Description("Rendimento richiesto sulla finestra breve. 0 significa 'basta non perdere'.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MinimumShortReturn { get; set; }

    [Category("3. Chi ammettere")]
    [DisplayName("Rendimento minimo di lungo")]
    [Description("Rendimento richiesto sulla finestra lunga. 0 significa 'basta non perdere'.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MinimumLongReturn { get; set; }

    [Category("3. Chi ammettere")]
    [DisplayName("Richiedi equity sopra la media mobile")]
    [Description("Se attivo, una strategia la cui equity sta sotto la propria media mobile non " +
                 "passa il voto di performance recente, per quanto buono sia il rendimento.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public bool RequireEquityAboveMovingAverage { get; set; } = true;

    [Category("3. Chi ammettere")]
    [DisplayName("Z-score minimo")]
    [Description("Quanto l'equity può stare sotto la propria media mobile, in deviazioni standard. " +
                 "Negativo perché sotto la media è la condizione da limitare.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MinimumZScore { get; set; } = -1.5m;

    [Category("3. Chi ammettere")]
    [DisplayName("Z-score massimo")]
    [Description("Anche l'eccesso positivo esclude: un'equity molto sopra la propria media è " +
                 "surriscaldamento, non forza, e statisticamente rientra.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal MaximumZScore { get; set; } = 2.5m;

    [Category("4. Quanto insistere")]
    [DisplayName("Riaccendi solo sotto questo drawdown")]
    [Description("Soglia di rientro, più severa di quella di spegnimento: fra le due c'è una zona " +
                 "morta in cui la strategia resta spenta. È ciò che evita che si accenda e spenga " +
                 "a ogni periodo. Se la porti al livello della soglia di spegnimento l'isteresi " +
                 "sparisce.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public decimal ReenableMaximumCurrentDrawdown { get; set; } = 0.10m;

    [Category("4. Quanto insistere")]
    [DisplayName("Periodi di fermo dopo uno spegnimento")]
    [Description("Quanti periodi la strategia resta ferma prima di poter anche solo essere " +
                 "riconsiderata. Con rotazione settimanale, 2 = due settimane piene di fermo. " +
                 "Scaduto il fermo il rientro non è automatico: devono tornare buoni anche i voti.")]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public int CooldownPeriodsAfterOff { get; set; } = 2;

    [Category("4. Quanto insistere")]
    [DisplayName("Blocco definitivo sopra questo drawdown")]
    [Description("Oltre questa perdita la strategia è bloccata a tempo indeterminato e si sblocca " +
                 "solo a mano. Prevale su tutto, anche sui periodi minimi in ON. " +
                 "Deve essere maggiore della soglia di spegnimento.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public decimal HardStopDrawdown { get; set; } = 0.35m;

    [Category("4. Quanto insistere")]
    [DisplayName("Periodi minimi in ON prima di poter spegnere")]
    [Description("Concede alla strategia appena accesa un margine di periodi prima di poterla " +
                 "spegnere, per non reagire al primo inciampo. Non protegge dal blocco definitivo.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int MinimumOnPeriods { get; set; } = 1;

    /// <summary>Vedi <see cref="TitanoRotationRequest.CrossSectionalSizing"/>.</summary>
    [Category("5. Quanto allocare")]
    [DisplayName("Alloca per classifica (consigliato)")]
    [Description("Attivo: l'allocazione dipende dalla posizione in classifica della strategia " +
                 "rispetto alle altre dello stesso periodo, su una curva continua fra il minimo e " +
                 "il massimo qui sotto. Spento: si torna agli scaglioni assoluti della categoria 6, " +
                 "che con le scale attuali finiscono quasi sempre nello stesso gradino. " +
                 "In entrambi i casi questo non decide chi è acceso, solo con quanta size.")]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public bool CrossSectionalSizing { get; set; } = true;

    /// <summary>Vedi <see cref="TitanoRotationRequest.MinimumAllocationMultiplier"/>.</summary>
    [Category("5. Quanto allocare")]
    [DisplayName("Allocazione della peggiore ammessa")]
    [Description("Quota di size che riceve l'ultima in classifica fra quelle comunque ammesse. " +
                 "Non è zero: chi passa i cancelli resta operativo, solo ridotto.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public decimal MinimumAllocationMultiplier { get; set; } = 0.25m;

    /// <summary>Vedi <see cref="TitanoRotationRequest.MaximumAllocationMultiplier"/>.</summary>
    [Category("5. Quanto allocare")]
    [DisplayName("Allocazione della migliore")]
    [Description("Quota di size che riceve la prima in classifica, ed è anche il tetto rispetto a " +
                 "cui una strategia risulta 'a pieno regime'. Metterlo sotto il 100% è il modo " +
                 "diretto di essere prudenti senza toccare nessun altro parametro.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Base)]
    public decimal MaximumAllocationMultiplier { get; set; } = 1m;

    /// <summary>Vedi <see cref="TitanoRotationRequest.AllocationStep"/>.</summary>
    [Category("5. Quanto allocare")]
    [DisplayName("Arrotonda l'allocazione a passi di")]
    [Description("Granularità della curva, per avere moltiplicatori leggibili e confrontabili fra " +
                 "periodi. 0 = nessun arrotondamento.")]
    [TypeConverter(typeof(PercentTypeConverter))]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal AllocationStep { get; set; } = 0.05m;

    [Category("6. Sizing a scaglioni (solo senza classifica)")]
    [DisplayName("Score sotto cui spegnere")]
    [Description("Ignorato quando 'Alloca per classifica' è attivo.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal DisableCompositeScore { get; set; } = 0.40m;

    [Category("6. Sizing a scaglioni (solo senza classifica)")]
    [DisplayName("Score sopra cui riaccendere")]
    [Description("Ignorato quando 'Alloca per classifica' è attivo.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal ReenableCompositeScore { get; set; } = 0.60m;

    [Category("6. Sizing a scaglioni (solo senza classifica)")]
    [DisplayName("Scaglioni")]
    [Description("Ignorati quando 'Alloca per classifica' è attivo.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public List<TitanoSizingTier> SizingTiers { get; set; } =
    [
        new() { MinimumScore = 0.80m, AllocationMultiplier = 1m },
        new() { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
        new() { MinimumScore = 0.40m, AllocationMultiplier = 0.25m },
        new() { MinimumScore = 0m, AllocationMultiplier = 0m }
    ];

    [Category("7. Costi simulati")]
    [DisplayName("Commissione per unità")]
    [Description("Sottratta all'equity calcolata offline nel manifest. Non influenza gli ordini reali.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal CommissionPerUnit { get; set; }

    [Category("7. Costi simulati")]
    [DisplayName("Slippage per unità")]
    [Description("Sottratto all'equity calcolata offline nel manifest. Non influenza gli ordini reali.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public decimal SlippagePerUnit { get; set; }

    [Category("8. Validazione walk-forward")]
    [DisplayName("Periodi di calibrazione")]
    [Description("Periodi iniziali usati per calibrare prima di iniziare a valutare fuori campione. " +
                 "Se il run ha meno periodi di questo numero, la validazione non viene fatta affatto " +
                 "e il report lo dichiara.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int CalibrationPeriods { get; set; } = 8;

    [Category("8. Validazione walk-forward")]
    [DisplayName("Periodi di valutazione")]
    [Description("Ampiezza della finestra fuori campione.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
    public int EvaluationPeriods { get; set; } = 4;

    [Category("8. Validazione walk-forward")]
    [DisplayName("Modalità")]
    [Description("Rolling sposta la finestra, Expanding la allunga tenendo fermo l'inizio.")]
    [TitanoLevel(TitanoParameterLevel.Avanzato)]
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

    /// <summary>Cadenza di rotazione del run (da <see cref="TitanoRotationRequest.RotationPeriod"/>).</summary>
    public TitanoRotationPeriod RotationPeriod { get; init; }

    /// <summary><c>EffectiveToUtc</c> più recente fra i periodi del run: oltre questo istante il run
    /// congela l'ultima decisione invece di calcolarne una nuova. Null se il run non ha periodi.</summary>
    public DateTime? LastEffectiveToUtc { get; init; }
}

/// <summary>
/// Stato di un run rispetto a "adesso": <see cref="Fresh"/> finché copre il periodo corrente,
/// <see cref="Stale"/> appena si è entrati in un periodo per cui non ha mai deciso nulla —
/// il run resta comunque applicabile (congela l'ultimo periodo in <see cref="TitanoFilterMode.Realtime"/>),
/// ma segnala che è ora di rifare backtest campione e rotazione.
/// </summary>
public enum TitanoRotationFreshness { Fresh, Stale, NoRun }

/// <summary>Stato di freschezza dell'ultimo run per una cartella di backtest, esposto a lista/dettaglio piano.</summary>
public sealed class TitanoRotationStatus
{
    public required string WorkspaceId { get; init; }
    public required string BacktestFolder { get; init; }
    public TitanoRotationFreshness Freshness { get; init; }
    public string? LatestRunId { get; init; }
    public DateTime? LatestRunGeneratedAtUtc { get; init; }
    public DateTime? EffectiveToUtc { get; init; }
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
