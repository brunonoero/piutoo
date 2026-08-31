using System.ComponentModel;
using Piootoo.Shared.Models;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia datafeed: la griglia non legge proprietà annidate né valori calcolati.</summary>
public sealed class DatafeedRow
{
    /// <summary>Etichetta dell'archivio: <c>interno</c> oppure <c>esterno/{BROKER}</c>.</summary>
    public string Source { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    public DateTime? FirstBarUtc { get; set; }

    public DateTime? LastBarUtc { get; set; }

    /// <summary>Ampiezza del periodo coperto in giorni di calendario, non giorni di borsa.</summary>
    public int? Days { get; set; }

    public int? CandleCount { get; set; }

    /// <summary>Fuso dichiarato dal feed, o l'avviso che non lo dichiara.</summary>
    public string Clock { get; set; } = string.Empty;

    public DateTime LastWriteUtc { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary>Serve al filtro per archivio: l'etichetta è per l'occhio, la chiave è questa.</summary>
    [Browsable(false)]
    public string? Broker { get; set; }
}

/// <summary>
/// Cosa c'è nel repository di barre e fin dove arriva: una riga per coppia (simbolo, timeframe),
/// col periodo che il file copre davvero.
///
/// <para>È la schermata che si guarda <b>prima</b> di lanciare un backtest. Un run che chiede date
/// oltre l'ultima barra di un feed non fallisce: produce meno operazioni del previsto, e la causa
/// si scopre dopo, leggendo <c>coversRequestedRange</c> nel summary. Qui il periodo si vede prima,
/// e insieme al periodo si vede l'archivio: interno ed esterno hanno gli stessi simboli con prezzi
/// diversi, e un run legge da uno solo dei due.</para>
///
/// <para>Il periodo viene dalla prima e dall'ultima barra del file — convertite a UTC vero con
/// l'orologio che il feed dichiara — non dalla data di modifica: sui feed raccolti dai cBot le due
/// cose divergono di giorni. Non è un'anagrafica: niente creazione né eliminazione, i feed si
/// generano fuori dalla console.</para>
/// </summary>
public partial class DatafeedListScreen : UserControl, IShellScreen
{
    /// <summary>Voce del filtro per archivio. Etichetta vuota (null) significa "tutti".</summary>
    private const string AllSources = "(tutti)";

    private readonly List<DatafeedRow> _allRows = new();
    private readonly SortableBindingList<DatafeedRow> _visibleRows = new();
    private ShellContext? _context;

    // La combo si ripopola a ogni caricamento: senza questa guardia ogni Items.Add solleverebbe
    // SelectedIndexChanged e rifiltrerebbe la griglia mentre la si sta ancora riempiendo.
    private bool _updatingSources;

    public DatafeedListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();

        _sourceCombo.Items.Add(AllSources);
        _sourceCombo.SelectedIndex = 0;
    }

    public string ScreenTitle => "Datafeed";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        _context.Navigation.SetStatus("Lettura degli archivi di barre…");
        try
        {
            var feeds = await _context.Services.Datafeed.ListFeedsAsync(
                cancellationToken: cancellationToken);

            _allRows.Clear();
            _allRows.AddRange(feeds.Select(ToRow));
            RefreshSourceFilter();
            ApplyFilter();

            var problems = _allRows.Count(row => row.Note.Length > 0);
            _context.Navigation.SetStatus(problems == 0
                ? $"{_allRows.Count} feed disponibili."
                : $"{_allRows.Count} feed disponibili, {problems} con note (colonna Note).");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _allRows.Clear();
            RefreshSourceFilter();
            ApplyFilter();
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private static DatafeedRow ToRow(DatafeedFeedInfo feed) => new()
    {
        Source = feed.Source,
        Broker = feed.Broker,
        Symbol = feed.Symbol,
        TimeframeMinutes = feed.TimeframeMinutes,
        FirstBarUtc = feed.FirstBarUtc,
        LastBarUtc = feed.LastBarUtc,
        Days = feed.FirstBarUtc is { } first && feed.LastBarUtc is { } last
            ? (int)Math.Round((last - first).TotalDays)
            : null,
        CandleCount = feed.CandleCount,
        Clock = feed.FeedClock ?? "non dichiarato",
        LastWriteUtc = feed.LastWriteUtc,
        Note = feed.Problem ?? string.Empty
    };

    /// <summary>
    /// Le voci del filtro sono gli archivi che esistono davvero, lette dalle righe caricate: un
    /// elenco fisso mostrerebbe broker spariti e nasconderebbe quelli aggiunti dopo.
    /// </summary>
    private void RefreshSourceFilter()
    {
        var selected = _sourceCombo.SelectedItem as string;
        _updatingSources = true;
        try
        {
            _sourceCombo.Items.Clear();
            _sourceCombo.Items.Add(AllSources);
            foreach (var source in _allRows
                .Select(row => row.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(source => source, StringComparer.OrdinalIgnoreCase))
            {
                _sourceCombo.Items.Add(source);
            }

            var index = selected == null ? 0 : _sourceCombo.Items.IndexOf(selected);
            _sourceCombo.SelectedIndex = index >= 0 ? index : 0;
        }
        finally
        {
            _updatingSources = false;
        }
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        var source = _sourceCombo.SelectedItem as string;
        if (source == AllSources)
        {
            source = null;
        }

        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row => Matches(row, filter, source)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
    }

    private static bool Matches(DatafeedRow row, string filter, string? source)
    {
        if (source != null && !string.Equals(row.Source, source, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return filter.Length == 0
            || row.Symbol.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || row.Source.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private void OnSourceFilterChanged(object? sender, EventArgs e)
    {
        if (!_updatingSources)
        {
            ApplyFilter();
        }
    }

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);
}
