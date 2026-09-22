using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PiutooStrategies;
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
    /// Le classi della serie PT3B stanno in un namespace proprio: il catalogo le deve trovare come
    /// le PTS, con Id e Name coincidenti, il simbolo e il timeframe dichiarati e la tenuta giusta.
    ///
    /// <para>Al posto della serie <b>PT2</b>, che questo test copriva fino al 22/09/2026 e che e'
    /// stata rimossa dal progetto: le sue quattro classi non avevano superato la validazione ai
    /// costi veri e restavano selezionabili nelle schermate.</para>
    /// </summary>
    [Theory]
    [InlineData(nameof(PT3BStrategies.PT3B_FDAX_PCH_001_240), "@FDAX", 240, true)]
    [InlineData(nameof(PT3BStrategies.PT3B_FDAX_PCH_002_240), "@FDAX", 240, true)]
    [InlineData(nameof(PT3BStrategies.PT3B_NQ_PCH_001_15), "@NQ", 15, true)]
    [InlineData(nameof(PT3BStrategies.PT3B_CL_PCH_001_30), "@CL", 30, true)]
    public void Pt3bStrategies_AreRegisteredWithExpectedMetadata(
        string id, string symbol, int timeframeMinutes, bool intraday)
    {
        // Con i contenitori: due delle quattro lo sono, e qui si verifica che il catalogo le
        // registri con i metadati giusti, non che siano selezionabili.
        var definition = Assert.Single(
            StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true),
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
