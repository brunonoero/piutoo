using Piootoo.Core.Optimization.Sweep;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Lo spazio di ricerca del trend following unmirrored e la restrizione a una regione
/// (<see cref="SweepSpace.Fix"/>). Sono le due cose scritte il 23/09/2026 per i due lavori di quel
/// giorno: cercare la 003 su FDAX dentro la regione che la griglia grossa ha indicato, e riprovare
/// il trend following cambiando motore, timeframe e filtri insieme.
/// </summary>
public sealed class SweepSpaceTests
{
    /// <summary>
    /// Le fasi di <c>tf_unmirrored.py</c> nell'ordine del metodo, piu' l'uscita di sessione dopo gli
    /// orari e prima dello stop come nel Price Channel. I pattern sono la seconda e la terza fase,
    /// non un raffinamento: sulle TF sono il filtro principale.
    /// </summary>
    [Fact]
    public void TheTrendFollowingSpaceFollowsThePythonPhasesAndDeclaresTheExitHour()
    {
        var space = SweepSpaces.TrendFollowingUnmirrored(15);

        Assert.Equal("TFU", space.Engine);
        Assert.Equal(
            ["uscita base", "pattern long", "pattern short", "orari e giorni", "uscita di sessione", "stop e target"],
            space.Phases.Select(phase => phase.Name).ToArray());

        var exit = Assert.Single(space.Parameters, p => p.Key == "ExitHour");
        Assert.Equal(-1, Convert.ToInt32(exit.Values[0]));
        Assert.True(space.Phases.Single(p => p.Keys.Contains("ExitHour")).RequiresAccurateClock);

        // Le sentinelle dei quattro gate — 152 sempre vero sui Yes, 153 sempre falso sui No — sono il
        // primo valore di ogni lista, e i due Yes portano in coda anche il 153: e' cosi' che una TF a
        // senso unico esiste senza un parametro Direction. I No hanno il 153 solo come sentinella.
        Assert.Equal(152, space.PatternSentinels["PtnLyYes"]);
        Assert.Equal(153, space.PatternSentinels["PtnLyNo"]);
        Assert.Equal(152, space.ByKey["PtnLyYes"].Values[0]);
        Assert.Equal(153, space.ByKey["PtnLyNo"].Values[0]);
        Assert.Equal(153, space.ByKey["PtnLyYes"].Values[^1]);
        Assert.Equal(153, space.ByKey["PtnSyYes"].Values[^1]);
        Assert.Equal(1 + 151 + 1, space.ByKey["PtnLyYes"].Values.Count);
        Assert.Equal(1 + 151, space.ByKey["PtnLyNo"].Values.Count);

        // get_default_params(): senza stop e target il TF intraday perde sempre.
        Assert.Equal(1000, space.Defaults["StopLoss"]);
        Assert.Equal(3000, space.Defaults["TakeProfit"]);
        Assert.Equal(0, space.Defaults["MaxBars"]);
    }

    /// <summary>Su daily ne' l'uscita base ne' l'ora di uscita: il motore di ricerca non le applica.</summary>
    [Fact]
    public void TheDailyTrendFollowingSpaceHasNeitherIntradayOnlyNorExitHour()
    {
        var space = SweepSpaces.TrendFollowingUnmirrored(1440);

        Assert.DoesNotContain(space.Parameters, p => p.Key == "IntradayOnly");
        Assert.DoesNotContain(space.Parameters, p => p.Key == "ExitHour");
        Assert.Equal(4, space.Phases.Count);
    }

    /// <summary>
    /// Fissare un parametro lo riduce a un valore, che diventa anche il default; il resto dello
    /// spazio e le fasi restano quelli. E' cosi' che la ricerca della 003 resta dentro la regione
    /// che la griglia grossa ha indicato.
    /// </summary>
    [Fact]
    public void FixingParametersShrinksTheirGridToOneValueAndMakesItTheDefault()
    {
        var space = SweepSpaces.PriceChannel(240);
        var fixedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ChannelBars"] = 20, ["Direction"] = 1, ["ExitHour"] = 21
        };

        var regione = space.Fix(fixedValues);

        Assert.Equal([20], regione.ByKey["ChannelBars"].Values.Cast<int>());
        Assert.Equal([1], regione.ByKey["Direction"].Values.Cast<int>());
        Assert.Equal([21], regione.ByKey["ExitHour"].Values.Cast<int>());
        Assert.Equal(20, regione.Defaults["ChannelBars"]);
        Assert.Equal(1, regione.Defaults["Direction"]);
        Assert.Equal(21, regione.Defaults["ExitHour"]);

        // Il resto non si tocca: stesse fasi, stesse griglie, stessi default.
        Assert.Equal(space.Phases, regione.Phases);
        Assert.Equal(space.ByKey["StopLoss"].Values, regione.ByKey["StopLoss"].Values);
        Assert.Equal(space.Defaults["StopLoss"], regione.Defaults["StopLoss"]);

        // La fase del trigger si riduce alle sole leve libere: offset e intraday.
        var trigger = regione.Phases.Single(phase => phase.Name == "trigger");
        Assert.Equal(4 * 2, regione.CombinationCount(trigger));
        var uscita = regione.Phases.Single(phase => phase.Keys.Contains("ExitHour"));
        Assert.Equal(1, regione.CombinationCount(uscita));

        // L'originale resta intatto: Fix restituisce uno spazio nuovo.
        Assert.Equal(10, space.ByKey["ChannelBars"].Values.Count);
    }

    /// <summary>Fissare un parametro che lo spazio non ha e' un errore di battitura, non un no-op.</summary>
    [Fact]
    public void FixingAnUnknownParameterIsRejected()
    {
        var space = SweepSpaces.PriceChannel(240);

        var errore = Assert.Throws<ArgumentException>(() =>
            space.Fix(new Dictionary<string, object> { ["Channel"] = 20 }));

        Assert.Contains("Channel", errore.Message);
    }
}
