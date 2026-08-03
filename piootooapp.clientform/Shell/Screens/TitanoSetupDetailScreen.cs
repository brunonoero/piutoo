using Piootoo.Shared.Models.Optimization;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Dettaglio di un setup di rotazione Titano.
///
/// <para>Nome e descrizione hanno campi propri perché sono ciò con cui il setup si sceglie
/// altrove; i circa trenta parametri numerici stanno in un <see cref="PropertyGrid"/> legato
/// direttamente a <see cref="TitanoRotationSetup"/>. È una scelta deliberata: replicarli a mano
/// significherebbe una seconda dichiarazione dello stesso modello, che resterebbe indietro al
/// primo parametro aggiunto — e un parametro assente dalla UI viene salvato al proprio default
/// senza che nulla lo segnali.</para>
///
/// <para>Il setup è globale: non appartiene a un workspace e si applica a quanti se ne vuole.
/// Quello che è per workspace è il <em>run</em>, che nasce dai trade di uno specifico backtest.</para>
/// </summary>
public partial class TitanoSetupDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    private ShellContext? _context;
    private TitanoRotationSetup _setup = new();
    private string? _setupId;
    private bool _isNew;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public TitanoSetupDetailScreen() => InitializeComponent();

    public string ScreenTitle => _isNew
        ? "Nuovo setup Titano"
        : _setupId is { Length: > 0 } id ? $"Setup {id}" : "Setup Titano";

    public bool HasUnsavedChanges => _isDirty;

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. Null = setup nuovo.</summary>
    public void SetSetup(string? setupId)
    {
        _setupId = setupId;
        _isNew = string.IsNullOrEmpty(setupId);
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
            if (_isNew)
            {
                _toolbar.Title = "Nuovo setup Titano";
                // I default del modello sono quelli documentati in docs/domini/titano-rotation.md:
                // partire da lì è più onesto che partire da zeri che nessuno userebbe.
                _setup = new TitanoRotationSetup();
                Fill();
                _context.Navigation.SetStatus("Nuovo setup di rotazione, con i default del modello.");
                return;
            }

            _setup = await _context.Services.Titano.GetSetupAsync(_setupId!, cancellationToken);
            _toolbar.Title = $"Setup {_setup.Name}";
            Fill();
            _context.Navigation.SetStatus(
                $"Setup '{_setup.Name}' (id {_setup.Id})" +
                (_setup.UpdatedAt is { } updated ? $", aggiornato il {updated:yyyy-MM-dd HH:mm} UTC." : "."));
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

    private void Fill()
    {
        _nameTextBox.Text = _setup.Name;
        _idTextBox.Text = _setup.Id;
        _descriptionTextBox.Text = _setup.Description;
        _parametersGrid.SelectedObject = _setup;
        _parametersGrid.Refresh();
    }

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

    private void OnParameterChanged(object? sender, PropertyValueChangedEventArgs e) => MarkDirty();

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        var name = _nameTextBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, "Il nome del setup è obbligatorio.", "Setup Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _setup.Name = name;
        _setup.Description = _descriptionTextBox.Text.Trim();
        // L'id lo assegna il server, come slug del nome, e non cambia più dopo la prima scrittura:
        // è la chiave con cui i piani referenziano il setup.
        _setup.Id = _isNew ? string.Empty : _setup.Id;

        _toolbar.SetBusy(true);
        try
        {
            var saved = await _context.Services.Titano.SaveSetupAsync(_setup);
            _setup = saved;
            _setupId = saved.Id;
            _isNew = false;
            _suspendDirtyTracking = true;
            Fill();
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.Title = $"Setup {saved.Name}";
            _context.Navigation.SetStatus($"Setup '{saved.Name}' salvato con id {saved.Id}.");
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
