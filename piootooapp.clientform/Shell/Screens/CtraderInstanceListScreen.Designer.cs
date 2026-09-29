namespace piootooapp.clientform.Shell.Screens;

partial class CtraderInstanceListScreen
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
        this._colAccount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colBot = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPlanCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colVerdict = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colProcess = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colStartedAtUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSessionStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMinutesSinceLastBar = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOpenPositions = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._commandPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._startButton = new System.Windows.Forms.Button();
        this._stopButton = new System.Windows.Forms.Button();
        this._restartButton = new System.Windows.Forms.Button();
        this._logButton = new System.Windows.Forms.Button();
        this._watchButton = new System.Windows.Forms.Button();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this._commandPanel.SuspendLayout();
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
            this._colAccount,
            this._colBot,
            this._colPlanCode,
            this._colVerdict,
            this._colProcess,
            this._colStartedAtUtc,
            this._colSessionStatus,
            this._colMinutesSinceLastBar,
            this._colOpenPositions});
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
        this._grid.SelectionChanged += new System.EventHandler(this.OnGridSelectionChanged);
        this._grid.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.OnGridRowPrePaint);
        //
        // _colName
        //
        this._colName.DataPropertyName = "Name";
        this._colName.FillWeight = 80F;
        this._colName.HeaderText = "Istanza";
        this._colName.Name = "_colName";
        this._colName.ReadOnly = true;
        //
        // _colAccount
        //
        this._colAccount.DataPropertyName = "Account";
        this._colAccount.FillWeight = 60F;
        this._colAccount.HeaderText = "Conto";
        this._colAccount.Name = "_colAccount";
        this._colAccount.ReadOnly = true;
        //
        // _colBot
        //
        this._colBot.DataPropertyName = "Bot";
        this._colBot.FillWeight = 110F;
        this._colBot.HeaderText = "Bot";
        this._colBot.Name = "_colBot";
        this._colBot.ReadOnly = true;
        //
        // _colPlanCode
        //
        this._colPlanCode.DataPropertyName = "PlanCode";
        this._colPlanCode.FillWeight = 70F;
        this._colPlanCode.HeaderText = "Piano";
        this._colPlanCode.Name = "_colPlanCode";
        this._colPlanCode.ReadOnly = true;
        //
        // _colVerdict
        //
        this._colVerdict.DataPropertyName = "Verdict";
        this._colVerdict.FillWeight = 110F;
        this._colVerdict.HeaderText = "Stato";
        this._colVerdict.Name = "_colVerdict";
        this._colVerdict.ReadOnly = true;
        //
        // _colProcess
        //
        this._colProcess.DataPropertyName = "Process";
        this._colProcess.FillWeight = 55F;
        this._colProcess.HeaderText = "Processo";
        this._colProcess.Name = "_colProcess";
        this._colProcess.ReadOnly = true;
        //
        // _colStartedAtUtc
        //
        this._colStartedAtUtc.DataPropertyName = "StartedAtUtc";
        this._colStartedAtUtc.FillWeight = 80F;
        this._colStartedAtUtc.HeaderText = "Avviato (UTC)";
        this._colStartedAtUtc.Name = "_colStartedAtUtc";
        this._colStartedAtUtc.ReadOnly = true;
        //
        // _colSessionStatus
        //
        this._colSessionStatus.DataPropertyName = "SessionStatus";
        this._colSessionStatus.FillWeight = 60F;
        this._colSessionStatus.HeaderText = "Sessione";
        this._colSessionStatus.Name = "_colSessionStatus";
        this._colSessionStatus.ReadOnly = true;
        //
        // _colMinutesSinceLastBar
        //
        this._colMinutesSinceLastBar.DataPropertyName = "MinutesSinceLastBar";
        this._colMinutesSinceLastBar.FillWeight = 55F;
        this._colMinutesSinceLastBar.HeaderText = "Ult. barra (min)";
        this._colMinutesSinceLastBar.Name = "_colMinutesSinceLastBar";
        this._colMinutesSinceLastBar.ReadOnly = true;
        //
        // _colOpenPositions
        //
        this._colOpenPositions.DataPropertyName = "OpenPositions";
        this._colOpenPositions.FillWeight = 50F;
        this._colOpenPositions.HeaderText = "Posizioni";
        this._colOpenPositions.Name = "_colOpenPositions";
        this._colOpenPositions.ReadOnly = true;
        //
        // _commandPanel
        //
        this._commandPanel.AutoSize = true;
        this._commandPanel.Controls.Add(this._startButton);
        this._commandPanel.Controls.Add(this._stopButton);
        this._commandPanel.Controls.Add(this._restartButton);
        this._commandPanel.Controls.Add(this._logButton);
        this._commandPanel.Controls.Add(this._watchButton);
        this._commandPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandPanel.Location = new System.Drawing.Point(0, 44);
        this._commandPanel.Name = "_commandPanel";
        this._commandPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._commandPanel.Size = new System.Drawing.Size(900, 38);
        this._commandPanel.TabIndex = 1;
        this._commandPanel.WrapContents = false;
        //
        // _startButton
        //
        this._startButton.AutoSize = true;
        this._startButton.Name = "_startButton";
        this._startButton.TabIndex = 0;
        this._startButton.Text = "Avvia";
        this._startButton.UseVisualStyleBackColor = true;
        this._startButton.Click += new System.EventHandler(this.OnStartClick);
        //
        // _stopButton
        //
        this._stopButton.AutoSize = true;
        this._stopButton.Name = "_stopButton";
        this._stopButton.TabIndex = 1;
        this._stopButton.Text = "Ferma…";
        this._stopButton.UseVisualStyleBackColor = true;
        this._stopButton.Click += new System.EventHandler(this.OnStopClick);
        //
        // _restartButton
        //
        this._restartButton.AutoSize = true;
        this._restartButton.Name = "_restartButton";
        this._restartButton.TabIndex = 2;
        this._restartButton.Text = "Riavvia…";
        this._restartButton.UseVisualStyleBackColor = true;
        this._restartButton.Click += new System.EventHandler(this.OnRestartClick);
        //
        // _logButton
        //
        this._logButton.AutoSize = true;
        this._logButton.Name = "_logButton";
        this._logButton.TabIndex = 3;
        this._logButton.Text = "Apri log";
        this._logButton.UseVisualStyleBackColor = true;
        this._logButton.Click += new System.EventHandler(this.OnLogClick);
        //
        // _watchButton
        //
        this._watchButton.AutoSize = true;
        this._watchButton.Name = "_watchButton";
        this._watchButton.TabIndex = 4;
        this._watchButton.Text = "Presidio del conto";
        this._watchButton.UseVisualStyleBackColor = true;
        this._watchButton.Click += new System.EventHandler(this.OnWatchClick);
        //
        // _toolbar
        //
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per istanza, conto, bot o piano…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Istanze cTrader";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // CtraderInstanceListScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._commandPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "CtraderInstanceListScreen";
        this.Size = new System.Drawing.Size(900, 500);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this._commandPanel.ResumeLayout(false);
        this._commandPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colBot;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPlanCode;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colVerdict;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colProcess;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStartedAtUtc;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSessionStatus;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMinutesSinceLastBar;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOpenPositions;
    private System.Windows.Forms.FlowLayoutPanel _commandPanel;
    private System.Windows.Forms.Button _startButton;
    private System.Windows.Forms.Button _stopButton;
    private System.Windows.Forms.Button _restartButton;
    private System.Windows.Forms.Button _logButton;
    private System.Windows.Forms.Button _watchButton;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
