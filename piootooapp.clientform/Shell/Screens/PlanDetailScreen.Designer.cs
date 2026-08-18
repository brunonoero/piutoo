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
        this._accountsBindingSource = new System.Windows.Forms.BindingSource(this.components);
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
        this._commissionLabel = new System.Windows.Forms.Label();
        this._commissionInput = new System.Windows.Forms.NumericUpDown();
        this._enforceConcurrencyLabel = new System.Windows.Forms.Label();
        this._enforceConcurrencyCombo = new System.Windows.Forms.ComboBox();
        this._titanoRunLabel = new System.Windows.Forms.Label();
        this._rotationStatusLabel = new System.Windows.Forms.Label();
        this._groupsTab = new System.Windows.Forms.TabPage();
        this._groupsGrid = new System.Windows.Forms.DataGridView();
        this._colGroupId = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colGroupRotationSetup = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colGroupTitanoFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colGroupTitanoPick = new System.Windows.Forms.DataGridViewButtonColumn();
        this._colGroupApplyTitano = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._groupsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._addGroupButton = new System.Windows.Forms.Button();
        this._removeGroupButton = new System.Windows.Forms.Button();
        this._accountsTab = new System.Windows.Forms.TabPage();
        this._accountsGrid = new System.Windows.Forms.DataGridView();
        this._colAccountNumber = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colAccountGroupId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountMaxConcurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountCountMode = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._accountsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._addAccountButton = new System.Windows.Forms.Button();
        this._removeAccountButton = new System.Windows.Forms.Button();
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
        this._aggressiveModulesCheckBox = new System.Windows.Forms.CheckBox();
        this._fractionalFactorLabel = new System.Windows.Forms.Label();
        this._fractionalFactorInput = new System.Windows.Forms.NumericUpDown();
        this._maximumMultiplierLabel = new System.Windows.Forms.Label();
        this._maximumMultiplierInput = new System.Windows.Forms.NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)(this._groupsBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._accountsBindingSource)).BeginInit();
        this._tabs.SuspendLayout();
        this._generalTab.SuspendLayout();
        this._generalLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).BeginInit();
        this._groupsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).BeginInit();
        this._groupsButtons.SuspendLayout();
        this._accountsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._accountsGrid)).BeginInit();
        this._accountsButtons.SuspendLayout();
        this._sizingTab.SuspendLayout();
        this._sizingLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._atrPeriodsInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._targetRiskInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxDrawdownInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxGrossExposureInput)).BeginInit();
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
        this._tabs.Controls.Add(this._accountsTab);
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
        this._generalLayout.Controls.Add(this._commissionLabel, 2, 1);
        this._generalLayout.Controls.Add(this._commissionInput, 3, 1);
        this._generalLayout.Controls.Add(this._enforceConcurrencyLabel, 0, 2);
        this._generalLayout.Controls.Add(this._enforceConcurrencyCombo, 1, 2);
        this._generalLayout.Controls.Add(this._titanoRunLabel, 2, 2);
        this._generalLayout.Controls.Add(this._rotationStatusLabel, 3, 2);
        this._generalLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._generalLayout.Location = new System.Drawing.Point(12, 12);
        this._generalLayout.Name = "_generalLayout";
        this._generalLayout.RowCount = 3;
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._generalLayout.Size = new System.Drawing.Size(868, 110);
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
        // _commissionLabel
        //
        this._commissionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionLabel.AutoSize = true;
        this._commissionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._commissionLabel.Name = "_commissionLabel";
        this._commissionLabel.Size = new System.Drawing.Size(170, 15);
        this._commissionLabel.TabIndex = 6;
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
        this._commissionInput.TabIndex = 7;
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
        this._enforceConcurrencyLabel.TabIndex = 8;
        this._enforceConcurrencyLabel.Text = "Limiti di concorrenza";
        //
        // _enforceConcurrencyCombo
        //
        this._enforceConcurrencyCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._enforceConcurrencyCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._enforceConcurrencyCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._enforceConcurrencyCombo.Name = "_enforceConcurrencyCombo";
        this._enforceConcurrencyCombo.Size = new System.Drawing.Size(280, 23);
        this._enforceConcurrencyCombo.TabIndex = 9;
        this._enforceConcurrencyCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _titanoRunLabel
        //
        this._titanoRunLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._titanoRunLabel.AutoSize = true;
        this._titanoRunLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._titanoRunLabel.Name = "_titanoRunLabel";
        this._titanoRunLabel.Size = new System.Drawing.Size(90, 15);
        this._titanoRunLabel.TabIndex = 10;
        this._titanoRunLabel.Text = "Stato rotazione";
        //
        // _rotationStatusLabel
        //
        this._rotationStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._rotationStatusLabel.AutoSize = true;
        this._rotationStatusLabel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._rotationStatusLabel.Name = "_rotationStatusLabel";
        this._rotationStatusLabel.Size = new System.Drawing.Size(280, 15);
        this._rotationStatusLabel.TabIndex = 11;
        this._rotationStatusLabel.Text = "—";
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
        this._groupsTab.Text = "Gruppi";
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
            this._colGroupRotationSetup,
            this._colGroupTitanoFolder,
            this._colGroupTitanoPick,
            this._colGroupApplyTitano});
        this._groupsGrid.DataSource = this._groupsBindingSource;
        this._groupsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._groupsGrid.Location = new System.Drawing.Point(12, 49);
        this._groupsGrid.Name = "_groupsGrid";
        this._groupsGrid.RowHeadersVisible = false;
        this._groupsGrid.Size = new System.Drawing.Size(868, 464);
        this._groupsGrid.TabIndex = 1;
        this._groupsGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGroupsGridCellClick);
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
        this._colGroupTitanoFolder.HeaderText = "Cartella backtest";
        this._colGroupTitanoFolder.Name = "_colGroupTitanoFolder";
        this._colGroupTitanoFolder.ReadOnly = true;
        //
        // _colGroupTitanoPick
        //
        this._colGroupTitanoPick.FillWeight = 30F;
        this._colGroupTitanoPick.HeaderText = "";
        this._colGroupTitanoPick.Name = "_colGroupTitanoPick";
        this._colGroupTitanoPick.Text = "Scegli…";
        this._colGroupTitanoPick.UseColumnTextForButtonValue = true;
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
        // _accountsTab
        //
        this._accountsTab.Controls.Add(this._accountsGrid);
        this._accountsTab.Controls.Add(this._accountsButtons);
        this._accountsTab.Location = new System.Drawing.Point(4, 27);
        this._accountsTab.Name = "_accountsTab";
        this._accountsTab.Padding = new System.Windows.Forms.Padding(12);
        this._accountsTab.Size = new System.Drawing.Size(892, 525);
        this._accountsTab.TabIndex = 2;
        this._accountsTab.Text = "Account";
        this._accountsTab.UseVisualStyleBackColor = true;
        //
        // _accountsGrid
        //
        this._accountsGrid.AllowUserToAddRows = false;
        this._accountsGrid.AllowUserToDeleteRows = false;
        this._accountsGrid.AutoGenerateColumns = false;
        this._accountsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._accountsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._accountsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._accountsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._accountsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colAccountNumber,
            this._colAccountGroupId,
            this._colAccountMaxConcurrent,
            this._colAccountCountMode});
        this._accountsGrid.DataSource = this._accountsBindingSource;
        this._accountsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._accountsGrid.Location = new System.Drawing.Point(12, 49);
        this._accountsGrid.Name = "_accountsGrid";
        this._accountsGrid.RowHeadersVisible = false;
        this._accountsGrid.Size = new System.Drawing.Size(868, 464);
        this._accountsGrid.TabIndex = 1;
        this._accountsGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnAccountsGridCellValueChanged);
        this._accountsGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.OnAccountsGridCurrentCellDirtyStateChanged);
        this._accountsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnAccountsGridDataError);
        //
        // _colAccountNumber
        //
        this._colAccountNumber.DataPropertyName = "AccountNumber";
        this._colAccountNumber.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colAccountNumber.HeaderText = "Account";
        this._colAccountNumber.Name = "_colAccountNumber";
        //
        // _colAccountGroupId
        //
        this._colAccountGroupId.DataPropertyName = "GroupId";
        this._colAccountGroupId.FillWeight = 60F;
        this._colAccountGroupId.HeaderText = "Gruppo";
        this._colAccountGroupId.Name = "_colAccountGroupId";
        this._colAccountGroupId.ReadOnly = true;
        //
        // _colAccountMaxConcurrent
        //
        this._colAccountMaxConcurrent.DataPropertyName = "MaxConcurrentTrades";
        this._colAccountMaxConcurrent.FillWeight = 70F;
        this._colAccountMaxConcurrent.HeaderText = "Max posizioni contemporanee (0 = illimitate)";
        this._colAccountMaxConcurrent.Name = "_colAccountMaxConcurrent";
        //
        // _colAccountCountMode
        //
        this._colAccountCountMode.DataPropertyName = "ConcurrencyCountMode";
        this._colAccountCountMode.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.DropDownButton;
        this._colAccountCountMode.FillWeight = 80F;
        this._colAccountCountMode.HeaderText = "Il massimo conta";
        this._colAccountCountMode.Name = "_colAccountCountMode";
        //
        // _accountsButtons
        //
        this._accountsButtons.AutoSize = true;
        this._accountsButtons.Controls.Add(this._addAccountButton);
        this._accountsButtons.Controls.Add(this._removeAccountButton);
        this._accountsButtons.Dock = System.Windows.Forms.DockStyle.Top;
        this._accountsButtons.Location = new System.Drawing.Point(12, 12);
        this._accountsButtons.Name = "_accountsButtons";
        this._accountsButtons.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
        this._accountsButtons.Size = new System.Drawing.Size(868, 37);
        this._accountsButtons.TabIndex = 0;
        this._accountsButtons.WrapContents = false;
        //
        // _addAccountButton
        //
        this._addAccountButton.AutoSize = true;
        this._addAccountButton.Name = "_addAccountButton";
        this._addAccountButton.Size = new System.Drawing.Size(100, 25);
        this._addAccountButton.TabIndex = 0;
        this._addAccountButton.Text = "Aggiungi riga";
        this._addAccountButton.UseVisualStyleBackColor = true;
        this._addAccountButton.Click += new System.EventHandler(this.OnAddAccountClick);
        //
        // _removeAccountButton
        //
        this._removeAccountButton.AutoSize = true;
        this._removeAccountButton.Name = "_removeAccountButton";
        this._removeAccountButton.Size = new System.Drawing.Size(100, 25);
        this._removeAccountButton.TabIndex = 1;
        this._removeAccountButton.Text = "Rimuovi riga";
        this._removeAccountButton.UseVisualStyleBackColor = true;
        this._removeAccountButton.Click += new System.EventHandler(this.OnRemoveAccountClick);
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
        this._sizingLayout.Controls.Add(this._aggressiveModulesCheckBox, 0, 5);
        this._sizingLayout.Controls.Add(this._fractionalFactorLabel, 0, 6);
        this._sizingLayout.Controls.Add(this._fractionalFactorInput, 1, 6);
        this._sizingLayout.Controls.Add(this._maximumMultiplierLabel, 2, 6);
        this._sizingLayout.Controls.Add(this._maximumMultiplierInput, 3, 6);
        this._sizingLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._sizingLayout.Location = new System.Drawing.Point(12, 12);
        this._sizingLayout.Name = "_sizingLayout";
        this._sizingLayout.RowCount = 7;
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
        ((System.ComponentModel.ISupportInitialize)(this._accountsBindingSource)).EndInit();
        this._tabs.ResumeLayout(false);
        this._generalTab.ResumeLayout(false);
        this._generalTab.PerformLayout();
        this._generalLayout.ResumeLayout(false);
        this._generalLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).EndInit();
        this._groupsTab.ResumeLayout(false);
        this._groupsTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).EndInit();
        this._groupsButtons.ResumeLayout(false);
        this._groupsButtons.PerformLayout();
        this._accountsTab.ResumeLayout(false);
        this._accountsTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._accountsGrid)).EndInit();
        this._accountsButtons.ResumeLayout(false);
        this._accountsButtons.PerformLayout();
        this._sizingTab.ResumeLayout(false);
        this._sizingTab.PerformLayout();
        this._sizingLayout.ResumeLayout(false);
        this._sizingLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._atrPeriodsInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._targetRiskInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxDrawdownInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maxGrossExposureInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._fractionalFactorInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._maximumMultiplierInput)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _groupsBindingSource;
    private System.Windows.Forms.BindingSource _accountsBindingSource;
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
    private System.Windows.Forms.Label _commissionLabel;
    private System.Windows.Forms.NumericUpDown _commissionInput;
    private System.Windows.Forms.Label _enforceConcurrencyLabel;
    private System.Windows.Forms.ComboBox _enforceConcurrencyCombo;
    private System.Windows.Forms.Label _titanoRunLabel;
    private System.Windows.Forms.Label _rotationStatusLabel;
    private System.Windows.Forms.TabPage _groupsTab;
    private System.Windows.Forms.DataGridView _groupsGrid;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupId;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupRotationSetup;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroupTitanoFolder;
    private System.Windows.Forms.DataGridViewButtonColumn _colGroupTitanoPick;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colGroupApplyTitano;
    private System.Windows.Forms.FlowLayoutPanel _groupsButtons;
    private System.Windows.Forms.Button _addGroupButton;
    private System.Windows.Forms.Button _removeGroupButton;
    private System.Windows.Forms.TabPage _accountsTab;
    private System.Windows.Forms.DataGridView _accountsGrid;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colAccountNumber;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountGroupId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountMaxConcurrent;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colAccountCountMode;
    private System.Windows.Forms.FlowLayoutPanel _accountsButtons;
    private System.Windows.Forms.Button _addAccountButton;
    private System.Windows.Forms.Button _removeAccountButton;
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
    private System.Windows.Forms.CheckBox _aggressiveModulesCheckBox;
    private System.Windows.Forms.Label _fractionalFactorLabel;
    private System.Windows.Forms.NumericUpDown _fractionalFactorInput;
    private System.Windows.Forms.Label _maximumMultiplierLabel;
    private System.Windows.Forms.NumericUpDown _maximumMultiplierInput;
}
