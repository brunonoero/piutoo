using System.ComponentModel;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga gruppo/account modificabile: <see cref="TradingGroupRow"/> è init-only.</summary>
public sealed class PlanGroupEditRow
{
    public string GroupId { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public int MaxConcurrentTrades { get; set; }

    public string RotationSetupId { get; set; } = string.Empty;

    public string TitanoBacktestFolder { get; set; } = string.Empty;

    public bool ApplyTitanoFilters { get; set; } = true;
}

/// <summary>Riga strumento modificabile: <see cref="InstrumentMetadata"/> è init-only.</summary>
public sealed class PlanInstrumentEditRow
{
    public string Symbol { get; set; } = string.Empty;

    public decimal DollarsPerPoint { get; set; } = 1m;

    public decimal MinimumQuantity { get; set; } = 1m;

    public decimal QuantityStep { get; set; } = 1m;

    public QuantityRoundingMode RoundingMode { get; set; } = QuantityRoundingMode.FuturesContracts;
}

/// <summary>
/// Dettaglio di un piano di trading. Il salvataggio riscrive il piano intero, quindi la schermata
/// deve esporre tutto ciò che il piano contiene: quello che non è modificabile qui verrebbe
/// riportato ai default alla prima modifica.
/// </summary>
public partial class PlanDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    // Griglie editabili: restano BindingList<T> e non ordinabili per colonna, perché l'ordine è
    // quello in cui si sta scrivendo. Vedi .cursor/rules/piutoo-console-screens.mdc.
    private readonly BindingList<PlanGroupEditRow> _groups = new();
    private readonly BindingList<PlanInstrumentEditRow> _instruments = new();

    /// <summary>Registro globale (<c>api/Accounts</c>), non del workspace: si carica una volta.</summary>
    private readonly List<string> _accountGroups = new();
    private readonly List<WorkspaceAccount> _accounts = new();

    /// <summary>Liste condivise fra le combo del tab Generale e le colonne della griglia gruppi.</summary>
    private readonly List<ValueComboItem> _rotationSetups = new();
    private readonly List<ValueComboItem> _backtestFolders = new();
    private ShellContext? _context;
    private string _workspaceId = string.Empty;
    private string? _code;
    private bool _isNew;
    private bool _suspendDirtyTracking;
    private bool _isDirty;
    private bool _suspendWorkspaceEvents;
    private bool _suspendTitanoEvents;

    public PlanDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _groupsBindingSource.DataSource = _groups;
        _instrumentsBindingSource.DataSource = _instruments;
        _colRoundingMode.DataSource = Enum.GetValues<QuantityRoundingMode>();
        _enforceConcurrencyCombo.Items.AddRange(new object[]
        {
            "Default (come da storico)",
            "Sì, applica i limiti",
            "No, ignora i limiti"
        });
        _enforceConcurrencyCombo.SelectedIndex = 0;

