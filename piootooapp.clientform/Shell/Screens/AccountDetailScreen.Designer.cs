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
        this._symbolConversionLabel = new System.Windows.Forms.Label();
        this._symbolConversionCombo = new System.Windows.Forms.ComboBox();
        this._notesLabel = new System.Windows.Forms.Label();
        this._notesTextBox = new System.Windows.Forms.TextBox();
        this._tabs = new System.Windows.Forms.TabControl();
        this._generalTab = new System.Windows.Forms.TabPage();
        this._strategiesTab = new System.Windows.Forms.TabPage();
        this._strategiesHeader = new System.Windows.Forms.TableLayoutPanel();
        this._strategiesFilterTextBox = new System.Windows.Forms.TextBox();
        this._strategiesCountLabel = new System.Windows.Forms.Label();
        this._strategiesGrid = new System.Windows.Forms.DataGridView();
        this._colStrategyCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategySymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategyTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategyActive = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategyHolding = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._strategiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._tabs.SuspendLayout();
        this._generalTab.SuspendLayout();
        this._strategiesTab.SuspendLayout();
        this._strategiesHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesBindingSource)).BeginInit();
        this._fieldsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialBalanceInput)).BeginInit();
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
        this._fieldsLayout.Controls.Add(this._symbolConversionLabel, 0, 4);
        this._fieldsLayout.Controls.Add(this._symbolConversionCombo, 1, 4);
        this._fieldsLayout.Controls.Add(this._notesLabel, 0, 5);
        this._fieldsLayout.Controls.Add(this._notesTextBox, 1, 5);
        this._fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._fieldsLayout.Location = new System.Drawing.Point(0, 0);
        this._fieldsLayout.Name = "_fieldsLayout";
        this._fieldsLayout.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._fieldsLayout.RowCount = 6;
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.SetColumnSpan(this._symbolConversionCombo, 3);
        this._fieldsLayout.SetColumnSpan(this._notesTextBox, 3);
        this._fieldsLayout.Size = new System.Drawing.Size(900, 200);
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
        // _symbolConversionLabel
        //
        this._symbolConversionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._symbolConversionLabel.AutoSize = true;
        this._symbolConversionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._symbolConversionLabel.Name = "_symbolConversionLabel";
        this._symbolConversionLabel.Size = new System.Drawing.Size(120, 15);
        this._symbolConversionLabel.TabIndex = 13;
        this._symbolConversionLabel.Text = "Conversione simbolo";
        //
        // _symbolConversionCombo
        //
        this._symbolConversionCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._symbolConversionCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._symbolConversionCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._symbolConversionCombo.Name = "_symbolConversionCombo";
        this._symbolConversionCombo.Size = new System.Drawing.Size(700, 23);
        this._symbolConversionCombo.TabIndex = 14;
        this._symbolConversionCombo.SelectedIndexChanged += new System.EventHandler(this.OnSymbolConversionChanged);
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
        // _tabs
        //
        this._tabs.Controls.Add(this._generalTab);
        this._tabs.Controls.Add(this._strategiesTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 71);
        this._tabs.Name = "_tabs";
        this._tabs.Padding = new System.Drawing.Point(12, 4);
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(900, 529);
        this._tabs.TabIndex = 2;
        //
        // _generalTab
        //
        this._generalTab.AutoScroll = true;
        this._generalTab.Controls.Add(this._fieldsLayout);
        this._generalTab.Location = new System.Drawing.Point(4, 27);
        this._generalTab.Name = "_generalTab";
        this._generalTab.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
        this._generalTab.Size = new System.Drawing.Size(892, 498);
        this._generalTab.TabIndex = 0;
        this._generalTab.Text = "Generale";
        this._generalTab.UseVisualStyleBackColor = true;
        //
        // _strategiesTab
        //
        this._strategiesTab.Controls.Add(this._strategiesGrid);
        this._strategiesTab.Controls.Add(this._strategiesHeader);
        this._strategiesTab.Location = new System.Drawing.Point(4, 27);
        this._strategiesTab.Name = "_strategiesTab";
        this._strategiesTab.Padding = new System.Windows.Forms.Padding(12);
        this._strategiesTab.Size = new System.Drawing.Size(892, 498);
        this._strategiesTab.TabIndex = 1;
        this._strategiesTab.Text = "Strategie";
        this._strategiesTab.UseVisualStyleBackColor = true;
        //
        // _strategiesHeader
        //
        this._strategiesHeader.AutoSize = true;
        this._strategiesHeader.ColumnCount = 2;
        this._strategiesHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._strategiesHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._strategiesHeader.Controls.Add(this._strategiesFilterTextBox, 0, 0);
        this._strategiesHeader.Controls.Add(this._strategiesCountLabel, 1, 0);
        this._strategiesHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this._strategiesHeader.Location = new System.Drawing.Point(12, 12);
        this._strategiesHeader.Name = "_strategiesHeader";
        this._strategiesHeader.RowCount = 1;
        this._strategiesHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._strategiesHeader.Size = new System.Drawing.Size(868, 33);
        this._strategiesHeader.TabIndex = 0;
        //
        // _strategiesFilterTextBox
        //
        this._strategiesFilterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._strategiesFilterTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 12, 4);
        this._strategiesFilterTextBox.Name = "_strategiesFilterTextBox";
        this._strategiesFilterTextBox.PlaceholderText = "Filtra per codice, simbolo o timeframe...";
        this._strategiesFilterTextBox.Size = new System.Drawing.Size(600, 23);
        this._strategiesFilterTextBox.TabIndex = 0;
        this._strategiesFilterTextBox.TextChanged += new System.EventHandler(this.OnStrategiesFilterChanged);
        //
        // _strategiesCountLabel
        //
        this._strategiesCountLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._strategiesCountLabel.AutoSize = true;
        this._strategiesCountLabel.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
        this._strategiesCountLabel.Name = "_strategiesCountLabel";
        this._strategiesCountLabel.Size = new System.Drawing.Size(200, 15);
        this._strategiesCountLabel.TabIndex = 1;
        this._strategiesCountLabel.Text = "-";
        //
        // _strategiesGrid
        //
        this._strategiesGrid.AllowUserToAddRows = false;
        this._strategiesGrid.AllowUserToDeleteRows = false;
        this._strategiesGrid.AutoGenerateColumns = false;
        this._strategiesGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._strategiesGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._strategiesGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._strategiesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._strategiesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colStrategyCode,
            this._colStrategySymbol,
            this._colAccountSymbol,
            this._colStrategyTimeframe,
            this._colStrategyActive,
            this._colStrategyHolding});
        this._strategiesGrid.DataSource = this._strategiesBindingSource;
        this._strategiesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._strategiesGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._strategiesGrid.Location = new System.Drawing.Point(12, 45);
        this._strategiesGrid.MultiSelect = false;
        this._strategiesGrid.Name = "_strategiesGrid";
        this._strategiesGrid.ReadOnly = true;
        this._strategiesGrid.RowHeadersVisible = false;
        this._strategiesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._strategiesGrid.Size = new System.Drawing.Size(868, 441);
        this._strategiesGrid.TabIndex = 1;
        //
        // _colStrategyCode
        //
        this._colStrategyCode.DataPropertyName = "Code";
        this._colStrategyCode.FillWeight = 160F;
        this._colStrategyCode.HeaderText = "Strategia";
        this._colStrategyCode.Name = "_colStrategyCode";
        this._colStrategyCode.ReadOnly = true;
        //
        // _colStrategySymbol
        //
        this._colStrategySymbol.DataPropertyName = "Symbol";
        this._colStrategySymbol.FillWeight = 60F;
        this._colStrategySymbol.HeaderText = "Simbolo";
        this._colStrategySymbol.Name = "_colStrategySymbol";
        this._colStrategySymbol.ReadOnly = true;
        //
        // _colAccountSymbol
        //
        this._colAccountSymbol.DataPropertyName = "AccountSymbol";
        this._colAccountSymbol.FillWeight = 80F;
        this._colAccountSymbol.HeaderText = "Simbolo conto";
        this._colAccountSymbol.Name = "_colAccountSymbol";
        this._colAccountSymbol.ReadOnly = true;
        //
        // _colStrategyTimeframe
        //
        this._colStrategyTimeframe.DataPropertyName = "TimeframeMinutes";
        this._colStrategyTimeframe.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colStrategyTimeframe.FillWeight = 50F;
        this._colStrategyTimeframe.HeaderText = "TF (min)";
        this._colStrategyTimeframe.Name = "_colStrategyTimeframe";
        this._colStrategyTimeframe.ReadOnly = true;
        //
        // _colStrategyActive
        //
        this._colStrategyActive.DataPropertyName = "ActiveText";
        this._colStrategyActive.FillWeight = 50F;
        this._colStrategyActive.HeaderText = "Attiva";
        this._colStrategyActive.Name = "_colStrategyActive";
        this._colStrategyActive.ReadOnly = true;
        //
        // _colStrategyHolding
        //
        this._colStrategyHolding.DataPropertyName = "Holding";
        this._colStrategyHolding.FillWeight = 80F;
        this._colStrategyHolding.HeaderText = "Tenuta";
        this._colStrategyHolding.Name = "_colStrategyHolding";
        this._colStrategyHolding.ReadOnly = true;
        //
        // AccountDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._identityLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "AccountDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._fieldsLayout.ResumeLayout(false);
        this._fieldsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._initialBalanceInput)).EndInit();
        this._tabs.ResumeLayout(false);
        this._generalTab.ResumeLayout(false);
        this._generalTab.PerformLayout();
        this._strategiesTab.ResumeLayout(false);
        this._strategiesTab.PerformLayout();
        this._strategiesHeader.ResumeLayout(false);
        this._strategiesHeader.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesBindingSource)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

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
    private System.Windows.Forms.Label _symbolConversionLabel;
    private System.Windows.Forms.ComboBox _symbolConversionCombo;
    private System.Windows.Forms.Label _notesLabel;
    private System.Windows.Forms.TextBox _notesTextBox;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _generalTab;
    private System.Windows.Forms.TabPage _strategiesTab;
    private System.Windows.Forms.TableLayoutPanel _strategiesHeader;
    private System.Windows.Forms.TextBox _strategiesFilterTextBox;
    private System.Windows.Forms.Label _strategiesCountLabel;
    private System.Windows.Forms.DataGridView _strategiesGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategyCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategySymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategyTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategyActive;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategyHolding;
    private System.Windows.Forms.BindingSource _strategiesBindingSource;
}
