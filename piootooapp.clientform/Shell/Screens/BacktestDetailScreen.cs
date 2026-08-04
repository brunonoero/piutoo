using System.ComponentModel;
using System.Text.Json;
using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Controls;

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
/// Dettaglio di una cartella di backtest, in sola lettura: un backtest è un artefatto prodotto da
/// un run, non un'anagrafica, e riscriverlo dall'interfaccia lo renderebbe incoerente con
/// <c>backtest-log.jsonl</c>, che è append-only.
///
/// Due tab. *Riepilogo* mostra per primo il blocco <c>diagnostics</c> di
/// <c>backtest-summary.json</c>: è la prima cosa da leggere quando un backtest non produce trade,
/// e nel JSON grezzo si perde. *Operazioni* mostra i trade chiusi di <c>trades.json</c>, che sono
/// l'unico input di Titano.
/// </summary>
public partial class BacktestDetailScreen : UserControl, IShellScreen
{
    private readonly List<PersistedTrade> _trades = new();
    private readonly SortableBindingList<TradeRow> _visibleTrades = new();
    private ShellContext? _context;
    private string _workspaceId = string.Empty;
    private string _folderName = string.Empty;

    public BacktestDetailScreen()
    {
        InitializeComponent();
        _tradesBindingSource.DataSource = _visibleTrades;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => _folderName.Length > 0 ? _folderName : "Backtest";

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell.</summary>
    public void SetBacktest(string workspaceId, string folderName)
    {
        _workspaceId = workspaceId;
        _folderName = folderName;
        _toolbar.Title = folderName;
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _workspaceId.Length == 0 || _folderName.Length == 0)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await LoadSummaryAsync(cancellationToken);
            await LoadTradesAsync(cancellationToken);
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

    // --- riepilogo --------------------------------------------------------

    private async Task LoadSummaryAsync(CancellationToken cancellationToken)
    {
        string summaryJson;
        try
        {
            summaryJson = await _context!.Services.Api.GetBacktestSummaryAsync(
                _workspaceId, _folderName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Manca in un run interrotto e in quelli prodotti dall'engine esterno: è un'assenza
            // normale, non un errore della schermata. Le operazioni restano leggibili.
            _headlineLabel.Text = "Nessun backtest-summary.json in questa cartella.";
            _diagnosticsList.Items.Clear();
            _diagnosticsList.Items.Add(ex.Message);
            _summaryJsonBox.Clear();
            return;
        }

        _summaryJsonBox.Text = NormalizeNewLines(summaryJson);
        BindSummary(summaryJson);
    }

    private void BindSummary(string summaryJson)
    {
        _diagnosticsList.Items.Clear();
        try
        {
            using var document = JsonDocument.Parse(summaryJson);
            var root = document.RootElement;

            _headlineLabel.Text = DescribeHeadline(root);

            if (root.TryGetProperty("diagnostics", out var diagnostics)
                && diagnostics.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in diagnostics.EnumerateArray())
                {
                    _diagnosticsList.Items.Add(entry.GetString() ?? string.Empty);
                }
            }

            if (_diagnosticsList.Items.Count == 0)
            {
                _diagnosticsList.Items.Add("Il summary non contiene un blocco diagnostics.");
            }
        }
        catch (JsonException ex)
        {
            _headlineLabel.Text = "Summary non leggibile come JSON.";
            _diagnosticsList.Items.Add(ex.Message);
        }
    }

    private static string DescribeHeadline(JsonElement root)
    {
        var parts = new List<string>();

        if (TryGetString(root, "outcome") is { Length: > 0 } outcome)
        {
            parts.Add($"esito {outcome}");
        }

        if (TryGetDateTime(root, "requestedStartUtc") is { } start
            && TryGetDateTime(root, "requestedEndUtc") is { } end)
        {
            parts.Add($"{start:yyyy-MM-dd} → {end:yyyy-MM-dd} UTC");
        }

        if (TryGetInt(root, "totalTrades") is { } totalTrades)
        {
            var winners = TryGetInt(root, "winningTrades");
            parts.Add(winners is { } won
                ? $"{totalTrades} trade ({won} vincenti)"
                : $"{totalTrades} trade");
        }

        if (TryGetDecimal(root, "totalNetProfit") is { } netProfit)
        {
            parts.Add($"P&L netto {netProfit:N2}");
        }

        if (TryGetDecimal(root, "maxDrawdown") is { } drawdown)
        {
            parts.Add($"drawdown max {drawdown:N2}");
        }

        if (TryGetInt(root, "openPositionsAtEnd") is { } open && open > 0)
        {
            // Il P&L delle posizioni ancora aperte non entra in trades.json, quindi non entra
            // in Titano: va detto qui, non lasciato dedurre dalla differenza fra i totali.
            parts.Add($"{open} posizioni aperte a fine run");
        }

        if (TryGetString(root, "errorMessage") is { Length: > 0 } error)
        {
            parts.Add($"errore: {error}");
        }

        return parts.Count > 0 ? string.Join("  ·  ", parts) : "Summary senza campi riconosciuti.";
    }

    private static string? TryGetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? TryGetInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.Number
           && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static decimal? TryGetDecimal(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.Number
           && value.TryGetDecimal(out var parsed)
            ? parsed
            : null;

    private static DateTime? TryGetDateTime(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String
           && value.TryGetDateTime(out var parsed)
            ? parsed
            : null;

    /// <summary>
    /// Il summary è scritto con <c>\n</c>; un <c>TextBox</c> multilinea senza <c>\r</c> mostra
    /// tutto su una riga sola.
    /// </summary>
    private static string NormalizeNewLines(string text)
        => text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

    // --- operazioni -------------------------------------------------------

    private async Task LoadTradesAsync(CancellationToken cancellationToken)
    {
        _trades.Clear();
        try
        {
            var trades = await _context!.Services.Api.GetBacktestTradesAsync(
                _workspaceId, _folderName, cancellationToken);
            _trades.AddRange(trades);
            _context.Navigation.SetStatus(trades.Count > 0
                ? $"{trades.Count} trade in '{_folderName}'."
                : $"'{_folderName}' non contiene trade: la diagnostica nel riepilogo dice perché.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError(ex.Message);
        }
        finally
        {
            ApplyTradeFilter();
        }
    }

    private void ApplyTradeFilter()
    {
        var filter = _tradesFilterBox.Text.Trim();
        _visibleTrades.RaiseListChangedEvents = false;
        _visibleTrades.Clear();

        decimal net = 0;
        var winners = 0;
        foreach (var trade in _trades.Where(trade => Matches(trade, filter)))
        {
            net += trade.NetProfit;
            if (trade.NetProfit > 0)
            {
                winners++;
            }

            _visibleTrades.Add(new TradeRow
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

        _visibleTrades.RaiseListChangedEvents = true;
        _visibleTrades.ReapplySort();
        _visibleTrades.ResetBindings();

        var shown = _visibleTrades.Count;
        _tradesSummaryLabel.Text = shown == 0
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

    // --- eventi -----------------------------------------------------------

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private void OnTradesFilterChanged(object? sender, EventArgs e) => ApplyTradeFilter();
}
