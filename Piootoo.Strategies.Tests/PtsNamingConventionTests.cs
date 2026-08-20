using System.Reflection;
using System.Text.RegularExpressions;
using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Impone la convenzione di nome delle strategie PTS: <c>PTS_[SYMBOL]_[ENG]_[NNN]_[TF]</c>.
/// </summary>
/// <remarks>
/// <para>Esempio: <c>PTS_NQ_PCH_001_15</c> — la prima PriceChannel su NQ a 15 minuti.</para>
///
/// <para>Il nome porta quattro informazioni perché sono le quattro che servono a leggere un
/// report senza aprire il codice: su cosa opera, con che logica, quale variante e su che
/// timeframe. La sigla motore sta prima del numero perché il numero riparte per coppia
/// (symbol, motore): senza la sigla, <c>001</c> sarebbe ambiguo.</para>
///
/// <para>Il test esiste perché il nome non è cosmetico. <c>Name</c> è lo
/// <c>StrategyCode</c> che finisce in <c>signals.json</c>, <c>trades.json</c>, nelle chiavi di
/// posizione e negli stati Titano: una strategia che sfugge alla convenzione non rompe la
/// compilazione, rompe i confronti fra artefatti mesi dopo. Vedi
/// <c>docs/domini/strategie-catalogo.md</c>.</para>
/// </remarks>
public sealed class PtsNamingConventionTests
{
    /// <summary>
    /// Sigla di tre lettere per ogni motore. Aggiungendo un motore si aggiunge qui la sigla:
    /// il test fallisce finché non è dichiarata, così la convenzione non si inventa da sola.
    /// </summary>
    private static readonly Dictionary<Type, string> EngineCodes = new()
    {
        [typeof(TfMirroredEngine)] = "TFM",
        [typeof(TfUnmirroredEngine)] = "TFU",
        [typeof(PriceChannelEngine)] = "PCH",
        [typeof(SessionBreakoutEngine)] = "SBO",
        [typeof(RbbMirroredEngine)] = "RBM",
        [typeof(RhlEngine)] = "RHL"
    };

    private static readonly Regex NamePattern = new(
        @"^PTS_(?<symbol>[A-Z0-9]+)_(?<engine>[A-Z]{3})_(?<number>\d{3})_(?<timeframe>\d+)$",
        RegexOptions.Compiled);

    public static TheoryData<Type> PtsStrategyTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in EnumeratePtsTypes())
            {
                data.Add(type);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PtsStrategyTypes))]
    public void NameMatchesConvention(Type type)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;

        var match = NamePattern.Match(strategy.Name);
        Assert.True(
            match.Success,
            $"{type.Name}: Name '{strategy.Name}' non rispetta PTS_[SYMBOL]_[ENG]_[NNN]_[TF].");

        // Id (nome della classe) e Name devono coincidere per le PTS: l'Id seleziona dal catalogo,
        // il Name viaggia nei dati di esecuzione, e tenerli allineati evita di dover passare da
        // StrategyCatalog.ResolveCodes per capire quale file corrisponde a un trade.
        Assert.Equal(type.Name, strategy.Name);
    }

    [Theory]
    [MemberData(nameof(PtsStrategyTypes))]
    public void NameAgreesWithSymbolTimeframeAndEngine(Type type)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;
        var match = NamePattern.Match(strategy.Name);
        Assert.True(match.Success, $"{type.Name}: Name '{strategy.Name}' non parsabile.");

        // Il Symbol della strategia è nella forma feed ('@NQ'): nel nome si usa senza prefisso.
        var expectedSymbol = strategy.Symbol.TrimStart('@').ToUpperInvariant();
        Assert.Equal(expectedSymbol, match.Groups["symbol"].Value);

        Assert.Equal(
            strategy.TimeframeMinutes.ToString(),
            match.Groups["timeframe"].Value);

        var engineType = ResolveEngineType(type);
        Assert.True(
            EngineCodes.TryGetValue(engineType, out var expectedEngineCode),
            $"{type.Name}: motore {engineType.Name} senza sigla dichiarata in {nameof(EngineCodes)}.");
        Assert.Equal(expectedEngineCode, match.Groups["engine"].Value);
    }

    [Fact]
    public void NumbersAreUniqueAndContiguousWithinSymbolAndEngine()
    {
        var groups = EnumeratePtsTypes()
            .Select(type => (ITradingStrategy)Activator.CreateInstance(type)!)
            .Select(strategy => NamePattern.Match(strategy.Name))
            .Where(match => match.Success)
            .GroupBy(match => (
                Symbol: match.Groups["symbol"].Value,
                Engine: match.Groups["engine"].Value));

        foreach (var group in groups)
        {
            var numbers = group
                .Select(match => int.Parse(match.Groups["number"].Value))
                .OrderBy(number => number)
                .ToList();

            Assert.Equal(numbers.Count, numbers.Distinct().Count());

            // Il progressivo riparte da 001 per ogni coppia (symbol, motore) e non salta:
            // un buco significa quasi sempre una strategia rimossa senza rinumerare, e da lì
            // in poi il numero smette di dire "la n-esima di questo tipo".
            Assert.Equal(Enumerable.Range(1, numbers.Count).ToList(), numbers);
        }
    }

    private static IEnumerable<Type> EnumeratePtsTypes() =>
        Assembly.GetAssembly(typeof(PTS_NQ_TFM_001_60))!
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true }
                           && type.Namespace == typeof(PTS_NQ_TFM_001_60).Namespace
                           && typeof(ITradingStrategy).IsAssignableFrom(type))
            .OrderBy(type => type.Name);

    /// <summary>
    /// Primo antenato dichiarato in <see cref="EngineCodes"/>, così una strategia che eredita da
    /// una specializzazione del motore resta associata alla sigla del motore.
    /// </summary>
    private static Type ResolveEngineType(Type strategyType)
    {
        for (var current = strategyType.BaseType; current is not null; current = current.BaseType)
        {
            if (EngineCodes.ContainsKey(current))
            {
                return current;
            }
        }

        return strategyType.BaseType ?? strategyType;
    }
}
