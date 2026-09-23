using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>FDAX a 1 ora sul periodo lungo</b>, Price Channel nudo, contenitore
/// <c>PT3B_FDAX_PCH_003_60</c>. Stesso mercato, stesso periodo, stesso split e stessi costi della
/// cella a 4 ore che ha un edge: la domanda e' se quell'edge sopravvive a una barra quattro volte
/// piu' fitta. Gli stop scendono di conseguenza: il range medio di una barra oraria e' circa la meta'
/// di quella da 4 ore, quindi la griglia parte da 500 invece che da 1000.
///
/// <para>L'archivio ICS ha il 60 minuti; il 15 non c'e' e va derivato dal minuto prima di poter
/// scendere ancora (<c>rebuild-from-minutes</c>).</para>
/// </summary>
public sealed class Fdax1hCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFdax1hOverTheLongPeriodMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FDAX_PCH_003_60",
            Symbol: "@FDAX",
            TimeframeMinutes: 60,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 19.23m,
            StartUtc: new DateTime(2014, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [500, 1000, 2500, 5000, 8000],
            Targets: [0, 2500, 4500, 9000, 18000],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "fdax-1h-griglia-grossa-lunga.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
