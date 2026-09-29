using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La <b>matrice</b> delle griglie grosse: ogni motore della ricerca v5.0 che ha un contenitore
/// generico (<c>RC_*</c>), su ogni mercato e timeframe, con la stessa griglia di rischio.
///
/// <para><b>Una cella per lancio.</b> Quale cella lo dice la variabile <c>PIOOTOO_CELLA</c> nella forma
/// <c>MOTORE|@SIMBOLO|TIMEFRAME|BROKER</c> (per esempio <c>RHL|@Z|60|FTMO</c>): la coda
/// (<c>tools/coda-ricerca.ps1</c>) la imposta per ogni riga di <c>ricerca/coda.json</c>. Senza la
/// variabile lo studio non fa niente, come ogni studio senza <c>PIOOTOO_STUDI</c>.</para>
///
/// <para><b>Stop e target in ATR</b> delle sessioni chiuse (decimi: 15 = 1,5 ATR), come la v5.0: la
/// stessa griglia vale per un indice da 25.000 punti e per l'argento, senza riscalarla a mano per
/// mercato. <b>Tenuta</b> intraday, uscita alle 21 di Roma, oppure fino a 1 o 3 sessioni.</para>
///
/// <para><b>Split</b> 2022-01 → 2025-01 → 2026-09 per tutte, cosi' le celle si confrontano e si
/// sommano in un paniere. I criteri sono quelli di <see cref="CoarseGridStudy"/>: equilibrate, poi
/// costanza negli anni e outlier, poi soglia di average trade e UngerFit.</para>
///
/// <para>I motori BIAS (tre varianti) e il ciclo settimanale non stanno nella matrice: la loro leva e'
/// un'ora o un giorno d'ingresso, che una griglia a una leva non rappresenta. Il ciclo settimanale ha
/// il suo spazio nella sweep.</para>
/// </summary>
public sealed class CoarseGridMatrixTests(ITestOutputHelper output)
{
    public const string CellVariable = "PIOOTOO_CELLA";

    private static readonly int[] AtrStops = [8, 15, 25];
    private static readonly int[] AtrTargets = [0, 15, 30];
    private static readonly int[] ExitHours = [-1, 21];
    // Tre giorni tolti il 24/09/2026 dopo la prima cella (RHL UK100 4h): 14 ammissibili su 224 e fuori
    // campione in perdita in media, contro un terzo del tempo della matrice. Si riapre su una cella
    // che risponde, non su tutte.
    private static readonly int[] HoldDays = [0, 1];

