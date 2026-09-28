namespace piootooapp.clientform.Shell.Controls;

partial class ProductionPlanDialog
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

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this._introLabel = new System.Windows.Forms.Label();
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeBox = new System.Windows.Forms.TextBox();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameBox = new System.Windows.Forms.TextBox();
        this._accountsLabel = new System.Windows.Forms.Label();
        this._accountsBox = new System.Windows.Forms.TextBox();
        this._verifyButton = new System.Windows.Forms.Button();
        this._previewBox = new System.Windows.Forms.TextBox();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._okButton = new System.Windows.Forms.Button();
        this._cancelButton = new System.Windows.Forms.Button();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // _introLabel
        //
        this._introLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._introLabel.Location = new System.Drawing.Point(14, 12);
        this._introLabel.Name = "_introLabel";
        this._introLabel.Size = new System.Drawing.Size(592, 48);
        this._introLabel.TabIndex = 0;
        this._introLabel.Text = "Piano di produzione";
        //
        // _codeLabel
        //
        this._codeLabel.AutoSize = true;
        this._codeLabel.Location = new System.Drawing.Point(14, 70);
        this._codeLabel.Name = "_codeLabel";
        this._codeLabel.Size = new System.Drawing.Size(45, 15);
        this._codeLabel.TabIndex = 1;
        this._codeLabel.Text = "Codice";
        //
        // _codeBox
        //
        this._codeBox.Location = new System.Drawing.Point(110, 67);
        this._codeBox.Name = "_codeBox";
        this._codeBox.PlaceholderText = "FTMO-EUROPA";
        this._codeBox.Size = new System.Drawing.Size(220, 23);
        this._codeBox.TabIndex = 2;
        this._codeBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _nameLabel
        //
        this._nameLabel.AutoSize = true;
        this._nameLabel.Location = new System.Drawing.Point(14, 101);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(40, 15);
        this._nameLabel.TabIndex = 3;
        this._nameLabel.Text = "Nome";
        //
        // _nameBox
        //
        this._nameBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameBox.Location = new System.Drawing.Point(110, 98);
        this._nameBox.Name = "_nameBox";
        this._nameBox.PlaceholderText = "vuoto = il nome del piano di origine";
        this._nameBox.Size = new System.Drawing.Size(496, 23);
        this._nameBox.TabIndex = 4;
        this._nameBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _accountsLabel
        //
        this._accountsLabel.AutoSize = true;
        this._accountsLabel.Location = new System.Drawing.Point(14, 132);
        this._accountsLabel.Name = "_accountsLabel";
        this._accountsLabel.Size = new System.Drawing.Size(34, 15);
        this._accountsLabel.TabIndex = 5;
        this._accountsLabel.Text = "Conti";
        //
        // _accountsBox
        //
        this._accountsBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._accountsBox.Location = new System.Drawing.Point(110, 129);
        this._accountsBox.Name = "_accountsBox";
        this._accountsBox.Size = new System.Drawing.Size(390, 23);
        this._accountsBox.TabIndex = 6;
        this._accountsBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _verifyButton
        //
        this._verifyButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        this._verifyButton.Location = new System.Drawing.Point(511, 128);
        this._verifyButton.Name = "_verifyButton";
        this._verifyButton.Size = new System.Drawing.Size(95, 25);
        this._verifyButton.TabIndex = 7;
        this._verifyButton.Text = "Verifica";
        this._verifyButton.UseVisualStyleBackColor = true;
        this._verifyButton.Click += new System.EventHandler(this.OnVerifyClick);
        //
        // _previewBox
        //
        this._previewBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._previewBox.Font = new System.Drawing.Font("Consolas", 9F);
        this._previewBox.Location = new System.Drawing.Point(14, 164);
        this._previewBox.Multiline = true;
        this._previewBox.Name = "_previewBox";
        this._previewBox.ReadOnly = true;
        this._previewBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this._previewBox.Size = new System.Drawing.Size(592, 300);
        this._previewBox.TabIndex = 8;
        this._previewBox.Text = "Premi Verifica: il server risponde con il piano che nascerebbe, o con il conflitto che lo impedisce.";
        this._previewBox.WordWrap = false;
        //
        // _buttons
        //
        this._buttons.Controls.Add(this._okButton);
        this._buttons.Controls.Add(this._cancelButton);
        this._buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this._buttons.Location = new System.Drawing.Point(0, 475);
        this._buttons.Name = "_buttons";
        this._buttons.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
        this._buttons.Size = new System.Drawing.Size(620, 49);
        this._buttons.TabIndex = 9;
        //
        // _okButton
        //
        this._okButton.AutoSize = true;
        this._okButton.Enabled = false;
        this._okButton.Name = "_okButton";
        this._okButton.Size = new System.Drawing.Size(85, 25);
        this._okButton.TabIndex = 0;
        this._okButton.Text = "Conferma";
        this._okButton.UseVisualStyleBackColor = true;
        this._okButton.Click += new System.EventHandler(this.OnOkClick);
        //
        // _cancelButton
        //
        this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this._cancelButton.Name = "_cancelButton";
        this._cancelButton.Size = new System.Drawing.Size(85, 25);
        this._cancelButton.TabIndex = 1;
        this._cancelButton.Text = "Annulla";
        this._cancelButton.UseVisualStyleBackColor = true;
        //
        // ProductionPlanDialog
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._cancelButton;
        this.ClientSize = new System.Drawing.Size(620, 524);
        this.Controls.Add(this._previewBox);
        this.Controls.Add(this._verifyButton);
        this.Controls.Add(this._accountsBox);
        this.Controls.Add(this._accountsLabel);
        this.Controls.Add(this._nameBox);
        this.Controls.Add(this._nameLabel);
        this.Controls.Add(this._codeBox);
        this.Controls.Add(this._codeLabel);
        this.Controls.Add(this._introLabel);
        this.Controls.Add(this._buttons);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(560, 420);
        this.Name = "ProductionPlanDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Piano di produzione";
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label _introLabel;
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeBox;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameBox;
    private System.Windows.Forms.Label _accountsLabel;
    private System.Windows.Forms.TextBox _accountsBox;
    private System.Windows.Forms.Button _verifyButton;
    private System.Windows.Forms.TextBox _previewBox;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _okButton;
    private System.Windows.Forms.Button _cancelButton;
}
