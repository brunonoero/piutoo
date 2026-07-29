using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform;

public partial class WorkspaceBacktestingForm : Form
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WorkspaceApiClient _workspaceApi;

    private readonly TextBox _serverUrlTextBox = new() { Text = "https://localhost:7116", Width = 280 };
    private readonly DateTimePicker _startDatePicker = new();
    private readonly DateTimePicker _endDatePicker = new();
    private readonly NumericUpDown _initialCapitalInput = new();
    private readonly NumericUpDown _commissionInput = new();
    private readonly Button _reloadButton = new();
    private readonly Button _runButton = new();
    private readonly Button _cancelBacktestButton = new();
    private readonly Button _openReportButton = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private readonly TextBox _logTextBox = new();
    private readonly TextBox _basePathTextBox = new();
    private readonly TextBox _backtestNameTextBox = new() { Text = $"backtest-{DateTime.Now:yyyyMMdd-HHmm}" };

    private readonly ListBox _workspaceList = new();
    private readonly FilterableStrategyChecklist _workspaceStrategiesList = new();
    private readonly TextBox _workspaceNameTextBox = new();
    private readonly Label _workspaceDetailLabel = new();
    private readonly Button _createWorkspaceButton = new();
    private readonly Button _deleteWorkspaceButton = new();
    private readonly Button _refreshWorkspacesButton = new();
    private readonly Button _saveMasterFilterButton = new();
    private readonly ComboBox _backtestingWorkspaceCombo = new();
    private readonly ComboBox _backtestingAccountCombo = new();
    private readonly ComboBox _titanoWorkspaceCombo = new();
    private readonly ComboBox _titanoBacktestCombo = new();
    private readonly Label _titanoPathLabel = new();
    private readonly Button _refreshTitanoBacktestsButton = new();
    private readonly Button _openTitanoFolderButton = new();
    private readonly ComboBox _titanoSetupCombo = new();
    private readonly TextBox _titanoSetupName = new();
    private readonly TextBox _titanoSetupDescription = new();
    private readonly Button _titanoLoadSetupButton = new();
    private readonly Button _titanoSaveSetupButton = new();
    private readonly Button _titanoReloadSetupsButton = new();
    private readonly ComboBox _titanoPeriodCombo = new();
    private readonly NumericUpDown _titanoMinimumTrades = new();
    private readonly CheckBox _titanoRequireEquityAboveMa = new() { Text = "Richiedi equity sopra la media", AutoSize = true };
    private readonly DateTimePicker _titanoStartPicker = new();
    private readonly DateTimePicker _titanoEndPicker = new();
    private readonly NumericUpDown _titanoShortDays = new();
    private readonly NumericUpDown _titanoLongDays = new();
    private readonly NumericUpDown _titanoMaDays = new();
    private readonly NumericUpDown _titanoMinShortReturn = new();
    private readonly NumericUpDown _titanoMinLongReturn = new();
    private readonly NumericUpDown _titanoMinZ = new();
    private readonly NumericUpDown _titanoMaxZ = new();
    private readonly NumericUpDown _titanoMaxCurrentDd = new();
    private readonly NumericUpDown _titanoMaxDd = new();
    private readonly NumericUpDown _titanoMaxVolatility = new();
    private readonly NumericUpDown _titanoReenableDd = new();
    private readonly NumericUpDown _titanoDisableScore = new();
    private readonly NumericUpDown _titanoReenableScore = new();
    private readonly NumericUpDown _titanoCooldown = new();
    private readonly NumericUpDown _titanoMinOn = new();
    private readonly NumericUpDown _titanoMinVotes = new();
    private readonly NumericUpDown _titanoHardStop = new();
    private readonly NumericUpDown _titanoCommission = new();
    private readonly NumericUpDown _titanoSlippage = new();
    private readonly NumericUpDown _titanoCalibration = new();
    private readonly NumericUpDown _titanoEvaluation = new();
    private readonly ComboBox _titanoWalkForwardMode = new();
    private readonly TextBox _titanoSizingTiers = new() { Text = "0.80=100%; 0.60=50%; 0.40=25%; 0=0%" };
    private readonly Button _titanoResetHardStopButton = new();
    private readonly Button _openTitanoReportButton = new();
    private TitanoRotationManifest? _lastTitanoManifest;
    private readonly ComboBox _sessionWorkspaceCombo = new();
    private readonly ComboBox _sessionModeCombo = new();
    private readonly ComboBox _sessionTitanoRunId = new();
    private readonly Button _sessionLoadTitanoRuns = new();
    private readonly ComboBox _sessionTitanoMode = new();
    private readonly TextBox _sessionTitanoBacktest = new();
    private readonly TextBox _sessionMetadata = new() { Text = "ES,50,1,1,FuturesContracts", Width = 420 };
    private readonly CheckBox _sessionAtrEnabled = new() { Text = "ATR/target volatility", AutoSize = true };
    private readonly NumericUpDown _sessionAtrPeriods = new();
    private readonly NumericUpDown _sessionTargetRisk = new();
    private readonly CheckBox _sessionPortfolioEnabled = new() { Text = "Limiti portfolio", AutoSize = true };
    private readonly NumericUpDown _sessionDrawdownCap = new();
    private readonly CheckBox _sessionCppiEnabled = new() { Text = "CPPI (opzionale)", AutoSize = true };
    private readonly NumericUpDown _sessionCppiFloor = new();
    private readonly NumericUpDown _sessionCppiMultiplier = new();
    private readonly Button _sessionCreate = new();
    private readonly Button _sessionStart = new();
    private readonly Button _sessionStop = new();
    private readonly Button _sessionResume = new();
    private readonly Button _sessionSnapshot = new();
    private readonly TextBox _sessionOutput = new();
    private TradingSessionDescriptor? _activeSession;
    private readonly DataGridView _sessionAccountGroups = new();
    private readonly Button _sessionAddAccountGroupRow = new();
    private readonly Button _sessionSaveAccountGroups = new();
    private readonly Button _sessionReloadAccountGroups = new();
    private readonly Button _sessionApplyTitanoToGroups = new();
    private readonly ComboBox _sessionPlanCombo = new();
    private readonly TextBox _sessionPlanCode = new();
    private readonly TextBox _sessionPlanName = new();
    private readonly Button _sessionPlanNew = new();
    private readonly Button _sessionPlanSave = new();
    private readonly Button _sessionPlanDelete = new();
    private List<TradingPlan> _tradingPlans = new();
    private readonly Button _runTitanoButton = new();
    private readonly TextBox _titanoResultsTextBox = new();
    private readonly ToolTip _formToolTip = new() { AutoPopDelay = 25000, InitialDelay = 350, ReshowDelay = 100, ShowAlways = true, IsBalloon = true };
    private readonly Label _backtestingWorkspaceHint = new();
    private readonly Label _backtestingMasterFilterSummary = new();
    private readonly ListBox _backtestingMasterFilterStrategies = new();
    private readonly Button _editMasterFilterButton = new();
    private TabControl? _mainTabs;
    private TabPage? _workspacesTab;
    private readonly ComboBox _tradingResultsWorkspaceCombo = new();
    private readonly ComboBox _tradingResultsBacktestCombo = new();
    private readonly Button _refreshTradingResultsButton = new();
    private readonly DataGridView _tradingResultsGrid = new();
    private readonly Label _tradingResultsSummary = new();
    private readonly ComboBox _rotationsWorkspaceCombo = new();
    private readonly ComboBox _rotationsBacktestCombo = new();
    private readonly ComboBox _rotationsRunCombo = new();
    private readonly Button _refreshRotationsButton = new();
    private readonly DataGridView _rotationsGrid = new();
    private readonly Label _rotationsSummary = new();

    private readonly ComboBox _accountsCombo = new();
    private readonly TextBox _accountNameTextBox = new();
    private readonly TextBox _accountNumberTextBox = new();
    private readonly ComboBox _accountGroupIdCombo = new();
    private readonly ComboBox _accountGroupsCombo = new();
    private readonly TextBox _newAccountGroupTextBox = new();
    private readonly Button _addAccountGroupButton = new();
    private readonly Button _removeAccountGroupButton = new();
    private readonly TextBox _accountBrokerTextBox = new();
    private readonly ComboBox _accountCurrencyCombo = new();
    private readonly NumericUpDown _accountInitialBalance = new();
    private readonly CheckBox _accountEnabledCheck = new() { Text = "Account attivo", AutoSize = true, Checked = true };
    private readonly TextBox _accountNotesTextBox = new();
    private readonly DataGridView _accountSymbolsGrid = new();
    private readonly Button _accountNewButton = new();
    private readonly Button _accountSaveButton = new();
    private readonly Button _accountDeleteButton = new();
    private readonly Button _accountsReloadButton = new();
    private readonly Button _accountAddSymbolRowButton = new();
    private readonly Button _accountFillSymbolsButton = new();
    private readonly Button _accountLoadPresetButton = new();
    private readonly Button _accountSavePresetButton = new();
    private readonly Button _accountCreateDefaultButton = new();
    private readonly Label _accountStatusLabel = new();
    private List<WorkspaceAccount> _accounts = new();
    private List<string> _accountGroups = new();
    private WorkspaceAccount? _editingAccount;
    private bool _suppressAccountEvents;

    private List<StrategyCatalogItem> _strategies = new();
    private List<WorkspaceInfo> _workspaces = new();
    private List<TitanoSetupInfo> _titanoSetups = new();
    private BacktestingResult? _lastResult;
    private string? _lastJobId;
    private bool _suppressWorkspaceEvents;
    private CancellationTokenSource? _pollingCts;
    private bool _backtestRunning;
    private bool _allowClose;

    public WorkspaceBacktestingForm()
    {
        _workspaceApi = new WorkspaceApiClient(_httpClient, _jsonOptions);
        InitializeComponent();
        BuildUi();
        Load += async (_, _) => await InitializeClientAsync();
        FormClosing += OnFormClosing;
    }

    private void BuildUi()
    {
        Text = "Piootoo Workspace Console";
        Width = 1180;
        Height = 820;
        MinimumSize = new Size(980, 680);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildServerBar(), 0, 0);

        _mainTabs = new TabControl { Dock = DockStyle.Fill };
        _workspacesTab = new TabPage("Workspaces");
        var backtestingTab = new TabPage("Backtesting");
        var accountsTab = new TabPage("Accounts");
        var titanoTab = new TabPage("Titano");
        var tradingResultsTab = new TabPage("Trading Results");
        var rotationsTab = new TabPage("Titano Rotations");
        var sessionsTab = new TabPage("Trading Session");
        _mainTabs.TabPages.Add(accountsTab);
        _mainTabs.TabPages.Add(_workspacesTab);
        _mainTabs.TabPages.Add(backtestingTab);
        _mainTabs.TabPages.Add(titanoTab);
        _mainTabs.TabPages.Add(tradingResultsTab);
        _mainTabs.TabPages.Add(rotationsTab);
        _mainTabs.TabPages.Add(sessionsTab);
        root.Controls.Add(_mainTabs, 0, 1);

        _workspacesTab.Controls.Add(BuildWorkspacesTab());
        accountsTab.Controls.Add(BuildAccountsTab());
        backtestingTab.Controls.Add(BuildBacktestingTab());
        titanoTab.Controls.Add(BuildTitanoTab());
        tradingResultsTab.Controls.Add(BuildTradingResultsTab());
        rotationsTab.Controls.Add(BuildTitanoRotationsTab());
        sessionsTab.Controls.Add(BuildTradingSessionTab());
    }

    private Control BuildServerBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(4, 0, 4, 4)
        };
        panel.Controls.Add(new Label
        {
            Text = "Server API:",
            AutoSize = true,
            Padding = new Padding(0, 7, 6, 0)
        });
        panel.Controls.Add(_serverUrlTextBox);
        var connectButton = new Button { Text = "Connetti / ricarica workspace", AutoSize = true };
        connectButton.Click += async (_, _) => await ReloadWorkspacesAsync(showErrors: true);
        panel.Controls.Add(connectButton);
        return panel;
    }

    private Control BuildBacktestingTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.Controls.Add(BuildParametersPanel(), 0, 0);
        root.Controls.Add(BuildStrategyPanel(), 0, 1);
        root.Controls.Add(BuildActionsPanel(), 0, 2);
        root.Controls.Add(BuildLogPanel(), 0, 3);
        return root;
    }

    private Control BuildTradingResultsTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureResultsSelector(_tradingResultsWorkspaceCombo, _tradingResultsBacktestCombo,
            LoadTradingResultsBacktestsAsync, LoadTradingResultsAsync);
        _refreshTradingResultsButton.Text = "Aggiorna risultati";
        _refreshTradingResultsButton.AutoSize = true;
        _refreshTradingResultsButton.Click += async (_, _) => await LoadTradingResultsAsync();

        root.Controls.Add(new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Controls =
            {
                TitanoLabel("Workspace"), _tradingResultsWorkspaceCombo,
                TitanoLabel("Backtest"), _tradingResultsBacktestCombo,
                _refreshTradingResultsButton
            }
        }, 0, 0);

        _tradingResultsSummary.AutoSize = true;
        _tradingResultsSummary.Padding = new Padding(0, 6, 0, 6);
        _tradingResultsSummary.Text = "Seleziona un workspace e un backtest.";
        root.Controls.Add(_tradingResultsSummary, 0, 1);

        ConfigureTradingResultsGrid();
        root.Controls.Add(_tradingResultsGrid, 0, 2);
        return root;
    }

    private Control BuildTitanoRotationsTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureResultsSelector(_rotationsWorkspaceCombo, _rotationsBacktestCombo,
            LoadRotationBacktestsAsync, LoadTitanoRunsAsync);
        _rotationsRunCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _rotationsRunCombo.Width = 310;
        _rotationsRunCombo.SelectedIndexChanged += async (_, _) => await LoadSelectedTitanoRotationAsync();
        _refreshRotationsButton.Text = "Aggiorna rotazioni";
        _refreshRotationsButton.AutoSize = true;
        _refreshRotationsButton.Click += async (_, _) => await LoadTitanoRunsAsync();

        root.Controls.Add(new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Controls =
            {
                TitanoLabel("Workspace"), _rotationsWorkspaceCombo,
                TitanoLabel("Backtest"), _rotationsBacktestCombo,
                TitanoLabel("Run"), _rotationsRunCombo,
                _refreshRotationsButton
            }
        }, 0, 0);

        _rotationsSummary.AutoSize = true;
        _rotationsSummary.Padding = new Padding(0, 6, 0, 6);
        _rotationsSummary.Text = "Seleziona un workspace e un backtest.";
        root.Controls.Add(_rotationsSummary, 0, 1);

        ConfigureRotationsGrid();
        root.Controls.Add(_rotationsGrid, 0, 2);
        return root;
    }

    private static void ConfigureResultsSelector(
        ComboBox workspace,
        ComboBox backtest,
        Func<Task> loadBacktests,
        Func<Task> loadData)
    {
        workspace.DropDownStyle = ComboBoxStyle.DropDownList;
        workspace.Width = 280;
        workspace.DisplayMember = nameof(WorkspaceListItem.DisplayText);
        workspace.SelectedIndexChanged += async (_, _) => await loadBacktests();
        backtest.DropDownStyle = ComboBoxStyle.DropDownList;
        backtest.Width = 290;
        backtest.SelectedIndexChanged += async (_, _) => await loadData();
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    private void ConfigureTradingResultsGrid()
    {
        ConfigureGrid(_tradingResultsGrid);
        _tradingResultsGrid.Columns.Add("ExitTimeUtc", "Uscita UTC");
        _tradingResultsGrid.Columns.Add("EntryTimeUtc", "Entrata UTC");
        _tradingResultsGrid.Columns.Add("StrategyCode", "Strategia");
        _tradingResultsGrid.Columns.Add("Symbol", "Symbol");
        _tradingResultsGrid.Columns.Add("Direction", "Direzione");
        _tradingResultsGrid.Columns.Add("Quantity", "Quantità");
        _tradingResultsGrid.Columns.Add("EntryPrice", "Prezzo entrata");
        _tradingResultsGrid.Columns.Add("ExitPrice", "Prezzo uscita");
        _tradingResultsGrid.Columns.Add("ExitReason", "Motivo uscita");
        _tradingResultsGrid.Columns.Add("GrossProfit", "P&L lordo");
        _tradingResultsGrid.Columns.Add("Commission", "Commissioni");
        _tradingResultsGrid.Columns.Add("NetProfit", "P&L netto");
        _tradingResultsGrid.Columns.Add("AccountNumber", "Account");
    }

    private void ConfigureRotationsGrid()
    {
        ConfigureGrid(_rotationsGrid);
        _rotationsGrid.Columns.Add("EffectiveFromUtc", "Dal UTC");
        _rotationsGrid.Columns.Add("EffectiveToUtc", "Al UTC");
        _rotationsGrid.Columns.Add("StrategyCode", "Strategia");
        _rotationsGrid.Columns.Add("State", "Stato");
        _rotationsGrid.Columns.Add("AllocationMultiplier", "Moltiplicatore");
        _rotationsGrid.Columns.Add("TransitionType", "Variazione");
        _rotationsGrid.Columns.Add("Reason", "Motivo");
    }

    private Control BuildWorkspacesTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _createWorkspaceButton.Text = "Nuovo workspace";
        _createWorkspaceButton.AutoSize = true;
        _createWorkspaceButton.Click += async (_, _) => await CreateWorkspaceAsync();

        _deleteWorkspaceButton.Text = "Elimina workspace";
        _deleteWorkspaceButton.AutoSize = true;
        _deleteWorkspaceButton.Enabled = false;
        _deleteWorkspaceButton.Click += async (_, _) => await DeleteSelectedWorkspaceAsync();

        _refreshWorkspacesButton.Text = "Ricarica";
        _refreshWorkspacesButton.AutoSize = true;
        _refreshWorkspacesButton.Click += async (_, _) => await ReloadWorkspacesAsync(showErrors: true);

        _saveMasterFilterButton.Text = "Salva masterfilter";
        _saveMasterFilterButton.AutoSize = true;
        _saveMasterFilterButton.Enabled = false;
        _saveMasterFilterButton.Click += async (_, _) => await SaveSelectedMasterFilterAsync();

        var commands = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        commands.Controls.AddRange(new Control[]
        {
            _createWorkspaceButton,
            _saveMasterFilterButton,
            _deleteWorkspaceButton,
            _refreshWorkspacesButton
        });
        root.Controls.Add(commands, 0, 0);
        root.SetColumnSpan(commands, 2);

        var namePanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        namePanel.Controls.Add(new Label
        {
            Text = "Nome visualizzato:",
            AutoSize = true,
            Padding = new Padding(0, 7, 8, 0)
        });
        _workspaceNameTextBox.Width = 320;
        namePanel.Controls.Add(_workspaceNameTextBox);
        _workspaceDetailLabel.AutoSize = true;
        _workspaceDetailLabel.Padding = new Padding(16, 7, 0, 0);
        _workspaceDetailLabel.Text = "Nessun workspace selezionato.";
        namePanel.Controls.Add(_workspaceDetailLabel);
        root.Controls.Add(namePanel, 0, 1);
        root.SetColumnSpan(namePanel, 2);

        _workspaceList.Dock = DockStyle.Fill;
        _workspaceList.DisplayMember = nameof(WorkspaceListItem.DisplayText);
        _workspaceList.SelectedIndexChanged += async (_, _) => await OnWorkspaceSelectionChangedAsync();

        _workspaceStrategiesList.Dock = DockStyle.Fill;

        root.Controls.Add(new GroupBox
        {
            Text = "Workspace",
            Dock = DockStyle.Fill,
            Controls = { _workspaceList }
        }, 0, 2);
        root.Controls.Add(new GroupBox
        {
            Text = "Strategie incluse nel masterfilter",
            Dock = DockStyle.Fill,
            Controls = { _workspaceStrategiesList }
        }, 1, 2);
        return root;
    }

    private Control BuildAccountsTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _accountsCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _accountsCombo.Width = 420;
        _accountsCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressAccountEvents) return;
            BindSelectedAccountToEditor();
        };

        var selectors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        selectors.Controls.Add(new Label
        {
            Text = "Account globale:",
            AutoSize = true,
            Padding = new Padding(0, 7, 8, 0)
        });
        selectors.Controls.Add(WithHelp(_accountsCombo,
            "Account condivisi da tutti i workspace. Selezionane uno per modificarne anagrafica e tabella di conversione."));

        _accountGroupsCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _accountGroupsCombo.Width = 150;
        _newAccountGroupTextBox.Width = 140;
        _addAccountGroupButton.Text = "Aggiungi gruppo";
        _addAccountGroupButton.AutoSize = true;
        _addAccountGroupButton.Click += async (_, _) => await AddAccountGroupAsync();
        _removeAccountGroupButton.Text = "Rimuovi gruppo";
        _removeAccountGroupButton.AutoSize = true;
        _removeAccountGroupButton.Click += async (_, _) => await RemoveAccountGroupAsync();
        selectors.Controls.Add(new Label
        {
            Text = "Gruppi:",
            AutoSize = true,
            Padding = new Padding(16, 7, 8, 0)
        });
        selectors.Controls.Add(WithHelp(_accountGroupsCombo, "Gruppi account globali configurati."));
        selectors.Controls.Add(WithHelp(_newAccountGroupTextBox, "Codice del nuovo gruppo da aggiungere."));
        selectors.Controls.Add(_addAccountGroupButton);
        selectors.Controls.Add(_removeAccountGroupButton);
        root.Controls.Add(selectors, 0, 0);

        _accountNewButton.Text = "Nuovo account…";
        _accountNewButton.AutoSize = true;
        _accountNewButton.Click += async (_, _) => await CreateAccountViaDialogAsync();

        _accountSaveButton.Text = "Salva modifiche";
        _accountSaveButton.AutoSize = true;
        _accountSaveButton.Enabled = false;
        _accountSaveButton.Click += async (_, _) => await SaveEditingAccountAsync();

        _accountDeleteButton.Text = "Elimina account";
        _accountDeleteButton.AutoSize = true;
        _accountDeleteButton.Enabled = false;
        _accountDeleteButton.Click += async (_, _) => await DeleteSelectedAccountAsync();

        _accountsReloadButton.Text = "Ricarica";
        _accountsReloadButton.AutoSize = true;
        _accountsReloadButton.Click += async (_, _) => await ReloadAccountsAsync(showErrors: true);

        _accountCreateDefaultButton.Text = "Crea account Default";
        _accountCreateDefaultButton.AutoSize = true;
        _accountCreateDefaultButton.Click += async (_, _) => await EnsureDefaultAccountAsync();

        _accountStatusLabel.AutoSize = true;
        _accountStatusLabel.Padding = new Padding(16, 7, 0, 0);
        _accountStatusLabel.Text = "Nessun account caricato.";

        var commands = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        commands.Controls.AddRange(new[]
        {
            WithHelp(_accountNewButton,
                "Apre la finestra di creazione: la tabella di conversione parte dal preset identità (@GC = @GC, moltiplicatore 1)."),
            WithHelp(_accountSaveButton, "Salva anagrafica e tabella di conversione dell'account selezionato."),
            WithHelp(_accountCreateDefaultButton,
                "Crea l'account 'Default': symbol mappati 1 a 1 sul catalogo, moltiplicatore 1 e balance iniziale 1.000.000."),
            WithHelp(_accountDeleteButton, "Rimuove l'account selezionato dal registro globale."),
            WithHelp(_accountsReloadButton, "Rilegge gli account dal server."),
            _accountStatusLabel
        });
        root.Controls.Add(commands, 0, 1);

        var detail = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detail.Controls.Add(BuildAccountDetailPanel(), 0, 0);
        detail.Controls.Add(BuildAccountSymbolsPanel(), 0, 1);
        root.Controls.Add(detail, 0, 2);

        ClearAccountEditor();
        return root;
    }

    private Control BuildAccountDetailPanel()
    {
        var group = new GroupBox
        {
            Text = "Anagrafica account",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Width = 700 };

        _accountNameTextBox.Width = 200;
        _accountNumberTextBox.Width = 160;
        _accountGroupIdCombo.Width = 160;
        _accountGroupIdCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _accountBrokerTextBox.Width = 160;

        _accountCurrencyCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _accountCurrencyCombo.Width = 90;
        _accountCurrencyCombo.Items.AddRange(new object[] { "USD", "EUR", "GBP", "CHF" });
        _accountCurrencyCombo.Text = "USD";

        _accountInitialBalance.Minimum = 0;
        _accountInitialBalance.Maximum = 1_000_000_000;
        _accountInitialBalance.DecimalPlaces = 2;
        _accountInitialBalance.ThousandsSeparator = true;
        _accountInitialBalance.Increment = 1000;
        _accountInitialBalance.Width = 140;

        _accountNotesTextBox.Width = 660;
        _accountNotesTextBox.Multiline = true;
        _accountNotesTextBox.Height = 44;

        layout.Controls.Add(LabeledField("Nome", _accountNameTextBox,
            "Nome visualizzato dell'account. Determina l'identificativo salvato su disco."));
        layout.Controls.Add(LabeledField("Codice account", _accountNumberTextBox,
            "Codice account del broker, lo stesso usato nei gruppi account della Trading Session."));
        layout.Controls.Add(LabeledField("Gruppo", _accountGroupIdCombo,
            "Gruppo anti copy-trading (tipicamente la prop firm). Account dello stesso gruppo non ricevono lo stesso segnale."));
        layout.Controls.Add(LabeledField("Broker", _accountBrokerTextBox, "Broker o prop firm, campo descrittivo."));
        layout.Controls.Add(LabeledField("Balance iniziale", _accountInitialBalance,
            "Capitale iniziale dell'account nella valuta indicata."));
        layout.Controls.Add(LabeledField("Valuta", _accountCurrencyCombo, "Valuta del balance."));
        layout.Controls.Add(LabeledField(" ", _accountEnabledCheck, "Se disattivato l'account resta configurato ma non operativo."));
        layout.Controls.Add(LabeledField("Note", _accountNotesTextBox, "Annotazioni libere."));

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildAccountSymbolsPanel()
    {
        var group = new GroupBox
        {
            Text = "Tabella di conversione symbol",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _accountSymbolsGrid.Dock = DockStyle.Fill;
        _accountSymbolsGrid.AllowUserToAddRows = true;
        _accountSymbolsGrid.AllowUserToDeleteRows = true;
        _accountSymbolsGrid.RowHeadersVisible = false;
        _accountSymbolsGrid.AutoGenerateColumns = false;
        _accountSymbolsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _accountSymbolsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Symbol", HeaderText = "Symbol Piootoo", FillWeight = 28,
            ToolTipText = "Simbolo come compare nel catalogo strategie, es. @NQ."
        });
        _accountSymbolsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "AccountSymbol", HeaderText = "Symbol account", FillWeight = 28,
            ToolTipText = "Simbolo equivalente sul broker dell'account, es. USDTEC."
        });
        _accountSymbolsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ContractMultiplier", HeaderText = "Moltiplicatore contratto", FillWeight = 30,
            ToolTipText = "1 contratto Piootoo = N contratti account. Es. 0,1 se il contratto broker vale 100k contro 1M."
        });
        _accountSymbolsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled", HeaderText = "Attivo", FillWeight = 14, TrueValue = true, FalseValue = false
        });
        _accountSymbolsGrid.DefaultValuesNeeded += (_, e) =>
        {
            e.Row.Cells["ContractMultiplier"].Value = "1";
            e.Row.Cells["Enabled"].Value = true;
        };
        _formToolTip.SetToolTip(_accountSymbolsGrid,
            "Una riga per simbolo: mappatura del simbolo e fattore di scala del contratto usato da questo account.");
        layout.Controls.Add(_accountSymbolsGrid, 0, 0);

        _accountAddSymbolRowButton.Text = "Aggiungi riga";
        _accountAddSymbolRowButton.AutoSize = true;
        _accountAddSymbolRowButton.Click += (_, _) => _accountSymbolsGrid.Rows.Add(string.Empty, string.Empty, "1", true);

        _accountFillSymbolsButton.Text = "Precompila dal catalogo";
        _accountFillSymbolsButton.AutoSize = true;
        _accountFillSymbolsButton.Click += (_, _) => FillAccountSymbolsFromCatalog();

        _accountLoadPresetButton.Text = "Carica preset";
        _accountLoadPresetButton.AutoSize = true;
        _accountLoadPresetButton.Click += async (_, _) => await LoadSymbolPresetAsync();

        _accountSavePresetButton.Text = "Salva come preset";
        _accountSavePresetButton.AutoSize = true;
        _accountSavePresetButton.Click += async (_, _) => await SaveSymbolPresetAsync();

        layout.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Controls =
            {
                WithHelp(_accountAddSymbolRowButton, "Aggiunge una riga vuota con moltiplicatore 1."),
                WithHelp(_accountFillSymbolsButton,
                    "Aggiunge una riga per ogni symbol distinto del catalogo strategie non ancora presente in tabella."),
                WithHelp(_accountLoadPresetButton,
                    "Sostituisce la tabella con il preset condiviso (settings/default-symbol-conversion.json)."),
                WithHelp(_accountSavePresetButton,
                    "Salva la tabella corrente come nuovo preset condiviso per tutti i workspace.")
            }
        }, 0, 1);

        group.Controls.Add(layout);
        return group;
    }

    private Control LabeledField(string label, Control control, string help)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(2, 2, 14, 6)
        };
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        panel.Controls.Add(WithHelp(control, help));
        return panel;
    }

    private void FillAccountSymbolsFromCatalog()
    {
        var existing = _accountSymbolsGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => Convert.ToString(row.Cells["Symbol"].Value ?? string.Empty)!.Trim())
            .Where(symbol => symbol.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var symbol in _strategies
            .Select(strategy => strategy.Symbol?.Trim() ?? string.Empty)
            .Where(symbol => symbol.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            if (!existing.Add(symbol)) continue;
            // Riga identità: symbol su se stesso e moltiplicatore 1, così aggiungerla non cambia
            // il comportamento finché non la si modifica.
            _accountSymbolsGrid.Rows.Add(symbol, symbol, "1", true);
            added++;
        }

        _accountStatusLabel.Text = added > 0
            ? $"Aggiunti {added} symbol dal catalogo, mappati 1 a 1."
            : "Nessun nuovo symbol da aggiungere dal catalogo.";
    }

    private void SetAccountSymbolRows(IEnumerable<AccountSymbolMapping> mappings)
    {
        _accountSymbolsGrid.Rows.Clear();
        foreach (var mapping in mappings)
            _accountSymbolsGrid.Rows.Add(
                mapping.Symbol,
                mapping.AccountSymbol,
                mapping.ContractMultiplier.ToString(System.Globalization.CultureInfo.CurrentCulture),
                mapping.Enabled);
    }

    private async Task LoadSymbolPresetAsync()
    {
        try
        {
            NormalizeBaseAddress();
            var preset = await _workspaceApi.GetSymbolConversionPresetAsync();
            SetAccountSymbolRows(preset);
            _accountStatusLabel.Text = $"Preset caricato: {preset.Count} symbol. Salva l'account per applicarlo.";
            Log($"Preset di conversione caricato ({preset.Count} symbol).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore preset conversione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveSymbolPresetAsync()
    {
        try
        {
            var mappings = ReadAccountFromEditorMappings();
            NormalizeBaseAddress();
            var saved = await _workspaceApi.SaveSymbolConversionPresetAsync(mappings);
            _accountStatusLabel.Text = $"Preset condiviso aggiornato con {saved.Count} symbol.";
            Log($"Preset di conversione salvato ({saved.Count} symbol).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore preset conversione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EnsureDefaultAccountAsync()
    {
        try
        {
            NormalizeBaseAddress();
            var account = await _workspaceApi.EnsureDefaultAccountAsync();
            _editingAccount = account;
            await ReloadAccountsAsync(showErrors: true);
            _accountStatusLabel.Text =
                $"Account '{account.Name}' pronto: {account.SymbolMappings.Count} symbol 1 a 1, " +
                $"balance {account.InitialBalance:N0} {account.Currency}.";
            Log("Account di default disponibile nel registro globale.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore account di default", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Popola il selettore Backtesting con gli account globali.</summary>
    private async Task LoadBacktestingAccountsAsync()
    {
        var previousId = (_backtestingAccountCombo.SelectedItem as AccountListItem)?.Account.Id;
        _suppressAccountEvents = true;
        try
        {
            _backtestingAccountCombo.Items.Clear();
            _backtestingAccountCombo.Items.Add(NoAccountItem);

            NormalizeBaseAddress();
            foreach (var account in await _workspaceApi.ListAccountsAsync())
                _backtestingAccountCombo.Items.Add(new AccountListItem(account));

            _backtestingAccountCombo.SelectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(previousId))
                for (var index = 1; index < _backtestingAccountCombo.Items.Count; index++)
                    if (_backtestingAccountCombo.Items[index] is AccountListItem item &&
                        item.Account.Id.Equals(previousId, StringComparison.OrdinalIgnoreCase))
                    {
                        _backtestingAccountCombo.SelectedIndex = index;
                        break;
                    }
        }
        catch (Exception ex)
        {
            Log($"Errore caricamento account per il backtesting: {ex.Message}");
        }
        finally
        {
            _suppressAccountEvents = false;
        }
    }

    private const string NoAccountItem = "(nessuna conversione — 1 a 1)";

    private async Task ReloadAccountsAsync(bool showErrors)
    {
        try
        {
            NormalizeBaseAddress();
            var previousId = _editingAccount?.Id;
            _accounts = (await _workspaceApi.ListAccountsAsync()).ToList();
            _accountGroups = (await _workspaceApi.ListAccountGroupsAsync()).ToList();
            RefreshAccountGroupLookups();
            BindAccountsList(previousId);
            _accountStatusLabel.Text = $"{_accounts.Count} account globali.";
            Log($"Caricati {_accounts.Count} account globali da API.");
        }
        catch (Exception ex)
        {
            Log($"Errore caricamento account: {ex.Message}");
            if (showErrors)
                MessageBox.Show(ex.Message, "Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshAccountGroupLookups()
    {
        var selectedEditorGroup = _accountGroupIdCombo.SelectedItem?.ToString();
        var selectedManagedGroup = _accountGroupsCombo.SelectedItem?.ToString();

        _accountGroupIdCombo.Items.Clear();
        _accountGroupIdCombo.Items.Add(string.Empty);
        _accountGroupsCombo.Items.Clear();
        foreach (var group in _accountGroups.OrderBy(group => group, StringComparer.OrdinalIgnoreCase))
        {
            _accountGroupIdCombo.Items.Add(group);
            _accountGroupsCombo.Items.Add(group);
        }
        SelectComboValue(_accountGroupIdCombo, selectedEditorGroup);
        SelectComboValue(_accountGroupsCombo, selectedManagedGroup);

        if (_sessionAccountGroups.Columns["GroupId"] is DataGridViewComboBoxColumn groupColumn)
            groupColumn.DataSource = _accountGroups.ToList();
        if (_sessionAccountGroups.Columns["AccountNumber"] is DataGridViewComboBoxColumn accountColumn)
            accountColumn.DataSource = _accounts
                .Where(account => account.Enabled && !string.IsNullOrWhiteSpace(account.AccountNumber))
                .Select(account => new AccountNumberListItem(account))
                .ToList();
    }

    private async Task AddAccountGroupAsync()
    {
        var groupId = _newAccountGroupTextBox.Text.Trim();
        if (groupId.Length == 0)
        {
            MessageBox.Show("Indica il codice del gruppo da aggiungere.", "Gruppi account",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            _accountGroups = (await _workspaceApi.AddAccountGroupAsync(groupId)).ToList();
            _newAccountGroupTextBox.Clear();
            RefreshAccountGroupLookups();
            SelectComboValue(_accountGroupsCombo, groupId);
            SelectComboValue(_accountGroupIdCombo, groupId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore aggiunta gruppo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RemoveAccountGroupAsync()
    {
        var groupId = _accountGroupsCombo.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(groupId))
            return;
        if (MessageBox.Show($"Eliminare il gruppo globale '{groupId}'?", "Gruppi account",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            NormalizeBaseAddress();
            _accountGroups = (await _workspaceApi.RemoveAccountGroupAsync(groupId)).ToList();
            RefreshAccountGroupLookups();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore eliminazione gruppo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void SelectComboValue(ComboBox combo, string? value)
    {
        combo.SelectedIndex = -1;
        for (var index = 0; index < combo.Items.Count; index++)
            if (string.Equals(combo.Items[index]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                return;
            }
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private void BindAccountsList(string? selectAccountId)
    {
        _suppressAccountEvents = true;
        try
        {
            _accountsCombo.Items.Clear();
            foreach (var account in _accounts)
                _accountsCombo.Items.Add(new AccountListItem(account));

            if (_accountsCombo.Items.Count > 0)
            {
                _accountsCombo.SelectedIndex = 0;
                if (!string.IsNullOrWhiteSpace(selectAccountId))
                    for (var index = 0; index < _accountsCombo.Items.Count; index++)
                        if (_accountsCombo.Items[index] is AccountListItem item &&
                            item.Account.Id.Equals(selectAccountId, StringComparison.OrdinalIgnoreCase))
                        {
                            _accountsCombo.SelectedIndex = index;
                            break;
                        }
            }
        }
        finally
        {
            _suppressAccountEvents = false;
        }

        if (_accountsCombo.SelectedItem is AccountListItem)
            BindSelectedAccountToEditor();
        else
            ClearAccountEditor();
    }

    /// <summary>Nessun account selezionato: pannello vuoto e comandi di modifica disattivati.</summary>
    private void ClearAccountEditor()
    {
        _suppressAccountEvents = true;
        try
        {
            _editingAccount = null;
            _accountNameTextBox.Text = string.Empty;
            _accountNumberTextBox.Text = string.Empty;
            _accountGroupIdCombo.SelectedIndex = _accountGroupIdCombo.Items.Count > 0 ? 0 : -1;
            _accountBrokerTextBox.Text = string.Empty;
            _accountCurrencyCombo.Text = "USD";
            _accountInitialBalance.Value = 0;
            _accountEnabledCheck.Checked = true;
            _accountNotesTextBox.Text = string.Empty;
            _accountSymbolsGrid.Rows.Clear();
            _accountDeleteButton.Enabled = false;
            _accountSaveButton.Enabled = false;
        }
        finally
        {
            _suppressAccountEvents = false;
        }
    }

    /// <summary>
    /// Creazione account in modale. La tabella di conversione parte sempre dal preset identità
    /// (ogni symbol su se stesso, moltiplicatore 1): così un account nuovo è idempotente finché non
    /// lo si modifica, e non serve ricordarsi di popolarlo per avere un run 1 a 1.
    /// </summary>
    private async Task CreateAccountViaDialogAsync()
    {
        IReadOnlyList<AccountSymbolMapping> identity;
        try
        {
            NormalizeBaseAddress();
            identity = await _workspaceApi.GetSymbolIdentityAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile leggere la tabella di conversione identità.{Environment.NewLine}{ex.Message}",
                "Nuovo account", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dialog = new NewAccountDialog(identity.Count, _accountGroups);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var account = dialog.BuildAccount();
        account.SymbolMappings = identity
            .Select(mapping => new AccountSymbolMapping
            {
                Symbol = mapping.Symbol,
                AccountSymbol = mapping.AccountSymbol,
                ContractMultiplier = mapping.ContractMultiplier,
                Enabled = mapping.Enabled
            })
            .ToList();

        try
        {
            NormalizeBaseAddress();
            var created = await _workspaceApi.CreateAccountAsync(account);
            _editingAccount = created;
            await ReloadAccountsAsync(showErrors: true);
            await LoadBacktestingAccountsAsync();
            _accountStatusLabel.Text =
                $"Account '{created.Name}' creato con {created.SymbolMappings.Count} symbol 1 a 1.";
            Log($"Account globale '{created.Id}' creato.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore creazione account", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindSelectedAccountToEditor()
    {
        if (_accountsCombo.SelectedItem is not AccountListItem item)
        {
            ClearAccountEditor();
            return;
        }

        var account = item.Account;
        _suppressAccountEvents = true;
        try
        {
            _editingAccount = account;
            _accountNameTextBox.Text = account.Name;
            _accountNumberTextBox.Text = account.AccountNumber;
            SelectComboValue(_accountGroupIdCombo, account.GroupId);
            _accountBrokerTextBox.Text = account.Broker;
            _accountCurrencyCombo.Text = string.IsNullOrWhiteSpace(account.Currency) ? "USD" : account.Currency;
            _accountInitialBalance.Value = Math.Clamp(
                account.InitialBalance,
                _accountInitialBalance.Minimum,
                _accountInitialBalance.Maximum);
            _accountEnabledCheck.Checked = account.Enabled;
            _accountNotesTextBox.Text = account.Notes;

            SetAccountSymbolRows(account.SymbolMappings);
            _accountDeleteButton.Enabled = true;
            _accountSaveButton.Enabled = true;
        }
        finally
        {
            _suppressAccountEvents = false;
        }
    }

    private WorkspaceAccount ReadAccountFromEditor()
    {
        if (string.IsNullOrWhiteSpace(_accountNameTextBox.Text))
            throw new InvalidOperationException("Il nome dell'account è obbligatorio.");

        return new WorkspaceAccount
        {
            Id = _editingAccount?.Id ?? string.Empty,
            Name = _accountNameTextBox.Text.Trim(),
            AccountNumber = _accountNumberTextBox.Text.Trim(),
            GroupId = _accountGroupIdCombo.SelectedItem?.ToString()?.Trim() ?? string.Empty,
            Broker = _accountBrokerTextBox.Text.Trim(),
            Currency = string.IsNullOrWhiteSpace(_accountCurrencyCombo.Text) ? "USD" : _accountCurrencyCombo.Text.Trim(),
            InitialBalance = _accountInitialBalance.Value,
            Enabled = _accountEnabledCheck.Checked,
            Notes = _accountNotesTextBox.Text.Trim(),
            SymbolMappings = ReadAccountFromEditorMappings()
        };
    }

    private List<AccountSymbolMapping> ReadAccountFromEditorMappings()
    {
        var mappings = new List<AccountSymbolMapping>();
        foreach (var row in _accountSymbolsGrid.Rows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow))
        {
            var symbol = Convert.ToString(row.Cells["Symbol"].Value ?? string.Empty)!.Trim();
            var accountSymbol = Convert.ToString(row.Cells["AccountSymbol"].Value ?? string.Empty)!.Trim();
            var rawMultiplier = Convert.ToString(row.Cells["ContractMultiplier"].Value ?? string.Empty)!.Trim();
            if (symbol.Length == 0 && accountSymbol.Length == 0 && rawMultiplier.Length == 0)
                continue;
            if (symbol.Length == 0)
                throw new InvalidOperationException("Ogni riga della tabella di conversione deve indicare il symbol Piootoo.");
            if (accountSymbol.Length == 0)
                throw new InvalidOperationException($"Indica il symbol account per '{symbol}'.");

            mappings.Add(new AccountSymbolMapping
            {
                Symbol = symbol,
                AccountSymbol = accountSymbol,
                ContractMultiplier = ParseMultiplier(symbol, rawMultiplier),
                Enabled = row.Cells["Enabled"].Value is not false
            });
        }

        return mappings;
    }

    private static decimal ParseMultiplier(string symbol, string rawValue)
    {
        if (rawValue.Length == 0)
            return 1m;

        var normalized = rawValue.Replace(',', '.');
        if (!decimal.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var multiplier) || multiplier <= 0)
            throw new InvalidOperationException(
                $"Il moltiplicatore contratto per '{symbol}' deve essere un numero maggiore di zero (es. 0,1).");
        return multiplier;
    }

    private async Task SaveEditingAccountAsync()
    {
        if (_editingAccount is null)
        {
            MessageBox.Show(
                "Seleziona un account dalla combo, oppure creane uno nuovo.",
                "Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var account = ReadAccountFromEditor();
            NormalizeBaseAddress();
            var saved = await _workspaceApi.SaveAccountAsync(_editingAccount.Id, account);

            _editingAccount = saved;
            await ReloadAccountsAsync(showErrors: true);
            await LoadBacktestingAccountsAsync();
            _accountStatusLabel.Text =
                $"Account '{saved.Name}' salvato ({saved.SymbolMappings.Count} symbol mappati).";
            Log($"Account globale '{saved.Id}' salvato.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore salvataggio account", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedAccountAsync()
    {
        if (_editingAccount is null) return;

        if (MessageBox.Show(
                $"Eliminare l'account globale '{_editingAccount.Name}'?",
                "Elimina account",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            NormalizeBaseAddress();
            await _workspaceApi.DeleteAccountAsync(_editingAccount.Id);
            Log($"Account globale '{_editingAccount.Id}' eliminato.");
            _editingAccount = null;
            await ReloadAccountsAsync(showErrors: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore eliminazione account", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Control BuildTradingSessionTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _sessionWorkspaceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionWorkspaceCombo.Width = 320;
        _sessionWorkspaceCombo.SelectedIndexChanged += async (_, _) => await LoadTradingPlansAsync();
        _sessionPlanCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionPlanCombo.Width = 220;
        _sessionPlanCombo.SelectedIndexChanged += (_, _) => LoadSelectedTradingPlan();
        _sessionPlanCode.Width = 110;
        _sessionPlanName.Width = 180;
        _sessionPlanNew.Text = "Nuovo piano";
        _sessionPlanSave.Text = "Salva piano";
        _sessionPlanDelete.Text = "Elimina piano";
        _sessionPlanNew.Click += (_, _) => ClearTradingPlanEditor();
        _sessionPlanSave.Click += async (_, _) => await SaveTradingPlanAsync();
        _sessionPlanDelete.Click += async (_, _) => await DeleteTradingPlanAsync();
        _sessionModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionModeCombo.Items.AddRange(Enum.GetNames<ExecutionMode>());
        _sessionModeCombo.SelectedItem = nameof(ExecutionMode.ServerSimulated);
        ConfigureTitanoNumber(_sessionAtrPeriods, 2, 500, 14);
        ConfigureTitanoNumber(_sessionTargetRisk, 1, 1_000_000, 1000, 2);
        ConfigureTitanoNumber(_sessionDrawdownCap, 1, 100, 20, 2);
        ConfigureTitanoNumber(_sessionCppiFloor, 0, 100, 80, 2);
        ConfigureTitanoNumber(_sessionCppiMultiplier, 0, 10, 1, 2);
        _sessionTitanoRunId.Width = 260;
        _sessionTitanoRunId.DropDownStyle = ComboBoxStyle.DropDown; // consente anche l'incolla manuale
        _sessionTitanoBacktest.Width = 160;
        _sessionLoadTitanoRuns.Text = "Carica run";
        _sessionLoadTitanoRuns.AutoSize = true;
        _sessionLoadTitanoRuns.Click += async (_, _) => await LoadTitanoRunsForSessionAsync();
        _sessionTitanoMode.Width = 200;
        _sessionTitanoMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionTitanoMode.Items.AddRange(Enum.GetNames<TitanoFilterMode>());
        _sessionTitanoMode.SelectedItem = nameof(TitanoFilterMode.Disabled);
        _sessionTitanoMode.SelectedIndexChanged += (_, _) => UpdateTitanoSessionControlsState();

        _formToolTip.SetToolTip(_sessionAtrEnabled,
            "Attiva il sizing basato su volatilità di mercato (ATR): dimensiona ogni posizione in base al rischio in dollari desiderato.");
        _formToolTip.SetToolTip(_sessionPortfolioEnabled,
            "Attiva i limiti di rischio a livello di portafoglio (drawdown massimo) che riducono l'esposizione quando superati.");
        _formToolTip.SetToolTip(_sessionCppiEnabled,
            "Attiva l'overlay CPPI (Constant Proportion Portfolio Insurance): protegge un floor di capitale scalando l'esposizione sul cuscinetto residuo.");

        var config = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        config.Controls.AddRange(new Control[]
        {
            TitanoLabel("Piano", "Configurazione operativa riutilizzabile salvata nel workspace."),
            _sessionPlanCombo,
            TitanoLabel("Codice piano", "Codice globale inserito nel cBot."),
            _sessionPlanCode,
            TitanoLabel("Nome piano", "Nome leggibile del piano."),
            _sessionPlanName,
            _sessionPlanNew,
            _sessionPlanSave,
            _sessionPlanDelete,
            TitanoLabel("Workspace", "Workspace per cui viene creata e gestita la sessione di trading."),
            WithHelp(_sessionWorkspaceCombo, "Workspace per cui viene creata e gestita la sessione di trading."),
            TitanoLabel("Modalità", "ServerSimulated: esecuzione simulata lato server. ExternalBroker: gli ordini vengono inoltrati a un broker esterno."),
            WithHelp(_sessionModeCombo, "ServerSimulated: esecuzione simulata lato server. ExternalBroker: gli ordini vengono inoltrati a un broker esterno."),
            TitanoLabel("Backtest Titano", "Cartella del backtest sorgente da cui è stata generata la rotazione Titano."),
            WithHelp(_sessionTitanoBacktest, "Cartella del backtest sorgente da cui è stata generata la rotazione Titano."),
            TitanoLabel("Setup Titano (run)", "Rotazione Titano salvata da collegare alla sessione. Premi 'Carica run' per elencare quelle disponibili nel backtest indicato."),
            WithHelp(_sessionTitanoRunId, "Rotazione Titano salvata da collegare alla sessione. Lascia vuoto per non collegare alcuna rotazione."),
            WithHelp(_sessionLoadTitanoRuns, "Elenca le rotazioni Titano già calcolate per il workspace e il backtest selezionati."),
            TitanoLabel("Modalità Titano", TitanoModeHelp),
            WithHelp(_sessionTitanoMode, TitanoModeHelp),
            TitanoLabel("Metadata symbol,DPP,min,step,mode",
                "Elenco strumenti nel formato simbolo,dollari-per-punto,quantità-minima,step-quantità,modalità-arrotondamento; più strumenti separati da ';'."),
            WithHelp(_sessionMetadata,
                "Elenco strumenti nel formato simbolo,dollari-per-punto,quantità-minima,step-quantità,modalità-arrotondamento; più strumenti separati da ';'."),
            _sessionAtrEnabled,
            TitanoLabel("Periodi ATR", "Numero di barre usate per calcolare l'ATR (Average True Range) su cui basare il sizing per volatilità."),
            WithHelp(_sessionAtrPeriods, "Numero di barre usate per calcolare l'ATR (Average True Range) su cui basare il sizing per volatilità."),
            TitanoLabel("Rischio target $", "Rischio in dollari che ogni posizione deve rappresentare quando il sizing ATR è attivo."),
            WithHelp(_sessionTargetRisk, "Rischio in dollari che ogni posizione deve rappresentare quando il sizing ATR è attivo."),
            _sessionPortfolioEnabled,
            TitanoLabel("DD cap %", "Drawdown massimo di portafoglio tollerato prima che i limiti di rischio riducano l'esposizione."),
            WithHelp(_sessionDrawdownCap, "Drawdown massimo di portafoglio tollerato prima che i limiti di rischio riducano l'esposizione."),
            _sessionCppiEnabled,
            TitanoLabel("Floor %", "Percentuale del capitale iniziale da proteggere come floor nell'overlay CPPI."),
            WithHelp(_sessionCppiFloor, "Percentuale del capitale iniziale da proteggere come floor nell'overlay CPPI."),
            TitanoLabel("Moltiplicatore", "Moltiplicatore CPPI applicato al cuscinetto (capitale sopra il floor) per determinare l'esposizione consentita."),
            WithHelp(_sessionCppiMultiplier, "Moltiplicatore CPPI applicato al cuscinetto (capitale sopra il floor) per determinare l'esposizione consentita.")
        });
        root.Controls.Add(config, 0, 0);

        _sessionCreate.Text = "Crea sessione";
        _sessionStart.Text = "Avvia"; _sessionStop.Text = "Ferma";
        _sessionResume.Text = "Riprendi"; _sessionSnapshot.Text = "Aggiorna snapshot";
        _sessionCreate.Click += async (_, _) => await CreateTradingSessionAsync();
        _sessionStart.Click += async (_, _) => await SetTradingSessionStatusAsync("start");
        _sessionStop.Click += async (_, _) => await SetTradingSessionStatusAsync("stop");
        _sessionResume.Click += async (_, _) => await SetTradingSessionStatusAsync("resume");
        _sessionSnapshot.Click += async (_, _) => await RefreshTradingSessionSnapshotAsync();
        root.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            Controls =
            {
                WithHelp(_sessionCreate, "Crea la sessione sul server con la configurazione impostata sopra (workspace, modalità, sizing)."),
                WithHelp(_sessionStart, "Avvia la sessione creata: da questo momento accetta barre e genera/esegue ordini."),
                WithHelp(_sessionStop, "Ferma la sessione attiva, interrompendo la generazione di nuovi ordini."),
                WithHelp(_sessionResume, "Riprende una sessione precedentemente fermata."),
                WithHelp(_sessionSnapshot, "Richiede al server lo stato corrente della sessione: saldo, equity, posizioni e ordini in sospeso.")
            }
        }, 0, 1);
        root.Controls.Add(BuildAccountGroupsPanel(), 0, 2);

        _sessionOutput.Dock = DockStyle.Fill;
        _sessionOutput.Multiline = true;
        _sessionOutput.ReadOnly = true;
        _sessionOutput.ScrollBars = ScrollBars.Both;
        _formToolTip.SetToolTip(_sessionOutput,
            "Mostra in formato JSON lo stato della sessione e, dopo 'Aggiorna snapshot', saldo/equity/posizioni/ordini in sospeso.");
        root.Controls.Add(_sessionOutput, 0, 3);
        return root;
    }

    private Control BuildAccountGroupsPanel()
    {
        var panel = new GroupBox
        {
            Text = "Gruppi account (anti copy-trading)",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, RowCount = 2, AutoSize = true };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _sessionAccountGroups.Width = 900;
        _sessionAccountGroups.Height = 140;
        _sessionAccountGroups.AllowUserToAddRows = true;
        _sessionAccountGroups.AllowUserToDeleteRows = true;
        _sessionAccountGroups.RowHeadersVisible = false;
        _sessionAccountGroups.AutoGenerateColumns = false;
        _sessionAccountGroups.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "GroupId", HeaderText = "Codice gruppo", FillWeight = 20,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _sessionAccountGroups.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "RotationSetupId", HeaderText = "Setup Titano", FillWeight = 25,
            DisplayMember = nameof(TitanoSetupInfo.Name), ValueMember = nameof(TitanoSetupInfo.Id),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _sessionAccountGroups.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "AccountNumber", HeaderText = "Codice account", FillWeight = 20,
            DisplayMember = nameof(AccountNumberListItem.DisplayText),
            ValueMember = nameof(AccountNumberListItem.AccountNumber),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _sessionAccountGroups.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MaxConcurrentTrades", HeaderText = "Max trade contemporanei", FillWeight = 15,
            ToolTipText = "0 = illimitato. Ignorato nel backtest senza Titano; applicato nel backtest Titano e in realtime."
        });
        _sessionAccountGroups.DataError += (_, e) => e.ThrowException = false;
        _sessionAccountGroups.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_sessionAccountGroups.IsCurrentCellDirty)
                _sessionAccountGroups.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _sessionAccountGroups.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0 || _sessionAccountGroups.Columns[e.ColumnIndex].Name != "AccountNumber")
                return;
            var row = _sessionAccountGroups.Rows[e.RowIndex];
            var accountNumber = Convert.ToString(row.Cells["AccountNumber"].Value);
            var account = _accounts.FirstOrDefault(item =>
                item.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account is not null && !string.IsNullOrWhiteSpace(account.GroupId))
                row.Cells["GroupId"].Value = account.GroupId;
        };
        _sessionAccountGroups.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "ApplyTitanoFilters", HeaderText = "Applica Titano", FillWeight = 12, TrueValue = true, FalseValue = false
        });
        _sessionAccountGroups.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TitanoRunId", HeaderText = "Run Titano", FillWeight = 23, ReadOnly = true
        });
        _formToolTip.SetToolTip(_sessionAccountGroups,
            "Un account per riga. Account con lo stesso 'Gruppo' (es. la stessa prop firm) non ricevono mai lo stesso segnale di ingresso; account di gruppi diversi sì.");
        layout.Controls.Add(_sessionAccountGroups, 0, 0);

        _sessionAddAccountGroupRow.Text = "Aggiungi riga";
        _sessionSaveAccountGroups.Text = "Salva gruppi account";
        _sessionReloadAccountGroups.Text = "Ricarica gruppi account";
        _sessionApplyTitanoToGroups.Text = "Genera e applica Titano";
        _sessionAddAccountGroupRow.Click += (_, _) => _sessionAccountGroups.Rows.Add();
        _sessionSaveAccountGroups.Click += async (_, _) => await SaveAccountGroupsAsync();
        _sessionReloadAccountGroups.Click += async (_, _) => await ReloadAccountGroupsAsync();
        _sessionApplyTitanoToGroups.Click += async (_, _) => await ApplyTitanoToGroupsAsync();
        layout.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            Controls =
            {
                WithHelp(_sessionAddAccountGroupRow, "Aggiunge una riga vuota alla griglia."),
                WithHelp(_sessionSaveAccountGroups,
                    "Salva gruppi, account e gli eventuali run Titano già generati."),
                WithHelp(_sessionApplyTitanoToGroups,
                    "Genera un run dai setup selezionati usando il backtest Titano della sessione e lo applica ai gruppi."),
                WithHelp(_sessionReloadAccountGroups, "Ricarica dalla sessione attiva la mappa account->gruppo corrente.")
            }
        }, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    private async Task SaveAccountGroupsAsync()
    {
        if (_activeSession is null)
        {
            MessageBox.Show("Crea prima una sessione ExternalBroker.", "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            NormalizeBaseAddress();
            var rows = ReadTradingGroupRows();

            using var request = new HttpRequestMessage(HttpMethod.Put,
                $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/groups")
            {
                Content = JsonContent.Create(new SetTradingGroupsRequest
                {
                    SessionToken = _activeSession.SessionToken,
                    Rows = rows
                }, options: _jsonOptions)
            };
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var snapshot = await response.Content.ReadFromJsonAsync<TradingSessionSnapshot>(_jsonOptions);
            ShowSession($"Gruppi account salvati ({rows.Count} account).");
            if (snapshot != null)
                _sessionOutput.Text += Environment.NewLine +
                    JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(_jsonOptions) { WriteIndented = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task ReloadAccountGroupsAsync()
    {
        if (_activeSession is null) return;
        try
        {
            NormalizeBaseAddress();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/groups");
            request.Headers.Add("X-Session-Token", _activeSession.SessionToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var accounts = await response.Content.ReadFromJsonAsync<List<TradingGroupRow>>(_jsonOptions) ?? [];
            _sessionAccountGroups.Rows.Clear();
            foreach (var mapping in accounts)
                _sessionAccountGroups.Rows.Add(
                    mapping.GroupId,
                    mapping.RotationSetupId,
                    mapping.AccountNumber,
                    mapping.MaxConcurrentTrades,
                    mapping.ApplyTitanoFilters,
                    mapping.TitanoRunId);
            ShowSession($"Caricati {accounts.Count} account configurati.");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private List<TradingGroupRow> ReadTradingGroupRows()
    {
        var backtestFolder = string.IsNullOrWhiteSpace(_sessionTitanoBacktest.Text)
            ? null
            : _sessionTitanoBacktest.Text.Trim();
        var rows = _sessionAccountGroups.Rows.Cast<DataGridViewRow>()
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
            throw new InvalidOperationException("Ogni riga deve contenere codice gruppo e codice account.");
        return rows;
    }

    private static int ParseMaxConcurrentTrades(DataGridViewRow row)
    {
        var raw = Convert.ToString(row.Cells["MaxConcurrentTrades"].Value ?? string.Empty)?.Trim();
        if (string.IsNullOrEmpty(raw))
            return 0;
        if (!int.TryParse(raw, out var value) || value < 0)
            throw new InvalidOperationException("Max trade contemporanei deve essere un intero maggiore o uguale a zero.");
        return value;
    }

    private async Task ApplyTitanoToGroupsAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            MessageBox.Show("Seleziona un workspace.", "Gruppi Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_sessionTitanoBacktest.Text))
        {
            MessageBox.Show("Indica la cartella Backtest Titano nella configurazione sessione.", "Gruppi Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var rows = ReadTradingGroupRows();
            var inconsistent = rows.GroupBy(row => row.GroupId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Select(row => row.RotationSetupId)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1);
            if (inconsistent is not null)
                throw new InvalidOperationException(
                    $"Tutte le righe del gruppo '{inconsistent.Key}' devono usare lo stesso setup Titano.");

            foreach (var group in rows.GroupBy(row => row.GroupId, StringComparer.OrdinalIgnoreCase))
            {
                var setupId = group.First().RotationSetupId;
                if (string.IsNullOrWhiteSpace(setupId))
                    continue;
                var setup = await _httpClient.GetFromJsonAsync<TitanoRotationSetup>(
                    $"api/Titano/rotation-setups/{Uri.EscapeDataString(setupId)}", _jsonOptions)
                    ?? throw new InvalidOperationException($"Setup Titano '{setupId}' non trovato.");
                var rotation = BuildTitanoRequest(
                    setup,
                    workspace.Info.Id,
                    _sessionTitanoBacktest.Text.Trim());
                var response = await _httpClient.PostAsJsonAsync("api/Titano/rotations", rotation, _jsonOptions);
                response.EnsureSuccessStatusCode();
                var manifest = await response.Content.ReadFromJsonAsync<TitanoRotationManifest>(_jsonOptions)
                    ?? throw new InvalidOperationException($"Manifest Titano non ricevuto per il gruppo '{group.Key}'.");

                foreach (DataGridViewRow gridRow in _sessionAccountGroups.Rows)
                    if (!gridRow.IsNewRow &&
                        string.Equals(Convert.ToString(gridRow.Cells["GroupId"].Value), group.Key,
                            StringComparison.OrdinalIgnoreCase))
                        gridRow.Cells["TitanoRunId"].Value = manifest.RunId;
            }

            if (_activeSession is not null)
                await SaveAccountGroupsAsync();
            else if (!string.IsNullOrWhiteSpace(_sessionPlanCode.Text))
                await SaveTradingPlanAsync();
            ShowSession("Setup Titano generati e applicati ai gruppi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore gruppi Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Control BuildTitanoTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildTitanoSourceGroup(), 0, 0);

        ConfigureTitanoControls();
        var settingsScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var settingsStack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        settingsStack.Controls.Add(BuildTitanoGroup("Setup rotazione", new Control[]
        {
            TitanoField("Setup salvato", _titanoSetupCombo,
                "Configurazione di parametri Titano salvata sul server."),
            WithHelp(_titanoLoadSetupButton, "Carica nella form il setup selezionato."),
            WithHelp(_titanoReloadSetupsButton, "Ricarica dal server l'elenco dei setup disponibili."),
            TitanoField("Nome", _titanoSetupName,
                "Nome leggibile con cui salvare o aggiornare il setup."),
            TitanoField("Descrizione e motivazioni", _titanoSetupDescription,
                "Motivazioni operative e di rischio alla base dei parametri scelti."),
            WithHelp(_titanoSaveSetupButton, "Salva sul server nome, descrizione e parametri correnti.")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Periodo di rotazione", new Control[]
        {
            TitanoField("Periodo", _titanoPeriodCombo,
                "Frequenza con cui Titano ricalcola i pesi delle strategie: settimanale, bisettimanale o mensile."),
            TitanoField("Trade minimi", _titanoMinimumTrades,
                "Numero minimo di trade richiesto per considerare statisticamente il periodo."),
            WithHelp(_titanoRequireEquityAboveMa,
                "Se attivo, l'equity deve essere sopra la propria media mobile per superare il filtro."),
            TitanoField("Start UTC", _titanoStartPicker,
                "Inizio dell'intervallo storico su cui simulare la rotazione (UTC)."),
            TitanoField("End UTC", _titanoEndPicker,
                "Fine dell'intervallo storico su cui simulare la rotazione (UTC).")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Finestre di analisi (momentum)", new Control[]
        {
            TitanoField("Breve gg", _titanoShortDays,
                "Giorni della finestra 'breve' usata per misurare la performance recente di ogni strategia."),
            TitanoField("Lunga gg", _titanoLongDays,
                "Giorni della finestra 'lunga' usata per misurare il trend di fondo di ogni strategia."),
            TitanoField("Media gg", _titanoMaDays,
                "Giorni della finestra usata per calcolare la media mobile dell'equity, base per lo z-score."),
            TitanoField("Min breve %", _titanoMinShortReturn,
                "Rendimento minimo nella finestra breve richiesto perché la strategia resti idonea."),
            TitanoField("Min lunga %", _titanoMinLongReturn,
                "Rendimento minimo nella finestra lunga richiesto perché la strategia resti idonea."),
            TitanoField("Z min", _titanoMinZ,
                "Limite inferiore accettabile per lo z-score dell'equity rispetto alla sua media mobile."),
            TitanoField("Z max", _titanoMaxZ,
                "Limite superiore accettabile per lo z-score dell'equity rispetto alla sua media mobile.")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Controllo rischio e drawdown", new Control[]
        {
            TitanoField("DD corrente %", _titanoMaxCurrentDd,
                "Drawdown corrente massimo tollerato: oltre questa soglia la strategia viene disattivata."),
            TitanoField("DD max %", _titanoMaxDd,
                "Drawdown massimo osservato tollerato nella finestra di analisi."),
            TitanoField("Volatilità %", _titanoMaxVolatility,
                "Volatilità massima dei rendimenti recenti tollerata prima di escludere la strategia."),
            TitanoField("DD riattiva %", _titanoReenableDd,
                "Soglia di drawdown corrente sotto la quale una strategia disattivata può tornare attiva."),
            TitanoField("Hard stop %", _titanoHardStop,
                "Drawdown oltre il quale la strategia viene bloccata definitivamente: serve un reset manuale per ripartire.")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Scoring e isteresi ON/OFF", new Control[]
        {
            TitanoField("Score OFF", _titanoDisableScore,
                "Punteggio composito sotto il quale una strategia attiva viene disattivata."),
            TitanoField("Score ON", _titanoReenableScore,
                "Punteggio composito sopra il quale una strategia disattivata può essere riattivata."),
            TitanoField("Cooldown", _titanoCooldown,
                "Periodi di attesa obbligatoria dopo una disattivazione, prima che la strategia possa rientrare."),
            TitanoField("Min ON", _titanoMinOn,
                "Periodi minimi consecutivi attivi richiesti prima che la strategia possa essere disattivata di nuovo."),
            TitanoField("Voti min", _titanoMinVotes,
                "Numero minimo di filtri superati (su 5) perché la strategia resti/diventi idonea.")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Sizing e costi", new Control[]
        {
            TitanoField("Tier sizing", _titanoSizingTiers,
                "Tabella soglia punteggio=percentuale allocazione, es. 0.80=100%; 0.60=50%; 0.40=25%; 0=0%."),
            TitanoField("Commissione/unità", _titanoCommission,
                "Commissione applicata per unità nella simulazione dell'equity filtrata da Titano."),
            TitanoField("Slippage/unità", _titanoSlippage,
                "Slippage applicato per unità nella simulazione dell'equity filtrata da Titano.")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Validazione walk-forward", new Control[]
        {
            TitanoField("Calibrazione", _titanoCalibration,
                "Numero di periodi usati per calibrare la rotazione prima di ogni finestra di verifica."),
            TitanoField("OOS", _titanoEvaluation,
                "Numero di periodi di verifica out-of-sample valutati dopo ogni calibrazione."),
            TitanoField("Walk-forward", _titanoWalkForwardMode,
                "Modalità di ricalibrazione: Rolling (finestra scorrevole) o Expanding (finestra che si allarga nel tempo).")
        }));

        settingsStack.Controls.Add(BuildTitanoGroup("Azioni", new Control[]
        {
            WithHelp(_runTitanoButton,
                "Invia la configurazione al server ed esegue la rotazione Titano sul backtest selezionato."),
            WithHelp(_openTitanoReportButton,
                "Apre il report HTML del risultato calcolato con i parametri correnti, incluso il confronto delle equity."),
            WithHelp(_titanoResetHardStopButton,
                "Rimuove il blocco hard-stop di una strategia dal prossimo periodo, richiedendo motivo e responsabile.")
        }));

        settingsScroll.Controls.Add(settingsStack);
        root.Controls.Add(settingsScroll, 0, 1);

        _titanoPathLabel.Text = "Seleziona workspace e backtest per collegare Titano ai risultati.";
        _titanoPathLabel.AutoSize = true;
        _titanoPathLabel.Dock = DockStyle.Top;
        _titanoResultsTextBox.Multiline = true;
        _titanoResultsTextBox.ReadOnly = true;
        _titanoResultsTextBox.ScrollBars = ScrollBars.Both;
        _titanoResultsTextBox.WordWrap = false;
        _titanoResultsTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(new GroupBox
        {
            Text = "Run, periodi, strategie e metriche",
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Controls = { _titanoResultsTextBox, _titanoPathLabel }
        }, 0, 2);
        root.Controls.Add(new Label
        {
            Text = "Titano usa come input/base la cartella risultati del backtest selezionato; i suoi report sono salvati nella sottocartella titano.",
            AutoSize = true,
            Dock = DockStyle.Bottom
        }, 0, 3);
        return root;
    }

    private Control BuildTitanoSourceGroup()
    {
        _titanoWorkspaceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoWorkspaceCombo.Width = 360;
        _titanoWorkspaceCombo.DisplayMember = nameof(WorkspaceListItem.DisplayText);
        _titanoWorkspaceCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (!_suppressWorkspaceEvents)
                await LoadTitanoBacktestsAsync();
        };
        _titanoBacktestCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoBacktestCombo.Width = 430;
        _titanoBacktestCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateTitanoPath();
            ApplySelectedBacktestPeriodToTitano();
        };
        _refreshTitanoBacktestsButton.Text = "Aggiorna backtest";
        _refreshTitanoBacktestsButton.AutoSize = true;
        _refreshTitanoBacktestsButton.Click += async (_, _) => await LoadTitanoBacktestsAsync(showErrors: true);
        _openTitanoFolderButton.Text = "Apri cartella risultati";
        _openTitanoFolderButton.AutoSize = true;
        _openTitanoFolderButton.Enabled = false;
        _openTitanoFolderButton.Click += (_, _) => OpenSelectedTitanoFolder();

        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        stack.Controls.Add(TitanoField("Workspace obbligatorio", _titanoWorkspaceCombo,
            "Workspace da cui Titano legge il master filter e a cui appartiene il backtest selezionato."));
        stack.Controls.Add(new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Controls =
            {
                TitanoField("Backtest", _titanoBacktestCombo,
                    "Backtest sorgente: Titano usa i trade salvati in questa cartella come base per la rotazione."),
                WithHelp(_refreshTitanoBacktestsButton,
                    "Ricarica l'elenco dei backtest disponibili per il workspace selezionato."),
                WithHelp(_openTitanoFolderButton,
                    "Apre la cartella dei risultati del backtest/rotazione selezionata in Esplora file.")
            }
        });

        return new GroupBox
        {
            Text = "Sorgente dati",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 18, 8, 8),
            Controls = { stack }
        };
    }

    private static GroupBox BuildTitanoGroup(string title, IEnumerable<Control> fields)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Width = 1080
        };
        flow.Controls.AddRange(fields.ToArray());
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 1100,
            Padding = new Padding(8, 18, 8, 8),
            Controls = { flow }
        };
    }

    private Control TitanoField(string label, Control control, string help)
    {
        _formToolTip.SetToolTip(control, help);
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(2, 2, 14, 8)
        };
        panel.Controls.Add(TitanoLabel(label, help));
        panel.Controls.Add(control);
        return panel;
    }

    private Control WithHelp(Control control, string help)
    {
        _formToolTip.SetToolTip(control, help);
        return control;
    }

    private void ConfigureTitanoControls()
    {
        _titanoSetupCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoSetupCombo.DisplayMember = nameof(TitanoSetupInfo.Name);
        _titanoSetupCombo.Width = 240;
        _titanoSetupName.Width = 220;
        _titanoSetupDescription.Multiline = true;
        _titanoSetupDescription.Width = 420;
        _titanoSetupDescription.Height = 64;
        _titanoLoadSetupButton.Text = "Carica";
        _titanoLoadSetupButton.AutoSize = true;
        _titanoLoadSetupButton.Click += async (_, _) => await LoadSelectedTitanoSetupAsync();
        _titanoReloadSetupsButton.Text = "Aggiorna setup";
        _titanoReloadSetupsButton.AutoSize = true;
        _titanoReloadSetupsButton.Click += async (_, _) => await LoadTitanoSetupsAsync();
        _titanoSaveSetupButton.Text = "Salva setup";
        _titanoSaveSetupButton.AutoSize = true;
        _titanoSaveSetupButton.Click += async (_, _) => await SaveTitanoSetupAsync();
        _titanoPeriodCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoPeriodCombo.Items.AddRange(Enum.GetNames<TitanoRotationPeriod>());
        _titanoPeriodCombo.SelectedItem = nameof(TitanoRotationPeriod.Weekly);
        ConfigureTitanoNumber(_titanoMinimumTrades, 1, 1000, 1);
        _titanoRequireEquityAboveMa.Checked = true;
        ConfigureTitanoDate(_titanoStartPicker, DateTime.UtcNow.Date.AddYears(-2));
        ConfigureTitanoDate(_titanoEndPicker, DateTime.UtcNow.Date);
        ConfigureTitanoNumber(_titanoShortDays, 1, 2000, 90);
        ConfigureTitanoNumber(_titanoLongDays, 1, 3000, 365);
        ConfigureTitanoNumber(_titanoMaDays, 1, 2000, 90);
        ConfigureTitanoNumber(_titanoMinShortReturn, -100, 1000, 0, 2);
        ConfigureTitanoNumber(_titanoMinLongReturn, -100, 1000, 0, 2);
        ConfigureTitanoNumber(_titanoMinZ, -10, 10, -1.5m, 2);
        ConfigureTitanoNumber(_titanoMaxZ, -10, 10, 2.5m, 2);
        ConfigureTitanoNumber(_titanoMaxCurrentDd, 0, 100, 15, 2);
        ConfigureTitanoNumber(_titanoMaxDd, 0, 100, 25, 2);
        ConfigureTitanoNumber(_titanoMaxVolatility, 0, 100, 10, 2);
        ConfigureTitanoNumber(_titanoReenableDd, 0, 100, 10, 2);
        ConfigureTitanoNumber(_titanoDisableScore, 0, 1, 0.40m, 2);
        ConfigureTitanoNumber(_titanoReenableScore, 0, 1, 0.60m, 2);
        ConfigureTitanoNumber(_titanoCooldown, 0, 100, 2);
        ConfigureTitanoNumber(_titanoMinOn, 0, 100, 1);
        ConfigureTitanoNumber(_titanoMinVotes, 0, 5, 4);
        ConfigureTitanoNumber(_titanoHardStop, 0, 100, 35, 2);
        ConfigureTitanoNumber(_titanoCommission, 0, 10000, 0, 2);
        ConfigureTitanoNumber(_titanoSlippage, 0, 10000, 0, 2);
        ConfigureTitanoNumber(_titanoCalibration, 1, 1000, 8);
        ConfigureTitanoNumber(_titanoEvaluation, 1, 1000, 4);
        _titanoWalkForwardMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoWalkForwardMode.Items.AddRange(Enum.GetNames<TitanoWalkForwardMode>());
        _titanoWalkForwardMode.SelectedItem = nameof(TitanoWalkForwardMode.Rolling);
        _titanoSizingTiers.Width = 250;
        _runTitanoButton.Text = "Avvia Titano via HTTP";
        _runTitanoButton.AutoSize = true;
        _runTitanoButton.Click += async (_, _) => await RunTitanoAsync();
        _openTitanoReportButton.Text = "Apri risultato Titano";
        _openTitanoReportButton.AutoSize = true;
        _openTitanoReportButton.Enabled = false;
        _openTitanoReportButton.Click += async (_, _) => await OpenTitanoReportAsync();
        _titanoResetHardStopButton.Text = "Reset hard-stop…";
        _titanoResetHardStopButton.AutoSize = true;
        _titanoResetHardStopButton.Enabled = false;
        _titanoResetHardStopButton.Click += async (_, _) => await ResetTitanoHardStopAsync();
    }

    private Label TitanoLabel(string text, string? help = null)
    {
        var label = new Label { Text = text, AutoSize = true, Padding = new Padding(6, 7, 2, 0) };
        if (!string.IsNullOrEmpty(help)) _formToolTip.SetToolTip(label, help);
        return label;
    }
    private static void ConfigureTitanoDate(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "yyyy-MM-dd";
        picker.Value = value;
        picker.Width = 110;
    }
    private static void ConfigureTitanoNumber(NumericUpDown input, decimal min, decimal max, decimal value, int decimals = 0)
    {
        input.Minimum = min; input.Maximum = max; input.Value = value; input.DecimalPlaces = decimals; input.Width = 70;
    }

    private Control BuildParametersPanel()
    {
        var group = new GroupBox { Text = "Parametri backtesting", Dock = DockStyle.Top, Height = 205 };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 5,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        group.Controls.Add(layout);

        _startDatePicker.Format = DateTimePickerFormat.Custom;
        _startDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _startDatePicker.Value = CreateUtcPickerDefault(DateTime.UtcNow.Date.AddYears(-1));
        _startDatePicker.Width = 180;

        _endDatePicker.Format = DateTimePickerFormat.Custom;
        _endDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _endDatePicker.Value = CreateUtcPickerDefault(DateTime.UtcNow);
        _endDatePicker.Width = 180;

        _initialCapitalInput.Minimum = 0;
        _initialCapitalInput.Maximum = 1_000_000_000;
        _initialCapitalInput.DecimalPlaces = 2;
        _initialCapitalInput.Increment = 1000;
        _initialCapitalInput.Value = 1_000_000;
        _initialCapitalInput.Width = 140;

        _commissionInput.Minimum = 0;
        _commissionInput.Maximum = 10000;
        _commissionInput.DecimalPlaces = 2;
        _commissionInput.Increment = 0.5m;
        _commissionInput.Value = 2;
        _commissionInput.Width = 120;

        _basePathTextBox.Dock = DockStyle.Fill;

        _backtestingWorkspaceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _backtestingWorkspaceCombo.Dock = DockStyle.Fill;
        _backtestingWorkspaceCombo.DisplayMember = nameof(WorkspaceListItem.DisplayText);
        _backtestingWorkspaceCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateBacktestingWorkspaceHint();
            if (_suppressWorkspaceEvents) return;
            await LoadBacktestingMasterFilterSummaryAsync();
        };

        _backtestingAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _backtestingAccountCombo.Dock = DockStyle.Fill;
        _backtestingAccountCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressAccountEvents) return;
            if (_backtestingAccountCombo.SelectedItem is AccountListItem item && item.Account.InitialBalance > 0)
                _initialCapitalInput.Value = Math.Clamp(
                    item.Account.InitialBalance,
                    _initialCapitalInput.Minimum,
                    _initialCapitalInput.Maximum);
        };

        _backtestingWorkspaceHint.AutoSize = true;
        _backtestingWorkspaceHint.Text = "Seleziona un workspace: le strategie eseguite verranno prese dal masterfilter.";

        layout.Controls.Add(new Label { Text = "Workspace", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_backtestingWorkspaceCombo, 1, 0);
        layout.SetColumnSpan(_backtestingWorkspaceCombo, 3);
        layout.Controls.Add(_backtestingWorkspaceHint, 4, 0);
        layout.SetColumnSpan(_backtestingWorkspaceHint, 2);

        _backtestNameTextBox.Dock = DockStyle.Fill;
        layout.Controls.Add(new Label { Text = "Nome backtest *", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_backtestNameTextBox, 1, 1);
        layout.SetColumnSpan(_backtestNameTextBox, 3);

        layout.Controls.Add(new Label { Text = "Start (UTC)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_startDatePicker, 1, 2);
        layout.Controls.Add(new Label { Text = "End (UTC)", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 2);
        layout.Controls.Add(_endDatePicker, 3, 2);
        layout.Controls.Add(new Label { Text = "Capitale", AutoSize = true, Anchor = AnchorStyles.Left }, 4, 2);
        layout.Controls.Add(_initialCapitalInput, 5, 2);

        layout.Controls.Add(new Label { Text = "Commissione/contratto", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(_commissionInput, 1, 3);
        layout.Controls.Add(new Label { Text = "Repository base", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 3);
        layout.SetColumnSpan(_basePathTextBox, 3);
        layout.Controls.Add(_basePathTextBox, 3, 3);

        layout.Controls.Add(new Label { Text = "Account", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        layout.Controls.Add(WithHelp(_backtestingAccountCombo,
            "Applica al run la tabella di conversione dell'account: scala la size con il moltiplicatore " +
            "contratto e riporta il symbol account nei signal. Senza account il run resta 1 a 1."), 1, 4);
        layout.SetColumnSpan(_backtestingAccountCombo, 3);

        _reloadButton.Text = "Ricarica strategie";
        _reloadButton.AutoSize = true;
        _reloadButton.Click += (_, _) => ReloadSettingsAndStrategies();
        layout.Controls.Add(_reloadButton, 5, 4);

        return group;
    }

    private Control BuildStrategyPanel()
    {
        var group = new GroupBox { Text = "Strategie eseguite (masterfilter)", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        group.Controls.Add(layout);

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _backtestingMasterFilterSummary.AutoSize = true;
        _backtestingMasterFilterSummary.Padding = new Padding(0, 7, 12, 0);
        _backtestingMasterFilterSummary.Text = "Seleziona un workspace per vedere le strategie.";
        _editMasterFilterButton.Text = "Modifica masterfilter";
        _editMasterFilterButton.AutoSize = true;
        _editMasterFilterButton.Enabled = false;
        _editMasterFilterButton.Click += (_, _) => OpenSelectedMasterFilter();
        header.Controls.Add(_backtestingMasterFilterSummary);
        header.Controls.Add(_editMasterFilterButton);
        layout.Controls.Add(header, 0, 0);

        _backtestingMasterFilterStrategies.Dock = DockStyle.Fill;
        _backtestingMasterFilterStrategies.HorizontalScrollbar = true;
        _backtestingMasterFilterStrategies.SelectionMode = SelectionMode.None;
        layout.Controls.Add(_backtestingMasterFilterStrategies, 0, 1);

        return group;
    }

    private Control BuildActionsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 6,
            RowCount = 1,
            Height = 48,
            Padding = new Padding(0, 8, 0, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _runButton.Text = "Avvia backtesting";
        _runButton.AutoSize = true;
        _runButton.Click += async (_, _) => await RunBacktestingAsync();

        _cancelBacktestButton.Text = "Interrompi backtest";
        _cancelBacktestButton.AutoSize = true;
        _cancelBacktestButton.Enabled = false;
        _cancelBacktestButton.Click += async (_, _) => await CancelBacktestingAsync();

        _openReportButton.Text = "Apri report HTML";
        _openReportButton.AutoSize = true;
        _openReportButton.Enabled = false;
        _openReportButton.Click += (_, _) => OpenLastReport();

        _progressBar.Width = 240;
        _progressBar.Style = ProgressBarStyle.Continuous;

        _statusLabel.AutoSize = true;
        _statusLabel.Text = "Pronto";
        _statusLabel.Anchor = AnchorStyles.Left;

        panel.Controls.Add(_runButton, 0, 0);
        panel.Controls.Add(_cancelBacktestButton, 1, 0);
        panel.Controls.Add(_openReportButton, 2, 0);
        panel.Controls.Add(_statusLabel, 3, 0);
        panel.Controls.Add(_progressBar, 4, 0);

        return panel;
    }

    private Control BuildLogPanel()
    {
        var group = new GroupBox { Text = "Log", Dock = DockStyle.Fill };
        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        group.Controls.Add(_logTextBox);
        return group;
    }

    private async Task InitializeClientAsync()
    {
        try
        {
            _basePathTextBox.Text = "Gestito dal server";
            _basePathTextBox.ReadOnly = true;
            await WaitForServerAndLoadStrategiesAsync();
            PopulateWorkspaceStrategiesChecklist(Array.Empty<string>());
            await LoadTitanoSetupsAsync();
            Log("Client inizializzato.");
            await ReloadWorkspacesAsync(showErrors: false);
        }
        catch (Exception ex)
        {
            Log($"Errore inizializzazione: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ReloadSettingsAndStrategies()
    {
        try
        {
            NormalizeBaseAddress();
            await LoadStrategiesAsync();
            var selectedIds = GetCheckedWorkspaceStrategyIds();
            PopulateWorkspaceStrategiesChecklist(selectedIds);
            Log("Catalogo strategie ricaricato dal server.");
        }
        catch (Exception ex)
        {
            Log($"Errore ricarica: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadStrategiesAsync()
    {
        NormalizeBaseAddress();
        _strategies = (await _workspaceApi.ListStrategiesAsync()).ToList();
        Log($"Caricate {_strategies.Count} strategie dal catalogo HTTP.");
    }

    private async Task WaitForServerAndLoadStrategiesAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? lastError = null;
        _statusLabel.Text = "Attesa API…";

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await LoadStrategiesAsync();
                _statusLabel.Text = "Pronto";
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                lastError = ex;
                await Task.Delay(500);
            }
        }

        throw new InvalidOperationException(
            "Il server API non è diventato disponibile entro 30 secondi.",
            lastError);
    }

    private void PopulateWorkspaceStrategiesChecklist(IEnumerable<string> selectedIds)
    {
        _workspaceStrategiesList.SetStrategies(_strategies, selectedIds);
    }

    private List<string> GetCheckedWorkspaceStrategyIds()
        => _workspaceStrategiesList.GetSelectedIds();

    private void NormalizeBaseAddress()
        => _workspaceApi.SetBaseAddress(_serverUrlTextBox.Text);

    private async Task ReloadWorkspacesAsync(bool showErrors)
    {
        try
        {
            NormalizeBaseAddress();
            _workspaces = (await _workspaceApi.ListAsync()).ToList();
            await BindWorkspaceSelectorsAsync(preserveSelection: true);
            if (_workspaceList.SelectedItem != null)
            {
                await LoadSelectedWorkspaceDetailsAsync();
            }
            await LoadTitanoBacktestsAsync();
            await ReloadAccountsAsync(showErrors: false);
            await LoadBacktestingAccountsAsync();

            Log($"Caricati {_workspaces.Count} workspace da API.");
        }
        catch (Exception ex)
        {
            Log($"Errore caricamento workspace: {ex.Message}");
            if (showErrors)
            {
                MessageBox.Show(
                    $"Impossibile caricare i workspace.{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                    "Verifica che PiootooApp.Server sia in esecuzione e l'URL API sia corretto.",
                    "Workspace",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private async Task BindWorkspaceSelectorsAsync(bool preserveSelection)
    {
        var selectedId = GetSelectedWorkspaceId()
            ?? (_workspaceList.SelectedItem as WorkspaceListItem)?.Info.Id;

        _suppressWorkspaceEvents = true;
        try
        {
            _workspaceList.Items.Clear();
            _backtestingWorkspaceCombo.Items.Clear();
            _titanoWorkspaceCombo.Items.Clear();
            _tradingResultsWorkspaceCombo.Items.Clear();
            _rotationsWorkspaceCombo.Items.Clear();
            _sessionWorkspaceCombo.Items.Clear();

            foreach (var workspace in _workspaces)
            {
                var item = new WorkspaceListItem(workspace);
                _workspaceList.Items.Add(item);
                _backtestingWorkspaceCombo.Items.Add(item);
                _titanoWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
                _tradingResultsWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
                _rotationsWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
                _sessionWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
            }

            if (!string.IsNullOrWhiteSpace(selectedId) && preserveSelection)
            {
                SelectWorkspaceEverywhere(selectedId);
            }
            else if (_workspaceList.Items.Count > 0)
            {
                _workspaceList.SelectedIndex = 0;
                if (_backtestingWorkspaceCombo.Items.Count > 0)
                {
                    _backtestingWorkspaceCombo.SelectedIndex = 0;
                }

                if (_titanoWorkspaceCombo.Items.Count > 0)
                {
                    _titanoWorkspaceCombo.SelectedIndex = 0;
                }
                if (_tradingResultsWorkspaceCombo.Items.Count > 0)
                    _tradingResultsWorkspaceCombo.SelectedIndex = 0;
                if (_rotationsWorkspaceCombo.Items.Count > 0)
                    _rotationsWorkspaceCombo.SelectedIndex = 0;
                if (_sessionWorkspaceCombo.Items.Count > 0) _sessionWorkspaceCombo.SelectedIndex = 0;
            }
            else
            {
                _deleteWorkspaceButton.Enabled = false;
                _saveMasterFilterButton.Enabled = false;
                _workspaceNameTextBox.Text = string.Empty;
                _workspaceDetailLabel.Text = "Nessun workspace disponibile. Crea un nuovo workspace.";
                PopulateWorkspaceStrategiesChecklist(Array.Empty<string>());
            }
        }
        finally
        {
            _suppressWorkspaceEvents = false;
        }

        UpdateBacktestingWorkspaceHint();
        await LoadBacktestingMasterFilterSummaryAsync();
        _deleteWorkspaceButton.Enabled = _workspaceList.SelectedItem != null;
        _saveMasterFilterButton.Enabled = _workspaceList.SelectedItem != null;
    }

    private void SelectWorkspaceEverywhere(string workspaceId)
    {
        for (var index = 0; index < _workspaceList.Items.Count; index++)
        {
            if (_workspaceList.Items[index] is WorkspaceListItem item &&
                item.Info.Id.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                _workspaceList.SelectedIndex = index;
                break;
            }
        }

        SelectComboWorkspace(_backtestingWorkspaceCombo, workspaceId);
        SelectComboWorkspace(_titanoWorkspaceCombo, workspaceId);
        SelectComboWorkspace(_tradingResultsWorkspaceCombo, workspaceId);
        SelectComboWorkspace(_rotationsWorkspaceCombo, workspaceId);
    }

    private static void SelectComboWorkspace(ComboBox combo, string workspaceId)
    {
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (combo.Items[index] is WorkspaceListItem item &&
                item.Info.Id.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                return;
            }
        }
    }

    private string? GetSelectedWorkspaceId()
    {
        if (_backtestingWorkspaceCombo.SelectedItem is WorkspaceListItem backtestingItem)
        {
            return backtestingItem.Info.Id;
        }

        if (_workspaceList.SelectedItem is WorkspaceListItem listItem)
        {
            return listItem.Info.Id;
        }

        return null;
    }

    private async Task OnWorkspaceSelectionChangedAsync()
    {
        if (_suppressWorkspaceEvents)
        {
            return;
        }

        if (_workspaceList.SelectedItem is not WorkspaceListItem item)
        {
            _deleteWorkspaceButton.Enabled = false;
            _saveMasterFilterButton.Enabled = false;
            return;
        }

        SelectComboWorkspace(_backtestingWorkspaceCombo, item.Info.Id);
        SelectComboWorkspace(_titanoWorkspaceCombo, item.Info.Id);
        UpdateBacktestingWorkspaceHint();
        await LoadSelectedWorkspaceDetailsAsync();
    }

    private async Task LoadSelectedWorkspaceDetailsAsync()
    {
        if (_workspaceList.SelectedItem is not WorkspaceListItem item)
        {
            _deleteWorkspaceButton.Enabled = false;
            _saveMasterFilterButton.Enabled = false;
            return;
        }

        _deleteWorkspaceButton.Enabled = true;
        _saveMasterFilterButton.Enabled = true;

        try
        {
            NormalizeBaseAddress();
            var filter = await _workspaceApi.GetMasterFilterAsync(item.Info.Id);
            _workspaceNameTextBox.Text = filter.Name;
            _workspaceDetailLabel.Text = $"Id: {item.Info.Id} · {filter.StrategiesFilter.Count} strategie nel masterfilter";
            PopulateWorkspaceStrategiesChecklist(filter.StrategiesFilter);
            Log($"Masterfilter caricato per '{item.Info.Id}'.");
        }
        catch (Exception ex)
        {
            Log($"Errore masterfilter: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateBacktestingWorkspaceHint()
    {
        if (_backtestingWorkspaceCombo.SelectedItem is WorkspaceListItem item)
        {
            _backtestingWorkspaceHint.Text =
                $"Workspace attivo: {item.Info.Name} ({item.Info.Id}) · {item.Info.StrategiesCount} strategie";
        }
        else
        {
            _backtestingWorkspaceHint.Text =
                "Seleziona un workspace: le strategie eseguite verranno prese dal masterfilter.";
        }
    }

    private async Task LoadBacktestingMasterFilterSummaryAsync()
    {
        _backtestingMasterFilterStrategies.Items.Clear();
        _editMasterFilterButton.Enabled = false;

        if (_backtestingWorkspaceCombo.SelectedItem is not WorkspaceListItem item)
        {
            _backtestingMasterFilterSummary.Text = "Seleziona un workspace per vedere le strategie.";
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var filter = await _workspaceApi.GetMasterFilterAsync(item.Info.Id);
            var strategyIds = filter.StrategiesFilter.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var strategies = _strategies
                .Where(strategy => strategyIds.Contains(strategy.Id))
                .OrderBy(strategy => strategy.Symbol)
                .ThenBy(strategy => strategy.Name)
                .ToList();
            var knownIds = strategies.Select(strategy => strategy.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var strategy in strategies)
                _backtestingMasterFilterStrategies.Items.Add(new StrategyListItem(strategy));
            foreach (var missingId in strategyIds.Where(id => !knownIds.Contains(id)).OrderBy(id => id))
                _backtestingMasterFilterStrategies.Items.Add($"Strategia non disponibile: {missingId}");

            _backtestingMasterFilterSummary.Text =
                $"{filter.StrategiesFilter.Count} strategie saranno eseguite. La selezione deriva esclusivamente dal masterfilter.";
            _editMasterFilterButton.Enabled = true;
        }
        catch (Exception ex)
        {
            _backtestingMasterFilterSummary.Text = $"Impossibile leggere il masterfilter: {ex.Message}";
        }
    }

    private void OpenSelectedMasterFilter()
    {
        if (_backtestingWorkspaceCombo.SelectedItem is not WorkspaceListItem item ||
            _mainTabs == null ||
            _workspacesTab == null)
            return;

        SelectComboWorkspace(_backtestingWorkspaceCombo, item.Info.Id);
        SelectComboWorkspace(_titanoWorkspaceCombo, item.Info.Id);
        for (var index = 0; index < _workspaceList.Items.Count; index++)
        {
            if (_workspaceList.Items[index] is WorkspaceListItem workspace &&
                workspace.Info.Id.Equals(item.Info.Id, StringComparison.OrdinalIgnoreCase))
            {
                _workspaceList.SelectedIndex = index;
                break;
            }
        }
        _mainTabs.SelectedTab = _workspacesTab;
    }

    private async Task CreateWorkspaceAsync()
    {
        using var dialog = new Form
        {
            Text = "Nuovo workspace",
            Width = 720,
            Height = 560,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ShowInTaskbar = false
        };

        var nameBox = new TextBox { Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8) };
        var strategiesList = new FilterableStrategyChecklist { Dock = DockStyle.Fill };
        strategiesList.SetStrategies(_strategies, Array.Empty<string>());

        var selectAll = new Button { Text = "Seleziona tutto", AutoSize = true };
        selectAll.Click += (_, _) => strategiesList.SetAll(true);
        var clearAll = new Button { Text = "Deseleziona", AutoSize = true };
        clearAll.Click += (_, _) => strategiesList.SetAll(false);

        var okButton = new Button { Text = "Crea", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Annulla", DialogResult = DialogResult.Cancel, AutoSize = true };
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(clearAll);
        buttons.Controls.Add(selectAll);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = "Nome workspace (obbligatorio). L'id filesystem verrà derivato dal nome.",
            AutoSize = true
        }, 0, 0);
        layout.Controls.Add(nameBox, 0, 1);
        layout.Controls.Add(new GroupBox
        {
            Text = "Strategie iniziali del masterfilter",
            Dock = DockStyle.Fill,
            Controls = { strategiesList }
        }, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        dialog.Controls.Add(layout);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Inserisci un nome workspace.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var strategyIds = strategiesList.GetSelectedIds();

        try
        {
            NormalizeBaseAddress();
            var created = await _workspaceApi.CreateAsync(new CreateWorkspaceRequest
            {
                Name = name,
                StrategiesFilter = strategyIds
            });
            Log($"Workspace creato: {created.Name} ({created.Id}), {created.StrategiesCount} strategie.");
            await ReloadWorkspacesAsync(showErrors: true);
            SelectWorkspaceEverywhere(created.Id);
            await LoadSelectedWorkspaceDetailsAsync();
        }
        catch (Exception ex)
        {
            Log($"Errore creazione workspace: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveSelectedMasterFilterAsync()
    {
        if (_workspaceList.SelectedItem is not WorkspaceListItem item)
        {
            MessageBox.Show("Seleziona un workspace da modificare.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var name = _workspaceNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Il nome del workspace è obbligatorio.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var saved = await _workspaceApi.SaveMasterFilterAsync(item.Info.Id, new WorkspaceMasterFilter
            {
                Name = name,
                StrategiesFilter = GetCheckedWorkspaceStrategyIds()
            });
            Log($"Masterfilter salvato per '{item.Info.Id}' ({saved.StrategiesFilter.Count} strategie).");
            await ReloadWorkspacesAsync(showErrors: true);
            SelectWorkspaceEverywhere(item.Info.Id);
            await LoadSelectedWorkspaceDetailsAsync();
        }
        catch (Exception ex)
        {
            Log($"Errore salvataggio masterfilter: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedWorkspaceAsync()
    {
        if (_workspaceList.SelectedItem is not WorkspaceListItem item)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Eliminare definitivamente il workspace '{item.Info.Name}' ({item.Info.Id})?",
            "Conferma eliminazione",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            NormalizeBaseAddress();
            await _workspaceApi.DeleteAsync(item.Info.Id);
            Log($"Workspace eliminato: {item.Info.Id}");
            await ReloadWorkspacesAsync(showErrors: true);
        }
        catch (Exception ex)
        {
            Log($"Errore eliminazione workspace: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RunBacktestingAsync()
    {
        if (_backtestingWorkspaceCombo.SelectedItem is not WorkspaceListItem workspaceItem)
        {
            MessageBox.Show(
                "Seleziona un workspace nel tab Backtesting (o creane uno dal tab Workspaces).",
                "Validazione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var backtestName = _backtestNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(backtestName))
        {
            MessageBox.Show("Il nome del backtest è obbligatorio.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = await _workspaceApi.ListBacktestsAsync(workspaceItem.Info.Id);
        var overwriteExisting = existing.Any(item =>
            item.FolderName.Equals(backtestName, StringComparison.OrdinalIgnoreCase));
        if (overwriteExisting && MessageBox.Show(
                "Esiste già un backtest con questo nome. Sostituirlo?",
                "Sostituire backtest esistente?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        if (_endDatePicker.Value <= _startDatePicker.Value)
        {
            MessageBox.Show("La data end deve essere successiva alla data start.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        WorkspaceMasterFilter masterFilter;
        try
        {
            NormalizeBaseAddress();
            masterFilter = await _workspaceApi.GetMasterFilterAsync(workspaceItem.Info.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile leggere il masterfilter del workspace.{Environment.NewLine}{ex.Message}",
                "Errore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (masterFilter.StrategiesFilter.Count == 0)
        {
            MessageBox.Show(
                "Il workspace non contiene strategie abilitate nel masterfilter. Aggiungile dal tab Workspaces e salva.",
                "Validazione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var request = new BacktestingRequest
        {
            WorkspaceId = workspaceItem.Info.Id,
            BacktestFolderName = backtestName,
            OverwriteExistingBacktest = overwriteExisting,
            Name = backtestName,
            StartDate = DateTime.SpecifyKind(_startDatePicker.Value, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(_endDatePicker.Value, DateTimeKind.Utc),
            InitialCapital = _initialCapitalInput.Value,
            CommissionPerContract = _commissionInput.Value,
            AccountId = (_backtestingAccountCombo.SelectedItem as AccountListItem)?.Account.Id
        };

        SetRunningState(true);
        _pollingCts?.Dispose();
        _pollingCts = new CancellationTokenSource();
        _lastResult = null;
        _openReportButton.Enabled = false;
        _progressBar.Value = 0;

        try
        {
            Log($"Workspace: {workspaceItem.Info.Name} ({workspaceItem.Info.Id})");
            Log($"Backtest server-side: {request.Name}");
            Log($"Avvio backtesting UTC: {request.StartDate:yyyy-MM-dd HH:mm}Z - {request.EndDate:yyyy-MM-dd HH:mm}Z");
            Log($"Strategie da masterfilter: {masterFilter.StrategiesFilter.Count}");

            NormalizeBaseAddress();
            var jobId = await _workspaceApi.StartBacktestingAsync(request);
            _lastJobId = jobId;
            await PollJobAsync(jobId, _pollingCts.Token);
            await LoadTitanoBacktestsAsync();
        }
        catch (OperationCanceledException) when (_pollingCts?.IsCancellationRequested == true)
        {
            Log("Polling locale interrotto.");
        }
        catch (Exception ex)
        {
            Log($"Errore backtesting: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _pollingCts?.Dispose();
            _pollingCts = null;
            SetRunningState(false);
        }
    }

    private async Task PollJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await _workspaceApi.PollBacktestingUntilTerminalAsync(
            jobId,
            job =>
            {
            var progress = Math.Clamp(job.ProgressPercent, 0, 100);
            _progressBar.Value = progress;
                _statusLabel.Text = $"{job.Phase} - {progress}% · {job.ProgressMessage}";
            },
            timeout: null,
            cancellationToken);

        if (job.Status == BacktestingJobStatus.Completed)
        {
            _lastResult = await _workspaceApi.GetBacktestingResultAsync(jobId, cancellationToken);
                Log("Backtesting completato.");
                if (_lastResult != null)
                {
                    Log($"Profit totale: {_lastResult.TotalProfit:F2}, final equity: {_lastResult.FinalEquity:F2}");
                    Log($"JSON: {_lastResult.ResultFilePath}");
                    Log($"HTML: {_lastResult.HtmlReportFilePath}");
                    _openReportButton.Enabled = true;
                }
            return;
        }

        if (job.Status == BacktestingJobStatus.Failed)
        {
                Log($"Backtesting fallito: {job.ErrorMessage}");
                MessageBox.Show(job.ErrorMessage, "Backtesting fallito", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Log("Backtesting interrotto dal server.");
        _statusLabel.Text = "Cancelled - backtest interrotto";
    }

    private void SetRunningState(bool isRunning)
    {
        _backtestRunning = isRunning;
        _runButton.Enabled = !isRunning;
        _cancelBacktestButton.Enabled = isRunning;
        _reloadButton.Enabled = !isRunning;
        _editMasterFilterButton.Enabled = !isRunning && _backtestingWorkspaceCombo.SelectedItem != null;
        _createWorkspaceButton.Enabled = !isRunning;
        _deleteWorkspaceButton.Enabled = !isRunning && _workspaceList.SelectedItem != null;
        _saveMasterFilterButton.Enabled = !isRunning && _workspaceList.SelectedItem != null;
        _refreshWorkspacesButton.Enabled = !isRunning;
        _backtestingWorkspaceCombo.Enabled = !isRunning;
        _statusLabel.Text = isRunning ? "Backtesting in corso..." : "Pronto";
        Cursor = Cursors.Default;
    }

    private async Task CancelBacktestingAsync()
    {
        if (!_backtestRunning || string.IsNullOrWhiteSpace(_lastJobId))
            return;

        _cancelBacktestButton.Enabled = false;
        _statusLabel.Text = "Interruzione in corso…";
        Log("Richiesta interruzione inviata al server.");
        try
        {
            await _workspaceApi.CancelBacktestingAsync(_lastJobId);
        }
        catch (Exception ex)
        {
            _cancelBacktestButton.Enabled = true;
            Log($"Errore richiesta interruzione: {ex.Message}");
            MessageBox.Show(ex.Message, "Interruzione backtest", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose || !_backtestRunning)
            return;

        e.Cancel = true;
        if (MessageBox.Show(
                "È in corso un backtest. Interromperlo e chiudere?",
                "Backtest in corso",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _statusLabel.Text = "Interruzione in corso…";
        if (!string.IsNullOrWhiteSpace(_lastJobId))
        {
            try { await _workspaceApi.CancelBacktestingAsync(_lastJobId); }
            catch (Exception ex) { Log($"Interruzione alla chiusura non confermata: {ex.Message}"); }
        }

        _pollingCts?.Cancel();
        _allowClose = true;
        Close();
    }

    private void OpenLastReport()
    {
        if (string.IsNullOrWhiteSpace(_lastJobId))
        {
            MessageBox.Show("Report HTML non disponibile.", "Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _workspaceApi.GetBacktestingReportUri(_lastJobId).ToString(),
            UseShellExecute = true
        });
    }

    private async Task LoadTitanoBacktestsAsync(bool showErrors = false)
    {
        _titanoBacktestCombo.Items.Clear();
        _openTitanoFolderButton.Enabled = false;

        if (_titanoWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            _titanoPathLabel.Text = "Seleziona un workspace.";
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var backtests = await _workspaceApi.ListBacktestsAsync(workspace.Info.Id);
            foreach (var backtest in backtests)
                _titanoBacktestCombo.Items.Add(new WorkspaceBacktestItem(backtest));

            if (_titanoBacktestCombo.Items.Count > 0)
            {
                _titanoBacktestCombo.SelectedIndex = 0;
            }
            else
            {
                _titanoPathLabel.Text =
                    $"Il workspace '{workspace.Info.Name}' non contiene backtest con risultati.";
            }
        }
        catch (Exception ex)
        {
            _titanoPathLabel.Text = $"Impossibile caricare i backtest: {ex.Message}";
            if (showErrors)
                MessageBox.Show(_titanoPathLabel.Text, "Titano", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task LoadTradingResultsBacktestsAsync()
    {
        _tradingResultsBacktestCombo.Items.Clear();
        _tradingResultsGrid.Rows.Clear();
        if (_tradingResultsWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            _tradingResultsSummary.Text = "Seleziona un workspace.";
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var backtests = await _workspaceApi.ListBacktestsAsync(workspace.Info.Id);
            foreach (var backtest in backtests.Where(backtest => backtest.HasResults))
                _tradingResultsBacktestCombo.Items.Add(new WorkspaceBacktestItem(backtest));
            _tradingResultsSummary.Text = _tradingResultsBacktestCombo.Items.Count == 0
                ? "Nessun backtest con risultati disponibile."
                : "Seleziona un backtest per leggere il suo trades.json.";
            if (_tradingResultsBacktestCombo.Items.Count > 0)
                _tradingResultsBacktestCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _tradingResultsSummary.Text = $"Impossibile caricare i backtest: {ex.Message}";
        }
    }

    private async Task LoadTradingResultsAsync()
    {
        _tradingResultsGrid.Rows.Clear();
        if (_tradingResultsWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _tradingResultsBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest)
            return;

        try
        {
            NormalizeBaseAddress();
            var trades = await _workspaceApi.GetBacktestTradesAsync(workspace.Info.Id, backtest.Info.FolderName);
            foreach (var trade in trades)
            {
                _tradingResultsGrid.Rows.Add(
                    trade.ExitTimeUtc.ToString("u"),
                    trade.EntryTimeUtc.ToString("u"),
                    trade.StrategyCode,
                    trade.Symbol,
                    trade.Direction,
                    trade.Quantity.ToString("N2"),
                    trade.EntryPrice.ToString("N4"),
                    trade.ExitPrice.ToString("N4"),
                    trade.ExitReason,
                    trade.GrossProfit.ToString("N2"),
                    trade.Commission.ToString("N2"),
                    trade.NetProfit.ToString("N2"),
                    trade.AccountNumber);
            }

            _tradingResultsSummary.Text =
                $"{backtest.Info.FolderName}: {trades.Count} operazioni chiuse lette da trades.json · " +
                $"P&L netto totale {trades.Sum(trade => trade.NetProfit):N2}.";
        }
        catch (Exception ex)
        {
            _tradingResultsSummary.Text = $"Impossibile leggere trades.json: {ex.Message}";
        }
    }

    private async Task LoadRotationBacktestsAsync()
    {
        _rotationsBacktestCombo.Items.Clear();
        _rotationsRunCombo.Items.Clear();
        _rotationsGrid.Rows.Clear();
        if (_rotationsWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            _rotationsSummary.Text = "Seleziona un workspace.";
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var backtests = await _workspaceApi.ListBacktestsAsync(workspace.Info.Id);
            foreach (var backtest in backtests.Where(backtest => backtest.HasResults))
                _rotationsBacktestCombo.Items.Add(new WorkspaceBacktestItem(backtest));
            _rotationsSummary.Text = _rotationsBacktestCombo.Items.Count == 0
                ? "Nessun backtest con risultati disponibile."
                : "Seleziona un backtest per elencare le rotazioni Titano.";
            if (_rotationsBacktestCombo.Items.Count > 0)
                _rotationsBacktestCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _rotationsSummary.Text = $"Impossibile caricare i backtest: {ex.Message}";
        }
    }

    private async Task LoadTitanoRunsAsync()
    {
        _rotationsRunCombo.Items.Clear();
        _rotationsGrid.Rows.Clear();
        if (_rotationsWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _rotationsBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest)
            return;

        try
        {
            NormalizeBaseAddress();
            var runs = await _httpClient.GetFromJsonAsync<List<TitanoRunInfo>>(
                $"api/Titano/rotations?workspaceId={Uri.EscapeDataString(workspace.Info.Id)}" +
                $"&backtestFolder={Uri.EscapeDataString(backtest.Info.FolderName)}",
                _jsonOptions) ?? [];
            foreach (var run in runs)
                _rotationsRunCombo.Items.Add(new TitanoRunListItem(run));

            _rotationsSummary.Text = runs.Count == 0
                ? "Nessuna rotazione Titano disponibile per il backtest selezionato."
                : $"{runs.Count} rotazioni Titano disponibili. Seleziona un run.";
            if (_rotationsRunCombo.Items.Count > 0)
                _rotationsRunCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _rotationsSummary.Text = $"Impossibile caricare le rotazioni: {ex.Message}";
        }
    }

    private async Task LoadSelectedTitanoRotationAsync()
    {
        _rotationsGrid.Rows.Clear();
        if (_rotationsWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _rotationsBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest ||
            _rotationsRunCombo.SelectedItem is not TitanoRunListItem run)
            return;

        try
        {
            NormalizeBaseAddress();
            var manifest = await _httpClient.GetFromJsonAsync<TitanoRotationManifest>(
                $"api/Titano/rotations/{Uri.EscapeDataString(run.Info.RunId)}" +
                $"?workspaceId={Uri.EscapeDataString(workspace.Info.Id)}" +
                $"&backtestFolder={Uri.EscapeDataString(backtest.Info.FolderName)}",
                _jsonOptions) ?? throw new InvalidOperationException("Manifest Titano vuoto.");

            foreach (var period in manifest.Periods)
            foreach (var strategy in period.Strategies.OrderBy(state => state.StrategyCode, StringComparer.OrdinalIgnoreCase))
            {
                _rotationsGrid.Rows.Add(
                    period.EffectiveFromUtc.ToString("u"),
                    period.EffectiveToUtc.ToString("u"),
                    strategy.StrategyCode,
                    strategy.Enabled ? "Accesa" : "Spenta",
                    strategy.AllocationMultiplier.ToString("P0"),
                    strategy.TransitionType,
                    strategy.Reason);
            }

            _rotationsSummary.Text =
                $"Run {manifest.RunId}: {manifest.Periods.Count} rotazioni, " +
                $"{manifest.Periods.Sum(period => period.Strategies.Count)} decisioni strategia.";
        }
        catch (Exception ex)
        {
            _rotationsSummary.Text = $"Impossibile leggere il manifest: {ex.Message}";
        }
    }

    private void UpdateTitanoPath()
    {
        if (_titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem item)
        {
            _openTitanoFolderButton.Enabled = false;
            return;
        }

        _titanoPathLabel.Text = item.Info.HasResults
            ? $"Input/base selezionato:{Environment.NewLine}{item.Info.FullPath}"
            : $"La cartella selezionata non contiene risultati backtest:{Environment.NewLine}{item.Info.FullPath}";
        _openTitanoFolderButton.Enabled = item.Info.HasResults && Directory.Exists(item.Info.FullPath);
    }

    private void ApplySelectedBacktestPeriodToTitano()
    {
        if (_titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem item)
            return;
        if (item.Info.StartDateUtc is { } start)
            _titanoStartPicker.Value = start.ToUniversalTime().Date;
        if (item.Info.EndDateUtc is { } end)
            _titanoEndPicker.Value = end.ToUniversalTime().Date;
    }

    /// <summary>
    /// Popola la combo dei setup Titano con le rotazioni già calcolate per il workspace e il
    /// backtest selezionati. Prima il RunId andava incollato a mano: un refuso significava una
    /// sessione che non applicava alcun filtro senza dirlo.
    /// </summary>
    private async Task LoadTitanoRunsForSessionAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            MessageBox.Show("Seleziona un workspace.", "Trading Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var backtestFolder = _sessionTitanoBacktest.Text.Trim();
        if (string.IsNullOrWhiteSpace(backtestFolder))
        {
            MessageBox.Show("Indica la cartella del backtest sorgente.", "Trading Session",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var url = "api/Titano/rotations" +
                      $"?workspaceId={Uri.EscapeDataString(workspace.Info.Id)}" +
                      $"&backtestFolder={Uri.EscapeDataString(backtestFolder)}";
            var runs = await _httpClient.GetFromJsonAsync<List<TitanoRunInfo>>(url, _jsonOptions) ?? [];

            var previous = _sessionTitanoRunId.Text;
            _sessionTitanoRunId.Items.Clear();
            foreach (var run in runs)
            {
                _sessionTitanoRunId.Items.Add(run.RunId);
            }

            if (runs.Count == 0)
            {
                ShowSession($"Nessuna rotazione Titano trovata in '{backtestFolder}'. " +
                            "Generane una dal tab Titano.");
                return;
            }

            _sessionTitanoRunId.Text = runs.Any(r => r.RunId == previous) ? previous : runs[0].RunId;
            ShowSession($"{runs.Count} rotazioni Titano disponibili in '{backtestFolder}'. " +
                        $"Selezionata: {_sessionTitanoRunId.Text}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore caricamento run Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private const string TitanoModeHelp =
        "Disabled: nessun filtro, vengono valutate tutte le strategie del masterfilter (è il run che " +
        "produce i trade su cui Titano calcola le rotazioni). " +
        "BacktestRotationFile: backtest filtrato con le rotazioni del manifest calcolato offline; " +
        "per ogni barra vale la decisione del periodo che la contiene. " +
        "Realtime: vale la decisione del periodo corrente dell'ultima analisi Titano.";

    private TitanoFilterMode SelectedTitanoMode =>
        Enum.TryParse<TitanoFilterMode>(_sessionTitanoMode.SelectedItem?.ToString(), out var mode)
            ? mode
            : TitanoFilterMode.Disabled;

    private void UpdateTitanoSessionControlsState()
    {
        var mode = SelectedTitanoMode;
        _formToolTip.SetToolTip(_sessionTitanoMode, mode switch
        {
            TitanoFilterMode.Disabled =>
                "Nessun filtro: tutte le strategie del masterfilter vengono valutate. Se indichi comunque " +
                "un run, la rotazione viene calcolata e registrata nel rotation-log per confronto.",
            TitanoFilterMode.BacktestRotationFile =>
                "Backtest filtrato: per ogni barra vale la decisione del periodo del manifest che la " +
                "contiene. Il manifest deve coprire l'intero intervallo del backtest.",
            _ =>
                "Realtime: vale la decisione del periodo corrente. Oltre la fine del manifest resta in " +
                "vigore l'ultimo periodo calcolato, dichiarandolo nel rotation-log."
        });

        // Le modalità filtrate senza un run non possono degradare in silenzio: il server le rifiuta.
        _sessionTitanoRunId.Enabled = true;
        _sessionTitanoBacktest.Enabled = true;
    }

    private async Task LoadTradingPlansAsync(string? selectCode = null)
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace) return;
        try
        {
            NormalizeBaseAddress();
            _tradingPlans = await _httpClient.GetFromJsonAsync<List<TradingPlan>>(
                $"api/v1/workspaces/{Uri.EscapeDataString(workspace.Info.Id)}/trading-plans", _jsonOptions) ?? [];
            _sessionPlanCombo.Items.Clear();
            foreach (var plan in _tradingPlans)
                _sessionPlanCombo.Items.Add($"{plan.Code} — {plan.Name}");
            if (_tradingPlans.Count > 0)
            {
                var index = selectCode is null
                    ? 0
                    : _tradingPlans.FindIndex(x => x.Code.Equals(selectCode, StringComparison.OrdinalIgnoreCase));
                _sessionPlanCombo.SelectedIndex = Math.Max(0, index);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore piani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSelectedTradingPlan()
    {
        var index = _sessionPlanCombo.SelectedIndex;
        if (index < 0 || index >= _tradingPlans.Count) return;
        var plan = _tradingPlans[index];
        _sessionPlanCode.Text = plan.Code;
        _sessionPlanName.Text = plan.Name;
        _sessionTitanoBacktest.Text = plan.TitanoBacktestFolder ?? string.Empty;
        _sessionTitanoRunId.Text = plan.TitanoRunId ?? string.Empty;
        _sessionMetadata.Text = string.Join(";", plan.Instruments.Select(x =>
            $"{x.Symbol},{x.DollarsPerPoint},{x.MinimumQuantity},{x.QuantityStep},{x.RoundingMode}"));
        _sessionAccountGroups.Rows.Clear();
        _sessionAccountGroups.Rows.Add(
            plan.GroupId, plan.RotationSetupId, plan.AccountNumber, plan.MaxConcurrentTrades,
            plan.ApplyTitanoFilters, plan.TitanoRunId);
    }

    private void ClearTradingPlanEditor()
    {
        _sessionPlanCombo.SelectedIndex = -1;
        _sessionPlanCode.Clear();
        _sessionPlanName.Clear();
        _sessionAccountGroups.Rows.Clear();
    }

    private async Task SaveTradingPlanAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace) return;
        try
        {
            var row = ReadTradingGroupRows().SingleOrDefault()
                ?? throw new InvalidOperationException("Il piano richiede una riga gruppo/account.");
            var request = new SaveTradingPlanRequest
            {
                Code = _sessionPlanCode.Text.Trim(),
                Name = _sessionPlanName.Text.Trim(),
                GroupId = row.GroupId,
                AccountNumber = row.AccountNumber,
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                RotationSetupId = row.RotationSetupId,
                TitanoRunId = row.TitanoRunId,
                TitanoBacktestFolder = row.TitanoBacktestFolder,
                ApplyTitanoFilters = row.ApplyTitanoFilters,
                Instruments = ParseInstrumentMetadata(_sessionMetadata.Text),
                PositionSizing = new PositionSizingConfig
                {
                    MarketVolatility = new MarketVolatilitySizingConfig
                    {
                        Enabled = _sessionAtrEnabled.Checked,
                        AtrPeriods = (int)_sessionAtrPeriods.Value,
                        TargetRiskDollars = _sessionTargetRisk.Value
                    },
                    PortfolioRisk = new PortfolioRiskSizingConfig
                    {
                        Enabled = _sessionPortfolioEnabled.Checked,
                        MaximumDrawdown = _sessionDrawdownCap.Value / 100m,
                        EnableCppi = _sessionCppiEnabled.Checked,
                        CppiFloorFraction = _sessionCppiFloor.Value / 100m,
                        CppiMultiplier = _sessionCppiMultiplier.Value
                    }
                }
            };
            var uri = $"api/v1/workspaces/{Uri.EscapeDataString(workspace.Info.Id)}/trading-plans/" +
                      Uri.EscapeDataString(request.Code);
            var response = await _httpClient.PutAsJsonAsync(uri, request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            await LoadTradingPlansAsync(request.Code);
            ShowSession($"Piano {request.Code} salvato.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore salvataggio piano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteTradingPlanAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            string.IsNullOrWhiteSpace(_sessionPlanCode.Text)) return;
        var response = await _httpClient.DeleteAsync(
            $"api/v1/workspaces/{Uri.EscapeDataString(workspace.Info.Id)}/trading-plans/" +
            Uri.EscapeDataString(_sessionPlanCode.Text.Trim()));
        response.EnsureSuccessStatusCode();
        ClearTradingPlanEditor();
        await LoadTradingPlansAsync();
    }

    private async Task CreateTradingSessionAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            MessageBox.Show("Seleziona un workspace.", "Trading Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var titanoRunId = string.IsNullOrWhiteSpace(_sessionTitanoRunId.Text) ? null : _sessionTitanoRunId.Text.Trim();
        if (titanoRunId is not null && string.IsNullOrWhiteSpace(_sessionTitanoBacktest.Text))
        {
            MessageBox.Show(
                "Un setup Titano richiede anche la cartella del backtest sorgente.",
                "Trading Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var titanoMode = SelectedTitanoMode;
        if (titanoMode != TitanoFilterMode.Disabled && titanoRunId is null)
        {
            MessageBox.Show(
                $"La modalità {titanoMode} richiede un setup Titano: seleziona una rotazione, " +
                "oppure passa alla modalità Disabled.",
                "Trading Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var request = new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Info.Id,
                ExecutionMode = Enum.Parse<ExecutionMode>(_sessionModeCombo.SelectedItem?.ToString() ?? "ServerSimulated"),
                TitanoRunId = titanoRunId,
                TitanoBacktestFolder = titanoRunId is null ? null : _sessionTitanoBacktest.Text.Trim(),
                TitanoMode = titanoMode,
                Instruments = ParseInstrumentMetadata(_sessionMetadata.Text),
                PositionSizing = new PositionSizingConfig
                {
                    MarketVolatility = new MarketVolatilitySizingConfig
                    {
                        Enabled = _sessionAtrEnabled.Checked, AtrPeriods = (int)_sessionAtrPeriods.Value,
                        TargetRiskDollars = _sessionTargetRisk.Value
                    },
                    PortfolioRisk = new PortfolioRiskSizingConfig
                    {
                        Enabled = _sessionPortfolioEnabled.Checked,
                        MaximumDrawdown = _sessionDrawdownCap.Value / 100m,
                        EnableCppi = _sessionCppiEnabled.Checked,
                        CppiFloorFraction = _sessionCppiFloor.Value / 100m,
                        CppiMultiplier = _sessionCppiMultiplier.Value,
                        EnableAggressiveModules = false, MaximumMultiplier = 1m
                    }
                }
            };
            var response = await _httpClient.PostAsJsonAsync("api/v1/trading-sessions", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            _activeSession = await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(_jsonOptions)
                ?? throw new InvalidOperationException("Il server non ha restituito la sessione.");
            ShowSession($"Sessione creata: {_activeSession.SessionId}");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore sessione", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task SetTradingSessionStatusAsync(string action)
    {
        if (_activeSession is null) return;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/{action}");
        request.Headers.Add("X-Session-Token", _activeSession.SessionToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        _activeSession = await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(_jsonOptions);
        ShowSession($"Stato: {_activeSession?.Status}");
    }

    private async Task RefreshTradingSessionSnapshotAsync()
    {
        if (_activeSession is null) return;
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/snapshot");
        request.Headers.Add("X-Session-Token", _activeSession.SessionToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<TradingSessionSnapshot>(_jsonOptions);
        ShowSession(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }));
    }

    private void ShowSession(string message) =>
        _sessionOutput.Text = $"{message}{Environment.NewLine}{JsonSerializer.Serialize(_activeSession, new JsonSerializerOptions(_jsonOptions) { WriteIndented = true })}";

    private static IReadOnlyList<InstrumentMetadata> ParseInstrumentMetadata(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(row =>
        {
            var p = row.Split(',', StringSplitOptions.TrimEntries);
            if (p.Length != 5) throw new FormatException("Metadata: symbol,DollarsPerPoint,minimo,step,modalità; separare strumenti con ';'.");
            return new InstrumentMetadata
            {
                Symbol = p[0],
                DollarsPerPoint = decimal.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                MinimumQuantity = decimal.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture),
                QuantityStep = decimal.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture),
                RoundingMode = Enum.Parse<QuantityRoundingMode>(p[4], true)
            };
        }).ToArray();

    private async Task LoadTitanoSetupsAsync(string? selectId = null)
    {
        try
        {
            NormalizeBaseAddress();
            var setups = await _httpClient.GetFromJsonAsync<List<TitanoSetupInfo>>(
                "api/Titano/rotation-setups", _jsonOptions) ?? [];
            _titanoSetups = setups;
            _titanoSetupCombo.Items.Clear();
            foreach (var setup in setups)
                _titanoSetupCombo.Items.Add(setup);
            if (_sessionAccountGroups.Columns["RotationSetupId"] is DataGridViewComboBoxColumn setupColumn)
                setupColumn.DataSource = setups.ToList();

            if (_titanoSetupCombo.Items.Count == 0)
                return;

            var selectedIndex = setups.FindIndex(setup =>
                setup.Id.Equals(selectId, StringComparison.OrdinalIgnoreCase));
            _titanoSetupCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        catch (Exception ex)
        {
            Log($"Impossibile caricare i setup Titano: {ex.Message}");
        }
    }

    private async Task LoadSelectedTitanoSetupAsync()
    {
        if (_titanoSetupCombo.SelectedItem is not TitanoSetupInfo info)
        {
            MessageBox.Show("Seleziona un setup Titano.", "Setup Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var setup = await _httpClient.GetFromJsonAsync<TitanoRotationSetup>(
                $"api/Titano/rotation-setups/{Uri.EscapeDataString(info.Id)}", _jsonOptions)
                ?? throw new InvalidOperationException("Il server ha restituito un setup vuoto.");
            ApplyTitanoSetupToUi(setup);
            Log($"Setup Titano caricato: {setup.Name}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore setup Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveTitanoSetupAsync()
    {
        if (string.IsNullOrWhiteSpace(_titanoSetupName.Text))
        {
            MessageBox.Show("Inserisci il nome del setup.", "Setup Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var setup = BuildTitanoSetupFromUi();
            var response = await _httpClient.PostAsJsonAsync(
                "api/Titano/rotation-setups", setup, _jsonOptions);
            response.EnsureSuccessStatusCode();
            var saved = await response.Content.ReadFromJsonAsync<TitanoRotationSetup>(_jsonOptions)
                ?? throw new InvalidOperationException("Il server non ha restituito il setup salvato.");
            await LoadTitanoSetupsAsync(saved.Id);
            Log($"Setup Titano salvato: {saved.Name}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore salvataggio setup Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private TitanoRotationSetup BuildTitanoSetupFromUi() => new()
    {
        Id = _titanoSetupCombo.SelectedItem is TitanoSetupInfo selected &&
             selected.Name.Equals(_titanoSetupName.Text.Trim(), StringComparison.OrdinalIgnoreCase)
            ? selected.Id
            : string.Empty,
        Name = _titanoSetupName.Text.Trim(),
        Description = _titanoSetupDescription.Text.Trim(),
        RotationPeriod = Enum.Parse<TitanoRotationPeriod>(
            _titanoPeriodCombo.SelectedItem?.ToString() ?? "Weekly"),
        MinimumTrades = (int)_titanoMinimumTrades.Value,
        ShortWindowDays = (int)_titanoShortDays.Value,
        LongWindowDays = (int)_titanoLongDays.Value,
        MovingAverageWindowDays = (int)_titanoMaDays.Value,
        MinimumShortReturn = _titanoMinShortReturn.Value / 100m,
        MinimumLongReturn = _titanoMinLongReturn.Value / 100m,
        MinimumZScore = _titanoMinZ.Value,
        MaximumZScore = _titanoMaxZ.Value,
        MaximumCurrentDrawdown = _titanoMaxCurrentDd.Value / 100m,
        MaximumObservedDrawdown = _titanoMaxDd.Value / 100m,
        MaximumReturnVolatility = _titanoMaxVolatility.Value / 100m,
        RequireEquityAboveMovingAverage = _titanoRequireEquityAboveMa.Checked,
        ReenableMaximumCurrentDrawdown = _titanoReenableDd.Value / 100m,
        DisableCompositeScore = _titanoDisableScore.Value,
        ReenableCompositeScore = _titanoReenableScore.Value,
        CooldownPeriodsAfterOff = (int)_titanoCooldown.Value,
        MinimumOnPeriods = (int)_titanoMinOn.Value,
        MinimumPassingFilters = (int)_titanoMinVotes.Value,
        HardStopDrawdown = _titanoHardStop.Value / 100m,
        CommissionPerUnit = _titanoCommission.Value,
        SlippagePerUnit = _titanoSlippage.Value,
        CalibrationPeriods = (int)_titanoCalibration.Value,
        EvaluationPeriods = (int)_titanoEvaluation.Value,
        WalkForwardMode = Enum.Parse<TitanoWalkForwardMode>(
            _titanoWalkForwardMode.SelectedItem?.ToString() ?? "Rolling"),
        SizingTiers = ParseSizingTiers(_titanoSizingTiers.Text).ToList()
    };

    private TitanoRotationRequest BuildTitanoRequest(
        TitanoRotationSetup setup,
        string workspaceId,
        string backtestFolder) => new()
    {
        WorkspaceId = workspaceId,
        BacktestFolder = backtestFolder,
        SetupName = setup.Name,
        Description = setup.Description,
        RotationPeriod = setup.RotationPeriod,
        StartUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
        EndUtc = DateTime.SpecifyKind(_titanoEndPicker.Value.Date, DateTimeKind.Utc),
        BiweeklyAnchorUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
        InitialCapital = _initialCapitalInput.Value,
        MinimumTrades = setup.MinimumTrades,
        ShortWindowDays = setup.ShortWindowDays,
        LongWindowDays = setup.LongWindowDays,
        MovingAverageWindowDays = setup.MovingAverageWindowDays,
        MinimumShortReturn = setup.MinimumShortReturn,
        MinimumLongReturn = setup.MinimumLongReturn,
        MinimumZScore = setup.MinimumZScore,
        MaximumZScore = setup.MaximumZScore,
        MaximumCurrentDrawdown = setup.MaximumCurrentDrawdown,
        MaximumObservedDrawdown = setup.MaximumObservedDrawdown,
        MaximumReturnVolatility = setup.MaximumReturnVolatility,
        RequireEquityAboveMovingAverage = setup.RequireEquityAboveMovingAverage,
        ReenableMaximumCurrentDrawdown = setup.ReenableMaximumCurrentDrawdown,
        DisableCompositeScore = setup.DisableCompositeScore,
        ReenableCompositeScore = setup.ReenableCompositeScore,
        CooldownPeriodsAfterOff = setup.CooldownPeriodsAfterOff,
        MinimumOnPeriods = setup.MinimumOnPeriods,
        MinimumPassingFilters = setup.MinimumPassingFilters,
        HardStopDrawdown = setup.HardStopDrawdown,
        CommissionPerUnit = setup.CommissionPerUnit,
        SlippagePerUnit = setup.SlippagePerUnit,
        CalibrationPeriods = setup.CalibrationPeriods,
        EvaluationPeriods = setup.EvaluationPeriods,
        WalkForwardMode = setup.WalkForwardMode,
        SizingTiers = setup.SizingTiers
    };

    private void ApplyTitanoSetupToUi(TitanoRotationSetup setup)
    {
        _titanoSetupName.Text = setup.Name;
        _titanoSetupDescription.Text = setup.Description;
        _titanoPeriodCombo.SelectedItem = setup.RotationPeriod.ToString();
        _titanoMinimumTrades.Value = setup.MinimumTrades;
        _titanoShortDays.Value = setup.ShortWindowDays;
        _titanoLongDays.Value = setup.LongWindowDays;
        _titanoMaDays.Value = setup.MovingAverageWindowDays;
        _titanoMinShortReturn.Value = setup.MinimumShortReturn * 100m;
        _titanoMinLongReturn.Value = setup.MinimumLongReturn * 100m;
        _titanoMinZ.Value = setup.MinimumZScore;
        _titanoMaxZ.Value = setup.MaximumZScore;
        _titanoMaxCurrentDd.Value = setup.MaximumCurrentDrawdown * 100m;
        _titanoMaxDd.Value = setup.MaximumObservedDrawdown * 100m;
        _titanoMaxVolatility.Value = setup.MaximumReturnVolatility * 100m;
        _titanoRequireEquityAboveMa.Checked = setup.RequireEquityAboveMovingAverage;
        _titanoReenableDd.Value = setup.ReenableMaximumCurrentDrawdown * 100m;
        _titanoDisableScore.Value = setup.DisableCompositeScore;
        _titanoReenableScore.Value = setup.ReenableCompositeScore;
        _titanoCooldown.Value = setup.CooldownPeriodsAfterOff;
        _titanoMinOn.Value = setup.MinimumOnPeriods;
        _titanoMinVotes.Value = setup.MinimumPassingFilters;
        _titanoHardStop.Value = setup.HardStopDrawdown * 100m;
        _titanoCommission.Value = setup.CommissionPerUnit;
        _titanoSlippage.Value = setup.SlippagePerUnit;
        _titanoCalibration.Value = setup.CalibrationPeriods;
        _titanoEvaluation.Value = setup.EvaluationPeriods;
        _titanoWalkForwardMode.SelectedItem = setup.WalkForwardMode.ToString();
        _titanoSizingTiers.Text = string.Join("; ", setup.SizingTiers
            .OrderByDescending(tier => tier.MinimumScore)
            .Select(tier =>
                $"{tier.MinimumScore.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}=" +
                $"{(tier.AllocationMultiplier * 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}%"));
    }

    private async Task RunTitanoAsync()
    {
        if (_titanoWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest)
        {
            MessageBox.Show("Seleziona workspace e backtest.", "Titano", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var request = new TitanoRotationRequest
        {
            WorkspaceId = workspace.Info.Id,
            BacktestFolder = backtest.Info.FolderName,
            SetupName = _titanoSetupName.Text.Trim(),
            Description = _titanoSetupDescription.Text.Trim(),
            RotationPeriod = Enum.Parse<TitanoRotationPeriod>(_titanoPeriodCombo.SelectedItem?.ToString() ?? "Weekly"),
            StartUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(_titanoEndPicker.Value.Date, DateTimeKind.Utc),
            BiweeklyAnchorUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
            InitialCapital = _initialCapitalInput.Value,
            MinimumTrades = (int)_titanoMinimumTrades.Value,
            ShortWindowDays = (int)_titanoShortDays.Value,
            LongWindowDays = (int)_titanoLongDays.Value,
            MovingAverageWindowDays = (int)_titanoMaDays.Value,
            MinimumShortReturn = _titanoMinShortReturn.Value / 100m,
            MinimumLongReturn = _titanoMinLongReturn.Value / 100m,
            MinimumZScore = _titanoMinZ.Value,
            MaximumZScore = _titanoMaxZ.Value,
            MaximumCurrentDrawdown = _titanoMaxCurrentDd.Value / 100m,
            MaximumObservedDrawdown = _titanoMaxDd.Value / 100m,
            MaximumReturnVolatility = _titanoMaxVolatility.Value / 100m,
            RequireEquityAboveMovingAverage = _titanoRequireEquityAboveMa.Checked,
            ReenableMaximumCurrentDrawdown = _titanoReenableDd.Value / 100m,
            DisableCompositeScore = _titanoDisableScore.Value,
            ReenableCompositeScore = _titanoReenableScore.Value,
            CooldownPeriodsAfterOff = (int)_titanoCooldown.Value,
            MinimumOnPeriods = (int)_titanoMinOn.Value,
            MinimumPassingFilters = (int)_titanoMinVotes.Value,
            HardStopDrawdown = _titanoHardStop.Value / 100m,
            CommissionPerUnit = _titanoCommission.Value,
            SlippagePerUnit = _titanoSlippage.Value,
            CalibrationPeriods = (int)_titanoCalibration.Value,
            EvaluationPeriods = (int)_titanoEvaluation.Value,
            WalkForwardMode = Enum.Parse<TitanoWalkForwardMode>(
                _titanoWalkForwardMode.SelectedItem?.ToString() ?? "Rolling"),
            SizingTiers = ParseSizingTiers(_titanoSizingTiers.Text)
        };

        try
        {
            NormalizeBaseAddress();
            _runTitanoButton.Enabled = false;
            var response = await _httpClient.PostAsJsonAsync("api/Titano/rotations", request, _jsonOptions);
            await EnsureSuccessWithDetailsAsync(response, "Avvio Titano");
            var manifest = await response.Content.ReadFromJsonAsync<TitanoRotationManifest>(_jsonOptions)
                ?? throw new InvalidOperationException("Manifest Titano non ricevuto.");
            _lastTitanoManifest = manifest;
            _openTitanoReportButton.Enabled = true;
            _titanoResetHardStopButton.Enabled = manifest.Periods.SelectMany(x => x.Strategies).Any(x => x.HardStopped);
            var lines = new List<string>
            {
                $"RunId: {manifest.RunId}",
                $"Manifest: {Path.Combine(backtest.Info.FullPath, "titano", manifest.RunId, "manifest.json")}",
                "TitanoRunId può essere associato a sessioni ServerSimulated o ExternalBroker."
            };
            foreach (var period in manifest.Periods)
            {
                lines.Add($"{period.EffectiveFromUtc:u} -> {period.EffectiveToUtc:u}");
                lines.AddRange(period.Strategies.Select(s =>
                    $"  {s.State,-11} {s.StrategyCode} | alloc={s.AllocationMultiplier:P0} " +
                    $"votes={s.PassingFilters}/{s.TotalFilters} cd={s.CooldownRemaining} | {s.Reason} | " +
                    $"short={s.Metrics.ShortReturn:P2} long={s.Metrics.LongReturn:P2} " +
                    $"ma={s.Metrics.MovingAverageEquity:F2} z={s.Metrics.ZScore:F2} " +
                    $"dd={s.Metrics.CurrentDrawdown:P2}/{s.Metrics.MaximumDrawdown:P2} vol={s.Metrics.ReturnVolatility:P2}"));
            }
            lines.AddRange(manifest.WalkForward.Select(x =>
                $"WF {x.EvaluationPeriodId}: IS={x.InSampleNetProfit:F2}, OOS={x.OutOfSampleNetProfit:F2}" +
                (x.InSampleOnlyImprovementWarning ? " [WARNING: migliora solo IS]" : "")));
            _titanoPathLabel.Visible = false;
            _titanoResultsTextBox.Text = string.Join(Environment.NewLine, lines);
            Log($"Titano completato: {manifest.RunId}, {manifest.Periods.Count} periodi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _runTitanoButton.Enabled = true; }
    }

    private static async Task EnsureSuccessWithDetailsAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        string? detail = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
                detail = error.GetString();
            else if (root.TryGetProperty("detail", out var problemDetail))
                detail = problemDetail.GetString();
            else if (root.TryGetProperty("title", out var title))
                detail = title.GetString();
        }
        catch (JsonException)
        {
            detail = body;
        }

        throw new InvalidOperationException(
            $"{operation} fallito: HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). " +
            (string.IsNullOrWhiteSpace(detail) ? "Il server non ha restituito dettagli." : detail));
    }

    private async Task OpenTitanoReportAsync()
    {
        if (_lastTitanoManifest is null ||
            _titanoWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest)
        {
            MessageBox.Show("Esegui prima Titano con i parametri desiderati.", "Report Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            NormalizeBaseAddress();
            var uri = new Uri(_httpClient.BaseAddress!,
                $"api/Titano/rotations/{Uri.EscapeDataString(_lastTitanoManifest.RunId)}/report" +
                $"?workspaceId={Uri.EscapeDataString(workspace.Info.Id)}" +
                $"&backtestFolder={Uri.EscapeDataString(backtest.Info.FolderName)}");
            await HtmlReportViewerForm.ShowFromUriAsync(this, _httpClient, uri, "Risultato Titano");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Report Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static IReadOnlyList<TitanoSizingTier> ParseSizingTiers(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item =>
            {
                var parts = item.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) throw new FormatException("Tier sizing: usare soglia=percentuale.");
                return new TitanoSizingTier
                {
                    MinimumScore = decimal.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    AllocationMultiplier = decimal.Parse(parts[1].TrimEnd('%'),
                        System.Globalization.CultureInfo.InvariantCulture) / 100m
                };
            }).ToArray();

    private async Task ResetTitanoHardStopAsync()
    {
        if (_lastTitanoManifest is null || _titanoWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace ||
            _titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem backtest) return;
        var candidates = _lastTitanoManifest.Periods.SelectMany(x => x.Strategies)
            .Where(x => x.HardStopped).Select(x => x.StrategyCode).Distinct().Order().ToArray();
        if (candidates.Length == 0) return;
        var strategy = Microsoft.VisualBasic.Interaction.InputBox(
            $"StrategyCode hard-stopped ({string.Join(", ", candidates)}):", "Reset hard-stop", candidates[0]);
        if (string.IsNullOrWhiteSpace(strategy) ||
            MessageBox.Show($"Confermi reset auditato di {strategy} dal periodo successivo?",
                "Conferma reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var payload = new TitanoHardStopResetRequest
        {
            StrategyCode = strategy.Trim(), RequestedBy = Environment.UserName,
            Reason = "Reset manuale confermato da WinForms", RequestedAtUtc = DateTime.UtcNow
        };
        var uri = $"api/Titano/rotations/{Uri.EscapeDataString(_lastTitanoManifest.RunId)}/hard-stop-reset" +
                  $"?workspaceId={Uri.EscapeDataString(workspace.Info.Id)}&backtestFolder={Uri.EscapeDataString(backtest.Info.FolderName)}";
        var response = await _httpClient.PostAsJsonAsync(uri, payload, _jsonOptions);
        response.EnsureSuccessStatusCode();
        Log($"Reset hard-stop auditato per {strategy}; efficace dal periodo successivo.");
    }

    private void OpenSelectedTitanoFolder()
    {
        if (_titanoBacktestCombo.SelectedItem is not WorkspaceBacktestItem item ||
            !Directory.Exists(item.Info.FullPath))
        {
            MessageBox.Show("Cartella risultati non disponibile.", "Titano", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = item.Info.FullPath, UseShellExecute = true });
    }

    private void Log(string message)
    {
        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static DateTime CreateUtcPickerDefault(DateTime utcValue) =>
        new(utcValue.Year, utcValue.Month, utcValue.Day, utcValue.Hour, utcValue.Minute, utcValue.Second);

    private sealed class StrategyListItem
    {
        public StrategyListItem(StrategyCatalogItem strategy)
        {
            Strategy = strategy;
        }

        public StrategyCatalogItem Strategy { get; }

        public override string ToString()
            => $"{Strategy.Symbol} | {Strategy.Name} | {Strategy.TimeframeMinutes}m | {Strategy.SourceFileName}";
    }

    private sealed class FilterableStrategyChecklist : UserControl
    {
        private readonly TextBox _filterTextBox = new();
        private readonly Button _clearFilterButton = new();
        private readonly CheckedListBox _list = new();
        private readonly HashSet<string> _selectedIds = new(StringComparer.OrdinalIgnoreCase);
        private List<StrategyCatalogItem> _strategies = new();
        private bool _updating;

        public FilterableStrategyChecklist()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 6)
            };
            searchPanel.Controls.Add(new Label
            {
                Text = "Cerca strategie:",
                AutoSize = true,
                Padding = new Padding(0, 7, 6, 0)
            });
            _filterTextBox.Width = 260;
            _filterTextBox.PlaceholderText = "Nome, codice, simbolo...";
            _filterTextBox.TextChanged += (_, _) => ApplyFilter();
            searchPanel.Controls.Add(_filterTextBox);

            _clearFilterButton.Text = "Azzera filtro";
            _clearFilterButton.AutoSize = true;
            _clearFilterButton.Enabled = false;
            _clearFilterButton.Click += (_, _) => _filterTextBox.Clear();
            searchPanel.Controls.Add(_clearFilterButton);

            _list.Dock = DockStyle.Fill;
            _list.CheckOnClick = true;
            _list.HorizontalScrollbar = true;
            _list.ItemCheck += (_, e) =>
            {
                if (_updating || _list.Items[e.Index] is not StrategyListItem item)
                {
                    return;
                }

                if (e.NewValue == CheckState.Checked)
                {
                    _selectedIds.Add(item.Strategy.Id);
                }
                else
                {
                    _selectedIds.Remove(item.Strategy.Id);
                }
            };

            layout.Controls.Add(searchPanel, 0, 0);
            layout.Controls.Add(_list, 0, 1);
            Controls.Add(layout);
        }

        public void SetStrategies(IEnumerable<StrategyCatalogItem> strategies, IEnumerable<string> selectedIds)
        {
            _strategies = strategies.OrderBy(s => s.Symbol).ThenBy(s => s.Name).ToList();
            _selectedIds.Clear();
            _selectedIds.UnionWith(selectedIds);
            ApplyFilter();
        }

        public List<string> GetSelectedIds() => _selectedIds.ToList();

        public void SetAll(bool isChecked)
        {
            _selectedIds.Clear();
            if (isChecked)
            {
                _selectedIds.UnionWith(_strategies.Select(strategy => strategy.Id));
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var filter = _filterTextBox.Text.Trim();
            _clearFilterButton.Enabled = filter.Length > 0;

            _updating = true;
            try
            {
                _list.Items.Clear();
                foreach (var strategy in _strategies)
                {
                    var item = new StrategyListItem(strategy);
                    if (filter.Length == 0 ||
                        item.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        strategy.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        _list.Items.Add(item, _selectedIds.Contains(strategy.Id));
                    }
                }
            }
            finally
            {
                _updating = false;
            }
        }
    }

    private sealed class WorkspaceListItem
    {
        public WorkspaceListItem(WorkspaceInfo info)
        {
            Info = info;
        }

        public WorkspaceInfo Info { get; }

        public string DisplayText => $"{Info.Name} ({Info.Id}) · {Info.StrategiesCount} strat.";

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Finestra di creazione account. Raccoglie solo l'anagrafica: la tabella di conversione viene
    /// aggiunta dal chiamante a partire dal preset identità, e si modifica poi nel tab.
    /// </summary>
    private sealed class NewAccountDialog : Form
    {
        private readonly TextBox _name = new() { Width = 260 };
        private readonly TextBox _accountNumber = new() { Width = 260 };
        private readonly ComboBox _groupId = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _broker = new() { Width = 260 };
        private readonly ComboBox _currency = new() { Width = 100 };
        private readonly NumericUpDown _initialBalance = new() { Width = 160 };
        private readonly TextBox _notes = new() { Width = 260, Multiline = true, Height = 52 };

        public NewAccountDialog(int presetSymbolCount, IReadOnlyList<string> groups)
        {
            Text = "Nuovo account globale";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(460, 380);

            _currency.Items.AddRange(new object[] { "USD", "EUR", "GBP", "CHF" });
            _currency.Text = "USD";
            _groupId.Items.Add(string.Empty);
            foreach (var group in groups.OrderBy(group => group, StringComparer.OrdinalIgnoreCase))
                _groupId.Items.Add(group);
            _groupId.SelectedIndex = 0;

            _initialBalance.Minimum = 0;
            _initialBalance.Maximum = 1_000_000_000;
            _initialBalance.DecimalPlaces = 2;
            _initialBalance.ThousandsSeparator = true;
            _initialBalance.Increment = 1000;
            _initialBalance.Value = 1_000_000;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, "Nome *", _name);
            AddRow(layout, "Codice account", _accountNumber);
            AddRow(layout, "Gruppo", _groupId);
            AddRow(layout, "Broker", _broker);
            AddRow(layout, "Balance iniziale", _initialBalance);
            AddRow(layout, "Valuta", _currency);
            AddRow(layout, "Note", _notes);

            var hint = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Text = presetSymbolCount > 0
                    ? $"La tabella di conversione parte dal preset identità: {presetSymbolCount} symbol " +
                      "mappati su se stessi (@GC = @GC) con moltiplicatore 1. Nessuna conversione finché non la modifichi."
                    : "Il preset di conversione è vuoto: l'account verrà creato senza symbol mappati."
            };
            layout.Controls.Add(hint, 0, layout.RowCount);
            layout.SetColumnSpan(hint, 2);
            layout.RowCount++;

            var okButton = new Button { Text = "Crea", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Annulla", DialogResult = DialogResult.Cancel, AutoSize = true };
            okButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text))
                {
                    MessageBox.Show("Il nome dell'account è obbligatorio.", "Nuovo account",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 12)
            };
            buttons.Controls.Add(okButton);
            buttons.Controls.Add(cancelButton);

            Controls.Add(layout);
            Controls.Add(buttons);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        public WorkspaceAccount BuildAccount() => new()
        {
            Name = _name.Text.Trim(),
            AccountNumber = _accountNumber.Text.Trim(),
            GroupId = _groupId.Text.Trim(),
            Broker = _broker.Text.Trim(),
            Currency = string.IsNullOrWhiteSpace(_currency.Text) ? "USD" : _currency.Text.Trim(),
            InitialBalance = _initialBalance.Value,
            Enabled = true,
            Notes = _notes.Text.Trim()
        };

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            var row = layout.RowCount;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 6, 10, 0)
            }, 0, row);
            layout.Controls.Add(control, 1, row);
            layout.RowCount = row + 1;
        }
    }

    private sealed class AccountListItem
    {
        public AccountListItem(WorkspaceAccount account) => Account = account;

        public WorkspaceAccount Account { get; }

        public override string ToString()
        {
            var group = string.IsNullOrWhiteSpace(Account.GroupId) ? "senza gruppo" : Account.GroupId;
            var state = Account.Enabled ? string.Empty : " · disattivo";
            return $"{Account.Name} · {group} · {Account.InitialBalance:N0} {Account.Currency} · " +
                   $"{Account.SymbolMappings.Count} symbol{state}";
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

    private sealed class WorkspaceBacktestItem
    {
        public WorkspaceBacktestItem(WorkspaceBacktestInfo info) => Info = info;
        public WorkspaceBacktestInfo Info { get; }
        public override string ToString()
            => $"{Info.FolderName} · {Info.ResultsCount} risultati · {Info.LastModifiedUtc.ToLocalTime():g}";
    }

    private sealed class TitanoRunListItem
    {
        public TitanoRunListItem(TitanoRunInfo info) => Info = info;
        public TitanoRunInfo Info { get; }
        public override string ToString()
            => $"{Info.RunId[..Math.Min(12, Info.RunId.Length)]}… · {Info.PeriodCount} periodi · {Info.GeneratedAtUtc.ToLocalTime():g}";
    }
}
