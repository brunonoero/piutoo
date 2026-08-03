using System.ComponentModel;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia trade, appiattita per la visualizzazione.</summary>
public sealed class TradeRow
{
    public string Strategy { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string Direction { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public DateTime EntryTimeUtc { get; set; }

    public decimal EntryPrice { get; set; }

    public DateTime ExitTimeUtc { get; set; }

    public decimal ExitPrice { get; set; }

    public string ExitReason { get; set; } = string.Empty;

    public decimal NetProfit { get; set; }

    public decimal Commission { get; set; }

    public string Account { get; set; } = string.Empty;
}

/// <summary>
/// Trade prodotti da un backtest, letti da <c>trades.json</c>. Sono l'unico input di Titano,
/// quindi è anche la schermata dove si verifica che un run abbia davvero prodotto qualcosa.
/// </summary>
public partial class TradingResultsScreen : UserControl, IShellScreen
{
    private readonly List<PersistedTrade> _trades = new();
    private readonly BindingList<TradeRow> _visibleRows = new();
    private ShellContext? _context;
    private bool _suspendReload;

    public TradingResultsScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    public string ScreenTitle => "Risultati trading";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var previousWorkspace = SelectedWorkspaceId;
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);

            _suspendReload = true;
            _workspaceCombo.Items.Clear();
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            var restored = FindWorkspaceIndex(previousWorkspace);
            _workspaceCombo.SelectedIndex = restored >= 0
                ? restored
                : _workspaceCombo.Items.Count > 0 ? 0 : -1;
            _suspendReload = false;

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
            _suspendReload = false;
            SetBusy(false);
        }
    }

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private string? SelectedBacktestFolder => (_backtestCombo.SelectedItem as BacktestComboItem)?.Info.FolderName;

    private int FindWorkspaceIndex(string? workspaceId)
    {
        if (string.IsNullOrEmpty(workspaceId))
        {
            return -1;
        }

        for (var index = 0; index < _workspaceCombo.Items.Count; index++)
        {
            if (_workspaceCombo.Items[index] is WorkspaceComboItem item
                && string.Equals(item.Info.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _reloadButton.Enabled = !busy;
        _workspaceCombo.Enabled = !busy;
        _backtestCombo.Enabled = !busy;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private async Task ReloadBacktestsAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            _backtestCombo.Items.Clear();
            ShowTrades(Array.Empty<PersistedTrade>());
            return;
        }

        var backtests = await _context.Services.Api.ListBacktestsAsync(workspaceId, cancellationToken);
        _suspendReload = true;
        _backtestCombo.Items.Clear();
        foreach (var backtest in backtests.OrderByDescending(backtest => backtest.LastModifiedUtc))
        {
            _backtestCombo.Items.Add(new BacktestComboItem(backtest));
        }

        _backtestCombo.SelectedIndex = _backtestCombo.Items.Count > 0 ? 0 : -1;
        _suspendReload = false;

        if (_backtestCombo.SelectedIndex < 0)
        {
            ShowTrades(Array.Empty<PersistedTrade>());
            _context.Navigation.SetStatus($"Il workspace '{workspaceId}' non ha backtest.");
            return;
        }

        await ReloadTradesAsync(cancellationToken);
    }

    private async Task ReloadTradesAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId
            || SelectedBacktestFolder is not { } folder)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var trades = await _context.Services.Api.GetBacktestTradesAsync(workspaceId, folder, cancellationToken);
            ShowTrades(trades);
            _context.Navigation.SetStatus(trades.Count > 0
                ? $"{trades.Count} trade in '{folder}'."
                : $"'{folder}' non contiene trade: guarda backtest-summary.json per capire perché.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ShowTrades(Array.Empty<PersistedTrade>());
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowTrades(IReadOnlyList<PersistedTrade> trades)
    {
        _trades.Clear();
        _trades.AddRange(trades);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = _filterTextBox.Text.Trim();
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();

        decimal net = 0;
        var winners = 0;
        foreach (var trade in _trades.Where(trade => Matches(trade, filter)))
        {
            net += trade.NetProfit;
            if (trade.NetProfit > 0)
            {
                winners++;
            }

            _visibleRows.Add(new TradeRow
            {
                Strategy = trade.StrategyCode,
                Symbol = trade.Symbol,
                Direction = trade.Direction.ToString(),
                Quantity = trade.Quantity,
                EntryTimeUtc = trade.EntryTimeUtc,
                EntryPrice = trade.EntryPrice,
                ExitTimeUtc = trade.ExitTimeUtc,
                ExitPrice = trade.ExitPrice,
                ExitReason = trade.ExitReason ?? string.Empty,
                NetProfit = trade.NetProfit,
                Commission = trade.Commission,
                Account = trade.AccountNumber ?? string.Empty
            });
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ResetBindings();

        var shown = _visibleRows.Count;
        _summaryLabel.Text = shown == 0
            ? "Nessun trade"
            : $"{shown} trade  ·  {winners} vincenti ({(decimal)winners / shown:P0})  ·  P&L netto {net:N2}";
    }

    private static bool Matches(PersistedTrade trade, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return trade.StrategyCode.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || trade.Symbol.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (trade.ExitReason?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (trade.AccountNumber?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            await ReloadBacktestsAsync(CancellationToken.None);
        }
    }

    private async void OnBacktestChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            await ReloadTradesAsync(CancellationToken.None);
        }
    }

    private async void OnReloadClick(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();
}
