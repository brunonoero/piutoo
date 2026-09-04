using System.ComponentModel;
using System.Drawing;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga della griglia dei rilievi.</summary>
public sealed class WatchFindingRow
{
    public string Gravita { get; set; } = string.Empty;

    public string Rilievo { get; set; } = string.Empty;

    public string Sessione { get; set; } = string.Empty;

    public string Strategia { get; set; } = string.Empty;

    public string Simbolo { get; set; } = string.Empty;

    public string Risulta { get; set; } = string.Empty;

    public string Azione { get; set; } = string.Empty;

    [Browsable(false)]
    public RealtimeWatchSeverity Severity { get; set; }
}

/// <summary>Riga della griglia delle sessioni.</summary>
public sealed class WatchSessionRow
{
    public string Sessione { get; set; } = string.Empty;

    public string Piano { get; set; } = string.Empty;

    public string Stato { get; set; } = string.Empty;

    public DateTime? UltimaBarraUtc { get; set; }

    public int? MinutiDiSilenzio { get; set; }

    public int TimeframeMinimo { get; set; }

    public string Holding { get; set; } = string.Empty;

    public string StatoBroker { get; set; } = string.Empty;

    /// <summary>Vuoto quando la sessione è stata aperta normalmente.</summary>
    public string Ripresa { get; set; } = string.Empty;

    public int Posizioni { get; set; }

    public int Ordini { get; set; }
}

/// <summary>Riga della griglia delle posizioni che il server crede aperte.</summary>
public sealed class WatchPositionRow
{
    public string Strategia { get; set; } = string.Empty;

    public string Simbolo { get; set; } = string.Empty;

    public string SimboloSuCTrader { get; set; } = string.Empty;

    public string Direzione { get; set; } = string.Empty;

    public decimal Quantita { get; set; }

    public decimal PrezzoIngresso { get; set; }

    public DateTime IngressoUtc { get; set; }

    public decimal? StopLoss { get; set; }

    public decimal? TakeProfit { get; set; }

    public DateTime? ChiusuraPrevistaUtc { get; set; }

    public string Confermata { get; set; } = string.Empty;
}

/// <summary>Riga della griglia degli ordini che per il server sono ancora in volo.</summary>
public sealed class WatchPendingRow
{
    public string Strategia { get; set; } = string.Empty;

    public string Simbolo { get; set; } = string.Empty;

    public string Lato { get; set; } = string.Empty;

    public string Stato { get; set; } = string.Empty;

    public decimal Prezzo { get; set; }

    public decimal Quantita { get; set; }

    public int TimeframeMinuti { get; set; }

    public DateTime CreatoUtc { get; set; }

    public DateTime? ValidoFinoUtc { get; set; }
}

/// <summary>
/// Presidio realtime di un conto: cosa il server sta governando adesso, e dove conviene aprire
/// cTrader e guardare con i propri occhi.
///
/// <para><b>Perché non basta la lista sessioni.</b> Quella dice cosa c'è; questa dice cosa manca.
/// Le sessioni vivono in RAM nel processo server: un riavvio le fa sparire tutte, e la lista si
/// limita a diventare vuota — mentre su cTrader le posizioni aperte dal sistema restano dove sono,
/// senza che nessuno le sorvegli più dal lato server. È esattamente il caso in cui una schermata
/// vuota è l'allarme più forte, e per riconoscerlo serve sapere che il conto <i>dovrebbe</i> avere
/// una sessione: da qui il confronto con i piani che lo nominano.</para>
///
/// <para><b>Cosa questa schermata non sa.</b> La console parla solo HTTP con l'API e non vede
/// cTrader: nessuna riga qui dentro afferma che una posizione è aperta, solo che il server la
/// crede aperta e da quanto non lo verifica. I verdetti li calcola il server
/// (<c>RealtimeWatchRules</c>), perché sono le stesse domande della riconciliazione descritta in
/// <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c> §4 e vanno scritte una volta sola.
/// Qui si impagina e si colora.</para>
/// </summary>
public partial class RealtimeWatchScreen : UserControl, IShellScreen
{
    // Fondi tenui: la gravità si legge dalla colonna, il colore serve a far saltare all'occhio la
    // riga giusta in un elenco lungo, non a sostituire il testo.
    private static readonly Color SfondoIntervento = Color.FromArgb(255, 232, 232);
    private static readonly Color SfondoAttenzione = Color.FromArgb(255, 247, 224);

