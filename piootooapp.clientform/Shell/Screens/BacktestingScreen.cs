using Piootoo.Shared.Models.Backtesting;

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

    public BacktestingScreen()
    {
        InitializeComponent();
        _startPicker.Value = DateTime.UtcNow.Date.AddDays(-30);
        _endPicker.Value = DateTime.UtcNow.Date;
        _nameTextBox.Text = $"backtest-{DateTime.UtcNow:yyyyMMdd-HHmm}";
    }

    public string ScreenTitle => "Backtesting";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _isRunning)
        {
            return;
        }

        try
        {
            var previousWorkspace = SelectedWorkspaceId;
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);
            _workspaceCombo.Items.Clear();
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            _workspaceCombo.SelectedIndex = FindWorkspaceIndex(previousWorkspace) is var index && index >= 0
                ? index
                : _workspaceCombo.Items.Count > 0 ? 0 : -1;

            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
            _accountCombo.Items.Clear();
            _accountCombo.Items.Add(new AccountComboItem(null));
            foreach (var account in accounts)
            {
                _accountCombo.Items.Add(new AccountComboItem(account));
            }

            _accountCombo.SelectedIndex = 0;
            _context.Navigation.SetStatus($"{workspaces.Count} workspace, {accounts.Count} account disponibili.");
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

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private int FindWorkspaceIndex(string? workspaceId)
    {
        if (string.IsNullOrEmpty(workspaceId))
        {
            return -1;
        }

        for (var index = 0; index < _workspaceCombo.Items.Count; index++)
        {
            if (_workspaceCombo.Items[index] is WorkspaceComboItem item
                && string.Equals(item.Info.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
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
        _workspaceCombo.Enabled = !running;
        _accountCombo.Enabled = !running;
        _parametersGroup.Enabled = !running;
    }

    private async void OnRunClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (_workspaceCombo.SelectedItem is not WorkspaceComboItem workspace)
        {
            MessageBox.Show(this, "Seleziona un workspace.", "Backtesting",
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
            var existing = await _context.Services.Api.ListBacktestsAsync(workspace.Info.Id);
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
            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspace.Info.Id);
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
                WorkspaceId = workspace.Info.Id,
                BacktestFolderName = backtestName,
                OverwriteExistingBacktest = overwrite,
                Name = backtestName,
                // I picker mostrano date senza fuso: il server accetta solo UTC ed è così che
                // vanno interpretate, non convertite dall'ora locale della postazione.
                StartDate = DateTime.SpecifyKind(_startPicker.Value, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(_endPicker.Value, DateTimeKind.Utc),
                InitialCapital = _capitalInput.Value,
                CommissionPerContract = _commissionInput.Value,
                CloseAllPositionsAtWeekEnd = _weekEndCheckBox.Checked,
                AccountId = (_accountCombo.SelectedItem as AccountComboItem)?.Account?.Id
            };

            SetRunningState(true);
            _reportButton.Enabled = false;
            _progressBar.Value = 0;
            _pollingCts?.Dispose();
            _pollingCts = new CancellationTokenSource();

            Log($"Workspace {workspace.Info.Name} ({workspace.Info.Id})");
            Log($"Finestra UTC {request.StartDate:yyyy-MM-dd HH:mm}Z → {request.EndDate:yyyy-MM-dd HH:mm}Z");
            Log($"Strategie dal masterfilter: {masterFilter.StrategiesFilter.Count}");

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
}
