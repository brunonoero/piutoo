using System.Text.Json;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Creazione e gestione di una sessione di trading live o simulata. I piani di trading restano
/// nella console legacy: qui si configura direttamente workspace, Titano e gruppi account.
/// </summary>
public partial class TradingSessionsScreen : UserControl, IShellScreen
{
    private ShellContext? _context;
    private TradingSessionDescriptor? _activeSession;
    private IReadOnlyList<WorkspaceAccount> _accounts = [];
    private IReadOnlyList<string> _accountGroups = [];
    private List<TitanoSetupInfo> _titanoSetups = [];
    private bool _suspendReload;
    private bool _isBusy;

    public TradingSessionsScreen()
    {
        InitializeComponent();
        _modeCombo.Items.AddRange(Enum.GetNames<ExecutionMode>());
        _modeCombo.SelectedItem = nameof(ExecutionMode.ServerSimulated);
        _titanoModeCombo.Items.AddRange(Enum.GetNames<TitanoFilterMode>());
        _titanoModeCombo.SelectedItem = nameof(TitanoFilterMode.Disabled);
        _titanoRunCombo.DropDownStyle = ComboBoxStyle.DropDown;
        ConfigureGroupsGrid();
        UpdateSessionControls();
    }

    public string ScreenTitle => "Sessioni di trading";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _isBusy)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var previousWorkspace = SelectedWorkspaceId;
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);

            _suspendReload = true;
            _workspaceCombo.Items.Clear();
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            _workspaceCombo.SelectedIndex = FindWorkspaceIndex(previousWorkspace) is var index && index >= 0
                ? index
                : _workspaceCombo.Items.Count > 0 ? 0 : -1;

            _accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
            _accountGroups = await _context.Services.Api.ListAccountGroupsAsync(cancellationToken);
            _titanoSetups = await _context.Services.Titano.ListSetupsAsync(cancellationToken);
            RefreshGroupColumnSources();

            _suspendReload = false;
            await ReloadBacktestsAsync(cancellationToken);
            _context.Navigation.SetStatus($"{workspaces.Count} workspace disponibili.");
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
            _suspendReload = false;
            SetBusy(false);
        }
    }

    private sealed class AccountNumberListItem
    {
        public AccountNumberListItem(WorkspaceAccount account)
        {
            AccountNumber = account.AccountNumber;
            DisplayText = $"{account.Name} · {account.AccountNumber}";
        }

        public string AccountNumber { get; }
        public string DisplayText { get; }
    }

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private string? SelectedBacktestFolder => (_titanoBacktestCombo.SelectedItem as BacktestComboItem)?.Info.FolderName;

    private TitanoFilterMode SelectedTitanoMode =>
        Enum.TryParse<TitanoFilterMode>(_titanoModeCombo.SelectedItem?.ToString(), out var mode)
            ? mode
            : TitanoFilterMode.Disabled;

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

    private void ConfigureGroupsGrid()
    {
        _groupsGrid.AutoGenerateColumns = false;
        _groupsGrid.AllowUserToAddRows = true;
        _groupsGrid.AllowUserToDeleteRows = true;
        _groupsGrid.RowHeadersVisible = false;
        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "GroupId",
            HeaderText = "Codice gruppo",
            FillWeight = 20,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "RotationSetupId",
            HeaderText = "Setup Titano",
            FillWeight = 25,
            DisplayMember = nameof(TitanoSetupInfo.Name),
            ValueMember = nameof(TitanoSetupInfo.Id),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "AccountNumber",
            HeaderText = "Codice account",
            FillWeight = 20,
            DisplayMember = nameof(AccountNumberListItem.DisplayText),
            ValueMember = nameof(AccountNumberListItem.AccountNumber),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _groupsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MaxConcurrentTrades",
            HeaderText = "Max trade contemporanei",
            FillWeight = 15
        });
        _groupsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "ApplyTitanoFilters",
            HeaderText = "Applica Titano",
            FillWeight = 12,
            TrueValue = true,
            FalseValue = false
        });
        _groupsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TitanoRunId",
            HeaderText = "Run Titano",
            FillWeight = 23,
            ReadOnly = true
        });
        _groupsGrid.DataError += (_, e) => e.ThrowException = false;
        _groupsGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_groupsGrid.IsCurrentCellDirty)
            {
                _groupsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _groupsGrid.CellValueChanged += OnGroupCellValueChanged;
    }

    private void RefreshGroupColumnSources()
    {
        if (_groupsGrid.Columns["GroupId"] is DataGridViewComboBoxColumn groupColumn)
        {
            groupColumn.DataSource = _accountGroups.ToList();
        }

        if (_groupsGrid.Columns["RotationSetupId"] is DataGridViewComboBoxColumn setupColumn)
        {
            setupColumn.DataSource = _titanoSetups.ToList();
        }

        if (_groupsGrid.Columns["AccountNumber"] is DataGridViewComboBoxColumn accountColumn)
        {
            accountColumn.DataSource = _accounts
                .Where(account => account.Enabled && !string.IsNullOrWhiteSpace(account.AccountNumber))
                .Select(account => new AccountNumberListItem(account))
                .ToList();
        }
    }

    private void OnGroupCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _groupsGrid.Columns[e.ColumnIndex].Name != "AccountNumber")
        {
            return;
        }

        var row = _groupsGrid.Rows[e.RowIndex];
        var accountNumber = Convert.ToString(row.Cells["AccountNumber"].Value);
        var account = _accounts.FirstOrDefault(item =>
            item.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
        if (account is not null && !string.IsNullOrWhiteSpace(account.GroupId))
        {
            row.Cells["GroupId"].Value = account.GroupId;
        }
    }

    private void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _isBusy = busy;
        _createButton.Enabled = !busy;
        _startButton.Enabled = !busy && _activeSession != null;
        _stopButton.Enabled = !busy && _activeSession != null;
        _resumeButton.Enabled = !busy && _activeSession != null;
        _snapshotButton.Enabled = !busy && _activeSession != null;
        _loadRunsButton.Enabled = !busy;
        _saveGroupsButton.Enabled = !busy && _activeSession != null;
        _reloadGroupsButton.Enabled = !busy && _activeSession != null;
        _addGroupRowButton.Enabled = !busy;
        _removeGroupRowButton.Enabled = !busy;
        _configGroup.Enabled = !busy && _activeSession == null;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private void UpdateSessionControls()
    {
        if (_activeSession == null)
        {
            _sessionIdLabel.Text = "—";
            _sessionStatusLabel.Text = "—";
            _sessionTokenLabel.Text = "—";
        }
        else
        {
            _sessionIdLabel.Text = _activeSession.SessionId;
            _sessionStatusLabel.Text = _activeSession.Status.ToString();
            var token = _activeSession.SessionToken;
            _sessionTokenLabel.Text = token.Length <= 12
                ? token
                : $"{token[..8]}…{token[^4..]}";
        }

        SetBusy(_isBusy);
    }

    private async Task ReloadBacktestsAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            _titanoBacktestCombo.Items.Clear();
            return;
        }

        var selectedFolder = SelectedBacktestFolder;
        var backtests = await _context.Services.Api.ListBacktestsAsync(workspaceId, cancellationToken);

        _suspendReload = true;
        _titanoBacktestCombo.Items.Clear();
        foreach (var backtest in backtests
                     .Where(backtest => backtest.HasResults)
                     .OrderByDescending(backtest => backtest.LastModifiedUtc))
        {
            _titanoBacktestCombo.Items.Add(new BacktestComboItem(backtest));
        }

        var restored = -1;
        for (var index = 0; index < _titanoBacktestCombo.Items.Count; index++)
        {
            if (_titanoBacktestCombo.Items[index] is BacktestComboItem item
                && string.Equals(item.Info.FolderName, selectedFolder, StringComparison.OrdinalIgnoreCase))
            {
                restored = index;
                break;
            }
        }

        _titanoBacktestCombo.SelectedIndex = restored >= 0
            ? restored
            : _titanoBacktestCombo.Items.Count > 0 ? 0 : -1;
        _suspendReload = false;

        if (_titanoBacktestCombo.SelectedIndex >= 0)
        {
            await LoadTitanoRunsAsync(showValidationError: false, cancellationToken);
        }
        else
        {
            _titanoRunCombo.Items.Clear();
            _titanoRunCombo.Text = string.Empty;
        }
    }

    private async Task LoadTitanoRunsAsync(bool showValidationError, CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            if (showValidationError)
            {
                MessageBox.Show(this, "Seleziona un workspace.", "Sessioni di trading",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedBacktestFolder))
        {
            if (showValidationError)
            {
                MessageBox.Show(this, "Seleziona un backtest sorgente.", "Sessioni di trading",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return;
        }

        try
        {
            var runs = await _context.Services.Titano.ListRunsAsync(
                workspaceId, SelectedBacktestFolder, cancellationToken);
            var previous = _titanoRunCombo.Text.Trim();
            _titanoRunCombo.Items.Clear();
            foreach (var run in runs)
            {
                _titanoRunCombo.Items.Add(run.RunId);
            }

            if (runs.Count == 0)
            {
                _titanoRunCombo.Text = string.Empty;
                _context.Navigation.SetStatus(
                    $"Nessuna rotazione Titano in '{SelectedBacktestFolder}'. Generane una dalla schermata Titano.");
                return;
            }

            _titanoRunCombo.Text = runs.Any(run => run.RunId == previous)
                ? previous
                : runs[0].RunId;
            _context.Navigation.SetStatus($"{runs.Count} rotazioni Titano disponibili.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Run Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? SelectedTitanoRunId =>
        string.IsNullOrWhiteSpace(_titanoRunCombo.Text) ? null : _titanoRunCombo.Text.Trim();

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (_context == null || _suspendReload)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await ReloadBacktestsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnBacktestChanged(object? sender, EventArgs e)
    {
        if (_context == null || _suspendReload)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await LoadTitanoRunsAsync(showValidationError: false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnLoadRunsClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await LoadTitanoRunsAsync(showValidationError: true, CancellationToken.None);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCreateClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (_workspaceCombo.SelectedItem is not WorkspaceComboItem workspace)
        {
            MessageBox.Show(this, "Seleziona un workspace.", "Sessioni di trading",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var titanoRunId = SelectedTitanoRunId;
        var titanoBacktestFolder = SelectedBacktestFolder;
        if (titanoRunId is not null && string.IsNullOrWhiteSpace(titanoBacktestFolder))
        {
            MessageBox.Show(this,
                "Un setup Titano richiede anche la cartella del backtest sorgente.",
                "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var titanoMode = SelectedTitanoMode;
        if (titanoMode != TitanoFilterMode.Disabled && titanoRunId is null)
        {
            MessageBox.Show(this,
                $"La modalità {titanoMode} richiede un setup Titano: seleziona una rotazione " +
                "oppure passa alla modalità Disabled.",
                "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspace.Info.Id);
            if (masterFilter.StrategiesFilter.Count == 0)
            {
                MessageBox.Show(this,
                    "Il masterfilter del workspace è vuoto: non c'è nessuna strategia da valutare.",
                    "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var request = new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Info.Id,
                ExecutionMode = Enum.Parse<ExecutionMode>(
                    _modeCombo.SelectedItem?.ToString() ?? nameof(ExecutionMode.ServerSimulated)),
                TitanoRunId = titanoRunId,
                TitanoBacktestFolder = titanoRunId is null ? null : titanoBacktestFolder,
                TitanoMode = titanoMode,
                Instruments = [],
                PositionSizing = new PositionSizingConfig
                {
                    PortfolioRisk = new PortfolioRiskSizingConfig
                    {
                        Enabled = _cppiEnabledCheckBox.Checked,
                        EnableCppi = _cppiEnabledCheckBox.Checked,
                        CppiFloorFraction = _cppiFloorInput.Value / 100m,
                        CppiMultiplier = _cppiMultiplierInput.Value,
                        EnableAggressiveModules = false,
                        MaximumMultiplier = 1m
                    }
                }
            };

            _activeSession = await _context.Services.Sessions.CreateAsync(request);
            UpdateSessionControls();
            ShowDescriptor("Sessione creata.");
            _context.Navigation.SetStatus($"Sessione {_activeSession.SessionId} creata.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Errore sessione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnStartClick(object? sender, EventArgs e) => await SetStatusAsync("start");

    private async void OnStopClick(object? sender, EventArgs e) => await SetStatusAsync("stop");

    private async void OnResumeClick(object? sender, EventArgs e) => await SetStatusAsync("resume");

    private async Task SetStatusAsync(string action)
    {
        if (_context == null || _activeSession is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            _activeSession = await _context.Services.Sessions.SetStatusAsync(
                _activeSession.SessionId, _activeSession.SessionToken, action);
            UpdateSessionControls();
            ShowDescriptor($"Stato aggiornato: {_activeSession.Status}.");
            _context.Navigation.SetStatus($"Sessione {_activeSession.Status}.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Sessione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSnapshotClick(object? sender, EventArgs e)
    {
        if (_context == null || _activeSession is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var snapshot = await _context.Services.Sessions.GetSnapshotAsync(
                _activeSession.SessionId, _activeSession.SessionToken);
            ShowSnapshot(snapshot);
            _context.Navigation.SetStatus("Snapshot aggiornato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Snapshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowDescriptor(string message)
    {
        if (_context == null || _activeSession is null)
        {
            _snapshotTextBox.Text = message;
            return;
        }

        _snapshotTextBox.Text = $"{message}{Environment.NewLine}{Environment.NewLine}" +
            JsonSerializer.Serialize(_activeSession, new JsonSerializerOptions(_context.Services.JsonOptions)
            {
                WriteIndented = true
            });
    }

    private void ShowSnapshot(TradingSessionSnapshot snapshot)
    {
        _balanceLabel.Text = snapshot.Balance.ToString("N2");
        _equityLabel.Text = snapshot.Equity.ToString("N2");
        _positionsLabel.Text = snapshot.Positions.Count.ToString();
        _pendingLabel.Text = snapshot.PendingIntents.Count.ToString();

        if (_context == null)
        {
            return;
        }

        _snapshotTextBox.Text = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(_context.Services.JsonOptions)
        {
            WriteIndented = true
        });
        _mainTabControl.SelectedTab = _snapshotTab;
    }

    private void OnAddGroupRowClick(object? sender, EventArgs e) => _groupsGrid.Rows.Add();

    private void OnRemoveGroupRowClick(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in _groupsGrid.SelectedRows.Cast<DataGridViewRow>().ToList())
        {
            if (!row.IsNewRow)
            {
                _groupsGrid.Rows.Remove(row);
            }
        }
    }

    private async void OnSaveGroupsClick(object? sender, EventArgs e)
    {
        if (_context == null || _activeSession is null)
        {
            MessageBox.Show(this, "Crea prima una sessione ExternalBroker.", "Gruppi account",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_activeSession.ExecutionMode != ExecutionMode.ExternalBroker)
        {
            MessageBox.Show(this,
                "I gruppi account si configurano solo in modalità ExternalBroker.",
                "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var rows = ReadTradingGroupRows();
            var snapshot = await _context.Services.Sessions.SetGroupsAsync(
                _activeSession.SessionId, _activeSession.SessionToken, rows);
            ShowSnapshot(snapshot);
            _context.Navigation.SetStatus($"Gruppi salvati ({rows.Count} righe).");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnReloadGroupsClick(object? sender, EventArgs e)
    {
        if (_context == null || _activeSession is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var rows = await _context.Services.Sessions.GetGroupsAsync(
                _activeSession.SessionId, _activeSession.SessionToken);
            _groupsGrid.Rows.Clear();
            foreach (var mapping in rows)
            {
                _groupsGrid.Rows.Add(
                    mapping.GroupId,
                    mapping.RotationSetupId,
                    mapping.AccountNumber,
                    mapping.MaxConcurrentTrades,
                    mapping.ApplyTitanoFilters,
                    mapping.TitanoRunId);
            }

            _context.Navigation.SetStatus($"Caricati {rows.Count} gruppi account.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private List<TradingGroupRow> ReadTradingGroupRows()
    {
        var backtestFolder = SelectedBacktestFolder;
        var rows = _groupsGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => new TradingGroupRow
            {
                GroupId = Convert.ToString(row.Cells["GroupId"].Value ?? string.Empty)!.Trim(),
                RotationSetupId = Convert.ToString(row.Cells["RotationSetupId"].Value ?? string.Empty)!.Trim() is { Length: > 0 } setupId
                    ? setupId
                    : null,
                AccountNumber = Convert.ToString(row.Cells["AccountNumber"].Value ?? string.Empty)!.Trim(),
                MaxConcurrentTrades = ParseMaxConcurrentTrades(row),
                ApplyTitanoFilters = row.Cells["ApplyTitanoFilters"].Value is not false,
                TitanoRunId = Convert.ToString(row.Cells["TitanoRunId"].Value ?? string.Empty)!.Trim() is { Length: > 0 } runId
                    ? runId
                    : null,
                TitanoBacktestFolder = backtestFolder
            })
            .Where(row => row.AccountNumber.Length > 0 || row.GroupId.Length > 0)
            .ToList();

        if (rows.Any(row => row.AccountNumber.Length == 0 || row.GroupId.Length == 0))
        {
            throw new InvalidOperationException("Ogni riga deve contenere codice gruppo e codice account.");
        }

        return rows;
    }

    private static int ParseMaxConcurrentTrades(DataGridViewRow row)
    {
        var raw = Convert.ToString(row.Cells["MaxConcurrentTrades"].Value ?? string.Empty)?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return 0;
        }

        if (!int.TryParse(raw, out var value) || value < 0)
        {
            throw new InvalidOperationException(
                "Max trade contemporanei deve essere un intero maggiore o uguale a zero.");
        }

        return value;
    }
}
