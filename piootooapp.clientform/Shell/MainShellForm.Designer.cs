namespace piootooapp.clientform.Shell;

partial class MainShellForm
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
        this._menuStrip = new System.Windows.Forms.MenuStrip();
        this._fileMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._legacyConsoleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._fileMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
        this._exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._viewMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._serverStateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._viewMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
        this._themeMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._themeBlueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._themeGreenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._themeOrangeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this._serverPanel = new System.Windows.Forms.Panel();
        this._applyServerUrlButton = new System.Windows.Forms.Button();
        this._serverUrlTextBox = new System.Windows.Forms.TextBox();
        this._serverUrlLabel = new System.Windows.Forms.Label();
        this._splitContainer = new System.Windows.Forms.SplitContainer();
        this._navigationTree = new System.Windows.Forms.TreeView();
        this._contentPanel = new System.Windows.Forms.Panel();
        this._breadcrumbLabel = new System.Windows.Forms.Label();
        this._statusStrip = new System.Windows.Forms.StatusStrip();
        this._statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this._menuStrip.SuspendLayout();
        this._serverPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
        this._splitContainer.Panel1.SuspendLayout();
        this._splitContainer.Panel2.SuspendLayout();
        this._splitContainer.SuspendLayout();
        this._statusStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // _menuStrip
        // 
        this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileMenu,
            this._viewMenu});
        this._menuStrip.Location = new System.Drawing.Point(0, 0);
        this._menuStrip.Name = "_menuStrip";
        this._menuStrip.Size = new System.Drawing.Size(1180, 24);
        this._menuStrip.TabIndex = 0;
        // 
        // _fileMenu
        // 
        this._fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._legacyConsoleMenuItem,
            this._fileMenuSeparator,
            this._exitMenuItem});
        this._fileMenu.Name = "_fileMenu";
        this._fileMenu.Size = new System.Drawing.Size(37, 20);
        this._fileMenu.Text = "File";
        // 
        // _legacyConsoleMenuItem
        // 
        this._legacyConsoleMenuItem.Name = "_legacyConsoleMenuItem";
        this._legacyConsoleMenuItem.Size = new System.Drawing.Size(200, 22);
        this._legacyConsoleMenuItem.Text = "Console legacy…";
        this._legacyConsoleMenuItem.Click += new System.EventHandler(this.OnOpenLegacyConsoleClick);
        // 
        // _fileMenuSeparator
        // 
        this._fileMenuSeparator.Name = "_fileMenuSeparator";
        this._fileMenuSeparator.Size = new System.Drawing.Size(197, 6);
        // 
        // _exitMenuItem
        // 
        this._exitMenuItem.Name = "_exitMenuItem";
        this._exitMenuItem.Size = new System.Drawing.Size(200, 22);
        this._exitMenuItem.Text = "Esci";
        this._exitMenuItem.Click += new System.EventHandler(this.OnExitClick);
        // 
        // _viewMenu
        // 
        this._viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._refreshMenuItem,
            this._serverStateMenuItem,
            this._viewMenuSeparator,
            this._themeMenu});
        this._viewMenu.Name = "_viewMenu";
        this._viewMenu.Size = new System.Drawing.Size(78, 20);
        this._viewMenu.Text = "Visualizza";
        //
        // _refreshMenuItem
        //
        this._refreshMenuItem.Name = "_refreshMenuItem";
        this._refreshMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
        this._refreshMenuItem.Size = new System.Drawing.Size(200, 22);
        this._refreshMenuItem.Text = "Aggiorna schermata";
        this._refreshMenuItem.Click += new System.EventHandler(this.OnRefreshCurrentScreenClick);
        //
        // _serverStateMenuItem
        //
        this._serverStateMenuItem.Name = "_serverStateMenuItem";
        this._serverStateMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F9;
        this._serverStateMenuItem.Size = new System.Drawing.Size(200, 22);
        this._serverStateMenuItem.Text = "Stato server (sessioni)…";
        this._serverStateMenuItem.ToolTipText = "Istantanea diagnostica delle sessioni vive sul server, copiabile.";
        this._serverStateMenuItem.Click += new System.EventHandler(this.OnOpenServerStateClick);
        //
        // _viewMenuSeparator
        //
        this._viewMenuSeparator.Name = "_viewMenuSeparator";
        this._viewMenuSeparator.Size = new System.Drawing.Size(197, 6);
        //
        // _themeMenu
        //
        this._themeMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._themeBlueMenuItem,
            this._themeGreenMenuItem,
            this._themeOrangeMenuItem});
        this._themeMenu.Name = "_themeMenu";
        this._themeMenu.Size = new System.Drawing.Size(200, 22);
        this._themeMenu.Text = "Tema";
        //
        // _themeBlueMenuItem
        //
        this._themeBlueMenuItem.Name = "_themeBlueMenuItem";
        this._themeBlueMenuItem.Size = new System.Drawing.Size(150, 22);
        this._themeBlueMenuItem.Text = "Blu";
        this._themeBlueMenuItem.Click += new System.EventHandler(this.OnThemeMenuItemClick);
        //
        // _themeGreenMenuItem
        //
        this._themeGreenMenuItem.Name = "_themeGreenMenuItem";
        this._themeGreenMenuItem.Size = new System.Drawing.Size(150, 22);
        this._themeGreenMenuItem.Text = "Verde";
        this._themeGreenMenuItem.Click += new System.EventHandler(this.OnThemeMenuItemClick);
        //
        // _themeOrangeMenuItem
        //
        this._themeOrangeMenuItem.Name = "_themeOrangeMenuItem";
        this._themeOrangeMenuItem.Size = new System.Drawing.Size(150, 22);
        this._themeOrangeMenuItem.Text = "Arancione";
        this._themeOrangeMenuItem.Click += new System.EventHandler(this.OnThemeMenuItemClick);
        //
        // _serverPanel
        // 
        this._serverPanel.Controls.Add(this._applyServerUrlButton);
        this._serverPanel.Controls.Add(this._serverUrlTextBox);
        this._serverPanel.Controls.Add(this._serverUrlLabel);
        this._serverPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._serverPanel.Location = new System.Drawing.Point(0, 24);
        this._serverPanel.Name = "_serverPanel";
        this._serverPanel.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
        this._serverPanel.Size = new System.Drawing.Size(1180, 38);
        this._serverPanel.TabIndex = 1;
        // 
        // _serverUrlLabel
        // 
        this._serverUrlLabel.AutoSize = true;
        this._serverUrlLabel.Location = new System.Drawing.Point(11, 11);
        this._serverUrlLabel.Name = "_serverUrlLabel";
        this._serverUrlLabel.Size = new System.Drawing.Size(64, 15);
        this._serverUrlLabel.TabIndex = 0;
        this._serverUrlLabel.Text = "Server API";
        // 
        // _serverUrlTextBox
        // 
        this._serverUrlTextBox.Location = new System.Drawing.Point(84, 8);
        this._serverUrlTextBox.Name = "_serverUrlTextBox";
        this._serverUrlTextBox.Size = new System.Drawing.Size(280, 23);
        this._serverUrlTextBox.TabIndex = 1;
        // 
        // _applyServerUrlButton
        // 
        this._applyServerUrlButton.Location = new System.Drawing.Point(372, 7);
        this._applyServerUrlButton.Name = "_applyServerUrlButton";
        this._applyServerUrlButton.Size = new System.Drawing.Size(90, 25);
        this._applyServerUrlButton.TabIndex = 2;
        this._applyServerUrlButton.Text = "Applica";
        this._applyServerUrlButton.UseVisualStyleBackColor = true;
        this._applyServerUrlButton.Click += new System.EventHandler(this.OnApplyServerUrlClick);
        // 
        // _splitContainer
        // 
        this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this._splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        this._splitContainer.Location = new System.Drawing.Point(0, 62);
        this._splitContainer.Name = "_splitContainer";
        this._splitContainer.Panel1.Controls.Add(this._navigationTree);
        this._splitContainer.Panel1MinSize = 160;
        this._splitContainer.Panel2.Controls.Add(this._contentPanel);
        this._splitContainer.Panel2.Controls.Add(this._breadcrumbLabel);
        this._splitContainer.Size = new System.Drawing.Size(1180, 636);
        this._splitContainer.SplitterDistance = 230;
        this._splitContainer.TabIndex = 2;
        // 
        // _navigationTree
        // 
        this._navigationTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._navigationTree.Dock = System.Windows.Forms.DockStyle.Fill;
        this._navigationTree.FullRowSelect = true;
        this._navigationTree.HideSelection = false;
        this._navigationTree.ItemHeight = 24;
        this._navigationTree.Location = new System.Drawing.Point(0, 0);
        this._navigationTree.Name = "_navigationTree";
        this._navigationTree.ShowLines = false;
        this._navigationTree.ShowNodeToolTips = true;
        this._navigationTree.ShowPlusMinus = false;
        this._navigationTree.ShowRootLines = false;
        this._navigationTree.Size = new System.Drawing.Size(230, 636);
        this._navigationTree.TabIndex = 0;
        this._navigationTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.OnNavigationNodeClick);
        // 
        // _breadcrumbLabel
        // 
        this._breadcrumbLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
        this._breadcrumbLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this._breadcrumbLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._breadcrumbLabel.Location = new System.Drawing.Point(0, 0);
        this._breadcrumbLabel.Name = "_breadcrumbLabel";
        this._breadcrumbLabel.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
        this._breadcrumbLabel.Size = new System.Drawing.Size(946, 28);
        this._breadcrumbLabel.TabIndex = 0;
        // 
        // _contentPanel
        // 
        this._contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this._contentPanel.Location = new System.Drawing.Point(0, 28);
        this._contentPanel.Name = "_contentPanel";
        this._contentPanel.Size = new System.Drawing.Size(946, 608);
        this._contentPanel.TabIndex = 1;
        // 
        // _statusStrip
        // 
        this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusLabel});
        this._statusStrip.Location = new System.Drawing.Point(0, 698);
        this._statusStrip.Name = "_statusStrip";
        this._statusStrip.Size = new System.Drawing.Size(1180, 22);
        this._statusStrip.TabIndex = 3;
        // 
        // _statusLabel
        // 
        this._statusLabel.Name = "_statusLabel";
        this._statusLabel.Size = new System.Drawing.Size(0, 17);
        // 
        // MainShellForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1180, 720);
        this.Controls.Add(this._splitContainer);
        this.Controls.Add(this._statusStrip);
        this.Controls.Add(this._serverPanel);
        this.Controls.Add(this._menuStrip);
        this.MainMenuStrip = this._menuStrip;
        this.MinimumSize = new System.Drawing.Size(900, 560);
        this.Name = "MainShellForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Piootoo Console";
        this._menuStrip.ResumeLayout(false);
        this._menuStrip.PerformLayout();
        this._serverPanel.ResumeLayout(false);
        this._serverPanel.PerformLayout();
        this._splitContainer.Panel1.ResumeLayout(false);
        this._splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
        this._splitContainer.ResumeLayout(false);
        this._statusStrip.ResumeLayout(false);
        this._statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip _menuStrip;
    private System.Windows.Forms.ToolStripMenuItem _fileMenu;
    private System.Windows.Forms.ToolStripMenuItem _legacyConsoleMenuItem;
    private System.Windows.Forms.ToolStripSeparator _fileMenuSeparator;
    private System.Windows.Forms.ToolStripMenuItem _exitMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _viewMenu;
    private System.Windows.Forms.ToolStripMenuItem _refreshMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _serverStateMenuItem;
    private System.Windows.Forms.ToolStripSeparator _viewMenuSeparator;
    private System.Windows.Forms.ToolStripMenuItem _themeMenu;
    private System.Windows.Forms.ToolStripMenuItem _themeBlueMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _themeGreenMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _themeOrangeMenuItem;
    private System.Windows.Forms.Panel _serverPanel;
    private System.Windows.Forms.Label _serverUrlLabel;
    private System.Windows.Forms.TextBox _serverUrlTextBox;
    private System.Windows.Forms.Button _applyServerUrlButton;
    private System.Windows.Forms.SplitContainer _splitContainer;
    private System.Windows.Forms.TreeView _navigationTree;
    private System.Windows.Forms.Label _breadcrumbLabel;
    private System.Windows.Forms.Panel _contentPanel;
    private System.Windows.Forms.StatusStrip _statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel _statusLabel;
}
