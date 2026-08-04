using System.ComponentModel;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia account. Tipo dedicato perché la griglia non sa leggere proprietà annidate.</summary>
public sealed class AccountRow
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string Broker { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }

    public bool Enabled { get; set; }

    public int SymbolCount { get; set; }
}

/// <summary>Elenco degli account globali: creazione ed eliminazione qui, modifica nel dettaglio.</summary>
public partial class AccountListScreen : UserControl, IShellScreen
{
    private readonly List<AccountRow> _allRows = new();
    private readonly SortableBindingList<AccountRow> _visibleRows = new();
    private ShellContext? _context;

    public AccountListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;

        // La corrispondenza fra indice di riga e indice nella lista resta 1 a 1 — è così che si
        // legge la selezione — perché a ordinare è la collezione, non una vista sopra di essa.
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Account";

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
            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
            _allRows.Clear();
            _allRows.AddRange(accounts.Select(ToRow));
            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} account caricati.");
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

    private static AccountRow ToRow(WorkspaceAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        AccountNumber = account.AccountNumber,
        GroupId = account.GroupId,
        Broker = account.Broker,
        Currency = account.Currency,
        InitialBalance = account.InitialBalance,
        Enabled = account.Enabled,
        SymbolCount = account.SymbolMappings.Count
    };

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

    private static bool Matches(AccountRow row, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return Contains(row.Name, filter)
            || Contains(row.AccountNumber, filter)
            || Contains(row.GroupId, filter)
            || Contains(row.Broker, filter);
    }

    private static bool Contains(string value, string filter)
        => value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// La riga selezionata si legge dalla lista locale e non da <c>DataBoundItem</c>: durante la
    /// distruzione del controllo la griglia azzera il DataSource e solleva <c>SelectionChanged</c>
    /// con il binding già smontato, e il CurrencyManager a quel punto lancia.
    /// </summary>
    private AccountRow? SelectedRow
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
            OpenDetail(row.Id);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow is { } row)
        {
            e.Handled = true;
            OpenDetail(row.Id);
        }
    }

    private void OpenDetail(string? accountId)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new AccountDetailScreen();
        detail.SetAccountId(accountId);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Eliminare l'account '{row.Name}'?",
            "Elimina account",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.DeleteAccountAsync(row.Id);
            _context.Navigation.SetStatus($"Account '{row.Name}' eliminato.");
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
