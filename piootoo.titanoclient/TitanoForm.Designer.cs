namespace piootoo.titanoclient;

public partial class TitanoForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        var root = new TableLayoutPanel();
        var serverPanel = new FlowLayoutPanel();
        var serverLabel = new Label();
        var tabs = new TabControl();
        var phase1Page = new TabPage();
        var phase1Layout = new TableLayoutPanel();
        var paramsPanel = new TableLayoutPanel();
        var nameLabel = new Label();
        var codeLabel = new Label();
        var capitalLabel = new Label();
        var commissionLabel = new Label();
        var startLabel = new Label();
        var endLabel = new Label();
        var strategyTools = new FlowLayoutPanel();
        var symbolLabel = new Label();
        var strategyFilterLabel = new Label();
        var allButton = new Button();
        var noneButton = new Button();
        var phase2Page = new TabPage();
        var phase2Scroll = new Panel();
        var phase2Layout = new TableLayoutPanel();
        var setupActionsPanel = new FlowLayoutPanel();
        var actionsPanel = new FlowLayoutPanel();
        var statusPanel = new FlowLayoutPanel();
        SuspendLayout();
        // 
        // root
        // 
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(serverPanel, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        root.Controls.Add(statusPanel, 0, 2);
        root.Controls.Add(_logTextBox, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.Padding = new Padding(12);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        root.Size = new Size(1220, 840);
        root.TabIndex = 0;
        // 
        // serverPanel
        // 
        serverPanel.AutoSize = true;
        serverPanel.Controls.Add(serverLabel);
        serverPanel.Controls.Add(_serverUrlTextBox);
        serverPanel.Controls.Add(_loadButton);
        serverPanel.Dock = DockStyle.Top;
        serverPanel.Location = new Point(15, 15);
        serverPanel.Name = "serverPanel";
        serverPanel.Size = new Size(1190, 29);
        serverPanel.TabIndex = 0;
        // 
        // serverLabel
        // 
        serverLabel.AutoSize = true;
        serverLabel.Padding = new Padding(0, 7, 4, 0);
        serverLabel.Text = "Server API:";
        // 
        // _serverUrlTextBox
        // 
        _serverUrlTextBox.Text = "https://localhost:7116";
        _serverUrlTextBox.Width = 260;
        // 
        // _loadButton
        // 
        _loadButton.AutoSize = true;
        _loadButton.Text = "Carica strategie/backtest";
        // 
        // tabs
        // 
        tabs.Controls.Add(phase1Page);
        tabs.Controls.Add(phase2Page);
        tabs.Dock = DockStyle.Fill;
        tabs.Location = new Point(15, 50);
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(1190, 486);
        tabs.TabIndex = 1;
        // 
        // phase1Page
        // 
        phase1Page.Controls.Add(phase1Layout);
        phase1Page.Location = new Point(4, 24);
        phase1Page.Name = "phase1Page";
        phase1Page.Padding = new Padding(3);
        phase1Page.Size = new Size(1182, 458);
        phase1Page.TabIndex = 0;
        phase1Page.Text = "Fase 1 - Backtesting";
        phase1Page.UseVisualStyleBackColor = true;
        // 
        // phase1Layout
        // 
        phase1Layout.ColumnCount = 1;
        phase1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        phase1Layout.Controls.Add(paramsPanel, 0, 0);
        phase1Layout.Controls.Add(strategyTools, 0, 1);
        phase1Layout.Controls.Add(_strategiesList, 0, 2);
        phase1Layout.Dock = DockStyle.Fill;
        phase1Layout.Location = new Point(3, 3);
        phase1Layout.Name = "phase1Layout";
        phase1Layout.Padding = new Padding(10);
        phase1Layout.RowCount = 3;
        phase1Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase1Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase1Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        phase1Layout.Size = new Size(1176, 452);
        phase1Layout.TabIndex = 0;
        // 
        // paramsPanel
        // 
        paramsPanel.AutoSize = true;
        paramsPanel.ColumnCount = 8;
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        paramsPanel.Controls.Add(nameLabel, 0, 0);
        paramsPanel.Controls.Add(_nameTextBox, 1, 0);
        paramsPanel.Controls.Add(codeLabel, 2, 0);
        paramsPanel.Controls.Add(_codeTextBox, 3, 0);
        paramsPanel.Controls.Add(capitalLabel, 4, 0);
        paramsPanel.Controls.Add(_capitalInput, 5, 0);
        paramsPanel.Controls.Add(commissionLabel, 6, 0);
        paramsPanel.Controls.Add(_commissionInput, 7, 0);
        paramsPanel.Controls.Add(startLabel, 0, 1);
        paramsPanel.Controls.Add(_startDatePicker, 1, 1);
        paramsPanel.Controls.Add(endLabel, 2, 1);
        paramsPanel.Controls.Add(_endDatePicker, 3, 1);
        paramsPanel.Controls.Add(_closeWeekEndCheckBox, 0, 2);
        paramsPanel.Dock = DockStyle.Top;
        paramsPanel.Location = new Point(13, 13);
        paramsPanel.Name = "paramsPanel";
        paramsPanel.RowCount = 3;
        paramsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paramsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paramsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paramsPanel.SetColumnSpan(_closeWeekEndCheckBox, 8);
        paramsPanel.Size = new Size(1150, 83);
        paramsPanel.TabIndex = 0;
        // 
        // parameter controls
        // 
        nameLabel.AutoSize = true;
        nameLabel.Text = "Nome";
        codeLabel.AutoSize = true;
        codeLabel.Text = "Codice";
        capitalLabel.AutoSize = true;
        capitalLabel.Text = "Capitale";
        commissionLabel.AutoSize = true;
        commissionLabel.Text = "Commissione";
        startLabel.AutoSize = true;
        startLabel.Text = "Start (UTC)";
        endLabel.AutoSize = true;
        endLabel.Text = "End (UTC)";
        _nameTextBox.Text = "Titano";
        _codeTextBox.Text = "TIT";
        _capitalInput.DecimalPlaces = 2;
        _capitalInput.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
        _capitalInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
        _capitalInput.Value = new decimal(new int[] { 1000000, 0, 0, 0 });
        _commissionInput.DecimalPlaces = 2;
        _commissionInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _commissionInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        _commissionInput.Value = new decimal(new int[] { 2, 0, 0, 0 });
        _startDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _startDatePicker.Format = DateTimePickerFormat.Custom;
        _endDatePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _endDatePicker.Format = DateTimePickerFormat.Custom;
        _closeWeekEndCheckBox.AutoSize = true;
        _closeWeekEndCheckBox.Checked = true;
        _closeWeekEndCheckBox.CheckState = CheckState.Checked;
        _closeWeekEndCheckBox.Text = "Chiudi tutte le posizioni a fine settimana";
        // 
        // strategyTools
        // 
        strategyTools.AutoSize = true;
        strategyTools.Controls.Add(symbolLabel);
        strategyTools.Controls.Add(_symbolsList);
        strategyTools.Controls.Add(strategyFilterLabel);
        strategyTools.Controls.Add(_strategyFilterTextBox);
        strategyTools.Controls.Add(allButton);
        strategyTools.Controls.Add(noneButton);
        strategyTools.Controls.Add(_runBacktestButton);
        strategyTools.Dock = DockStyle.Top;
        strategyTools.Location = new Point(13, 102);
        strategyTools.Name = "strategyTools";
        strategyTools.Size = new Size(1150, 98);
        strategyTools.TabIndex = 1;
        symbolLabel.AutoSize = true;
        symbolLabel.Padding = new Padding(0, 7, 4, 0);
        symbolLabel.Text = "Symbol:";
        _symbolsList.CheckOnClick = true;
        _symbolsList.Height = 92;
        _symbolsList.Width = 220;
        strategyFilterLabel.AutoSize = true;
        strategyFilterLabel.Padding = new Padding(10, 7, 4, 0);
        strategyFilterLabel.Text = "Filtro strategie:";
        _strategyFilterTextBox.PlaceholderText = "Filtro strategie libero";
        _strategyFilterTextBox.Width = 260;
        allButton.AutoSize = true;
        allButton.Text = "Seleziona tutte";
        noneButton.AutoSize = true;
        noneButton.Text = "Deseleziona";
        _runBacktestButton.AutoSize = true;
        _runBacktestButton.Text = "Avvia backtesting";
        _strategiesList.CheckOnClick = true;
        _strategiesList.Dock = DockStyle.Fill;
        _strategiesList.HorizontalScrollbar = true;
        // 
        // phase2Page
        // 
        phase2Page.Controls.Add(phase2Scroll);
        phase2Page.Location = new Point(4, 24);
        phase2Page.Name = "phase2Page";
        phase2Page.Padding = new Padding(3);
        phase2Page.Size = new Size(1182, 458);
        phase2Page.TabIndex = 1;
        phase2Page.Text = "Fase 2 - Filtro Titano";
        phase2Page.UseVisualStyleBackColor = true;
        phase2Scroll.AutoScroll = true;
        phase2Scroll.Controls.Add(phase2Layout);
        phase2Scroll.Dock = DockStyle.Fill;
        phase2Scroll.Location = new Point(3, 3);
        phase2Scroll.Name = "phase2Scroll";
        phase2Scroll.Size = new Size(1176, 452);
        phase2Scroll.TabIndex = 0;
        phase2Layout.AutoSize = true;
        phase2Layout.ColumnCount = 2;
        phase2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        phase2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        phase2Layout.Dock = DockStyle.Top;
        phase2Layout.Location = new Point(0, 0);
        phase2Layout.Name = "phase2Layout";
        phase2Layout.Padding = new Padding(10);
        phase2Layout.RowCount = 20;
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phase2Layout.Size = new Size(1176, 428);
        phase2Layout.TabIndex = 0;
        _backtestsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _backtestsComboBox.Margin = new Padding(3, 4, 3, 4);
        _backtestsComboBox.Width = 620;
        _refreshBacktestsButton.AutoSize = true;
        _refreshBacktestsButton.Margin = new Padding(3, 4, 3, 8);
        _refreshBacktestsButton.Text = "Aggiorna backtest";
        _setupComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _setupComboBox.Margin = new Padding(3, 4, 3, 4);
        _setupComboBox.Width = 420;
        _loadSetupButton.AutoSize = true;
        _loadSetupButton.Text = "Carica setup";
        _saveSetupButton.AutoSize = true;
        _saveSetupButton.Text = "Salva setup";
        setupActionsPanel.AutoSize = true;
        setupActionsPanel.Margin = new Padding(3, 4, 3, 8);
        setupActionsPanel.Controls.Add(_loadSetupButton);
        setupActionsPanel.Controls.Add(_saveSetupButton);
        _lookbackInput.Minimum = 1;
        _lookbackInput.Maximum = 52;
        _lookbackInput.Value = 6;
        _minWeeksBeforeRulesInput.Maximum = 52;
        _minWeeksBeforeRulesInput.Value = 6;
        _minProfitInput.Minimum = -1000000;
        _minProfitInput.Maximum = 1000000;
        _minProfitInput.DecimalPlaces = 2;
        _maxDdInput.Minimum = -100;
        _maxDdInput.DecimalPlaces = 2;
        _maxDdInput.Value = -12;
        _minWinRateInput.Maximum = 100;
        _minWinRateInput.DecimalPlaces = 2;
        _minWinRateInput.Value = 50;
        _minTradeWinRateInput.Maximum = 100;
        _minTradeWinRateInput.DecimalPlaces = 2;
        _minTradeWinRateInput.Value = 45;
        _minTradesInput.Maximum = 1000;
        _minTradesInput.Value = 4;
        _profitFactorInput.Maximum = 100;
        _profitFactorInput.DecimalPlaces = 2;
        _profitFactorInput.Value = new decimal(new int[] { 110, 0, 0, 131072 });
        _positiveWeeksInput.Maximum = 100;
        _positiveWeeksInput.DecimalPlaces = 2;
        _positiveWeeksInput.Value = 50;
        _maxLossStreakInput.Maximum = 52;
        _maxLossStreakInput.Value = 2;
        _maxWeeklyLossInput.Minimum = -100;
        _maxWeeklyLossInput.DecimalPlaces = 2;
        _maxWeeklyLossInput.Value = -3;
        _maxSingleWeekReturnInput.Maximum = 200;
        _maxSingleWeekReturnInput.DecimalPlaces = 2;
        _maxSingleWeekReturnInput.Value = 12;
        _cooldownInput.Maximum = 52;
        _cooldownInput.Value = 3;
        _maxStrategiesOnInput.Maximum = 20;
        _maxStrategiesOnInput.Value = 2;
        _minSharpeInput.Maximum = 10;
        _minSharpeInput.DecimalPlaces = 2;
        _minSharpeInput.Value = new decimal(new int[] { 35, 0, 0, 131072 });
        phase2Layout.Controls.Add(new Label { Text = "Backtesting", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 0);
        phase2Layout.Controls.Add(_backtestsComboBox, 1, 0);
        phase2Layout.Controls.Add(_refreshBacktestsButton, 1, 1);
        phase2Layout.SetColumnSpan(_refreshBacktestsButton, 1);
        phase2Layout.Controls.Add(new Label { Text = "Setup Titano", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 2);
        phase2Layout.Controls.Add(_setupComboBox, 1, 2);
        phase2Layout.Controls.Add(setupActionsPanel, 1, 3);
        phase2Layout.SetColumnSpan(setupActionsPanel, 1);
        phase2Layout.Controls.Add(new Label { Text = "Finestra N settimane", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 4);
        phase2Layout.Controls.Add(_lookbackInput, 1, 4);
        phase2Layout.Controls.Add(new Label { Text = "Min settimane prima regole", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 5);
        phase2Layout.Controls.Add(_minWeeksBeforeRulesInput, 1, 5);
        phase2Layout.Controls.Add(new Label { Text = "Min rolling profit", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 6);
        phase2Layout.Controls.Add(_minProfitInput, 1, 6);
        phase2Layout.Controls.Add(new Label { Text = "Max DD rolling (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 7);
        phase2Layout.Controls.Add(_maxDdInput, 1, 7);
        phase2Layout.Controls.Add(new Label { Text = "Min settimane positive (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 8);
        phase2Layout.Controls.Add(_minWinRateInput, 1, 8);
        phase2Layout.Controls.Add(new Label { Text = "Min win rate trade (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 9);
        phase2Layout.Controls.Add(_minTradeWinRateInput, 1, 9);
        phase2Layout.Controls.Add(new Label { Text = "Min trades", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 10);
        phase2Layout.Controls.Add(_minTradesInput, 1, 10);
        phase2Layout.Controls.Add(new Label { Text = "Min profit factor", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 11);
        phase2Layout.Controls.Add(_profitFactorInput, 1, 11);
        phase2Layout.Controls.Add(new Label { Text = "Min settimane positive ratio (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 12);
        phase2Layout.Controls.Add(_positiveWeeksInput, 1, 12);
        phase2Layout.Controls.Add(new Label { Text = "Max loss streak settimane", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 13);
        phase2Layout.Controls.Add(_maxLossStreakInput, 1, 13);
        phase2Layout.Controls.Add(new Label { Text = "Max perdita ultima settimana (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 14);
        phase2Layout.Controls.Add(_maxWeeklyLossInput, 1, 14);
        phase2Layout.Controls.Add(new Label { Text = "Max spike settimanale (%)", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 15);
        phase2Layout.Controls.Add(_maxSingleWeekReturnInput, 1, 15);
        phase2Layout.Controls.Add(new Label { Text = "Cooldown settimane dopo OFF", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 16);
        phase2Layout.Controls.Add(_cooldownInput, 1, 16);
        phase2Layout.Controls.Add(new Label { Text = "Max strategie ON", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 17);
        phase2Layout.Controls.Add(_maxStrategiesOnInput, 1, 17);
        phase2Layout.Controls.Add(new Label { Text = "Min Sharpe rolling", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 18);
        phase2Layout.Controls.Add(_minSharpeInput, 1, 18);
        actionsPanel.AutoSize = true;
        actionsPanel.Controls.Add(_applyFilterButton);
        actionsPanel.Controls.Add(_openReportButton);
        _applyFilterButton.AutoSize = true;
        _applyFilterButton.Text = "Applica filtro Titano";
        _openReportButton.AutoSize = true;
        _openReportButton.Enabled = false;
        _openReportButton.Text = "Apri report HTML";
        phase2Layout.Controls.Add(new Label { Text = "Azioni", AutoSize = true, Padding = new Padding(0, 7, 10, 0) }, 0, 19);
        phase2Layout.Controls.Add(actionsPanel, 1, 19);
        // 
        // statusPanel
        // 
        statusPanel.AutoSize = true;
        statusPanel.Controls.Add(_progressBar);
        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Dock = DockStyle.Top;
        statusPanel.Location = new Point(15, 542);
        statusPanel.Name = "statusPanel";
        statusPanel.Size = new Size(1190, 29);
        statusPanel.TabIndex = 2;
        _progressBar.Width = 260;
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(8, 7, 0, 0);
        _statusLabel.Text = "Pronto";
        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        // 
        // TitanoForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1220, 840);
        Controls.Add(root);
        Name = "TitanoForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Piootoo Titano Client";
        ResumeLayout(false);
    }

    #endregion
}
