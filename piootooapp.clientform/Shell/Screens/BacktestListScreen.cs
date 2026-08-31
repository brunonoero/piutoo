using System.ComponentModel;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della lista backtest, appiattita per la griglia.</summary>
public sealed class BacktestRow
{
    public string FolderName { get; set; } = string.Empty;

    /// <summary>Etichetta leggibile dell'origine, la stessa usata dalle combo.</summary>
    public string Origin { get; set; } = string.Empty;

    public DateTime LastModifiedUtc { get; set; }

    public int ResultsCount { get; set; }

    public string Range { get; set; } = string.Empty;

    /// <summary>Serve al filtro: l'etichetta è per l'occhio, la discriminante è l'enum.</summary>
    [Browsable(false)]
    public BacktestOrigin OriginKind { get; set; } = BacktestOrigin.Unknown;

    [Browsable(false)]
    public string PlanCode { get; set; } = string.Empty;
}

/// <summary>Voce del filtro per origine. Null significa "tutte".</summary>
internal sealed class OriginFilterItem
{
    private OriginFilterItem(BacktestOrigin? origin, string label)
    {
        Origin = origin;
        Label = label;
    }

    public BacktestOrigin? Origin { get; }

    public string Label { get; }

    public static OriginFilterItem All { get; } = new(null, "(tutte)");

    public static OriginFilterItem Of(BacktestOrigin origin, string label) => new(origin, label);

    public override string ToString() => Label;
}

/// <summary>
/// Lista dei backtest del workspace corrente, quello scelto nella barra in alto. Da quando anche
/// le sessioni di trading da piano scrivono
/// sotto <c>backtests/</c>, la cartella contiene due popolazioni diverse: l'origine è in colonna
/// e filtrabile perché scambiare un run del cBot per uno interno non dà errore, dà numeri diversi.
/// Il dettaglio è di sola lettura: un backtest è un artefatto, non un'anagrafica.
/// </summary>
public partial class BacktestListScreen : UserControl, IShellScreen
{
    private readonly List<BacktestRow> _allRows = new();
    private readonly SortableBindingList<BacktestRow> _visibleRows = new();
    private ShellContext? _context;

    public BacktestListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();

        _originCombo.Items.Add(OriginFilterItem.All);
        _originCombo.Items.Add(OriginFilterItem.Of(BacktestOrigin.Internal, "interno"));
        _originCombo.Items.Add(OriginFilterItem.Of(BacktestOrigin.ExternalBroker, "cBot"));
        _originCombo.Items.Add(OriginFilterItem.Of(BacktestOrigin.Unknown, "origine ignota"));
        _originCombo.SelectedIndex = 0;
    }

    public string ScreenTitle => "Backtesting";

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
            await ReloadBacktestsAsync(cancellationToken);
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

    private BacktestOrigin? SelectedOriginFilter => (_originCombo.SelectedItem as OriginFilterItem)?.Origin;

    private async Task ReloadBacktestsAsync(CancellationToken cancellationToken)
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
            var backtests = await _context.Services.Api.ListBacktestsAsync(workspaceId, cancellationToken);

            // Il più recente in cima: su una cartella con mesi di run è l'unico ordinamento utile.
            foreach (var backtest in backtests.OrderByDescending(item => item.LastModifiedUtc))
            {
                _allRows.Add(new BacktestRow
                {
                    FolderName = backtest.FolderName,
                    Origin = BacktestComboItem.DescribeOrigin(backtest),
                    OriginKind = backtest.Origin,
                    PlanCode = backtest.PlanCode ?? string.Empty,
                    LastModifiedUtc = backtest.LastModifiedUtc,
                    ResultsCount = backtest.ResultsCount,
                    Range = DescribeRange(backtest)
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} backtest in '{workspaceId}'.");
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

    private static string DescribeRange(WorkspaceBacktestInfo info)
        => info is { StartDateUtc: { } start, EndDateUtc: { } end }
            ? $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}"
            : string.Empty;

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        var origin = SelectedOriginFilter;

        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     (origin == null || row.OriginKind == origin)
                     && (filter.Length == 0
                         || row.FolderName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                         || row.PlanCode.Contains(filter, StringComparison.OrdinalIgnoreCase))))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    private BacktestRow? SelectedRow
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

    private void OnOriginFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    /// <summary>"Nuovo backtest" porta alla schermata di avvio, che resta quella storica.</summary>
    private void OnCreateRequested(object? sender, EventArgs e)
    {
        _context?.Navigation.Push(new BacktestingScreen());
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

    private void OpenDetail(BacktestRow row)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        var detail = new BacktestDetailScreen();
        detail.SetBacktest(workspaceId, row.FolderName, row.OriginKind);
        _context.Navigation.Push(detail);
    }

    /// <summary>
    /// La conferma elenca i run Titano contenuti nella cartella: un piano che referenzia un run
    /// cancellato non fallisce qui, fallisce all'apertura della sessione, quando è tardi.
    /// </summary>
    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        _toolbar.SetBusy(true);
        IReadOnlyList<string> titanoRuns;
        try
        {
            titanoRuns = await _context.Services.Api.ListBacktestTitanoRunsAsync(workspaceId, row.FolderName);
        }
        catch (Exception ex)
        {
            // Non sapere quali run ci sono dentro è già un motivo per non cancellare al buio.
            _context.Navigation.SetError($"Impossibile elencare i run Titano di '{row.FolderName}': {ex.Message}");
            return;
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        if (MessageBox.Show(this, BuildDeleteMessage(row, titanoRuns), "Elimina backtest",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.DeleteBacktestAsync(workspaceId, row.FolderName);
            _context.Navigation.SetStatus($"Backtest '{row.FolderName}' eliminato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        await ReloadBacktestsAsync(CancellationToken.None);
    }

    private static string BuildDeleteMessage(BacktestRow row, IReadOnlyList<string> titanoRuns)
    {
        var message = $"Eliminare il backtest '{row.FolderName}' con tutto il contenuto?";
        if (titanoRuns.Count == 0)
        {
            return message + Environment.NewLine + Environment.NewLine +
                   "La cartella non contiene run Titano.";
        }

        var listed = string.Join(Environment.NewLine, titanoRuns.Take(15).Select(run => $"  • {run}"));
        if (titanoRuns.Count > 15)
        {
            listed += $"{Environment.NewLine}  • … e altri {titanoRuns.Count - 15}";
        }

        return message + Environment.NewLine + Environment.NewLine +
               $"Vengono cancellati anche {titanoRuns.Count} run Titano:" + Environment.NewLine +
               listed + Environment.NewLine + Environment.NewLine +
               "Un piano che referenzia uno di questi run non darà errore subito: fallirà " +
               "all'apertura della sessione.";
    }
}