        _groups.ListChanged += (_, _) => MarkDirty();
        _instruments.ListChanged += (_, _) => MarkDirty();
    }

    public string ScreenTitle => _isNew
        ? "Nuovo piano"
        : _code is { Length: > 0 } code ? $"Piano {code}" : "Piano";

    public bool HasUnsavedChanges => _isDirty;

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. <paramref name="code"/> null = nuovo piano.</summary>
    public void SetPlan(string workspaceId, string? code)
    {
        _workspaceId = workspaceId;
        _code = code;
        _isNew = string.IsNullOrEmpty(code);
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _suspendDirtyTracking = true;
        _toolbar.SetBusy(true);
        try
        {
            await LoadWorkspacesAsync(cancellationToken);
            await LoadAccountRegistryAsync(cancellationToken);
            if (_isNew)
            {
                _toolbar.Title = "Nuovo piano";
                _codeTextBox.ReadOnly = false;
                ResetToDefaults();
                await ApplyTitanoSelectionAsync(null, null, cancellationToken);
                _rotationStatusLabel.Text = "disponibile dopo il primo salvataggio";
                await RefreshGroupChoicesAsync(cancellationToken);
                _context.Navigation.SetStatus($"Nuovo piano nel workspace '{_workspaceId}'.");
                return;
            }

            var plan = await _context.Services.Plans.GetAsync(_workspaceId, _code!, cancellationToken);
            _toolbar.Title = $"Piano {plan.Code}";
            _codeTextBox.ReadOnly = true;
            Fill(plan);
            await ApplyTitanoSelectionAsync(plan.RotationSetupId, plan.TitanoBacktestFolder, cancellationToken);
            await RefreshGroupChoicesAsync(cancellationToken);
            await RefreshRotationStatusAsync(cancellationToken);
            _context.Navigation.SetStatus(
                $"Piano '{plan.Code}' con {plan.Groups.Count} righe gruppo/account, " +
                $"aggiornato il {plan.UpdatedUtc:yyyy-MM-dd HH:mm} UTC.");
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
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// Popola la combo dei workspace. È modificabile solo per un piano nuovo: un piano esistente
    /// vive in <c>&lt;workspace&gt;/plans/plans.json</c> e spostarlo non è una modifica di campo.
    /// </summary>
    private async Task LoadWorkspacesAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _suspendWorkspaceEvents = true;
        try
        {
            _workspaceCombo.Items.Clear();
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            var index = FindWorkspaceIndex(_workspaceId);
            if (index < 0 && _isNew && _workspaceCombo.Items.Count > 0)
            {
                index = 0;
            }

            _workspaceCombo.SelectedIndex = index;
            if (SelectedWorkspaceId is { } selected)
            {
                _workspaceId = selected;
            }
        }
        finally
        {
            _suspendWorkspaceEvents = false;
            // Il workspace di un piano esistente è parte della sua identità, non un campo editabile.
            _workspaceCombo.Enabled = _isNew;
        }
    }

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private int FindWorkspaceIndex(string? workspaceId)
    {
        if (string.IsNullOrEmpty(workspaceId))
        {
            return -1;
        }

        for (var index = 0; index < _workspaceCombo.Items.Count; index++)
        {
            if (_workspaceCombo.Items[index] is WorkspaceComboItem item
                && string.Equals(item.Info.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (_suspendWorkspaceEvents || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        _workspaceId = workspaceId;
        MarkDirty();
        _context?.Navigation.SetStatus($"Il piano verrà salvato nel workspace '{workspaceId}'.");

        // Cartelle di backtest vivono dentro il workspace: cambiato quello, la selezione Titano
        // precedente non è più risolvibile e va ricostruita dalla nuova lista.
        await ApplyTitanoSelectionAsync(SelectedId(_rotationSetupCombo), null, CancellationToken.None);
        await RefreshGroupChoicesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Ricostruisce le due combo Titano nell'ordine in cui dipendono l'una dall'altra: i setup sono
    /// globali, le cartelle appartengono al workspace. Il run non si sceglie più: è sempre l'ultimo
    /// generato per la cartella, mostrato in sola lettura da <see cref="RefreshRotationStatusAsync"/>.
    /// </summary>
    private async Task ApplyTitanoSelectionAsync(
        string? setupId,
        string? backtestFolder,
        CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _suspendTitanoEvents = true;
        try
        {
            await LoadRotationSetupsAsync(cancellationToken);
            SelectValue(_rotationSetupCombo, setupId);

            await LoadBacktestFoldersAsync(cancellationToken);
            SelectValue(_titanoFolderCombo, backtestFolder);
        }
        finally
        {
            _suspendTitanoEvents = false;
        }
    }

    /// <summary>
    /// Stato di freschezza dell'ultimo run per la cartella della riga primaria, in sola lettura: il
    /// run non è più un campo del piano, quindi non c'è nulla da selezionare, solo da mostrare.
    /// Richiede un piano già salvato (l'endpoint risolve per codice).
    /// </summary>
    private async Task RefreshRotationStatusAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _isNew || _code is not { Length: > 0 } code)
        {
            return;
        }

        try
        {
            var status = await _context.Services.Plans.GetRotationStatusAsync(_workspaceId, code, cancellationToken);
            _rotationStatusLabel.Text = status.Freshness switch
            {
                TitanoRotationFreshness.Fresh =>
                    $"🟢 pronto (ultimo run {status.LatestRunGeneratedAtUtc:yyyy-MM-dd HH:mm} UTC)",
                TitanoRotationFreshness.Stale =>
                    $"🟡 da aggiornare (ultimo run {status.LatestRunGeneratedAtUtc:yyyy-MM-dd HH:mm} UTC, periodo scaduto)",
                _ => "⚪ nessun run Titano per questa cartella"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _rotationStatusLabel.Text = $"non disponibile ({ex.Message})";
        }
    }

    private async Task LoadRotationSetupsAsync(CancellationToken cancellationToken)
    {
        _rotationSetups.Clear();
        _rotationSetupCombo.Items.Clear();
        _rotationSetupCombo.Items.Add(ValueComboItem.None("(nessun setup)"));
        try
        {
            var setups = await _context!.Services.Titano.ListSetupsAsync(cancellationToken);
            foreach (var setup in setups.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                _rotationSetups.Add(ValueComboItem.Of(setup.Id, $"{setup.Name}  ({setup.Id})"));
                _rotationSetupCombo.Items.Add(ValueComboItem.Of(setup.Id, $"{setup.Name}  ({setup.Id})"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Setup di rotazione non elencabili: {ex.Message}");
        }
    }

    private async Task LoadBacktestFoldersAsync(CancellationToken cancellationToken)
    {
        _backtestFolders.Clear();
        _titanoFolderCombo.Items.Clear();
        _titanoFolderCombo.Items.Add(ValueComboItem.None("(nessuna cartella)"));
        if (string.IsNullOrEmpty(_workspaceId))
        {
            return;
        }

        try
        {
            var backtests = await _context!.Services.Api.ListBacktestsAsync(_workspaceId, cancellationToken);
            foreach (var backtest in backtests.OrderByDescending(b => b.LastModifiedUtc))
            {
                var item = ValueComboItem.Of(
                    backtest.FolderName,
                    $"{backtest.FolderName}  ·  {BacktestComboItem.DescribeOrigin(backtest)}" +
                    $"  ·  {backtest.LastModifiedUtc:yyyy-MM-dd HH:mm} UTC");
                _backtestFolders.Add(item);
                _titanoFolderCombo.Items.Add(item);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Cartelle di backtest non elencabili: {ex.Message}");
        }
    }

    /// <summary>
    /// Gruppi e account stanno in un registro globale (<c>api/Accounts</c>), non nel workspace:
    /// una sola lettura serve tutte le righe del piano.
    /// </summary>
    private async Task LoadAccountRegistryAsync(CancellationToken cancellationToken)
    {
        _accountGroups.Clear();
        _accounts.Clear();
        try
        {
            var groups = await _context!.Services.Api.ListAccountGroupsAsync(cancellationToken);
            _accountGroups.AddRange(groups
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .OrderBy(group => group, StringComparer.OrdinalIgnoreCase));

            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
            _accounts.AddRange(accounts
                .Where(account => !string.IsNullOrWhiteSpace(account.AccountNumber))
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Registro gruppi/account non leggibile: {ex.Message}");
        }
    }

    private Task RefreshGroupChoicesAsync(CancellationToken cancellationToken)
    {
        RefreshGroupColumnItems();
        RefreshTitanoColumnItems();

        for (var index = 0; index < _groups.Count; index++)
        {
            RefreshAccountCell(index);
        }

        return Task.CompletedTask;
    }

    private void RefreshTitanoColumnItems()
    {
        SetColumnItems(
            _colGroupRotationSetup,
            "(nessun setup)",
            _rotationSetups,
            _groups.Select(row => row.RotationSetupId));

        SetColumnItems(
            _colGroupTitanoFolder,
            "(nessuna cartella)",
            _backtestFolders,
            _groups.Select(row => row.TitanoBacktestFolder));
    }

    private static void SetColumnItems(
        DataGridViewComboBoxColumn column,
        string blankLabel,
        IEnumerable<ValueComboItem> known,
        IEnumerable<string> usedValues)
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank(blankLabel) };
        items.AddRange(known);
        foreach (var value in usedValues)
        {
            if (!string.IsNullOrWhiteSpace(value) && !ContainsId(items, value))
            {
                items.Add(ValueComboItem.Missing(value));
            }
        }

        column.DisplayMember = nameof(ValueComboItem.Display);
        column.ValueMember = nameof(ValueComboItem.Id);
        column.DataSource = items;
    }

    private void RefreshGroupColumnItems()
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun gruppo)") };
        foreach (var group in _accountGroups)
        {
            items.Add(ValueComboItem.Of(group, group));
        }

        // I gruppi già scritti nel piano ma spariti dal registro restano selezionabili: il
        // salvataggio riscrive il piano intero, quindi scartarli qui li cancellerebbe.
        foreach (var row in _groups)
        {
            if (!string.IsNullOrWhiteSpace(row.GroupId) && !ContainsId(items, row.GroupId))
            {
                items.Add(ValueComboItem.Missing(row.GroupId));
            }
        }

        _colGroupId.DisplayMember = nameof(ValueComboItem.Display);
        _colGroupId.ValueMember = nameof(ValueComboItem.Id);
        _colGroupId.DataSource = items;
    }

    /// <summary>
    /// La lista account della riga dipende dal gruppo scelto sulla stessa riga, quindi vive sulla
    /// cella e non sulla colonna. Senza gruppo non si filtra nulla: mostrare zero account
    /// sembrerebbe un registro vuoto invece di una scelta ancora da fare.
    /// </summary>
    private void RefreshAccountCell(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _groups.Count || rowIndex >= _groupsGrid.Rows.Count)
        {
            return;
        }

        var row = _groups[rowIndex];
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun account)") };
        foreach (var account in _accounts.Where(account => BelongsToGroup(account, row.GroupId)))
        {
            items.Add(ValueComboItem.Of(
                account.AccountNumber,
                $"{account.Name}  ·  {account.AccountNumber}"));
        }

        if (!string.IsNullOrWhiteSpace(row.AccountNumber) && !ContainsId(items, row.AccountNumber))
        {
            items.Add(ValueComboItem.Missing(row.AccountNumber));
        }

        if (_groupsGrid.Rows[rowIndex].Cells[_colAccountNumber.Index] is not DataGridViewComboBoxCell cell)
        {
            return;
        }

        cell.DisplayMember = nameof(ValueComboItem.Display);
        cell.ValueMember = nameof(ValueComboItem.Id);
        cell.DataSource = items;
    }

    private static bool BelongsToGroup(WorkspaceAccount account, string groupId)
        => string.IsNullOrWhiteSpace(groupId)
           || string.Equals(account.GroupId, groupId.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool ContainsId(IEnumerable<ValueComboItem> items, string id)
        => items.Any(item => string.Equals(item.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Senza questo una combo di griglia notifica il cambio solo all'uscita dalla cella.</summary>
    private void OnGroupsGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_groupsGrid.IsCurrentCellDirty && _groupsGrid.CurrentCell is DataGridViewComboBoxCell)
        {
            _groupsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OnGroupsGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _groups.Count)
        {
            return;
        }

        var row = _groups[e.RowIndex];

        if (e.ColumnIndex == _colGroupId.Index)
        {
            // L'account precedente può appartenere a un altro gruppo: tenerlo produrrebbe una riga
            // che il registro contraddice, ed è ciò che il filtro serve a evitare.
            if (!string.IsNullOrWhiteSpace(row.AccountNumber)
                && _accounts.FirstOrDefault(account => string.Equals(
                    account.AccountNumber, row.AccountNumber, StringComparison.OrdinalIgnoreCase)) is { } known
                && !BelongsToGroup(known, row.GroupId))
            {
                row.AccountNumber = string.Empty;
                _groups.ResetItem(e.RowIndex);
            }

            RefreshAccountCell(e.RowIndex);
        }
    }

    /// <summary>
    /// Un valore fuori lista farebbe comparire un dialog di errore per cella. Le voci orfane sono
    /// già iniettate, quindi qui si segnala e basta.
    /// </summary>
    private void OnGroupsGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        _context?.Navigation.SetError($"Valore non valido nella griglia gruppi: {e.Exception.Message}");
    }

    private static string? SelectedId(ComboBox combo) => (combo.SelectedItem as ValueComboItem)?.Id;

    /// <summary>
    /// Seleziona <paramref name="id"/>; se non è fra le voci disponibili lo aggiunge come voce
    /// "non più presente" invece di perderlo, perché il salvataggio riscrive il piano intero.
    /// </summary>
    private static void SelectValue(ComboBox combo, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
            return;
        }

        var trimmed = id.Trim();
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (combo.Items[index] is ValueComboItem item
                && string.Equals(item.Id, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                return;
            }
        }

        combo.SelectedIndex = combo.Items.Add(ValueComboItem.Missing(trimmed));
    }

    private void OnTitanoFieldChanged(object? sender, EventArgs e)
    {
        if (!_suspendTitanoEvents)
        {
            MarkDirty();
        }
    }

    private void OnTitanoFolderChanged(object? sender, EventArgs e)
    {
        if (!_suspendTitanoEvents)
        {
            MarkDirty();
        }
    }

    private void ResetToDefaults()
    {
        _codeTextBox.Text = string.Empty;
        _nameTextBox.Text = string.Empty;
        _maxConcurrentInput.Value = 0;
        _commissionInput.Value = 2m;
        _applyTitanoCheckBox.Checked = false;
        _enforceConcurrencyCombo.SelectedIndex = 0;

        _clampMultipliersCheckBox.Checked = true;
        _volatilityEnabledCheckBox.Checked = false;
        _atrPeriodsInput.Value = 14;
        _targetRiskInput.Value = 1_000m;
        _portfolioRiskEnabledCheckBox.Checked = false;
        _maxDrawdownInput.Value = 0.20m;
        _maxGrossExposureInput.Value = 1m;
        _aggressiveModulesCheckBox.Checked = false;
        _fractionalFactorInput.Value = 0.25m;
        _maximumMultiplierInput.Value = 1m;

        _groups.Clear();
        _instruments.Clear();
    }

    private void Fill(TradingPlan plan)
    {
        _codeTextBox.Text = plan.Code;
        _nameTextBox.Text = plan.Name;
        _maxConcurrentInput.Value = plan.MaxConcurrentTrades;
        _commissionInput.Value = Clamp(_commissionInput, plan.CommissionPerContract);
        _applyTitanoCheckBox.Checked = plan.ApplyTitanoFilters;
        // Le tre combo Titano sono popolate a parte da ApplyTitanoSelectionAsync: le liste
        // arrivano dal server e Fill deve restare sincrona.
        _enforceConcurrencyCombo.SelectedIndex = plan.EnforceConcurrencyLimits switch
        {
            null => 0,
            true => 1,
            false => 2
        };

        var sizing = plan.PositionSizing;
        _clampMultipliersCheckBox.Checked = sizing.ClampMultipliersToUnitInterval;
        _volatilityEnabledCheckBox.Checked = sizing.MarketVolatility.Enabled;
        _atrPeriodsInput.Value = Math.Clamp(sizing.MarketVolatility.AtrPeriods, 1, 1000);
        _targetRiskInput.Value = Clamp(_targetRiskInput, sizing.MarketVolatility.TargetRiskDollars);
        _portfolioRiskEnabledCheckBox.Checked = sizing.PortfolioRisk.Enabled;
        _maxDrawdownInput.Value = Clamp(_maxDrawdownInput, sizing.PortfolioRisk.MaximumDrawdown);
        _maxGrossExposureInput.Value = Clamp(_maxGrossExposureInput, sizing.PortfolioRisk.MaximumGrossExposure);
        _aggressiveModulesCheckBox.Checked = sizing.PortfolioRisk.EnableAggressiveModules;
        _fractionalFactorInput.Value = Clamp(_fractionalFactorInput, sizing.PortfolioRisk.FractionalFactor);
        _maximumMultiplierInput.Value = Clamp(_maximumMultiplierInput, sizing.PortfolioRisk.MaximumMultiplier);

        _groups.RaiseListChangedEvents = false;
        _groups.Clear();
        foreach (var group in plan.Groups)
        {
            _groups.Add(new PlanGroupEditRow
            {
                GroupId = group.GroupId,
                AccountNumber = group.AccountNumber,
                MaxConcurrentTrades = group.MaxConcurrentTrades,
                RotationSetupId = group.RotationSetupId ?? string.Empty,
                TitanoBacktestFolder = group.TitanoBacktestFolder ?? string.Empty,
                ApplyTitanoFilters = group.ApplyTitanoFilters
            });
        }

        _groups.RaiseListChangedEvents = true;
        _groups.ResetBindings();

        _instruments.RaiseListChangedEvents = false;
        _instruments.Clear();
        foreach (var instrument in plan.Instruments)
        {
            _instruments.Add(new PlanInstrumentEditRow
            {
                Symbol = instrument.Symbol,
                DollarsPerPoint = instrument.DollarsPerPoint,
                MinimumQuantity = instrument.MinimumQuantity,
                QuantityStep = instrument.QuantityStep,
                RoundingMode = instrument.RoundingMode
            });
        }

        _instruments.RaiseListChangedEvents = true;
        _instruments.ResetBindings();
    }

    private static decimal Clamp(NumericUpDown input, decimal value)
        => Math.Clamp(value, input.Minimum, input.Maximum);

    private void MarkDirty()
    {
        if (!_suspendDirtyTracking)
        {
            SetDirty(true);
        }
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        _toolbar.SetDirty(dirty);
    }

    private void OnFieldChanged(object? sender, EventArgs e) => MarkDirty();

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnAddGroupClick(object? sender, EventArgs e)
    {
        // Gruppo e account si scelgono dalle combo: un placeholder testuale finirebbe nel piano
        // come gruppo inesistente.
        _groups.Add(new PlanGroupEditRow { GroupId = string.Empty, AccountNumber = string.Empty });
        RefreshAccountCell(_groups.Count - 1);
    }

    private void OnRemoveGroupClick(object? sender, EventArgs e)
    {
        if (_groupsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _groups.Count)
        {
            _groups.RemoveAt(index);
        }
    }

    private void OnAddInstrumentClick(object? sender, EventArgs e)
        => _instruments.Add(new PlanInstrumentEditRow());

    /// <summary>
    /// Precarica una riga per ogni simbolo del masterfilter del workspace. La lista strumenti del
    /// piano è fatta di override: i simboli assenti ricevono comunque i default della sessione
    /// (<c>DollarsPerPoint = 1</c> e passo 1), che su un future sono quasi sempre sbagliati e
    /// falsano il sizing senza segnalare nulla. Meglio vederli tutti e correggere quelli che
    /// servono, che doverli indovinare a memoria.
    /// </summary>
    private async void OnImportSymbolsClick(object? sender, EventArgs e)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var filter = await _context.Services.Api.GetMasterFilterAsync(workspaceId);
            var catalog = await _context.Services.Api.ListStrategiesAsync();
            var byId = catalog.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

            var symbols = filter.StrategiesFilter
                .Select(id => byId.TryGetValue(id, out var item) ? item.Symbol : null)
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (symbols.Count == 0)
            {
                _context.Navigation.SetStatus(
                    $"Il masterfilter di '{workspaceId}' non contiene strategie con un simbolo risolvibile.");
                return;
            }

            // Il server normalizza i simboli (trim, via la '@', maiuscolo) prima di cercarli, quindi
            // il confronto qui deve fare lo stesso o si aggiungerebbero doppioni apparenti.
            var existing = _instruments
                .Select(row => NormalizeSymbol(row.Symbol))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var symbol in symbols.Where(symbol => !existing.Contains(NormalizeSymbol(symbol))))
            {
                _instruments.Add(new PlanInstrumentEditRow { Symbol = symbol });
                added++;
            }

            _context.Navigation.SetStatus(added == 0
                ? $"Tutti i {symbols.Count} simboli del masterfilter sono già in tabella."
                : $"Aggiunti {added} simboli su {symbols.Count} dal masterfilter di '{workspaceId}'. " +
                  "Correggi DollarsPerPoint: il default 1 vale solo per uno strumento da 1$ a punto.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError($"Import simboli non riuscito: {ex.Message}");
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private static string NormalizeSymbol(string value) => value.Trim().TrimStart('@').ToUpperInvariant();

    private void OnRemoveInstrumentClick(object? sender, EventArgs e)
    {
        if (_instrumentsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _instruments.Count)
        {
            _instruments.RemoveAt(index);
        }
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (SelectedWorkspaceId is not { } targetWorkspaceId)
        {
            MessageBox.Show(this, "Seleziona il workspace in cui salvare il piano.", "Piano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var code = _codeTextBox.Text.Trim();
        if (code.Length == 0)
        {
            MessageBox.Show(this, "Il codice del piano è obbligatorio.", "Piano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var validGroups = _groups
            .Where(row => !string.IsNullOrWhiteSpace(row.GroupId) && !string.IsNullOrWhiteSpace(row.AccountNumber))
            .ToList();
        if (validGroups.Count == 0)
        {
            MessageBox.Show(
                this,
                "Serve almeno una riga gruppo/account completa: è la configurazione canonica del piano.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var request = new SaveTradingPlanRequest
        {
            Code = code,
            Name = _nameTextBox.Text.Trim() is { Length: > 0 } name ? name : code,
            Groups = validGroups.Select(row => new TradingGroupRow
            {
                GroupId = row.GroupId.Trim(),
                AccountNumber = row.AccountNumber.Trim(),
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                RotationSetupId = NullIfEmpty(row.RotationSetupId),
                TitanoBacktestFolder = NullIfEmpty(row.TitanoBacktestFolder),
                ApplyTitanoFilters = row.ApplyTitanoFilters
            }).ToList(),
            MaxConcurrentTrades = (int)_maxConcurrentInput.Value,
            RotationSetupId = SelectedId(_rotationSetupCombo),
            TitanoBacktestFolder = SelectedId(_titanoFolderCombo),
            ApplyTitanoFilters = _applyTitanoCheckBox.Checked,
            EnforceConcurrencyLimits = _enforceConcurrencyCombo.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            },
            CommissionPerContract = _commissionInput.Value,
            PositionSizing = new PositionSizingConfig
            {
                ClampMultipliersToUnitInterval = _clampMultipliersCheckBox.Checked,
                MarketVolatility = new MarketVolatilitySizingConfig
                {
                    Enabled = _volatilityEnabledCheckBox.Checked,
                    AtrPeriods = (int)_atrPeriodsInput.Value,
                    TargetRiskDollars = _targetRiskInput.Value
                },
                PortfolioRisk = new PortfolioRiskSizingConfig
                {
                    Enabled = _portfolioRiskEnabledCheckBox.Checked,
                    MaximumDrawdown = _maxDrawdownInput.Value,
                    MaximumGrossExposure = _maxGrossExposureInput.Value,
                    EnableAggressiveModules = _aggressiveModulesCheckBox.Checked,
                    FractionalFactor = _fractionalFactorInput.Value,
                    MaximumMultiplier = _maximumMultiplierInput.Value
                }
            },
            Instruments = _instruments
                .Where(row => !string.IsNullOrWhiteSpace(row.Symbol))
                .Select(row => new InstrumentMetadata
                {
                    Symbol = row.Symbol.Trim(),
                    DollarsPerPoint = row.DollarsPerPoint,
                    MinimumQuantity = row.MinimumQuantity,
                    QuantityStep = row.QuantityStep,
                    RoundingMode = row.RoundingMode
                }).ToList()
        };

        _toolbar.SetBusy(true);
        try
        {
            var saved = await _context.Services.Plans.SaveAsync(targetWorkspaceId, request);
            _workspaceId = targetWorkspaceId;
            _code = saved.Code;
            _isNew = false;
            _codeTextBox.ReadOnly = true;
            _workspaceCombo.Enabled = false;
            _suspendDirtyTracking = true;
            Fill(saved);
            await ApplyTitanoSelectionAsync(saved.RotationSetupId, saved.TitanoBacktestFolder, CancellationToken.None);
            await RefreshGroupChoicesAsync(CancellationToken.None);
            await RefreshRotationStatusAsync(CancellationToken.None);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.Title = $"Piano {saved.Code}";
            _context.Navigation.SetStatus($"Piano '{saved.Code}' salvato nel workspace '{targetWorkspaceId}'.");
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

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
