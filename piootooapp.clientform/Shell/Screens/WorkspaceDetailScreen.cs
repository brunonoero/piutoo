using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Voce spuntabile del masterfilter. Rappresenta una strategia del catalogo oppure un id che sta
/// nel masterfilter ma <b>non</b> nel catalogo, perche' e' stata disabilitata o rimossa dopo che
/// il workspace era gia' stato salvato.
///
/// <para>La seconda forma esiste per un motivo solo: senza una voce propria quell'id resterebbe
/// invisibile nell'elenco e nessuno potrebbe toglierlo dal masterfilter, mentre l'apertura di una
/// sessione lo rifiuta (<c>TradingSessionService.CreateCore</c>). Il workspace resterebbe
/// inutilizzabile senza che la schermata mostri il perche'.</para>
/// </summary>
public sealed class StrategyChecklistItem
{
    public StrategyChecklistItem(StrategyCatalogItem strategy)
    {
        Strategy = strategy;
        Id = strategy.Id;
    }

    private StrategyChecklistItem(string strategyId)
    {
        Strategy = null;
        Id = strategyId;
    }

    /// <summary>Voce per un id del masterfilter che il catalogo non elenca piu'.</summary>
    public static StrategyChecklistItem OutOfCatalog(string strategyId) => new(strategyId);

    /// <summary>Null quando l'id non e' nel catalogo.</summary>
    public StrategyCatalogItem? Strategy { get; }

    /// <summary>Id di classe: la chiave che finisce nel masterfilter, sempre valorizzata.</summary>
    public string Id { get; }

    public override string ToString()
    {
        if (Strategy is null)
        {
            return $"(!)  fuori catalogo: disabilitata o rimossa  ·  togli la spunta per pulire il masterfilter   [{Id}]";
        }

        var timeframe = Strategy.TimeframeMinutes > 0 ? $"{Strategy.TimeframeMinutes}m" : "—";
        // La tenuta si legge qui perche' e' qui che si sceglie: un masterfilter pieno di multiday
        // su un piano che vieta l'overnight e' un run che misura il flat, non le strategie.
        var holding = string.IsNullOrWhiteSpace(Strategy.HoldingLabel) ? "?" : Strategy.HoldingLabel;
        return $"{Strategy.Symbol}  ·  {Strategy.Name}  ·  {timeframe}  ·  {holding}   [{Strategy.Id}]";
    }
}

