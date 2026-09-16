using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PiutooStrategies;
using Piootoo.Strategies.PT2Strategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

public class PtsStrategyRegistrationTests
{
    [Fact]
    public void PtsTfm001_IsRegisteredWithExpectedMetadata()
    {
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(),
            strategy => strategy.Id == nameof(PTS_NQ_TFM_001_60));

        Assert.Equal("PTS_NQ_TFM_001_60", definition.Name);
        Assert.Equal("@NQ", definition.Symbol);
        Assert.Equal(60, definition.TimeframeMinutes);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public void PtsTfm001_CanBeCreatedByCatalogId()
    {
        var strategy = StrategyFactory.CreateStrategy(nameof(PTS_NQ_TFM_001_60), "@NQ", 60);

        Assert.NotNull(strategy);
        Assert.IsType<PTS_NQ_TFM_001_60>(strategy);
        Assert.False(strategy.IsPositionCloseDependent);
    }

    [Theory]
    [InlineData(nameof(PTS_NQ_PCH_001_15))]
    [InlineData(nameof(PTS_NQ_PCH_002_15))]
    public void PcPtsStrategies_AreRegisteredWithExpectedMetadata(string id)
    {
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(),
            strategy => strategy.Id == id);

        Assert.Equal("@NQ", definition.Symbol);
        Assert.Equal(15, definition.TimeframeMinutes);
        Assert.True(definition.IsActive);
    }

    /// <summary>
    /// Le quattro classi della serie PT2 (<c>run-engine-v2/DOSSIER_PANIERE_001.md</c>) stanno in un
    /// namespace proprio: il catalogo le deve trovare come le PTS, con Id e Name coincidenti, il
    /// simbolo e il timeframe del dossier e la tenuta che il dossier dichiara.
    /// </summary>
    [Theory]
    [InlineData(nameof(PT2_FDAX_BSW_001_60), "@FDAX", 60, false)]
    [InlineData(nameof(PT2_NQ_PCH_001_240), "@NQ", 240, false)]
    [InlineData(nameof(PT2_FDAX_PCH_001_240), "@FDAX", 240, true)]
    [InlineData(nameof(PT2_NQ_PCH_002_30), "@NQ", 30, false)]
    public void Pt2Strategies_AreRegisteredWithExpectedMetadata(
        string id, string symbol, int timeframeMinutes, bool intraday)
    {
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(),
            strategy => strategy.Id == id);

        Assert.Equal(id, definition.Name);
        Assert.Equal(symbol, definition.Symbol);
        Assert.Equal(timeframeMinutes, definition.TimeframeMinutes);
        Assert.True(definition.IsActive);
        Assert.Equal(intraday ? StrategyHolding.Intraday : StrategyHolding.Multiday, definition.Holding);

        var strategy = StrategyFactory.CreateStrategy(id, symbol, timeframeMinutes);
        Assert.NotNull(strategy);
        Assert.Equal(id, strategy!.GetType().Name);
        Assert.False(strategy.IsPositionCloseDependent);
    }
}
