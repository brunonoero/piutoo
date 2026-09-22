using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>GC a 4 ore</b>, sul <b>periodo lungo</b> (2014-09 → 2026-09).
///
/// <para><b>Perche' rifatta.</b> Il primo giro (22/09/2026, campione 2022-2025 e validazione a
/// 2026) trovo' 146 celle su 148 in utile fuori campione con il motore nudo, e la lettura fu che
/// quel fuori campione era il <i>rally dell'oro</i> e non i pattern: quando una cella qualunque
/// guadagna, quello che si misura e' il mercato. Il test che quella lettura prescriveva era
/// esattamente questo — rifare la griglia su un campione che contenga anche rialzi, ribassi e
/// laterali — e allora non si poteva fare perche' l'archivio ICS di XAUUSD partiva dal settembre
/// 2022. La raccolta del 21-22/09 lo ha portato al <b>2014-09</b>: adesso si puo'.
/// Risultati del primo giro in <c>ricerca/gc-4h-griglia-grossa.md</c>, che resta valido per il
/// periodo che misurava.</para>
///
/// <para><b>Due cose cambiano oltre al periodo.</b> Lo spread e' ora quello <b>ICS</b> misurato
/// (0,09 di mediana su 10,3 milioni di tick, dal 22/09) e non piu' quello FTMO usato come ripiego:
/// il listino di un altro broker sul feed di questo era la cosa meno difendibile del primo giro.
/// E le uscite scendono da quattro a due, perche' il primo giro ha <i>misurato</i> che sull'oro
/// l'ora di uscita non conta (103-113k in ogni caso, coerente con i nove orari provati sulla PTS):
/// dodici anni al minuto costano tre volte il campione di allora, e le ore risparmiate si spendono
/// meglio sul periodo.</para>
///
/// <para>Vedi <see cref="CoarseGridStudy"/>.</para>
/// </summary>
public sealed class GcCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnGcOverTheLongPeriodMeasuredInAndOutOfSample()
    {
        // Stop e target in dollari per contratto GC ($100 al punto): 1000 = 10 punti d'oro, 8000 = 80.
        var spec = new CoarseGridSpec(
            StrategyId: "PTS_GC_PCH_004_240",
            Symbol: "@GC",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 10.8m,
            StartUtc: new DateTime(2014, 9, 22, 0, 0, 0, DateTimeKind.Utc),
            // Split a poco piu' di meta': dentro il 2015-2020 (oro laterale, poi il rialzo del
            // 2019-20), fuori il 2021-22 (la discesa) e il 2023-26 (il rally). Nessuno dei due
            // tratti e' un regime solo, che e' precisamente cio' che mancava al primo giro.
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            Stops: [1000, 2000, 3000, 5000, 8000],
            Targets: [0, 2000, 4000, 8000, 15000],
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "gc-4h-griglia-grossa-lunga.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
