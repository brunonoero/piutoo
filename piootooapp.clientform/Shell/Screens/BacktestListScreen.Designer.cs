namespace piootooapp.clientform.Shell.Screens;

partial class BacktestListScreen
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
        this._colFolderName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrigin = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colLastModifiedUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colResultsCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRange = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colNetProfitPercent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMaxDrawdownPercent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._filterPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._originLabel = new System.Windows.Forms.Label();
        this._originCombo = new System.Windows.Forms.ComboBox();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this._filterPanel.SuspendLayout();
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
            this._colFolderName,
            this._colOrigin,
            this._colVersion,
            this._colLastModifiedUtc,
            this._colResultsCount,
            this._colRange,
            this._colNetProfitPercent,
            this._colMaxDrawdownPercent});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 82);
        this._grid.MultiSelect = true;
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
        // _colFolderName
        //
        this._colFolderName.DataPropertyName = "FolderName";
        this._colFolderName.FillWeight = 160F;
        this._colFolderName.HeaderText = "Backtest";
        this._colFolderName.Name = "_colFolderName";
        this._colFolderName.ReadOnly = true;
        //
        // _colOrigin
        //
        this._colOrigin.DataPropertyName = "Origin";
        this._colOrigin.FillWeight = 80F;
        this._colOrigin.HeaderText = "Origine";
        this._colOrigin.Name = "_colOrigin";
        this._colOrigin.ReadOnly = true;
        //
        // _colVersion
        //
        this._colVersion.DataPropertyName = "Version";
        this._colVersion.FillWeight = 45F;
        this._colVersion.HeaderText = "Versione";
        this._colVersion.Name = "_colVersion";
        this._colVersion.ReadOnly = true;
        this._colVersion.ToolTipText = "Versione di chi ha generato i fill: il server per un run interno, il cBot per un run dell'engine esterno (da origin.json)";
        //
        // _colLastModifiedUtc
        //
        this._colLastModifiedUtc.DataPropertyName = "LastModifiedUtc";
        this._colLastModifiedUtc.FillWeight = 80F;
        this._colLastModifiedUtc.HeaderText = "Ultima modifica (UTC)";
        this._colLastModifiedUtc.Name = "_colLastModifiedUtc";
        this._colLastModifiedUtc.ReadOnly = true;
        //
        // _colResultsCount
        //
        this._colResultsCount.DataPropertyName = "ResultsCount";
        this._colResultsCount.FillWeight = 50F;
        this._colResultsCount.HeaderText = "Risultati";
        this._colResultsCount.Name = "_colResultsCount";
        this._colResultsCount.ReadOnly = true;
        //
        // _colRange
        //
        this._colRange.DataPropertyName = "Range";
        this._colRange.FillWeight = 110F;
        this._colRange.HeaderText = "Intervallo";
        this._colRange.Name = "_colRange";
        this._colRange.ReadOnly = true;
        //
        // _colNetProfitPercent
        //
        this._colNetProfitPercent.DataPropertyName = "NetProfitPercent";
        this._colNetProfitPercent.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colNetProfitPercent.DefaultCellStyle.Format = "N1";
        this._colNetProfitPercent.FillWeight = 55F;
        this._colNetProfitPercent.HeaderText = "Equity %";
        this._colNetProfitPercent.Name = "_colNetProfitPercent";
        this._colNetProfitPercent.ReadOnly = true;
        this._colNetProfitPercent.ToolTipText = "P&L netto in percentuale del capitale iniziale, dal backtest-summary.json";
        //
        // _colMaxDrawdownPercent
        //
        this._colMaxDrawdownPercent.DataPropertyName = "MaxDrawdownPercent";
        this._colMaxDrawdownPercent.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMaxDrawdownPercent.DefaultCellStyle.Format = "N1";
        this._colMaxDrawdownPercent.FillWeight = 55F;
        this._colMaxDrawdownPercent.HeaderText = "DD %";
        this._colMaxDrawdownPercent.Name = "_colMaxDrawdownPercent";
        this._colMaxDrawdownPercent.ReadOnly = true;
        this._colMaxDrawdownPercent.ToolTipText = "Drawdown massimo mark-to-market in percentuale dal picco di equity, dal backtest-summary.json";
        //
        // _filterPanel
        //
        this._filterPanel.AutoSize = true;
        this._filterPanel.Controls.Add(this._originLabel);
        this._filterPanel.Controls.Add(this._originCombo);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 44);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._filterPanel.Size = new System.Drawing.Size(900, 38);
        this._filterPanel.TabIndex = 1;
        this._filterPanel.WrapContents = false;
        //
        // _originLabel
        //
        this._originLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._originLabel.AutoSize = true;
        this._originLabel.Margin = new System.Windows.Forms.Padding(24, 6, 8, 0);
        this._originLabel.Name = "_originLabel";
        this._originLabel.Size = new System.Drawing.Size(48, 15);
        this._originLabel.TabIndex = 2;
        this._originLabel.Text = "Origine";
        //
        // _originCombo
        //
        this._originCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._originCombo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3);
        this._originCombo.Name = "_originCombo";
        this._originCombo.Size = new System.Drawing.Size(180, 23);
        this._originCombo.TabIndex = 3;
        this._originCombo.SelectedIndexChanged += new System.EventHandler(this.OnOriginFilterChanged);
        //
        // _toolbar
        //
        this._toolbar.CanExport = true;
        this._toolbar.CreateButtonText = "Nuovo backtest";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.ExportButtonText = "Confronta…";
        this._toolbar.FilterPlaceholder = "Filtra per nome…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Backtesting";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.ExportRequested += new System.EventHandler(this.OnCompareRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // BacktestListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "BacktestListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this._filterPanel.ResumeLayout(false);
        this._filterPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colFolderName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrigin;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colVersion;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colLastModifiedUtc;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colResultsCount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRange;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colNetProfitPercent;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMaxDrawdownPercent;
    private System.Windows.Forms.FlowLayoutPanel _filterPanel;
    private System.Windows.Forms.Label _originLabel;
    private System.Windows.Forms.ComboBox _originCombo;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
