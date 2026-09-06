using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
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
/// Quale piano un run riproduce: da lui vengono l'universo operativo (la tabella di conversione del
/// suo broker), le strategie spente, la policy di tenuta e la commissione per contratto.
///
/// <para>La prima voce e' l'assenza di piano, che e' il run neutro di sempre. L'etichetta nomina il
/// <b>broker</b> perche' e' lui a decidere l'universo: tutti i conti di un piano sono suoi e
/// condividono la stessa tabella, quindi il numero di conto non aggiungerebbe niente.</para>
/// </summary>
public sealed class PlanComboItem
{
    private PlanComboItem(TradingPlan? plan, string display)
    {
        Plan = plan;
        Display = display;
    }

    /// <summary>Null e' l'assenza di piano, non un piano chiamato "nessuno".</summary>
    public TradingPlan? Plan { get; }

    public string Display { get; }

    public static PlanComboItem None() => new(null, "Nessun piano  ·  intero masterfilter");

    /// <param name="activeStrategies">
    /// Quante strategie del masterfilter il piano lascia accese. Null quando il masterfilter non e'
    /// disponibile: si tace, invece di scrivere uno zero che si leggerebbe come «non opera nulla».
    /// Si mostra questo e non il numero delle spente perche' e' la domanda che si fa scegliendo un
    /// piano — quante ne gira — mentre le spente sono un numero che dipende da quanto e' grande il
    /// masterfilter e non dice quanto il piano opera.
    /// </param>
    public static PlanComboItem Of(TradingPlan plan, int? activeStrategies)
        => new(plan,
            $"{plan.Name}  ·  {plan.Code}  ·  " +
            (string.IsNullOrWhiteSpace(plan.BrokerCode) ? "senza broker" : $"broker {plan.BrokerCode}") +
            (activeStrategies is { } active ? $"  ·  {active} strategie attive" : string.Empty));

    // Nessuna voce "non piu' presente" come per i datasource: li' basta il nome per far fallire il
    // run in modo esplicito, qui la voce dovrebbe portarsi dietro il piano intero — universo,
    // spente, tenuta — che non esiste piu'. Un piano cancellato riporta quindi la scelta su
    // «nessun piano», che e' cio' che il workspace dichiara adesso.

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

// Niente AccountComboItem: il backtest sceglie il piano, e il conto non decideva nulla che il piano
// non dicesse già (docs/decisioni.md 2026-09-05).

/// <summary>
/// Da quale misura prendere lo spread degli ingressi: nessuna, oppure quella di un broker sotto
/// <c>piootoo-repository/spread</c>.
///
/// <para>L'etichetta porta simboli, data e <b>se ci sono le ore</b>: senza il file
/// <c>spread-by-hour</c> la risoluzione oraria non è proponibile per quel broker, e scoprirlo dopo
/// il lancio costerebbe il run.</para>
/// </summary>
public sealed class SpreadComboItem
{
    private SpreadComboItem(string? broker, bool hasHourly, string display)
    {
        Broker = broker;
        HasHourly = hasHourly;
        Display = display;
    }

    /// <summary>Null è l'assenza di misura: il run entra al prezzo del feed, come ha sempre fatto.</summary>
    public string? Broker { get; }

    /// <summary>Se esiste il file per ora della stessa misura.</summary>
    public bool HasHourly { get; }

    public string Display { get; }

    public static SpreadComboItem None() => new(null, false, "Nessuno  ·  ingressi al prezzo del feed");

    public static SpreadComboItem Measured(SpreadBrokerInfo info)
        => new(info.Broker, info.HasHourly,
            $"{info.Broker}  ·  {info.SymbolCount} simboli  ·  {info.LastWriteUtc:yyyy-MM-dd}" +
            (info.HasHourly ? "  ·  con le ore" : "  ·  senza le ore"));

    public override string ToString() => Display;
}

/// <summary>Quale colonna della distribuzione diventa il numero del run.</summary>
public sealed class SpreadStatisticItem
{
    public SpreadStatisticItem(SpreadStatistic statistic, string display)
    {
        Statistic = statistic;
        Display = display;
    }

    public SpreadStatistic Statistic { get; }

    public string Display { get; }

    public override string ToString() => Display;
}

/// <summary>Quale riga: la costante del simbolo, o l'ora UTC dell'ingresso.</summary>
public sealed class SpreadResolutionItem
{
    public SpreadResolutionItem(SpreadResolution resolution, string display)
    {
        Resolution = resolution;
        Display = display;
    }

    public SpreadResolution Resolution { get; }

    public string Display { get; }

    public override string ToString() => Display;
}
