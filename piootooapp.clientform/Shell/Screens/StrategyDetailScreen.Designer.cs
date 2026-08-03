namespace piootooapp.clientform.Shell.Screens;

partial class StrategyDetailScreen
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
        this._toolbar = new piootooapp.clientform.Shell.Controls.DetailToolbar();
        this._fieldsLayout = new System.Windows.Forms.TableLayoutPanel();
        this._idLabel = new System.Windows.Forms.Label();
        this._idTextBox = new System.Windows.Forms.TextBox();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._codeLabel = new System.Windows.Forms.Label();
        this._codeTextBox = new System.Windows.Forms.TextBox();
        this._symbolLabel = new System.Windows.Forms.Label();
        this._symbolTextBox = new System.Windows.Forms.TextBox();
        this._timeframeLabel = new System.Windows.Forms.Label();
        this._timeframeTextBox = new System.Windows.Forms.TextBox();
        this._barTypeLabel = new System.Windows.Forms.Label();
        this._barTypeTextBox = new System.Windows.Forms.TextBox();
        this._typeLabel = new System.Windows.Forms.Label();
        this._typeTextBox = new System.Windows.Forms.TextBox();
        this._activeLabel = new System.Windows.Forms.Label();
        this._activeTextBox = new System.Windows.Forms.TextBox();
        this._sourceLabel = new System.Windows.Forms.Label();
        this._sourceTextBox = new System.Windows.Forms.TextBox();
        this._descriptionGroup = new System.Windows.Forms.GroupBox();
        this._descriptionTextBox = new System.Windows.Forms.TextBox();
        this._fieldsLayout.SuspendLayout();
        this._descriptionGroup.SuspendLayout();
        this.SuspendLayout();
        // 
        // _toolbar
        // 
        this._toolbar.CanSave = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Strategia";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        // 
        // _fieldsLayout
        // 
        this._fieldsLayout.AutoSize = true;
        this._fieldsLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._fieldsLayout.ColumnCount = 4;
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._fieldsLayout.Controls.Add(this._nameLabel, 0, 0);
        this._fieldsLayout.Controls.Add(this._nameTextBox, 1, 0);
        this._fieldsLayout.Controls.Add(this._idLabel, 2, 0);
        this._fieldsLayout.Controls.Add(this._idTextBox, 3, 0);
        this._fieldsLayout.Controls.Add(this._codeLabel, 0, 1);
        this._fieldsLayout.Controls.Add(this._codeTextBox, 1, 1);
        this._fieldsLayout.Controls.Add(this._symbolLabel, 2, 1);
        this._fieldsLayout.Controls.Add(this._symbolTextBox, 3, 1);
        this._fieldsLayout.Controls.Add(this._timeframeLabel, 0, 2);
        this._fieldsLayout.Controls.Add(this._timeframeTextBox, 1, 2);
        this._fieldsLayout.Controls.Add(this._barTypeLabel, 2, 2);
        this._fieldsLayout.Controls.Add(this._barTypeTextBox, 3, 2);
        this._fieldsLayout.Controls.Add(this._typeLabel, 0, 3);
        this._fieldsLayout.Controls.Add(this._typeTextBox, 1, 3);
        this._fieldsLayout.Controls.Add(this._activeLabel, 2, 3);
        this._fieldsLayout.Controls.Add(this._activeTextBox, 3, 3);
        this._fieldsLayout.Controls.Add(this._sourceLabel, 0, 4);
        this._fieldsLayout.Controls.Add(this._sourceTextBox, 1, 4);
        this._fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._fieldsLayout.Location = new System.Drawing.Point(0, 44);
        this._fieldsLayout.Name = "_fieldsLayout";
        this._fieldsLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
        this._fieldsLayout.RowCount = 5;
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.SetColumnSpan(this._sourceTextBox, 3);
        this._fieldsLayout.Size = new System.Drawing.Size(900, 171);
        this._fieldsLayout.TabIndex = 1;
        // 
        // _nameLabel
        // 
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(160, 15);
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Nome (codice esecuzione)";
        // 
        // _nameTextBox
        // 
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.ReadOnly = true;
        this._nameTextBox.Size = new System.Drawing.Size(250, 23);
        this._nameTextBox.TabIndex = 1;
        // 
        // _idLabel
        // 
        this._idLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._idLabel.AutoSize = true;
        this._idLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._idLabel.Name = "_idLabel";
        this._idLabel.Size = new System.Drawing.Size(150, 15);
        this._idLabel.TabIndex = 2;
        this._idLabel.Text = "Id di classe (masterfilter)";
        // 
        // _idTextBox
        // 
        this._idTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._idTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._idTextBox.Name = "_idTextBox";
        this._idTextBox.ReadOnly = true;
        this._idTextBox.Size = new System.Drawing.Size(250, 23);
        this._idTextBox.TabIndex = 3;
        // 
        // _codeLabel
        // 
        this._codeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._codeLabel.AutoSize = true;
        this._codeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._codeLabel.Name = "_codeLabel";
        this._codeLabel.Size = new System.Drawing.Size(40, 15);
        this._codeLabel.TabIndex = 4;
        this._codeLabel.Text = "Code";
        // 
        // _codeTextBox
        // 
        this._codeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._codeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._codeTextBox.Name = "_codeTextBox";
        this._codeTextBox.ReadOnly = true;
        this._codeTextBox.Size = new System.Drawing.Size(250, 23);
        this._codeTextBox.TabIndex = 5;
        // 
        // _symbolLabel
        // 
        this._symbolLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._symbolLabel.AutoSize = true;
        this._symbolLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._symbolLabel.Name = "_symbolLabel";
        this._symbolLabel.Size = new System.Drawing.Size(52, 15);
        this._symbolLabel.TabIndex = 6;
        this._symbolLabel.Text = "Simbolo";
        // 
        // _symbolTextBox
        // 
        this._symbolTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._symbolTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._symbolTextBox.Name = "_symbolTextBox";
        this._symbolTextBox.ReadOnly = true;
        this._symbolTextBox.Size = new System.Drawing.Size(250, 23);
        this._symbolTextBox.TabIndex = 7;
        // 
        // _timeframeLabel
        // 
        this._timeframeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._timeframeLabel.AutoSize = true;
        this._timeframeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._timeframeLabel.Name = "_timeframeLabel";
        this._timeframeLabel.Size = new System.Drawing.Size(70, 15);
        this._timeframeLabel.TabIndex = 8;
        this._timeframeLabel.Text = "Timeframe";
        // 
        // _timeframeTextBox
        // 
        this._timeframeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._timeframeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._timeframeTextBox.Name = "_timeframeTextBox";
        this._timeframeTextBox.ReadOnly = true;
        this._timeframeTextBox.Size = new System.Drawing.Size(250, 23);
        this._timeframeTextBox.TabIndex = 9;
        // 
        // _barTypeLabel
        // 
        this._barTypeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._barTypeLabel.AutoSize = true;
        this._barTypeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._barTypeLabel.Name = "_barTypeLabel";
        this._barTypeLabel.Size = new System.Drawing.Size(70, 15);
        this._barTypeLabel.TabIndex = 10;
        this._barTypeLabel.Text = "Tipo barra";
        // 
        // _barTypeTextBox
        // 
        this._barTypeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._barTypeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._barTypeTextBox.Name = "_barTypeTextBox";
        this._barTypeTextBox.ReadOnly = true;
        this._barTypeTextBox.Size = new System.Drawing.Size(250, 23);
        this._barTypeTextBox.TabIndex = 11;
        // 
        // _typeLabel
        // 
        this._typeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._typeLabel.AutoSize = true;
        this._typeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._typeLabel.Name = "_typeLabel";
        this._typeLabel.Size = new System.Drawing.Size(32, 15);
        this._typeLabel.TabIndex = 12;
        this._typeLabel.Text = "Tipo";
        // 
        // _typeTextBox
        // 
        this._typeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._typeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 24, 4);
        this._typeTextBox.Name = "_typeTextBox";
        this._typeTextBox.ReadOnly = true;
        this._typeTextBox.Size = new System.Drawing.Size(250, 23);
        this._typeTextBox.TabIndex = 13;
        // 
        // _activeLabel
        // 
        this._activeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._activeLabel.AutoSize = true;
        this._activeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._activeLabel.Name = "_activeLabel";
        this._activeLabel.Size = new System.Drawing.Size(40, 15);
        this._activeLabel.TabIndex = 14;
        this._activeLabel.Text = "Attiva";
        // 
        // _activeTextBox
        // 
        this._activeTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._activeTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._activeTextBox.Name = "_activeTextBox";
        this._activeTextBox.ReadOnly = true;
        this._activeTextBox.Size = new System.Drawing.Size(250, 23);
        this._activeTextBox.TabIndex = 15;
        // 
        // _sourceLabel
        // 
        this._sourceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._sourceLabel.AutoSize = true;
        this._sourceLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._sourceLabel.Name = "_sourceLabel";
        this._sourceLabel.Size = new System.Drawing.Size(90, 15);
        this._sourceLabel.TabIndex = 16;
        this._sourceLabel.Text = "File sorgente";
        // 
        // _sourceTextBox
        // 
        this._sourceTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._sourceTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._sourceTextBox.Name = "_sourceTextBox";
        this._sourceTextBox.ReadOnly = true;
        this._sourceTextBox.Size = new System.Drawing.Size(700, 23);
        this._sourceTextBox.TabIndex = 17;
        // 
        // _descriptionGroup
        // 
        this._descriptionGroup.Controls.Add(this._descriptionTextBox);
        this._descriptionGroup.Dock = System.Windows.Forms.DockStyle.Fill;
        this._descriptionGroup.Location = new System.Drawing.Point(0, 215);
        this._descriptionGroup.Name = "_descriptionGroup";
        this._descriptionGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._descriptionGroup.Size = new System.Drawing.Size(900, 385);
        this._descriptionGroup.TabIndex = 2;
        this._descriptionGroup.TabStop = false;
        this._descriptionGroup.Text = "Descrizione";
        // 
        // _descriptionTextBox
        // 
        this._descriptionTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._descriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._descriptionTextBox.Location = new System.Drawing.Point(12, 22);
        this._descriptionTextBox.Multiline = true;
        this._descriptionTextBox.Name = "_descriptionTextBox";
        this._descriptionTextBox.ReadOnly = true;
        this._descriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._descriptionTextBox.Size = new System.Drawing.Size(876, 351);
        this._descriptionTextBox.TabIndex = 0;
        // 
        // StrategyDetailScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._descriptionGroup);
        this.Controls.Add(this._fieldsLayout);
        this.Controls.Add(this._toolbar);
        this.Name = "StrategyDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._fieldsLayout.ResumeLayout(false);
        this._fieldsLayout.PerformLayout();
        this._descriptionGroup.ResumeLayout(false);
        this._descriptionGroup.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.TableLayoutPanel _fieldsLayout;
    private System.Windows.Forms.Label _idLabel;
    private System.Windows.Forms.TextBox _idTextBox;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _codeLabel;
    private System.Windows.Forms.TextBox _codeTextBox;
    private System.Windows.Forms.Label _symbolLabel;
    private System.Windows.Forms.TextBox _symbolTextBox;
    private System.Windows.Forms.Label _timeframeLabel;
    private System.Windows.Forms.TextBox _timeframeTextBox;
    private System.Windows.Forms.Label _barTypeLabel;
    private System.Windows.Forms.TextBox _barTypeTextBox;
    private System.Windows.Forms.Label _typeLabel;
    private System.Windows.Forms.TextBox _typeTextBox;
    private System.Windows.Forms.Label _activeLabel;
    private System.Windows.Forms.TextBox _activeTextBox;
    private System.Windows.Forms.Label _sourceLabel;
    private System.Windows.Forms.TextBox _sourceTextBox;
    private System.Windows.Forms.GroupBox _descriptionGroup;
    private System.Windows.Forms.TextBox _descriptionTextBox;
}
