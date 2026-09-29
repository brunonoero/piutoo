using Piootoo.Core.Optimization.Sweep;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La probabilita' di sovra-adattamento per validazione incrociata combinatoria: una griglia di solo
/// rumore sta attorno a 0,5, una griglia con una configurazione vera va verso zero.
/// </summary>
public sealed class BacktestOverfittingTests
{
    /// <summary>
    /// Cinquanta configurazioni senza edge: la migliore in campione e' fuori campione una a caso, sopra
    /// o sotto la mediana con la stessa frequenza.
    /// </summary>
    [Fact]
    public void PureNoiseGridIsAsLikelyToOverfitAsNot()
    {
        var grid = Grid(configurations: 50, months: 144, seed: 3, edgeMean: 0d);
        var result = BacktestOverfitting.Evaluate(grid)!;

        Assert.Equal(16, result.Blocks);
        Assert.Equal(12_870, result.Splits);
        Assert.InRange(result.Probability, 0.3, 0.7);
        Assert.InRange(result.MedianOutOfSamplePercentile, 0.3, 0.7);
    }

    /// <summary>
    /// Una configurazione con un edge vero fra quarantanove di rumore: vince quasi sempre in campione e
    /// resta in testa fuori, quindi la selezione non sovra-adatta.
    /// </summary>
    [Fact]
    public void ARealEdgeKeepsTheSelectionHonest()
    {
        var grid = Grid(configurations: 50, months: 144, seed: 5, edgeMean: 600d);
        var result = BacktestOverfitting.Evaluate(grid)!;

        Assert.True(result.Probability < 0.05, $"PBO = {result.Probability:P1}");
        Assert.True(result.MedianOutOfSamplePercentile > 0.9);
        Assert.True(result.LossProbability < 0.05);
    }

    [Fact]
    public void FewPeriodsReduceTheBlocksAndTooFewGiveNoVerdict()
    {
        var shortGrid = Grid(configurations: 10, months: 30, seed: 1, edgeMean: 0d);
        Assert.Equal(10, BacktestOverfitting.Evaluate(shortGrid)!.Blocks);

        var tooShort = Grid(configurations: 10, months: 11, seed: 1, edgeMean: 0d);
        Assert.Null(BacktestOverfitting.Evaluate(tooShort));
        Assert.Null(BacktestOverfitting.Evaluate(Grid(configurations: 1, months: 144, seed: 1, edgeMean: 0d)));
    }

    [Fact]
    public void RowsOfDifferentLengthAreRejected() =>
        Assert.Throws<ArgumentException>(() => BacktestOverfitting.Evaluate([new double[48], new double[47]]));

    /// <summary>Ogni trade va nel mese in cui si chiude; i mesi vuoti restano a zero, quelli fuori periodo si scartano.</summary>
    [Fact]
    public void MonthlyProfitBucketsTradesByExitMonth()
    {
        var from = new DateTime(2022, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2022, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        var trades = new[]
        {
            Trade(new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc), 999m),
            Trade(new DateTime(2022, 1, 20, 0, 0, 0, DateTimeKind.Utc), 100m),
            Trade(new DateTime(2022, 1, 31, 23, 0, 0, DateTimeKind.Utc), -30m),
            Trade(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), 50m),
            Trade(new DateTime(2022, 4, 2, 0, 0, 0, DateTimeKind.Utc), 7m)
        };

        var monthly = BacktestOverfitting.MonthlyProfit(trades, from, to);

        Assert.Equal([70d, 0d, 50d, 7d], monthly);
        Assert.Equal(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc), BacktestOverfitting.MonthStart(from, 2));
    }

    /// <summary>
    /// Mesi di P&amp;L gaussiano con deviazione standard 1.000; la configurazione 0 ha media
    /// <paramref name="edgeMean"/>, le altre zero.
    /// </summary>
    private static List<double[]> Grid(int configurations, int months, int seed, double edgeMean)
    {
        var random = new Random(seed);
        var grid = new List<double[]>(configurations);
        for (var configuration = 0; configuration < configurations; configuration++)
        {
            var row = new double[months];
            for (var month = 0; month < months; month++)
            {
                var gaussian = Math.Sqrt(-2d * Math.Log(1d - random.NextDouble())) * Math.Cos(2d * Math.PI * random.NextDouble());
                row[month] = (configuration == 0 ? edgeMean : 0d) + 1000d * gaussian;
            }

            grid.Add(row);
        }

        return grid;
    }

    /// <summary>Un trade con netto <paramref name="net"/>: un contratto, punto di valore 1.</summary>
    private static TradingResult Trade(DateTime exitUtc, decimal net) => new()
    {
        Direction = SignalType.Buy,
        EntryPrice = 10_000m,
        ExitPrice = 10_000m + net,
        Quantity = 1m,
        EntryDate = exitUtc.AddHours(-1),
        ExitDate = exitUtc
    };
}
