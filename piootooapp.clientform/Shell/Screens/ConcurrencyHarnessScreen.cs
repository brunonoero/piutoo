using System.ComponentModel;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using piootooapp.clientform.Shell.Api;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia gruppi. Editabile, quindi mutabile: <see cref="TradingGroupRow"/> è init-only.</summary>
public sealed class HarnessGroupRow
{
    public string GroupId { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Zero = illimitato, come nel contratto del server.</summary>
    public int MaxConcurrentTrades { get; set; }

    public bool ApplyTitanoFilters { get; set; }

    public string TitanoBacktestFolder { get; set; } = string.Empty;
}

/// <summary>Un poll: chi ha chiesto, cosa ha ottenuto e con quali numeri il server ha deciso.</summary>
public sealed class PollLogRow
{
    public DateTime BarTimeUtc { get; set; }

    public string BarSymbol { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    /// <summary>Assegnato / MaxConcurrentTradesExceeded / NoSignal / SessionNotRunning.</summary>
    public string Outcome { get; set; } = string.Empty;

    public string StrategyCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public int OpenPositions { get; set; }

    public int PendingOrders { get; set; }

    public int MaxConcurrentTrades { get; set; }

    public string Note { get; set; } = string.Empty;
}

/// <summary>Una riga per account: quanto ha ottenuto e quanto gli è stato negato, e da cosa.</summary>
public sealed class AccountMatrixRow
{
    public string AccountNumber { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public int MaxConcurrentTrades { get; set; }

    public int Polls { get; set; }

    public int Entries { get; set; }

    public int Closes { get; set; }

    /// <summary>Rifiuti del passo 2: il limite per account.</summary>
    public int LimitRejections { get; set; }

    /// <summary>Rifiuti dei passi 3-5: template già consumato dal gruppo, slot occupato, simbolo occupato.</summary>
    public int LockRejections { get; set; }

    public int OpenNow { get; set; }
}

/// <summary>Un template per riga: chi lo ha consumato e chi no. È la vista del fan-out fra gruppi.</summary>
public sealed class TemplateRow
{
    public DateTime CreatedAtUtc { get; set; }

    public string StrategyCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string ClaimedByGroups { get; set; } = string.Empty;

    public string ClaimedByAccounts { get; set; } = string.Empty;

    public string State { get; set; } = "non reclamato";
}

/// <summary>Conteggio per causa di scarto: dice quale vincolo è binding nello scenario provato.</summary>
public sealed class RejectionReasonRow
{
    public string Reason { get; set; } = string.Empty;

    public string Meaning { get; set; } = string.Empty;

    public int Count { get; set; }

    public string Share { get; set; } = string.Empty;
}

/// <summary>
/// Banco di prova della distribuzione multi-account: apre una sessione da un piano reale, la
/// alimenta con le barre del datafeed e polla al posto dei cBot, registrando ogni decisione del
/// server.
///
/// <para>Serve a rispondere a una domanda che i log di produzione non rispondono bene: quando un
/// account non riceve un segnale, è per il limite di trade concorrenti o per uno dei tre lucchetti
/// di gruppo? Il server distingue i due casi nella risposta al poll — <c>MaxConcurrentTradesExceeded</c>
/// contro <c>NoSignal</c> — ma quella risposta la vede solo il cBot. Qui la si vede tutta.
/// Vedi <c>docs/domini/distribuzione-multi-account.md</c>.</para>
///
/// <para>La sessione è sempre nuova (l'execution key contiene l'istante di avvio) e vive nel
/// processo del server come tutte le altre: non tocca né interferisce con le sessioni dei cBot
/// veri. I fill sono simulati dalla console, che si comporta come un broker che riempie tutto e
/// chiude dopo un numero fisso di barre.</para>
/// </summary>
public partial class ConcurrencyHarnessScreen : UserControl, IShellScreen
{
    /// <summary>
    /// Griglia editabile: qui l'ordine è quello in cui l'utente sta scrivendo, quindi
    /// <see cref="BindingList{T}"/> e non <c>SortableBindingList</c> (vedi le regole delle schermate).
    /// </summary>
    private readonly BindingList<HarnessGroupRow> _groupRows = new();

    private readonly SortableBindingList<PollLogRow> _pollRows = new();
    private readonly SortableBindingList<AccountMatrixRow> _matrixRows = new();
    private readonly SortableBindingList<TemplateRow> _templateRows = new();
    private readonly SortableBindingList<RejectionReasonRow> _reasonRows = new();

