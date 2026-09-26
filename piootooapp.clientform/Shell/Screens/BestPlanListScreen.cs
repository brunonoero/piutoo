using System.Data;
using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// I best plan: i backtest messi in evidenza, di tutti i workspace. Unica lista del menu che non
/// dipende dal workspace scelto in alto — il workspace di ogni riga e' una colonna.
///
/// <para><b>Una colonna per anno.</b> P&amp;L e drawdown di ogni anno stanno in riga, cosi' i piani si
/// confrontano senza aprirli; gli anni sono quelli coperti da almeno un piano, quindi le colonne si
/// costruiscono a ogni caricamento. Per questo la sorgente e' una <see cref="DataTable"/> e non una
/// <see cref="SortableBindingList{T}"/>: le proprieta' di un tipo non possono crescere con gli anni,
/// e la <see cref="DataView"/> ordina per colonna da se'.</para>
///
/// <para>La miniatura della curva e' una colonna senza dato, disegnata in <c>CellPainting</c>: non si
/// ordina, come ogni colonna senza <c>DataPropertyName</c>.</para>
/// </summary>
public partial class BestPlanListScreen : UserControl, IShellScreen
{
    private const string IdColumn = "Id";
    private const string SparklineColumn = "_colSparkline";
    private const string ProfitTag = "profit";

    private readonly Dictionary<string, BestPlan> _plans = new(StringComparer.Ordinal);
    private readonly DataTable _table = new();
    private ShellContext? _context;

