using System.ComponentModel;
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
///
/// <para><b>Tre livelli di aiuto, perché trenta parametri corretti non dicono cosa faranno.</b>
/// Il grid parte in vista <em>Base</em> — dieci parametri, filtrati via
/// <see cref="PropertyGrid.BrowsableAttributes"/> su <see cref="TitanoLevelAttribute"/> — e la
/// spunta apre il resto. La combo dei preset copia dentro un setup professionale già calibrato,
/// così si parte da qualcosa che funziona invece che dai default. Il riquadro in basso, infine,
/// riscrive la configurazione corrente in prosa e ne elenca le incoerenze: è l'unico posto in cui
/// si vede l'effetto <em>combinato</em> dei parametri, che è dove si sbaglia.</para>
/// </summary>
public partial class TitanoSetupDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    /// <summary>Filtro del grid per la vista Base. Va costruito una volta: è confrontato per valore a ogni refresh.</summary>
    private static readonly AttributeCollection BaseOnlyFilter =
        new(new TitanoLevelAttribute(TitanoParameterLevel.Base));

    /// <summary>Filtro predefinito del <see cref="PropertyGrid"/>: tutto ciò che è browsable.</summary>
    private static readonly AttributeCollection EverythingFilter =
        new(new BrowsableAttribute(true));

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
            ApplyLevelFilter();

            if (_isNew)
            {
                _toolbar.Title = "Nuovo setup Titano";
                // I default del modello sono quelli documentati in docs/domini/titano-rotation.md:
                // partire da lì è più onesto che partire da zeri che nessuno userebbe.
                _setup = new TitanoRotationSetup();
                Fill();
                _context.Navigation.SetStatus(
                    "Nuovo setup di rotazione. Se non sai da dove partire, applica un preset invece dei default.");
            }
            else
            {
                _setup = await _context.Services.Titano.GetSetupAsync(_setupId!, cancellationToken);
                _toolbar.Title = $"Setup {_setup.Name}";
                Fill();
                _context.Navigation.SetStatus(
                    $"Setup '{_setup.Name}' (id {_setup.Id})" +
                    (_setup.UpdatedAt is { } updated ? $", aggiornato il {updated:yyyy-MM-dd HH:mm} UTC." : "."));
            }

            await LoadPresetsAsync(cancellationToken);
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
    /// I preset sono setup salvati come tutti gli altri: il server ne semina tre professionali
    /// (Conservativo, Bilanciato, Dinamico) alla prima esecuzione. Elencarli tutti invece dei soli
    /// tre predefiniti è voluto — un setup che si è già calibrato è il miglior punto di partenza
    /// per il successivo.
    /// </summary>
    private async Task LoadPresetsAsync(CancellationToken cancellationToken)
    {
        _presetCombo.Items.Clear();
        try
        {
            var setups = await _context!.Services.Titano.ListSetupsAsync(cancellationToken);
            foreach (var info in setups.Where(x => x.Id != _setupId).OrderBy(x => x.Name))
            {
                _presetCombo.Items.Add(new PresetItem(info));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Un elenco preset non recuperabile non deve impedire di modificare il setup aperto.
        }

        var available = _presetCombo.Items.Count > 0;
        _presetCombo.Enabled = available;
        _presetApplyButton.Enabled = available;
        if (available)
        {
            _presetCombo.SelectedIndex = 0;
        }
    }

    private void Fill()
    {
        _nameTextBox.Text = _setup.Name;
        _idTextBox.Text = _setup.Id;
        _descriptionTextBox.Text = _setup.Description;
        _parametersGrid.SelectedObject = _setup;
        _parametersGrid.Refresh();
        RefreshSummary();
    }

    /// <summary>
    /// Il <see cref="PropertyGrid"/> mostra solo le proprietà che portano <em>tutti</em> gli
    /// attributi elencati in <see cref="PropertyGrid.BrowsableAttributes"/>, confrontati per valore.
    /// È il motivo per cui <see cref="TitanoLevelAttribute"/> ridefinisce <c>Equals</c>.
    /// </summary>
    private void ApplyLevelFilter()
    {
        _parametersGrid.BrowsableAttributes = _advancedCheckBox.Checked ? EverythingFilter : BaseOnlyFilter;
        if (_parametersGrid.SelectedObject != null)
        {
            _parametersGrid.ExpandAllGridItems();
        }
    }

    /// <summary>
    /// Riscrive il riquadro in basso. Le etichette sono in <c>AutoSize</c>, quindi il ritorno a capo
    /// va imposto con <see cref="Control.MaximumSize"/>: senza, la frase resta su una riga sola e
    /// il pannello guadagna una barra di scorrimento orizzontale.
    ///
    /// <para>La larghezza della barra verticale si sottrae <em>sempre</em>, anche quando non è
    /// visibile. Calcolarla sulla larghezza piena farebbe comparire la barra, che stringe l'area
    /// utile, che allunga il testo, che tiene la barra: un ciclo di layout che si stabilizza solo
    /// per caso. Costa una manciata di pixel e li vale.</para>
    /// </summary>
    private void RefreshSummary()
    {
        var width = Math.Max(
            200,
            _summaryPanel.ClientSize.Width
            - _summaryPanel.Padding.Horizontal
            - SystemInformation.VerticalScrollBarWidth);
        _summaryLabel.MaximumSize = new Size(width, 0);
        _warningsLabel.MaximumSize = new Size(width, 0);

        _summaryLabel.Text = TitanoSetupSummary.Describe(_setup);

        var warnings = TitanoSetupSummary.Warnings(_setup);
        _warningsLabel.Visible = warnings.Count > 0;
        _warningsLabel.Text = warnings.Count == 0
            ? string.Empty
            : "Da sapere:" + Environment.NewLine +
              string.Join(Environment.NewLine, warnings.Select(x => "• " + x));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_summaryLabel != null)
        {
            RefreshSummary();
        }
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

    private void OnParameterChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        MarkDirty();
        // Il riepilogo è utile solo se segue la modifica: un riassunto in ritardo di un parametro
        // è peggio di nessun riassunto.
        RefreshSummary();
    }

    private void OnAdvancedToggled(object? sender, EventArgs e) => ApplyLevelFilter();

    /// <summary>
    /// Copia i parametri del preset dentro il setup aperto, lasciando intatti id, nome e
    /// descrizione: chi applica un preset vuole la sua calibrazione, non la sua identità.
    /// </summary>
    private async void OnApplyPresetClick(object? sender, EventArgs e)
    {
        if (_context == null || _presetCombo.SelectedItem is not PresetItem preset)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Sostituisco tutti i parametri con quelli di '{preset.Info.Name}'?" + Environment.NewLine +
            Environment.NewLine +
            "Nome, descrizione e id di questo setup restano invariati. Le modifiche non salvate ai " +
            "parametri vanno perse.",
            "Applica preset",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            var source = await _context.Services.Titano.GetSetupAsync(preset.Info.Id);

            source.Id = _setup.Id;
            source.Name = _setup.Name;
            source.Description = _setup.Description;
            source.UpdatedAt = _setup.UpdatedAt;
            _setup = source;

            _suspendDirtyTracking = true;
            Fill();
            _suspendDirtyTracking = false;
            SetDirty(true);
            _context.Navigation.SetStatus(
                $"Parametri copiati da '{preset.Info.Name}'. Non è ancora salvato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Applica preset", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

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

    /// <summary>Voce della combo dei preset: la combo mostra il nome, il codice usa l'id.</summary>
    private sealed record PresetItem(TitanoSetupInfo Info)
    {
        public override string ToString() => Info.Name;
    }
}
