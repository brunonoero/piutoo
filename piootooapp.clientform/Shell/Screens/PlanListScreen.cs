using System.ComponentModel;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

public sealed class PlanRow
{
    /// <summary>"🔒 Bloccato" o vuoto: ordinando la colonna i bloccati finiscono insieme.</summary>
    public string Status { get; set; } = string.Empty;

    [Browsable(false)]
    public bool Locked { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quanti conti esegue il piano. Era il conteggio delle righe gruppo/account, che oggi non
    /// esiste più: <c>TradingPlan.Groups</c> sopravvive solo per leggere i <c>plans.json</c>
    /// anteriori alla migrazione e <c>NormalizeLoadedPlan</c> lo azzera, quindi vale <c>null</c>
    /// su ogni piano che arriva dal server.
    /// </summary>
    public int Accounts { get; set; }

    public int MaxConcurrentTrades { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

/// <summary>
/// Piani di trading del workspace corrente. A differenza delle altre anagrafiche non sono globali:
/// vivono in <c>&lt;workspace&gt;/plans/plans.json</c>. Il workspace non si sceglie qui: è quello
/// selezionato nella barra in alto, e cambiandolo lo shell ricarica questa schermata.
/// </summary>
public partial class PlanListScreen : UserControl, IShellScreen
{
    private readonly List<PlanRow> _allRows = new();
    private readonly SortableBindingList<PlanRow> _visibleRows = new();
    private ShellContext? _context;

    public PlanListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
    }

    public string ScreenTitle => "Piani di trading";

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
            await ReloadPlansAsync(cancellationToken);
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

    private async Task ReloadPlansAsync(CancellationToken cancellationToken)
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
            var plans = await _context.Services.Plans.ListAsync(workspaceId, cancellationToken);
            foreach (var plan in plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase))
            {
                _allRows.Add(new PlanRow
                {
                    Status = plan.Locked ? "🔒 Bloccato" : string.Empty,
                    Locked = plan.Locked,
                    Code = plan.Code,
                    Name = plan.Name,
                    Accounts = plan.Accounts.Count,
                    MaxConcurrentTrades = plan.MaxConcurrentTrades,
                    UpdatedUtc = plan.UpdatedUtc
                });
            }

            ApplyFilter();
            _context.Navigation.SetStatus($"{_allRows.Count} piani in '{workspaceId}'.");
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
                     || row.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateDeleteAvailability();
    }

    private PlanRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    /// <summary>Un piano bloccato non si elimina: il server lo rifiuterebbe comunque.</summary>
    private void UpdateDeleteAvailability()
    {
        _toolbar.SetDeleteEnabled(SelectedRow is { Locked: false });
        _toolbar.SetExportEnabled(SelectedRow != null);
    }

    /// <summary>Le righe dei piani bloccati hanno sfondo ambra: il blocco si vede senza leggere la colonna.</summary>
    private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visibleRows.Count || e.CellStyle is null || !_visibleRows[e.RowIndex].Locked)
        {
            return;
        }

        e.CellStyle.BackColor = Color.FromArgb(255, 236, 204);
        e.CellStyle.ForeColor = Color.FromArgb(120, 60, 0);
    }

    /// <summary>Copia il piano selezionato con un codice nuovo e apre la copia.</summary>
    private async void OnDuplicateRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        using var dialog = new TextPromptDialog
        {
            Text = "Duplica piano",
            Prompt = $"Codice del nuovo piano (copia di '{row.Code}'):",
            Value = row.Code + "-B"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var copy = await _context.Services.Plans.DuplicateAsync(workspaceId, row.Code, dialog.Value, null);
            _context.Navigation.SetStatus($"Piano '{row.Code}' duplicato in '{copy.Code}'.");
            OpenDetail(copy.Code);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Duplica piano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateDeleteAvailability();

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnCreateRequested(object? sender, EventArgs e) => OpenDetail(null);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && SelectedRow is { } row)
        {
            OpenDetail(row.Code);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow is { } row)
        {
            e.Handled = true;
            OpenDetail(row.Code);
        }
    }

    private void OpenDetail(string? code)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        var detail = new PlanDetailScreen();
        detail.SetPlan(workspaceId, code);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedRow is not { Locked: false } row || SelectedWorkspaceId is not { } workspaceId)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Eliminare il piano '{row.Code}'?{Environment.NewLine}{Environment.NewLine}" +
                "Le sessioni già aperte non cambiano: ne hanno acquisito uno snapshot alla creazione.",
                "Elimina piano",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await _context.Services.Plans.DeleteAsync(workspaceId, row.Code);
            _context.Navigation.SetStatus($"Piano '{row.Code}' eliminato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }

        await ReloadPlansAsync(CancellationToken.None);
    }
}
