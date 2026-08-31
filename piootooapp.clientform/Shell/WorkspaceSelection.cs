using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell;

/// <summary>
/// Workspace corrente della console. È scelto una volta nella barra in alto e vale per tutte le
/// schermate.
///
/// <para>Prima ogni schermata operativa aveva la propria combo: la stessa lista letta N volte dal
/// server, la stessa scelta da rifare a ogni cambio di schermata, e — peggio — due schermate
/// aperte potevano puntare a workspace diversi senza che nulla lo dicesse. Il workspace non è un
/// filtro di una schermata: è il contesto in cui si sta lavorando.</para>
///
/// <para>L'anagrafica dei workspace resta fuori da questo contesto (si crea e si elimina dalla
/// voce in alto, non dal menu filtrato): è la radice, non uno dei suoi contenuti.</para>
/// </summary>
public sealed class WorkspaceSelection
{
    private readonly WorkspaceApiClient _api;
    private IReadOnlyList<WorkspaceInfo> _workspaces = Array.Empty<WorkspaceInfo>();

    public WorkspaceSelection(WorkspaceApiClient api) => _api = api;

    /// <summary>
    /// È cambiato il workspace corrente: chi mostra dati di un workspace deve ricaricarsi. Lo
    /// shell è l'unico sottoscrittore, e ricarica la schermata in cima allo stack.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// È cambiato l'elenco (ricaricato, oppure creato/eliminato un workspace): serve a ridisegnare
    /// la combo della barra, non a ricaricare le schermate.
    /// </summary>
    public event EventHandler? ListChanged;

    public IReadOnlyList<WorkspaceInfo> Workspaces => _workspaces;

    public WorkspaceInfo? Current { get; private set; }

    /// <summary>Null quando non esiste alcun workspace: le schermate lo dicono e non caricano nulla.</summary>
    public string? CurrentId => Current?.Id;

    /// <summary>Etichetta da mostrare dove il workspace è contesto in sola lettura.</summary>
    public string CurrentDisplay => Current is { } workspace
        ? $"{workspace.Name}  ({workspace.Id})"
        : "(nessun workspace)";

    /// <summary>
    /// Rilegge l'elenco dal server e riconferma la scelta corrente. Il workspace scelto può non
    /// esistere più (eliminato, oppure si è cambiato server dalla barra in alto): in quel caso si
    /// ripiega sul primo dell'elenco, perché restare puntati su un id inesistente farebbe fallire
    /// ogni schermata con un errore che non parla di workspace.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _workspaces = await _api.ListAsync(cancellationToken);
        ListChanged?.Invoke(this, EventArgs.Empty);
        var resolved = Current is { } previous ? Find(previous.Id) : null;
        Apply(resolved ?? _workspaces.FirstOrDefault());
    }

    /// <summary>Scelta esplicita dell'utente. Un id sconosciuto non seleziona nulla.</summary>
    public void Select(string? workspaceId)
        => Apply(string.IsNullOrEmpty(workspaceId) ? null : Find(workspaceId));

    private void Apply(WorkspaceInfo? workspace)
    {
        var changed = !string.Equals(workspace?.Id, Current?.Id, StringComparison.OrdinalIgnoreCase);

        // Anche a id invariato l'istanza va aggiornata: il nome può essere cambiato nel dettaglio,
        // ed è quello che si legge nella barra.
        Current = workspace;
        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private WorkspaceInfo? Find(string workspaceId)
        => _workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id, workspaceId, StringComparison.OrdinalIgnoreCase));
}
