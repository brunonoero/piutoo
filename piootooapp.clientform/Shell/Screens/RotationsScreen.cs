using System.ComponentModel;
using Piootoo.Shared.Models.Optimization;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class RotationRow
{
    public DateTime EffectiveFromUtc { get; set; }

    public DateTime EffectiveToUtc { get; set; }

    public string StrategyCode { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public decimal Allocation { get; set; }

    public decimal Score { get; set; }

    public decimal RawScore { get; set; }

    public string Filters { get; set; } = string.Empty;

    public string Transition { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Lettura dei run Titano già calcolati, periodo per periodo. È la schermata dove si verifica
/// cosa una rotazione ha davvero deciso prima di darla in pasto a una sessione o a un backtest.
/// </summary>
public partial class RotationsScreen : UserControl, IShellScreen
{
    private readonly List<RotationRow> _allRows = new();
    private readonly BindingList<RotationRow> _visibleRows = new();
    private ShellContext? _context;
    private TitanoRotationManifest? _manifest;
    private bool _suspendReload;

    public RotationsScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    public string ScreenTitle => "Rotazioni Titano";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var previous = SelectedWorkspaceId;
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);

            _suspendReload = true;
            _workspaceCombo.Items.Clear();
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            var restored = FindWorkspaceIndex(previous);
            _workspaceCombo.SelectedIndex = restored >= 0
                ? restored
                : _workspaceCombo.Items.Count > 0 ? 0 : -1;
            _suspendReload = false;

            await ReloadBacktestsAsync(cancellationToken);
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

    private sealed class RunComboItem
    {
        public RunComboItem(TitanoRunInfo info) => Info = info;

        public TitanoRunInfo Info { get; }

        public override string ToString()
        {
            var shortId = Info.RunId.Length > 12 ? Info.RunId[..12] + "…" : Info.RunId;
            return $"{shortId}  ·  {Info.PeriodCount} periodi  ·  " +
                   $"{Info.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC  ·  {Info.Status}";
        }
    }

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private string? SelectedBacktestFolder => (_backtestCombo.SelectedItem as BacktestComboItem)?.Info.FolderName;

    private TitanoRunInfo? SelectedRun => (_runCombo.SelectedItem as RunComboItem)?.Info;

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

    private void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _workspaceCombo.Enabled = !busy;
        _backtestCombo.Enabled = !busy;
        _runCombo.Enabled = !busy;
        _reloadButton.Enabled = !busy;
        _reportButton.Enabled = !busy && _manifest != null;
        _hardStopButton.Enabled = !busy && _manifest != null;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private async Task ReloadBacktestsAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            _backtestCombo.Items.Clear();
            _runCombo.Items.Clear();
            ShowManifest(null);
            return;
        }

        var backtests = await _context.Services.Api.ListBacktestsAsync(workspaceId, cancellationToken);
        _suspendReload = true;
        _backtestCombo.Items.Clear();
        foreach (var backtest in backtests.OrderByDescending(backtest => backtest.LastModifiedUtc))
        {
            _backtestCombo.Items.Add(new BacktestComboItem(backtest));
        }

        _backtestCombo.SelectedIndex = _backtestCombo.Items.Count > 0 ? 0 : -1;
        _suspendReload = false;

        await ReloadRunsAsync(cancellationToken);
    }

    private async Task ReloadRunsAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _runCombo.Items.Clear();
        if (SelectedWorkspaceId is not { } workspaceId || SelectedBacktestFolder is not { } folder)
        {
            ShowManifest(null);
            return;
        }

        try
        {
            var runs = await _context.Services.Titano.ListRunsAsync(workspaceId, folder, cancellationToken);
            _suspendReload = true;
            foreach (var run in runs.OrderByDescending(run => run.GeneratedAtUtc))
            {
                _runCombo.Items.Add(new RunComboItem(run));
            }

            _runCombo.SelectedIndex = _runCombo.Items.Count > 0 ? 0 : -1;
            _suspendReload = false;

            if (_runCombo.SelectedIndex < 0)
            {
                ShowManifest(null);
                _context.Navigation.SetStatus($"'{folder}' non contiene run Titano.");
                return;
            }

            await ReloadManifestAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _suspendReload = false;
            ShowManifest(null);
            _context.Navigation.SetError(ex.Message);
        }
    }

    private async Task ReloadManifestAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedRun is not { } run
            || SelectedWorkspaceId is not { } workspaceId
            || SelectedBacktestFolder is not { } folder)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var manifest = await _context.Services.Titano.GetManifestAsync(
                run.RunId, workspaceId, folder, cancellationToken);
            ShowManifest(manifest);

            var status = $"Run {run.RunId}: {manifest.Periods.Count} periodi";
            if (manifest.TradesOutsideCoverage > 0)
            {
                status += $", {manifest.TradesOutsideCoverage} trade fuori dai periodi efficaci";
            }

            if (!string.IsNullOrWhiteSpace(manifest.WalkForwardNote))
            {
                status += $" · walk-forward: {manifest.WalkForwardNote}";
            }

            _context.Navigation.SetStatus(status);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ShowManifest(null);
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowManifest(TitanoRotationManifest? manifest)
    {
        _manifest = manifest;
        _allRows.Clear();

        if (manifest != null)
        {
            foreach (var period in manifest.Periods)
            {
                foreach (var strategy in period.Strategies.OrderBy(
                             state => state.StrategyCode, StringComparer.OrdinalIgnoreCase))
                {
                    _allRows.Add(new RotationRow
                    {
                        EffectiveFromUtc = period.EffectiveFromUtc,
                        EffectiveToUtc = period.EffectiveToUtc,
                        StrategyCode = strategy.StrategyCode,
                        State = strategy.State.ToString(),
                        Allocation = strategy.AllocationMultiplier,
                        Score = strategy.Score,
                        RawScore = strategy.RawScore,
                        Filters = $"{strategy.PassingFilters}/{strategy.TotalFilters}",
                        Transition = strategy.TransitionType,
                        Reason = strategy.AnomalyFlags.Count > 0
                            ? $"⚠ {string.Join("; ", strategy.AnomalyFlags)} — {strategy.Reason}"
                            : strategy.Reason
                    });
                }
            }
        }

        ApplyFilter();
        _reportButton.Enabled = manifest != null;
        _hardStopButton.Enabled = manifest != null;
    }

    private void ApplyFilter()
    {
        var terms = _filterTextBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries
                                                   | StringSplitOptions.TrimEntries);
        var onlyChanges = _onlyChangesCheckBox.Checked;

        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows)
        {
            if (onlyChanges && row.Transition is "Unchanged" or "")
            {
                continue;
            }

            if (terms.Length > 0 && !terms.All(term =>
                    row.StrategyCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || row.State.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || row.Transition.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ResetBindings();
        _summaryLabel.Text = $"{_visibleRows.Count} righe su {_allRows.Count}";
    }

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            await ReloadBacktestsAsync(CancellationToken.None);
        }
    }

    private async void OnBacktestChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            await ReloadRunsAsync(CancellationToken.None);
        }
    }

    private async void OnRunChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            await ReloadManifestAsync(CancellationToken.None);
        }
    }

    private async void OnReloadClick(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnReportClick(object? sender, EventArgs e)
    {
        if (_context == null || _manifest == null
            || SelectedWorkspaceId is not { } workspaceId
            || SelectedBacktestFolder is not { } folder)
        {
            return;
        }

        try
        {
            var uri = _context.Services.Titano.GetReportUri(_manifest.RunId, workspaceId, folder);
            await HtmlReportViewerForm.ShowFromUriAsync(
                FindForm()!, _context.Services.Http, uri, "Report Titano");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Report Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnHardStopClick(object? sender, EventArgs e)
    {
        if (_context == null || _manifest == null
            || SelectedWorkspaceId is not { } workspaceId
            || SelectedBacktestFolder is not { } folder)
        {
            return;
        }

        var suggested = (_grid.CurrentRow?.Index is { } index && index >= 0 && index < _visibleRows.Count)
            ? _visibleRows[index].StrategyCode
            : string.Empty;

        using var dialog = new TextPromptDialog
        {
            Text = "Sblocca hard stop",
            Prompt = "Codice della strategia da riabilitare (StrategyCode, non l'id di classe):",
            Value = suggested
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var strategyCode = dialog.Value;

        using var reasonDialog = new TextPromptDialog
        {
            Text = "Sblocca hard stop",
            Prompt = "Motivo dello sblocco:"
        };
        if (reasonDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var reset = await _context.Services.Titano.ResetHardStopAsync(
                _manifest.RunId,
                workspaceId,
                folder,
                new TitanoHardStopResetRequest
                {
                    StrategyCode = strategyCode,
                    RequestedBy = Environment.UserName,
                    Reason = reasonDialog.Value,
                    RequestedAtUtc = DateTime.UtcNow
                });
            _context.Navigation.SetStatus(
                $"Hard stop di '{reset.StrategyCode}' sbloccato, in vigore dal " +
                $"{reset.EffectiveFromUtc:yyyy-MM-dd HH:mm} UTC.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Sblocco hard stop", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }

        await ReloadManifestAsync(CancellationToken.None);
    }
}
