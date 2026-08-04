namespace piootooapp.clientform.Shell.Screens;

partial class TitanoRunDetailScreen
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
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._headlineLabel = new System.Windows.Forms.Label();
        this._commandPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._filterTextBox = new System.Windows.Forms.TextBox();
        this._onlyChangesCheckBox = new System.Windows.Forms.CheckBox();
        this._reportButton = new System.Windows.Forms.Button();
        this._hardStopButton = new System.Windows.Forms.Button();
        this._summaryLabel = new System.Windows.Forms.Label();
        this._grid = new System.Windows.Forms.DataGridView();
        this._colFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAllocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRawScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colFilters = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTransition = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        this._commandPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        //
        // _toolbar
        //
        this._toolbar.CanGoBack = true;
        this._toolbar.CanSave = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Run Titano";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        //
        // _headlineLabel
        //
        this._headlineLabel.AutoSize = true;
        this._headlineLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._headlineLabel.Location = new System.Drawing.Point(0, 44);
        this._headlineLabel.Name = "_headlineLabel";
        this._headlineLabel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
        this._headlineLabel.Size = new System.Drawing.Size(200, 27);
        this._headlineLabel.TabIndex = 1;
        this._headlineLabel.Text = "Caricamento…";
        //
        // _commandPanel
        //
        this._commandPanel.AutoSize = true;
        this._commandPanel.Controls.Add(this._filterTextBox);
        this._commandPanel.Controls.Add(this._onlyChangesCheckBox);
        this._commandPanel.Controls.Add(this._reportButton);
        this._commandPanel.Controls.Add(this._hardStopButton);
        this._commandPanel.Controls.Add(this._summaryLabel);
        this._commandPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandPanel.Location = new System.Drawing.Point(0, 71);
        this._commandPanel.Name = "_commandPanel";
        this._commandPanel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        this._commandPanel.Size = new System.Drawing.Size(900, 40);
        this._commandPanel.TabIndex = 2;
        this._commandPanel.WrapContents = false;
        //
        // _filterTextBox
        //
        this._filterTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 12, 3);
        this._filterTextBox.Name = "_filterTextBox";
        this._filterTextBox.PlaceholderText = "Filtra per strategia, stato o transizione…";
        this._filterTextBox.Size = new System.Drawing.Size(320, 23);
        this._filterTextBox.TabIndex = 0;
        this._filterTextBox.TextChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // _onlyChangesCheckBox
        //
        this._onlyChangesCheckBox.AutoSize = true;
        this._onlyChangesCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 20, 3);
        this._onlyChangesCheckBox.Name = "_onlyChangesCheckBox";
        this._onlyChangesCheckBox.Size = new System.Drawing.Size(96, 19);
        this._onlyChangesCheckBox.TabIndex = 1;
        this._onlyChangesCheckBox.Text = "Solo i cambi";
        this._onlyChangesCheckBox.UseVisualStyleBackColor = true;
        this._onlyChangesCheckBox.CheckedChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // _reportButton
        //
        this._reportButton.AutoSize = true;
        this._reportButton.Enabled = false;
        this._reportButton.Margin = new System.Windows.Forms.Padding(3, 2, 6, 3);
        this._reportButton.Name = "_reportButton";
        this._reportButton.Size = new System.Drawing.Size(90, 25);
        this._reportButton.TabIndex = 2;
        this._reportButton.Text = "Report";
        this._reportButton.UseVisualStyleBackColor = true;
        this._reportButton.Click += new System.EventHandler(this.OnReportClick);
        //
        // _hardStopButton
        //
        this._hardStopButton.AutoSize = true;
        this._hardStopButton.Enabled = false;
        this._hardStopButton.Margin = new System.Windows.Forms.Padding(3, 2, 20, 3);
        this._hardStopButton.Name = "_hardStopButton";
        this._hardStopButton.Size = new System.Drawing.Size(130, 25);
        this._hardStopButton.TabIndex = 3;
        this._hardStopButton.Text = "Sblocca hard stop";
        this._hardStopButton.UseVisualStyleBackColor = true;
        this._hardStopButton.Click += new System.EventHandler(this.OnHardStopClick);
        //
        // _summaryLabel
        //
        this._summaryLabel.AutoSize = true;
        this._summaryLabel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
        this._summaryLabel.Name = "_summaryLabel";
        this._summaryLabel.Size = new System.Drawing.Size(120, 15);
        this._summaryLabel.TabIndex = 4;
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
            this._colFrom,
            this._colTo,
            this._colStrategy,
            this._colState,
            this._colAllocation,
            this._colScore,
            this._colRawScore,
            this._colFilters,
            this._colTransition,
            this._colReason});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 111);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 489);
        this._grid.TabIndex = 3;
        //
        // _colFrom
        //
        this._colFrom.DataPropertyName = "EffectiveFromUtc";
        this._colFrom.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colFrom.FillWeight = 85F;
        this._colFrom.HeaderText = "Da (UTC)";
        this._colFrom.Name = "_colFrom";
        this._colFrom.ReadOnly = true;
        //
        // _colTo
        //
        this._colTo.DataPropertyName = "EffectiveToUtc";
        this._colTo.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colTo.FillWeight = 85F;
        this._colTo.HeaderText = "A (UTC)";
        this._colTo.Name = "_colTo";
        this._colTo.ReadOnly = true;
        //
        // _colStrategy
        //
        this._colStrategy.DataPropertyName = "StrategyCode";
        this._colStrategy.FillWeight = 100F;
        this._colStrategy.HeaderText = "Strategia";
        this._colStrategy.Name = "_colStrategy";
        this._colStrategy.ReadOnly = true;
        //
        // _colState
        //
        this._colState.DataPropertyName = "State";
        this._colState.FillWeight = 70F;
        this._colState.HeaderText = "Stato";
        this._colState.Name = "_colState";
        this._colState.ReadOnly = true;
        //
        // _colAllocation
        //
        this._colAllocation.DataPropertyName = "Allocation";
        this._colAllocation.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colAllocation.DefaultCellStyle.Format = "P0";
        this._colAllocation.FillWeight = 60F;
        this._colAllocation.HeaderText = "Alloc.";
        this._colAllocation.Name = "_colAllocation";
        this._colAllocation.ReadOnly = true;
        //
        // _colScore
        //
        this._colScore.DataPropertyName = "Score";
        this._colScore.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colScore.DefaultCellStyle.Format = "N2";
        this._colScore.FillWeight = 50F;
        this._colScore.HeaderText = "Score";
        this._colScore.Name = "_colScore";
        this._colScore.ReadOnly = true;
        //
        // _colRawScore
        //
        this._colRawScore.DataPropertyName = "RawScore";
        this._colRawScore.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colRawScore.DefaultCellStyle.Format = "N2";
        this._colRawScore.FillWeight = 55F;
        this._colRawScore.HeaderText = "Score grezzo";
        this._colRawScore.Name = "_colRawScore";
        this._colRawScore.ReadOnly = true;
        //
        // _colFilters
        //
        this._colFilters.DataPropertyName = "Filters";
        this._colFilters.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        this._colFilters.FillWeight = 45F;
        this._colFilters.HeaderText = "Voti";
        this._colFilters.Name = "_colFilters";
        this._colFilters.ReadOnly = true;
        //
        // _colTransition
        //
        this._colTransition.DataPropertyName = "Transition";
        this._colTransition.FillWeight = 90F;
        this._colTransition.HeaderText = "Transizione";
        this._colTransition.Name = "_colTransition";
        this._colTransition.ReadOnly = true;
        //
        // _colReason
        //
        this._colReason.DataPropertyName = "Reason";
        this._colReason.FillWeight = 170F;
        this._colReason.HeaderText = "Motivo";
        this._colReason.Name = "_colReason";
        this._colReason.ReadOnly = true;
        //
        // TitanoRunDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._commandPanel);
        this.Controls.Add(this._headlineLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "TitanoRunDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._commandPanel.ResumeLayout(false);
        this._commandPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.Label _headlineLabel;
    private System.Windows.Forms.FlowLayoutPanel _commandPanel;
    private System.Windows.Forms.TextBox _filterTextBox;
    private System.Windows.Forms.CheckBox _onlyChangesCheckBox;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _hardStopButton;
    private System.Windows.Forms.Label _summaryLabel;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colFrom;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategy;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colState;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAllocation;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colScore;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRawScore;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colFilters;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTransition;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReason;
}
