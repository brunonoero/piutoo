using System.ComponentModel;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Preset condiviso di conversione simboli. Non è una lista: è un unico documento globale,
/// quindi la schermata è direttamente un editor senza livello di elenco sopra.
/// Modificarlo <b>non</b> tocca gli account già creati: ognuno porta la propria copia.
/// </summary>
public partial class SymbolPresetScreen : UserControl, IShellScreen, IDirtyAware
{
    private readonly BindingList<AccountSymbolMapping> _mappings = new();
    private ShellContext? _context;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public SymbolPresetScreen()
    {
        InitializeComponent();
        _bindingSource.DataSource = _mappings;
        _mappings.ListChanged += (_, _) =>
        {
            if (!_suspendDirtyTracking)
            {
                SetDirty(true);
            }
        };
    }

    public string ScreenTitle => "Conversioni simbolo";

    public bool HasUnsavedChanges => _isDirty;

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
            var preset = await _context.Services.Api.GetSymbolConversionPresetAsync(cancellationToken);
            Fill(preset);
            SetDirty(false);
            _context.Navigation.SetStatus($"Preset di conversione: {preset.Count} simboli.");
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

    private void Fill(IReadOnlyList<AccountSymbolMapping> mappings)
    {
        _mappings.RaiseListChangedEvents = false;
        _mappings.Clear();
        foreach (var mapping in mappings)
        {
            _mappings.Add(new AccountSymbolMapping
            {
                Symbol = mapping.Symbol,
                AccountSymbol = mapping.AccountSymbol,
                ContractMultiplier = mapping.ContractMultiplier,
                Enabled = mapping.Enabled
            });
        }

        _mappings.RaiseListChangedEvents = true;
        _mappings.ResetBindings();
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        _toolbar.SetDirty(dirty);
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var saved = await _context.Services.Api.SaveSymbolConversionPresetAsync(_mappings
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Symbol))
                .ToList());
            _suspendDirtyTracking = true;
            Fill(saved);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _context.Navigation.SetStatus($"Preset salvato: {saved.Count} simboli.");
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

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnRemoveRowClick(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Index is { } index && index >= 0 && index < _mappings.Count)
        {
            _mappings.RemoveAt(index);
        }
    }

    private async void OnLoadIdentityClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        try
        {
            var identity = await _context.Services.Api.GetSymbolIdentityAsync();
            Fill(identity);
            SetDirty(true);
            _context.Navigation.SetStatus(
                $"Caricata la tabella identità dal catalogo: {identity.Count} simboli, moltiplicatore 1.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
    }
}
