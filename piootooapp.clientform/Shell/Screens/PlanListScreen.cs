using System.ComponentModel;
using Piootoo.Shared.Models.Optimization;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class PlanRow
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Groups { get; set; }

    public int MaxConcurrentTrades { get; set; }

    /// <summary>Cartella di backtest della riga primaria: null/vuota = nessun Titano configurato.</summary>
    public string TitanoBacktestFolder { get; set; } = string.Empty;

    /// <summary>Etichetta di stato risolta in modo asincrono (ListRotationStatusAsync), "…" finché non arriva.</summary>
    public string RotationStatus { get; set; } = string.Empty;

    public DateTime UpdatedUtc { get; set; }
}

/// <summary>
/// Piani di trading di un workspace. A differenza delle altre anagrafiche non sono globali:
/// vivono in <c>&lt;workspace&gt;/plans/plans.json</c>, quindi la schermata parte da un workspace.
/// </summary>
public partial class PlanListScreen : UserControl, IShellScreen
{
    private readonly List<PlanRow> _allRows = new();
    private readonly SortableBindingList<PlanRow> _visibleRows = new();
    private ShellContext? _context;
    private bool _suspendReload;

    public PlanListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Piani di trading";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
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

            await ReloadPlansAsync(cancellationToken);
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
            _toolbar.SetBusy(false);
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

    private async Task ReloadPlansAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _allRows.Clear();
        if (SelectedWorkspaceId is not { } workspaceId)
        {
            ApplyFilter();
            _context.Navigation.SetStatus("Nessun workspace disponibile.");
            return;
        }

        try
        {
            var plans = await _context.Services.Plans.ListAsync(workspaceId, cancellationToken);
            foreach (var plan in plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase))
            {
                _allRows.Add(new PlanRow
                {
                    Code = plan.Code,
                    Name = plan.Name,
                    Groups = plan.Groups.Count,
                    MaxConcurrentTrades = plan.MaxConcurrentTrades,
                    RotationStatus = "…",
                    UpdatedUtc = plan.UpdatedUtc
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} piani in '{workspaceId}'.");

            await LoadRotationStatusesAsync(workspaceId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ApplyFilter();
            _context.Navigation.SetError(ex.Message);
        }
    }

    /// <summary>
    /// Freschezza per riga, risolta dopo la prima resa della griglia: una chiamata per piano al
    /// registro dei run Titano, il run non è più un campo del piano ma sempre "l'ultimo per la
    /// cartella" (vedi <c>TitanoRotationService.GetFreshness</c>).
    /// </summary>
    private async Task LoadRotationStatusesAsync(string workspaceId, CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        foreach (var row in _allRows)
        {
            try
            {
                var status = await _context.Services.Plans.GetRotationStatusAsync(workspaceId, row.Code, cancellationToken);
                row.RotationStatus = DescribeRotationStatus(status);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                row.RotationStatus = "?";
            }
        }

        _visibleRows.ResetBindings();
    }

    private static string DescribeRotationStatus(TitanoRotationStatus status) => status.Freshness switch
    {
        TitanoRotationFreshness.Fresh => $"🟢 Pronto ({status.LatestRunGeneratedAtUtc:yyyy-MM-dd})",
        TitanoRotationFreshness.Stale => $"🟡 Da aggiornare ({status.LatestRunGeneratedAtUtc:yyyy-MM-dd})",
        _ => "⚪ Nessun run"
    };

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     filter.Length == 0
                     || row.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    private PlanRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void UpdateDeleteAvailability() => _toolbar.SetDeleteEnabled(SelectedRow != null);

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateDeleteAvailability();

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (_suspendReload)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await ReloadPlansAsync(CancellationToken.None);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private void OnCreateRequested(object? sender, EventArgs e) => OpenDetail(null);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && SelectedRow is { } row)
        {
            OpenDetail(row.Code);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow is { } row)
        {
            e.Handled = true;
            OpenDetail(row.Code);
        }
    }

    private void OpenDetail(string? code)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        var detail = new PlanDetailScreen();
        detail.SetPlan(workspaceId, code);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Eliminare il piano '{row.Code}'?{Environment.NewLine}{Environment.NewLine}" +
                "Le sessioni già aperte non cambiano: ne hanno acquisito uno snapshot alla creazione.",
                "Elimina piano",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Plans.DeleteAsync(workspaceId, row.Code);
            _context.Navigation.SetStatus($"Piano '{row.Code}' eliminato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        await ReloadPlansAsync(CancellationToken.None);
    }
}
