using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>NQ a 15 minuti</b>: la cella che le 39 del catalogo ai costi veri hanno
/// indicato come l'unica dove qualcosa regge dentro e fuori campione (le tre TF a 15), mentre la 4
/// ore perde 47k fuori. Motore nudo, contenitore <c>PT3B_NQ_PCH_001_15</c>. Vedi
/// <see cref="CoarseGridStudy"/>.
/// </summary>
public sealed class NqCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnNq15MeasuredInAndOutOfSample()
    {
        // Stop e target in dollari per contratto NQ ($20 al punto): 500 = 25 punti, 4000 = 200.
        // Uscita: fine sessione oppure le 21 di Roma, che sul DAX era la leva decisiva e sull'oro
        // inerte — qui si misura. Rollover ICS alle 21:00 UTC = 23:00 Roma d'estate.
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_NQ_PCH_001_15",
            Symbol: "@NQ",
            TimeframeMinutes: 15,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS", "FTMOPLATFORM"],
            SwapBrokers: ["ICS", "FTMO"],
            CommissionPerSide: 19.23m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [500, 1000, 1500, 2500, 4000],
            Targets: [0, 1000, 2000, 4000, 8000],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "nq-15m-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
