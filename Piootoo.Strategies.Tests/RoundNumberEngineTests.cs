using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore RNM della serie PT6EXO. Livelli ogni 100 punti, tick di 0,25 e distanza di 20 tick: una
/// chiusura e' vicina a un livello entro 5 punti. Le serie di prova sono piatte a 18.050, a meta' fra due
/// livelli; ogni caso cambia la chiusura dell'ultima barra.
/// </summary>
public sealed class RoundNumberEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Magnete: 18.097 e' 3 punti sotto 18.100, long a mercato sulla barra dopo con il target sul livello,
    /// cioe' 3 punti nel denaro del contratto.
    /// </summary>
    [Fact]
    public void BelowALevelTheMagnetBuysWithTheTargetOnTheLevel()
    {
        var bars = Closing(18_097m);

        var signal = Evaluate(new TestRnm(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(3m * InstrumentRegistry.PointValue("@NQ"), signal.TakeProfitMoneyPerFutureContract);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>Lo specchio: 18.004 e' 4 punti sopra 18.000, short con il target a 4 punti.</summary>
    [Fact]
    public void AboveALevelTheMagnetSells()
    {
        var signal = Evaluate(new TestRnm(), Closing(18_004m), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(4m * InstrumentRegistry.PointValue("@NQ"), signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// La distanza e' compresa (5 punti esatti scattano, 5,25 no), a meta' fra due livelli non succede
    /// niente, e una chiusura esattamente sul livello e' gia' arrivata: i livelli vicini sono a 100 punti.
    /// </summary>
    [Fact]
    public void TheDistanceIsInclusiveAndACloseOnTheLevelIsNotASignal()
    {
        Assert.Equal(SignalType.Buy, Evaluate(new TestRnm(), Closing(18_095m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm(), Closing(18_094.75m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm(), Closing(18_050m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm(), Closing(18_000m), null).Type);
    }

    /// <summary>Con <c>TargetAtLevel = 0</c> il magnete non fissa un target: restano le uscite comuni (qui nessuna).</summary>
    [Fact]
    public void WithoutTargetAtLevelTheCommonExitsApply()
    {
        var signal = Evaluate(new TestRnm { Target = false }, Closing(18_097m), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// Muro: 18.097 e' vicino a 18.100, limit sell sul livello valido una barra; 18.004 e' vicino a 18.000,
    /// limit buy.
    /// </summary>
    [Fact]
    public void TheWallPlacesALimitOnTheNearLevel()
    {
        var bars = Closing(18_097m);

        var sell = Evaluate(new TestRnm { Wall = true }, bars, null);
        var buy = Evaluate(new TestRnm { Wall = true }, Closing(18_004m), null);

        Assert.Equal(SignalType.Sell, sell.Type);
        Assert.Equal(TradeOrderType.Limit, sell.OrderType);
        Assert.Equal(18_100m, sell.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), sell.ValidFromUtc);
        Assert.Equal(bars[^1].DateTime.AddHours(1), sell.ExpiresAtUtc);
        Assert.Equal(SignalType.Buy, buy.Type);
        Assert.Equal(TradeOrderType.Limit, buy.OrderType);
        Assert.Equal(18_000m, buy.Price);
    }

    /// <summary>
    /// Con livelli ogni 10 punti 18.005 dista 5 da entrambi: il muro arma i due limit in OCO, il sell a
    /// 18.010 come primario e il buy a 18.000 come compagno.
    /// </summary>
    [Fact]
    public void TheWallArmsBothLevelsInOcoWhenBothAreNear()
    {
        var signal = Evaluate(new TestRnm { Wall = true, Step = 10m }, Closing(18_005m), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(18_010m, signal.Price);
        var companion = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Buy, companion.Type);
        Assert.Equal(TradeOrderType.Limit, companion.OrderType);
        Assert.Equal(18_000m, companion.Price);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm { Side = 1 }, Closing(18_004m), null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestRnm { Side = 1 }, Closing(18_097m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm { Side = 1, Wall = true }, Closing(18_097m), null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestRnm(), Closing(18_097m), SignalType.Sell).Type);
    }

    /// <summary>
    /// Configurazioni incoerenti: un passo nullo, e un magnete con le due zone che si toccano (5 punti per
    /// lato su livelli ogni 10), che armerebbe long e short a mercato sulla stessa barra. Lo stesso passo
    /// col muro e' lecito.
    /// </summary>
    [Fact]
    public void InconsistentConfigurationsAreRejected()
    {
        var bars = Closing(18_005m);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestRnm { Step = 0m }.GenerateSignal(bars, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestRnm { Step = 10m }.GenerateSignal(bars, Start));
        Assert.NotEqual(SignalType.Hold, new TestRnm { Step = 10m, Wall = true }.GenerateSignal(bars, Start).Type);
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e prende il tick dal registro dello strumento.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_RNM();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 60,
            ["RoundStep"] = 500m, ["Mode"] = 1, ["DistanceTicks"] = 10,
            ["TargetAtLevel"] = 0, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["MaxBars"] = 4, ["IntradayOnly"] = 1,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(500m, Read<decimal>(strategy, "RoundStep"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(10, Read<int>(strategy, "DistanceTicks"));
        Assert.Equal(0, Read<int>(strategy, "TargetAtLevel"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestRnm strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Serie piatta a 18.050 la cui ultima barra chiude a <paramref name="close"/>.</summary>
    private static OhlcvData[] Closing(decimal close)
    {
        var bars = Enumerable.Range(0, 200).Select(index => Bar(Start.AddHours(index), 18_050m)).ToArray();
        bars[^1] = Bar(bars[^1].DateTime, close);
        return bars;
    }

    private static OhlcvData Bar(DateTime time, decimal close) =>
        new() { DateTime = time, Open = close, High = close + 1m, Low = close - 1m, Close = close, Volume = 1m };

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
    /// RNM di prova su NQ a 60 minuti: livelli ogni 100, tick 0,25, distanza 20 tick, magnete con target
    /// sul livello. Le proprieta' scrivono i campi del motore.
    /// </summary>
    private sealed class TestRnm : RoundNumberEngine
    {
        public TestRnm()
        {
            IntradayOnly = false;
            RoundStep = 100m;
            TickSize = 0.25m;
            DistanceTicks = 20;
            TargetAtLevel = 1;
        }

        public decimal Step { set => RoundStep = value; }
        public bool Wall { set => Mode = value ? 1 : 0; }
        public bool Target { set => TargetAtLevel = value ? 1 : 0; }
        public int Side { set => Direction = value; }

        public override string Name => "RNM_TEST";
        public override string Description => "RNM di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
