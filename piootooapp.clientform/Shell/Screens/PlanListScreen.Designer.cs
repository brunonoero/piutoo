namespace piootooapp.clientform.Shell.Screens;

partial class PlanListScreen
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
        this._colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colGroups = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMaxConcurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._colCode,
            this._colName,
            this._colGroups,
            this._colMaxConcurrent,
            this._colUpdated});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 80);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 420);
        this._grid.TabIndex = 2;
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellDoubleClick);
        this._grid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        // 
        // _colCode
        // 
        this._colCode.DataPropertyName = "Code";
        this._colCode.FillWeight = 80F;
        this._colCode.HeaderText = "Codice";
        this._colCode.Name = "_colCode";
        this._colCode.ReadOnly = true;
        // 
        // _colName
        // 
        this._colName.DataPropertyName = "Name";
        this._colName.FillWeight = 130F;
        this._colName.HeaderText = "Nome";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        // 
        // _colGroups
        // 
        this._colGroups.DataPropertyName = "Groups";
        this._colGroups.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colGroups.FillWeight = 50F;
        this._colGroups.HeaderText = "Gruppi";
        this._colGroups.Name = "_colGroups";
        this._colGroups.ReadOnly = true;
        // 
        // _colMaxConcurrent
        // 
        this._colMaxConcurrent.DataPropertyName = "MaxConcurrentTrades";
        this._colMaxConcurrent.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMaxConcurrent.FillWeight = 60F;
        this._colMaxConcurrent.HeaderText = "Max concorrenti";
        this._colMaxConcurrent.Name = "_colMaxConcurrent";
        this._colMaxConcurrent.ReadOnly = true;
        // 
        // _colUpdated
        // 
        this._colUpdated.DataPropertyName = "UpdatedUtc";
        this._colUpdated.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colUpdated.FillWeight = 90F;
        this._colUpdated.HeaderText = "Aggiornato UTC";
        this._colUpdated.Name = "_colUpdated";
        this._colUpdated.ReadOnly = true;
        // 
        // _toolbar
        // 
        this._toolbar.CreateButtonText = "Nuovo piano";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per codice o nome…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Piani di trading";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // PlanListScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "PlanListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroups;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMaxConcurrent;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colUpdated;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
