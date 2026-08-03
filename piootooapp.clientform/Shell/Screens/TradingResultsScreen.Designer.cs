namespace piootooapp.clientform.Shell.Screens;

partial class TradingResultsScreen
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
        this._reloadButton = new System.Windows.Forms.Button();
        this._filterPanel = new System.Windows.Forms.TableLayoutPanel();
        this._filterTextBox = new System.Windows.Forms.TextBox();
        this._summaryLabel = new System.Windows.Forms.Label();
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
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        this._selectorPanel.SuspendLayout();
        this._filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();
        // 
        // _selectorPanel
        // 
        this._selectorPanel.AutoSize = true;
        this._selectorPanel.ColumnCount = 5;
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
        this._selectorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._selectorPanel.Controls.Add(this._workspaceLabel, 0, 0);
        this._selectorPanel.Controls.Add(this._workspaceCombo, 1, 0);
        this._selectorPanel.Controls.Add(this._backtestLabel, 2, 0);
        this._selectorPanel.Controls.Add(this._backtestCombo, 3, 0);
        this._selectorPanel.Controls.Add(this._reloadButton, 4, 0);
        this._selectorPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._selectorPanel.Location = new System.Drawing.Point(0, 0);
        this._selectorPanel.Name = "_selectorPanel";
        this._selectorPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
        this._selectorPanel.RowCount = 1;
        this._selectorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._selectorPanel.Size = new System.Drawing.Size(900, 43);
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
        this._workspaceCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._workspaceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._workspaceCombo.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._workspaceCombo.Name = "_workspaceCombo";
        this._workspaceCombo.Size = new System.Drawing.Size(240, 23);
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
        this._backtestCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._backtestCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._backtestCombo.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._backtestCombo.Name = "_backtestCombo";
        this._backtestCombo.Size = new System.Drawing.Size(360, 23);
        this._backtestCombo.TabIndex = 3;
        this._backtestCombo.SelectedIndexChanged += new System.EventHandler(this.OnBacktestChanged);
        // 
        // _reloadButton
        // 
        this._reloadButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._reloadButton.AutoSize = true;
        this._reloadButton.Name = "_reloadButton";
        this._reloadButton.Size = new System.Drawing.Size(90, 25);
        this._reloadButton.TabIndex = 4;
        this._reloadButton.Text = "Aggiorna";
        this._reloadButton.UseVisualStyleBackColor = true;
        this._reloadButton.Click += new System.EventHandler(this.OnReloadClick);
        // 
        // _filterPanel
        // 
        this._filterPanel.AutoSize = true;
        this._filterPanel.ColumnCount = 2;
        this._filterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._filterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._filterPanel.Controls.Add(this._filterTextBox, 0, 0);
        this._filterPanel.Controls.Add(this._summaryLabel, 1, 0);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 43);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 6);
        this._filterPanel.RowCount = 1;
        this._filterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._filterPanel.Size = new System.Drawing.Size(900, 35);
        this._filterPanel.TabIndex = 1;
        // 
        // _filterTextBox
        // 
        this._filterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._filterTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._filterTextBox.Name = "_filterTextBox";
        this._filterTextBox.PlaceholderText = "Filtra per strategia, simbolo, motivo di uscita o account…";
        this._filterTextBox.Size = new System.Drawing.Size(500, 23);
        this._filterTextBox.TabIndex = 0;
        this._filterTextBox.TextChanged += new System.EventHandler(this.OnFilterChanged);
        // 
        // _summaryLabel
        // 
        this._summaryLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._summaryLabel.AutoSize = true;
        this._summaryLabel.Name = "_summaryLabel";
        this._summaryLabel.Size = new System.Drawing.Size(120, 15);
        this._summaryLabel.TabIndex = 1;
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
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 78);
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(900, 522);
        this._grid.TabIndex = 2;
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
        // TradingResultsScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._selectorPanel);
        this.Name = "TradingResultsScreen";
        this.Size = new System.Drawing.Size(900, 600);
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._selectorPanel.ResumeLayout(false);
        this._selectorPanel.PerformLayout();
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
    private System.Windows.Forms.Button _reloadButton;
    private System.Windows.Forms.TableLayoutPanel _filterPanel;
    private System.Windows.Forms.TextBox _filterTextBox;
    private System.Windows.Forms.Label _summaryLabel;
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
