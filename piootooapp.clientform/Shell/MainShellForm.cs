using Piootoo.Shared;

namespace piootooapp.clientform.Shell;

/// <summary>
/// Finestra principale della console: menu di navigazione a sinistra, area contenuti a destra.
/// Le schermate vengono impilate (lista → dettaglio) e la breadcrumb mostra il percorso.
/// </summary>
public partial class MainShellForm : Form, INavigationHost
{
    private readonly AppServices _services = new();
    private readonly List<Control> _stack = new();
    private ShellContext? _context;
    private CancellationTokenSource? _activationCts;

    /// <summary>
    /// Ultima versione server già segnalata con un alert. L'avviso deve comparire una volta per
    /// server, non a ogni "Applica" sull'URL: un popup che si ripresenta a ogni gesto smette di
    /// essere letto, ed è proprio quello che non si vuole per un disallineamento di versione.
    /// </summary>
    private string? _alertedServerVersion;

    public MainShellForm()
    {
        InitializeComponent();
    }

    private ShellContext Context => _context ??= new ShellContext(_services, this);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (DesignMode)
        {
            return;
        }

        _serverUrlTextBox.Text = _services.ServerUrl;
        Text = $"Piootoo Console v{PiootooVersion.Current}";
        BuildNavigationTree();
        UpdateThemeMenuCheckState();
        ApplyTheme();
        SetStatus("Pronto.");
        _ = CheckServerVersionAsync();
    }

    /// <summary>
    /// Confronta la versione compilata nella console con quella dichiarata dal server e, se
    /// differiscono, lo dice con un alert.
    ///
    /// <para>Il confronto non è tautologico anche se console e server leggono la stessa costante:
    /// la console si ricompila dalla solution mentre il server gira spesso da una cartella
    /// pubblicata a parte, che può essere di una build precedente. È esattamente il caso in cui i
    /// contratti divergono e i sintomi non parlano di versioni — campi che arrivano null, endpoint
    /// che rispondono 404, sessioni che si aprono ma non valutano nulla.</para>
    ///
    /// <para>Server irraggiungibile non è un errore da popup: all'avvio è normalissimo che il
    /// server non sia ancora su, quindi finisce solo nella barra di stato.</para>
    /// </summary>
    private async Task CheckServerVersionAsync()
    {
        string serverVersion;
        Piootoo.Shared.Models.Diagnostics.ServerVersionInfo info;
        try
        {
            info = await _services.ServerInfo.GetVersionAsync();
            serverVersion = info.Version;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                SetError($"Versione server non verificata: {ex.Message}");
            }

            return;
        }

        if (IsDisposed)
        {
            return;
        }

        if (string.Equals(serverVersion, PiootooVersion.Current, StringComparison.Ordinal))
        {
            SetStatus($"Console e server allineati sulla v{serverVersion}.");
            return;
        }

        SetError($"Versione disallineata: console v{PiootooVersion.Current}, server v{serverVersion}.");

        if (_alertedServerVersion == serverVersion)
        {
            return;
        }

        _alertedServerVersion = serverVersion;
        MessageBox.Show(
            this,
            $"La console e il server non hanno la stessa versione.{Environment.NewLine}{Environment.NewLine}" +
            $"Console : v{PiootooVersion.Current}{Environment.NewLine}" +
            $"Server  : v{serverVersion}{Environment.NewLine}" +
            $"in ascolto su {_services.ServerUrl}{Environment.NewLine}" +
            $"avviato il {info.StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}" +
            $"da {info.ContentRootPath}{Environment.NewLine}{Environment.NewLine}" +
            "Di solito significa che il server gira da una build pubblicata più vecchia: i contratti " +
            "possono differire e gli errori che ne derivano non parlano di versioni. Puoi continuare, " +
            "ma se qualcosa non torna ripubblica il server prima di cercare altrove.",
            "Versioni disallineate",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _activationCts?.Cancel();
        _activationCts?.Dispose();
        _services.Dispose();
    }

    private void BuildNavigationTree()
    {
        _navigationTree.BeginUpdate();
        _navigationTree.Nodes.Clear();

        foreach (var section in NavigationRegistry.Build())
        {
            var sectionNode = new TreeNode(section.Label) { NodeFont = new Font(_navigationTree.Font, FontStyle.Bold) };
            foreach (var entry in section.Entries)
            {
                var entryNode = new TreeNode(entry.Label) { Tag = entry };
                if (!entry.IsAvailable)
                {
                    // Sul menu scuro il grigio di sistema sparisce: serve un grigio chiaro.
                    entryNode.ForeColor = Color.FromArgb(150, 165, 185);
                    entryNode.ToolTipText = "Schermata non ancora disponibile nella nuova console.";
                }

                sectionNode.Nodes.Add(entryNode);
            }

            _navigationTree.Nodes.Add(sectionNode);
        }

        _navigationTree.ExpandAll();
        _navigationTree.EndUpdate();
    }

    private void OnNavigationNodeClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node.Tag is not NavigationEntry { ScreenFactory: { } factory })
        {
            return;
        }

        if (!ConfirmLeavingCurrentScreen())
        {
            return;
        }

        ClearStack();
        ShowScreen(factory());
    }

    private void OnThemeMenuItemClick(object? sender, EventArgs e)
    {
        var kind = sender switch
        {
            var s when s == _themeGreenMenuItem => ShellThemeKind.Green,
            var s when s == _themeOrangeMenuItem => ShellThemeKind.Orange,
            _ => ShellThemeKind.Blue,
        };

        ShellTheme.SetTheme(kind);
        UpdateThemeMenuCheckState();
        ApplyTheme();
    }

    private void UpdateThemeMenuCheckState()
    {
        _themeBlueMenuItem.Checked = ShellTheme.Current == ShellThemeKind.Blue;
        _themeGreenMenuItem.Checked = ShellTheme.Current == ShellThemeKind.Green;
        _themeOrangeMenuItem.Checked = ShellTheme.Current == ShellThemeKind.Orange;
    }

    // Le tre zone della shell: header (menu + barra server) nella tonalità più scura dell'accento,
    // navigazione a sinistra in tonalità intermedia, area di lavoro chiara con testo scuro.
    // ShowScreen tema la singola schermata con la stessa zona dell'area di lavoro.
    private void ApplyTheme()
    {
        ShellTheme.ApplyMenuStrip(_menuStrip);
        ShellTheme.ApplyStatusStrip(_statusStrip);
        ShellTheme.ApplyZone(_serverPanel, ShellTheme.HeaderZone);
        ShellTheme.ApplyZone(_splitContainer.Panel1, ShellTheme.MenuZone);
        ShellTheme.ApplyZone(_splitContainer.Panel2, ShellTheme.WorkspaceZone);

        _splitContainer.BackColor = ShellTheme.Border;
        _breadcrumbLabel.BackColor = ShellTheme.Card;
        _breadcrumbLabel.ForeColor = ShellTheme.MutedInk;
        _contentPanel.BackColor = ShellTheme.Surface;
        foreach (var screen in _stack)
        {
            ShellTheme.Apply(screen);
        }
    }

    // --- INavigationHost -------------------------------------------------

    public void Push(Control screen) => ShowScreen(screen);

    public void GoBack()
    {
        if (_stack.Count <= 1)
        {
            return;
        }

        var current = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        _contentPanel.Controls.Remove(current);
        current.Dispose();

        var previous = _stack[^1];
        previous.Visible = true;
        UpdateBreadcrumb();
        ActivateAsync(previous);
    }

    public void SetStatus(string message)
    {
        _statusLabel.ForeColor = ShellTheme.MutedInk;
        _statusLabel.Text = message;
    }

    public void SetError(string message)
    {
        _statusLabel.ForeColor = Color.Firebrick;
        _statusLabel.Text = message;
    }

    // --- gestione dello stack -------------------------------------------

    private void ShowScreen(Control screen)
    {
        if (screen is IShellScreen shellScreen)
        {
            shellScreen.Initialize(Context);
        }

        foreach (var existing in _stack)
        {
            existing.Visible = false;
        }

        screen.Dock = DockStyle.Fill;
        _stack.Add(screen);
        _contentPanel.Controls.Add(screen);
        ShellTheme.Apply(screen);
        screen.BringToFront();
        UpdateBreadcrumb();
        ActivateAsync(screen);
    }

    private void ClearStack()
    {
        foreach (var screen in _stack)
        {
            _contentPanel.Controls.Remove(screen);
            screen.Dispose();
        }

        _stack.Clear();
    }

    private async void ActivateAsync(Control screen)
    {
        // Il caricamento in corso appartiene a una schermata che sta per essere distrutta:
        // senza annullarlo il completamento arriverebbe su controlli già smontati.
        _activationCts?.Cancel();
        _activationCts?.Dispose();
        _activationCts = new CancellationTokenSource();
        var token = _activationCts.Token;

        if (screen is not IShellScreen shellScreen)
        {
            return;
        }

        // Lo stato va scritto *prima* della await: le schermate lo scrivono alla fine del
        // caricamento, quindi finché il server non risponde la barra mostrava ancora il
        // risultato precedente e sembrava che non stesse succedendo niente.
        SetStatus("Caricamento…");
        _contentPanel.UseWaitCursor = true;
        try
        {
            await shellScreen.LoadAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            if (!IsDisposed)
            {
                _contentPanel.UseWaitCursor = false;
            }
        }

        if (!token.IsCancellationRequested && !IsDisposed)
        {
            // Una schermata che non ha nulla da dire non deve lasciare "Caricamento…" appeso.
            if (_statusLabel.Text == "Caricamento…")
            {
                SetStatus("Pronto.");
            }

            // Il titolo di un dettaglio è noto solo dopo il caricamento.
            UpdateBreadcrumb();
        }
    }

    private void UpdateBreadcrumb()
        => _breadcrumbLabel.Text = string.Join("  ›  ", _stack.Select(screen =>
            screen is IShellScreen shellScreen ? shellScreen.ScreenTitle : screen.Text));

    private bool ConfirmLeavingCurrentScreen()
    {
        if (_stack.Count == 0 || _stack[^1] is not IDirtyAware { HasUnsavedChanges: true })
        {
            return true;
        }

        return MessageBox.Show(
            this,
            "Ci sono modifiche non salvate. Vuoi abbandonarle?",
            "Modifiche non salvate",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmLeavingCurrentScreen())
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    // --- barra server e menu ---------------------------------------------

    private void OnApplyServerUrlClick(object? sender, EventArgs e)
    {
        try
        {
            _services.SetServerUrl(_serverUrlTextBox.Text);
            SetStatus($"Server impostato su {_services.ServerUrl}.");

            // Altro indirizzo, altro processo: l'alert già mostrato non vale più per questo server.
            _alertedServerVersion = null;
            _ = CheckServerVersionAsync();

            if (_stack.Count > 0)
            {
                ActivateAsync(_stack[^1]);
            }
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private void OnOpenLegacyConsoleClick(object? sender, EventArgs e)
    {
        var legacy = new WorkspaceBacktestingForm();
        legacy.FormClosed += (_, _) => legacy.Dispose();
        legacy.Show(this);
    }

    private void OnExitClick(object? sender, EventArgs e) => Close();

    /// <summary>
    /// Apre il monitor diagnostico dello stato server. Sta nel menu e non fra le voci di
    /// navigazione perché non è un'entità del dominio da elencare: è una lente sul processo server,
    /// utile da qualunque punto della console si stia lavorando.
    /// </summary>
    private void OnOpenServerStateClick(object? sender, EventArgs e)
    {
        if (!ConfirmLeavingCurrentScreen())
        {
            return;
        }

        ClearStack();
        ShowScreen(new Screens.ServerSessionMonitorScreen());
    }

    private void OnRefreshCurrentScreenClick(object? sender, EventArgs e)
    {
        if (_stack.Count > 0)
        {
            ActivateAsync(_stack[^1]);
        }
    }
}
