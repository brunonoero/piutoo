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

    private readonly TextBox _serverUrlTextBox = new() { Text = Shell.ClientSettings.ServerBaseUrl, Width = 280 };
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
    // Il nome finisce nel percorso dell'artefatto sul server, quindi è datato in UTC come tutto
    // ciò che il server scrive: con l'ora locale due postazioni in fusi diversi produrrebbero
    // nomi diversi per lo stesso run.
    private readonly TextBox _backtestNameTextBox = new() { Text = $"backtest-{DateTime.UtcNow:yyyyMMdd-HHmm}" };

    private readonly ListBox _workspaceList = new();
    private readonly FilterableStrategyChecklist _workspaceStrategiesList = new();
    private readonly TextBox _workspaceNameTextBox = new();
    private readonly Label _workspaceDetailLabel = new();
    private readonly Button _createWorkspaceButton = new();
    private readonly Button _deleteWorkspaceButton = new();
    private readonly Button _refreshWorkspacesButton = new();
    private readonly Button _saveMasterFilterButton = new();
    private readonly ComboBox _backtestingWorkspaceCombo = new();
    private readonly ComboBox _sessionWorkspaceCombo = new();
    private readonly ComboBox _sessionModeCombo = new();
    private readonly CheckBox _sessionPortfolioRiskEnabled =
        new() { Text = "Rischio di portafoglio", AutoSize = true };
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
    private readonly ComboBox _sessionPlanCombo = new();
    private readonly TextBox _sessionPlanCode = new();
    private readonly TextBox _sessionPlanName = new();
    private readonly Button _sessionPlanNew = new();
    private readonly Button _sessionPlanSave = new();
    private readonly Button _sessionPlanDelete = new();
    private List<TradingPlan> _tradingPlans = new();
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
    private readonly TextBox _tradingResultsFilter = new() { Width = 250, PlaceholderText = "Cerca symbol o strategia..." };

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
    private readonly ComboBox _accountSymbolConversionCombo = new();
    private readonly Button _accountNewButton = new();
    private readonly Button _accountSaveButton = new();
    private readonly Button _accountDeleteButton = new();
    private readonly Button _accountsReloadButton = new();
    private readonly Button _accountCreateDefaultButton = new();
    private readonly Label _accountStatusLabel = new();
    private List<WorkspaceAccount> _accounts = new();
    private List<string> _accountGroups = new();
    private List<SymbolConversion> _symbolConversions = new();
    private WorkspaceAccount? _editingAccount;
    private bool _suppressAccountEvents;

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
        var accountsTab = new TabPage("Accounts");
        var tradingResultsTab = new TabPage("Trading Results");
        var sessionsTab = new TabPage("Trading Session");
        _mainTabs.TabPages.Add(accountsTab);
        _mainTabs.TabPages.Add(_workspacesTab);
        _mainTabs.TabPages.Add(backtestingTab);
        _mainTabs.TabPages.Add(tradingResultsTab);
        _mainTabs.TabPages.Add(sessionsTab);
        root.Controls.Add(_mainTabs, 0, 1);

        _workspacesTab.Controls.Add(BuildWorkspacesTab());
        accountsTab.Controls.Add(BuildAccountsTab());
        backtestingTab.Controls.Add(BuildBacktestingTab());
        tradingResultsTab.Controls.Add(BuildTradingResultsTab());
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
        _tradingResultsFilter.TextChanged += (_, _) => ApplySymbolAndStrategyFilter(_tradingResultsGrid, _tradingResultsFilter.Text);

        root.Controls.Add(new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Controls =
            {
                FieldLabel("Workspace"), _tradingResultsWorkspaceCombo,
                FieldLabel("Backtest"), _tradingResultsBacktestCombo,
                FieldLabel("Filtro"), _tradingResultsFilter,
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

    private static void ApplySymbolAndStrategyFilter(DataGridView grid, string text)
    {
        var terms = text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (DataGridViewRow row in grid.Rows)
        {
            var searchable = $"{row.Cells["Symbol"].Value} {row.Cells["StrategyCode"].Value}";
            row.Visible = terms.All(term =>
                searchable.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    private string GetStrategySymbol(string strategyCode) =>
        _strategies.FirstOrDefault(strategy =>
            string.Equals(strategy.Name, strategyCode, StringComparison.OrdinalIgnoreCase))?.Symbol
        ?? string.Empty;

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
                "Apre la finestra di creazione: nessuna tabella di conversione assegnata, l'account opera 1 a 1 finché non se ne sceglie una."),
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

    /// <summary>
    /// La tabella di conversione non si edita più qui: è un registro globale, fuori da workspace e
    /// account (Anagrafiche → Conversioni simbolo nella Shell nuova). Qui si sceglie solo quale
    /// tabella nominata (per codice) referenzia questo account.
    /// </summary>
    private Control BuildAccountSymbolsPanel()
    {
        var group = new GroupBox
        {
            Text = "Conversione symbol",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Width = 700 };

        _accountSymbolConversionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _accountSymbolConversionCombo.Width = 400;

        layout.Controls.Add(LabeledField("Tabella di conversione", _accountSymbolConversionCombo,
            "Tabella nominata dal registro globale (Anagrafiche → Conversioni simbolo). Vuota = nessuna conversione, l'account opera 1 a 1."));

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

    private static readonly SymbolConversionListItem NoSymbolConversionItem = new(null, "(nessuna conversione — 1 a 1)");

    /// <summary>Ripopola la combo tabelle di conversione dal registro globale, preservando la selezione.</summary>
    private void RefreshSymbolConversionCombo()
    {
        var selectedCode = (_accountSymbolConversionCombo.SelectedItem as SymbolConversionListItem)?.Code;
        _accountSymbolConversionCombo.Items.Clear();
        _accountSymbolConversionCombo.Items.Add(NoSymbolConversionItem);
        foreach (var conversion in _symbolConversions.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            _accountSymbolConversionCombo.Items.Add(new SymbolConversionListItem(conversion.Code, conversion.Name));

        SelectSymbolConversion(selectedCode);
    }

    private void SelectSymbolConversion(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _accountSymbolConversionCombo.SelectedIndex = 0;
            return;
        }

        for (var index = 0; index < _accountSymbolConversionCombo.Items.Count; index++)
            if (_accountSymbolConversionCombo.Items[index] is SymbolConversionListItem item &&
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                _accountSymbolConversionCombo.SelectedIndex = index;
                return;
            }

        // Codice persistito ma non più nel registro: lo mostro invece di scartarlo, altrimenti
        // salvare l'account azzererebbe in silenzio un riferimento che un run potrebbe usare ancora.
        _accountSymbolConversionCombo.Items.Add(new SymbolConversionListItem(code, $"{code}  ·  (non più presente)"));
        _accountSymbolConversionCombo.SelectedIndex = _accountSymbolConversionCombo.Items.Count - 1;
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
                $"Account '{account.Name}' pronto: nessuna conversione (1 a 1), " +
                $"balance {account.InitialBalance:N0} {account.Currency}.";
            Log("Account di default disponibile nel registro globale.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore account di default", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private async Task ReloadAccountsAsync(bool showErrors)
    {
        try
        {
            NormalizeBaseAddress();
            var previousId = _editingAccount?.Id;
            _accounts = (await _workspaceApi.ListAccountsAsync()).ToList();
            _accountGroups = (await _workspaceApi.ListAccountGroupsAsync()).ToList();
            _symbolConversions = (await _workspaceApi.ListSymbolConversionsAsync()).ToList();
            RefreshAccountGroupLookups();
            RefreshSymbolConversionCombo();
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
            SelectSymbolConversion(null);
            _accountDeleteButton.Enabled = false;
            _accountSaveButton.Enabled = false;
        }
        finally
        {
            _suppressAccountEvents = false;
        }
    }

    /// <summary>
    /// Creazione account in modale. Nessuna tabella di conversione assegnata: l'account nuovo opera
    /// 1 a 1 finché non se ne sceglie una dal registro globale (Anagrafiche → Conversioni simbolo).
    /// </summary>
    private async Task CreateAccountViaDialogAsync()
    {
        using var dialog = new NewAccountDialog(_accountGroups);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var account = dialog.BuildAccount();

        try
        {
            NormalizeBaseAddress();
            var created = await _workspaceApi.CreateAccountAsync(account);
            _editingAccount = created;
            await ReloadAccountsAsync(showErrors: true);
            _accountStatusLabel.Text = $"Account '{created.Name}' creato, nessuna conversione (1 a 1).";
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

            SelectSymbolConversion(account.SymbolConversionCode);
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
            SymbolConversionCode = (_accountSymbolConversionCombo.SelectedItem as SymbolConversionListItem)?.Code ?? string.Empty
        };
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
            _accountStatusLabel.Text = $"Account '{saved.Name}' salvato.";
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
        _formToolTip.SetToolTip(_sessionPortfolioRiskEnabled,
            "Attiva i freni di portafoglio del sizing: riduzione sul drawdown dal picco e sull'esposizione lorda. " +
            "In ExternalBroker il rischio di portafoglio è governato dal broker, non dal server.");

        var config = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        config.Controls.AddRange(new Control[]
        {
            FieldLabel("Piano", "Configurazione operativa riutilizzabile salvata nel workspace."),
            _sessionPlanCombo,
            FieldLabel("Codice piano", "Codice globale inserito nel cBot."),
            _sessionPlanCode,
            FieldLabel("Nome piano", "Nome leggibile del piano."),
            _sessionPlanName,
            _sessionPlanNew,
            _sessionPlanSave,
            _sessionPlanDelete,
            FieldLabel("Workspace", "Workspace per cui viene creata e gestita la sessione di trading."),
            WithHelp(_sessionWorkspaceCombo, "Workspace per cui viene creata e gestita la sessione di trading."),
            FieldLabel("Modalità", "ServerSimulated: esecuzione simulata lato server. ExternalBroker: gli ordini vengono inoltrati a un broker esterno."),
            WithHelp(_sessionModeCombo, "ServerSimulated: esecuzione simulata lato server. ExternalBroker: gli ordini vengono inoltrati a un broker esterno."),
            _sessionPortfolioRiskEnabled
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
            Name = "AccountNumber", HeaderText = "Conto cTrader", FillWeight = 30,
            DisplayMember = nameof(AccountNumberListItem.DisplayText),
            ValueMember = nameof(AccountNumberListItem.AccountNumber),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        _sessionAccountGroups.DataError += (_, e) => e.ThrowException = false;
        _sessionAccountGroups.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_sessionAccountGroups.IsCurrentCellDirty)
                _sessionAccountGroups.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _formToolTip.SetToolTip(_sessionAccountGroups,
            "Un conto per riga: ognuno riceve ogni segnale della sessione, con la size del proprio capitale. Quanto puo' tenere aperto lo dice il piano, non questa griglia.");
        layout.Controls.Add(_sessionAccountGroups, 0, 0);

        _sessionAddAccountGroupRow.Text = "Aggiungi riga";
        _sessionSaveAccountGroups.Text = "Salva gruppi account";
        _sessionReloadAccountGroups.Text = "Ricarica gruppi account";
        _sessionAddAccountGroupRow.Click += (_, _) => _sessionAccountGroups.Rows.Add();
        _sessionSaveAccountGroups.Click += async (_, _) => await SaveAccountGroupsAsync();
        _sessionReloadAccountGroups.Click += async (_, _) => await ReloadAccountGroupsAsync();
        layout.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            Controls =
            {
                WithHelp(_sessionAddAccountGroupRow, "Aggiunge una riga vuota alla griglia."),
                WithHelp(_sessionSaveAccountGroups, "Salva gruppi e account."),
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
            var conti = ReadSessionAccounts();

            using var request = new HttpRequestMessage(HttpMethod.Put,
                $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/accounts")
            {
                Content = JsonContent.Create(new SetSessionAccountsRequest
                {
                    SessionToken = _activeSession.SessionToken,
                    Accounts = conti
                }, options: _jsonOptions)
            };
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var snapshot = await response.Content.ReadFromJsonAsync<TradingSessionSnapshot>(_jsonOptions);
            ShowSession($"Conti della sessione salvati ({conti.Count}).");
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
                $"api/v1/trading-sessions/{Uri.EscapeDataString(_activeSession.SessionId)}/accounts");
            request.Headers.Add("X-Session-Token", _activeSession.SessionToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var accounts = await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions) ?? [];
            _sessionAccountGroups.Rows.Clear();
            foreach (var conto in accounts)
                _sessionAccountGroups.Rows.Add(conto);
            ShowSession($"Caricati {accounts.Count} conti configurati.");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private List<string> ReadSessionAccounts()
        => _sessionAccountGroups.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => Convert.ToString(row.Cells["AccountNumber"].Value ?? string.Empty)!.Trim())
            .Where(account => account.Length > 0)
            .ToList();

    private static int ParseMaxConcurrentTrades(DataGridViewRow row)
    {
        var raw = Convert.ToString(row.Cells["MaxConcurrentTrades"].Value ?? string.Empty)?.Trim();
        if (string.IsNullOrEmpty(raw))
            return 0;
        if (!int.TryParse(raw, out var value) || value < 0)
            throw new InvalidOperationException("Max trade contemporanei deve essere un intero maggiore o uguale a zero.");
        return value;
    }

    private Control WithHelp(Control control, string help)
    {
        _formToolTip.SetToolTip(control, help);
        return control;
    }

    private Label FieldLabel(string text, string? help = null)
    {
        var label = new Label { Text = text, AutoSize = true, Padding = new Padding(6, 7, 2, 0) };
        if (!string.IsNullOrEmpty(help)) _formToolTip.SetToolTip(label, help);
        return label;
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
        _initialCapitalInput.Value = TradingConventions.StrategyReferenceBalance;
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

        // Niente selettore account: il backtest interno è neutro rispetto ai conti. Conversione
        // simbolo e scala del capitale agiscono solo sulle sessioni (docs/decisioni.md 2026-08-05).

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
            await ReloadAccountsAsync(showErrors: false);

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
            _tradingResultsWorkspaceCombo.Items.Clear();
            _sessionWorkspaceCombo.Items.Clear();

            foreach (var workspace in _workspaces)
            {
                var item = new WorkspaceListItem(workspace);
                _workspaceList.Items.Add(item);
                _backtestingWorkspaceCombo.Items.Add(item);
                _tradingResultsWorkspaceCombo.Items.Add(new WorkspaceListItem(workspace));
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

                if (_tradingResultsWorkspaceCombo.Items.Count > 0)
                    _tradingResultsWorkspaceCombo.SelectedIndex = 0;
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
        SelectComboWorkspace(_tradingResultsWorkspaceCombo, workspaceId);
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
            ApplySymbolAndStrategyFilter(_tradingResultsGrid, _tradingResultsFilter.Text);

            _tradingResultsSummary.Text =
                $"{backtest.Info.FolderName}: {trades.Count} operazioni chiuse lette da trades.json · " +
                $"P&L netto totale {trades.Sum(trade => trade.NetProfit):N2}.";
        }
        catch (Exception ex)
        {
            _tradingResultsSummary.Text = $"Impossibile leggere trades.json: {ex.Message}";
        }
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
        _sessionAccountGroups.Rows.Clear();
        foreach (var account in plan.Accounts)
        {
            _sessionAccountGroups.Rows.Add(account);
        }
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
            var conti = ReadSessionAccounts();
            if (conti.Count == 0)
                throw new InvalidOperationException("Il piano richiede almeno un conto.");
            var request = new SaveTradingPlanRequest
            {
                Code = _sessionPlanCode.Text.Trim(),
                Name = _sessionPlanName.Text.Trim(),
                Accounts = conti,
                PositionSizing = new PositionSizingConfig
                {
                    PortfolioRisk = new PortfolioRiskSizingConfig
                    {
                        Enabled = _sessionPortfolioRiskEnabled.Checked
                    }
                }
            };
            var uri = $"api/v1/workspaces/{Uri.EscapeDataString(workspace.Info.Id)}/trading-plans/" +
                      Uri.EscapeDataString(request.Code);
            var response = await _httpClient.PutAsJsonAsync(uri, request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            await LoadTradingPlansAsync(request.Code);
            ShowSession($"Piano {request.Code} salvato ({conti.Count} conti).");
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
        try
        {
            NormalizeBaseAddress();
            var request = new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Info.Id,
                ExecutionMode = Enum.Parse<ExecutionMode>(_sessionModeCombo.SelectedItem?.ToString() ?? "ServerSimulated"),
                PositionSizing = new PositionSizingConfig
                {
                    PortfolioRisk = new PortfolioRiskSizingConfig
                    {
                        Enabled = _sessionPortfolioRiskEnabled.Checked,
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

        public NewAccountDialog(IReadOnlyList<string> groups)
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
                Text = "Nessuna tabella di conversione assegnata: l'account opera 1 a 1. Puoi sceglierne una dal " +
                       "registro globale (Anagrafiche → Conversioni simbolo) dopo la creazione."
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
            var conversion = string.IsNullOrWhiteSpace(Account.SymbolConversionCode)
                ? "nessuna conversione"
                : $"conversione {Account.SymbolConversionCode}";
            var state = Account.Enabled ? string.Empty : " · disattivo";
            return $"{Account.Name} · {group} · {Account.InitialBalance:N0} {Account.Currency} · " +
                   $"{conversion}{state}";
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

    /// <summary>Voce della combo tabelle di conversione: null è "nessuna conversione", 1 a 1.</summary>
    private sealed class SymbolConversionListItem
    {
        public SymbolConversionListItem(string? code, string display)
        {
            Code = code;
            Display = display;
        }

        public string? Code { get; }
        public string Display { get; }

        public override string ToString() => Display;
    }

    private sealed class WorkspaceBacktestItem
    {
        public WorkspaceBacktestItem(WorkspaceBacktestInfo info) => Info = info;
        public WorkspaceBacktestInfo Info { get; }
        public override string ToString()
            => $"{Info.FolderName} · {Info.ResultsCount} risultati · {Info.LastModifiedUtc.ToLocalTime():g}";
    }

}
