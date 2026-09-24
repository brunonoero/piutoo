using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>UK100 a 4 ore</b> (FTMO, 2022-01 → 2026-09), Price Channel nudo, contenitore
/// <c>PT3B_Z_PCH_001_240</c>. Le stesse leve della cella FDAX 4 ore, l'unica con un edge: la
/// domanda e' se l'edge e' del motore o del solo DAX.
///
/// <para><b>Feed e costi sono FTMO</b>, il broker su cui si opererebbe: il feed parte a novembre 2020,
/// quindi vale lo split corto (2022 → 2025 → 2026). Il risultato non si somma alla cella FDAX, che
/// e' su ICS e sullo split lungo. Commissione zero: FTMO non la applica sugli indici cash.</para>
///
/// <para><b>Stop e target scalati.</b> Range medio della barra da 4 ore nel campione: 34,8 punti, 348 sterline.</para>
/// </summary>
public sealed class Uk100CoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnUk100MeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_Z_PCH_001_240",
            Symbol: "@Z",
            TimeframeMinutes: 240,
            FeedBroker: "FTMO",
            SpreadBrokers: ["FTMO"],
            SwapBrokers: ["FTMO"],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // La griglia FDAX 4 ore riscalata sul range in denaro: circa un quinto.
            Stops: [200, 500, 1000, 1550, 2350],
            Targets: [0, 500, 900, 1750, 3500],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "uk100-4h-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
