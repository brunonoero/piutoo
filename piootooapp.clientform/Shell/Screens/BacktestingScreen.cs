using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Avvio e sorveglianza di un backtest. Le strategie non si scelgono qui: vengono dal
/// masterfilter del workspace, che è l'unica fonte autorevole di cosa gira.
/// </summary>
public partial class BacktestingScreen : UserControl, IShellScreen
{
    private ShellContext? _context;
    private CancellationTokenSource? _pollingCts;
    private string? _lastJobId;
    private bool _isRunning;
    private string? _reportTempHtmlPath;

    /// <summary>
    /// Gli Id del masterfilter del workspace, come erano al caricamento della schermata: servono
    /// solo a contare quante strategie ogni piano lascia accese. Null quando il server non l'ha
    /// dato.
    /// </summary>
    private IReadOnlyCollection<string>? _masterFilterIds;

    public BacktestingScreen()
    {
        InitializeComponent();
        // Il capitale proposto è quello di riferimento delle strategie: dichiarano un contratto per
        // un milione, e il backtest interno non scala quella size. Resta modificabile — cambiarlo
        // sposta solo il denominatore di equity e drawdown, non le quantità.
        _capitalInput.Value = TradingConventions.StrategyReferenceBalance;
        _startPicker.Value = DateTime.UtcNow.Date.AddDays(-30);
        _endPicker.Value = DateTime.UtcNow.Date;
        _nameTextBox.Text = $"backtest-{DateTime.UtcNow:yyyyMMdd-HHmm}";
    }