    /// <summary>Template per IntentId: il claim si chiama <c>{templateId}::{gruppo}</c>, quindi il legame è esatto.</summary>
    private readonly Dictionary<string, TemplateRow> _templatesById = new(StringComparer.Ordinal);

    /// <summary>Posizioni che la console tiene aperte per conto dei finti broker, chiave account|simbolo|strategia.</summary>
    private readonly Dictionary<string, SimulatedPosition> _positions = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<ClosedBar> _bars = new();

    private ShellContext? _context;
    private TradingSessionDescriptor? _session;
    private int _barIndex;
    private CancellationTokenSource? _runCts;
    private bool _isRunning;

    public ConcurrencyHarnessScreen()
    {
        InitializeComponent();

        _groupsSource.DataSource = _groupRows;
        _pollSource.DataSource = _pollRows;
        _matrixSource.DataSource = _matrixRows;
        _templateSource.DataSource = _templateRows;
        _reasonSource.DataSource = _reasonRows;

        _pollGrid.EnableColumnSorting();
        _matrixGrid.EnableColumnSorting();
        _templateGrid.EnableColumnSorting();
        _reasonGrid.EnableColumnSorting();

        _runModeCombo.Items.AddRange(new object[] { ClientRunMode.Backtest, ClientRunMode.Realtime });
        _runModeCombo.SelectedIndex = 0;
        UpdateCommandAvailability();
    }

    public string ScreenTitle => "Verifica concorrenza";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        // Il workspace è quello scelto in alto: qui si legge, e i piani vengono da lì.
        _workspaceValueLabel.Text = _context.Services.Workspaces.CurrentDisplay;
        await ReloadPlansAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ selezione e preparazione

    private string? SelectedWorkspaceId => _context?.Services.Workspaces.CurrentId;

    private TradingPlan? SelectedPlan => (_planCombo.SelectedItem as PlanComboItem)?.Plan;

    private ClientRunMode SelectedRunMode =>
        _runModeCombo.SelectedItem is ClientRunMode mode ? mode : ClientRunMode.Backtest;

    private async Task ReloadPlansAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _planCombo.Items.Clear();
        if (SelectedWorkspaceId is not { } workspaceId)
        {
            _context.Navigation.SetStatus("Nessun workspace selezionato: scegline uno nella barra in alto.");
            return;
        }

        _toolbar.SetBusy(true);
        _context.Navigation.SetStatus("Caricamento piani…");
        try
        {
            var plans = await _context.Services.Plans.ListAsync(workspaceId, cancellationToken);
            foreach (var plan in plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase))
            {
                _planCombo.Items.Add(new PlanComboItem(plan));
            }

            if (_planCombo.Items.Count > 0)
            {
                _planCombo.SelectedIndex = 0;
            }

            _context.Navigation.SetStatus($"{plans.Count} piani nel workspace '{workspaceId}'.");
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
            _toolbar.SetBusy(false);
        }
    }

    private void OnPlanChanged(object? sender, EventArgs e)
    {
        if (SelectedPlan is not { } plan)
        {
            return;
        }

        _groupRows.Clear();
        foreach (var row in plan.Groups)
        {
            _groupRows.Add(new HarnessGroupRow
            {
                GroupId = row.GroupId,
                AccountNumber = row.AccountNumber,
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                ApplyTitanoFilters = row.ApplyTitanoFilters,
                TitanoBacktestFolder = row.TitanoBacktestFolder ?? string.Empty
            });
        }

        UpdateLimitsLabel();
        UpdateCommandAvailability();
    }

    private void OnRunModeChanged(object? sender, EventArgs e) => UpdateLimitsLabel();