    private readonly SortableBindingList<WatchFindingRow> _findings = new();
    private readonly SortableBindingList<WatchSessionRow> _sessions = new();
    private readonly SortableBindingList<WatchPositionRow> _positions = new();
    private readonly SortableBindingList<WatchPendingRow> _pending = new();

    private readonly List<WatchFindingRow> _allFindings = new();
    private readonly List<WatchPositionRow> _allPositions = new();
    private readonly List<WatchPendingRow> _allPending = new();

    private ShellContext? _context;
    private bool _suppressComboEvents;
    private string? _selectedAccount;

    public RealtimeWatchScreen()
    {
        InitializeComponent();
        ShellGridHelper.ConfigureReadableGrids(this);

        _findingsSource.DataSource = _findings;
        _sessionsSource.DataSource = _sessions;
        _positionsSource.DataSource = _positions;
        _pendingSource.DataSource = _pending;

        _findingsGrid.EnableColumnSorting();
        _sessionsGrid.EnableColumnSorting();
        _positionsGrid.EnableColumnSorting();
        _pendingGrid.EnableColumnSorting();
    }

    public string ScreenTitle => "Presidio realtime";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _toolbar.SetBusy(true);
        _context.Navigation.SetStatus("Lettura dei conti…");
        try
        {
            await LoadAccountsAsync(cancellationToken);
            await LoadWatchAsync(cancellationToken);
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

    /// <summary>
    /// I conti vengono dal registro globale, non dalle sessioni: un conto che ha perso la propria
    /// sessione deve restare selezionabile, altrimenti proprio il caso da diagnosticare sparisce
    /// dalla combo.
    /// </summary>
    private async Task LoadAccountsAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        var accounts = await _context.Services.Api.ListAccountsAsync(cancellationToken);
        var items = accounts
            .Where(account => !string.IsNullOrWhiteSpace(account.AccountNumber))
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(account => new AccountComboItem(account))
            .ToArray();

        _suppressComboEvents = true;
        try
        {
            _accountCombo.Items.Clear();
            _accountCombo.Items.AddRange(items);

            var index = _selectedAccount is null
                ? -1
                : Array.FindIndex(items, item =>
                    string.Equals(item.AccountNumber, _selectedAccount, StringComparison.OrdinalIgnoreCase));

            _accountCombo.SelectedIndex = index >= 0 ? index : (items.Length > 0 ? 0 : -1);
            _selectedAccount = (_accountCombo.SelectedItem as AccountComboItem)?.AccountNumber;
        }
        finally
        {
            _suppressComboEvents = false;
        }
    }

    private async Task LoadWatchAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _allFindings.Clear();
        _allPositions.Clear();
        _allPending.Clear();
        _sessions.Clear();

        if (_selectedAccount is null)
        {
            ApplyFilter();
            _severityLabel.Text = string.Empty;
            _context.Navigation.SetStatus(
                "Nessun conto in anagrafica: creane uno in Anagrafiche → Account.");
            return;
        }

        var watch = await _context.Services.Sessions.GetAccountWatchAsync(_selectedAccount, cancellationToken);

        foreach (var rilievo in watch.Rilievi)
        {
            _allFindings.Add(new WatchFindingRow
            {
                Gravita = DescribeSeverity(rilievo.Severity),
                Rilievo = SpaziaCamelCase(rilievo.Finding.ToString()),
                Sessione = ShortId(rilievo.SessionId),
                Strategia = rilievo.StrategyCode,
                Simbolo = rilievo.Symbol,
                Risulta = rilievo.Message,
                Azione = rilievo.Action,
                Severity = rilievo.Severity
            });
        }

