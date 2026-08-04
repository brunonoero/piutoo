using System.ComponentModel;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia tabelle di conversione. Tipo dedicato: la griglia non legge proprietà annidate.</summary>
public sealed class SymbolConversionRow
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int MappingCount { get; set; }
}

/// <summary>
/// Elenco delle tabelle di conversione simbolo globali: registro fuori da workspace e account, ogni
/// account ne referenzia una per codice. Creazione ed eliminazione qui, modifica nel dettaglio.
/// </summary>
public partial class SymbolConversionListScreen : UserControl, IShellScreen
{
    private readonly List<SymbolConversionRow> _allRows = new();
    private readonly SortableBindingList<SymbolConversionRow> _visibleRows = new();
    private ShellContext? _context;

    public SymbolConversionListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Conversioni simbolo";

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
            var conversions = await _context.Services.Api.ListSymbolConversionsAsync(cancellationToken);
            _allRows.Clear();
            _allRows.AddRange(conversions.Select(ToRow));
            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} tabelle di conversione caricate.");
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
            UpdateDeleteAvailability();
        }
    }

    private static SymbolConversionRow ToRow(SymbolConversion conversion) => new()
    {
        Code = conversion.Code,
        Name = conversion.Name,
        MappingCount = conversion.Mappings.Count
    };

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row => Matches(row, filter)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    private static bool Matches(SymbolConversionRow row, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return Contains(row.Name, filter) || Contains(row.Code, filter);
    }

    private static bool Contains(string value, string filter)
        => value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// La riga selezionata si legge dalla lista locale e non da <c>DataBoundItem</c>: durante la
    /// distruzione del controllo la griglia azzera il DataSource e solleva <c>SelectionChanged</c>
    /// con il binding già smontato, e il CurrencyManager a quel punto lancia.
    /// </summary>
    private SymbolConversionRow? SelectedRow
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
        if (_context == null)
        {
            return;
        }

        var detail = new SymbolConversionDetailScreen();
        detail.SetCode(code);
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
            $"Eliminare la tabella di conversione '{row.Name}'?",
            "Elimina tabella di conversione",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.DeleteSymbolConversionAsync(row.Code);
            _context.Navigation.SetStatus($"Tabella di conversione '{row.Name}' eliminata.");
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
