using System.ComponentModel;
using Piootoo.Shared.Models.Strategies;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class StrategyRow
{
    public string Id { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

/// <summary>
/// Catalogo strategie, in sola lettura: le strategie sono classi compilate, non dati.
/// Da qui si guarda cosa esiste; l'abilitazione avviene nel masterfilter del workspace.
/// </summary>
public partial class StrategyListScreen : UserControl, IShellScreen
{
    private readonly List<StrategyCatalogItem> _catalog = new();
    private readonly SortableBindingList<StrategyRow> _visibleRows = new();
    private ShellContext? _context;

    public StrategyListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
        UpdateRowCount();
    }

    public string ScreenTitle => "Strategie";

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
            var strategies = await _context.Services.Api.ListStrategiesAsync(cancellationToken);
            _catalog.Clear();
            _catalog.AddRange(strategies
                .OrderBy(strategy => strategy.Symbol, StringComparer.OrdinalIgnoreCase)
                .ThenBy(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase));
            ApplyFilter();
            _context.Navigation.SetStatus($"{_catalog.Count} strategie nel catalogo.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _catalog.Clear();
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
        foreach (var strategy in _catalog.Where(strategy => Matches(strategy, filter)))
        {
            _visibleRows.Add(new StrategyRow
            {
                Id = strategy.Id,
                Symbol = strategy.Symbol,
                Name = strategy.Name,
                Code = strategy.Code,
                Timeframe = strategy.TimeframeMinutes > 0 ? $"{strategy.TimeframeMinutes}m" : "—",
                Type = strategy.Type,
                IsActive = strategy.IsActive
            });
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateRowCount();
    }

    /// <summary>
    /// Conteggio delle righe effettivamente in griglia. Quando un filtro è attivo mostra anche il
    /// totale del catalogo: leggere "12" senza sapere che le strategie sono 340 è fuorviante.
    /// </summary>
    private void UpdateRowCount()
    {
        _rowCountLabel.Text = _visibleRows.Count == _catalog.Count
            ? $"{_visibleRows.Count} righe"
            : $"{_visibleRows.Count} righe (di {_catalog.Count})";
    }

    private static bool Matches(StrategyCatalogItem strategy, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return strategy.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Symbol.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.Type.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private StrategyRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            OpenDetail();
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            OpenDetail();
        }
    }

    private void OpenDetail()
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        var strategy = _catalog.FirstOrDefault(item =>
            string.Equals(item.Id, row.Id, StringComparison.OrdinalIgnoreCase));
        if (strategy == null)
        {
            return;
        }

        var detail = new StrategyDetailScreen();
        detail.SetStrategy(strategy);
        _context.Navigation.Push(detail);
    }
}
