using System.Reflection;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore RAN, controllo a ingresso casuale della serie PT6EXO. Il punto che conta e' che il caso
/// sia una <b>funzione della barra</b>: se dipendesse dalla storia ricevuta o dal processo, backtest e
/// sessione live sceglierebbero barre diverse e il controllo non controllerebbe niente.
/// </summary>
public sealed class RandomEntryEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// La stessa barra da' la stessa decisione con una finestra di storia di lunghezza diversa: e' la
    /// condizione perche' backtest (finestra del cursore) e live (finestra spinta dal cBot) coincidano.
    /// </summary>
    [Fact]
    public void TheDecisionDependsOnTheBarNotOnTheHistory()
    {
        var strategy = new TestRan { Probability = 0.2m };
        var bars = HourlyBars(600);
        var entries = 0;

        for (var last = 400; last < 600; last++)
        {
            var shortWindow = bars[(last - strategy.RequiredCandles + 1)..(last + 1)];
            var longWindow = bars[..(last + 1)];

            var fromShort = Evaluate(strategy, shortWindow, null);
            var fromLong = Evaluate(strategy, longWindow, null);

            Assert.Equal(fromShort.Type, fromLong.Type);
            if (fromShort.Type != SignalType.Hold) entries++;
        }

        Assert.True(entries > 0, "Con p = 0,2 su 200 barre ci si aspetta qualche ingresso.");
    }

    /// <summary>
    /// Il valore dell'estrazione e' fissato: se cambia, cambiano le barre scelte da ogni seme e i run
    /// di controllo archiviati non si ripetono piu'. Un cambio voluto aggiorna questo numero e lo dice
    /// in <c>decisioni.md</c>.
    /// </summary>
    [Fact]
    public void TheDrawIsStableAcrossProcesses()
    {
        var strategy = new TestRan();

        Assert.Equal(GoldenDraw, strategy.Draw(new DateTime(2025, 3, 14, 15, 0, 0, DateTimeKind.Utc), 0UL), 15);
    }

    // Misurato il 25/09/2026 alla nascita del motore: seme 1, @NQ, 60 minuti, 14/03/2025 15:00 UTC, sale 0.
    private const double GoldenDraw = 0.97774101993210527;

    /// <summary>La frequenza degli ingressi e' quella dichiarata, e i due lati si dividono a meta'.</summary>
    [Fact]
    public void TheEntryFrequencyMatchesTheProbabilityAndTheSidesAreBalanced()
    {
        var strategy = new TestRan { Probability = 0.05m };
        const int samples = 50_000;
        var entries = 0;
        var longs = 0;

        for (var index = 0; index < samples; index++)
        {
            var time = Start.AddHours(index);
            if (strategy.Draw(time, EntrySalt) >= 0.05) continue;
            entries++;
            if (strategy.Draw(time, SideSalt) < 0.5) longs++;
        }

        var frequency = entries / (double)samples;
        Assert.InRange(frequency, 0.045, 0.055);
        Assert.InRange(longs / (double)entries, 0.46, 0.54);
    }

    /// <summary>
    /// Semi diversi scelgono barre indipendenti: la quota di barre scelte da entrambi e' circa p, non
    /// zero (sequenze complementari) e non uno (lo stesso seme travestito).
    /// </summary>
    [Fact]
    public void DifferentSeedsChooseIndependentBars()
    {
        var first = new TestRan { SeedValue = 1 };
        var second = new TestRan { SeedValue = 2 };
        const double p = 0.1;
        var chosen = 0;
        var both = 0;

        for (var index = 0; index < 30_000; index++)
        {
            var time = Start.AddHours(index);
            if (first.Draw(time, EntrySalt) >= p) continue;
            chosen++;
            if (second.Draw(time, EntrySalt) < p) both++;
        }

        Assert.InRange(both / (double)chosen, 0.07, 0.13);
    }

    /// <summary>Anche simboli e timeframe diversi, a parita' di seme, estraggono in modo indipendente.</summary>
    [Fact]
    public void SymbolAndTimeframeEnterTheDraw()
    {
        var time = Start.AddHours(123);
        var nq60 = new TestRan().Draw(time, EntrySalt);

        Assert.NotEqual(nq60, new TestRan { SymbolValue = "@ES" }.Draw(time, EntrySalt));
        Assert.NotEqual(nq60, new TestRan { Timeframe = 30 }.Draw(time, EntrySalt));
    }

    /// <summary>L'ingresso e' a mercato sulla barra dopo, come ogni "next bar at market".</summary>
    [Fact]
    public void AnEntryIsAMarketOrderOnTheNextBar()
    {
        var strategy = new TestRan { Probability = 1m };
        var bars = HourlyBars(200);

        var signal = Evaluate(strategy, bars, null);

        Assert.True(signal.Type is SignalType.Buy or SignalType.Sell);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    /// <summary>In posizione non nasce nulla: il caso conta le sole barre flat.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var strategy = new TestRan { Probability = 1m };
        var bars = HourlyBars(200);

        Assert.Equal(SignalType.Hold, Evaluate(strategy, bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(strategy, bars, SignalType.Sell).Type);
    }

    /// <summary>Con un lato dichiarato si entra solo da quel lato.</summary>
    [Theory]
    [InlineData(1, SignalType.Buy)]
    [InlineData(2, SignalType.Sell)]
    public void ADeclaredDirectionFixesTheSide(int direction, SignalType expected)
    {
        var strategy = new TestRan { Probability = 1m, DirectionValue = direction };
        var bars = HourlyBars(400);

        for (var last = 200; last < 400; last++)
            Assert.Equal(expected, Evaluate(strategy, bars[..(last + 1)], null).Type);
    }

    /// <summary>Una probabilita' fuori da [0, 1] e' un errore di configurazione, non un motore muto.</summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void AProbabilityOutsideZeroOneIsRejected(double probability)
    {
        var strategy = new TestRan { Probability = (decimal)probability };

        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(HourlyBars(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni della griglia.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_RAN();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 15,
            ["Seed"] = 7, ["EntryProbability"] = 0.02m, ["Direction"] = 1,
            ["StopAtr"] = 1.5m, ["TargetAtr"] = 3m, ["MaxBars"] = 12, ["IntradayOnly"] = 0,
            ["ExitHour"] = 21, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(15, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(7, Read<int>(strategy, "Seed"));
        Assert.Equal(0.02m, Read<decimal>(strategy, "EntryProbability"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.Equal(1.5m, Read<decimal>(strategy, "StopAtrMultiplier"));
    }

    // ------------------------------------------------------------------ supporto

    private const ulong EntrySalt = 0x9E3779B97F4A7C15UL;
    private const ulong SideSalt = 0xC2B2AE3D27D4EB4FUL;

    private static TradeSignal Evaluate(TestRan strategy, OhlcvData[] bars, SignalType? position) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime,
                Position = position.HasValue ? new StrategyPositionSnapshot { Direction = position.Value } : null
            }
        });

    private static OhlcvData[] HourlyBars(int count) =>
        Enumerable.Range(0, count).Select(index => new OhlcvData
        {
            DateTime = Start.AddHours(index),
            Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1m
        }).ToArray();

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>
    /// RAN di prova. Le proprieta' scrivono i campi protetti del motore: la base clona i campi a ogni
    /// valutazione, quindi devono stare li' e non in campi di questa classe.
    /// </summary>
    private sealed class TestRan : RandomEntryEngine
    {
        public TestRan() => IntradayOnly = false;

        public decimal Probability { set => EntryProbability = value; }
        public int SeedValue { set => Seed = value; }
        public int DirectionValue { set => Direction = value; }
        public string SymbolValue { set => _symbol = value; }
        public int Timeframe { set => _timeframe = value; }

        private string _symbol = "@NQ";
        private int _timeframe = 60;

        public override string Name => "RAN_TEST";
        public override string Description => "RAN di prova";
        public override string Symbol => _symbol;
        public override int TimeframeMinutes => _timeframe;
    }
}
