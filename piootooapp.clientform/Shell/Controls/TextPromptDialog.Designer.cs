namespace piootooapp.clientform.Shell.Controls;

partial class TextPromptDialog
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
        this._promptLabel = new System.Windows.Forms.Label();
        this._valueTextBox = new System.Windows.Forms.TextBox();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._okButton = new System.Windows.Forms.Button();
        this._cancelButton = new System.Windows.Forms.Button();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        // 
        // _promptLabel
        // 
        this._promptLabel.AutoSize = true;
        this._promptLabel.Location = new System.Drawing.Point(14, 16);
        this._promptLabel.Name = "_promptLabel";
        this._promptLabel.Size = new System.Drawing.Size(60, 15);
        this._promptLabel.TabIndex = 0;
        this._promptLabel.Text = "Valore";
        // 
        // _valueTextBox
        // 
        this._valueTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._valueTextBox.Location = new System.Drawing.Point(14, 38);
        this._valueTextBox.Name = "_valueTextBox";
        this._valueTextBox.Size = new System.Drawing.Size(372, 23);
        this._valueTextBox.TabIndex = 1;
        // 
        // _buttons
        // 
        this._buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this._buttons.Controls.Add(this._okButton);
        this._buttons.Controls.Add(this._cancelButton);
        this._buttons.Location = new System.Drawing.Point(0, 77);
        this._buttons.Name = "_buttons";
        this._buttons.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
        this._buttons.Size = new System.Drawing.Size(400, 49);
        this._buttons.TabIndex = 2;
        // 
        // _okButton
        // 
        this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
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
        // TextPromptDialog
        // 
        this.AcceptButton = this._okButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._cancelButton;
        this.ClientSize = new System.Drawing.Size(400, 126);
        this.Controls.Add(this._buttons);
        this.Controls.Add(this._valueTextBox);
        this.Controls.Add(this._promptLabel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "TextPromptDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Nuovo valore";
        this._buttons.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label _promptLabel;
    private System.Windows.Forms.TextBox _valueTextBox;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _okButton;
    private System.Windows.Forms.Button _cancelButton;
}
