using Piootoo.Shared.Models.BrokerWorkspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// I piani in produzione, una riga per broker. Come i best plan non dipende dal workspace scelto in
/// alto: un broker workspace non sta dentro un workspace. Non ha "Nuovo": un broker workspace nasce
/// alla prima promozione di un best plan del suo broker (dettaglio best plan, "Promuovi in
/// produzione…"). Vedi <c>docs/domini/broker-workspace.md</c>.
/// </summary>
public partial class BrokerWorkspaceListScreen : UserControl, IShellScreen
{
    private readonly List<BrokerWorkspaceSummary> _all = new();
    private readonly SortableBindingList<BrokerWorkspaceSummary> _visible = new();
    private ShellContext? _context;

    public BrokerWorkspaceListScreen()
    {
        InitializeComponent();
        _grid.EnableColumnSorting();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visible;
    }

    public string ScreenTitle => "Produzione";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _context.Navigation.SetStatus("Caricamento dei broker workspace…");
        _toolbar.SetBusy(true);
        UseWaitCursor = true;
        try
        {
            var workspaces = await _context.Services.BrokerWorkspaces.ListAsync(cancellationToken);
            _all.Clear();
            _all.AddRange(workspaces);
            ApplyFilter();
            _context.Navigation.SetStatus(workspaces.Count == 0
                ? "Nessun piano in produzione: si promuove un best plan dal suo dettaglio, con \"Promuovi in produzione…\"."
                : $"{workspaces.Count} broker workspace, {workspaces.Sum(workspace => workspace.ActivePlans)} piani attivi.");
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
            UseWaitCursor = false;
            _toolbar.SetBusy(false);
        }
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visible.RaiseListChangedEvents = false;
        _visible.Clear();
        foreach (var workspace in _all.Where(workspace => filter.Length == 0
                     || workspace.BrokerCode.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || workspace.BrokerName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visible.Add(workspace);
        }

        _visible.RaiseListChangedEvents = true;
        _visible.ReapplySort();
        _visible.ResetBindings();
    }

    private BrokerWorkspaceSummary? SelectedWorkspace
        => _grid.CurrentRow is { Index: >= 0 } row && row.Index < _visible.Count ? _visible[row.Index] : null;

    private void OpenDetail(BrokerWorkspaceSummary workspace)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new BrokerWorkspaceDetailScreen();
        detail.SetBroker(workspace.BrokerCode);
        _context.Navigation.Push(detail);
    }

    // --- eventi -----------------------------------------------------------

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < _visible.Count)
        {
            OpenDetail(_visible[e.RowIndex]);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedWorkspace is { } workspace)
        {
            e.Handled = true;
            OpenDetail(workspace);
        }
    }
}
