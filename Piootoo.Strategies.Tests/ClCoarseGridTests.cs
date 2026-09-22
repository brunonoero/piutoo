using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>CL a 30 minuti</b>, la terza cella dopo GC e NQ. E' la piu' interessante
/// delle tre per una ragione sola: <b>dieci anni di storia</b> (2016-06 → 2026-09) contro i quattro
/// di GC, quindi il campione contiene regimi davvero diversi — il crollo del 2020, lo shock del
/// 2022, il laterale del 2024 — e il fuori campione non puo' essere un solo trend come lo era il
/// rally dell'oro. Costi ICS veri su tutte e tre le voci: spread misurato (0,02), swap dalla scheda,
/// commissione zero. Vedi <see cref="CoarseGridStudy"/>.
///
/// <para><b>Il denaro.</b> Un contratto CL e' 1.000 barili, cioe' 1.000 dollari per punto: gli stop
/// della griglia (150-1.800 dollari per contratto) sono da 0,15 a 1,80 dollari al barile, cioe' dallo
/// 0,2% al 2,6% con il greggio a 70. Non e' la stessa scala di GC (100 $/punto) ne' di NQ (20).</para>
///
/// <para><b>Commissione zero, e non e' una dimenticanza</b>: la scheda XTIUSD di ICS dichiara
/// <c>Commission 0</c>, a differenza dell'oro che ne ha 30 per milione. Su CL il costo di
/// transazione e' tutto nello spread.</para>
/// </summary>
public sealed class ClCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnCl30MeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_CL_PCH_001_30",
            Symbol: "@CL",
            TimeframeMinutes: 30,
            FeedBroker: "ICS",
            // Spread ICS vero, disponibile dal 22/09: prima su questo simbolo si sarebbe dovuto
            // usare quello FTMO, cioe' il listino di un altro broker sul feed di questo.
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2016, 6, 9, 0, 0, 0, DateTimeKind.Utc),
            // Split a due terzi: sei anni e mezzo dentro, tre e tre quarti fuori. Il campione
            // contiene il 2020 e il 2022, cioe' i due regimi che su CL cambiano tutto.
            SplitUtc: new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [150, 300, 600, 1000, 1800],
            Targets: [0, 300, 600, 1200, 2500],
            // Solo due uscite invece delle quattro di GC: dieci anni al minuto costano due volte e
            // mezzo il campione dell'oro, e il rollover ICS e' uno solo, alle 21:00 UTC.
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "cl-30m-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
