using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>BP a 60 minuti</b>, la quarta cella dopo GC, NQ e CL, e la prima su una
/// <b>valuta</b>. Dodici anni di storia (2014-09 → 2026-09) e costi ICS veri su tutte e tre le voci.
/// Vedi <see cref="CoarseGridStudy"/>.
///
/// <para><b>Perche' una valuta, dopo tre bocciature su indici, metalli ed energia.</b> Il Price
/// Channel nudo non ha retto le soglie su GC 4h, NQ 15m e CL 30m. Se le bocciasse anche qui, il
/// motore e' il problema e non il mercato, e la conclusione varrebbe per tutte e quattro le classi
/// di sottostante provate — che e' un'informazione piu' forte di tre no separati.</para>
///
/// <para><b>Il denaro.</b> Il future CME 6B e' 62.500 sterline quotate in dollari: 62.500 dollari per
/// punto e 6,25 per tick, cioe' per pip. Gli stop della griglia (100-1.200 dollari per contratto)
/// sono da 16 a 192 pip.</para>
///
/// <para><b>La soglia dell'average trade qui e' quella dei tick.</b> Il range medio della barra da 60
/// minuti e' 22 pip nel campione e 18 fuori, quindi il 15% vale 20 e 17 dollari: meno dei 38-44 che
/// valgono 6-7 tick. Sulle altre tre celle era il contrario. Il numero da guardare nel resoconto e'
/// quindi <b>40 dollari di average trade</b>, non una frazione del range.</para>
///
/// <para><b>Commissione 4 per lato</b>, e non e' la lettura diretta della scheda: ICS dichiara 30
/// dollari per milione di controvalore, e un contratto BP a 1,30 vale 81.250 dollari, cioe' 2,44 per
/// lato. Si usa 4 per la stessa ragione per cui si prende il costo peggiore fra due broker: una cella
/// che sopravvive a un costo piu' alto di quello vero rendera' di piu', mai di meno.</para>
/// </summary>
public sealed class BpCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnBp60MeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_BP_PCH_001_60",
            Symbol: "@BP",
            TimeframeMinutes: 60,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 4m,
            StartUtc: new DateTime(2014, 9, 21, 0, 0, 0, DateTimeKind.Utc),
            // Stesso split di GC sul periodo lungo: dentro il 2014-2020 (Brexit, il crollo del 2016,
            // il laterale del 2019), fuori il 2021-2026. Nessuno dei due e' un regime solo.
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [100, 200, 400, 700, 1200],
            Targets: [0, 200, 400, 800, 1600],
            // Il rollover ICS di @BP e' alle 21:00 UTC, con il triplo di mercoledi'. Le 21
            // dell'orologio della ricerca sono le 19:00 o 20:00 UTC, quindi prima.
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "bp-60m-griglia-grossa.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
