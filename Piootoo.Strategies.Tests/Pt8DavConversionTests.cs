using System.Globalization;
using System.Reflection;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Piootoo.Strategies.PT5DAVStrategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La serie <c>PT8DAV_*</c> e' la consegna v5.1 (<c>piootoo-repository/run-engine-v3/</c>, 111
/// strategie) portata sui motori PT5DAV. Questi test impongono che la conversione sia <b>una a una e
/// verbatim</b>: ogni riga di <c>strategie_111.csv</c> ha esattamente una classe, e la classe ha la
/// stessa configurazione che quella riga da' al contenitore di ricerca dello stesso motore.
///
/// <para><b>Perche' passare dal contenitore.</b> Il contenitore (<c>RC5_*</c>) traduce una colonna
/// alla volta con il nome della consegna, il generatore (<c>tools/pt8dav/gen_pt8dav.py</c>) scrive i
/// campi della classe: sono due traduzioni indipendenti della stessa riga, e se coincidono campo per
/// campo un refuso in una delle due non passa. E' lo stesso controllo delle 218 PT5DAV
/// (<see cref="Pt5DavResearchContainerTests"/>), sul CSV della v5.1.</para>
/// </summary>
public sealed class Pt8DavConversionTests
{
    private const int DeliveredStrategies = 111;

    private static readonly Dictionary<string, Type> ContainerByEngine = new(StringComparer.Ordinal)
    {
        ["TF_M"] = typeof(RC5_TFM), ["PC"] = typeof(RC5_PCH),
        ["BO"] = typeof(RC5_SBO), ["BO_S"] = typeof(RC5_BOS), ["VBO"] = typeof(RC5_VBO),
        ["LF"] = typeof(RC5_LFD), ["LF_HL"] = typeof(RC5_LFH), ["RHL"] = typeof(RC5_RHL),
        ["RBB_M"] = typeof(RC5_RBM), ["RBB_U"] = typeof(RC5_RBU), ["BIAS"] = typeof(RC5_BIA),
        ["BIAS_RT"] = typeof(RC5_BRT), ["BIAS_BO"] = typeof(RC5_BBO), ["MAC"] = typeof(RC5_MAC),
        ["BIASW"] = typeof(RC5_BSW)
    };

    private static readonly Dictionary<string, int> TimeframeMinutes = new(StringComparer.Ordinal)
    {
        ["15m"] = 15, ["30m"] = 30, ["1h"] = 60, ["4h"] = 240
    };

    private static IEnumerable<Type> Types() =>
        typeof(Pt5DavEngineBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.Name.StartsWith("PT8DAV_", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    public static IEnumerable<object[]> Classes() => Types().Select(type => new object[] { type });

    [Fact]
    public void EveryDeliveredRowHasExactlyOneClass()
    {
        var codes = Types()
            .Select(type => ((Pt5DavEngineBase)Activator.CreateInstance(type)!).ResearchCode)
            .ToList();

        Assert.Equal(DeliveredStrategies, Rows.Value.Count);
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            Rows.Value.Keys.OrderBy(code => code, StringComparer.Ordinal),
            codes.OrderBy(code => code, StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Classes))]
    public void TheClassSitsOnTheSymbolTimeframeAndEngineOfItsRow(Type type)
    {
        var strategy = (Pt5DavEngineBase)Activator.CreateInstance(type)!;
        var row = Rows.Value[strategy.ResearchCode];

        Assert.Equal("@" + row["simbolo"], strategy.Symbol);
        // La barra su cui il motore ragiona e' quella della riga. La serie ricevuta coincide, salvo
        // per le FDAX a 4 ore, che ricevono l'oraria e piegano le barre in motore.
        Assert.Equal(TimeframeMinutes[row["timeframe"]], strategy.BarMinutes);
        var folds = row["simbolo"] == "FDAX" && row["timeframe"] == "4h";
        Assert.Equal(folds ? 60 : strategy.BarMinutes, strategy.TimeframeMinutes);
        // Il contenitore e la classe derivano dallo stesso motore astratto.
        Assert.Equal(ContainerByEngine[row["motore"]].BaseType, type.BaseType);
    }

    [Theory]
    [MemberData(nameof(Classes))]
    public void TheResearchRowGivesTheContainerTheSameConfigurationAsTheClass(Type type)
    {
        var strategy = (Pt5DavEngineBase)Activator.CreateInstance(type)!;
        var row = Rows.Value[strategy.ResearchCode];

        var container = (Pt5DavEngineBase)Activator.CreateInstance(ContainerByEngine[row["motore"]])!;
        container.Initialize(Parameters(row, strategy));

        Assert.Equal(strategy.TradingWindow, container.TradingWindow);
        Assert.Equal(strategy.Holding, container.Holding);
        Assert.Equal(AnchorOf(strategy), AnchorOf(container));

        foreach (var field in ConfigurationFields(container.GetType()))
            Assert.True(Equals(field.GetValue(strategy), field.GetValue(container)),
                $"{type.Name}: {field.DeclaringType!.Name}.{field.Name} vale {field.GetValue(strategy)} nella classe " +
                $"e {field.GetValue(container)} nel contenitore");
    }

    // ------------------------------------------------------------------ riga della consegna

    private static readonly Lazy<string[]> Header = new(() => File.ReadLines(CsvPath()).First().Split(','));

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Rows = new(LoadRows);

    /// <summary>
    /// Le colonne dei parametri stanno fra <c>intraday_only</c> e la prima metrica (<c>s_n</c>): nel
    /// CSV della v5.1 le leve della MAC (<c>fast</c> … <c>daily_factor</c>) vengono <b>dopo</b>
    /// <c>entrata_inizio</c>, dove nella v5.0 la lista finiva.
    /// </summary>
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
            if (column == "s_n")
                break;
            if (column == "intraday_only")
                inside = true;
            if (inside && row[column] != string.Empty)
                parameters[column] = decimal.Parse(row[column], CultureInfo.InvariantCulture);
        }

        return parameters;
    }

    private static Dictionary<string, Dictionary<string, string>> LoadRows()
    {
        var header = Header.Value;
        return File.ReadLines(CsvPath())
            .Skip(1)
            .Where(line => line.Length > 0)
            .Select(line => line.Split(','))
            .ToDictionary(
                cells => cells[0],
                cells => header.Select((column, index) => (column, value: cells[index]))
                    .ToDictionary(pair => pair.column, pair => pair.value, StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    private static string CsvPath()
    {
        var root = FindSolutionRoot(AppContext.BaseDirectory)
                   ?? Environment.GetEnvironmentVariable("PIOOTOO_ROOT")
                   ?? throw new DirectoryNotFoundException("PiootooApp.sln non trovata (o PIOOTOO_ROOT)");
        return Path.Combine(root, "piootoo-repository", "run-engine-v3", "strategie_111.csv");
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
