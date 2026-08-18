namespace piootooapp.clientform.Shell.Controls;

partial class BacktestPickerDialog
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
        this.components = new System.ComponentModel.Container();
        this._filterPanel = new System.Windows.Forms.Panel();
        this._filterTextBox = new System.Windows.Forms.TextBox();
        this._filterLabel = new System.Windows.Forms.Label();
        this._grid = new System.Windows.Forms.DataGridView();
        this._folderColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._originColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._lastModifiedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._resultsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._rangeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._bindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._selectButton = new System.Windows.Forms.Button();
        this._cancelButton = new System.Windows.Forms.Button();
        this._statusLabel = new System.Windows.Forms.Label();
        this._filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // _filterPanel
        //
        this._filterPanel.Controls.Add(this._filterTextBox);
        this._filterPanel.Controls.Add(this._filterLabel);
        this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._filterPanel.Location = new System.Drawing.Point(0, 0);
        this._filterPanel.Name = "_filterPanel";
        this._filterPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 6);
        this._filterPanel.Size = new System.Drawing.Size(820, 44);
        this._filterPanel.TabIndex = 0;
        //
        // _filterLabel
        //
        this._filterLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._filterLabel.AutoSize = true;
        this._filterLabel.Location = new System.Drawing.Point(12, 14);
        this._filterLabel.Name = "_filterLabel";
        this._filterLabel.Size = new System.Drawing.Size(40, 15);
        this._filterLabel.TabIndex = 0;
        this._filterLabel.Text = "Filtro";
        //
        // _filterTextBox
        //
        this._filterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._filterTextBox.Location = new System.Drawing.Point(60, 10);
        this._filterTextBox.Name = "_filterTextBox";
        this._filterTextBox.PlaceholderText = "cartella, origine, piano o data";
        this._filterTextBox.Size = new System.Drawing.Size(748, 23);
        this._filterTextBox.TabIndex = 1;
        this._filterTextBox.TextChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // _grid
        //
        this._grid.AllowUserToAddRows = false;
        this._grid.AllowUserToDeleteRows = false;
        this._grid.AutoGenerateColumns = false;
        this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._folderColumn,
            this._originColumn,
            this._lastModifiedColumn,
            this._resultsColumn,
            this._rangeColumn});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 44);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(820, 392);
        this._grid.TabIndex = 1;
        this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridDoubleClick);
        //
        // _folderColumn
        //
        this._folderColumn.DataPropertyName = "FolderName";
        this._folderColumn.FillWeight = 210F;
        this._folderColumn.HeaderText = "Cartella";
        this._folderColumn.Name = "_folderColumn";
        this._folderColumn.ReadOnly = true;
        //
        // _originColumn
        //
        this._originColumn.DataPropertyName = "Origin";
        this._originColumn.FillWeight = 90F;
        this._originColumn.HeaderText = "Origine";
        this._originColumn.Name = "_originColumn";
        this._originColumn.ReadOnly = true;
        //
        // _lastModifiedColumn
        //
        this._lastModifiedColumn.DataPropertyName = "LastModifiedUtc";
        this._lastModifiedColumn.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._lastModifiedColumn.FillWeight = 100F;
        this._lastModifiedColumn.HeaderText = "Ultima modifica (UTC)";
        this._lastModifiedColumn.Name = "_lastModifiedColumn";
        this._lastModifiedColumn.ReadOnly = true;
        //
        // _resultsColumn
        //
        this._resultsColumn.DataPropertyName = "ResultsCount";
        this._resultsColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._resultsColumn.FillWeight = 60F;
        this._resultsColumn.HeaderText = "Risultati";
        this._resultsColumn.Name = "_resultsColumn";
        this._resultsColumn.ReadOnly = true;
        //
        // _rangeColumn
        //
        this._rangeColumn.DataPropertyName = "Range";
        this._rangeColumn.FillWeight = 120F;
        this._rangeColumn.HeaderText = "Periodo";
        this._rangeColumn.Name = "_rangeColumn";
        this._rangeColumn.ReadOnly = true;
        //
        // _buttons
        //
        this._buttons.Controls.Add(this._selectButton);
        this._buttons.Controls.Add(this._cancelButton);
        this._buttons.Controls.Add(this._statusLabel);
        this._buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this._buttons.Location = new System.Drawing.Point(0, 436);
        this._buttons.Name = "_buttons";
        this._buttons.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
        this._buttons.Size = new System.Drawing.Size(820, 49);
        this._buttons.TabIndex = 2;
        this._buttons.WrapContents = false;
        //
        // _selectButton
        //
        this._selectButton.AutoSize = true;
        this._selectButton.Name = "_selectButton";
        this._selectButton.Size = new System.Drawing.Size(95, 25);
        this._selectButton.TabIndex = 0;
        this._selectButton.Text = "Seleziona";
        this._selectButton.UseVisualStyleBackColor = true;
        this._selectButton.Click += new System.EventHandler(this.OnSelectClick);
        //
        // _cancelButton
        //
        this._cancelButton.AutoSize = true;
        this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this._cancelButton.Name = "_cancelButton";
        this._cancelButton.Size = new System.Drawing.Size(95, 25);
        this._cancelButton.TabIndex = 1;
        this._cancelButton.Text = "Annulla";
        this._cancelButton.UseVisualStyleBackColor = true;
        //
        // _statusLabel
        //
        this._statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._statusLabel.AutoSize = true;
        this._statusLabel.Margin = new System.Windows.Forms.Padding(12, 8, 12, 3);
        this._statusLabel.Name = "_statusLabel";
        this._statusLabel.Size = new System.Drawing.Size(80, 15);
        this._statusLabel.TabIndex = 2;
        this._statusLabel.Text = "0 backtest";
        //
        // BacktestPickerDialog
        //
        this.AcceptButton = this._selectButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._cancelButton;
        this.ClientSize = new System.Drawing.Size(820, 485);
        this.Controls.Add(this._grid);
        this.Controls.Add(this._filterPanel);
        this.Controls.Add(this._buttons);
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(620, 400);
        this.Name = "BacktestPickerDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Scegli il backtest sorgente";
        this._filterPanel.ResumeLayout(false);
        this._filterPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel _filterPanel;
    private System.Windows.Forms.Label _filterLabel;
    private System.Windows.Forms.TextBox _filterTextBox;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _folderColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _originColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _lastModifiedColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _resultsColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _rangeColumn;
    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _selectButton;
    private System.Windows.Forms.Button _cancelButton;
    private System.Windows.Forms.Label _statusLabel;
}
