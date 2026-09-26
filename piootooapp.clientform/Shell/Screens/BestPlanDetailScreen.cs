using System.Text;
using Piootoo.Shared.Models.BestPlans;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Dettaglio di un best plan, in sola lettura: e' la fotografia di un backtest, e le sue cifre non
/// si ricalcolano. Curva di equity con il drawdown, resoconto per anno, strategie del piano e
/// provenienza — tutto dalla copia sotto <c>best-plans</c>, non dalla cartella del backtest, che
/// nel frattempo puo' essere stata cancellata.
/// </summary>
public partial class BestPlanDetailScreen : UserControl, IShellScreen
{
    private readonly SortableBindingList<BestPlanYear> _years = new();
    private readonly SortableBindingList<BestPlanStrategy> _strategies = new();
    private ShellContext? _context;
    private BestPlan? _plan;
    private string _id = string.Empty;
    private string _title = "Best plan";

    public BestPlanDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _yearsBindingSource.DataSource = _years;
        _strategiesBindingSource.DataSource = _strategies;
        _yearsGrid.EnableColumnSorting();
        _strategiesGrid.EnableColumnSorting();
    }

    public string ScreenTitle => _title;

    public void SetBestPlan(string id, string planCode)
    {
        _id = id;
        _title = planCode.Length > 0 ? planCode : id;
        _toolbar.Title = _title;
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _id.Length == 0)
        {
            return;
        }

        _context.Navigation.SetStatus($"Caricamento del best plan {_title}…");
        _toolbar.SetBusy(true);
        UseWaitCursor = true;
        try
        {
            _plan = await _context.Services.BestPlans.GetAsync(_id, cancellationToken);
            Bind(_plan);
            _reportButton.Enabled = _plan.HasHtmlReport;
            _removeButton.Enabled = true;
            _context.Navigation.SetStatus($"Best plan {_title}: {_plan.Strategies.Count} strategie, {_plan.TotalTrades} trade.");
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

    private void Bind(BestPlan plan)
    {
        var winRate = plan.TotalTrades > 0 ? (decimal)plan.WinningTrades / plan.TotalTrades : 0m;
        _headlineLabel.Text =
            $"{Describe(plan)}   ·   {plan.WorkspaceName}{Environment.NewLine}" +
            $"{plan.StartUtc:yyyy-MM-dd} → {plan.EndUtc:yyyy-MM-dd}   ·   " +
            $"P&L {plan.NetProfit:N2} ({plan.NetProfitPercent:N1}%)   ·   " +
            $"Max DD {plan.MaxDrawdown:N2} ({plan.MaxDrawdownPercent:N1}%)   ·   " +
            $"{plan.TotalTrades} trade, {winRate:P0} vincenti   ·   curva {plan.EquitySource}";

        _chart.SetData(plan.Equity, plan.InitialCapital);

        Fill(_years, plan.Years);
        Fill(_strategies, plan.Strategies);
        _infoBox.Text = DescribeProvenance(plan);
    }

    private static string Describe(BestPlan plan)
        => plan.PlanName.Length > 0 && plan.PlanName != plan.PlanCode
            ? $"{plan.PlanCode} — {plan.PlanName}"
            : plan.PlanCode.Length > 0 ? plan.PlanCode : "(run senza piano)";

    private static void Fill<T>(SortableBindingList<T> target, IEnumerable<T> source)
    {
        target.RaiseListChangedEvents = false;
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }

        target.RaiseListChangedEvents = true;
        target.ReapplySort();
        target.ResetBindings();
    }

    private static string DescribeProvenance(BestPlan plan)
    {
        var text = new StringBuilder();
        void Line(string label, object? value) => text.AppendLine($"{label,-22}{value}");

        Line("Piano", Describe(plan));
        Line("Workspace", $"{plan.WorkspaceName} ({plan.WorkspaceId})");
        Line("Backtest", plan.BacktestFolder);
        Line("Origine", BestPlanListScreen.DescribeOrigin(plan.Origin));
        Line("Prezzi", plan.PriceSource);
        Line("Versione", plan.Version);
        Line("Eseguito (UTC)", $"{plan.ExecutedUtc:yyyy-MM-dd HH:mm}");
        Line("Periodo (UTC)", $"{plan.StartUtc:yyyy-MM-dd HH:mm} → {plan.EndUtc:yyyy-MM-dd HH:mm}");
        Line("Promosso (UTC)", $"{plan.PromotedUtc:yyyy-MM-dd HH:mm}");
        Line("Capitale iniziale", $"{plan.InitialCapital:N2}");
        Line("Equity finale", $"{plan.FinalEquity:N2}");
        Line("Curva", plan.EquitySource == "realizzata"
            ? "realizzata — ricostruita dai trade chiusi, drawdown fra chiusure"
            : plan.EquitySource);
        text.AppendLine();
        text.AppendLine("Artefatti copiati (best-plans\\" + plan.Id + "\\artifacts):");
        foreach (var artifact in plan.Artifacts)
        {
            text.AppendLine("  " + artifact);
        }

        if (plan.Artifacts.Contains("plan.json"))
        {
            text.AppendLine();
            text.AppendLine("plan.json e' il piano com'era al momento della promozione, non per forza al momento del run.");
        }

        return text.ToString();
    }

    /// <summary>
    /// Chiede conferma e toglie il best plan. Condiviso con la lista: il messaggio deve dire le stesse
    /// cose da entrambe le parti, in particolare che il backtest non si tocca.
    /// </summary>
    internal static async Task<bool> ConfirmAndDeleteAsync(IWin32Window owner, ShellContext context, BestPlan plan)
    {
        var nl = Environment.NewLine;
        if (MessageBox.Show(
                owner,
                $"Togliere '{Describe(plan)}' dai best plan?{nl}{nl}" +
                $"Si cancella la copia sotto best-plans ({plan.Id}): cifre, curva e artefatti copiati. " +
                "La cartella di backtest nel workspace, se c'e' ancora, non si tocca.",
                "Rimuovi best plan",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return false;
        }

        try
        {
            await context.Services.BestPlans.DeleteAsync(plan.Id);
            context.Navigation.SetStatus($"'{Describe(plan)}' tolto dai best plan.");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "Rimuovi best plan", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // --- eventi -----------------------------------------------------------

    private async void OnReportClick(object? sender, EventArgs e)
    {
        if (_context == null || _plan == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await HtmlReportViewerForm.ShowFromUriAsync(
                FindForm()!, _context.Services.Http, _context.Services.BestPlans.GetReportUri(_plan.Id),
                $"Report {_title}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Report HTML", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private async void OnRemoveClick(object? sender, EventArgs e)
    {
        if (_context == null || _plan == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            if (await ConfirmAndDeleteAsync(this, _context, _plan))
            {
                _context.Navigation.GoBack();
            }
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private void OnProfitCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.ColumnIndex < 0 || e.CellStyle is null
            || e.Value is not decimal value)
        {
            return;
        }

        var property = grid.Columns[e.ColumnIndex].DataPropertyName;
        if (property is "NetProfit" or "ReturnPercent")
        {
            e.CellStyle.ForeColor = value < 0 ? Color.Firebrick : Color.ForestGreen;
        }
    }

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    // --- costruzione delle griglie (usate da InitializeComponent) ---------

    private static DataGridViewTextBoxColumn NewColumn(
        string property, string header, float weight, string? format = null, bool right = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            FillWeight = weight,
            Name = "_col" + property,
            ReadOnly = true
        };
        if (format != null)
        {
            column.DefaultCellStyle.Format = format;
        }

        if (right)
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        return column;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Dock = DockStyle.Fill;
        grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }
}
