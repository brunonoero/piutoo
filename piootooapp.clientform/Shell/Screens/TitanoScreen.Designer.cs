namespace piootooapp.clientform.Shell.Screens;

partial class TitanoScreen
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this._commandsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._setupCombo = new System.Windows.Forms.ComboBox();
        this._loadSetupButton = new System.Windows.Forms.Button();
        this._saveSetupButton = new System.Windows.Forms.Button();
        this._runButton = new System.Windows.Forms.Button();
        this._reportButton = new System.Windows.Forms.Button();
        this._reloadButton = new System.Windows.Forms.Button();
        this._tabs = new System.Windows.Forms.TabControl();
        this._runTab = new System.Windows.Forms.TabPage();
        this._resultsTextBox = new System.Windows.Forms.TextBox();
        this._contextLayout = new System.Windows.Forms.TableLayoutPanel();
        this._workspaceLabel = new System.Windows.Forms.Label();
        this._workspaceCombo = new System.Windows.Forms.ComboBox();
        this._backtestLabel = new System.Windows.Forms.Label();
        this._backtestCombo = new System.Windows.Forms.ComboBox();
        this._startLabel = new System.Windows.Forms.Label();
        this._startPicker = new System.Windows.Forms.DateTimePicker();
        this._endLabel = new System.Windows.Forms.Label();
        this._endPicker = new System.Windows.Forms.DateTimePicker();
        this._initialCapitalLabel = new System.Windows.Forms.Label();
        this._initialCapitalInput = new System.Windows.Forms.NumericUpDown();
        this._periodLabel = new System.Windows.Forms.Label();
        this._periodCombo = new System.Windows.Forms.ComboBox();
        this._setupNameLabel = new System.Windows.Forms.Label();
        this._setupNameTextBox = new System.Windows.Forms.TextBox();
        this._setupDescriptionLabel = new System.Windows.Forms.Label();
        this._setupDescriptionTextBox = new System.Windows.Forms.TextBox();
        this._filtersTab = new System.Windows.Forms.TabPage();
        this._filtersLayout = new System.Windows.Forms.TableLayoutPanel();
        this._minimumTradesLabel = new System.Windows.Forms.Label();
        this._minimumTradesInput = new System.Windows.Forms.NumericUpDown();
        this._minimumPassingFiltersLabel = new System.Windows.Forms.Label();
        this._minimumPassingFiltersInput = new System.Windows.Forms.NumericUpDown();
        this._shortWindowLabel = new System.Windows.Forms.Label();
        this._shortWindowInput = new System.Windows.Forms.NumericUpDown();
        this._longWindowLabel = new System.Windows.Forms.Label();
        this._longWindowInput = new System.Windows.Forms.NumericUpDown();
        this._movingAverageWindowLabel = new System.Windows.Forms.Label();
        this._movingAverageWindowInput = new System.Windows.Forms.NumericUpDown();
        this._requireEquityAboveMaCheckBox = new System.Windows.Forms.CheckBox();
        this._minimumShortReturnLabel = new System.Windows.Forms.Label();
        this._minimumShortReturnInput = new System.Windows.Forms.NumericUpDown();
        this._minimumLongReturnLabel = new System.Windows.Forms.Label();
        this._minimumLongReturnInput = new System.Windows.Forms.NumericUpDown();
        this._minimumZScoreLabel = new System.Windows.Forms.Label();
        this._minimumZScoreInput = new System.Windows.Forms.NumericUpDown();
        this._maximumZScoreLabel = new System.Windows.Forms.Label();
        this._maximumZScoreInput = new System.Windows.Forms.NumericUpDown();
        this._maximumCurrentDrawdownLabel = new System.Windows.Forms.Label();
        this._maximumCurrentDrawdownInput = new System.Windows.Forms.NumericUpDown();
        this._maximumObservedDrawdownLabel = new System.Windows.Forms.Label();
        this._maximumObservedDrawdownInput = new System.Windows.Forms.NumericUpDown();
        this._maximumVolatilityLabel = new System.Windows.Forms.Label();
        this._maximumVolatilityInput = new System.Windows.Forms.NumericUpDown();
        this._reenableDrawdownLabel = new System.Windows.Forms.Label();
        this._reenableDrawdownInput = new System.Windows.Forms.NumericUpDown();
        this._hardStopLabel = new System.Windows.Forms.Label();
        this._hardStopInput = new System.Windows.Forms.NumericUpDown();
        this._cooldownLabel = new System.Windows.Forms.Label();
        this._cooldownInput = new System.Windows.Forms.NumericUpDown();
        this._minimumOnPeriodsLabel = new System.Windows.Forms.Label();
        this._minimumOnPeriodsInput = new System.Windows.Forms.NumericUpDown();
        this._sizingTab = new System.Windows.Forms.TabPage();
        this._sizingLayout = new System.Windows.Forms.TableLayoutPanel();
        this._crossSectionalCheckBox = new System.Windows.Forms.CheckBox();
        this._crossSectionalHintLabel = new System.Windows.Forms.Label();
        this._minimumAllocationLabel = new System.Windows.Forms.Label();
        this._minimumAllocationInput = new System.Windows.Forms.NumericUpDown();
        this._maximumAllocationLabel = new System.Windows.Forms.Label();
        this._maximumAllocationInput = new System.Windows.Forms.NumericUpDown();
        this._allocationStepLabel = new System.Windows.Forms.Label();
        this._allocationStepInput = new System.Windows.Forms.NumericUpDown();
        this._sizingTiersLabel = new System.Windows.Forms.Label();
        this._sizingTiersTextBox = new System.Windows.Forms.TextBox();
        this._disableScoreLabel = new System.Windows.Forms.Label();
        this._disableScoreInput = new System.Windows.Forms.NumericUpDown();
        this._reenableScoreLabel = new System.Windows.Forms.Label();
        this._reenableScoreInput = new System.Windows.Forms.NumericUpDown();
        this._commissionLabel = new System.Windows.Forms.Label();
        this._commissionInput = new System.Windows.Forms.NumericUpDown();
        this._slippageLabel = new System.Windows.Forms.Label();
        this._slippageInput = new System.Windows.Forms.NumericUpDown();
        this._calibrationPeriodsLabel = new System.Windows.Forms.Label();
        this._calibrationPeriodsInput = new System.Windows.Forms.NumericUpDown();
        this._evaluationPeriodsLabel = new System.Windows.Forms.Label();
        this._evaluationPeriodsInput = new System.Windows.Forms.NumericUpDown();
        this._walkForwardLabel = new System.Windows.Forms.Label();
        this._walkForwardCombo = new System.Windows.Forms.ComboBox();
        this._commandsPanel.SuspendLayout();
        this._tabs.SuspendLayout();
        this._runTab.SuspendLayout();
        this._contextLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialCapitalInput)).BeginInit();
        this._filtersTab.SuspendLayout();
        this._filtersLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._minimumTradesInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumPassingFiltersInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._shortWindowInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._longWindowInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._movingAverageWindowInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumShortReturnInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumLongReturnInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumZScoreInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumZScoreInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumCurrentDrawdownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumObservedDrawdownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumVolatilityInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._reenableDrawdownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._hardStopInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._cooldownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumOnPeriodsInput)).BeginInit();
        this._sizingTab.SuspendLayout();
        this._sizingLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._minimumAllocationInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumAllocationInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._allocationStepInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._disableScoreInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._reenableScoreInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._slippageInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._calibrationPeriodsInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._evaluationPeriodsInput)).BeginInit();
        this.SuspendLayout();
        // 
        // _commandsPanel
        // 
        this._commandsPanel.AutoSize = true;
        this._commandsPanel.Controls.Add(this._setupCombo);
        this._commandsPanel.Controls.Add(this._loadSetupButton);
        this._commandsPanel.Controls.Add(this._saveSetupButton);
        this._commandsPanel.Controls.Add(this._runButton);
        this._commandsPanel.Controls.Add(this._reportButton);
        this._commandsPanel.Controls.Add(this._reloadButton);
        this._commandsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandsPanel.Location = new System.Drawing.Point(0, 0);
        this._commandsPanel.Name = "_commandsPanel";
        this._commandsPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 6);
        this._commandsPanel.Size = new System.Drawing.Size(900, 43);
        this._commandsPanel.TabIndex = 0;
        this._commandsPanel.WrapContents = false;
        // 
        // _setupCombo
        // 
        this._setupCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._setupCombo.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
        this._setupCombo.Name = "_setupCombo";
        this._setupCombo.Size = new System.Drawing.Size(280, 23);
        this._setupCombo.TabIndex = 0;
        // 
        // _loadSetupButton
        // 
        this._loadSetupButton.AutoSize = true;
        this._loadSetupButton.Name = "_loadSetupButton";
        this._loadSetupButton.Size = new System.Drawing.Size(100, 25);
        this._loadSetupButton.TabIndex = 1;
        this._loadSetupButton.Text = "Carica setup";
        this._loadSetupButton.UseVisualStyleBackColor = true;
        this._loadSetupButton.Click += new System.EventHandler(this.OnLoadSetupClick);
        // 
        // _saveSetupButton
        // 
        this._saveSetupButton.AutoSize = true;
        this._saveSetupButton.Name = "_saveSetupButton";
        this._saveSetupButton.Size = new System.Drawing.Size(100, 25);
        this._saveSetupButton.TabIndex = 2;
        this._saveSetupButton.Text = "Salva setup";
        this._saveSetupButton.UseVisualStyleBackColor = true;
        this._saveSetupButton.Click += new System.EventHandler(this.OnSaveSetupClick);
        // 
        // _runButton
        // 
        this._runButton.AutoSize = true;
        this._runButton.Margin = new System.Windows.Forms.Padding(20, 3, 3, 3);
        this._runButton.Name = "_runButton";
        this._runButton.Size = new System.Drawing.Size(130, 25);
        this._runButton.TabIndex = 3;
        this._runButton.Text = "Esegui rotazione";
        this._runButton.UseVisualStyleBackColor = true;
        this._runButton.Click += new System.EventHandler(this.OnRunClick);
        // 
        // _reportButton
        // 
        this._reportButton.AutoSize = true;
        this._reportButton.Enabled = false;
        this._reportButton.Name = "_reportButton";
        this._reportButton.Size = new System.Drawing.Size(100, 25);
        this._reportButton.TabIndex = 4;
        this._reportButton.Text = "Apri report";
        this._reportButton.UseVisualStyleBackColor = true;
        this._reportButton.Click += new System.EventHandler(this.OnReportClick);
        // 
        // _reloadButton
        // 
        this._reloadButton.AutoSize = true;
        this._reloadButton.Name = "_reloadButton";
        this._reloadButton.Size = new System.Drawing.Size(90, 25);
        this._reloadButton.TabIndex = 5;
        this._reloadButton.Text = "Aggiorna";
        this._reloadButton.UseVisualStyleBackColor = true;
        this._reloadButton.Click += new System.EventHandler(this.OnReloadClick);
        // 
        // _tabs
        // 
        this._tabs.Controls.Add(this._runTab);
        this._tabs.Controls.Add(this._filtersTab);
        this._tabs.Controls.Add(this._sizingTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 43);
        this._tabs.Name = "_tabs";
        this._tabs.Padding = new System.Drawing.Point(12, 4);
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(900, 557);
        this._tabs.TabIndex = 1;
        // 
        // _runTab
        // 
        this._runTab.Controls.Add(this._resultsTextBox);
        this._runTab.Controls.Add(this._contextLayout);
        this._runTab.Location = new System.Drawing.Point(4, 27);
        this._runTab.Name = "_runTab";
        this._runTab.Padding = new System.Windows.Forms.Padding(12);
        this._runTab.Size = new System.Drawing.Size(892, 526);
        this._runTab.TabIndex = 0;
        this._runTab.Text = "Esecuzione";
        this._runTab.UseVisualStyleBackColor = true;
        // 
        // _resultsTextBox
        // 
        this._resultsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._resultsTextBox.Font = new System.Drawing.Font("Consolas", 9F);
        this._resultsTextBox.Location = new System.Drawing.Point(12, 186);
        this._resultsTextBox.Multiline = true;
        this._resultsTextBox.Name = "_resultsTextBox";
        this._resultsTextBox.ReadOnly = true;
        this._resultsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._resultsTextBox.Size = new System.Drawing.Size(868, 328);
        this._resultsTextBox.TabIndex = 1;
        this._resultsTextBox.WordWrap = false;
        // 
        // _contextLayout
        // 
        this._contextLayout.AutoSize = true;
        this._contextLayout.ColumnCount = 4;
        this._contextLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._contextLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._contextLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._contextLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._contextLayout.Controls.Add(this._workspaceLabel, 0, 0);
        this._contextLayout.Controls.Add(this._workspaceCombo, 1, 0);
        this._contextLayout.Controls.Add(this._backtestLabel, 2, 0);
        this._contextLayout.Controls.Add(this._backtestCombo, 3, 0);
        this._contextLayout.Controls.Add(this._startLabel, 0, 1);
        this._contextLayout.Controls.Add(this._startPicker, 1, 1);
        this._contextLayout.Controls.Add(this._endLabel, 2, 1);
        this._contextLayout.Controls.Add(this._endPicker, 3, 1);
        this._contextLayout.Controls.Add(this._initialCapitalLabel, 0, 2);
        this._contextLayout.Controls.Add(this._initialCapitalInput, 1, 2);
        this._contextLayout.Controls.Add(this._periodLabel, 2, 2);
        this._contextLayout.Controls.Add(this._periodCombo, 3, 2);
        this._contextLayout.Controls.Add(this._setupNameLabel, 0, 3);
        this._contextLayout.Controls.Add(this._setupNameTextBox, 1, 3);
        this._contextLayout.Controls.Add(this._setupDescriptionLabel, 2, 3);
        this._contextLayout.Controls.Add(this._setupDescriptionTextBox, 3, 3);
        this._contextLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._contextLayout.Location = new System.Drawing.Point(12, 12);
        this._contextLayout.Name = "_contextLayout";
        this._contextLayout.RowCount = 4;
        this._contextLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._contextLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._contextLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._contextLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._contextLayout.Size = new System.Drawing.Size(868, 174);
        this._contextLayout.TabIndex = 0;
        // 
        // _workspaceLabel
        // 
        this._workspaceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._workspaceLabel.AutoSize = true;
        this._workspaceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._workspaceLabel.Name = "_workspaceLabel";
        this._workspaceLabel.Size = new System.Drawing.Size(70, 15);
        this._workspaceLabel.TabIndex = 0;
        this._workspaceLabel.Text = "Workspace";
        // 
        // _workspaceCombo
        // 
        this._workspaceCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._workspaceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._workspaceCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._workspaceCombo.Name = "_workspaceCombo";
        this._workspaceCombo.Size = new System.Drawing.Size(280, 23);
        this._workspaceCombo.TabIndex = 1;
        this._workspaceCombo.SelectedIndexChanged += new System.EventHandler(this.OnWorkspaceChanged);
        // 
        // _backtestLabel
        // 
        this._backtestLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._backtestLabel.AutoSize = true;
        this._backtestLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._backtestLabel.Name = "_backtestLabel";
        this._backtestLabel.Size = new System.Drawing.Size(120, 15);
        this._backtestLabel.TabIndex = 2;
        this._backtestLabel.Text = "Backtest sorgente";
        // 
        // _backtestCombo
        // 
        this._backtestCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._backtestCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._backtestCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._backtestCombo.Name = "_backtestCombo";
        this._backtestCombo.Size = new System.Drawing.Size(280, 23);
        this._backtestCombo.TabIndex = 3;
        // 
        // _startLabel
        // 
        this._startLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._startLabel.AutoSize = true;
        this._startLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._startLabel.Name = "_startLabel";
        this._startLabel.Size = new System.Drawing.Size(80, 15);
        this._startLabel.TabIndex = 4;
        this._startLabel.Text = "Inizio (UTC)";
        // 
        // _startPicker
        // 
        this._startPicker.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._startPicker.CustomFormat = "yyyy-MM-dd";
        this._startPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this._startPicker.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._startPicker.Name = "_startPicker";
        this._startPicker.Size = new System.Drawing.Size(160, 23);
        this._startPicker.TabIndex = 5;
        // 
        // _endLabel
        // 
        this._endLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._endLabel.AutoSize = true;
        this._endLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._endLabel.Name = "_endLabel";
        this._endLabel.Size = new System.Drawing.Size(80, 15);
        this._endLabel.TabIndex = 6;
        this._endLabel.Text = "Fine (UTC)";
        // 
        // _endPicker
        // 
        this._endPicker.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._endPicker.CustomFormat = "yyyy-MM-dd";
        this._endPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this._endPicker.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._endPicker.Name = "_endPicker";
        this._endPicker.Size = new System.Drawing.Size(160, 23);
        this._endPicker.TabIndex = 7;
        // 
        // _initialCapitalLabel
        // 
        this._initialCapitalLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._initialCapitalLabel.AutoSize = true;
        this._initialCapitalLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._initialCapitalLabel.Name = "_initialCapitalLabel";
        this._initialCapitalLabel.Size = new System.Drawing.Size(110, 15);
        this._initialCapitalLabel.TabIndex = 8;
        this._initialCapitalLabel.Text = "Capitale iniziale";
        // 
        // _initialCapitalInput
        // 
        this._initialCapitalInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._initialCapitalInput.DecimalPlaces = 2;
        this._initialCapitalInput.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
        this._initialCapitalInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._initialCapitalInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
        this._initialCapitalInput.Name = "_initialCapitalInput";
        this._initialCapitalInput.Size = new System.Drawing.Size(160, 23);
        this._initialCapitalInput.TabIndex = 9;
        this._initialCapitalInput.ThousandsSeparator = true;
        this._initialCapitalInput.Value = new decimal(new int[] { 100000, 0, 0, 0 });
        // 
        // _periodLabel
        // 
        this._periodLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._periodLabel.AutoSize = true;
        this._periodLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._periodLabel.Name = "_periodLabel";
        this._periodLabel.Size = new System.Drawing.Size(140, 15);
        this._periodLabel.TabIndex = 10;
        this._periodLabel.Text = "Periodo di rotazione";
        // 
        // _periodCombo
        // 
        this._periodCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._periodCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._periodCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._periodCombo.Name = "_periodCombo";
        this._periodCombo.Size = new System.Drawing.Size(160, 23);
        this._periodCombo.TabIndex = 11;
        // 
        // _setupNameLabel
        // 
        this._setupNameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._setupNameLabel.AutoSize = true;
        this._setupNameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._setupNameLabel.Name = "_setupNameLabel";
        this._setupNameLabel.Size = new System.Drawing.Size(90, 15);
        this._setupNameLabel.TabIndex = 12;
        this._setupNameLabel.Text = "Nome setup";
        // 
        // _setupNameTextBox
        // 
        this._setupNameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._setupNameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._setupNameTextBox.Name = "_setupNameTextBox";
        this._setupNameTextBox.Size = new System.Drawing.Size(280, 23);
        this._setupNameTextBox.TabIndex = 13;
        // 
        // _setupDescriptionLabel
        // 
        this._setupDescriptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._setupDescriptionLabel.AutoSize = true;
        this._setupDescriptionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._setupDescriptionLabel.Name = "_setupDescriptionLabel";
        this._setupDescriptionLabel.Size = new System.Drawing.Size(80, 15);
        this._setupDescriptionLabel.TabIndex = 14;
        this._setupDescriptionLabel.Text = "Descrizione";
        // 
        // _setupDescriptionTextBox
        // 
        this._setupDescriptionTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._setupDescriptionTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._setupDescriptionTextBox.Name = "_setupDescriptionTextBox";
        this._setupDescriptionTextBox.Size = new System.Drawing.Size(280, 23);
        this._setupDescriptionTextBox.TabIndex = 15;
        // 
        // _filtersTab
        // 
        this._filtersTab.AutoScroll = true;
        this._filtersTab.Controls.Add(this._filtersLayout);
        this._filtersTab.Location = new System.Drawing.Point(4, 27);
        this._filtersTab.Name = "_filtersTab";
        this._filtersTab.Padding = new System.Windows.Forms.Padding(12);
        this._filtersTab.Size = new System.Drawing.Size(892, 526);
        this._filtersTab.TabIndex = 1;
        this._filtersTab.Text = "Filtri";
        this._filtersTab.UseVisualStyleBackColor = true;
        // 
        // _filtersLayout
        // 
        this._filtersLayout.AutoSize = true;
        this._filtersLayout.ColumnCount = 4;
        this._filtersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._filtersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._filtersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._filtersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._filtersLayout.Controls.Add(this._minimumTradesLabel, 0, 0);
        this._filtersLayout.Controls.Add(this._minimumTradesInput, 1, 0);
        this._filtersLayout.Controls.Add(this._minimumPassingFiltersLabel, 2, 0);
        this._filtersLayout.Controls.Add(this._minimumPassingFiltersInput, 3, 0);
        this._filtersLayout.Controls.Add(this._shortWindowLabel, 0, 1);
        this._filtersLayout.Controls.Add(this._shortWindowInput, 1, 1);
        this._filtersLayout.Controls.Add(this._longWindowLabel, 2, 1);
        this._filtersLayout.Controls.Add(this._longWindowInput, 3, 1);
        this._filtersLayout.Controls.Add(this._movingAverageWindowLabel, 0, 2);
        this._filtersLayout.Controls.Add(this._movingAverageWindowInput, 1, 2);
        this._filtersLayout.Controls.Add(this._requireEquityAboveMaCheckBox, 3, 2);
        this._filtersLayout.Controls.Add(this._minimumShortReturnLabel, 0, 3);
        this._filtersLayout.Controls.Add(this._minimumShortReturnInput, 1, 3);
        this._filtersLayout.Controls.Add(this._minimumLongReturnLabel, 2, 3);
        this._filtersLayout.Controls.Add(this._minimumLongReturnInput, 3, 3);
        this._filtersLayout.Controls.Add(this._minimumZScoreLabel, 0, 4);
        this._filtersLayout.Controls.Add(this._minimumZScoreInput, 1, 4);
        this._filtersLayout.Controls.Add(this._maximumZScoreLabel, 2, 4);
        this._filtersLayout.Controls.Add(this._maximumZScoreInput, 3, 4);
        this._filtersLayout.Controls.Add(this._maximumCurrentDrawdownLabel, 0, 5);
        this._filtersLayout.Controls.Add(this._maximumCurrentDrawdownInput, 1, 5);
        this._filtersLayout.Controls.Add(this._maximumObservedDrawdownLabel, 2, 5);
        this._filtersLayout.Controls.Add(this._maximumObservedDrawdownInput, 3, 5);
        this._filtersLayout.Controls.Add(this._maximumVolatilityLabel, 0, 6);
        this._filtersLayout.Controls.Add(this._maximumVolatilityInput, 1, 6);
        this._filtersLayout.Controls.Add(this._reenableDrawdownLabel, 2, 6);
        this._filtersLayout.Controls.Add(this._reenableDrawdownInput, 3, 6);
        this._filtersLayout.Controls.Add(this._hardStopLabel, 0, 7);
        this._filtersLayout.Controls.Add(this._hardStopInput, 1, 7);
        this._filtersLayout.Controls.Add(this._cooldownLabel, 2, 7);
        this._filtersLayout.Controls.Add(this._cooldownInput, 3, 7);
        this._filtersLayout.Controls.Add(this._minimumOnPeriodsLabel, 0, 8);
        this._filtersLayout.Controls.Add(this._minimumOnPeriodsInput, 1, 8);
        this._filtersLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._filtersLayout.Location = new System.Drawing.Point(12, 12);
        this._filtersLayout.Name = "_filtersLayout";
        this._filtersLayout.RowCount = 9;
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filtersLayout.Size = new System.Drawing.Size(868, 300);
        this._filtersLayout.TabIndex = 0;
        // 
        // _minimumTradesLabel
        // 
        this._minimumTradesLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumTradesLabel.AutoSize = true;
        this._minimumTradesLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumTradesLabel.Name = "_minimumTradesLabel";
        this._minimumTradesLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumTradesLabel.TabIndex = 0;
        this._minimumTradesLabel.Text = "Trade minimi nel periodo";
        // 
        // _minimumTradesInput
        // 
        this._minimumTradesInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumTradesInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._minimumTradesInput.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        this._minimumTradesInput.Name = "_minimumTradesInput";
        this._minimumTradesInput.Size = new System.Drawing.Size(120, 23);
        this._minimumTradesInput.TabIndex = 1;
        this._minimumTradesInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _minimumPassingFiltersLabel
        // 
        this._minimumPassingFiltersLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumPassingFiltersLabel.AutoSize = true;
        this._minimumPassingFiltersLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumPassingFiltersLabel.Name = "_minimumPassingFiltersLabel";
        this._minimumPassingFiltersLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumPassingFiltersLabel.TabIndex = 2;
        this._minimumPassingFiltersLabel.Text = "Voti minimi da superare";
        // 
        // _minimumPassingFiltersInput
        // 
        this._minimumPassingFiltersInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumPassingFiltersInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._minimumPassingFiltersInput.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        this._minimumPassingFiltersInput.Name = "_minimumPassingFiltersInput";
        this._minimumPassingFiltersInput.Size = new System.Drawing.Size(120, 23);
        this._minimumPassingFiltersInput.TabIndex = 3;
        this._minimumPassingFiltersInput.Value = new decimal(new int[] { 4, 0, 0, 0 });
        // 
        // _shortWindowLabel
        // 
        this._shortWindowLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._shortWindowLabel.AutoSize = true;
        this._shortWindowLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._shortWindowLabel.Name = "_shortWindowLabel";
        this._shortWindowLabel.Size = new System.Drawing.Size(200, 15);
        this._shortWindowLabel.TabIndex = 4;
        this._shortWindowLabel.Text = "Finestra breve (giorni)";
        // 
        // _shortWindowInput
        // 
        this._shortWindowInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._shortWindowInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._shortWindowInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this._shortWindowInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._shortWindowInput.Name = "_shortWindowInput";
        this._shortWindowInput.Size = new System.Drawing.Size(120, 23);
        this._shortWindowInput.TabIndex = 5;
        this._shortWindowInput.Value = new decimal(new int[] { 90, 0, 0, 0 });
        // 
        // _longWindowLabel
        // 
        this._longWindowLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._longWindowLabel.AutoSize = true;
        this._longWindowLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._longWindowLabel.Name = "_longWindowLabel";
        this._longWindowLabel.Size = new System.Drawing.Size(200, 15);
        this._longWindowLabel.TabIndex = 6;
        this._longWindowLabel.Text = "Finestra lunga (giorni)";
        // 
        // _longWindowInput
        // 
        this._longWindowInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._longWindowInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._longWindowInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this._longWindowInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._longWindowInput.Name = "_longWindowInput";
        this._longWindowInput.Size = new System.Drawing.Size(120, 23);
        this._longWindowInput.TabIndex = 7;
        this._longWindowInput.Value = new decimal(new int[] { 365, 0, 0, 0 });
        // 
        // _movingAverageWindowLabel
        // 
        this._movingAverageWindowLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._movingAverageWindowLabel.AutoSize = true;
        this._movingAverageWindowLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._movingAverageWindowLabel.Name = "_movingAverageWindowLabel";
        this._movingAverageWindowLabel.Size = new System.Drawing.Size(200, 15);
        this._movingAverageWindowLabel.TabIndex = 8;
        this._movingAverageWindowLabel.Text = "Finestra media mobile (giorni)";
        // 
        // _movingAverageWindowInput
        // 
        this._movingAverageWindowInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._movingAverageWindowInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._movingAverageWindowInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this._movingAverageWindowInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._movingAverageWindowInput.Name = "_movingAverageWindowInput";
        this._movingAverageWindowInput.Size = new System.Drawing.Size(120, 23);
        this._movingAverageWindowInput.TabIndex = 9;
        this._movingAverageWindowInput.Value = new decimal(new int[] { 90, 0, 0, 0 });
        // 
        // _requireEquityAboveMaCheckBox
        // 
        this._requireEquityAboveMaCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._requireEquityAboveMaCheckBox.AutoSize = true;
        this._requireEquityAboveMaCheckBox.Checked = true;
        this._requireEquityAboveMaCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this._requireEquityAboveMaCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        this._requireEquityAboveMaCheckBox.Name = "_requireEquityAboveMaCheckBox";
        this._requireEquityAboveMaCheckBox.Size = new System.Drawing.Size(300, 19);
        this._requireEquityAboveMaCheckBox.TabIndex = 10;
        this._requireEquityAboveMaCheckBox.Text = "Richiedi equity sopra la media mobile";
        this._requireEquityAboveMaCheckBox.UseVisualStyleBackColor = true;
        // 
        // _minimumShortReturnLabel
        // 
        this._minimumShortReturnLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumShortReturnLabel.AutoSize = true;
        this._minimumShortReturnLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumShortReturnLabel.Name = "_minimumShortReturnLabel";
        this._minimumShortReturnLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumShortReturnLabel.TabIndex = 11;
        this._minimumShortReturnLabel.Text = "Rendimento breve minimo (0-1)";
        // 
        // _minimumShortReturnInput
        // 
        this._minimumShortReturnInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumShortReturnInput.DecimalPlaces = 4;
        this._minimumShortReturnInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._minimumShortReturnInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._minimumShortReturnInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._minimumShortReturnInput.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
        this._minimumShortReturnInput.Name = "_minimumShortReturnInput";
        this._minimumShortReturnInput.Size = new System.Drawing.Size(120, 23);
        this._minimumShortReturnInput.TabIndex = 12;
        // 
        // _minimumLongReturnLabel
        // 
        this._minimumLongReturnLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumLongReturnLabel.AutoSize = true;
        this._minimumLongReturnLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumLongReturnLabel.Name = "_minimumLongReturnLabel";
        this._minimumLongReturnLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumLongReturnLabel.TabIndex = 13;
        this._minimumLongReturnLabel.Text = "Rendimento lungo minimo (0-1)";
        // 
        // _minimumLongReturnInput
        // 
        this._minimumLongReturnInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumLongReturnInput.DecimalPlaces = 4;
        this._minimumLongReturnInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._minimumLongReturnInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._minimumLongReturnInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._minimumLongReturnInput.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
        this._minimumLongReturnInput.Name = "_minimumLongReturnInput";
        this._minimumLongReturnInput.Size = new System.Drawing.Size(120, 23);
        this._minimumLongReturnInput.TabIndex = 14;
        // 
        // _minimumZScoreLabel
        // 
        this._minimumZScoreLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumZScoreLabel.AutoSize = true;
        this._minimumZScoreLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumZScoreLabel.Name = "_minimumZScoreLabel";
        this._minimumZScoreLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumZScoreLabel.TabIndex = 15;
        this._minimumZScoreLabel.Text = "Z-score minimo";
        // 
        // _minimumZScoreInput
        // 
        this._minimumZScoreInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumZScoreInput.DecimalPlaces = 2;
        this._minimumZScoreInput.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        this._minimumZScoreInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._minimumZScoreInput.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._minimumZScoreInput.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
        this._minimumZScoreInput.Name = "_minimumZScoreInput";
        this._minimumZScoreInput.Size = new System.Drawing.Size(120, 23);
        this._minimumZScoreInput.TabIndex = 16;
        this._minimumZScoreInput.Value = new decimal(new int[] { 15, 0, 0, -2147418112 });
        // 
        // _maximumZScoreLabel
        // 
        this._maximumZScoreLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumZScoreLabel.AutoSize = true;
        this._maximumZScoreLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumZScoreLabel.Name = "_maximumZScoreLabel";
        this._maximumZScoreLabel.Size = new System.Drawing.Size(200, 15);
        this._maximumZScoreLabel.TabIndex = 17;
        this._maximumZScoreLabel.Text = "Z-score massimo";
        // 
        // _maximumZScoreInput
        // 
        this._maximumZScoreInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumZScoreInput.DecimalPlaces = 2;
        this._maximumZScoreInput.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        this._maximumZScoreInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maximumZScoreInput.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._maximumZScoreInput.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
        this._maximumZScoreInput.Name = "_maximumZScoreInput";
        this._maximumZScoreInput.Size = new System.Drawing.Size(120, 23);
        this._maximumZScoreInput.TabIndex = 18;
        this._maximumZScoreInput.Value = new decimal(new int[] { 25, 0, 0, 65536 });
        // 
        // _maximumCurrentDrawdownLabel
        // 
        this._maximumCurrentDrawdownLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumCurrentDrawdownLabel.AutoSize = true;
        this._maximumCurrentDrawdownLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumCurrentDrawdownLabel.Name = "_maximumCurrentDrawdownLabel";
        this._maximumCurrentDrawdownLabel.Size = new System.Drawing.Size(200, 15);
        this._maximumCurrentDrawdownLabel.TabIndex = 19;
        this._maximumCurrentDrawdownLabel.Text = "Drawdown corrente massimo (0-1)";
        // 
        // _maximumCurrentDrawdownInput
        // 
        this._maximumCurrentDrawdownInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumCurrentDrawdownInput.DecimalPlaces = 4;
        this._maximumCurrentDrawdownInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._maximumCurrentDrawdownInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._maximumCurrentDrawdownInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._maximumCurrentDrawdownInput.Name = "_maximumCurrentDrawdownInput";
        this._maximumCurrentDrawdownInput.Size = new System.Drawing.Size(120, 23);
        this._maximumCurrentDrawdownInput.TabIndex = 20;
        this._maximumCurrentDrawdownInput.Value = new decimal(new int[] { 15, 0, 0, 131072 });
        // 
        // _maximumObservedDrawdownLabel
        // 
        this._maximumObservedDrawdownLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumObservedDrawdownLabel.AutoSize = true;
        this._maximumObservedDrawdownLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumObservedDrawdownLabel.Name = "_maximumObservedDrawdownLabel";
        this._maximumObservedDrawdownLabel.Size = new System.Drawing.Size(200, 15);
        this._maximumObservedDrawdownLabel.TabIndex = 21;
        this._maximumObservedDrawdownLabel.Text = "Drawdown storico massimo (0-1)";
        // 
        // _maximumObservedDrawdownInput
        // 
        this._maximumObservedDrawdownInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumObservedDrawdownInput.DecimalPlaces = 4;
        this._maximumObservedDrawdownInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._maximumObservedDrawdownInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maximumObservedDrawdownInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._maximumObservedDrawdownInput.Name = "_maximumObservedDrawdownInput";
        this._maximumObservedDrawdownInput.Size = new System.Drawing.Size(120, 23);
        this._maximumObservedDrawdownInput.TabIndex = 22;
        this._maximumObservedDrawdownInput.Value = new decimal(new int[] { 25, 0, 0, 131072 });
        // 
        // _maximumVolatilityLabel
        // 
        this._maximumVolatilityLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumVolatilityLabel.AutoSize = true;
        this._maximumVolatilityLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumVolatilityLabel.Name = "_maximumVolatilityLabel";
        this._maximumVolatilityLabel.Size = new System.Drawing.Size(200, 15);
        this._maximumVolatilityLabel.TabIndex = 23;
        this._maximumVolatilityLabel.Text = "Volatilità dei rendimenti massima";
        // 
        // _maximumVolatilityInput
        // 
        this._maximumVolatilityInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumVolatilityInput.DecimalPlaces = 4;
        this._maximumVolatilityInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._maximumVolatilityInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._maximumVolatilityInput.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._maximumVolatilityInput.Name = "_maximumVolatilityInput";
        this._maximumVolatilityInput.Size = new System.Drawing.Size(120, 23);
        this._maximumVolatilityInput.TabIndex = 24;
        this._maximumVolatilityInput.Value = new decimal(new int[] { 10, 0, 0, 131072 });
        // 
        // _reenableDrawdownLabel
        // 
        this._reenableDrawdownLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._reenableDrawdownLabel.AutoSize = true;
        this._reenableDrawdownLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._reenableDrawdownLabel.Name = "_reenableDrawdownLabel";
        this._reenableDrawdownLabel.Size = new System.Drawing.Size(200, 15);
        this._reenableDrawdownLabel.TabIndex = 25;
        this._reenableDrawdownLabel.Text = "Drawdown per riaccensione (0-1)";
        // 
        // _reenableDrawdownInput
        // 
        this._reenableDrawdownInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._reenableDrawdownInput.DecimalPlaces = 4;
        this._reenableDrawdownInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._reenableDrawdownInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._reenableDrawdownInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._reenableDrawdownInput.Name = "_reenableDrawdownInput";
        this._reenableDrawdownInput.Size = new System.Drawing.Size(120, 23);
        this._reenableDrawdownInput.TabIndex = 26;
        this._reenableDrawdownInput.Value = new decimal(new int[] { 10, 0, 0, 131072 });
        // 
        // _hardStopLabel
        // 
        this._hardStopLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._hardStopLabel.AutoSize = true;
        this._hardStopLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._hardStopLabel.Name = "_hardStopLabel";
        this._hardStopLabel.Size = new System.Drawing.Size(200, 15);
        this._hardStopLabel.TabIndex = 27;
        this._hardStopLabel.Text = "Drawdown di hard stop (0-1)";
        // 
        // _hardStopInput
        // 
        this._hardStopInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._hardStopInput.DecimalPlaces = 4;
        this._hardStopInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._hardStopInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._hardStopInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._hardStopInput.Name = "_hardStopInput";
        this._hardStopInput.Size = new System.Drawing.Size(120, 23);
        this._hardStopInput.TabIndex = 28;
        this._hardStopInput.Value = new decimal(new int[] { 35, 0, 0, 131072 });
        // 
        // _cooldownLabel
        // 
        this._cooldownLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cooldownLabel.AutoSize = true;
        this._cooldownLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._cooldownLabel.Name = "_cooldownLabel";
        this._cooldownLabel.Size = new System.Drawing.Size(200, 15);
        this._cooldownLabel.TabIndex = 29;
        this._cooldownLabel.Text = "Periodi di cooldown dopo lo spegnimento";
        // 
        // _cooldownInput
        // 
        this._cooldownInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cooldownInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._cooldownInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._cooldownInput.Name = "_cooldownInput";
        this._cooldownInput.Size = new System.Drawing.Size(120, 23);
        this._cooldownInput.TabIndex = 30;
        this._cooldownInput.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // _minimumOnPeriodsLabel
        // 
        this._minimumOnPeriodsLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumOnPeriodsLabel.AutoSize = true;
        this._minimumOnPeriodsLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumOnPeriodsLabel.Name = "_minimumOnPeriodsLabel";
        this._minimumOnPeriodsLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumOnPeriodsLabel.TabIndex = 31;
        this._minimumOnPeriodsLabel.Text = "Periodi minimi di accensione";
        // 
        // _minimumOnPeriodsInput
        // 
        this._minimumOnPeriodsInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumOnPeriodsInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._minimumOnPeriodsInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._minimumOnPeriodsInput.Name = "_minimumOnPeriodsInput";
        this._minimumOnPeriodsInput.Size = new System.Drawing.Size(120, 23);
        this._minimumOnPeriodsInput.TabIndex = 32;
        this._minimumOnPeriodsInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _sizingTab
        // 
        this._sizingTab.AutoScroll = true;
        this._sizingTab.Controls.Add(this._sizingLayout);
        this._sizingTab.Location = new System.Drawing.Point(4, 27);
        this._sizingTab.Name = "_sizingTab";
        this._sizingTab.Padding = new System.Windows.Forms.Padding(12);
        this._sizingTab.Size = new System.Drawing.Size(892, 526);
        this._sizingTab.TabIndex = 2;
        this._sizingTab.Text = "Sizing e walk-forward";
        this._sizingTab.UseVisualStyleBackColor = true;
        // 
        // _sizingLayout
        // 
        this._sizingLayout.AutoSize = true;
        this._sizingLayout.ColumnCount = 4;
        this._sizingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._sizingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._sizingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._sizingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._sizingLayout.Controls.Add(this._crossSectionalCheckBox, 0, 0);
        this._sizingLayout.Controls.Add(this._crossSectionalHintLabel, 0, 1);
        this._sizingLayout.Controls.Add(this._minimumAllocationLabel, 0, 2);
        this._sizingLayout.Controls.Add(this._minimumAllocationInput, 1, 2);
        this._sizingLayout.Controls.Add(this._maximumAllocationLabel, 2, 2);
        this._sizingLayout.Controls.Add(this._maximumAllocationInput, 3, 2);
        this._sizingLayout.Controls.Add(this._allocationStepLabel, 0, 3);
        this._sizingLayout.Controls.Add(this._allocationStepInput, 1, 3);
        this._sizingLayout.Controls.Add(this._sizingTiersLabel, 0, 4);
        this._sizingLayout.Controls.Add(this._sizingTiersTextBox, 1, 4);
        this._sizingLayout.Controls.Add(this._disableScoreLabel, 0, 5);
        this._sizingLayout.Controls.Add(this._disableScoreInput, 1, 5);
        this._sizingLayout.Controls.Add(this._reenableScoreLabel, 2, 5);
        this._sizingLayout.Controls.Add(this._reenableScoreInput, 3, 5);
        this._sizingLayout.Controls.Add(this._commissionLabel, 0, 6);
        this._sizingLayout.Controls.Add(this._commissionInput, 1, 6);
        this._sizingLayout.Controls.Add(this._slippageLabel, 2, 6);
        this._sizingLayout.Controls.Add(this._slippageInput, 3, 6);
        this._sizingLayout.Controls.Add(this._calibrationPeriodsLabel, 0, 7);
        this._sizingLayout.Controls.Add(this._calibrationPeriodsInput, 1, 7);
        this._sizingLayout.Controls.Add(this._evaluationPeriodsLabel, 2, 7);
        this._sizingLayout.Controls.Add(this._evaluationPeriodsInput, 3, 7);
        this._sizingLayout.Controls.Add(this._walkForwardLabel, 0, 8);
        this._sizingLayout.Controls.Add(this._walkForwardCombo, 1, 8);
        this._sizingLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._sizingLayout.Location = new System.Drawing.Point(12, 12);
        this._sizingLayout.Name = "_sizingLayout";
        this._sizingLayout.RowCount = 9;
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._sizingLayout.SetColumnSpan(this._crossSectionalCheckBox, 4);
        this._sizingLayout.SetColumnSpan(this._crossSectionalHintLabel, 4);
        this._sizingLayout.SetColumnSpan(this._sizingTiersTextBox, 3);
        this._sizingLayout.Size = new System.Drawing.Size(868, 320);
        this._sizingLayout.TabIndex = 0;
        // 
        // _crossSectionalCheckBox
        // 
        this._crossSectionalCheckBox.AutoSize = true;
        this._crossSectionalCheckBox.Checked = true;
        this._crossSectionalCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this._crossSectionalCheckBox.Margin = new System.Windows.Forms.Padding(3, 8, 3, 2);
        this._crossSectionalCheckBox.Name = "_crossSectionalCheckBox";
        this._crossSectionalCheckBox.Size = new System.Drawing.Size(400, 19);
        this._crossSectionalCheckBox.TabIndex = 0;
        this._crossSectionalCheckBox.Text = "Sizing per percentile (cross-sectional)";
        this._crossSectionalCheckBox.UseVisualStyleBackColor = true;
        this._crossSectionalCheckBox.CheckedChanged += new System.EventHandler(this.OnCrossSectionalChanged);
        // 
        // _crossSectionalHintLabel
        // 
        this._crossSectionalHintLabel.AutoSize = true;
        this._crossSectionalHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._crossSectionalHintLabel.Margin = new System.Windows.Forms.Padding(22, 0, 3, 10);
        this._crossSectionalHintLabel.Name = "_crossSectionalHintLabel";
        this._crossSectionalHintLabel.Size = new System.Drawing.Size(700, 15);
        this._crossSectionalHintLabel.TabIndex = 1;
        this._crossSectionalHintLabel.Text = "Decide solo quanto allocare a una strategia già eleggibile: accensione e spegnimento restano ai cancelli assoluti dei filtri.";
        // 
        // _minimumAllocationLabel
        // 
        this._minimumAllocationLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumAllocationLabel.AutoSize = true;
        this._minimumAllocationLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._minimumAllocationLabel.Name = "_minimumAllocationLabel";
        this._minimumAllocationLabel.Size = new System.Drawing.Size(200, 15);
        this._minimumAllocationLabel.TabIndex = 2;
        this._minimumAllocationLabel.Text = "Allocazione minima (peggiore)";
        // 
        // _minimumAllocationInput
        // 
        this._minimumAllocationInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._minimumAllocationInput.DecimalPlaces = 4;
        this._minimumAllocationInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._minimumAllocationInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._minimumAllocationInput.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._minimumAllocationInput.Name = "_minimumAllocationInput";
        this._minimumAllocationInput.Size = new System.Drawing.Size(120, 23);
        this._minimumAllocationInput.TabIndex = 3;
        this._minimumAllocationInput.Value = new decimal(new int[] { 25, 0, 0, 131072 });
        // 
        // _maximumAllocationLabel
        // 
        this._maximumAllocationLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumAllocationLabel.AutoSize = true;
        this._maximumAllocationLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumAllocationLabel.Name = "_maximumAllocationLabel";
        this._maximumAllocationLabel.Size = new System.Drawing.Size(200, 15);
        this._maximumAllocationLabel.TabIndex = 4;
        this._maximumAllocationLabel.Text = "Allocazione massima (migliore)";
        // 
        // _maximumAllocationInput
        // 
        this._maximumAllocationInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumAllocationInput.DecimalPlaces = 4;
        this._maximumAllocationInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._maximumAllocationInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maximumAllocationInput.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._maximumAllocationInput.Name = "_maximumAllocationInput";
        this._maximumAllocationInput.Size = new System.Drawing.Size(120, 23);
        this._maximumAllocationInput.TabIndex = 5;
        this._maximumAllocationInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _allocationStepLabel
        // 
        this._allocationStepLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._allocationStepLabel.AutoSize = true;
        this._allocationStepLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._allocationStepLabel.Name = "_allocationStepLabel";
        this._allocationStepLabel.Size = new System.Drawing.Size(200, 15);
        this._allocationStepLabel.TabIndex = 6;
        this._allocationStepLabel.Text = "Passo di allocazione (0 = nessuno)";
        // 
        // _allocationStepInput
        // 
        this._allocationStepInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._allocationStepInput.DecimalPlaces = 4;
        this._allocationStepInput.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        this._allocationStepInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._allocationStepInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._allocationStepInput.Name = "_allocationStepInput";
        this._allocationStepInput.Size = new System.Drawing.Size(120, 23);
        this._allocationStepInput.TabIndex = 7;
        this._allocationStepInput.Value = new decimal(new int[] { 5, 0, 0, 131072 });
        // 
        // _sizingTiersLabel
        // 
        this._sizingTiersLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sizingTiersLabel.AutoSize = true;
        this._sizingTiersLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._sizingTiersLabel.Name = "_sizingTiersLabel";
        this._sizingTiersLabel.Size = new System.Drawing.Size(200, 15);
        this._sizingTiersLabel.TabIndex = 8;
        this._sizingTiersLabel.Text = "Scaglioni (score=moltiplicatore)";
        // 
        // _sizingTiersTextBox
        // 
        this._sizingTiersTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._sizingTiersTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._sizingTiersTextBox.Name = "_sizingTiersTextBox";
        this._sizingTiersTextBox.Size = new System.Drawing.Size(560, 23);
        this._sizingTiersTextBox.TabIndex = 9;
        // 
        // _disableScoreLabel
        // 
        this._disableScoreLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._disableScoreLabel.AutoSize = true;
        this._disableScoreLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._disableScoreLabel.Name = "_disableScoreLabel";
        this._disableScoreLabel.Size = new System.Drawing.Size(200, 15);
        this._disableScoreLabel.TabIndex = 10;
        this._disableScoreLabel.Text = "Score di spegnimento";
        // 
        // _disableScoreInput
        // 
        this._disableScoreInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._disableScoreInput.DecimalPlaces = 4;
        this._disableScoreInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._disableScoreInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._disableScoreInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._disableScoreInput.Name = "_disableScoreInput";
        this._disableScoreInput.Size = new System.Drawing.Size(120, 23);
        this._disableScoreInput.TabIndex = 11;
        this._disableScoreInput.Value = new decimal(new int[] { 40, 0, 0, 131072 });
        // 
        // _reenableScoreLabel
        // 
        this._reenableScoreLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._reenableScoreLabel.AutoSize = true;
        this._reenableScoreLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._reenableScoreLabel.Name = "_reenableScoreLabel";
        this._reenableScoreLabel.Size = new System.Drawing.Size(200, 15);
        this._reenableScoreLabel.TabIndex = 12;
        this._reenableScoreLabel.Text = "Score di riaccensione";
        // 
        // _reenableScoreInput
        // 
        this._reenableScoreInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._reenableScoreInput.DecimalPlaces = 4;
        this._reenableScoreInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._reenableScoreInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._reenableScoreInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._reenableScoreInput.Name = "_reenableScoreInput";
        this._reenableScoreInput.Size = new System.Drawing.Size(120, 23);
        this._reenableScoreInput.TabIndex = 13;
        this._reenableScoreInput.Value = new decimal(new int[] { 60, 0, 0, 131072 });
        // 
        // _commissionLabel
        // 
        this._commissionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionLabel.AutoSize = true;
        this._commissionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._commissionLabel.Name = "_commissionLabel";
        this._commissionLabel.Size = new System.Drawing.Size(200, 15);
        this._commissionLabel.TabIndex = 14;
        this._commissionLabel.Text = "Commissione per unità";
        // 
        // _commissionInput
        // 
        this._commissionInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionInput.DecimalPlaces = 4;
        this._commissionInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._commissionInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this._commissionInput.Name = "_commissionInput";
        this._commissionInput.Size = new System.Drawing.Size(120, 23);
        this._commissionInput.TabIndex = 15;
        // 
        // _slippageLabel
        // 
        this._slippageLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._slippageLabel.AutoSize = true;
        this._slippageLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._slippageLabel.Name = "_slippageLabel";
        this._slippageLabel.Size = new System.Drawing.Size(200, 15);
        this._slippageLabel.TabIndex = 16;
        this._slippageLabel.Text = "Slippage per unità";
        // 
        // _slippageInput
        // 
        this._slippageInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._slippageInput.DecimalPlaces = 4;
        this._slippageInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._slippageInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this._slippageInput.Name = "_slippageInput";
        this._slippageInput.Size = new System.Drawing.Size(120, 23);
        this._slippageInput.TabIndex = 17;
        // 
        // _calibrationPeriodsLabel
        // 
        this._calibrationPeriodsLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._calibrationPeriodsLabel.AutoSize = true;
        this._calibrationPeriodsLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._calibrationPeriodsLabel.Name = "_calibrationPeriodsLabel";
        this._calibrationPeriodsLabel.Size = new System.Drawing.Size(200, 15);
        this._calibrationPeriodsLabel.TabIndex = 18;
        this._calibrationPeriodsLabel.Text = "Periodi di calibrazione";
        // 
        // _calibrationPeriodsInput
        // 
        this._calibrationPeriodsInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._calibrationPeriodsInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._calibrationPeriodsInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._calibrationPeriodsInput.Name = "_calibrationPeriodsInput";
        this._calibrationPeriodsInput.Size = new System.Drawing.Size(120, 23);
        this._calibrationPeriodsInput.TabIndex = 19;
        this._calibrationPeriodsInput.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // _evaluationPeriodsLabel
        // 
        this._evaluationPeriodsLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._evaluationPeriodsLabel.AutoSize = true;
        this._evaluationPeriodsLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._evaluationPeriodsLabel.Name = "_evaluationPeriodsLabel";
        this._evaluationPeriodsLabel.Size = new System.Drawing.Size(200, 15);
        this._evaluationPeriodsLabel.TabIndex = 20;
        this._evaluationPeriodsLabel.Text = "Periodi di valutazione (OOS)";
        // 
        // _evaluationPeriodsInput
        // 
        this._evaluationPeriodsInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._evaluationPeriodsInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._evaluationPeriodsInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._evaluationPeriodsInput.Name = "_evaluationPeriodsInput";
        this._evaluationPeriodsInput.Size = new System.Drawing.Size(120, 23);
        this._evaluationPeriodsInput.TabIndex = 21;
        this._evaluationPeriodsInput.Value = new decimal(new int[] { 4, 0, 0, 0 });
        // 
        // _walkForwardLabel
        // 
        this._walkForwardLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._walkForwardLabel.AutoSize = true;
        this._walkForwardLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._walkForwardLabel.Name = "_walkForwardLabel";
        this._walkForwardLabel.Size = new System.Drawing.Size(200, 15);
        this._walkForwardLabel.TabIndex = 22;
        this._walkForwardLabel.Text = "Modalità walk-forward";
        // 
        // _walkForwardCombo
        // 
        this._walkForwardCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._walkForwardCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._walkForwardCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._walkForwardCombo.Name = "_walkForwardCombo";
        this._walkForwardCombo.Size = new System.Drawing.Size(160, 23);
        this._walkForwardCombo.TabIndex = 23;
        // 
        // TitanoScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._commandsPanel);
        this.Name = "TitanoScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._commandsPanel.ResumeLayout(false);
        this._commandsPanel.PerformLayout();
        this._tabs.ResumeLayout(false);
        this._runTab.ResumeLayout(false);
        this._runTab.PerformLayout();
        this._contextLayout.ResumeLayout(false);
        this._contextLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialCapitalInput)).EndInit();
        this._filtersTab.ResumeLayout(false);
        this._filtersTab.PerformLayout();
        this._filtersLayout.ResumeLayout(false);
        this._filtersLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._minimumTradesInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumPassingFiltersInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._shortWindowInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._longWindowInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._movingAverageWindowInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumShortReturnInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumLongReturnInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumZScoreInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumZScoreInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumCurrentDrawdownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumObservedDrawdownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumVolatilityInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._reenableDrawdownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._hardStopInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._cooldownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._minimumOnPeriodsInput)).EndInit();
        this._sizingTab.ResumeLayout(false);
        this._sizingTab.PerformLayout();
        this._sizingLayout.ResumeLayout(false);
        this._sizingLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._minimumAllocationInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumAllocationInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._allocationStepInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._disableScoreInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._reenableScoreInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._slippageInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._calibrationPeriodsInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._evaluationPeriodsInput)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.FlowLayoutPanel _commandsPanel;
    private System.Windows.Forms.ComboBox _setupCombo;
    private System.Windows.Forms.Button _loadSetupButton;
    private System.Windows.Forms.Button _saveSetupButton;
    private System.Windows.Forms.Button _runButton;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _reloadButton;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _runTab;
    private System.Windows.Forms.TextBox _resultsTextBox;
    private System.Windows.Forms.TableLayoutPanel _contextLayout;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.ComboBox _workspaceCombo;
    private System.Windows.Forms.Label _backtestLabel;
    private System.Windows.Forms.ComboBox _backtestCombo;
    private System.Windows.Forms.Label _startLabel;
    private System.Windows.Forms.DateTimePicker _startPicker;
    private System.Windows.Forms.Label _endLabel;
    private System.Windows.Forms.DateTimePicker _endPicker;
    private System.Windows.Forms.Label _initialCapitalLabel;
    private System.Windows.Forms.NumericUpDown _initialCapitalInput;
    private System.Windows.Forms.Label _periodLabel;
    private System.Windows.Forms.ComboBox _periodCombo;
    private System.Windows.Forms.Label _setupNameLabel;
    private System.Windows.Forms.TextBox _setupNameTextBox;
    private System.Windows.Forms.Label _setupDescriptionLabel;
    private System.Windows.Forms.TextBox _setupDescriptionTextBox;
    private System.Windows.Forms.TabPage _filtersTab;
    private System.Windows.Forms.TableLayoutPanel _filtersLayout;
    private System.Windows.Forms.Label _minimumTradesLabel;
    private System.Windows.Forms.NumericUpDown _minimumTradesInput;
    private System.Windows.Forms.Label _minimumPassingFiltersLabel;
    private System.Windows.Forms.NumericUpDown _minimumPassingFiltersInput;
    private System.Windows.Forms.Label _shortWindowLabel;
    private System.Windows.Forms.NumericUpDown _shortWindowInput;
    private System.Windows.Forms.Label _longWindowLabel;
    private System.Windows.Forms.NumericUpDown _longWindowInput;
    private System.Windows.Forms.Label _movingAverageWindowLabel;
    private System.Windows.Forms.NumericUpDown _movingAverageWindowInput;
    private System.Windows.Forms.CheckBox _requireEquityAboveMaCheckBox;
    private System.Windows.Forms.Label _minimumShortReturnLabel;
    private System.Windows.Forms.NumericUpDown _minimumShortReturnInput;
    private System.Windows.Forms.Label _minimumLongReturnLabel;
    private System.Windows.Forms.NumericUpDown _minimumLongReturnInput;
    private System.Windows.Forms.Label _minimumZScoreLabel;
    private System.Windows.Forms.NumericUpDown _minimumZScoreInput;
    private System.Windows.Forms.Label _maximumZScoreLabel;
    private System.Windows.Forms.NumericUpDown _maximumZScoreInput;
    private System.Windows.Forms.Label _maximumCurrentDrawdownLabel;
    private System.Windows.Forms.NumericUpDown _maximumCurrentDrawdownInput;
    private System.Windows.Forms.Label _maximumObservedDrawdownLabel;
    private System.Windows.Forms.NumericUpDown _maximumObservedDrawdownInput;
    private System.Windows.Forms.Label _maximumVolatilityLabel;
    private System.Windows.Forms.NumericUpDown _maximumVolatilityInput;
    private System.Windows.Forms.Label _reenableDrawdownLabel;
    private System.Windows.Forms.NumericUpDown _reenableDrawdownInput;
    private System.Windows.Forms.Label _hardStopLabel;
    private System.Windows.Forms.NumericUpDown _hardStopInput;
    private System.Windows.Forms.Label _cooldownLabel;
    private System.Windows.Forms.NumericUpDown _cooldownInput;
    private System.Windows.Forms.Label _minimumOnPeriodsLabel;
    private System.Windows.Forms.NumericUpDown _minimumOnPeriodsInput;
    private System.Windows.Forms.TabPage _sizingTab;
    private System.Windows.Forms.TableLayoutPanel _sizingLayout;
    private System.Windows.Forms.CheckBox _crossSectionalCheckBox;
    private System.Windows.Forms.Label _crossSectionalHintLabel;
    private System.Windows.Forms.Label _minimumAllocationLabel;
    private System.Windows.Forms.NumericUpDown _minimumAllocationInput;
    private System.Windows.Forms.Label _maximumAllocationLabel;
    private System.Windows.Forms.NumericUpDown _maximumAllocationInput;
    private System.Windows.Forms.Label _allocationStepLabel;
    private System.Windows.Forms.NumericUpDown _allocationStepInput;
    private System.Windows.Forms.Label _sizingTiersLabel;
    private System.Windows.Forms.TextBox _sizingTiersTextBox;
    private System.Windows.Forms.Label _disableScoreLabel;
    private System.Windows.Forms.NumericUpDown _disableScoreInput;
    private System.Windows.Forms.Label _reenableScoreLabel;
    private System.Windows.Forms.NumericUpDown _reenableScoreInput;
    private System.Windows.Forms.Label _commissionLabel;
    private System.Windows.Forms.NumericUpDown _commissionInput;
    private System.Windows.Forms.Label _slippageLabel;
    private System.Windows.Forms.NumericUpDown _slippageInput;
    private System.Windows.Forms.Label _calibrationPeriodsLabel;
    private System.Windows.Forms.NumericUpDown _calibrationPeriodsInput;
    private System.Windows.Forms.Label _evaluationPeriodsLabel;
    private System.Windows.Forms.NumericUpDown _evaluationPeriodsInput;
    private System.Windows.Forms.Label _walkForwardLabel;
    private System.Windows.Forms.ComboBox _walkForwardCombo;
}
