namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le strategie di un piano si ricavano in un punto solo, <c>PlanResolver</c>. Fuori da li' il
/// masterfilter di un workspace si legge soltanto dove un piano non c'e': la sessione manuale e il
/// backtest neutro, piu' il servizio che lo possiede e il controller che lo espone.
///
/// <para>Un nuovo lettore del masterfilter e' quasi sempre un nuovo modo di ricavare le strategie di
/// un piano, e con i piani di produzione — che il masterfilter non ce l'hanno — sarebbe il modo in cui
/// una sessione e il suo backtest finiscono per eseguire insiemi diversi. Vedi
/// <c>docs/domini/broker-workspace.md</c>.</para>
/// </summary>
public sealed class PlanResolutionConformanceTests
{
    private static readonly string[] ProjectsUnderConstraint =
    [
        "Piootoo.Domain",
        "Piootoo.Core",
        "PiootooApp.Server",
        "Piootoo.FeedWorker"
    ];

    /// <summary>File ammessi e quante chiamate ciascuno puo' contenere.</summary>
    private static readonly Dictionary<string, int> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        // Il proprietario del file masterfilter.json.
        [Path.Combine("Piootoo.Core", "Services", "WorkspaceService.cs")] = int.MaxValue,
        // Il risolutore: l'universo di un piano di workspace.
        [Path.Combine("Piootoo.Core", "Services", "Plans", "PlanResolver.cs")] = 1,
        // La sessione manuale, senza piano (CreateCore con resolvedPlan null).
        [Path.Combine("Piootoo.Core", "Services", "TradingSessionService.cs")] = 1,
        // Il backtest neutro, senza piano.
        [Path.Combine("PiootooApp.Server", "Controllers", "BacktestingController.cs")] = 1,
        // L'endpoint che mostra il masterfilter.
        [Path.Combine("PiootooApp.Server", "Controllers", "WorkspaceController.cs")] = int.MaxValue
    };

    [Fact]
    public void OnlyThePlanResolverDerivesThePlanStrategiesFromTheMasterfilter()
    {
        var root = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var project in ProjectsUnderConstraint)
        {
            var directory = Path.Combine(root, project);
            if (!Directory.Exists(directory))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                     && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
            {
                var relative = Path.GetRelativePath(root, file);
                var calls = File.ReadAllLines(file).Count(line => line.Contains("GetMasterFilter(", StringComparison.Ordinal));
                if (calls == 0)
                    continue;

                if (!Allowed.TryGetValue(relative, out var limit))
                    violations.Add($"{relative}: legge il masterfilter ({calls}×). Le strategie di un piano si risolvono con PlanResolver.");
                else if (calls > limit)
                    violations.Add($"{relative}: {calls} letture del masterfilter, ammesse {limit}.");
            }
        }

        Assert.Empty(violations);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException($"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
