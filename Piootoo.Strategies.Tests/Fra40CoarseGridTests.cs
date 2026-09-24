using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>FRA40 a 4 ore</b> (FTMO, 2022-01 → 2026-09), Price Channel nudo, contenitore
/// <c>PT3B_FCE_PCH_001_240</c>. Le stesse leve della cella FDAX 4 ore, l'unica con un edge: la
/// domanda e' se l'edge e' del motore o del solo DAX.
///
/// <para><b>Feed e costi sono FTMO</b>, il broker su cui si opererebbe: il feed parte a novembre 2020,
/// quindi vale lo split corto (2022 → 2025 → 2026). Il risultato non si somma alla cella FDAX, che
/// e' su ICS e sullo split lungo. Commissione zero: FTMO non la applica sugli indici cash.</para>
///
/// <para><b>Stop e target scalati.</b> Range medio della barra da 4 ore nel campione: 48,5 punti, 485 euro.</para>
/// </summary>
public sealed class Fra40CoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFra40MeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FCE_PCH_001_240",
            Symbol: "@FCE",
            TimeframeMinutes: 240,
            FeedBroker: "FTMO",
            SpreadBrokers: ["FTMO"],
            SwapBrokers: ["FTMO"],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // La griglia FDAX 4 ore riscalata sul range in denaro: circa un terzo abbondante.
            Stops: [300, 700, 1350, 2200, 3300],
            Targets: [0, 700, 1250, 2500, 5000],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "fra40-4h-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
