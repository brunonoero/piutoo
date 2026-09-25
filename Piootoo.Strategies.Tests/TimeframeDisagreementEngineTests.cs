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
/// Il motore MTF della serie PT6EXO. La serie bassa e' oraria su NQ, piatta a 100 salvo le ultime due
/// chiusure: 99 e 98 danno un RSI(2) di Wilder pari a 0 (nessun guadagno, perdite 0,5 e poi 0,75), 101 e
/// 102 un RSI di 100. La serie alta e' giornaliera, con le barre aperte alle 23:00 UTC — la mezzanotte di
/// Roma d'inverno, l'ancoraggio di NQ — e chiusure crescenti (trend su) o calanti (trend giu').
/// </summary>
public sealed class TimeframeDisagreementEngineTests
{
    /// <summary>Ultima barra bassa: martedi' 16/01/2024 07:00 UTC, chiude alle 08:00.</summary>
    private static readonly DateTime LowerEnd = new(2024, 1, 16, 7, 0, 0, DateTimeKind.Utc);

    /// <summary>Apertura della giornaliera in corso a <see cref="LowerEnd"/>: lunedi' 15/01 23:00 UTC.</summary>
    private static readonly DateTime HigherInProgress = new(2024, 1, 15, 23, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Trend alto su (ultima chiusa 158 contro la media 133,5 delle cinquanta chiuse) e RSI basso a 0:
    /// long a mercato sulla barra dopo.
    /// </summary>
    [Fact]
    public void AnOversoldPullbackInAnUptrendBuysAtMarketOnTheNextBar()
    {
        var bars = Oversold(LowerEnd);

        var signal = Evaluate(new TestMtf(), bars, Rising(HigherInProgress, 60), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio: trend alto giu' e RSI a 100, short.</summary>
    [Fact]
    public void AnOverboughtBounceInADowntrendSells()
    {
        var signal = Evaluate(new TestMtf(), Overbought(LowerEnd), Falling(HigherInProgress, 60), null);

        Assert.Equal(SignalType.Sell, signal.Type);
    }

    /// <summary>
    /// Timeframe d'accordo non e' un segnale: l'ipercomprato in trend su e l'ipervenduto in trend giu'
    /// non si tradano, e senza eccesso (RSI a 50 su una serie piatta) nemmeno.
    /// </summary>
    [Fact]
    public void AgreementOrNoExcessIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestMtf(), Overbought(LowerEnd), Rising(HigherInProgress, 60), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestMtf(), Oversold(LowerEnd), Falling(HigherInProgress, 60), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestMtf(), Lower(LowerEnd, 200), Rising(HigherInProgress, 60), null).Type);
    }

