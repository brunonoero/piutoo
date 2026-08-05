using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell;

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

    public AccountDetailScreen()
    {
        InitializeComponent();
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
    }

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
