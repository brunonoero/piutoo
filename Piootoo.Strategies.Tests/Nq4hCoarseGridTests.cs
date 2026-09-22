using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>NQ a 4 ore sul periodo lungo</b> (2014-07 → 2026-09), contenitore
/// <c>PT3B_NQ_PCH_002_240</c>.
///
/// <para><b>Perche', dopo due bocciature sulla stessa cella.</b> NQ a 4 ore e' stato cercato due
/// volte — la sweep del 21/09 e quella con il criterio nuovo del 22/09 — e in entrambi i casi nessuna
/// finalista e' sopravvissuta. Ma tutte e due giravano su <b>2022-2026</b>: quattro anni, di cui il
/// fuori campione e' il rialzo del 2025-26. L'archivio ICS arriva al 2014-07, e su dodici anni la
/// domanda cambia — non "questa configurazione regge?" ma "la cella ha un edge?". E' la stessa
/// differenza che su GC ha portato da zero celle equilibrate a trentaquattro.</para>
///
/// <para><b>Il metro non e' lo stesso nei due periodi.</b> Il range medio della barra da 4 ore passa
/// da 42 punti (834 dollari per contratto) a 124 (2.486): la volatilita' di NQ e' <b>triplicata</b>,
/// e con lei la soglia del 15% sull'average trade, da 125 a 373 dollari. Un average trade che cresce
/// fuori campione non e' di per se' un miglioramento — su GC era esattamente il regime.</para>
/// </summary>
public sealed class Nq4hCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnNq4hOverTheLongPeriodMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_NQ_PCH_002_240",
            Symbol: "@NQ",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            // La scheda USTEC di ICS dichiara commissione zero: il costo e' tutto nello spread.
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2014, 7, 17, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // Da 25 a 500 punti NQ: copre il range della barra del campione e quello del fuori.
            Stops: [500, 1500, 3000, 6000, 10000],
            Targets: [0, 1500, 3000, 6000, 12000],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "nq-4h-griglia-grossa-lunga.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
