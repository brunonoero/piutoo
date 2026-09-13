using System.ComponentModel;
using System.Diagnostics;
using Piootoo.Shared.Models.Backtesting;
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

    /// <summary>
    /// P&amp;L netto in percentuale del capitale iniziale e drawdown massimo in percentuale dal
    /// picco di equity, dal <c>backtest-summary.json</c>. Null — cella vuota, in fondo a ogni
    /// ordinamento — quando la cartella non ha il summary: run interrotti e run del cBot.
    /// </summary>
    public decimal? NetProfitPercent { get; set; }

    public decimal? MaxDrawdownPercent { get; set; }

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
                    Range = DescribeRange(backtest),
                    NetProfitPercent = backtest.NetProfitPercent,
                    MaxDrawdownPercent = backtest.MaxDrawdownPercent
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
        UpdateCommandAvailability();
    }

    private BacktestRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    /// <summary>Le righe selezionate, nell'ordine della griglia.</summary>
    private IReadOnlyList<BacktestRow> SelectedRows
        => _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .Where(index => index >= 0 && index < _visibleRows.Count)
            .OrderBy(index => index)
            .Select(index => _visibleRows[index])
            .ToList();

    /// <summary>
    /// Eliminare vale per una riga sola: con la selezione multipla accesa, cancellare "quella
    /// corrente" mentre ne sono evidenziate due sarebbe un gesto ambiguo. Il confronto vuole
    /// esattamente due righe.
    /// </summary>
    private void UpdateCommandAvailability()
    {
        var selected = SelectedRows.Count;
        _toolbar.SetDeleteEnabled(selected == 1 && SelectedRow != null);
        _toolbar.SetExportEnabled(selected == 2);
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateCommandAvailability();

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

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRows.Count != 1 || SelectedRow is not { } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        if (MessageBox.Show(this, BuildDeleteMessage(row), "Elimina backtest",
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

    /// <summary>
    /// Confronta le due righe selezionate: il server crea la cartella <c>compare-NNNN</c>
    /// successiva, ci copia trade, summary e marcatore dei due run e fa girare lo strumento di
    /// confronto. Quale dei due sia il cBot lo decide il server da <c>origin.json</c>; una coppia che
    /// non si confronta (due interni, broker diversi, run senza marcatore) torna con le sue parole.
    /// </summary>
    private async void OnCompareRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId || SelectedRows is not { Count: 2 } rows)
        {
            return;
        }

        var nl = Environment.NewLine;
        var question = "Confrontare questi due run?" + nl + nl +
                       $"{rows[0].FolderName}  ({rows[0].Origin}){nl}" +
                       $"{rows[1].FolderName}  ({rows[1].Origin}){nl}{nl}" +
                       "Il server crea la cartella compare-NNNN successiva, ci copia trade, summary e " +
                       "origin dei due run e lancia lo strumento di confronto. Il log del cBot e l'export " +
                       "Events di cTrader si aggiungono a mano.";
        if (MessageBox.Show(this, question, "Confronta backtest", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var job = await _context.Services.Api.StartCompareAsync(workspaceId, rows[0].FolderName, rows[1].FolderName);
            job = await _context.Services.Api.PollCompareUntilTerminalAsync(
                job.JobId,
                progress => _context.Navigation.SetStatus($"Confronto {progress.FolderName}: {progress.ProgressMessage}"));

            if (job.Status != BacktestingJobStatus.Completed)
            {
                MessageBox.Show(this, $"{job.FolderName}: {job.ErrorMessage ?? job.ProgressMessage}",
                    "Confronta backtest", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _context.Navigation.SetStatus($"Confronto scritto in {job.FolderName}.");
            if (MessageBox.Show(this, $"Confronto scritto in{nl}{job.FolderPath}{nl}{nl}Aprire la cartella?",
                    "Confronta backtest", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                OpenFolder(job.FolderPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Confronta backtest", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// Il percorso e' quello del server: si apre solo se la console gira sulla stessa macchina, e
    /// altrimenti lo si dice invece di aprire una finestra su una cartella che non esiste.
    /// </summary>
    private void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            MessageBox.Show(this, $"La cartella {path} e' sul server e non e' raggiungibile da questo computer.",
                "Confronta backtest", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    private static string BuildDeleteMessage(BacktestRow row)
        => $"Eliminare il backtest '{row.FolderName}' con tutto il contenuto?" +
           Environment.NewLine + Environment.NewLine +
           "Vengono rimossi artefatti, report e log del run.";
}
