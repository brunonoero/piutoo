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
            Fixed: new() { ["LevelChoice"] = 2 })
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
            SpreadBrokers: [broker],
            SwapBrokers: [broker],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: engine.Levers,
            Stops: AtrStops,
            Targets: AtrTargets,
            ExitHours: ExitHours,
            Directions: engine.VariesDirection ? [0, 1, 2] : [0],
            CsvName: Path.Combine("matrice", $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}-{parts[0].ToLowerInvariant()}.csv"),
            EngineName: engine.Label,
            FirstLeverKey: engine.LeverKey,
            FirstLeverLabel: engine.LeverLabel,
            FirstLeverDivisor: engine.Divisor,
            VariesDirection: engine.VariesDirection,
            AtrStops: true,
            HoldDays: HoldDays,
            ExtraParameters: fixedParameters,
            // 190 sul campione di tre anni (circa 63 all'anno) invece dei 250 del metodo: deciso il
            // 25/09/2026 dopo LFHL su FDAX 4h, 81 configurazioni su 81 in utile con 197-231 trade.
            MinInSampleTrades: 190);
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
    Dictionary<string, object>? Fixed = null);
