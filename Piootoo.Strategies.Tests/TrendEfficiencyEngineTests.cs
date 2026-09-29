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
/// Il motore ERT della serie PT6EXO. Finestra di 4 barre, quindi punteggio = ER × 2; soglie 1,5 di
/// ingresso (ER 0,75) e 0,5 di uscita (ER 0,25). Le serie di prova partono da 601 chiusure che alternano
/// 100 e 101 — ER nullo, l'ultima a 100 — e aggiungono in coda le chiusure del caso.
/// </summary>
public sealed class TrendEfficiencyEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Lunghezza del tratto laterale: dispari, cosi' finisce a 100, e oltre la storia minima della base.</summary>
    private const int Prefix = 601;

    /// <summary>
    /// Quattro chiusure in salita dopo il tratto laterale: ER 1, punteggio 2. Sulla barra prima la finestra
    /// era 101, 100, 101, 102, 103 (netto 2 su un percorso di 4, punteggio 1): il trend si accende ora, long
    /// a mercato sulla barra dopo.
    /// </summary>
    [Fact]
    public void ATrendThatSwitchesOnBuysAtMarketOnTheNextBar()
    {
        var bars = Series(101m, 102m, 103m, 104m);

        var signal = Evaluate(new TestErt(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.False(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>
    /// Lo specchio: dopo un rimbalzo a 101, quattro chiusure in discesa vendono. Il rimbalzo serve perche' il
    /// laterale finisce con un passo in giu' (101 → 100): senza, il trend sarebbe gia' acceso sulla barra prima.
    /// </summary>
    [Fact]
    public void ATrendThatSwitchesOnDownSells()
    {
        Assert.Equal(SignalType.Sell, Evaluate(new TestErt(), Series(101m, 100m, 99m, 98m, 97m), null).Type);
    }

    /// <summary>
    /// Un trend gia' acceso sulla barra prima (finestra 100…104, punteggio 2) non e' nuovo: con
    /// <c>FreshOnly</c> non si entra, senza si'.
    /// </summary>
    [Fact]
    public void AStaleTrendEntersOnlyWithoutFreshOnly()
    {
        var bars = Series(101m, 102m, 103m, 104m, 105m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestErt(), bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestErt { Fresh = 0 }, bars, null).Type);
    }

    /// <summary>Il tratto laterale e un percorso nullo non danno segnale.</summary>
    [Fact]
    public void AChoppyOrFlatSeriesIsNotASignal()
    {
        var flat = Enumerable.Range(0, Prefix).Select(index => Bar(Start.AddHours(index), 100m)).ToArray();

        Assert.Equal(SignalType.Hold, Evaluate(new TestErt(), Series(), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestErt(), flat, null).Type);
    }

    /// <summary>In long, il trend spento (punteggio 0 sul laterale) chiude a mercato con un'uscita pura.</summary>
    [Fact]
    public void ALongExitsWhenTheTrendSwitchesOff()
    {
        var signal = Evaluate(new TestErt(), Series(), SignalType.Buy);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.True(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
    }

    /// <summary>Lo short esce allo stesso modo; un long con il trend ancora acceso resta dentro.</summary>
    [Fact]
    public void AShortExitsAndALongInTrendHolds()
    {
        var shortExit = Evaluate(new TestErt(), Series(), SignalType.Sell);

        Assert.Equal(SignalType.Buy, shortExit.Type);
        Assert.True(shortExit.ExitOnly);
        Assert.Equal(SignalType.Hold, Evaluate(new TestErt(), Series(101m, 102m, 103m, 104m), SignalType.Buy).Type);
    }

    /// <summary>
    /// Un'inversione passa per l'uscita: long dentro un trend che ora scende (punteggio −2) esce, e il
    /// segnale non apre uno short.
    /// </summary>
    [Fact]
    public void AReversalExitsTheLongWithoutOpeningAShort()
    {
        var signal = Evaluate(new TestErt(), Series(99m, 98m, 97m, 96m), SignalType.Buy);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.True(signal.ExitOnly);
    }

    /// <summary>Senza uscita di regime restano le uscite comuni: in posizione non nasce nulla.</summary>
    [Fact]
    public void WithoutExitScoreNothingIsEmittedInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestErt { ExitAt = 0m }, Series(), SignalType.Buy).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = Series(101m, 102m, 103m, 104m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestErt { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestErt { Side = 1 }, bars, null).Type);
    }

    /// <summary>
    /// Il punteggio ha la stessa scala per ogni finestra: su 16 barre la stessa rampa dopo il laterale ha
    /// ER 1 e punteggio 4, oltre la soglia come su 4 barre.
    /// </summary>
    [Fact]
    public void TheScoreScalesWithTheSquareRootOfTheWindow()
    {
        var bars = Series(Enumerable.Range(1, 16).Select(step => 100m + step).ToArray());

        Assert.Equal(SignalType.Buy, Evaluate(new TestErt { Fresh = 0, Window = 16, EntryAt = 3.9m }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestErt { Fresh = 0, Window = 16, EntryAt = 4.1m }, bars, null).Type);
    }

    /// <summary>Soglie incoerenti sono un errore di configurazione, non un motore muto.</summary>
    [Fact]
    public void InconsistentThresholdsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TestErt { EntryAt = 1m, ExitAt = 1m }.GenerateSignal(Series(), Start));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TestErt { Window = 1 }.GenerateSignal(Series(), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e la finestra allarga la storia minima.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_ERT();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 240,
            ["ErBars"] = 80, ["EntryScore"] = 2.5m, ["ExitScore"] = 0.3m, ["FreshOnly"] = 0, ["Direction"] = 1,
            ["StopAtr"] = 2m, ["MaxBars"] = 5, ["IntradayOnly"] = 0,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(240, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(80, Read<int>(strategy, "ErBars"));
        Assert.Equal(2.5m, Read<decimal>(strategy, "EntryScore"));
        Assert.Equal(0.3m, Read<decimal>(strategy, "ExitScore"));
        Assert.Equal(0, Read<int>(strategy, "FreshOnly"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 82);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestErt strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary><see cref="Prefix"/> chiusure laterali (100, 101, …, 100) seguite da <paramref name="tail"/>, barre orarie.</summary>
    private static OhlcvData[] Series(params decimal[] tail)
    {
        var closes = Enumerable.Range(0, Prefix).Select(index => index % 2 == 0 ? 100m : 101m).Concat(tail).ToArray();
        return closes.Select((close, index) => Bar(Start.AddHours(index), close)).ToArray();
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

    /// <summary>ERT di prova su NQ a 60 minuti, finestra 4, soglie 1,5/0,5. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestErt : TrendEfficiencyEngine
    {
        public TestErt()
        {
            IntradayOnly = false;
            ErBars = 4;
            EntryScore = 1.5m;
            ExitScore = 0.5m;
        }

        public int Window { set => ErBars = value; }
        public decimal EntryAt { set => EntryScore = value; }
        public decimal ExitAt { set => ExitScore = value; }
        public int Fresh { set => FreshOnly = value; }
        public int Side { set => Direction = value; }

        public override string Name => "ERT_TEST";
        public override string Description => "ERT di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