        foreach (var sessione in watch.Sessioni)
        {
            _sessions.Add(new WatchSessionRow
            {
                Sessione = ShortId(sessione.SessionId),
                Piano = sessione.PlanCode,
                Stato = sessione.Status.ToString(),
                UltimaBarraUtc = sessione.LastBarUtc,
                MinutiDiSilenzio = sessione.MinutiDallUltimaBarra is { } minuti ? (int)minuti : null,
                TimeframeMinimo = sessione.MinTimeframeMinutes,
                Holding = sessione.Holding.Describe(),
                Ripresa = sessione.RipresaDaDumpAtUtc is { } ripresa
                    ? $"da dump {ripresa:dd/MM HH:mm}"
                    : string.Empty,
                // "Mai verificato" non è un dettaglio tecnico: dice che tutta la colonna delle
                // posizioni è memoria del server, non una lettura del conto.
                StatoBroker = sessione.RiceveStatoBroker ? "riconciliato" : "mai verificato",
                Posizioni = sessione.Posizioni.Count,
                Ordini = sessione.Pendenti.Count
            });

            foreach (var posizione in sessione.Posizioni)
            {
                _allPositions.Add(new WatchPositionRow
                {
                    Strategia = posizione.StrategyCode,
                    Simbolo = posizione.Symbol,
                    SimboloSuCTrader = posizione.AccountSymbol,
                    Direzione = posizione.Direction.ToString(),
                    Quantita = posizione.Quantity,
                    PrezzoIngresso = posizione.EntryPrice,
                    IngressoUtc = posizione.EntryTimeUtc,
                    StopLoss = posizione.StopLoss,
                    TakeProfit = posizione.TakeProfit,
                    ChiusuraPrevistaUtc = posizione.CloseAtUtc,
                    Confermata = sessione.RiceveStatoBroker
                        ? (posizione.BrokerConfermata ? "sì" : "mai")
                        : "n/d"
                });
            }

            foreach (var pendente in sessione.Pendenti)
            {
                _allPending.Add(new WatchPendingRow
                {
                    Strategia = pendente.StrategyCode,
                    Simbolo = pendente.Symbol,
                    Lato = pendente.Side.ToString(),
                    Stato = pendente.Status.ToString(),
                    Prezzo = pendente.Price,
                    Quantita = pendente.Quantity,
                    TimeframeMinuti = pendente.TimeframeMinutes,
                    CreatoUtc = pendente.CreatedAtUtc,
                    // La scadenza vera è ExpiresAtUtc + timeframe: quel campo è l'inizio
                    // dell'ultima barra valida, non la sua fine.
                    ValidoFinoUtc = pendente.ExpiresAtUtc?.AddMinutes(Math.Max(1, pendente.TimeframeMinutes))
                });
            }
        }

        _sessions.ReapplySort();
        _sessions.ResetBindings();
        ApplyFilter();

        _severityLabel.Text = DescribeSeverity(watch.Severity).ToUpperInvariant();
        _severityLabel.ForeColor = watch.Severity switch
        {
            RealtimeWatchSeverity.Intervento => Color.Firebrick,
            RealtimeWatchSeverity.Attenzione => Color.DarkGoldenrod,
            _ => Color.SeaGreen
        };

