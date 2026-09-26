namespace piootooapp.clientform.Shell.Screens;

partial class BestPlanDetailScreen
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
        this._yearsBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._strategiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._commandPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._reportButton = new System.Windows.Forms.Button();
        this._removeButton = new System.Windows.Forms.Button();
        this._headlineLabel = new System.Windows.Forms.Label();
        this._tabs = new System.Windows.Forms.TabControl();
        this._equityTab = new System.Windows.Forms.TabPage();
        this._chart = new piootooapp.clientform.Shell.Controls.EquityChart();
        this._yearsTab = new System.Windows.Forms.TabPage();
        this._yearsGrid = new System.Windows.Forms.DataGridView();
        this._strategiesTab = new System.Windows.Forms.TabPage();
        this._strategiesGrid = new System.Windows.Forms.DataGridView();
        this._infoTab = new System.Windows.Forms.TabPage();
        this._infoBox = new System.Windows.Forms.TextBox();
        ((System.ComponentModel.ISupportInitialize)(this._yearsBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesBindingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._yearsGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesGrid)).BeginInit();
        this._commandPanel.SuspendLayout();
        this._tabs.SuspendLayout();
        this._equityTab.SuspendLayout();
        this._yearsTab.SuspendLayout();
        this._strategiesTab.SuspendLayout();
        this._infoTab.SuspendLayout();
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
        this._toolbar.Title = "Best plan";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        //
        // _commandPanel
        //
        this._commandPanel.AutoSize = true;
        this._commandPanel.Controls.Add(this._reportButton);
        this._commandPanel.Controls.Add(this._removeButton);
        this._commandPanel.Dock = System.Windows.Forms.DockStyle.Top;
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
        // _removeButton
        //
        this._removeButton.AutoSize = true;
        this._removeButton.Enabled = false;
        this._removeButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        this._removeButton.Name = "_removeButton";
        this._removeButton.Size = new System.Drawing.Size(150, 25);
        this._removeButton.TabIndex = 1;
        this._removeButton.Text = "Rimuovi dai best plan";
        this._removeButton.Click += new System.EventHandler(this.OnRemoveClick);
        //
        // _headlineLabel
        //
        this._headlineLabel.AutoSize = true;
        this._headlineLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._headlineLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
        this._headlineLabel.Name = "_headlineLabel";
        this._headlineLabel.Padding = new System.Windows.Forms.Padding(14, 4, 12, 8);
        this._headlineLabel.TabIndex = 2;
        this._headlineLabel.Text = "Caricamento…";
        //
        // _tabs
        //
        this._tabs.Controls.Add(this._equityTab);
        this._tabs.Controls.Add(this._yearsTab);
        this._tabs.Controls.Add(this._strategiesTab);
        this._tabs.Controls.Add(this._infoTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Name = "_tabs";
        this._tabs.Padding = new System.Drawing.Point(12, 6);
        this._tabs.SelectedIndex = 0;
        this._tabs.TabIndex = 3;
        //
        // _equityTab
        //
        this._equityTab.Controls.Add(this._chart);
        this._equityTab.Name = "_equityTab";
        this._equityTab.Padding = new System.Windows.Forms.Padding(8);
        this._equityTab.Text = "Equity";
        this._equityTab.UseVisualStyleBackColor = true;
        //
        // _chart
        //
        this._chart.Dock = System.Windows.Forms.DockStyle.Fill;
        this._chart.Name = "_chart";
        //
        // _yearsTab
        //
        this._yearsTab.Controls.Add(this._yearsGrid);
        this._yearsTab.Name = "_yearsTab";
        this._yearsTab.Padding = new System.Windows.Forms.Padding(8);
        this._yearsTab.Text = "Per anno";
        this._yearsTab.UseVisualStyleBackColor = true;
        //
        // _yearsGrid
        //
        ConfigureGrid(this._yearsGrid);
        this._yearsGrid.Name = "_yearsGrid";
        this._yearsGrid.DataSource = this._yearsBindingSource;
        this._yearsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            NewColumn("Year", "Anno", 45F),
            NewColumn("StartEquity", "Equity iniziale", 90F, "N2", true),
            NewColumn("EndEquity", "Equity finale", 90F, "N2", true),
            NewColumn("NetProfit", "P&&L", 80F, "N2", true),
            NewColumn("ReturnPercent", "Return %", 60F, "N2", true),
            NewColumn("MaxDrawdown", "Max DD", 80F, "N2", true),
            NewColumn("MaxDrawdownPercent", "Max DD %", 60F, "N2", true),
            NewColumn("Trades", "Trade", 50F, null, true),
            NewColumn("WinningTrades", "Vinti", 50F, null, true),
            NewColumn("LosingTrades", "Persi", 50F, null, true)});
        this._yearsGrid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.OnProfitCellFormatting);
        //
        // _strategiesTab
        //
        this._strategiesTab.Controls.Add(this._strategiesGrid);
        this._strategiesTab.Name = "_strategiesTab";
        this._strategiesTab.Padding = new System.Windows.Forms.Padding(8);
        this._strategiesTab.Text = "Strategie";
        this._strategiesTab.UseVisualStyleBackColor = true;
        //
        // _strategiesGrid
        //
        ConfigureGrid(this._strategiesGrid);
        this._strategiesGrid.Name = "_strategiesGrid";
        this._strategiesGrid.DataSource = this._strategiesBindingSource;
        this._strategiesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            NewColumn("StrategyCode", "Strategia", 160F),
            NewColumn("Symbol", "Simbolo", 55F),
            NewColumn("TimeframeMinutes", "TF (min)", 50F, null, true),
            NewColumn("Trades", "Trade", 50F, null, true),
            NewColumn("WinningTrades", "Vinti", 50F, null, true),
            NewColumn("NetProfit", "P&&L netto", 80F, "N2", true)});
        this._strategiesGrid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.OnProfitCellFormatting);
        //
        // _infoTab
        //
        this._infoTab.Controls.Add(this._infoBox);
        this._infoTab.Name = "_infoTab";
        this._infoTab.Padding = new System.Windows.Forms.Padding(8);
        this._infoTab.Text = "Provenienza";
        this._infoTab.UseVisualStyleBackColor = true;
        //
        // _infoBox
        //
        this._infoBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._infoBox.Font = new System.Drawing.Font("Consolas", 9.5F);
        this._infoBox.Multiline = true;
        this._infoBox.Name = "_infoBox";
        this._infoBox.ReadOnly = true;
        this._infoBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._infoBox.WordWrap = false;
        //
        // BestPlanDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._headlineLabel);
        this.Controls.Add(this._commandPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "BestPlanDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._yearsBindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesBindingSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._yearsGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._strategiesGrid)).EndInit();
        this._commandPanel.ResumeLayout(false);
        this._commandPanel.PerformLayout();
        this._tabs.ResumeLayout(false);
        this._equityTab.ResumeLayout(false);
        this._yearsTab.ResumeLayout(false);
        this._strategiesTab.ResumeLayout(false);
        this._infoTab.ResumeLayout(false);
        this._infoTab.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _yearsBindingSource;
    private System.Windows.Forms.BindingSource _strategiesBindingSource;
    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.FlowLayoutPanel _commandPanel;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _removeButton;
    private System.Windows.Forms.Label _headlineLabel;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _equityTab;
    private piootooapp.clientform.Shell.Controls.EquityChart _chart;
    private System.Windows.Forms.TabPage _yearsTab;
    private System.Windows.Forms.DataGridView _yearsGrid;
    private System.Windows.Forms.TabPage _strategiesTab;
    private System.Windows.Forms.DataGridView _strategiesGrid;
    private System.Windows.Forms.TabPage _infoTab;
    private System.Windows.Forms.TextBox _infoBox;
}
