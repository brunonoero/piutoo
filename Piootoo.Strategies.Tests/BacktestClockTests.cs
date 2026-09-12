using Piootoo.Core.Services;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'orologio del loop di backtesting è il minuto e non si sceglie: compare-0040 (12/09/2026) ha
/// misurato che con l'orologio al timeframe delle strategie i riempimenti non sono confrontabili
/// con il conto. Il test fissa il numero e il fatto che divida ogni timeframe del catalogo, che è
/// la condizione per cui nessuna strategia può essere saltata dal tick del loop.
/// </summary>
public sealed class BacktestClockTests
{
    [Fact]
    public void TheClockIsOneMinute()
        => Assert.Equal(1, BacktestClock.TimeframeMinutes);

    [Fact]
    public void TheClockDividesEveryRegisteredTimeframe()
    {
        var notDivisible = StrategyFactory.GetRegisteredStrategies()
            .Where(definition => definition.TimeframeMinutes % BacktestClock.TimeframeMinutes != 0)
            .Select(definition => definition.Id)
            .ToList();

        Assert.True(notDivisible.Count == 0,
            "Strategie che il tick del loop salterebbe: " + string.Join(", ", notDivisible));
    }
}
