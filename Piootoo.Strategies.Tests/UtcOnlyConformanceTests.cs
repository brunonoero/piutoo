using System.Text.RegularExpressions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Nel dominio Piootoo non esiste "adesso" secondo la macchina: esiste solo UTC.
///
/// <para>Il fuso dell'host non è un dato del sistema — cambia fra la postazione di sviluppo, il
/// server e il container, e cambia due volte l'anno da solo. Se ci finisce dentro, sposta la
/// finestra di un'ottimizzazione, l'orario di polling del feed e il momento in cui una rotazione
/// settimanale scade, senza produrre alcun errore: i valori restano plausibili e diventano
/// irriproducibili. L'unico fuso diverso da UTC ammesso nel sistema è quello di borsa dichiarato
/// per simbolo, che si attraversa da <c>SessionClock</c> e non dall'orologio locale.</para>
///
/// <para>Questo test è un vincolo sul sorgente e non sul comportamento, perché il comportamento
/// sbagliato è indistinguibile da quello giusto su una macchina configurata su UTC — cioè
/// esattamente la CI dove il test girerebbe.</para>
/// </summary>
public sealed class UtcOnlyConformanceTests
{
    // La presentazione all'utente è l'eccezione legittima: la console WinForms mostra gli istanti
    // nell'ora di chi guarda lo schermo. Tutto ciò che decide, calcola o persiste sta qui dentro.
    private static readonly string[] ProjectsUnderConstraint =
    [
        "Piootoo.Shared",
        "Piootoo.Domain",
        "Piootoo.Core",
        "Piootoo.Strategies",
        "PiootooApp.Server",
        "Piootoo.FeedWorker"
    ];

    private static readonly (string Token, string Instead)[] Forbidden =
    [
        ("DateTime.Now", "DateTime.UtcNow"),
        ("DateTime.Today", "DateTime.UtcNow.Date"),
        ("DateTimeOffset.Now", "DateTimeOffset.UtcNow"),
        ("TimeZoneInfo.Local", "il fuso di borsa da InstrumentRegistry"),
        ("DateTimeKind.Local", "DateTimeKind.Utc"),
        (".ToLocalTime()", "nulla: l'istante resta UTC fino alla presentazione")
    ];

    [Fact]
    public void NoProductionCodeReadsTheClockOfTheHost()
    {
        var root = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var project in ProjectsUnderConstraint)
        {
            foreach (var file in EnumerateSources(Path.Combine(root, project)))
            {
                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    foreach (var (token, instead) in Forbidden)
                    {
                        if (lines[index].Contains(token, StringComparison.Ordinal))
                        {
                            violations.Add(
                                $"{Path.GetRelativePath(root, file)}({index + 1}): " +
                                $"'{token}' — usa {instead}.");
                        }
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// Il feed è UTC anche nel generatore Python: <c>--source-timezone</c> non ha default proprio
    /// perché il fuso del CSV va dichiarato invece che ereditato dalla macchina che lancia lo
    /// script. Se qualcuno gli desse un default, i feed comincerebbero a dipendere da dove sono
    /// stati generati.
    /// </summary>
    [Fact]
    public void FeedGeneratorRequiresAnExplicitSourceTimeZone()
    {
        var script = Path.Combine(
            FindRepositoryRoot(), "piootoo-repository", "datafeed-future", "aggregate_nq_ascii.py");
        Assert.True(File.Exists(script), $"Script di generazione non trovato: {script}");

        var source = File.ReadAllText(script);
        var declaration = Regex.Match(
            source,
            @"--source-timezone""\s*,(?<body>.*?)\)",
            RegexOptions.Singleline);

        Assert.True(declaration.Success, "Argomento --source-timezone non trovato.");
        Assert.Contains("required=True", declaration.Groups["body"].Value);
        Assert.DoesNotContain("default=", declaration.Groups["body"].Value);
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
