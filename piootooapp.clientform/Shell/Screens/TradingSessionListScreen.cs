using System.ComponentModel;
using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della lista sessioni, appiattita per la griglia.</summary>
public sealed class TradingSessionRow
{
    public string ShortId { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public string ClientRunMode { get; set; } = string.Empty;

    public string ExecutionMode { get; set; } = string.Empty;

    public string TitanoMode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    [Browsable(false)]
    public TradingSessionSummary Summary { get; set; } = null!;
}

/// <summary>
/// Elenco delle sessioni vive nel processo server: sia quelle aperte da questa console, sia quelle
/// aperte da un cBot cTrader con <c>open-plan</c> — prima di questa lista erano visibili solo le
/// prime, perché il server non esponeva nulla per scoprire le seconde (<c>_sessions</c> restava un
/// dizionario in RAM senza un elenco pubblico). Le sessioni sono un dato di processo, non di
/// workspace: sparisce tutto al riavvio del server, quindi non c'è nulla da filtrare per workspace
/// a monte, a differenza di backtest e piani.
/// </summary>
public partial class TradingSessionListScreen : UserControl, IShellScreen
{
    private readonly List<TradingSessionRow> _allRows = new();
    private readonly SortableBindingList<TradingSessionRow> _visibleRows = new();
    private ShellContext? _context;

    public TradingSessionListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
        _toolbar.CanDelete = false;
    }

    public string ScreenTitle => "Sessioni di trading";

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
            var sessions = await _context.Services.Sessions.ListAsync(cancellationToken);

            _allRows.Clear();
            foreach (var summary in sessions)
            {
                _allRows.Add(new TradingSessionRow
                {
                    ShortId = summary.SessionId.Length <= 8 ? summary.SessionId : summary.SessionId[..8],
                    WorkspaceId = summary.WorkspaceId,
                    PlanCode = summary.PlanCode ?? string.Empty,
                    ClientRunMode = summary.ClientRunMode.ToString(),
                    ExecutionMode = summary.ExecutionMode.ToString(),
                    TitanoMode = summary.TitanoMode.ToString(),
                    Status = summary.Status.ToString(),
                    CreatedAtUtc = summary.CreatedAtUtc,
                    Summary = summary
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus(_allRows.Count == 0
                ? "Nessuna sessione viva sul server (sparita al riavvio, o mai apertane una)."
                : $"{_allRows.Count} sessioni vive.");
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
                     || row.ShortId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.WorkspaceId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.PlanCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
    }

    private TradingSessionRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    /// <summary>Il pulsante principale della lista apre "da piano": è il percorso allineato al cBot,
    /// quello che si vuole incoraggiare. La creazione diretta resta un pulsante secondario.</summary>
    private void OnCreateRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        var screen = new TradingSessionsScreen();
        screen.SelectOpenFromPlan();
        _context.Navigation.Push(screen);
    }

    private void OnDirectCreateClick(object? sender, EventArgs e)
        => _context?.Navigation.Push(new TradingSessionsScreen());

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && SelectedRow is { } row)
        {
            OpenDetail(row);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow is { } row)
        {
            e.Handled = true;
            OpenDetail(row);
        }
    }

    private void OpenDetail(TradingSessionRow row)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new TradingSessionsScreen();
        detail.SetSession(row.Summary);
        _context.Navigation.Push(detail);
    }
}
