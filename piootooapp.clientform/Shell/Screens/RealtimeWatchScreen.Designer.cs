namespace piootooapp.clientform.Shell.Screens;

partial class RealtimeWatchScreen
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
        this._findingsSource = new System.Windows.Forms.BindingSource(this.components);
        this._sessionsSource = new System.Windows.Forms.BindingSource(this.components);
        this._positionsSource = new System.Windows.Forms.BindingSource(this.components);
        this._pendingSource = new System.Windows.Forms.BindingSource(this.components);
        this._split = new System.Windows.Forms.SplitContainer();
        this._findingsGrid = new System.Windows.Forms.DataGridView();
        this._colGravita = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRilievo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRilievoSessione = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRilievoStrategia = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRilievoSimbolo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colRisulta = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colAzione = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._tabs = new System.Windows.Forms.TabControl();
        this._tabSessioni = new System.Windows.Forms.TabPage();
        this._sessionsGrid = new System.Windows.Forms.DataGridView();
        this._colSesSessione = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesPiano = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesStato = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesUltimaBarra = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesSilenzio = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesHolding = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesStatoBroker = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesRipresa = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesPosizioni = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colSesOrdini = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._tabPosizioni = new System.Windows.Forms.TabPage();
        this._positionsGrid = new System.Windows.Forms.DataGridView();
        this._colPosStrategia = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosSimbolo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosSimboloBroker = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosDirezione = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosQuantita = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosPrezzo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosIngresso = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosStopLoss = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosTakeProfit = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosChiusura = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colPosConfermata = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._tabOrdini = new System.Windows.Forms.TabPage();
        this._pendingGrid = new System.Windows.Forms.DataGridView();
        this._colOrdStrategia = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdSimbolo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdLato = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdStato = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdPrezzo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdQuantita = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdTimeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdCreato = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._colOrdValidoFino = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this._headerPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._accountLabel = new System.Windows.Forms.Label();
        this._accountCombo = new System.Windows.Forms.ComboBox();
        this._severityLabel = new System.Windows.Forms.Label();
        this._disclaimerLabel = new System.Windows.Forms.Label();
        this._toolbar = new piootooapp.clientform.Shell.Controls.EntityToolbar();
        ((System.ComponentModel.ISupportInitialize)(this._findingsSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._sessionsSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._positionsSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._pendingSource)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._findingsGrid)).BeginInit();
        this._tabs.SuspendLayout();
        this._tabSessioni.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._sessionsGrid)).BeginInit();
        this._tabPosizioni.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._positionsGrid)).BeginInit();
        this._tabOrdini.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._pendingGrid)).BeginInit();
        this._headerPanel.SuspendLayout();
        this.SuspendLayout();
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 82);
        this._split.Name = "_split";
        this._split.Orientation = System.Windows.Forms.Orientation.Horizontal;
        this._split.Panel1.Controls.Add(this._findingsGrid);
        this._split.Panel2.Controls.Add(this._tabs);
        this._split.Size = new System.Drawing.Size(1000, 518);
        this._split.SplitterDistance = 240;
        this._split.TabIndex = 2;
        //
        // _findingsGrid
        //
        this._findingsGrid.AllowUserToAddRows = false;
        this._findingsGrid.AllowUserToDeleteRows = false;
        this._findingsGrid.AutoGenerateColumns = false;
        this._findingsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._findingsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._findingsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._findingsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._findingsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colGravita,
            this._colRilievo,
            this._colRilievoSessione,
            this._colRilievoStrategia,
            this._colRilievoSimbolo,
            this._colRisulta,
            this._colAzione});
        this._findingsGrid.DataSource = this._findingsSource;
        this._findingsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._findingsGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._findingsGrid.Location = new System.Drawing.Point(0, 0);
        this._findingsGrid.MultiSelect = false;
        this._findingsGrid.Name = "_findingsGrid";
        this._findingsGrid.ReadOnly = true;
        this._findingsGrid.RowHeadersVisible = false;
        this._findingsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._findingsGrid.Size = new System.Drawing.Size(1000, 240);
        this._findingsGrid.TabIndex = 0;
        this._findingsGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.OnFindingsBindingComplete);
        //
        // _colGravita
        //
        this._colGravita.DataPropertyName = "Gravita";
        this._colGravita.FillWeight = 45F;
        this._colGravita.HeaderText = "Gravità";
        this._colGravita.Name = "_colGravita";
        this._colGravita.ReadOnly = true;
        //
        // _colRilievo
        //
        this._colRilievo.DataPropertyName = "Rilievo";
        this._colRilievo.FillWeight = 85F;
        this._colRilievo.HeaderText = "Rilievo";
        this._colRilievo.Name = "_colRilievo";
        this._colRilievo.ReadOnly = true;
        //
        // _colRilievoSessione
        //
        this._colRilievoSessione.DataPropertyName = "Sessione";
        this._colRilievoSessione.FillWeight = 45F;
        this._colRilievoSessione.HeaderText = "Sessione";
        this._colRilievoSessione.Name = "_colRilievoSessione";
        this._colRilievoSessione.ReadOnly = true;
        //
        // _colRilievoStrategia
        //
        this._colRilievoStrategia.DataPropertyName = "Strategia";
        this._colRilievoStrategia.FillWeight = 70F;
        this._colRilievoStrategia.HeaderText = "Strategia";
        this._colRilievoStrategia.Name = "_colRilievoStrategia";
        this._colRilievoStrategia.ReadOnly = true;
        //
        // _colRilievoSimbolo
        //
        this._colRilievoSimbolo.DataPropertyName = "Simbolo";
        this._colRilievoSimbolo.FillWeight = 40F;
        this._colRilievoSimbolo.HeaderText = "Simbolo";
        this._colRilievoSimbolo.Name = "_colRilievoSimbolo";
        this._colRilievoSimbolo.ReadOnly = true;
        //
        // _colRisulta
        //
        this._colRisulta.DataPropertyName = "Risulta";
        this._colRisulta.FillWeight = 200F;
        this._colRisulta.HeaderText = "Cosa risulta al server";
        this._colRisulta.Name = "_colRisulta";
        this._colRisulta.ReadOnly = true;
        //
        // _colAzione
        //
        this._colAzione.DataPropertyName = "Azione";
        this._colAzione.FillWeight = 160F;
        this._colAzione.HeaderText = "Cosa fare";
        this._colAzione.Name = "_colAzione";
        this._colAzione.ReadOnly = true;
        //
        // _tabs
        //
        this._tabs.Controls.Add(this._tabSessioni);
        this._tabs.Controls.Add(this._tabPosizioni);
        this._tabs.Controls.Add(this._tabOrdini);
        this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tabs.Location = new System.Drawing.Point(0, 0);
        this._tabs.Name = "_tabs";
        this._tabs.SelectedIndex = 0;
        this._tabs.Size = new System.Drawing.Size(1000, 274);
        this._tabs.TabIndex = 0;
        //
        // _tabSessioni
        //
        this._tabSessioni.Controls.Add(this._sessionsGrid);
        this._tabSessioni.Location = new System.Drawing.Point(4, 24);
        this._tabSessioni.Name = "_tabSessioni";
        this._tabSessioni.Padding = new System.Windows.Forms.Padding(3);
        this._tabSessioni.Size = new System.Drawing.Size(992, 246);
        this._tabSessioni.TabIndex = 0;
        this._tabSessioni.Text = "Sessioni";
        this._tabSessioni.UseVisualStyleBackColor = true;
        //
        // _sessionsGrid
        //
        this._sessionsGrid.AllowUserToAddRows = false;
        this._sessionsGrid.AllowUserToDeleteRows = false;
        this._sessionsGrid.AutoGenerateColumns = false;
        this._sessionsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._sessionsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._sessionsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._sessionsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._sessionsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colSesSessione,
            this._colSesPiano,
            this._colSesStato,
            this._colSesUltimaBarra,
            this._colSesSilenzio,
            this._colSesTimeframe,
            this._colSesHolding,
            this._colSesStatoBroker,
            this._colSesRipresa,
            this._colSesPosizioni,
            this._colSesOrdini});
        this._sessionsGrid.DataSource = this._sessionsSource;
        this._sessionsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._sessionsGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._sessionsGrid.Location = new System.Drawing.Point(3, 3);
        this._sessionsGrid.MultiSelect = false;
        this._sessionsGrid.Name = "_sessionsGrid";
        this._sessionsGrid.ReadOnly = true;
        this._sessionsGrid.RowHeadersVisible = false;
        this._sessionsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._sessionsGrid.Size = new System.Drawing.Size(986, 240);
        this._sessionsGrid.TabIndex = 0;
        //
        // _colSesSessione
        //
        this._colSesSessione.DataPropertyName = "Sessione";
        this._colSesSessione.FillWeight = 50F;
        this._colSesSessione.HeaderText = "Sessione";
        this._colSesSessione.Name = "_colSesSessione";
        this._colSesSessione.ReadOnly = true;
        //
        // _colSesPiano
        //
        this._colSesPiano.DataPropertyName = "Piano";
        this._colSesPiano.FillWeight = 60F;
        this._colSesPiano.HeaderText = "Piano";
        this._colSesPiano.Name = "_colSesPiano";
        this._colSesPiano.ReadOnly = true;
        //
        // _colSesStato
        //
        this._colSesStato.DataPropertyName = "Stato";
        this._colSesStato.FillWeight = 50F;
        this._colSesStato.HeaderText = "Stato";
        this._colSesStato.Name = "_colSesStato";
        this._colSesStato.ReadOnly = true;
        //
        // _colSesUltimaBarra
        //
        this._colSesUltimaBarra.DataPropertyName = "UltimaBarraUtc";
        this._colSesUltimaBarra.FillWeight = 80F;
        this._colSesUltimaBarra.HeaderText = "Ultima barra (UTC)";
        this._colSesUltimaBarra.Name = "_colSesUltimaBarra";
        this._colSesUltimaBarra.ReadOnly = true;
        //
        // _colSesSilenzio
        //
        this._colSesSilenzio.DataPropertyName = "MinutiDiSilenzio";
        this._colSesSilenzio.FillWeight = 55F;
        this._colSesSilenzio.HeaderText = "Silenzio (min)";
        this._colSesSilenzio.Name = "_colSesSilenzio";
        this._colSesSilenzio.ReadOnly = true;
        //
        // _colSesTimeframe
        //
        this._colSesTimeframe.DataPropertyName = "TimeframeMinimo";
        this._colSesTimeframe.FillWeight = 50F;
        this._colSesTimeframe.HeaderText = "TF minimo";
        this._colSesTimeframe.Name = "_colSesTimeframe";
        this._colSesTimeframe.ReadOnly = true;
        //
        // _colSesHolding
        //
        this._colSesHolding.DataPropertyName = "Holding";
        this._colSesHolding.FillWeight = 130F;
        this._colSesHolding.HeaderText = "Il conto tiene";
        this._colSesHolding.Name = "_colSesHolding";
        this._colSesHolding.ReadOnly = true;
        //
        // _colSesStatoBroker
        //
        this._colSesStatoBroker.DataPropertyName = "StatoBroker";
        this._colSesStatoBroker.FillWeight = 90F;
        this._colSesStatoBroker.HeaderText = "Stato broker";
        this._colSesStatoBroker.Name = "_colSesStatoBroker";
        this._colSesStatoBroker.ReadOnly = true;
        //
        // _colSesRipresa
        //
        this._colSesRipresa.DataPropertyName = "Ripresa";
        this._colSesRipresa.FillWeight = 75F;
        this._colSesRipresa.HeaderText = "Ripresa";
        this._colSesRipresa.Name = "_colSesRipresa";
        this._colSesRipresa.ReadOnly = true;
        //
        // _colSesPosizioni
        //
        this._colSesPosizioni.DataPropertyName = "Posizioni";
        this._colSesPosizioni.FillWeight = 45F;
        this._colSesPosizioni.HeaderText = "Posizioni";
        this._colSesPosizioni.Name = "_colSesPosizioni";
        this._colSesPosizioni.ReadOnly = true;
        //
        // _colSesOrdini
        //
        this._colSesOrdini.DataPropertyName = "Ordini";
        this._colSesOrdini.FillWeight = 45F;
        this._colSesOrdini.HeaderText = "Ordini";
        this._colSesOrdini.Name = "_colSesOrdini";
        this._colSesOrdini.ReadOnly = true;
        //
        // _tabPosizioni
        //
        this._tabPosizioni.Controls.Add(this._positionsGrid);
        this._tabPosizioni.Location = new System.Drawing.Point(4, 24);
        this._tabPosizioni.Name = "_tabPosizioni";
        this._tabPosizioni.Padding = new System.Windows.Forms.Padding(3);
        this._tabPosizioni.Size = new System.Drawing.Size(992, 246);
        this._tabPosizioni.TabIndex = 1;
        this._tabPosizioni.Text = "Posizioni per il server";
        this._tabPosizioni.UseVisualStyleBackColor = true;
        //
        // _positionsGrid
        //
        this._positionsGrid.AllowUserToAddRows = false;
        this._positionsGrid.AllowUserToDeleteRows = false;
        this._positionsGrid.AutoGenerateColumns = false;
        this._positionsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._positionsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._positionsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._positionsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._positionsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colPosStrategia,
            this._colPosSimbolo,
            this._colPosSimboloBroker,
            this._colPosDirezione,
            this._colPosQuantita,
            this._colPosPrezzo,
            this._colPosIngresso,
            this._colPosStopLoss,
            this._colPosTakeProfit,
            this._colPosChiusura,
            this._colPosConfermata});
        this._positionsGrid.DataSource = this._positionsSource;
        this._positionsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._positionsGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._positionsGrid.Location = new System.Drawing.Point(3, 3);
        this._positionsGrid.MultiSelect = false;
        this._positionsGrid.Name = "_positionsGrid";
        this._positionsGrid.ReadOnly = true;
        this._positionsGrid.RowHeadersVisible = false;
        this._positionsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._positionsGrid.Size = new System.Drawing.Size(986, 240);
        this._positionsGrid.TabIndex = 0;
        //
        // _colPosStrategia
        //
        this._colPosStrategia.DataPropertyName = "Strategia";
        this._colPosStrategia.FillWeight = 90F;
        this._colPosStrategia.HeaderText = "Strategia";
        this._colPosStrategia.Name = "_colPosStrategia";
        this._colPosStrategia.ReadOnly = true;
        //
        // _colPosSimbolo
        //
        this._colPosSimbolo.DataPropertyName = "Simbolo";
        this._colPosSimbolo.FillWeight = 45F;
        this._colPosSimbolo.HeaderText = "Simbolo";
        this._colPosSimbolo.Name = "_colPosSimbolo";
        this._colPosSimbolo.ReadOnly = true;
        //
        // _colPosSimboloBroker
        //
        this._colPosSimboloBroker.DataPropertyName = "SimboloSuCTrader";
        this._colPosSimboloBroker.FillWeight = 60F;
        this._colPosSimboloBroker.HeaderText = "Su cTrader";
        this._colPosSimboloBroker.Name = "_colPosSimboloBroker";
        this._colPosSimboloBroker.ReadOnly = true;
        //
        // _colPosDirezione
        //
        this._colPosDirezione.DataPropertyName = "Direzione";
        this._colPosDirezione.FillWeight = 45F;
        this._colPosDirezione.HeaderText = "Lato";
        this._colPosDirezione.Name = "_colPosDirezione";
        this._colPosDirezione.ReadOnly = true;
        //
        // _colPosQuantita
        //
        this._colPosQuantita.DataPropertyName = "Quantita";
        this._colPosQuantita.FillWeight = 45F;
        this._colPosQuantita.HeaderText = "Qty";
        this._colPosQuantita.Name = "_colPosQuantita";
        this._colPosQuantita.ReadOnly = true;
        //
        // _colPosPrezzo
        //
        this._colPosPrezzo.DataPropertyName = "PrezzoIngresso";
        this._colPosPrezzo.FillWeight = 60F;
        this._colPosPrezzo.HeaderText = "Ingresso";
        this._colPosPrezzo.Name = "_colPosPrezzo";
        this._colPosPrezzo.ReadOnly = true;
        //
        // _colPosIngresso
        //
        this._colPosIngresso.DataPropertyName = "IngressoUtc";
        this._colPosIngresso.FillWeight = 80F;
        this._colPosIngresso.HeaderText = "Aperta il (UTC)";
        this._colPosIngresso.Name = "_colPosIngresso";
        this._colPosIngresso.ReadOnly = true;
        //
        // _colPosStopLoss
        //
        this._colPosStopLoss.DataPropertyName = "StopLoss";
        this._colPosStopLoss.FillWeight = 55F;
        this._colPosStopLoss.HeaderText = "Stop";
        this._colPosStopLoss.Name = "_colPosStopLoss";
        this._colPosStopLoss.ReadOnly = true;
        //
        // _colPosTakeProfit
        //
        this._colPosTakeProfit.DataPropertyName = "TakeProfit";
        this._colPosTakeProfit.FillWeight = 55F;
        this._colPosTakeProfit.HeaderText = "Target";
        this._colPosTakeProfit.Name = "_colPosTakeProfit";
        this._colPosTakeProfit.ReadOnly = true;
        //
        // _colPosChiusura
        //
        this._colPosChiusura.DataPropertyName = "ChiusuraPrevistaUtc";
        this._colPosChiusura.FillWeight = 80F;
        this._colPosChiusura.HeaderText = "Uscita a tempo (UTC)";
        this._colPosChiusura.Name = "_colPosChiusura";
        this._colPosChiusura.ReadOnly = true;
        //
        // _colPosConfermata
        //
        this._colPosConfermata.DataPropertyName = "Confermata";
        this._colPosConfermata.FillWeight = 80F;
        this._colPosConfermata.HeaderText = "Vista dal broker";
        this._colPosConfermata.Name = "_colPosConfermata";
        this._colPosConfermata.ReadOnly = true;
        //
        // _tabOrdini
        //
        this._tabOrdini.Controls.Add(this._pendingGrid);
        this._tabOrdini.Location = new System.Drawing.Point(4, 24);
        this._tabOrdini.Name = "_tabOrdini";
        this._tabOrdini.Padding = new System.Windows.Forms.Padding(3);
        this._tabOrdini.Size = new System.Drawing.Size(992, 246);
        this._tabOrdini.TabIndex = 2;
        this._tabOrdini.Text = "Ordini in volo";
        this._tabOrdini.UseVisualStyleBackColor = true;
        //
        // _pendingGrid
        //
        this._pendingGrid.AllowUserToAddRows = false;
        this._pendingGrid.AllowUserToDeleteRows = false;
        this._pendingGrid.AutoGenerateColumns = false;
        this._pendingGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this._pendingGrid.BackgroundColor = System.Drawing.SystemColors.Window;
        this._pendingGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._pendingGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this._pendingGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colOrdStrategia,
            this._colOrdSimbolo,
            this._colOrdLato,
            this._colOrdStato,
            this._colOrdPrezzo,
            this._colOrdQuantita,
            this._colOrdTimeframe,
            this._colOrdCreato,
            this._colOrdValidoFino});
        this._pendingGrid.DataSource = this._pendingSource;
        this._pendingGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pendingGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        this._pendingGrid.Location = new System.Drawing.Point(3, 3);
        this._pendingGrid.MultiSelect = false;
        this._pendingGrid.Name = "_pendingGrid";
        this._pendingGrid.ReadOnly = true;
        this._pendingGrid.RowHeadersVisible = false;
        this._pendingGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._pendingGrid.Size = new System.Drawing.Size(986, 240);
        this._pendingGrid.TabIndex = 0;
        //
        // _colOrdStrategia
        //
        this._colOrdStrategia.DataPropertyName = "Strategia";
        this._colOrdStrategia.FillWeight = 90F;
        this._colOrdStrategia.HeaderText = "Strategia";
        this._colOrdStrategia.Name = "_colOrdStrategia";
        this._colOrdStrategia.ReadOnly = true;
        //
        // _colOrdSimbolo
        //
        this._colOrdSimbolo.DataPropertyName = "Simbolo";
        this._colOrdSimbolo.FillWeight = 50F;
        this._colOrdSimbolo.HeaderText = "Simbolo";
        this._colOrdSimbolo.Name = "_colOrdSimbolo";
        this._colOrdSimbolo.ReadOnly = true;
        //
        // _colOrdLato
        //
        this._colOrdLato.DataPropertyName = "Lato";
        this._colOrdLato.FillWeight = 45F;
        this._colOrdLato.HeaderText = "Lato";
        this._colOrdLato.Name = "_colOrdLato";
        this._colOrdLato.ReadOnly = true;
        //
        // _colOrdStato
        //
        this._colOrdStato.DataPropertyName = "Stato";
        this._colOrdStato.FillWeight = 55F;
        this._colOrdStato.HeaderText = "Stato";
        this._colOrdStato.Name = "_colOrdStato";
        this._colOrdStato.ReadOnly = true;
        //
        // _colOrdPrezzo
        //
        this._colOrdPrezzo.DataPropertyName = "Prezzo";
        this._colOrdPrezzo.FillWeight = 55F;
        this._colOrdPrezzo.HeaderText = "Livello";
        this._colOrdPrezzo.Name = "_colOrdPrezzo";
        this._colOrdPrezzo.ReadOnly = true;
        //
        // _colOrdQuantita
        //
        this._colOrdQuantita.DataPropertyName = "Quantita";
        this._colOrdQuantita.FillWeight = 45F;
        this._colOrdQuantita.HeaderText = "Qty";
        this._colOrdQuantita.Name = "_colOrdQuantita";
        this._colOrdQuantita.ReadOnly = true;
        //
        // _colOrdTimeframe
        //
        this._colOrdTimeframe.DataPropertyName = "TimeframeMinuti";
        this._colOrdTimeframe.FillWeight = 45F;
        this._colOrdTimeframe.HeaderText = "TF";
        this._colOrdTimeframe.Name = "_colOrdTimeframe";
        this._colOrdTimeframe.ReadOnly = true;
        //
        // _colOrdCreato
        //
        this._colOrdCreato.DataPropertyName = "CreatoUtc";
        this._colOrdCreato.FillWeight = 80F;
        this._colOrdCreato.HeaderText = "Creato (UTC)";
        this._colOrdCreato.Name = "_colOrdCreato";
        this._colOrdCreato.ReadOnly = true;
        //
        // _colOrdValidoFino
        //
        this._colOrdValidoFino.DataPropertyName = "ValidoFinoUtc";
        this._colOrdValidoFino.FillWeight = 80F;
        this._colOrdValidoFino.HeaderText = "Valido fino a (UTC)";
        this._colOrdValidoFino.Name = "_colOrdValidoFino";
        this._colOrdValidoFino.ReadOnly = true;
        //
        // _headerPanel
        //
        this._headerPanel.AutoSize = true;
        this._headerPanel.Controls.Add(this._accountLabel);
        this._headerPanel.Controls.Add(this._accountCombo);
        this._headerPanel.Controls.Add(this._severityLabel);
        this._headerPanel.Controls.Add(this._disclaimerLabel);
        this._headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this._headerPanel.Location = new System.Drawing.Point(0, 44);
        this._headerPanel.Name = "_headerPanel";
        this._headerPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        this._headerPanel.Size = new System.Drawing.Size(1000, 38);
        this._headerPanel.TabIndex = 1;
        this._headerPanel.WrapContents = false;
        //
        // _accountLabel
        //
        this._accountLabel.AutoSize = true;
        this._accountLabel.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
        this._accountLabel.Name = "_accountLabel";
        this._accountLabel.Size = new System.Drawing.Size(50, 15);
        this._accountLabel.TabIndex = 0;
        this._accountLabel.Text = "Conto:";
        //
        // _accountCombo
        //
        this._accountCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._accountCombo.Margin = new System.Windows.Forms.Padding(3, 4, 12, 3);
        this._accountCombo.Name = "_accountCombo";
        this._accountCombo.Size = new System.Drawing.Size(300, 23);
        this._accountCombo.TabIndex = 1;
        this._accountCombo.SelectedIndexChanged += new System.EventHandler(this.OnAccountChanged);
        //
        // _severityLabel
        //
        this._severityLabel.AutoSize = true;
        this._severityLabel.Margin = new System.Windows.Forms.Padding(3, 8, 12, 3);
        this._severityLabel.Name = "_severityLabel";
        this._severityLabel.Size = new System.Drawing.Size(0, 15);
        this._severityLabel.TabIndex = 2;
        //
        // _disclaimerLabel
        //
        this._disclaimerLabel.AutoSize = true;
        this._disclaimerLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this._disclaimerLabel.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
        this._disclaimerLabel.Name = "_disclaimerLabel";
        this._disclaimerLabel.Size = new System.Drawing.Size(0, 15);
        this._disclaimerLabel.TabIndex = 3;
        this._disclaimerLabel.Text = "La console non vede cTrader: qui c'è solo ciò che il server crede.";
        //
        // _toolbar
        //
        this._toolbar.CanCreate = false;
        this._toolbar.CanDelete = false;
        this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this._toolbar.FilterPlaceholder = "Filtra per strategia o simbolo…";
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(1000, 44);
        this._toolbar.TabIndex = 0;
        this._toolbar.Title = "Presidio realtime";
        this._toolbar.RefreshRequested += new System.EventHandler(this.OnRefreshRequested);
        this._toolbar.FilterChanged += new System.EventHandler(this.OnFilterChanged);
        //
        // RealtimeWatchScreen
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this._split);
        this.Controls.Add(this._headerPanel);
        this.Controls.Add(this._toolbar);
        this.Name = "RealtimeWatchScreen";
        this.Size = new System.Drawing.Size(1000, 600);
        ((System.ComponentModel.ISupportInitialize)(this._findingsSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._sessionsSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._positionsSource)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._pendingSource)).EndInit();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._findingsGrid)).EndInit();
        this._tabs.ResumeLayout(false);
        this._tabSessioni.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._sessionsGrid)).EndInit();
        this._tabPosizioni.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._positionsGrid)).EndInit();
        this._tabOrdini.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._pendingGrid)).EndInit();
        this._headerPanel.ResumeLayout(false);
        this._headerPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.BindingSource _findingsSource;
    private System.Windows.Forms.BindingSource _sessionsSource;
    private System.Windows.Forms.BindingSource _positionsSource;
    private System.Windows.Forms.BindingSource _pendingSource;
    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.DataGridView _findingsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colGravita;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRilievo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRilievoSessione;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRilievoStrategia;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRilievoSimbolo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colRisulta;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAzione;
    private System.Windows.Forms.TabControl _tabs;
    private System.Windows.Forms.TabPage _tabSessioni;
    private System.Windows.Forms.DataGridView _sessionsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesSessione;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesPiano;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesStato;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesUltimaBarra;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesSilenzio;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesHolding;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesStatoBroker;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesRipresa;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesPosizioni;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colSesOrdini;
    private System.Windows.Forms.TabPage _tabPosizioni;
    private System.Windows.Forms.DataGridView _positionsGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosStrategia;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosSimbolo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosSimboloBroker;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosDirezione;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosQuantita;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosPrezzo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosIngresso;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosStopLoss;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosTakeProfit;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosChiusura;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colPosConfermata;
    private System.Windows.Forms.TabPage _tabOrdini;
    private System.Windows.Forms.DataGridView _pendingGrid;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdStrategia;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdSimbolo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdLato;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdStato;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdPrezzo;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdQuantita;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdTimeframe;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdCreato;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colOrdValidoFino;
    private System.Windows.Forms.FlowLayoutPanel _headerPanel;
    private System.Windows.Forms.Label _accountLabel;
    private System.Windows.Forms.ComboBox _accountCombo;
    private System.Windows.Forms.Label _severityLabel;
    private System.Windows.Forms.Label _disclaimerLabel;
    private piootooapp.clientform.Shell.Controls.EntityToolbar _toolbar;
}
