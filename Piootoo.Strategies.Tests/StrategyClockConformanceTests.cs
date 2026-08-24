using System.Reflection;
using System.Text.RegularExpressions;
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
    /// giorno grezzo della barra. Queste occorrenze si tolgono prima del controllo.
    /// </summary>
    private static readonly Regex ClockDerivedRead = new(
        @"\b\w*Clock\.(SessionDay|ToSessionTime)\([^)]*\)\.(Hour|Minute|DayOfWeek|TimeOfDay|Date)\b",
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
    /// Il fuso della sessione è una proprietà <b>della strategia</b>, non del simbolo. Dedurlo dal
    /// registro strumenti resta possibile per compatibilità, ma una <c>PTS_*</c> lo dichiara
    /// sempre: un simbolo può ospitare strategie con sessioni diverse, ed è tutto il punto.
    /// </summary>
    [Theory]
    [MemberData(nameof(PtsStrategyTypes))]
    public void EveryPtsStrategyDeclaresItsOwnSessionAndWindow(Type type)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;

        var session = ReadProtected<ZonedWindow>(strategy, "Session");
        Assert.True(session is not null, $"{type.Name}: nessuna sessione dichiarata.");
        Assert.True(session!.HasDeclaredTimeZone,
            $"{type.Name}: la sessione non dichiara il proprio fuso e lo eredita dal simbolo.");

        var window = ReadProtected<ZonedWindow>(strategy, "TradingWindow");
        Assert.True(window is not null,
            $"{type.Name}: nessuna finestra operativa dichiarata.");
        Assert.True(window!.HasDeclaredTimeZone,
            $"{type.Name}: la finestra operativa non dichiara il proprio fuso. Gli orari dei run " +
            "sono in ora della ricerca: usa ZonedWindow.ResearchHours e riportali verbatim.");
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
