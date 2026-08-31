namespace piootooapp.clientform.Shell.Screens;

partial class DatafeedListScreen
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
        this._colSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colFirstBar = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colLastBar = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colDays = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCandleCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colClock = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colLastWrite = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colNote = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._filterPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._sourceLabel = new System.Windows.Forms.Label();
        this._sourceCombo = new System.Windows.Forms.ComboBox();
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
            this._colSource,
            this._colSymbol,
            this._colTimeframe,
            this._colFirstBar,
            this._colLastBar,
            this._colDays,
            this._colCandleCount,
            this._colClock,
            this._colLastWrite,
            this._colNote});
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
        //
        // _colSource
        //
        this._colSource.DataPropertyName = "Source";
        this._colSource.FillWeight = 100F;
        this._colSource.HeaderText = "Archivio";
        this._colSource.Name = "_colSource";
        this._colSource.ReadOnly = true;
        //
        // _colSymbol
        //
        this._colSymbol.DataPropertyName = "Symbol";
        this._colSymbol.FillWeight = 60F;
        this._colSymbol.HeaderText = "Simbolo";
        this._colSymbol.Name = "_colSymbol";
        this._colSymbol.ReadOnly = true;
        //
        // _colTimeframe
        //
        this._colTimeframe.DataPropertyName = "TimeframeMinutes";
        this._colTimeframe.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colTimeframe.FillWeight = 55F;
        this._colTimeframe.HeaderText = "TF (min)";
        this._colTimeframe.Name = "_colTimeframe";
        this._colTimeframe.ReadOnly = true;
        //
        // _colFirstBar
        //
        this._colFirstBar.DataPropertyName = "FirstBarUtc";
        this._colFirstBar.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colFirstBar.FillWeight = 95F;
        this._colFirstBar.HeaderText = "Prima barra (UTC)";
        this._colFirstBar.Name = "_colFirstBar";
        this._colFirstBar.ReadOnly = true;
        //
        // _colLastBar
        //
        this._colLastBar.DataPropertyName = "LastBarUtc";
        this._colLastBar.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colLastBar.FillWeight = 95F;
        this._colLastBar.HeaderText = "Ultima barra (UTC)";
        this._colLastBar.Name = "_colLastBar";
        this._colLastBar.ReadOnly = true;
        //
        // _colDays
        //
        this._colDays.DataPropertyName = "Days";
        this._colDays.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colDays.FillWeight = 50F;
        this._colDays.HeaderText = "Giorni";
        this._colDays.Name = "_colDays";
        this._colDays.ReadOnly = true;
        //
        // _colCandleCount
        //
        this._colCandleCount.DataPropertyName = "CandleCount";
        this._colCandleCount.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colCandleCount.DefaultCellStyle.Format = "N0";
        this._colCandleCount.FillWeight = 65F;
        this._colCandleCount.HeaderText = "Barre";
        this._colCandleCount.Name = "_colCandleCount";
        this._colCandleCount.ReadOnly = true;
        //
        // _colClock
        //
        this._colClock.DataPropertyName = "Clock";
        this._colClock.FillWeight = 65F;
        this._colClock.HeaderText = "Fuso";
        this._colClock.Name = "_colClock";
        this._colClock.ReadOnly = true;
        //
        // _colLastWrite
        //
        this._colLastWrite.DataPropertyName = "LastWriteUtc";
        this._colLastWrite.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colLastWrite.FillWeight = 95F;
        this._colLastWrite.HeaderText = "File aggiornato (UTC)";
        this._colLastWrite.Name = "_colLastWrite";
        this._colLastWrite.ReadOnly = true;
        //
        // _colNote
        //
        this._colNote.DataPropertyName = "Note";
        this._colNote.FillWeight = 90F;
        this._colNote.HeaderText = "Note";
        this._colNote.Name = "_colNote";
        this._colNote.ReadOnly = true;
        //
        // _filterPanel
        //
        this._filterPanel.AutoSize = true;
        this._filterPanel.Controls.Add(this._sourceLabel);
        this._filterPanel.Controls.Add(this._sourceCombo);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 44);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._filterPanel.Size = new System.Drawing.Size(900, 38);
        this._filterPanel.TabIndex = 1;
        this._filterPanel.WrapContents = false;
        //
        // _sourceLabel
        //
        this._sourceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sourceLabel.AutoSize = true;
        this._sourceLabel.Margin = new System.Windows.Forms.Padding(24, 6, 8, 0);
        this._sourceLabel.Name = "_sourceLabel";
        this._sourceLabel.Size = new System.Drawing.Size(52, 15);
        this._sourceLabel.TabIndex = 0;
        this._sourceLabel.Text = "Archivio";
        //
        // _sourceCombo
        //
        this._sourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._sourceCombo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3);
        this._sourceCombo.Name = "_sourceCombo";
        this._sourceCombo.Size = new System.Drawing.Size(180, 23);
        this._sourceCombo.TabIndex = 1;
        this._sourceCombo.SelectedIndexChanged += new System.EventHandler(this.OnSourceFilterChanged);
        //
        // _toolbar
        //
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per simbolo o archivio…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Datafeed";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // DatafeedListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "DatafeedListScreen";
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
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSource;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colFirstBar;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colLastBar;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colDays;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCandleCount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colClock;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colLastWrite;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colNote;
    private System.Windows.Forms.FlowLayoutPanel _filterPanel;
    private System.Windows.Forms.Label _sourceLabel;
    private System.Windows.Forms.ComboBox _sourceCombo;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
