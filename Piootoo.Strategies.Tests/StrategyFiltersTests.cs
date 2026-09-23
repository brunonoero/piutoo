using piootooapp.clientform.Shell.Controls;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I filtri condivisi dalle schermate che elencano strategie — elenco strategie, masterfilter del
/// workspace, tab Strategie del piano. Il filtro «inizia con» e' un prefisso libero dell'Id di classe
/// e sostituisce dal 23/09/2026 la combo «Serie», che aveva un elenco chiuso di prefissi.
/// </summary>
public sealed class StrategyFiltersTests
{
    [Theory]
    [InlineData("PT3B_FDAX_PCH_001_240", "PT3", true)]
    [InlineData("PT3B_FDAX_PCH_001_240", "pt3b_fdax", true)]
    [InlineData("PTS_GC_PCH_004_240", "PT3", false)]
    [InlineData("Easy_218_GC_60", "PT", false)]
    [InlineData("PTS_GC_PCH_004_240", null, true)]
    [InlineData("", null, true)]
    [InlineData("", "PT", false)]
    public void ThePrefixFilterMatchesTheStartOfTheId(string strategyId, string? prefix, bool expected) =>
        Assert.Equal(expected, StrategyFilters.StartsWithPrefix(strategyId, prefix));

    /// <summary>
    /// Il prefisso e il simbolo si compongono, e <c>null</c> da un lato vuol dire nessun filtro da
    /// quel lato.
    /// </summary>
    [Fact]
    public void ThePrefixAndSymbolFiltersCompose()
    {
        const string id = "PT3B_FDAX_PCH_002_240";

        Assert.True(StrategyFilters.Passes(id, "@FDAX", null, null));
        Assert.True(StrategyFilters.Passes(id, "@FDAX", "@FDAX", "PT3"));
        Assert.False(StrategyFilters.Passes(id, "@FDAX", "@NQ", "PT3"));
        Assert.False(StrategyFilters.Passes(id, "@FDAX", null, "PTS"));
    }
}
