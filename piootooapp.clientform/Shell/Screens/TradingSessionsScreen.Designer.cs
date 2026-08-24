namespace piootooapp.clientform.Shell.Screens;

partial class TradingSessionsScreen
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
        components = new System.ComponentModel.Container();
        _screenExplainerLabel = new System.Windows.Forms.Label();
        _configGroup = new System.Windows.Forms.GroupBox();
        _configLayout = new System.Windows.Forms.TableLayoutPanel();
        _creationSourceLabel = new System.Windows.Forms.Label();
        _creationSourceCombo = new System.Windows.Forms.ComboBox();
        _planLabel = new System.Windows.Forms.Label();
        _planCombo = new System.Windows.Forms.ComboBox();
        _workspaceLabel = new System.Windows.Forms.Label();
        _workspaceCombo = new System.Windows.Forms.ComboBox();
        _derivedWorkspaceLabel = new System.Windows.Forms.Label();
        _masterfilterInfoLabel = new System.Windows.Forms.Label();
        _modeLabel = new System.Windows.Forms.Label();
        _modeCombo = new System.Windows.Forms.ComboBox();
        _clientRunModeLabel = new System.Windows.Forms.Label();
        _clientRunModeCombo = new System.Windows.Forms.ComboBox();
        _titanoModeLabel = new System.Windows.Forms.Label();
        _titanoModeCombo = new System.Windows.Forms.ComboBox();
        _titanoBacktestLabel = new System.Windows.Forms.Label();
        _titanoBacktestPanel = new System.Windows.Forms.TableLayoutPanel();
        _titanoBacktestTextBox = new System.Windows.Forms.TextBox();
        _titanoBacktestPickButton = new System.Windows.Forms.Button();
        _titanoRunLabel = new System.Windows.Forms.Label();
        _titanoRunCombo = new System.Windows.Forms.ComboBox();
        _loadRunsButton = new System.Windows.Forms.Button();
        _executionKeyLabel = new System.Windows.Forms.Label();
        _executionKeyTextBox = new System.Windows.Forms.TextBox();
        _openPlanAccountLabel = new System.Windows.Forms.Label();
        _openPlanAccountCombo = new System.Windows.Forms.ComboBox();
        _distributeCheckBox = new System.Windows.Forms.CheckBox();
        _portfolioRiskEnabledCheckBox = new System.Windows.Forms.CheckBox();
        _scenarioHintLabel = new System.Windows.Forms.Label();
        _commandsPanel = new System.Windows.Forms.FlowLayoutPanel();
        _createButton = new System.Windows.Forms.Button();
        _startButton = new System.Windows.Forms.Button();
        _stopButton = new System.Windows.Forms.Button();
        _resumeButton = new System.Windows.Forms.Button();
        _snapshotButton = new System.Windows.Forms.Button();
        _sessionInfoPanel = new System.Windows.Forms.TableLayoutPanel();
        _sessionIdCaption = new System.Windows.Forms.Label();
        _sessionIdLabel = new System.Windows.Forms.Label();
        _sessionStatusCaption = new System.Windows.Forms.Label();
        _sessionStatusLabel = new System.Windows.Forms.Label();
        _sessionTokenCaption = new System.Windows.Forms.Label();
        _sessionTokenLabel = new System.Windows.Forms.Label();
        _mainTabControl = new System.Windows.Forms.TabControl();
        _groupsTab = new System.Windows.Forms.TabPage();
        _groupsToolbar = new System.Windows.Forms.FlowLayoutPanel();
        _addGroupRowButton = new System.Windows.Forms.Button();
        _removeGroupRowButton = new System.Windows.Forms.Button();
        _saveGroupsButton = new System.Windows.Forms.Button();
        _reloadGroupsButton = new System.Windows.Forms.Button();
        _groupsGrid = new System.Windows.Forms.DataGridView();
        _snapshotTab = new System.Windows.Forms.TabPage();
        _snapshotSummaryPanel = new System.Windows.Forms.TableLayoutPanel();
        _balanceCaption = new System.Windows.Forms.Label();
        _balanceLabel = new System.Windows.Forms.Label();
        _equityCaption = new System.Windows.Forms.Label();
        _equityLabel = new System.Windows.Forms.Label();
        _positionsCaption = new System.Windows.Forms.Label();
        _positionsLabel = new System.Windows.Forms.Label();
        _pendingCaption = new System.Windows.Forms.Label();
        _pendingLabel = new System.Windows.Forms.Label();
        _snapshotTextBox = new System.Windows.Forms.TextBox();
        _screenToolTip = new System.Windows.Forms.ToolTip(components);
        _configGroup.SuspendLayout();
        _configLayout.SuspendLayout();
        _titanoBacktestPanel.SuspendLayout();
        _commandsPanel.SuspendLayout();
        _sessionInfoPanel.SuspendLayout();
        _mainTabControl.SuspendLayout();
        _groupsTab.SuspendLayout();
        _groupsToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(_groupsGrid)).BeginInit();
        _snapshotTab.SuspendLayout();
        _snapshotSummaryPanel.SuspendLayout();
        SuspendLayout();
        //
        // _screenExplainerLabel
        //
        _screenExplainerLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _screenExplainerLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _screenExplainerLabel.Location = new System.Drawing.Point(0, 0);
        _screenExplainerLabel.Name = "_screenExplainerLabel";
        _screenExplainerLabel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
        _screenExplainerLabel.Size = new System.Drawing.Size(960, 40);
        _screenExplainerLabel.TabIndex = 4;
        _screenExplainerLabel.Text = "—";
        //
        // _configGroup
        //
        _configGroup.AutoSize = true;
        _configGroup.Controls.Add(_configLayout);
        _configGroup.Dock = System.Windows.Forms.DockStyle.Top;
        _configGroup.Location = new System.Drawing.Point(0, 0);
        _configGroup.Name = "_configGroup";
        _configGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        _configGroup.Size = new System.Drawing.Size(960, 320);
        _configGroup.TabIndex = 0;
        _configGroup.TabStop = false;
        _configGroup.Text = "Configurazione sessione";
        // 
        // _configLayout
        // 
        _configLayout.AutoSize = true;
        _configLayout.ColumnCount = 4;
        _configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        _configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        _configLayout.Controls.Add(_creationSourceLabel, 0, 0);
        _configLayout.Controls.Add(_creationSourceCombo, 1, 0);
        _configLayout.Controls.Add(_planLabel, 2, 0);
        _configLayout.Controls.Add(_planCombo, 3, 0);
        _configLayout.Controls.Add(_workspaceLabel, 0, 1);
        _configLayout.Controls.Add(_workspaceCombo, 1, 1);
        _configLayout.Controls.Add(_derivedWorkspaceLabel, 2, 1);
        _configLayout.SetColumnSpan(_derivedWorkspaceLabel, 2);
        _configLayout.Controls.Add(_masterfilterInfoLabel, 0, 2);
        _configLayout.SetColumnSpan(_masterfilterInfoLabel, 4);
        _configLayout.Controls.Add(_modeLabel, 0, 3);
        _configLayout.Controls.Add(_modeCombo, 1, 3);
        _configLayout.Controls.Add(_clientRunModeLabel, 2, 3);
        _configLayout.Controls.Add(_clientRunModeCombo, 3, 3);
        _configLayout.Controls.Add(_titanoModeLabel, 0, 4);
        _configLayout.Controls.Add(_titanoModeCombo, 1, 4);
        _configLayout.Controls.Add(_titanoBacktestLabel, 2, 4);
        _configLayout.Controls.Add(_titanoBacktestPanel, 3, 4);
        _configLayout.Controls.Add(_titanoRunLabel, 0, 5);
        _configLayout.Controls.Add(_titanoRunCombo, 1, 5);
        _configLayout.Controls.Add(_loadRunsButton, 2, 5);
        _configLayout.Controls.Add(_executionKeyLabel, 0, 6);
        _configLayout.Controls.Add(_executionKeyTextBox, 1, 6);
        _configLayout.Controls.Add(_openPlanAccountLabel, 2, 6);
        _configLayout.Controls.Add(_openPlanAccountCombo, 3, 6);
        _configLayout.Controls.Add(_distributeCheckBox, 0, 7);
        _configLayout.SetColumnSpan(_distributeCheckBox, 2);
        _configLayout.Controls.Add(_portfolioRiskEnabledCheckBox, 2, 7);
        _configLayout.Controls.Add(_scenarioHintLabel, 0, 8);
        _configLayout.SetColumnSpan(_scenarioHintLabel, 4);
        _configLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _configLayout.Location = new System.Drawing.Point(12, 22);
        _configLayout.Name = "_configLayout";
        _configLayout.RowCount = 9;
        for (var row = 0; row < 9; row++)
        {
            _configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        }

        _configLayout.Size = new System.Drawing.Size(936, 286);
        _configLayout.TabIndex = 0;
        // 
        // _creationSourceLabel
        // 
        _creationSourceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _creationSourceLabel.AutoSize = true;
        _creationSourceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _creationSourceLabel.Name = "_creationSourceLabel";
        _creationSourceLabel.Size = new System.Drawing.Size(38, 15);
        _creationSourceLabel.TabIndex = 0;
        _creationSourceLabel.Text = "Origine";
        // 
        // _creationSourceCombo
        // 
        _creationSourceCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _creationSourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _creationSourceCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _creationSourceCombo.Name = "_creationSourceCombo";
        _creationSourceCombo.Size = new System.Drawing.Size(320, 23);
        _creationSourceCombo.TabIndex = 1;
        _creationSourceCombo.SelectedIndexChanged += OnCreationSourceChanged;
        // 
        // _planLabel
        // 
        _planLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _planLabel.AutoSize = true;
        _planLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _planLabel.Name = "_planLabel";
        _planLabel.Size = new System.Drawing.Size(34, 15);
        _planLabel.TabIndex = 2;
        _planLabel.Text = "Piano";
        // 
        // _planCombo
        // 
        _planCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _planCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _planCombo.Enabled = false;
        _planCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        _planCombo.Name = "_planCombo";
        _planCombo.Size = new System.Drawing.Size(320, 23);
        _planCombo.TabIndex = 3;
        _planCombo.SelectedIndexChanged += OnPlanChanged;
        // 
        // _workspaceLabel
        // 
        _workspaceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _workspaceLabel.AutoSize = true;
        _workspaceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _workspaceLabel.Name = "_workspaceLabel";
        _workspaceLabel.Size = new System.Drawing.Size(70, 15);
        _workspaceLabel.TabIndex = 4;
        _workspaceLabel.Text = "Workspace";
        // 
        // _workspaceCombo
        // 
        _workspaceCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _workspaceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _workspaceCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _workspaceCombo.Name = "_workspaceCombo";
        _workspaceCombo.Size = new System.Drawing.Size(320, 23);
        _workspaceCombo.TabIndex = 5;
        _workspaceCombo.SelectedIndexChanged += OnWorkspaceChanged;
        // 
        // _derivedWorkspaceLabel
        // 
        _derivedWorkspaceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _derivedWorkspaceLabel.AutoSize = true;
        _derivedWorkspaceLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _derivedWorkspaceLabel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        _derivedWorkspaceLabel.Name = "_derivedWorkspaceLabel";
        _derivedWorkspaceLabel.Size = new System.Drawing.Size(12, 15);
        _derivedWorkspaceLabel.TabIndex = 6;
        _derivedWorkspaceLabel.Text = "—";
        // 
        // _masterfilterInfoLabel
        // 
        _masterfilterInfoLabel.AutoSize = true;
        _masterfilterInfoLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _masterfilterInfoLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
        _masterfilterInfoLabel.Name = "_masterfilterInfoLabel";
        _masterfilterInfoLabel.Size = new System.Drawing.Size(12, 15);
        _masterfilterInfoLabel.TabIndex = 7;
        _masterfilterInfoLabel.Text = "—";
        // 
        // _modeLabel
        // 
        _modeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _modeLabel.AutoSize = true;
        _modeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _modeLabel.Name = "_modeLabel";
        _modeLabel.Size = new System.Drawing.Size(110, 15);
        _modeLabel.TabIndex = 8;
        _modeLabel.Text = "Modalità esecuzione";
        // 
        // _modeCombo
        // 
        _modeCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _modeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _modeCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _modeCombo.Name = "_modeCombo";
        _modeCombo.Size = new System.Drawing.Size(320, 23);
        _modeCombo.TabIndex = 9;
        _modeCombo.SelectedIndexChanged += OnExecutionModeChanged;
        // 
        // _clientRunModeLabel
        // 
        _clientRunModeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _clientRunModeLabel.AutoSize = true;
        _clientRunModeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _clientRunModeLabel.Name = "_clientRunModeLabel";
        _clientRunModeLabel.Size = new System.Drawing.Size(100, 15);
        _clientRunModeLabel.TabIndex = 10;
        _clientRunModeLabel.Text = "Contesto client";
        // 
        // _clientRunModeCombo
        // 
        _clientRunModeCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _clientRunModeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _clientRunModeCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        _clientRunModeCombo.Name = "_clientRunModeCombo";
        _clientRunModeCombo.Size = new System.Drawing.Size(320, 23);
        _clientRunModeCombo.TabIndex = 11;
        _clientRunModeCombo.SelectedIndexChanged += OnRunModeOrTitanoChanged;
        // 
        // _titanoModeLabel
        // 
        _titanoModeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _titanoModeLabel.AutoSize = true;
        _titanoModeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _titanoModeLabel.Name = "_titanoModeLabel";
        _titanoModeLabel.Size = new System.Drawing.Size(85, 15);
        _titanoModeLabel.TabIndex = 12;
        _titanoModeLabel.Text = "Filtro Titano";
        // 
        // _titanoModeCombo
        // 
        _titanoModeCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _titanoModeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _titanoModeCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _titanoModeCombo.Name = "_titanoModeCombo";
        _titanoModeCombo.Size = new System.Drawing.Size(320, 23);
        _titanoModeCombo.TabIndex = 13;
        _titanoModeCombo.SelectedIndexChanged += OnRunModeOrTitanoChanged;
        // 
        // _titanoBacktestLabel
        // 
        _titanoBacktestLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _titanoBacktestLabel.AutoSize = true;
        _titanoBacktestLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _titanoBacktestLabel.Name = "_titanoBacktestLabel";
        _titanoBacktestLabel.Size = new System.Drawing.Size(105, 15);
        _titanoBacktestLabel.TabIndex = 14;
        _titanoBacktestLabel.Text = "Backtest sorgente";
        // 
        // _titanoBacktestPanel
        // 
        _titanoBacktestPanel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _titanoBacktestPanel.AutoSize = true;
        _titanoBacktestPanel.ColumnCount = 2;
        _titanoBacktestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _titanoBacktestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _titanoBacktestPanel.Controls.Add(_titanoBacktestTextBox, 0, 0);
        _titanoBacktestPanel.Controls.Add(_titanoBacktestPickButton, 1, 0);
        _titanoBacktestPanel.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
        _titanoBacktestPanel.Name = "_titanoBacktestPanel";
        _titanoBacktestPanel.RowCount = 1;
        _titanoBacktestPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _titanoBacktestPanel.Size = new System.Drawing.Size(420, 31);
        _titanoBacktestPanel.TabIndex = 15;
        // 
        // _titanoBacktestTextBox
        // 
        _titanoBacktestTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _titanoBacktestTextBox.Margin = new System.Windows.Forms.Padding(0, 3, 6, 3);
        _titanoBacktestTextBox.Name = "_titanoBacktestTextBox";
        _titanoBacktestTextBox.PlaceholderText = "nessun backtest selezionato";
        _titanoBacktestTextBox.ReadOnly = true;
        _titanoBacktestTextBox.Size = new System.Drawing.Size(320, 23);
        _titanoBacktestTextBox.TabIndex = 0;
        // 
        // _titanoBacktestPickButton
        // 
        _titanoBacktestPickButton.AutoSize = true;
        _titanoBacktestPickButton.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
        _titanoBacktestPickButton.Name = "_titanoBacktestPickButton";
        _titanoBacktestPickButton.Size = new System.Drawing.Size(90, 25);
        _titanoBacktestPickButton.TabIndex = 1;
        _titanoBacktestPickButton.Text = "Scegli…";
        _titanoBacktestPickButton.UseVisualStyleBackColor = true;
        _titanoBacktestPickButton.Click += OnPickBacktestClick;
        // 
        // _titanoRunLabel
        // 
        _titanoRunLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _titanoRunLabel.AutoSize = true;
        _titanoRunLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _titanoRunLabel.Name = "_titanoRunLabel";
        _titanoRunLabel.Size = new System.Drawing.Size(70, 15);
        _titanoRunLabel.TabIndex = 16;
        _titanoRunLabel.Text = "Run Titano";
        // 
        // _titanoRunCombo
        // 
        _titanoRunCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _titanoRunCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _titanoRunCombo.Name = "_titanoRunCombo";
        _titanoRunCombo.Size = new System.Drawing.Size(320, 23);
        _titanoRunCombo.TabIndex = 17;
        // 
        // _loadRunsButton
        // 
        _loadRunsButton.AutoSize = true;
        _loadRunsButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        _loadRunsButton.Name = "_loadRunsButton";
        _loadRunsButton.Size = new System.Drawing.Size(80, 27);
        _loadRunsButton.TabIndex = 18;
        _loadRunsButton.Text = "Carica run";
        _loadRunsButton.UseVisualStyleBackColor = true;
        _loadRunsButton.Click += OnLoadRunsClick;
        // 
        // _executionKeyLabel
        // 
        _executionKeyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _executionKeyLabel.AutoSize = true;
        _executionKeyLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _executionKeyLabel.Name = "_executionKeyLabel";
        _executionKeyLabel.Size = new System.Drawing.Size(85, 15);
        _executionKeyLabel.TabIndex = 19;
        _executionKeyLabel.Text = "Execution key";
        // 
        // _executionKeyTextBox
        // 
        _executionKeyTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _executionKeyTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        _executionKeyTextBox.Name = "_executionKeyTextBox";
        _executionKeyTextBox.Size = new System.Drawing.Size(320, 23);
        _executionKeyTextBox.TabIndex = 20;
        // 
        // _openPlanAccountLabel
        // 
        _openPlanAccountLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _openPlanAccountLabel.AutoSize = true;
        _openPlanAccountLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        _openPlanAccountLabel.Name = "_openPlanAccountLabel";
        _openPlanAccountLabel.Size = new System.Drawing.Size(52, 15);
        _openPlanAccountLabel.TabIndex = 21;
        _openPlanAccountLabel.Text = "Account";
        // 
        // _openPlanAccountCombo
        // 
        _openPlanAccountCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _openPlanAccountCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _openPlanAccountCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        _openPlanAccountCombo.Name = "_openPlanAccountCombo";
        _openPlanAccountCombo.Size = new System.Drawing.Size(320, 23);
        _openPlanAccountCombo.TabIndex = 22;
        // 
        // _distributeCheckBox
        // 
        _distributeCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _distributeCheckBox.AutoSize = true;
        _distributeCheckBox.Checked = true;
        _distributeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        _distributeCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        _distributeCheckBox.Name = "_distributeCheckBox";
        _distributeCheckBox.Size = new System.Drawing.Size(280, 19);
        _distributeCheckBox.TabIndex = 23;
        _distributeCheckBox.Text = "Distribuisci ai gruppi (PiootooLiveTradingBot)";
        _distributeCheckBox.UseVisualStyleBackColor = true;
        //
        // _portfolioRiskEnabledCheckBox
        //
        _portfolioRiskEnabledCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _portfolioRiskEnabledCheckBox.AutoSize = true;
        _portfolioRiskEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        _portfolioRiskEnabledCheckBox.Name = "_portfolioRiskEnabledCheckBox";
        _portfolioRiskEnabledCheckBox.Size = new System.Drawing.Size(180, 19);
        _portfolioRiskEnabledCheckBox.TabIndex = 24;
        _portfolioRiskEnabledCheckBox.Text = "Rischio di portafoglio";
        _portfolioRiskEnabledCheckBox.UseVisualStyleBackColor = true;
        //
        // _scenarioHintLabel
        // 
        _scenarioHintLabel.AutoSize = true;
        _scenarioHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _scenarioHintLabel.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
        _scenarioHintLabel.MaximumSize = new System.Drawing.Size(920, 0);
        _scenarioHintLabel.Name = "_scenarioHintLabel";
        _scenarioHintLabel.Size = new System.Drawing.Size(12, 15);
        _scenarioHintLabel.TabIndex = 29;
        _scenarioHintLabel.Text = "—";
        // 
        // _commandsPanel
        // 
        _commandsPanel.AutoSize = true;
        _commandsPanel.Controls.Add(_createButton);
        _commandsPanel.Controls.Add(_startButton);
        _commandsPanel.Controls.Add(_stopButton);
        _commandsPanel.Controls.Add(_resumeButton);
        _commandsPanel.Controls.Add(_snapshotButton);
        _commandsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _commandsPanel.Location = new System.Drawing.Point(0, 320);
        _commandsPanel.Name = "_commandsPanel";
        _commandsPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
        _commandsPanel.Size = new System.Drawing.Size(960, 41);
        _commandsPanel.TabIndex = 1;
        _commandsPanel.WrapContents = false;
        // 
        // _createButton
        // 
        _createButton.AutoSize = true;
        _createButton.Name = "_createButton";
        _createButton.Size = new System.Drawing.Size(100, 27);
        _createButton.TabIndex = 0;
        _createButton.Text = "Crea sessione";
        _createButton.UseVisualStyleBackColor = true;
        _createButton.Click += OnCreateClick;
        // 
        // _startButton
        // 
        _startButton.AutoSize = true;
        _startButton.Enabled = false;
        _startButton.Name = "_startButton";
        _startButton.Size = new System.Drawing.Size(60, 27);
        _startButton.TabIndex = 1;
        _startButton.Text = "Avvia";
        _startButton.UseVisualStyleBackColor = true;
        _startButton.Click += OnStartClick;
        // 
        // _stopButton
        // 
        _stopButton.AutoSize = true;
        _stopButton.Enabled = false;
        _stopButton.Name = "_stopButton";
        _stopButton.Size = new System.Drawing.Size(60, 27);
        _stopButton.TabIndex = 2;
        _stopButton.Text = "Ferma";
        _stopButton.UseVisualStyleBackColor = true;
        _stopButton.Click += OnStopClick;
        // 
        // _resumeButton
        // 
        _resumeButton.AutoSize = true;
        _resumeButton.Enabled = false;
        _resumeButton.Name = "_resumeButton";
        _resumeButton.Size = new System.Drawing.Size(70, 27);
        _resumeButton.TabIndex = 3;
        _resumeButton.Text = "Riprendi";
        _resumeButton.UseVisualStyleBackColor = true;
        _resumeButton.Click += OnResumeClick;
        // 
        // _snapshotButton
        // 
        _snapshotButton.AutoSize = true;
        _snapshotButton.Enabled = false;
        _snapshotButton.Name = "_snapshotButton";
        _snapshotButton.Size = new System.Drawing.Size(120, 27);
        _snapshotButton.TabIndex = 4;
        _snapshotButton.Text = "Aggiorna snapshot";
        _snapshotButton.UseVisualStyleBackColor = true;
        _snapshotButton.Click += OnSnapshotClick;
        // 
        // _sessionInfoPanel
        // 
        _sessionInfoPanel.AutoSize = true;
        _sessionInfoPanel.ColumnCount = 6;
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        _sessionInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
        _sessionInfoPanel.Controls.Add(_sessionIdCaption, 0, 0);
        _sessionInfoPanel.Controls.Add(_sessionIdLabel, 1, 0);
        _sessionInfoPanel.Controls.Add(_sessionStatusCaption, 2, 0);
        _sessionInfoPanel.Controls.Add(_sessionStatusLabel, 3, 0);
        _sessionInfoPanel.Controls.Add(_sessionTokenCaption, 4, 0);
        _sessionInfoPanel.Controls.Add(_sessionTokenLabel, 5, 0);
        _sessionInfoPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _sessionInfoPanel.Location = new System.Drawing.Point(0, 361);
        _sessionInfoPanel.Name = "_sessionInfoPanel";
        _sessionInfoPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        _sessionInfoPanel.RowCount = 1;
        _sessionInfoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _sessionInfoPanel.Size = new System.Drawing.Size(960, 31);
        _sessionInfoPanel.TabIndex = 2;
        // 
        // _sessionIdCaption
        // 
        _sessionIdCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionIdCaption.AutoSize = true;
        _sessionIdCaption.Name = "_sessionIdCaption";
        _sessionIdCaption.Size = new System.Drawing.Size(18, 15);
        _sessionIdCaption.TabIndex = 0;
        _sessionIdCaption.Text = "ID";
        // 
        // _sessionIdLabel
        // 
        _sessionIdLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionIdLabel.AutoSize = true;
        _sessionIdLabel.Name = "_sessionIdLabel";
        _sessionIdLabel.Size = new System.Drawing.Size(12, 15);
        _sessionIdLabel.TabIndex = 1;
        _sessionIdLabel.Text = "—";
        // 
        // _sessionStatusCaption
        // 
        _sessionStatusCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionStatusCaption.AutoSize = true;
        _sessionStatusCaption.Name = "_sessionStatusCaption";
        _sessionStatusCaption.Size = new System.Drawing.Size(42, 15);
        _sessionStatusCaption.TabIndex = 2;
        _sessionStatusCaption.Text = "Stato";
        // 
        // _sessionStatusLabel
        // 
        _sessionStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionStatusLabel.AutoSize = true;
        _sessionStatusLabel.Name = "_sessionStatusLabel";
        _sessionStatusLabel.Size = new System.Drawing.Size(12, 15);
        _sessionStatusLabel.TabIndex = 3;
        _sessionStatusLabel.Text = "—";
        // 
        // _sessionTokenCaption
        // 
        _sessionTokenCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionTokenCaption.AutoSize = true;
        _sessionTokenCaption.Name = "_sessionTokenCaption";
        _sessionTokenCaption.Size = new System.Drawing.Size(42, 15);
        _sessionTokenCaption.TabIndex = 4;
        _sessionTokenCaption.Text = "Token";
        // 
        // _sessionTokenLabel
        // 
        _sessionTokenLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _sessionTokenLabel.AutoSize = true;
        _sessionTokenLabel.Name = "_sessionTokenLabel";
        _sessionTokenLabel.Size = new System.Drawing.Size(12, 15);
        _sessionTokenLabel.TabIndex = 5;
        _sessionTokenLabel.Text = "—";
        // 
        // _mainTabControl
        // 
        _mainTabControl.Controls.Add(_groupsTab);
        _mainTabControl.Controls.Add(_snapshotTab);
        _mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        _mainTabControl.Location = new System.Drawing.Point(0, 392);
        _mainTabControl.Name = "_mainTabControl";
        _mainTabControl.SelectedIndex = 0;
        _mainTabControl.Size = new System.Drawing.Size(960, 308);
        _mainTabControl.TabIndex = 3;
        // 
        // _groupsTab
        // 
        _groupsTab.Controls.Add(_groupsGrid);
        _groupsTab.Controls.Add(_groupsToolbar);
        _groupsTab.Location = new System.Drawing.Point(4, 24);
        _groupsTab.Name = "_groupsTab";
        _groupsTab.Padding = new System.Windows.Forms.Padding(8);
        _groupsTab.Size = new System.Drawing.Size(952, 280);
        _groupsTab.TabIndex = 0;
        _groupsTab.Text = "Gruppi account (ExternalBroker)";
        _groupsTab.UseVisualStyleBackColor = true;
        // 
        // _groupsToolbar
        // 
        _groupsToolbar.AutoSize = true;
        _groupsToolbar.Controls.Add(_addGroupRowButton);
        _groupsToolbar.Controls.Add(_removeGroupRowButton);
        _groupsToolbar.Controls.Add(_saveGroupsButton);
        _groupsToolbar.Controls.Add(_reloadGroupsButton);
        _groupsToolbar.Dock = System.Windows.Forms.DockStyle.Top;
        _groupsToolbar.Location = new System.Drawing.Point(8, 8);
        _groupsToolbar.Name = "_groupsToolbar";
        _groupsToolbar.Size = new System.Drawing.Size(936, 35);
        _groupsToolbar.TabIndex = 0;
        _groupsToolbar.WrapContents = false;
        // 
        // _addGroupRowButton
        // 
        _addGroupRowButton.AutoSize = true;
        _addGroupRowButton.Name = "_addGroupRowButton";
        _addGroupRowButton.Size = new System.Drawing.Size(90, 27);
        _addGroupRowButton.TabIndex = 0;
        _addGroupRowButton.Text = "Aggiungi riga";
        _addGroupRowButton.UseVisualStyleBackColor = true;
        _addGroupRowButton.Click += OnAddGroupRowClick;
        // 
        // _removeGroupRowButton
        // 
        _removeGroupRowButton.AutoSize = true;
        _removeGroupRowButton.Name = "_removeGroupRowButton";
        _removeGroupRowButton.Size = new System.Drawing.Size(90, 27);
        _removeGroupRowButton.TabIndex = 1;
        _removeGroupRowButton.Text = "Rimuovi riga";
        _removeGroupRowButton.UseVisualStyleBackColor = true;
        _removeGroupRowButton.Click += OnRemoveGroupRowClick;
        // 
        // _saveGroupsButton
        // 
        _saveGroupsButton.AutoSize = true;
        _saveGroupsButton.Enabled = false;
        _saveGroupsButton.Name = "_saveGroupsButton";
        _saveGroupsButton.Size = new System.Drawing.Size(100, 27);
        _saveGroupsButton.TabIndex = 2;
        _saveGroupsButton.Text = "Salva gruppi";
        _saveGroupsButton.UseVisualStyleBackColor = true;
        _saveGroupsButton.Click += OnSaveGroupsClick;
        // 
        // _reloadGroupsButton
        // 
        _reloadGroupsButton.AutoSize = true;
        _reloadGroupsButton.Enabled = false;
        _reloadGroupsButton.Name = "_reloadGroupsButton";
        _reloadGroupsButton.Size = new System.Drawing.Size(100, 27);
        _reloadGroupsButton.TabIndex = 3;
        _reloadGroupsButton.Text = "Ricarica gruppi";
        _reloadGroupsButton.UseVisualStyleBackColor = true;
        _reloadGroupsButton.Click += OnReloadGroupsClick;
        // 
        // _groupsGrid
        // 
        _groupsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _groupsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        _groupsGrid.Location = new System.Drawing.Point(8, 43);
        _groupsGrid.Name = "_groupsGrid";
        _groupsGrid.Size = new System.Drawing.Size(936, 229);
        _groupsGrid.TabIndex = 1;
        // 
        // _snapshotTab
        // 
        _snapshotTab.Controls.Add(_snapshotTextBox);
        _snapshotTab.Controls.Add(_snapshotSummaryPanel);
        _snapshotTab.Location = new System.Drawing.Point(4, 24);
        _snapshotTab.Name = "_snapshotTab";
        _snapshotTab.Padding = new System.Windows.Forms.Padding(8);
        _snapshotTab.Size = new System.Drawing.Size(952, 280);
        _snapshotTab.TabIndex = 1;
        _snapshotTab.Text = "Snapshot";
        _snapshotTab.UseVisualStyleBackColor = true;
        // 
        // _snapshotSummaryPanel
        // 
        _snapshotSummaryPanel.AutoSize = true;
        _snapshotSummaryPanel.ColumnCount = 8;
        for (var column = 0; column < 8; column++)
        {
            _snapshotSummaryPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        }

        _snapshotSummaryPanel.Controls.Add(_balanceCaption, 0, 0);
        _snapshotSummaryPanel.Controls.Add(_balanceLabel, 1, 0);
        _snapshotSummaryPanel.Controls.Add(_equityCaption, 2, 0);
        _snapshotSummaryPanel.Controls.Add(_equityLabel, 3, 0);
        _snapshotSummaryPanel.Controls.Add(_positionsCaption, 4, 0);
        _snapshotSummaryPanel.Controls.Add(_positionsLabel, 5, 0);
        _snapshotSummaryPanel.Controls.Add(_pendingCaption, 6, 0);
        _snapshotSummaryPanel.Controls.Add(_pendingLabel, 7, 0);
        _snapshotSummaryPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _snapshotSummaryPanel.Location = new System.Drawing.Point(8, 8);
        _snapshotSummaryPanel.Name = "_snapshotSummaryPanel";
        _snapshotSummaryPanel.RowCount = 1;
        _snapshotSummaryPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _snapshotSummaryPanel.Size = new System.Drawing.Size(936, 23);
        _snapshotSummaryPanel.TabIndex = 0;
        // 
        // _balanceCaption
        // 
        _balanceCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _balanceCaption.AutoSize = true;
        _balanceCaption.Name = "_balanceCaption";
        _balanceCaption.Size = new System.Drawing.Size(45, 15);
        _balanceCaption.TabIndex = 0;
        _balanceCaption.Text = "Saldo";
        // 
        // _balanceLabel
        // 
        _balanceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _balanceLabel.AutoSize = true;
        _balanceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
        _balanceLabel.Name = "_balanceLabel";
        _balanceLabel.Size = new System.Drawing.Size(13, 15);
        _balanceLabel.TabIndex = 1;
        _balanceLabel.Text = "0";
        // 
        // _equityCaption
        // 
        _equityCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _equityCaption.AutoSize = true;
        _equityCaption.Name = "_equityCaption";
        _equityCaption.Size = new System.Drawing.Size(42, 15);
        _equityCaption.TabIndex = 2;
        _equityCaption.Text = "Equity";
        // 
        // _equityLabel
        // 
        _equityLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _equityLabel.AutoSize = true;
        _equityLabel.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
        _equityLabel.Name = "_equityLabel";
        _equityLabel.Size = new System.Drawing.Size(13, 15);
        _equityLabel.TabIndex = 3;
        _equityLabel.Text = "0";
        // 
        // _positionsCaption
        // 
        _positionsCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _positionsCaption.AutoSize = true;
        _positionsCaption.Name = "_positionsCaption";
        _positionsCaption.Size = new System.Drawing.Size(62, 15);
        _positionsCaption.TabIndex = 4;
        _positionsCaption.Text = "Posizioni";
        // 
        // _positionsLabel
        // 
        _positionsLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _positionsLabel.AutoSize = true;
        _positionsLabel.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
        _positionsLabel.Name = "_positionsLabel";
        _positionsLabel.Size = new System.Drawing.Size(13, 15);
        _positionsLabel.TabIndex = 5;
        _positionsLabel.Text = "0";
        // 
        // _pendingCaption
        // 
        _pendingCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _pendingCaption.AutoSize = true;
        _pendingCaption.Name = "_pendingCaption";
        _pendingCaption.Size = new System.Drawing.Size(58, 15);
        _pendingCaption.TabIndex = 6;
        _pendingCaption.Text = "In attesa";
        // 
        // _pendingLabel
        // 
        _pendingLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _pendingLabel.AutoSize = true;
        _pendingLabel.Name = "_pendingLabel";
        _pendingLabel.Size = new System.Drawing.Size(13, 15);
        _pendingLabel.TabIndex = 7;
        _pendingLabel.Text = "0";
        // 
        // _snapshotTextBox
        // 
        _snapshotTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        _snapshotTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _snapshotTextBox.Font = new System.Drawing.Font("Consolas", 9F);
        _snapshotTextBox.Location = new System.Drawing.Point(8, 31);
        _snapshotTextBox.Multiline = true;
        _snapshotTextBox.Name = "_snapshotTextBox";
        _snapshotTextBox.ReadOnly = true;
        _snapshotTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        _snapshotTextBox.Size = new System.Drawing.Size(936, 241);
        _snapshotTextBox.TabIndex = 1;
        // 
        // TradingSessionsScreen
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(_mainTabControl);
        Controls.Add(_sessionInfoPanel);
        Controls.Add(_commandsPanel);
        Controls.Add(_configGroup);
        Controls.Add(_screenExplainerLabel);
        Name = "TradingSessionsScreen";
        Size = new System.Drawing.Size(960, 700);
        _configGroup.ResumeLayout(false);
        _configGroup.PerformLayout();
        _configLayout.ResumeLayout(false);
        _titanoBacktestPanel.ResumeLayout(false);
        _titanoBacktestPanel.PerformLayout();
        _configLayout.PerformLayout();
        _commandsPanel.ResumeLayout(false);
        _commandsPanel.PerformLayout();
        _sessionInfoPanel.ResumeLayout(false);
        _sessionInfoPanel.PerformLayout();
        _mainTabControl.ResumeLayout(false);
        _groupsTab.ResumeLayout(false);
        _groupsTab.PerformLayout();
        _groupsToolbar.ResumeLayout(false);
        _groupsToolbar.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(_groupsGrid)).EndInit();
        _snapshotTab.ResumeLayout(false);
        _snapshotTab.PerformLayout();
        _snapshotSummaryPanel.ResumeLayout(false);
        _snapshotSummaryPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label _screenExplainerLabel;
    private System.Windows.Forms.GroupBox _configGroup;
    private System.Windows.Forms.TableLayoutPanel _configLayout;
    private System.Windows.Forms.Label _creationSourceLabel;
    private System.Windows.Forms.ComboBox _creationSourceCombo;
    private System.Windows.Forms.Label _planLabel;
    private System.Windows.Forms.ComboBox _planCombo;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.ComboBox _workspaceCombo;
    private System.Windows.Forms.Label _derivedWorkspaceLabel;
    private System.Windows.Forms.Label _masterfilterInfoLabel;
    private System.Windows.Forms.Label _modeLabel;
    private System.Windows.Forms.ComboBox _modeCombo;
    private System.Windows.Forms.Label _clientRunModeLabel;
    private System.Windows.Forms.ComboBox _clientRunModeCombo;
    private System.Windows.Forms.Label _titanoModeLabel;
    private System.Windows.Forms.ComboBox _titanoModeCombo;
    private System.Windows.Forms.Label _titanoBacktestLabel;
    private System.Windows.Forms.TableLayoutPanel _titanoBacktestPanel;
    private System.Windows.Forms.TextBox _titanoBacktestTextBox;
    private System.Windows.Forms.Button _titanoBacktestPickButton;
    private System.Windows.Forms.Label _titanoRunLabel;
    private System.Windows.Forms.ComboBox _titanoRunCombo;
    private System.Windows.Forms.Button _loadRunsButton;
    private System.Windows.Forms.Label _executionKeyLabel;
    private System.Windows.Forms.TextBox _executionKeyTextBox;
    private System.Windows.Forms.Label _openPlanAccountLabel;
    private System.Windows.Forms.ComboBox _openPlanAccountCombo;
    private System.Windows.Forms.CheckBox _distributeCheckBox;
    private System.Windows.Forms.CheckBox _portfolioRiskEnabledCheckBox;
    private System.Windows.Forms.Label _scenarioHintLabel;
    private System.Windows.Forms.FlowLayoutPanel _commandsPanel;
    private System.Windows.Forms.Button _createButton;
    private System.Windows.Forms.Button _startButton;
    private System.Windows.Forms.Button _stopButton;
    private System.Windows.Forms.Button _resumeButton;
    private System.Windows.Forms.Button _snapshotButton;
    private System.Windows.Forms.TableLayoutPanel _sessionInfoPanel;
    private System.Windows.Forms.Label _sessionIdCaption;
    private System.Windows.Forms.Label _sessionIdLabel;
    private System.Windows.Forms.Label _sessionStatusCaption;
    private System.Windows.Forms.Label _sessionStatusLabel;
    private System.Windows.Forms.Label _sessionTokenCaption;
    private System.Windows.Forms.Label _sessionTokenLabel;
    private System.Windows.Forms.TabControl _mainTabControl;
    private System.Windows.Forms.TabPage _groupsTab;
    private System.Windows.Forms.FlowLayoutPanel _groupsToolbar;
    private System.Windows.Forms.Button _addGroupRowButton;
    private System.Windows.Forms.Button _removeGroupRowButton;
    private System.Windows.Forms.Button _saveGroupsButton;
    private System.Windows.Forms.Button _reloadGroupsButton;
    private System.Windows.Forms.DataGridView _groupsGrid;
    private System.Windows.Forms.TabPage _snapshotTab;
    private System.Windows.Forms.TableLayoutPanel _snapshotSummaryPanel;
    private System.Windows.Forms.Label _balanceCaption;
    private System.Windows.Forms.Label _balanceLabel;
    private System.Windows.Forms.Label _equityCaption;
    private System.Windows.Forms.Label _equityLabel;
    private System.Windows.Forms.Label _positionsCaption;
    private System.Windows.Forms.Label _positionsLabel;
    private System.Windows.Forms.Label _pendingCaption;
    private System.Windows.Forms.Label _pendingLabel;
    private System.Windows.Forms.TextBox _snapshotTextBox;
    private System.Windows.Forms.ToolTip _screenToolTip;
}
