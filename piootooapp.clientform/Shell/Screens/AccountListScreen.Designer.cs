namespace piootooapp.clientform.Shell.Screens;

partial class AccountListScreen
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
        this._grid = new System.Windows.Forms.DataGridView();
        this._colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colGroupId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colBroker = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCurrency = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colInitialBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSymbolCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        // 
        // _grid
        // 
        this._grid.AllowUserToAddRows = false;
        this._grid.AllowUserToDeleteRows = false;
        this._grid.AutoGenerateColumns = false;
        this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._grid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colName,
            this._colAccountNumber,
            this._colGroupId,
            this._colBroker,
            this._colCurrency,
            this._colInitialBalance,
            this._colSymbolCount,
            this._colEnabled});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 44);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 456);
        this._grid.TabIndex = 1;
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellDoubleClick);
        this._grid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        // 
        // _colName
        // 
        this._colName.DataPropertyName = "Name";
        this._colName.HeaderText = "Nome";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        // 
        // _colAccountNumber
        // 
        this._colAccountNumber.DataPropertyName = "AccountNumber";
        this._colAccountNumber.HeaderText = "Codice account";
        this._colAccountNumber.Name = "_colAccountNumber";
        this._colAccountNumber.ReadOnly = true;
        // 
        // _colGroupId
        // 
        this._colGroupId.DataPropertyName = "GroupId";
        this._colGroupId.HeaderText = "Gruppo";
        this._colGroupId.Name = "_colGroupId";
        this._colGroupId.ReadOnly = true;
        // 
        // _colBroker
        // 
        this._colBroker.DataPropertyName = "Broker";
        this._colBroker.HeaderText = "Broker";
        this._colBroker.Name = "_colBroker";
        this._colBroker.ReadOnly = true;
        // 
        // _colCurrency
        // 
        this._colCurrency.DataPropertyName = "Currency";
        this._colCurrency.FillWeight = 60F;
        this._colCurrency.HeaderText = "Valuta";
        this._colCurrency.Name = "_colCurrency";
        this._colCurrency.ReadOnly = true;
        // 
        // _colInitialBalance
        // 
        this._colInitialBalance.DataPropertyName = "InitialBalance";
        this._colInitialBalance.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colInitialBalance.DefaultCellStyle.Format = "N2";
        this._colInitialBalance.HeaderText = "Balance iniziale";
        this._colInitialBalance.Name = "_colInitialBalance";
        this._colInitialBalance.ReadOnly = true;
        // 
        // _colSymbolCount
        // 
        this._colSymbolCount.DataPropertyName = "SymbolCount";
        this._colSymbolCount.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colSymbolCount.FillWeight = 60F;
        this._colSymbolCount.HeaderText = "Simboli";
        this._colSymbolCount.Name = "_colSymbolCount";
        this._colSymbolCount.ReadOnly = true;
        // 
        // _colEnabled
        // 
        this._colEnabled.DataPropertyName = "Enabled";
        this._colEnabled.FillWeight = 60F;
        this._colEnabled.HeaderText = "Abilitato";
        this._colEnabled.Name = "_colEnabled";
        this._colEnabled.ReadOnly = true;
        // 
        // _toolbar
        // 
        this._toolbar.CreateButtonText = "Nuovo account";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per nome, codice, gruppo o broker…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Account";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // AccountListScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "AccountListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountNumber;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroupId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colBroker;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCurrency;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colInitialBalance;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbolCount;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colEnabled;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
