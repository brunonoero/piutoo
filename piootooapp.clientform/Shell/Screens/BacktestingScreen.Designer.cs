namespace piootooapp.clientform.Shell.Screens;

partial class BacktestingScreen
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (components != null)
            {
                components.Dispose();
            }

            DeleteTempReportFile(_reportTempHtmlPath);
            _reportTempHtmlPath = null;
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this._tabs = new System.Windows.Forms.TabControl();
        this._executionTab = new System.Windows.Forms.TabPage();
        this._reportTab = new System.Windows.Forms.TabPage();
        this._reportBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
        this._parametersGroup = new System.Windows.Forms.GroupBox();
        this._parametersLayout = new System.Windows.Forms.TableLayoutPanel();
        this._workspaceLabel = new System.Windows.Forms.Label();
        this._workspaceValueLabel = new System.Windows.Forms.Label();
        this._datafeedLabel = new System.Windows.Forms.Label();
        this._datafeedCombo = new System.Windows.Forms.ComboBox();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._weekEndCheckBox = new System.Windows.Forms.CheckBox();
        this._startLabel = new System.Windows.Forms.Label();
        this._startPicker = new System.Windows.Forms.DateTimePicker();
        this._endLabel = new System.Windows.Forms.Label();
        this._endPicker = new System.Windows.Forms.DateTimePicker();
        this._capitalLabel = new System.Windows.Forms.Label();
        this._capitalInput = new System.Windows.Forms.NumericUpDown();
        this._commissionLabel = new System.Windows.Forms.Label();
        this._commissionInput = new System.Windows.Forms.NumericUpDown();
        this._commandsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._runButton = new System.Windows.Forms.Button();
        this._cancelButton = new System.Windows.Forms.Button();
        this._reportButton = new System.Windows.Forms.Button();
        this._clearLogButton = new System.Windows.Forms.Button();
        this._progressPanel = new System.Windows.Forms.TableLayoutPanel();
        this._progressBar = new System.Windows.Forms.ProgressBar();
        this._statusLabel = new System.Windows.Forms.Label();
        this._logGroup = new System.Windows.Forms.GroupBox();
        this._logTextBox = new System.Windows.Forms.TextBox();
        this._tabs.SuspendLayout();
        this._executionTab.SuspendLayout();
        this._reportTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._reportBrowser)).BeginInit();
        this._parametersGroup.SuspendLayout();
        this._parametersLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._capitalInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).BeginInit();
        this._commandsPanel.SuspendLayout();
        this._progressPanel.SuspendLayout();
        this._logGroup.SuspendLayout();
        this.SuspendLayout();
        // 
        // _parametersGroup
        // 
        this._parametersGroup.AutoSize = true;
        this._parametersGroup.Controls.Add(this._parametersLayout);
        this._parametersGroup.Dock = System.Windows.Forms.DockStyle.Top;
        this._parametersGroup.Location = new System.Drawing.Point(0, 0);
        this._parametersGroup.Name = "_parametersGroup";
        this._parametersGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._parametersGroup.Size = new System.Drawing.Size(900, 190);
        this._parametersGroup.TabIndex = 0;
        this._parametersGroup.TabStop = false;
        this._parametersGroup.Text = "Parametri — le strategie vengono dal masterfilter del workspace";
        // 
        // _parametersLayout
        // 
        this._parametersLayout.AutoSize = true;
        this._parametersLayout.ColumnCount = 4;
        this._parametersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._parametersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._parametersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._parametersLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._parametersLayout.Controls.Add(this._workspaceLabel, 0, 0);
        this._parametersLayout.Controls.Add(this._workspaceValueLabel, 1, 0);
        this._parametersLayout.Controls.Add(this._datafeedLabel, 2, 0);
        this._parametersLayout.Controls.Add(this._datafeedCombo, 3, 0);
        this._parametersLayout.Controls.Add(this._nameLabel, 0, 1);
        this._parametersLayout.Controls.Add(this._nameTextBox, 1, 1);
        this._parametersLayout.Controls.Add(this._weekEndCheckBox, 3, 1);
        this._parametersLayout.Controls.Add(this._startLabel, 0, 2);
        this._parametersLayout.Controls.Add(this._startPicker, 1, 2);
        this._parametersLayout.Controls.Add(this._endLabel, 2, 2);
        this._parametersLayout.Controls.Add(this._endPicker, 3, 2);
        this._parametersLayout.Controls.Add(this._capitalLabel, 0, 3);
        this._parametersLayout.Controls.Add(this._capitalInput, 1, 3);
        this._parametersLayout.Controls.Add(this._commissionLabel, 2, 3);
        this._parametersLayout.Controls.Add(this._commissionInput, 3, 3);
        this._parametersLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._parametersLayout.Location = new System.Drawing.Point(12, 22);
        this._parametersLayout.Name = "_parametersLayout";
        this._parametersLayout.RowCount = 4;
        this._parametersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._parametersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._parametersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._parametersLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._parametersLayout.Size = new System.Drawing.Size(876, 156);
        this._parametersLayout.TabIndex = 0;
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
        // _workspaceValueLabel
        // 
        this._workspaceValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._workspaceValueLabel.AutoSize = true;
        this._workspaceValueLabel.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._workspaceValueLabel.Name = "_workspaceValueLabel";
        this._workspaceValueLabel.Size = new System.Drawing.Size(300, 15);
        this._workspaceValueLabel.TabIndex = 1;
        //
        // _datafeedLabel
        //
        this._datafeedLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._datafeedLabel.AutoSize = true;
        this._datafeedLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._datafeedLabel.Name = "_datafeedLabel";
        this._datafeedLabel.Size = new System.Drawing.Size(70, 15);
        this._datafeedLabel.TabIndex = 2;
        this._datafeedLabel.Text = "Datasource";
        //
        // _datafeedCombo
        //
        this._datafeedCombo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._datafeedCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._datafeedCombo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._datafeedCombo.Name = "_datafeedCombo";
        this._datafeedCombo.Size = new System.Drawing.Size(300, 23);
        this._datafeedCombo.TabIndex = 3;
        //
        // _nameLabel
        // 
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(110, 15);
        this._nameLabel.TabIndex = 4;
        this._nameLabel.Text = "Nome backtest";
        // 
        // _nameTextBox
        // 
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(300, 23);
        this._nameTextBox.TabIndex = 5;
        // 
        // _weekEndCheckBox
        // 
        this._weekEndCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._weekEndCheckBox.AutoSize = true;
        this._weekEndCheckBox.Checked = true;
        this._weekEndCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this._weekEndCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        this._weekEndCheckBox.Name = "_weekEndCheckBox";
        this._weekEndCheckBox.Size = new System.Drawing.Size(280, 19);
        this._weekEndCheckBox.TabIndex = 6;
        this._weekEndCheckBox.Text = "Chiudi posizioni e ordini a fine settimana";
        this._weekEndCheckBox.UseVisualStyleBackColor = true;
        // 
        // _startLabel
        // 
        this._startLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._startLabel.AutoSize = true;
        this._startLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._startLabel.Name = "_startLabel";
        this._startLabel.Size = new System.Drawing.Size(80, 15);
        this._startLabel.TabIndex = 7;
        this._startLabel.Text = "Inizio (UTC)";
        // 
        // _startPicker
        // 
        this._startPicker.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._startPicker.CustomFormat = "yyyy-MM-dd HH:mm";
        this._startPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this._startPicker.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._startPicker.Name = "_startPicker";
        this._startPicker.Size = new System.Drawing.Size(200, 23);
        this._startPicker.TabIndex = 8;
        // 
        // _endLabel
        // 
        this._endLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._endLabel.AutoSize = true;
        this._endLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._endLabel.Name = "_endLabel";
        this._endLabel.Size = new System.Drawing.Size(80, 15);
        this._endLabel.TabIndex = 9;
        this._endLabel.Text = "Fine (UTC)";
        // 
        // _endPicker
        // 
        this._endPicker.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._endPicker.CustomFormat = "yyyy-MM-dd HH:mm";
        this._endPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this._endPicker.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._endPicker.Name = "_endPicker";
        this._endPicker.Size = new System.Drawing.Size(200, 23);
        this._endPicker.TabIndex = 10;
        // 
        // _capitalLabel
        // 
        this._capitalLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._capitalLabel.AutoSize = true;
        this._capitalLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._capitalLabel.Name = "_capitalLabel";
        this._capitalLabel.Size = new System.Drawing.Size(110, 15);
        this._capitalLabel.TabIndex = 11;
        this._capitalLabel.Text = "Capitale iniziale";
        // 
        // _capitalInput
        // 
        this._capitalInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._capitalInput.DecimalPlaces = 2;
        this._capitalInput.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
        this._capitalInput.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._capitalInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
        this._capitalInput.Name = "_capitalInput";
        this._capitalInput.Size = new System.Drawing.Size(160, 23);
        this._capitalInput.TabIndex = 12;
        this._capitalInput.ThousandsSeparator = true;
        // Il valore effettivo è impostato dal costruttore da TradingConventions.StrategyReferenceBalance.
        this._capitalInput.Value = new decimal(new int[] { 1000000, 0, 0, 0 });
        // 
        // _commissionLabel
        // 
        this._commissionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionLabel.AutoSize = true;
        this._commissionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._commissionLabel.Name = "_commissionLabel";
        this._commissionLabel.Size = new System.Drawing.Size(170, 15);
        this._commissionLabel.TabIndex = 13;
        this._commissionLabel.Text = "Commissione per contratto";
        // 
        // _commissionInput
        // 
        this._commissionInput.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._commissionInput.DecimalPlaces = 2;
        this._commissionInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._commissionInput.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        this._commissionInput.Name = "_commissionInput";
        this._commissionInput.Size = new System.Drawing.Size(160, 23);
        this._commissionInput.TabIndex = 14;
        this._commissionInput.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // _commandsPanel
        // 
        this._commandsPanel.AutoSize = true;
        this._commandsPanel.Controls.Add(this._runButton);
        this._commandsPanel.Controls.Add(this._cancelButton);
        this._commandsPanel.Controls.Add(this._reportButton);
        this._commandsPanel.Controls.Add(this._clearLogButton);
        this._commandsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._commandsPanel.Location = new System.Drawing.Point(0, 190);
        this._commandsPanel.Name = "_commandsPanel";
        this._commandsPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
        this._commandsPanel.Size = new System.Drawing.Size(900, 41);
        this._commandsPanel.TabIndex = 1;
        this._commandsPanel.WrapContents = false;
        // 
        // _runButton
        // 
        this._runButton.AutoSize = true;
        this._runButton.Name = "_runButton";
        this._runButton.Size = new System.Drawing.Size(110, 27);
        this._runButton.TabIndex = 0;
        this._runButton.Text = "Avvia backtest";
        this._runButton.UseVisualStyleBackColor = true;
        this._runButton.Click += new System.EventHandler(this.OnRunClick);
        // 
        // _cancelButton
        // 
        this._cancelButton.AutoSize = true;
        this._cancelButton.Enabled = false;
        this._cancelButton.Name = "_cancelButton";
        this._cancelButton.Size = new System.Drawing.Size(90, 27);
        this._cancelButton.TabIndex = 1;
        this._cancelButton.Text = "Annulla";
        this._cancelButton.UseVisualStyleBackColor = true;
        this._cancelButton.Click += new System.EventHandler(this.OnCancelClick);
        // 
        // _reportButton
        // 
        this._reportButton.AutoSize = true;
        this._reportButton.Enabled = false;
        this._reportButton.Name = "_reportButton";
        this._reportButton.Size = new System.Drawing.Size(100, 27);
        this._reportButton.TabIndex = 2;
        this._reportButton.Text = "Apri report";
        this._reportButton.UseVisualStyleBackColor = true;
        this._reportButton.Click += new System.EventHandler(this.OnReportClick);
        // 
        // _clearLogButton
        // 
        this._clearLogButton.AutoSize = true;
        this._clearLogButton.Name = "_clearLogButton";
        this._clearLogButton.Size = new System.Drawing.Size(90, 27);
        this._clearLogButton.TabIndex = 3;
        this._clearLogButton.Text = "Pulisci log";
        this._clearLogButton.UseVisualStyleBackColor = true;
        this._clearLogButton.Click += new System.EventHandler(this.OnClearLogClick);
        // 
        // _progressPanel
        // 
        this._progressPanel.AutoSize = true;
        this._progressPanel.ColumnCount = 2;
        this._progressPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
        this._progressPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
        this._progressPanel.Controls.Add(this._progressBar, 0, 0);
        this._progressPanel.Controls.Add(this._statusLabel, 1, 0);
        this._progressPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._progressPanel.Location = new System.Drawing.Point(0, 231);
        this._progressPanel.Name = "_progressPanel";
        this._progressPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._progressPanel.RowCount = 1;
        this._progressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._progressPanel.Size = new System.Drawing.Size(900, 36);
        this._progressPanel.TabIndex = 2;
        // 
        // _progressBar
        // 
        this._progressBar.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._progressBar.Margin = new System.Windows.Forms.Padding(3, 4, 20, 4);
        this._progressBar.Name = "_progressBar";
        this._progressBar.Size = new System.Drawing.Size(320, 20);
        this._progressBar.TabIndex = 0;
        // 
        // _statusLabel
        // 
        this._statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._statusLabel.AutoSize = true;
        this._statusLabel.Name = "_statusLabel";
        this._statusLabel.Size = new System.Drawing.Size(120, 15);
        this._statusLabel.TabIndex = 1;
        // 
        // _logGroup
        // 
        this._logGroup.Controls.Add(this._logTextBox);
        this._logGroup.Dock = System.Windows.Forms.DockStyle.Fill;
        this._logGroup.Location = new System.Drawing.Point(0, 267);
        this._logGroup.Name = "_logGroup";
        this._logGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._logGroup.Size = new System.Drawing.Size(900, 333);
        this._logGroup.TabIndex = 3;
        this._logGroup.TabStop = false;
        this._logGroup.Text = "Log del client";
        // 
        // _logTextBox
        // 
        this._logTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._logTextBox.Font = new System.Drawing.Font("Consolas", 9F);
        this._logTextBox.Location = new System.Drawing.Point(12, 22);
        this._logTextBox.Multiline = true;
        this._logTextBox.Name = "_logTextBox";
        this._logTextBox.ReadOnly = true;
        this._logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._logTextBox.Size = new System.Drawing.Size(876, 299);
        this._logTextBox.TabIndex = 0;
        // 
        // _tabs
        //
        this._tabs.Controls.Add(this._executionTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 0);
        this._tabs.Name = "_tabs";
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(900, 600);
        this._tabs.TabIndex = 0;
        //
        // _executionTab
        //
        this._executionTab.Controls.Add(this._logGroup);
        this._executionTab.Controls.Add(this._progressPanel);
        this._executionTab.Controls.Add(this._commandsPanel);
        this._executionTab.Controls.Add(this._parametersGroup);
        this._executionTab.Location = new System.Drawing.Point(4, 24);
        this._executionTab.Name = "_executionTab";
        this._executionTab.Padding = new System.Windows.Forms.Padding(3);
        this._executionTab.Size = new System.Drawing.Size(892, 572);
        this._executionTab.TabIndex = 0;
        this._executionTab.Text = "Esecuzione";
        //
        // _reportTab
        //
        // Non entra in _tabs.TabPages qui: viene aggiunta a runtime solo se il report è
        // stato effettivamente creato (vedi BacktestingScreen.cs, ShowReportTabAsync).
        this._reportTab.Controls.Add(this._reportBrowser);
        this._reportTab.Location = new System.Drawing.Point(4, 24);
        this._reportTab.Name = "_reportTab";
        this._reportTab.Padding = new System.Windows.Forms.Padding(3);
        this._reportTab.Size = new System.Drawing.Size(892, 572);
        this._reportTab.TabIndex = 1;
        this._reportTab.Text = "Report";
        //
        // _reportBrowser
        //
        this._reportBrowser.AllowExternalDrop = true;
        this._reportBrowser.CreationProperties = null;
        this._reportBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
        this._reportBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
        this._reportBrowser.Location = new System.Drawing.Point(3, 3);
        this._reportBrowser.Name = "_reportBrowser";
        this._reportBrowser.Size = new System.Drawing.Size(886, 566);
        this._reportBrowser.TabIndex = 0;
        this._reportBrowser.ZoomFactor = 1D;
        //
        // BacktestingScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Name = "BacktestingScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._tabs.ResumeLayout(false);
        this._executionTab.ResumeLayout(false);
        this._executionTab.PerformLayout();
        this._reportTab.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._reportBrowser)).EndInit();
        this._parametersGroup.ResumeLayout(false);
        this._parametersGroup.PerformLayout();
        this._parametersLayout.ResumeLayout(false);
        this._parametersLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._capitalInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._commissionInput)).EndInit();
        this._commandsPanel.ResumeLayout(false);
        this._commandsPanel.PerformLayout();
        this._progressPanel.ResumeLayout(false);
        this._progressPanel.PerformLayout();
        this._logGroup.ResumeLayout(false);
        this._logGroup.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _executionTab;
    private System.Windows.Forms.TabPage _reportTab;
    private Microsoft.Web.WebView2.WinForms.WebView2 _reportBrowser;
    private System.Windows.Forms.GroupBox _parametersGroup;
    private System.Windows.Forms.TableLayoutPanel _parametersLayout;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.Label _workspaceValueLabel;
    private System.Windows.Forms.Label _datafeedLabel;
    private System.Windows.Forms.ComboBox _datafeedCombo;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.CheckBox _weekEndCheckBox;
    private System.Windows.Forms.Label _startLabel;
    private System.Windows.Forms.DateTimePicker _startPicker;
    private System.Windows.Forms.Label _endLabel;
    private System.Windows.Forms.DateTimePicker _endPicker;
    private System.Windows.Forms.Label _capitalLabel;
    private System.Windows.Forms.NumericUpDown _capitalInput;
    private System.Windows.Forms.Label _commissionLabel;
    private System.Windows.Forms.NumericUpDown _commissionInput;
    private System.Windows.Forms.FlowLayoutPanel _commandsPanel;
    private System.Windows.Forms.Button _runButton;
    private System.Windows.Forms.Button _cancelButton;
    private System.Windows.Forms.Button _reportButton;
    private System.Windows.Forms.Button _clearLogButton;
    private System.Windows.Forms.TableLayoutPanel _progressPanel;
    private System.Windows.Forms.ProgressBar _progressBar;
    private System.Windows.Forms.Label _statusLabel;
    private System.Windows.Forms.GroupBox _logGroup;
    private System.Windows.Forms.TextBox _logTextBox;
}
