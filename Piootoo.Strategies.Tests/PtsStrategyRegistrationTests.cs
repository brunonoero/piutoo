using Piootoo.Core.Services;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

public class PtsStrategyRegistrationTests
{
    [Fact]
    public void Pts001_IsRegisteredWithExpectedMetadata()
    {
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(),
            strategy => strategy.Id == nameof(PTS_001_NQ_60));

        Assert.Equal("PTS_001_NQ_60", definition.Name);
        Assert.Equal("@NQ", definition.Symbol);
        Assert.Equal(60, definition.TimeframeMinutes);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public void Pts001_CanBeCreatedByCatalogId()
    {
        var strategy = StrategyFactory.CreateStrategy(nameof(PTS_001_NQ_60), "@NQ", 60);

        Assert.NotNull(strategy);
        Assert.IsType<PTS_001_NQ_60>(strategy);
        Assert.False(strategy.IsPositionCloseDependent);
    }

    [Theory]
    [InlineData(nameof(PTS_002_NQ_15))]
    [InlineData(nameof(PTS_003_NQ_15))]
    public void PcPtsStrategies_AreRegisteredWithExpectedMetadata(string id)
    {
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(),
            strategy => strategy.Id == id);

        Assert.Equal("@NQ", definition.Symbol);
        Assert.Equal(15, definition.TimeframeMinutes);
        Assert.True(definition.IsActive);
    }
}
