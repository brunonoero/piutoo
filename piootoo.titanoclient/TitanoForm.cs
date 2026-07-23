using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Utilities;

namespace piootoo.titanoclient;

public partial class TitanoForm : Form
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TextBox _serverUrlTextBox = new() { Text = "https://localhost:7116" };
    private readonly TextBox _nameTextBox = new() { Text = $"Titano_{DateTime.Now:yyyyMMdd_HHmm}" };
    private readonly TextBox _codeTextBox = new() { Text = $"TIT_{DateTime.Now:yyyyMMdd_HHmm}" };
    private readonly DateTimePicker _startDatePicker = new();
    private readonly DateTimePicker _endDatePicker = new();
    private readonly NumericUpDown _capitalInput = new();
    private readonly NumericUpDown _commissionInput = new();
    private readonly TextBox _strategyFilterTextBox = new();
    private readonly CheckedListBox _symbolsList = new();
    private readonly CheckedListBox _strategiesList = new();
    private readonly ComboBox _backtestsComboBox = new();
    private readonly NumericUpDown _lookbackInput = new();
    private readonly NumericUpDown _minProfitInput = new();
    private readonly NumericUpDown _maxDdInput = new();
    private readonly NumericUpDown _minWinRateInput = new();
    private readonly NumericUpDown _minTradesInput = new();
    private readonly NumericUpDown _profitFactorInput = new();
    private readonly NumericUpDown _positiveWeeksInput = new();
    private readonly NumericUpDown _maxLossStreakInput = new();
    private readonly NumericUpDown _minTradeWinRateInput = new();
    private readonly NumericUpDown _maxWeeklyLossInput = new();
    private readonly NumericUpDown _maxSingleWeekReturnInput = new();
    private readonly NumericUpDown _cooldownInput = new();
    private readonly NumericUpDown _maxStrategiesOnInput = new();
    private readonly NumericUpDown _minSharpeInput = new();
    private readonly NumericUpDown _minWeeksBeforeRulesInput = new();
    private readonly ComboBox _setupComboBox = new();
    private readonly CheckBox _closeWeekEndCheckBox = new() { Text = "Chiudi tutte le posizioni a fine settimana", AutoSize = true, Checked = true };
    private readonly Button _loadSetupButton = new();
    private readonly Button _saveSetupButton = new();
    private readonly Button _loadButton = new();
    private readonly Button _runBacktestButton = new();
    private readonly Button _refreshBacktestsButton = new();
    private readonly Button _applyFilterButton = new();
    private readonly Button _openReportButton = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private readonly TextBox _logTextBox = new();

    private List<StrategyDefinition> _strategies = new();
    private readonly HashSet<string> _selectedStrategyIds = new(StringComparer.OrdinalIgnoreCase);
    private List<BacktestingResult> _backtests = new();
    private List<TitanoSetupInfo> _titanoSetups = new();
    private TitanoFilterResult? _lastTitanoResult;
    private string? _currentSetupId;

    public TitanoForm()
    {
        InitializeComponent();
        WireEvents();
    }

    private void WireEvents()
    {
        _loadButton.Click += async (_, _) => await LoadRemoteDataAsync();
        _symbolsList.ItemCheck += (_, _) => BeginInvoke((Action)RefreshStrategiesList);
        _strategyFilterTextBox.TextChanged += (_, _) => RefreshStrategiesList();
        _strategiesList.ItemCheck += (_, args) => BeginInvoke((Action)(() => SyncStrategySelection(args.Index, args.NewValue == CheckState.Checked)));
        _runBacktestButton.Click += async (_, _) => await StartBacktestAsync();
        _refreshBacktestsButton.Click += async (_, _) => await LoadBacktestsAsync();
        _loadSetupButton.Click += async (_, _) => await LoadSelectedSetupAsync();
        _saveSetupButton.Click += async (_, _) => await SaveCurrentSetupAsync();
        _applyFilterButton.Click += async (_, _) => await ApplyTitanoFilterAsync();
        _openReportButton.Click += (_, _) => OpenReport();
    }

    private void BuildUi()
    {
        Text = "Piootoo Titano Client";
        Width = 1220;
        Height = 840;
        StartPosition = FormStartPosition.CenterScreen;

        _startDatePicker.Format = DateTimePickerFormat.Custom;
        _startDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _startDatePicker.Value = CreateUtcPickerDefault(DateTime.UtcNow.Date.AddYears(-2));
        _endDatePicker.Format = DateTimePickerFormat.Custom;
        _endDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _endDatePicker.Value = CreateUtcPickerDefault(DateTime.UtcNow);

        _capitalInput.Maximum = 1_000_000_000;
        _capitalInput.DecimalPlaces = 2;
        _capitalInput.Increment = 1000;
        _capitalInput.Value = 1_000_000;
        _commissionInput.Maximum = 1000;
        _commissionInput.DecimalPlaces = 2;
        _commissionInput.Increment = 0.5m;
        _commissionInput.Value = 2;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        Controls.Add(root);

        root.Controls.Add(BuildServerPanel(), 0, 0);
        root.Controls.Add(BuildTabs(), 0, 1);
        root.Controls.Add(BuildStatusPanel(), 0, 2);
        root.Controls.Add(BuildLogPanel(), 0, 3);
    }

    private Control BuildServerPanel()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        _serverUrlTextBox.Width = 260;
        _loadButton.Text = "Carica strategie/backtest";
        _loadButton.AutoSize = true;
        _loadButton.Click += async (_, _) => await LoadRemoteDataAsync();
        panel.Controls.Add(new Label { Text = "Server API:", AutoSize = true, Padding = new Padding(0, 7, 4, 0) });
        panel.Controls.Add(_serverUrlTextBox);
        panel.Controls.Add(_loadButton);
        return panel;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildPhase1Tab());
        tabs.TabPages.Add(BuildPhase2Tab());
        return tabs;
    }

    private TabPage BuildPhase1Tab()
    {
        var page = new TabPage("Fase 1 - Backtesting");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);

        var paramsPanel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 8, AutoSize = true };
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        paramsPanel.Controls.Add(new Label { Text = "Nome", AutoSize = true }, 0, 0);
        paramsPanel.Controls.Add(_nameTextBox, 1, 0);
        paramsPanel.Controls.Add(new Label { Text = "Codice", AutoSize = true }, 2, 0);
        paramsPanel.Controls.Add(_codeTextBox, 3, 0);
        paramsPanel.Controls.Add(new Label { Text = "Capitale", AutoSize = true }, 4, 0);
        paramsPanel.Controls.Add(_capitalInput, 5, 0);
        paramsPanel.Controls.Add(new Label { Text = "Commissione", AutoSize = true }, 6, 0);
        paramsPanel.Controls.Add(_commissionInput, 7, 0);
        paramsPanel.Controls.Add(new Label { Text = "Start (UTC)", AutoSize = true }, 0, 1);
        paramsPanel.Controls.Add(_startDatePicker, 1, 1);
        paramsPanel.Controls.Add(new Label { Text = "End (UTC)", AutoSize = true }, 2, 1);
        paramsPanel.Controls.Add(_endDatePicker, 3, 1);
        paramsPanel.SetColumnSpan(_closeWeekEndCheckBox, 8);
        paramsPanel.Controls.Add(_closeWeekEndCheckBox, 0, 2);
        layout.Controls.Add(paramsPanel, 0, 0);

        var strategyTools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        _symbolsList.CheckOnClick = true;
        _symbolsList.Width = 220;
        _symbolsList.Height = 92;
        _symbolsList.ItemCheck += (_, _) => BeginInvoke((Action)RefreshStrategiesList);
        _strategyFilterTextBox.Width = 260;
        _strategyFilterTextBox.PlaceholderText = "Filtro strategie libero";
        _strategyFilterTextBox.TextChanged += (_, _) => RefreshStrategiesList();
        _strategiesList.ItemCheck += (_, args) => BeginInvoke((Action)(() => SyncStrategySelection(args.Index, args.NewValue == CheckState.Checked)));
        _runBacktestButton.Text = "Avvia backtesting";
        _runBacktestButton.AutoSize = true;
        _runBacktestButton.Click += async (_, _) => await StartBacktestAsync();
        var allButton = new Button { Text = "Seleziona tutte", AutoSize = true };
        allButton.Click += (_, _) => SetAllStrategyChecks(true);
        var noneButton = new Button { Text = "Deseleziona", AutoSize = true };
        noneButton.Click += (_, _) => SetAllStrategyChecks(false);
        strategyTools.Controls.Add(new Label { Text = "Symbol:", AutoSize = true, Padding = new Padding(0, 7, 4, 0) });
        strategyTools.Controls.Add(_symbolsList);
        strategyTools.Controls.Add(new Label { Text = "Filtro strategie:", AutoSize = true, Padding = new Padding(10, 7, 4, 0) });
        strategyTools.Controls.Add(_strategyFilterTextBox);
        strategyTools.Controls.Add(allButton);
        strategyTools.Controls.Add(noneButton);
        strategyTools.Controls.Add(_runBacktestButton);
        layout.Controls.Add(strategyTools, 0, 1);

        _strategiesList.Dock = DockStyle.Fill;
        _strategiesList.CheckOnClick = true;
        _strategiesList.HorizontalScrollbar = true;
        layout.Controls.Add(_strategiesList, 0, 2);
        return page;
    }

    private TabPage BuildPhase2Tab()
    {
        var page = new TabPage("Fase 2 - Filtro Titano");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scroll.Controls.Add(layout);
        page.Controls.Add(scroll);

        _backtestsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _backtestsComboBox.Width = 620;
        _refreshBacktestsButton.Text = "Aggiorna backtest";
        _refreshBacktestsButton.AutoSize = true;
        _refreshBacktestsButton.Click += async (_, _) => await LoadBacktestsAsync();

        _setupComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _setupComboBox.Width = 420;
        _loadSetupButton.Text = "Carica setup";
        _loadSetupButton.AutoSize = true;
        _loadSetupButton.Click += async (_, _) => await LoadSelectedSetupAsync();
        _saveSetupButton.Text = "Salva setup";
        _saveSetupButton.AutoSize = true;
        _saveSetupButton.Click += async (_, _) => await SaveCurrentSetupAsync();

        ConfigureNumeric(_lookbackInput, 1, 52, 0, 6);
        ConfigureNumeric(_minProfitInput, -1_000_000, 1_000_000, 2, 0);
        ConfigureNumeric(_maxDdInput, -100, 0, 2, -12);
        ConfigureNumeric(_minWinRateInput, 0, 100, 2, 50);
        ConfigureNumeric(_minTradesInput, 0, 1000, 0, 4);
        ConfigureNumeric(_profitFactorInput, 0, 100, 2, 1.10m);
        ConfigureNumeric(_positiveWeeksInput, 0, 100, 2, 50);
        ConfigureNumeric(_maxLossStreakInput, 0, 52, 0, 2);
        ConfigureNumeric(_minTradeWinRateInput, 0, 100, 2, 45);
        ConfigureNumeric(_maxWeeklyLossInput, -100, 0, 2, -3);
        ConfigureNumeric(_maxSingleWeekReturnInput, 0, 200, 2, 12);
        ConfigureNumeric(_cooldownInput, 0, 52, 0, 3);
        ConfigureNumeric(_maxStrategiesOnInput, 0, 20, 0, 2);
        ConfigureNumeric(_minSharpeInput, 0, 10, 2, 0.35m);
        ConfigureNumeric(_minWeeksBeforeRulesInput, 0, 52, 0, 6);

        var row = 0;
        AddRow(layout, row++, "Backtesting", _backtestsComboBox, _refreshBacktestsButton);
        AddRow(layout, row++, "Setup Titano", _setupComboBox, BuildSetupActionsPanel());
        AddRow(layout, row++, "Finestra N settimane", _lookbackInput);
        AddRow(layout, row++, "Min settimane prima regole", _minWeeksBeforeRulesInput);
        AddRow(layout, row++, "Min rolling profit", _minProfitInput);
        AddRow(layout, row++, "Max DD rolling (%)", _maxDdInput);
        AddRow(layout, row++, "Min settimane positive (%)", _minWinRateInput);
        AddRow(layout, row++, "Min win rate trade (%)", _minTradeWinRateInput);
        AddRow(layout, row++, "Min trades", _minTradesInput);
        AddRow(layout, row++, "Min profit factor", _profitFactorInput);
        AddRow(layout, row++, "Min settimane positive ratio (%)", _positiveWeeksInput);
        AddRow(layout, row++, "Max loss streak settimane", _maxLossStreakInput);
        AddRow(layout, row++, "Max perdita ultima settimana (%)", _maxWeeklyLossInput);
        AddRow(layout, row++, "Max spike settimanale (%)", _maxSingleWeekReturnInput);
        AddRow(layout, row++, "Cooldown settimane dopo OFF", _cooldownInput);
        AddRow(layout, row++, "Max strategie ON", _maxStrategiesOnInput);
        AddRow(layout, row++, "Min Sharpe rolling", _minSharpeInput);

        var actions = new FlowLayoutPanel { AutoSize = true };
        _applyFilterButton.Text = "Applica filtro Titano";
        _applyFilterButton.AutoSize = true;
        _applyFilterButton.Click += async (_, _) => await ApplyTitanoFilterAsync();
        _openReportButton.Text = "Apri report HTML";
        _openReportButton.AutoSize = true;
        _openReportButton.Enabled = false;
        _openReportButton.Click += (_, _) => OpenReport();
        actions.Controls.Add(_applyFilterButton);
        actions.Controls.Add(_openReportButton);
        layout.Controls.Add(new Label { Text = "Azioni", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, row);
        layout.Controls.Add(actions, 1, row);
        return page;
    }

    private Control BuildSetupActionsPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true };
        panel.Controls.Add(_loadSetupButton);
        panel.Controls.Add(_saveSetupButton);
        return panel;
    }

    private static void ConfigureNumeric(NumericUpDown input, decimal min, decimal max, int decimals, decimal value)
    {
        input.Minimum = min;
        input.Maximum = max;
        input.DecimalPlaces = decimals;
        input.Increment = decimals == 0 ? 1 : 0.01m;
        input.Value = Math.Clamp(value, min, max);
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control, Control? extra = null)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 10, 0) }, 0, row);
        if (extra == null)
        {
            layout.Controls.Add(control, 1, row);
        }
        else
        {
            var panel = new FlowLayoutPanel { AutoSize = true };
            panel.Controls.Add(control);
            panel.Controls.Add(extra);
            layout.Controls.Add(panel, 1, row);
        }
    }

    private Control BuildStatusPanel()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        _progressBar.Width = 260;
        _statusLabel.Text = "Pronto";
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(8, 7, 0, 0);
        panel.Controls.Add(_progressBar);
        panel.Controls.Add(_statusLabel);
        return panel;
    }

    private Control BuildLogPanel()
    {
        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        return _logTextBox;
    }

    private async Task LoadRemoteDataAsync()
    {
        try
        {
            NormalizeBaseAddress();
            await LoadStrategiesAsync();
            await LoadBacktestsAsync();
            await LoadTitanoSetupsAsync();
        }
        catch (Exception ex)
        {
            Log($"Errore caricamento: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadStrategiesAsync()
    {
        _strategies = await _httpClient.GetFromJsonAsync<List<StrategyDefinition>>("api/PiootooOptimization/strategies", _jsonOptions) ?? new();
        _selectedStrategyIds.Clear();
        foreach (var strategy in _strategies)
        {
            _selectedStrategyIds.Add(strategy.Id);
        }
        _symbolsList.Items.Clear();
        foreach (var symbol in _strategies.Select(s => s.Symbol).Distinct().OrderBy(s => s))
        {
            _symbolsList.Items.Add(symbol, true);
        }
        RefreshStrategiesList();
        Log($"Caricate {_strategies.Count} strategie.");
    }

    private async Task LoadBacktestsAsync()
    {
        NormalizeBaseAddress();
        _backtests = await _httpClient.GetFromJsonAsync<List<BacktestingResult>>("api/Backtesting/list", _jsonOptions) ?? new();
        _backtestsComboBox.Items.Clear();
        foreach (var backtest in _backtests.OrderByDescending(b => b.CreatedAt))
        {
            _backtestsComboBox.Items.Add(new BacktestItem(backtest));
        }
        if (_backtestsComboBox.Items.Count > 0) _backtestsComboBox.SelectedIndex = 0;
        Log($"Caricati {_backtests.Count} backtesting salvati.");
    }

    private async Task LoadTitanoSetupsAsync()
    {
        NormalizeBaseAddress();
        _titanoSetups = (await _httpClient.GetFromJsonAsync<List<TitanoSetupInfo>>("api/Titano/setups", _jsonOptions) ?? new())
            .OrderBy(s => s.Name)
            .ToList();
        _setupComboBox.Items.Clear();
        foreach (var setup in _titanoSetups)
        {
            _setupComboBox.Items.Add(new SetupItem(setup));
        }

        var preferred = _titanoSetups.FirstOrDefault(s => s.Id.Equals("gc-bias-consigliato", StringComparison.OrdinalIgnoreCase));
        if (preferred != null)
        {
            for (var i = 0; i < _setupComboBox.Items.Count; i++)
            {
                if (_setupComboBox.Items[i] is SetupItem item && item.Info.Id == preferred.Id)
                {
                    _setupComboBox.SelectedIndex = i;
                    await LoadSelectedSetupAsync(silent: true);
                    break;
                }
            }
        }
        else if (_setupComboBox.Items.Count > 0)
        {
            _setupComboBox.SelectedIndex = 0;
        }

        Log($"Caricati {_titanoSetups.Count} setup Titano.");
    }

    private async Task LoadSelectedSetupAsync(bool silent = false)
    {
        if (_setupComboBox.SelectedItem is not SetupItem item)
        {
            if (!silent)
            {
                MessageBox.Show("Seleziona un setup.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return;
        }

        NormalizeBaseAddress();
        var setup = await _httpClient.GetFromJsonAsync<TitanoFilterSetup>($"api/Titano/setups/{Uri.EscapeDataString(item.Info.Id)}", _jsonOptions);
        if (setup == null)
        {
            throw new InvalidOperationException($"Setup '{item.Info.Id}' non trovato.");
        }

        ApplySetupToUi(setup);
        if (!silent)
        {
            Log($"Setup caricato: {setup.Name} ({setup.Id})");
        }
    }

    private async Task SaveCurrentSetupAsync()
    {
        var setup = BuildSetupFromUi();
        using var dialog = new Form
        {
            Text = "Salva setup Titano",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(420, 180)
        };
        var idBox = new TextBox { Text = setup.Id, Width = 280, Left = 110, Top = 16 };
        var nameBox = new TextBox { Text = setup.Name, Width = 280, Left = 110, Top = 48 };
        var descBox = new TextBox { Text = setup.Description, Width = 280, Left = 110, Top = 80, Multiline = true, Height = 48 };
        dialog.Controls.Add(new Label { Text = "Id", AutoSize = true, Left = 16, Top = 20 });
        dialog.Controls.Add(idBox);
        dialog.Controls.Add(new Label { Text = "Nome", AutoSize = true, Left = 16, Top = 52 });
        dialog.Controls.Add(nameBox);
        dialog.Controls.Add(new Label { Text = "Descrizione", AutoSize = true, Left = 16, Top = 84 });
        dialog.Controls.Add(descBox);
        var ok = new Button { Text = "Salva", DialogResult = DialogResult.OK, Left = 220, Top = 136, Width = 80 };
        var cancel = new Button { Text = "Annulla", DialogResult = DialogResult.Cancel, Left = 310, Top = 136, Width = 80 };
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        setup.Id = idBox.Text.Trim();
        setup.Name = nameBox.Text.Trim();
        setup.Description = descBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(setup.Name))
        {
            MessageBox.Show("Il nome del setup e' obbligatorio.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        NormalizeBaseAddress();
        var response = await _httpClient.PostAsJsonAsync("api/Titano/setups", setup);
        response.EnsureSuccessStatusCode();
        var saved = await response.Content.ReadFromJsonAsync<TitanoFilterSetup>(_jsonOptions);
        if (saved != null)
        {
            ApplySetupToUi(saved);
        }

        await LoadTitanoSetupsAsync();
        Log($"Setup salvato: {setup.Name} ({setup.Id})");
    }

    private void ApplySetupToUi(TitanoFilterSetup setup)
    {
        _currentSetupId = setup.Id;
        _lookbackInput.Value = Math.Clamp(setup.LookbackWeeks, (int)_lookbackInput.Minimum, (int)_lookbackInput.Maximum);
        _minProfitInput.Value = Math.Clamp(setup.Rules.MinRollingProfit, _minProfitInput.Minimum, _minProfitInput.Maximum);
        _maxDdInput.Value = Math.Clamp(setup.Rules.MaxRollingDrawdown * 100m, _maxDdInput.Minimum, _maxDdInput.Maximum);
        _minWinRateInput.Value = Math.Clamp(setup.Rules.MinWinRate * 100m, _minWinRateInput.Minimum, _minWinRateInput.Maximum);
        _minTradesInput.Value = Math.Clamp(setup.Rules.MinTrades, _minTradesInput.Minimum, _minTradesInput.Maximum);
        _profitFactorInput.Value = Math.Clamp(setup.Rules.MinProfitFactor, _profitFactorInput.Minimum, _profitFactorInput.Maximum);
        _positiveWeeksInput.Value = Math.Clamp(setup.Rules.MinPositiveWeeksRatio * 100m, _positiveWeeksInput.Minimum, _positiveWeeksInput.Maximum);
        _maxLossStreakInput.Value = Math.Clamp(setup.Rules.MaxConsecutiveLosingWeeks, _maxLossStreakInput.Minimum, _maxLossStreakInput.Maximum);
        _minTradeWinRateInput.Value = Math.Clamp(setup.Rules.MinTradeWinRate * 100m, _minTradeWinRateInput.Minimum, _minTradeWinRateInput.Maximum);
        _maxWeeklyLossInput.Value = Math.Clamp(setup.Rules.MaxWeeklyLoss * 100m, _maxWeeklyLossInput.Minimum, _maxWeeklyLossInput.Maximum);
        _maxSingleWeekReturnInput.Value = Math.Clamp(setup.Rules.MaxSingleWeekReturn * 100m, _maxSingleWeekReturnInput.Minimum, _maxSingleWeekReturnInput.Maximum);
        _cooldownInput.Value = Math.Clamp(setup.Rules.CooldownWeeksAfterOff, _cooldownInput.Minimum, _cooldownInput.Maximum);
        _maxStrategiesOnInput.Value = Math.Clamp(setup.Rules.MaxStrategiesOn, _maxStrategiesOnInput.Minimum, _maxStrategiesOnInput.Maximum);
        _minSharpeInput.Value = Math.Clamp(setup.Rules.MinSharpeRatio, _minSharpeInput.Minimum, _minSharpeInput.Maximum);
        _minWeeksBeforeRulesInput.Value = Math.Clamp(setup.Rules.MinWeeksBeforeRulesApply, _minWeeksBeforeRulesInput.Minimum, _minWeeksBeforeRulesInput.Maximum);
        _closeWeekEndCheckBox.Checked = setup.TradingRules.CloseAllPositionsAtWeekEnd;

        for (var i = 0; i < _setupComboBox.Items.Count; i++)
        {
            if (_setupComboBox.Items[i] is SetupItem item && item.Info.Id.Equals(setup.Id, StringComparison.OrdinalIgnoreCase))
            {
                _setupComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private TitanoFilterSetup BuildSetupFromUi()
    {
        return new TitanoFilterSetup
        {
            Id = _currentSetupId ?? string.Empty,
            Name = _setupComboBox.SelectedItem is SetupItem selected ? selected.Info.Name : "Setup personalizzato",
            Description = _setupComboBox.SelectedItem is SetupItem selectedInfo ? selectedInfo.Info.Description : string.Empty,
            LookbackWeeks = (int)_lookbackInput.Value,
            Rules = BuildTitanoRulesFromUi(),
            TradingRules = new TitanoTradingRules
            {
                CloseAllPositionsAtWeekEnd = _closeWeekEndCheckBox.Checked
            }
        };
    }

    private TitanoFilterRules BuildTitanoRulesFromUi() => new()
    {
        MinRollingProfit = _minProfitInput.Value,
        MaxRollingDrawdown = _maxDdInput.Value / 100m,
        MinWinRate = _minWinRateInput.Value / 100m,
        MinTrades = (int)_minTradesInput.Value,
        MinProfitFactor = _profitFactorInput.Value,
        MinPositiveWeeksRatio = _positiveWeeksInput.Value / 100m,
        MaxConsecutiveLosingWeeks = (int)_maxLossStreakInput.Value,
        MinTradeWinRate = _minTradeWinRateInput.Value / 100m,
        MaxWeeklyLoss = _maxWeeklyLossInput.Value / 100m,
        MaxSingleWeekReturn = _maxSingleWeekReturnInput.Value / 100m,
        CooldownWeeksAfterOff = (int)_cooldownInput.Value,
        MaxStrategiesOn = (int)_maxStrategiesOnInput.Value,
        MinSharpeRatio = _minSharpeInput.Value,
        MinWeeksBeforeRulesApply = (int)_minWeeksBeforeRulesInput.Value
    };

    private TitanoFilterRequest BuildTitanoFilterRequest(string backtestingId) => new()
    {
        BacktestingId = backtestingId,
        Name = _nameTextBox.Text.Trim(),
        Code = _codeTextBox.Text.Trim(),
        SetupId = _currentSetupId,
        LookbackWeeks = (int)_lookbackInput.Value,
        Rules = BuildTitanoRulesFromUi(),
        TradingRules = new TitanoTradingRules
        {
            CloseAllPositionsAtWeekEnd = _closeWeekEndCheckBox.Checked
        }
    };

    private void RefreshStrategiesList()
    {
        var selectedSymbols = GetSelectedSymbols();
        var textFilter = _strategyFilterTextBox.Text.Trim();
        _strategiesList.Items.Clear();
        foreach (var strategy in _strategies.Where(s =>
            (!selectedSymbols.Any() || selectedSymbols.Contains(s.Symbol)) &&
            StrategyMatchesTextFilter(s, textFilter)))
        {
            _strategiesList.Items.Add(new StrategyItem(strategy), _selectedStrategyIds.Contains(strategy.Id));
        }
    }

    private HashSet<string> GetSelectedSymbols()
    {
        return _symbolsList.CheckedItems
            .Cast<object>()
            .Select(item => item.ToString() ?? string.Empty)
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void SetAllStrategyChecks(bool isChecked)
    {
        for (var i = 0; i < _strategiesList.Items.Count; i++)
        {
            _strategiesList.SetItemChecked(i, isChecked);
            if (_strategiesList.Items[i] is StrategyItem item)
            {
                if (isChecked)
                {
                    _selectedStrategyIds.Add(item.Strategy.Id);
                }
                else
                {
                    _selectedStrategyIds.Remove(item.Strategy.Id);
                }
            }
        }
    }

    private async Task StartBacktestAsync()
    {
        var selectedSymbols = GetSelectedSymbols();
        var textFilter = _strategyFilterTextBox.Text.Trim();
        var selected = _strategies
            .Where(strategy =>
                _selectedStrategyIds.Contains(strategy.Id) &&
                (!selectedSymbols.Any() || selectedSymbols.Contains(strategy.Symbol)) &&
                StrategyMatchesTextFilter(strategy, textFilter))
            .ToList();
        if (!selected.Any())
        {
            MessageBox.Show("Seleziona almeno una strategia.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        NormalizeBaseAddress();
        var request = new BacktestingRequest
        {
            Name = $"{_nameTextBox.Text.Trim()}_{_codeTextBox.Text.Trim()}",
            StartDate = TradingDateTime.ToFeedUtc(_startDatePicker.Value),
            EndDate = TradingDateTime.ToFeedUtc(_endDatePicker.Value),
            InitialCapital = _capitalInput.Value,
            CommissionPerContract = _commissionInput.Value,
            CloseAllPositionsAtWeekEnd = _closeWeekEndCheckBox.Checked,
            SelectedSymbols = selected.Select(s => s.Symbol).Distinct().ToList(),
            SelectedStrategyIds = selected.Select(s => s.Id).ToList()
        };

        SetBusy(true);
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Backtesting/start", request);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<JobStartResponse>(_jsonOptions);
            if (payload == null || string.IsNullOrWhiteSpace(payload.JobId)) throw new InvalidOperationException("JobId non ricevuto.");
            Log($"Backtesting avviato: {payload.JobId}");
            await PollBacktestAsync(payload.JobId);
            await LoadBacktestsAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task PollBacktestAsync(string jobId)
    {
        while (true)
        {
            await Task.Delay(1000);
            var job = await _httpClient.GetFromJsonAsync<BacktestingJob>($"api/Backtesting/status/{jobId}", _jsonOptions);
            if (job == null) continue;
            _progressBar.Value = Math.Clamp(job.ProgressPercent, 0, 100);
            _statusLabel.Text = $"Backtest {job.Status} {job.ProgressPercent}%";
            if (job.Status == BacktestingJobStatus.Completed)
            {
                Log($"Backtesting completato: {jobId}");
                return;
            }
            if (job.Status == BacktestingJobStatus.Failed)
            {
                throw new InvalidOperationException(job.ErrorMessage ?? "Backtesting fallito.");
            }
        }
    }

    private async Task ApplyTitanoFilterAsync()
    {
        if (_backtestsComboBox.SelectedItem is not BacktestItem item)
        {
            MessageBox.Show("Seleziona un backtesting.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var request = BuildTitanoFilterRequest(item.Result.JobId);

        SetBusy(true);
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Titano/apply-filter", request);
            response.EnsureSuccessStatusCode();
            _lastTitanoResult = await response.Content.ReadFromJsonAsync<TitanoFilterResult>(_jsonOptions);
            if (_lastTitanoResult == null) throw new InvalidOperationException("Risultato Titano non ricevuto.");
            _openReportButton.Enabled = !string.IsNullOrWhiteSpace(_lastTitanoResult.HtmlReportFilePath) && File.Exists(_lastTitanoResult.HtmlReportFilePath);
            Log($"Filtro Titano completato. Profit originale {_lastTitanoResult.OriginalTotalProfit:F2}, filtrato {_lastTitanoResult.FilteredTotalProfit:F2}");
            Log($"HTML: {_lastTitanoResult.HtmlReportFilePath}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OpenReport()
    {
        var path = _lastTitanoResult?.HtmlReportFilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void NormalizeBaseAddress()
    {
        var url = _serverUrlTextBox.Text.Trim().TrimEnd('/') + "/";
        if (_httpClient.BaseAddress?.ToString() != url)
        {
            _httpClient.BaseAddress = new Uri(url);
        }
    }

    private void SetBusy(bool busy)
    {
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        _runBacktestButton.Enabled = !busy;
        _applyFilterButton.Enabled = !busy;
        _loadButton.Enabled = !busy;
        _refreshBacktestsButton.Enabled = !busy;
        _loadSetupButton.Enabled = !busy;
        _saveSetupButton.Enabled = !busy;
        _symbolsList.Enabled = !busy;
        _strategyFilterTextBox.Enabled = !busy;
    }

    private void SyncStrategySelection(int index, bool isChecked)
    {
        if (index < 0 || index >= _strategiesList.Items.Count || _strategiesList.Items[index] is not StrategyItem item)
        {
            return;
        }

        if (isChecked)
        {
            _selectedStrategyIds.Add(item.Strategy.Id);
        }
        else
        {
            _selectedStrategyIds.Remove(item.Strategy.Id);
        }
    }

    private static bool StrategyMatchesTextFilter(StrategyDefinition strategy, string textFilter)
    {
        if (string.IsNullOrWhiteSpace(textFilter))
        {
            return true;
        }

        return new[]
        {
            strategy.Id,
            strategy.Name,
            strategy.FileName,
            strategy.Symbol,
            strategy.Description,
            strategy.TimeframeMinutes.ToString()
        }.Any(value => (value ?? string.Empty).Contains(textFilter, StringComparison.OrdinalIgnoreCase));
    }

    private void Log(string message) => _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");

    private sealed class StrategyItem
    {
        public StrategyItem(StrategyDefinition strategy) => Strategy = strategy;
        public StrategyDefinition Strategy { get; }
        public override string ToString() => $"{Strategy.Symbol} | {Strategy.Name} | {Strategy.TimeframeMinutes}m | {Strategy.FileName}";
    }

    private sealed class BacktestItem
    {
        public BacktestItem(BacktestingResult result) => Result = result;
        public BacktestingResult Result { get; }
        public override string ToString() => $"{Result.CreatedAt:yyyy-MM-dd HH:mm} | {Result.SetupName} | {Result.JobId} | Profit {Result.TotalProfit:F2}";
    }

    private sealed class SetupItem
    {
        public SetupItem(TitanoSetupInfo info) => Info = info;
        public TitanoSetupInfo Info { get; }
        public override string ToString() => $"{Info.Name} ({Info.Id})";
    }

    private static DateTime CreateUtcPickerDefault(DateTime utcValue) =>
        new(utcValue.Year, utcValue.Month, utcValue.Day, utcValue.Hour, utcValue.Minute, utcValue.Second);

    private sealed class JobStartResponse
    {
        public string JobId { get; set; } = string.Empty;
    }
}
