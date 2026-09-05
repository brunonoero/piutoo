using System.Text.Json;

using Piootoo.Shared.Models.Optimization;

using Piootoo.Shared.Models.Trading;

using Piootoo.Shared.Models.Workspaces;

using piootooapp.clientform.Shell.Controls;



namespace piootooapp.clientform.Shell.Screens;



/// <summary>

/// Creazione e gestione di sessioni di trading (stato, avvio/stop, snapshot) — non il backtest

/// batch, che è la schermata Backtesting separata (<c>PiootooBacktestingService</c>, un motore

/// diverso che non passa da qui). "Sessione diretta" crea una sessione con stato via

/// <c>POST /trading-sessions</c> (ServerSimulated per test/FeedWorker, o ExternalBroker manuale);

/// "Apri da piano" segue lo stesso percorso <c>open-plan</c> usato dai cBot cTrader — le strategie

/// valutate arrivano sempre dal masterfilter del workspace, il piano non limita l'universo ma

/// decide gruppi e concorrenza per account.

/// </summary>

public partial class TradingSessionsScreen : UserControl, IShellScreen

{

    private const string CreationManual = "Sessione diretta (senza piano)";

    private const string CreationFromPlan = "Apri da piano";



    private ShellContext? _context;

    private TradingSessionDescriptor? _activeSession;

    private TradingSessionSummary? _preloadedSummary;

    private IReadOnlyList<WorkspaceAccount> _accounts = [];




    private List<TradingPlan> _plans = [];



    /// <summary>

    /// Backtest del workspace corrente. Non stanno in una combo: le cartelle sono troppe perché un

    /// menu a tendina sia usabile, la scelta passa da <see cref="BacktestPickerDialog"/>.

    /// </summary>





    private bool _suspendReload;

    private bool _isBusy;



    public TradingSessionsScreen()

    {

        InitializeComponent();

        _screenExplainerLabel.Text =

            "Sessioni con stato (avvio/stop/snapshot) — non il backtest batch, che è nella schermata " +

            "Backtesting. \"Sessione diretta\" crea qui una sessione ServerSimulated o ExternalBroker " +

            "senza piano; \"Apri da piano\" usa lo stesso percorso open-plan dei cBot cTrader.";

        _creationSourceCombo.Items.AddRange([CreationManual, CreationFromPlan]);

        _creationSourceCombo.SelectedIndex = 0;

        _clientRunModeCombo.Items.AddRange(Enum.GetNames<ClientRunMode>());

        _clientRunModeCombo.SelectedItem = nameof(ClientRunMode.Unknown);

        _modeCombo.Items.AddRange(Enum.GetNames<ExecutionMode>());

        _modeCombo.SelectedItem = nameof(ExecutionMode.ServerSimulated);

        ConfigureGroupsGrid();

        ShellGridHelper.ConfigureReadableGrids(this);

        ConfigureTooltips();

        UpdateCreationModeControls();

        UpdateSessionControls();

    }



    public string ScreenTitle => _preloadedSummary is { } summary

        ? $"Sessione {ShortId(summary.SessionId)}"

        : "Nuova sessione di trading";



    private static string ShortId(string id) => id.Length <= 8 ? id : id[..8];



    public void Initialize(ShellContext context) => _context = context;



    /// <summary>

    /// Va chiamato prima di aggiungere il controllo allo shell: apre direttamente in gestione,

    /// saltando la UI di creazione. È così che una sessione aperta da un cBot diventa gestibile da

    /// console — il token del summary è la stessa cosa che il cBot userebbe.

    /// </summary>

    /// <summary>Preseleziona "Apri da piano". Va chiamato prima di Push, sostituendo la creazione
    /// manuale di default: la lista lo usa per il pulsante primario, allineato al percorso del cBot.</summary>
    public void SelectOpenFromPlan() => _creationSourceCombo.SelectedIndex = 1;

    public void SetSession(TradingSessionSummary summary)

