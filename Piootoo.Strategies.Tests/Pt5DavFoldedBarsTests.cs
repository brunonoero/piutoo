using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le barre piegate dal motore (<see cref="Pt5DavEngineBase.BarMinutes"/>): la strategia riceve la
/// serie a 60 minuti e ragiona sulle barre a 4 ore della ricerca, che sul DAX partono dalle 08:00 e
/// nel feed non esistono.
///
/// <para>La fedelta' la misura la riconciliazione (<c>Pt5DavParityStudy</c>, le cinque
/// <c>PT8DAV_FDAX_*_240</c>); qui si fissano le tre proprieta' da cui dipende, perche' uno studio non
/// gira a ogni build: si decide solo sulla barra oraria che chiude una barra della ricerca, il
/// livello e' quello delle barre piegate, e l'ordine vive una barra della ricerca.</para>
/// </summary>
public sealed class Pt5DavFoldedBarsTests
{
    private static readonly SessionClock Rome = new("Europe/Rome");

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(20)]
    public void AnHourlyBarThatDoesNotCloseAResearchBarIsNotAMomentOfTheStrategy(int romeHour)
    {
        var signal = Evaluate(new FoldedBreakout(), EndingAt(new DateTime(2026, 2, 26), romeHour));

        Assert.Equal(SignalType.Hold, signal.Type);
    }

    [Fact]
    public void TheOrderIsBornOnTheResearchBarAndLivesOneOfThem()
    {
        var day = new DateTime(2026, 2, 26);
        var signal = Evaluate(new FoldedBreakout(), EndingAt(day, 11));

        var buy = Entries(signal).Single(entry => entry.Type == SignalType.Buy);

        // Valido dalla chiusura della barra 08-12, per una barra della ricerca: non dalle 12:00 alle
        // 13:00, che sarebbe "la barra successiva" della serie ricevuta.
        Assert.Equal(Rome.ToUtc(day.AddHours(12)), buy.ValidFromUtc);
        Assert.Equal(Rome.ToUtc(day.AddHours(12)), buy.ExpiresAtUtc);
        Assert.Equal(240, buy.TimeframeMinutes);
        Assert.Equal(TradeOrderType.Stop, buy.OrderType);

        // Il livello e' il massimo della sessione in corso sulle barre PIEGATE: il massimo delle
        // quattro orarie 08-11, non della sola ultima.
        Assert.Equal(SessionBase(day) + 10m + 3m, buy.Price);
    }

    [Fact]
    public void AStopOrderBornOnTheLastResearchBarOfTheSessionDoesNotCrossTheNight()
    {
        // La barra oraria delle 21 chiude la barra tronca 20-22: la barra dopo e' quella delle 08:00
        // del giorno dopo, e uno stop non ci arriva.
        var signal = Evaluate(new FoldedBreakout(), EndingAt(new DateTime(2026, 2, 26), 21));

        Assert.DoesNotContain(Entries(signal), entry => entry.Type != SignalType.Hold);
    }

    [Fact]
    public void AnEngineThatLetsTheExecutorCountBarsRefusesToFold()
    {
        // La valutazione passa per riflessione (StatelessEasyStrategyBase), che incarta l'eccezione.
        var error = Assert.ThrowsAny<Exception>(
            () => Evaluate(new FoldedBias(), EndingAt(new DateTime(2026, 2, 26), 11)));

        var refusal = Assert.IsType<NotSupportedException>(error.GetBaseException());
        Assert.Contains("BarMinutes", refusal.Message);
    }

    // ------------------------------------------------------------------ supporto

    /// <summary>BO_S senza filtri sul DAX: serie a 60 minuti, barre da 240.</summary>
    private sealed class FoldedBreakout : Pt5DavCurrentSessionBreakoutEngine
    {
        public override string Name => "TEST_FDAX_BOS_FOLDED";
        public override string Description => "BO_S di prova con le barre piegate";
        public override string Symbol => "@FDAX";
        public override int TimeframeMinutes => 60;
        public override int BarMinutes => 240;
        public override string ResearchCode => "TEST";

        public FoldedBreakout()
        {
            TradingWindow = ResearchWindow(-1, -1);
            IntradayOnly = false;
            StopAtr = 1m;
        }
    }

    private sealed class FoldedBias : Pt5DavBiasMarketEngine
    {
        public override string Name => "TEST_FDAX_BIA_FOLDED";
        public override string Description => "BIAS di prova con le barre piegate";
        public override string Symbol => "@FDAX";
        public override int TimeframeMinutes => 60;
        public override int BarMinutes => 240;
        public override string ResearchCode => "TEST";

        public FoldedBias()
        {
            TradingWindow = ResearchWindow(-1, -1);
        }
    }

    private static IEnumerable<TradeSignal> Entries(TradeSignal signal) =>
        new[] { signal }.Concat(signal.CompanionSignals ?? []);

    private static TradeSignal Evaluate(Pt5DavEngineBase strategy, OhlcvData[] bars) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "FDAX",
                BarTimeUtc = bars[^1].DateTime
            }
        });

    private static decimal SessionBase(DateTime day) => 20_000m + day.DayOfYear;

    /// <summary>
    /// Ottanta sessioni di barre orarie 08-21 di Roma (giorni feriali), l'ultima delle quali finisce
    /// con la barra che apre a <paramref name="lastRomeHour"/> del giorno indicato. In ogni sessione
    /// il massimo sale di un punto all'ora, cosi' il massimo "finora" dice quante ore sono entrate.
    /// </summary>
    private static OhlcvData[] EndingAt(DateTime lastDay, int lastRomeHour)
    {
        var days = new List<DateTime>();
        for (var day = lastDay.Date; days.Count < 80; day = day.AddDays(-1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days.Add(day);
        }

        days.Reverse();
        var bars = new List<OhlcvData>(80 * 14);
        foreach (var day in days)
        {
            var last = day == lastDay.Date ? lastRomeHour : 21;
            for (var hour = 8; hour <= last; hour++)
            {
                var price = SessionBase(day);
                bars.Add(new OhlcvData
                {
                    DateTime = Rome.ToUtc(day.AddHours(hour)),
                    Open = price,
                    High = price + 10m + (hour - 8),
                    Low = price - 10m,
                    Close = price,
                    Volume = 1m
                });
            }
        }

        return bars.ToArray();
    }
}
