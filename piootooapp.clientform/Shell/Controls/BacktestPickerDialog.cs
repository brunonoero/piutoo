using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Screens;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>Riga della lista di scelta del backtest sorgente.</summary>
public sealed class BacktestPickerRow
{
    public BacktestPickerRow(WorkspaceBacktestInfo info)
    {
        Info = info;
        FolderName = info.FolderName;
        Origin = BacktestComboItem.DescribeOrigin(info);
        LastModifiedUtc = info.LastModifiedUtc;
        ResultsCount = info.ResultsCount;
        Range = info is { StartDateUtc: { } start, EndDateUtc: { } end }
            ? $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}"
            : string.Empty;
    }

    /// <summary>Non è una colonna: è l'oggetto che il chiamante riceve indietro.</summary>
    public WorkspaceBacktestInfo Info { get; }

    public string FolderName { get; }

    public string Origin { get; }

    public DateTime LastModifiedUtc { get; }

    public int ResultsCount { get; }

    public string Range { get; }

    /// <summary>Testo su cui lavora il filtro: una riga sola, così il confronto è uno.</summary>
    public string SearchText => $"{FolderName} {Origin} {Range} {LastModifiedUtc:yyyy-MM-dd HH:mm}";
}

/// <summary>
/// Scelta del backtest sorgente in una modale con filtro.
///
/// Era una combo, ma un workspace con qualche centinaio di cartelle rende il menu a tendina
/// inservibile: non si filtra, non si ordina e la riga non ha spazio per l'origine, che è
/// esattamente il dato che distingue due backtest omonimi. Qui la lista è una griglia ordinabile
/// come tutte le altre della console, aperta sull'ordine più utile — data decrescente, il run
/// appena prodotto in cima.
/// </summary>
public partial class BacktestPickerDialog : Form
{
    private readonly List<BacktestPickerRow> _allRows = new();
    private readonly SortableBindingList<BacktestPickerRow> _visibleRows = new();

    public BacktestPickerDialog()
    {
        InitializeComponent();
        ShellTheme.Apply(this);
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    /// <summary>Backtest scelto, valorizzato solo con <see cref="DialogResult.OK"/>.</summary>
    public WorkspaceBacktestInfo? Selected { get; private set; }

    /// <summary>
    /// Apre la modale sull'elenco dato e restituisce la scelta, oppure null se l'utente annulla.
    /// <paramref name="currentFolder"/> è la selezione corrente del form chiamante: la riga viene
    /// pre-selezionata, così riaprire la modale non perde il punto in cui si era.
    /// </summary>
    public static WorkspaceBacktestInfo? Pick(
        IWin32Window owner,
        IEnumerable<WorkspaceBacktestInfo> backtests,
        string? currentFolder)
    {
        using var dialog = new BacktestPickerDialog();
        dialog.SetBacktests(backtests, currentFolder);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Selected : null;
    }

    public void SetBacktests(IEnumerable<WorkspaceBacktestInfo> backtests, string? currentFolder)
    {
        _allRows.Clear();
        foreach (var backtest in backtests.OrderByDescending(backtest => backtest.LastModifiedUtc))
        {
            _allRows.Add(new BacktestPickerRow(backtest));
        }

        ApplyFilter();

        // L'ordine di default è quello del server (data decrescente): impostarlo anche sulla
        // griglia serve a farlo vedere, la freccetta sull'intestazione dice su cosa si sta
        // guardando invece di lasciarlo intuire.
        _grid.Sort(_lastModifiedColumn, System.ComponentModel.ListSortDirection.Descending);

        SelectFolder(currentFolder);
        UpdateStatus();
    }

    /// <summary>
    /// L'ordinamento riordina la lista sottostante, quindi indice di riga e indice nella
    /// collezione restano allineati anche dopo un click sull'intestazione.
    /// </summary>
    private BacktestPickerRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void SelectFolder(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        for (var index = 0; index < _visibleRows.Count; index++)
        {
            if (!string.Equals(_visibleRows[index].FolderName, folderName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _grid.ClearSelection();
            _grid.Rows[index].Selected = true;
            _grid.CurrentCell = _grid.Rows[index].Cells[0];
            return;
        }
    }

    private void ApplyFilter()
    {
        var filter = _filterTextBox.Text.Trim();
        _visibleRows.Clear();
        foreach (var row in _allRows)
        {
            if (filter.Length == 0
                || row.SearchText.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            {
                _visibleRows.Add(row);
            }
        }

        _visibleRows.ReapplySort();
        _bindingSource.ResetBindings(false);
    }

    private void UpdateStatus()
        => _statusLabel.Text = _visibleRows.Count == _allRows.Count
            ? $"{_allRows.Count} backtest"
            : $"{_visibleRows.Count} di {_allRows.Count} backtest";

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
        UpdateStatus();
    }

    private void OnGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            Confirm();
        }
    }

    private void OnSelectClick(object? sender, EventArgs e) => Confirm();

    private void Confirm()
    {
        if (SelectedRow is not { } row)
        {
            MessageBox.Show(this, "Seleziona un backtest.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Selected = row.Info;
        DialogResult = DialogResult.OK;
        Close();
    }
}
