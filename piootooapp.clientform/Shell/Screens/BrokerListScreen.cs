using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia broker. Tipo dedicato: la griglia non legge proprietà annidate.</summary>
public sealed class BrokerRow
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string SymbolConversionCode { get; set; } = string.Empty;

    public string DatafeedFolder { get; set; } = string.Empty;

    public int Accounts { get; set; }

    public string Enabled { get; set; } = string.Empty;
}

/// <summary>
/// Anagrafica dei broker: chi quota gli strumenti su cui i conti operano. Da qui vengono la tabella
/// dei simboli dei conti e la cartella del datafeed raccolto, e il piano dichiara su quale broker
/// opera.
/// </summary>
public partial class BrokerListScreen : UserControl, IShellScreen
{
    private readonly List<BrokerRow> _allRows = new();
    private readonly SortableBindingList<BrokerRow> _visibleRows = new();
    private ShellContext? _context;

    public BrokerListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Broker";

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
            var brokers = await _context.Services.Api.ListBrokersAsync(cancellationToken);
            // I conti si contano qui e non nel dettaglio: dice a colpo d'occhio quale broker è
            // davvero in uso, ed è l'informazione che serve prima di provare a eliminarne uno.
            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);

            _allRows.Clear();
            _allRows.AddRange(brokers.Select(broker => new BrokerRow
            {
                Code = broker.Code,
                Name = broker.Name,
                SymbolConversionCode = broker.SymbolConversionCode,
                DatafeedFolder = string.IsNullOrWhiteSpace(broker.DatafeedFolder)
                    ? broker.Code
                    : broker.DatafeedFolder,
                Accounts = accounts.Count(account => string.Equals(
                    account.BrokerCode?.Trim(), broker.Code, StringComparison.OrdinalIgnoreCase)),
                Enabled = broker.Enabled ? "sì" : "no"
            }));

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} broker in anagrafica.");
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

    private static bool Matches(BrokerRow row, string filter)
        => filter.Length == 0
           || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || row.Code.Contains(filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>Vedi la nota in <see cref="AccountListScreen"/>: mai <c>DataBoundItem</c> qui.</summary>
    private BrokerRow? SelectedRow
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

        var detail = new BrokerDetailScreen();
        detail.SetCode(code);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        // La conferma NOMINA i conti che lo usano: il server rifiuta comunque, ma leggerlo prima
        // evita il giro "provo, prendo l'errore, vado a cercare quali conti erano".
        var conti = row.Accounts == 0
            ? "Nessun conto lo referenzia."
            : $"Lo referenziano {row.Accounts} conti: l'eliminazione verrà rifiutata finché ci sono.";

        var confirm = MessageBox.Show(
            this,
            $"Eliminare il broker '{row.Name}'?{Environment.NewLine}{conti}",
            "Elimina broker",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.DeleteBrokerAsync(row.Code);
            _context.Navigation.SetStatus($"Broker '{row.Name}' eliminato.");
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
