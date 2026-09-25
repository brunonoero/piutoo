using System.Reflection;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore XMK (tra mercati) della serie PT6EXO e il contratto che lo serve: NQ a 60 minuti operato,
/// ES come riferimento. Le serie sono sintetiche e allineate sull'ora; ogni caso muove l'una o l'altra.
/// </summary>
public sealed class CrossMarketEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Anticipo: il riferimento chiude sopra il proprio canale → long sul simbolo operato, a mercato sulla barra dopo.</summary>
    [Fact]
    public void AReferenceBreakoutUpBuysTheTradedSymbol()
    {
        var primary = Flat(200);
        var reference = Flat(200);
        reference[^1] = Bar(reference[^1].DateTime, 106m, 99m, 105m);

        var signal = Evaluate(new TestXmk(), primary, reference, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(primary[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio: rottura sotto il canale → short.</summary>
    [Fact]
    public void AReferenceBreakoutDownSellsTheTradedSymbol()
    {
        var primary = Flat(200);
        var reference = Flat(200);
        reference[^1] = Bar(reference[^1].DateTime, 101m, 94m, 95m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestXmk(), primary, reference, null).Type);
    }

    /// <summary>Un riferimento dentro il canale non dice niente.</summary>
    [Fact]
    public void AReferenceInsideItsChannelIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestXmk(), Flat(200), Flat(200), null).Type);
    }

    /// <summary>
    /// Se il riferimento non ha stampato la barra di segnale, la strategia non valuta: la barra mancante
    /// non si sostituisce con la precedente.
    /// </summary>
    [Fact]
    public void AMissingReferenceBarForTheSignalBarIsNotEvaluated()
    {
        var primary = Flat(200);
        var reference = Flat(200);
        reference[^2] = Bar(reference[^2].DateTime, 106m, 99m, 105m);
        reference = reference[..^1];

        var signal = Evaluate(new TestXmk(), primary, reference, null);

        Assert.Equal(SignalType.Hold, signal.Type);
        Assert.Equal("Barra di riferimento assente", signal.Reason);
    }

    /// <summary>
    /// Le barre si accoppiano per istante: un buco nel riferimento in mezzo alla finestra non sposta le
    /// coppie, e la rottura sull'ultima barra si vede lo stesso.
    /// </summary>
    [Fact]
    public void AGapInTheMiddleOfTheReferenceKeepsThePairsAligned()
    {
        var primary = Flat(200);
        var reference = Flat(200).Where((_, index) => index is < 190 or > 193).ToArray();
        reference[^1] = Bar(reference[^1].DateTime, 106m, 99m, 105m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestXmk(), primary, reference, null).Type);
    }

    /// <summary>Divergenza: il riferimento sale del 2%, il simbolo operato scende → long, a inseguire il riferimento.</summary>
    [Fact]
    public void ADivergenceFollowsTheReference()
    {
        var primary = Flat(200);
        primary[^1] = Bar(primary[^1].DateTime, 101m, 98m, 99m);
        var reference = Flat(200);
        reference[^1] = Bar(reference[^1].DateTime, 103m, 99m, 102m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestXmk { ModeValue = 1 }, primary, reference, null).Type);
    }

    /// <summary>Rapporto: il simbolo operato sale del 10% da solo, lo z-score del rapporto supera la soglia → short.</summary>
    [Fact]
    public void AStretchedRatioSellsTheTradedSymbol()
    {
        var primary = Flat(200);
        primary[^1] = Bar(primary[^1].DateTime, 111m, 99m, 110m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestXmk { ModeValue = 2 }, primary, Flat(200), null).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce; in posizione non nasce nulla.</summary>
    [Fact]
    public void DirectionAndPositionBlockTheEntry()
    {
        var reference = Flat(200);
        reference[^1] = Bar(reference[^1].DateTime, 106m, 99m, 105m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestXmk { Side = 2 }, Flat(200), reference, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestXmk(), Flat(200), reference, SignalType.Sell).Type);
    }

    /// <summary>
    /// Una serie dichiarata e non arrivata e' un errore, non un "niente segnale": e' cio' che succede
    /// oggi nel backtest completo e nella sessione live, che gli altri simboli non li portano.
    /// </summary>
    [Fact]
    public void AMissingReferenceSeriesIsAnError()
    {
        var strategy = new TestXmk();
        var primary = Flat(200);

        var error = Assert.Throws<InvalidOperationException>(() => strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = primary,
            BarTimeUtc = primary[^1].DateTime,
            Execution = new StrategyExecutionSnapshot { StrategyCode = strategy.Name, Symbol = "NQ", BarTimeUtc = primary[^1].DateTime }
        }));

        Assert.Contains("@ES", error.Message);
        Assert.ThrowsAny<Exception>(() => ((ITradingStrategy)strategy).GenerateSignal(primary, primary[^1].DateTime));
    }

    /// <summary>Una configurazione incoerente ferma la valutazione.</summary>
    [Fact]
    public void AnInvalidConfigurationIsRejected()
    {
        var strategy = new TestXmk { ModeValue = 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            strategy.GenerateSignal(Flat(200), new Dictionary<string, OhlcvData[]> { ["@ES"] = Flat(200) }, Start));
    }

    /// <summary>Il contenitore legge le leve, e il simbolo di riferimento esce normalizzato.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_XMK();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["ReferenceSymbol"] = "gc", ["Mode"] = 2, ["LookbackBars"] = 40,
            ["MoveThresholdPct"] = 1.5m, ["ZEntry"] = 2.5m, ["Direction"] = 1,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 1, ["ExitHour"] = 21,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(new[] { "@GC" }, strategy.ReferenceSymbols);
        Assert.Equal(2, Read<int>(strategy, "Mode"));
        Assert.Equal(40, Read<int>(strategy, "LookbackBars"));
        Assert.Equal(1.5m, Read<decimal>(strategy, "MoveThresholdPct"));
        Assert.Equal(2.5m, Read<decimal>(strategy, "ZEntry"));
        Assert.True(strategy.RequiredCandles >= 82);
    }

    /// <summary>
    /// Lo studio NQ/GC passa al contenitore le chiavi fisse della griglia grossa, le sue e la leva: il
    /// contenitore le deve leggere tutte, per ogni modo e timeframe. Una chiave che non legge fermerebbe
    /// lo studio dopo ore di caricamento, invece che qui.
    /// </summary>
    [Theory]
    [InlineData(0, 60)]
    [InlineData(1, 60)]
    [InlineData(2, 240)]
    public void TheNqGcStudyParametersAreAllReadByTheContainer(int mode, int timeframe)
    {
        var spec = NqGcCrossMarketStudy.Spec(mode, timeframe);
        var parameters = new Dictionary<string, object>
        {
            ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
            ["StartHour"] = -1, ["EndHour"] = -1, ["DvolMin"] = 0, ["SkipDay"] = -1,
            ["IntradayOnly"] = 1, ["OffsetTicks"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
            ["MaxBars"] = 0, ["ExitHour"] = 21, ["Direction"] = 1,
            ["StopLoss"] = 0, ["TakeProfit"] = 0, ["StopAtr"] = 1.5m, ["TargetAtr"] = 3m,
            [spec.FirstLeverKey] = spec.Channels[1]
        };
        foreach (var (key, value) in spec.ExtraParameters!)
            parameters[key] = value;

        var strategy = new RC_XMK();
        strategy.Initialize(parameters);

        Assert.Equal(new[] { "@GC" }, strategy.ReferenceSymbols);
        Assert.Equal(timeframe, strategy.TimeframeMinutes);
        Assert.Equal(mode, Read<int>(strategy, "Mode"));
        Assert.Equal(spec.Channels[1], Read<int>(strategy, "LookbackBars"));
        Assert.Equal(new[] { "@GC" }, spec.ReferenceSymbols);
    }

    /// <summary>
    /// La sweep vera: con la serie di ES il contenitore viene valutato e produce trade; senza, il run si
    /// ferma e dice quale simbolo manca.
    /// </summary>
    [Fact]
    public async Task TheSweepCarriesTheReferenceSeriesAndStopsWithoutIt()
    {
        var end = Start.AddDays(40);
        var feed = new FakeFeed();
        var nq = await SweepSeries.LoadAsync(feed, "@NQ", [60], Start.AddDays(10), end, warmupDays: 30d);
        var es = await SweepSeries.LoadAsync(feed, "@ES", [60], Start.AddDays(10), end, warmupDays: 30d);
        var job = new SweepJob("RC_XMK", new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60, ["ReferenceSymbol"] = "ES",
            ["Mode"] = 0, ["LookbackBars"] = 5, ["MaxBars"] = 2, ["IntradayOnly"] = 0
        });

        var outcome = new SweepRunner(nq, new Dictionary<string, SweepSeries> { ["@ES"] = es }).Run(job);

        Assert.True(outcome.Evaluations > 0);
        Assert.True(outcome.Signals > 0, "Il riferimento a dente di sega rompe il canale a ogni barra: sopra salendo, sotto al ritorno.");
        Assert.True(outcome.Trades > 0);

        var error = Assert.Throws<InvalidOperationException>(() => new SweepRunner(nq).Run(job));
        Assert.Contains("@ES", error.Message);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestXmk strategy, OhlcvData[] primary, OhlcvData[] reference, SignalType? position) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = primary,
            BarTimeUtc = primary[^1].DateTime,
            ReferenceOhlcv = new Dictionary<string, OhlcvData[]> { ["@ES"] = reference },
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = primary[^1].DateTime,
                Position = position.HasValue ? new StrategyPositionSnapshot { Direction = position.Value } : null
            }
        });

    private static OhlcvData[] Flat(int count) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), 101m, 99m, 100m)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal high, decimal low, decimal close) =>
        new() { DateTime = time, Open = 100m, High = high, Low = low, Close = close, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>XMK di prova: NQ a 60 minuti con ES come riferimento, canale di 20 barre, multiday.</summary>
    private sealed class TestXmk : CrossMarketEngine
    {
        public TestXmk() => IntradayOnly = false;

        public int ModeValue { set => Mode = value; }
        public int Side { set => Direction = value; }

        public override string Name => "XMK_TEST";
        public override string Description => "XMK di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }

    /// <summary>
    /// Feed in memoria: NQ piatto a 100, ES a dente di sega (100, 101, ... 109, poi di nuovo 100), cosi'
    /// il riferimento rompe il proprio canale in continuazione.
    /// </summary>
    private sealed class FakeFeed : IPiootooDataFeedService
    {
        public Task<OhlcvData[]> GetCandlesAsync(string symbol, DateTime currentDate, int numberOfCandles, int timeframeMinutes = 60, string? broker = null) =>
            throw new NotSupportedException();

        public Task<OhlcvData[]> GetCandlesRangeAsync(string symbol, DateTime startUtc, DateTime endUtc, int timeframeMinutes, string? broker = null)
        {
            var sawtooth = symbol.Contains("ES", StringComparison.OrdinalIgnoreCase);
            var bars = new List<OhlcvData>();
            var index = 0;
            for (var time = startUtc; time <= endUtc; time = time.AddMinutes(timeframeMinutes), index++)
            {
                var close = sawtooth ? 100m + index % 10 : 100m;
                bars.Add(new OhlcvData { DateTime = time, Open = close, High = close + 0.5m, Low = close - 0.5m, Close = close, Volume = 1m });
            }

            return Task.FromResult(bars.ToArray());
        }
    }
}