    /// <summary>
    /// Il limite è attivo o no <b>prima</b> di premere qualsiasi cosa, e dipende da piano e modalità:
    /// un backtest senza filtro Titano lo disattiva per default, perché quel run deve produrre il
    /// campione sorgente completo. Senza questo avviso si passerebbe un'ora a chiedersi perché il
    /// limite «non funziona». Vedi <c>docs/domini/distribuzione-multi-account.md</c> §4.
    /// </summary>
    private void UpdateLimitsLabel()
    {
        if (SelectedPlan is not { } plan)
        {
            _limitsLabel.Text = "Limiti: —";
            return;
        }

        var runMode = SelectedRunMode;
        var titanoDisabled = string.IsNullOrWhiteSpace(plan.TitanoBacktestFolder) || !plan.ApplyTitanoFilters;
        var byDefault = !(runMode == ClientRunMode.Backtest && titanoDisabled);
        var effective = plan.EnforceConcurrencyLimits ?? byDefault;

        _limitsLabel.Text = effective
            ? "Limiti di concorrenza: ATTIVI" +
              (plan.EnforceConcurrencyLimits.HasValue ? " (forzati dal piano)" : " (default)")
            : "Limiti di concorrenza: DISATTIVI" +
              (plan.EnforceConcurrencyLimits.HasValue
                  ? " (forzati dal piano) — MaxConcurrentTrades non verrà mai applicato"
                  : " (default del backtest senza Titano) — MaxConcurrentTrades non verrà mai applicato");
        _limitsLabel.ForeColor = effective ? SystemColors.ControlText : Color.Firebrick;
    }

