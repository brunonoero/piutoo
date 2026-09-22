using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>NQ a 4 ore con il Trend Following mirrored</b>, contenitore
/// <c>PT3B_NQ_TFM_001_240</c>. È il <b>confronto fra motori a parità di tutto il resto</b>: stessa
/// cella, stesso periodo, stesso split, stessi costi della griglia Price Channel
/// (<see cref="Nq4hCoarseGridTests"/>). L'unica cosa che cambia è il motore.
///
/// <para><b>Perché questo confronto e non un altro mercato.</b> Quattro griglie su quattro classi di
/// sottostante hanno detto che il Price Channel nudo non produce un average trade sopra soglia da
/// nessuna parte: GC si ferma al 77% della soglia, NQ al 99% ma con UngerFit 0,35, CL al 65%, BP al
/// 30%. Quattro no coerenti su mercati che non si somigliano spostano la domanda dal mercato al
/// motore. E l'indizio su quale motore provare viene dal catalogo: le uniche strategie coerenti su
/// NQ erano trend following con average trade fra 250 e 400 dollari, da tre a cinque volte il
/// migliore che il Price Channel abbia prodotto su qualunque cella.</para>
///
/// <para><b>Le leve sono diverse, e la griglia lo dichiara.</b> Il trend following entra sull'estremo
/// della sessione precedente, che non è un parametro: al posto del canale la leva strutturale è
/// <c>MaxBars</c>, cioè dopo quante barre la posizione muore — su una 4 ore, 6 barre sono un giorno e
/// 30 una settimana. La direzione non è una leva: il motore emette entrambi i lati e a filtrarli
/// sarebbero i gate di pattern, che qui sono spenti. Sono 250 combinazioni contro le 450 del Price
/// Channel, e il confronto si fa sulle distribuzioni, non sul conteggio.</para>
/// </summary>
public sealed class NqTfCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnNqTrendFollowingMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_NQ_TFM_001_240",
            Symbol: "@NQ",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 0m,
            StartUtc: new DateTime(2014, 7, 17, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            // Prima leva: MaxBars. 0 = nessun limite, 6 = un giorno, 30 = una settimana.
            Channels: [0, 6, 12, 30, 60],
            Stops: [500, 1500, 3000, 6000, 10000],
            Targets: [0, 1500, 3000, 6000, 12000],
            ExitHours: [-1, 21],
            // Il motore non legge Direction: un valore solo, altrimenti girerebbe tre volte la
            // stessa combinazione senza che nulla lo dica.
            Directions: [0],
            CsvName: "nq-4h-tf-griglia-grossa.csv",
            EngineName: "Trend Following mirrored",
            FirstLeverKey: "MaxBars",
            FirstLeverLabel: "maxBars",
            VariesDirection: false);

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