    /// <summary>
    /// I motori della matrice: contenitore, leva strutturale con i suoi valori, se ha una direzione,
    /// e i parametri fissi che ne fanno la variante.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, MatrixEngine> Engines = new Dictionary<string, MatrixEngine>
    {
        ["PCH"] = new("RC_PCH", "Price Channel", "ChannelBars", "channelBars", [1, 10, 20, 50]),
        ["TFM"] = new("RC_TFM", "Trend following mirrored", "MaxBars", "maxBars", [0, 6, 12, 30], VariesDirection: false),
        ["TFU"] = new("RC_TFU", "Trend following unmirrored", "MaxBars", "maxBars", [0, 6, 12, 30], VariesDirection: false),
        ["BO"] = new("RC_SBO", "Breakout di N sessioni", "Sessions", "sessions", [1, 2, 3, 5], VariesDirection: false,
            Fixed: new() { ["LevelSource"] = 0, ["IncludeCurrentSession"] = 0 }),
        ["BOS"] = new("RC_SBO", "Breakout della sessione in corso", "BreakoutOffsetTicks", "offsetTicks", [0, 5, 20], VariesDirection: false,
            Fixed: new() { ["LevelSource"] = 1 }),
        ["RBM"] = new("RC_RBM", "Reversal Bollinger mirrored", "BbLength", "bbLength", [10, 20, 50], VariesDirection: false),
        ["RBU"] = new("RC_RBU", "Reversal Bollinger unmirrored", "BbLength", "bbLength", [10, 20, 50], VariesDirection: false),
        ["RHL"] = new("RC_RHL", "Reversal sui livelli di ieri", "LevelOffsetTicks", "offsetTicks", [0, 5, 20]),
        ["VBO"] = new("RC_VBO", "Volatility breakout (range d1)", "AtrMultiplierLong", "volMultTenths", [5, 10, 20, 40], Divisor: 10m),
        ["MAC"] = new("RC_MAC", "Incrocio di medie (lenta 50)", "FastPeriod", "fastPeriod", [5, 10, 20],
            Fixed: new() { ["SlowPeriod"] = 50 }),
        ["LF"] = new("RC_LFD", "Level fader sul pivot di ieri", "LevelShift", "shiftTicks", [0, 5, 20], VariesDirection: false,
            Fixed: new() { ["LevelChoice"] = 1 }),
        ["LFHL"] = new("RC_LFD", "Level fader sugli estremi di ieri", "LevelShift", "shiftTicks", [0, 5, 20], VariesDirection: false,
            Fixed: new() { ["LevelChoice"] = 2 }),

        // Serie PT6EXO (25/09/2026), famiglie cercate per essere scorrelate dal resto: vedi
        // docs/domini/catalogo-idee-pt6exo.md. Primo lotto su FDAX e NQ, davanti alla matrice vecchia.
        ["FBO"] = new("RC_FBO", "Falso breakout (rientro nella stessa barra)", "ChannelBars", "channelBars", [5, 10, 20, 50],
            Fixed: new() { ["ReentryBars"] = 1 }),
        // Uscita classica accesa: il long chiude alla prima chiusura con IBS >= 0,5, lo short specchio.
        ["IBS"] = new("RC_IBS", "Forza interna della barra (uscita a IBS 0,5)", "SymmetricThreshold", "thresholdPct", [10, 20, 30],
            Divisor: 100m, Fixed: new() { ["ExitIbs"] = 0.5m }),
        ["RUN"] = new("RC_RUN", "Serie di chiusure (uscita alla prima chiusura contraria)", "RunBars", "runBars", [2, 3, 4, 5],
            Fixed: new() { ["Mode"] = 0, ["ExitOnFirstOppositeClose"] = 1 }),
        ["NRX"] = new("RC_NRX", "Compressione NR-N, stop sugli estremi", "LookbackBars", "lookbackBars", [4, 7, 10],
            Fixed: new() { ["CompressionKind"] = 0, ["ValidBars"] = 1 }),
        // Deriva pura: verso fisso long o short, niente momentum. Le ore sono di Roma e valgono sulle celle
        // a 60 minuti: su quelle a 4 ore la barra non apre a ogni ora e l'ingresso non scatterebbe mai.
        ["HOD"] = new("RC_HOD", "Deriva oraria (4 ore, verso fisso)", "EntryHour", "entryHour", [8, 9, 10, 14, 15, 16],
            Fixed: new() { ["MomentumMode"] = 0, ["HoldHours"] = 4 }, Directions: [1, 2]),
        // Regime di trend temporaneo (29/09/2026): la leva e' quanto e' "temporaneo", cioe' la finestra; il
        // punteggio ER × √N tiene la soglia sulla stessa scala per ogni finestra. Uscita di regime accesa.
        ["ERT"] = new("RC_ERT", "Regime di trend (efficiency ratio, uscita a trend spento)", "ErBars", "erBars", [10, 20, 40, 80],
            Fixed: new() { ["EntryScore"] = 2m, ["ExitScore"] = 0.5m, ["FreshOnly"] = 1 }),

        // Il resto del catalogo PT6EXO (29/09/2026), sulle stesse quattro celle. Fuori: DXH (la leva e' una
        // tabella di regole, e il catalogo la vuole solo dove HOD ha risposto), MTF (solo backtest) e XMK
        // (studio proprio). CAL e LUN fanno pochi trade per costruzione: sul campione di tre anni non
        // arrivano all'ammissibilita' e vanno lette sul CSV grezzo.
        // Percentili 20/80. Il regime piu' lungo e' di 500 barre: a 4 ore sono quasi tre mesi, e con i 30
        // giorni di riscaldamento della griglia l'inizio del campione resta muto.
        ["REG"] = new("RC_REG", "Regime di volatilita' (breakout calmo, fade agitato)", "RegimeBars", "regimeBars", [100, 250, 500]),
        // Turn-of-month, entrata l'N-esimo ultimo giorno e quattro sessioni di tenuta. Multiday: niente ora di
        // uscita (il motore la rifiuta) e niente tenuta della matrice, che la taglierebbe.
        ["CAL"] = new("RC_CAL", "Turn-of-month (4 sessioni)", "DaysBeforeMonthEnd", "daysBeforeMonthEnd", [1, 2, 3],
            Fixed: new() { ["Mode"] = 0, ["HoldSessions"] = 4 }, Directions: [1, 2], ExitHours: [-1], HoldDays: [0]),
        // Gap all'ancoraggio: fade con target al riempimento, e go a favore.
        ["GAP"] = new("RC_GAP", "Gap di sessione, fade al riempimento", "GapAtr", "gapAtrTenths", [1, 3, 5, 10], Divisor: 10m,
            Fixed: new() { ["Mode"] = 0, ["TargetAtGapFill"] = 1 }),
        ["GAPGO"] = new("RC_GAP", "Gap di sessione, a favore", "GapAtr", "gapAtrTenths", [1, 3, 5, 10], Divisor: 10m,
            Fixed: new() { ["Mode"] = 1, ["TargetAtGapFill"] = 0 }),
        // Pin bar sugli estremi di ieri, stop comune.
        ["CDL"] = new("RC_CDL", "Pin bar sugli estremi di ieri", "WickRatio", "wickRatioTenths", [15, 20, 30], Divisor: 10m,
            Fixed: new() { ["LevelKind"] = 0, ["CandleKind"] = 0, ["ToleranceTicks"] = 0, ["StopAtExtreme"] = 0 }),
        // Magnete dei numeri tondi, target sul livello. Ha senso solo sul prezzo vero: il feed FTMO lo e'.
        ["RNM"] = new("RC_RNM", "Numeri tondi, magnete con target sul livello", "RoundStep", "roundStep", [50, 100, 250, 500],
            Fixed: new() { ["Mode"] = 0, ["DistanceTicks"] = 20, ["TargetAtLevel"] = 1 }),
        // Contro il movimento partito dall'ultimo pivot di 3 barre, dopo N barre.
        ["FIB"] = new("RC_FIB", "Conte di Fibonacci dal pivot (contro)", "CountBars", "countBars", [8, 13, 21, 34],
            Fixed: new() { ["PivotBars"] = 3, ["Mode"] = 0 }),
        // La leva e' il verso del ciclo: 0 long dalla nuova alla piena, 1 il contrario.
        ["LUN"] = new("RC_LUN", "Fasi lunari", "Mode", "mode", [0, 1]),
        ["MOD"] = new("RC_MOD", "Modulo del tempo (nel verso della barra)", "ModuloBars", "moduloBars", [5, 7, 11, 13],
            Fixed: new() { ["Remainder"] = 0, ["Mode"] = 0 }),
        // Il volume e il suo controllo: la cella sul tick volume vale solo se batte la stessa sull'ampiezza.
        ["VLM"] = new("RC_VLM", "Anomalia di tick volume (segue la barra)", "SpikeRatio", "spikeRatioTenths", [20, 30, 40], Divisor: 10m,
            Fixed: new() { ["LookbackBars"] = 20, ["Mode"] = 0, ["ActivitySource"] = 0 }),
        ["VLMA"] = new("RC_VLM", "Anomalia di ampiezza (controllo di VLM)", "SpikeRatio", "spikeRatioTenths", [20, 30, 40], Divisor: 10m,
            Fixed: new() { ["LookbackBars"] = 20, ["Mode"] = 0, ["ActivitySource"] = 1 })
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnTheCellFromTheEnvironment()
    {
        var cell = Environment.GetEnvironmentVariable(CellVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{CellVariable} non impostata: nessuna cella da lanciare.");
            return;
        }

        var spec = Spec(cell);
        output.WriteLine($"cella {cell}: {spec.EngineName}, contenitore {spec.StrategyId}");
        await CoarseGridStudy.RunAsync(spec, output);
    }

    /// <summary>La griglia di una cella <c>MOTORE|@SIMBOLO|TIMEFRAME|BROKER</c>.</summary>
    public static CoarseGridSpec Spec(string cell)
    {
        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !Engines.TryGetValue(parts[0].ToUpperInvariant(), out var engine) || !int.TryParse(parts[2], out var timeframe))
            throw new ArgumentException(
                $"Cella '{cell}' non valida: attesa MOTORE|@SIMBOLO|TIMEFRAME|BROKER, motori {string.Join(", ", Engines.Keys)}.");

        var symbol = "@" + parts[1].TrimStart('@').ToUpperInvariant();
        var broker = parts[3].ToUpperInvariant();

        // VENDOR = il future del vendor su 14 anni (29/09/2026): FBO NQ 1h e NRX NQ 4h sembravano buone sui
        // tre anni e mezzo FTMO, anche contro il RAN, e sul 2008-2021 hanno perso tutti gli anni. Il
        // campione lungo attraversa regimi che il FTMO non ha. Costi FTMO, gli stessi del conto: lo spread
        // di oggi su un indice che nel 2008 valeva un decimo pesa di piu', e va letto come prudenza.
        var vendor = broker == CoarseGridStudy.VendorFeed;
        var costBroker = vendor ? "FTMO" : broker;
        var fixedParameters = new Dictionary<string, object>(engine.Fixed ?? [])
        {
            ["Symbol"] = symbol,
            ["TimeframeMinutes"] = timeframe
        };

        return new CoarseGridSpec(
            StrategyId: engine.Container,
            Symbol: symbol,
            TimeframeMinutes: timeframe,
            FeedBroker: broker,
            SpreadBrokers: [costBroker],
            SwapBrokers: [costBroker],
            CommissionPerSide: 0m,
            // Il minuto del vendor parte dal 2008 per FDAX (2006 per NQ) e finisce il 30/05/2025.
            StartUtc: vendor ? new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc) : new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: vendor ? new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc) : new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: vendor ? new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc) : new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: engine.Levers,
            Stops: AtrStops,
            Targets: AtrTargets,
            ExitHours: engine.ExitHours ?? ExitHours,
            Directions: engine.Directions ?? (engine.VariesDirection ? [0, 1, 2] : [0]),
            CsvName: Path.Combine("matrice", $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}-{parts[0].ToLowerInvariant()}{(vendor ? "-vendor" : "")}.csv"),
            EngineName: engine.Label,
            FirstLeverKey: engine.LeverKey,
            FirstLeverLabel: engine.LeverLabel,
            FirstLeverDivisor: engine.Divisor,
            VariesDirection: engine.VariesDirection,
            AtrStops: true,
            HoldDays: engine.HoldDays ?? HoldDays,
            ExtraParameters: fixedParameters,
            // 190 sul campione di tre anni (circa 63 all'anno) invece dei 250 del metodo: deciso il
            // 25/09/2026 dopo LFHL su FDAX 4h, 81 configurazioni su 81 in utile con 197-231 trade. Sui 14
            // anni del vendor torna la soglia del metodo.
            MinInSampleTrades: vendor ? 250 : 190);
    }
}

/// <summary>Un motore della matrice. Vedi <see cref="CoarseGridMatrixTests.Engines"/>.</summary>
public sealed record MatrixEngine(
    string Container,
    string Label,
    string LeverKey,
    string LeverLabel,
    int[] Levers,
    bool VariesDirection = true,
    decimal Divisor = 1m,
    Dictionary<string, object>? Fixed = null,
    // Le direzioni da permutare quando non sono 0/1/2: HOD senza momentum non ha un "entrambi".
    int[]? Directions = null,
    // Ore di uscita e tenute proprie, quando quelle della matrice non hanno senso: CAL e' multiday per natura
    // e rifiuta un'ora di uscita di sessione, e la sua tenuta la dichiara HoldSessions.
    int[]? ExitHours = null,
    int[]? HoldDays = null);
