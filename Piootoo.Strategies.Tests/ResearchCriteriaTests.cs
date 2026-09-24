using Piootoo.Core.Optimization.Sweep;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I criteri con cui la ricerca giudica una configurazione, portati dalla ricerca Python v4/v5 il
/// 24/09/2026: costanza negli anni, outlier, soglia di average trade, UngerFit.
/// </summary>
public sealed class ResearchCriteriaTests
{
    /// <summary>
    /// Quattro anni regolari passano; uno che fa tutto l'utile e tre in perdita no, anche se il netto
    /// complessivo e' lo stesso: e' la differenza fra un edge e un anno fortunato.
    /// </summary>
    [Fact]
    public void AYearThatCarriesEverythingFailsTheConsistency()
    {
        var regular = Enumerable.Range(2022, 4).SelectMany(year => Trades(year, 20, 100m)).ToList();
        var lucky = Trades(2022, 20, 1000m).Concat(Enumerable.Range(2023, 3).SelectMany(year => Trades(year, 20, -100m))).ToList();

        Assert.True(ResearchCriteria.Years(regular, Utc(2022, 1, 1), Utc(2026, 1, 1)).Passes);
        Assert.False(ResearchCriteria.Years(lucky, Utc(2022, 1, 1), Utc(2026, 1, 1)).Passes);
    }

    /// <summary>Un anno con quattro trade boccia: la regola e' "almeno cinque in ogni anno che ne ha".</summary>
    [Fact]
    public void AThinYearFailsTheConsistency()
    {
        var trades = Trades(2022, 30, 50m).Concat(Trades(2023, 4, 50m)).Concat(Trades(2024, 30, 50m)).ToList();

        var years = ResearchCriteria.Years(trades, Utc(2022, 1, 1), Utc(2025, 1, 1));

        Assert.Equal(4, years.MinTradesInYear);
        Assert.False(years.Passes);
    }

    /// <summary>Troppo pochi trade all'anno in media bocciano anche se ogni anno ne ha cinque.</summary>
    [Fact]
    public void TooFewTradesPerYearFail()
    {
        var trades = Enumerable.Range(2022, 4).SelectMany(year => Trades(year, 6, 100m)).ToList();

        Assert.False(ResearchCriteria.Years(trades, Utc(2022, 1, 1), Utc(2026, 1, 1)).Passes);
    }

    /// <summary>Un trade che fa il 40% del netto e' un outlier; sotto il 30% no.</summary>
    [Fact]
    public void TheBestTradeShareIsMeasuredOnTheNet()
    {
        var withOutlier = Trades(2022, 10, 100m).Append(Trade(2022, 12, 1, 667m)).ToList();

        Assert.Equal(0.40m, Math.Round(ResearchCriteria.BestTradeShare(withOutlier)!.Value, 2));
        Assert.Null(ResearchCriteria.BestTradeShare(Trades(2022, 5, -10m)));
    }

    /// <summary>La soglia e' il 15% del range medio in denaro, con un pavimento di sei tick.</summary>
    [Fact]
    public void TheThresholdIsFifteenPercentOfTheRangeWithATickFloor()
    {
        var bars = new[] { Bar(100m, 90m), Bar(120m, 90m) };  // range medio 20

        Assert.Equal(75m, ResearchCriteria.AverageTradeThreshold(bars, pointValue: 25m, tickSize: 0.1m)); // 0,15 × 20 × 25
        Assert.Equal(1500m, ResearchCriteria.AverageTradeThreshold(bars, pointValue: 25m, tickSize: 10m)); // 6 × 10 × 25
    }

    /// <summary>UngerFit = √(E·100/R): average trade pari alla soglia e drawdown di cento trade medi fa 1.</summary>
    [Fact]
    public void UngerFitIsOneAtTheThresholdWithAHundredTradeDrawdown()
    {
        Assert.Equal(1m, Math.Round(ResearchCriteria.UngerFit(100m, 100m, 10_000m)!.Value, 6));
        Assert.Null(ResearchCriteria.UngerFit(-5m, 100m, 1_000m));
    }

    /// <summary>
    /// Un pattern che batte 190 estrazioni su 200 passa; uno che ne batte meta' no. Le estrazioni con
    /// meno di max(10, meta' dei trade della candidata) non contano.
    /// </summary>
    [Fact]
    public void RandomPatternsRejectAPatternThatAnyPatternMatches()
    {
        var strong = Enumerable.Range(0, 200).Select(i => (Trades: 100, AverageTrade: i < 10 ? 60m : 20m));
        var weak = Enumerable.Range(0, 200).Select(i => (Trades: 100, AverageTrade: i % 2 == 0 ? 60m : 20m));
        var thin = Enumerable.Range(0, 200).Select(_ => (Trades: 30, AverageTrade: 90m));

        var passes = ResearchCriteria.RandomPatterns(50m, 80, strong);
        Assert.Equal(200, passes.ValidDraws);
        Assert.True(passes.Passes, $"p {passes.P:N3}, Wilson {passes.WilsonLower:N3}");
        Assert.False(ResearchCriteria.RandomPatterns(50m, 80, weak).Passes);

        var none = ResearchCriteria.RandomPatterns(50m, 80, thin);
        Assert.Equal(0, none.ValidDraws);
        Assert.True(none.Passes);
    }

    /// <summary>Il Wilson inferiore di Python: p = 0,5 su 100 prove a z 1,2816 fa circa 0,436.</summary>
    [Fact]
    public void WilsonLowerMatchesTheFormula()
    {
        Assert.Equal(0.436, Math.Round(ResearchCriteria.WilsonLower(0.5, 100, ResearchCriteria.WilsonZ), 3));
    }

    private static IEnumerable<TradingResult> Trades(int year, int count, decimal net) =>
        Enumerable.Range(0, count).Select(i => Trade(year, 1 + i % 12, 1 + i % 27, net));

    private static TradingResult Trade(int year, int month, int day, decimal net) => new()
    {
        Symbol = "@FESX",
        EntryDate = Utc(year, month, day),
        ExitDate = Utc(year, month, day).AddHours(4),
        // Il netto si ottiene dai prezzi: un long da 1 contratto a 1 per punto.
        Direction = Piootoo.Shared.Enums.SignalType.Buy,
        EntryPrice = 1000m,
        ExitPrice = 1000m + net,
        Quantity = 1m
    };

    private static OhlcvData Bar(decimal high, decimal low) => new() { High = high, Low = low, Open = low, Close = high };

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
