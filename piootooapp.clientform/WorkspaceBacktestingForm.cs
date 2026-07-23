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
    private readonly ComboBox _titanoWorkspaceCombo = new();
    private readonly ComboBox _titanoBacktestCombo = new();
    private readonly Label _titanoPathLabel = new();
    private readonly Button _refreshTitanoBacktestsButton = new();
    private readonly Button _openTitanoFolderButton = new();
    private readonly ComboBox _titanoPeriodCombo = new();
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
    private TitanoRotationManifest? _lastTitanoManifest;
    private readonly ComboBox _sessionWorkspaceCombo = new();
    private readonly ComboBox _sessionModeCombo = new();
    private readonly TextBox _sessionTitanoRunId = new();
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
    private readonly Button _runTitanoButton = new();
    private readonly TextBox _titanoResultsTextBox = new();
    private readonly Label _backtestingWorkspaceHint = new();
    private readonly Label _backtestingMasterFilterSummary = new();
    private readonly ListBox _backtestingMasterFilterStrategies = new();
    private readonly Button _editMasterFilterButton = new();
    private TabControl? _mainTabs;
    private TabPage? _workspacesTab;

    private List<StrategyCatalogItem> _strategies = new();
    private List<WorkspaceInfo> _workspaces = new();
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
        var titanoTab = new TabPage("Titano");
        var sessionsTab = new TabPage("Trading Session");
        _mainTabs.TabPages.Add(_workspacesTab);
        _mainTabs.TabPages.Add(backtestingTab);
        _mainTabs.TabPages.Add(titanoTab);
        _mainTabs.TabPages.Add(sessionsTab);
        root.Controls.Add(_mainTabs, 0, 1);

        _workspacesTab.Controls.Add(BuildWorkspacesTab());
        backtestingTab.Controls.Add(BuildBacktestingTab());
        titanoTab.Controls.Add(BuildTitanoTab());
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

    private Control BuildTradingSessionTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _sessionWorkspaceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionWorkspaceCombo.Width = 320;
        _sessionModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionModeCombo.Items.AddRange(Enum.GetNames<ExecutionMode>());
        _sessionModeCombo.SelectedItem = nameof(ExecutionMode.ServerSimulated);
        ConfigureTitanoNumber(_sessionAtrPeriods, 2, 500, 14);
        ConfigureTitanoNumber(_sessionTargetRisk, 1, 1_000_000, 1000, 2);
        ConfigureTitanoNumber(_sessionDrawdownCap, 1, 100, 20, 2);
        ConfigureTitanoNumber(_sessionCppiFloor, 0, 100, 80, 2);
        ConfigureTitanoNumber(_sessionCppiMultiplier, 0, 10, 1, 2);
        _sessionTitanoRunId.Width = 240;
        _sessionTitanoBacktest.Width = 160;

        var config = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        config.Controls.AddRange(new Control[]
        {
            TitanoLabel("Workspace"), _sessionWorkspaceCombo,
            TitanoLabel("Modalità"), _sessionModeCombo,
            TitanoLabel("Titano RunId (opz.)"), _sessionTitanoRunId,
            TitanoLabel("Backtest Titano"), _sessionTitanoBacktest,
            TitanoLabel("Metadata symbol,DPP,min,step,mode"), _sessionMetadata,
            _sessionAtrEnabled, TitanoLabel("Periodi ATR"), _sessionAtrPeriods,
            TitanoLabel("Rischio target $"), _sessionTargetRisk,
            _sessionPortfolioEnabled, TitanoLabel("DD cap %"), _sessionDrawdownCap,
            _sessionCppiEnabled, TitanoLabel("Floor %"), _sessionCppiFloor,
            TitanoLabel("Moltiplicatore"), _sessionCppiMultiplier
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
            Controls = { _sessionCreate, _sessionStart, _sessionStop, _sessionResume, _sessionSnapshot }
        }, 0, 1);
        _sessionOutput.Dock = DockStyle.Fill;
        _sessionOutput.Multiline = true;
        _sessionOutput.ReadOnly = true;
        _sessionOutput.ScrollBars = ScrollBars.Both;
        root.Controls.Add(_sessionOutput, 0, 2);
        return root;
    }

    private Control BuildTitanoTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titanoWorkspaceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoWorkspaceCombo.Width = 360;
        _titanoWorkspaceCombo.DisplayMember = nameof(WorkspaceListItem.DisplayText);
        _titanoWorkspaceCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (!_suppressWorkspaceEvents)
                await LoadTitanoBacktestsAsync();
        };
        root.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Controls =
            {
                new Label { Text = "Workspace obbligatorio:", AutoSize = true, Padding = new Padding(0, 7, 8, 0) },
                _titanoWorkspaceCombo
            }
        }, 0, 0);

        _titanoBacktestCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoBacktestCombo.Width = 430;
        _titanoBacktestCombo.SelectedIndexChanged += (_, _) => UpdateTitanoPath();
        _refreshTitanoBacktestsButton.Text = "Aggiorna backtest";
        _refreshTitanoBacktestsButton.AutoSize = true;
        _refreshTitanoBacktestsButton.Click += async (_, _) => await LoadTitanoBacktestsAsync(showErrors: true);
        _openTitanoFolderButton.Text = "Apri cartella risultati";
        _openTitanoFolderButton.AutoSize = true;
        _openTitanoFolderButton.Enabled = false;
        _openTitanoFolderButton.Click += (_, _) => OpenSelectedTitanoFolder();
        root.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Controls =
            {
                new Label { Text = "Backtest:", AutoSize = true, Padding = new Padding(0, 7, 8, 0) },
                _titanoBacktestCombo,
                _refreshTitanoBacktestsButton,
                _openTitanoFolderButton
            }
        }, 0, 1);

        ConfigureTitanoControls();
        var config = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        config.Controls.AddRange(new Control[]
        {
            TitanoLabel("Periodo"), _titanoPeriodCombo,
            TitanoLabel("Start UTC"), _titanoStartPicker, TitanoLabel("End UTC"), _titanoEndPicker,
            TitanoLabel("Breve gg"), _titanoShortDays, TitanoLabel("Lunga gg"), _titanoLongDays,
            TitanoLabel("Media gg"), _titanoMaDays, TitanoLabel("Min breve %"), _titanoMinShortReturn,
            TitanoLabel("Min lunga %"), _titanoMinLongReturn, TitanoLabel("Z min"), _titanoMinZ,
            TitanoLabel("Z max"), _titanoMaxZ, TitanoLabel("DD corrente %"), _titanoMaxCurrentDd,
            TitanoLabel("DD max %"), _titanoMaxDd, TitanoLabel("Volatilità %"), _titanoMaxVolatility,
            TitanoLabel("DD riattiva %"), _titanoReenableDd,
            TitanoLabel("Score OFF"), _titanoDisableScore, TitanoLabel("Score ON"), _titanoReenableScore,
            TitanoLabel("Cooldown"), _titanoCooldown, TitanoLabel("Min ON"), _titanoMinOn,
            TitanoLabel("Voti min"), _titanoMinVotes, TitanoLabel("Hard stop %"), _titanoHardStop,
            TitanoLabel("Tier sizing"), _titanoSizingTiers,
            TitanoLabel("Commissione/unità"), _titanoCommission, TitanoLabel("Slippage/unità"), _titanoSlippage,
            TitanoLabel("Calibrazione"), _titanoCalibration, TitanoLabel("OOS"), _titanoEvaluation,
            TitanoLabel("Walk-forward"), _titanoWalkForwardMode,
            _runTitanoButton, _titanoResetHardStopButton
        });
        root.Controls.Add(config, 0, 2);

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
        }, 0, 3);
        root.Controls.Add(new Label
        {
            Text = "Titano usa come input/base la cartella risultati del backtest selezionato; i suoi report sono salvati nella sottocartella titano.",
            AutoSize = true,
            Dock = DockStyle.Bottom
        }, 0, 4);
        return root;
    }

    private void ConfigureTitanoControls()
    {
        _titanoPeriodCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _titanoPeriodCombo.Items.AddRange(Enum.GetNames<TitanoRotationPeriod>());
        _titanoPeriodCombo.SelectedItem = nameof(TitanoRotationPeriod.Weekly);
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
        _titanoResetHardStopButton.Text = "Reset hard-stop…";
        _titanoResetHardStopButton.AutoSize = true;
        _titanoResetHardStopButton.Enabled = false;
        _titanoResetHardStopButton.Click += async (_, _) => await ResetTitanoHardStopAsync();
    }

    private static Label TitanoLabel(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(6, 7, 2, 0) };
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
        _startDatePicker.Value = CreateUtcPickerDefault(DateTime.UtcNow.Date.AddYears(-2));
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
            if (!_suppressWorkspaceEvents)
                await LoadBacktestingMasterFilterSummaryAsync();
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
            await LoadStrategiesAsync();
            PopulateWorkspaceStrategiesChecklist(Array.Empty<string>());
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
            _sessionWorkspaceCombo.Items.Clear();

            foreach (var workspace in _workspaces)
            {
                var item = new WorkspaceListItem(workspace);
                _workspaceList.Items.Add(item);
                _backtestingWorkspaceCombo.Items.Add(item);
                _titanoWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
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
            CommissionPerContract = _commissionInput.Value
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

    private async Task CreateTradingSessionAsync()
    {
        if (_sessionWorkspaceCombo.SelectedItem is not WorkspaceListItem workspace)
        {
            MessageBox.Show("Seleziona un workspace.", "Trading Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            NormalizeBaseAddress();
            var request = new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Info.Id,
                ExecutionMode = Enum.Parse<ExecutionMode>(_sessionModeCombo.SelectedItem?.ToString() ?? "ServerSimulated"),
                TitanoRunId = string.IsNullOrWhiteSpace(_sessionTitanoRunId.Text) ? null : _sessionTitanoRunId.Text.Trim(),
                TitanoBacktestFolder = string.IsNullOrWhiteSpace(_sessionTitanoRunId.Text) ? null : _sessionTitanoBacktest.Text.Trim(),
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
            RotationPeriod = Enum.Parse<TitanoRotationPeriod>(_titanoPeriodCombo.SelectedItem?.ToString() ?? "Weekly"),
            StartUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(_titanoEndPicker.Value.Date, DateTimeKind.Utc),
            BiweeklyAnchorUtc = DateTime.SpecifyKind(_titanoStartPicker.Value.Date, DateTimeKind.Utc),
            InitialCapital = _initialCapitalInput.Value,
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
            response.EnsureSuccessStatusCode();
            var manifest = await response.Content.ReadFromJsonAsync<TitanoRotationManifest>(_jsonOptions)
                ?? throw new InvalidOperationException("Manifest Titano non ricevuto.");
            _lastTitanoManifest = manifest;
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

    private sealed class WorkspaceBacktestItem
    {
        public WorkspaceBacktestItem(WorkspaceBacktestInfo info) => Info = info;
        public WorkspaceBacktestInfo Info { get; }
        public override string ToString()
            => $"{Info.FolderName} · {Info.ResultsCount} risultati · {Info.LastModifiedUtc.ToLocalTime():g}";
    }
}
