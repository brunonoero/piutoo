using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Voce del selettore di workspace nella barra in alto. Non è più usata dalle schermate: il
/// workspace è contesto della console, non un filtro che ognuna ripropone per conto suo.
/// </summary>
public sealed class WorkspaceComboItem
{
    public WorkspaceComboItem(WorkspaceInfo info) => Info = info;

    public WorkspaceInfo Info { get; }

    public override string ToString() => $"{Info.Name}  ({Info.Id})";
}

/// <summary>
/// Da quale archivio di barre far leggere un run: il datafeed interno, oppure quello di un broker
/// sotto <c>datafeed-external</c>.
///
/// <para>L'etichetta porta simboli e ultima scrittura perché due archivi non si distinguono dal
/// nome: uno fermo da settimane produce un backtest che finisce prima di quanto sembri, e il
/// summary lo direbbe solo a run concluso.</para>
/// </summary>
public sealed class DatafeedComboItem
{
    private DatafeedComboItem(string? broker, string display)
    {
        Broker = broker;
        Display = display;
    }

    /// <summary>Null è il datafeed interno: l'assenza di broker, non un broker chiamato "interno".</summary>
    public string? Broker { get; }

    public string Display { get; }

    public static DatafeedComboItem Internal() => new(null, "Interno  ·  piootoo-repository/datafeed");

    public static DatafeedComboItem External(DatafeedBrokerInfo info)
        => new(info.Broker,
            $"{info.Broker}  ·  {info.SymbolCount} simboli, {info.FeedCount} feed" +
            (info.LastWriteUtc is { } last ? $"  ·  agg. {last:yyyy-MM-dd HH:mm} UTC" : string.Empty));

    /// <summary>Broker non più presente nell'elenco del server: si mostra invece di sparire in silenzio.</summary>
    public static DatafeedComboItem Missing(string broker) => new(broker, $"{broker}  ·  (non più presente)");

    public override string ToString() => Display;
}

/// <summary>
/// Quale conto usare come <b>universo operativo</b> di un run: girano solo le strategie sui simboli
/// che la sua tabella di conversione prevede.
///
/// <para>La prima voce e' l'assenza di conto, che e' il run neutro di sempre. L'etichetta nomina la
/// tabella perche' e' quella a decidere l'universo, non il conto: due conti sulla stessa tabella
/// producono lo stesso elenco di strategie, e un conto senza tabella non restringe niente.</para>
/// </summary>
public sealed class AccountComboItem
{
    private AccountComboItem(string accountNumber, string display)
    {
        AccountNumber = accountNumber;
        Display = display;
    }

    /// <summary>Vuoto e' l'assenza di conto, non un conto chiamato "nessuno".</summary>
    public string AccountNumber { get; }

    public string Display { get; }

    public static AccountComboItem None() => new(string.Empty, "Nessun conto  ·  intero masterfilter");

    public static AccountComboItem Of(WorkspaceAccount account)
        => new(account.AccountNumber,
            $"{account.Name}  ·  {account.AccountNumber}  ·  " +
            (string.IsNullOrWhiteSpace(account.SymbolConversionCode)
                ? "nessuna conversione (opera tutto)"
                : $"conversione {account.SymbolConversionCode}"));

    public override string ToString() => Display;
}

public sealed class BacktestComboItem
{
    public BacktestComboItem(WorkspaceBacktestInfo info) => Info = info;

    public WorkspaceBacktestInfo Info { get; }

    /// <summary>
    /// L'origine è in etichetta perché da quando le sessioni di backtest scrivono anch'esse sotto
    /// <c>backtests/</c> i due tipi convivono nella stessa lista, e prendere un run dell'engine
    /// esterno invece di quello interno non dà alcun errore: dà numeri diversi.
    /// </summary>
    public override string ToString()
        => $"{Info.FolderName}  ·  {DescribeOrigin(Info)}  ·  {Info.LastModifiedUtc:yyyy-MM-dd HH:mm} UTC" +
           (Info.ResultsCount > 0 ? $"  ·  {Info.ResultsCount} risultati" : "  ·  nessun risultato");

    public static string DescribeOrigin(WorkspaceBacktestInfo info) => info.Origin switch
    {
        BacktestOrigin.Internal => "interno",
        BacktestOrigin.ExternalBroker => string.IsNullOrWhiteSpace(info.PlanCode)
            ? "cBot"
            : $"cBot {info.PlanCode}",
        _ => "origine ignota"
    };
}

/// <summary>
/// Voce generica id + etichetta per le combo che devono poter esporre anche un valore già
/// persistito ma non più presente nella lista corrente. Scartarlo in silenzio riscriverebbe
/// l'entità azzerando un riferimento che il resto del sistema considera ancora valido.
/// </summary>
public sealed class ValueComboItem
{
    private ValueComboItem(string? id, string display)
    {
        Id = id;
        Display = display;
    }

    /// <summary>Null è la voce "nessuno".</summary>
    public string? Id { get; }

    public string Display { get; }

    public static ValueComboItem None(string label) => new(null, label);

    /// <summary>
    /// Voce "vuota" per le celle di griglia: l'id è stringa vuota e non null, perché il binding
    /// di <c>DataGridViewComboBoxColumn</c> confronta il valore della cella con il ValueMember.
    /// </summary>
    public static ValueComboItem Blank(string label) => new(string.Empty, label);

    public static ValueComboItem Of(string id, string display) => new(id, display);

    public static ValueComboItem Missing(string id) => new(id, $"{id}  ·  (non più presente)");

    public override string ToString() => Display;
}

// AccountComboItem rimosso con il selettore account del backtest: l'unico consumatore era la
// schermata di avvio, e il backtest interno non conosce più i conti (docs/decisioni.md 2026-08-05).
