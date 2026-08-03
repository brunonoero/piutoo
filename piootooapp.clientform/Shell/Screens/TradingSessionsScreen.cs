using System.Text.Json;

using Piootoo.Shared.Models.Optimization;

using Piootoo.Shared.Models.Trading;

using Piootoo.Shared.Models.Workspaces;



namespace piootooapp.clientform.Shell.Screens;



/// <summary>

/// Creazione e gestione di sessioni di trading. Le strategie valutate arrivano sempre dal

/// masterfilter del workspace — il piano non limita l'universo, ma decide Titano, gruppi e

/// concorrenza per account.

/// </summary>

public partial class TradingSessionsScreen : UserControl, IShellScreen

{

    private const string CreationManual = "Creazione manuale";

    private const string CreationFromPlan = "Apri da piano";



    private ShellContext? _context;

    private TradingSessionDescriptor? _activeSession;

    private IReadOnlyList<WorkspaceAccount> _accounts = [];

    private IReadOnlyList<string> _accountGroups = [];

    private List<TitanoSetupInfo> _titanoSetups = [];

    private List<TradingPlan> _plans = [];

    private bool _suspendReload;

    private bool _isBusy;



    public TradingSessionsScreen()

    {

        InitializeComponent();

        _creationSourceCombo.Items.AddRange([CreationManual, CreationFromPlan]);

        _creationSourceCombo.SelectedIndex = 0;

        _clientRunModeCombo.Items.AddRange(Enum.GetNames<ClientRunMode>());

        _clientRunModeCombo.SelectedItem = nameof(ClientRunMode.Unknown);

        _modeCombo.Items.AddRange(Enum.GetNames<ExecutionMode>());

        _modeCombo.SelectedItem = nameof(ExecutionMode.ServerSimulated);

        _titanoModeCombo.Items.AddRange(Enum.GetNames<TitanoFilterMode>());

        _titanoModeCombo.SelectedItem = nameof(TitanoFilterMode.Disabled);

        _titanoRunCombo.DropDownStyle = ComboBoxStyle.DropDown;

        ConfigureGroupsGrid();

        ConfigureTooltips();

        UpdateCreationModeControls();

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

            RefreshOpenPlanAccountCombo(null);



            _suspendReload = false;

            await ReloadPlansAsync(cancellationToken);

            await ReloadBacktestsAsync(cancellationToken);

            await RefreshMasterfilterInfoAsync(cancellationToken);

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



        public AccountNumberListItem(string accountNumber, string displayText)

        {

            AccountNumber = accountNumber;

            DisplayText = displayText;

        }



        public string AccountNumber { get; }

        public string DisplayText { get; }

    }



    private sealed class PlanComboItem

    {

        public PlanComboItem(TradingPlan plan) => Plan = plan;



        public TradingPlan Plan { get; }



        public override string ToString() => $"{Plan.Code} — {Plan.Name}";

    }



    private bool IsFromPlan => _creationSourceCombo.SelectedItem?.ToString() == CreationFromPlan;



    private string? SelectedWorkspaceId => IsFromPlan

        ? SelectedPlan?.WorkspaceId

