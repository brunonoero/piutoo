using System.ComponentModel;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Dettaglio di una tabella di conversione simbolo del registro globale. Con <see cref="SetCode"/>
/// a null è la schermata di creazione: il codice resta editabile finché non si salva, dopodiché è
/// l'identificativo con cui gli account la referenziano e non è più modificabile.
/// </summary>
public partial class SymbolConversionDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    // Griglia editabile: resta BindingList<T> e non ordinabile per colonna, perché l'ordine è
    // quello in cui si sta scrivendo. Vedi .cursor/rules/piutoo-console-screens.mdc.
    private readonly BindingList<AccountSymbolMapping> _mappings = new();
    private ShellContext? _context;
    private string? _code;
    private SymbolConversion? _loaded;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public SymbolConversionDetailScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _mappingsBindingSource.DataSource = _mappings;
        // Deferred non è una scelta significativa qui: la impone il motore alle sessioni
        // ExternalBroker (vedi InstrumentMetadata.RoundingMode), non la sceglie chi compila la
        // riga di conversione.
        _colRoundingMode.DataSource = Enum.GetValues<QuantityRoundingMode>()
            .Where(mode => mode != QuantityRoundingMode.Deferred)
            .ToArray();
        _mappings.ListChanged += (_, _) => MarkDirty();
    }

    public string ScreenTitle => IsNew
        ? "Nuova tabella di conversione"
        : _loaded?.Name is { Length: > 0 } name ? name : _code ?? "Tabella di conversione";

    public bool HasUnsavedChanges => _isDirty;

    private bool IsNew => string.IsNullOrWhiteSpace(_code);

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. Null significa nuova tabella.</summary>
    public void SetCode(string? code) => _code = code;

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
            if (IsNew)
            {
                _loaded = null;
                _codeTextBox.ReadOnly = false;
                Bind(new SymbolConversion());
                _context.Navigation.SetStatus("Nuova tabella di conversione.");
            }
            else
            {
                var conversion = await _context.Services.Api.GetSymbolConversionAsync(_code!, cancellationToken);
                _loaded = conversion;
                _codeTextBox.ReadOnly = true;
                Bind(conversion);
                _context.Navigation.SetStatus($"Tabella di conversione '{conversion.Name}' caricata.");
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

    private static AccountSymbolMapping Clone(AccountSymbolMapping mapping) => new()
    {
        Symbol = mapping.Symbol,
        AccountSymbol = mapping.AccountSymbol,
        ContractMultiplier = mapping.ContractMultiplier,
        MinimumQuantity = mapping.MinimumQuantity,
        QuantityStep = mapping.QuantityStep,
        RoundingMode = mapping.RoundingMode,
        Enabled = mapping.Enabled
    };

    private void Bind(SymbolConversion conversion)
    {
        _toolbar.Title = IsNew ? "Nuova tabella di conversione" : conversion.Name;
        _nameTextBox.Text = conversion.Name;
        _codeTextBox.Text = conversion.Code;
        _identityLabel.Text = IsNew
            ? "Il codice è l'identificativo con cui gli account la referenziano: non è più modificabile dopo il primo salvataggio."
            : $"Creata {conversion.CreatedUtc:yyyy-MM-dd HH:mm} UTC  ·  aggiornata {conversion.UpdatedUtc:yyyy-MM-dd HH:mm} UTC";

        _mappings.RaiseListChangedEvents = false;
        _mappings.Clear();
        foreach (var mapping in conversion.Mappings)
        {
            _mappings.Add(Clone(mapping));
        }

        _mappings.RaiseListChangedEvents = true;
        _mappings.ResetBindings();
    }

    private SymbolConversion BuildConversion() => new()
    {
        Code = _codeTextBox.Text.Trim(),
        Name = _nameTextBox.Text.Trim(),
        CreatedUtc = _loaded?.CreatedUtc ?? default,
        UpdatedUtc = _loaded?.UpdatedUtc ?? default,
        Mappings = _mappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Symbol))
            .Select(Clone)
            .ToList()
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
            MessageBox.Show(this, "Il nome della tabella di conversione è obbligatorio.", "Salvataggio",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameTextBox.Focus();
            return;
        }

        if (_codeTextBox.Text.Trim().Length == 0)
        {
            MessageBox.Show(this, "Il codice della tabella di conversione è obbligatorio.", "Salvataggio",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _codeTextBox.Focus();
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var conversion = BuildConversion();
            var saved = IsNew
                ? await _context.Services.Api.CreateSymbolConversionAsync(conversion)
                : await _context.Services.Api.SaveSymbolConversionAsync(_code!, conversion);

            _code = saved.Code;
            _loaded = saved;
            _suspendDirtyTracking = true;
            _codeTextBox.ReadOnly = true;
            Bind(saved);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _context.Navigation.SetStatus($"Tabella di conversione '{saved.Name}' salvata con {saved.Mappings.Count} simboli.");
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

    private void OnRemoveMappingClick(object? sender, EventArgs e)
    {
        if (_mappingsGrid.CurrentRow?.DataBoundItem is AccountSymbolMapping mapping)
        {
            _mappings.Remove(mapping);
        }
    }

    private async void OnLoadIdentityMappingsClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        try
        {
            var identity = await _context.Services.Api.GetSymbolIdentityAsync();
            _mappings.RaiseListChangedEvents = false;
            _mappings.Clear();
            foreach (var mapping in identity)
            {
                _mappings.Add(Clone(mapping));
            }

            _mappings.RaiseListChangedEvents = true;
            _mappings.ResetBindings();
            MarkDirty();
            _context.Navigation.SetStatus($"Tabella riempita dal catalogo: {identity.Count} simboli, moltiplicatore 1.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
    }
}
