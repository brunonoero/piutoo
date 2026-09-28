namespace piootooapp.clientform.Shell.Screens;

partial class BrokerWorkspaceListScreen
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
        this._colBroker = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colBrokerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colActivePlans = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRetiredPlans = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategies = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccounts = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._colBroker,
            this._colBrokerName,
            this._colActivePlans,
            this._colRetiredPlans,
            this._colStrategies,
            this._colAccounts,
            this._colCreated});
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
        // _colBroker
        //
        this._colBroker.DataPropertyName = "BrokerCode";
        this._colBroker.FillWeight = 60F;
        this._colBroker.HeaderText = "Broker";
        this._colBroker.Name = "_colBroker";
        this._colBroker.ReadOnly = true;
        //
        // _colBrokerName
        //
        this._colBrokerName.DataPropertyName = "BrokerName";
        this._colBrokerName.FillWeight = 100F;
        this._colBrokerName.HeaderText = "Nome";
        this._colBrokerName.Name = "_colBrokerName";
        this._colBrokerName.ReadOnly = true;
        //
        // _colActivePlans
        //
        this._colActivePlans.DataPropertyName = "ActivePlans";
        this._colActivePlans.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colActivePlans.FillWeight = 50F;
        this._colActivePlans.HeaderText = "Piani attivi";
        this._colActivePlans.Name = "_colActivePlans";
        this._colActivePlans.ReadOnly = true;
        //
        // _colRetiredPlans
        //
        this._colRetiredPlans.DataPropertyName = "RetiredPlans";
        this._colRetiredPlans.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colRetiredPlans.FillWeight = 50F;
        this._colRetiredPlans.HeaderText = "Ritirati";
        this._colRetiredPlans.Name = "_colRetiredPlans";
        this._colRetiredPlans.ReadOnly = true;
        //
        // _colStrategies
        //
        this._colStrategies.DataPropertyName = "ActiveStrategies";
        this._colStrategies.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colStrategies.FillWeight = 50F;
        this._colStrategies.HeaderText = "Strategie";
        this._colStrategies.Name = "_colStrategies";
        this._colStrategies.ReadOnly = true;
        this._colStrategies.ToolTipText = "Strategie dei piani attivi: ognuna sta in un piano solo.";
        //
        // _colAccounts
        //
        this._colAccounts.DataPropertyName = "ActiveAccounts";
        this._colAccounts.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colAccounts.FillWeight = 50F;
        this._colAccounts.HeaderText = "Conti";
        this._colAccounts.Name = "_colAccounts";
        this._colAccounts.ReadOnly = true;
        this._colAccounts.ToolTipText = "Conti dei piani attivi: ognuno esegue un piano solo.";
        //
        // _colCreated
        //
        this._colCreated.DataPropertyName = "CreatedUtc";
        this._colCreated.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colCreated.FillWeight = 70F;
        this._colCreated.HeaderText = "Creato (UTC)";
        this._colCreated.Name = "_colCreated";
        this._colCreated.ReadOnly = true;
        //
        // _toolbar
        //
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per broker…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Produzione";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // BrokerWorkspaceListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._toolbar);
        this.Name = "BrokerWorkspaceListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colBroker;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colBrokerName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colActivePlans;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRetiredPlans;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategies;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccounts;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCreated;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