        : (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;



    private TradingPlan? SelectedPlan => (_planCombo.SelectedItem as PlanComboItem)?.Plan;



    private string? SelectedBacktestFolder => (_titanoBacktestCombo.SelectedItem as BacktestComboItem)?.Info.FolderName;



    private ClientRunMode SelectedClientRunMode =>

        Enum.TryParse<ClientRunMode>(_clientRunModeCombo.SelectedItem?.ToString(), out var mode)

            ? mode

            : ClientRunMode.Unknown;



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



    private void ConfigureTooltips()

    {

        _screenToolTip.SetToolTip(_creationSourceCombo,

            "Manuale: configura workspace, Titano e gruppi come la console legacy. " +

            "Da piano: il server estrae workspace e snapshot dal piano (come il cBot).");

        _screenToolTip.SetToolTip(_clientRunModeCombo,

            "Contesto dichiarato dal client. Backtest/Realtime abilitano la validazione incrociata " +

            "con il filtro Titano; Unknown lascia la responsabilità a chi configura.");

        _screenToolTip.SetToolTip(_titanoModeCombo,

            "Disabled: tutte le strategie del masterfilter. BacktestRotationFile: rotazione offline " +

            "per barra. Realtime: periodo corrente dell'ultima analisi.");

        _screenToolTip.SetToolTip(_distributeCheckBox,

            "Attivo = PiootooLiveTradingBot reclama i segnali per account. Disattivo = esecuzione diretta " +

            "come PiootooTradingSessionBot (intent già assegnati su POST /bars).");

        _screenToolTip.SetToolTip(_groupsGrid,

            "Solo ExternalBroker: MaxConcurrentTrades e profilo Titano per gruppo/account " +

            "(claim multi-account).");

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

            FillWeight = 16,

            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton

        });

        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn

