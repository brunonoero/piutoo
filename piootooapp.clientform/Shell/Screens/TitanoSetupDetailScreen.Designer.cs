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
        this._optionsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._advancedCheckBox = new System.Windows.Forms.CheckBox();
        this._presetLabel = new System.Windows.Forms.Label();
        this._presetCombo = new System.Windows.Forms.ComboBox();
        this._presetApplyButton = new System.Windows.Forms.Button();
        this._parametersGrid = new System.Windows.Forms.PropertyGrid();
        this._summarySplitter = new System.Windows.Forms.Splitter();
        this._summaryPanel = new System.Windows.Forms.Panel();
        this._summaryLayout = new System.Windows.Forms.TableLayoutPanel();
        this._warningsLabel = new System.Windows.Forms.Label();
        this._summaryLabel = new System.Windows.Forms.Label();
        this._summaryTitleLabel = new System.Windows.Forms.Label();
        this._headerLayout.SuspendLayout();
        this._optionsPanel.SuspendLayout();
        this._summaryPanel.SuspendLayout();
        this._summaryLayout.SuspendLayout();
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
        // _optionsPanel
        //
        this._optionsPanel.AutoSize = true;
        this._optionsPanel.Controls.Add(this._advancedCheckBox);
        this._optionsPanel.Controls.Add(this._presetLabel);
        this._optionsPanel.Controls.Add(this._presetCombo);
        this._optionsPanel.Controls.Add(this._presetApplyButton);
        this._optionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._optionsPanel.Name = "_optionsPanel";
        this._optionsPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 8);
        this._optionsPanel.Size = new System.Drawing.Size(900, 40);
        this._optionsPanel.TabIndex = 2;
        this._optionsPanel.WrapContents = false;
        //
        // _advancedCheckBox
        //
        this._advancedCheckBox.AutoSize = true;
        this._advancedCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 32, 3);
        this._advancedCheckBox.Name = "_advancedCheckBox";
        this._advancedCheckBox.Size = new System.Drawing.Size(220, 19);
        this._advancedCheckBox.TabIndex = 0;
        this._advancedCheckBox.Text = "Mostra anche i parametri avanzati";
        this._advancedCheckBox.UseVisualStyleBackColor = true;
        this._advancedCheckBox.CheckedChanged += new System.EventHandler(this.OnAdvancedToggled);
        //
        // _presetLabel
        //
        this._presetLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._presetLabel.AutoSize = true;
        this._presetLabel.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
        this._presetLabel.Name = "_presetLabel";
        this._presetLabel.Size = new System.Drawing.Size(110, 15);
        this._presetLabel.TabIndex = 1;
        this._presetLabel.Text = "Parti da un preset";
        //
        // _presetCombo
        //
        this._presetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._presetCombo.Margin = new System.Windows.Forms.Padding(3, 4, 8, 3);
        this._presetCombo.Name = "_presetCombo";
        this._presetCombo.Size = new System.Drawing.Size(240, 23);
        this._presetCombo.TabIndex = 2;
        //
        // _presetApplyButton
        //
        this._presetApplyButton.AutoSize = true;
        this._presetApplyButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this._presetApplyButton.Name = "_presetApplyButton";
        this._presetApplyButton.Size = new System.Drawing.Size(90, 25);
        this._presetApplyButton.TabIndex = 3;
        this._presetApplyButton.Text = "Applica";
        this._presetApplyButton.UseVisualStyleBackColor = true;
        this._presetApplyButton.Click += new System.EventHandler(this.OnApplyPresetClick);
        //
        // _summarySplitter
        //
        // Il riquadro descrittivo è basso di proposito e scorre; chi vuole leggerlo tutto insieme
        // lo allarga da qui invece di perdere per sempre spazio al grid.
        this._summarySplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._summarySplitter.Height = 4;
        this._summarySplitter.MinExtra = 200;
        this._summarySplitter.MinSize = 48;
        this._summarySplitter.Name = "_summarySplitter";
        this._summarySplitter.TabStop = false;
        //
        // _summaryPanel
        //
        // AutoScroll con figli in Dock.Top non calcola l'area virtuale in modo affidabile: il
        // contenuto sta quindi in un TableLayoutPanel che cresce in altezza, ed è quello a
        // sforare il pannello e a far comparire la barra.
        this._summaryPanel.AutoScroll = true;
        this._summaryPanel.Controls.Add(this._summaryLayout);
        this._summaryPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._summaryPanel.Name = "_summaryPanel";
        this._summaryPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
        this._summaryPanel.Size = new System.Drawing.Size(900, 104);
        this._summaryPanel.TabIndex = 4;
        //
        // _summaryLayout
        //
        this._summaryLayout.AutoSize = true;
        this._summaryLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._summaryLayout.ColumnCount = 1;
        this._summaryLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._summaryLayout.Controls.Add(this._summaryTitleLabel, 0, 0);
        this._summaryLayout.Controls.Add(this._summaryLabel, 0, 1);
        this._summaryLayout.Controls.Add(this._warningsLabel, 0, 2);
        this._summaryLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._summaryLayout.Name = "_summaryLayout";
        this._summaryLayout.RowCount = 3;
        this._summaryLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._summaryLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._summaryLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this._summaryLayout.Size = new System.Drawing.Size(876, 88);
        this._summaryLayout.TabIndex = 0;
        //
        // _summaryTitleLabel
        //
        this._summaryTitleLabel.AutoSize = true;
        this._summaryTitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this._summaryTitleLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this._summaryTitleLabel.Name = "_summaryTitleLabel";
        this._summaryTitleLabel.Size = new System.Drawing.Size(300, 15);
        this._summaryTitleLabel.TabIndex = 0;
        this._summaryTitleLabel.Text = "Cosa farà questa configurazione";
        //
        // _summaryLabel
        //
        this._summaryLabel.AutoSize = true;
        this._summaryLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
        this._summaryLabel.Name = "_summaryLabel";
        this._summaryLabel.Size = new System.Drawing.Size(860, 30);
        this._summaryLabel.TabIndex = 1;
        //
        // _warningsLabel
        //
        this._warningsLabel.AutoSize = true;
        this._warningsLabel.ForeColor = System.Drawing.Color.FromArgb(150, 75, 0);
        this._warningsLabel.Margin = new System.Windows.Forms.Padding(0);
        this._warningsLabel.Name = "_warningsLabel";
        this._warningsLabel.Size = new System.Drawing.Size(860, 30);
        this._warningsLabel.TabIndex = 2;
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
        // L'ordine conta: il docking si applica in ordine inverso di aggiunta, quindi il primo
        // aggiunto (Fill) resta al centro e gli ultimi occupano i bordi esterni.
        this.Controls.Add(this._parametersGrid);
        this.Controls.Add(this._summarySplitter);
        this.Controls.Add(this._summaryPanel);
        this.Controls.Add(this._optionsPanel);
        this.Controls.Add(this._headerLayout);
        this.Controls.Add(this._toolbar);
        this.Name = "TitanoSetupDetailScreen";
        this.Size = new System.Drawing.Size(900, 820);
        this._headerLayout.ResumeLayout(false);
        this._headerLayout.PerformLayout();
        this._optionsPanel.ResumeLayout(false);
        this._optionsPanel.PerformLayout();
        this._summaryLayout.ResumeLayout(false);
        this._summaryLayout.PerformLayout();
        this._summaryPanel.ResumeLayout(false);
        this._summaryPanel.PerformLayout();
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
    private System.Windows.Forms.FlowLayoutPanel _optionsPanel;
    private System.Windows.Forms.CheckBox _advancedCheckBox;
    private System.Windows.Forms.Label _presetLabel;
    private System.Windows.Forms.ComboBox _presetCombo;
    private System.Windows.Forms.Button _presetApplyButton;
    private System.Windows.Forms.PropertyGrid _parametersGrid;
    private System.Windows.Forms.Splitter _summarySplitter;
    private System.Windows.Forms.Panel _summaryPanel;
    private System.Windows.Forms.TableLayoutPanel _summaryLayout;
    private System.Windows.Forms.Label _summaryTitleLabel;
    private System.Windows.Forms.Label _summaryLabel;
    private System.Windows.Forms.Label _warningsLabel;
}