    private async void OnPrepareRequested(object? sender, EventArgs e)
    {
        if (_context == null || SelectedPlan is not { } plan)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            ResetResults();
            _context.Navigation.SetStatus("Apertura sessione dal piano…");
            _session = await _context.Services.Sessions.OpenFromPlanAsync(new OpenTradingPlanSessionRequest
            {
                PlanCode = plan.Code,
                ClientRunMode = SelectedRunMode,
                // Chiave sempre nuova: il banco di prova non deve mai riprendere una sessione altrui.
                ExecutionKey = $"harness-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                DistributeToAccounts = true
            });

            await ApplyGroupsAsync();

            _context.Navigation.SetStatus("Caricamento barre dal datafeed…");
            await LoadBarsAsync(plan.WorkspaceId);

            await _context.Services.Sessions.SetStatusAsync(_session.SessionId, _session.SessionToken, "start");

            _barIndex = 0;
            RebuildMatrix();
            UpdateProgressLabel();
            _context.Navigation.SetStatus(
                $"Sessione {_session.SessionId} pronta: {_bars.Count} barre, {_groupRows.Count} account.");
        }
        catch (Exception ex)
        {
            _session = null;
            _bars.Clear();
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            UpdateCommandAvailability();
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// Le barre sono quelle vere del repository, una serie per ogni coppia (simbolo, timeframe) del
    /// masterfilter, fuse in un'unica sequenza ordinata nel tempo. Ogni push porta una sola barra:
    /// è così che arrivano dai cBot, ed è l'unico modo di vedere le decisioni una alla volta.
    /// </summary>
    private async Task LoadBarsAsync(string workspaceId)
    {
        if (_context == null)
        {
            return;
        }

        var masterFilter = await _context.Services.Api.GetMasterFilterAsync(workspaceId);
        var catalog = await _context.Services.Api.ListStrategiesAsync();
        var selected = catalog
            .Where(item => masterFilter.StrategiesFilter.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(
                "Il masterfilter del workspace non contiene strategie: non c'è niente da valutare.");
        }

        var streams = selected
            .GroupBy(item => (Symbol: item.Symbol, item.TimeframeMinutes))
            .Select(group => group.Key)
            .ToList();

        var days = (int)_daysInput.Value;
        _bars.Clear();
        var missing = new List<string>();
        foreach (var (symbol, timeframe) in streams)
        {
            List<OhlcvData> candles;
            try
            {
                candles = await _context.Services.Datafeed.GetLatestAsync(
                    symbol, days, DatafeedApiClient.BarTypeFor(timeframe));
            }
            catch (Exception ex)
            {
                // Datafeed mancante = errore esplicito, mai proseguire in silenzio: qui però il
                // banco può ancora lavorare sugli altri stream, quindi si annota e si va avanti.
                missing.Add($"{symbol} {timeframe}m ({ex.Message})");
                continue;
            }

            foreach (var candle in candles)
            {
                var barTimeUtc = AsUtc(candle.DateTime);
                _bars.Add(new ClosedBar
                {
                    Symbol = symbol,
                    TimeframeMinutes = timeframe,
                    BarTimeUtc = barTimeUtc,
                    Sequence = barTimeUtc.Ticks,
                    IdempotencyKey = $"{symbol}|{timeframe}|{barTimeUtc:O}",
                    Bar = new OhlcvData
                    {
                        DateTime = barTimeUtc,
                        Open = candle.Open,
                        High = candle.High,
                        Low = candle.Low,
                        Close = candle.Close,
                        Volume = candle.Volume
                    }
                });
            }
        }

        _bars.Sort((left, right) => left.BarTimeUtc.CompareTo(right.BarTimeUtc));

        if (_bars.Count == 0)
        {
            throw new InvalidOperationException(
                "Nessuna barra disponibile per gli stream del masterfilter: " + string.Join(", ", missing));
        }

        if (missing.Count > 0)
        {
            _context.Navigation.SetError("Stream senza dati, esclusi dalla prova: " + string.Join(", ", missing));
        }
    }

    /// <summary>
    /// Il feed dichiara gli orari in UTC ma il JSON non porta il fuso, quindi il valore arriva
    /// <c>Unspecified</c> e i contratti di sessione lo rifiuterebbero. Non è un aggiustamento a
    /// valle: è il client che dichiara ciò che il formato del feed già garantisce.
    /// </summary>
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private async void OnApplyGroupsRequested(object? sender, EventArgs e)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await ApplyGroupsAsync();
            RebuildMatrix();
            _context.Navigation.SetStatus($"Configurazione applicata: {_groupRows.Count} account.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private async Task ApplyGroupsAsync()
    {
        if (_context == null || _session == null)
        {
            return;
        }

        var rows = _groupRows
            .Where(row => !string.IsNullOrWhiteSpace(row.GroupId) && !string.IsNullOrWhiteSpace(row.AccountNumber))
            .Select(row => new TradingGroupRow
            {
                GroupId = row.GroupId.Trim(),
                AccountNumber = row.AccountNumber.Trim(),
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                ApplyTitanoFilters = row.ApplyTitanoFilters,
                TitanoBacktestFolder = string.IsNullOrWhiteSpace(row.TitanoBacktestFolder)
                    ? null
                    : row.TitanoBacktestFolder.Trim()
            })
            .ToList();

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Serve almeno una riga gruppo/account.");
        }

        await _context.Services.Sessions.SetGroupsAsync(_session.SessionId, _session.SessionToken, rows);
    }

    private void OnAddGroupRow(object? sender, EventArgs e)
    {
        var last = _groupRows.LastOrDefault();
        _groupRows.Add(new HarnessGroupRow
        {
            GroupId = last?.GroupId ?? "g1",
            AccountNumber = string.Empty,
            MaxConcurrentTrades = last?.MaxConcurrentTrades ?? 1,
            ApplyTitanoFilters = last?.ApplyTitanoFilters ?? false,
            TitanoBacktestFolder = last?.TitanoBacktestFolder ?? string.Empty
        });
    }

    private void OnRemoveGroupRow(object? sender, EventArgs e)
    {
        var index = _groupsGrid.CurrentRow?.Index ?? -1;
        if (index >= 0 && index < _groupRows.Count)
        {
            _groupRows.RemoveAt(index);
        }
    }

    // ------------------------------------------------------------------------------ esecuzione

    private async void OnStepRequested(object? sender, EventArgs e)
    {
        if (_context == null || _session == null || _barIndex >= _bars.Count)
        {
            return;
        }

        _toolbar.SetBusy(true);
        try
        {
            await StepAsync(CancellationToken.None);
            RefreshDerivedViews();
            UpdateProgressLabel();
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            UpdateCommandAvailability();
            _toolbar.SetBusy(false);
        }
    }

    private async void OnRunRequested(object? sender, EventArgs e)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        _runCts?.Dispose();
        _runCts = new CancellationTokenSource();
        var token = _runCts.Token;

        _isRunning = true;
        _toolbar.SetBusy(true);
        UpdateCommandAvailability();
        try
        {
            while (_barIndex < _bars.Count && !token.IsCancellationRequested)
            {
                await StepAsync(token);
                if (_barIndex % 20 == 0)
                {
                    // Aggiornare le viste derivate a ogni barra costa più della simulazione stessa.
                    RefreshDerivedViews();
                    UpdateProgressLabel();
                }
            }

            RefreshDerivedViews();
            UpdateProgressLabel();
            _context.Navigation.SetStatus(token.IsCancellationRequested
                ? $"Interrotto alla barra {_barIndex} di {_bars.Count}."
                : $"Completate {_bars.Count} barre.");
        }
        catch (OperationCanceledException)
        {
            _context.Navigation.SetStatus($"Interrotto alla barra {_barIndex} di {_bars.Count}.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _isRunning = false;
            UpdateCommandAvailability();
            _toolbar.SetBusy(false);
        }
    }

    private void OnStopRequested(object? sender, EventArgs e) => _runCts?.Cancel();

    private void OnResetRequested(object? sender, EventArgs e)
    {
        _runCts?.Cancel();
        ResetResults();
        _session = null;
        _bars.Clear();
        _barIndex = 0;
        UpdateProgressLabel();
        UpdateCommandAvailability();
        _context?.Navigation.SetStatus("Banco azzerato. Prepara una nuova sessione.");
    }

    /// <summary>
    /// Una barra: la si spinge, poi ogni account polla nell'ordine in cui è configurato. L'ordine
    /// conta e non è un dettaglio — la distribuzione è pull, quindi chi polla prima serve per primo
    /// (vedi §3 del documento). Alla fine si chiudono le posizioni che hanno raggiunto la durata.
    /// </summary>
    private async Task StepAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        var bar = _bars[_barIndex];
        var pushed = await _context.Services.Sessions.PushBarsAsync(new PushBarsRequest
        {
            SessionId = _session.SessionId,
            SessionToken = _session.SessionToken,
            Bars = new[] { bar }
        }, cancellationToken);

        foreach (var template in pushed.Intents)
        {
            var row = new TemplateRow
            {
                CreatedAtUtc = template.CreatedAtUtc,
                StrategyCode = template.StrategyCode,
                Symbol = template.Symbol,
                Quantity = template.FinalQuantity,
                State = template.Status == OrderIntentStatus.Pending ? "non reclamato" : template.Status.ToString()
            };
            _templatesById[template.IntentId] = row;
            _templateRows.Add(row);
        }

        foreach (var account in _groupRows.Select(row => row.AccountNumber).Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PollAccountAsync(bar, account.Trim(), cancellationToken);
        }

        await CloseExpiredPositionsAsync(cancellationToken);
        _barIndex++;
    }

