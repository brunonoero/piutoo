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
            new NavigationEntry("Broker", () => new BrokerListScreen()),
            // I gruppi non hanno una voce propria: sono un campo dell'account, e la loro anagrafica
            // mostrava soltanto l'id piu' l'elenco dei conti che lo dichiarano — cioe' una vista di
            // dati che la lista account gia' porta in colonna. Si creano scrivendo un id nuovo nella
            // combo del dettaglio account, che e' anche l'unico posto da cui possono cambiare.
            new NavigationEntry("Account", () => new AccountListScreen()),
            // Il workspace non è qui di proposito: tutto ciò che questo menu elenca vive *dentro*
            // il workspace corrente, e l'anagrafica dei workspace non può essere filtrata per sé
            // stessa. Si apre da "Gestisci workspace…" accanto al selettore, in alto.
            new NavigationEntry("Piani di trading", () => new PlanListScreen()),
            new NavigationEntry("Strategie", () => new StrategyListScreen()),
            new NavigationEntry("Conversioni simbolo", () => new SymbolConversionListScreen()),
            // Sola lettura: elenca cosa c'è nel repository di barre e fin dove arriva. Sta fra le
            // anagrafiche perché è ciò che un piano e un backtest possono nominare, ma non si crea
            // da qui — i feed li generano lo script di aggregazione e i cBot raccoglitori.
            new NavigationEntry("Datafeed", () => new DatafeedListScreen())),
        new NavigationSection(
            "Operatività",
            // La voce apre la lista, non il form di avvio: quest'ultimo è la destinazione di
            // "Nuovo backtest" nella lista, come per le altre anagrafiche.
            new NavigationEntry("Backtesting", () => new BacktestListScreen()),
            // Stessa forma delle altre due: la voce apre la lista, "Apri da piano"/"Sessione diretta"
            // portano a TradingSessionsScreen — include anche le sessioni aperte da un cBot.
            new NavigationEntry("Sessioni di trading", () => new TradingSessionListScreen()),
            // Voce a sé e non un tab della lista sessioni: quella elenca cosa il server ha, questa
            // risponde alla domanda opposta — cosa c'è su cTrader che il server non sta più
            // governando. Il caso che la giustifica è il riavvio del server, dove la lista sessioni
            // diventa semplicemente vuota mentre le posizioni restano aperte sul conto. Filtra per
            // conto, perché è il conto che si va ad aprire sulla piattaforma.
            new NavigationEntry("Presidio realtime", () => new RealtimeWatchScreen()),
            // Unica voce che non apre una lista, e la ragione è che non ha un'anagrafica dietro:
            // è uno strumento diagnostico che crea una propria sessione usa e getta da un piano.
            // Sta qui e non sotto le sessioni perché non osserva quelle esistenti, ne fabbrica una.
            new NavigationEntry("Verifica concorrenza", () => new ConcurrencyHarnessScreen()))
        // La sezione "Analisi" non esiste più: i risultati di trading sono il tab Operazioni del
        // dettaglio backtest, dove stanno accanto a ciò che li spiega.
    };
}
