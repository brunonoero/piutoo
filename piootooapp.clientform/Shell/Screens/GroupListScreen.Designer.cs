namespace piootooapp.clientform.Shell.Screens;

partial class GroupListScreen
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
        this._colGroupId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccounts = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._colGroupId,
            this._colAccountCount,
            this._colAccounts});
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
        // 
        // _colGroupId
        // 
        this._colGroupId.DataPropertyName = "GroupId";
        this._colGroupId.FillWeight = 80F;
        this._colGroupId.HeaderText = "Gruppo";
        this._colGroupId.Name = "_colGroupId";
        this._colGroupId.ReadOnly = true;
        // 
        // _colAccountCount
        // 
        this._colAccountCount.DataPropertyName = "AccountCount";
        this._colAccountCount.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colAccountCount.FillWeight = 50F;
        this._colAccountCount.HeaderText = "Account";
        this._colAccountCount.Name = "_colAccountCount";
        this._colAccountCount.ReadOnly = true;
        // 
        // _colAccounts
        // 
        this._colAccounts.DataPropertyName = "Accounts";
        this._colAccounts.FillWeight = 200F;
        this._colAccounts.HeaderText = "Account associati";
        this._colAccounts.Name = "_colAccounts";
        this._colAccounts.ReadOnly = true;
        // 
        // _toolbar
        // 
        this._toolbar.CreateButtonText = "Nuovo gruppo";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per gruppo o account…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Gruppi";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.DeleteRequested += new System.EventHandler(this.OnDeleteRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // GroupListScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "GroupListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroupId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountCount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccounts;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
