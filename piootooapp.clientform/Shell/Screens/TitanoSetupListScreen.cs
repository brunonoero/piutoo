using System.ComponentModel;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class TitanoSetupRow
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Setup di rotazione Titano. Sono un'anagrafica e non un parametro di esecuzione: vivono in
/// <c>settings/titano-rotation-setups/</c>, sono globali e indipendenti dal workspace, e la
/// rotazione li usa come ricetta. L'esecuzione vera resta nella schermata <c>Titano</c>.
/// </summary>
public partial class TitanoSetupListScreen : UserControl, IShellScreen
{
    /// <summary>
    /// Ricreati dal server a ogni avvio, quindi non eliminabili: il pulsante resta spento invece di
    /// proporre un'azione che il server rifiuterebbe.
    /// </summary>
    private static readonly HashSet<string> SeededIds =
        new(StringComparer.OrdinalIgnoreCase) { "conservativo", "bilanciato", "dinamico" };

    private readonly List<TitanoSetupRow> _allRows = new();
    private readonly SortableBindingList<TitanoSetupRow> _visibleRows = new();
    private ShellContext? _context;

    public TitanoSetupListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Setup Titano";

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
            var setups = await _context.Services.Titano.ListSetupsAsync(cancellationToken);
            _allRows.Clear();
            foreach (var setup in setups.OrderBy(setup => setup.Name, StringComparer.OrdinalIgnoreCase))
            {
                _allRows.Add(new TitanoSetupRow
                {
                    Id = setup.Id,
                    Name = setup.Name,
                    Description = setup.Description,
                    UpdatedAt = setup.UpdatedAt
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} setup di rotazione.");
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

    private TitanoSetupRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void UpdateDeleteAvailability()
        => _toolbar.SetDeleteEnabled(SelectedRow is { } row && !SeededIds.Contains(row.Id));

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateDeleteAvailability();

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

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

    private void OpenDetail(string? setupId)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new TitanoSetupDetailScreen();
        detail.SetSetup(setupId);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Eliminare il setup '{row.Name}'?{Environment.NewLine}{Environment.NewLine}" +
                "I run già calcolati non cambiano: portano i propri parametri nel manifest. " +
                "Nei piani il riferimento al setup resta come tracciamento.",
                "Elimina setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Titano.DeleteSetupAsync(row.Id);
            _context.Navigation.SetStatus($"Setup '{row.Name}' eliminato.");
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
