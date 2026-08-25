namespace piootooapp.clientform.Shell.Screens;

partial class StrategyListScreen
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
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colIsActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        this._rowCountLabel = new System.Windows.Forms.Label();
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
            this._colSymbol,
            this._colName,
            this._colCode,
            this._colId,
            this._colTimeframe,
            this._colType,
            this._colIsActive});
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
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        // 
        // _colSymbol
        // 
        this._colSymbol.DataPropertyName = "Symbol";
        this._colSymbol.FillWeight = 60F;
        this._colSymbol.HeaderText = "Simbolo";
        this._colSymbol.Name = "_colSymbol";
        this._colSymbol.ReadOnly = true;
        // 
        // _colName
        // 
        this._colName.DataPropertyName = "Name";
        this._colName.FillWeight = 130F;
        this._colName.HeaderText = "Nome (codice di esecuzione)";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        // 
        // _colCode
        // 
        this._colCode.DataPropertyName = "Code";
        this._colCode.FillWeight = 80F;
        this._colCode.HeaderText = "Code";
        this._colCode.Name = "_colCode";
        this._colCode.ReadOnly = true;
        // 
        // _colId
        // 
        this._colId.DataPropertyName = "Id";
        this._colId.FillWeight = 130F;
        this._colId.HeaderText = "Id di classe (masterfilter)";
        this._colId.Name = "_colId";
        this._colId.ReadOnly = true;
        // 
        // _colTimeframe
        // 
        this._colTimeframe.DataPropertyName = "Timeframe";
        this._colTimeframe.FillWeight = 50F;
        this._colTimeframe.HeaderText = "TF";
        this._colTimeframe.Name = "_colTimeframe";
        this._colTimeframe.ReadOnly = true;
        // 
        // _colType
        // 
        this._colType.DataPropertyName = "Type";
        this._colType.FillWeight = 70F;
        this._colType.HeaderText = "Tipo";
        this._colType.Name = "_colType";
        this._colType.ReadOnly = true;
        // 
        // _colIsActive
        // 
        this._colIsActive.DataPropertyName = "IsActive";
        this._colIsActive.FillWeight = 50F;
        this._colIsActive.HeaderText = "Attiva";
        this._colIsActive.Name = "_colIsActive";
        this._colIsActive.ReadOnly = true;
        // 
        // _toolbar
        // 
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per simbolo, nome, codice, id o tipo…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Strategie";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // _rowCountLabel
        //
        this._rowCountLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._rowCountLabel.Location = new System.Drawing.Point(0, 476);
        this._rowCountLabel.Name = "_rowCountLabel";
        this._rowCountLabel.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
        this._rowCountLabel.Size = new System.Drawing.Size(900, 24);
        this._rowCountLabel.TabIndex = 2;
        this._rowCountLabel.Text = "";
        this._rowCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // StrategyListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        // L'ordine di aggiunta determina l'ordine di docking (dall'ultimo al primo):
        // toolbar in alto, conteggio righe in basso, griglia a riempire il resto.
        this.Controls.Add(this._grid);
        this.Controls.Add(this._rowCountLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "StrategyListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colType;
    private System.Windows.Forms.DataGridViewCheckBoxColumn _colIsActive;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
    private System.Windows.Forms.Label _rowCountLabel;
}