    {

        _preloadedSummary = summary;

        _activeSession = new TradingSessionDescriptor

        {

            SessionId = summary.SessionId,

            SessionToken = summary.SessionToken,

            WorkspaceId = summary.WorkspaceId,

            PlanCode = summary.PlanCode,

            ExecutionKey = summary.ExecutionKey,

            ExecutionMode = summary.ExecutionMode,

            Status = summary.Status,

            ClientRunMode = summary.ClientRunMode

        };

        _configGroup.Visible = false;

        _createButton.Visible = false;

        UpdateSessionControls();

    }



    public async Task LoadAsync(CancellationToken cancellationToken)

    {

        if (_context == null || _isBusy)

        {

            return;

        }



        SetBusy(true);

        try

        {

            _accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);



            RefreshGroupColumnSources();



            if (_preloadedSummary is not null && _activeSession is not null)

            {

                var conti = await _context.Services.Sessions.GetAccountsAsync(
                    _activeSession.SessionId, _activeSession.SessionToken, cancellationToken);

                _groupsGrid.Rows.Clear();

                foreach (var conto in conti)
                {
                    _groupsGrid.Rows.Add(conto);

                }



                var snapshot = await _context.Services.Sessions.GetSnapshotAsync(

                    _activeSession.SessionId, _activeSession.SessionToken, cancellationToken);

                ShowSnapshot(snapshot);

                _context.Navigation.SetStatus(

                    $"Sessione {_activeSession.SessionId} · {_activeSession.WorkspaceId} · {_activeSession.Status}.");

                return;

            }



            RefreshOpenPlanAccountCombo(null);



            // Il workspace è quello scelto nella barra in alto: qui si legge soltanto, e tutto
            // il resto della schermata (piani, backtest, masterfilter) viene da lì.
            _workspaceValueLabel.Text = _context.Services.Workspaces.CurrentDisplay;


            await ReloadPlansAsync(cancellationToken);


            await RefreshMasterfilterInfoAsync(cancellationToken);

            _context.Navigation.SetStatus(SelectedWorkspaceId is { } currentWorkspaceId
                ? $"Sessioni nel workspace '{currentWorkspaceId}'."
                : "Nessun workspace selezionato: scegline uno nella barra in alto.");


        }

        catch (OperationCanceledException)

        {

            throw;

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

        }

        finally

