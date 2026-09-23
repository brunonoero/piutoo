using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>Altri motori sulla cella che ha un edge.</b> FDAX 4 ore e' la sola cella in cui il Price
/// Channel nudo supera le soglie del metodo dentro il campione
/// (<c>ricerca/fdax-4h-griglia-grossa-lunga.md</c>). La domanda seguente non e' «quale altro
/// mercato» ma «l'edge e' del mercato o del motore?», e la si fa a parita' di tutto il resto: stessa
/// cella, stesso periodo, stesso split, stessi costi della griglia Price Channel. Cambia il motore.
///
/// <para>Tre motori, tre famiglie di trigger: il <b>breakout di sessione</b> rompe l'estremo delle
/// ultime N sessioni chiuse (con N = 1 e' il trend following); il <b>reversal di Bollinger</b> e'
/// l'unico contrarian e compra la banda inferiore con un limit; il <b>volatility breakout</b> rompe
/// dall'apertura di sessione di k volte il range del giorno prima. Se anche uno solo vede quello che
/// vede il Price Channel, la cella ha l'edge per conto suo.</para>
///
/// <para>Le leve strutturali sono diverse e ogni griglia dichiara la propria; stop, target e uscita
/// sono quelli della griglia Price Channel, cosi' le colonne si confrontano. Il denaro: un contratto
/// FDAX e' 25 euro per punto e la soglia dell'average trade e' 266 in campione e 365 fuori.</para>
/// </summary>
public sealed class FdaxEnginesCoarseGridTests(ITestOutputHelper output)
{
    private static readonly DateTime StartUtc = new(2014, 7, 18, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly int[] Stops = [1000, 2500, 5000, 8000, 12000];
    private static readonly int[] Targets = [0, 2500, 4500, 9000, 18000];

    /// <summary>Breakout di sessione: la leva e' quante sessioni chiuse fanno il canale. 200 combinazioni.</summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFdaxSessionBreakoutMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FDAX_SBO_001_240",
            Symbol: "@FDAX",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 19.23m,
            StartUtc: StartUtc,
            SplitUtc: SplitUtc,
            EndUtc: EndUtc,
            // Sessioni chiuse nel canale: 1 e' il trend following mirrored, 5 una settimana.
            Channels: [1, 2, 3, 5],
            Stops: Stops,
            Targets: Targets,
            ExitHours: [-1, 21],
            // Il motore non ha Direction: la scelgono i pattern, qui spenti.
            Directions: [0],
            CsvName: "fdax-4h-bo-griglia-grossa.csv",
            EngineName: "Breakout di sessione",
            FirstLeverKey: "Sessions",
            FirstLeverLabel: "sessions",
            VariesDirection: false);

        await CoarseGridStudy.RunAsync(spec, output);
    }

    /// <summary>Reversal di Bollinger mirrored: la leva e' la lunghezza delle bande, deviazioni a 2,0. 150 combinazioni.</summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFdaxReversalBollingerMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FDAX_RBM_001_240",
            Symbol: "@FDAX",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 19.23m,
            StartUtc: StartUtc,
            SplitUtc: SplitUtc,
            EndUtc: EndUtc,
            Channels: [10, 20, 50],
            Stops: Stops,
            Targets: Targets,
            ExitHours: [-1, 21],
            Directions: [0],
            CsvName: "fdax-4h-rbm-griglia-grossa.csv",
            EngineName: "Reversal Bollinger mirrored",
            FirstLeverKey: "BbLength",
            FirstLeverLabel: "bbLength",
            VariesDirection: false);

        await CoarseGridStudy.RunAsync(spec, output);
    }

    /// <summary>
    /// Volatility breakout dall'apertura: la leva e' il moltiplicatore k del range della sessione
    /// precedente, in decimi (5 = 0,5). Ha Direction, quindi 600 combinazioni.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFdaxVolatilityBreakoutMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FDAX_VBO_001_240",
            Symbol: "@FDAX",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            CommissionPerSide: 19.23m,
            StartUtc: StartUtc,
            SplitUtc: SplitUtc,
            EndUtc: EndUtc,
            // k in decimi: 0,5 · 1,0 · 2,0 · 4,0 volte il range d1.
            Channels: [5, 10, 20, 40],
            Stops: Stops,
            Targets: Targets,
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "fdax-4h-vbo-griglia-grossa.csv",
            EngineName: "Volatility breakout (range d1)",
            FirstLeverKey: "AtrMultiplierLong",
            FirstLeverLabel: "volMultTenths",
            FirstLeverDivisor: 10m);

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
