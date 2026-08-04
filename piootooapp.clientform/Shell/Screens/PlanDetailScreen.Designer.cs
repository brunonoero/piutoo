namespace piootooapp.clientform.Shell.Screens;

partial class PlanDetailScreen
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
        this.components = new System.ComponentModel.Container();
        this._groupsBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._instrumentsBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._tabs = new System.Windows.Forms.TabControl();
        this._generalTab = new System.Windows.Forms.TabPage();
        this._generalLayout = new System.Windows.Forms.TableLayoutPanel();
        this._workspaceLabel = new System.Windows.Forms.Label();
        this._workspaceCombo = new System.Windows.Forms.ComboBox();
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeTextBox = new System.Windows.Forms.TextBox();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._maxConcurrentLabel = new System.Windows.Forms.Label();
        this._maxConcurrentInput = new System.Windows.Forms.NumericUpDown();
        this._initialCapitalLabel = new System.Windows.Forms.Label();
        this._initialCapitalInput = new System.Windows.Forms.NumericUpDown();
        this._commissionLabel = new System.Windows.Forms.Label();
        this._commissionInput = new System.Windows.Forms.NumericUpDown();
        this._enforceConcurrencyLabel = new System.Windows.Forms.Label();
        this._enforceConcurrencyCombo = new System.Windows.Forms.ComboBox();
        this._applyTitanoCheckBox = new System.Windows.Forms.CheckBox();
        this._rotationSetupLabel = new System.Windows.Forms.Label();
        this._rotationSetupCombo = new System.Windows.Forms.ComboBox();
        this._titanoRunLabel = new System.Windows.Forms.Label();
        this._rotationStatusLabel = new System.Windows.Forms.Label();
        this._titanoFolderLabel = new System.Windows.Forms.Label();
        this._titanoFolderCombo = new System.Windows.Forms.ComboBox();
        this._groupsTab = new System.Windows.Forms.TabPage();
        this._groupsGrid = new System.Windows.Forms.DataGridView();
        this._colGroupId = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colAccountNumber = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colGroupMaxConcurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colGroupRotationSetup = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colGroupTitanoFolder = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colGroupApplyTitano = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._groupsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._addGroupButton = new System.Windows.Forms.Button();
        this._removeGroupButton = new System.Windows.Forms.Button();
        this._instrumentsTab = new System.Windows.Forms.TabPage();
        this._instrumentsGrid = new System.Windows.Forms.DataGridView();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colDollarsPerPoint = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMinimumQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colQuantityStep = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRoundingMode = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._instrumentsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._addInstrumentButton = new System.Windows.Forms.Button();
        this._removeInstrumentButton = new System.Windows.Forms.Button();
        this._importSymbolsButton = new System.Windows.Forms.Button();
        this._sizingTab = new System.Windows.Forms.TabPage();
        this._sizingLayout = new System.Windows.Forms.TableLayoutPanel();
        this._clampMultipliersCheckBox = new System.Windows.Forms.CheckBox();
        this._volatilityEnabledCheckBox = new System.Windows.Forms.CheckBox();
        this._atrPeriodsLabel = new System.Windows.Forms.Label();
        this._atrPeriodsInput = new System.Windows.Forms.NumericUpDown();
        this._targetRiskLabel = new System.Windows.Forms.Label();
        this._targetRiskInput = new System.Windows.Forms.NumericUpDown();
        this._portfolioRiskEnabledCheckBox = new System.Windows.Forms.CheckBox();
        this._maxDrawdownLabel = new System.Windows.Forms.Label();
        this._maxDrawdownInput = new System.Windows.Forms.NumericUpDown();
        this._maxGrossExposureLabel = new System.Windows.Forms.Label();
        this._maxGrossExposureInput = new System.Windows.Forms.NumericUpDown();
        this._cppiEnabledCheckBox = new System.Windows.Forms.CheckBox();
        this._cppiFloorLabel = new System.Windows.Forms.Label();
        this._cppiFloorInput = new System.Windows.Forms.NumericUpDown();
        this._cppiMultiplierLabel = new System.Windows.Forms.Label();
        this._cppiMultiplierInput = new System.Windows.Forms.NumericUpDown();
        this._aggressiveModulesCheckBox = new System.Windows.Forms.CheckBox();
        this._fractionalFactorLabel = new System.Windows.Forms.Label();
        this._fractionalFactorInput = new System.Windows.Forms.NumericUpDown();
        this._maximumMultiplierLabel = new System.Windows.Forms.Label();
        this._maximumMultiplierInput = new System.Windows.Forms.NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)(this._groupsBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._instrumentsBindingSource)).BeginInit();
        this._tabs.SuspendLayout();
        this._generalTab.SuspendLayout();
        this._generalLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._maxConcurrentInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._initialCapitalInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).BeginInit();
        this._groupsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).BeginInit();
        this._groupsButtons.SuspendLayout();
        this._instrumentsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._instrumentsGrid)).BeginInit();
        this._instrumentsButtons.SuspendLayout();
        this._sizingTab.SuspendLayout();
        this._sizingLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._atrPeriodsInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._targetRiskInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxDrawdownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxGrossExposureInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._cppiFloorInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._cppiMultiplierInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._fractionalFactorInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumMultiplierInput)).BeginInit();
        this.SuspendLayout();
        // 
        // _toolbar
        // 
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Piano";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        // 
        // _tabs
        // 
        this._tabs.Controls.Add(this._generalTab);
        this._tabs.Controls.Add(this._groupsTab);
        this._tabs.Controls.Add(this._instrumentsTab);
        this._tabs.Controls.Add(this._sizingTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 44);
        this._tabs.Name = "_tabs";
        this._tabs.Padding = new System.Drawing.Point(12, 4);
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(900, 556);
        this._tabs.TabIndex = 1;
        // 
        // _generalTab
        // 
        this._generalTab.Controls.Add(this._generalLayout);
        this._generalTab.Location = new System.Drawing.Point(4, 27);
        this._generalTab.Name = "_generalTab";
        this._generalTab.Padding = new System.Windows.Forms.Padding(12);
        this._generalTab.Size = new System.Drawing.Size(892, 525);
        this._generalTab.TabIndex = 0;
        this._generalTab.Text = "Generale";
        this._generalTab.UseVisualStyleBackColor = true;
        // 
        // _generalLayout
        // 
        this._generalLayout.AutoSize = true;
        this._generalLayout.ColumnCount = 4;
        this._generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._generalLayout.Controls.Add(this._workspaceLabel, 0, 0);
        this._generalLayout.Controls.Add(this._workspaceCombo, 1, 0);
        this._generalLayout.Controls.Add(this._codeLabel, 2, 0);
        this._generalLayout.Controls.Add(this._codeTextBox, 3, 0);
        this._generalLayout.Controls.Add(this._nameLabel, 0, 1);
        this._generalLayout.Controls.Add(this._nameTextBox, 1, 1);
        this._generalLayout.Controls.Add(this._maxConcurrentLabel, 2, 1);
        this._generalLayout.Controls.Add(this._maxConcurrentInput, 3, 1);
        this._generalLayout.Controls.Add(this._initialCapitalLabel, 0, 2);
        this._generalLayout.Controls.Add(this._initialCapitalInput, 1, 2);
        this._generalLayout.Controls.Add(this._commissionLabel, 2, 2);
        this._generalLayout.Controls.Add(this._commissionInput, 3, 2);
        this._generalLayout.Controls.Add(this._enforceConcurrencyLabel, 0, 3);
        this._generalLayout.Controls.Add(this._enforceConcurrencyCombo, 1, 3);
        this._generalLayout.Controls.Add(this._applyTitanoCheckBox, 3, 3);
        this._generalLayout.Controls.Add(this._titanoFolderLabel, 0, 4);
        this._generalLayout.Controls.Add(this._titanoFolderCombo, 1, 4);
        this._generalLayout.Controls.Add(this._titanoRunLabel, 2, 4);
        this._generalLayout.Controls.Add(this._rotationStatusLabel, 3, 4);
        this._generalLayout.Controls.Add(this._rotationSetupLabel, 0, 5);
        this._generalLayout.Controls.Add(this._rotationSetupCombo, 1, 5);
        this._generalLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._generalLayout.Location = new System.Drawing.Point(12, 12);
        this._generalLayout.Name = "_generalLayout";
        this._generalLayout.RowCount = 6;
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.Size = new System.Drawing.Size(868, 200);
        this._generalLayout.TabIndex = 0;
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
        // _codeLabel
        // 
        this._codeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._codeLabel.AutoSize = true;
        this._codeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._codeLabel.Name = "_codeLabel";
        this._codeLabel.Size = new System.Drawing.Size(50, 15);
        this._codeLabel.TabIndex = 2;
        this._codeLabel.Text = "Codice";
        // 
        // _codeTextBox
        // 
        this._codeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._codeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._codeTextBox.Name = "_codeTextBox";
        this._codeTextBox.Size = new System.Drawing.Size(280, 23);
        this._codeTextBox.TabIndex = 3;
        this._codeTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _nameLabel
        // 
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(45, 15);
        this._nameLabel.TabIndex = 4;
        this._nameLabel.Text = "Nome";
        // 
        // _nameTextBox
        // 
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(280, 23);
        this._nameTextBox.TabIndex = 5;
        this._nameTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _maxConcurrentLabel
        // 
        this._maxConcurrentLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxConcurrentLabel.AutoSize = true;
        this._maxConcurrentLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maxConcurrentLabel.Name = "_maxConcurrentLabel";
        this._maxConcurrentLabel.Size = new System.Drawing.Size(210, 15);
        this._maxConcurrentLabel.TabIndex = 6;
        this._maxConcurrentLabel.Text = "Max posizioni contemporanee (0 = illimitate)";
        // 
        // _maxConcurrentInput
        // 
        this._maxConcurrentInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxConcurrentInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maxConcurrentInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._maxConcurrentInput.Name = "_maxConcurrentInput";
        this._maxConcurrentInput.Size = new System.Drawing.Size(100, 23);
        this._maxConcurrentInput.TabIndex = 7;
        this._maxConcurrentInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
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
        this._initialCapitalInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _commissionLabel
        // 
        this._commissionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionLabel.AutoSize = true;
        this._commissionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._commissionLabel.Name = "_commissionLabel";
        this._commissionLabel.Size = new System.Drawing.Size(170, 15);
        this._commissionLabel.TabIndex = 10;
        this._commissionLabel.Text = "Commissione per contratto";
        // 
        // _commissionInput
        // 
        this._commissionInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionInput.DecimalPlaces = 2;
        this._commissionInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._commissionInput.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        this._commissionInput.Name = "_commissionInput";
        this._commissionInput.Size = new System.Drawing.Size(160, 23);
        this._commissionInput.TabIndex = 11;
        this._commissionInput.Value = new decimal(new int[] { 2, 0, 0, 0 });
        this._commissionInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _enforceConcurrencyLabel
        // 
        this._enforceConcurrencyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._enforceConcurrencyLabel.AutoSize = true;
        this._enforceConcurrencyLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._enforceConcurrencyLabel.Name = "_enforceConcurrencyLabel";
        this._enforceConcurrencyLabel.Size = new System.Drawing.Size(160, 15);
        this._enforceConcurrencyLabel.TabIndex = 12;
        this._enforceConcurrencyLabel.Text = "Limiti di concorrenza";
        // 
        // _enforceConcurrencyCombo
        // 
        this._enforceConcurrencyCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._enforceConcurrencyCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._enforceConcurrencyCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._enforceConcurrencyCombo.Name = "_enforceConcurrencyCombo";
        this._enforceConcurrencyCombo.Size = new System.Drawing.Size(280, 23);
        this._enforceConcurrencyCombo.TabIndex = 13;
        this._enforceConcurrencyCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _applyTitanoCheckBox
        // 
        this._applyTitanoCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._applyTitanoCheckBox.AutoSize = true;
        this._applyTitanoCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        this._applyTitanoCheckBox.Name = "_applyTitanoCheckBox";
        this._applyTitanoCheckBox.Size = new System.Drawing.Size(200, 19);
        this._applyTitanoCheckBox.TabIndex = 14;
        this._applyTitanoCheckBox.Text = "Applica i filtri Titano";
        this._applyTitanoCheckBox.UseVisualStyleBackColor = true;
        this._applyTitanoCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _rotationSetupLabel
        // 
        this._rotationSetupLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._rotationSetupLabel.AutoSize = true;
        this._rotationSetupLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._rotationSetupLabel.Name = "_rotationSetupLabel";
        this._rotationSetupLabel.Size = new System.Drawing.Size(110, 15);
        this._rotationSetupLabel.TabIndex = 19;
        this._rotationSetupLabel.Text = "Setup di rotazione";
        // 
        // _rotationSetupCombo
        //
        this._rotationSetupCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._rotationSetupCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._rotationSetupCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._rotationSetupCombo.Name = "_rotationSetupCombo";
        this._rotationSetupCombo.Size = new System.Drawing.Size(280, 23);
        this._rotationSetupCombo.TabIndex = 20;
        this._rotationSetupCombo.SelectedIndexChanged += new System.EventHandler(this.OnTitanoFieldChanged);
        // 
        // _titanoRunLabel
        // 
        this._titanoRunLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._titanoRunLabel.AutoSize = true;
        this._titanoRunLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._titanoRunLabel.Name = "_titanoRunLabel";
        this._titanoRunLabel.Size = new System.Drawing.Size(90, 15);
        this._titanoRunLabel.TabIndex = 17;
        this._titanoRunLabel.Text = "Stato rotazione";
        //
        // _rotationStatusLabel
        //
        this._rotationStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._rotationStatusLabel.AutoSize = true;
        this._rotationStatusLabel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._rotationStatusLabel.Name = "_rotationStatusLabel";
        this._rotationStatusLabel.Size = new System.Drawing.Size(280, 15);
        this._rotationStatusLabel.TabIndex = 18;
        this._rotationStatusLabel.Text = "—";
        //
        // _titanoFolderLabel
        // 
        this._titanoFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._titanoFolderLabel.AutoSize = true;
        this._titanoFolderLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._titanoFolderLabel.Name = "_titanoFolderLabel";
        this._titanoFolderLabel.Size = new System.Drawing.Size(130, 15);
        this._titanoFolderLabel.TabIndex = 15;
        this._titanoFolderLabel.Text = "Cartella backtest Titano";
        // 
        // _titanoFolderCombo
        //
        this._titanoFolderCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._titanoFolderCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._titanoFolderCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._titanoFolderCombo.Name = "_titanoFolderCombo";
        this._titanoFolderCombo.Size = new System.Drawing.Size(280, 23);
        this._titanoFolderCombo.TabIndex = 16;
        this._titanoFolderCombo.SelectedIndexChanged += new System.EventHandler(this.OnTitanoFolderChanged);
        // 
        // _groupsTab
        // 
        this._groupsTab.Controls.Add(this._groupsGrid);
        this._groupsTab.Controls.Add(this._groupsButtons);
        this._groupsTab.Location = new System.Drawing.Point(4, 27);
        this._groupsTab.Name = "_groupsTab";
        this._groupsTab.Padding = new System.Windows.Forms.Padding(12);
        this._groupsTab.Size = new System.Drawing.Size(892, 525);
        this._groupsTab.TabIndex = 1;
        this._groupsTab.Text = "Gruppi e account";
        this._groupsTab.UseVisualStyleBackColor = true;
        // 
        // _groupsGrid
        // 
        this._groupsGrid.AllowUserToAddRows = false;
        this._groupsGrid.AllowUserToDeleteRows = false;
        this._groupsGrid.AutoGenerateColumns = false;
        this._groupsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._groupsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._groupsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._groupsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._groupsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colGroupId,
            this._colAccountNumber,
            this._colGroupMaxConcurrent,
            this._colGroupRotationSetup,
            this._colGroupTitanoFolder,
            this._colGroupApplyTitano});
        this._groupsGrid.DataSource = this._groupsBindingSource;
        this._groupsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._groupsGrid.Location = new System.Drawing.Point(12, 49);
        this._groupsGrid.Name = "_groupsGrid";
        this._groupsGrid.RowHeadersVisible = false;
        this._groupsGrid.Size = new System.Drawing.Size(868, 464);
        this._groupsGrid.TabIndex = 1;
        this._groupsGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGroupsGridCellValueChanged);
        this._groupsGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.OnGroupsGridCurrentCellDirtyStateChanged);
        this._groupsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnGroupsGridDataError);
        // 
        // _colGroupId
        // 
        this._colGroupId.DataPropertyName = "GroupId";
        this._colGroupId.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colGroupId.HeaderText = "Gruppo";
        this._colGroupId.Name = "_colGroupId";
        // 
        // _colAccountNumber
        // 
        this._colAccountNumber.DataPropertyName = "AccountNumber";
        this._colAccountNumber.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colAccountNumber.HeaderText = "Account cTrader";
        this._colAccountNumber.Name = "_colAccountNumber";
        // 
        // _colGroupMaxConcurrent
        // 
        this._colGroupMaxConcurrent.DataPropertyName = "MaxConcurrentTrades";
        this._colGroupMaxConcurrent.FillWeight = 70F;
        this._colGroupMaxConcurrent.HeaderText = "Max concorrenti";
        this._colGroupMaxConcurrent.Name = "_colGroupMaxConcurrent";
        // 
        // _colGroupRotationSetup
        // 
        this._colGroupRotationSetup.DataPropertyName = "RotationSetupId";
        this._colGroupRotationSetup.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colGroupRotationSetup.HeaderText = "Setup rotazione";
        this._colGroupRotationSetup.Name = "_colGroupRotationSetup";
        //
        // _colGroupTitanoFolder
        // 
        this._colGroupTitanoFolder.DataPropertyName = "TitanoBacktestFolder";
        this._colGroupTitanoFolder.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colGroupTitanoFolder.HeaderText = "Cartella backtest";
        this._colGroupTitanoFolder.Name = "_colGroupTitanoFolder";
        // 
        // _colGroupApplyTitano
        // 
        this._colGroupApplyTitano.DataPropertyName = "ApplyTitanoFilters";
        this._colGroupApplyTitano.FillWeight = 60F;
        this._colGroupApplyTitano.HeaderText = "Filtra";
        this._colGroupApplyTitano.Name = "_colGroupApplyTitano";
        // 
        // _groupsButtons
        // 
        this._groupsButtons.AutoSize = true;
        this._groupsButtons.Controls.Add(this._addGroupButton);
        this._groupsButtons.Controls.Add(this._removeGroupButton);
        this._groupsButtons.Dock = System.Windows.Forms.DockStyle.Top;
        this._groupsButtons.Location = new System.Drawing.Point(12, 12);
        this._groupsButtons.Name = "_groupsButtons";
        this._groupsButtons.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
        this._groupsButtons.Size = new System.Drawing.Size(868, 37);
        this._groupsButtons.TabIndex = 0;
        this._groupsButtons.WrapContents = false;
        // 
        // _addGroupButton
        // 
        this._addGroupButton.AutoSize = true;
        this._addGroupButton.Name = "_addGroupButton";
        this._addGroupButton.Size = new System.Drawing.Size(100, 25);
        this._addGroupButton.TabIndex = 0;
        this._addGroupButton.Text = "Aggiungi riga";
        this._addGroupButton.UseVisualStyleBackColor = true;
        this._addGroupButton.Click += new System.EventHandler(this.OnAddGroupClick);
        // 
        // _removeGroupButton
        // 
        this._removeGroupButton.AutoSize = true;
        this._removeGroupButton.Name = "_removeGroupButton";
        this._removeGroupButton.Size = new System.Drawing.Size(100, 25);
        this._removeGroupButton.TabIndex = 1;
        this._removeGroupButton.Text = "Rimuovi riga";
        this._removeGroupButton.UseVisualStyleBackColor = true;
        this._removeGroupButton.Click += new System.EventHandler(this.OnRemoveGroupClick);
        // 
        // _instrumentsTab
        // 
        this._instrumentsTab.Controls.Add(this._instrumentsGrid);
        this._instrumentsTab.Controls.Add(this._instrumentsButtons);
        this._instrumentsTab.Location = new System.Drawing.Point(4, 27);
        this._instrumentsTab.Name = "_instrumentsTab";
        this._instrumentsTab.Padding = new System.Windows.Forms.Padding(12);
        this._instrumentsTab.Size = new System.Drawing.Size(892, 525);
        this._instrumentsTab.TabIndex = 2;
        this._instrumentsTab.Text = "Strumenti";
        this._instrumentsTab.UseVisualStyleBackColor = true;
        // 
        // _instrumentsGrid
        // 
        this._instrumentsGrid.AllowUserToAddRows = false;
        this._instrumentsGrid.AllowUserToDeleteRows = false;
        this._instrumentsGrid.AutoGenerateColumns = false;
        this._instrumentsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._instrumentsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._instrumentsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._instrumentsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._instrumentsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colSymbol,
            this._colDollarsPerPoint,
            this._colMinimumQuantity,
            this._colQuantityStep,
            this._colRoundingMode});
        this._instrumentsGrid.DataSource = this._instrumentsBindingSource;
        this._instrumentsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._instrumentsGrid.Location = new System.Drawing.Point(12, 49);
        this._instrumentsGrid.Name = "_instrumentsGrid";
        this._instrumentsGrid.RowHeadersVisible = false;
        this._instrumentsGrid.Size = new System.Drawing.Size(868, 464);
        this._instrumentsGrid.TabIndex = 1;
        // 
        // _colSymbol
        // 
        this._colSymbol.DataPropertyName = "Symbol";
        this._colSymbol.HeaderText = "Simbolo";
        this._colSymbol.Name = "_colSymbol";
        // 
        // _colDollarsPerPoint
        // 
        this._colDollarsPerPoint.DataPropertyName = "DollarsPerPoint";
        this._colDollarsPerPoint.HeaderText = "Dollari per punto";
        this._colDollarsPerPoint.Name = "_colDollarsPerPoint";
        // 
        // _colMinimumQuantity
        // 
        this._colMinimumQuantity.DataPropertyName = "MinimumQuantity";
        this._colMinimumQuantity.HeaderText = "Quantità minima";
        this._colMinimumQuantity.Name = "_colMinimumQuantity";
        // 
        // _colQuantityStep
        // 
        this._colQuantityStep.DataPropertyName = "QuantityStep";
        this._colQuantityStep.HeaderText = "Passo quantità";
        this._colQuantityStep.Name = "_colQuantityStep";
        // 
        // _colRoundingMode
        // 
        this._colRoundingMode.DataPropertyName = "RoundingMode";
        this._colRoundingMode.HeaderText = "Arrotondamento";
        this._colRoundingMode.Name = "_colRoundingMode";
        // 
        // _instrumentsButtons
        // 
        this._instrumentsButtons.AutoSize = true;
        this._instrumentsButtons.Controls.Add(this._addInstrumentButton);
        this._instrumentsButtons.Controls.Add(this._removeInstrumentButton);
        this._instrumentsButtons.Controls.Add(this._importSymbolsButton);
        this._instrumentsButtons.Dock = System.Windows.Forms.DockStyle.Top;
        this._instrumentsButtons.Location = new System.Drawing.Point(12, 12);
        this._instrumentsButtons.Name = "_instrumentsButtons";
        this._instrumentsButtons.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
        this._instrumentsButtons.Size = new System.Drawing.Size(868, 37);
        this._instrumentsButtons.TabIndex = 0;
        this._instrumentsButtons.WrapContents = false;
        // 
        // _addInstrumentButton
        // 
        this._addInstrumentButton.AutoSize = true;
        this._addInstrumentButton.Name = "_addInstrumentButton";
        this._addInstrumentButton.Size = new System.Drawing.Size(100, 25);
        this._addInstrumentButton.TabIndex = 0;
        this._addInstrumentButton.Text = "Aggiungi riga";
        this._addInstrumentButton.UseVisualStyleBackColor = true;
        this._addInstrumentButton.Click += new System.EventHandler(this.OnAddInstrumentClick);
        // 
        // _removeInstrumentButton
        // 
        this._removeInstrumentButton.AutoSize = true;
        this._removeInstrumentButton.Name = "_removeInstrumentButton";
        this._removeInstrumentButton.Size = new System.Drawing.Size(100, 25);
        this._removeInstrumentButton.TabIndex = 1;
        this._removeInstrumentButton.Text = "Rimuovi riga";
        this._removeInstrumentButton.UseVisualStyleBackColor = true;
        this._removeInstrumentButton.Click += new System.EventHandler(this.OnRemoveInstrumentClick);
        // 
        // _importSymbolsButton
        // 
        this._importSymbolsButton.AutoSize = true;
        this._importSymbolsButton.Margin = new System.Windows.Forms.Padding(24, 3, 3, 3);
        this._importSymbolsButton.Name = "_importSymbolsButton";
        this._importSymbolsButton.Size = new System.Drawing.Size(200, 25);
        this._importSymbolsButton.TabIndex = 2;
        this._importSymbolsButton.Text = "Importa simboli dal masterfilter";
        this._importSymbolsButton.UseVisualStyleBackColor = true;
        this._importSymbolsButton.Click += new System.EventHandler(this.OnImportSymbolsClick);
        // 
        // _sizingTab
        // 
        this._sizingTab.AutoScroll = true;
        this._sizingTab.Controls.Add(this._sizingLayout);
        this._sizingTab.Location = new System.Drawing.Point(4, 27);
        this._sizingTab.Name = "_sizingTab";
        this._sizingTab.Padding = new System.Windows.Forms.Padding(12);
        this._sizingTab.Size = new System.Drawing.Size(892, 525);
        this._sizingTab.TabIndex = 3;
        this._sizingTab.Text = "Sizing";
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
        this._sizingLayout.Controls.Add(this._clampMultipliersCheckBox, 0, 0);
        this._sizingLayout.Controls.Add(this._volatilityEnabledCheckBox, 0, 1);
        this._sizingLayout.Controls.Add(this._atrPeriodsLabel, 0, 2);
        this._sizingLayout.Controls.Add(this._atrPeriodsInput, 1, 2);
        this._sizingLayout.Controls.Add(this._targetRiskLabel, 2, 2);
        this._sizingLayout.Controls.Add(this._targetRiskInput, 3, 2);
        this._sizingLayout.Controls.Add(this._portfolioRiskEnabledCheckBox, 0, 3);
        this._sizingLayout.Controls.Add(this._maxDrawdownLabel, 0, 4);
        this._sizingLayout.Controls.Add(this._maxDrawdownInput, 1, 4);
        this._sizingLayout.Controls.Add(this._maxGrossExposureLabel, 2, 4);
        this._sizingLayout.Controls.Add(this._maxGrossExposureInput, 3, 4);
        this._sizingLayout.Controls.Add(this._cppiEnabledCheckBox, 0, 5);
        this._sizingLayout.Controls.Add(this._cppiFloorLabel, 0, 6);
        this._sizingLayout.Controls.Add(this._cppiFloorInput, 1, 6);
        this._sizingLayout.Controls.Add(this._cppiMultiplierLabel, 2, 6);
        this._sizingLayout.Controls.Add(this._cppiMultiplierInput, 3, 6);
        this._sizingLayout.Controls.Add(this._aggressiveModulesCheckBox, 0, 7);
        this._sizingLayout.Controls.Add(this._fractionalFactorLabel, 0, 8);
        this._sizingLayout.Controls.Add(this._fractionalFactorInput, 1, 8);
        this._sizingLayout.Controls.Add(this._maximumMultiplierLabel, 2, 8);
        this._sizingLayout.Controls.Add(this._maximumMultiplierInput, 3, 8);
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
        this._sizingLayout.SetColumnSpan(this._clampMultipliersCheckBox, 4);
        this._sizingLayout.SetColumnSpan(this._volatilityEnabledCheckBox, 4);
        this._sizingLayout.SetColumnSpan(this._portfolioRiskEnabledCheckBox, 4);
        this._sizingLayout.SetColumnSpan(this._cppiEnabledCheckBox, 4);
        this._sizingLayout.SetColumnSpan(this._aggressiveModulesCheckBox, 4);
        this._sizingLayout.Size = new System.Drawing.Size(868, 300);
        this._sizingLayout.TabIndex = 0;
        // 
        // _clampMultipliersCheckBox
        // 
        this._clampMultipliersCheckBox.AutoSize = true;
        this._clampMultipliersCheckBox.Checked = true;
        this._clampMultipliersCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this._clampMultipliersCheckBox.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
        this._clampMultipliersCheckBox.Name = "_clampMultipliersCheckBox";
        this._clampMultipliersCheckBox.Size = new System.Drawing.Size(400, 19);
        this._clampMultipliersCheckBox.TabIndex = 0;
        this._clampMultipliersCheckBox.Text = "Limita i moltiplicatori all'intervallo [0, 1]";
        this._clampMultipliersCheckBox.UseVisualStyleBackColor = true;
        this._clampMultipliersCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _volatilityEnabledCheckBox
        // 
        this._volatilityEnabledCheckBox.AutoSize = true;
        this._volatilityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 8, 3, 4);
        this._volatilityEnabledCheckBox.Name = "_volatilityEnabledCheckBox";
        this._volatilityEnabledCheckBox.Size = new System.Drawing.Size(400, 19);
        this._volatilityEnabledCheckBox.TabIndex = 1;
        this._volatilityEnabledCheckBox.Text = "Sizing su volatilità di mercato (ATR)";
        this._volatilityEnabledCheckBox.UseVisualStyleBackColor = true;
        this._volatilityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _atrPeriodsLabel
        // 
        this._atrPeriodsLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._atrPeriodsLabel.AutoSize = true;
        this._atrPeriodsLabel.Margin = new System.Windows.Forms.Padding(22, 0, 8, 0);
        this._atrPeriodsLabel.Name = "_atrPeriodsLabel";
        this._atrPeriodsLabel.Size = new System.Drawing.Size(90, 15);
        this._atrPeriodsLabel.TabIndex = 2;
        this._atrPeriodsLabel.Text = "Periodi ATR";
        // 
        // _atrPeriodsInput
        // 
        this._atrPeriodsInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._atrPeriodsInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._atrPeriodsInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._atrPeriodsInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._atrPeriodsInput.Name = "_atrPeriodsInput";
        this._atrPeriodsInput.Size = new System.Drawing.Size(120, 23);
        this._atrPeriodsInput.TabIndex = 3;
        this._atrPeriodsInput.Value = new decimal(new int[] { 14, 0, 0, 0 });
        this._atrPeriodsInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _targetRiskLabel
        // 
        this._targetRiskLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._targetRiskLabel.AutoSize = true;
        this._targetRiskLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._targetRiskLabel.Name = "_targetRiskLabel";
        this._targetRiskLabel.Size = new System.Drawing.Size(150, 15);
        this._targetRiskLabel.TabIndex = 4;
        this._targetRiskLabel.Text = "Rischio obiettivo (dollari)";
        // 
        // _targetRiskInput
        // 
        this._targetRiskInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._targetRiskInput.DecimalPlaces = 2;
        this._targetRiskInput.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        this._targetRiskInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._targetRiskInput.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
        this._targetRiskInput.Name = "_targetRiskInput";
        this._targetRiskInput.Size = new System.Drawing.Size(140, 23);
        this._targetRiskInput.TabIndex = 5;
        this._targetRiskInput.ThousandsSeparator = true;
        this._targetRiskInput.Value = new decimal(new int[] { 1000, 0, 0, 0 });
        this._targetRiskInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _portfolioRiskEnabledCheckBox
        // 
        this._portfolioRiskEnabledCheckBox.AutoSize = true;
        this._portfolioRiskEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 12, 3, 4);
        this._portfolioRiskEnabledCheckBox.Name = "_portfolioRiskEnabledCheckBox";
        this._portfolioRiskEnabledCheckBox.Size = new System.Drawing.Size(400, 19);
        this._portfolioRiskEnabledCheckBox.TabIndex = 6;
        this._portfolioRiskEnabledCheckBox.Text = "Controllo del rischio di portafoglio";
        this._portfolioRiskEnabledCheckBox.UseVisualStyleBackColor = true;
        this._portfolioRiskEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _maxDrawdownLabel
        // 
        this._maxDrawdownLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxDrawdownLabel.AutoSize = true;
        this._maxDrawdownLabel.Margin = new System.Windows.Forms.Padding(22, 0, 8, 0);
        this._maxDrawdownLabel.Name = "_maxDrawdownLabel";
        this._maxDrawdownLabel.Size = new System.Drawing.Size(130, 15);
        this._maxDrawdownLabel.TabIndex = 7;
        this._maxDrawdownLabel.Text = "Drawdown massimo";
        // 
        // _maxDrawdownInput
        // 
        this._maxDrawdownInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxDrawdownInput.DecimalPlaces = 4;
        this._maxDrawdownInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._maxDrawdownInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._maxDrawdownInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._maxDrawdownInput.Name = "_maxDrawdownInput";
        this._maxDrawdownInput.Size = new System.Drawing.Size(120, 23);
        this._maxDrawdownInput.TabIndex = 8;
        this._maxDrawdownInput.Value = new decimal(new int[] { 20, 0, 0, 131072 });
        this._maxDrawdownInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _maxGrossExposureLabel
        // 
        this._maxGrossExposureLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxGrossExposureLabel.AutoSize = true;
        this._maxGrossExposureLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maxGrossExposureLabel.Name = "_maxGrossExposureLabel";
        this._maxGrossExposureLabel.Size = new System.Drawing.Size(150, 15);
        this._maxGrossExposureLabel.TabIndex = 9;
        this._maxGrossExposureLabel.Text = "Esposizione lorda massima";
        // 
        // _maxGrossExposureInput
        // 
        this._maxGrossExposureInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maxGrossExposureInput.DecimalPlaces = 4;
        this._maxGrossExposureInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._maxGrossExposureInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maxGrossExposureInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._maxGrossExposureInput.Name = "_maxGrossExposureInput";
        this._maxGrossExposureInput.Size = new System.Drawing.Size(140, 23);
        this._maxGrossExposureInput.TabIndex = 10;
        this._maxGrossExposureInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        this._maxGrossExposureInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _cppiEnabledCheckBox
        // 
        this._cppiEnabledCheckBox.AutoSize = true;
        this._cppiEnabledCheckBox.Margin = new System.Windows.Forms.Padding(22, 8, 3, 4);
        this._cppiEnabledCheckBox.Name = "_cppiEnabledCheckBox";
        this._cppiEnabledCheckBox.Size = new System.Drawing.Size(400, 19);
        this._cppiEnabledCheckBox.TabIndex = 11;
        this._cppiEnabledCheckBox.Text = "Abilita CPPI";
        this._cppiEnabledCheckBox.UseVisualStyleBackColor = true;
        this._cppiEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _cppiFloorLabel
        // 
        this._cppiFloorLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cppiFloorLabel.AutoSize = true;
        this._cppiFloorLabel.Margin = new System.Windows.Forms.Padding(40, 0, 8, 0);
        this._cppiFloorLabel.Name = "_cppiFloorLabel";
        this._cppiFloorLabel.Size = new System.Drawing.Size(110, 15);
        this._cppiFloorLabel.TabIndex = 12;
        this._cppiFloorLabel.Text = "Frazione di floor";
        // 
        // _cppiFloorInput
        // 
        this._cppiFloorInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cppiFloorInput.DecimalPlaces = 4;
        this._cppiFloorInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._cppiFloorInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._cppiFloorInput.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        this._cppiFloorInput.Name = "_cppiFloorInput";
        this._cppiFloorInput.Size = new System.Drawing.Size(120, 23);
        this._cppiFloorInput.TabIndex = 13;
        this._cppiFloorInput.Value = new decimal(new int[] { 80, 0, 0, 131072 });
        this._cppiFloorInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _cppiMultiplierLabel
        // 
        this._cppiMultiplierLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cppiMultiplierLabel.AutoSize = true;
        this._cppiMultiplierLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._cppiMultiplierLabel.Name = "_cppiMultiplierLabel";
        this._cppiMultiplierLabel.Size = new System.Drawing.Size(150, 15);
        this._cppiMultiplierLabel.TabIndex = 14;
        this._cppiMultiplierLabel.Text = "Moltiplicatore CPPI";
        // 
        // _cppiMultiplierInput
        // 
        this._cppiMultiplierInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._cppiMultiplierInput.DecimalPlaces = 4;
        this._cppiMultiplierInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._cppiMultiplierInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._cppiMultiplierInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._cppiMultiplierInput.Name = "_cppiMultiplierInput";
        this._cppiMultiplierInput.Size = new System.Drawing.Size(140, 23);
        this._cppiMultiplierInput.TabIndex = 15;
        this._cppiMultiplierInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        this._cppiMultiplierInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _aggressiveModulesCheckBox
        // 
        this._aggressiveModulesCheckBox.AutoSize = true;
        this._aggressiveModulesCheckBox.Margin = new System.Windows.Forms.Padding(22, 8, 3, 4);
        this._aggressiveModulesCheckBox.Name = "_aggressiveModulesCheckBox";
        this._aggressiveModulesCheckBox.Size = new System.Drawing.Size(400, 19);
        this._aggressiveModulesCheckBox.TabIndex = 16;
        this._aggressiveModulesCheckBox.Text = "Abilita i moduli aggressivi";
        this._aggressiveModulesCheckBox.UseVisualStyleBackColor = true;
        this._aggressiveModulesCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _fractionalFactorLabel
        // 
        this._fractionalFactorLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._fractionalFactorLabel.AutoSize = true;
        this._fractionalFactorLabel.Margin = new System.Windows.Forms.Padding(40, 0, 8, 0);
        this._fractionalFactorLabel.Name = "_fractionalFactorLabel";
        this._fractionalFactorLabel.Size = new System.Drawing.Size(110, 15);
        this._fractionalFactorLabel.TabIndex = 17;
        this._fractionalFactorLabel.Text = "Fattore frazionale";
        // 
        // _fractionalFactorInput
        // 
        this._fractionalFactorInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._fractionalFactorInput.DecimalPlaces = 4;
        this._fractionalFactorInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._fractionalFactorInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._fractionalFactorInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._fractionalFactorInput.Name = "_fractionalFactorInput";
        this._fractionalFactorInput.Size = new System.Drawing.Size(120, 23);
        this._fractionalFactorInput.TabIndex = 18;
        this._fractionalFactorInput.Value = new decimal(new int[] { 25, 0, 0, 131072 });
        this._fractionalFactorInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _maximumMultiplierLabel
        // 
        this._maximumMultiplierLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumMultiplierLabel.AutoSize = true;
        this._maximumMultiplierLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._maximumMultiplierLabel.Name = "_maximumMultiplierLabel";
        this._maximumMultiplierLabel.Size = new System.Drawing.Size(150, 15);
        this._maximumMultiplierLabel.TabIndex = 19;
        this._maximumMultiplierLabel.Text = "Moltiplicatore massimo";
        // 
        // _maximumMultiplierInput
        // 
        this._maximumMultiplierInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._maximumMultiplierInput.DecimalPlaces = 4;
        this._maximumMultiplierInput.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        this._maximumMultiplierInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._maximumMultiplierInput.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this._maximumMultiplierInput.Name = "_maximumMultiplierInput";
        this._maximumMultiplierInput.Size = new System.Drawing.Size(140, 23);
        this._maximumMultiplierInput.TabIndex = 20;
        this._maximumMultiplierInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        this._maximumMultiplierInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // PlanDetailScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._toolbar);
        this.Name = "PlanDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._groupsBindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._instrumentsBindingSource)).EndInit();
        this._tabs.ResumeLayout(false);
        this._generalTab.ResumeLayout(false);
        this._generalTab.PerformLayout();
        this._generalLayout.ResumeLayout(false);
        this._generalLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._maxConcurrentInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._initialCapitalInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).EndInit();
        this._groupsTab.ResumeLayout(false);
        this._groupsTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).EndInit();
        this._groupsButtons.ResumeLayout(false);
        this._groupsButtons.PerformLayout();
        this._instrumentsTab.ResumeLayout(false);
        this._instrumentsTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._instrumentsGrid)).EndInit();
        this._instrumentsButtons.ResumeLayout(false);
        this._instrumentsButtons.PerformLayout();
        this._sizingTab.ResumeLayout(false);
        this._sizingTab.PerformLayout();
        this._sizingLayout.ResumeLayout(false);
        this._sizingLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._atrPeriodsInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._targetRiskInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxDrawdownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxGrossExposureInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._cppiFloorInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._cppiMultiplierInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._fractionalFactorInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumMultiplierInput)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _groupsBindingSource;
    private System.Windows.Forms.BindingSource _instrumentsBindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _generalTab;
    private System.Windows.Forms.TableLayoutPanel _generalLayout;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.ComboBox _workspaceCombo;
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeTextBox;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _maxConcurrentLabel;
    private System.Windows.Forms.NumericUpDown _maxConcurrentInput;
    private System.Windows.Forms.Label _initialCapitalLabel;
    private System.Windows.Forms.NumericUpDown _initialCapitalInput;
    private System.Windows.Forms.Label _commissionLabel;
    private System.Windows.Forms.NumericUpDown _commissionInput;
    private System.Windows.Forms.Label _enforceConcurrencyLabel;
    private System.Windows.Forms.ComboBox _enforceConcurrencyCombo;
    private System.Windows.Forms.CheckBox _applyTitanoCheckBox;
    private System.Windows.Forms.Label _rotationSetupLabel;
    private System.Windows.Forms.ComboBox _rotationSetupCombo;
    private System.Windows.Forms.Label _titanoRunLabel;
    private System.Windows.Forms.Label _rotationStatusLabel;
    private System.Windows.Forms.Label _titanoFolderLabel;
    private System.Windows.Forms.ComboBox _titanoFolderCombo;
    private System.Windows.Forms.TabPage _groupsTab;
    private System.Windows.Forms.DataGridView _groupsGrid;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupId;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colAccountNumber;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroupMaxConcurrent;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupRotationSetup;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupTitanoFolder;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colGroupApplyTitano;
    private System.Windows.Forms.FlowLayoutPanel _groupsButtons;
    private System.Windows.Forms.Button _addGroupButton;
    private System.Windows.Forms.Button _removeGroupButton;
    private System.Windows.Forms.TabPage _instrumentsTab;
    private System.Windows.Forms.DataGridView _instrumentsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colDollarsPerPoint;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMinimumQuantity;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colQuantityStep;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colRoundingMode;
    private System.Windows.Forms.FlowLayoutPanel _instrumentsButtons;
    private System.Windows.Forms.Button _addInstrumentButton;
    private System.Windows.Forms.Button _removeInstrumentButton;
    private System.Windows.Forms.Button _importSymbolsButton;
    private System.Windows.Forms.TabPage _sizingTab;
    private System.Windows.Forms.TableLayoutPanel _sizingLayout;
    private System.Windows.Forms.CheckBox _clampMultipliersCheckBox;
    private System.Windows.Forms.CheckBox _volatilityEnabledCheckBox;
    private System.Windows.Forms.Label _atrPeriodsLabel;
    private System.Windows.Forms.NumericUpDown _atrPeriodsInput;
    private System.Windows.Forms.Label _targetRiskLabel;
    private System.Windows.Forms.NumericUpDown _targetRiskInput;
    private System.Windows.Forms.CheckBox _portfolioRiskEnabledCheckBox;
    private System.Windows.Forms.Label _maxDrawdownLabel;
    private System.Windows.Forms.NumericUpDown _maxDrawdownInput;
    private System.Windows.Forms.Label _maxGrossExposureLabel;
    private System.Windows.Forms.NumericUpDown _maxGrossExposureInput;
    private System.Windows.Forms.CheckBox _cppiEnabledCheckBox;
    private System.Windows.Forms.Label _cppiFloorLabel;
    private System.Windows.Forms.NumericUpDown _cppiFloorInput;
    private System.Windows.Forms.Label _cppiMultiplierLabel;
    private System.Windows.Forms.NumericUpDown _cppiMultiplierInput;
    private System.Windows.Forms.CheckBox _aggressiveModulesCheckBox;
    private System.Windows.Forms.Label _fractionalFactorLabel;
    private System.Windows.Forms.NumericUpDown _fractionalFactorInput;
    private System.Windows.Forms.Label _maximumMultiplierLabel;
    private System.Windows.Forms.NumericUpDown _maximumMultiplierInput;
}