    // Non è più una voce di menu: ci si arriva solo da "Nuovo backtest" nella lista, e la
    // breadcrumb deve dirlo invece di ripetere "Backtesting › Backtesting".
    public string ScreenTitle => "Nuovo backtest";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _isRunning)
        {
            return;
        }

        try
        {
            // Il workspace non si sceglie qui: il run gira su quello selezionato in alto, e la
            // schermata lo dichiara perché è la cartella in cui finiranno gli artefatti.
            _workspaceValueLabel.Text = _context.Services.Workspaces.CurrentDisplay;

            await LoadDatafeedSourcesAsync(cancellationToken);
            await LoadPlansAsync(cancellationToken);

            _context.Navigation.SetStatus(SelectedWorkspaceId is { } workspaceId
                ? $"Nuovo backtest nel workspace '{workspaceId}'."
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
    }

    private string? SelectedWorkspaceId => _context?.Services.Workspaces.CurrentId;

    /// <summary>Broker selezionato, null quando la scelta è il datafeed interno.</summary>
    private string? SelectedDatafeedBroker => (_datafeedCombo.SelectedItem as DatafeedComboItem)?.Broker;

    /// <summary>Piano selezionato, null quando la scelta è «nessun piano».</summary>
    private TradingPlan? SelectedPlan => (_planCombo.SelectedItem as PlanComboItem)?.Plan;

    /// <summary>
    /// Riempie la combo dei piani. La prima voce è «nessun piano», che è il run neutro di sempre:
    /// tutte le strategie del masterfilter, e i parametri di questa schermata così come sono.
    ///
    /// <para>Scegliere un piano non cambia le size — il backtest interno resta neutro sul capitale,
    /// e il <c>SizeMultiplier</c> del piano non entra — ma governa tutto il resto: l'<b>universo</b>
    /// (girano solo le strategie sui simboli che la tabella del suo broker prevede), le strategie
    /// che il piano tiene spente, la policy di tenuta e la commissione. È il server ad applicarle,
    /// da <c>PlanCode</c>: qui si mostrano soltanto, così il run e il live dello stesso piano
    /// restano confrontabili per costruzione.</para>
    /// </summary>
    private async Task LoadPlansAsync(CancellationToken cancellationToken)
    {
        if (_context == null) return;

        var previous = SelectedPlan?.Code;
        _planCombo.Items.Clear();
        _planCombo.Items.Add(PlanComboItem.None());

        try
        {
            if (SelectedWorkspaceId is { } workspaceId)
            {
                // Il masterfilter serve a dire quante strategie ogni piano lascia accese: il piano
                // elenca le spente, e da solo quel numero non dice quanto opera. Se non arriva si
                // prosegue senza il conteggio invece di bloccare la scelta del piano.
                try
                {
                    _masterFilterIds = (await _context.Services.Api.GetMasterFilterAsync(workspaceId, cancellationToken))
                        .StrategiesFilter;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _masterFilterIds = null;
                    Log($"Masterfilter non disponibile, piani senza conteggio strategie: {ex.Message}");
                }

                foreach (var plan in await _context.Services.Plans.ListAsync(workspaceId, cancellationToken))
                {
                    if (!string.IsNullOrWhiteSpace(plan.Code))
                        _planCombo.Items.Add(PlanComboItem.Of(plan, CountActiveStrategies(plan, _masterFilterIds)));
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Come per i datasource esterni: senza elenco si resta sul run neutro invece di
            // bloccare la schermata.
            Log($"Elenco piani non disponibile: {ex.Message}");
        }

        _planCombo.SelectedIndex = 0;
        if (previous is not null)
        {
            for (var index = 0; index < _planCombo.Items.Count; index++)
            {
                if (_planCombo.Items[index] is PlanComboItem item
                    && string.Equals(item.Plan?.Code, previous, StringComparison.OrdinalIgnoreCase))
                {
                    _planCombo.SelectedIndex = index;
                    break;
                }
            }
        }

        ApplySelectedPlanToParameters();
    }

    /// <summary>
    /// Quante strategie del masterfilter il piano lascia accese: <c>masterfilter −
    /// DisabledStrategies</c>, confrontate per <b>Id</b> di catalogo, che è come il piano le nomina
    /// (CLAUDE.md, «Id ≠ Name»). Null senza masterfilter.
    ///
    /// <para>Non tiene conto della tabella del broker, che vive sul server: qui si dice quante il
    /// piano <i>accende</i>, non quante il broker potrà operare. La differenza, quando c'è, la
    /// dichiara il summary del run.</para>
    /// </summary>
    private static int? CountActiveStrategies(TradingPlan plan, IReadOnlyCollection<string>? masterFilter)
    {
        if (masterFilter is null) return null;

        var disabled = plan.DisabledStrategies.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return masterFilter.Count(id => !disabled.Contains(id));
    }

    /// <summary>
    /// Mostra nei campi ciò che il piano imporrà al run, e li blocca: tenuta e commissione le
    /// decide lui, e lasciarli modificabili significherebbe far compilare all'operatore dei valori
    /// che il server sovrascrive comunque.
    ///
    /// <para><b>Il datasource resta libero</b>: il broker del piano dice con che tabella si opera,
    /// non da quale archivio di barre si legge. Lo stesso piano misurato sul feed interno e su
    /// quello del suo broker è il confronto che dice quanto vale lo spread — quindi la combo si
    /// posiziona sull'archivio del broker, se c'è, ma non si blocca.</para>
    /// </summary>
    private void ApplySelectedPlanToParameters()
    {
        var plan = SelectedPlan;

        _commissionInput.Enabled = plan is null;
        _weekEndCheckBox.Enabled = plan is null;

        if (plan is null)
        {
            _planDerivedLabel.Text =
                "Nessun piano: il run gira sull'intero masterfilter con i parametri qui sopra.";
            return;
        }

        // Clamp e non assegnazione diretta: il campo ha un massimo, e una commissione fuori scala
        // farebbe eccezione nella schermata invece di mostrarsi come il valore assurdo che e'. Il
        // run usa comunque quella del piano — la applica il server — e la riga qui sotto la dice.
        _commissionInput.Value = Math.Clamp(
            plan.CommissionPerContract, _commissionInput.Minimum, _commissionInput.Maximum);
        _weekEndCheckBox.Checked = !plan.Holding.AllowOverweek;

        if (!string.IsNullOrWhiteSpace(plan.BrokerCode))
        {
            var index = FindDatafeedIndex(plan.BrokerCode);
            if (index >= 0) _datafeedCombo.SelectedIndex = index;
        }

        var active = CountActiveStrategies(plan, _masterFilterIds);
        _planDerivedLabel.Text =
            $"Dal piano: universo del broker {(string.IsNullOrWhiteSpace(plan.BrokerCode) ? "—" : plan.BrokerCode)}"
            + (active is { } count ? $"  ·  {count} strategie attive" : string.Empty)
            + $"  ·  overnight {(plan.Holding.AllowOvernight ? "permesso" : $"piatto {plan.Holding.SessionFlatUtcHhmm:0000}Z")}"
            + $"  ·  fine settimana {(plan.Holding.AllowOverweek ? "permesso" : $"piatto {plan.Holding.WeekEnd.FromUtcHhmm:0000}Z")}"
            + $"  ·  commissione {plan.CommissionPerContract:0.##}/contratto";
    }

    private void OnPlanChanged(object? sender, EventArgs e) => ApplySelectedPlanToParameters();

    /// <summary>
    /// Riempie la combo del datasource: prima l'interno, poi un broker per cartella di
    /// <c>datafeed-external</c>. L'elenco arriva dal server come qualsiasi altro dato: la console
    /// non guarda dentro il repository.
    ///
    /// <para>Un server senza archivi esterni lascia la sola voce interna, che resta selezionata: è
    /// il comportamento di prima che questa scelta esistesse, e non deve diventare un errore.</para>
    /// </summary>
    private async Task LoadDatafeedSourcesAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        var previousBroker = SelectedDatafeedBroker;
        _datafeedCombo.Items.Clear();
        _datafeedCombo.Items.Add(DatafeedComboItem.Internal());

        try
        {
            foreach (var broker in await _context.Services.Datafeed.ListBrokersAsync(cancellationToken))
            {
                _datafeedCombo.Items.Add(DatafeedComboItem.External(broker));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // L'elenco dei broker non è indispensabile per far girare un backtest interno: si
            // segnala e si prosegue con la sola voce interna, invece di bloccare la schermata.
            Log($"Elenco datasource esterni non disponibile: {ex.Message}");
        }

        // Il broker già scelto va ritrovato, non azzerato da un semplice ricaricamento della
        // schermata; se è sparito dal server si mostra come mancante invece di scivolare
        // sull'interno senza dirlo.
        if (!string.IsNullOrEmpty(previousBroker))
        {
            var index = FindDatafeedIndex(previousBroker);
            if (index < 0)
            {
                index = _datafeedCombo.Items.Add(DatafeedComboItem.Missing(previousBroker));
            }

            _datafeedCombo.SelectedIndex = index;
            return;
        }

        _datafeedCombo.SelectedIndex = 0;
    }

    private int FindDatafeedIndex(string broker)
    {
        for (var index = 0; index < _datafeedCombo.Items.Count; index++)
        {
            if (_datafeedCombo.Items[index] is DatafeedComboItem item
                && string.Equals(item.Broker, broker, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void Log(string message)
        => _logTextBox.AppendText($"[{DateTime.UtcNow:HH:mm:ss}Z] {message}{Environment.NewLine}");

    private void SetRunningState(bool running)
    {
        _isRunning = running;
        _runButton.Enabled = !running;
        _cancelButton.Enabled = running;
        _parametersGroup.Enabled = !running;
    }

    private async void OnRunClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (_context.Services.Workspaces.Current is not { } workspace)
        {
            MessageBox.Show(this, "Seleziona un workspace nella barra in alto.", "Backtesting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var backtestName = _nameTextBox.Text.Trim();
        if (backtestName.Length == 0)
        {
            MessageBox.Show(this, "Il nome del backtest è obbligatorio.", "Backtesting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_endPicker.Value <= _startPicker.Value)
        {
            MessageBox.Show(this, "La data di fine deve essere successiva a quella di inizio.", "Backtesting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var existing = await _context.Services.Api.ListBacktestsAsync(workspace.Id);
            var overwrite = existing.Any(item =>
                item.FolderName.Equals(backtestName, StringComparison.OrdinalIgnoreCase));
            if (overwrite && MessageBox.Show(
                    this,
                    $"Esiste già un backtest chiamato '{backtestName}'. Sostituirlo?",
                    "Backtest esistente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            // Il masterfilter vuoto è la causa più frequente di un run muto: meglio fermarsi prima.
            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspace.Id);
            if (masterFilter.StrategiesFilter.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Il masterfilter del workspace è vuoto: non c'è nessuna strategia da valutare. " +
                    "Aprilo da Anagrafiche → Workspace e seleziona le strategie.",
                    "Backtesting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var request = new BacktestingRequest
            {
                WorkspaceId = workspace.Id,
                BacktestFolderName = backtestName,
                OverwriteExistingBacktest = overwrite,
                Name = backtestName,
                // I picker mostrano date senza fuso: il server accetta solo UTC ed è così che
                // vanno interpretate, non convertite dall'ora locale della postazione.
                StartDate = DateTime.SpecifyKind(_startPicker.Value, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(_endPicker.Value, DateTimeKind.Utc),
                InitialCapital = _capitalInput.Value,
                CommissionPerContract = _commissionInput.Value,
                // Null = datafeed interno. Il server rifiuta un broker che non esiste invece di
                // ripiegare sull'interno: un run letto dal feed sbagliato non si distinguerebbe.
                DatafeedBroker = SelectedDatafeedBroker,
                // Null = nessun piano, run sull'intero masterfilter. Con un piano il server ne
                // applica universo, strategie spente, tenuta e commissione, e lo dichiara nel
                // summary: due run con piani diversi non sono confrontabili.
                PlanCode = SelectedPlan?.Code,
                // Senza piano la spunta e' la stessa regola di prima, letta dal verso opposto:
                // chiudere a fine settimana significa non concedere l'overweek. Parte **spenta**:
                // il run interno non impone alcun flat di conto, cosi' l'equity e' quella delle
                // strategie e non quella del venerdi'. Chi vuole misurare il vincolo di una prop lo
                // accende, o sceglie il piano che lo dichiara.
                //
                // Con un piano questo campo non decide nulla: il server lo sovrascrive con la
                // policy del piano, che e' la stessa che scende in sessione e nel cBot. Si manda
                // comunque quella del piano perche' il log di avvio del client dica il vero.
                Holding = SelectedPlan?.Holding ?? (AccountHoldingPolicy.Default with
                {
                    AllowOvernight = true,
                    AllowOverweek = !_weekEndCheckBox.Checked
                })
            };

            SetRunningState(true);
            _reportButton.Enabled = false;
            HideReportTab();
            _progressBar.Value = 0;
            _pollingCts?.Dispose();
            _pollingCts = new CancellationTokenSource();

            Log($"Workspace {workspace.Name} ({workspace.Id})");
            Log($"Datasource: {(request.DatafeedBroker is null ? "interno" : $"esterno / {request.DatafeedBroker}")}");
            Log($"Piano: {request.PlanCode ?? "nessuno (intero masterfilter)"}" +
                (SelectedPlan is { } selectedPlan
                    ? $" · broker {(string.IsNullOrWhiteSpace(selectedPlan.BrokerCode) ? "-" : selectedPlan.BrokerCode)}" +
                      $" · {CountActiveStrategies(selectedPlan, _masterFilterIds)?.ToString() ?? "?"} strategie attive"
                    : string.Empty));
            Log($"Finestra UTC {request.StartDate:yyyy-MM-dd HH:mm}Z → {request.EndDate:yyyy-MM-dd HH:mm}Z");
            Log($"Strategie dal masterfilter: {masterFilter.StrategiesFilter.Count}");
            Log($"Vincoli di conto: overnight {(request.Holding.AllowOvernight ? "permesso" : "vietato")}, " +
                $"fine settimana {(request.Holding.AllowOverweek ? "permesso" : "piatto")}");

            _lastJobId = await _context.Services.Api.StartBacktestingAsync(request);
            Log($"Job {_lastJobId} avviato.");
            await PollAsync(_lastJobId, _pollingCts.Token);
        }
        catch (OperationCanceledException)
        {
            Log("Sorveglianza del job interrotta in locale. Il server può stare ancora lavorando.");
        }
        catch (Exception ex)
        {
            Log($"Errore: {ex.Message}");
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Errore backtesting", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _pollingCts?.Dispose();
            _pollingCts = null;
            SetRunningState(false);
        }
    }

    private async Task PollAsync(string jobId, CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        var job = await _context.Services.Api.PollBacktestingUntilTerminalAsync(
            jobId,
            progress =>
            {
                if (IsDisposed)
                {
                    return;
                }

                _progressBar.Value = Math.Clamp(progress.ProgressPercent, 0, 100);
                _statusLabel.Text = $"{progress.Phase} · {_progressBar.Value}% · {progress.ProgressMessage}";
            },
            timeout: null,
            cancellationToken);

        switch (job.Status)
        {
            case BacktestingJobStatus.Completed:
                var result = await _context.Services.Api.GetBacktestingResultAsync(jobId, cancellationToken);
                Log($"Completato: {result.TotalTrades} trade, equity finale {result.FinalEquity:N2}, " +
                    $"drawdown massimo {result.MaxDrawdown:N2}, win rate {result.WinRate:P1}.");
                _statusLabel.Text = "Completato.";
                _reportButton.Enabled = true;
                _context.Navigation.SetStatus($"Backtest '{job.JobId}' completato.");
                await TryShowReportTabAsync(jobId, cancellationToken);
                break;
            case BacktestingJobStatus.Cancelled:
                Log("Annullato dal server.");
                _statusLabel.Text = "Annullato.";
                break;
            default:
                Log($"Fallito: {job.ErrorMessage}");
                _statusLabel.Text = "Fallito.";
                _context.Navigation.SetError(job.ErrorMessage ?? "Backtest fallito.");
                break;
        }
    }

    private async void OnCancelClick(object? sender, EventArgs e)
    {
        if (_context == null || _lastJobId == null)
        {
            return;
        }

        try
        {
            await _context.Services.Api.CancelBacktestingAsync(_lastJobId);
            Log("Richiesta di annullamento inviata al server.");
        }
        catch (Exception ex)
        {
            Log($"Annullamento non riuscito: {ex.Message}");
        }
    }

    private async void OnReportClick(object? sender, EventArgs e)
    {
        if (_context == null || _lastJobId == null)
        {
            return;
        }

        try
        {
            var uri = _context.Services.Api.GetBacktestingReportUri(_lastJobId);
            await HtmlReportViewerForm.ShowFromUriAsync(
                FindForm()!, _context.Services.Http, uri, "Report di backtest");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClearLogClick(object? sender, EventArgs e) => _logTextBox.Clear();

    // Il tab "Report" compare solo se il report del job è stato davvero prodotto: niente tab
    // vuoto o con un errore dentro. Il pulsante "Apri report" resta comunque come fallback
    // (finestra separata, utile anche per "Apri nel browser").
    private async Task TryShowReportTabAsync(string jobId, CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        try
        {
            var uri = _context.Services.Api.GetBacktestingReportUri(jobId);
            var html = await _context.Services.Http.GetStringAsync(uri, cancellationToken);

            var previousPath = _reportTempHtmlPath;
            var temporaryHtmlPath = Path.Combine(Path.GetTempPath(), $"piootoo-report-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(temporaryHtmlPath, html, cancellationToken);
            _reportTempHtmlPath = temporaryHtmlPath;
            DeleteTempReportFile(previousPath);

            await _reportBrowser.EnsureCoreWebView2Async();
            _reportBrowser.Source = new Uri(temporaryHtmlPath);

            if (!_tabs.TabPages.Contains(_reportTab))
            {
                _tabs.TabPages.Add(_reportTab);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Report non disponibile (404, WebView2 non inizializzabile, ecc.): il tab resta
            // assente invece di mostrare un errore incorporato.
            Log($"Report non incorporato nel tab: {ex.Message}");
            HideReportTab();
        }
    }

    private void HideReportTab()
    {
        if (_tabs.TabPages.Contains(_reportTab))
        {
            _tabs.TabPages.Remove(_reportTab);
        }
    }

    private static void DeleteTempReportFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
