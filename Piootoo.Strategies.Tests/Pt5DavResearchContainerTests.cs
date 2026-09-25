using System.Globalization;
using System.Reflection;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Piootoo.Strategies.PT5DAVStrategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I contenitori di ricerca PT5DAV (<c>RC5_*</c>) ricevono le leve con il nome della colonna di
/// <c>strategie_224.csv</c>: una riga della consegna data a un contenitore deve produrre <b>la stessa
/// configurazione</b> della classe generata da quella riga.
///
/// <para><b>Perche' questi test esistono.</b> La sweep cerca sui contenitori e la classe che ne esce si
/// scrive con il generatore: se le due traduzioni della stessa colonna divergessero, la finalista
/// operata non sarebbe quella misurata, e nessun numero lo direbbe. Il confronto e' campo per campo
/// su tutta la gerarchia dei motori, per le 218 classi.</para>
/// </summary>
public sealed class Pt5DavResearchContainerTests
{
    private static readonly Dictionary<string, Type> ContainerByEngine = new(StringComparer.Ordinal)
    {
        ["TF_M"] = typeof(RC5_TFM), ["TF_U"] = typeof(RC5_TFU), ["PC"] = typeof(RC5_PCH),
        ["BO"] = typeof(RC5_SBO), ["BO_S"] = typeof(RC5_BOS), ["VBO"] = typeof(RC5_VBO),
        ["LF"] = typeof(RC5_LFD), ["LF_HL"] = typeof(RC5_LFH), ["RHL"] = typeof(RC5_RHL),
        ["RBB_M"] = typeof(RC5_RBM), ["RBB_U"] = typeof(RC5_RBU), ["BIAS"] = typeof(RC5_BIA),
        ["BIAS_RT"] = typeof(RC5_BRT), ["BIAS_BO"] = typeof(RC5_BBO), ["MAC"] = typeof(RC5_MAC),
        ["BIASW"] = typeof(RC5_BSW)
    };

    public static IEnumerable<object[]> Classes() =>
        typeof(Pt5DavEngineBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.Name.StartsWith("PT5DAV_", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(Classes))]
    public void TheResearchRowGivesTheContainerTheSameConfigurationAsTheClass(Type type)
    {
        var strategy = (Pt5DavEngineBase)Activator.CreateInstance(type)!;
        var row = Rows.Value[strategy.ResearchCode];

        var container = (Pt5DavEngineBase)Activator.CreateInstance(ContainerByEngine[row["motore"]])!;
        container.Initialize(Parameters(row, strategy));

        Assert.Equal(strategy.Symbol, container.Symbol);
        Assert.Equal(strategy.TimeframeMinutes, container.TimeframeMinutes);
        Assert.Equal(strategy.TradingWindow, container.TradingWindow);
        Assert.Equal(strategy.Holding, container.Holding);
        Assert.Equal(AnchorOf(strategy), AnchorOf(container));

        foreach (var field in ConfigurationFields(container.GetType()))
            Assert.True(Equals(field.GetValue(strategy), field.GetValue(container)),
                $"{type.Name}: {field.DeclaringType!.Name}.{field.Name} vale {field.GetValue(strategy)} nella classe " +
                $"e {field.GetValue(container)} nel contenitore");
    }

    [Fact]
    public void AColumnTheEngineDoesNotReadStopsTheConfiguration()
    {
        var container = new RC5_RHL();

        var error = Assert.Throws<ArgumentException>(() =>
            container.Initialize(new Dictionary<string, object> { ["channel_len"] = 20 }));
        Assert.Contains("channel_len", error.Message);
    }

    [Fact]
    public void AnOffColumnTheEngineDoesNotReadIsIgnored()
    {
        // La MAC non ha giorno escluso: -1 e' "nessuno", e la riga della consegna lo porta.
        new RC5_MAC().Initialize(new Dictionary<string, object> { ["skip_day"] = -1 });
    }

    [Fact]
    public void TheContainerIsARealResearchContainerAndDefaultsToNasdaq()
    {
        var container = new RC5_PCH();

        Assert.True(container.IsResearchContainer);
        Assert.Equal("@NQ", container.Symbol);
    }

    // ------------------------------------------------------------------ riga della consegna

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Rows = new(LoadRows);

    /// <summary>Le colonne dei parametri stanno fra <c>intraday_only</c> ed <c>entrata_inizio</c>.</summary>
    private static Dictionary<string, object> Parameters(Dictionary<string, string> row, Pt5DavEngineBase strategy)
    {
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Symbol"] = strategy.Symbol,
            ["TimeframeMinutes"] = strategy.TimeframeMinutes
        };

        var inside = false;
        foreach (var column in Header.Value)
        {
            if (column == "intraday_only")
                inside = true;
            if (inside && row[column] != string.Empty)
                parameters[column] = decimal.Parse(row[column], CultureInfo.InvariantCulture);
            if (column == "entrata_inizio")
                break;
        }

        return parameters;
    }

    private static readonly Lazy<string[]> Header = new(() => File.ReadLines(CsvPath()).First().Split(','));

    private static Dictionary<string, Dictionary<string, string>> LoadRows()
    {
        var header = Header.Value;
        return File.ReadLines(CsvPath())
            .Skip(1)
            .Select(line => line.Split(','))
            .ToDictionary(
                cells => cells[0],
                cells => header.Select((column, index) => (column, value: cells[index]))
                    .ToDictionary(pair => pair.column, pair => pair.value, StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    private static string CsvPath()
    {
        // Dalla cartella dei binari, o da quella di lavoro quando i test girano da una copia compilata
        // altrove (una coda di ricerca puo' tenere bloccata la cartella dei test).
        var root = FindSolutionRoot(AppContext.BaseDirectory)
                   ?? Environment.GetEnvironmentVariable("PIOOTOO_ROOT")
                   ?? throw new DirectoryNotFoundException("PiootooApp.sln non trovata (o PIOOTOO_ROOT)");
        return Path.Combine(root, "piootoo-repository", "PT5DAV", "strategie_224.csv");
    }

    private static string? FindSolutionRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
            directory = directory.Parent;
        return directory?.FullName;
    }

    // ------------------------------------------------------------------ confronto

    /// <summary>
    /// I campi di configurazione di tutta la gerarchia fino a <see cref="EasyEngineBase"/>: quelli con
    /// il nome che comincia per <c>_</c> sono stato o cache, e restano fuori.
    /// </summary>
    private static IEnumerable<FieldInfo> ConfigurationFields(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!field.Name.StartsWith('_') && !field.Name.Contains('<'))
                    yield return field;
            }

            if (current == typeof(EasyEngineBase))
                yield break;
        }
    }

    private static object? AnchorOf(EasyEngineBase engine) =>
        typeof(EasyEngineBase).GetField("_sessionAnchorOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(engine);
}