    private async Task PollAccountAsync(ClosedBar bar, string accountNumber, CancellationToken cancellationToken)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        var open = _positions.Values.Where(p => Same(p.AccountNumber, accountNumber)).ToList();
        var response = await _context.Services.Sessions.PollSignalAsync(
            _session.SessionId,
            _session.SessionToken,
            accountNumber,
            new AccountSignalPollRequest
            {
                SessionToken = _session.SessionToken,
                // Lo stato dichiarato dal broker è ciò che il server conta per il limite. Qui il
                // broker finto riempie subito, quindi non ha mai ordini pendenti: le posizioni
                // aperte sono l'unico ingrediente del conteggio.
                Positions = open
                    .Select(p => new BrokerPositionSnapshot
                    {
                        PositionId = p.IntentId, Symbol = p.Symbol, StrategyCode = p.StrategyCode
                    })
                    .ToList(),
                Orders = Array.Empty<BrokerOrderSnapshot>()
            },
            cancellationToken);

        var configured = _groupRows.FirstOrDefault(row => Same(row.AccountNumber, accountNumber));
        // I numeri in colonna sono quelli **dichiarati nel poll**, non quelli della risposta: il
        // server li rimanda indietro solo quando rifiuta per limite, e una colonna vuota su ogni
        // riga andata a buon fine renderebbe illeggibile proprio il confronto che serve.
        var log = new PollLogRow
        {
            BarTimeUtc = bar.BarTimeUtc,
            BarSymbol = bar.Symbol,
            AccountNumber = accountNumber,
            GroupId = configured?.GroupId ?? string.Empty,
            OpenPositions = open.Count,
            PendingOrders = 0,
            MaxConcurrentTrades = configured?.MaxConcurrentTrades ?? 0
        };
        var groupId = log.GroupId;

        if (response.Intent is not { } intent)
        {
            log.Outcome = response.Reason ?? "NoSignal";
            log.Note = DescribeReason(response.Reason);
            _pollRows.Add(log);
            return;
        }

        log.Outcome = intent.Kind == OrderIntentKind.Close ? "Chiusura assegnata" : "Ingresso assegnato";
        log.StrategyCode = intent.StrategyCode;
        log.Symbol = intent.Symbol;
        log.Quantity = intent.FinalQuantity;
        _pollRows.Add(log);

        await ReportFilledAsync(intent, cancellationToken);

