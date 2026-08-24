namespace piootooapp.clientform.Shell.Screens;

partial class ServerSessionMonitorScreen
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
        this._titleLabel = new System.Windows.Forms.Label();
        this._toolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._sessionLabel = new System.Windows.Forms.Label();
        this._sessionCombo = new System.Windows.Forms.ComboBox();
        this._refreshButton = new System.Windows.Forms.Button();
        this._copyButton = new System.Windows.Forms.Button();
        this._saveButton = new System.Windows.Forms.Button();
        this._output = new System.Windows.Forms.TextBox();
        this._toolbarPanel.SuspendLayout();
        this.SuspendLayout();
        //
        // _titleLabel
        //
        this._titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._titleLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this._titleLabel.Location = new System.Drawing.Point(0, 0);
        this._titleLabel.Name = "_titleLabel";
        this._titleLabel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 4);
        this._titleLabel.Size = new System.Drawing.Size(900, 34);
        this._titleLabel.TabIndex = 0;
        this._titleLabel.Text = "Stato server — sessioni di trading";
        //
        // _toolbarPanel
        //
        this._toolbarPanel.AutoSize = true;
        this._toolbarPanel.Controls.Add(this._sessionLabel);
        this._toolbarPanel.Controls.Add(this._sessionCombo);
        this._toolbarPanel.Controls.Add(this._refreshButton);
        this._toolbarPanel.Controls.Add(this._copyButton);
        this._toolbarPanel.Controls.Add(this._saveButton);
        this._toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbarPanel.Location = new System.Drawing.Point(0, 34);
        this._toolbarPanel.Name = "_toolbarPanel";
        this._toolbarPanel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        this._toolbarPanel.Size = new System.Drawing.Size(900, 44);
        this._toolbarPanel.TabIndex = 1;
        this._toolbarPanel.WrapContents = false;
        //
        // _sessionLabel
        //
        this._sessionLabel.AutoSize = true;
        this._sessionLabel.Margin = new System.Windows.Forms.Padding(3, 8, 6, 3);
        this._sessionLabel.Name = "_sessionLabel";
        this._sessionLabel.Size = new System.Drawing.Size(55, 15);
        this._sessionLabel.TabIndex = 0;
        this._sessionLabel.Text = "Sessione";
        //
        // _sessionCombo
        //
        this._sessionCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._sessionCombo.Margin = new System.Windows.Forms.Padding(3, 4, 12, 3);
        this._sessionCombo.Name = "_sessionCombo";
        this._sessionCombo.Size = new System.Drawing.Size(460, 23);
        this._sessionCombo.TabIndex = 1;
        this._sessionCombo.SelectedIndexChanged += new System.EventHandler(this.OnSessionChanged);
        //
        // _refreshButton
        //
        this._refreshButton.AutoSize = true;
        this._refreshButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._refreshButton.Name = "_refreshButton";
        this._refreshButton.Size = new System.Drawing.Size(90, 25);
        this._refreshButton.TabIndex = 2;
        this._refreshButton.Text = "Aggiorna";
        this._refreshButton.UseVisualStyleBackColor = true;
        this._refreshButton.Click += new System.EventHandler(this.OnRefreshClick);
        //
        // _copyButton
        //
        this._copyButton.AutoSize = true;
        this._copyButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._copyButton.Name = "_copyButton";
        this._copyButton.Size = new System.Drawing.Size(90, 25);
        this._copyButton.TabIndex = 3;
        this._copyButton.Text = "Copia tutto";
        this._copyButton.UseVisualStyleBackColor = true;
        this._copyButton.Click += new System.EventHandler(this.OnCopyClick);
        //
        // _saveButton
        //
        this._saveButton.AutoSize = true;
        this._saveButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._saveButton.Name = "_saveButton";
        this._saveButton.Size = new System.Drawing.Size(110, 25);
        this._saveButton.TabIndex = 4;
        this._saveButton.Text = "Salva su file…";
        this._saveButton.UseVisualStyleBackColor = true;
        this._saveButton.Click += new System.EventHandler(this.OnSaveClick);
        //
        // _output
        //
        this._output.BackColor = System.Drawing.SystemColors.Window;
        this._output.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this._output.Dock = System.Windows.Forms.DockStyle.Fill;
        this._output.Font = new System.Drawing.Font("Consolas", 9F);
        this._output.HideSelection = false;
        this._output.Location = new System.Drawing.Point(0, 78);
        // Il default di una TextBox multilinea è 32767 caratteri: un dump di sessione con
        // segnali e trade lo supera, e il testo verrebbe troncato senza dire niente.
        this._output.MaxLength = 0;
        this._output.Multiline = true;
        this._output.Name = "_output";
        this._output.ReadOnly = true;
        this._output.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._output.Size = new System.Drawing.Size(900, 422);
        this._output.TabIndex = 2;
        this._output.WordWrap = false;
        //
        // ServerSessionMonitorScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._output);
        this.Controls.Add(this._toolbarPanel);
        this.Controls.Add(this._titleLabel);
        this.Name = "ServerSessionMonitorScreen";
        this.Size = new System.Drawing.Size(900, 500);
        this._toolbarPanel.ResumeLayout(false);
        this._toolbarPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label _titleLabel;
    private System.Windows.Forms.FlowLayoutPanel _toolbarPanel;
    private System.Windows.Forms.Label _sessionLabel;
    private System.Windows.Forms.ComboBox _sessionCombo;
    private System.Windows.Forms.Button _refreshButton;
    private System.Windows.Forms.Button _copyButton;
    private System.Windows.Forms.Button _saveButton;
    private System.Windows.Forms.TextBox _output;
}
