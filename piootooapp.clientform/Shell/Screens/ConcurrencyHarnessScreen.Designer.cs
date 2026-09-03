namespace piootooapp.clientform.Shell.Screens;

partial class ConcurrencyHarnessScreen
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runCts?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        this._configPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._workspaceLabel = new System.Windows.Forms.Label();
        this._workspaceValueLabel = new System.Windows.Forms.Label();
        this._planLabel = new System.Windows.Forms.Label();
        this._planCombo = new System.Windows.Forms.ComboBox();
        this._runModeLabel = new System.Windows.Forms.Label();
        this._runModeCombo = new System.Windows.Forms.ComboBox();
        this._daysLabel = new System.Windows.Forms.Label();
        this._daysInput = new System.Windows.Forms.NumericUpDown();
        this._closeAfterLabel = new System.Windows.Forms.Label();
        this._closeAfterInput = new System.Windows.Forms.NumericUpDown();
        this._limitsLabel = new System.Windows.Forms.Label();
        this._groupsPanel = new System.Windows.Forms.Panel();
        this._groupsGrid = new System.Windows.Forms.DataGridView();
        this._colGroupId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAccountNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMaxConcurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._groupsCommands = new System.Windows.Forms.FlowLayoutPanel();
        this._groupsTitle = new System.Windows.Forms.Label();
        this._applyGroupsButton = new System.Windows.Forms.Button();
        this._addRowButton = new System.Windows.Forms.Button();
        this._removeRowButton = new System.Windows.Forms.Button();
        this._runPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._prepareButton = new System.Windows.Forms.Button();
        this._stepButton = new System.Windows.Forms.Button();
        this._runButton = new System.Windows.Forms.Button();
        this._stopButton = new System.Windows.Forms.Button();
        this._resetButton = new System.Windows.Forms.Button();
        this._progressLabel = new System.Windows.Forms.Label();
        this._tabs = new System.Windows.Forms.TabControl();
        this._pollTab = new System.Windows.Forms.TabPage();
        this._pollGrid = new System.Windows.Forms.DataGridView();
        this._colPollBar = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollBarSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollAccount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollOutcome = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollOpen = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollPending = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPollNote = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._matrixTab = new System.Windows.Forms.TabPage();
        this._matrixGrid = new System.Windows.Forms.DataGridView();
        this._colMatrixAccount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixPolls = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixEntries = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixCloses = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixLock = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colMatrixOpen = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._templateTab = new System.Windows.Forms.TabPage();
        this._templateGrid = new System.Windows.Forms.DataGridView();
        this._colTemplateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateGroups = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateAccounts = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colTemplateState = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._reasonTab = new System.Windows.Forms.TabPage();
        this._reasonGrid = new System.Windows.Forms.DataGridView();
        this._colReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colReasonMeaning = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colReasonCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colReasonShare = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._groupsSource = new System.Windows.Forms.BindingSource(this.components);
        this._pollSource = new System.Windows.Forms.BindingSource(this.components);
        this._matrixSource = new System.Windows.Forms.BindingSource(this.components);
        this._templateSource = new System.Windows.Forms.BindingSource(this.components);
        this._reasonSource = new System.Windows.Forms.BindingSource(this.components);
        ((System.ComponentModel.ISupportInitialize)(this._daysInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._closeAfterInput)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._pollGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._matrixGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._templateGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._reasonGrid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._pollSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._matrixSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._templateSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._reasonSource)).BeginInit();
        this._configPanel.SuspendLayout();
        this._groupsPanel.SuspendLayout();
        this._groupsCommands.SuspendLayout();
        this._runPanel.SuspendLayout();
        this._tabs.SuspendLayout();
        this._pollTab.SuspendLayout();
        this._matrixTab.SuspendLayout();
        this._templateTab.SuspendLayout();
        this._reasonTab.SuspendLayout();
        this.SuspendLayout();
        //
        // _tabs
        //
        this._tabs.Controls.Add(this._pollTab);
        this._tabs.Controls.Add(this._matrixTab);
        this._tabs.Controls.Add(this._templateTab);
        this._tabs.Controls.Add(this._reasonTab);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 332);
        this._tabs.Name = "_tabs";
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(1100, 368);
        this._tabs.TabIndex = 4;
        //
        // _pollTab
        //
        this._pollTab.Controls.Add(this._pollGrid);
        this._pollTab.Location = new System.Drawing.Point(4, 24);
        this._pollTab.Name = "_pollTab";
        this._pollTab.Padding = new System.Windows.Forms.Padding(3);
        this._pollTab.Size = new System.Drawing.Size(1092, 340);
        this._pollTab.TabIndex = 0;
        this._pollTab.Text = "Decisioni per poll";
        this._pollTab.UseVisualStyleBackColor = true;
        //
        // _pollGrid
        //
        this._pollGrid.AllowUserToAddRows = false;
        this._pollGrid.AllowUserToDeleteRows = false;
        this._pollGrid.AutoGenerateColumns = false;
        this._pollGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._pollGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._pollGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._pollGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colPollBar,
            this._colPollBarSymbol,
            this._colPollAccount,
            this._colPollGroup,
            this._colPollOutcome,
            this._colPollStrategy,
            this._colPollSymbol,
            this._colPollQuantity,
            this._colPollOpen,
            this._colPollPending,
            this._colPollMax,
            this._colPollNote});
        this._pollGrid.DataSource = this._pollSource;
        this._pollGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pollGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._pollGrid.Location = new System.Drawing.Point(3, 3);
        this._pollGrid.MultiSelect = false;
        this._pollGrid.Name = "_pollGrid";
        this._pollGrid.ReadOnly = true;
        this._pollGrid.RowHeadersVisible = false;
        this._pollGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._pollGrid.Size = new System.Drawing.Size(1086, 334);
        this._pollGrid.TabIndex = 0;
        //
        // colonne di _pollGrid
        //
        this._colPollBar.DataPropertyName = "BarTimeUtc";
        this._colPollBar.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colPollBar.FillWeight = 110F;
        this._colPollBar.HeaderText = "Barra (UTC)";
        this._colPollBar.Name = "_colPollBar";
        this._colPollBar.ReadOnly = true;
        this._colPollBarSymbol.DataPropertyName = "BarSymbol";
        this._colPollBarSymbol.FillWeight = 60F;
        this._colPollBarSymbol.HeaderText = "Stream";
        this._colPollBarSymbol.Name = "_colPollBarSymbol";
        this._colPollBarSymbol.ReadOnly = true;
        this._colPollAccount.DataPropertyName = "AccountNumber";
        this._colPollAccount.FillWeight = 70F;
        this._colPollAccount.HeaderText = "Account";
        this._colPollAccount.Name = "_colPollAccount";
        this._colPollAccount.ReadOnly = true;
        this._colPollGroup.DataPropertyName = "GroupId";
        this._colPollGroup.FillWeight = 60F;
        this._colPollGroup.HeaderText = "Gruppo";
        this._colPollGroup.Name = "_colPollGroup";
        this._colPollGroup.ReadOnly = true;
        this._colPollOutcome.DataPropertyName = "Outcome";
        this._colPollOutcome.FillWeight = 130F;
        this._colPollOutcome.HeaderText = "Esito";
        this._colPollOutcome.Name = "_colPollOutcome";
        this._colPollOutcome.ReadOnly = true;
        this._colPollStrategy.DataPropertyName = "StrategyCode";
        this._colPollStrategy.FillWeight = 90F;
        this._colPollStrategy.HeaderText = "Strategia";
        this._colPollStrategy.Name = "_colPollStrategy";
        this._colPollStrategy.ReadOnly = true;
        this._colPollSymbol.DataPropertyName = "Symbol";
        this._colPollSymbol.FillWeight = 60F;
        this._colPollSymbol.HeaderText = "Simbolo";
        this._colPollSymbol.Name = "_colPollSymbol";
        this._colPollSymbol.ReadOnly = true;
        this._colPollQuantity.DataPropertyName = "Quantity";
        this._colPollQuantity.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colPollQuantity.FillWeight = 55F;
        this._colPollQuantity.HeaderText = "Qtà";
        this._colPollQuantity.Name = "_colPollQuantity";
        this._colPollQuantity.ReadOnly = true;
        this._colPollOpen.DataPropertyName = "OpenPositions";
        this._colPollOpen.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colPollOpen.FillWeight = 55F;
        this._colPollOpen.HeaderText = "Aperte";
        this._colPollOpen.Name = "_colPollOpen";
        this._colPollOpen.ReadOnly = true;
        this._colPollPending.DataPropertyName = "PendingOrders";
        this._colPollPending.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colPollPending.FillWeight = 60F;
        this._colPollPending.HeaderText = "Pendenti";
        this._colPollPending.Name = "_colPollPending";
        this._colPollPending.ReadOnly = true;
        this._colPollMax.DataPropertyName = "MaxConcurrentTrades";
        this._colPollMax.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colPollMax.FillWeight = 50F;
        this._colPollMax.HeaderText = "Max";
        this._colPollMax.Name = "_colPollMax";
        this._colPollMax.ReadOnly = true;
        this._colPollNote.DataPropertyName = "Note";
        this._colPollNote.FillWeight = 220F;
        this._colPollNote.HeaderText = "Perché";
        this._colPollNote.Name = "_colPollNote";
        this._colPollNote.ReadOnly = true;
        //
        // _matrixTab
        //
        this._matrixTab.Controls.Add(this._matrixGrid);
        this._matrixTab.Location = new System.Drawing.Point(4, 24);
        this._matrixTab.Name = "_matrixTab";
        this._matrixTab.Padding = new System.Windows.Forms.Padding(3);
        this._matrixTab.Size = new System.Drawing.Size(1092, 340);
        this._matrixTab.TabIndex = 1;
        this._matrixTab.Text = "Matrice account × gruppo";
        this._matrixTab.UseVisualStyleBackColor = true;
        //
        // _matrixGrid
        //
        this._matrixGrid.AllowUserToAddRows = false;
        this._matrixGrid.AllowUserToDeleteRows = false;
        this._matrixGrid.AutoGenerateColumns = false;
        this._matrixGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._matrixGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._matrixGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._matrixGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colMatrixAccount,
            this._colMatrixGroup,
            this._colMatrixMax,
            this._colMatrixPolls,
            this._colMatrixEntries,
            this._colMatrixCloses,
            this._colMatrixLimit,
            this._colMatrixLock,
            this._colMatrixOpen});
        this._matrixGrid.DataSource = this._matrixSource;
        this._matrixGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._matrixGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._matrixGrid.Location = new System.Drawing.Point(3, 3);
        this._matrixGrid.MultiSelect = false;
        this._matrixGrid.Name = "_matrixGrid";
        this._matrixGrid.ReadOnly = true;
        this._matrixGrid.RowHeadersVisible = false;
        this._matrixGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._matrixGrid.Size = new System.Drawing.Size(1086, 334);
        this._matrixGrid.TabIndex = 0;
        //
        // colonne di _matrixGrid
        //
        this._colMatrixAccount.DataPropertyName = "AccountNumber";
        this._colMatrixAccount.FillWeight = 80F;
        this._colMatrixAccount.HeaderText = "Account";
        this._colMatrixAccount.Name = "_colMatrixAccount";
        this._colMatrixAccount.ReadOnly = true;
        this._colMatrixGroup.DataPropertyName = "GroupId";
        this._colMatrixGroup.FillWeight = 70F;
        this._colMatrixGroup.HeaderText = "Gruppo";
        this._colMatrixGroup.Name = "_colMatrixGroup";
        this._colMatrixGroup.ReadOnly = true;
        this._colMatrixMax.DataPropertyName = "MaxConcurrentTrades";
        this._colMatrixMax.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixMax.FillWeight = 50F;
        this._colMatrixMax.HeaderText = "Max";
        this._colMatrixMax.Name = "_colMatrixMax";
        this._colMatrixMax.ReadOnly = true;
        this._colMatrixPolls.DataPropertyName = "Polls";
        this._colMatrixPolls.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixPolls.FillWeight = 50F;
        this._colMatrixPolls.HeaderText = "Poll";
        this._colMatrixPolls.Name = "_colMatrixPolls";
        this._colMatrixPolls.ReadOnly = true;
        this._colMatrixEntries.DataPropertyName = "Entries";
        this._colMatrixEntries.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixEntries.FillWeight = 60F;
        this._colMatrixEntries.HeaderText = "Ingressi";
        this._colMatrixEntries.Name = "_colMatrixEntries";
        this._colMatrixEntries.ReadOnly = true;
        this._colMatrixCloses.DataPropertyName = "Closes";
        this._colMatrixCloses.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixCloses.FillWeight = 60F;
        this._colMatrixCloses.HeaderText = "Chiusure";
        this._colMatrixCloses.Name = "_colMatrixCloses";
        this._colMatrixCloses.ReadOnly = true;
        this._colMatrixLimit.DataPropertyName = "LimitRejections";
        this._colMatrixLimit.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixLimit.FillWeight = 110F;
        this._colMatrixLimit.HeaderText = "Negati dal limite";
        this._colMatrixLimit.Name = "_colMatrixLimit";
        this._colMatrixLimit.ReadOnly = true;
        this._colMatrixLock.DataPropertyName = "LockRejections";
        this._colMatrixLock.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixLock.FillWeight = 120F;
        this._colMatrixLock.HeaderText = "Negati dai lucchetti";
        this._colMatrixLock.Name = "_colMatrixLock";
        this._colMatrixLock.ReadOnly = true;
        this._colMatrixOpen.DataPropertyName = "OpenNow";
        this._colMatrixOpen.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMatrixOpen.FillWeight = 80F;
        this._colMatrixOpen.HeaderText = "Aperte ora";
        this._colMatrixOpen.Name = "_colMatrixOpen";
        this._colMatrixOpen.ReadOnly = true;
        //
        // _templateTab
        //
        this._templateTab.Controls.Add(this._templateGrid);
        this._templateTab.Location = new System.Drawing.Point(4, 24);
        this._templateTab.Name = "_templateTab";
        this._templateTab.Padding = new System.Windows.Forms.Padding(3);
        this._templateTab.Size = new System.Drawing.Size(1092, 340);
        this._templateTab.TabIndex = 2;
        this._templateTab.Text = "Template e chi li ha presi";
        this._templateTab.UseVisualStyleBackColor = true;
        //
        // _templateGrid
        //
        this._templateGrid.AllowUserToAddRows = false;
        this._templateGrid.AllowUserToDeleteRows = false;
        this._templateGrid.AutoGenerateColumns = false;
        this._templateGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._templateGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._templateGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._templateGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colTemplateCreated,
            this._colTemplateStrategy,
            this._colTemplateSymbol,
            this._colTemplateQuantity,
            this._colTemplateGroups,
            this._colTemplateAccounts,
            this._colTemplateState});
        this._templateGrid.DataSource = this._templateSource;
        this._templateGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._templateGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._templateGrid.Location = new System.Drawing.Point(3, 3);
        this._templateGrid.MultiSelect = false;
        this._templateGrid.Name = "_templateGrid";
        this._templateGrid.ReadOnly = true;
        this._templateGrid.RowHeadersVisible = false;
        this._templateGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._templateGrid.Size = new System.Drawing.Size(1086, 334);
        this._templateGrid.TabIndex = 0;
        //
        // colonne di _templateGrid
        //
        this._colTemplateCreated.DataPropertyName = "CreatedAtUtc";
        this._colTemplateCreated.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        this._colTemplateCreated.FillWeight = 110F;
        this._colTemplateCreated.HeaderText = "Creato (UTC)";
        this._colTemplateCreated.Name = "_colTemplateCreated";
        this._colTemplateCreated.ReadOnly = true;
        this._colTemplateStrategy.DataPropertyName = "StrategyCode";
        this._colTemplateStrategy.FillWeight = 100F;
        this._colTemplateStrategy.HeaderText = "Strategia";
        this._colTemplateStrategy.Name = "_colTemplateStrategy";
        this._colTemplateStrategy.ReadOnly = true;
        this._colTemplateSymbol.DataPropertyName = "Symbol";
        this._colTemplateSymbol.FillWeight = 60F;
        this._colTemplateSymbol.HeaderText = "Simbolo";
        this._colTemplateSymbol.Name = "_colTemplateSymbol";
        this._colTemplateSymbol.ReadOnly = true;
        this._colTemplateQuantity.DataPropertyName = "Quantity";
        this._colTemplateQuantity.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colTemplateQuantity.FillWeight = 55F;
        this._colTemplateQuantity.HeaderText = "Qtà";
        this._colTemplateQuantity.Name = "_colTemplateQuantity";
        this._colTemplateQuantity.ReadOnly = true;
        this._colTemplateGroups.DataPropertyName = "ClaimedByGroups";
        this._colTemplateGroups.FillWeight = 110F;
        this._colTemplateGroups.HeaderText = "Gruppi che l'hanno consumato";
        this._colTemplateGroups.Name = "_colTemplateGroups";
        this._colTemplateGroups.ReadOnly = true;
        this._colTemplateAccounts.DataPropertyName = "ClaimedByAccounts";
        this._colTemplateAccounts.FillWeight = 110F;
        this._colTemplateAccounts.HeaderText = "Account assegnatari";
        this._colTemplateAccounts.Name = "_colTemplateAccounts";
        this._colTemplateAccounts.ReadOnly = true;
        this._colTemplateState.DataPropertyName = "State";
        this._colTemplateState.FillWeight = 90F;
        this._colTemplateState.HeaderText = "Stato";
        this._colTemplateState.Name = "_colTemplateState";
        this._colTemplateState.ReadOnly = true;
        //
        // _reasonTab
        //
        this._reasonTab.Controls.Add(this._reasonGrid);
        this._reasonTab.Location = new System.Drawing.Point(4, 24);
        this._reasonTab.Name = "_reasonTab";
        this._reasonTab.Padding = new System.Windows.Forms.Padding(3);
        this._reasonTab.Size = new System.Drawing.Size(1092, 340);
        this._reasonTab.TabIndex = 3;
        this._reasonTab.Text = "Cause di scarto";
        this._reasonTab.UseVisualStyleBackColor = true;
        //
        // _reasonGrid
        //
        this._reasonGrid.AllowUserToAddRows = false;
        this._reasonGrid.AllowUserToDeleteRows = false;
        this._reasonGrid.AutoGenerateColumns = false;
        this._reasonGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._reasonGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._reasonGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._reasonGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colReason,
            this._colReasonMeaning,
            this._colReasonCount,
            this._colReasonShare});
        this._reasonGrid.DataSource = this._reasonSource;
        this._reasonGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._reasonGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._reasonGrid.Location = new System.Drawing.Point(3, 3);
        this._reasonGrid.MultiSelect = false;
        this._reasonGrid.Name = "_reasonGrid";
        this._reasonGrid.ReadOnly = true;
        this._reasonGrid.RowHeadersVisible = false;
        this._reasonGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._reasonGrid.Size = new System.Drawing.Size(1086, 334);
        this._reasonGrid.TabIndex = 0;
        //
        // colonne di _reasonGrid
        //
        this._colReason.DataPropertyName = "Reason";
        this._colReason.FillWeight = 100F;
        this._colReason.HeaderText = "Esito";
        this._colReason.Name = "_colReason";
        this._colReason.ReadOnly = true;
        this._colReasonMeaning.DataPropertyName = "Meaning";
        this._colReasonMeaning.FillWeight = 300F;
        this._colReasonMeaning.HeaderText = "Significato";
        this._colReasonMeaning.Name = "_colReasonMeaning";
        this._colReasonMeaning.ReadOnly = true;
        this._colReasonCount.DataPropertyName = "Count";
        this._colReasonCount.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colReasonCount.FillWeight = 60F;
        this._colReasonCount.HeaderText = "Conteggio";
        this._colReasonCount.Name = "_colReasonCount";
        this._colReasonCount.ReadOnly = true;
        this._colReasonShare.DataPropertyName = "Share";
        this._colReasonShare.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colReasonShare.FillWeight = 60F;
        this._colReasonShare.HeaderText = "Quota";
        this._colReasonShare.Name = "_colReasonShare";
        this._colReasonShare.ReadOnly = true;
        //
        // _runPanel
        //
        this._runPanel.Controls.Add(this._prepareButton);
        this._runPanel.Controls.Add(this._stepButton);
        this._runPanel.Controls.Add(this._runButton);
        this._runPanel.Controls.Add(this._stopButton);
        this._runPanel.Controls.Add(this._resetButton);
        this._runPanel.Controls.Add(this._progressLabel);
        this._runPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._runPanel.Location = new System.Drawing.Point(0, 288);
        this._runPanel.Name = "_runPanel";
        this._runPanel.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this._runPanel.Size = new System.Drawing.Size(1100, 44);
        this._runPanel.TabIndex = 3;
        this._runPanel.WrapContents = false;
        //
        // _prepareButton
        //
        this._prepareButton.AutoSize = true;
        this._prepareButton.Name = "_prepareButton";
        this._prepareButton.Size = new System.Drawing.Size(140, 27);
        this._prepareButton.TabIndex = 0;
        this._prepareButton.Text = "Prepara sessione";
        this._prepareButton.UseVisualStyleBackColor = true;
        this._prepareButton.Click += new System.EventHandler(this.OnPrepareRequested);
        //
        // _stepButton
        //
        this._stepButton.AutoSize = true;
        this._stepButton.Name = "_stepButton";
        this._stepButton.Size = new System.Drawing.Size(110, 27);
        this._stepButton.TabIndex = 1;
        this._stepButton.Text = "Avanza 1 barra";
        this._stepButton.UseVisualStyleBackColor = true;
        this._stepButton.Click += new System.EventHandler(this.OnStepRequested);
        //
        // _runButton
        //
        this._runButton.AutoSize = true;
        this._runButton.Name = "_runButton";
        this._runButton.Size = new System.Drawing.Size(100, 27);
        this._runButton.TabIndex = 2;
        this._runButton.Text = "Esegui tutto";
        this._runButton.UseVisualStyleBackColor = true;
        this._runButton.Click += new System.EventHandler(this.OnRunRequested);
        //
        // _stopButton
        //
        this._stopButton.AutoSize = true;
        this._stopButton.Name = "_stopButton";
        this._stopButton.Size = new System.Drawing.Size(80, 27);
        this._stopButton.TabIndex = 3;
        this._stopButton.Text = "Ferma";
        this._stopButton.UseVisualStyleBackColor = true;
        this._stopButton.Click += new System.EventHandler(this.OnStopRequested);
        //
        // _resetButton
        //
        this._resetButton.AutoSize = true;
        this._resetButton.Name = "_resetButton";
        this._resetButton.Size = new System.Drawing.Size(80, 27);
        this._resetButton.TabIndex = 4;
        this._resetButton.Text = "Azzera";
        this._resetButton.UseVisualStyleBackColor = true;
        this._resetButton.Click += new System.EventHandler(this.OnResetRequested);
        //
        // _progressLabel
        //
        this._progressLabel.AutoSize = true;
        this._progressLabel.Margin = new System.Windows.Forms.Padding(16, 9, 3, 0);
        this._progressLabel.Name = "_progressLabel";
        this._progressLabel.Size = new System.Drawing.Size(160, 15);
        this._progressLabel.TabIndex = 5;
        this._progressLabel.Text = "Nessuna barra caricata";
        //
        // _groupsPanel
        //
        this._groupsPanel.Controls.Add(this._groupsGrid);
        this._groupsPanel.Controls.Add(this._groupsCommands);
        this._groupsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._groupsPanel.Location = new System.Drawing.Point(0, 108);
        this._groupsPanel.Name = "_groupsPanel";
        this._groupsPanel.Size = new System.Drawing.Size(1100, 180);
        this._groupsPanel.TabIndex = 2;
        //
        // _groupsGrid
        //
        this._groupsGrid.AllowUserToAddRows = false;
        this._groupsGrid.AllowUserToDeleteRows = false;
        this._groupsGrid.AutoGenerateColumns = false;
        this._groupsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._groupsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._groupsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._groupsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colGroupId,
            this._colAccountNumber,
            this._colMaxConcurrent});
        this._groupsGrid.DataSource = this._groupsSource;
        this._groupsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._groupsGrid.Location = new System.Drawing.Point(0, 36);
        this._groupsGrid.MultiSelect = false;
        this._groupsGrid.Name = "_groupsGrid";
        this._groupsGrid.RowHeadersVisible = false;
        this._groupsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._groupsGrid.Size = new System.Drawing.Size(1100, 144);
        this._groupsGrid.TabIndex = 1;
        //
        // colonne di _groupsGrid
        //
        this._colGroupId.DataPropertyName = "GroupId";
        this._colGroupId.FillWeight = 80F;
        this._colGroupId.HeaderText = "Gruppo";
        this._colGroupId.Name = "_colGroupId";
        this._colAccountNumber.DataPropertyName = "AccountNumber";
        this._colAccountNumber.FillWeight = 90F;
        this._colAccountNumber.HeaderText = "Account";
        this._colAccountNumber.Name = "_colAccountNumber";
        this._colMaxConcurrent.DataPropertyName = "MaxConcurrentTrades";
        this._colMaxConcurrent.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this._colMaxConcurrent.FillWeight = 80F;
        this._colMaxConcurrent.HeaderText = "Max concorrenti (0 = illimitato)";
        this._colMaxConcurrent.Name = "_colMaxConcurrent";
        //
        // _groupsCommands
        //
        this._groupsCommands.Controls.Add(this._groupsTitle);
        this._groupsCommands.Controls.Add(this._applyGroupsButton);
        this._groupsCommands.Controls.Add(this._addRowButton);
        this._groupsCommands.Controls.Add(this._removeRowButton);
        this._groupsCommands.Dock = System.Windows.Forms.DockStyle.Top;
        this._groupsCommands.Location = new System.Drawing.Point(0, 0);
        this._groupsCommands.Name = "_groupsCommands";
        this._groupsCommands.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        this._groupsCommands.Size = new System.Drawing.Size(1100, 36);
        this._groupsCommands.TabIndex = 0;
        this._groupsCommands.WrapContents = false;
        //
        // _groupsTitle
        //
        this._groupsTitle.AutoSize = true;
        this._groupsTitle.Margin = new System.Windows.Forms.Padding(3, 8, 12, 0);
        this._groupsTitle.Name = "_groupsTitle";
        this._groupsTitle.Size = new System.Drawing.Size(260, 15);
        this._groupsTitle.TabIndex = 0;
        this._groupsTitle.Text = "Gruppi e account (dal piano, modificabili)";
        //
        // _applyGroupsButton
        //
        this._applyGroupsButton.AutoSize = true;
        this._applyGroupsButton.Name = "_applyGroupsButton";
        this._applyGroupsButton.Size = new System.Drawing.Size(150, 25);
        this._applyGroupsButton.TabIndex = 1;
        this._applyGroupsButton.Text = "Applica alla sessione";
        this._applyGroupsButton.UseVisualStyleBackColor = true;
        this._applyGroupsButton.Click += new System.EventHandler(this.OnApplyGroupsRequested);
        //
        // _addRowButton
        //
        this._addRowButton.AutoSize = true;
        this._addRowButton.Name = "_addRowButton";
        this._addRowButton.Size = new System.Drawing.Size(100, 25);
        this._addRowButton.TabIndex = 2;
        this._addRowButton.Text = "Aggiungi riga";
        this._addRowButton.UseVisualStyleBackColor = true;
        this._addRowButton.Click += new System.EventHandler(this.OnAddGroupRow);
        //
        // _removeRowButton
        //
        this._removeRowButton.AutoSize = true;
        this._removeRowButton.Name = "_removeRowButton";
        this._removeRowButton.Size = new System.Drawing.Size(100, 25);
        this._removeRowButton.TabIndex = 3;
        this._removeRowButton.Text = "Rimuovi riga";
        this._removeRowButton.UseVisualStyleBackColor = true;
        this._removeRowButton.Click += new System.EventHandler(this.OnRemoveGroupRow);
        //
        // _configPanel
        //
        this._configPanel.Controls.Add(this._workspaceLabel);
        this._configPanel.Controls.Add(this._workspaceValueLabel);
        this._configPanel.Controls.Add(this._planLabel);
        this._configPanel.Controls.Add(this._planCombo);
        this._configPanel.Controls.Add(this._runModeLabel);
        this._configPanel.Controls.Add(this._runModeCombo);
        this._configPanel.Controls.Add(this._daysLabel);
        this._configPanel.Controls.Add(this._daysInput);
        this._configPanel.Controls.Add(this._closeAfterLabel);
        this._configPanel.Controls.Add(this._closeAfterInput);
        this._configPanel.Controls.Add(this._limitsLabel);
        this._configPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._configPanel.Location = new System.Drawing.Point(0, 44);
        this._configPanel.Name = "_configPanel";
        this._configPanel.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this._configPanel.Size = new System.Drawing.Size(1100, 64);
        this._configPanel.TabIndex = 1;
        //
        // etichette e campi di _configPanel
        //
        this._workspaceLabel.AutoSize = true;
        this._workspaceLabel.Margin = new System.Windows.Forms.Padding(3, 9, 3, 0);
        this._workspaceLabel.Name = "_workspaceLabel";
        this._workspaceLabel.Size = new System.Drawing.Size(70, 15);
        this._workspaceLabel.TabIndex = 0;
        this._workspaceLabel.Text = "Workspace";
        this._workspaceValueLabel.AutoSize = true;
        this._workspaceValueLabel.Margin = new System.Windows.Forms.Padding(3, 9, 3, 0);
        this._workspaceValueLabel.Name = "_workspaceValueLabel";
        this._workspaceValueLabel.Size = new System.Drawing.Size(220, 15);
        this._workspaceValueLabel.TabIndex = 1;
        this._planLabel.AutoSize = true;
        this._planLabel.Margin = new System.Windows.Forms.Padding(12, 9, 3, 0);
        this._planLabel.Name = "_planLabel";
        this._planLabel.Size = new System.Drawing.Size(35, 15);
        this._planLabel.TabIndex = 2;
        this._planLabel.Text = "Piano";
        this._planCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._planCombo.Name = "_planCombo";
        this._planCombo.Size = new System.Drawing.Size(260, 23);
        this._planCombo.TabIndex = 3;
        this._planCombo.SelectedIndexChanged += new System.EventHandler(this.OnPlanChanged);
        this._runModeLabel.AutoSize = true;
        this._runModeLabel.Margin = new System.Windows.Forms.Padding(12, 9, 3, 0);
        this._runModeLabel.Name = "_runModeLabel";
        this._runModeLabel.Size = new System.Drawing.Size(55, 15);
        this._runModeLabel.TabIndex = 4;
        this._runModeLabel.Text = "Modalità";
        this._runModeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._runModeCombo.Name = "_runModeCombo";
        this._runModeCombo.Size = new System.Drawing.Size(110, 23);
        this._runModeCombo.TabIndex = 5;
        this._runModeCombo.SelectedIndexChanged += new System.EventHandler(this.OnRunModeChanged);
        this._daysLabel.AutoSize = true;
        this._daysLabel.Margin = new System.Windows.Forms.Padding(12, 9, 3, 0);
        this._daysLabel.Name = "_daysLabel";
        this._daysLabel.Size = new System.Drawing.Size(100, 15);
        this._daysLabel.TabIndex = 6;
        this._daysLabel.Text = "Sessioni di dati";
        this._daysInput.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
        this._daysInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._daysInput.Name = "_daysInput";
        this._daysInput.Size = new System.Drawing.Size(60, 23);
        this._daysInput.TabIndex = 7;
        this._daysInput.Value = new decimal(new int[] { 5, 0, 0, 0 });
        this._closeAfterLabel.AutoSize = true;
        this._closeAfterLabel.Margin = new System.Windows.Forms.Padding(12, 9, 3, 0);
        this._closeAfterLabel.Name = "_closeAfterLabel";
        this._closeAfterLabel.Size = new System.Drawing.Size(130, 15);
        this._closeAfterLabel.TabIndex = 8;
        this._closeAfterLabel.Text = "Chiudi dopo N barre";
        this._closeAfterInput.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
        this._closeAfterInput.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        this._closeAfterInput.Name = "_closeAfterInput";
        this._closeAfterInput.Size = new System.Drawing.Size(60, 23);
        this._closeAfterInput.TabIndex = 9;
        this._closeAfterInput.Value = new decimal(new int[] { 3, 0, 0, 0 });
        this._limitsLabel.AutoSize = true;
        this._limitsLabel.Margin = new System.Windows.Forms.Padding(18, 9, 3, 0);
        this._limitsLabel.Name = "_limitsLabel";
        this._limitsLabel.Size = new System.Drawing.Size(200, 15);
        this._limitsLabel.TabIndex = 10;
        this._limitsLabel.Text = "Limiti: —";
        //
        // _toolbar
        //
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(1100, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Verifica concorrenza";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        //
        // ConcurrencyHarnessScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._tabs);
        this.Controls.Add(this._runPanel);
        this.Controls.Add(this._groupsPanel);
        this.Controls.Add(this._configPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "ConcurrencyHarnessScreen";
        this.Size = new System.Drawing.Size(1100, 700);
        ((System.ComponentModel.ISupportInitialize)(this._daysInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._closeAfterInput)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._pollGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._matrixGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._templateGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._reasonGrid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._groupsSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._pollSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._matrixSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._templateSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._reasonSource)).EndInit();
        this._configPanel.ResumeLayout(false);
        this._configPanel.PerformLayout();
        this._groupsPanel.ResumeLayout(false);
        this._groupsCommands.ResumeLayout(false);
        this._groupsCommands.PerformLayout();
        this._runPanel.ResumeLayout(false);
        this._runPanel.PerformLayout();
        this._tabs.ResumeLayout(false);
        this._pollTab.ResumeLayout(false);
        this._matrixTab.ResumeLayout(false);
        this._templateTab.ResumeLayout(false);
        this._reasonTab.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
    private System.Windows.Forms.FlowLayoutPanel _configPanel;
    private System.Windows.Forms.Label _workspaceLabel;
    private System.Windows.Forms.Label _workspaceValueLabel;
    private System.Windows.Forms.Label _planLabel;
    private System.Windows.Forms.ComboBox _planCombo;
    private System.Windows.Forms.Label _runModeLabel;
    private System.Windows.Forms.ComboBox _runModeCombo;
    private System.Windows.Forms.Label _daysLabel;
    private System.Windows.Forms.NumericUpDown _daysInput;
    private System.Windows.Forms.Label _closeAfterLabel;
    private System.Windows.Forms.NumericUpDown _closeAfterInput;
    private System.Windows.Forms.Label _limitsLabel;
    private System.Windows.Forms.Panel _groupsPanel;
    private System.Windows.Forms.FlowLayoutPanel _groupsCommands;
    private System.Windows.Forms.Label _groupsTitle;
    private System.Windows.Forms.Button _applyGroupsButton;
    private System.Windows.Forms.Button _addRowButton;
    private System.Windows.Forms.Button _removeRowButton;
    private System.Windows.Forms.DataGridView _groupsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGroupId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAccountNumber;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMaxConcurrent;
    private System.Windows.Forms.FlowLayoutPanel _runPanel;
    private System.Windows.Forms.Button _prepareButton;
    private System.Windows.Forms.Button _stepButton;
    private System.Windows.Forms.Button _runButton;
    private System.Windows.Forms.Button _stopButton;
    private System.Windows.Forms.Button _resetButton;
    private System.Windows.Forms.Label _progressLabel;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _pollTab;
    private System.Windows.Forms.DataGridView _pollGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollBar;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollBarSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollAccount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollGroup;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollOutcome;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollStrategy;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollQuantity;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollOpen;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollPending;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollMax;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPollNote;
    private System.Windows.Forms.TabPage _matrixTab;
    private System.Windows.Forms.DataGridView _matrixGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixAccount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixGroup;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixMax;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixPolls;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixEntries;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixCloses;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixLimit;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixLock;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colMatrixOpen;
    private System.Windows.Forms.TabPage _templateTab;
    private System.Windows.Forms.DataGridView _templateGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateCreated;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateStrategy;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateSymbol;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateQuantity;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateGroups;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateAccounts;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTemplateState;
    private System.Windows.Forms.TabPage _reasonTab;
    private System.Windows.Forms.DataGridView _reasonGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReason;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReasonMeaning;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReasonCount;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colReasonShare;
    private System.Windows.Forms.BindingSource _groupsSource;
    private System.Windows.Forms.BindingSource _pollSource;
    private System.Windows.Forms.BindingSource _matrixSource;
    private System.Windows.Forms.BindingSource _templateSource;
    private System.Windows.Forms.BindingSource _reasonSource;
}