    /// <summary>
    /// La giornaliera in corso non si legge. Qui la sua chiusura (0) rovescerebbe il trend, se contasse:
    /// il long nasce lo stesso, perche' il trend e' quello dell'ultima giornaliera chiusa.
    /// </summary>
    [Fact]
    public void TheHigherBarInProgressIsIgnored()
    {
        var higher = Rising(HigherInProgress, 60);
        higher[^1] = Daily(HigherInProgress, 0m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestMtf(), Oversold(LowerEnd), higher, null).Type);
    }

    /// <summary>
    /// Il confine: la giornaliera aperta domenica 14/01 alle 23:00 UTC chiude lunedi' alle 23:00. La barra
    /// oraria delle 21:00 (chiude alle 22:00) non la vede e compra sul trend della giornaliera prima; quella
    /// delle 22:00 chiude insieme a lei, la vede chiusa, e la chiusura a 0 toglie il trend su.
    /// </summary>
    [Fact]
    public void AHigherBarCountsFromTheLowerBarThatClosesWithIt()
    {
        var lastDaily = new DateTime(2024, 1, 14, 23, 0, 0, DateTimeKind.Utc);
        var higher = Rising(lastDaily, 60);
        higher[^1] = Daily(lastDaily, 0m);

        var before = Evaluate(new TestMtf(), Oversold(new DateTime(2024, 1, 15, 21, 0, 0, DateTimeKind.Utc)), higher, null);
        var closing = Evaluate(new TestMtf(), Oversold(new DateTime(2024, 1, 15, 22, 0, 0, DateTimeKind.Utc)), higher, null);

        Assert.Equal(SignalType.Buy, before.Type);
        Assert.Equal(SignalType.Hold, closing.Type);
    }

    /// <summary>
    /// Senza serie alta il motore non valuta e lo dice: dal percorso senza timeframe aggiuntivi, da un
    /// dizionario vuoto, e da una serie con meno barre chiuse della media.
    /// </summary>
    [Fact]
    public void WithoutTheHigherSeriesNothingIsEmittedAndTheReasonSaysWhy()
    {
        var bars = Oversold(LowerEnd);

        var direct = new TestMtf().GenerateSignal(bars, LowerEnd);
        var empty = Evaluate(new TestMtf(), bars, null, null);
        var tooShort = Evaluate(new TestMtf(), bars, Rising(HigherInProgress, 8), null);

        Assert.Equal(SignalType.Hold, direct.Type);
        Assert.Contains("assente", direct.Reason);
        Assert.Equal(SignalType.Hold, empty.Type);
        Assert.Contains("assente", empty.Reason);
        Assert.Equal(SignalType.Hold, tooShort.Type);
        Assert.Contains("troppo corta", tooShort.Reason);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold,
            Evaluate(new TestMtf(), Oversold(LowerEnd), Rising(HigherInProgress, 60), SignalType.Sell).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = Oversold(LowerEnd);
        var higher = Rising(HigherInProgress, 60);

        Assert.Equal(SignalType.Hold, Evaluate(new TestMtf { Side = 2 }, bars, higher, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestMtf { Side = 1 }, bars, higher, null).Type);
    }

    /// <summary>
    /// Configurazioni incoerenti fermano la valutazione: un timeframe alto non piu' alto di quello della
    /// strategia (il backtest non lo consegnerebbe nemmeno), soglie RSI invertite.
    /// </summary>
    [Fact]
    public void InconsistentConfigurationsAreRejected()
    {
        var bars = Oversold(LowerEnd);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestMtf { Higher = 60 }.GenerateSignal(bars, LowerEnd));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestMtf { Low = 80m, High = 20m }.GenerateSignal(bars, LowerEnd));
    }

    /// <summary>
    /// Il contenitore legge le leve proprie e quelle comuni, dichiara il timeframe alto come aggiuntivo, e
    /// l'RSI allarga la finestra minima.
    /// </summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_MTF();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 15,
            ["HigherTimeframeMinutes"] = 240, ["TrendBars"] = 20, ["RsiPeriod"] = 30,
            ["RsiLow"] = 5m, ["RsiHigh"] = 95m, ["Direction"] = 1,
            ["StopAtr"] = 1m, ["MaxBars"] = 8, ["IntradayOnly"] = 1,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(15, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(240, Assert.Single(((IMultiTimeframeTradingStrategy)strategy).AdditionalTimeframes));
        Assert.Equal(240, Read<int>(strategy, "HigherTimeframeMinutes"));
        Assert.Equal(20, Read<int>(strategy, "TrendBars"));
        Assert.Equal(30, Read<int>(strategy, "RsiPeriod"));
        Assert.Equal(5m, Read<decimal>(strategy, "RsiLow"));
        Assert.Equal(95m, Read<decimal>(strategy, "RsiHigh"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 301);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestMtf strategy, OhlcvData[] bars, OhlcvData[]? higher, SignalType? position) =>
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
            },
            AdditionalOhlcv = higher is null
                ? new Dictionary<int, OhlcvData[]>()
                : new Dictionary<int, OhlcvData[]> { [1440] = higher }
        });

    /// <summary>Serie oraria piatta a 100 che finisce in <paramref name="end"/>.</summary>
    private static OhlcvData[] Lower(DateTime end, int count) =>
        Enumerable.Range(0, count).Select(index => Hourly(end.AddHours(index - count + 1), 100m)).ToArray();

    /// <summary>Le ultime due chiusure scendono di un punto: RSI(2) = 0.</summary>
    private static OhlcvData[] Oversold(DateTime end)
    {
        var bars = Lower(end, 200);
        bars[^2] = Hourly(bars[^2].DateTime, 99m);
        bars[^1] = Hourly(bars[^1].DateTime, 98m);
        return bars;
    }

    /// <summary>Le ultime due chiusure salgono di un punto: RSI(2) = 100.</summary>
    private static OhlcvData[] Overbought(DateTime end)
    {
        var bars = Lower(end, 200);
        bars[^2] = Hourly(bars[^2].DateTime, 101m);
        bars[^1] = Hourly(bars[^1].DateTime, 102m);
        return bars;
    }

    /// <summary>
    /// <paramref name="count"/> giornaliere fino a quella aperta in <paramref name="lastOpen"/>, chiusure
    /// 100, 101, 102...: l'ultima chiusa ha sempre la chiusura piu' alta della propria media.
    /// </summary>
    private static OhlcvData[] Rising(DateTime lastOpen, int count) =>
        Enumerable.Range(0, count).Select(index => Daily(lastOpen.AddDays(index - count + 1), 100m + index)).ToArray();

    /// <summary>Lo specchio di <see cref="Rising"/>: chiusure 200, 199, 198...</summary>
    private static OhlcvData[] Falling(DateTime lastOpen, int count) =>
        Enumerable.Range(0, count).Select(index => Daily(lastOpen.AddDays(index - count + 1), 200m - index)).ToArray();

    private static OhlcvData Hourly(DateTime time, decimal close) =>
        new() { DateTime = time, Open = close, High = close + 1m, Low = close - 1m, Close = close, Volume = 1m };

    private static OhlcvData Daily(DateTime time, decimal close) =>
        new() { DateTime = time, Open = close, High = close + 5m, Low = close - 5m, Close = close, Volume = 1m };

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
    /// MTF di prova su NQ a 60 minuti con la giornaliera come timeframe alto, media a 50, RSI(2) 10/90.
    /// Le proprieta' scrivono i campi del motore.
    /// </summary>
    private sealed class TestMtf : TimeframeDisagreementEngine
    {
        public TestMtf() => IntradayOnly = false;

        public int Higher { set => HigherTimeframeMinutes = value; }
        public decimal Low { set => RsiLow = value; }
        public decimal High { set => RsiHigh = value; }
        public int Side { set => Direction = value; }

        public override string Name => "MTF_TEST";
        public override string Description => "MTF di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
