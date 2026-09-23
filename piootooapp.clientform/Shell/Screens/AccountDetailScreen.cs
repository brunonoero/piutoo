using System.ComponentModel;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Dettaglio di un account globale. Con <see cref="SetAccountId"/> a null è la schermata di
/// creazione: la tabella di conversione non è gestita qui, solo scelta fra quelle già definite nel
/// registro globale (Anagrafiche → Conversioni simbolo) — vedi
/// <c>docs/domini/account-e-conversione-symbol.md</c>.
/// </summary>
public partial class AccountDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    private ShellContext? _context;
    private string? _accountId;
    private WorkspaceAccount? _loaded;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    /// <summary>
    /// Le strategie del <b>workspace corrente</b> — il suo masterfilter risolto sul catalogo del
    /// server — e la base su cui si calcola l'universo del conto.
    ///
    /// <para>Non e' il catalogo intero, che sono classi valide per tutti i workspace: elencarle
    /// tutte qui faceva leggere come universo operativo del conto un centinaio di strategie che nel
    /// workspace su cui si sta lavorando non esistono. E' la stessa vista di
    /// <see cref="StrategyListScreen"/>.</para>
    /// </summary>
    private readonly List<StrategyCatalogItem> _catalog = new();

    /// <summary>Quante ne ha il catalogo del server, workspace a parte. Distingue "masterfilter
    /// vuoto" da "catalogo non letto", che a griglia vuota si assomigliano.</summary>
    private int _catalogTotal;

    /// <summary>Workspace su cui i due tab Strategie sono calcolati; vuoto se non ce n'e' uno.</summary>
    private string _workspaceId = string.Empty;

    /// <summary>Tabelle di conversione del registro globale, per risolvere quella dell'account.</summary>
    private readonly List<SymbolConversion> _conversions = new();
    private readonly List<TradingBroker> _brokers = new();

    /// <summary>Strategie che questo conto puo' operare, prima del filtro di testo.</summary>
    private readonly List<AccountStrategyRow> _supported = new();

    /// <summary>
    /// Strategie del workspace che questo conto <b>non</b> puo' operare, con il motivo.
    ///
    /// <para>Sono il complemento esatto di <see cref="_supported"/> sullo stesso insieme: le due
    /// liste insieme fanno le strategie del workspace, e nessuna sta in tutte e due. E' il punto
    /// del tab — non "quante ne mancano", ma <i>quali</i> e <i>perche'</i>.</para>
    /// </summary>
    private readonly List<AccountStrategyRow> _excluded = new();

    private readonly SortableBindingList<AccountStrategyRow> _visibleStrategies = new();

    private readonly SortableBindingList<AccountStrategyRow> _visibleExcluded = new();

    public AccountDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _strategiesBindingSource.DataSource = _visibleStrategies;
        _strategiesGrid.EnableColumnSorting();
        _excludedBindingSource.DataSource = _visibleExcluded;
        _excludedGrid.EnableColumnSorting();
    }

    public string ScreenTitle => IsNew
        ? "Nuovo account"
        : _loaded?.Name is { Length: > 0 } name ? name : _accountId ?? "Account";

    public bool HasUnsavedChanges => _isDirty;

    private bool IsNew => string.IsNullOrWhiteSpace(_accountId);

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. Null significa nuovo account.</summary>
    public void SetAccountId(string? accountId) => _accountId = accountId;

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        _suspendDirtyTracking = true;
        try
        {
            var groups = await _context.Services.Api.ListAccountGroupsAsync(cancellationToken);
            _groupCombo.Items.Clear();
            _groupCombo.Items.Add(string.Empty);
            foreach (var group in groups.OrderBy(group => group, StringComparer.OrdinalIgnoreCase))
            {
                _groupCombo.Items.Add(group);
            }

            var conversions = await _context.Services.Api.ListSymbolConversionsAsync(cancellationToken);
            _conversions.Clear();
            _conversions.AddRange(conversions);

            // I broker servono prima del bind: e' da loro che arriva la tabella dei simboli del
            // conto, e la combo delle tabelle diventa di sola lettura quando un broker c'e'.
            _brokers.Clear();
            try
            {
                _brokers.AddRange(await _context.Services.Api.ListBrokersAsync(cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _context.Navigation.SetError($"Anagrafica broker non leggibile: {ex.Message}");
            }
            await LoadStrategyCatalogAsync(cancellationToken);

            if (IsNew)
            {
                _loaded = null;
                BindAccount(new WorkspaceAccount
                {
                    Currency = "USD",
                    Enabled = true,
                    // Capitale di riferimento delle strategie: così un account nuovo opera 1 a 1
                    // finché non gli si dà il saldo reale del conto.
                    InitialBalance = TradingConventions.StrategyReferenceBalance
                }, conversions);
                _context.Navigation.SetStatus("Nuovo account: nessuna conversione, opera 1 a 1.");
            }
            else
            {
                var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
                var account = accounts.FirstOrDefault(item =>
                    string.Equals(item.Id, _accountId, StringComparison.OrdinalIgnoreCase));
                if (account == null)
                {
                    _context.Navigation.SetError($"Account '{_accountId}' non trovato sul server.");
                    return;
                }

                _loaded = account;
                BindAccount(account, conversions);
                _context.Navigation.SetStatus($"Account '{account.Name}' caricato.");
            }

            SetDirty(false);
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
            _toolbar.SetBusy(false);
        }
    }

    private void BindAccount(WorkspaceAccount account, IReadOnlyList<SymbolConversion> conversions)
    {
        _toolbar.Title = IsNew ? "Nuovo account" : account.Name;
        _nameTextBox.Text = account.Name;
        _accountNumberTextBox.Text = account.AccountNumber;
        _groupCombo.Text = account.GroupId;
        FillBrokerCombo(account);
        _currencyCombo.Text = string.IsNullOrWhiteSpace(account.Currency) ? "USD" : account.Currency;
        _initialBalanceInput.Value = Math.Clamp(
            account.InitialBalance,
            _initialBalanceInput.Minimum,
            _initialBalanceInput.Maximum);
        _enabledCheckBox.Checked = account.Enabled;
        _notesTextBox.Text = account.Notes;
        _identityLabel.Text = IsNew
            ? "L'identificativo viene derivato dal nome al salvataggio."
            : $"Id: {account.Id}  ·  creato {account.CreatedUtc:yyyy-MM-dd HH:mm} UTC  ·  " +
              $"aggiornato {account.UpdatedUtc:yyyy-MM-dd HH:mm} UTC";

        FillSymbolConversionCombo(conversions, ResolveConversionCode(account));
        ApplyBrokerToConversion();
        RefreshSupportedStrategies();
    }

    /// <summary>
    /// La tabella con cui il conto opera davvero: quella del suo broker, o quella scritta sul conto
    /// finche' un broker non c'e'. E' la stessa regola del server
    /// (<c>WorkspaceService.ResolveConversionForAccount</c>): se le due divergessero, la schermata
    /// prometterebbe un universo operativo diverso da quello che gira.
    /// </summary>
    private string ResolveConversionCode(WorkspaceAccount account)
    {
        var broker = _brokers.FirstOrDefault(item => string.Equals(
            item.Code, account.BrokerCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        return broker is not null
            ? broker.SymbolConversionCode ?? string.Empty
            : account.SymbolConversionCode ?? string.Empty;
    }

    private void FillBrokerCombo(WorkspaceAccount account)
    {
        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessun broker)") };
        items.AddRange(_brokers
            .Where(broker => !string.IsNullOrWhiteSpace(broker.Code))
            .Select(broker => ValueComboItem.Of(broker.Code, $"{broker.Name}  ·  {broker.Code}")));

        var current = account.BrokerCode?.Trim() ?? string.Empty;
        if (current.Length > 0 &&
            !items.Any(item => string.Equals(item.Id, current, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(ValueComboItem.Missing(current));
        }

        _brokerCombo.DisplayMember = nameof(ValueComboItem.Display);
        _brokerCombo.ValueMember = nameof(ValueComboItem.Id);
        _brokerCombo.DataSource = items;
        _brokerCombo.SelectedIndex = Math.Max(0, items.FindIndex(item =>
            string.Equals(item.Id, current, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Con un broker scelto la tabella dei simboli non si edita qui: e' sua, e mostrarla modificabile
    /// farebbe credere che il conto possa averne una propria. Senza broker resta editabile, che e' il
    /// modo vecchio e serve ai conti non ancora migrati.
    /// </summary>
    private void ApplyBrokerToConversion()
    {
        var broker = SelectedBroker;
        _symbolConversionCombo.Enabled = broker is null;

        if (broker is null)
        {
            return;
        }

        var code = broker.SymbolConversionCode ?? string.Empty;
        for (var index = 0; index < _symbolConversionCombo.Items.Count; index++)
        {
            if (_symbolConversionCombo.Items[index] is ValueComboItem item &&
                string.Equals(item.Id ?? string.Empty, code, StringComparison.OrdinalIgnoreCase))
            {
                _symbolConversionCombo.SelectedIndex = index;
                return;
            }
        }

        _symbolConversionCombo.Items.Add(ValueComboItem.Missing(code));
        _symbolConversionCombo.SelectedIndex = _symbolConversionCombo.Items.Count - 1;
    }

    private TradingBroker? SelectedBroker
    {
        get
        {
            var code = (_brokerCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty;
            return code.Length == 0
                ? null
                : _brokers.FirstOrDefault(broker =>
                    string.Equals(broker.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void OnBrokerChanged(object? sender, EventArgs e)
    {
        ApplyBrokerToConversion();
        RefreshSupportedStrategies();
        OnFieldChanged(sender, e);
    }

    /// <summary>
    /// Le strategie del workspace corrente: il catalogo del server ristretto al masterfilter, come
    /// in <see cref="StrategyListScreen"/>. Un conto opera dentro un workspace, e il catalogo
    /// intero qui prometteva un universo che quel workspace non contiene.
    ///
    /// <para>Non e' una precondizione della schermata: senza workspace, senza masterfilter o senza
    /// catalogo i due tab Strategie restano vuoti e lo dichiarano, ma l'account si modifica e si
    /// salva lo stesso.</para>
    /// </summary>
    private async Task LoadStrategyCatalogAsync(CancellationToken cancellationToken)
    {
        _catalog.Clear();
        _catalogTotal = 0;
        _workspaceId = _context!.Services.Workspaces.CurrentId ?? string.Empty;
        if (_workspaceId.Length == 0)
        {
            return;
        }

        try
        {
            var strategies = await _context.Services.Api.ListStrategiesAsync(cancellationToken);
            var masterFilter = await _context.Services.Api.GetMasterFilterAsync(_workspaceId, cancellationToken);
            _catalogTotal = strategies.Count;

            // Il masterfilter puo' portare l'Id della classe o il codice di esecuzione: si
            // confrontano tutti e due, che e' la stessa regola dell'elenco strategie.
            var wanted = new HashSet<string>(
                masterFilter.StrategiesFilter
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim()),
                StringComparer.OrdinalIgnoreCase);

            _catalog.AddRange(strategies.Where(item =>
                wanted.Contains(item.Id) || wanted.Contains(item.Name)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _catalog.Clear();
            _catalogTotal = 0;
            _context.Navigation.SetError(
                $"Strategie del workspace '{_workspaceId}' non disponibili: {ex.Message}");
        }
    }

    /// <summary>
    /// L'universo operativo del conto: fra le strategie del workspace, quelle il cui simbolo
    /// compare, abilitato, nella tabella di conversione scelta.
    ///
    /// <para>Un conto <b>senza</b> tabella non restringe niente e le opera tutte: e' il conto neutro,
    /// non un conto che non supporta nulla. La stessa regola vale a runtime
    /// (<c>AccountSymbolConversion.SupportsSymbol</c>), ed e' il motivo per cui questa vista puo'
    /// essere letta come una promessa: quello che elenca e' quello che girera'.</para>
    ///
    /// <para>Si ricalcola sulla combo e non sull'account salvato, cosi' cambiando tabella l'elenco
    /// segue subito la scelta invece di aspettare il salvataggio.</para>
    /// </summary>
    private void RefreshSupportedStrategies()
    {
        _supported.Clear();
        _excluded.Clear();

        var code = (_symbolConversionCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty;
        var conversion = _conversions.FirstOrDefault(item =>
            string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));

        var mappings = conversion?.Mappings ?? new List<AccountSymbolMapping>();
        var bySymbol = new Dictionary<string, AccountSymbolMapping>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in mappings)
        {
            var key = NormalizeSymbol(mapping.Symbol);
            if (key.Length > 0) bySymbol[key] = mapping;
        }

        var senzaTabella = bySymbol.Count == 0;
        foreach (var item in _catalog)
        {
            var key = NormalizeSymbol(item.Symbol);
            bySymbol.TryGetValue(key, out var mapping);

            var supportata = senzaTabella || (mapping is not null && mapping.Enabled);

            var row = new AccountStrategyRow
            {
                Code = string.IsNullOrWhiteSpace(item.Code) ? item.Name : item.Code,
                Symbol = item.Symbol,
                AccountSymbol = mapping is null || string.IsNullOrWhiteSpace(mapping.AccountSymbol)
                    ? (senzaTabella ? "(1 a 1)" : item.Symbol)
                    : mapping.AccountSymbol,
                TimeframeMinutes = item.TimeframeMinutes,
                IsActive = item.IsActive,
                Holding = new StrategyHolding(item.Overnight, item.Overweek).Normalized().Describe(),
                // I due motivi non sono la stessa cosa e non si risolvono allo stesso modo: il
                // simbolo assente si aggiunge alla tabella, quello disabilitato si riabilita. Dirlo
                // in colonna evita di aprire il file di conversione per capire quale dei due e'.
                Reason = supportata
                    ? string.Empty
                    : mapping is null
                        ? "simbolo assente dalla tabella di conversione"
                        : "simbolo presente ma disabilitato"
            };

            if (supportata) _supported.Add(row);
            else _excluded.Add(row);
        }

        _supported.Sort(PerCodice);
        _excluded.Sort(PerCodice);
        ApplyStrategiesFilter();
        ApplyExcludedFilter();
    }

    private static int PerCodice(AccountStrategyRow a, AccountStrategyRow b)
        => string.Compare(a.Code, b.Code, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSymbol(string? symbol)
        => symbol is null ? string.Empty : symbol.Trim().TrimStart('@').ToUpperInvariant();

    private void OnStrategiesFilterChanged(object? sender, EventArgs e) => ApplyStrategiesFilter();

    /// <summary>
    /// Applica il filtro di testo e aggiorna il contatore.
    ///
    /// <para>Il contatore dice <b>n/k strategie attive del workspace</b>: <c>k</c> sono le attive
    /// del masterfilter, <c>n</c> quelle che questo conto puo' operare. E' la sola forma che
    /// risponde alla domanda vera — quanta parte di cio' che il workspace fa girare questo conto e'
    /// in grado di eseguire — e non cambia mentre si scrive nel filtro: un contatore che segue il
    /// filtro direbbe quanto si sta cercando, non quanto il conto opera.</para>
    /// </summary>
    private void ApplyStrategiesFilter()
    {
        var filtro = _strategiesFilterTextBox.Text.Trim();
        var prefisso = StrategyFilters.WantedPrefix(_strategiesPrefixTextBox);

        _visibleStrategies.RaiseListChangedEvents = false;
        _visibleStrategies.Clear();
        foreach (var row in _supported.Where(row =>
                     Matches(row, filtro) && StrategyFilters.StartsWithPrefix(row.Code, prefisso)))
        {
            _visibleStrategies.Add(row);
        }

        _visibleStrategies.RaiseListChangedEvents = true;
        _visibleStrategies.ReapplySort();
        _visibleStrategies.ResetBindings();

        var attiveSupportate = _supported.Count(row => row.IsActive);
        var attiveWorkspace = _catalog.Count(item => item.IsActive);

        _strategiesCountLabel.Text = DescribeEmptyScope()
            ?? $"{attiveSupportate}/{attiveWorkspace} strategie attive del workspace" +
               (filtro.Length > 0 || prefisso is not null ? $"  ·  {_visibleStrategies.Count} nel filtro" : string.Empty);
    }

    /// <summary>
    /// Perche' non c'e' niente da contare, o null quando invece c'e'. Le tre cause si risolvono in
    /// posti diversi — scegliere un workspace, riempirne il masterfilter, far tornare il catalogo —
    /// e una griglia vuota da sola non le distingue.
    /// </summary>
    private string? DescribeEmptyScope()
    {
        if (_workspaceId.Length == 0)
        {
            return "nessun workspace selezionato";
        }

        if (_catalog.Count > 0)
        {
            return null;
        }

        return _catalogTotal == 0
            ? "catalogo non disponibile"
            : $"nessuna strategia nel masterfilter del workspace '{_workspaceId}'";
    }

    private void OnExcludedFilterChanged(object? sender, EventArgs e) => ApplyExcludedFilter();

    /// <summary>
    /// Il gemello di <see cref="ApplyStrategiesFilter"/> sul tab delle escluse.
    ///
    /// <para>Il contatore e' nello stesso formato — <b>n/k strategie attive del workspace</b> —
    /// perche' i due tab rispondono alla stessa domanda da due lati: quanta parte del workspace
    /// questo conto opera, e quanta ne perde. Sommati fanno le attive del masterfilter, ed e' cosi'
    /// che si legge se un numero non torna.</para>
    ///
    /// <para>Un conto senza tabella di conversione non esclude niente e lo dice: una lista vuota da
    /// sola non distingue "li supporta tutti" da "le strategie del workspace non sono arrivate".</para>
    /// </summary>
    private void ApplyExcludedFilter()
    {
        var filtro = _excludedFilterTextBox.Text.Trim();
        var prefisso = StrategyFilters.WantedPrefix(_excludedPrefixTextBox);

        _visibleExcluded.RaiseListChangedEvents = false;
        _visibleExcluded.Clear();
        foreach (var row in _excluded.Where(row =>
                     Matches(row, filtro) && StrategyFilters.StartsWithPrefix(row.Code, prefisso)))
        {
            _visibleExcluded.Add(row);
        }

        _visibleExcluded.RaiseListChangedEvents = true;
        _visibleExcluded.ReapplySort();
        _visibleExcluded.ResetBindings();

        if (DescribeEmptyScope() is { } vuoto)
        {
            _excludedCountLabel.Text = vuoto;
            return;
        }

        if (_excluded.Count == 0)
        {
            _excludedCountLabel.Text = HasSymbolTable()
                ? "nessuna esclusione: il conto opera tutti i simboli del workspace"
                : "nessuna esclusione: il conto non ha tabella di conversione, opera 1 a 1";
            return;
        }

        var attiveWorkspace = _catalog.Count(item => item.IsActive);
        _excludedCountLabel.Text =
            $"{_excluded.Count(row => row.IsActive)}/{attiveWorkspace} strategie attive del workspace escluse" +
            (filtro.Length > 0 || prefisso is not null ? $"  ·  {_visibleExcluded.Count} nel filtro" : string.Empty);
    }

    /// <summary>La tabella scelta nella combo ha almeno una riga.</summary>
    private bool HasSymbolTable()
    {
        var code = (_symbolConversionCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty;
        return _conversions.Any(item =>
            string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)
            && item.Mappings.Count > 0);
    }

    private static bool Matches(AccountStrategyRow row, string filtro)
        => filtro.Length == 0
           || row.Code.Contains(filtro, StringComparison.OrdinalIgnoreCase)
           || row.Symbol.Contains(filtro, StringComparison.OrdinalIgnoreCase)
           || row.AccountSymbol.Contains(filtro, StringComparison.OrdinalIgnoreCase)
           || row.TimeframeMinutes.ToString().Contains(filtro, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Un codice già persistito ma non più presente nel registro compare come «non più presente»
    /// invece di essere scartato: il salvataggio riscrive l'account intero, quindi perderlo in
    /// silenzio azzererebbe un riferimento che un run potrebbe usare ancora.
    /// </summary>
    private void FillSymbolConversionCombo(IReadOnlyList<SymbolConversion> conversions, string currentCode)
    {
        _symbolConversionCombo.Items.Clear();
        _symbolConversionCombo.Items.Add(ValueComboItem.None("(nessuna conversione — 1 a 1)"));
        foreach (var conversion in conversions.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            _symbolConversionCombo.Items.Add(ValueComboItem.Of(
                conversion.Code, $"{conversion.Name}  ·  {conversion.Code}  ·  {conversion.Mappings.Count} simboli"));
        }

        if (string.IsNullOrWhiteSpace(currentCode))
        {
            _symbolConversionCombo.SelectedIndex = 0;
            return;
        }

        for (var index = 0; index < _symbolConversionCombo.Items.Count; index++)
        {
            if (_symbolConversionCombo.Items[index] is ValueComboItem item &&
                string.Equals(item.Id, currentCode, StringComparison.OrdinalIgnoreCase))
            {
                _symbolConversionCombo.SelectedIndex = index;
                return;
            }
        }

        _symbolConversionCombo.Items.Add(ValueComboItem.Missing(currentCode));
        _symbolConversionCombo.SelectedIndex = _symbolConversionCombo.Items.Count - 1;
    }

    private WorkspaceAccount BuildAccount() => new()
    {
        Id = _accountId ?? string.Empty,
        Name = _nameTextBox.Text.Trim(),
        AccountNumber = _accountNumberTextBox.Text.Trim(),
        GroupId = _groupCombo.Text.Trim(),
        BrokerCode = (_brokerCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty,
        // L'etichetta storica resta quella caricata: non e' piu' un campo di questa schermata, e
        // riscriverla dal codice del broker cambierebbe in silenzio i marcatori dei run gia' fatti.
        Broker = _loaded?.Broker ?? string.Empty,
        Currency = string.IsNullOrWhiteSpace(_currencyCombo.Text) ? "USD" : _currencyCombo.Text.Trim(),
        InitialBalance = _initialBalanceInput.Value,
        Enabled = _enabledCheckBox.Checked,
        Notes = _notesTextBox.Text.Trim(),
        CreatedUtc = _loaded?.CreatedUtc ?? default,
        UpdatedUtc = _loaded?.UpdatedUtc ?? default,
        // Con un broker scelto la tabella e' sua e sul conto non si scrive: due posti che
        // dichiarano la stessa cosa sono la premessa della divergenza.
        SymbolConversionCode = _brokerCombo.SelectedItem is ValueComboItem { Id.Length: > 0 }
            ? string.Empty
            : (_symbolConversionCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty
    };

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

    /// <summary>
    /// Cambiare tabella cambia l'universo operativo del conto: l'elenco segue subito la scelta,
    /// senza aspettare il salvataggio. Vedere prima quali strategie si perdono e' il punto del tab.
    /// </summary>
    private void OnSymbolConversionChanged(object? sender, EventArgs e)
    {
        MarkDirty();
        RefreshSupportedStrategies();
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (_nameTextBox.Text.Trim().Length == 0)
        {
            MessageBox.Show(this, "Il nome dell'account è obbligatorio.", "Salvataggio account",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameTextBox.Focus();
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var account = BuildAccount();
            var saved = IsNew
                ? await _context.Services.Api.CreateAccountAsync(account)
                : await _context.Services.Api.SaveAccountAsync(_accountId!, account);

            _accountId = saved.Id;
            _loaded = saved;
            _suspendDirtyTracking = true;
            var conversions = await _context.Services.Api.ListSymbolConversionsAsync();
            BindAccount(saved, conversions);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _context.Navigation.SetStatus($"Account '{saved.Name}' salvato.");
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

    private async void OnRevertRequested(object? sender, EventArgs e)
    {
        if (IsNew)
        {
            _context?.Navigation.GoBack();
            return;
        }

        await LoadAsync(CancellationToken.None);
    }

    private void OnBackRequested(object? sender, EventArgs e)
    {
        if (_isDirty)
        {
            var confirm = MessageBox.Show(
                this,
                "Ci sono modifiche non salvate. Vuoi abbandonarle?",
                "Modifiche non salvate",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetDirty(false);
        }

        _context?.Navigation.GoBack();
    }
}

/// <summary>
/// Riga del tab Strategie del dettaglio account: una strategia del workspace che questo conto puo'
/// operare, con il nome che il suo simbolo ha sul broker.
/// </summary>
public sealed class AccountStrategyRow
{
    public string Code { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    /// <summary>Simbolo sul broker del conto; «(1 a 1)» quando non c'e' tabella di conversione.</summary>
    public string AccountSymbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    [Browsable(false)]
    public bool IsActive { get; set; }

    /// <summary>Colonna della griglia: <see cref="IsActive"/> in forma leggibile.</summary>
    public string ActiveText => IsActive ? "si" : "no";

    /// <summary>Cosa la strategia vuole tenere: intraday, overnight, overnight+overweek.</summary>
    public string Holding { get; set; } = string.Empty;

    /// <summary>
    /// Perche' il conto non la opera. Vuoto sulle strategie supportate, dove non c'e' niente da
    /// spiegare: la colonna esiste solo sul tab delle escluse.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
