using System.Reflection;
using System.Text.RegularExpressions;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Una strategia non deve dipendere né dall'ora della macchina su cui gira, né dall'orologio in
/// cui sono stampate le barre che riceve.
///
/// <para>Il secondo vincolo è quello che si è rotto davvero. Il feed <c>@NQ</c> ha i timestamp
/// marcati <c>Z</c> ma stampati in ora europea, e finché il codice leggeva l'ora grezza della
/// barra il confine di sessione cadeva un'ora prima d'inverno e due d'estate rispetto a quello
/// che la ricerca aveva usato. Non produceva errori: produceva numeri plausibili. Vedi
/// <c>docs/decisioni.md</c>, voce del 19/08/2026.</para>
///
/// <para>La forma corretta è una sola: la strategia dichiara i propri orari come
/// <see cref="ZonedWindow"/> — orario locale più fuso IANA — e il confronto passa da
/// <see cref="SessionClock"/>, che converte l'istante assoluto della barra. Questi test rendono
/// l'altra forma non esprimibile.</para>
/// </summary>
public sealed class StrategyClockConformanceTests
{
    /// <summary>
    /// Letture dell'orologio di un <c>DateTime</c>. Dentro <c>Piootoo.Strategies</c> non ne
    /// esistono di legittime: l'istante di una barra è UTC e non dice nulla di utile finché non
    /// viene portato in un fuso dichiarato.
    /// </summary>
    private static readonly Regex RawClockRead = new(
        @"\.(Hour|Minute|DayOfWeek|TimeOfDay)\b", RegexOptions.Compiled);

    /// <summary>
    /// Leggere un componente di un valore <b>già passato dall'orologio</b> è la forma corretta,
    /// non una violazione: <c>Clock.SessionDay(bar).DayOfWeek</c> è il giorno di borsa, non il
    /// giorno grezzo della barra, e <c>Clock.BarLabelDay(bar, tf).DayOfWeek</c> e' il giorno con
    /// cui la ricerca chiamava quella barra. Queste occorrenze si tolgono prima del controllo.
    /// </summary>
    private static readonly Regex ClockDerivedRead = new(
        @"\b\w*Clock\.(SessionDay|ToSessionTime|BarLabelDay)\([^)]*\)\)?\.(Hour|Minute|DayOfWeek|TimeOfDay|Date)\b",
        RegexOptions.Compiled);

