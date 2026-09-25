using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Gli spazi di ricerca PT5DAV (<see cref="Pt5DavSweepSpaces"/>) parlano la lingua dei contenitori
/// <c>RC5_*</c>: ogni valore di ogni griglia deve essere una leva che il contenitore legge.
///
/// <para><b>Perche' questi test esistono.</b> Una chiave che il contenitore rifiuta ferma la sweep a
/// meta' di una corsa di ore; una che accettasse senza leggerla varierebbe una leva inerte, e la
/// griglia girerebbe piu' volte la stessa strategia. Il contenitore rifiuta le colonne che non ha
/// (<c>Pt5DavResearchContainerTests</c>), quindi basta istanziarlo con ogni valore.</para>
/// </summary>
public sealed class Pt5DavSweepSpaceTests
{
    public static IEnumerable<object[]> Cells() =>
        from engine in Pt5DavSweepSpaces.Containers.Keys.OrderBy(key => key, StringComparer.Ordinal)
        from timeframe in new[] { 15, 30, 60, 240 }
        select new object[] { engine, timeframe };

    [Theory]
    [MemberData(nameof(Cells))]
    public void EveryValueOfEveryGridIsALeverTheContainerReads(string engine, int timeframe)
    {
        var space = Pt5DavSweepSpaces.For(engine, timeframe);
        var container = Pt5DavSweepSpaces.Containers[engine];

        Create(container, space.Defaults);
        foreach (var parameter in space.Parameters)
        {
            foreach (var value in parameter.Values)
            {
                var parameters = new Dictionary<string, object>(space.Defaults, StringComparer.Ordinal)
                {
                    [parameter.Key] = value
                };
                Create(container, parameters);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void TheTimeframeOfTheCellReachesTheContainer(string engine, int timeframe)
    {
        var space = Pt5DavSweepSpaces.For(engine, timeframe);

        var strategy = Create(Pt5DavSweepSpaces.Containers[engine], space.Defaults);

        Assert.Equal(timeframe, strategy.TimeframeMinutes);
        Assert.Equal("@NQ", strategy.Symbol);
    }

    [Fact]
    public void EveryPhaseNamesOnlyDeclaredParameters()
    {
        // Il costruttore di SweepSpace lo verifica: qui basta costruirli tutti.
        foreach (var engine in Pt5DavSweepSpaces.Containers.Keys)
        foreach (var timeframe in new[] { 15, 30, 60, 240 })
            Assert.NotEmpty(Pt5DavSweepSpaces.For(engine, timeframe).Phases);
    }

    private static Piootoo.Shared.Interfaces.ITradingStrategy Create(string container, IReadOnlyDictionary<string, object> parameters)
    {
        StrategyFactory.LogStrategyCreation = false;
        return StrategyFactory.CreateStrategy(container, "@NQ", 0, new Dictionary<string, object>(parameters))
               ?? throw new InvalidOperationException($"{container} non creato");
    }
}
