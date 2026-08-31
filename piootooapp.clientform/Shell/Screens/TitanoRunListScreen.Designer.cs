namespace piootooapp.clientform.Shell.Screens;

partial class TitanoRunListScreen
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
        this._colRunId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colBacktestFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colGeneratedAtUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPeriodCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._colRunId,
            this._colBacktestFolder,
            this._colGeneratedAtUtc,
            this._colPeriodCount});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 82);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 418);
        this._grid.TabIndex = 2;
        this._grid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellDoubleClick);
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        //
        // _colRunId
        //
        this._colRunId.DataPropertyName = "RunId";
        this._colRunId.FillWeight = 200F;
        this._colRunId.HeaderText = "Run";
        this._colRunId.Name = "_colRunId";
        this._colRunId.ReadOnly = true;
        //
        // _colBacktestFolder
        //
        this._colBacktestFolder.DataPropertyName = "BacktestFolder";
        this._colBacktestFolder.FillWeight = 140F;
        this._colBacktestFolder.HeaderText = "Backtest di origine";
        this._colBacktestFolder.Name = "_colBacktestFolder";
        this._colBacktestFolder.ReadOnly = true;
        //
        // _colGeneratedAtUtc
        //
        this._colGeneratedAtUtc.DataPropertyName = "GeneratedAtUtc";
        this._colGeneratedAtUtc.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colGeneratedAtUtc.FillWeight = 90F;
        this._colGeneratedAtUtc.HeaderText = "Generato (UTC)";
        this._colGeneratedAtUtc.Name = "_colGeneratedAtUtc";
        this._colGeneratedAtUtc.ReadOnly = true;
        //
        // _colPeriodCount
        //
        this._colPeriodCount.DataPropertyName = "PeriodCount";
        this._colPeriodCount.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colPeriodCount.FillWeight = 50F;
        this._colPeriodCount.HeaderText = "Periodi";
        this._colPeriodCount.Name = "_colPeriodCount";
        this._colPeriodCount.ReadOnly = true;
        //
        // _toolbar
        //
        this._toolbar.CreateButtonText = "Nuova rotazione";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per run o backtest…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Run Titano";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // TitanoRunListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "TitanoRunListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRunId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colBacktestFolder;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGeneratedAtUtc;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPeriodCount;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
