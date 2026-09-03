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
        this._workspaceValueLabel = new System.Windows.Forms.Label();
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeTextBox = new System.Windows.Forms.TextBox();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._commissionLabel = new System.Windows.Forms.Label();
        this._commissionInput = new System.Windows.Forms.NumericUpDown();
        this._enforceConcurrencyLabel = new System.Windows.Forms.Label();
        this._enforceConcurrencyCombo = new System.Windows.Forms.ComboBox();
        this._sizeMultiplierLabel = new System.Windows.Forms.Label();
        this._sizeMultiplierInput = new System.Windows.Forms.NumericUpDown();
        this._groupsTab = new System.Windows.Forms.TabPage();
        this._groupsGrid = new System.Windows.Forms.DataGridView();
        this._colGroupId = new System.Windows.Forms.DataGridViewComboBoxColumn();
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
        this._holdingTab = new System.Windows.Forms.TabPage();
        this._holdingLayout = new System.Windows.Forms.TableLayoutPanel();
        this._forceNightCloseCheckBox = new System.Windows.Forms.CheckBox();
        this._sessionFlatLabel = new System.Windows.Forms.Label();
        this._sessionFlatInput = new System.Windows.Forms.NumericUpDown();
        this._forceWeekCloseCheckBox = new System.Windows.Forms.CheckBox();
        this._weekEndFromLabel = new System.Windows.Forms.Label();
        this._weekEndFromInput = new System.Windows.Forms.NumericUpDown();
        this._weekEndUntilLabel = new System.Windows.Forms.Label();
        this._weekEndUntilInput = new System.Windows.Forms.NumericUpDown();
        this._holdingWarningLabel = new System.Windows.Forms.Label();
        this._conflictsBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._conflictsGrid = new System.Windows.Forms.DataGridView();
        this._colConflictStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colConflictSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colConflictTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colConflictHolding = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colConflictEffect = new System.Windows.Forms.DataGridViewTextBoxColumn();
        ((System.ComponentModel.ISupportInitialize)(this._conflictsBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._conflictsGrid)).BeginInit();
        this._sessionFlatInput.BeginInit();
        this._weekEndFromInput.BeginInit();
        this._weekEndUntilInput.BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._accountsBindingSource)).BeginInit();
        this._tabs.SuspendLayout();
        this._generalTab.SuspendLayout();
        this._generalLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._sizeMultiplierInput)).BeginInit();
        this._groupsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).BeginInit();
        this._groupsButtons.SuspendLayout();
        this._accountsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._accountsGrid)).BeginInit();
        this._accountsButtons.SuspendLayout();
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
        this._tabs.Controls.Add(this._holdingTab);
        this._tabs.Controls.Add(this._groupsTab);
        this._tabs.Controls.Add(this._accountsTab);
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
        this._generalLayout.Controls.Add(this._workspaceValueLabel, 1, 0);
        this._generalLayout.Controls.Add(this._codeLabel, 2, 0);
        this._generalLayout.Controls.Add(this._codeTextBox, 3, 0);
        this._generalLayout.Controls.Add(this._nameLabel, 0, 1);
        this._generalLayout.Controls.Add(this._nameTextBox, 1, 1);
        this._generalLayout.Controls.Add(this._commissionLabel, 2, 1);
        this._generalLayout.Controls.Add(this._commissionInput, 3, 1);
        this._generalLayout.Controls.Add(this._enforceConcurrencyLabel, 0, 2);
        this._generalLayout.Controls.Add(this._enforceConcurrencyCombo, 1, 2);
        this._generalLayout.Controls.Add(this._sizeMultiplierLabel, 2, 2);
        this._generalLayout.Controls.Add(this._sizeMultiplierInput, 3, 2);
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
        // _workspaceValueLabel
        //
        this._workspaceValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._workspaceValueLabel.AutoSize = true;
        this._workspaceValueLabel.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._workspaceValueLabel.Name = "_workspaceValueLabel";
        this._workspaceValueLabel.Size = new System.Drawing.Size(280, 15);
        this._workspaceValueLabel.TabIndex = 1;
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
        // _sizeMultiplierLabel
        //
        this._sizeMultiplierLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sizeMultiplierLabel.AutoSize = true;
        this._sizeMultiplierLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._sizeMultiplierLabel.Name = "_sizeMultiplierLabel";
        this._sizeMultiplierLabel.Size = new System.Drawing.Size(140, 15);
        this._sizeMultiplierLabel.TabIndex = 10;
        this._sizeMultiplierLabel.Text = "Moltiplicatore size";
        //
        // _sizeMultiplierInput
        //
        this._sizeMultiplierInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sizeMultiplierInput.DecimalPlaces = 2;
        this._sizeMultiplierInput.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        this._sizeMultiplierInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._sizeMultiplierInput.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._sizeMultiplierInput.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        this._sizeMultiplierInput.Name = "_sizeMultiplierInput";
        this._sizeMultiplierInput.Size = new System.Drawing.Size(160, 23);
        this._sizeMultiplierInput.TabIndex = 11;
        this._sizeMultiplierInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        this._sizeMultiplierInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
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
            this._colGroupId});
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
        // _holdingTab
        //
        this._holdingTab.Controls.Add(this._conflictsGrid);
        this._holdingTab.Controls.Add(this._holdingWarningLabel);
        this._holdingTab.Controls.Add(this._holdingLayout);
        this._holdingTab.Location = new System.Drawing.Point(4, 27);
        this._holdingTab.Name = "_holdingTab";
        this._holdingTab.Padding = new System.Windows.Forms.Padding(12);
        this._holdingTab.Size = new System.Drawing.Size(892, 525);
        this._holdingTab.TabIndex = 4;
        this._holdingTab.Text = "Chiusure forzate";
        this._holdingTab.UseVisualStyleBackColor = true;
        //
        // _holdingLayout
        //
        this._holdingLayout.AutoSize = true;
        this._holdingLayout.ColumnCount = 4;
        this._holdingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._holdingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._holdingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._holdingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._holdingLayout.Controls.Add(this._forceNightCloseCheckBox, 0, 0);
        this._holdingLayout.Controls.Add(this._sessionFlatLabel, 2, 0);
        this._holdingLayout.Controls.Add(this._sessionFlatInput, 3, 0);
        this._holdingLayout.Controls.Add(this._forceWeekCloseCheckBox, 0, 1);
        this._holdingLayout.Controls.Add(this._weekEndFromLabel, 2, 1);
        this._holdingLayout.Controls.Add(this._weekEndFromInput, 3, 1);
        this._holdingLayout.Controls.Add(this._weekEndUntilLabel, 2, 2);
        this._holdingLayout.Controls.Add(this._weekEndUntilInput, 3, 2);
        this._holdingLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._holdingLayout.Location = new System.Drawing.Point(12, 12);
        this._holdingLayout.Name = "_holdingLayout";
        this._holdingLayout.RowCount = 3;
        this._holdingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._holdingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._holdingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._holdingLayout.Size = new System.Drawing.Size(868, 96);
        this._holdingLayout.TabIndex = 0;
        //
        // _forceNightCloseCheckBox
        //
        this._forceNightCloseCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._forceNightCloseCheckBox.AutoSize = true;
        this._holdingLayout.SetColumnSpan(this._forceNightCloseCheckBox, 2);
        this._forceNightCloseCheckBox.Name = "_forceNightCloseCheckBox";
        this._forceNightCloseCheckBox.TabIndex = 0;
        this._forceNightCloseCheckBox.Text = "Forza chiusura night (nessuna posizione oltre la fine sessione)";
        this._forceNightCloseCheckBox.UseVisualStyleBackColor = true;
        this._forceNightCloseCheckBox.CheckedChanged += new System.EventHandler(this.OnHoldingChanged);
        //
        // _sessionFlatLabel
        //
        this._sessionFlatLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sessionFlatLabel.AutoSize = true;
        this._sessionFlatLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._sessionFlatLabel.Name = "_sessionFlatLabel";
        this._sessionFlatLabel.TabIndex = 1;
        this._sessionFlatLabel.Text = "Flat di sessione (HHMM UTC)";
        //
        // _sessionFlatInput
        //
        this._sessionFlatInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sessionFlatInput.Maximum = new decimal(new int[] { 2359, 0, 0, 0 });
        this._sessionFlatInput.Name = "_sessionFlatInput";
        this._sessionFlatInput.Size = new System.Drawing.Size(90, 23);
        this._sessionFlatInput.TabIndex = 2;
        this._sessionFlatInput.ValueChanged += new System.EventHandler(this.OnHoldingChanged);
        //
        // _forceWeekCloseCheckBox
        //
        this._forceWeekCloseCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._forceWeekCloseCheckBox.AutoSize = true;
        this._holdingLayout.SetColumnSpan(this._forceWeekCloseCheckBox, 2);
        this._forceWeekCloseCheckBox.Name = "_forceWeekCloseCheckBox";
        this._forceWeekCloseCheckBox.TabIndex = 3;
        this._forceWeekCloseCheckBox.Text = "Forza chiusura week (nessuna posizione oltre il fine settimana)";
        this._forceWeekCloseCheckBox.UseVisualStyleBackColor = true;
        this._forceWeekCloseCheckBox.CheckedChanged += new System.EventHandler(this.OnHoldingChanged);
        //
        // _weekEndFromLabel
        //
        this._weekEndFromLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._weekEndFromLabel.AutoSize = true;
        this._weekEndFromLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._weekEndFromLabel.Name = "_weekEndFromLabel";
        this._weekEndFromLabel.TabIndex = 4;
        this._weekEndFromLabel.Text = "Flat weekend da (ven, HHMM UTC)";
        //
        // _weekEndFromInput
        //
        this._weekEndFromInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._weekEndFromInput.Maximum = new decimal(new int[] { 2359, 0, 0, 0 });
        this._weekEndFromInput.Name = "_weekEndFromInput";
        this._weekEndFromInput.Size = new System.Drawing.Size(90, 23);
        this._weekEndFromInput.TabIndex = 5;
        this._weekEndFromInput.ValueChanged += new System.EventHandler(this.OnHoldingChanged);
        //
        // _weekEndUntilLabel
        //
        this._weekEndUntilLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._weekEndUntilLabel.AutoSize = true;
        this._weekEndUntilLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._weekEndUntilLabel.Name = "_weekEndUntilLabel";
        this._weekEndUntilLabel.TabIndex = 6;
        this._weekEndUntilLabel.Text = "fino a (dom, HHMM UTC)";
        //
        // _weekEndUntilInput
        //
        this._weekEndUntilInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._weekEndUntilInput.Maximum = new decimal(new int[] { 2359, 0, 0, 0 });
        this._weekEndUntilInput.Name = "_weekEndUntilInput";
        this._weekEndUntilInput.Size = new System.Drawing.Size(90, 23);
        this._weekEndUntilInput.TabIndex = 7;
        this._weekEndUntilInput.ValueChanged += new System.EventHandler(this.OnHoldingChanged);
        //
        // _holdingWarningLabel
        //
        this._holdingWarningLabel.AutoSize = false;
        this._holdingWarningLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._holdingWarningLabel.Name = "_holdingWarningLabel";
        this._holdingWarningLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
        this._holdingWarningLabel.Size = new System.Drawing.Size(868, 56);
        this._holdingWarningLabel.TabIndex = 8;
        this._holdingWarningLabel.Text = "";
        //
        // _conflictsGrid
        //
        this._conflictsGrid.AllowUserToAddRows = false;
        this._conflictsGrid.AllowUserToDeleteRows = false;
        this._conflictsGrid.AutoGenerateColumns = false;
        this._conflictsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._conflictsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._conflictsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._conflictsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._conflictsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colConflictStrategy,
            this._colConflictSymbol,
            this._colConflictTimeframe,
            this._colConflictHolding,
            this._colConflictEffect});
        this._conflictsGrid.DataSource = this._conflictsBindingSource;
        this._conflictsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._conflictsGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._conflictsGrid.MultiSelect = false;
        this._conflictsGrid.Name = "_conflictsGrid";
        this._conflictsGrid.ReadOnly = true;
        this._conflictsGrid.RowHeadersVisible = false;
        this._conflictsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._conflictsGrid.TabIndex = 9;
        //
        // _colConflictStrategy
        //
        this._colConflictStrategy.DataPropertyName = "Strategy";
        this._colConflictStrategy.FillWeight = 140F;
        this._colConflictStrategy.HeaderText = "Strategia";
        this._colConflictStrategy.Name = "_colConflictStrategy";
        this._colConflictStrategy.ReadOnly = true;
        //
        // _colConflictSymbol
        //
        this._colConflictSymbol.DataPropertyName = "Symbol";
        this._colConflictSymbol.FillWeight = 50F;
        this._colConflictSymbol.HeaderText = "Simbolo";
        this._colConflictSymbol.Name = "_colConflictSymbol";
        this._colConflictSymbol.ReadOnly = true;
        //
        // _colConflictTimeframe
        //
        this._colConflictTimeframe.DataPropertyName = "Timeframe";
        this._colConflictTimeframe.FillWeight = 40F;
        this._colConflictTimeframe.HeaderText = "TF";
        this._colConflictTimeframe.Name = "_colConflictTimeframe";
        this._colConflictTimeframe.ReadOnly = true;
        //
        // _colConflictHolding
        //
        this._colConflictHolding.DataPropertyName = "Holding";
        this._colConflictHolding.FillWeight = 70F;
        this._colConflictHolding.HeaderText = "Dichiara";
        this._colConflictHolding.Name = "_colConflictHolding";
        this._colConflictHolding.ReadOnly = true;
        //
        // _colConflictEffect
        //
        this._colConflictEffect.DataPropertyName = "Effect";
        this._colConflictEffect.FillWeight = 130F;
        this._colConflictEffect.HeaderText = "Effetto del piano";
        this._colConflictEffect.Name = "_colConflictEffect";
        this._colConflictEffect.ReadOnly = true;
        //
        // PlanDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._toolbar);
        this.Name = "PlanDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._conflictsBindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._conflictsGrid)).EndInit();
        this._sessionFlatInput.EndInit();
        this._weekEndFromInput.EndInit();
        this._weekEndUntilInput.EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsBindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._accountsBindingSource)).EndInit();
        this._tabs.ResumeLayout(false);
        this._generalTab.ResumeLayout(false);
        this._generalTab.PerformLayout();
        this._generalLayout.ResumeLayout(false);
        this._generalLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._sizeMultiplierInput)).EndInit();
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
    private System.Windows.Forms.Label _workspaceValueLabel;
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeTextBox;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _commissionLabel;
    private System.Windows.Forms.NumericUpDown _commissionInput;
    private System.Windows.Forms.Label _enforceConcurrencyLabel;
    private System.Windows.Forms.ComboBox _enforceConcurrencyCombo;
    private System.Windows.Forms.Label _sizeMultiplierLabel;
    private System.Windows.Forms.NumericUpDown _sizeMultiplierInput;
    private System.Windows.Forms.TabPage _groupsTab;
    private System.Windows.Forms.DataGridView _groupsGrid;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colGroupId;
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
    private System.Windows.Forms.TabPage _holdingTab;
    private System.Windows.Forms.TableLayoutPanel _holdingLayout;
    private System.Windows.Forms.CheckBox _forceNightCloseCheckBox;
    private System.Windows.Forms.Label _sessionFlatLabel;
    private System.Windows.Forms.NumericUpDown _sessionFlatInput;
    private System.Windows.Forms.CheckBox _forceWeekCloseCheckBox;
    private System.Windows.Forms.Label _weekEndFromLabel;
    private System.Windows.Forms.NumericUpDown _weekEndFromInput;
    private System.Windows.Forms.Label _weekEndUntilLabel;
    private System.Windows.Forms.NumericUpDown _weekEndUntilInput;
    private System.Windows.Forms.Label _holdingWarningLabel;
    private System.Windows.Forms.BindingSource _conflictsBindingSource;
    private System.Windows.Forms.DataGridView _conflictsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colConflictStrategy;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colConflictSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colConflictTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colConflictHolding;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colConflictEffect;
}
