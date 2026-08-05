namespace piootooapp.clientform.Shell.Screens;

partial class SymbolConversionDetailScreen
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
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeTextBox = new System.Windows.Forms.TextBox();
        this._mappingsGroup = new System.Windows.Forms.GroupBox();
        this._mappingsGrid = new System.Windows.Forms.DataGridView();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colContractMultiplier = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMinimumQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colQuantityStep = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRoundingMode = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this._colMappingEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._mappingsButtons = new System.Windows.Forms.FlowLayoutPanel();
        this._removeMappingButton = new System.Windows.Forms.Button();
        this._identityMappingsButton = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this._mappingsBindingSource)).BeginInit();
        this._fieldsLayout.SuspendLayout();
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
        this._toolbar.Title = "Conversioni simbolo";
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
        this._fieldsLayout.Controls.Add(this._codeLabel, 2, 0);
        this._fieldsLayout.Controls.Add(this._codeTextBox, 3, 0);
        this._fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._fieldsLayout.Location = new System.Drawing.Point(0, 71);
        this._fieldsLayout.Name = "_fieldsLayout";
        this._fieldsLayout.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._fieldsLayout.RowCount = 1;
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.Size = new System.Drawing.Size(900, 40);
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
        // _codeLabel
        //
        this._codeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._codeLabel.AutoSize = true;
        this._codeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._codeLabel.Name = "_codeLabel";
        this._codeLabel.Size = new System.Drawing.Size(56, 15);
        this._codeLabel.TabIndex = 2;
        this._codeLabel.Text = "Codice *";
        //
        // _codeTextBox
        //
        this._codeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._codeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._codeTextBox.Name = "_codeTextBox";
        this._codeTextBox.Size = new System.Drawing.Size(300, 23);
        this._codeTextBox.TabIndex = 3;
        this._codeTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _mappingsGroup
        //
        this._mappingsGroup.Controls.Add(this._mappingsGrid);
        this._mappingsGroup.Controls.Add(this._mappingsButtons);
        this._mappingsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
        this._mappingsGroup.Location = new System.Drawing.Point(0, 111);
        this._mappingsGroup.Margin = new System.Windows.Forms.Padding(12);
        this._mappingsGroup.Name = "_mappingsGroup";
        this._mappingsGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._mappingsGroup.Size = new System.Drawing.Size(900, 489);
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
        this._identityMappingsButton.Size = new System.Drawing.Size(190, 25);
        this._identityMappingsButton.TabIndex = 1;
        this._identityMappingsButton.Text = "Riempi dal catalogo (identità)";
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
            this._colMinimumQuantity,
            this._colQuantityStep,
            this._colRoundingMode,
            this._colMappingEnabled});
        this._mappingsGrid.DataSource = this._mappingsBindingSource;
        this._mappingsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._mappingsGrid.Location = new System.Drawing.Point(12, 53);
        this._mappingsGrid.Name = "_mappingsGrid";
        this._mappingsGrid.RowHeadersVisible = false;
        this._mappingsGrid.Size = new System.Drawing.Size(876, 424);
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
        // _colMappingEnabled
        //
        this._colMappingEnabled.DataPropertyName = "Enabled";
        this._colMappingEnabled.FillWeight = 60F;
        this._colMappingEnabled.HeaderText = "Abilitato";
        this._colMappingEnabled.Name = "_colMappingEnabled";
        //
        // SymbolConversionDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._mappingsGroup);
        this.Controls.Add(this._fieldsLayout);
        this.Controls.Add(this._identityLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "SymbolConversionDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._mappingsBindingSource)).EndInit();
        this._fieldsLayout.ResumeLayout(false);
        this._fieldsLayout.PerformLayout();
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
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeTextBox;
    private System.Windows.Forms.GroupBox _mappingsGroup;
    private System.Windows.Forms.FlowLayoutPanel _mappingsButtons;
    private System.Windows.Forms.Button _removeMappingButton;
    private System.Windows.Forms.Button _identityMappingsButton;
    private System.Windows.Forms.DataGridView _mappingsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colContractMultiplier;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMinimumQuantity;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colQuantityStep;
    private System.Windows.Forms.DataGridViewComboBoxColumn _colRoundingMode;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colMappingEnabled;
}
