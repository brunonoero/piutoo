namespace piootooapp.clientform.Shell.Screens;

partial class SymbolPresetScreen
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
        this._bindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._hintLabel = new System.Windows.Forms.Label();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._removeRowButton = new System.Windows.Forms.Button();
        this._identityButton = new System.Windows.Forms.Button();
        this._grid = new System.Windows.Forms.DataGridView();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colContractMultiplier = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        this._buttons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        // 
        // _toolbar
        // 
        this._toolbar.CanGoBack = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Conversioni simbolo";
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        // 
        // _hintLabel
        // 
        this._hintLabel.AutoSize = true;
        this._hintLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._hintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._hintLabel.Location = new System.Drawing.Point(0, 44);
        this._hintLabel.Name = "_hintLabel";
        this._hintLabel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        this._hintLabel.Size = new System.Drawing.Size(900, 27);
        this._hintLabel.TabIndex = 1;
        this._hintLabel.Text = "Preset condiviso usato come punto di partenza dei nuovi account. Modificarlo non cambia gli account già creati: ognuno porta la propria tabella.";
        // 
        // _buttons
        // 
        this._buttons.AutoSize = true;
        this._buttons.Controls.Add(this._removeRowButton);
        this._buttons.Controls.Add(this._identityButton);
        this._buttons.Dock = System.Windows.Forms.DockStyle.Top;
        this._buttons.Location = new System.Drawing.Point(0, 71);
        this._buttons.Name = "_buttons";
        this._buttons.Padding = new System.Windows.Forms.Padding(12, 0, 12, 6);
        this._buttons.Size = new System.Drawing.Size(900, 37);
        this._buttons.TabIndex = 2;
        this._buttons.WrapContents = false;
        // 
        // _removeRowButton
        // 
        this._removeRowButton.AutoSize = true;
        this._removeRowButton.Name = "_removeRowButton";
        this._removeRowButton.Size = new System.Drawing.Size(100, 25);
        this._removeRowButton.TabIndex = 0;
        this._removeRowButton.Text = "Rimuovi riga";
        this._removeRowButton.UseVisualStyleBackColor = true;
        this._removeRowButton.Click += new System.EventHandler(this.OnRemoveRowClick);
        // 
        // _identityButton
        // 
        this._identityButton.AutoSize = true;
        this._identityButton.Name = "_identityButton";
        this._identityButton.Size = new System.Drawing.Size(160, 25);
        this._identityButton.TabIndex = 1;
        this._identityButton.Text = "Riempi dal catalogo (identità)";
        this._identityButton.UseVisualStyleBackColor = true;
        this._identityButton.Click += new System.EventHandler(this.OnLoadIdentityClick);
        // 
        // _grid
        // 
        this._grid.AutoGenerateColumns = false;
        this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._grid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colSymbol,
            this._colAccountSymbol,
            this._colContractMultiplier,
            this._colEnabled});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.Location = new System.Drawing.Point(0, 108);
        this._grid.Name = "_grid";
        this._grid.RowHeadersVisible = false;
        this._grid.Size = new System.Drawing.Size(900, 492);
        this._grid.TabIndex = 3;
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
        // _colEnabled
        // 
        this._colEnabled.DataPropertyName = "Enabled";
        this._colEnabled.FillWeight = 60F;
        this._colEnabled.HeaderText = "Abilitato";
        this._colEnabled.Name = "_colEnabled";
        // 
        // SymbolPresetScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._buttons);
        this.Controls.Add(this._hintLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "SymbolPresetScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.Label _hintLabel;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _removeRowButton;
    private System.Windows.Forms.Button _identityButton;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colContractMultiplier;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colEnabled;
}
