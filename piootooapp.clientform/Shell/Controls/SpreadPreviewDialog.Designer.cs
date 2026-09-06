namespace piootooapp.clientform.Shell.Controls;

partial class SpreadPreviewDialog
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
        this._headerPanel = new System.Windows.Forms.Panel();
        this._sourceLabel = new System.Windows.Forms.Label();
        this._grid = new System.Windows.Forms.DataGridView();
        this._symbolColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._pointsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._hourMinColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._hourMaxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._spanColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._fallbackColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._bindingSource = new System.Windows.Forms.BindingSource(this.components);
        this._warningsBox = new System.Windows.Forms.TextBox();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._closeButton = new System.Windows.Forms.Button();
        this._statusLabel = new System.Windows.Forms.Label();
        this._headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // _headerPanel
        //
        this._headerPanel.AutoSize = true;
        this._headerPanel.Controls.Add(this._sourceLabel);
        this._headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._headerPanel.Location = new System.Drawing.Point(0, 0);
        this._headerPanel.Name = "_headerPanel";
        this._headerPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 6);
        this._headerPanel.Size = new System.Drawing.Size(860, 44);
        this._headerPanel.TabIndex = 0;
        //
        // _sourceLabel
        //
        this._sourceLabel.AutoSize = true;
        this._sourceLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._sourceLabel.Location = new System.Drawing.Point(12, 10);
        this._sourceLabel.Name = "_sourceLabel";
        this._sourceLabel.Size = new System.Drawing.Size(400, 15);
        this._sourceLabel.TabIndex = 0;
        //
        // _grid
        //
        this._grid.AllowUserToAddRows = false;
        this._grid.AllowUserToDeleteRows = false;
        this._grid.AutoGenerateColumns = false;
        this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._symbolColumn,
            this._pointsColumn,
            this._hourMinColumn,
            this._hourMaxColumn,
            this._spanColumn,
            this._fallbackColumn});
        this._grid.DataSource = this._bindingSource;
        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._grid.Location = new System.Drawing.Point(0, 44);
        this._grid.MultiSelect = false;
        this._grid.Name = "_grid";
        this._grid.ReadOnly = true;
        this._grid.RowHeadersVisible = false;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.Size = new System.Drawing.Size(860, 300);
        this._grid.TabIndex = 1;
        //
        // _symbolColumn
        //
        this._symbolColumn.DataPropertyName = "Symbol";
        this._symbolColumn.FillWeight = 80F;
        this._symbolColumn.HeaderText = "Symbol";
        this._symbolColumn.Name = "_symbolColumn";
        this._symbolColumn.ReadOnly = true;
        //
        // _pointsColumn
        //
        this._pointsColumn.DataPropertyName = "Points";
        this._pointsColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._pointsColumn.DefaultCellStyle.Format = "0.#####";
        this._pointsColumn.FillWeight = 90F;
        this._pointsColumn.HeaderText = "Spread (punti)";
        this._pointsColumn.Name = "_pointsColumn";
        this._pointsColumn.ReadOnly = true;
        //
        // _hourMinColumn
        //
        this._hourMinColumn.DataPropertyName = "HourMinText";
        this._hourMinColumn.FillWeight = 100F;
        this._hourMinColumn.HeaderText = "Ora minima (UTC)";
        this._hourMinColumn.Name = "_hourMinColumn";
        this._hourMinColumn.ReadOnly = true;
        //
        // _hourMaxColumn
        //
        this._hourMaxColumn.DataPropertyName = "HourMaxText";
        this._hourMaxColumn.FillWeight = 100F;
        this._hourMaxColumn.HeaderText = "Ora massima (UTC)";
        this._hourMaxColumn.Name = "_hourMaxColumn";
        this._hourMaxColumn.ReadOnly = true;
        //
        // _spanColumn
        //
        this._spanColumn.DataPropertyName = "Span";
        this._spanColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._spanColumn.DefaultCellStyle.Format = "0.0";
        this._spanColumn.FillWeight = 60F;
        this._spanColumn.HeaderText = "max/min";
        this._spanColumn.Name = "_spanColumn";
        this._spanColumn.ReadOnly = true;
        //
        // _fallbackColumn
        //
        this._fallbackColumn.DataPropertyName = "FallbackHours";
        this._fallbackColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._fallbackColumn.FillWeight = 70F;
        this._fallbackColumn.HeaderText = "Ore ripiegate";
        this._fallbackColumn.Name = "_fallbackColumn";
        this._fallbackColumn.ReadOnly = true;
        //
        // _warningsBox
        //
        this._warningsBox.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._warningsBox.Location = new System.Drawing.Point(0, 344);
        this._warningsBox.Multiline = true;
        this._warningsBox.Name = "_warningsBox";
        this._warningsBox.ReadOnly = true;
        this._warningsBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._warningsBox.Size = new System.Drawing.Size(860, 92);
        this._warningsBox.TabIndex = 2;
        //
        // _buttons
        //
        this._buttons.Controls.Add(this._closeButton);
        this._buttons.Controls.Add(this._statusLabel);
        this._buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this._buttons.Location = new System.Drawing.Point(0, 436);
        this._buttons.Name = "_buttons";
        this._buttons.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
        this._buttons.Size = new System.Drawing.Size(860, 49);
        this._buttons.TabIndex = 3;
        this._buttons.WrapContents = false;
        //
        // _closeButton
        //
        this._closeButton.AutoSize = true;
        this._closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        this._closeButton.Name = "_closeButton";
        this._closeButton.Size = new System.Drawing.Size(95, 25);
        this._closeButton.TabIndex = 0;
        this._closeButton.Text = "Chiudi";
        this._closeButton.UseVisualStyleBackColor = true;
        //
        // _statusLabel
        //
        this._statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._statusLabel.AutoSize = true;
        this._statusLabel.Margin = new System.Windows.Forms.Padding(12, 8, 12, 3);
        this._statusLabel.Name = "_statusLabel";
        this._statusLabel.Size = new System.Drawing.Size(80, 15);
        this._statusLabel.TabIndex = 1;
        //
        // SpreadPreviewDialog
        //
        this.AcceptButton = this._closeButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._closeButton;
        this.ClientSize = new System.Drawing.Size(860, 485);
        this.Controls.Add(this._grid);
        this.Controls.Add(this._warningsBox);
        this.Controls.Add(this._buttons);
        this.Controls.Add(this._headerPanel);
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(680, 420);
        this.Name = "SpreadPreviewDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Spread che il run applicherebbe";
        this._headerPanel.ResumeLayout(false);
        this._headerPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Panel _headerPanel;
    private System.Windows.Forms.Label _sourceLabel;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _symbolColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _pointsColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _hourMinColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _hourMaxColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _spanColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _fallbackColumn;
    private System.Windows.Forms.BindingSource _bindingSource;
    private System.Windows.Forms.TextBox _warningsBox;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private System.Windows.Forms.Button _closeButton;
    private System.Windows.Forms.Label _statusLabel;
}
