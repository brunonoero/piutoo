using piootooapp.clientform.Shell.Controls;
using Piootoo.Core.Services;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il filtro per serie delle schermate che elencano strategie. E' una sola implementazione condivisa
/// da tre schermate — elenco strategie, masterfilter del workspace, tab Strategie del piano — quindi
/// una serie che non viene riconosciuta sparisce da tutte e tre insieme, e non come "filtro rotto"
/// ma come "quella strategia non c'e'".
///
/// <para>Il caso che questo test tiene fermo: un Id di serie <b>nuova</b> deve comparire nel proprio
/// gruppo e non finire in «altro», che nessuna combo elenca.</para>
/// </summary>
public sealed class StrategyFiltersTests
{
    [Theory]
    [InlineData("PT3B_FDAX_PCH_001_240", StrategyFilters.SeriesPt3b)]
    [InlineData("PT3B_NQ_PCH_001_15", StrategyFilters.SeriesPt3b)]
    [InlineData("PTS_GC_PCH_004_240", StrategyFilters.SeriesPts)]
    [InlineData("Easy_218_GC_60", "altro")]
    [InlineData("", "altro")]
    // La serie PT2 e' stata rimossa dal progetto il 22/09/2026. Un Id rimasto in un masterfilter
    // vecchio deve finire in «altro», che nessuna combo elenca: quell'Id non e' piu' eseguibile e
    // deve saltare all'occhio invece di nascondersi sotto una serie.
    [InlineData("PT2_NQ_PCH_001_240", "altro")]
    public void TheSeriesOfAStrategyIsThePrefixOfItsId(string strategyId, string expected) =>
        Assert.Equal(expected, StrategyFilters.SeriesOf(strategyId));

    /// <summary>
    /// Ogni serie del catalogo vero deve essere riconosciuta: una classe che finisce in «altro» e'
    /// invisibile a tutte e tre le schermate, perche' nessuna combo elenca quel valore.
    /// </summary>
    [Fact]
    public void EveryStrategyOfTheCatalogBelongsToAListedSeries()
    {
        var unlisted = StrategyFactory.GetRegisteredStrategies()
            .Where(strategy => StrategyFilters.SeriesOf(strategy.Id) == "altro")
            .Select(strategy => strategy.Id)
            .ToList();

        Assert.True(unlisted.Count == 0,
            "Strategie che non appartengono a nessuna serie elencata dalle combo, quindi invisibili " +
            "nelle schermate: " + string.Join(", ", unlisted) +
            ". Aggiungi la serie a StrategyFilters.");
    }

    /// <summary>
    /// Il filtro per serie e quello per simbolo si compongono, e <c>null</c> da un lato vuol dire
    /// nessun filtro da quel lato.
    /// </summary>
    [Fact]
    public void TheSeriesAndSymbolFiltersCompose()
    {
        const string id = "PT3B_FDAX_PCH_002_240";

        Assert.True(StrategyFilters.Passes(id, "@FDAX", null, null));
        Assert.True(StrategyFilters.Passes(id, "@FDAX", "@FDAX", StrategyFilters.SeriesPt3b));
        Assert.False(StrategyFilters.Passes(id, "@FDAX", "@NQ", StrategyFilters.SeriesPt3b));
        Assert.False(StrategyFilters.Passes(id, "@FDAX", null, StrategyFilters.SeriesPts));
    }
}
