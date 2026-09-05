using System.ComponentModel;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Riga del tab Conti: un conto cTrader che esegue il piano. Non c'è altro sulla riga — il gruppo
/// non esiste più e il tetto di posizioni è del piano, uguale per tutti i conti.
/// </summary>
public sealed class PlanAccountEditRow
{
    public string AccountNumber { get; set; } = string.Empty;
}

/// <summary>
/// Riga del tab Strategie: una strategia del masterfilter, accesa o spenta in questo piano.
///
/// <para>La spunta e' in <b>positivo</b> ("Attiva") mentre il piano scrive l'elenco delle
/// <i>spente</i> (<see cref="TradingPlan.DisabledStrategies"/>): l'unica negazione sta in
/// <see cref="PlanDetailScreen.SetStrategyActive"/>, come per le chiusure forzate. Il contratto
/// elenca le spente perche' il masterfilter cambia, e una strategia che vi entra domani deve
/// nascere accesa in ogni piano gia' scritto.</para>
/// </summary>
public sealed class PlanStrategyEditRow
{
    public bool Active { get; set; } = true;

    /// <summary>Id di catalogo: e' cio' che il piano salva. Non ha colonna, non si edita.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Se l'Id sta nel masterfilter del workspace. Le righe che non ci stanno sono spegnimenti
    /// dichiarati dal piano su strategie che il masterfilter non contiene (piu'): restano a video e
    /// restano nel file, perche' a cambiare e' stato il masterfilter, non la scelta di chi ha spento.
    /// </summary>
    public bool InMasterFilter { get; set; }

    public string Strategy { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public string Holding { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
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
    // Griglia editabile: resta BindingList<T> e non ordinabile per colonna, perché l'ordine è
    // quello in cui si sta scrivendo. Vedi .cursor/rules/piutoo-console-screens.mdc.
    private readonly BindingList<PlanAccountEditRow> _accounts = new();

    /// <summary>Registro globale (<c>api/Accounts</c>), non del workspace: si carica una volta.</summary>
    private readonly List<WorkspaceAccount> _registryAccounts = new();

    /// <summary>
    /// Anagrafica broker. Il piano ne dichiara uno, e i conti selezionabili sono i suoi: due broker
    /// non quotano la stessa serie di barre, quindi un piano che li mescolasse non corrisponderebbe
    /// a nessun conto.
    /// </summary>
    private readonly List<TradingBroker> _brokers = new();

    /// <summary>
    /// Catalogo strategie del server: serve a sapere quali strategie del masterfilter sono
    /// multiday, cioe' quali il piano taglierebbe. Si carica una volta, come il registro account.
    /// </summary>
    private readonly List<StrategyCatalogItem> _catalog = new();

    /// <summary>Id del masterfilter del workspace scelto: cambia con la combo workspace.</summary>
    private readonly List<string> _masterFilter = new();

    private readonly SortableBindingList<PlanHoldingConflictRow> _conflicts = new();

    /// <summary>
    /// Tutte le righe del tab Strategie: il masterfilter del workspace, piu' le spente che il
    /// masterfilter non contiene piu'. E' l'elenco completo, indipendente dal filtro di ricerca.
    /// </summary>
    private readonly List<PlanStrategyEditRow> _allStrategies = new();

    /// <summary>
    /// Le righe <b>visibili</b>, cioe' quelle che passano il filtro di ricerca: e' la collezione
    /// legata alla griglia. Ordinabile — l'ordine di questa griglia non e' un dato del piano, e' una
    /// lente per trovare la riga da spegnere fra decine.
    /// </summary>
    private readonly SortableBindingList<PlanStrategyEditRow> _strategies = new();

    /// <summary>
    /// Perche' l'elenco e' vuoto, quando lo e'. Vuoto = nessun motivo, cioe' l'elenco non lo e'.
    /// Una griglia vuota senza spiegazione e' il silenzio che questo progetto non ammette.
    /// </summary>
    private string _strategiesUnavailableReason = string.Empty;

    /// <summary>
    /// Gli Id spenti, cioe' esattamente cio' che finisce in <c>DisabledStrategies</c>. E' questo
    /// l'insieme autorevole, non le spunte della griglia: un Id spento e sparito dal masterfilter
    /// deve sopravvivere a un ricalcolo delle righe.
    /// </summary>
    private readonly HashSet<string> _disabled = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Il <c>PositionSizing</c> del piano come letto dal server, riproposto tale e quale al
    /// salvataggio.
    ///
    /// <para>Il tab che lo editava non c'è più, ma il salvataggio riscrive il piano <b>intero</b>:
    /// senza questo campo la prima modifica fatta da questa schermata riporterebbe ai default un
    /// blocco che nessuno ha toccato. Togliere una schermata non è una decisione su cosa il piano
    /// contiene.</para>
    /// </summary>
    private PositionSizingConfig _loadedPositionSizing = new();
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
        _countModeCombo.DisplayMember = nameof(ValueComboItem.Display);
        _countModeCombo.ValueMember = nameof(ValueComboItem.Id);
        _countModeCombo.DataSource = new List<ValueComboItem>
        {
            ValueComboItem.Of(
                nameof(ConcurrencyCountMode.PositionsAndPendingOrders),
                "Posizioni + ordini pendenti"),
            ValueComboItem.Of(
                nameof(ConcurrencyCountMode.PositionsOnly),
                "Solo posizioni riempite")
        };

        _accounts.ListChanged += (_, _) => MarkDirty();

        _conflictsBindingSource.DataSource = _conflicts;
        _conflictsGrid.EnableColumnSorting();

        _strategiesBindingSource.DataSource = _strategies;
        _strategiesGrid.EnableColumnSorting();
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
                $"Piano '{plan.Code}' su {plan.Accounts.Count} conti, " +
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
        _registryAccounts.Clear();
        try
        {
            var accounts = await _context!.Services.Api.ListAccountsAsync(cancellationToken);
            _registryAccounts.AddRange(accounts
                .Where(account => !string.IsNullOrWhiteSpace(account.AccountNumber))
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase));

            _brokers.Clear();
            _brokers.AddRange(await _context.Services.Api.ListBrokersAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Registro conti non leggibile: {ex.Message}");
        }
    }

    private Task RefreshGroupChoicesAsync(CancellationToken cancellationToken)
    {
        RefreshAccountNumberColumnItems();
        return Task.CompletedTask;
    }

    /// <summary>Il broker del piano: da lui vengono la tabella dei simboli e il feed dei conti.</summary>
    private void FillBrokerCombo(string currentCode)
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun broker)") };
        items.AddRange(_brokers
            .Where(broker => !string.IsNullOrWhiteSpace(broker.Code))
            .Select(broker => ValueComboItem.Of(broker.Code, $"{broker.Name}  ·  {broker.Code}")));

