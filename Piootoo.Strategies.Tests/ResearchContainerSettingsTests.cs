using System.Reflection;
using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I contenitori generici della ricerca (<c>RC_*</c>) e l'impostazione delle leve per nome. Il
/// difetto che questi test chiudono e' quello che la skill <c>griglia-grossa</c> descrive: una leva
/// che la classe non legge e' inerte in silenzio, e la griglia misura tre volte la stessa cosa.
/// </summary>
public sealed class ResearchContainerSettingsTests
{
    public static TheoryData<string> MatrixEngines
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var key in CoarseGridMatrixTests.Engines.Keys) data.Add(key);
            return data;
        }
    }

    /// <summary>
    /// Ogni motore della matrice accetta tutte le chiavi che la griglia gli passa, prende simbolo e
    /// timeframe dai parametri, e la sua leva strutturale arriva davvero al membro del motore.
    /// </summary>
    [Theory]
    [MemberData(nameof(MatrixEngines))]
    public void EveryMatrixEngineTakesTheGridParametersAndItsLever(string key)
    {
        var spec = CoarseGridMatrixTests.Spec($"{key}|@Z|60|FTMO");
        var engine = CoarseGridMatrixTests.Engines[key];
        var lever = engine.Levers[^1];

        var parameters = new Dictionary<string, object>(spec.ExtraParameters!)
        {
            ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
            ["StartHour"] = -1, ["EndHour"] = -1, ["DvolMin"] = 0, ["SkipDay"] = -1,
            ["IntradayOnly"] = 0, ["OffsetTicks"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
            ["MaxBars"] = 18, ["ExitHour"] = -1,
            ["StopLoss"] = 0, ["TakeProfit"] = 0, ["StopAtr"] = 1.5m, ["TargetAtr"] = 3m,
            [spec.FirstLeverKey] = spec.FirstLeverDivisor == 1m ? lever : lever / spec.FirstLeverDivisor
        };
        if (spec.VariesDirection) parameters["Direction"] = 1;

        var strategy = Create(engine.Container, parameters);

        Assert.Equal("@Z", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(strategy.IsResearchContainer);
        Assert.Equal(1.5m, Read<decimal>(strategy, "StopAtrMultiplier"));

        var member = spec.FirstLeverKey switch
        {
            "BbLength" => "BollingerLength",
            "LevelOffsetTicks" => "LongLevelOffsetTicks",
            var name => name
        };
        Assert.Equal(
            System.Convert.ToDecimal(spec.FirstLeverDivisor == 1m ? lever : lever / spec.FirstLeverDivisor),
            System.Convert.ToDecimal(Read<object>(strategy, member)));
    }

    /// <summary>
    /// Con stop o target in ATR la finestra minima copre le sessioni dell'ATR. Fino al 24/09/2026 era
    /// di sei sessioni, l'ATR a quattordici non si calcolava mai e lo stop restava a zero in silenzio:
    /// la prima griglia in ATR dava lo stesso netto al centesimo per tre stop e tre target.
    /// </summary>
    [Theory]
    [MemberData(nameof(MatrixEngines))]
    public void AnAtrStopWidensTheWindowToTheAtrSessions(string key)
    {
        var engine = CoarseGridMatrixTests.Engines[key];
        var withoutAtr = Create(engine.Container, new Dictionary<string, object> { ["Symbol"] = "@Z", ["TimeframeMinutes"] = 60 });
        var withAtr = Create(engine.Container, new Dictionary<string, object>
        {
            ["Symbol"] = "@Z", ["TimeframeMinutes"] = 60, ["StopAtr"] = 1.5m
        });

        // 16 sessioni (14 chiuse, quella in corso e una di margine) a 24 barre orarie.
        Assert.True(withAtr.RequiredCandles >= 16 * 24,
            $"{engine.Container}: finestra di {withAtr.RequiredCandles} barre con lo stop in ATR, ne servono almeno {16 * 24}.");
        Assert.True(withoutAtr.RequiredCandles <= withAtr.RequiredCandles);
    }

    /// <summary>
    /// Ogni contenitore espone il <c>GenerateSignal</c> che la base cerca per riflessione. RBM, RBU e VBO
    /// non lo ereditano dal motore (hanno <c>EvaluateCore</c>): il 25/09/2026 le loro celle sono fallite
    /// al primo run della matrice.
    /// </summary>
    [Theory]
    [MemberData(nameof(MatrixEngines))]
    public void EveryContainerExposesGenerateSignal(string key)
    {
        var strategy = Create(CoarseGridMatrixTests.Engines[key].Container, new Dictionary<string, object>());

        Assert.NotNull(strategy.GetType().GetMethod(
            "GenerateSignal", BindingFlags.Instance | BindingFlags.Public,
            [typeof(Piootoo.Shared.Models.OhlcvData[]), typeof(DateTime)]));
    }

    /// <summary>Una leva che il motore non ha ferma la configurazione invece di restare inerte.</summary>
    [Fact]
    public void ALeverTheEngineDoesNotHaveIsRejected()
    {
        var error = Assert.Throws<TargetInvocationException>(() =>
            Create("RC_MAC", new Dictionary<string, object> { ["ChannelBars"] = 20 }));

        Assert.Contains("ChannelBars", error.InnerException!.Message);
    }

    /// <summary>Una chiave comune al valore spento si ignora su un motore che non la ha: spenta non cambia niente.</summary>
    [Fact]
    public void ACommonKeyAtItsOffValueIsIgnored()
    {
        var strategy = Create("RC_MAC", new Dictionary<string, object> { ["PtnNeutYes"] = 55, ["SkipDay"] = -1 });

        Assert.Equal("RC_MAC", strategy.Name);
    }

    private static ITradingStrategy Create(string container, Dictionary<string, object> parameters)
    {
        var type = typeof(RC_PCH).Assembly.GetType($"{typeof(RC_PCH).Namespace}.{container}")!;
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;
        type.GetMethod("Initialize")!.Invoke(strategy, [parameters]);
        return strategy;
    }

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }
}
