using System.ComponentModel;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Riga del tab Gruppi. <see cref="TradingGroupRow"/> è init-only, quindi non è bindabile
/// direttamente alla griglia.
/// </summary>
public sealed class PlanGroupEditRow
{
    public string GroupId { get; set; } = string.Empty;
}

/// <summary>Riga del tab Account: account cTrader con il proprio limite di posizioni concorrenti.</summary>
public sealed class PlanAccountEditRow
{
    /// <summary>
    /// Sola lettura in griglia: deriva dal registro (<see cref="Piootoo.Shared.Models.Workspaces.WorkspaceAccount.GroupId"/>)
    /// al momento in cui si sceglie <see cref="AccountNumber"/>, non è un campo scelto dall'utente.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public int MaxConcurrentTrades { get; set; }

    /// <summary>
    /// Cosa conta <see cref="MaxConcurrentTrades"/>: solo le posizioni riempite, oppure anche gli
    /// ordini pendenti. Vedi <c>docs/domini/distribuzione-multi-account.md</c> §2.
    ///
    /// <para>È il <b>nome</b> del valore di <see cref="Piootoo.Shared.Models.Trading.ConcurrencyCountMode"/>,
    /// non il valore: <c>DataGridViewComboBoxColumn</c> confronta la cella con il ValueMember, che
    /// per <c>ValueComboItem</c> è una stringa. Tenendo qui l'enum il binding dovrebbe convertire a
    /// ogni cella, ed è esattamente il genere di conversione che finisce in <c>DataError</c>.</para>
    /// </summary>
    public string ConcurrencyCountMode { get; set; } =
        nameof(Piootoo.Shared.Models.Trading.ConcurrencyCountMode.PositionsAndPendingOrders);
}

/// <summary>
/// Riga dell'avviso: una strategia del masterfilter che il piano taglierebbe. Sola lettura, e
/// ordinabile come ogni elenco della console.
/// </summary>
public sealed class PlanHoldingConflictRow
{
    public string Strategy { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public string Holding { get; set; } = string.Empty;

    public string Effect { get; set; } = string.Empty;
}

/// <summary>
/// Dettaglio di un piano di trading. Il salvataggio riscrive il piano intero, quindi la schermata
/// deve esporre tutto ciò che il piano contiene: quello che non è modificabile qui verrebbe
/// riportato ai default alla prima modifica.
///
/// <para>Il piano è editato in due griglie separate che rispecchiano la semantica di
/// <see cref="TradingGroupRow"/>: il tab Gruppi elenca i gruppi, il tab Account porta
/// <c>MaxConcurrentTrades</c>, che è per-account (vedi <c>TradingSessionService.GetNextSignalForAccount</c>).
/// Il salvataggio ricompone le due griglie in <see cref="TradingGroupRow"/> per riga account.</para>
/// </summary>
public partial class PlanDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    // Griglie editabili: restano BindingList<T> e non ordinabili per colonna, perché l'ordine è
    // quello in cui si sta scrivendo. Vedi .cursor/rules/piutoo-console-screens.mdc.
    private readonly BindingList<PlanGroupEditRow> _groups = new();
    private readonly BindingList<PlanAccountEditRow> _accounts = new();

    /// <summary>Registro globale (<c>api/Accounts</c>), non del workspace: si carica una volta.</summary>
    private readonly List<string> _accountGroups = new();
    private readonly List<WorkspaceAccount> _registryAccounts = new();

    /// <summary>
    /// Catalogo strategie del server: serve a sapere quali strategie del masterfilter sono
    /// multiday, cioe' quali il piano taglierebbe. Si carica una volta, come il registro account.
    /// </summary>
    private readonly List<StrategyCatalogItem> _catalog = new();

    /// <summary>Id del masterfilter del workspace scelto: cambia con la combo workspace.</summary>
    private readonly List<string> _masterFilter = new();

