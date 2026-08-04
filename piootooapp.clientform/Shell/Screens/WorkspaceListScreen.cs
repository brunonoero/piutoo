using System.ComponentModel;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class WorkspaceRow
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StrategiesCount { get; set; }
}

/// <summary>Elenco dei workspace. Il masterfilter si modifica nel dettaglio.</summary>
public partial class WorkspaceListScreen : UserControl, IShellScreen
{
    private readonly List<WorkspaceRow> _allRows = new();
    private readonly SortableBindingList<WorkspaceRow> _visibleRows = new();
    private ShellContext? _context;

    public WorkspaceListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Workspace";

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
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);
            _allRows.Clear();
            _allRows.AddRange(workspaces.Select(workspace => new WorkspaceRow
            {
                Id = workspace.Id,
                Name = workspace.Name,
                StrategiesCount = workspace.StrategiesCount
            }));
            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} workspace caricati.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _allRows.Clear();
            ApplyFilter();
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     filter.Length == 0
                     || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    /// <summary>Vedi la nota in <see cref="AccountListScreen"/>: mai <c>DataBoundItem</c> qui.</summary>
    private WorkspaceRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void UpdateDeleteAvailability() => _toolbar.SetDeleteEnabled(SelectedRow != null);

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateDeleteAvailability();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnCreateRequested(object? sender, EventArgs e) => OpenDetail(null);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && SelectedRow is { } row)
        {
            OpenDetail(row.Id);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow is { } row)
        {
            e.Handled = true;
            OpenDetail(row.Id);
        }
    }

    private void OpenDetail(string? workspaceId)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new WorkspaceDetailScreen();
        detail.SetWorkspaceId(workspaceId);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Eliminare il workspace '{row.Name}'?{Environment.NewLine}{Environment.NewLine}" +
            "Vengono rimossi anche backtest, piani, run Titano e sessioni contenuti nella cartella.",
            "Elimina workspace",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.DeleteAsync(row.Id);
            _context.Navigation.SetStatus($"Workspace '{row.Name}' eliminato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        await LoadAsync(CancellationToken.None);
    }
}
