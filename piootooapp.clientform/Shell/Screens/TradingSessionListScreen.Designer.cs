namespace piootooapp.clientform.Shell.Screens;

partial class TradingSessionListScreen
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
        this._colShortId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colWorkspaceId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPlanCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colClientRunMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colExecutionMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCreatedAtUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._filterPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._directCreateButton = new System.Windows.Forms.Button();
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
            this._colShortId,
            this._colWorkspaceId,
            this._colPlanCode,
            this._colClientRunMode,
            this._colExecutionMode,
            this._colStatus,
            this._colCreatedAtUtc});
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
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellDoubleClick);
        this._grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnGridKeyDown);
        //
        // _colShortId
        //
        this._colShortId.DataPropertyName = "ShortId";
        this._colShortId.FillWeight = 60F;
        this._colShortId.HeaderText = "Sessione";
        this._colShortId.Name = "_colShortId";
        this._colShortId.ReadOnly = true;
        //
        // _colWorkspaceId
        //
        this._colWorkspaceId.DataPropertyName = "WorkspaceId";
        this._colWorkspaceId.FillWeight = 90F;
        this._colWorkspaceId.HeaderText = "Workspace";
        this._colWorkspaceId.Name = "_colWorkspaceId";
        this._colWorkspaceId.ReadOnly = true;
        //
        // _colPlanCode
        //
        this._colPlanCode.DataPropertyName = "PlanCode";
        this._colPlanCode.FillWeight = 70F;
        this._colPlanCode.HeaderText = "Piano";
        this._colPlanCode.Name = "_colPlanCode";
        this._colPlanCode.ReadOnly = true;
        //
        // _colClientRunMode
        //
        this._colClientRunMode.DataPropertyName = "ClientRunMode";
        this._colClientRunMode.FillWeight = 70F;
        this._colClientRunMode.HeaderText = "Contesto";
        this._colClientRunMode.Name = "_colClientRunMode";
        this._colClientRunMode.ReadOnly = true;
        //
        // _colExecutionMode
        //
        this._colExecutionMode.DataPropertyName = "ExecutionMode";
        this._colExecutionMode.FillWeight = 80F;
        this._colExecutionMode.HeaderText = "Esecuzione";
        this._colExecutionMode.Name = "_colExecutionMode";
        this._colExecutionMode.ReadOnly = true;
        //
        // _colStatus
        //
        this._colStatus.DataPropertyName = "Status";
        this._colStatus.FillWeight = 60F;
        this._colStatus.HeaderText = "Stato";
        this._colStatus.Name = "_colStatus";
        this._colStatus.ReadOnly = true;
        //
        // _colCreatedAtUtc
        //
        this._colCreatedAtUtc.DataPropertyName = "CreatedAtUtc";
        this._colCreatedAtUtc.FillWeight = 90F;
        this._colCreatedAtUtc.HeaderText = "Aperta il (UTC)";
        this._colCreatedAtUtc.Name = "_colCreatedAtUtc";
        this._colCreatedAtUtc.ReadOnly = true;
        //
        // _filterPanel
        //
        this._filterPanel.AutoSize = true;
        this._filterPanel.Controls.Add(this._directCreateButton);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 44);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._filterPanel.Size = new System.Drawing.Size(900, 38);
        this._filterPanel.TabIndex = 1;
        this._filterPanel.WrapContents = false;
        //
        // _directCreateButton
        //
        this._directCreateButton.AutoSize = true;
        this._directCreateButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._directCreateButton.Name = "_directCreateButton";
        this._directCreateButton.Size = new System.Drawing.Size(220, 27);
        this._directCreateButton.TabIndex = 0;
        this._directCreateButton.Text = "Sessione diretta (senza piano)…";
        this._directCreateButton.UseVisualStyleBackColor = true;
        this._directCreateButton.Click += new System.EventHandler(this.OnDirectCreateClick);
        //
        // _toolbar
        //
        this._toolbar.CreateButtonText = "Apri da piano";
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per sessione, workspace o piano…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Sessioni di trading";
        this._toolbar.CreateRequested += new System.EventHandler(this.OnCreateRequested);
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // TradingSessionListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "TradingSessionListScreen";
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
    private System.Windows.Forms.DataGridViewTextBoxColumn _colShortId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colWorkspaceId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPlanCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colClientRunMode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colExecutionMode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStatus;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCreatedAtUtc;
    private System.Windows.Forms.FlowLayoutPanel _filterPanel;
    private System.Windows.Forms.Button _directCreateButton;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