        var piani = watch.Piani.Count == 0 ? "nessun piano" : string.Join(", ", watch.Piani);
        _context.Navigation.SetStatus(
            $"Conto {watch.AccountNumber} · {piani} · {watch.Sessioni.Count} sessione/i realtime · " +
            $"{_allPositions.Count} posizione/i e {_allPending.Count} ordine/i per il server · " +
            $"letto alle {watch.GeneratedAtUtc:HH:mm:ss} UTC.");
    }

    /// <summary>
    /// Il filtro vale su strategia e simbolo, che è come si cerca una posizione quando si ha
    /// cTrader aperto accanto. Le sessioni non si filtrano: sono poche e servono da contesto.
    /// </summary>
    private void ApplyFilter()
    {
        var filtro = _toolbar.FilterText;

        Riempi(_findings, _allFindings.Where(row =>
            filtro.Length == 0
            || Contiene(row.Strategia, filtro)
            || Contiene(row.Simbolo, filtro)
            || Contiene(row.Rilievo, filtro)));

        Riempi(_positions, _allPositions.Where(row =>
            filtro.Length == 0
            || Contiene(row.Strategia, filtro)
            || Contiene(row.Simbolo, filtro)
            || Contiene(row.SimboloSuCTrader, filtro)));

        Riempi(_pending, _allPending.Where(row =>
            filtro.Length == 0
            || Contiene(row.Strategia, filtro)
            || Contiene(row.Simbolo, filtro)));
    }

    private static void Riempi<T>(SortableBindingList<T> destinazione, IEnumerable<T> righe)
    {
        destinazione.RaiseListChangedEvents = false;
        destinazione.Clear();
        foreach (var riga in righe)
        {
            destinazione.Add(riga);
        }

        destinazione.RaiseListChangedEvents = true;
        // Prima il riordino, poi il rebind: al contrario la griglia mostra la freccetta
        // sull'intestazione mentre le righe sono tornate nell'ordine della sorgente.
        destinazione.ReapplySort();
        destinazione.ResetBindings();
    }

    /// <summary>
    /// Il colore si applica qui e non alla costruzione della riga perché l'ordinamento e il filtro
    /// ricostruiscono le righe della griglia: <c>DataBindingComplete</c> è l'unico punto che scatta
    /// dopo ognuna delle due cose.
    /// </summary>
    private void OnFindingsBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        foreach (DataGridViewRow riga in _findingsGrid.Rows)
        {
            if (riga.DataBoundItem is not WatchFindingRow dati)
            {
                continue;
            }

            riga.DefaultCellStyle.BackColor = dati.Severity switch
            {
                RealtimeWatchSeverity.Intervento => SfondoIntervento,
                RealtimeWatchSeverity.Attenzione => SfondoAttenzione,
                _ => _findingsGrid.DefaultCellStyle.BackColor
            };
        }
    }

    private async void OnAccountChanged(object? sender, EventArgs e)
    {
        if (_suppressComboEvents || _context == null)
        {
            return;
        }

        _selectedAccount = (_accountCombo.SelectedItem as AccountComboItem)?.AccountNumber;

        _toolbar.SetBusy(true);
        _context.Navigation.SetStatus("Lettura del presidio…");
        try
        {
            await LoadWatchAsync(CancellationToken.None);
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

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void OnRefreshRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private static bool Contiene(string valore, string filtro) =>
        valore.Contains(filtro, StringComparison.OrdinalIgnoreCase);

    private static string ShortId(string sessionId) =>
        sessionId.Length <= 8 ? sessionId : sessionId[..8];

    private static string DescribeSeverity(RealtimeWatchSeverity severity) => severity switch
    {
        RealtimeWatchSeverity.Intervento => "Intervento",
        RealtimeWatchSeverity.Attenzione => "Attenzione",
        _ => "Ok"
    };

    /// <summary>
    /// I nomi dei rilievi arrivano dal contratto in PascalCase: separarli rende la colonna
    /// leggibile senza doverli tradurre nel client, dove diventerebbero una seconda verità da
    /// tenere allineata.
    /// </summary>
    private static string SpaziaCamelCase(string valore)
    {
        var risultato = new System.Text.StringBuilder(valore.Length + 8);
        for (var i = 0; i < valore.Length; i++)
        {
            if (i > 0 && char.IsUpper(valore[i]))
            {
                risultato.Append(' ');
                risultato.Append(char.ToLowerInvariant(valore[i]));
            }
            else
            {
                risultato.Append(valore[i]);
            }
        }

        return risultato.ToString();
    }

    /// <summary>Voce della combo: mostra il nome, ma ciò che conta è il numero di conto.</summary>
    private sealed class AccountComboItem
    {
        private readonly WorkspaceAccount _account;

        public AccountComboItem(WorkspaceAccount account) => _account = account;

        public string AccountNumber => _account.AccountNumber;

        public override string ToString() =>
            $"{_account.AccountNumber} · {_account.Name}" +
            (_account.Enabled ? string.Empty : "  (disabilitato)");
    }
}