    [Fact]
    public void NoStrategyCodeReadsTheRawClockOfABar()
    {
        var root = FindRepositoryRoot();
        var progetto = Path.Combine(root, "Piootoo.Strategies");
        var violations = new List<string>();

        foreach (var file in EnumerateSources(progetto))
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = ClockDerivedRead.Replace(StripComment(lines[index]), string.Empty);
                if (!RawClockRead.IsMatch(line))
                    continue;

                violations.Add(
                    $"{Path.GetRelativePath(root, file)}({index + 1}): {lines[index].Trim()}");
            }
        }

        Assert.True(violations.Count == 0,
            "Una strategia non legge l'ora di una barra, legge il suo istante. Passa da " +
            "SessionClock (Clock / WindowClock), che converte nel fuso dichiarato:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// La sessione non si dichiara più: la governa il calendario del simbolo. Qui si verifica che
    /// <b>si risolva</b> e che dichiari il proprio fuso — non che la classe la scriva.
    ///
    /// <para><b>I simboli che il calendario non conosce si saltano</b>, con lo stesso criterio del
    /// test gemello <c>ResearchSessionStartConformanceTests</c>: sono un buco noto del registro, non
    /// un porting da correggere, e una strategia su un simbolo non verificato non è comunque
    /// eseguibile — il <c>PointValue</c> le manca prima ancora della sessione. Quante siano lo dice
    /// <see cref="StrategiesOnSymbolsWithoutACalendarAreDeclared"/>.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(PtsStrategyTypes))]
    public void EveryPtsStrategyResolvesItsSessionAndDeclaresItsWindow(Type type)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;

        if (!MarketCalendarRegistry.Current.TryGet(strategy.Symbol, out _))
            return;

        var session = ReadProtected<ZonedWindow>(strategy, "Session");
        Assert.True(session is not null, $"{type.Name}: sessione non risolta dal calendario.");

        var window = ReadProtected<ZonedWindow>(strategy, "TradingWindow");
        Assert.True(window is not null,
            $"{type.Name}: nessuna finestra operativa dichiarata.");

        // L'orologio non si "dichiara" piu' come stringa: e' nel tipo, e il fuso vero lo risolve il
        // calendario del simbolo. Resta pero' da imporre QUALE: tutte le PTS vengono da run di
        // ricerca, che scrivono le finestre in CET per ogni simbolo. Una finestra in ora di borsa
        // qui significherebbe un porting da sorgente EasyLanguage, che e' un caso legittimo ma da
        // decidere — non da scoprire mesi dopo dai numeri.
        Assert.True(window!.Clock == InstrumentClock.Research,
            $"{type.Name}: la finestra operativa e' dichiarata in ora di BORSA. Le PTS vengono dai " +
            "run di ricerca, che scrivono start_hour/end_hour in CET per ogni simbolo: se questa " +
            "arriva davvero da una sorgente EasyLanguage va messa in lista, non lasciata passare.");
    }

    /// <summary>
    /// Le strategie su simboli che il calendario non conosce, contate una per una invece che
    /// saltate in silenzio. <b>Non sono eseguibili</b>: manca loro il <c>PointValue</c> prima ancora
    /// della sessione, quindi un backtest che le includa si ferma con un errore esplicito.
    ///
    /// <para>Dall'08/09/2026 l'elenco è <b>vuoto</b>: le 21 classi che ci stavano — HK (5), HO (8),
    /// JY (8) — sono state cancellate invece di essere verificate. Il test resta perché la lista
    /// torni a crescere solo di proposito: una classe nuova su un simbolo che il calendario non
    /// conosce lo fa fallire subito, invece di essere saltata in silenzio dal catalogo.</para>
    /// </summary>
    [Fact]
    public void StrategiesOnSymbolsWithoutACalendarAreDeclared()
    {
        var senzaCalendario = PtsStrategyTypes
            .Cast<object[]>()
            .Select(row => (ITradingStrategy)Activator.CreateInstance((Type)row[0])!)
            .Where(strategy => !MarketCalendarRegistry.Current.TryGet(strategy.Symbol, out _))
            .GroupBy(strategy => MarketCalendar.Normalize(strategy.Symbol), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        // Vuoto di proposito: ogni PTS_* del catalogo sta su un simbolo che il calendario dichiara.
        var attese = new Dictionary<string, int>(StringComparer.Ordinal);

        Assert.Equal(
            attese.OrderBy(x => x.Key, StringComparer.Ordinal),
            senzaCalendario.OrderBy(x => x.Key, StringComparer.Ordinal));
    }

    public static TheoryData<Type> PtsStrategyTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in Assembly.GetAssembly(typeof(PTS_NQ_TFM_001_60))!
                         .GetTypes()
                         .Where(t => t is { IsAbstract: false, IsClass: true }
                                     && t.Namespace == typeof(PTS_NQ_TFM_001_60).Namespace
                                     && typeof(ITradingStrategy).IsAssignableFrom(t))
                         .OrderBy(t => t.Name))
            {
                data.Add(type);
            }

            return data;
        }
    }

    private static T? ReadProtected<T>(object instance, string name) where T : class
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.DeclaredOnly);
            if (property is not null)
                return property.GetValue(instance) as T;
        }

        return null;
    }

    /// <summary>
    /// Toglie commenti di riga e XMLdoc: il vincolo è sul codice, e una spiegazione che nomina
    /// <c>.Hour</c> per dire di non usarlo non è una violazione.
    /// </summary>
    private static string StripComment(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
            trimmed.StartsWith("*", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return comment >= 0 ? line[..comment] : line;
    }

    private static IEnumerable<string> EnumerateSources(string projectDirectory) =>
        Directory.Exists(projectDirectory)
            ? Directory
                .EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                            && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            : throw new DirectoryNotFoundException($"Progetto non trovato: {projectDirectory}");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
