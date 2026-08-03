using piootooapp.clientform.Shell.Screens;

namespace piootooapp.clientform.Shell;

/// <summary>Voce di primo livello del menu di sinistra.</summary>
public sealed class NavigationEntry
{
    public NavigationEntry(string label, Func<Control>? screenFactory)
    {
        Label = label;
        ScreenFactory = screenFactory;
    }

    public string Label { get; }

    /// <summary>Null finché la schermata non è implementata: la voce resta visibile ma disabilitata.</summary>
    public Func<Control>? ScreenFactory { get; }

    public bool IsAvailable => ScreenFactory != null;
}

public sealed class NavigationSection
{
    public NavigationSection(string label, params NavigationEntry[] entries)
    {
        Label = label;
        Entries = entries;
    }

    public string Label { get; }

    public IReadOnlyList<NavigationEntry> Entries { get; }
}

/// <summary>
/// Menu di navigazione. È l'unico punto da toccare per aggiungere una voce: una riga qui
/// più la coppia di UserControl lista/dettaglio.
/// </summary>
public static class NavigationRegistry
{
    public static IReadOnlyList<NavigationSection> Build() => new[]
    {
        new NavigationSection(
            "Anagrafiche",
            new NavigationEntry("Account", () => new AccountListScreen()),
            new NavigationEntry("Gruppi", () => new GroupListScreen()),
            new NavigationEntry("Workspace", () => new WorkspaceListScreen()),
            new NavigationEntry("Piani di trading", () => new PlanListScreen()),
            new NavigationEntry("Strategie", () => new StrategyListScreen()),
            new NavigationEntry("Conversioni simbolo", () => new SymbolPresetScreen())),
        new NavigationSection(
            "Operatività",
            new NavigationEntry("Backtesting", () => new BacktestingScreen()),
            new NavigationEntry("Titano", () => new TitanoScreen()),
            new NavigationEntry("Sessioni di trading", () => new TradingSessionsScreen())),
        new NavigationSection(
            "Analisi",
            new NavigationEntry("Risultati trading", () => new TradingResultsScreen()),
            new NavigationEntry("Rotazioni Titano", () => new RotationsScreen()))
    };
}
