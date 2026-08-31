using System.Text;
using Piootoo.Shared;
using Piootoo.Shared.Models.Diagnostics;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Voce della combo: una sessione viva, oppure tutte.</summary>
public sealed class SessionMonitorTarget
{
    private SessionMonitorTarget(TradingSessionSummary? summary) => Summary = summary;

    /// <summary>Null è la voce "tutte le sessioni".</summary>
    public TradingSessionSummary? Summary { get; }

    public static SessionMonitorTarget All { get; } = new(null);

    public static SessionMonitorTarget Of(TradingSessionSummary summary) => new(summary);

    public override string ToString()
    {
        if (Summary is not { } s)
        {
            return "(tutte le sessioni)";
        }

        var shortId = s.SessionId.Length <= 8 ? s.SessionId : s.SessionId[..8];
        var plan = string.IsNullOrWhiteSpace(s.PlanCode) ? "senza piano" : s.PlanCode;
        return $"{shortId}  ·  {plan}  ·  {s.ExecutionMode}  ·  {s.Status}";
    }
}

/// <summary>
/// Monitor diagnostico on demand dello stato server. Interroga le sessioni vive e ne riversa in una
/// text area tutto ciò che il server espone — riepilogo, snapshot, gruppi, intent, segnali, trade,
/// log di rotazione — in JSON grezzo, pronto da copiare e incollare in un ticket o in chat.
///
/// <para>Non è una schermata operativa e non fa polling: il quadro è quello dell'istante in cui si
/// preme Aggiorna, e le sessioni vivono in RAM nel processo server (spariscono al riavvio). Nasce
/// dal caso "il cBot non apre posizioni": il log del bot dice solo <i>nessun intent</i>, e per
/// distinguere "nessun setup" da "strategia senza barre" serve vedere lo stato dal lato server.</para>
///
/// <para>Il dump è volutamente non tipizzato (<see cref="Api.TradingSessionApiClient.GetRawJsonAsync"/>):
/// una schermata diagnostica che filtrasse i campi attraverso i contratti del client nasconderebbe
/// proprio i campi nuovi che si sta cercando di leggere.</para>
/// </summary>
public partial class ServerSessionMonitorScreen : UserControl, IShellScreen
{
    /// <summary>Risorse interrogate per ogni sessione, nell'ordine in cui compaiono nel report.</summary>
    private static readonly (string Titolo, string Risorsa)[] DiagnosticResources =
    [
        ("snapshot", "snapshot"),
        ("gruppi", "groups"),
        ("intent", "intents?after=0"),
        ("segnali", "signals"),
        ("trade", "trades"),
        ("log di rotazione", "rotation-log")
    ];

    private readonly List<TradingSessionSummary> _sessions = new();
    private ShellContext? _context;
    private bool _suppressComboEvents;
    private ServerVersionInfo? _serverVersion;

    public ServerSessionMonitorScreen()
    {
        InitializeComponent();
    }

    public string ScreenTitle => "Stato server";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var previous = SelectedTarget?.Summary?.SessionId;

            // Best-effort: un server che non espone /version è un server vecchio, ed è esattamente
            // ciò che il report deve poter dire invece di fallire.
            try
            {
                _serverVersion = await _context.Services.ServerInfo.GetVersionAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _serverVersion = null;
            }

            var sessions = await _context.Services.Sessions.ListAsync(cancellationToken);

            _sessions.Clear();
            _sessions.AddRange(sessions);
            RebuildCombo(previous);

            if (_sessions.Count == 0)
            {
                _output.Text = Normalize(BuildHeader(0) + Environment.NewLine
                    + "Nessuna sessione viva sul server." + Environment.NewLine
                    + "Le sessioni stanno in RAM nel processo: un riavvio del server le azzera, e finché "
                    + "un cBot o la console non ne apre una qui non c'è niente da guardare.");
                _context.Navigation.SetStatus("Nessuna sessione viva sul server.");
                return;
            }

            await BuildReportAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            _output.Text = Normalize($"Impossibile interrogare il server.{Environment.NewLine}{ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private SessionMonitorTarget? SelectedTarget => _sessionCombo.SelectedItem as SessionMonitorTarget;

    private void RebuildCombo(string? previousSessionId)
    {
        _suppressComboEvents = true;
        try
        {
            _sessionCombo.Items.Clear();
            if (_sessions.Count > 1)
            {
                _sessionCombo.Items.Add(SessionMonitorTarget.All);
            }

            foreach (var session in _sessions)
            {
                _sessionCombo.Items.Add(SessionMonitorTarget.Of(session));
            }

            if (_sessionCombo.Items.Count == 0)
            {
                return;
            }

            var restored = -1;
            if (previousSessionId != null)
            {
                for (var i = 0; i < _sessionCombo.Items.Count; i++)
                {
                    if (_sessionCombo.Items[i] is SessionMonitorTarget { Summary.SessionId: { } id }
                        && id == previousSessionId)
                    {
                        restored = i;
                        break;
                    }
                }
            }

            _sessionCombo.SelectedIndex = restored >= 0 ? restored : 0;
        }
        finally
        {
            _suppressComboEvents = false;
        }
    }

    private async Task BuildReportAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedTarget is not { } target)
        {
            return;
        }