        var key = PositionKey(accountNumber, intent.Symbol, intent.StrategyCode);
        if (intent.Kind == OrderIntentKind.Close)
        {
            _positions.Remove(key);
        }
        else
        {
            _positions[key] = new SimulatedPosition(
                accountNumber, intent.Symbol, intent.StrategyCode, intent.IntentId, intent.FinalQuantity, _barIndex);
            RecordClaim(intent, groupId, accountNumber);
        }
    }

    /// <summary>Il claim si chiama <c>{template}::{gruppo}</c>: da lì si risale al template esatto.</summary>
    private void RecordClaim(OrderIntent claim, string groupId, string accountNumber)
    {
        var separator = claim.IntentId.LastIndexOf("::", StringComparison.Ordinal);
        var templateId = separator > 0 ? claim.IntentId[..separator] : claim.IntentId;
        if (!_templatesById.TryGetValue(templateId, out var row))
        {
            return;
        }

        row.ClaimedByGroups = Append(row.ClaimedByGroups, string.IsNullOrWhiteSpace(groupId) ? claim.AssignedGroupId ?? "?" : groupId);
        row.ClaimedByAccounts = Append(row.ClaimedByAccounts, accountNumber);
        row.State = "reclamato";
    }

    private static string Append(string current, string value)
        => current.Length == 0 ? value : $"{current}, {value}";

    private async Task ReportFilledAsync(OrderIntent intent, CancellationToken cancellationToken)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        await _context.Services.Sessions.ApplyReportAsync(_session.SessionId, new ExecutionReportRequest
        {
            SessionToken = _session.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"harness-{Guid.NewGuid():N}",
                IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = intent.Price > 0 ? intent.Price : 1m,
                EventTimeUtc = _bars[_barIndex].BarTimeUtc
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Chiude ciò che ha superato la durata configurata. L'intent di chiusura resta pendente: sarà
    /// il poll successivo di quell'account a consegnarlo (passo 1 dell'algoritmo), ed è il suo
    /// report a liberare slot di gruppo e lucchetto simbolo. Non si scorciatoia il percorso.
    /// </summary>
    private async Task CloseExpiredPositionsAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _session == null)
        {
            return;
        }

        var life = (int)_closeAfterInput.Value;
        if (life <= 0)
        {
            return;
        }

        foreach (var position in _positions.Values.ToList())
        {
            // Una chiusura già richiesta è ancora in viaggio: l'intent resta pendente finché il poll
            // successivo di quell'account non lo consegna. Richiederla di nuovo creerebbe intent
            // duplicati sulla stessa posizione.
            if (position.CloseRequested || _barIndex - position.OpenedAtBar < life)
            {
                continue;
            }

            try
            {
                await _context.Services.Sessions.CreateExternalCloseIntentAsync(_session.SessionId,
                    new CreateExternalCloseIntentRequest
                    {
                        SessionToken = _session.SessionToken,
                        StrategyCode = position.StrategyCode,
                        Symbol = position.Symbol,
                        AccountNumber = position.AccountNumber,
                        Reason = "HarnessTimeExit"
                    }, cancellationToken);
                position.CloseRequested = true;
            }
            catch (InvalidOperationException)
            {
                // Chiusura già registrata per questa posizione: la richiesta successiva è un doppione
                // innocuo, la posizione sparirà quando il poll consegnerà l'intent già pendente.
                position.CloseRequested = true;
            }
        }
    }

    private static string PositionKey(string account, string symbol, string strategyCode)
        => $"{account}|{symbol}|{strategyCode}";