        var current = currentCode?.Trim() ?? string.Empty;
        if (current.Length > 0 &&
            !items.Any(item => string.Equals(item.Id, current, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(ValueComboItem.Missing(current));
        }

        _planBrokerCombo.DisplayMember = nameof(ValueComboItem.Display);
        _planBrokerCombo.ValueMember = nameof(ValueComboItem.Id);
        _planBrokerCombo.DataSource = items;
        _planBrokerCombo.SelectedIndex = Math.Max(0, items.FindIndex(item =>
            string.Equals(item.Id, current, StringComparison.OrdinalIgnoreCase)));
    }

    private string SelectedBrokerCode =>
        (_planBrokerCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty;

    /// <summary>
    /// Cambiato il broker cambiano i conti selezionabili. Le righe già scritte non si toccano: se
    /// una non appartiene al broker nuovo il salvataggio lo dirà, invece di sparire dalla griglia.
    /// </summary>
    private void OnPlanBrokerChanged(object? sender, EventArgs e)
    {
        RefreshAccountNumberColumnItems();
        MarkDirty();
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



    /// <summary>
    /// La colonna Account cTrader propone tutto il registro: il gruppo non si sceglie più a parte,
    /// deriva dall'account scelto (<see cref="OnAccountsGridCellValueChanged"/>), quindi qui non
    /// c'è nulla da filtrare per riga.
    /// </summary>
    private void RefreshAccountNumberColumnItems()
    {
        var broker = SelectedBrokerCode;
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun conto)") };

        // Con un broker scelto la combo propone solo i suoi conti, più quelli che non ne dichiarano
        // ancora uno (anagrafica non migrata): proporre i conti di un altro broker sarebbe proporre
        // una configurazione che il salvataggio rifiuta.
        foreach (var account in _registryAccounts)
        {
            var suo = account.BrokerCode?.Trim() ?? string.Empty;
            if (broker.Length > 0 && suo.Length > 0 &&
                !suo.Equals(broker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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





    private void OnAccountsGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_accountsGrid.IsCurrentCellDirty && _accountsGrid.CurrentCell is DataGridViewComboBoxCell)
        {
            _accountsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    /// <summary>
    /// Nessun effetto collaterale: la riga porta solo il numero di conto. Il gruppo non esiste più e
    /// il tetto di posizioni è del piano, nel tab Generale.
    /// </summary>
    private void OnAccountsGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
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
        _sizeMultiplierInput.Value = 1m;
        _maxConcurrentInput.Value = 0m;
        _countModeCombo.SelectedIndex = 0;
        FillBrokerCombo(string.Empty);

        FillHolding(AccountHoldingPolicy.Default);

        _loadedPositionSizing = new PositionSizingConfig();

        _disabled.Clear();
        RebuildStrategyRows();

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

        // Un piano scritto prima che il moltiplicatore esistesse lo presenta a 0: il minimo del
        // controllo lo riporta a 0,1, che non e' quello che quel piano fa. Il server normalizza gia'
        // a 1 in lettura (TradingPlanService.NormalizeSizeMultiplier); qui si ripete la stessa
        // regola per non dipendere dall'ordine in cui server e console vengono aggiornati.
        _sizeMultiplierInput.Value = plan.SizeMultiplier > 0m
            ? Clamp(_sizeMultiplierInput, plan.SizeMultiplier)
            : 1m;

        FillHolding(plan.Holding);

        _disabled.Clear();
        foreach (var id in plan.DisabledStrategies)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _disabled.Add(id.Trim());
            }
        }

        RebuildStrategyRows();

        _loadedPositionSizing = plan.PositionSizing;

        FillBrokerCombo(plan.BrokerCode);
        _maxConcurrentInput.Value = Math.Clamp(
            plan.MaxConcurrentTrades, (int)_maxConcurrentInput.Minimum, (int)_maxConcurrentInput.Maximum);
        _countModeCombo.SelectedIndex =
            plan.ConcurrencyCountMode == ConcurrencyCountMode.PositionsOnly ? 1 : 0;

        _accounts.RaiseListChangedEvents = false;
        _accounts.Clear();
        foreach (var account in plan.Accounts)
        {
            _accounts.Add(new PlanAccountEditRow { AccountNumber = account });
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

    /// <summary>
    /// Le due spunte sono in <b>positivo sul taglio</b> ("forza chiusura"), il contratto e' in
    /// positivo sul permesso (<c>AllowOvernight</c>/<c>AllowOverweek</c>): qui e in
    /// <see cref="ReadHolding"/> c'e' l'unica negazione, e non deve comparire altrove.
    ///
    /// <para>L'etichetta segue quello che il piano <i>fa</i>: spuntata, il segnale esce con una
    /// deadline che il conto impone. Il campo del contratto resta in positivo perche' lo leggono i
    /// <c>plans.json</c> gia' scritti e i cBot installati, e invertirlo li' ribalterebbe in silenzio
    /// il significato di ogni file esistente.</para>
    /// </summary>
    private void FillHolding(AccountHoldingPolicy holding)
    {
        _forceNightCloseCheckBox.Checked = !holding.AllowOvernight;
        _forceWeekCloseCheckBox.Checked = !holding.AllowOverweek;
        _sessionFlatInput.Value = Math.Clamp(holding.SessionFlatUtcHhmm, 0, 2359);
        _weekEndFromInput.Value = Math.Clamp(holding.WeekEnd.FromUtcHhmm, 0, 2359);
        _weekEndUntilInput.Value = Math.Clamp(holding.WeekEnd.UntilUtcHhmm, 0, 2359);
        ApplyHoldingEnablement();
    }

    /// <inheritdoc cref="FillHolding"/>
    private AccountHoldingPolicy ReadHolding() => new()
    {
        AllowOvernight = !_forceNightCloseCheckBox.Checked,
        AllowOverweek = !_forceWeekCloseCheckBox.Checked,
        SessionFlatUtcHhmm = (int)_sessionFlatInput.Value,
        WeekEnd = new WeekEndFlatPolicy((int)_weekEndFromInput.Value, (int)_weekEndUntilInput.Value)
    };

    /// <summary>
    /// Un orario di taglio ha senso solo se quel taglio esiste: mostrare un campo attivo che non
    /// governa nulla fa credere che il piano stia facendo qualcosa che non fa.
    ///
    /// <para>Chi chiude ogni notte chiude per forza anche il fine settimana: la spunta week viene
    /// forzata e bloccata, che e' la stessa regola che il server verifica al salvataggio
    /// (<c>AccountHoldingPolicy.Validate</c>) espressa nel verso del taglio.</para>
    /// </summary>
    private void ApplyHoldingEnablement()
    {
        var cutsNight = _forceNightCloseCheckBox.Checked;
        _sessionFlatInput.Enabled = cutsNight;
        _sessionFlatLabel.Enabled = cutsNight;

        if (cutsNight && !_forceWeekCloseCheckBox.Checked)
        {
            _forceWeekCloseCheckBox.Checked = true;
        }

        _forceWeekCloseCheckBox.Enabled = !cutsNight;

        var cutsWeek = _forceWeekCloseCheckBox.Checked;
        _weekEndFromInput.Enabled = cutsWeek;
        _weekEndFromLabel.Enabled = cutsWeek;
        _weekEndUntilInput.Enabled = cutsWeek;
        _weekEndUntilLabel.Enabled = cutsWeek;
    }

    private void OnHoldingChanged(object? sender, EventArgs e)
    {
        ApplyHoldingEnablement();
        MarkDirty();
        UpdateHoldingImpact();
    }

    /// <summary>
    /// Ricarica il masterfilter del workspace corrente, ricostruisce le righe del tab Strategie e
    /// ricalcola l'avviso delle chiusure. Il masterfilter e' del workspace, quindi va riletto ogni
    /// volta che il workspace cambia.
    ///
    /// <para>Un masterfilter illeggibile <b>non</b> passa in silenzio: il motivo finisce nella
    /// riga sopra la griglia. Una schermata che mostra un elenco vuoto senza dire perche' fa
    /// cercare un errore di configurazione che magari e' solo un server spento.</para>
    /// </summary>
    private async Task RefreshHoldingImpactAsync(CancellationToken cancellationToken)
    {
        _masterFilter.Clear();
        _strategiesUnavailableReason = string.Empty;

        if (_context == null)
        {
            _strategiesUnavailableReason = "schermata non inizializzata";
        }
        else if (string.IsNullOrWhiteSpace(_workspaceId))
        {
            _strategiesUnavailableReason =
                "nessun workspace selezionato: scegline uno nella barra in alto";
        }
        else
        {
            try
            {
                var filter = await _context.Services.Api.GetMasterFilterAsync(_workspaceId, cancellationToken);
                _masterFilter.AddRange(filter.StrategiesFilter);
                if (_masterFilter.Count == 0)
                {
                    _strategiesUnavailableReason =
                        $"il masterfilter del workspace '{_workspaceId}' e' vuoto: aggiungici le " +
                        "strategie da far girare, il piano puo' solo spegnerne un sottoinsieme";
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _strategiesUnavailableReason =
                    $"masterfilter del workspace '{_workspaceId}' non leggibile: {ex.Message}";
            }
        }

        RebuildStrategyRows();
        UpdateHoldingImpact();
    }

    /// <summary>
    /// Le righe del tab Strategie: il masterfilter del workspace, piu' gli spegnimenti che il
    /// masterfilter non copre. Si richiama a ogni cambio di masterfilter o di piano caricato; le
    /// spunte le rilegge da <see cref="_disabled"/>, non dalle righe di prima.
    /// </summary>
    private void RebuildStrategyRows()
    {
        var byId = _catalog.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var inFilter = new HashSet<string>(
            _masterFilter.Select(id => id.Trim()).Where(id => id.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        _allStrategies.Clear();
        foreach (var id in inFilter.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            _allStrategies.Add(BuildStrategyRow(id, byId.GetValueOrDefault(id), inMasterFilter: true));
        }

        foreach (var id in _disabled.Where(id => !inFilter.Contains(id))
                     .OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            _allStrategies.Add(BuildStrategyRow(id, byId.GetValueOrDefault(id), inMasterFilter: false));
        }

        ApplyStrategyFilter();
    }

    /// <summary>
    /// Riempie la griglia con le sole righe che passano ricerca e <i>solo selezionate</i>. Il filtro
    /// e' una lente sulla vista: non tocca <see cref="_disabled"/>, quindi cercare non accende e non
    /// spegne niente.
    ///
    /// <para><b>Perche' non si riapplica a ogni spunta.</b> Con <i>solo selezionate</i> acceso,
    /// togliere la spunta a una riga la renderebbe subito invisibile: la griglia si accorcerebbe
    /// sotto il cursore e il clic successivo cadrebbe su un'altra strategia. La riga sparisce alla
    /// prossima riapplicazione del filtro — un tocco alla ricerca, la casella, un ricarico — che e'
    /// il momento in cui l'utente sta guardando l'elenco e non le singole spunte.</para>
    ///
    /// <para><c>ReapplySort</c> prima di <c>ResetBindings</c>, come impone la regola delle griglie:
    /// senza, la freccetta resta sull'intestazione mentre le righe tornano nell'ordine della
    /// sorgente — sembra ordinata e non lo e'.</para>
    /// </summary>
    private void ApplyStrategyFilter()
    {
        var query = _strategyFilterBox.Text.Trim();
        var soloSelezionate = _onlySelectedStrategiesCheck.Checked;

        _strategies.RaiseListChangedEvents = false;
        _strategies.Clear();
        foreach (var row in _allStrategies.Where(row => (!soloSelezionate || row.Active) && Matches(row, query)))
        {
            _strategies.Add(row);
        }

        _strategies.RaiseListChangedEvents = true;
        _strategies.ReapplySort();
        _strategies.ResetBindings();
        UpdateStrategiesSummary();
        UpdateToggleAllCaption();
    }

    /// <summary>
    /// Una riga passa la ricerca se il testo compare nel codice, nel simbolo, nel timeframe o nella
    /// tenuta. Piu' parole separate da spazio devono comparire <b>tutte</b>: "nq 15" trova le
    /// strategie a 15 minuti su NQ, che e' il modo in cui si cerca in un elenco lungo.
    /// </summary>
    private static bool Matches(PlanStrategyEditRow row, string query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        var haystack = $"{row.Strategy} {row.Id} {row.Symbol} {row.Timeframe} {row.Holding}";
        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void OnStrategyFilterChanged(object? sender, EventArgs e) => ApplyStrategyFilter();

    /// <summary>
    /// Se la griglia sta mostrando un sottoinsieme. Conta sia la ricerca sia <i>solo selezionate</i>:
    /// e' l'unica cosa che serve sapere a chi scrive "tutto" oppure "i filtrati" in un'etichetta, e
    /// tenerla in un posto solo evita che i due comandi si contraddicano.
    /// </summary>
    private bool StrategyViewIsFiltered =>
        _strategyFilterBox.Text.Trim().Length > 0 || _onlySelectedStrategiesCheck.Checked;

    /// <summary>
    /// L'etichetta del pulsante dice cosa farebbe adesso, non cosa fa in generale: se anche una
    /// sola riga visibile e' spenta, il clic accende tutto; se sono tutte accese, spegne tutto.
    ///
    /// <para>Con <i>solo selezionate</i> acceso le righe visibili sono per definizione tutte attive,
    /// quindi il pulsante propone sempre di spegnerle: e' corretto — spegne quelle che il piano fa
    /// girare — e dice "i filtrati" per non farlo leggere come "tutte quelle del masterfilter".</para>
    /// </summary>
    private void UpdateToggleAllCaption()
    {
        var visibili = _strategies.Count;
        var tutteAttive = visibili > 0 && _strategies.All(row => row.Active);
        _toggleAllStrategiesButton.Enabled = visibili > 0;
        _toggleAllStrategiesButton.Text = tutteAttive
            ? (StrategyViewIsFiltered ? "Deseleziona i filtrati" : "Deseleziona tutto")
            : (StrategyViewIsFiltered ? "Seleziona i filtrati" : "Seleziona tutto");
    }

    private PlanStrategyEditRow BuildStrategyRow(string id, StrategyCatalogItem? item, bool inMasterFilter) => new()
    {
        Id = id,
        Active = !_disabled.Contains(id),
        InMasterFilter = inMasterFilter,
        // Il codice di esecuzione quando c'e' (e' quello che si legge in trades.json), l'Id quando
        // il catalogo non conosce la voce: meglio un Id nudo che una riga senza nome.
        Strategy = item is null || string.IsNullOrWhiteSpace(item.Name) ? id : item.Name,
        Symbol = item?.Symbol ?? string.Empty,
        Timeframe = item is { TimeframeMinutes: > 0 } ? $"{item.TimeframeMinutes}m" : "—",
        Holding = item?.HoldingLabel ?? string.Empty,
        Note = !inMasterFilter
            ? "fuori dal masterfilter: lo spegnimento resta scritto nel piano"
            : item is null
                ? "non nel catalogo: e' il masterfilter da correggere"
                : string.Empty
    };

    /// <summary>Quante ne gira il piano e quante ne ha spente, detto sopra la griglia.</summary>
    private void UpdateStrategiesSummary()
    {
        if (_strategiesUnavailableReason.Length > 0)
        {
            _strategiesSummaryLabel.ForeColor = Color.FromArgb(176, 84, 0);
            _strategiesSummaryLabel.Text =
                $"Elenco non disponibile — {_strategiesUnavailableReason}. Le strategie di un piano " +
                "sono un sottoinsieme del masterfilter del workspace: senza masterfilter non c'e' " +
                "nulla da accendere o spegnere.";
            return;
        }

        var attive = _allStrategies.Count(row => row.InMasterFilter && row.Active);
        var nelFiltro = _allStrategies.Count(row => row.InMasterFilter);
        var orfane = _allStrategies.Count(row => !row.InMasterFilter);
        var visibili = _strategies.Count;
        var ricerca = StrategyViewIsFiltered && visibili != _allStrategies.Count
            ? $" Filtro attivo: {visibili} righe su {_allStrategies.Count}."
            : string.Empty;
        var coda = orfane == 0
            ? string.Empty
            : $" Altre {orfane} spente non sono nel masterfilter: restano scritte nel piano e " +
              "tornano a valere se il masterfilter le riprende.";

        if (attive == 0)
        {
            _strategiesSummaryLabel.ForeColor = Color.FromArgb(176, 84, 0);
            _strategiesSummaryLabel.Text =
                $"Nessuna delle {nelFiltro} strategie del masterfilter e' attiva in questo piano: " +
                "una sessione aperta cosi' viene rifiutata all'apertura, non parte muta." + coda + ricerca;
            return;
        }

        _strategiesSummaryLabel.ForeColor = SystemColors.ControlText;
        _strategiesSummaryLabel.Text =
            $"Il piano fa girare {attive} strategie sulle {nelFiltro} del masterfilter " +
            $"({nelFiltro - attive} spente). Il masterfilter decide cosa esiste nel workspace, " +
            "il piano ne spegne un sottoinsieme." + coda + ricerca;
    }

    /// <summary>Senza questa una spunta di griglia notifica il cambio solo all'uscita dalla cella.</summary>
    private void OnStrategiesGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_strategiesGrid.IsCurrentCellDirty && _strategiesGrid.CurrentCell is DataGridViewCheckBoxCell)
        {
            _strategiesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OnStrategiesGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _strategies.Count || e.ColumnIndex != _colStrategyActive.Index)
        {
            return;
        }

        var row = _strategies[e.RowIndex];
        SetStrategyActive(row.Id, row.Active);
        MarkDirty();
        UpdateStrategiesSummary();
        UpdateToggleAllCaption();
        // Una strategia spenta non e' piu' tagliata da nulla: l'avviso delle chiusure forzate
        // conta solo cio' che il piano fa girare davvero.
        UpdateHoldingImpact();
    }

    /// <summary>L'unica negazione fra la spunta "Attiva" e l'elenco delle spente del contratto.</summary>
    private void SetStrategyActive(string id, bool active)
    {
        if (active)
        {
            _disabled.Remove(id);
        }
        else
        {
            _disabled.Add(id);
        }
    }

    /// <summary>
    /// Accende o spegne <b>le righe visibili</b>, cioe' quelle che la ricerca sta mostrando. Agire
    /// sull'elenco intero mentre se ne vede una parte e' il modo piu' rapido di spegnere per
    /// sbaglio venti strategie che non si stavano nemmeno guardando.
    /// </summary>
    private void OnToggleAllStrategiesClick(object? sender, EventArgs e)
    {
        if (_strategies.Count == 0)
        {
            return;
        }

        var accendi = !_strategies.All(row => row.Active);
        foreach (var row in _strategies)
        {
            row.Active = accendi;
            SetStrategyActive(row.Id, accendi);
        }

        // Ricostruzione e non semplice ResetBindings: accendendo tutto, le righe fuori dal
        // masterfilter non hanno piu' motivo di esistere — erano li' solo per dire che quel piano le
        // teneva spente — e lasciarle a video le farebbe contare ancora nel riepilogo.
        RebuildStrategyRows();
        MarkDirty();
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
            // Solo le strategie che il piano fa girare: una spenta nel tab Strategie non viene
            // troncata da questo piano, non viene proprio eseguita, e mostrarla qui come "tagliata"
            // farebbe cercare un effetto che non c'e'.
            var selected = _masterFilter
                .Select(id => id.Trim())
                .Where(id => !_disabled.Contains(id))
                .Select(byId.GetValueOrDefault)
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
                    ? "Nessuna strategia attiva nel piano (masterfilter vuoto, tutte spente nel tab " +
                      "Strategie, o non presenti nel catalogo): non c'e' nulla di cui calcolare l'impatto."
                    : $"Nessuna delle {selected.Count} strategie attive del piano viene tagliata: " +
                      "chiudono tutte entro i limiti che il piano concede.";
            }
            else
            {
                _holdingWarningLabel.ForeColor = Color.FromArgb(176, 84, 0);
                _holdingWarningLabel.Text =
                    $"Questo piano tronca {conflicts.Count} strategie sulle {selected.Count} che fa girare. " +
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





    private void OnAddAccountClick(object? sender, EventArgs e)
    {
        // Il conto si sceglie dalla combo: un placeholder testuale finirebbe nel piano come
        // conto inesistente.
        _accounts.Add(new PlanAccountEditRow { AccountNumber = string.Empty });
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

        if (_accounts.Any(row => string.IsNullOrWhiteSpace(row.AccountNumber)))
        {
            MessageBox.Show(
                this,
                "C'è una riga senza conto nel tab Conti: scegli un conto dalla combo o rimuovi la riga.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var validAccounts = _accounts
            .Select(row => row.AccountNumber.Trim())
            .Where(account => account.Length > 0)
            .ToList();
        if (validAccounts.Count == 0)
        {
            MessageBox.Show(
                this,
                "Serve almeno un conto nel tab Conti: è la configurazione canonica del piano.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var duplicato = validAccounts
            .GroupBy(account => account, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(gruppo => gruppo.Count() > 1);
        if (duplicato != null)
        {
            MessageBox.Show(
                this,
                $"Il conto '{duplicato.Key}' è configurato più di una volta nel tab Conti.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Un piano che spegne tutto il masterfilter non è un piano che non opera: è un piano che
        // la sessione rifiuta all'apertura (TradingSessionService.CreateCore). Scoprirlo lì
        // significa scoprirlo dal cBot, a mercato aperto. Il controllo salta se il masterfilter non
        // si è potuto leggere: l'avviso è diagnostica, non deve bloccare un salvataggio.
        if (_masterFilter.Count > 0 && !_strategies.Any(row => row.InMasterFilter && row.Active))
        {
            MessageBox.Show(
                this,
                "Tutte le strategie del masterfilter sono spente nel tab Strategie: una sessione " +
                "aperta con questo piano verrebbe rifiutata. Riaccendine almeno una.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _tabs.SelectedTab = _strategiesTab;
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
            BrokerCode = SelectedBrokerCode,
            Accounts = validAccounts,
            MaxConcurrentTrades = (int)_maxConcurrentInput.Value,
            ConcurrencyCountMode = _countModeCombo.SelectedIndex == 1
                ? ConcurrencyCountMode.PositionsOnly
                : ConcurrencyCountMode.PositionsAndPendingOrders,
            EnforceConcurrencyLimits = _enforceConcurrencyCombo.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            },
            CommissionPerContract = _commissionInput.Value,
            SizeMultiplier = _sizeMultiplierInput.Value,
            Holding = ReadHolding(),
            // Le spente, non le accese: vedi TradingPlan.DisabledStrategies. Comprende gli Id che
            // il masterfilter non contiene piu', che il server conserva senza validarli.
            DisabledStrategies = _disabled.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList(),
            PositionSizing = _loadedPositionSizing
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
