namespace piootooapp.clientform.Shell.Controls;

partial class DetailToolbar
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
        this._layout = new System.Windows.Forms.TableLayoutPanel();
        this._backButton = new System.Windows.Forms.Button();
        this._titleLabel = new System.Windows.Forms.Label();
        this._dirtyLabel = new System.Windows.Forms.Label();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._saveButton = new System.Windows.Forms.Button();
        this._revertButton = new System.Windows.Forms.Button();
        this._layout.SuspendLayout();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        // 
        // _layout
        // 
        this._layout.ColumnCount = 4;
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.Controls.Add(this._backButton, 0, 0);
        this._layout.Controls.Add(this._titleLabel, 1, 0);
        this._layout.Controls.Add(this._dirtyLabel, 2, 0);
        this._layout.Controls.Add(this._buttons, 3, 0);
        this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._layout.Location = new System.Drawing.Point(0, 0);
        this._layout.Name = "_layout";
        this._layout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
        this._layout.RowCount = 1;
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.Size = new System.Drawing.Size(900, 44);
        this._layout.TabIndex = 0;
        // 
        // _backButton
        // 
        this._backButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._backButton.AutoSize = true;
        this._backButton.Location = new System.Drawing.Point(11, 9);
        this._backButton.Name = "_backButton";
        this._backButton.Size = new System.Drawing.Size(80, 25);
        this._backButton.TabIndex = 0;
        this._backButton.Text = "← Indietro";
        this._backButton.UseVisualStyleBackColor = true;
        this._backButton.Click += new System.EventHandler(this.OnBackClick);
        // 
        // _titleLabel
        // 
        this._titleLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._titleLabel.AutoSize = true;
        this._titleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
        this._titleLabel.Location = new System.Drawing.Point(103, 12);
        this._titleLabel.Margin = new System.Windows.Forms.Padding(9, 0, 12, 0);
        this._titleLabel.Name = "_titleLabel";
        this._titleLabel.Size = new System.Drawing.Size(70, 19);
        this._titleLabel.TabIndex = 1;
        this._titleLabel.Text = "";
        // 
        // _dirtyLabel
        // 
        this._dirtyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._dirtyLabel.AutoSize = true;
        this._dirtyLabel.ForeColor = System.Drawing.Color.FromArgb(180, 95, 6);
        this._dirtyLabel.Location = new System.Drawing.Point(188, 13);
        this._dirtyLabel.Name = "_dirtyLabel";
        this._dirtyLabel.Size = new System.Drawing.Size(110, 15);
        this._dirtyLabel.TabIndex = 2;
        this._dirtyLabel.Text = "modifiche non salvate";
        this._dirtyLabel.Visible = false;
        // 
        // _buttons
        // 
        this._buttons.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._buttons.AutoSize = true;
        this._buttons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._buttons.Controls.Add(this._saveButton);
        this._buttons.Controls.Add(this._revertButton);
        this._buttons.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
        this._buttons.Name = "_buttons";
        this._buttons.Size = new System.Drawing.Size(170, 29);
        this._buttons.TabIndex = 3;
        this._buttons.WrapContents = false;
        // 
        // _saveButton
        // 
        this._saveButton.AutoSize = true;
        this._saveButton.Name = "_saveButton";
        this._saveButton.Size = new System.Drawing.Size(75, 25);
        this._saveButton.TabIndex = 0;
        this._saveButton.Text = "Salva";
        this._saveButton.UseVisualStyleBackColor = true;
        this._saveButton.Click += new System.EventHandler(this.OnSaveClick);
        // 
        // _revertButton
        // 
        this._revertButton.AutoSize = true;
        this._revertButton.Enabled = false;
        this._revertButton.Name = "_revertButton";
        this._revertButton.Size = new System.Drawing.Size(75, 25);
        this._revertButton.TabIndex = 1;
        this._revertButton.Text = "Annulla";
        this._revertButton.UseVisualStyleBackColor = true;
        this._revertButton.Click += new System.EventHandler(this.OnRevertClick);
        // 
        // DetailToolbar
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._layout);
        this.Name = "DetailToolbar";
        this.Size = new System.Drawing.Size(900, 44);
        this._layout.ResumeLayout(false);
        this._layout.PerformLayout();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel _layout;
    private System.Windows.Forms.Button _backButton;
    private System.Windows.Forms.Label _titleLabel;
    private System.Windows.Forms.Label _dirtyLabel;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _saveButton;
    private System.Windows.Forms.Button _revertButton;
}
