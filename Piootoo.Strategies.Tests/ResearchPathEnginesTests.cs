using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Ogni valore di ogni passo del percorso arriva al contenitore senza errori: un contenitore che non
/// ha la leva ferma la configurazione, e qui lo si scopre prima di un'ora di run invece che a meta'.
/// </summary>
public sealed class ResearchPathEnginesTests
{
    public static TheoryData<string> PathEngines
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var key in ResearchPathEngines.Keys) data.Add(key);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PathEngines))]
    public void EveryStepValueIsAcceptedByTheContainer(string engine)
    {
        var definition = ResearchPathEngines.Definition(engine, "@FDAX", 240);
        var type = typeof(RC_PCH).Assembly.GetType($"{typeof(RC_PCH).Namespace}.{ResearchPathEngines.Container(engine)}")!;

        Create(type, new Dictionary<string, object>(definition.Base));
        foreach (var step in definition.Steps)
        {
            foreach (var value in step.Values)
            {
                var parameters = new Dictionary<string, object>(definition.Base, StringComparer.OrdinalIgnoreCase);
                if (value is IReadOnlyDictionary<string, object> levers)
                    foreach (var (key, lever) in levers) parameters[key] = lever;
                else
                    parameters[step.Key] = value;
                var strategy = Create(type, step.Adjust is null ? parameters : new Dictionary<string, object>(step.Adjust(value, parameters)));
                Assert.Equal("@FDAX", strategy.Symbol);
            }
        }
    }

    /// <summary>Il percorso tiene l'ordine della famiglia: lo stop viene prima dei pattern, il target dopo i trend following.</summary>
    [Fact]
    public void TheTrendFollowingPathSetsTheStopBeforeThePatternsAndTheTargetAtTheEnd()
    {
        var steps = ResearchPathEngines.Definition("PCH", "@FDAX", 240).Steps.Select(step => step.Key).ToList();

        Assert.True(steps.IndexOf("StopAtr") < steps.IndexOf("PtnNeutYes"));
        Assert.True(steps.IndexOf("PtnNeutYes") < steps.IndexOf("TargetAtr"));
        Assert.Equal("StopAtr", steps[^1]);
    }

    private static ITradingStrategy Create(Type type, IDictionary<string, object> parameters)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;
        type.GetMethod("Initialize")!.Invoke(strategy, [new Dictionary<string, object>(parameters)]);
        return strategy;
    }
}
