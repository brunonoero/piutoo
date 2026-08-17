using Piootoo.Core.Services;
using Piootoo.Strategies.Easy;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PiutooStrategies;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Registro operativo strategia → motore. Se una migrazione cambia classe base, il test
/// fallisce e obbliga ad aggiornare anche <c>docs/domini/motori-strategie.md</c>.
/// </summary>
public sealed class EngineCatalogMigrationTests
{
    public static TheoryData<string, Type, MigrationStatus> Catalog { get; } = new()
    {
        // BIAS
        { "Easy_218_GC_60", typeof(BiasBarCountEngine), MigrationStatus.Migrated },
        { "Easy_244_FDAX_15", typeof(BiasBarCountEngine), MigrationStatus.Hybrid },
        { "Easy_261_GC_60", typeof(BiasBarCountEngine), MigrationStatus.Migrated },
        { "Easy_460_GC_30", typeof(BiasBarCountEngine), MigrationStatus.Migrated },
        { "Easy_872_CL_15", typeof(BiasBarCountEngine), MigrationStatus.Hybrid },
        { "Easy_960_GC_60", typeof(BiasBarCountEngine), MigrationStatus.Migrated },

        // BIASW
        { "Easy_15_EC_5", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },
        { "Easy_99_CL_5", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },
        { "Easy_100_PL_5", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },
        { "Easy_196_EC_5", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },
        { "Easy_452_BP_15", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },
        { "Easy_545_HG_15", typeof(BiasWeeklyEngine), MigrationStatus.Migrated },

        // TF
        { "Easy_156_NQ_15", typeof(TfUnmirroredEngine), MigrationStatus.Migrated },
        { "PTS_NQ_TFM_001_60", typeof(TfMirroredEngine), MigrationStatus.Migrated },

        // RBB
        { "Easy_181_NQ_30", typeof(RbbUnmirroredEngine), MigrationStatus.Migrated },
        { "Easy_416_GC_30", typeof(RbbMirroredEngine), MigrationStatus.Migrated },

        // BO
        { "Easy_120_CL_15", typeof(SessionBreakoutEngine), MigrationStatus.Migrated },
        { "Easy_287_GC_5", typeof(SessionBreakoutEngine), MigrationStatus.Migrated },
        { "Easy_298_NQ_30", typeof(SessionBreakoutEngine), MigrationStatus.Migrated },

        // LF
        { "Easy_515_FDAX_15", typeof(LevelFaderEngine), MigrationStatus.Migrated },
        { "Easy_940_GC_15", typeof(LevelFaderEngine), MigrationStatus.Migrated },

        // PC
        { "Easy_336_GC_15", typeof(PriceChannelEngine), MigrationStatus.CloseDependent },
        { "Easy_361_FDAX_30", typeof(PriceChannelEngine), MigrationStatus.Migrated },
        { "PTS_NQ_PCH_001_15", typeof(PriceChannelEngine), MigrationStatus.Migrated },
        { "PTS_NQ_PCH_002_15", typeof(PriceChannelEngine), MigrationStatus.Migrated },

        // VBO
        { "Easy_342_NQ_15", typeof(VolatilityBreakoutEngine), MigrationStatus.Migrated },
        { "Easy_486_NQ_15", typeof(VolatilityBreakoutEngine), MigrationStatus.Hybrid },
        { "Easy_587_NQ_15", typeof(VolatilityBreakoutEngine), MigrationStatus.Hybrid },
        { "Easy_643_FDAX_60", typeof(VolatilityBreakoutEngine), MigrationStatus.Migrated },
        { "Easy_666_GC_5", typeof(VolatilityBreakoutEngine), MigrationStatus.Migrated },

        // MAC
        { "Easy_772_CL_60", typeof(MovingAverageCrossoverEngine), MigrationStatus.Migrated },

        // Trend Developer (non è uno dei 12 Unger catalogati; famiglia residua)
        { "Easy_102_FDAX_5", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_152_NQ_5", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_195_CL_15", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_246_CL_5", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_291_GC_15", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_303_GC_15", typeof(TrendDeveloperEngine), MigrationStatus.Hybrid },
        { "Easy_32_FDAX_15", typeof(TrendDeveloperEngine), MigrationStatus.Hybrid },
        { "Easy_653_GC_60", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_695_GC_5", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_796_NQ_15", typeof(TrendDeveloperEngine), MigrationStatus.Migrated },
        { "Easy_851_GC_5", typeof(TrendDeveloperEngine), MigrationStatus.Hybrid },

        // Ibridi su EasyEngineBase / Aroon
        { "Easy_123_CL_5", typeof(AroonCrossoverEngine), MigrationStatus.Hybrid },
        { "Easy_228_FDAX_30", typeof(EasyEngineBase), MigrationStatus.Hybrid },
        { "Easy_506_GC_30", typeof(EasyEngineBase), MigrationStatus.Hybrid },
        { "Easy_531_NQ_60", typeof(EasyEngineBase), MigrationStatus.Hybrid },
        { "Easy_956_NQ_15", typeof(EasyEngineBase), MigrationStatus.Hybrid },

        // Esclusa dal catalogo
        { "Easy_661_GC_30", typeof(StatelessEasyStrategyBase), MigrationStatus.CloseDependent },
    };

    [Theory]
    [MemberData(nameof(Catalog))]
    public void StrategyDerivesFromExpectedEngine(string id, Type expectedBase, MigrationStatus _)
    {
        var type = ResolveType(id);
        Assert.True(expectedBase.IsAssignableFrom(type),
            $"{id} dovrebbe derivare da {expectedBase.Name}, ma deriva da {type.BaseType?.Name}.");
    }

    [Fact]
    public void TwelveUngerEnginesExistAsConcreteBases()
    {
        Type[] engines =
        [
            typeof(TfMirroredEngine),
            typeof(TfUnmirroredEngine),
            typeof(BiasBarCountEngine),
            typeof(BiasWeeklyEngine),
            typeof(RbbMirroredEngine),
            typeof(RbbUnmirroredEngine),
            typeof(SessionBreakoutEngine),
            typeof(LevelFaderEngine),
            typeof(PriceChannelEngine),
            typeof(VolatilityBreakoutEngine),
            typeof(RhlEngine),
            typeof(MovingAverageCrossoverEngine),
        ];

        Assert.Equal(12, engines.Length);
        Assert.All(engines, engine => Assert.True(typeof(EasyEngineBase).IsAssignableFrom(engine)));
    }

    [Fact]
    public void CloseDependentStrategiesStayOutOfFactoryCatalog()
    {
        var ids = StrategyFactory.GetRegisteredStrategies()
            .Select(s => s.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Easy_661_GC_30", ids);
        Assert.DoesNotContain("Easy_32_FDAX_15", ids);
        Assert.DoesNotContain("Easy_851_GC_5", ids);
        Assert.DoesNotContain("Easy_336_GC_15", ids);
    }

    private static Type ResolveType(string id) => id switch
    {
        "PTS_NQ_TFM_001_60" => typeof(PTS_NQ_TFM_001_60),
        "PTS_NQ_PCH_001_15" => typeof(PTS_NQ_PCH_001_15),
        "PTS_NQ_PCH_002_15" => typeof(PTS_NQ_PCH_002_15),
        _ => Type.GetType($"Piootoo.Strategies.Easy.{id}, Piootoo.Strategies")
             ?? throw new InvalidOperationException($"Tipo non trovato per {id}")
    };

    public enum MigrationStatus
    {
        Migrated,
        Hybrid,
        CloseDependent
    }
}
