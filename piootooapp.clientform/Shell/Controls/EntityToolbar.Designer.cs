namespace piootooapp.clientform.Shell.Controls;

partial class EntityToolbar
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
        this._titleLabel = new System.Windows.Forms.Label();
        this._filterBox = new System.Windows.Forms.TextBox();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._createButton = new System.Windows.Forms.Button();
        this._deleteButton = new System.Windows.Forms.Button();
        this._refreshButton = new System.Windows.Forms.Button();
        this._layout.SuspendLayout();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        // 
        // _layout
        // 
        this._layout.ColumnCount = 3;
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._layout.Controls.Add(this._titleLabel, 0, 0);
        this._layout.Controls.Add(this._filterBox, 1, 0);
        this._layout.Controls.Add(this._buttons, 2, 0);
        this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._layout.Location = new System.Drawing.Point(0, 0);
        this._layout.Name = "_layout";
        this._layout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
        this._layout.RowCount = 1;
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.Size = new System.Drawing.Size(900, 44);
        this._layout.TabIndex = 0;
        // 
        // _titleLabel
        // 
        this._titleLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._titleLabel.AutoSize = true;
        this._titleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
        this._titleLabel.Location = new System.Drawing.Point(11, 8);
        this._titleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
        this._titleLabel.Name = "_titleLabel";
        this._titleLabel.Size = new System.Drawing.Size(70, 19);
        this._titleLabel.TabIndex = 0;
        this._titleLabel.Text = "";
        // 
        // _filterBox
        // 
        this._filterBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._filterBox.Location = new System.Drawing.Point(100, 8);
        this._filterBox.Name = "_filterBox";
        this._filterBox.PlaceholderText = "Filtra…";
        this._filterBox.Size = new System.Drawing.Size(400, 23);
        this._filterBox.TabIndex = 1;
        this._filterBox.TextChanged += new System.EventHandler(this.OnFilterTextChanged);
        // 
        // _buttons
        // 
        this._buttons.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this._buttons.AutoSize = true;
        this._buttons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._buttons.Controls.Add(this._createButton);
        this._buttons.Controls.Add(this._deleteButton);
        this._buttons.Controls.Add(this._refreshButton);
        this._buttons.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
        this._buttons.Name = "_buttons";
        this._buttons.Size = new System.Drawing.Size(260, 29);
        this._buttons.TabIndex = 2;
        this._buttons.WrapContents = false;
        // 
        // _createButton
        // 
        this._createButton.AutoSize = true;
        this._createButton.Name = "_createButton";
        this._createButton.Size = new System.Drawing.Size(75, 25);
        this._createButton.TabIndex = 0;
        this._createButton.Text = "Nuovo";
        this._createButton.UseVisualStyleBackColor = true;
        this._createButton.Click += new System.EventHandler(this.OnCreateClick);
        // 
        // _deleteButton
        // 
        this._deleteButton.AutoSize = true;
        this._deleteButton.Enabled = false;
        this._deleteButton.Name = "_deleteButton";
        this._deleteButton.Size = new System.Drawing.Size(75, 25);
        this._deleteButton.TabIndex = 1;
        this._deleteButton.Text = "Elimina";
        this._deleteButton.UseVisualStyleBackColor = true;
        this._deleteButton.Click += new System.EventHandler(this.OnDeleteClick);
        // 
        // _refreshButton
        // 
        this._refreshButton.AutoSize = true;
        this._refreshButton.Name = "_refreshButton";
        this._refreshButton.Size = new System.Drawing.Size(75, 25);
        this._refreshButton.TabIndex = 2;
        this._refreshButton.Text = "Aggiorna";
        this._refreshButton.UseVisualStyleBackColor = true;
        this._refreshButton.Click += new System.EventHandler(this.OnRefreshClick);
        // 
        // EntityToolbar
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._layout);
        this.Name = "EntityToolbar";
        this.Size = new System.Drawing.Size(900, 44);
        this._layout.ResumeLayout(false);
        this._layout.PerformLayout();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel _layout;
    private System.Windows.Forms.Label _titleLabel;
    private System.Windows.Forms.TextBox _filterBox;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _createButton;
    private System.Windows.Forms.Button _deleteButton;
    private System.Windows.Forms.Button _refreshButton;
}
