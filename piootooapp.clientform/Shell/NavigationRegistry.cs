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
            // Il workspace non è qui di proposito: tutto ciò che questo menu elenca vive *dentro*
            // il workspace corrente, e l'anagrafica dei workspace non può essere filtrata per sé
            // stessa. Si apre da "Gestisci workspace…" accanto al selettore, in alto.
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
            // Stessa forma delle altre due: la voce apre la lista, "Apri da piano"/"Sessione diretta"
            // portano a TradingSessionsScreen — include anche le sessioni aperte da un cBot.
            new NavigationEntry("Sessioni di trading", () => new TradingSessionListScreen()),
            // Unica voce che non apre una lista, e la ragione è che non ha un'anagrafica dietro:
            // è uno strumento diagnostico che crea una propria sessione usa e getta da un piano.
            // Sta qui e non sotto le sessioni perché non osserva quelle esistenti, ne fabbrica una.
            new NavigationEntry("Verifica concorrenza", () => new ConcurrencyHarnessScreen()))
        // La sezione "Analisi" non esiste più. Conteneva "Risultati trading" e "Rotazioni Titano":
        // i primi sono il tab Operazioni del dettaglio backtest, le seconde il dettaglio di un run.
        // Entrambe erano la stessa cosa vista due volte, e separavano il dato da ciò che lo spiega.
    };
}