    private readonly SortableBindingList<PlanHoldingConflictRow> _conflicts = new();
    private ShellContext? _context;
    private string _workspaceId = string.Empty;
    private string? _code;
    private bool _isNew;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public PlanDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _groupsBindingSource.DataSource = _groups;
        _accountsBindingSource.DataSource = _accounts;
        _enforceConcurrencyCombo.Items.AddRange(new object[]
        {
            "Default (come da storico)",
            "Sì, applica i limiti",
            "No, ignora i limiti"
        });
        _enforceConcurrencyCombo.SelectedIndex = 0;

        // Cosa conta il massimo di posizioni contemporanee. Le etichette dicono la conseguenza
        // operativa, non il nome del contratto: "PositionsOnly" da solo non fa capire che gli stop
        // pendenti restano tutti a mercato finché uno non entra.
        _colAccountCountMode.DisplayMember = nameof(ValueComboItem.Display);
        _colAccountCountMode.ValueMember = nameof(ValueComboItem.Id);
        _colAccountCountMode.DataSource = new List<ValueComboItem>
        {
            ValueComboItem.Of(
                nameof(ConcurrencyCountMode.PositionsAndPendingOrders),
                "Posizioni + ordini pendenti"),
            ValueComboItem.Of(
                nameof(ConcurrencyCountMode.PositionsOnly),
                "Solo posizioni riempite")
        };

        _groups.ListChanged += (_, _) => MarkDirty();
        _accounts.ListChanged += (_, _) => MarkDirty();

