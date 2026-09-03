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

    /// <summary>Catalogo completo del server: la base su cui si calcola l'universo del conto.</summary>
    private readonly List<StrategyCatalogItem> _catalog = new();

    /// <summary>Tabelle di conversione del registro globale, per risolvere quella dell'account.</summary>
    private readonly List<SymbolConversion> _conversions = new();

    /// <summary>Strategie che questo conto puo' operare, prima del filtro di testo.</summary>
    private readonly List<AccountStrategyRow> _supported = new();

    /// <summary>
    /// Strategie del catalogo che questo conto <b>non</b> puo' operare, con il motivo.
    ///
    /// <para>Sono il complemento esatto di <see cref="_supported"/> sullo stesso catalogo: le due
    /// liste insieme fanno il catalogo intero, e nessuna strategia sta in tutte e due. E' il punto
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
        _brokerTextBox.Text = account.Broker;
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

        FillSymbolConversionCombo(conversions, account.SymbolConversionCode);
        RefreshSupportedStrategies();
    }

    /// <summary>
    /// Il catalogo del server. Non e' una precondizione della schermata: senza, il tab Strategie
    /// resta vuoto e lo dichiara, ma l'account si modifica e si salva lo stesso.
    /// </summary>
    private async Task LoadStrategyCatalogAsync(CancellationToken cancellationToken)
    {
        _catalog.Clear();
        try
        {
            _catalog.AddRange(await _context!.Services.Api.ListStrategiesAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Catalogo strategie non disponibile: {ex.Message}");
        }
    }

    /// <summary>
    /// L'universo operativo del conto: le strategie il cui simbolo compare, abilitato, nella
    /// tabella di conversione scelta.
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
    /// <para>Il contatore dice <b>n/k strategie attive</b>: <c>k</c> sono le attive dell'intero
    /// catalogo, <c>n</c> quelle che questo conto puo' operare. E' la sola forma che risponde alla
    /// domanda vera — quanta parte del sistema questo conto e' in grado di eseguire — e non cambia
    /// mentre si scrive nel filtro: un contatore che segue il filtro direbbe quanto si sta cercando,
    /// non quanto il conto opera.</para>
    /// </summary>
    private void ApplyStrategiesFilter()
    {
        var filtro = _strategiesFilterTextBox.Text.Trim();

        _visibleStrategies.RaiseListChangedEvents = false;
        _visibleStrategies.Clear();
        foreach (var row in _supported.Where(row => Matches(row, filtro)))
        {
            _visibleStrategies.Add(row);
        }

        _visibleStrategies.RaiseListChangedEvents = true;
        _visibleStrategies.ReapplySort();
        _visibleStrategies.ResetBindings();

        var attiveSupportate = _supported.Count(row => row.IsActive);
        var attiveCatalogo = _catalog.Count(item => item.IsActive);

        _strategiesCountLabel.Text = attiveCatalogo == 0
            ? "catalogo non disponibile"
            : $"{attiveSupportate}/{attiveCatalogo} strategie attive" +
              (filtro.Length > 0 ? $"  ·  {_visibleStrategies.Count} nel filtro" : string.Empty);
    }

    private void OnExcludedFilterChanged(object? sender, EventArgs e) => ApplyExcludedFilter();

    /// <summary>
    /// Il gemello di <see cref="ApplyStrategiesFilter"/> sul tab delle escluse.
    ///
    /// <para>Il contatore e' nello stesso formato — <b>n/k strategie attive</b> — perche' i due tab
    /// rispondono alla stessa domanda da due lati: quanta parte del sistema questo conto opera, e
    /// quanta ne perde. Sommati fanno le attive del catalogo, ed e' cosi' che si legge se un numero
    /// non torna.</para>
    ///
    /// <para>Un conto senza tabella di conversione non esclude niente e lo dice: una lista vuota da
    /// sola non distingue "li supporta tutti" da "il catalogo non e' arrivato".</para>
    /// </summary>
    private void ApplyExcludedFilter()
    {
        var filtro = _excludedFilterTextBox.Text.Trim();

        _visibleExcluded.RaiseListChangedEvents = false;
        _visibleExcluded.Clear();
        foreach (var row in _excluded.Where(row => Matches(row, filtro)))
        {
            _visibleExcluded.Add(row);
        }

        _visibleExcluded.RaiseListChangedEvents = true;
        _visibleExcluded.ReapplySort();
        _visibleExcluded.ResetBindings();

        var attiveCatalogo = _catalog.Count(item => item.IsActive);
        if (attiveCatalogo == 0)
        {
            _excludedCountLabel.Text = "catalogo non disponibile";
            return;
        }

        if (_excluded.Count == 0)
        {
            _excludedCountLabel.Text = HasSymbolTable()
                ? "nessuna esclusione: il conto opera tutti i simboli del catalogo"
                : "nessuna esclusione: il conto non ha tabella di conversione, opera 1 a 1";
            return;
        }

        _excludedCountLabel.Text =
            $"{_excluded.Count(row => row.IsActive)}/{attiveCatalogo} strategie attive escluse" +
            (filtro.Length > 0 ? $"  ·  {_visibleExcluded.Count} nel filtro" : string.Empty);
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
        Broker = _brokerTextBox.Text.Trim(),
        Currency = string.IsNullOrWhiteSpace(_currencyCombo.Text) ? "USD" : _currencyCombo.Text.Trim(),
        InitialBalance = _initialBalanceInput.Value,
        Enabled = _enabledCheckBox.Checked,
        Notes = _notesTextBox.Text.Trim(),
        CreatedUtc = _loaded?.CreatedUtc ?? default,
        UpdatedUtc = _loaded?.UpdatedUtc ?? default,
        SymbolConversionCode = (_symbolConversionCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty
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
/// Riga del tab Strategie del dettaglio account: una strategia del catalogo che questo conto puo'
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
