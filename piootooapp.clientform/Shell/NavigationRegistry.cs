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
            new NavigationEntry("Conversioni simbolo", () => new SymbolConversionListScreen()),
            // Il setup di rotazione è globale come account e gruppi, non appartiene a un
            // workspace: sta fra le anagrafiche, non nell'operatività. Per workspace è il run.
            new NavigationEntry("Setup Titano", () => new TitanoSetupListScreen())),
        new NavigationSection(
            "Operatività",
            // La voce apre la lista, non il form di avvio: quest'ultimo è la destinazione di
            // "Nuovo backtest" nella lista, come per le altre anagrafiche.
            new NavigationEntry("Backtesting", () => new BacktestListScreen()),
            // Stessa forma: la voce apre la lista dei run, "Nuova rotazione" porta a TitanoScreen.
            new NavigationEntry("Run Titano", () => new TitanoRunListScreen()),
            new NavigationEntry("Sessioni di trading", () => new TradingSessionsScreen()))
        // La sezione "Analisi" non esiste più. Conteneva "Risultati trading" e "Rotazioni Titano":
        // i primi sono il tab Operazioni del dettaglio backtest, le seconde il dettaglio di un run.
        // Entrambe erano la stessa cosa vista due volte, e separavano il dato da ciò che lo spiega.
    };
}
