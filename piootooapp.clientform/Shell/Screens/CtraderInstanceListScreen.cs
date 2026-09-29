using System.ComponentModel;
using System.Diagnostics;
using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della lista istanze: il processo sulla macchina accanto a ciò che il server ne vede.</summary>
public sealed class CtraderInstanceRow
{
    public string Name { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public string Bot { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public string Process { get; set; } = string.Empty;

    public DateTime? StartedAtUtc { get; set; }

    public string SessionStatus { get; set; } = string.Empty;

    public double? MinutesSinceLastBar { get; set; }

    public int? OpenPositions { get; set; }

    /// <summary>La sintesi in una parola: è la colonna che si legge per prima.</summary>
    public string Verdict { get; set; } = string.Empty;

    [Browsable(false)]
    public InstanceHealth Health { get; set; }

    [Browsable(false)]
    public CtraderInstanceStatus Status { get; set; } = null!;
}

public enum InstanceHealth
{
    Ok,
    Off,
    Warning,
    Problem
}

/// <summary>
/// Le istanze dei cBot che girano su cTrader CLI (<c>tools\conti-ctrader.ps1</c>), una per conto: se
/// il processo è vivo, e se il server sta davvero ricevendo barre da lui. Le due cose vanno lette
/// insieme perché si smentiscono a vicenda nei casi che contano — un processo vivo il cui bot non
/// ha aperto la sessione, una sessione ancora in RAM sul server il cui processo è morto.
///
/// Il lato server viene dal presidio per conto (<c>accounts/{n}/watch</c>), la sessione si abbina
/// all'istanza per codice piano. Il raccoglitore non apre sessioni: per lui conta solo il processo.
/// Doppio clic apre il presidio del conto, che resta la vista di dettaglio: questa lista non la
/// ripete, dice solo dove guardare.
/// </summary>
public partial class CtraderInstanceListScreen : UserControl, IShellScreen
{
    private static readonly Color BackgroundProblem = Color.FromArgb(255, 232, 232);
    private static readonly Color BackgroundWarning = Color.FromArgb(255, 247, 224);

    private readonly List<CtraderInstanceRow> _allRows = new();
    private readonly SortableBindingList<CtraderInstanceRow> _visibleRows = new();
    private ShellContext? _context;
    private bool _busy;

    public CtraderInstanceListScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _visibleRows;
        _grid.EnableColumnSorting();
        _toolbar.CanCreate = false;
        _toolbar.CanDelete = false;
        UpdateCommands();
    }

    public string ScreenTitle => "Istanze cTrader";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        SetBusy(true);
        _context.Navigation.SetStatus("Lettura delle istanze…");
        try
        {
            var instances = await CtraderInstancesClient.GetStatusAsync(cancellationToken);
            var serverNote = string.Empty;
            var watches = new Dictionary<string, AccountRealtimeWatch>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var account in instances.Where(i => i.PlanCode != null).Select(i => i.Account).Distinct())
                {
                    watches[account] = await _context.Services.Sessions.GetAccountWatchAsync(account, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Il server giù è proprio uno dei casi in cui si apre questa schermata: i processi
                // si mostrano lo stesso, le colonne della sessione restano vuote.
                serverNote = $" Server non raggiungibile: {ex.Message}";
                watches.Clear();
            }

            _allRows.Clear();
            foreach (var instance in instances)
            {
                _allRows.Add(BuildRow(instance, watches, serverNote.Length > 0));
            }

            ApplyFilter();
            var running = _allRows.Count(row => row.Status.Running);
            _context.Navigation.SetStatus(_allRows.Count == 0
                ? "Nessuna istanza nel file di configurazione."
                : $"{running} istanze accese su {_allRows.Count}.{serverNote}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _allRows.Clear();
            ApplyFilter();
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static CtraderInstanceRow BuildRow(
        CtraderInstanceStatus instance,
        IReadOnlyDictionary<string, AccountRealtimeWatch> watches,
        bool serverUnavailable)
    {
        var row = new CtraderInstanceRow
        {
            Name = instance.Name,
            Account = instance.Account,
            Bot = instance.Bot,
            PlanCode = instance.PlanCode ?? string.Empty,
            Process = instance.Running ? "acceso" : instance.Enabled ? "spento" : "disattivata",
            StartedAtUtc = instance.StartedAtUtc,
            Status = instance
        };

        RealtimeWatchSession? session = null;
        if (instance.PlanCode != null && watches.TryGetValue(instance.Account, out var watch))
        {
            session = watch.Sessioni.FirstOrDefault(s =>
                string.Equals(s.PlanCode, instance.PlanCode, StringComparison.OrdinalIgnoreCase));
        }

        if (session != null)
        {
            row.SessionStatus = session.Status.ToString();
            row.MinutesSinceLastBar = session.MinutiDallUltimaBarra is { } minutes ? Math.Round(minutes, 0) : null;
            row.OpenPositions = session.Posizioni.Count;
        }

        (row.Health, row.Verdict) = Judge(instance, session, serverUnavailable);
        return row;
    }

    private static (InstanceHealth, string) Judge(
        CtraderInstanceStatus instance,
        RealtimeWatchSession? session,
        bool serverUnavailable)
    {
        if (!instance.Running)
        {
            if (session != null)
            {
                return (InstanceHealth.Problem, "processo morto, sessione ancora sul server");
            }

            return instance.Enabled ? (InstanceHealth.Problem, "spento") : (InstanceHealth.Off, "disattivata");
        }

        if (instance.PlanCode == null)
        {
            return (InstanceHealth.Ok, "acceso");
        }

        if (serverUnavailable)
        {
            return (InstanceHealth.Warning, "server non raggiungibile");
        }

        if (session == null)
        {
            return (InstanceHealth.Problem, "nessuna sessione sul server");
        }

        if (session.MinutiDallUltimaBarra is null)
        {
            return (InstanceHealth.Warning, "sessione senza barre");
        }

        // Due barre del timeframe più fitto senza nulla: una sola può essere la barra in corso.
        // Il fine settimana e le pause di mercato qui sembrano fermi; il presidio li distingue.
        var tolerance = Math.Max(session.MinTimeframeMinutes, 1) * 2;
        if (session.MinutiDallUltimaBarra > tolerance)
        {
            return (InstanceHealth.Warning, $"nessuna barra da {session.MinutiDallUltimaBarra:0} min");
        }

        return (InstanceHealth.Ok, "ok");
    }

    private void ApplyFilter()
    {
        var filter = _toolbar.FilterText;

        _visibleRows.RaiseListChangedEvents = false;
        _visibleRows.Clear();
        foreach (var row in _allRows.Where(row =>
                     filter.Length == 0
                     || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Account.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.Bot.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || row.PlanCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            _visibleRows.Add(row);
        }

        _visibleRows.RaiseListChangedEvents = true;
        _visibleRows.ReapplySort();
        _visibleRows.ResetBindings();
        UpdateCommands();
    }

    private CtraderInstanceRow? SelectedRow
    {
        get
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _toolbar.SetBusy(busy);
        UpdateCommands();
    }

    private void UpdateCommands()
    {
        var row = SelectedRow;
        _startButton.Enabled = !_busy && row is { Status.Running: false };
        _stopButton.Enabled = !_busy && row is { Status.Running: true };
        _restartButton.Enabled = !_busy && row is { Status.Running: true };
        _logButton.Enabled = row != null && File.Exists(row.Status.LogPath);
        _watchButton.Enabled = row != null;
    }

    private async Task RunActionAsync(string action, string verb)
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        if (action != "Start")
        {
            var positions = row.OpenPositions is > 0 ? $" Il conto ha {row.OpenPositions} posizioni aperte: restano sul broker con i loro stop, ma nessuno le governa finché il bot è fermo." : string.Empty;
            var answer = MessageBox.Show(
                this,
                $"{verb} l'istanza {row.Name} (conto {row.Account}, {row.Bot})?{positions}",
                ScreenTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (answer != DialogResult.OK)
            {
                return;
            }
        }

        SetBusy(true);
        _context.Navigation.SetStatus($"{verb}: {row.Name}… (lo stop aspetta fino a 30 secondi che il bot chiuda)");
        try
        {
            var result = await CtraderInstancesClient.RunActionAsync(action, row.Name, CancellationToken.None);
            if (result.ExitCode != 0)
            {
                _context.Navigation.SetError(result.Describe());
                return;
            }

            // Lo script scrive una riga per passo e un WARNING quando qualcosa non torna (uscito
            // subito, OnStop non eseguito): l'avviso è ciò che va mostrato, se c'è.
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var warning = lines.FirstOrDefault(line => line.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase));
            SetBusy(false);
            await LoadAsync(CancellationToken.None);
            if (warning != null)
            {
                _context.Navigation.SetError(warning["WARNING:".Length..].Trim());
            }
            else
            {
                _context.Navigation.SetStatus(string.Join("  ", lines));
            }
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

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnGridSelectionChanged(object? sender, EventArgs e) => UpdateCommands();

    private async void OnStartClick(object? sender, EventArgs e) => await RunActionAsync("Start", "Avvio");

    private async void OnStopClick(object? sender, EventArgs e) => await RunActionAsync("Stop", "Fermare");

    private async void OnRestartClick(object? sender, EventArgs e) => await RunActionAsync("Restart", "Riavviare");

    private void OnLogClick(object? sender, EventArgs e)
    {
        if (SelectedRow is { } row && File.Exists(row.Status.LogPath))
        {
            Process.Start(new ProcessStartInfo(row.Status.LogPath) { UseShellExecute = true });
        }
    }

    private void OnWatchClick(object? sender, EventArgs e) => OpenWatch();

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            OpenWatch();
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && SelectedRow != null)
        {
            e.Handled = true;
            OpenWatch();
        }
    }

    private void OpenWatch()
    {
        if (_context == null || SelectedRow is not { } row)
        {
            return;
        }

        var watch = new RealtimeWatchScreen();
        watch.SelectAccount(row.Account);
        _context.Navigation.Push(watch);
    }

    private void OnGridRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visibleRows.Count)
        {
            return;
        }

        var style = _grid.Rows[e.RowIndex].DefaultCellStyle;
        style.BackColor = _visibleRows[e.RowIndex].Health switch
        {
            InstanceHealth.Problem => BackgroundProblem,
            InstanceHealth.Warning => BackgroundWarning,
            _ => _grid.DefaultCellStyle.BackColor
        };
        style.ForeColor = _visibleRows[e.RowIndex].Health == InstanceHealth.Off ? SystemColors.GrayText : _grid.DefaultCellStyle.ForeColor;
    }
}
