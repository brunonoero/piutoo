using System.Text.RegularExpressions;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Un broker workspace: i suoi piani in produzione, attivi e ritirati, con la scheda del piano
/// selezionato. Sola lettura, come ogni piano di produzione: si cambia con <i>Duplica con altri
/// conti…</i> (il broker ha cambiato il numero di conto, o si passa dal demo al conto vero) e con
/// <i>Ritira piano</i>. Strategie, pesi e tenuta non si toccano qui: vengono dal run misurato, e un
/// piano diverso si promuove da un best plan diverso.
/// </summary>
public partial class BrokerWorkspaceDetailScreen : UserControl, IShellScreen
{
    private readonly SortableBindingList<ProductionPlanRow> _plans = new();
    private ShellContext? _context;
    private string _brokerCode = string.Empty;

    public BrokerWorkspaceDetailScreen()
    {
        InitializeComponent();
        _grid.EnableColumnSorting();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _plans;
    }

    public string ScreenTitle => _brokerCode.Length > 0 ? _brokerCode : "Broker workspace";

    public void SetBroker(string brokerCode)
    {
        _brokerCode = brokerCode;
        _toolbar.Title = $"Produzione {brokerCode}";
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _brokerCode.Length == 0)
        {
            return;
        }

        var selected = SelectedPlan?.Code;
        _context.Navigation.SetStatus($"Caricamento dei piani in produzione di {_brokerCode}…");
        SetBusy(true);
        try
        {
            var detail = await _context.Services.BrokerWorkspaces.GetAsync(_brokerCode, cancellationToken);
            _toolbar.Title = $"Produzione {detail.BrokerCode} — {detail.BrokerName}";

            _plans.RaiseListChangedEvents = false;
            _plans.Clear();
            foreach (var plan in detail.Plans)
            {
                _plans.Add(new ProductionPlanRow(plan));
            }

            _plans.RaiseListChangedEvents = true;
            _plans.ReapplySort();
            _plans.ResetBindings();
            Reselect(selected);
            ShowSelected();

            var active = detail.Plans.Count(plan => plan.RetiredUtc is null);
            _context.Navigation.SetStatus(
                $"{detail.BrokerCode}: {active} piani attivi, {detail.Plans.Count - active} ritirati.");
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
            SetBusy(false);
        }
    }

    private TradingPlan? SelectedPlan
        => _grid.CurrentRow is { Index: >= 0 } row && row.Index < _plans.Count ? _plans[row.Index].Plan : null;

    private void Reselect(string? code)
    {
        if (code == null)
        {
            return;
        }

        for (var index = 0; index < _plans.Count; index++)
        {
            if (_plans[index].Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
                return;
            }
        }
    }

    private void ShowSelected()
    {
        var plan = SelectedPlan;
        _infoBox.Text = plan == null
            ? "Seleziona un piano."
            : ProductionPlanText.Describe(plan).Replace("\n", Environment.NewLine);
        _duplicateButton.Enabled = plan != null;
        _retireButton.Enabled = plan is { RetiredUtc: null };
    }

    private void SetBusy(bool busy)
    {
        _toolbar.SetBusy(busy);
        UseWaitCursor = busy;
        if (busy)
        {
            _duplicateButton.Enabled = false;
            _retireButton.Enabled = false;
        }
        else
        {
            ShowSelected();
        }
    }

    /// <summary>Il codice del duplicato: <c>-2</c> in coda, o il numero dopo quello che c'e' gia'.</summary>
    internal static string NextCode(string code)
    {
        var match = Regex.Match(code, @"^(?<base>.+)-(?<n>\d+)$");
        return match.Success && int.TryParse(match.Groups["n"].Value, out var n)
            ? $"{match.Groups["base"].Value}-{n + 1}"
            : $"{code}-2";
    }

    // --- eventi -----------------------------------------------------------

    private void OnSelectionChanged(object? sender, EventArgs e) => ShowSelected();

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnDuplicateClick(object? sender, EventArgs e)
    {
        if (_context == null || SelectedPlan is not { } plan)
        {
            return;
        }

        using var dialog = new ProductionPlanDialog
        {
            Text = $"Duplica {plan.Code}",
            Intro = $"Un piano uguale a {plan.Code} — stesse strategie, pesi e tenuta — sui conti che indichi. " +
                    (plan.RetiredUtc is null
                        ? $"{plan.Code} viene ritirato nello stesso passaggio: la sua sessione resta, ma non ne apre di nuove."
                        : $"{plan.Code} e' gia' ritirato."),
            ConfirmText = "Duplica e ritira",
            PlanCode = NextCode(plan.Code),
            PlanName = plan.Name,
            AccountsHint = "numeri di conto, separati da virgola (obbligatori)"
        };
        var services = _context.Services;
        dialog.SetSubmit(dryRun => services.BrokerWorkspaces.DuplicateAsync(_brokerCode, plan.Code, new DuplicateProductionPlanRequest
        {
            NewCode = dialog.PlanCode,
            NewName = dialog.PlanName,
            Accounts = dialog.Accounts,
            DryRun = dryRun
        }));

        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is not { } created)
        {
            return;
        }

        _context.Navigation.SetStatus(
            $"{created.Code} creato sui conti {string.Join(", ", created.Accounts)}, {plan.Code} ritirato. " +
            $"Sull'istanza cTrader del conto nuovo il codice piano e' {created.Code}.");
        await LoadAsync(CancellationToken.None);
        Reselect(created.Code);
        ShowSelected();
    }

    private async void OnRetireClick(object? sender, EventArgs e)
    {
        if (_context == null || SelectedPlan is not { RetiredUtc: null } plan)
        {
            return;
        }

        var nl = Environment.NewLine;
        if (MessageBox.Show(
                this,
                $"Ritirare {plan.Code}?{nl}{nl}" +
                $"Il conto {string.Join(", ", plan.Accounts)} non potra' aprire sessioni realtime nuove su questo piano. " +
                $"Una sessione gia' aperta resta: il cBot ci rientra e le sue posizioni restano sorvegliate.{nl}{nl}" +
                $"Le sue {plan.EnabledStrategies?.Count ?? 0} strategie tornano libere per un altro piano. " +
                "Il ritiro non si annulla.",
                "Ritira piano",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        SetBusy(true);
        try
        {
            await _context.Services.BrokerWorkspaces.RetireAsync(_brokerCode, plan.Code);
            _context.Navigation.SetStatus($"{plan.Code} ritirato.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ritira piano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }

        await LoadAsync(CancellationToken.None);
    }

    /// <summary>Riga della griglia: le colonne ordinabili di un piano di produzione.</summary>
    private sealed class ProductionPlanRow(TradingPlan plan)
    {
        public TradingPlan Plan { get; } = plan;
        public string Code => Plan.Code;
        public string Name => Plan.Name;
        public string State => Plan.RetiredUtc is null ? "attivo" : "ritirato";
        public string Accounts => string.Join(", ", Plan.Accounts);
        public int Strategies => Plan.EnabledStrategies?.Count ?? 0;
        public decimal SizeMultiplier => Plan.SizeMultiplier;
        public DateTime? PromotedUtc => Plan.Provenance?.PromotedUtc;
        public DateTime? RetiredUtc => Plan.RetiredUtc;
        public string Replaces => Plan.Provenance?.PreviousPlanCode ?? string.Empty;
        public string SourcePlan => Plan.Provenance?.SourcePlanCode ?? string.Empty;
    }
}