        _conflictsBindingSource.DataSource = _conflicts;
        _conflictsGrid.EnableColumnSorting();
    }

    public string ScreenTitle => _isNew
        ? "Nuovo piano"
        : _code is { Length: > 0 } code ? $"Piano {code}" : "Piano";

    public bool HasUnsavedChanges => _isDirty;

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. <paramref name="code"/> null = nuovo piano.</summary>
    public void SetPlan(string workspaceId, string? code)
    {
        _workspaceId = workspaceId;
        _code = code;
        _isNew = string.IsNullOrEmpty(code);
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _suspendDirtyTracking = true;
        _toolbar.SetBusy(true);
        try
        {
            ShowWorkspace();
            await LoadAccountRegistryAsync(cancellationToken);
            await LoadStrategyCatalogAsync(cancellationToken);
            if (_isNew)
            {
                _toolbar.Title = "Nuovo piano";
                _codeTextBox.ReadOnly = false;
                ResetToDefaults();
                await RefreshGroupChoicesAsync(cancellationToken);
                await RefreshHoldingImpactAsync(cancellationToken);
                _context.Navigation.SetStatus($"Nuovo piano nel workspace '{_workspaceId}'.");
                return;
            }

            var plan = await _context.Services.Plans.GetAsync(_workspaceId, _code!, cancellationToken);
            _toolbar.Title = $"Piano {plan.Code}";
            _codeTextBox.ReadOnly = true;
            Fill(plan);
            await RefreshGroupChoicesAsync(cancellationToken);
            await RefreshHoldingImpactAsync(cancellationToken);
            _context.Navigation.SetStatus(
                $"Piano '{plan.Code}' con {plan.Groups.Count} righe gruppo/account, " +
                $"aggiornato il {plan.UpdatedUtc:yyyy-MM-dd HH:mm} UTC.");
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
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// Mostra il workspace del piano, in sola lettura. Non è più una scelta di questa schermata:
    /// un piano esistente vive in <c>&lt;workspace&gt;/plans/plans.json</c> e spostarlo non è una
    /// modifica di campo; un piano nuovo nasce nel workspace corrente, quello della barra in alto.
    /// </summary>
    private void ShowWorkspace()
    {
        if (_context == null)
        {
            return;
        }

        if (_isNew)
        {
            _workspaceId = _context.Services.Workspaces.CurrentId ?? string.Empty;
        }

        _workspaceValueLabel.Text = _workspaceId.Length > 0
            ? _workspaceId
            : "(nessun workspace selezionato)";
    }

    private string? SelectedWorkspaceId => _workspaceId.Length > 0 ? _workspaceId : null;

    /// <summary>
    /// Gruppi e account stanno in un registro globale (<c>api/Accounts</c>), non nel workspace:
    /// una sola lettura serve tutte le righe del piano.
    /// </summary>
    private async Task LoadAccountRegistryAsync(CancellationToken cancellationToken)
    {
        _accountGroups.Clear();
        _registryAccounts.Clear();
        try
        {
            var groups = await _context!.Services.Api.ListAccountGroupsAsync(cancellationToken);
            _accountGroups.AddRange(groups
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .OrderBy(group => group, StringComparer.OrdinalIgnoreCase));

            var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
            _registryAccounts.AddRange(accounts
                .Where(account => !string.IsNullOrWhiteSpace(account.AccountNumber))
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Registro gruppi/account non leggibile: {ex.Message}");
        }
    }

    private Task RefreshGroupChoicesAsync(CancellationToken cancellationToken)
    {
        RefreshGroupColumnItems();
        RefreshAccountNumberColumnItems();
        return Task.CompletedTask;
    }

    private static void SetColumnItems(
        DataGridViewComboBoxColumn column,
        string blankLabel,
        IEnumerable<ValueComboItem> known,
        IEnumerable<string> usedValues)
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank(blankLabel) };
        items.AddRange(known);
        foreach (var value in usedValues)
        {
            if (!string.IsNullOrWhiteSpace(value) && !ContainsId(items, value))
            {
                items.Add(ValueComboItem.Missing(value));
            }
        }

        column.DisplayMember = nameof(ValueComboItem.Display);
        column.ValueMember = nameof(ValueComboItem.Id);
        column.DataSource = items;
    }

    private void RefreshGroupColumnItems()
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun gruppo)") };
        foreach (var group in _accountGroups)
        {
            items.Add(ValueComboItem.Of(group, group));
        }

        // I gruppi già scritti nel piano ma spariti dal registro restano selezionabili: il
        // salvataggio riscrive il piano intero, quindi scartarli qui li cancellerebbe.
        foreach (var row in _groups)
        {
            if (!string.IsNullOrWhiteSpace(row.GroupId) && !ContainsId(items, row.GroupId))
            {
                items.Add(ValueComboItem.Missing(row.GroupId));
            }
        }

        _colGroupId.DisplayMember = nameof(ValueComboItem.Display);
        _colGroupId.ValueMember = nameof(ValueComboItem.Id);
        _colGroupId.DataSource = items;
    }

    /// <summary>
    /// La colonna Account cTrader propone tutto il registro: il gruppo non si sceglie più a parte,
    /// deriva dall'account scelto (<see cref="OnAccountsGridCellValueChanged"/>), quindi qui non
    /// c'è nulla da filtrare per riga.
    /// </summary>
    private void RefreshAccountNumberColumnItems()
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun account)") };
        foreach (var account in _registryAccounts)
        {
            items.Add(ValueComboItem.Of(
                account.AccountNumber,
                $"{account.Name}  ·  {account.AccountNumber}"));
        }

        // Un account scritto nel piano ma sparito dal registro resta selezionabile: il salvataggio
        // riscrive il piano intero, quindi scartarlo qui lo cancellerebbe.
        foreach (var row in _accounts)
        {
            if (!string.IsNullOrWhiteSpace(row.AccountNumber) && !ContainsId(items, row.AccountNumber))
            {
                items.Add(ValueComboItem.Missing(row.AccountNumber));
            }
        }

        _colAccountNumber.DisplayMember = nameof(ValueComboItem.Display);
        _colAccountNumber.ValueMember = nameof(ValueComboItem.Id);
        _colAccountNumber.DataSource = items;
    }

    private static bool ContainsId(IEnumerable<ValueComboItem> items, string id)
        => items.Any(item => string.Equals(item.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Senza questo una combo di griglia notifica il cambio solo all'uscita dalla cella.</summary>
    private void OnGroupsGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_groupsGrid.IsCurrentCellDirty && _groupsGrid.CurrentCell is DataGridViewComboBoxCell)
        {
            _groupsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OnGroupsGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        // Nessun effetto collaterale sulle altre colonne/griglie: il gruppo del tab Account deriva
        // dall'account scelto (registro), non da questa griglia.
    }

    private void OnAccountsGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_accountsGrid.IsCurrentCellDirty && _accountsGrid.CurrentCell is DataGridViewComboBoxCell)
        {
            _accountsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    /// <summary>
    /// Il Gruppo del tab Account è sola lettura: deriva dall'account appena scelto (registro
    /// <c>api/Accounts</c>), non è un campo editabile separatamente.
    /// </summary>
    private void OnAccountsGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _accounts.Count || e.ColumnIndex != _colAccountNumber.Index)
        {
            return;
        }

        var row = _accounts[e.RowIndex];
        var matched = _registryAccounts.FirstOrDefault(account => string.Equals(
            account.AccountNumber, row.AccountNumber, StringComparison.OrdinalIgnoreCase));
        row.GroupId = matched?.GroupId ?? string.Empty;
        _accounts.ResetItem(e.RowIndex);
    }

    /// <summary>
    /// Un valore fuori lista farebbe comparire un dialog di errore per cella. Le voci orfane sono
    /// già iniettate, quindi qui si segnala e basta.
    /// </summary>
    private void OnGroupsGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        _context?.Navigation.SetError($"Valore non valido nella griglia gruppi: {e.Exception.Message}");
    }

    private void OnAccountsGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        _context?.Navigation.SetError($"Valore non valido nella griglia account: {e.Exception.Message}");
    }

    private void ResetToDefaults()
    {
        _codeTextBox.Text = string.Empty;
        _nameTextBox.Text = string.Empty;
        _commissionInput.Value = 2m;
        _enforceConcurrencyCombo.SelectedIndex = 0;

        FillHolding(AccountHoldingPolicy.Default);

        _clampMultipliersCheckBox.Checked = true;
        _volatilityEnabledCheckBox.Checked = false;
        _atrPeriodsInput.Value = 14;
        _targetRiskInput.Value = 1_000m;
        _portfolioRiskEnabledCheckBox.Checked = false;
        _maxDrawdownInput.Value = 0.20m;
        _maxGrossExposureInput.Value = 1m;
        _aggressiveModulesCheckBox.Checked = false;
        _fractionalFactorInput.Value = 0.25m;
        _maximumMultiplierInput.Value = 1m;

        _groups.Clear();
        _accounts.Clear();
    }

    private void Fill(TradingPlan plan)
    {
        _codeTextBox.Text = plan.Code;
        _nameTextBox.Text = plan.Name;
        _commissionInput.Value = Clamp(_commissionInput, plan.CommissionPerContract);
        _enforceConcurrencyCombo.SelectedIndex = plan.EnforceConcurrencyLimits switch
        {
            null => 0,
            true => 1,
            false => 2
        };

        FillHolding(plan.Holding);

        var sizing = plan.PositionSizing;
        _clampMultipliersCheckBox.Checked = sizing.ClampMultipliersToUnitInterval;
        _volatilityEnabledCheckBox.Checked = sizing.MarketVolatility.Enabled;
        _atrPeriodsInput.Value = Math.Clamp(sizing.MarketVolatility.AtrPeriods, 1, 1000);
        _targetRiskInput.Value = Clamp(_targetRiskInput, sizing.MarketVolatility.TargetRiskDollars);
        _portfolioRiskEnabledCheckBox.Checked = sizing.PortfolioRisk.Enabled;
        _maxDrawdownInput.Value = Clamp(_maxDrawdownInput, sizing.PortfolioRisk.MaximumDrawdown);
        _maxGrossExposureInput.Value = Clamp(_maxGrossExposureInput, sizing.PortfolioRisk.MaximumGrossExposure);
        _aggressiveModulesCheckBox.Checked = sizing.PortfolioRisk.EnableAggressiveModules;
        _fractionalFactorInput.Value = Clamp(_fractionalFactorInput, sizing.PortfolioRisk.FractionalFactor);
        _maximumMultiplierInput.Value = Clamp(_maximumMultiplierInput, sizing.PortfolioRisk.MaximumMultiplier);

        _groups.RaiseListChangedEvents = false;
        _groups.Clear();
        foreach (var group in plan.Groups.GroupBy(row => row.GroupId, StringComparer.OrdinalIgnoreCase))
        {
            _groups.Add(new PlanGroupEditRow { GroupId = group.Key });
        }

        _groups.RaiseListChangedEvents = true;
        _groups.ResetBindings();

        _accounts.RaiseListChangedEvents = false;
        _accounts.Clear();
        foreach (var row in plan.Groups)
        {
            _accounts.Add(new PlanAccountEditRow
            {
                GroupId = row.GroupId,
                AccountNumber = row.AccountNumber,
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                ConcurrencyCountMode = row.ConcurrencyCountMode.ToString()
            });
        }

        _accounts.RaiseListChangedEvents = true;
        _accounts.ResetBindings();
    }

    /// <summary>
    /// Il catalogo strategie del server. Serve solo all'avviso di questa schermata: senza sapere
    /// quali strategie sono multiday non si puo' dire quali il piano taglierebbe.
    /// </summary>
    private async Task LoadStrategyCatalogAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _catalog.Count > 0)
        {
            return;
        }

        try
        {
            _catalog.AddRange(await _context.Services.Api.ListStrategiesAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // L'avviso e' un di piu': se il catalogo non arriva, il piano resta salvabile e
            // l'etichetta lo dichiara, invece di far fallire l'intera schermata.
            _catalog.Clear();
        }
    }

    private void FillHolding(AccountHoldingPolicy holding)
    {
        _allowOvernightCheckBox.Checked = holding.AllowOvernight;
        _allowOverweekCheckBox.Checked = holding.AllowOverweek;
        _sessionFlatInput.Value = Math.Clamp(holding.SessionFlatUtcHhmm, 0, 2359);
        _weekEndFromInput.Value = Math.Clamp(holding.WeekEnd.FromUtcHhmm, 0, 2359);
        _weekEndUntilInput.Value = Math.Clamp(holding.WeekEnd.UntilUtcHhmm, 0, 2359);
        ApplyHoldingEnablement();
    }

    private AccountHoldingPolicy ReadHolding() => new()
    {
        AllowOvernight = _allowOvernightCheckBox.Checked,
        AllowOverweek = _allowOverweekCheckBox.Checked,
        SessionFlatUtcHhmm = (int)_sessionFlatInput.Value,
        WeekEnd = new WeekEndFlatPolicy((int)_weekEndFromInput.Value, (int)_weekEndUntilInput.Value)
    };

    /// <summary>
    /// Un orario di taglio ha senso solo se quel taglio esiste: mostrare un campo attivo che non
    /// governa nulla fa credere che il piano stia facendo qualcosa che non fa.
    ///
    /// <para>Overweek segue overnight anche qui: tenere il fine settimana senza tenere la notte non
    /// descrive alcun conto reale, e il server rifiuterebbe la combinazione al salvataggio.</para>
    /// </summary>
    private void ApplyHoldingEnablement()
    {
        _sessionFlatInput.Enabled = !_allowOvernightCheckBox.Checked;
        _sessionFlatLabel.Enabled = !_allowOvernightCheckBox.Checked;

        _allowOverweekCheckBox.Enabled = _allowOvernightCheckBox.Checked;
        if (!_allowOvernightCheckBox.Checked && _allowOverweekCheckBox.Checked)
        {
            _allowOverweekCheckBox.Checked = false;
        }

        var weekEndCuts = !_allowOverweekCheckBox.Checked;
        _weekEndFromInput.Enabled = weekEndCuts;
        _weekEndFromLabel.Enabled = weekEndCuts;
        _weekEndUntilInput.Enabled = weekEndCuts;
        _weekEndUntilLabel.Enabled = weekEndCuts;
    }

    private void OnHoldingChanged(object? sender, EventArgs e)
    {
        ApplyHoldingEnablement();
        MarkDirty();
        UpdateHoldingImpact();
    }

    /// <summary>
    /// Ricarica il masterfilter del workspace corrente e ricalcola l'avviso. Il masterfilter e' del
    /// workspace, quindi va riletto ogni volta che il workspace cambia.
    /// </summary>
    private async Task RefreshHoldingImpactAsync(CancellationToken cancellationToken)
    {
        _masterFilter.Clear();
        if (_context != null && !string.IsNullOrWhiteSpace(_workspaceId))
        {
            try
            {
                var filter = await _context.Services.Api.GetMasterFilterAsync(_workspaceId, cancellationToken);
                _masterFilter.AddRange(filter.StrategiesFilter);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Come per il catalogo: l'avviso e' diagnostica, non una precondizione del piano.
            }
        }

        UpdateHoldingImpact();
    }

    /// <summary>
    /// L'avviso: quali strategie del masterfilter questo piano taglierebbe, e come.
    ///
    /// <para>Il conflitto lo calcola <see cref="HoldingResolver.FindConflicts"/>, cioe' la stessa
    /// regola che poi esegue il motore: se la schermata se la riscrivesse per conto proprio,
    /// mostrerebbe prima o poi un elenco diverso da quello che il run produce — ed e' esattamente
    /// il tipo di divergenza che questa gerarchia esiste per chiudere.</para>
    /// </summary>
    private void UpdateHoldingImpact()
    {
        _conflicts.RaiseListChangedEvents = false;
        _conflicts.Clear();

        var holding = ReadHolding();
        if (_catalog.Count == 0)
        {
            _holdingWarningLabel.ForeColor = SystemColors.GrayText;
            _holdingWarningLabel.Text =
                "Catalogo strategie non disponibile: l'elenco delle strategie tagliate da questo piano " +
                "non puo' essere calcolato. Il piano resta salvabile.";
        }
        else if (holding.AllowOvernight && holding.AllowOverweek)
        {
            _holdingWarningLabel.ForeColor = SystemColors.ControlText;
            _holdingWarningLabel.Text =
                "Il piano non impone alcun flat: overnight e overweek restano decisi da motore e strategia. " +
                "Attenzione, e' una configurazione da conto proprio: quasi nessuna prop la ammette.";
        }
        else
        {
            var byId = _catalog.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
            var selected = _masterFilter
                .Select(id => byId.GetValueOrDefault(id.Trim()))
                .Where(item => item is not null)
                .Select(item => item!)
                .ToList();

            var conflicts = HoldingResolver.FindConflicts(
                selected.Select(item => (
                    item.Id,
                    item.Name,
                    new StrategyHolding(item.Overnight, item.Overweek))),
                holding);

            foreach (var conflict in conflicts)
            {
                var strategy = byId[conflict.StrategyId];
                _conflicts.Add(new PlanHoldingConflictRow
                {
                    Strategy = conflict.StrategyCode,
                    Symbol = strategy.Symbol,
                    Timeframe = strategy.TimeframeMinutes > 0 ? $"{strategy.TimeframeMinutes}m" : "—",
                    Holding = conflict.Holding.Describe(),
                    Effect = conflict.CutAtSessionFlat
                        ? $"chiusa ogni giorno alle {Hhmm(holding.SessionFlatUtcHhmm)} UTC"
                        : $"chiusa il venerdi alle {Hhmm(holding.WeekEnd.FromUtcHhmm)} UTC"
                });
            }

            if (conflicts.Count == 0)
            {
                _holdingWarningLabel.ForeColor = SystemColors.ControlText;
                _holdingWarningLabel.Text = selected.Count == 0
                    ? "Il masterfilter del workspace e' vuoto (o le sue strategie non sono nel catalogo): " +
                      "non c'e' nulla di cui calcolare l'impatto."
                    : $"Nessuna delle {selected.Count} strategie del masterfilter viene tagliata: " +
                      "chiudono tutte entro i limiti che il piano concede.";
            }
            else
            {
                _holdingWarningLabel.ForeColor = Color.FromArgb(176, 84, 0);
                _holdingWarningLabel.Text =
                    $"Questo piano tronca {conflicts.Count} strategie su {selected.Count} del masterfilter. " +
                    "Sono strategie che la ricerca ha misurato multiday: su questo piano non possono esserlo, " +
                    "quindi i loro trade non saranno confrontabili con il run originale. Il taglio e' " +
                    "legittimo — e' cio' che la prop impone — ma va saputo prima, non letto dopo nei trade.";
            }
        }

        _conflicts.RaiseListChangedEvents = true;
        _conflicts.ReapplySort();
        _conflicts.ResetBindings();
    }

    private static string Hhmm(int value) => $"{value / 100:00}:{value % 100:00}";

    private static decimal Clamp(NumericUpDown input, decimal value)
        => Math.Clamp(value, input.Minimum, input.Maximum);

    private void MarkDirty()
    {
        if (!_suspendDirtyTracking)
        {
            SetDirty(true);
        }
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        _toolbar.SetDirty(dirty);
    }

    private void OnFieldChanged(object? sender, EventArgs e) => MarkDirty();

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnAddGroupClick(object? sender, EventArgs e)
    {
        // Il gruppo si sceglie dalla combo: un placeholder testuale finirebbe nel piano come
        // gruppo inesistente.
        _groups.Add(new PlanGroupEditRow { GroupId = string.Empty });
    }

    private void OnRemoveGroupClick(object? sender, EventArgs e)
    {
        if (_groupsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _groups.Count)
        {
            _groups.RemoveAt(index);
        }
    }

    private void OnAddAccountClick(object? sender, EventArgs e)
    {
        // L'account si sceglie dalla combo: un placeholder testuale finirebbe nel piano come
        // account inesistente. Il gruppo si popola da solo alla scelta dell'account.
        _accounts.Add(new PlanAccountEditRow { GroupId = string.Empty, AccountNumber = string.Empty });
    }

    private void OnRemoveAccountClick(object? sender, EventArgs e)
    {
        if (_accountsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _accounts.Count)
        {
            _accounts.RemoveAt(index);
        }
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (SelectedWorkspaceId is not { } targetWorkspaceId)
        {
            MessageBox.Show(this, "Seleziona nella barra in alto il workspace in cui salvare il piano.",
                "Piano", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var code = _codeTextBox.Text.Trim();
        if (code.Length == 0)
        {
            MessageBox.Show(this, "Il codice del piano è obbligatorio.", "Piano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Una riga con il gruppo non ancora scelto non va scartata in silenzio: prima del refactor
        // in due tab bastava filtrarla (era anche priva di account), ma qui sparirebbe dalla
        // griglia al giro di Fill() successivo senza che l'utente capisca perché.
        if (_groups.Any(row => string.IsNullOrWhiteSpace(row.GroupId)))
        {
            MessageBox.Show(
                this,
                "C'è una riga senza gruppo nel tab Gruppi: scegli un gruppo dalla combo o rimuovi la riga.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var duplicateGroup = _groups
            .GroupBy(row => row.GroupId.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateGroup != null)
        {
            MessageBox.Show(
                this,
                $"Il gruppo '{duplicateGroup.Key}' è configurato più di una volta nel tab Gruppi.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var groupProfiles = _groups.ToDictionary(
            row => row.GroupId.Trim(), row => row, StringComparer.OrdinalIgnoreCase);
        if (groupProfiles.Count == 0)
        {
            MessageBox.Show(
                this,
                "Serve almeno un gruppo nel tab Gruppi.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_accounts.Any(row => string.IsNullOrWhiteSpace(row.AccountNumber)))
        {
            MessageBox.Show(
                this,
                "C'è una riga senza account nel tab Account: scegli un account dalla combo o rimuovi la riga.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var validAccounts = _accounts
            .Where(row => !string.IsNullOrWhiteSpace(row.AccountNumber))
            .ToList();
        if (validAccounts.Count == 0)
        {
            MessageBox.Show(
                this,
                "Serve almeno un account nel tab Account: è la configurazione canonica del piano.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var orphanAccount = validAccounts.FirstOrDefault(row => !groupProfiles.ContainsKey(row.GroupId.Trim()));
        if (orphanAccount != null)
        {
            MessageBox.Show(
                this,
                $"L'account '{orphanAccount.AccountNumber}' fa riferimento al gruppo '{orphanAccount.GroupId}', " +
                "che non è configurato nel tab Gruppi.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Un gruppo senza nemmeno un account non ha modo di essere scritto: TradingGroupRow è
        // sempre una coppia gruppo+account, quindi un profilo "solo gruppo" verrebbe scartato in
        // silenzio al salvataggio invece di comparire come errore.
        var emptyGroup = groupProfiles.Keys.FirstOrDefault(groupId => !validAccounts.Any(row =>
            string.Equals(row.GroupId.Trim(), groupId, StringComparison.OrdinalIgnoreCase)));
        if (emptyGroup != null)
        {
            MessageBox.Show(
                this,
                $"Il gruppo '{emptyGroup}' non ha nessun account nel tab Account: aggiungine almeno uno " +
                "(o rimuovi il gruppo), altrimenti il profilo non verrebbe salvato.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var holding = ReadHolding();
        try
        {
            // Stessa validazione del server, anticipata: un HHMM impossibile va detto mentre lo si
            // sta scrivendo, non come 400 al salvataggio.
            holding.Validate();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Piano", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _tabs.SelectedTab = _holdingTab;
            return;
        }

        var request = new SaveTradingPlanRequest
        {
            Code = code,
            Name = _nameTextBox.Text.Trim() is { Length: > 0 } name ? name : code,
            Groups = validAccounts.Select(row =>
            {
                return new TradingGroupRow
                {
                    GroupId = row.GroupId.Trim(),
                    AccountNumber = row.AccountNumber.Trim(),
                    MaxConcurrentTrades = row.MaxConcurrentTrades,
                    // Un valore illeggibile (piano scritto a mano, enum rinominato) ricade sul
                    // default storico invece di far fallire il salvataggio dell'intero piano.
                    ConcurrencyCountMode = Enum.TryParse<ConcurrencyCountMode>(
                        row.ConcurrencyCountMode, ignoreCase: true, out var countMode)
                        ? countMode
                        : ConcurrencyCountMode.PositionsAndPendingOrders
                };
            }).ToList(),
            EnforceConcurrencyLimits = _enforceConcurrencyCombo.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            },
            CommissionPerContract = _commissionInput.Value,
            Holding = ReadHolding(),
            PositionSizing = new PositionSizingConfig
            {
                ClampMultipliersToUnitInterval = _clampMultipliersCheckBox.Checked,
                MarketVolatility = new MarketVolatilitySizingConfig
                {
                    Enabled = _volatilityEnabledCheckBox.Checked,
                    AtrPeriods = (int)_atrPeriodsInput.Value,
                    TargetRiskDollars = _targetRiskInput.Value
                },
                PortfolioRisk = new PortfolioRiskSizingConfig
                {
                    Enabled = _portfolioRiskEnabledCheckBox.Checked,
                    MaximumDrawdown = _maxDrawdownInput.Value,
                    MaximumGrossExposure = _maxGrossExposureInput.Value,
                    EnableAggressiveModules = _aggressiveModulesCheckBox.Checked,
                    FractionalFactor = _fractionalFactorInput.Value,
                    MaximumMultiplier = _maximumMultiplierInput.Value
                }
            }
        };

        _toolbar.SetBusy(true);
        try
        {
            var saved = await _context.Services.Plans.SaveAsync(targetWorkspaceId, request);
            _workspaceId = targetWorkspaceId;
            _code = saved.Code;
            _isNew = false;
            _codeTextBox.ReadOnly = true;
            _suspendDirtyTracking = true;
            Fill(saved);
            await RefreshGroupChoicesAsync(CancellationToken.None);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.Title = $"Piano {saved.Code}";
            _context.Navigation.SetStatus($"Piano '{saved.Code}' salvato nel workspace '{targetWorkspaceId}'.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Errore di salvataggio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
