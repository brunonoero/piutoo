using System.Reflection;
using System.Text.RegularExpressions;
using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Impone la convenzione di nome delle strategie di catalogo:
/// <c>[SERIE]_[SYMBOL]_[ENG]_[NNN]_[TF]</c>, con la serie fra quelle di <see cref="Series"/>.
/// </summary>
/// <remarks>
/// <para>Esempio: <c>PTS_NQ_PCH_001_15</c> — la prima PriceChannel su NQ a 15 minuti della serie
/// PTS; <c>PT2_NQ_PCH_001_240</c> la prima della serie PT2, che nasce dal rifacimento dell'analisi
/// (<c>run-engine-v2/</c>, dal 16/09/2026) e numera per conto proprio.</para>
///
/// <para>Il nome porta quattro informazioni perché sono le quattro che servono a leggere un
/// report senza aprire il codice: su cosa opera, con che logica, quale variante e su che
/// timeframe. La sigla motore sta prima del numero perché il numero riparte per coppia
/// (symbol, motore): senza la sigla, <c>001</c> sarebbe ambiguo.</para>
///
/// <para>Il test esiste perché il nome non è cosmetico. <c>Name</c> è lo
/// <c>StrategyCode</c> che finisce in <c>signals.json</c>, <c>trades.json</c>, nelle chiavi di
/// posizione: una strategia che sfugge alla convenzione non rompe la
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
        [typeof(RhlEngine)] = "RHL",
        [typeof(BiasWeeklyEngine)] = "BSW",
        [typeof(BiasBarCountEngine)] = "BIA",
        [typeof(VolatilityBreakoutEngine)] = "VBO",
        [typeof(MovingAverageCrossoverEngine)] = "MAC",

        // Serie PT5DAV (24/09/2026): motori propri, sulla base comune Pt5DavEngineBase. Le sigle
        // seguono i motori della ricerca v5.0; dove il motore esiste gia' fra quelli condivisi la
        // sigla e' la stessa.
        [typeof(Pt5DavTfMirroredEngine)] = "TFM",
        [typeof(Pt5DavTfUnmirroredEngine)] = "TFU",
        [typeof(Pt5DavPriceChannelEngine)] = "PCH",
        [typeof(Pt5DavSessionBreakoutEngine)] = "SBO",
        [typeof(Pt5DavCurrentSessionBreakoutEngine)] = "BOS",
        [typeof(Pt5DavVolatilityBreakoutEngine)] = "VBO",
        [typeof(Pt5DavPivotFaderEngine)] = "LFD",
        [typeof(Pt5DavHighLowFaderEngine)] = "LFH",
        [typeof(Pt5DavRhlEngine)] = "RHL",
        [typeof(Pt5DavBollingerMirroredEngine)] = "RBM",
        [typeof(Pt5DavBollingerUnmirroredEngine)] = "RBU",
        [typeof(Pt5DavBiasMarketEngine)] = "BIA",
        [typeof(Pt5DavBiasRetracementEngine)] = "BRT",
        [typeof(Pt5DavBiasBreakoutEngine)] = "BBO",
        [typeof(Pt5DavMovingAverageCrossoverEngine)] = "MAC",
        [typeof(Pt5DavBiasWeeklyEngine)] = "BSW",

        // Serie PT6EXO (25/09/2026): famiglie nuove, cercate per essere scorrelate dal resto. Sigle in
        // docs/domini/catalogo-idee-pt6exo.md.
        [typeof(RandomEntryEngine)] = "RAN",
        [typeof(FailedBreakoutEngine)] = "FBO",
        [typeof(InternalBarStrengthEngine)] = "IBS",
        [typeof(HourOfDayEngine)] = "HOD",
        [typeof(RunOfClosesEngine)] = "RUN",
        [typeof(CompressionEngine)] = "NRX",
        [typeof(VolatilityRegimeEngine)] = "REG",
        [typeof(CalendarAnomalyEngine)] = "CAL",
        [typeof(SessionGapEngine)] = "GAP",
        [typeof(RejectionCandleEngine)] = "CDL",
        [typeof(TimeframeDisagreementEngine)] = "MTF",
        [typeof(VolumeSpikeEngine)] = "VLM",
        [typeof(RoundNumberEngine)] = "RNM",
        [typeof(FibonacciTimeEngine)] = "FIB",
        [typeof(LunarPhaseEngine)] = "LUN",
        [typeof(TimeModuloEngine)] = "MOD",
        [typeof(DayHourGridEngine)] = "DXH",
        [typeof(CrossMarketEngine)] = "XMK"
    };

    /// <summary>
    /// Le serie del catalogo, ognuna con il proprio namespace. Una serie nuova si aggiunge qui: il
    /// prefisso entra nella convenzione di nome e il namespace nell'enumerazione, cosi' le classi
    /// della serie passano dagli stessi controlli delle altre invece di essere saltate in silenzio.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Series = new Dictionary<string, string>
    {
        // PTS eliminata dal progetto il 24/09/2026, perche' obsoleta. PT2 rimossa il 22/09/2026: le
        // sue quattro classi non avevano superato la validazione ai costi veri e restavano
        // selezionabili nelle schermate.
        // PT3B: non viene da un dossier ma dall'ottimizzatore interno (piootoo-sweep). Numera per
        // conto proprio come le altre due — PT3B_FDAX_PCH_001 non e' la seconda PCH su FDAX, e' la
        // prima della sua serie.
        ["PT3B"] = typeof(PT3BStrategies.PT3B_FDAX_PCH_001_240).Namespace!,
        // PT5DAV: dalla ricerca v5.0 fatta su un altro server (piootoo-repository/PT5DAV/), 24/09/2026.
        ["PT5DAV"] = typeof(Pt5DavEngineBase).Namespace!.Replace(".Engines", string.Empty),
        // PT6EXO: motori di famiglie nuove (catalogo-idee-pt6exo.md), 25/09/2026.
        ["PT6EXO"] = typeof(RandomEntryEngine).Namespace!.Replace(".Engines", string.Empty)
    };

    private static readonly Regex NamePattern = new(
        @"^(?<series>PTS|PT3B|PT5DAV|PT6EXO)_(?<symbol>[A-Z0-9]+)_(?<engine>[A-Z]{3})_(?<number>\d{3})_(?<timeframe>\d+)$",
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
            $"{type.Name}: Name '{strategy.Name}' non rispetta [PTS|PT2]_[SYMBOL]_[ENG]_[NNN]_[TF].");

        // La serie del nome deve essere quella della cartella: una PT2 nel namespace delle PTS, o
        // viceversa, sarebbe trovata dal catalogo ma descritta dal documento sbagliato.
        var expectedSeries = Series.Single(pair => pair.Value == type.Namespace).Key;
        Assert.Equal(expectedSeries, match.Groups["series"].Value);

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
                Series: match.Groups["series"].Value,
                Symbol: match.Groups["symbol"].Value,
                Engine: match.Groups["engine"].Value));

        foreach (var group in groups)
        {
            var numbers = group
                .Select(match => int.Parse(match.Groups["number"].Value))
                .OrderBy(number => number)
                .ToList();

            Assert.Equal(numbers.Count, numbers.Distinct().Count());

            // Il progressivo riparte da 001 per ogni tripla (serie, symbol, motore) e non salta:
            // un buco significa quasi sempre una strategia rimossa senza rinumerare, e da lì
            // in poi il numero smette di dire "la n-esima di questo tipo". Le serie numerano
            // ognuna per conto proprio: PT2_NQ_PCH_001 non e' la nona PCH su NQ, e' la prima
            // dell'analisi rifatta.
            Assert.Equal(Enumerable.Range(1, numbers.Count).ToList(), numbers);
        }
    }

    private static IEnumerable<Type> EnumeratePtsTypes() =>
        Assembly.GetAssembly(typeof(PT3BStrategies.PT3B_FDAX_PCH_001_240))!
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true }
                           && type.Namespace is { } ns && Series.Values.Contains(ns)
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
