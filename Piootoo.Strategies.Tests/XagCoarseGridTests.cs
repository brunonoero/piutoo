using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>XAGUSD a 4 ore</b> (FTMO, 2022-01 → 2026-09), Price Channel nudo, contenitore
/// <c>PT3B_SI_PCH_001_240</c>. Le stesse leve della cella FDAX 4 ore, l'unica con un edge: la
/// domanda e' se l'edge e' del motore o del solo DAX.
///
/// <para><b>Feed e costi sono FTMO</b>, il broker su cui si opererebbe: il feed parte a novembre 2020,
/// quindi vale lo split corto (2022 → 2025 → 2026). Il risultato non si somma alla cella FDAX, che
/// e' su ICS e sullo split lungo. Commissione dalla scheda XAGUSD: 0,0007% del volume per lato, su un lotto da 5.000 once a circa 65 dollari fa 2,3 dollari.</para>
///
/// <para><b>Stop e target scalati.</b> Range medio della barra da 4 ore nel campione: 0,252 dollari, 1.261 dollari.</para>
/// </summary>
public sealed class XagCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnXagMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_SI_PCH_001_240",
            Symbol: "@SI",
            TimeframeMinutes: 240,
            FeedBroker: "FTMO",
            SpreadBrokers: ["FTMO"],
            SwapBrokers: ["FTMO"],
            CommissionPerSide: 2.3m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // La griglia FDAX 4 ore riscalata sul range in denaro: circa sette decimi.
            Stops: [700, 1800, 3550, 5650, 8500],
            Targets: [0, 1750, 3200, 6400, 12750],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "xag-4h-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
