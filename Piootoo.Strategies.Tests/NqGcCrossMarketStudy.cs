using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>XMK su NQ guardando GC</b>: la prima cella tra mercati della serie PT6EXO (vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §XMK). Si opera sul Nasdaq, si decide guardando l'oro.
///
/// <para><b>Perche' questa coppia.</b> NQ e GC sono i due mercati del paniere con il comportamento piu'
/// diverso nei giorni di stress — l'oro sale quando l'azionario scende, a volte, e a volte no — e su NQ
/// i motori vecchi sono gia' misurati: se una cella XMK risponde, la sua correlazione con il resto si
/// calcola subito. Non c'e' un'ipotesi da dimostrare, c'e' una griglia da far rispondere.</para>
///
/// <para><b>Le celle.</b> I tre modi del motore — 0 anticipo (GC rompe il proprio canale, NQ nello
/// stesso verso), 1 divergenza (GC si muove di almeno l'1%, NQ al contrario: si segue GC), 2 rapporto
/// (z-score del log di NQ/GC oltre 2) — a 60 e a 240 minuti. La leva strutturale e' la finestra,
/// <c>LookbackBars</c> 10/20/50; il resto e' la griglia di rischio della matrice: stop e target in ATR,
/// uscita alle 21 o tenuta fino a una notte, direzione 0/1/2. Feed, split e costi sono quelli della
/// matrice FTMO (2022-01 → 2025-01 → 2026-09), cosi' le celle si confrontano con quelle NQ gia' fatte.</para>
///
/// <para><b>Durata.</b> Sei griglie da 324 combinazioni sull'orologio al minuto: ore. Si lancia come ogni
/// studio, con <c>PIOOTOO_STUDI=1</c>; un caso solo si sceglie con il filtro sul nome del metodo e
/// sull'argomento (<c>DisplayName~mode: 2</c>).</para>
/// </summary>
public sealed class NqGcCrossMarketStudy(ITestOutputHelper output)
{
    private static readonly string[] ModeNames = ["anticipo", "divergenza", "rapporto"];

    [Theory]
    [Trait("Category", ResearchStudy.Category)]
    [InlineData(0, 60)]
    [InlineData(1, 60)]
    [InlineData(2, 60)]
    [InlineData(0, 240)]
    [InlineData(1, 240)]
    [InlineData(2, 240)]
    public async Task CrossMarketGridOnNqWatchingGc(int mode, int timeframe)
    {
        var spec = Spec(mode, timeframe);
        output.WriteLine($"XMK {ModeNames[mode]} · NQ operato, GC riferimento · {timeframe}m");
        await CoarseGridStudy.RunAsync(spec, output);
    }

    /// <summary>La griglia di una cella: la stessa della matrice FTMO, con GC caricato come riferimento.</summary>
    public static CoarseGridSpec Spec(int mode, int timeframe) => new(
        StrategyId: "RC_XMK",
        Symbol: "@NQ",
        TimeframeMinutes: timeframe,
        FeedBroker: "FTMO",
        SpreadBrokers: ["FTMO"],
        SwapBrokers: ["FTMO"],
        CommissionPerSide: 0m,
        StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        Channels: [10, 20, 50],
        Stops: [8, 15, 25],
        Targets: [0, 15, 30],
        ExitHours: [-1, 21],
        Directions: [0, 1, 2],
        CsvName: Path.Combine("matrice", $"nq-{timeframe}-xmk-gc-{ModeNames[mode]}.csv"),
        EngineName: $"Tra mercati, GC, {ModeNames[mode]}",
        FirstLeverKey: "LookbackBars",
        FirstLeverLabel: "lookbackBars",
        VariesDirection: true,
        AtrStops: true,
        HoldDays: [0, 1],
        ExtraParameters: new Dictionary<string, object>
        {
            ["Symbol"] = "@NQ",
            ["TimeframeMinutes"] = timeframe,
            ["ReferenceSymbol"] = "@GC",
            ["Mode"] = mode,
            ["MoveThresholdPct"] = 1m,
            ["ZEntry"] = 2m
        },
        // Come la matrice: 190 trade in campione sui tre anni.
        MinInSampleTrades: 190,
        ReferenceSymbols: ["@GC"]);
}
