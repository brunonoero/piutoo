namespace piootooapp.clientform.Shell.Screens;

partial class TitanoSetupDetailScreen
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
        this._headerLayout = new System.Windows.Forms.TableLayoutPanel();
        this._nameLabel = new System.Windows.Forms.Label();
        this._nameTextBox = new System.Windows.Forms.TextBox();
        this._idLabel = new System.Windows.Forms.Label();
        this._idTextBox = new System.Windows.Forms.TextBox();
        this._descriptionLabel = new System.Windows.Forms.Label();
        this._descriptionTextBox = new System.Windows.Forms.TextBox();
        this._parametersGrid = new System.Windows.Forms.PropertyGrid();
        this._headerLayout.SuspendLayout();
        this.SuspendLayout();
        //
        // _parametersGrid
        //
        this._parametersGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._parametersGrid.Location = new System.Drawing.Point(0, 152);
        this._parametersGrid.Name = "_parametersGrid";
        this._parametersGrid.HelpVisible = true;
        this._parametersGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
        this._parametersGrid.Size = new System.Drawing.Size(900, 348);
        this._parametersGrid.TabIndex = 2;
        this._parametersGrid.ToolbarVisible = false;
        this._parametersGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.OnParameterChanged);
        //
        // _headerLayout
        //
        this._headerLayout.AutoSize = true;
        this._headerLayout.ColumnCount = 2;
        this._headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this._headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._headerLayout.Controls.Add(this._nameLabel, 0, 0);
        this._headerLayout.Controls.Add(this._nameTextBox, 1, 0);
        this._headerLayout.Controls.Add(this._idLabel, 0, 1);
        this._headerLayout.Controls.Add(this._idTextBox, 1, 1);
        this._headerLayout.Controls.Add(this._descriptionLabel, 0, 2);
        this._headerLayout.Controls.Add(this._descriptionTextBox, 1, 2);
        this._headerLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._headerLayout.Location = new System.Drawing.Point(0, 44);
        this._headerLayout.Name = "_headerLayout";
        this._headerLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
        this._headerLayout.RowCount = 3;
        this._headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._headerLayout.Size = new System.Drawing.Size(900, 108);
        this._headerLayout.TabIndex = 1;
        //
        // _nameLabel
        //
        this._nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.Size = new System.Drawing.Size(45, 15);
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Nome";
        //
        // _nameTextBox
        //
        this._nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._nameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._nameTextBox.Name = "_nameTextBox";
        this._nameTextBox.Size = new System.Drawing.Size(700, 23);
        this._nameTextBox.TabIndex = 1;
        this._nameTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _idLabel
        //
        this._idLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._idLabel.AutoSize = true;
        this._idLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._idLabel.Name = "_idLabel";
        this._idLabel.Size = new System.Drawing.Size(20, 15);
        this._idLabel.TabIndex = 2;
        this._idLabel.Text = "Id";
        //
        // _idTextBox
        //
        this._idTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._idTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._idTextBox.Name = "_idTextBox";
        this._idTextBox.ReadOnly = true;
        this._idTextBox.Size = new System.Drawing.Size(700, 23);
        this._idTextBox.TabIndex = 3;
        //
        // _descriptionLabel
        //
        this._descriptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._descriptionLabel.AutoSize = true;
        this._descriptionLabel.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
        this._descriptionLabel.Name = "_descriptionLabel";
        this._descriptionLabel.Size = new System.Drawing.Size(75, 15);
        this._descriptionLabel.TabIndex = 4;
        this._descriptionLabel.Text = "Descrizione";
        //
        // _descriptionTextBox
        //
        this._descriptionTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._descriptionTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this._descriptionTextBox.Name = "_descriptionTextBox";
        this._descriptionTextBox.Size = new System.Drawing.Size(700, 23);
        this._descriptionTextBox.TabIndex = 5;
        this._descriptionTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
        //
        // _toolbar
        //
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Setup Titano";
        this._toolbar.BackRequested += new System.EventHandler(this.OnBackRequested);
        this._toolbar.SaveRequested += new System.EventHandler(this.OnSaveRequested);
        this._toolbar.RevertRequested += new System.EventHandler(this.OnRevertRequested);
        //
        // TitanoSetupDetailScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._parametersGrid);
        this.Controls.Add(this._headerLayout);
        this.Controls.Add(this._toolbar);
        this.Name = "TitanoSetupDetailScreen";
        this.Size = new System.Drawing.Size(900, 500);
        this._headerLayout.ResumeLayout(false);
        this._headerLayout.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.DetailToolbar _toolbar;
    private System.Windows.Forms.TableLayoutPanel _headerLayout;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _idLabel;
    private System.Windows.Forms.TextBox _idTextBox;
    private System.Windows.Forms.Label _descriptionLabel;
    private System.Windows.Forms.TextBox _descriptionTextBox;
    private System.Windows.Forms.PropertyGrid _parametersGrid;
}
