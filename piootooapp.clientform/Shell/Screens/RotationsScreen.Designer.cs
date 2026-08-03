namespace piootooapp.clientform.Shell.Screens;

partial class RotationsScreen
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
        this._selectorPanel = new System.Windows.Forms.TableLayoutPanel();
        this._workspaceLabel = new System.Windows.Forms.Label();
        this._workspaceCombo = new System.Windows.Forms.ComboBox();
        this._backtestLabel = new System.Windows.Forms.Label();
        this._backtestCombo = new System.Windows.Forms.ComboBox();
        this._runLabel = new System.Windows.Forms.Label();
        this._runCombo = new System.Windows.Forms.ComboBox();
        this._commandsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._reloadButton = new System.Windows.Forms.Button();
        this._reportButton = new System.Windows.Forms.Button();
        this._hardStopButton = new System.Windows.Forms.Button();
        this._filterPanel = new System.Windows.Forms.TableLayoutPanel();
        this._filterTextBox = new System.Windows.Forms.TextBox();
        this._onlyChangesCheckBox = new System.Windows.Forms.CheckBox();
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
        this._selectorPanel.SuspendLayout();
        this._commandsPanel.SuspendLayout();
        this._filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        // 
        // _selectorPanel
        // 
        this._selectorPanel.AutoSize = true;
        this._selectorPanel.ColumnCount = 2;
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._selectorPanel.Controls.Add(this._workspaceLabel, 0, 0);
        this._selectorPanel.Controls.Add(this._workspaceCombo, 1, 0);
        this._selectorPanel.Controls.Add(this._backtestLabel, 0, 1);
        this._selectorPanel.Controls.Add(this._backtestCombo, 1, 1);
        this._selectorPanel.Controls.Add(this._runLabel, 0, 2);
        this._selectorPanel.Controls.Add(this._runCombo, 1, 2);
        this._selectorPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._selectorPanel.Location = new System.Drawing.Point(0, 0);
        this._selectorPanel.Name = "_selectorPanel";
        this._selectorPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
        this._selectorPanel.RowCount = 3;
        this._selectorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._selectorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._selectorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._selectorPanel.Size = new System.Drawing.Size(900, 105);
        this._selectorPanel.TabIndex = 0;
        // 
        // _workspaceLabel
        // 
        this._workspaceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._workspaceLabel.AutoSize = true;
        this._workspaceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._workspaceLabel.Name = "_workspaceLabel";
        this._workspaceLabel.Size = new System.Drawing.Size(70, 15);
        this._workspaceLabel.TabIndex = 0;
        this._workspaceLabel.Text = "Workspace";
        // 
        // _workspaceCombo
        // 
        this._workspaceCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._workspaceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._workspaceCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._workspaceCombo.Name = "_workspaceCombo";
        this._workspaceCombo.Size = new System.Drawing.Size(420, 23);
        this._workspaceCombo.TabIndex = 1;
        this._workspaceCombo.SelectedIndexChanged += new System.EventHandler(this.OnWorkspaceChanged);
        // 
        // _backtestLabel
        // 
        this._backtestLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._backtestLabel.AutoSize = true;
        this._backtestLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._backtestLabel.Name = "_backtestLabel";
        this._backtestLabel.Size = new System.Drawing.Size(56, 15);
        this._backtestLabel.TabIndex = 2;
        this._backtestLabel.Text = "Backtest";
        // 
        // _backtestCombo
        // 
        this._backtestCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._backtestCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._backtestCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._backtestCombo.Name = "_backtestCombo";
        this._backtestCombo.Size = new System.Drawing.Size(420, 23);
        this._backtestCombo.TabIndex = 3;
        this._backtestCombo.SelectedIndexChanged += new System.EventHandler(this.OnBacktestChanged);
        // 
        // _runLabel
        // 
        this._runLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._runLabel.AutoSize = true;
        this._runLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._runLabel.Name = "_runLabel";
        this._runLabel.Size = new System.Drawing.Size(70, 15);
        this._runLabel.TabIndex = 4;
        this._runLabel.Text = "Run Titano";
        // 
        // _runCombo
        // 
        this._runCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._runCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._runCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._runCombo.Name = "_runCombo";
        this._runCombo.Size = new System.Drawing.Size(420, 23);
        this._runCombo.TabIndex = 5;
        this._runCombo.SelectedIndexChanged += new System.EventHandler(this.OnRunChanged);
        // 
        // _commandsPanel
        // 
        this._commandsPanel.AutoSize = true;
        this._commandsPanel.Controls.Add(this._reloadButton);
        this._commandsPanel.Controls.Add(this._reportButton);
        this._commandsPanel.Controls.Add(this._hardStopButton);
        this._commandsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandsPanel.Location = new System.Drawing.Point(0, 105);
        this._commandsPanel.Name = "_commandsPanel";
        this._commandsPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 4);
        this._commandsPanel.Size = new System.Drawing.Size(900, 33);
        this._commandsPanel.TabIndex = 1;
        this._commandsPanel.WrapContents = false;
        // 
        // _reloadButton
        // 
        this._reloadButton.AutoSize = true;
        this._reloadButton.Name = "_reloadButton";
        this._reloadButton.Size = new System.Drawing.Size(90, 25);
        this._reloadButton.TabIndex = 0;
        this._reloadButton.Text = "Aggiorna";
        this._reloadButton.UseVisualStyleBackColor = true;
        this._reloadButton.Click += new System.EventHandler(this.OnReloadClick);
        // 
        // _reportButton
        // 
        this._reportButton.AutoSize = true;
        this._reportButton.Enabled = false;
        this._reportButton.Name = "_reportButton";
        this._reportButton.Size = new System.Drawing.Size(110, 25);
        this._reportButton.TabIndex = 1;
        this._reportButton.Text = "Apri report";
        this._reportButton.UseVisualStyleBackColor = true;
        this._reportButton.Click += new System.EventHandler(this.OnReportClick);
        // 
        // _hardStopButton
        // 
        this._hardStopButton.AutoSize = true;
        this._hardStopButton.Enabled = false;
        this._hardStopButton.Name = "_hardStopButton";
        this._hardStopButton.Size = new System.Drawing.Size(140, 25);
        this._hardStopButton.TabIndex = 2;
        this._hardStopButton.Text = "Sblocca hard stop…";
        this._hardStopButton.UseVisualStyleBackColor = true;
        this._hardStopButton.Click += new System.EventHandler(this.OnHardStopClick);
        // 
        // _filterPanel
        // 
        this._filterPanel.AutoSize = true;
        this._filterPanel.ColumnCount = 3;
        this._filterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._filterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._filterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._filterPanel.Controls.Add(this._filterTextBox, 0, 0);
        this._filterPanel.Controls.Add(this._onlyChangesCheckBox, 1, 0);
        this._filterPanel.Controls.Add(this._summaryLabel, 2, 0);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 138);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 6);
        this._filterPanel.RowCount = 1;
        this._filterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filterPanel.Size = new System.Drawing.Size(900, 35);
        this._filterPanel.TabIndex = 2;
        // 
        // _filterTextBox
        // 
        this._filterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._filterTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._filterTextBox.Name = "_filterTextBox";
        this._filterTextBox.PlaceholderText = "Filtra per strategia, stato o transizione (termini separati da spazio)…";
        this._filterTextBox.Size = new System.Drawing.Size(480, 23);
        this._filterTextBox.TabIndex = 0;
        this._filterTextBox.TextChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // _onlyChangesCheckBox
        // 
        this._onlyChangesCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._onlyChangesCheckBox.AutoSize = true;
        this._onlyChangesCheckBox.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._onlyChangesCheckBox.Name = "_onlyChangesCheckBox";
        this._onlyChangesCheckBox.Size = new System.Drawing.Size(180, 19);
        this._onlyChangesCheckBox.TabIndex = 1;
        this._onlyChangesCheckBox.Text = "Solo cambi di stato";
        this._onlyChangesCheckBox.UseVisualStyleBackColor = true;
        this._onlyChangesCheckBox.CheckedChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // _summaryLabel
        // 
        this._summaryLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._summaryLabel.AutoSize = true;
        this._summaryLabel.Name = "_summaryLabel";
        this._summaryLabel.Size = new System.Drawing.Size(100, 15);
        this._summaryLabel.TabIndex = 2;
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
        this._grid.Location = new System.Drawing.Point(0, 173);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 427);
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
        // RotationsScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._commandsPanel);
        this.Controls.Add(this._selectorPanel);
        this.Name = "RotationsScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._selectorPanel.ResumeLayout(false);
        this._selectorPanel.PerformLayout();
        this._commandsPanel.ResumeLayout(false);
        this._commandsPanel.PerformLayout();
        this._filterPanel.ResumeLayout(false);
        this._filterPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.TableLayoutPanel _selectorPanel;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.ComboBox _workspaceCombo;
    private System.Windows.Forms.Label _backtestLabel;
    private System.Windows.Forms.ComboBox _backtestCombo;
    private System.Windows.Forms.Label _runLabel;
    private System.Windows.Forms.ComboBox _runCombo;
    private System.Windows.Forms.FlowLayoutPanel _commandsPanel;
    private System.Windows.Forms.Button _reloadButton;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _hardStopButton;
    private System.Windows.Forms.TableLayoutPanel _filterPanel;
    private System.Windows.Forms.TextBox _filterTextBox;
    private System.Windows.Forms.CheckBox _onlyChangesCheckBox;
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
