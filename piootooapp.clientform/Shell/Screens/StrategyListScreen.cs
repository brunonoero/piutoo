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

    /// <summary>
    /// Cosa la strategia dichiara di voler tenere: "intraday", "overnight", "overnight+overweek".
    /// Il piano che la esegue puo' comunque troncarla — la parola finale e' sua.
    /// </summary>
    public string Holding { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

/// <summary>
/// Le strategie del <b>workspace corrente</b>, in sola lettura: le strategie sono classi compilate,
/// non dati. Da qui si guarda con cosa si sta lavorando; quali strategie il workspace contenga si
/// decide nel suo masterfilter (*Gestisci workspace…*, accanto al selettore).
///
/// <para><b>Perché non il catalogo intero.</b> Il catalogo sono 111 classi che valgono per tutti i
/// workspace; quello su cui si lavora è un sottoinsieme di poche. Elencarle tutte qui obbligava a
/// ricordare a memoria quali fossero quelle del workspace, e faceva sembrare disponibile a un
/// backtest anche ciò che il masterfilter non contiene. Il conteggio a fondo pagina e la riga di
/// stato dicono sempre quante sono sul totale, così il sottoinsieme non si legge come "tutto".</para>
/// </summary>
public partial class StrategyListScreen : UserControl, IShellScreen
{
    /// <summary>Le strategie del workspace corrente: è già il masterfilter risolto sul catalogo.</summary>
    private readonly List<StrategyCatalogItem> _catalog = new();

    /// <summary>
    /// Quante ne ha il catalogo del server, workspace a parte. Serve solo a dire "12 di 111": un
    /// sottoinsieme senza il totale accanto non si distingue da un catalogo piccolo.
    /// </summary>
    private int _catalogTotal;

    private readonly SortableBindingList<StrategyRow> _visibleRows = new();
    private ShellContext? _context;

    public StrategyListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
        UpdateRowCount();
        UpdateExportAvailability();
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
        _context.Navigation.SetStatus("Lettura delle strategie del workspace…");
        try
        {
            _catalog.Clear();
            _catalogTotal = 0;

            if (_context.Services.Workspaces.CurrentId is not { } workspaceId)
            {
                ApplyFilter();
                _context.Navigation.SetStatus(
                    "Nessun workspace selezionato: scegline uno nella barra in alto.");
                return;
            }

            var strategies = await _context.Services.Api.ListStrategiesAsync(cancellationToken);
            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspaceId, cancellationToken);
            _catalogTotal = strategies.Count;

            var wanted = new HashSet<string>(
                masterFilter.StrategiesFilter.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
                StringComparer.OrdinalIgnoreCase);

            _catalog.AddRange(strategies
                .Where(strategy => wanted.Contains(strategy.Id) || wanted.Contains(strategy.Name))
                .OrderBy(strategy => strategy.Symbol, StringComparer.OrdinalIgnoreCase)
                .ThenBy(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase));

            ApplyFilter();
            _context.Navigation.SetStatus(DescribeLoad(workspaceId, wanted, strategies));
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

    /// <summary>
    /// La riga di stato dopo un caricamento. Dice tre cose che la sola griglia non direbbe: che si
    /// sta guardando un workspace e quale, quanto è il totale del catalogo, e — se ce ne sono — le
    /// voci del masterfilter che <b>nessuna strategia del catalogo</b> soddisfa.
    ///
    /// <para>Quest'ultimo caso è il motivo per cui il messaggio esiste: un Id scritto male, o una
    /// strategia disabilitata dopo che il masterfilter era stato salvato, sparirebbe dalla griglia
    /// senza lasciare traccia, e il workspace sembrerebbe semplicemente più piccolo.</para>
    /// </summary>
    private string DescribeLoad(
        string workspaceId,
        IReadOnlyCollection<string> wanted,
        IReadOnlyCollection<StrategyCatalogItem> catalog)
    {
        if (wanted.Count == 0)
        {
            return $"Il masterfilter del workspace '{workspaceId}' è vuoto: aggiungici le strategie " +
                   "da «Gestisci workspace…», accanto al selettore in alto.";
        }

        var known = new HashSet<string>(
            catalog.Select(strategy => strategy.Id).Concat(catalog.Select(strategy => strategy.Name)),
            StringComparer.OrdinalIgnoreCase);
        var unknown = wanted.Where(id => !known.Contains(id)).Order(StringComparer.OrdinalIgnoreCase).ToList();

        var testa = $"{_catalog.Count} strategie nel workspace '{workspaceId}' (catalogo: {_catalogTotal}).";
        return unknown.Count == 0
            ? testa
            : $"{testa} {unknown.Count} voci del masterfilter non sono nel catalogo: {string.Join(", ", unknown)}.";
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
                Holding = strategy.HoldingLabel,
                IsActive = strategy.IsActive
            });
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateRowCount();
        UpdateExportAvailability();
    }

    /// <summary>
    /// Conteggio delle righe effettivamente in griglia, con accanto i due totali che le danno una
    /// scala: quante ne ha il workspace e quante ne ha il catalogo. Leggere "12" senza sapere che il
    /// workspace ne ha 40 e il catalogo 111 è fuorviante in due modi diversi.
    /// </summary>
    private void UpdateRowCount()
    {
        _rowCountLabel.Text = _visibleRows.Count == _catalog.Count
            ? $"{_visibleRows.Count} righe (catalogo: {_catalogTotal})"
            : $"{_visibleRows.Count} righe (di {_catalog.Count} nel workspace, catalogo: {_catalogTotal})";
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
            || strategy.Type.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || strategy.HoldingLabel.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private StrategyRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    /// <summary>
    /// L'export lavora sulle righe in griglia, non sulla selezione: basta che ce ne sia una.
    /// </summary>
    private void UpdateExportAvailability() => _toolbar.SetExportEnabled(_visibleRows.Count > 0);

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    /// <summary>
    /// Salva su file un array JSON con la scheda completa di <b>tutte le strategie in griglia</b>:
    /// parametri della traduzione, commenti di conversione, sorgente C# e motore Python di
    /// provenienza, una voce per riga.
    ///
    /// <para><b>Quel che si esporta è quel che si vede.</b> Con un filtro attivo escono solo le righe
    /// filtrate, nello stesso ordine della griglia — è il modo per portarsi via un mercato o un
    /// motore alla volta senza dover scegliere gli id a mano. Senza filtro escono tutte le strategie
    /// del workspace corrente, e il titolo del dialog dice quante sono prima di partire.</para>
    ///
    /// <para>Le schede le costruisce il <b>server</b> (<c>api/strategies/export</c>): le strategie
    /// sono classi compilate, e i loro parametri sono leggibili solo da chi le istanzia. La console
    /// riceve il testo e lo scrive dov'è stato chiesto, senza rileggerlo — vedi
    /// <c>WorkspaceApiClient.ExportStrategiesAsync</c>.</para>
    /// </summary>
    private async void OnExportRequested(object? sender, EventArgs e)
    {
        if (_context == null || _visibleRows.Count == 0)
        {
            return;
        }

        // Copia scattata prima del dialog: la griglia puo' cambiare sotto (un refresh, un filtro)
        // mentre l'utente sceglie il file, e l'export deve essere quello che ha visto.
        var ids = _visibleRows.Select(row => row.Id).ToList();

        using var dialog = new SaveFileDialog
        {
            Title = ids.Count == _catalog.Count
                ? $"Esporta tutte le {ids.Count} strategie del workspace"
                : $"Esporta le {ids.Count} strategie filtrate",
            Filter = "Schede strategia (*.json)|*.json|Tutti i file (*.*)|*.*",
            FileName = ids.Count == 1 ? $"{ids[0]}.strategia.json" : "strategie.json",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _toolbar.SetBusy(true);
        _context.Navigation.SetStatus($"Export di {ids.Count} strategie in corso…");
        try
        {
            var json = await _context.Services.Api.ExportStrategiesAsync(ids);
            await File.WriteAllTextAsync(dialog.FileName, json);
            _context.Navigation.SetStatus($"{ids.Count} strategie salvate in {dialog.FileName}.");
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
