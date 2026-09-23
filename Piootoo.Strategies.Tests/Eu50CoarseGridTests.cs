using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>EU50 a 4 ore</b> (FTMO, 2022-01 → 2026-09), Price Channel nudo, contenitore
/// <c>PT3B_FESX_PCH_001_240</c>. Le stesse leve della cella FDAX 4 ore, l'unica con un edge: la
/// domanda e' se l'edge e' del motore sugli indici europei o del solo DAX.
///
/// <para><b>Feed e costi sono FTMO</b>, il broker su cui si opererebbe: il feed parte a novembre 2020,
/// quindi vale lo split corto (2022 → 2025 → 2026). Il risultato non si somma alla cella FDAX, che
/// e' su ICS e sullo split lungo. Commissione zero: FTMO non la applica sugli indici cash.</para>
///
/// <para><b>Stop e target scalati.</b> Il range medio della barra da 4 ore e' 24,6 punti nel
/// campione, 246 euro a contratto contro i 1.775 del FDAX: circa un settimo, e la griglia FDAX
/// divisa per sette.</para>
/// </summary>
public sealed class Eu50CoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnEu50MeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FESX_PCH_001_240",
            Symbol: "@FESX",
            TimeframeMinutes: 240,
            FeedBroker: "FTMO",
            SpreadBrokers: ["FTMO"],
            SwapBrokers: ["FTMO"],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // La griglia FDAX 4 ore [1000..12000] divisa per sette.
            Stops: [150, 350, 700, 1100, 1700],
            Targets: [0, 350, 650, 1250, 2500],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "eu50-4h-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
