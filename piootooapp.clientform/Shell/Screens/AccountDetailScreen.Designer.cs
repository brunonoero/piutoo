namespace piootooapp.clientform.Shell.Screens;

partial class AccountDetailScreen
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
        this._mappingsBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._identityLabel = new System.Windows.Forms.Label();
        this._fieldsLayout = new System.Windows.Forms.TableLayoutPanel();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._accountNumberLabel = new System.Windows.Forms.Label();
        this._accountNumberTextBox = new System.Windows.Forms.TextBox();
        this._groupLabel = new System.Windows.Forms.Label();
        this._groupCombo = new System.Windows.Forms.ComboBox();
        this._brokerLabel = new System.Windows.Forms.Label();
        this._brokerTextBox = new System.Windows.Forms.TextBox();
        this._currencyLabel = new System.Windows.Forms.Label();
        this._currencyCombo = new System.Windows.Forms.ComboBox();
        this._initialBalanceLabel = new System.Windows.Forms.Label();
        this._initialBalanceInput = new System.Windows.Forms.NumericUpDown();
        this._enabledCheckBox = new System.Windows.Forms.CheckBox();
        this._notesLabel = new System.Windows.Forms.Label();
        this._notesTextBox = new System.Windows.Forms.TextBox();
        this._mappingsGroup = new System.Windows.Forms.GroupBox();
        this._mappingsGrid = new System.Windows.Forms.DataGridView();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colContractMultiplier = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMappingEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._mappingsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._removeMappingButton = new System.Windows.Forms.Button();
        this._identityMappingsButton = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this._mappingsBindingSource)).BeginInit();
        this._fieldsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialBalanceInput)).BeginInit();
        this._mappingsGroup.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._mappingsGrid)).BeginInit();
        this._mappingsButtons.SuspendLayout();
        this.SuspendLayout();
        // 
        // _toolbar
        // 
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Account";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        // 
        // _identityLabel
        // 
        this._identityLabel.AutoSize = true;
        this._identityLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._identityLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._identityLabel.Location = new System.Drawing.Point(0, 44);
        this._identityLabel.Name = "_identityLabel";
        this._identityLabel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        this._identityLabel.Size = new System.Drawing.Size(900, 27);
        this._identityLabel.TabIndex = 1;
        // 
        // _fieldsLayout
        // 
        this._fieldsLayout.AutoSize = true;
        this._fieldsLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._fieldsLayout.ColumnCount = 4;
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._fieldsLayout.Controls.Add(this._nameLabel, 0, 0);
        this._fieldsLayout.Controls.Add(this._nameTextBox, 1, 0);
        this._fieldsLayout.Controls.Add(this._accountNumberLabel, 2, 0);
        this._fieldsLayout.Controls.Add(this._accountNumberTextBox, 3, 0);
        this._fieldsLayout.Controls.Add(this._groupLabel, 0, 1);
        this._fieldsLayout.Controls.Add(this._groupCombo, 1, 1);
        this._fieldsLayout.Controls.Add(this._brokerLabel, 2, 1);
        this._fieldsLayout.Controls.Add(this._brokerTextBox, 3, 1);
        this._fieldsLayout.Controls.Add(this._currencyLabel, 0, 2);
        this._fieldsLayout.Controls.Add(this._currencyCombo, 1, 2);
        this._fieldsLayout.Controls.Add(this._initialBalanceLabel, 2, 2);
        this._fieldsLayout.Controls.Add(this._initialBalanceInput, 3, 2);
        this._fieldsLayout.Controls.Add(this._enabledCheckBox, 1, 3);
        this._fieldsLayout.Controls.Add(this._notesLabel, 0, 4);
        this._fieldsLayout.Controls.Add(this._notesTextBox, 1, 4);
        this._fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._fieldsLayout.Location = new System.Drawing.Point(0, 71);
        this._fieldsLayout.Name = "_fieldsLayout";
        this._fieldsLayout.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._fieldsLayout.RowCount = 5;
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.SetColumnSpan(this._notesTextBox, 3);
        this._fieldsLayout.Size = new System.Drawing.Size(900, 170);
        this._fieldsLayout.TabIndex = 2;
        // 
        // _nameLabel
        // 
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(48, 15);
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Nome *";
        // 
        // _nameTextBox
        // 
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(300, 23);
        this._nameTextBox.TabIndex = 1;
        this._nameTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _accountNumberLabel
        // 
        this._accountNumberLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._accountNumberLabel.AutoSize = true;
        this._accountNumberLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._accountNumberLabel.Name = "_accountNumberLabel";
        this._accountNumberLabel.Size = new System.Drawing.Size(90, 15);
        this._accountNumberLabel.TabIndex = 2;
        this._accountNumberLabel.Text = "Codice account";
        // 
        // _accountNumberTextBox
        // 
        this._accountNumberTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._accountNumberTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._accountNumberTextBox.Name = "_accountNumberTextBox";
        this._accountNumberTextBox.Size = new System.Drawing.Size(300, 23);
        this._accountNumberTextBox.TabIndex = 3;
        this._accountNumberTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _groupLabel
        // 
        this._groupLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._groupLabel.AutoSize = true;
        this._groupLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._groupLabel.Name = "_groupLabel";
        this._groupLabel.Size = new System.Drawing.Size(48, 15);
        this._groupLabel.TabIndex = 4;
        this._groupLabel.Text = "Gruppo";
        // 
        // _groupCombo
        // 
        this._groupCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groupCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._groupCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._groupCombo.Name = "_groupCombo";
        this._groupCombo.Size = new System.Drawing.Size(300, 23);
        this._groupCombo.TabIndex = 5;
        this._groupCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _brokerLabel
        // 
        this._brokerLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._brokerLabel.AutoSize = true;
        this._brokerLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._brokerLabel.Name = "_brokerLabel";
        this._brokerLabel.Size = new System.Drawing.Size(44, 15);
        this._brokerLabel.TabIndex = 6;
        this._brokerLabel.Text = "Broker";
        // 
        // _brokerTextBox
        // 
        this._brokerTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._brokerTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._brokerTextBox.Name = "_brokerTextBox";
        this._brokerTextBox.Size = new System.Drawing.Size(300, 23);
        this._brokerTextBox.TabIndex = 7;
        this._brokerTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _currencyLabel
        // 
        this._currencyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._currencyLabel.AutoSize = true;
        this._currencyLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._currencyLabel.Name = "_currencyLabel";
        this._currencyLabel.Size = new System.Drawing.Size(44, 15);
        this._currencyLabel.TabIndex = 8;
        this._currencyLabel.Text = "Valuta";
        // 
        // _currencyCombo
        // 
        this._currencyCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._currencyCombo.Items.AddRange(new object[] {
            "USD",
            "EUR",
            "GBP",
            "CHF"});
        this._currencyCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._currencyCombo.Name = "_currencyCombo";
        this._currencyCombo.Size = new System.Drawing.Size(100, 23);
        this._currencyCombo.TabIndex = 9;
        this._currencyCombo.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _initialBalanceLabel
        // 
        this._initialBalanceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._initialBalanceLabel.AutoSize = true;
        this._initialBalanceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._initialBalanceLabel.Name = "_initialBalanceLabel";
        this._initialBalanceLabel.Size = new System.Drawing.Size(90, 15);
        this._initialBalanceLabel.TabIndex = 10;
        this._initialBalanceLabel.Text = "Balance iniziale";
        // 
        // _initialBalanceInput
        // 
        this._initialBalanceInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._initialBalanceInput.DecimalPlaces = 2;
        this._initialBalanceInput.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
        this._initialBalanceInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._initialBalanceInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
        this._initialBalanceInput.Name = "_initialBalanceInput";
        this._initialBalanceInput.Size = new System.Drawing.Size(160, 23);
        this._initialBalanceInput.TabIndex = 11;
        this._initialBalanceInput.ThousandsSeparator = true;
        this._initialBalanceInput.ValueChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _enabledCheckBox
        // 
        this._enabledCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._enabledCheckBox.AutoSize = true;
        this._enabledCheckBox.Checked = true;
        this._enabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this._enabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        this._enabledCheckBox.Name = "_enabledCheckBox";
        this._enabledCheckBox.Size = new System.Drawing.Size(74, 19);
        this._enabledCheckBox.TabIndex = 12;
        this._enabledCheckBox.Text = "Abilitato";
        this._enabledCheckBox.UseVisualStyleBackColor = true;
        this._enabledCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _notesLabel
        // 
        this._notesLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._notesLabel.AutoSize = true;
        this._notesLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._notesLabel.Name = "_notesLabel";
        this._notesLabel.Size = new System.Drawing.Size(33, 15);
        this._notesLabel.TabIndex = 13;
        this._notesLabel.Text = "Note";
        // 
        // _notesTextBox
        // 
        this._notesTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._notesTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._notesTextBox.Multiline = true;
        this._notesTextBox.Name = "_notesTextBox";
        this._notesTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._notesTextBox.Size = new System.Drawing.Size(700, 52);
        this._notesTextBox.TabIndex = 14;
        this._notesTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        // 
        // _mappingsGroup
        // 
        this._mappingsGroup.Controls.Add(this._mappingsGrid);
        this._mappingsGroup.Controls.Add(this._mappingsButtons);
        this._mappingsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
        this._mappingsGroup.Location = new System.Drawing.Point(0, 241);
        this._mappingsGroup.Margin = new System.Windows.Forms.Padding(12);
        this._mappingsGroup.Name = "_mappingsGroup";
        this._mappingsGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._mappingsGroup.Size = new System.Drawing.Size(900, 359);
        this._mappingsGroup.TabIndex = 3;
        this._mappingsGroup.TabStop = false;
        this._mappingsGroup.Text = "Tabella di conversione simboli";
        // 
        // _mappingsButtons
        // 
        this._mappingsButtons.AutoSize = true;
        this._mappingsButtons.Controls.Add(this._removeMappingButton);
        this._mappingsButtons.Controls.Add(this._identityMappingsButton);
        this._mappingsButtons.Dock = System.Windows.Forms.DockStyle.Top;
        this._mappingsButtons.Location = new System.Drawing.Point(12, 22);
        this._mappingsButtons.Name = "_mappingsButtons";
        this._mappingsButtons.Size = new System.Drawing.Size(876, 31);
        this._mappingsButtons.TabIndex = 0;
        this._mappingsButtons.WrapContents = false;
        // 
        // _removeMappingButton
        // 
        this._removeMappingButton.AutoSize = true;
        this._removeMappingButton.Name = "_removeMappingButton";
        this._removeMappingButton.Size = new System.Drawing.Size(100, 25);
        this._removeMappingButton.TabIndex = 0;
        this._removeMappingButton.Text = "Rimuovi riga";
        this._removeMappingButton.UseVisualStyleBackColor = true;
        this._removeMappingButton.Click += new System.EventHandler(this.OnRemoveMappingClick);
        // 
        // _identityMappingsButton
        // 
        this._identityMappingsButton.AutoSize = true;
        this._identityMappingsButton.Name = "_identityMappingsButton";
        this._identityMappingsButton.Size = new System.Drawing.Size(140, 25);
        this._identityMappingsButton.TabIndex = 1;
        this._identityMappingsButton.Text = "Ripristina identità";
        this._identityMappingsButton.UseVisualStyleBackColor = true;
        this._identityMappingsButton.Click += new System.EventHandler(this.OnLoadIdentityMappingsClick);
        // 
        // _mappingsGrid
        // 
        this._mappingsGrid.AutoGenerateColumns = false;
        this._mappingsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._mappingsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._mappingsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._mappingsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._mappingsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colSymbol,
            this._colAccountSymbol,
            this._colContractMultiplier,
            this._colMappingEnabled});
        this._mappingsGrid.DataSource = this._mappingsBindingSource;
        this._mappingsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._mappingsGrid.Location = new System.Drawing.Point(12, 53);
        this._mappingsGrid.Name = "_mappingsGrid";
        this._mappingsGrid.RowHeadersVisible = false;
        this._mappingsGrid.Size = new System.Drawing.Size(876, 294);
        this._mappingsGrid.TabIndex = 1;
        // 
        // _colSymbol
        // 
        this._colSymbol.DataPropertyName = "Symbol";
        this._colSymbol.HeaderText = "Simbolo Piootoo";
        this._colSymbol.Name = "_colSymbol";
        // 
        // _colAccountSymbol
        // 
        this._colAccountSymbol.DataPropertyName = "AccountSymbol";
        this._colAccountSymbol.HeaderText = "Simbolo account";
        this._colAccountSymbol.Name = "_colAccountSymbol";
        // 
        // _colContractMultiplier
        // 
        this._colContractMultiplier.DataPropertyName = "ContractMultiplier";
        this._colContractMultiplier.HeaderText = "Moltiplicatore contratto";
        this._colContractMultiplier.Name = "_colContractMultiplier";
        // 
        // _colMappingEnabled
        // 
        this._colMappingEnabled.DataPropertyName = "Enabled";
        this._colMappingEnabled.FillWeight = 60F;
        this._colMappingEnabled.HeaderText = "Abilitato";
        this._colMappingEnabled.Name = "_colMappingEnabled";
        // 
        // AccountDetailScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._mappingsGroup);
        this.Controls.Add(this._fieldsLayout);
        this.Controls.Add(this._identityLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "AccountDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._mappingsBindingSource)).EndInit();
        this._fieldsLayout.ResumeLayout(false);
        this._fieldsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialBalanceInput)).EndInit();
        this._mappingsGroup.ResumeLayout(false);
        this._mappingsGroup.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._mappingsGrid)).EndInit();
        this._mappingsButtons.ResumeLayout(false);
        this._mappingsButtons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _mappingsBindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.Label _identityLabel;
    private System.Windows.Forms.TableLayoutPanel _fieldsLayout;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _accountNumberLabel;
    private System.Windows.Forms.TextBox _accountNumberTextBox;
    private System.Windows.Forms.Label _groupLabel;
    private System.Windows.Forms.ComboBox _groupCombo;
    private System.Windows.Forms.Label _brokerLabel;
    private System.Windows.Forms.TextBox _brokerTextBox;
    private System.Windows.Forms.Label _currencyLabel;
    private System.Windows.Forms.ComboBox _currencyCombo;
    private System.Windows.Forms.Label _initialBalanceLabel;
    private System.Windows.Forms.NumericUpDown _initialBalanceInput;
    private System.Windows.Forms.CheckBox _enabledCheckBox;
    private System.Windows.Forms.Label _notesLabel;
    private System.Windows.Forms.TextBox _notesTextBox;
    private System.Windows.Forms.GroupBox _mappingsGroup;
    private System.Windows.Forms.FlowLayoutPanel _mappingsButtons;
    private System.Windows.Forms.Button _removeMappingButton;
    private System.Windows.Forms.Button _identityMappingsButton;
    private System.Windows.Forms.DataGridView _mappingsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colContractMultiplier;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colMappingEnabled;
}
