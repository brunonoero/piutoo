using Piootoo.Shared.Models.Optimization;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia delle decisioni: un periodo per una strategia.</summary>
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
/// Dettaglio di un run Titano: cosa la rotazione ha deciso, periodo per periodo e strategia per
/// strategia. È la schermata dove si verifica un run prima di darlo in pasto a una sessione o a
/// un backtest.
///
/// Di sola lettura per quanto riguarda le decisioni — un manifest è immutabile, il suo
/// <c>RunId</c> è l'hash dei propri input — con due sole azioni che non lo riscrivono: il report
/// HTML e lo sblocco di un hard stop, che è un file separato nella cartella del run.
/// </summary>
public partial class TitanoRunDetailScreen : UserControl, IShellScreen
{
    private readonly List<RotationRow> _allRows = new();
    private readonly SortableBindingList<RotationRow> _visibleRows = new();
    private ShellContext? _context;
    private TitanoRotationManifest? _manifest;
    private string _workspaceId = string.Empty;
    private string _backtestFolder = string.Empty;
    private string _runId = string.Empty;

    public TitanoRunDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => _runId.Length > 0 ? _runId : "Run Titano";

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell.</summary>
    public void SetRun(string workspaceId, string backtestFolder, string runId)
    {
        _workspaceId = workspaceId;
        _backtestFolder = backtestFolder;
        _runId = runId;
        _toolbar.Title = runId;
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _runId.Length == 0)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var manifest = await _context.Services.Titano.GetManifestAsync(
                _runId, _workspaceId, _backtestFolder, cancellationToken);
            ShowManifest(manifest);
            _context.Navigation.SetStatus(DescribeRun(manifest));
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
            _toolbar.SetBusy(false);
        }
    }

    private string DescribeRun(TitanoRotationManifest manifest)
    {
        var parts = new List<string>
        {
            $"{manifest.Periods.Count} periodi",
            $"da '{_backtestFolder}'",
            $"generato {manifest.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC"
        };

        if (manifest.TradesOutsideCoverage > 0)
        {
            // Il primo periodo è solo osservazione e l'ultimo non produce decisione: senza dirlo,
            // il confronto fra le due curve di equity sembra a parità di campione e non lo è.
            parts.Add($"{manifest.TradesOutsideCoverage} trade fuori dai periodi efficaci");
        }

        if (!string.IsNullOrWhiteSpace(manifest.WalkForwardNote))
        {
            parts.Add($"walk-forward: {manifest.WalkForwardNote}");
        }

        return string.Join("  ·  ", parts);
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

        _headlineLabel.Text = manifest != null
            ? DescribeRun(manifest)
            : "Run non caricato.";

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
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        _summaryLabel.Text = $"{_visibleRows.Count} righe su {_allRows.Count}";
    }

    // --- eventi -----------------------------------------------------------

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnReportClick(object? sender, EventArgs e)
    {
        if (_context == null || _manifest == null)
        {
            return;
        }

        try
        {
            var uri = _context.Services.Titano.GetReportUri(_manifest.RunId, _workspaceId, _backtestFolder);
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
        if (_context == null || _manifest == null)
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

        _toolbar.SetBusy(true);
        try
        {
            var reset = await _context.Services.Titano.ResetHardStopAsync(
                _manifest.RunId,
                _workspaceId,
                _backtestFolder,
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
            _toolbar.SetBusy(false);
        }

        await LoadAsync(CancellationToken.None);
    }
}
