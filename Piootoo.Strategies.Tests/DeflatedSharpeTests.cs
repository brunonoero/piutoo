using Piootoo.Core.Optimization.Sweep;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Lo Sharpe deflazionato: la soglia che il migliore di K prove raggiunge con il solo rumore.
/// </summary>
public sealed class DeflatedSharpeTests
{
    [Theory]
    [InlineData(0.5, 0.0)]
    [InlineData(0.975, 1.959964)]
    [InlineData(0.999, 3.090232)]
    [InlineData(0.01, -2.326348)]
    public void NormalQuantileMatchesTheTables(double p, double expected) =>
        Assert.Equal(expected, DeflatedSharpe.NormalQuantile(p), 5);

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.959964, 0.975)]
    [InlineData(-1.644854, 0.05)]
    public void NormalCdfMatchesTheTables(double x, double expected) =>
        Assert.Equal(expected, DeflatedSharpe.NormalCdf(x), 5);

    /// <summary>
    /// Il massimo atteso di K normali: zero per una prova sola, circa 1,54 per 10, 3,24 per 1.000 e 4,37 per 100.000
    /// (valori esatti per integrazione; l'approssimazione di Bailey e Lopez de Prado sta entro 0,1).
    /// </summary>
    [Theory]
    [InlineData(1L, 0.0)]
    [InlineData(10L, 1.539)]
    [InlineData(1000L, 3.241)]
    [InlineData(100000L, 4.370)]
    public void ExpectedMaximumGrowsLikeTheSquareRootOfTheLogarithm(long trials, double expected) =>
        Assert.InRange(DeflatedSharpe.ExpectedMaxOfNormals(trials), expected - 0.1, expected + 0.1);

    /// <summary>
    /// Lo stesso average trade si distingue dal rumore dopo una prova sola e non dopo centomila: e'
    /// la ragione per cui il conteggio delle prove sta accanto al risultato.
    /// </summary>
    [Fact]
    public void SameResultStopsBeingDistinguishableAsTrialsGrow()
    {
        var trades = Trades(count: 300, mean: 200m, spread: 1000m, seed: 7);

        var single = DeflatedSharpe.Evaluate(trades, trials: 1);
        var many = DeflatedSharpe.Evaluate(trades, trials: 100_000);

        Assert.True(single.Probability > many.Probability);
        Assert.True(single.Distinguishable, $"con una prova P = {single.Probability:P1}");
        Assert.False(many.Distinguishable, $"con 100.000 prove P = {many.Probability:P1}");
        Assert.True(many.NoiseAverageTrade > single.NoiseAverageTrade);
        Assert.Equal(0m, single.NoiseAverageTrade);
    }

    [Fact]
    public void ALargeEdgeSurvivesManyTrials()
    {
        var trades = Trades(count: 2000, mean: 300m, spread: 1000m, seed: 11);
        var result = DeflatedSharpe.Evaluate(trades, trials: 100_000);
        Assert.True(result.Distinguishable, $"P = {result.Probability:P1}, soglia {result.NoiseAverageTrade:N0}");
    }

    [Fact]
    public void TooFewTradesGiveNoVerdict()
    {
        var result = DeflatedSharpe.Evaluate(Trades(count: 2, mean: 100m, spread: 10m, seed: 1), trials: 50);
        Assert.True(double.IsNaN(result.NoiseSharpe));
        Assert.Equal(0d, result.Probability);
    }

    /// <summary>
    /// Trade con netto alternato attorno alla media: il segno del rumore e' casuale ma riproducibile.
    /// Il netto passa dal prezzo di uscita, con un contratto e un punto di valore 1.
    /// </summary>
    private static List<TradingResult> Trades(int count, decimal mean, decimal spread, int seed)
    {
        var random = new Random(seed);
        var trades = new List<TradingResult>(count);
        for (var index = 0; index < count; index++)
        {
            var noise = (decimal)(random.NextDouble() * 2d - 1d) * spread * 1.732m;
            trades.Add(new TradingResult
            {
                Direction = SignalType.Buy,
                EntryPrice = 10_000m,
                ExitPrice = 10_000m + mean + noise,
                Quantity = 1m
            });
        }

        return trades;
    }
}
