using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Dettaglio di un broker. Con <see cref="SetCode"/> a null è la schermata di creazione: il codice
/// resta editabile finché non si salva, dopodiché è l'identificativo con cui conti e piani lo
/// referenziano e non è più modificabile.
/// </summary>
public partial class BrokerDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    private readonly List<SymbolConversion> _conversions = new();
    private ShellContext? _context;
    private string? _code;
    private TradingBroker? _loaded;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public BrokerDetailScreen()
    {
        InitializeComponent();
    }

    public string ScreenTitle => IsNew
        ? "Nuovo broker"
        : _loaded?.Name is { Length: > 0 } name ? name : _code ?? "Broker";

    public bool HasUnsavedChanges => _isDirty;

    private bool IsNew => string.IsNullOrWhiteSpace(_code);

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. Null significa nuovo broker.</summary>
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
            await LoadConversionsAsync(cancellationToken);

            if (IsNew)
            {
                _loaded = null;
                _codeTextBox.ReadOnly = false;
                Bind(new TradingBroker { Enabled = true });
                _context.Navigation.SetStatus("Nuovo broker.");
            }
            else
            {
                var broker = await _context.Services.Api.GetBrokerAsync(_code!, cancellationToken);
                _loaded = broker;
                _codeTextBox.ReadOnly = true;
                Bind(broker);
                _context.Navigation.SetStatus($"Broker '{broker.Name}' caricato.");
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

    /// <summary>
    /// Le tabelle di conversione fra cui scegliere. Una tabella scritta sul broker ma sparita dal
    /// registro resta selezionabile: il salvataggio riscrive il broker intero, quindi scartarla qui
    /// la cancellerebbe.
    /// </summary>
    private async Task LoadConversionsAsync(CancellationToken cancellationToken)
    {
        _conversions.Clear();
        try
        {
            _conversions.AddRange(await _context!.Services.Api.ListSymbolConversionsAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context!.Navigation.SetError($"Tabelle di conversione non leggibili: {ex.Message}");
        }
    }

    private void Bind(TradingBroker broker)
    {
        _toolbar.Title = IsNew ? "Nuovo broker" : broker.Name;
        _nameTextBox.Text = broker.Name;
        _codeTextBox.Text = broker.Code;
        _datafeedTextBox.Text = broker.DatafeedFolder;
        _notesTextBox.Text = broker.Notes;
        _enabledCheckBox.Checked = broker.Enabled;

        var items = new List<ValueComboItem> { ValueComboItem.Blank("(nessuna: simboli 1 a 1)") };
        items.AddRange(_conversions
            .Where(conversion => !string.IsNullOrWhiteSpace(conversion.Code))
            .Select(conversion => ValueComboItem.Of(conversion.Code, $"{conversion.Name}  ·  {conversion.Code}")));

        if (!string.IsNullOrWhiteSpace(broker.SymbolConversionCode) &&
            !items.Any(item => string.Equals(item.Id, broker.SymbolConversionCode, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(ValueComboItem.Missing(broker.SymbolConversionCode));
        }

        _conversionCombo.DisplayMember = nameof(ValueComboItem.Display);
        _conversionCombo.ValueMember = nameof(ValueComboItem.Id);
        _conversionCombo.DataSource = items;
        _conversionCombo.SelectedIndex = Math.Max(0, items.FindIndex(item =>
            string.Equals(item.Id, broker.SymbolConversionCode ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

        _identityLabel.Text = IsNew
            ? "Il codice è l'identificativo con cui conti e piani referenziano il broker, ed è il nome " +
              "cartella sotto datafeed-external/ se non ne indichi un altro: non è più modificabile dopo il salvataggio."
            : $"Creato {broker.CreatedUtc:yyyy-MM-dd HH:mm} UTC  ·  aggiornato {broker.UpdatedUtc:yyyy-MM-dd HH:mm} UTC";
    }

    private TradingBroker Read() => new()
    {
        Code = _codeTextBox.Text.Trim(),
        Name = _nameTextBox.Text.Trim(),
        SymbolConversionCode = (_conversionCombo.SelectedItem as ValueComboItem)?.Id ?? string.Empty,
        DatafeedFolder = _datafeedTextBox.Text.Trim(),
        Enabled = _enabledCheckBox.Checked,
        Notes = _notesTextBox.Text.Trim()
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

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        var broker = Read();
        if (broker.Code.Length == 0)
        {
            MessageBox.Show(this, "Il codice del broker è obbligatorio.", "Broker",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var saved = IsNew
                ? await _context.Services.Api.CreateBrokerAsync(broker)
                : await _context.Services.Api.SaveBrokerAsync(_code!, broker);

            _code = saved.Code;
            _loaded = saved;
            _codeTextBox.ReadOnly = true;
            _suspendDirtyTracking = true;
            Bind(saved);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _context.Navigation.SetStatus($"Broker '{saved.Name}' salvato.");
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
}
