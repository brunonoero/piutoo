namespace piootooapp.clientform.Shell.Screens;

partial class BrokerWorkspaceDetailScreen
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
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._commandPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._duplicateButton = new System.Windows.Forms.Button();
        this._retireButton = new System.Windows.Forms.Button();
        this._split = new System.Windows.Forms.SplitContainer();
        this._bindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._grid = new System.Windows.Forms.DataGridView();
        this._colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccounts = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategies = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSourcePlan = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colReplaces = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPromoted = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRetired = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._infoBox = new System.Windows.Forms.TextBox();
        this._commandPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        //
        // _toolbar
        //
        this._toolbar.CanGoBack = true;
        this._toolbar.CanSave = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Produzione";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        //
        // _commandPanel
        //
        this._commandPanel.AutoSize = true;
        this._commandPanel.Controls.Add(this._duplicateButton);
        this._commandPanel.Controls.Add(this._retireButton);
        this._commandPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandPanel.Location = new System.Drawing.Point(0, 44);
        this._commandPanel.Name = "_commandPanel";
        this._commandPanel.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
        this._commandPanel.Size = new System.Drawing.Size(900, 37);
        this._commandPanel.TabIndex = 1;
        //
        // _duplicateButton
        //
        this._duplicateButton.AutoSize = true;
        this._duplicateButton.Enabled = false;
        this._duplicateButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._duplicateButton.Name = "_duplicateButton";
        this._duplicateButton.Size = new System.Drawing.Size(170, 25);
        this._duplicateButton.TabIndex = 0;
        this._duplicateButton.Text = "Duplica con altri conti…";
        this._duplicateButton.Click += new System.EventHandler(this.OnDuplicateClick);
        //
        // _retireButton
        //
        this._retireButton.AutoSize = true;
        this._retireButton.Enabled = false;
        this._retireButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._retireButton.Name = "_retireButton";
        this._retireButton.Size = new System.Drawing.Size(110, 25);
        this._retireButton.TabIndex = 1;
        this._retireButton.Text = "Ritira piano";
        this._retireButton.Click += new System.EventHandler(this.OnRetireClick);
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 81);
        this._split.Name = "_split";
        this._split.Orientation = System.Windows.Forms.Orientation.Horizontal;
        this._split.Panel1.Controls.Add(this._grid);
        this._split.Panel2.Controls.Add(this._infoBox);
        this._split.Size = new System.Drawing.Size(900, 519);
        this._split.SplitterDistance = 220;
        this._split.TabIndex = 2;
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
            this._colState,
            this._colAccounts,
            this._colStrategies,
            this._colSize,
            this._colSourcePlan,
            this._colReplaces,
            this._colPromoted,
            this._colRetired});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.TabIndex = 0;
        this._grid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
        //
        // _colCode
        //
        this._colCode.DataPropertyName = "Code";
        this._colCode.FillWeight = 90F;
        this._colCode.HeaderText = "Piano";
        this._colCode.Name = "_colCode";
        this._colCode.ReadOnly = true;
        //
        // _colName
        //
        this._colName.DataPropertyName = "Name";
        this._colName.FillWeight = 90F;
        this._colName.HeaderText = "Nome";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        //
        // _colState
        //
        this._colState.DataPropertyName = "State";
        this._colState.FillWeight = 50F;
        this._colState.HeaderText = "Stato";
        this._colState.Name = "_colState";
        this._colState.ReadOnly = true;
        //
        // _colAccounts
        //
        this._colAccounts.DataPropertyName = "Accounts";
        this._colAccounts.FillWeight = 70F;
        this._colAccounts.HeaderText = "Conti";
        this._colAccounts.Name = "_colAccounts";
        this._colAccounts.ReadOnly = true;
        //
        // _colStrategies
        //
        this._colStrategies.DataPropertyName = "Strategies";
        this._colStrategies.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colStrategies.FillWeight = 45F;
        this._colStrategies.HeaderText = "Strategie";
        this._colStrategies.Name = "_colStrategies";
        this._colStrategies.ReadOnly = true;
        //
        // _colSize
        //
        this._colSize.DataPropertyName = "SizeMultiplier";
        this._colSize.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colSize.DefaultCellStyle.Format = "0.###";
        this._colSize.FillWeight = 40F;
        this._colSize.HeaderText = "Size (k)";
        this._colSize.Name = "_colSize";
        this._colSize.ReadOnly = true;
        //
        // _colSourcePlan
        //
        this._colSourcePlan.DataPropertyName = "SourcePlan";
        this._colSourcePlan.FillWeight = 90F;
        this._colSourcePlan.HeaderText = "Dal piano";
        this._colSourcePlan.Name = "_colSourcePlan";
        this._colSourcePlan.ReadOnly = true;
        this._colSourcePlan.ToolTipText = "Il piano di ricerca da cui e' stato promosso il best plan.";
        //
        // _colReplaces
        //
        this._colReplaces.DataPropertyName = "Replaces";
        this._colReplaces.FillWeight = 80F;
        this._colReplaces.HeaderText = "Sostituisce";
        this._colReplaces.Name = "_colReplaces";
        this._colReplaces.ReadOnly = true;
        this._colReplaces.ToolTipText = "Il piano di produzione di cui questo e' il duplicato con altri conti.";
        //
        // _colPromoted
        //
        this._colPromoted.DataPropertyName = "PromotedUtc";
        this._colPromoted.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colPromoted.FillWeight = 70F;
        this._colPromoted.HeaderText = "Creato (UTC)";
        this._colPromoted.Name = "_colPromoted";
        this._colPromoted.ReadOnly = true;
        //
        // _colRetired
        //
        this._colRetired.DataPropertyName = "RetiredUtc";
        this._colRetired.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colRetired.FillWeight = 70F;
        this._colRetired.HeaderText = "Ritirato (UTC)";
        this._colRetired.Name = "_colRetired";
        this._colRetired.ReadOnly = true;
        //
        // _infoBox
        //
        this._infoBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._infoBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._infoBox.Font = new System.Drawing.Font("Consolas", 9F);
        this._infoBox.Multiline = true;
        this._infoBox.Name = "_infoBox";
        this._infoBox.ReadOnly = true;
        this._infoBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._infoBox.TabIndex = 0;
        this._infoBox.WordWrap = false;
        //
        // BrokerWorkspaceDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._split);
        this.Controls.Add(this._commandPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "BrokerWorkspaceDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._commandPanel.ResumeLayout(false);
        this._commandPanel.PerformLayout();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        this._split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.FlowLayoutPanel _commandPanel;
    private System.Windows.Forms.Button _duplicateButton;
    private System.Windows.Forms.Button _retireButton;
    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colState;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccounts;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategies;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSize;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSourcePlan;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReplaces;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPromoted;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRetired;
    private System.Windows.Forms.TextBox _infoBox;
}
