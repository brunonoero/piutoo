using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della lista dei run Titano.</summary>
public sealed class TitanoRunRow
{
    public string RunId { get; set; } = string.Empty;

    /// <summary>Cartella di backtest che contiene il run: è la sua provenienza, non un filtro.</summary>
    public string BacktestFolder { get; set; } = string.Empty;

    public DateTime GeneratedAtUtc { get; set; }

    public int PeriodCount { get; set; }
}

/// <summary>
/// Run Titano di un workspace, quale che sia la cartella di backtest che li contiene.
///
/// Un run non è un backtest: è una rotazione calcolata sui trade di un backtest, e vive dentro la
/// sua cartella. Ma per chi lo usa — un piano che ne referenzia uno — quella gerarchia è un
/// dettaglio di archiviazione, quindi la lista è piatta e la provenienza è una colonna.
/// </summary>
public partial class TitanoRunListScreen : UserControl, IShellScreen
{
    private readonly List<TitanoRunRow> _allRows = new();
    private readonly SortableBindingList<TitanoRunRow> _visibleRows = new();
    private ShellContext? _context;

    public TitanoRunListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Run Titano";

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
            await ReloadRunsAsync(cancellationToken);
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

    private string? SelectedWorkspaceId => _context?.Services.Workspaces.CurrentId;

    private async Task ReloadRunsAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _allRows.Clear();
        if (SelectedWorkspaceId is not { } workspaceId)
        {
            ApplyFilter();
            _context.Navigation.SetStatus("Nessun workspace selezionato: scegline uno nella barra in alto.");
            return;
        }

        try
        {
            var runs = await _context.Services.Titano.ListRunsAsync(workspaceId, cancellationToken);
            foreach (var run in runs.OrderByDescending(run => run.GeneratedAtUtc))
            {
                _allRows.Add(new TitanoRunRow
                {
                    RunId = run.RunId,
                    BacktestFolder = run.BacktestFolder,
                    GeneratedAtUtc = run.GeneratedAtUtc,
                    PeriodCount = run.PeriodCount
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} run Titano in '{workspaceId}'.");
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

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     filter.Length == 0
                     || row.RunId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.BacktestFolder.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    private TitanoRunRow? SelectedRow
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

    /// <summary>"Nuova rotazione" porta alla schermata di esecuzione, che resta quella storica.</summary>
    private void OnCreateRequested(object? sender, EventArgs e)
    {
        _context?.Navigation.Push(new TitanoScreen());
    }

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

    private void OpenDetail(TitanoRunRow row)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        var detail = new TitanoRunDetailScreen();
        detail.SetRun(workspaceId, row.BacktestFolder, row.RunId);
        _context.Navigation.Push(detail);
    }

    /// <summary>
    /// La conferma nomina i piani che referenziano il run.
    ///
    /// Per i backtest ci si limita a un avviso generico perché la cartella contiene molti run; qui
    /// il riferimento è puntuale — <c>TradingPlan.TitanoRunId</c>, sul piano o sul singolo gruppo —
    /// quindi si può dire *quali* piani si stanno rompendo invece di dire che qualcosa si romperà.
    /// Il piano non fallisce alla cancellazione: fallisce all'apertura della sessione.
    /// </summary>
    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        _toolbar.SetBusy(true);
        IReadOnlyList<string> affectedPlans;
        try
        {
            var plans = await _context.Services.Plans.ListAsync(workspaceId);
            affectedPlans = plans
                .Where(plan => ReferencesRun(plan, row.BacktestFolder))
                .Select(plan => $"{plan.Code} — {plan.Name}")
                .ToList();
        }
        catch (Exception ex)
        {
            // Senza l'elenco dei piani la conferma sarebbe muta proprio sulla cosa che conta.
            _context.Navigation.SetError($"Impossibile verificare i piani che usano '{row.RunId}': {ex.Message}");
            return;
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        if (MessageBox.Show(this, BuildDeleteMessage(row, affectedPlans), "Elimina run Titano",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Titano.DeleteRunAsync(row.RunId, workspaceId, row.BacktestFolder);
            _context.Navigation.SetStatus($"Run '{row.RunId}' eliminato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        await ReloadRunsAsync(CancellationToken.None);
    }

    /// <summary>
    /// Il run non è più un campo del piano: si usa sempre l'ultimo generato per la cartella. Un
    /// piano "referenzia" quindi un run se una sua riga (o il mirror legacy) usa la stessa cartella
    /// — cancellarlo cambia (o azzera) l'ultimo disponibile per quel piano.
    /// </summary>
    private static bool ReferencesRun(TradingPlan plan, string backtestFolder)
        => string.Equals(plan.TitanoBacktestFolder, backtestFolder, StringComparison.OrdinalIgnoreCase)
           || plan.Groups.Any(group =>
               string.Equals(group.TitanoBacktestFolder, backtestFolder, StringComparison.OrdinalIgnoreCase));

    private static string BuildDeleteMessage(TitanoRunRow row, IReadOnlyList<string> affectedPlans)
    {
        var message =
            $"Eliminare il run '{row.RunId}'?{Environment.NewLine}{Environment.NewLine}" +
            $"Prodotto dal backtest '{row.BacktestFolder}', {row.PeriodCount} periodi.";

        if (affectedPlans.Count == 0)
        {
            return message + Environment.NewLine + Environment.NewLine +
                   "Nessun piano del workspace usa questa cartella.";
        }

        var listed = string.Join(Environment.NewLine, affectedPlans.Take(15).Select(plan => $"  • {plan}"));
        if (affectedPlans.Count > 15)
        {
            listed += $"{Environment.NewLine}  • … e altri {affectedPlans.Count - 15}";
        }

        return message + Environment.NewLine + Environment.NewLine +
               $"{affectedPlans.Count} piani usano questa cartella:" + Environment.NewLine +
               listed + Environment.NewLine + Environment.NewLine +
               "Il run non è indicato sul piano: si usa sempre l'ultimo disponibile per la cartella. " +
               "Se era l'unico, quei piani smetteranno di trovarne uno all'apertura della sessione.";
    }
}
