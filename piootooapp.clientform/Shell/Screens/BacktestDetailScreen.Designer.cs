namespace piootooapp.clientform.Shell.Screens;

partial class BacktestDetailScreen
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
        this._tradesBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._commandPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._reportButton = new System.Windows.Forms.Button();
        this._exportButton = new System.Windows.Forms.Button();
        this._generateReportButton = new System.Windows.Forms.Button();
        this._tabs = new System.Windows.Forms.TabControl();
        this._summaryTab = new System.Windows.Forms.TabPage();
        this._summaryJsonBox = new System.Windows.Forms.TextBox();
        this._summaryJsonLabel = new System.Windows.Forms.Label();
        this._diagnosticsList = new System.Windows.Forms.ListBox();
        this._diagnosticsLabel = new System.Windows.Forms.Label();
        this._headlineLabel = new System.Windows.Forms.Label();
        this._tradesTab = new System.Windows.Forms.TabPage();
        this._grid = new System.Windows.Forms.DataGridView();
        this._colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colDirection = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colEntryTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colEntryPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colExitTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colExitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colExitReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colNetProfit = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colCommission = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._tradesFilterPanel = new System.Windows.Forms.TableLayoutPanel();
        this._tradesFilterBox = new System.Windows.Forms.TextBox();
        this._tradesSummaryLabel = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)(this._tradesBindingSource)).BeginInit();
        this._tabs.SuspendLayout();
        this._summaryTab.SuspendLayout();
        this._tradesTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this._tradesFilterPanel.SuspendLayout();
        this._commandPanel.SuspendLayout();
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
        this._toolbar.Title = "Backtest";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        //
        // _commandPanel
        //
        this._commandPanel.AutoSize = true;
        this._commandPanel.Controls.Add(this._reportButton);
        this._commandPanel.Controls.Add(this._exportButton);
        this._commandPanel.Controls.Add(this._generateReportButton);
        this._commandPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandPanel.Location = new System.Drawing.Point(0, 44);
        this._commandPanel.Name = "_commandPanel";
        this._commandPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._commandPanel.Size = new System.Drawing.Size(900, 37);
        this._commandPanel.TabIndex = 1;
        //
        // _reportButton
        //
        this._reportButton.AutoSize = true;
        this._reportButton.Enabled = false;
        this._reportButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._reportButton.Name = "_reportButton";
        this._reportButton.Size = new System.Drawing.Size(120, 25);
        this._reportButton.TabIndex = 0;
        this._reportButton.Text = "Report HTML";
        this._reportButton.Click += new System.EventHandler(this.OnReportClick);
        //
        // _exportButton
        //
        this._exportButton.AutoSize = true;
        this._exportButton.Enabled = false;
        this._exportButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._exportButton.Name = "_exportButton";
        this._exportButton.Size = new System.Drawing.Size(160, 25);
        this._exportButton.TabIndex = 1;
        this._exportButton.Text = "Esporta per confronto";
        this._exportButton.Click += new System.EventHandler(this.OnExportForCompareClick);
        //
        // _generateReportButton
        //
        this._generateReportButton.AutoSize = true;
        this._generateReportButton.Enabled = false;
        this._generateReportButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._generateReportButton.Name = "_generateReportButton";
        this._generateReportButton.Size = new System.Drawing.Size(130, 25);
        this._generateReportButton.TabIndex = 2;
        this._generateReportButton.Text = "Genera report";
        this._generateReportButton.Click += new System.EventHandler(this.OnGenerateReportClick);
        //
        // _tabs
        //
        this._tabs.Controls.Add(this._summaryTab);
        this._tabs.Controls.Add(this._tradesTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 44);
        this._tabs.Name = "_tabs";
        this._tabs.Padding = new System.Drawing.Point(12, 6);
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(900, 556);
        this._tabs.TabIndex = 1;
        //
        // _summaryTab
        //
        this._summaryTab.Controls.Add(this._summaryJsonBox);
        this._summaryTab.Controls.Add(this._summaryJsonLabel);
        this._summaryTab.Controls.Add(this._diagnosticsList);
        this._summaryTab.Controls.Add(this._diagnosticsLabel);
        this._summaryTab.Controls.Add(this._headlineLabel);
        this._summaryTab.Location = new System.Drawing.Point(4, 24);
        this._summaryTab.Name = "_summaryTab";
        this._summaryTab.Padding = new System.Windows.Forms.Padding(12);
        this._summaryTab.Size = new System.Drawing.Size(892, 528);
        this._summaryTab.TabIndex = 0;
        this._summaryTab.Text = "Riepilogo";
        this._summaryTab.UseVisualStyleBackColor = true;
        //
        // _summaryJsonBox
        //
        this._summaryJsonBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._summaryJsonBox.Font = new System.Drawing.Font("Consolas", 9F);
        this._summaryJsonBox.Location = new System.Drawing.Point(12, 12);
        this._summaryJsonBox.Multiline = true;
        this._summaryJsonBox.Name = "_summaryJsonBox";
        this._summaryJsonBox.ReadOnly = true;
        this._summaryJsonBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._summaryJsonBox.Size = new System.Drawing.Size(868, 240);
        this._summaryJsonBox.TabIndex = 4;
        this._summaryJsonBox.WordWrap = false;
        //
        // _summaryJsonLabel
        //
        this._summaryJsonLabel.AutoSize = true;
        this._summaryJsonLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._summaryJsonLabel.Location = new System.Drawing.Point(12, 12);
        this._summaryJsonLabel.Name = "_summaryJsonLabel";
        this._summaryJsonLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
        this._summaryJsonLabel.Size = new System.Drawing.Size(160, 27);
        this._summaryJsonLabel.TabIndex = 3;
        this._summaryJsonLabel.Text = "backtest-summary.json";
        //
        // _diagnosticsList
        //
        this._diagnosticsList.Dock = System.Windows.Forms.DockStyle.Top;
        this._diagnosticsList.FormattingEnabled = true;
        this._diagnosticsList.HorizontalScrollbar = true;
        this._diagnosticsList.IntegralHeight = false;
        this._diagnosticsList.ItemHeight = 15;
        this._diagnosticsList.Location = new System.Drawing.Point(12, 12);
        this._diagnosticsList.Name = "_diagnosticsList";
        this._diagnosticsList.SelectionMode = System.Windows.Forms.SelectionMode.None;
        this._diagnosticsList.Size = new System.Drawing.Size(868, 150);
        this._diagnosticsList.TabIndex = 2;
        //
        // _diagnosticsLabel
        //
        this._diagnosticsLabel.AutoSize = true;
        this._diagnosticsLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._diagnosticsLabel.Location = new System.Drawing.Point(12, 12);
        this._diagnosticsLabel.Name = "_diagnosticsLabel";
        this._diagnosticsLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
        this._diagnosticsLabel.Size = new System.Drawing.Size(80, 27);
        this._diagnosticsLabel.TabIndex = 1;
        this._diagnosticsLabel.Text = "Diagnostica";
        //
        // _headlineLabel
        //
        this._headlineLabel.AutoSize = true;
        this._headlineLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._headlineLabel.Location = new System.Drawing.Point(12, 12);
        this._headlineLabel.Name = "_headlineLabel";
        this._headlineLabel.Size = new System.Drawing.Size(200, 15);
        this._headlineLabel.TabIndex = 0;
        this._headlineLabel.Text = "Caricamento…";
        //
        // _tradesTab
        //
        this._tradesTab.Controls.Add(this._grid);
        this._tradesTab.Controls.Add(this._tradesFilterPanel);
        this._tradesTab.Location = new System.Drawing.Point(4, 24);
        this._tradesTab.Name = "_tradesTab";
        this._tradesTab.Padding = new System.Windows.Forms.Padding(12);
        this._tradesTab.Size = new System.Drawing.Size(892, 528);
        this._tradesTab.TabIndex = 1;
        this._tradesTab.Text = "Operazioni";
        this._tradesTab.UseVisualStyleBackColor = true;
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
            this._colStrategy,
            this._colSymbol,
            this._colDirection,
            this._colQuantity,
            this._colEntryTime,
            this._colEntryPrice,
            this._colExitTime,
            this._colExitPrice,
            this._colExitReason,
            this._colNetProfit,
            this._colCommission,
            this._colAccount});
        this._grid.DataSource = this._tradesBindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(12, 47);
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(868, 469);
        this._grid.TabIndex = 1;
        //
        // _colStrategy
        //
        this._colStrategy.DataPropertyName = "Strategy";
        this._colStrategy.FillWeight = 110F;
        this._colStrategy.HeaderText = "Strategia";
        this._colStrategy.Name = "_colStrategy";
        this._colStrategy.ReadOnly = true;
        //
        // _colSymbol
        //
        this._colSymbol.DataPropertyName = "Symbol";
        this._colSymbol.FillWeight = 55F;
        this._colSymbol.HeaderText = "Simbolo";
        this._colSymbol.Name = "_colSymbol";
        this._colSymbol.ReadOnly = true;
        //
        // _colDirection
        //
        this._colDirection.DataPropertyName = "Direction";
        this._colDirection.FillWeight = 50F;
        this._colDirection.HeaderText = "Lato";
        this._colDirection.Name = "_colDirection";
        this._colDirection.ReadOnly = true;
        //
        // _colQuantity
        //
        this._colQuantity.DataPropertyName = "Quantity";
        this._colQuantity.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colQuantity.FillWeight = 45F;
        this._colQuantity.HeaderText = "Qtà";
        this._colQuantity.Name = "_colQuantity";
        this._colQuantity.ReadOnly = true;
        //
        // _colEntryTime
        //
        this._colEntryTime.DataPropertyName = "EntryTimeUtc";
        this._colEntryTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colEntryTime.FillWeight = 90F;
        this._colEntryTime.HeaderText = "Entrata UTC";
        this._colEntryTime.Name = "_colEntryTime";
        this._colEntryTime.ReadOnly = true;
        //
        // _colEntryPrice
        //
        this._colEntryPrice.DataPropertyName = "EntryPrice";
        this._colEntryPrice.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colEntryPrice.DefaultCellStyle.Format = "N2";
        this._colEntryPrice.FillWeight = 60F;
        this._colEntryPrice.HeaderText = "Prezzo in";
        this._colEntryPrice.Name = "_colEntryPrice";
        this._colEntryPrice.ReadOnly = true;
        //
        // _colExitTime
        //
        this._colExitTime.DataPropertyName = "ExitTimeUtc";
        this._colExitTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colExitTime.FillWeight = 90F;
        this._colExitTime.HeaderText = "Uscita UTC";
        this._colExitTime.Name = "_colExitTime";
        this._colExitTime.ReadOnly = true;
        //
        // _colExitPrice
        //
        this._colExitPrice.DataPropertyName = "ExitPrice";
        this._colExitPrice.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colExitPrice.DefaultCellStyle.Format = "N2";
        this._colExitPrice.FillWeight = 60F;
        this._colExitPrice.HeaderText = "Prezzo out";
        this._colExitPrice.Name = "_colExitPrice";
        this._colExitPrice.ReadOnly = true;
        //
        // _colExitReason
        //
        this._colExitReason.DataPropertyName = "ExitReason";
        this._colExitReason.FillWeight = 80F;
        this._colExitReason.HeaderText = "Motivo uscita";
        this._colExitReason.Name = "_colExitReason";
        this._colExitReason.ReadOnly = true;
        //
        // _colNetProfit
        //
        this._colNetProfit.DataPropertyName = "NetProfit";
        this._colNetProfit.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colNetProfit.DefaultCellStyle.Format = "N2";
        this._colNetProfit.FillWeight = 70F;
        this._colNetProfit.HeaderText = "P&&L netto";
        this._colNetProfit.Name = "_colNetProfit";
        this._colNetProfit.ReadOnly = true;
        //
        // _colCommission
        //
        this._colCommission.DataPropertyName = "Commission";
        this._colCommission.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colCommission.DefaultCellStyle.Format = "N2";
        this._colCommission.FillWeight = 60F;
        this._colCommission.HeaderText = "Commissioni";
        this._colCommission.Name = "_colCommission";
        this._colCommission.ReadOnly = true;
        //
        // _colAccount
        //
        this._colAccount.DataPropertyName = "Account";
        this._colAccount.FillWeight = 70F;
        this._colAccount.HeaderText = "Account";
        this._colAccount.Name = "_colAccount";
        this._colAccount.ReadOnly = true;
        //
        // _tradesFilterPanel
        //
        this._tradesFilterPanel.AutoSize = true;
        this._tradesFilterPanel.ColumnCount = 2;
        this._tradesFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tradesFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._tradesFilterPanel.Controls.Add(this._tradesFilterBox, 0, 0);
        this._tradesFilterPanel.Controls.Add(this._tradesSummaryLabel, 1, 0);
        this._tradesFilterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._tradesFilterPanel.Location = new System.Drawing.Point(12, 12);
        this._tradesFilterPanel.Name = "_tradesFilterPanel";
        this._tradesFilterPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
        this._tradesFilterPanel.RowCount = 1;
        this._tradesFilterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._tradesFilterPanel.Size = new System.Drawing.Size(868, 35);
        this._tradesFilterPanel.TabIndex = 0;
        //
        // _tradesFilterBox
        //
        this._tradesFilterBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._tradesFilterBox.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._tradesFilterBox.Name = "_tradesFilterBox";
        this._tradesFilterBox.PlaceholderText = "Filtra per strategia, simbolo, motivo di uscita o account…";
        this._tradesFilterBox.Size = new System.Drawing.Size(500, 23);
        this._tradesFilterBox.TabIndex = 0;
        this._tradesFilterBox.TextChanged += new System.EventHandler(this.OnTradesFilterChanged);
        //
        // _tradesSummaryLabel
        //
        this._tradesSummaryLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._tradesSummaryLabel.AutoSize = true;
        this._tradesSummaryLabel.Name = "_tradesSummaryLabel";
        this._tradesSummaryLabel.Size = new System.Drawing.Size(120, 15);
        this._tradesSummaryLabel.TabIndex = 1;
        //
        // BacktestDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._commandPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "BacktestDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._tradesBindingSource)).EndInit();
        this._tabs.ResumeLayout(false);
        this._summaryTab.ResumeLayout(false);
        this._summaryTab.PerformLayout();
        this._tradesTab.ResumeLayout(false);
        this._tradesTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this._tradesFilterPanel.ResumeLayout(false);
        this._tradesFilterPanel.PerformLayout();
        this._commandPanel.ResumeLayout(false);
        this._commandPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _tradesBindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.FlowLayoutPanel _commandPanel;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _exportButton;
    private System.Windows.Forms.Button _generateReportButton;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _summaryTab;
    private System.Windows.Forms.Label _headlineLabel;
    private System.Windows.Forms.Label _diagnosticsLabel;
    private System.Windows.Forms.ListBox _diagnosticsList;
    private System.Windows.Forms.Label _summaryJsonLabel;
    private System.Windows.Forms.TextBox _summaryJsonBox;
    private System.Windows.Forms.TabPage _tradesTab;
    private System.Windows.Forms.TableLayoutPanel _tradesFilterPanel;
    private System.Windows.Forms.TextBox _tradesFilterBox;
    private System.Windows.Forms.Label _tradesSummaryLabel;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategy;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colDirection;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colQuantity;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colEntryTime;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colEntryPrice;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colExitTime;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colExitPrice;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colExitReason;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colNetProfit;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colCommission;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccount;
}