        var targets = target.Summary is { } single ? new List<TradingSessionSummary> { single } : _sessions;

        var report = new StringBuilder();
        report.Append(BuildHeader(_sessions.Count));

        foreach (var session in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendSessionSummary(report, session);

            foreach (var (titolo, risorsa) in DiagnosticResources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AppendSection(report, $"{titolo}  ({risorsa})");
                try
                {
                    var json = await _context.Services.Sessions.GetRawJsonAsync(
                        session.SessionId, session.SessionToken, risorsa, cancellationToken);
                    report.AppendLine(json);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Una risorsa che non risponde non deve far saltare il resto del dump: spesso è
                    // proprio il buco che si sta cercando (per esempio rotation-log su una sessione
                    // senza Titano), e va letto insieme a ciò che invece ha risposto.
                    report.AppendLine($"!! errore: {ex.Message}");
                }

                report.AppendLine();
            }
        }

        _output.Text = Normalize(report.ToString());
        _output.SelectionStart = 0;
        _output.SelectionLength = 0;
        _output.ScrollToCaret();
        _context.Navigation.SetStatus(
            $"Stato letto alle {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC · {targets.Count} sessioni.");
    }

    private string BuildHeader(int sessionCount)
    {
        var header = new StringBuilder();
        header.AppendLine("========================================================================");
        header.AppendLine("PIOOTOO — STATO SERVER (istantanea on demand)");
        header.AppendLine($"letto il          : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        header.AppendLine($"server API        : {_context?.Services.ServerUrl ?? "(non configurato)"}");
        header.AppendLine($"versione console  : v{PiootooVersion.Current}");

        if (_serverVersion is { } v)
        {
            // Allineato = stesso contratto major.minor. La patch diversa si dice, ma non è un
            // disallineamento: serve proprio a portare una fix su una parte sola.
            header.AppendLine($"versione server   : v{v.Version}"
                + (v.Version == PiootooVersion.Current
                    ? "  (allineata)"
                    : PiootooVersion.IsSameContract(v.Version)
                        ? $"  (contratto {PiootooVersion.Contract} allineato, patch diversa)"
                        : "  >>> DISALLINEATA <<<"));
            header.AppendLine($"server avviato il : {v.StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
            header.AppendLine($"server gira da    : {v.ContentRootPath}  [{v.Environment}]");
        }
        else
        {
            header.AppendLine("versione server   : (non dichiarata — build precedente a /api/v1/version)");
        }

        header.AppendLine($"sessioni vive     : {sessionCount}");
        header.AppendLine("========================================================================");
        header.AppendLine();
        return header.ToString();
    }

    private static void AppendSessionSummary(StringBuilder report, TradingSessionSummary s)
    {
        report.AppendLine("########################################################################");
        report.AppendLine($"# SESSIONE {s.SessionId}");
        report.AppendLine("########################################################################");
        report.AppendLine($"workspace       : {s.WorkspaceId}");
        report.AppendLine($"piano           : {s.PlanCode ?? "(nessuno)"}");
        report.AppendLine($"chiave exec     : {s.ExecutionKey ?? "(nessuna)"}");
        report.AppendLine($"esecuzione      : {s.ExecutionMode}");
        report.AppendLine($"contesto client : {s.ClientRunMode}");
        report.AppendLine($"Titano          : {s.TitanoMode}");
        report.AppendLine($"stato           : {s.Status}");
        report.AppendLine($"aperta il       : {s.CreatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");

        // È il campo che risponde alla domanda "il riscaldamento è arrivato?": se resta null mentre
        // il cBot dichiara di aver inviato le candele, il problema è a monte della strategia.
        report.AppendLine(s.LastBarTimeUtc is { } last
            ? $"ultima barra    : {last:yyyy-MM-dd HH:mm:ss} UTC"
            : "ultima barra    : (nessuna barra ancora ricevuta)");
        report.AppendLine();
    }

    private static void AppendSection(StringBuilder report, string titolo)
    {
        report.AppendLine("------------------------------------------------------------------------");
        report.AppendLine($"-- {titolo}");
        report.AppendLine("------------------------------------------------------------------------");
    }

    /// <summary>
    /// Una TextBox multilinea rende a capo solo su CRLF: il JSON del serializzatore usa LF e
    /// finirebbe tutto su una riga sola.
    /// </summary>
    private static string Normalize(string text)
        => text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

    private void SetBusy(bool busy)
    {
        _sessionCombo.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _copyButton.Enabled = !busy;
        _saveButton.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private async void OnRefreshClick(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        if (_suppressComboEvents || _context == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            await BuildReportAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // cambio schermata durante il caricamento
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCopyClick(object? sender, EventArgs e)
    {
        if (_output.TextLength == 0)
        {
            _context?.Navigation.SetStatus("Niente da copiare.");
            return;
        }

        Clipboard.SetText(_output.Text);
        _context?.Navigation.SetStatus("Stato copiato negli appunti.");
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (_output.TextLength == 0)
        {
            _context?.Navigation.SetStatus("Niente da salvare.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "File di testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
            FileName = $"piootoo-stato-server-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, _output.Text);
            _context?.Navigation.SetStatus($"Stato salvato in {dialog.FileName}.");
        }
        catch (Exception ex)
        {
            _context?.Navigation.SetError(ex.Message);
        }
    }
}