        {

            Name = "RotationSetupId",

            HeaderText = "Setup Titano",

            FillWeight = 18,

            DisplayMember = nameof(TitanoSetupInfo.Name),

            ValueMember = nameof(TitanoSetupInfo.Id),

            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton

        });

        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn

        {

            Name = "AccountNumber",

            HeaderText = "Codice account",

            FillWeight = 16,

            DisplayMember = nameof(AccountNumberListItem.DisplayText),

            ValueMember = nameof(AccountNumberListItem.AccountNumber),

            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton

        });

        _groupsGrid.Columns.Add(new DataGridViewTextBoxColumn

        {

            Name = "MaxConcurrentTrades",

            HeaderText = "Max trade contemporanei",

            FillWeight = 12

        });

        _groupsGrid.Columns.Add(new DataGridViewCheckBoxColumn

        {

            Name = "ApplyTitanoFilters",

            HeaderText = "Applica Titano",

            FillWeight = 10,

            TrueValue = true,

            FalseValue = false

        });

        _groupsGrid.Columns.Add(new DataGridViewTextBoxColumn

        {

            Name = "TitanoRunId",

            HeaderText = "Run Titano",

            FillWeight = 16

        });

        _groupsGrid.Columns.Add(new DataGridViewTextBoxColumn

        {

            Name = "TitanoBacktestFolder",

            HeaderText = "Cartella backtest",

            FillWeight = 16

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



    private void RefreshOpenPlanAccountCombo(TradingPlan? plan)

    {

        var previous = (_openPlanAccountCombo.SelectedItem as AccountNumberListItem)?.AccountNumber;

        _openPlanAccountCombo.Items.Clear();

        var rows = plan?.Groups.Count > 0

            ? plan.Groups

            : plan is null

                ? []

                :

                [

                    new TradingGroupRow

                    {

                        GroupId = plan.GroupId,

                        AccountNumber = plan.AccountNumber,

                        MaxConcurrentTrades = plan.MaxConcurrentTrades,

                        RotationSetupId = plan.RotationSetupId,

                        TitanoRunId = plan.TitanoRunId,

                        TitanoBacktestFolder = plan.TitanoBacktestFolder,

                        ApplyTitanoFilters = plan.ApplyTitanoFilters

                    }

                ];



        foreach (var row in rows.Where(row => !string.IsNullOrWhiteSpace(row.AccountNumber)))

        {

            _openPlanAccountCombo.Items.Add(new AccountNumberListItem(

                row.AccountNumber,

                $"{row.AccountNumber} · gruppo {row.GroupId}"));

        }



        if (_openPlanAccountCombo.Items.Count == 0)

        {

            _openPlanAccountCombo.SelectedIndex = -1;

            return;

        }



        var restored = -1;

        for (var index = 0; index < _openPlanAccountCombo.Items.Count; index++)

        {

            if (_openPlanAccountCombo.Items[index] is AccountNumberListItem item

                && string.Equals(item.AccountNumber, previous, StringComparison.OrdinalIgnoreCase))

            {

                restored = index;

                break;

            }

        }



        _openPlanAccountCombo.SelectedIndex = restored >= 0 ? restored : 0;

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



    private void UpdateCreationModeControls()

    {

        var fromPlan = IsFromPlan;

        _planCombo.Enabled = fromPlan;

        _workspaceCombo.Enabled = !fromPlan;

        _workspaceLabel.Visible = !fromPlan;

        _workspaceCombo.Visible = !fromPlan;

        _derivedWorkspaceLabel.Visible = fromPlan;

        _modeCombo.Enabled = !fromPlan;

        _modeCombo.SelectedItem = fromPlan

            ? nameof(ExecutionMode.ExternalBroker)

            : _modeCombo.SelectedItem ?? nameof(ExecutionMode.ServerSimulated);

        _cppiEnabledCheckBox.Enabled = !fromPlan;

        _cppiFloorInput.Enabled = !fromPlan && _cppiEnabledCheckBox.Checked;

        _cppiMultiplierInput.Enabled = !fromPlan && _cppiEnabledCheckBox.Checked;



        _executionKeyLabel.Visible = fromPlan;

        _executionKeyTextBox.Visible = fromPlan;

        _openPlanAccountLabel.Visible = fromPlan;

        _openPlanAccountCombo.Visible = fromPlan;

        _distributeCheckBox.Visible = fromPlan;



        _titanoModeCombo.Enabled = !fromPlan;

        _titanoBacktestCombo.Enabled = !fromPlan;

        _titanoRunCombo.Enabled = !fromPlan;

        _loadRunsButton.Enabled = !fromPlan && !_isBusy;



        _createButton.Text = fromPlan ? "Apri sessione da piano" : "Crea sessione";

        _groupsGrid.ReadOnly = fromPlan;

        _addGroupRowButton.Enabled = !fromPlan && !_isBusy;

        _removeGroupRowButton.Enabled = !fromPlan && !_isBusy;



        if (fromPlan)

        {

            _clientRunModeCombo.Items.Clear();

            _clientRunModeCombo.Items.AddRange(

                [nameof(ClientRunMode.Backtest), nameof(ClientRunMode.Realtime)]);

            if (_clientRunModeCombo.SelectedItem?.ToString() is not (nameof(ClientRunMode.Backtest) or nameof(ClientRunMode.Realtime)))

            {

                _clientRunModeCombo.SelectedItem = nameof(ClientRunMode.Backtest);

            }

        }

        else

        {

            var selected = _clientRunModeCombo.SelectedItem?.ToString();

            _clientRunModeCombo.Items.Clear();

            _clientRunModeCombo.Items.AddRange(Enum.GetNames<ClientRunMode>());

            _clientRunModeCombo.SelectedItem = selected is { Length: > 0 } value && _clientRunModeCombo.Items.Contains(value)

                ? value

                : nameof(ClientRunMode.Unknown);

        }



        UpdateScenarioHint();

    }



    private void UpdateScenarioHint()

    {

        if (IsFromPlan)

        {

            var plan = SelectedPlan;

            var runMode = SelectedClientRunMode;

            var titanoMode = plan is null || !PrimaryPlanRow(plan).ApplyTitanoFilters

                ? TitanoFilterMode.Disabled

                : runMode == ClientRunMode.Backtest

                    ? TitanoFilterMode.BacktestRotationFile

                    : TitanoFilterMode.Realtime;

            _scenarioHintLabel.Text = plan is null

                ? "Seleziona un piano: il workspace e i gruppi verranno estratti dal piano."

                : DescribeScenario(runMode, titanoMode, fromPlan: true);

            return;

        }



        _scenarioHintLabel.Text = DescribeScenario(SelectedClientRunMode, SelectedTitanoMode, fromPlan: false);

        _scenarioHintLabel.ForeColor = ValidateRunModeCoherence(out var message)

            ? SystemColors.GrayText

            : Color.DarkRed;

        if (!ValidateRunModeCoherence(out message) && SelectedClientRunMode != ClientRunMode.Unknown)

        {

            _scenarioHintLabel.Text = message;

        }

    }



    private static string DescribeScenario(ClientRunMode runMode, TitanoFilterMode titanoMode, bool fromPlan)

    {

        var prefix = fromPlan

            ? "Apertura da piano: strategie dal masterfilter del workspace del piano. "

            : "Creazione manuale: strategie dal masterfilter del workspace selezionato. ";



        if (titanoMode == TitanoFilterMode.Disabled)

        {

            return prefix +

                   "Titano disattivato — vengono valutate tutte le strategie del masterfilter " +

                   "(run campione sorgente). MaxConcurrentTrades disattivato di default in backtest.";

        }



        if (titanoMode == TitanoFilterMode.BacktestRotationFile)

        {

            return prefix +

                   "Backtest filtrato — per ogni barra vale la rotazione del manifest offline. " +

                   "In ExternalBroker si applicano MaxConcurrentTrades e ordine di claim per account/gruppo.";

        }



        return prefix +

               "Realtime — vale il periodo corrente dell'ultima analisi Titano. " +

               "In ExternalBroker si applicano MaxConcurrentTrades e ordine di claim per account/gruppo.";

    }



    private static TradingGroupRow PrimaryPlanRow(TradingPlan plan)

        => plan.Groups.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.TitanoRunId))

           ?? plan.Groups.FirstOrDefault()

           ?? new TradingGroupRow

           {

               GroupId = plan.GroupId,

               AccountNumber = plan.AccountNumber,

               MaxConcurrentTrades = plan.MaxConcurrentTrades,

               RotationSetupId = plan.RotationSetupId,

               TitanoRunId = plan.TitanoRunId,

               TitanoBacktestFolder = plan.TitanoBacktestFolder,

               ApplyTitanoFilters = plan.ApplyTitanoFilters

           };



    private bool ValidateRunModeCoherence(out string message)

    {

        message = string.Empty;

        var runMode = SelectedClientRunMode;

        var titanoMode = SelectedTitanoMode;

        if (runMode == ClientRunMode.Unknown)

        {

            return true;

        }



        if (titanoMode == TitanoFilterMode.Realtime && runMode == ClientRunMode.Backtest)

        {

            message = "Combinazione non valida: Realtime con Backtest (look-ahead). " +

                      "Usa BacktestRotationFile oppure Disabled.";

            return false;

        }



        if (titanoMode == TitanoFilterMode.BacktestRotationFile && runMode == ClientRunMode.Realtime)

        {

            message = "Combinazione non valida: BacktestRotationFile con Realtime. " +

                      "Usa Realtime oppure Disabled.";

            return false;

        }



        return true;

    }



    private async Task ReloadPlansAsync(CancellationToken cancellationToken)

    {

        if (_context == null || SelectedWorkspaceId is not { } workspaceId)

        {

            _plans = [];

            _planCombo.Items.Clear();

            return;

        }



        _plans = await _context.Services.Plans.ListAsync(workspaceId, cancellationToken);

        var previousCode = SelectedPlan?.Code;

        _planCombo.Items.Clear();

        foreach (var plan in _plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase))

        {

            _planCombo.Items.Add(new PlanComboItem(plan));

        }



        if (_planCombo.Items.Count == 0)

        {

            _derivedWorkspaceLabel.Text = "—";

            return;

        }



        var restored = _plans.FindIndex(plan =>

            plan.Code.Equals(previousCode, StringComparison.OrdinalIgnoreCase));

        _planCombo.SelectedIndex = restored >= 0 ? restored : 0;

    }



    private async Task RefreshMasterfilterInfoAsync(CancellationToken cancellationToken)

    {

        if (_context == null || SelectedWorkspaceId is not { } workspaceId)

        {

            _masterfilterInfoLabel.Text = "—";

            return;

        }



        try

        {

            var filter = await _context.Services.Api.GetMasterFilterAsync(workspaceId, cancellationToken);

            _masterfilterInfoLabel.Text = filter.StrategiesFilter.Count == 0

                ? "Masterfilter vuoto: nessuna strategia da valutare."

                : $"Masterfilter: {filter.StrategiesFilter.Count} strategie (unica fonte di cosa gira in sessione).";

        }

        catch (Exception ex)

        {

            _masterfilterInfoLabel.Text = $"Masterfilter non leggibile: {ex.Message}";

        }

    }



    private void FillGroupsFromPlan(TradingPlan plan)

    {

        _groupsGrid.Rows.Clear();

        var rows = plan.Groups.Count > 0

            ? plan.Groups

            :

            [

                new TradingGroupRow

                {

                    GroupId = plan.GroupId,

                    AccountNumber = plan.AccountNumber,

                    MaxConcurrentTrades = plan.MaxConcurrentTrades,

                    RotationSetupId = plan.RotationSetupId,

                    TitanoRunId = plan.TitanoRunId,

                    TitanoBacktestFolder = plan.TitanoBacktestFolder,

                    ApplyTitanoFilters = plan.ApplyTitanoFilters

                }

            ];



        foreach (var row in rows)

        {

            _groupsGrid.Rows.Add(

                row.GroupId,

                row.RotationSetupId,

                row.AccountNumber,

                row.MaxConcurrentTrades,

                row.ApplyTitanoFilters,

                row.TitanoRunId ?? string.Empty,

                row.TitanoBacktestFolder ?? string.Empty);

        }

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



        if (_titanoBacktestCombo.SelectedIndex >= 0 && !IsFromPlan)

        {

            await LoadTitanoRunsAsync(showValidationError: false, cancellationToken);

        }

        else if (!IsFromPlan)

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



    private void OnCreationSourceChanged(object? sender, EventArgs e)

    {

        UpdateCreationModeControls();

        if (IsFromPlan && SelectedPlan is not null)

        {

            ApplySelectedPlan();

        }

    }



    private async void OnPlanChanged(object? sender, EventArgs e)

    {

        if (_context == null || _suspendReload)

        {

            return;

        }



        ApplySelectedPlan();

        try

        {

            SetBusy(true);

            await RefreshMasterfilterInfoAsync(CancellationToken.None);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private void ApplySelectedPlan()

    {

        var plan = SelectedPlan;

        if (plan is null)

        {

            _derivedWorkspaceLabel.Text = "—";

            RefreshOpenPlanAccountCombo(null);

            _groupsGrid.Rows.Clear();

            UpdateScenarioHint();

            return;

        }



        _derivedWorkspaceLabel.Text = $"Workspace del piano: {plan.WorkspaceId}";

        RefreshOpenPlanAccountCombo(plan);

        FillGroupsFromPlan(plan);

        UpdateScenarioHint();

    }



    private async void OnWorkspaceChanged(object? sender, EventArgs e)

    {

        if (_context == null || _suspendReload || IsFromPlan)

        {

            return;

        }



        try

        {

            SetBusy(true);

            await ReloadPlansAsync(CancellationToken.None);

            await ReloadBacktestsAsync(CancellationToken.None);

            await RefreshMasterfilterInfoAsync(CancellationToken.None);

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

        if (_context == null || _suspendReload || IsFromPlan)

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



    private void OnExecutionModeChanged(object? sender, EventArgs e)

    {

        var external = _modeCombo.SelectedItem?.ToString() == nameof(ExecutionMode.ExternalBroker);

        _mainTabControl.SelectedTab = external ? _groupsTab : _snapshotTab;

        UpdateScenarioHint();

    }



    private void OnRunModeOrTitanoChanged(object? sender, EventArgs e) => UpdateScenarioHint();



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



        if (IsFromPlan)

        {

            await OpenFromPlanAsync();

            return;

        }



        await CreateManualSessionAsync();

    }



    private async Task CreateManualSessionAsync()

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



        if (!ValidateRunModeCoherence(out var coherenceMessage))

        {

            MessageBox.Show(this, coherenceMessage, "Sessioni di trading",

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



        var executionMode = Enum.Parse<ExecutionMode>(

            _modeCombo.SelectedItem?.ToString() ?? nameof(ExecutionMode.ServerSimulated));

        var clientRunMode = SelectedClientRunMode;



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

                ExecutionMode = executionMode,

                TitanoRunId = titanoRunId,

                TitanoBacktestFolder = titanoRunId is null ? null : titanoBacktestFolder,

                TitanoMode = titanoMode,

                ClientRunMode = clientRunMode,

                EnforceConcurrencyLimits = clientRunMode == ClientRunMode.Unknown

                    ? null

                    : !(clientRunMode == ClientRunMode.Backtest && titanoMode == TitanoFilterMode.Disabled),

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



            if (executionMode == ExecutionMode.ExternalBroker)

            {

                var rows = ReadTradingGroupRows(allowEmpty: true);

                if (rows.Count > 0)

                {

                    await _context.Services.Sessions.SetGroupsAsync(

                        _activeSession.SessionId, _activeSession.SessionToken, rows);

                }

            }



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



    private async Task OpenFromPlanAsync()

    {

        if (_context == null)

        {

            return;

        }



        var plan = SelectedPlan;

        if (plan is null)

        {

            MessageBox.Show(this, "Seleziona un piano di trading.", "Sessioni di trading",

                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        var clientRunMode = SelectedClientRunMode;

        if (clientRunMode is ClientRunMode.Unknown)

        {

            MessageBox.Show(this,

                "Per aprire da piano devi dichiarare Backtest oppure Realtime (come il cBot).",

                "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        var executionKey = _executionKeyTextBox.Text.Trim();

        if (executionKey.Length == 0)

        {

            MessageBox.Show(this, "Execution key obbligatoria.", "Sessioni di trading",

                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        try

        {

            SetBusy(true);

            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(plan.WorkspaceId);

            if (masterFilter.StrategiesFilter.Count == 0)

            {

                MessageBox.Show(this,

                    "Il masterfilter del workspace del piano è vuoto.",

                    "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;

            }



            var account = (_openPlanAccountCombo.SelectedItem as AccountNumberListItem)?.AccountNumber;

            var request = new OpenTradingPlanSessionRequest

            {

                PlanCode = plan.Code,

                ClientRunMode = clientRunMode,

                ExecutionKey = executionKey,

                AccountNumber = account,

                DistributeToAccounts = _distributeCheckBox.Checked

            };



            _activeSession = await _context.Services.Sessions.OpenFromPlanAsync(request);

            UpdateSessionControls();

            ShowDescriptor("Sessione aperta da piano.");

            _context.Navigation.SetStatus(

                $"Sessione {_activeSession.SessionId} · workspace {plan.WorkspaceId} · " +

                $"Titano {_activeSession.TitanoMode} · contesto {_activeSession.ClientRunMode}.");

            _mainTabControl.SelectedTab = _snapshotTab;

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Errore apertura piano", MessageBoxButtons.OK, MessageBoxIcon.Error);

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

            var rows = ReadTradingGroupRows(allowEmpty: false);

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

                    mapping.TitanoRunId,

                    mapping.TitanoBacktestFolder);

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



    private List<TradingGroupRow> ReadTradingGroupRows(bool allowEmpty)

    {

        var defaultBacktestFolder = SelectedBacktestFolder;

        var rows = _groupsGrid.Rows.Cast<DataGridViewRow>()

            .Where(row => !row.IsNewRow)

            .Select(row =>

            {

                var folder = Convert.ToString(row.Cells["TitanoBacktestFolder"].Value ?? string.Empty)!.Trim();

                return new TradingGroupRow

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

                    TitanoBacktestFolder = folder.Length > 0 ? folder : defaultBacktestFolder

                };

            })

            .Where(row => row.AccountNumber.Length > 0 || row.GroupId.Length > 0)

            .ToList();



        if (!allowEmpty && rows.Count == 0)

        {

            throw new InvalidOperationException("Serve almeno una riga gruppo/account.");

        }



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


