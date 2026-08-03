namespace piootooapp.clientform.Shell.Screens;

partial class WorkspaceDetailScreen
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
        this._infoLabel = new System.Windows.Forms.Label();
        this._fieldsLayout = new System.Windows.Forms.TableLayoutPanel();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._strategiesGroup = new System.Windows.Forms.GroupBox();
        this._strategiesList = new System.Windows.Forms.CheckedListBox();
        this._strategyFilterPanel = new System.Windows.Forms.TableLayoutPanel();
        this._strategyFilterTextBox = new System.Windows.Forms.TextBox();
        this._onlySelectedCheckBox = new System.Windows.Forms.CheckBox();
        this._selectAllButton = new System.Windows.Forms.Button();
        this._selectNoneButton = new System.Windows.Forms.Button();
        this._selectionCountLabel = new System.Windows.Forms.Label();
        this._fieldsLayout.SuspendLayout();
        this._strategiesGroup.SuspendLayout();
        this._strategyFilterPanel.SuspendLayout();
        this.SuspendLayout();
        // 
        // _toolbar
        // 
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Workspace";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        // 
        // _infoLabel
        // 
        this._infoLabel.AutoSize = true;
        this._infoLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._infoLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._infoLabel.Location = new System.Drawing.Point(0, 44);
        this._infoLabel.Name = "_infoLabel";
        this._infoLabel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        this._infoLabel.Size = new System.Drawing.Size(900, 27);
        this._infoLabel.TabIndex = 1;
        // 
        // _fieldsLayout
        // 
        this._fieldsLayout.AutoSize = true;
        this._fieldsLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._fieldsLayout.ColumnCount = 2;
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._fieldsLayout.Controls.Add(this._nameLabel, 0, 0);
        this._fieldsLayout.Controls.Add(this._nameTextBox, 1, 0);
        this._fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._fieldsLayout.Location = new System.Drawing.Point(0, 71);
        this._fieldsLayout.Name = "_fieldsLayout";
        this._fieldsLayout.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._fieldsLayout.RowCount = 1;
        this._fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._fieldsLayout.Size = new System.Drawing.Size(900, 39);
        this._fieldsLayout.TabIndex = 2;
        // 
        // _nameLabel
        // 
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(48, 15);
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Nome *";
        // 
        // _nameTextBox
        // 
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(320, 23);
        this._nameTextBox.TabIndex = 1;
        this._nameTextBox.TextChanged += new System.EventHandler(this.OnNameChanged);
        // 
        // _strategiesGroup
        // 
        this._strategiesGroup.Controls.Add(this._strategiesList);
        this._strategiesGroup.Controls.Add(this._strategyFilterPanel);
        this._strategiesGroup.Dock = System.Windows.Forms.DockStyle.Fill;
        this._strategiesGroup.Location = new System.Drawing.Point(0, 110);
        this._strategiesGroup.Name = "_strategiesGroup";
        this._strategiesGroup.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
        this._strategiesGroup.Size = new System.Drawing.Size(900, 490);
        this._strategiesGroup.TabIndex = 3;
        this._strategiesGroup.TabStop = false;
        this._strategiesGroup.Text = "Masterfilter — strategie abilitate";
        // 
        // _strategyFilterPanel
        // 
        this._strategyFilterPanel.AutoSize = true;
        this._strategyFilterPanel.ColumnCount = 5;
        this._strategyFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._strategyFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._strategyFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._strategyFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._strategyFilterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._strategyFilterPanel.Controls.Add(this._strategyFilterTextBox, 0, 0);
        this._strategyFilterPanel.Controls.Add(this._onlySelectedCheckBox, 1, 0);
        this._strategyFilterPanel.Controls.Add(this._selectAllButton, 2, 0);
        this._strategyFilterPanel.Controls.Add(this._selectNoneButton, 3, 0);
        this._strategyFilterPanel.Controls.Add(this._selectionCountLabel, 4, 0);
        this._strategyFilterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._strategyFilterPanel.Location = new System.Drawing.Point(12, 22);
        this._strategyFilterPanel.Name = "_strategyFilterPanel";
        this._strategyFilterPanel.RowCount = 1;
        this._strategyFilterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._strategyFilterPanel.Size = new System.Drawing.Size(876, 33);
        this._strategyFilterPanel.TabIndex = 0;
        // 
        // _strategyFilterTextBox
        // 
        this._strategyFilterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._strategyFilterTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 12, 4);
        this._strategyFilterTextBox.Name = "_strategyFilterTextBox";
        this._strategyFilterTextBox.PlaceholderText = "Filtra per simbolo, nome, codice o id…";
        this._strategyFilterTextBox.Size = new System.Drawing.Size(400, 23);
        this._strategyFilterTextBox.TabIndex = 0;
        this._strategyFilterTextBox.TextChanged += new System.EventHandler(this.OnStrategyFilterChanged);
        // 
        // _onlySelectedCheckBox
        // 
        this._onlySelectedCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._onlySelectedCheckBox.AutoSize = true;
        this._onlySelectedCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
        this._onlySelectedCheckBox.Name = "_onlySelectedCheckBox";
        this._onlySelectedCheckBox.Size = new System.Drawing.Size(115, 19);
        this._onlySelectedCheckBox.TabIndex = 1;
        this._onlySelectedCheckBox.Text = "Solo selezionate";
        this._onlySelectedCheckBox.UseVisualStyleBackColor = true;
        this._onlySelectedCheckBox.CheckedChanged += new System.EventHandler(this.OnStrategyFilterChanged);
        // 
        // _selectAllButton
        // 
        this._selectAllButton.AutoSize = true;
        this._selectAllButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._selectAllButton.Name = "_selectAllButton";
        this._selectAllButton.Size = new System.Drawing.Size(120, 25);
        this._selectAllButton.TabIndex = 2;
        this._selectAllButton.Text = "Seleziona mostrate";
        this._selectAllButton.UseVisualStyleBackColor = true;
        this._selectAllButton.Click += new System.EventHandler(this.OnSelectAllClick);
        // 
        // _selectNoneButton
        // 
        this._selectNoneButton.AutoSize = true;
        this._selectNoneButton.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
        this._selectNoneButton.Name = "_selectNoneButton";
        this._selectNoneButton.Size = new System.Drawing.Size(120, 25);
        this._selectNoneButton.TabIndex = 3;
        this._selectNoneButton.Text = "Deseleziona mostrate";
        this._selectNoneButton.UseVisualStyleBackColor = true;
        this._selectNoneButton.Click += new System.EventHandler(this.OnSelectNoneClick);
        // 
        // _selectionCountLabel
        // 
        this._selectionCountLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._selectionCountLabel.AutoSize = true;
        this._selectionCountLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._selectionCountLabel.Name = "_selectionCountLabel";
        this._selectionCountLabel.Size = new System.Drawing.Size(120, 15);
        this._selectionCountLabel.TabIndex = 4;
        // 
        // _strategiesList
        // 
        this._strategiesList.CheckOnClick = true;
        this._strategiesList.Dock = System.Windows.Forms.DockStyle.Fill;
        this._strategiesList.FormattingEnabled = true;
        this._strategiesList.HorizontalScrollbar = true;
        this._strategiesList.IntegralHeight = false;
        this._strategiesList.Location = new System.Drawing.Point(12, 55);
        this._strategiesList.Name = "_strategiesList";
        this._strategiesList.Size = new System.Drawing.Size(876, 423);
        this._strategiesList.TabIndex = 1;
        this._strategiesList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnStrategyItemCheck);
        // 
        // WorkspaceDetailScreen
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._strategiesGroup);
        this.Controls.Add(this._fieldsLayout);
        this.Controls.Add(this._infoLabel);
        this.Controls.Add(this._toolbar);
        this.Name = "WorkspaceDetailScreen";
        this.Size = new System.Drawing.Size(900, 600);
        this._fieldsLayout.ResumeLayout(false);
        this._fieldsLayout.PerformLayout();
        this._strategiesGroup.ResumeLayout(false);
        this._strategiesGroup.PerformLayout();
        this._strategyFilterPanel.ResumeLayout(false);
        this._strategyFilterPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.Label _infoLabel;
    private System.Windows.Forms.TableLayoutPanel _fieldsLayout;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.GroupBox _strategiesGroup;
    private System.Windows.Forms.TableLayoutPanel _strategyFilterPanel;
    private System.Windows.Forms.TextBox _strategyFilterTextBox;
    private System.Windows.Forms.CheckBox _onlySelectedCheckBox;
    private System.Windows.Forms.Button _selectAllButton;
    private System.Windows.Forms.Button _selectNoneButton;
    private System.Windows.Forms.Label _selectionCountLabel;
    private System.Windows.Forms.CheckedListBox _strategiesList;
}
