using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>GC a 4 ore</b>, la prima (22/09/2026): il controllo che ha detto che il fuori
/// campione della sweep GC era il rally dell'oro e non i pattern — 146 celle su 148 in utile fuori
/// campione con il motore nudo. Risultati in <c>ricerca/gc-4h-griglia-grossa.md</c>. Vedi
/// <see cref="CoarseGridStudy"/>.
/// </summary>
public sealed class GcCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnGcMeasuredInAndOutOfSample()
    {
        // Stop e target in dollari per contratto GC ($100 al punto): 1000 = 10 punti d'oro, 8000 = 80.
        // Spread FTMO perche' ICS non ha ancora misurato XAUUSD; swap ICS dalla scheda.
        var spec = new CoarseGridSpec(
            StrategyId: "PTS_GC_PCH_004_240",
            Symbol: "@GC",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["FTMOPLATFORM"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 10.8m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [1000, 2000, 3000, 5000, 8000],
            Targets: [0, 2000, 4000, 8000, 15000],
            ExitHours: [-1, 19, 20, 21],
            Directions: [0, 1, 2],
            CsvName: "gc-4h-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
