using System.ComponentModel;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia gruppi: il conteggio account viene dal registro account.</summary>
public sealed class GroupRow
{
    public string GroupId { get; set; } = string.Empty;

    public int AccountCount { get; set; }

    public string Accounts { get; set; } = string.Empty;
}

/// <summary>
/// Gruppi account anti copy-trading. Non hanno un dettaglio proprio: l'API li tratta come
/// semplici identificativi, quindi la creazione passa da un dialog a campo singolo.
/// </summary>
public partial class GroupListScreen : UserControl, IShellScreen
{
    private readonly List<GroupRow> _allRows = new();
    private readonly BindingList<GroupRow> _visibleRows = new();
    private ShellContext? _context;

    public GroupListScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _visibleRows;
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    public string ScreenTitle => "Gruppi";

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
            var groups = await _context.Services.Api.ListAccountGroupsAsync(cancellationToken);
            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);

            _allRows.Clear();
            foreach (var group in groups.OrderBy(group => group, StringComparer.OrdinalIgnoreCase))
            {
                var members = accounts
                    .Where(account => string.Equals(account.GroupId, group, StringComparison.OrdinalIgnoreCase))
                    .Select(account => account.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _allRows.Add(new GroupRow
                {
                    GroupId = group,
                    AccountCount = members.Count,
                    Accounts = string.Join(", ", members)
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} gruppi caricati.");
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
        }
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     filter.Length == 0
                     || row.GroupId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Accounts.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    /// <summary>Vedi la nota in <see cref="AccountListScreen"/>: mai <c>DataBoundItem</c> qui.</summary>
    private GroupRow? SelectedRow
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

    private async void OnCreateRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        using var dialog = new TextPromptDialog
        {
            Text = "Nuovo gruppo",
            Prompt = "Identificativo del gruppo (tipicamente la prop firm)",
            Placeholder = "es. FTMO"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.AddAccountGroupAsync(dialog.Value);
            _context.Navigation.SetStatus($"Gruppo '{dialog.Value}' creato.");
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

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        var warning = row.AccountCount > 0
            ? $"Al gruppo '{row.GroupId}' sono associati {row.AccountCount} account. Eliminarlo comunque?"
            : $"Eliminare il gruppo '{row.GroupId}'?";
        if (MessageBox.Show(this, warning, "Elimina gruppo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Api.RemoveAccountGroupAsync(row.GroupId);
            _context.Navigation.SetStatus($"Gruppo '{row.GroupId}' eliminato.");
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