        {

            _suspendReload = false;

            SetBusy(false);

        }

    }



    private sealed class AccountNumberListItem

    {

        public AccountNumberListItem(WorkspaceAccount account)

        {

            AccountNumber = account.AccountNumber;

            DisplayText = $"{account.Name} · {account.AccountNumber}";

        }



        public AccountNumberListItem(string accountNumber, string displayText)

        {

            AccountNumber = accountNumber;

            DisplayText = displayText;

        }



        public string AccountNumber { get; }

        public string DisplayText { get; }

    }



    private sealed class PlanComboItem

    {

        public PlanComboItem(TradingPlan plan) => Plan = plan;



        public TradingPlan Plan { get; }



        public override string ToString() => $"{Plan.Code} — {Plan.Name}";

    }



    private bool IsFromPlan => _creationSourceCombo.SelectedItem?.ToString() == CreationFromPlan;



    // Da piano vince il workspace del piano: la sessione nasce dal suo snapshot, non dal contesto
    // in cui la si sta aprendo.
    private string? SelectedWorkspaceId => IsFromPlan
        ? SelectedPlan?.WorkspaceId
        : _context?.Services.Workspaces.CurrentId;




    private TradingPlan? SelectedPlan => (_planCombo.SelectedItem as PlanComboItem)?.Plan;






    private ClientRunMode SelectedClientRunMode =>

        Enum.TryParse<ClientRunMode>(_clientRunModeCombo.SelectedItem?.ToString(), out var mode)

            ? mode

            : ClientRunMode.Unknown;







    private void ConfigureTooltips()

    {

        _screenToolTip.SetToolTip(_creationSourceCombo,

            "Manuale: configura workspace e gruppi come la console legacy. " +

            "Da piano: il server estrae workspace e snapshot dal piano (come il cBot).");

        _screenToolTip.SetToolTip(_clientRunModeCombo,

            "Contesto dichiarato dal client: Backtest, Realtime, oppure Unknown per lasciare la " +

            "responsabilità a chi configura.");

        _screenToolTip.SetToolTip(_distributeCheckBox,

            "Attivo = PiootooLiveTradingBot reclama i segnali per account. Disattivo = esecuzione diretta " +

            "come PiootooTradingSessionBot (intent già assegnati su POST /bars).");

        _screenToolTip.SetToolTip(_groupsGrid,

            "Solo ExternalBroker: MaxConcurrentTrades per gruppo/account (claim multi-account).");

    }



    /// <summary>
    /// La griglia dei conti della sessione. Una colonna sola: il gruppo non esiste piu' e il tetto
    /// di concorrenza sta sul piano, non per riga.
    /// </summary>
    private void ConfigureGroupsGrid()
    {
        _groupsGrid.AutoGenerateColumns = false;
        _groupsGrid.AllowUserToAddRows = true;
        _groupsGrid.AllowUserToDeleteRows = true;
        _groupsGrid.RowHeadersVisible = false;
        _groupsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "AccountNumber",
            HeaderText = "Conto cTrader",
            FillWeight = 30,
            DisplayMember = nameof(AccountNumberListItem.DisplayText),
            ValueMember = nameof(AccountNumberListItem.AccountNumber),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });

        _groupsGrid.DataError += (_, e) => e.ThrowException = false;
        _groupsGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_groupsGrid.IsCurrentCellDirty)
            {
                _groupsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
    }



    private void RefreshGroupColumnSources()
    {
        if (_groupsGrid.Columns["AccountNumber"] is DataGridViewComboBoxColumn accountColumn)
        {
            accountColumn.DataSource = _accounts
                .Where(account => !string.IsNullOrWhiteSpace(account.AccountNumber))
                .Select(account => new AccountNumberListItem(
                    account.AccountNumber,
                    $"{account.Name}  ·  {account.AccountNumber}"))
                .ToList();
        }
    }



    private void RefreshOpenPlanAccountCombo(TradingPlan? plan)
    {
        var previous = (_openPlanAccountCombo.SelectedItem as AccountNumberListItem)?.AccountNumber;
        _openPlanAccountCombo.Items.Clear();

        foreach (var account in (plan?.Accounts ?? []).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            _openPlanAccountCombo.Items.Add(new AccountNumberListItem(account, account));
        }

        if (_openPlanAccountCombo.Items.Count == 0)
        {
            _openPlanAccountCombo.SelectedIndex = -1;
            return;
        }

        var restored = -1;
        for (var index = 0; index < _openPlanAccountCombo.Items.Count; index++)
        {
            if (_openPlanAccountCombo.Items[index] is AccountNumberListItem item &&
                string.Equals(item.AccountNumber, previous, StringComparison.OrdinalIgnoreCase))
            {
                restored = index;
                break;
            }
        }

        _openPlanAccountCombo.SelectedIndex = restored >= 0 ? restored : 0;
    }







    private void SetBusy(bool busy)

    {

        if (IsDisposed)

        {

            return;

        }



        _isBusy = busy;

        _createButton.Enabled = !busy;

        _startButton.Enabled = !busy && _activeSession != null;

        _stopButton.Enabled = !busy && _activeSession != null;

        _resumeButton.Enabled = !busy && _activeSession != null;

        _snapshotButton.Enabled = !busy && _activeSession != null;

        _saveGroupsButton.Enabled = !busy && _activeSession != null;

        _reloadGroupsButton.Enabled = !busy && _activeSession != null;

        _addGroupRowButton.Enabled = !busy;

        _removeGroupRowButton.Enabled = !busy;

        _configGroup.Enabled = !busy && _activeSession == null;

        Cursor = busy ? Cursors.AppStarting : Cursors.Default;

    }



    private void UpdateSessionControls()

    {

        if (_activeSession == null)

        {

            _sessionIdLabel.Text = "—";

            _sessionStatusLabel.Text = "—";

            _sessionTokenLabel.Text = "—";

        }

        else

        {

            _sessionIdLabel.Text = _activeSession.SessionId;

            _sessionStatusLabel.Text = _activeSession.Status.ToString();

            var token = _activeSession.SessionToken;

            _sessionTokenLabel.Text = token.Length <= 12

                ? token

                : $"{token[..8]}…{token[^4..]}";

        }



        SetBusy(_isBusy);

    }



    private void UpdateCreationModeControls()

    {

        var fromPlan = IsFromPlan;

        _planCombo.Enabled = fromPlan;

        _workspaceLabel.Visible = !fromPlan;

        _workspaceValueLabel.Visible = !fromPlan;


        _derivedWorkspaceLabel.Visible = fromPlan;

        // Da piano questi campi non servono a nulla: OpenFromPlan non li legge mai, li ricava dal
        // piano (ExecutionMode è sempre ExternalBroker, il sizing viene dalla riga gruppo
        // primaria). Tenerli visibili-ma-disabilitati farebbe pensare che descrivano la sessione in
        // apertura, mentre non vengono nemmeno inviati: si nascondono, non solo si disabilitano.
        _modeLabel.Visible = !fromPlan;

        _modeCombo.Visible = !fromPlan;

        _modeCombo.SelectedItem ??= nameof(ExecutionMode.ServerSimulated);

        _portfolioRiskEnabledCheckBox.Visible = !fromPlan;



        _executionKeyLabel.Visible = fromPlan;

        _executionKeyTextBox.Visible = fromPlan;

        _openPlanAccountLabel.Visible = fromPlan;

        _openPlanAccountCombo.Visible = fromPlan;

        _distributeCheckBox.Visible = fromPlan;



        _createButton.Text = fromPlan ? "Apri sessione da piano" : "Crea sessione";

        _groupsGrid.ReadOnly = fromPlan;

        _addGroupRowButton.Enabled = !fromPlan && !_isBusy;

        _removeGroupRowButton.Enabled = !fromPlan && !_isBusy;



        if (fromPlan)

        {

            _clientRunModeCombo.Items.Clear();

            _clientRunModeCombo.Items.AddRange(

                [nameof(ClientRunMode.Backtest), nameof(ClientRunMode.Realtime)]);

            if (_clientRunModeCombo.SelectedItem?.ToString() is not (nameof(ClientRunMode.Backtest) or nameof(ClientRunMode.Realtime)))

            {

                _clientRunModeCombo.SelectedItem = nameof(ClientRunMode.Backtest);

            }

        }

        else

        {

            var selected = _clientRunModeCombo.SelectedItem?.ToString();

            _clientRunModeCombo.Items.Clear();

            _clientRunModeCombo.Items.AddRange(Enum.GetNames<ClientRunMode>());

            _clientRunModeCombo.SelectedItem = selected is { Length: > 0 } value && _clientRunModeCombo.Items.Contains(value)

                ? value

                : nameof(ClientRunMode.Unknown);

        }



        UpdateScenarioHint();

    }



    private void UpdateScenarioHint()

    {

        if (IsFromPlan)

        {

            var plan = SelectedPlan;

            _scenarioHintLabel.Text = plan is null

                ? "Seleziona un piano: il workspace e i gruppi verranno estratti dal piano."

                : DescribeScenario(fromPlan: true);

            return;

        }



        _scenarioHintLabel.Text = DescribeScenario(fromPlan: false);

        _scenarioHintLabel.ForeColor = SystemColors.GrayText;

    }



    private static string DescribeScenario(bool fromPlan)

    {

        var prefix = fromPlan

            ? "Apertura da piano: strategie dal masterfilter del workspace del piano. "

            : "Creazione manuale: strategie dal masterfilter del workspace selezionato. ";

        return prefix +

               "Vengono valutate tutte le strategie del masterfilter. " +

               "In ExternalBroker si applicano MaxConcurrentTrades e ordine di claim per account/gruppo; " +

               "in backtest il limite è disattivato di default.";

    }



    private async Task ReloadPlansAsync(CancellationToken cancellationToken)

    {

        if (_context == null || SelectedWorkspaceId is not { } workspaceId)

        {

            _plans = [];

            _planCombo.Items.Clear();

            return;

        }



        _plans = await _context.Services.Plans.ListAsync(workspaceId, cancellationToken);

        var previousCode = SelectedPlan?.Code;

        _planCombo.Items.Clear();

        foreach (var plan in _plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase))

        {

            _planCombo.Items.Add(new PlanComboItem(plan));

        }



        if (_planCombo.Items.Count == 0)

        {

            _derivedWorkspaceLabel.Text = "—";

            return;

        }



        var restored = _plans.FindIndex(plan =>

            plan.Code.Equals(previousCode, StringComparison.OrdinalIgnoreCase));

        _planCombo.SelectedIndex = restored >= 0 ? restored : 0;

    }



    private async Task RefreshMasterfilterInfoAsync(CancellationToken cancellationToken)

    {

        if (_context == null || SelectedWorkspaceId is not { } workspaceId)

        {

            _masterfilterInfoLabel.Text = "—";

            return;

        }



        try

        {

            var filter = await _context.Services.Api.GetMasterFilterAsync(workspaceId, cancellationToken);

            _masterfilterInfoLabel.Text = filter.StrategiesFilter.Count == 0

                ? "Masterfilter vuoto: nessuna strategia da valutare."

                : $"Masterfilter: {filter.StrategiesFilter.Count} strategie (unica fonte di cosa gira in sessione).";

        }

        catch (Exception ex)

        {

            _masterfilterInfoLabel.Text = $"Masterfilter non leggibile: {ex.Message}";

        }

    }



    private void FillGroupsFromPlan(TradingPlan plan)
    {
        _groupsGrid.Rows.Clear();
        foreach (var account in plan.Accounts.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            _groupsGrid.Rows.Add(account);
        }
    }



    private void OnCreationSourceChanged(object? sender, EventArgs e)

    {

        UpdateCreationModeControls();

        if (IsFromPlan && SelectedPlan is not null)

        {

            ApplySelectedPlan();

        }

    }



    private async void OnPlanChanged(object? sender, EventArgs e)

    {

        if (_context == null || _suspendReload)

        {

            return;

        }



        ApplySelectedPlan();

        try

        {

            SetBusy(true);

            await RefreshMasterfilterInfoAsync(CancellationToken.None);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private void ApplySelectedPlan()

    {

        var plan = SelectedPlan;

        if (plan is null)

        {

            _derivedWorkspaceLabel.Text = "—";

            RefreshOpenPlanAccountCombo(null);

            _groupsGrid.Rows.Clear();

            UpdateScenarioHint();

            return;

        }



        _derivedWorkspaceLabel.Text = $"Workspace del piano: {plan.WorkspaceId}";

        RefreshOpenPlanAccountCombo(plan);

        FillGroupsFromPlan(plan);

        UpdateScenarioHint();

    }







    private void OnExecutionModeChanged(object? sender, EventArgs e)

    {

        var external = _modeCombo.SelectedItem?.ToString() == nameof(ExecutionMode.ExternalBroker);

        _mainTabControl.SelectedTab = external ? _groupsTab : _snapshotTab;

        UpdateScenarioHint();

    }



    private void OnRunModeChanged(object? sender, EventArgs e) => UpdateScenarioHint();



    private async void OnCreateClick(object? sender, EventArgs e)

    {

        if (_context == null)

        {

            return;

        }



        if (IsFromPlan)

        {

            await OpenFromPlanAsync();

            return;

        }



        await CreateManualSessionAsync();

    }



    private async Task CreateManualSessionAsync()

    {

        if (_context == null)

        {

            return;

        }



        if (_context.Services.Workspaces.Current is not { } workspace)
        {
            MessageBox.Show(this, "Seleziona un workspace nella barra in alto.", "Sessioni di trading",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }






        var executionMode = Enum.Parse<ExecutionMode>(

            _modeCombo.SelectedItem?.ToString() ?? nameof(ExecutionMode.ServerSimulated));

        var clientRunMode = SelectedClientRunMode;



        try

        {

            SetBusy(true);

            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspace.Id);


            if (masterFilter.StrategiesFilter.Count == 0)

            {

                MessageBox.Show(this,

                    "Il masterfilter del workspace è vuoto: non c'è nessuna strategia da valutare.",

                    "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;

            }



            var request = new CreateTradingSessionRequest

            {

                WorkspaceId = workspace.Id,


                ExecutionMode = executionMode,

                ClientRunMode = clientRunMode,

                EnforceConcurrencyLimits = clientRunMode == ClientRunMode.Unknown

                    ? null

                    : clientRunMode != ClientRunMode.Backtest,

                PositionSizing = new PositionSizingConfig

                {

                    PortfolioRisk = new PortfolioRiskSizingConfig

                    {

                        Enabled = _portfolioRiskEnabledCheckBox.Checked,

                        EnableAggressiveModules = false,

                        MaximumMultiplier = 1m

                    }

                }

            };



            _activeSession = await _context.Services.Sessions.CreateAsync(request);



            if (executionMode == ExecutionMode.ExternalBroker)

            {

                var conti = ReadSessionAccounts(allowEmpty: true);

                if (conti.Count > 0)

                {

                    await _context.Services.Sessions.SetAccountsAsync(
                        _activeSession.SessionId, _activeSession.SessionToken, conti);

                }

            }



            UpdateSessionControls();

            ShowDescriptor("Sessione creata.");

            _context.Navigation.SetStatus($"Sessione {_activeSession.SessionId} creata.");

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Errore sessione", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private async Task OpenFromPlanAsync()

    {

        if (_context == null)

        {

            return;

        }



        var plan = SelectedPlan;

        if (plan is null)

        {

            MessageBox.Show(this, "Seleziona un piano di trading.", "Sessioni di trading",

                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        var clientRunMode = SelectedClientRunMode;

        if (clientRunMode is ClientRunMode.Unknown)

        {

            MessageBox.Show(this,

                "Per aprire da piano devi dichiarare Backtest oppure Realtime (come il cBot).",

                "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        var executionKey = _executionKeyTextBox.Text.Trim();

        if (executionKey.Length == 0)

        {

            MessageBox.Show(this, "Execution key obbligatoria.", "Sessioni di trading",

                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        try

        {

            SetBusy(true);

            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(plan.WorkspaceId);

            if (masterFilter.StrategiesFilter.Count == 0)

            {

                MessageBox.Show(this,

                    "Il masterfilter del workspace del piano è vuoto.",

                    "Sessioni di trading", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;

            }



            var account = (_openPlanAccountCombo.SelectedItem as AccountNumberListItem)?.AccountNumber;

            var request = new OpenTradingPlanSessionRequest

            {

                PlanCode = plan.Code,

                ClientRunMode = clientRunMode,

                ExecutionKey = executionKey,

                AccountNumber = account,

                DistributeToAccounts = _distributeCheckBox.Checked

            };



            _activeSession = await _context.Services.Sessions.OpenFromPlanAsync(request);

            UpdateSessionControls();

            ShowDescriptor("Sessione aperta da piano.");

            _context.Navigation.SetStatus(

                $"Sessione {_activeSession.SessionId} · workspace {plan.WorkspaceId} · " +

                $"contesto {_activeSession.ClientRunMode}.");

            _mainTabControl.SelectedTab = _snapshotTab;

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Errore apertura piano", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private async void OnStartClick(object? sender, EventArgs e) => await SetStatusAsync("start");



    private async void OnStopClick(object? sender, EventArgs e) => await SetStatusAsync("stop");



    private async void OnResumeClick(object? sender, EventArgs e) => await SetStatusAsync("resume");



    private async Task SetStatusAsync(string action)

    {

        if (_context == null || _activeSession is null)

        {

            return;

        }



        try

        {

            SetBusy(true);

            _activeSession = await _context.Services.Sessions.SetStatusAsync(

                _activeSession.SessionId, _activeSession.SessionToken, action);

            UpdateSessionControls();

            ShowDescriptor($"Stato aggiornato: {_activeSession.Status}.");

            _context.Navigation.SetStatus($"Sessione {_activeSession.Status}.");

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Sessione", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private async void OnSnapshotClick(object? sender, EventArgs e)

    {

        if (_context == null || _activeSession is null)

        {

            return;

        }



        try

        {

            SetBusy(true);

            var snapshot = await _context.Services.Sessions.GetSnapshotAsync(

                _activeSession.SessionId, _activeSession.SessionToken);

            ShowSnapshot(snapshot);

            _context.Navigation.SetStatus("Snapshot aggiornato.");

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Snapshot", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private void ShowDescriptor(string message)

    {

        if (_context == null || _activeSession is null)

        {

            _snapshotTextBox.Text = message;

            return;

        }



        _snapshotTextBox.Text = $"{message}{Environment.NewLine}{Environment.NewLine}" +

            JsonSerializer.Serialize(_activeSession, new JsonSerializerOptions(_context.Services.JsonOptions)

            {

                WriteIndented = true

            });

    }



    private void ShowSnapshot(TradingSessionSnapshot snapshot)

    {

        _balanceLabel.Text = snapshot.Balance.ToString("N2");

        _equityLabel.Text = snapshot.Equity.ToString("N2");

        _positionsLabel.Text = snapshot.Positions.Count.ToString();

        _pendingLabel.Text = snapshot.PendingIntents.Count.ToString();



        if (_context == null)

        {

            return;

        }



        _snapshotTextBox.Text = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(_context.Services.JsonOptions)

        {

            WriteIndented = true

        });

        _mainTabControl.SelectedTab = _snapshotTab;

    }



    // _groupsGrid è editabile e non legato a una collezione: le righe si aggiungono qui. È
    // l'eccezione alla regola «ogni griglia è ordinabile» — l'ordine è quello in cui si sta
    // scrivendo. Vedi .cursor/rules/piutoo-console-screens.mdc.
    private void OnAddGroupRowClick(object? sender, EventArgs e) => _groupsGrid.Rows.Add();



    private void OnRemoveGroupRowClick(object? sender, EventArgs e)

    {

        foreach (DataGridViewRow row in _groupsGrid.SelectedRows.Cast<DataGridViewRow>().ToList())

        {

            if (!row.IsNewRow)

            {

                _groupsGrid.Rows.Remove(row);

            }

        }

    }



    private async void OnSaveGroupsClick(object? sender, EventArgs e)

    {

        if (_context == null || _activeSession is null)

        {

            MessageBox.Show(this, "Crea prima una sessione ExternalBroker.", "Gruppi account",

                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        if (_activeSession.ExecutionMode != ExecutionMode.ExternalBroker)

        {

            MessageBox.Show(this,

                "I gruppi account si configurano solo in modalità ExternalBroker.",

                "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;

        }



        try

        {

            SetBusy(true);

            var conti = ReadSessionAccounts(allowEmpty: false);

            var snapshot = await _context.Services.Sessions.SetAccountsAsync(
                _activeSession.SessionId, _activeSession.SessionToken, conti);

            ShowSnapshot(snapshot);

            _context.Navigation.SetStatus($"Conti salvati ({conti.Count}).");

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    private async void OnReloadGroupsClick(object? sender, EventArgs e)

    {

        if (_context == null || _activeSession is null)

        {

            return;

        }



        try

        {

            SetBusy(true);

            var conti = await _context.Services.Sessions.GetAccountsAsync(
                _activeSession.SessionId, _activeSession.SessionToken);

            _groupsGrid.Rows.Clear();

            foreach (var conto in conti)
            {
                _groupsGrid.Rows.Add(conto);

            }



            _context.Navigation.SetStatus($"Caricati {conti.Count} conti.");

        }

        catch (Exception ex)

        {

            _context.Navigation.SetError(ex.Message);

            MessageBox.Show(this, ex.Message, "Gruppi account", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        finally

        {

            SetBusy(false);

        }

    }



    /// <summary>
    /// I conti scritti nella griglia. Non c'e' piu' un gruppo da comporre: un conto e' un
    /// destinatario, e quanto puo' tenere aperto lo dice il piano, non questa schermata.
    /// </summary>
    private List<string> ReadSessionAccounts(bool allowEmpty)
    {
        var accounts = _groupsGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => Convert.ToString(row.Cells["AccountNumber"].Value ?? string.Empty)!.Trim())
            .Where(account => account.Length > 0)
            .ToList();

        if (!allowEmpty && accounts.Count == 0)
        {
            throw new InvalidOperationException("Serve almeno un conto.");
        }

        var duplicato = accounts.GroupBy(account => account, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(gruppo => gruppo.Count() > 1);
        if (duplicato != null)
        {
            throw new InvalidOperationException($"Il conto '{duplicato.Key}' compare più di una volta.");
        }

        return accounts;
    }





}


