namespace piootooapp.clientform.Shell.Screens;

partial class TitanoSetupListScreen
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
        this._colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colUpdatedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._colId,
            this._colDescription,
            this._colUpdatedAt});
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
        this._grid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellDoubleClick);
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        //
        // _colName
        //
        this._colName.DataPropertyName = "Name";
        this._colName.FillWeight = 90F;
        this._colName.HeaderText = "Nome";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        //
        // _colId
        //
        this._colId.DataPropertyName = "Id";
        this._colId.FillWeight = 70F;
        this._colId.HeaderText = "Id";
        this._colId.Name = "_colId";
        this._colId.ReadOnly = true;
        //
        // _colDescription
        //
        this._colDescription.DataPropertyName = "Description";
        this._colDescription.FillWeight = 180F;
        this._colDescription.HeaderText = "Descrizione";
        this._colDescription.Name = "_colDescription";
        this._colDescription.ReadOnly = true;
        //
        // _colUpdatedAt
        //
        this._colUpdatedAt.DataPropertyName = "UpdatedAt";
        this._colUpdatedAt.FillWeight = 80F;
        this._colUpdatedAt.HeaderText = "Aggiornato (UTC)";
        this._colUpdatedAt.Name = "_colUpdatedAt";
        this._colUpdatedAt.ReadOnly = true;
        //
        // _toolbar
        //
        this._toolbar.CreateButtonText = "Nuovo setup";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per nome o id…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Setup Titano";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // TitanoSetupListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "TitanoSetupListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colDescription;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colUpdatedAt;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