    private static bool Same(string left, string right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string DescribeReason(string? reason) => reason switch
    {
        "MaxConcurrentTradesExceeded" =>
            "limite per account: posizioni + ordini pendenti dichiarati hanno raggiunto MaxConcurrentTrades",
        "NoSignal" =>
            "nessun template libero: consumato dal gruppo, slot (gruppo, strategia, simbolo) occupato, " +
            "oppure l'account ha già qualcosa su quel simbolo",
        "SessionNotRunning" => "la sessione non è in esecuzione",
        null => string.Empty,
        _ => reason
    };

    // ------------------------------------------------------------------------------- viste

    private void RefreshDerivedViews()
    {
        RebuildMatrix();
        RebuildReasons();
        _templateRows.ReapplySort();
        _templateRows.ResetBindings();
        _pollRows.ReapplySort();
        _pollRows.ResetBindings();
    }

    private void RebuildMatrix()
    {
        _matrixRows.RaiseListChangedEvents = false;
        _matrixRows.Clear();
        foreach (var group in _groupRows.Where(row => !string.IsNullOrWhiteSpace(row.AccountNumber)))
        {
            var account = group.AccountNumber.Trim();
            var polls = _pollRows.Where(row => Same(row.AccountNumber, account)).ToList();
            _matrixRows.Add(new AccountMatrixRow
            {
                AccountNumber = account,
                GroupId = group.GroupId,
                MaxConcurrentTrades = group.MaxConcurrentTrades,
                Polls = polls.Count,
                Entries = polls.Count(row => row.Outcome == "Ingresso assegnato"),
                Closes = polls.Count(row => row.Outcome == "Chiusura assegnata"),
                LimitRejections = polls.Count(row => row.Outcome == "MaxConcurrentTradesExceeded"),
                LockRejections = polls.Count(row => row.Outcome == "NoSignal"),
                OpenNow = _positions.Values.Count(p => Same(p.AccountNumber, account))
            });
        }

        _matrixRows.RaiseListChangedEvents = true;
        _matrixRows.ReapplySort();
        _matrixRows.ResetBindings();
    }

    private void RebuildReasons()
    {
        var total = _pollRows.Count;
        _reasonRows.RaiseListChangedEvents = false;
        _reasonRows.Clear();
        foreach (var group in _pollRows.GroupBy(row => row.Outcome).OrderByDescending(g => g.Count()))
        {
            _reasonRows.Add(new RejectionReasonRow
            {
                Reason = group.Key,
                Meaning = group.Key switch
                {
                    "Ingresso assegnato" => "claim andato a buon fine",
                    "Chiusura assegnata" => "intent di chiusura consegnato",
                    _ => DescribeReason(group.Key)
                },
                Count = group.Count(),
                Share = total == 0 ? "0%" : $"{100m * group.Count() / total:0.0}%"
            });
        }

        _reasonRows.RaiseListChangedEvents = true;
        _reasonRows.ReapplySort();
        _reasonRows.ResetBindings();
    }

    private void ResetResults()
    {
        _pollRows.Clear();
        _matrixRows.Clear();
        _templateRows.Clear();
        _reasonRows.Clear();
        _templatesById.Clear();
        _positions.Clear();
        _barIndex = 0;
    }

    private void UpdateProgressLabel() => _progressLabel.Text = _bars.Count == 0
        ? "Nessuna barra caricata"
        : $"Barra {_barIndex} di {_bars.Count}" +
          (_barIndex < _bars.Count ? $"  ·  prossima {_bars[_barIndex].BarTimeUtc:yyyy-MM-dd HH:mm} UTC" : "  ·  fine");

    private void UpdateCommandAvailability()
    {
        var ready = _session != null && _bars.Count > 0;
        var running = _isRunning;
        _prepareButton.Enabled = SelectedPlan != null && !running;
        _applyGroupsButton.Enabled = _session != null && !running;
        _stepButton.Enabled = ready && !running && _barIndex < _bars.Count;
        _runButton.Enabled = ready && !running && _barIndex < _bars.Count;
        _stopButton.Enabled = running;
    }

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    /// <summary>Posizione tenuta aperta dal broker simulato della console.</summary>
    private sealed class SimulatedPosition
    {
        public SimulatedPosition(
            string accountNumber, string symbol, string strategyCode, string intentId, decimal quantity, int openedAtBar)
        {
            AccountNumber = accountNumber;
            Symbol = symbol;
            StrategyCode = strategyCode;
            IntentId = intentId;
            Quantity = quantity;
            OpenedAtBar = openedAtBar;
        }

        public string AccountNumber { get; }

        public string Symbol { get; }

        public string StrategyCode { get; }

        public string IntentId { get; }

        public decimal Quantity { get; }

        public int OpenedAtBar { get; }

        public bool CloseRequested { get; set; }
    }
}

/// <summary>Voce della combo piani: codice e nome, come nella lista piani.</summary>
public sealed class PlanComboItem
{
    public PlanComboItem(TradingPlan plan) => Plan = plan;

    public TradingPlan Plan { get; }

    public override string ToString() => $"{Plan.Code}  ·  {Plan.Name}  ·  {Plan.Groups.Count} righe";
}
