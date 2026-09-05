namespace piootooapp.clientform.Shell.Screens;

partial class BrokerDetailScreen
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
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._layout = new System.Windows.Forms.TableLayoutPanel();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeTextBox = new System.Windows.Forms.TextBox();
        this._conversionLabel = new System.Windows.Forms.Label();
        this._conversionCombo = new System.Windows.Forms.ComboBox();
        this._datafeedLabel = new System.Windows.Forms.Label();
        this._datafeedTextBox = new System.Windows.Forms.TextBox();
        this._enabledCheckBox = new System.Windows.Forms.CheckBox();
        this._notesLabel = new System.Windows.Forms.Label();
        this._notesTextBox = new System.Windows.Forms.TextBox();
        this._identityLabel = new System.Windows.Forms.Label();
        this._layout.SuspendLayout();
        this.SuspendLayout();
        //
        // _toolbar
        //
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Broker";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        //
        // _layout
        //
        this._layout.AutoSize = true;
        this._layout.ColumnCount = 2;
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.Controls.Add(this._nameLabel, 0, 0);
        this._layout.Controls.Add(this._nameTextBox, 1, 0);
        this._layout.Controls.Add(this._codeLabel, 0, 1);
        this._layout.Controls.Add(this._codeTextBox, 1, 1);
        this._layout.Controls.Add(this._conversionLabel, 0, 2);
        this._layout.Controls.Add(this._conversionCombo, 1, 2);
        this._layout.Controls.Add(this._datafeedLabel, 0, 3);
        this._layout.Controls.Add(this._datafeedTextBox, 1, 3);
        this._layout.Controls.Add(this._enabledCheckBox, 1, 4);
        this._layout.Controls.Add(this._notesLabel, 0, 5);
        this._layout.Controls.Add(this._notesTextBox, 1, 5);
        this._layout.Controls.Add(this._identityLabel, 1, 6);
        this._layout.Dock = System.Windows.Forms.DockStyle.Top;
        this._layout.Location = new System.Drawing.Point(0, 44);
        this._layout.Name = "_layout";
        this._layout.Padding = new System.Windows.Forms.Padding(12);
        this._layout.RowCount = 7;
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._layout.Size = new System.Drawing.Size(900, 260);
        this._layout.TabIndex = 1;
        //
        // _nameLabel
        //
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Nome";
        //
        // _nameTextBox
        //
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(360, 23);
        this._nameTextBox.TabIndex = 1;
        this._nameTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _codeLabel
        //
        this._codeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._codeLabel.AutoSize = true;
        this._codeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
        this._codeLabel.Name = "_codeLabel";
        this._codeLabel.TabIndex = 2;
        this._codeLabel.Text = "Codice";
        //
        // _codeTextBox
        //
        this._codeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._codeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._codeTextBox.Name = "_codeTextBox";
        this._codeTextBox.Size = new System.Drawing.Size(200, 23);
        this._codeTextBox.TabIndex = 3;
        this._codeTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _conversionLabel
        //
        this._conversionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._conversionLabel.AutoSize = true;
        this._conversionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
        this._conversionLabel.Name = "_conversionLabel";
        this._conversionLabel.TabIndex = 4;
        this._conversionLabel.Text = "Tabella simboli";
        //
        // _conversionCombo
        //
        this._conversionCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._conversionCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._conversionCombo.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._conversionCombo.Name = "_conversionCombo";
        this._conversionCombo.Size = new System.Drawing.Size(360, 23);
        this._conversionCombo.TabIndex = 5;
        this._conversionCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _datafeedLabel
        //
        this._datafeedLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._datafeedLabel.AutoSize = true;
        this._datafeedLabel.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
        this._datafeedLabel.Name = "_datafeedLabel";
        this._datafeedLabel.TabIndex = 6;
        this._datafeedLabel.Text = "Cartella datafeed";
        //
        // _datafeedTextBox
        //
        this._datafeedTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._datafeedTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._datafeedTextBox.Name = "_datafeedTextBox";
        this._datafeedTextBox.PlaceholderText = "vuoto = il codice";
        this._datafeedTextBox.Size = new System.Drawing.Size(200, 23);
        this._datafeedTextBox.TabIndex = 7;
        this._datafeedTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _enabledCheckBox
        //
        this._enabledCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._enabledCheckBox.AutoSize = true;
        this._enabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
        this._enabledCheckBox.Name = "_enabledCheckBox";
        this._enabledCheckBox.TabIndex = 8;
        this._enabledCheckBox.Text = "Attivo";
        this._enabledCheckBox.UseVisualStyleBackColor = true;
        this._enabledCheckBox.CheckedChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _notesLabel
        //
        this._notesLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._notesLabel.AutoSize = true;
        this._notesLabel.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
        this._notesLabel.Name = "_notesLabel";
        this._notesLabel.TabIndex = 9;
        this._notesLabel.Text = "Note";
        //
        // _notesTextBox
        //
        this._notesTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._notesTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._notesTextBox.Multiline = true;
        this._notesTextBox.Name = "_notesTextBox";
        this._notesTextBox.Size = new System.Drawing.Size(360, 60);
        this._notesTextBox.TabIndex = 10;
        this._notesTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _identityLabel
        //
        this._identityLabel.AutoSize = false;
        this._identityLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._identityLabel.Margin = new System.Windows.Forms.Padding(3, 8, 24, 4);
        this._identityLabel.Name = "_identityLabel";
        this._identityLabel.Size = new System.Drawing.Size(700, 40);
        this._identityLabel.TabIndex = 11;
        this._identityLabel.Text = "";
        //
        // BrokerDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._layout);
        this.Controls.Add(this._toolbar);
        this.Name = "BrokerDetailScreen";
        this.Size = new System.Drawing.Size(900, 500);
        this._layout.ResumeLayout(false);
        this._layout.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.TableLayoutPanel _layout;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeTextBox;
    private System.Windows.Forms.Label _conversionLabel;
    private System.Windows.Forms.ComboBox _conversionCombo;
    private System.Windows.Forms.Label _datafeedLabel;
    private System.Windows.Forms.TextBox _datafeedTextBox;
    private System.Windows.Forms.CheckBox _enabledCheckBox;
    private System.Windows.Forms.Label _notesLabel;
    private System.Windows.Forms.TextBox _notesTextBox;
    private System.Windows.Forms.Label _identityLabel;
}