/// <summary>
/// Dettaglio di un workspace: nome e masterfilter. Le voci spuntate sono <b>Id di classe</b>
/// (es. <c>PTS_NQ_TFM_001_60</c>), che è la chiave di selezione dal catalogo — non il codice di
/// esecuzione. Con <see cref="SetWorkspaceId"/> a null la schermata crea un nuovo workspace.
/// </summary>
public partial class WorkspaceDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    private readonly List<StrategyCatalogItem> _catalog = new();
    private readonly HashSet<string> _selectedIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Id gia' nel masterfilter che il catalogo non elenca. Restano in elenco per tutta la vita
    /// della schermata anche dopo che si e' tolta la spunta: e' cosi' che si vede di averli tolti,
    /// e che si puo' rimettere una spunta data per sbaglio.
    /// </summary>
    private readonly List<string> _outOfCatalogIds = new();
    private ShellContext? _context;
    private string? _workspaceId;
    private string _masterFilterName = string.Empty;
    private bool _suppressItemCheck;
    private bool _isDirty;

    public WorkspaceDetailScreen()
    {
        InitializeComponent();
    }

    public string ScreenTitle => IsNew ? "Nuovo workspace" : _workspaceId ?? "Workspace";

    public bool HasUnsavedChanges => _isDirty;

    private bool IsNew => string.IsNullOrWhiteSpace(_workspaceId);

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. Null significa nuovo workspace.</summary>
    public void SetWorkspaceId(string? workspaceId) => _workspaceId = workspaceId;

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var strategies = await _context.Services.Api.ListStrategiesAsync(cancellationToken);
            _catalog.Clear();
            _catalog.AddRange(strategies
                .OrderBy(strategy => strategy.Symbol, StringComparer.OrdinalIgnoreCase)
                .ThenBy(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase));

            _selectedIds.Clear();
            _outOfCatalogIds.Clear();
            if (IsNew)
            {
                _masterFilterName = string.Empty;
                _nameTextBox.Text = string.Empty;
                _nameTextBox.ReadOnly = false;
                _toolbar.Title = "Nuovo workspace";
            }
            else
            {
                var filter = await _context.Services.Api.GetMasterFilterAsync(_workspaceId!, cancellationToken);
                _masterFilterName = filter.Name;
                _selectedIds.UnionWith(filter.StrategiesFilter);

                // Il catalogo non elenca le strategie disabilitate: gli id del masterfilter che non
                // vi compaiono vanno mostrati lo stesso, altrimenti ogni salvataggio li riscrive e
                // l'apertura di una sessione continua a rifiutare il workspace.
                var catalogIds = new HashSet<string>(
                    _catalog.Select(strategy => strategy.Id), StringComparer.OrdinalIgnoreCase);
                _outOfCatalogIds.AddRange(_selectedIds
                    .Where(id => !catalogIds.Contains(id))
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

                _nameTextBox.Text = string.IsNullOrWhiteSpace(filter.Name) ? _workspaceId! : filter.Name;
                // Il nome del workspace è la cartella su disco: l'API non espone una rinomina.
                _nameTextBox.ReadOnly = true;
                _toolbar.Title = _nameTextBox.Text;
            }

            _infoLabel.Text = IsNew
                ? "Il nome diventa la cartella del workspace. Le strategie spuntate finiscono nel masterfilter."
                : $"Id: {_workspaceId}  ·  catalogo: {_catalog.Count} strategie disponibili"
                  + (_outOfCatalogIds.Count == 0
                      ? string.Empty
                      : $"  ·  {_outOfCatalogIds.Count} id fuori catalogo: togli la spunta e salva, "
                        + "altrimenti la sessione rifiuta il workspace");

            ApplyStrategyFilter();
            SetDirty(false);
            _context.Navigation.SetStatus(IsNew
                ? $"Catalogo caricato: {_catalog.Count} strategie."
                : $"Masterfilter di '{_workspaceId}': {_selectedIds.Count} strategie selezionate."
                  + (_outOfCatalogIds.Count == 0
                      ? string.Empty
                      : $" {_outOfCatalogIds.Count} fuori catalogo: {string.Join(", ", _outOfCatalogIds)}."));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// Ricostruisce la lista in base al testo di ricerca e alla spunta "solo selezionate".
    /// Non viene richiamata quando si spunta una strategia: una voce che sparisce sotto il
    /// cursore appena la togli renderebbe impossibile correggere un click sbagliato.
    /// </summary>
    private void ApplyStrategyFilter()
    {
        var filter = _strategyFilterTextBox.Text.Trim();
        var onlySelected = _onlySelectedCheckBox.Checked;
        _suppressItemCheck = true;
        _strategiesList.BeginUpdate();
        _strategiesList.Items.Clear();

        // In testa, cosi' si vedono senza cercarli: sono l'unica cosa che impedisce al workspace
        // di aprire una sessione.
        foreach (var id in _outOfCatalogIds.Where(id =>
                     MatchesId(id, filter) && (!onlySelected || _selectedIds.Contains(id))))
        {
            var outOfCatalogIndex = _strategiesList.Items.Add(StrategyChecklistItem.OutOfCatalog(id));
            _strategiesList.SetItemChecked(outOfCatalogIndex, _selectedIds.Contains(id));
        }

        foreach (var strategy in _catalog.Where(strategy =>
                     Matches(strategy, filter)
                     && (!onlySelected || _selectedIds.Contains(strategy.Id))))
        {
            var index = _strategiesList.Items.Add(new StrategyChecklistItem(strategy));
            _strategiesList.SetItemChecked(index, _selectedIds.Contains(strategy.Id));
        }

        _strategiesList.EndUpdate();
        _suppressItemCheck = false;
        UpdateSelectionCount();
    }

    /// <summary>Filtro per le voci fuori catalogo, di cui si conosce il solo id.</summary>
    private static bool MatchesId(string strategyId, string filter)
        => filter.Length == 0 || strategyId.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static bool Matches(StrategyCatalogItem strategy, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return strategy.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Symbol.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSelectionCount()
    {
        var shown = _strategiesList.Items.Count;
        var outOfCatalogSelected = _outOfCatalogIds.Count(_selectedIds.Contains);
        _selectionCountLabel.Text = $"{_selectedIds.Count} selezionate su {_catalog.Count}" +
            (shown != _catalog.Count ? $"  ·  {shown} in elenco" : string.Empty) +
            (outOfCatalogSelected > 0 ? $"  ·  {outOfCatalogSelected} fuori catalogo" : string.Empty);
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        _toolbar.SetDirty(dirty);
    }

    private void OnStrategyFilterChanged(object? sender, EventArgs e) => ApplyStrategyFilter();

    private void OnNameChanged(object? sender, EventArgs e)
    {
        if (!_nameTextBox.ReadOnly)
        {
            SetDirty(true);
        }
    }

    private void OnStrategyItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressItemCheck || _strategiesList.Items[e.Index] is not StrategyChecklistItem item)
        {
            return;
        }

        if (e.NewValue == CheckState.Checked)
        {
            _selectedIds.Add(item.Id);
        }
        else
        {
            _selectedIds.Remove(item.Id);
        }

        SetDirty(true);
        // Il conteggio va aggiornato dopo che la spunta è stata applicata alla lista.
        if (IsHandleCreated && !IsDisposed)
        {
            BeginInvoke(UpdateSelectionCount);
        }
    }

    private void OnSelectAllClick(object? sender, EventArgs e) => SetAllVisible(true);

    private void OnSelectNoneClick(object? sender, EventArgs e) => SetAllVisible(false);

    /// <summary>Agisce solo sulle voci mostrate, così il filtro è anche uno strumento di selezione.</summary>
    private void SetAllVisible(bool isChecked)
    {
        _suppressItemCheck = true;
        for (var index = 0; index < _strategiesList.Items.Count; index++)
        {
            if (_strategiesList.Items[index] is not StrategyChecklistItem item)
            {
                continue;
            }

            _strategiesList.SetItemChecked(index, isChecked);
            if (isChecked)
            {
                _selectedIds.Add(item.Id);
            }
            else
            {
                _selectedIds.Remove(item.Id);
            }
        }

        _suppressItemCheck = false;
        SetDirty(true);

        // Su un'azione massiva il filtro va riapplicato: se mostra solo le selezionate e le hai
        // appena deselezionate in blocco, la lista deve svuotarsi.
        if (_onlySelectedCheckBox.Checked)
        {
            ApplyStrategyFilter();
        }
        else
        {
            UpdateSelectionCount();
        }
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        var name = _nameTextBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, "Il nome del workspace è obbligatorio.", "Salvataggio workspace",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameTextBox.Focus();
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            if (IsNew)
            {
                var created = await _context.Services.Api.CreateAsync(new CreateWorkspaceRequest
                {
                    Name = name,
                    StrategiesFilter = _selectedIds.ToList()
                });
                _workspaceId = created.Id;

                // Il selettore in alto è l'unico elenco dei workspace che resta a video: se non lo
                // si aggiorna, il workspace appena creato non esiste per nessuna schermata.
                await _context.Services.Workspaces.RefreshAsync();
                _context.Services.Workspaces.Select(created.Id);
                _context.Navigation.SetStatus(
                    $"Workspace '{created.Name}' creato con {created.StrategiesCount} strategie.");
                await LoadAsync(CancellationToken.None);
            }
            else
            {
                var saved = await _context.Services.Api.SaveMasterFilterAsync(_workspaceId!, new WorkspaceMasterFilter
                {
                    Name = string.IsNullOrWhiteSpace(_masterFilterName) ? name : _masterFilterName,
                    StrategiesFilter = _selectedIds.ToList()
                });
                _masterFilterName = saved.Name;
                SetDirty(false);

                // Il nome può essere cambiato: è quello che si legge nel selettore in alto.
                await _context.Services.Workspaces.RefreshAsync();
                _context.Navigation.SetStatus(
                    $"Masterfilter salvato: {saved.StrategiesFilter.Count} strategie abilitate.");
            }
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Errore di salvataggio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private async void OnRevertRequested(object? sender, EventArgs e)
    {
        if (IsNew)
        {
            _context?.Navigation.GoBack();
            return;
        }

        await LoadAsync(CancellationToken.None);
    }

    private void OnBackRequested(object? sender, EventArgs e)
    {
        if (_isDirty)
        {
            var confirm = MessageBox.Show(
                this,
                "Ci sono modifiche non salvate. Vuoi abbandonarle?",
                "Modifiche non salvate",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetDirty(false);
        }

        _context?.Navigation.GoBack();
    }
}
