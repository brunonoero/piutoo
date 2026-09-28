using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Models.BrokerWorkspaces;

/// <summary>
/// Il posto dei piani in produzione di un broker: uno per broker, in
/// <c>[BasePath]\broker-workspaces\{BROKER}\</c>. Non ha masterfilter: ogni piano dichiara le proprie
/// strategie (<see cref="TradingPlan.EnabledStrategies"/>), e ci si entra solo da un best plan.
/// Vedi <c>docs/domini/broker-workspace.md</c>.
/// </summary>
public sealed class BrokerWorkspace
{
    public const string FileName = "broker-workspace.json";

    /// <summary>Il codice del broker (<c>TradingBroker.Code</c>): e' anche la chiave, uno per broker.</summary>
    public string BrokerCode { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}

/// <summary>Riga dell'elenco dei broker workspace.</summary>
public sealed class BrokerWorkspaceSummary
{
    public string BrokerCode { get; set; } = string.Empty;
    public string BrokerName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public int ActivePlans { get; set; }
    public int RetiredPlans { get; set; }

    /// <summary>Strategie dei piani attivi: per costruzione ognuna sta in un piano solo.</summary>
    public int ActiveStrategies { get; set; }

    /// <summary>Conti dei piani attivi, contati una volta per piano: un conto puo' eseguire piu' piani.</summary>
    public int ActiveAccounts { get; set; }
}

/// <summary>Un broker workspace con i suoi piani, attivi e ritirati.</summary>
public sealed class BrokerWorkspaceDetail
{
    public string BrokerCode { get; set; } = string.Empty;
    public string BrokerName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public List<TradingPlan> Plans { get; set; } = new();
}

/// <summary>
/// Promozione di un best plan a piano di produzione. Il broker workspace e' quello del broker del
/// piano, e nasce alla prima promozione.
/// </summary>
public sealed class PromoteToProductionRequest
{
    public string BestPlanId { get; set; } = string.Empty;

    /// <summary>Codice nuovo, unico fra workspace e broker workspace (per esempio <c>FTMO-EUROPA</c>).</summary>
    public string PlanCode { get; set; } = string.Empty;

    /// <summary>Vuoto = il nome del piano di origine.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// I conti che eseguono il piano in produzione. Vuota = quelli del piano di origine. I conti non
    /// cambiano cio' che il run ha misurato — le size scalano con il capitale del conto — e un piano di
    /// ricerca gira spesso sul conto di prova comune a tutti.
    /// </summary>
    public List<string> Accounts { get; set; } = new();

    /// <summary>
    /// Verifica tutto e restituisce il piano che nascerebbe, senza scrivere niente. Un conflitto e'
    /// un errore anche qui: serve alla console per mostrarlo prima della conferma.
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Duplicato di un piano di produzione con conti nuovi, per quando il broker cambia il numero di
/// conto. L'originale viene ritirato nello stesso passaggio.
/// </summary>
public sealed class DuplicateProductionPlanRequest
{
    public string NewCode { get; set; } = string.Empty;

    /// <summary>Vuoto = il nome dell'originale.</summary>
    public string? NewName { get; set; }

    /// <summary>I conti del piano nuovo. Obbligatori: e' la sola cosa che il duplicato cambia.</summary>
    public List<string> Accounts { get; set; } = new();

    public bool DryRun { get; set; }
}