    public BestPlanListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _table;
    }

    public string ScreenTitle => "Best plans";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _context.Navigation.SetStatus("Caricamento dei best plan…");
        _toolbar.SetBusy(true);
        UseWaitCursor = true;
        try
        {
            var plans = await _context.Services.BestPlans.ListAsync(cancellationToken);
            _plans.Clear();
            foreach (var plan in plans)
            {
                _plans[plan.Id] = plan;
            }

            BuildColumns();
            ApplyFilter();
            _context.Navigation.SetStatus(plans.Count == 0
                ? "Nessun best plan: si promuove un backtest dal suo dettaglio, con \"Promuovi a best plan\"."
                : $"{plans.Count} best plan.");
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
            UseWaitCursor = false;
            _toolbar.SetBusy(false);
        }
    }

    private IReadOnlyList<int> Years
        => _plans.Values.SelectMany(plan => plan.Years).Select(year => year.Year).Distinct().Order().ToList();

    /// <summary>
    /// Ricostruisce tabella e colonne. L'ordinamento scelto sopravvive: e' una proprieta' della
    /// vista, e si riapplica se la colonna esiste ancora.
    /// </summary>
    private void BuildColumns()
    {
        var sort = _bindingSource.Sort;

        _bindingSource.DataSource = null;
        _grid.Columns.Clear();
        _table.Rows.Clear();
        _table.Columns.Clear();

        _table.Columns.Add(IdColumn, typeof(string));
        AddColumn("PlanCode", "Piano", typeof(string), 110);
        AddColumn("Workspace", "Workspace", typeof(string), 120);
        AddSparklineColumn();
        AddColumn("NetProfit", "P&&L", typeof(decimal), 85, "N0", ProfitTag);
        AddColumn("NetProfitPercent", "P&&L %", typeof(decimal), 60, "N1", ProfitTag);
        AddColumn("MaxDrawdownPercent", "DD %", typeof(decimal), 55, "N1",
            toolTip: "Drawdown massimo dal picco. Su una curva realizzata (run cBot) e' misurato fra chiusure: vedi la colonna Curva.");
        AddColumn("Trades", "Trade", typeof(int), 50);
        foreach (var year in Years)
        {
            AddColumn($"Y{year}Profit", $"{year} P&&L", typeof(decimal), 80, "N0", ProfitTag,
                $"P&L netto del {year}, dall'equity di fine {year - 1} (o dal capitale iniziale) a quella di fine {year}.");
            AddColumn($"Y{year}Drawdown", $"{year} DD %", typeof(decimal), 65, "N1",
                toolTip: $"Drawdown massimo del {year} in percentuale dal picco.");
        }

        AddColumn("StartUtc", "Inizio", typeof(DateTime), 80, "yyyy-MM-dd");
        AddColumn("EndUtc", "Fine", typeof(DateTime), 80, "yyyy-MM-dd");
        AddColumn("ExecutedUtc", "Eseguito (UTC)", typeof(DateTime), 110, "yyyy-MM-dd HH:mm");
        AddColumn("Strategies", "Strategie", typeof(int), 60);
        AddColumn("Origin", "Origine", typeof(string), 70);
        AddColumn("EquitySource", "Curva", typeof(string), 90,
            toolTip: "mark-to-market: equity del motore interno. realizzata: ricostruita dai trade chiusi.");
        AddColumn("BacktestFolder", "Backtest", typeof(string), 260);

        _bindingSource.DataSource = _table;
        _grid.EnableColumnSorting();
        ShellGridHelper.ConfigureReadableGrid(_grid);

        if (!string.IsNullOrEmpty(sort)
            && _table.Columns.Contains(sort.Split(' ')[0]))
        {
            _bindingSource.Sort = sort;
        }
    }

    private void AddColumn(
        string name, string header, Type type, int width, string? format = null, string? tag = null, string? toolTip = null)
    {
        _table.Columns.Add(name, type);
        var column = new DataGridViewTextBoxColumn
        {
            Name = name,
            DataPropertyName = name,
            HeaderText = header,
            Width = Math.Max(width, 20),
            ReadOnly = true,
            Tag = tag,
            ToolTipText = toolTip ?? string.Empty
        };
        if (format != null)
        {
            column.DefaultCellStyle.Format = format;
        }

        if (type == typeof(decimal) || type == typeof(int))
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        _grid.Columns.Add(column);
    }

    private void AddSparklineColumn()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = SparklineColumn,
            HeaderText = "Equity",
            Width = 150,
            ReadOnly = true,
            ToolTipText = "Curva di equity dall'inizio alla fine del backtest; tratteggio = capitale iniziale."
        });
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;
        _table.BeginLoadData();
        _table.Rows.Clear();
        foreach (var plan in _plans.Values.Where(plan => Matches(plan, filter)))
        {
            var row = _table.NewRow();
            row[IdColumn] = plan.Id;
            row["PlanCode"] = plan.PlanCode.Length > 0 ? plan.PlanCode : "(nessun piano)";
            row["Workspace"] = plan.WorkspaceName;
            row["NetProfit"] = plan.NetProfit;
            row["NetProfitPercent"] = plan.NetProfitPercent;
            row["MaxDrawdownPercent"] = plan.MaxDrawdownPercent;
            row["Trades"] = plan.TotalTrades;
            foreach (var year in plan.Years)
            {
                row[$"Y{year.Year}Profit"] = year.NetProfit;
                row[$"Y{year.Year}Drawdown"] = year.MaxDrawdownPercent;
            }

            row["StartUtc"] = (object?)plan.StartUtc ?? DBNull.Value;
            row["EndUtc"] = (object?)plan.EndUtc ?? DBNull.Value;
            row["ExecutedUtc"] = (object?)plan.ExecutedUtc ?? DBNull.Value;
            row["Strategies"] = plan.Strategies.Count;
            row["Origin"] = DescribeOrigin(plan.Origin);
            row["EquitySource"] = plan.EquitySource;
            row["BacktestFolder"] = plan.BacktestFolder;
            _table.Rows.Add(row);
        }

        _table.EndLoadData();
        UpdateCommandAvailability();
    }

    private static bool Matches(BestPlan plan, string filter)
        => filter.Length == 0
           || plan.PlanCode.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || plan.PlanName.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || plan.WorkspaceName.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || plan.WorkspaceId.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || plan.BacktestFolder.Contains(filter, StringComparison.OrdinalIgnoreCase)
           || plan.Strategies.Any(strategy => strategy.StrategyCode.Contains(filter, StringComparison.OrdinalIgnoreCase));

    internal static string DescribeOrigin(BacktestOrigin origin) => origin switch
    {
        BacktestOrigin.Internal => "interno",
        BacktestOrigin.ExternalBroker => "cBot",
        _ => "ignota"
    };

    private BestPlan? PlanAt(int rowIndex)
        => rowIndex >= 0
           && rowIndex < _grid.Rows.Count
           && _grid.Rows[rowIndex].DataBoundItem is DataRowView view
           && view[IdColumn] is string id
           && _plans.TryGetValue(id, out var plan)
            ? plan
            : null;

    private BestPlan? SelectedPlan => PlanAt(_grid.CurrentRow?.Index ?? -1);

    private void UpdateCommandAvailability() => _toolbar.SetDeleteEnabled(SelectedPlan != null);

    // --- eventi -----------------------------------------------------------

    private void OnGridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != SparklineColumn
            || e.Graphics is null)
        {
            return;
        }

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
        if (PlanAt(e.RowIndex) is { } plan)
        {
            var bounds = Rectangle.Inflate(e.CellBounds, -6, -5);
            EquityChart.DrawSparkline(e.Graphics, bounds, plan.Equity, plan.InitialCapital);
        }

        e.Handled = true;
    }

    private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Tag as string != ProfitTag
            || e.Value is not decimal value || e.CellStyle is null)
        {
            return;
        }

        e.CellStyle.ForeColor = value < 0 ? Color.Firebrick : Color.ForestGreen;
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateCommandAvailability();

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && PlanAt(e.RowIndex) is { } plan)
        {
            OpenDetail(plan);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedPlan is { } plan)
        {
            e.Handled = true;
            OpenDetail(plan);
        }
    }

    private void OpenDetail(BestPlan plan)
    {
        if (_context == null)
        {
            return;
        }

        var detail = new BestPlanDetailScreen();
        detail.SetBestPlan(plan.Id, plan.PlanCode);
        _context.Navigation.Push(detail);
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedPlan is not { } plan)
        {
            return;
        }

        if (!await BestPlanDetailScreen.ConfirmAndDeleteAsync(this, _context, plan))
        {
            return;
        }

        await LoadAsync(CancellationToken.None);
    }
}
