using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using cAlgo.API;
using File = System.IO.File;
using HttpMethod = System.Net.Http.HttpMethod;

namespace cAlgo.Robots
{
    // ------------------------------------------------------------------------------------------
    // PiootooTradingSessionBot
    //
    // cBot "live" per la Trading sessions API v1 di Piootoo (vedi docs/domini/trading-sessions-api.md).
    // A differenza di PiootooSignalReplayBot (che rilegge segnali da file), questo cBot dialoga
    // direttamente con PiootooApp.Server:
    //
    //   1) OnStart  -> apre la sessione con POST /trading-sessions/open-plan indicando SOLO il
    //                  codice del piano di trading, l'account e il contesto. Il piano e' la
    //                  configurazione operativa salvata nel workspace e porta con se workspace,
    //                  capitale, commissioni, position sizing, metadata degli strumenti, run
    //                  Titano e modalita di filtro: nessuna di queste cose e' un parametro del
    //                  cBot, cosi non puo' esistere un bot configurato diversamente dal piano che
    //                  crede di eseguire. Vedi docs/domini/trading-plans.md.
    //   2) OnBar    -> ad ogni barra chiusa invia SOLO quella barra (account/simbolo gia legati
    //                  alla sessione) a POST /{sessionId}/bars e riceve gli OrderIntent generati
    //                  dal server. Sono SEMPRE e SOLO intent di INGRESSO: il server non decide
    //                  uscite.
    //   3) Ogni intent di ingresso porta con se la specifica di uscita completa, e il bot la applica
    //      PER INTERO, senza parametri locali e senza interpretazioni: StopLoss e TakeProfit come
    //      livelli nativi cTrader, BreakEven e TrailingStop come modifiche dello stop nativo
    //      sorvegliate a ogni tick, CloseAtUtc (uscita a tempo), ProfitStallAfterUtc (stallo
    //      dell'utile) e MaxBarsInPosition (limite di barre) valutati a ogni barra. Applicarne solo
    //      una parte non produce una versione prudente della strategia, ne produce un'altra: sulle
    //      PC del catalogo il trailing stop e' la causa di uscita di circa un trade su tre ed e' da
    //      solo tutto il profitto. Le strategie che deciderebbero l'uscita a runtime verificando un
    //      pattern sono close-dependent e sono escluse dal catalogo lato server.
    //
    //      Un intent con Status diverso da "Pending" e' un intent che il server ha SCARTATO (sizing
    //      a zero, allocazione Titano nulla, limite di fill per sessione raggiunto): viene
    //      consegnato per tracciabilita e non va eseguito. La quantita da eseguire e' FinalQuantity,
    //      mai Quantity.
    //
    //      Gli ordini di ingresso dei motori Unger sono "next bar at ... stop": vivono la sola barra
    //      successiva al segnale (ExpiresAtUtc) e vengono riemessi a ogni barra col livello
    //      ricalcolato. Il bot quindi cancella l'ordine pending della barra precedente prima di
    //      piazzare il nuovo, e comunque alla barra dopo la scadenza.
    //
    //      Il bot NON e' compatibile con la distribuzione multi-account, e per questo apre il piano
    //      con DistributeToAccounts=false: il server non configura i gruppi della sessione e
    //      POST /bars restituisce intent gia assegnati. Con i gruppi attivi restituirebbe invece
    //      template non assegnati, da reclamare con GET /accounts/{n}/signals (vedi
    //      PiootooLiveTradingBot e docs/domini/distribuzione-multi-account.md); il controllo
    //      all'avvio resta come rete di sicurezza.
    //   4) Ogni fill di apertura viene riportato con POST /{sessionId}/execution-reports.
    //      Ogni CHIUSURA — qualunque ne sia la causa, SL/TP nativo, uscita a tempo o limite barre —
    //      passa da un canale unico: POST /{sessionId}/intents/close-external registra lato server
    //      un intent OrderIntentKind.Close per la posizione, che viene poi referenziato nel normale
    //      execution report (vedi RegisterExternalCloseAndReport). E cosi che nasce il
    //      PersistedTrade (GET /{sessionId}/trades) che alimenta le rotazioni Titano
    //      (vedi docs/domini/titano-rotation.md, "In ExternalBroker un trade nasce esclusivamente
    //      dai fill di chiusura autorevoli").
    //
    //   5) Riavvio: non serve piu ricordarsi la sessione. La chiave (codice piano, contesto,
    //      execution key, account) e' idempotente lato server, quindi rilanciare il cBot con gli
    //      stessi parametri riprende la STESSA sessione; cambiare Execution Key ne apre una nuova.
    //      Il file locale conserva soltanto l'ancora del pannello (equity iniziale e picco) e l'id
    //      della sessione a cui appartiene, per non riusarla su una sessione diversa.
    //      Una volta nota la sessione, il cBot chiede al controller
    //      (GET {sessionId}/intents) i signal gia emessi e li riconcilia con le Position/PendingOrder
    //      ancora aperte in cTrader. Questa ricostruzione e' NECESSARIA: senza l'intent di apertura il
    //      bot non conosce piu CloseAtUtc e MaxBarsInPosition della posizione e non saprebbe piu
    //      chiuderla a tempo. Stop Loss/Take Profit invece non vanno ricostruiti: restano attivi come
    //      livelli nativi sulla posizione anche se il cBot viene riavviato.
    //
    //   6) Pannello a chart: nome bot, versione (costante BotVersion, da aggiornare ad ogni release),
    //      profit e drawdown percentuale correnti, aggiornati a ogni tick. L'ancora (equity iniziale e
    //      picco) viene salvata nello stesso file di stato della sessione, cosi non si azzera a ogni
    //      riavvio del cBot ma solo quando parte davvero una sessione nuova.
    // ------------------------------------------------------------------------------------------

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooTradingSessionBot : Robot
    {
        private const string LabelPrefix = "PiootooSession";
        private const string BotName = "PiootooTradingSessionBot";
        private const string BotVersion = "1.3.0"; // aggiornare qui ad ogni release
        private const string ChartInfoObjectName = "PiootooTradingSessionBot_InfoPanel";

        // ---------------------------------------------------------------- Connessione / sessione

        [Parameter("API Base Url", DefaultValue = "http://localhost:5142", Group = "Connessione")]
        public string ApiBaseUrl { get; set; }

        // Unico parametro funzionale del bot. Il piano vive nel workspace lato server e contiene
        // capitale, commissioni, position sizing, metadata degli strumenti, gruppo/account, run
        // Titano e flag di applicazione del filtro. Il codice e' univoco fra tutti i workspace,
        // quindi qui non serve indicare anche il workspace. Vedi docs/domini/trading-plans.md.
        [Parameter("Codice piano", DefaultValue = "", Group = "Connessione")]
        public string PlanCode { get; set; }

        [Parameter("Account Number Override", DefaultValue = 0, Group = "Connessione")]
        public int AccountNumberOverride { get; set; }

        // Distingue le esecuzioni dello stesso piano. La tripla (piano, contesto, execution key)
        // piu' l'account e' idempotente lato server: rilanciare il cBot con la stessa chiave
        // riprende la sessione, cambiarla ne apre una nuova. Vuoto = "LIVE" sul mercato reale e
        // una chiave derivata dall'istante di partenza nel backtest, cosi ogni backtest e' pulito
        // e ogni riavvio in live ritrova le proprie posizioni.
        [Parameter("Execution Key", DefaultValue = "", Group = "Connessione")]
        public string ExecutionKey { get; set; }

        /// <summary>
        /// Contesto in cui il bot sta girando, letto dalla piattaforma e non da un parametro: e' la
        /// ragione per cui il server puo' fidarsene per scegliere la modalita Titano del piano
        /// (backtest filtrato o realtime) e per rifiutare le combinazioni incoerenti.
        /// </summary>
        private string CurrentClientRunMode => IsBacktesting ? "Backtest" : "Realtime";

        [Parameter("Http Timeout (s)", DefaultValue = 15, MinValue = 1, Group = "Connessione")]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("Persist Stato Locale", DefaultValue = true, Group = "Connessione")]
        public bool PersistLocalState { get; set; }

        // ---------------------------------------------------------------------------- Esecuzione
        //
        // Capitale, commissioni, sizing e metadata dello strumento arrivano dal piano. Qui resta
        // solo la conversione fra la quantita del dominio Piootoo (contratti) e il volume del
        // broker, che e' una proprieta del conto cTrader e non della configurazione operativa.

        [Parameter("Volume Per Quantity Unit", DefaultValue = 1.0, MinValue = 0.0001, Group = "Esecuzione")]
        public double VolumePerQuantityUnit { get; set; }

        // ------------------------------------------------------------------------------- Uscite
        //
        // Non ci sono parametri di uscita: arrivano tutti dall'intent di ingresso (StopLoss,
        // TakeProfit, BreakEven, TrailingStop, CloseAtUtc, ProfitStallAfterUtc,
        // MaxBarsInPosition). Un intent Close del server rappresenta invece un ExitOnly della
        // strategia e va eseguito contro la posizione già aperta.

        // ------------------------------------------------------------------------ Fine settimana
        //
        // Regola operativa: nel fine settimana non restano ne' posizioni ne' ordini. Vive qui, nel
        // bot, e non lato server, perche' e' una regola di sicurezza e deve tenere anche quando il
        // server e' irraggiungibile.

        [Parameter("Flat nel fine settimana", DefaultValue = true, Group = "Fine settimana")]
        public bool FlatAtWeekEnd { get; set; }

        // Il taglio e' un'ora UTC di venerdi invece della chiusura CME reale (16:00 di Chicago)
        // perche' quest'ultima cade alle 21:00 oppure alle 22:00 UTC secondo l'ora legale
        // americana: un default prudente, prima della piu' presta delle due, va bene in entrambi i
        // periodi dell'anno senza dover gestire il fuso.
        [Parameter("Flat da venerdi (HHMM UTC)", DefaultValue = 2045, MinValue = 0, MaxValue = 2359, Group = "Fine settimana")]
        public int WeekEndFlatFromUtc { get; set; }

        [Parameter("Operativo da domenica (HHMM UTC)", DefaultValue = 2300, MinValue = 0, MaxValue = 2359, Group = "Fine settimana")]
        public int WeekEndFlatUntilUtc { get; set; }

        // ------------------------------------------------------------------------------- Stato

        private HttpClient _http;
        private JsonSerializerOptions _json;
        private string _sessionId;
        private string _sessionToken;
        private int _timeframeMinutes;

        // Nome Piootoo dello strumento del grafico: e' la chiave con cui il server indicizza barre,
        // strategie e posizioni, e non coincide col nome del broker quando l'account converte i
        // simboli. Vale SymbolName finche' la sessione non lo ha risolto.
        private string _piootooSymbol;
        private long _accountNumber;
        private bool _sessionReady;
        private double _initialEquity;
        private double _peakEquity;

        // Traccia, per label, l'ultimo intent di apertura inviato (serve a risolvere il fill
        // quando la posizione nasce in modo asincrono, es. ordine pending Stop/Limit).
        private readonly Dictionary<string, OrderIntentDto> _lastOpenIntentByLabel = new();
        private readonly Dictionary<long, OrderIntentDto> _serverCloseIntents = new();

        // Traccia l'intent (di apertura) che ha originato ciascuna posizione, per calcolare le
        // barre trascorse (uscita locale "numero di barre").
        private readonly Dictionary<long, OrderIntentDto> _positionIntent = new();
        private readonly HashSet<string> _submittedCloseIntentIds = new();
        private readonly Dictionary<long, int> _positionEntryBar = new();

        /// <summary>
        /// Barra in cui e stato piazzato l'ordine pending di ciascuna label, per gli intent che
        /// dichiarano una scadenza. Un ordine "next bar" vive una barra: alla successiva va
        /// cancellato, altrimenti resta a mercato e se ne accumula uno per barra.
        /// </summary>
        private readonly Dictionary<string, int> _pendingOrderBar = new();

        /// <summary>
        /// Massimo utile per contratto osservato dopo ProfitStallAfterUtc, per posizione. E'
        /// memoria di esecuzione locale: non viene ricostruita al riavvio, quindi una posizione
        /// ripresa riparte a sorvegliare lo stallo dal proprio utile corrente.
        /// </summary>
        private readonly Dictionary<long, decimal> _peakProfitAfterStall = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(PlanCode))
            {
                Print("Codice piano non impostato.");
                Stop();
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                Print("API Base Url non impostato.");
                Stop();
                return;
            }

            _accountNumber = AccountNumberOverride > 0 ? AccountNumberOverride : Account.Number;
            _piootooSymbol = SymbolName;
            _timeframeMinutes = ResolveTimeframeMinutes(TimeFrame);
            if (_timeframeMinutes <= 0)
            {
                Print("Timeframe '{0}' non riconosciuto: impossibile calcolare TimeframeMinutes.", TimeFrame);
                Stop();
                return;
            }

            _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            var baseUrl = ApiBaseUrl.TrimEnd('/') + "/";
            _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            try
            {
                // Un'unica chiamata: il server risolve il piano e, sulla chiave idempotente,
                // riprende la sessione esistente oppure ne crea e ne avvia una nuova. Non c'e'
                // piu' un /start separato ne' una sessione da ricordare fra un riavvio e l'altro.
                var descriptor = OpenPlanSession();
                _sessionId = descriptor.SessionId;
                _sessionToken = descriptor.SessionToken;

                RequirePlanCoversChart(descriptor);

                if (HasAccountGroups())
                {
                    // Non dovrebbe accadere: apriamo il piano con DistributeToAccounts=false. Se
                    // accade, POST /bars restituisce template NON assegnati, da reclamare con
                    // GET /accounts/{n}/signals: la' vivono slot di gruppo, limite di trade
                    // concorrenti ed eleggibilita'. Eseguirli qui scavalcherebbe l'intero layer di
                    // distribuzione e lo stesso template finirebbe su piu' account.
                    Print("La sessione {0} ha gruppi account configurati: questo cBot esegue i signal " +
                          "di POST /bars e non e' compatibile con la distribuzione multi-account. " +
                          "Usa PiootooLiveTradingBot.", _sessionId);
                    Stop();
                    return;
                }

                // Ancora del pannello profit/drawdown: si riusa solo se appartiene proprio a questa
                // sessione, altrimenti il punto zero e' l'equity attuale. Cosi il pannello non si
                // azzera ad ogni riavvio del cBot ma riparte quando parte davvero un'esecuzione nuova.
                var savedState = PersistLocalState ? LoadSessionState() : null;
                var anchored = savedState != null &&
                               string.Equals(savedState.SessionId, _sessionId, StringComparison.Ordinal);
                _initialEquity = anchored ? savedState.InitialEquity : Account.Equity;
                _peakEquity = anchored ? Math.Max(savedState.PeakEquity, Account.Equity) : Account.Equity;

                SaveSessionState();
                _sessionReady = true;
                Print("Sessione {0} attiva su {1} ({2} min), account {3}, piano {4} (workspace {5}, Titano {6}).",
                    _sessionId, SymbolName, _timeframeMinutes, _accountNumber, PlanCode.Trim(),
                    descriptor.WorkspaceId, descriptor.TitanoMode);

                UpdateChartDisplay();

                // Riavvio del cBot con posizioni/ordini pending gia aperti sulla stessa sessione:
                // ricostruisce lo stato locale necessario per le uscite (limite barre, reporting).
                ReconcileExistingPositionsAndOrders();
            }
            catch (Exception ex)
            {
                Print("Errore avvio sessione trading: {0}", ex.Message);
                Stop();
            }
        }

        protected override void OnTick()
        {
            if (!_sessionReady)
                return;

            // Dentro la finestra di fine settimana non c'e' nulla da proteggere con break-even o
            // trailing: va solo chiuso, e il flat precede tutto.
            if (EnforceWeekEndFlat())
            {
                UpdateChartDisplay();
                return;
            }

            // Break-even e trailing stop vanno sorvegliati a ogni tick: il prezzo puo' raggiungere e
            // perdere la soglia dentro la stessa barra, e l'engine di backtest le valuta intrabar.
            MoveStopsToBreakEven();
            MoveTrailingStops();
            UpdateChartDisplay();
        }

        protected override void OnBar()
        {
            if (!_sessionReady)
                return;

            try
            {
                var weekEndFlat = EnforceWeekEndFlat();

                // Prima si ritira l'ordine della barra appena chiusa, poi si chiede il nuovo signal:
                // l'ordine "next bar" ha esaurito la sua unica barra di validita.
                CancelExpiredPendingOrders();

                // La barra va pubblicata anche dentro la finestra di flat: la storia del server e' il
                // dato su cui girano gli indicatori, e un buco la falserebbe alla riapertura. Gli
                // intent di apertura che ne tornano sono fermati da ApplyOpenIntent.
                PushClosedBar();
                if (weekEndFlat)
                    return;

                MoveStopsToBreakEven();
                MoveTrailingStops();
                CloseExpiredPositions();
            }
            catch (Exception ex)
            {
                Print("Errore elaborazione barra: {0}", ex.Message);
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (!IsOurs(position))
                return;

            if (_positionIntent.ContainsKey(position.Id))
                return;

            if (!_lastOpenIntentByLabel.TryGetValue(position.Label, out var intent))
            {
                Print("Posizione {0} aperta ({1}) senza un intent locale associato: nessun report inviato.", position.Id, position.Label);
                return;
            }

            _positionIntent[position.Id] = intent;
            _positionEntryBar[position.Id] = Bars.Count;
            _pendingOrderBar.Remove(position.Label);
            ReportOpeningFill(intent, position);
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (!IsOurs(position))
                return;

            if (_serverCloseIntents.Remove(position.Id, out var closeIntent))
                ReportClosingFill(closeIntent, position);
            else
                RegisterExternalCloseAndReport(position, args.Reason);

            _positionIntent.Remove(position.Id);
            _positionEntryBar.Remove(position.Id);
            _peakProfitAfterStall.Remove(position.Id);
        }

        protected override void OnStop()
        {
            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;
            if (!_sessionReady || _http == null)
            {
                _http?.Dispose();
                return;
            }

            try
            {
                SendJson<SessionDescriptorDto>(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/stop");
            }
            catch (Exception ex)
            {
                Print("Impossibile fermare la sessione lato server: {0}", ex.Message);
            }
            finally
            {
                _http.Dispose();
            }
        }

        // ------------------------------------------------------------------ Stato sessione su file

        // Non persistiamo ne' i segnali ne' la sessione: i signal stanno lato server
        // (GET {sessionId}/intents) e la sessione si ritrova da sola grazie alla chiave idempotente
        // del piano. Il file conserva solo l'ancora del pannello (equity iniziale e picco) insieme
        // all'id della sessione a cui si riferisce, per non riportarla su un'esecuzione diversa.
        private string ResolveSessionStateFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooTradingSessionBot");
            Directory.CreateDirectory(folder);
            var safePlan = SanitizeForFileName(PlanCode);
            var safeSymbol = SanitizeForFileName(NormalizeSymbol(SymbolName));
            return Path.Combine(folder, $"session-{safePlan}-{_accountNumber}-{safeSymbol}-{_timeframeMinutes}.json");
        }

        private static string SanitizeForFileName(string value)
        {
            var text = (value ?? string.Empty).Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
                text = text.Replace(invalid, '_');
            return text;
        }

        private SessionStateFileDto LoadSessionState()
        {
            try
            {
                var path = ResolveSessionStateFilePath();
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SessionStateFileDto>(json, _json);
            }
            catch (Exception ex)
            {
                Print("Impossibile leggere lo stato sessione salvato su file: {0}", ex.Message);
                return null;
            }
        }

        private void SaveSessionState()
        {
            if (!PersistLocalState)
                return;

            try
            {
                var path = ResolveSessionStateFilePath();
                var json = JsonSerializer.Serialize(
                    new SessionStateFileDto
                    {
                        SessionId = _sessionId,
                        InitialEquity = _initialEquity,
                        PeakEquity = _peakEquity
                    }, _json);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Print("Impossibile salvare lo stato sessione su file: {0}", ex.Message);
            }
        }

        // ------------------------------------------------------------------------- Pannello a chart

        private void UpdateChartDisplay()
        {
            var equity = Account.Equity;
            var peakChanged = false;
            if (equity > _peakEquity)
            {
                _peakEquity = equity;
                peakChanged = true;
            }

            var profit = equity - _initialEquity;
            var drawdownPct = _peakEquity > 0 ? (_peakEquity - equity) / _peakEquity * 100.0 : 0.0;
            var profitColor = profit >= 0 ? Color.LightGreen : Color.OrangeRed;

            var text = $"{BotName} v{BotVersion}\n" +
                       $"Profit: {profit:+0.00;-0.00;0.00}\n" +
                       $"Drawdown: {drawdownPct:0.00}%";

            Chart.DrawStaticText(ChartInfoObjectName, text, VerticalAlignment.Top, HorizontalAlignment.Right, profitColor);

            // Salva l'ancora solo quando il picco si aggiorna, per non scrivere su file ad ogni tick.
            if (peakChanged)
                SaveSessionState();
        }

        // ------------------------------------------------------------- Ricostruzione dopo riavvio

        private void ReconcileExistingPositionsAndOrders()
        {
            try
            {
                var openPositions = Positions.Where(IsOurs).ToList();
                var pendingOrders = PendingOrders
                    .Where(o => o.SymbolName == SymbolName && o.Label != null && o.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                    .ToList();

                if (openPositions.Count == 0 && pendingOrders.Count == 0)
                    return;

                List<OrderIntentDto> intents;
                try
                {
                    intents = SendJson<List<OrderIntentDto>>(HttpMethod.Get, $"api/v1/trading-sessions/{_sessionId}/intents?after=0")
                              ?? new List<OrderIntentDto>();
                }
                catch (Exception ex)
                {
                    Print("Impossibile recuperare i signal dal server per la ricostruzione dello stato: {0}", ex.Message);
                    intents = new List<OrderIntentDto>();
                }

                foreach (var position in openPositions)
                {
                    var strategyCode = ExtractStrategyCode(position.Label);
                    var entryBarIndex = ResolveBarIndexForTime(position.EntryTime);
                    _positionEntryBar[position.Id] = entryBarIndex;

                    var matched = FindLatestMatchingIntent(intents, strategyCode);
                    if (matched != null)
                        _positionIntent[position.Id] = matched;

                    Print("Riavvio: posizione {0} ({1}) ricostruita, entry bar {2}.", position.Id, position.Label, entryBarIndex);
                }

                foreach (var order in pendingOrders)
                {
                    var matched = FindLatestMatchingIntent(intents, ExtractStrategyCode(order.Label));
                    if (matched != null)
                    {
                        _lastOpenIntentByLabel[order.Label] = matched;

                        // L'ordine sopravvissuto al riavvio va comunque ritirato: se il signal aveva
                        // una scadenza, la barra su cui era valido e' al piu' quella corrente.
                        if (matched.ExpiresAtUtc.HasValue)
                            _pendingOrderBar[order.Label] = Bars.Count;

                        Print("Riavvio: ordine pending {0} ({1}) ricollegato al signal {2}.", order.Id, order.Label, matched.IntentId);
                    }
                    else
                    {
                        Print("Riavvio: ordine pending {0} ({1}) senza signal corrispondente sul server: " +
                              "al fill non verra inviato un execution report.", order.Id, order.Label);
                    }
                }
            }
            catch (Exception ex)
            {
                Print("Errore durante la ricostruzione dello stato dopo il riavvio: {0}", ex.Message);
            }
        }

        private OrderIntentDto FindLatestMatchingIntent(List<OrderIntentDto> intents, string strategyCode) =>
            intents
                .Where(i => !i.IsClose &&
                            string.Equals(i.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase) &&
                            NormalizeSymbol(ResolveIntentSymbol(i)) == NormalizeSymbol(SymbolName))
                .OrderByDescending(i => i.CreatedAtUtc)
                .FirstOrDefault();

        private int ResolveBarIndexForTime(DateTime timeUtc)
        {
            for (var i = Bars.Count - 1; i >= 0; i--)
            {
                if (BarOpenTimeUtc(Bars.OpenTimes[i]) <= timeUtc)
                    return i;
            }

            return 0;
        }

        /// <summary>
        /// Marca come UTC l'orario di una barra. Il [Robot] dichiara TimeZone=UTC — che e' un
        /// attributo di compilazione, non l'impostazione di visualizzazione di cTrader, quindi non
        /// dipende da come l'utente ha configurato la piattaforma — ma la serie restituisce
        /// DateTime con Kind non impostato. Senza questo passaggio System.Text.Json serializza
        /// senza il suffisso "Z", il server rilegge Kind=Unspecified e ValidateBar/RequireUtc
        /// rifiuta la barra: il push fallirebbe sempre. Unico punto di conversione del bot.
        /// </summary>
        private static DateTime BarOpenTimeUtc(DateTime barOpenTime) =>
            DateTime.SpecifyKind(barOpenTime, DateTimeKind.Utc);

        // ------------------------------------------------------------------------ Sessione HTTP

        /// <summary>
        /// Risolve il piano e crea o riprende la sessione. <c>DistributeToAccounts=false</c> chiede
        /// al server di NON configurare i gruppi: senza gruppi POST /bars restituisce intent gia
        /// assegnati, che e' l'unico modello di esecuzione che questo cBot implementa. Il server
        /// rifiuta l'apertura se il piano dichiara un limite di trade concorrenti, perche' quel
        /// limite e' applicato dal percorso di claim e qui non avrebbe dove agire.
        /// </summary>
        private SessionDescriptorDto OpenPlanSession()
        {
            var request = new OpenPlanSessionRequestDto
            {
                PlanCode = PlanCode.Trim(),
                // NON e' un parametro: lo legge la piattaforma. Cosi il server puo' scegliere la
                // modalita Titano coerente col contesto e rifiutare le combinazioni impossibili
                // invece di lasciarle passare e produrre numeri plausibili ma sbagliati.
                ClientRunMode = CurrentClientRunMode,
                ExecutionKey = ResolveExecutionKey(),
                AccountNumber = _accountNumber.ToString(),
                DistributeToAccounts = false
            };

            var descriptor = SendJson<SessionDescriptorDto>(
                HttpMethod.Post, "api/v1/trading-sessions/open-plan", request, includeToken: false);
            if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.SessionId))
                throw new InvalidOperationException($"il piano '{PlanCode.Trim()}' non ha restituito una sessione");

            return descriptor;
        }

        private string ResolveExecutionKey()
        {
            if (!string.IsNullOrWhiteSpace(ExecutionKey))
                return ExecutionKey.Trim();

            // In backtest l'istante di avvio e' fisso per tutto il run: ogni run parte da una
            // sessione nuova, ma un rilancio dello stesso run non ne accumula un'altra.
            return IsBacktesting ? $"BT-{Server.TimeInUtc:yyyyMMddHHmmss}" : "LIVE";
        }

        /// <summary>
        /// Verifica che il grafico corrisponda a una coppia (simbolo, timeframe) del masterfilter
        /// del piano. Il server accetta e archivia qualunque barra: se nessuna strategia gira su
        /// quello stream, il bot lavorerebbe per sempre senza produrre un solo segnale e senza un
        /// errore. Meglio non partire.
        /// </summary>
        /// <summary>
        /// Verifica che il piano copra il grafico e fissa il simbolo Piootoo corrispondente.
        ///
        /// <para>Il match e' sul nome che lo strumento ha sull'ACCOUNT, che e' quello del grafico;
        /// verso il server si usa poi il nome Piootoo, con cui sono indicizzate barre, posizioni e
        /// strategie. Sono lo stesso strumento con due nomi, e confonderli significa spedire barre
        /// che non alimentano nessuna strategia.</para>
        /// </summary>
        private void RequirePlanCoversChart(SessionDescriptorDto descriptor)
        {
            var instruments = descriptor.Instruments ?? new List<TradingInstrumentDto>();
            var instrument = instruments.FirstOrDefault(
                item => NormalizeSymbol(ResolveInstrumentAccountSymbol(item)) == NormalizeSymbol(SymbolName));
            if (instrument == null)
            {
                throw new InvalidOperationException(
                    $"il piano non ha strategie su {SymbolName}. Strumenti previsti: " +
                    $"{string.Join(", ", instruments.Select(DescribeInstrument))}");
            }

            _piootooSymbol = instrument.Symbol;
            if (NormalizeSymbol(_piootooSymbol) != NormalizeSymbol(SymbolName))
                Print("Conversione symbol attiva: il grafico {0} alimenta lo strumento Piootoo {1}.",
                    SymbolName, _piootooSymbol);

            var timeframes = instrument.TimeframesMinutes ?? new List<int>();
            if (!timeframes.Contains(_timeframeMinutes))
            {
                throw new InvalidOperationException(
                    $"il piano usa {SymbolName} a {string.Join("/", timeframes)} minuti, il grafico e' a " +
                    $"{_timeframeMinutes}: le barre inviate non alimenterebbero alcuna strategia");
            }
        }

        private static string DescribeInstrument(TradingInstrumentDto instrument)
        {
            var accountSymbol = ResolveInstrumentAccountSymbol(instrument);
            var name = NormalizeSymbol(accountSymbol) == NormalizeSymbol(instrument.Symbol)
                ? instrument.Symbol
                : $"{accountSymbol} [{instrument.Symbol}]";
            return $"{name} ({string.Join("/", instrument.TimeframesMinutes ?? new List<int>())} min)";
        }

        private static string ResolveInstrumentAccountSymbol(TradingInstrumentDto instrument) =>
            string.IsNullOrWhiteSpace(instrument.AccountSymbol) ? instrument.Symbol : instrument.AccountSymbol;

        /// <summary>
        /// Vero se la sessione ha una mappa account -> gruppo, cioe' se usa la distribuzione
        /// multi-account. In caso di errore di rete si risponde no: bloccare l'avvio per una GET
        /// fallita sarebbe peggio del rischio che copre.
        /// </summary>
        private bool HasAccountGroups()
        {
            try
            {
                var groups = SendJson<List<AccountGroupMappingDto>>(
                    HttpMethod.Get, $"api/v1/trading-sessions/{_sessionId}/account-groups");
                return groups != null && groups.Count > 0;
            }
            catch (Exception ex)
            {
                Print("Impossibile verificare i gruppi account della sessione: {0}", ex.Message);
                return false;
            }
        }

        private void PushClosedBar()
        {
            var closedBar = Bars.Last(1);
            var openTimeUtc = BarOpenTimeUtc(closedBar.OpenTime);

            var payload = new PushBarsRequestDto
            {
                SessionId = _sessionId,
                SessionToken = _sessionToken,
                Bars = new List<ClosedBarDto>
                {
                    new ClosedBarDto
                    {
                        // Nome Piootoo: e' con quello che il server indicizza le serie.
                        Symbol = _piootooSymbol,
                        TimeframeMinutes = _timeframeMinutes,
                        BarTimeUtc = openTimeUtc,
                        Sequence = new DateTimeOffset(openTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
                        IdempotencyKey = $"{_piootooSymbol}:{_timeframeMinutes}:{openTimeUtc:O}",
                        Bar = new OhlcvDto
                        {
                            DateTime = openTimeUtc,
                            Open = (decimal)closedBar.Open,
                            High = (decimal)closedBar.High,
                            Low = (decimal)closedBar.Low,
                            Close = (decimal)closedBar.Close,
                            Volume = (decimal)closedBar.TickVolume
                        }
                    }
                }
            };

            PushBarsResponseDto response;
            try
            {
                response = SendJson<PushBarsResponseDto>(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/bars", payload);
            }
            catch (Exception ex)
            {
                Print("Push barra fallito ({0} {1}): {2}", SymbolName, openTimeUtc, ex.Message);
                return;
            }

            foreach (var intent in response.Intents)
            {
                // Il match e' sul simbolo dell'ACCOUNT, non su quello Piootoo: la tabella di
                // conversione del conto traduce @NQ nel nome del broker, ed e' quello il nome del
                // grafico su cui gira il bot. Symbol resta il nome interno con cui il server
                // indicizza posizioni e valutazioni, e non coincide per forza.
                if (NormalizeSymbol(ResolveIntentSymbol(intent)) != NormalizeSymbol(SymbolName))
                    continue;

                ApplyIntent(intent);
            }
        }

        // -------------------------------------------------------------------------- Gestione segnali

        private void ApplyIntent(OrderIntentDto intent)
        {
            if (string.Equals(intent.Side, "Hold", StringComparison.OrdinalIgnoreCase))
                return;

            // Il server consegna anche gli intent che ha appena scartato: sizing che ha azzerato la
            // quantita, allocazione Titano nulla, limite di fill per sessione raggiunto. Eseguirli
            // significa rimettere a mercato esattamente i trade che il server ha rifiutato.
            if (!string.IsNullOrEmpty(intent.Status) &&
                !string.Equals(intent.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                Print("Intent {0} ({1}) ignorato: stato {2} lato server ({3}).",
                    intent.IntentId, intent.StrategyCode, intent.Status, intent.Reason);
                return;
            }

            if (intent.IsClose || string.Equals(intent.Kind, "Close", StringComparison.OrdinalIgnoreCase))
            {
                ApplyCloseIntent(intent);
                return;
            }

            ApplyOpenIntent(intent, MakeLabel(intent.StrategyCode));
        }

        private void ApplyOpenIntent(OrderIntentDto intent, string label)
        {
            var tradeType = string.Equals(intent.Side, "Sell", StringComparison.OrdinalIgnoreCase) ? TradeType.Sell : TradeType.Buy;

            // Gli intent di chiusura passano comunque (riducono rischio): qui si fermano solo le
            // aperture, altrimenti il flat appena imposto verrebbe disfatto dal signal successivo.
            if (FlatAtWeekEnd && IsWeekEndFlatWindow(Server.TimeInUtc))
            {
                Print("Intent {0} ({1}) ignorato: finestra di flat di fine settimana.",
                    intent.IntentId, intent.StrategyCode);
                return;
            }

            if (Positions.Find(label, SymbolName, tradeType) != null)
            {
                Print("Intent {0} ({1} {2}) ignorato: posizione gia aperta su questa label.", intent.IntentId, tradeType, label);
                return;
            }

            // Solo FinalQuantity: e' la quantita dopo Titano e position sizing. Ricadere su
            // Quantity quando FinalQuantity e' zero rimetterebbe a mercato la size base proprio
            // nei casi in cui il server ha deciso di non operare.
            var rawQuantity = intent.FinalQuantity;
            if (rawQuantity <= 0)
            {
                Print("Intent {0} ({1}) scartato: quantita finale nulla ({2}).",
                    intent.IntentId, intent.StrategyCode, intent.Reason);
                return;
            }

            if (intent.TimeframeMinutes > 0 && intent.TimeframeMinutes != _timeframeMinutes)
            {
                Print("Intent {0} scartato: la strategia lavora su {1} minuti, il grafico su {2}. " +
                      "MaxBarsInPosition non sarebbe contabile. Usa un grafico dello stesso timeframe.",
                    intent.IntentId, intent.TimeframeMinutes, _timeframeMinutes);
                return;
            }

            var rawVolume = (double)rawQuantity * VolumePerQuantityUnit;
            var volume = Symbol.NormalizeVolumeInUnits(rawVolume, RoundingMode.Down);
            if (volume <= 0)
            {
                Print("Intent {0} scartato: volume normalizzato non valido.", intent.IntentId);
                return;
            }

            // Sempre applicati: sono livelli nativi cTrader e sopravvivono a un riavvio del bot.
            double? stopLossPips = ToPips(intent.StopLoss);
            double? takeProfitPips = ToPips(intent.TakeProfit);
            if (!stopLossPips.HasValue && !takeProfitPips.HasValue &&
                !(intent.TrailingStop > 0) && !intent.CloseAtUtc.HasValue &&
                !intent.ProfitStallAfterUtc.HasValue && !(intent.MaxBarsInPosition > 0))
            {
                Print("Intent {0} ({1}) non porta alcuna condizione di uscita: la posizione restera aperta " +
                      "finche non arriva un segnale opposto. Verifica la strategia.", intent.IntentId, intent.StrategyCode);
            }

            // Il segnale precedente della stessa strategia e' scaduto nel momento in cui ne arriva
            // uno nuovo: il motore riemette l'ordine a ogni barra col livello ricalcolato, quindi
            // il vecchio ordine pending non e' un secondo ordine, e' lo stesso ordine da sostituire.
            CancelPendingOrders(label, "sostituito dal signal successivo");

            _lastOpenIntentByLabel[label] = intent;

            TradeResult result;
            switch (intent.OrderType)
            {
                case "Stop":
                    result = PlaceStopOrder(tradeType, SymbolName, volume, (double)intent.Price, label, stopLossPips, takeProfitPips);
                    break;
                case "Limit":
                    result = PlaceLimitOrder(tradeType, SymbolName, volume, (double)intent.Price, label, stopLossPips, takeProfitPips);
                    break;
                default:
                    result = ExecuteMarketOrder(tradeType, SymbolName, volume, label, stopLossPips, takeProfitPips, intent.Reason);
                    break;
            }

            if (!result.IsSuccessful)
            {
                Print("Errore apertura posizione da intent {0} ({1} {2}): {3}", intent.IntentId, tradeType, SymbolName, result.Error);
                _lastOpenIntentByLabel.Remove(label);
                return;
            }

            // Scadenza dell'ordine pending: la barra corrente e' l'unica in cui puo' essere
            // eseguito, come "next bar at ... stop" di EasyLanguage.
            if (intent.ExpiresAtUtc.HasValue && result.PendingOrder != null)
                _pendingOrderBar[label] = Bars.Count;
            else
                _pendingOrderBar.Remove(label);

            // Se il risultato porta gia una Position (ordine a mercato), il fill effettivo viene
            // comunque riportato da OnPositionOpened per avere un solo punto di reporting ed
            // evitare doppi execution-report verso il server.
        }

        private void ApplyCloseIntent(OrderIntentDto intent)
        {
            if (!_submittedCloseIntentIds.Add(intent.IntentId))
                return;

            var position = Positions.FirstOrDefault(candidate =>
                candidate.SymbolName == SymbolName &&
                candidate.Label == MakeLabel(intent.StrategyCode));
            if (position is null)
            {
                _submittedCloseIntentIds.Remove(intent.IntentId);
                Print("Intent di chiusura {0} ignorato: posizione {1}/{2} non trovata.",
                    intent.IntentId, intent.Symbol, intent.StrategyCode);
                return;
            }

            _serverCloseIntents[position.Id] = intent;
            var result = ClosePosition(position);
            if (!result.IsSuccessful)
            {
                _serverCloseIntents.Remove(position.Id);
                _submittedCloseIntentIds.Remove(intent.IntentId);
                Print("Errore chiusura intent {0} sulla posizione {1}: {2}",
                    intent.IntentId, position.Id, result.Error);
            }
        }

        /// <summary>
        /// Cancella gli ordini pending la cui barra di validita e' passata. I motori Unger emettono
        /// ordini "next bar": vivono la sola barra successiva al segnale e vengono riemessi finche'
        /// la condizione resta valida. Senza questa cancellazione ne resta a mercato uno per ogni
        /// barra della finestra operativa, a livelli diversi, tutti eseguibili.
        /// </summary>
        private void CancelExpiredPendingOrders()
        {
            if (_pendingOrderBar.Count == 0)
                return;

            foreach (var entry in _pendingOrderBar.ToList())
            {
                if (Bars.Count <= entry.Value)
                    continue;

                CancelPendingOrders(entry.Key, "scaduto (valido una barra sola)");
                _pendingOrderBar.Remove(entry.Key);
            }
        }

        /// <summary>
        /// Porta il conto a flat quando la finestra di fine settimana e' aperta: prima cancella gli
        /// ordini, poi chiude le posizioni. L'ordine conta: chiudere per primo lascerebbe uno stop a
        /// mercato che puo' riaprire nel frattempo.
        ///
        /// <para>Restituisce true quando la finestra e' attiva, cosi' il chiamante sa che in questa
        /// passata non si apre nulla.</para>
        /// </summary>
        private bool EnforceWeekEndFlat()
        {
            if (!FlatAtWeekEnd || !IsWeekEndFlatWindow(Server.TimeInUtc))
                return false;

            var labels = PendingOrders
                .Where(o => o.SymbolName == SymbolName && o.Label != null &&
                            o.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .Select(o => o.Label)
                .Distinct()
                .ToList();
            foreach (var label in labels)
            {
                CancelPendingOrders(label, "flat di fine settimana");
                _pendingOrderBar.Remove(label);
            }

            foreach (var position in Positions.Where(IsOurs).ToList())
            {
                var result = ClosePosition(position);
                if (result.IsSuccessful)
                    Print("Posizione {0} chiusa per il flat di fine settimana.", position.Id);
                else
                    Print("Impossibile chiudere {0} per il fine settimana: {1}", position.Id, result.Error);
            }

            return true;
        }

        /// <summary>
        /// La finestra va da venerdi all'ora dichiarata fino alla domenica all'ora di riapertura.
        /// Il sabato e' sempre dentro.
        /// </summary>
        private bool IsWeekEndFlatWindow(DateTime nowUtc)
        {
            var hhmm = nowUtc.Hour * 100 + nowUtc.Minute;
            switch (nowUtc.DayOfWeek)
            {
                case DayOfWeek.Friday:
                    return hhmm >= WeekEndFlatFromUtc;
                case DayOfWeek.Saturday:
                    return true;
                case DayOfWeek.Sunday:
                    return hhmm < WeekEndFlatUntilUtc;
                default:
                    return false;
            }
        }

        private void CancelPendingOrders(string label, string reason)
        {
            foreach (var order in PendingOrders
                .Where(o => o.SymbolName == SymbolName && o.Label == label)
                .ToList())
            {
                var result = CancelPendingOrder(order);
                if (result.IsSuccessful)
                    Print("Ordine pending {0} ({1}) cancellato: {2}.", order.Id, label, reason);
                else
                    Print("Impossibile cancellare l'ordine pending {0} ({1}): {2}", order.Id, label, result.Error);
            }
        }

        /// <summary>
        /// Quando il movimento favorevole raggiunge il break-even dichiarato dall'intent, sposta lo
        /// stop nativo al prezzo di ingresso. La soglia e' in punti, non in pip.
        /// </summary>
        private void MoveStopsToBreakEven()
        {
            foreach (var position in Positions.Where(IsOurs).ToList())
            {
                if (!_positionIntent.TryGetValue(position.Id, out var intent))
                    continue;
                if (!intent.BreakEven.HasValue || intent.BreakEven.Value <= 0)
                    continue;

                var favorableMove = position.TradeType == TradeType.Buy
                    ? Symbol.Bid - position.EntryPrice
                    : position.EntryPrice - Symbol.Ask;
                if (favorableMove < (double)intent.BreakEven.Value)
                    continue;

                var alreadyAtEntry = position.StopLoss.HasValue &&
                    (position.TradeType == TradeType.Buy
                        ? position.StopLoss.Value >= position.EntryPrice
                        : position.StopLoss.Value <= position.EntryPrice);
                if (alreadyAtEntry)
                    continue;

                var result = ModifyPosition(position, position.EntryPrice, position.TakeProfit);
                if (!result.IsSuccessful)
                    Print("Impossibile spostare a break-even la posizione {0} ({1}): {2}",
                        position.Id, position.Label, result.Error);
            }
        }

        /// <summary>
        /// Mantiene lo stop nativo alla distanza dichiarata dall'intent rispetto al massimo/minimo
        /// favorevole corrente. Lo stop viene aggiornato solo in direzione protettiva, quindi un
        /// ritracciamento non lo allarga mai.
        /// </summary>
        private void MoveTrailingStops()
        {
            foreach (var position in Positions.Where(IsOurs).ToList())
            {
                if (!_positionIntent.TryGetValue(position.Id, out var intent))
                    continue;
                if (!intent.TrailingStop.HasValue || intent.TrailingStop.Value <= 0)
                    continue;

                var distance = (double)intent.TrailingStop.Value;
                var candidate = position.TradeType == TradeType.Buy
                    ? Symbol.Bid - distance
                    : Symbol.Ask + distance;
                var improves = !position.StopLoss.HasValue ||
                    (position.TradeType == TradeType.Buy
                        ? candidate > position.StopLoss.Value
                        : candidate < position.StopLoss.Value);
                if (!improves)
                    continue;

                var result = ModifyPosition(position, candidate, position.TakeProfit);
                if (!result.IsSuccessful)
                    Print("Impossibile aggiornare il trailing stop della posizione {0} ({1}): {2}",
                        position.Id, position.Label, result.Error);
            }
        }

        /// <summary>
        /// Applica le uscite che il broker non sa gestire nativamente: uscita a tempo
        /// (<c>CloseAtUtc</c>) e limite di barre (<c>MaxBarsInPosition</c>). Entrambe arrivano
        /// dall'intent di ingresso, non da parametri del bot. Stop Loss e Take Profit non passano
        /// di qui: sono livelli nativi gia applicati all'ordine.
        ///
        /// Ogni chiusura viene poi registrata lato server da OnPositionClosed tramite
        /// RegisterExternalCloseAndReport, cosi confluisce in trades.json e alimenta Titano.
        /// </summary>
        private void CloseExpiredPositions()
        {
            var now = Server.TimeInUtc;

            foreach (var position in Positions
                .Where(p => p.SymbolName == SymbolName && p.Label != null && p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
            {
                if (!_positionIntent.TryGetValue(position.Id, out var intent))
                    continue;

                string reason = null;

                // Utile aperto per singolo contratto, nella stessa grandezza con cui il server
                // dichiara le soglie: e' cosi che le strategie EasyLanguage esprimono
                // openpositionprofit quando e' attivo setstopcontract.
                //
                // Quelle soglie sono per contratto PIOOTOO, mentre il volume a mercato e' in
                // contratti del BROKER: con un ContractMultiplier diverso da 1 le due grandezze non
                // coincidono, e confrontarle direttamente farebbe scattare le uscite alla soglia
                // sbagliata di un fattore pari al moltiplicatore.
                var contractMultiplier = intent.ContractMultiplier > 0 ? intent.ContractMultiplier : 1m;
                var brokerContracts = (decimal)(position.VolumeInUnits / VolumePerQuantityUnit);
                var profitPerContract = brokerContracts > 0
                    ? (decimal)position.NetProfit / brokerContracts * contractMultiplier
                    : 0m;

                if (intent.CloseAtUtc.HasValue && now >= intent.CloseAtUtc.Value)
                {
                    // La chiusura a tempo puo' essere condizionata all'utile: alcune strategie
                    // escono all'ora prevista solo se sono sotto, altre lasciano correre il
                    // vincente che ha gia' raggiunto la soglia. Stessa regola, soglia diversa.
                    if (!intent.TimeExitOnlyIfProfitBelowMoneyPerContract.HasValue ||
                        profitPerContract < intent.TimeExitOnlyIfProfitBelowMoneyPerContract.Value)
                    {
                        reason = "uscita a tempo (CloseAtUtc)";
                    }
                }

                if (reason == null && intent.MaxBarsInPosition > 0 &&
                    _positionEntryBar.TryGetValue(position.Id, out var entryBar) &&
                    Bars.Count - entryBar >= intent.MaxBarsInPosition.Value)
                {
                    reason = "limite barre (MaxBarsInPosition)";
                }

                // Uscita per stallo dell'utile: dopo la deadline si tiene il massimo osservato e
                // si chiude alla prima barra che non lo supera. Il picco vive in un dizionario
                // locale perche' e' memoria di esecuzione, non parte dell'intent.
                if (reason == null && intent.ProfitStallAfterUtc.HasValue && now >= intent.ProfitStallAfterUtc.Value)
                {
                    decimal peak;
                    if (!_peakProfitAfterStall.TryGetValue(position.Id, out peak))
                    {
                        _peakProfitAfterStall[position.Id] = profitPerContract;
                    }
                    else if (profitPerContract > peak)
                    {
                        _peakProfitAfterStall[position.Id] = profitPerContract;
                    }
                    else
                    {
                        reason = "stallo dell'utile (ProfitStallAfterUtc)";
                    }
                }

                if (reason == null)
                    continue;

                var result = ClosePosition(position);
                if (!result.IsSuccessful)
                    Print("Errore chiusura per {0} su posizione {1}: {2}", reason, position.Id, result.Error);
            }
        }

        // ------------------------------------------------------------------------- Reporting fill

        private void ReportOpeningFill(OrderIntentDto intent, Position position)
        {
            var report = new ExecutionReportRequestDto
            {
                SessionToken = _sessionToken,
                Report = new ExternalExecutionReportDto
                {
                    ReportId = $"open-{position.Id}-{Guid.NewGuid():N}",
                    IntentId = intent.IntentId,
                    ExternalOrderId = position.Id.ToString(),
                    Status = "Filled",
                    CumulativeFilledQuantity = intent.FinalQuantity > 0 ? intent.FinalQuantity : intent.Quantity,
                    FillPrice = (decimal)position.EntryPrice,
                    Commission = 0m,
                    EventTimeUtc = Server.TimeInUtc
                }
            };

            TrySendReport(report, "apertura");
        }

        private void ReportClosingFill(OrderIntentDto intent, Position position)
        {
            var exitPrice = position.TradeType == TradeType.Buy
                ? position.EntryPrice + position.Pips * Symbol.PipSize
                : position.EntryPrice - position.Pips * Symbol.PipSize;

            var report = new ExecutionReportRequestDto
            {
                SessionToken = _sessionToken,
                Report = new ExternalExecutionReportDto
                {
                    ReportId = $"close-{position.Id}-{Guid.NewGuid():N}",
                    IntentId = intent.IntentId,
                    ExternalOrderId = position.Id.ToString(),
                    Status = "Filled",
                    CumulativeFilledQuantity = intent.FinalQuantity > 0 ? intent.FinalQuantity : intent.Quantity,
                    FillPrice = (decimal)exitPrice,
                    Commission = (decimal)Math.Abs(position.Commissions),
                    EventTimeUtc = Server.TimeInUtc
                }
            };

            TrySendReport(report, "chiusura");
        }

        private void RegisterExternalCloseAndReport(Position position, PositionCloseReason reason)
        {
            // La strategia si ricava direttamente dalla label (formato "PiootooSession:{StrategyCode}"),
            // non dal dizionario locale _positionIntent: cosi il percorso funziona anche per posizioni
            // aperte prima di un riavvio del cBot, di cui non abbiamo piu l'intent originale in memoria.
            var strategyCode = ExtractStrategyCode(position.Label);
            if (string.IsNullOrWhiteSpace(strategyCode))
            {
                Print("Posizione {0} ({1}) chiusa (motivo: {2}) senza una label riconoscibile: " +
                      "impossibile registrare la chiusura lato server.", position.Id, position.Label, reason);
                return;
            }

            try
            {
                var closeIntentRequest = new CreateExternalCloseIntentRequestDto
                {
                    SessionToken = _sessionToken,
                    StrategyCode = strategyCode,
                    Symbol = _piootooSymbol,
                    Quantity = 0m, // 0 = il server usa l'intera quantita della posizione aperta
                    Reason = $"LocalExit:{reason}"
                };

                var closeIntent = SendJson<OrderIntentDto>(
                    HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/intents/close-external", closeIntentRequest);

                ReportClosingFill(closeIntent, position);
            }
            catch (Exception ex)
            {
                Print("Impossibile registrare la chiusura locale di {0} ({1}) lato server: {2}", position.Id, position.Label, ex.Message);
            }
        }

        private void TrySendReport(ExecutionReportRequestDto report, string kind)
        {
            try
            {
                SendJson<SessionSnapshotDto>(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/execution-reports", report);
            }
            catch (Exception ex)
            {
                Print("Errore invio execution report di {0} (intent {1}): {2}", kind, report.Report.IntentId, ex.Message);
            }
        }

        // --------------------------------------------------------------------------------- HTTP

        private TResponse SendJson<TResponse>(HttpMethod method, string path, object body = null, bool includeToken = true)
        {
            using var request = new HttpRequestMessage(method, path);
            if (includeToken && !string.IsNullOrEmpty(_sessionToken))
                request.Headers.Add("X-Session-Token", _sessionToken);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, body.GetType(), _json);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = _http.Send(request);
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)response.StatusCode} su {path}: {text}");

            if (string.IsNullOrWhiteSpace(text))
                return default;

            return JsonSerializer.Deserialize<TResponse>(text, _json);
        }

        // ------------------------------------------------------------------------------- Helper

        private bool IsOurs(Position position) =>
            position.SymbolName == SymbolName &&
            position.Label != null &&
            position.Label.StartsWith(LabelPrefix, StringComparison.Ordinal);

        private double? ToPips(decimal? priceDistance)
        {
            if (!priceDistance.HasValue || priceDistance.Value <= 0)
                return null;

            return (double)priceDistance.Value / Symbol.PipSize;
        }

        private static string NormalizeSymbol(string symbol) =>
            (symbol ?? string.Empty).Trim().TrimStart('@').ToUpperInvariant();

        /// <summary>
        /// Simbolo con cui inoltrare l'ordine: quello dell'account se la sessione lo ha risolto,
        /// altrimenti quello Piootoo. Il fallback copre le sessioni aperte senza anagrafica account.
        /// </summary>
        private static string ResolveIntentSymbol(OrderIntentDto intent) =>
            string.IsNullOrWhiteSpace(intent.AccountSymbol) ? intent.Symbol : intent.AccountSymbol;

        private static string MakeLabel(string strategyCode) =>
            $"{LabelPrefix}:{strategyCode}";

        private static string ExtractStrategyCode(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "";

            var prefix = LabelPrefix + ":";
            return label.StartsWith(prefix, StringComparison.Ordinal) ? label.Substring(prefix.Length) : label;
        }

        private static int ResolveTimeframeMinutes(TimeFrame timeFrame)
        {
            var name = timeFrame.ToString();

            if (name.StartsWith("Minute", StringComparison.Ordinal))
                return ParseTrailingNumber(name, "Minute", 1);
            if (name.StartsWith("Hour", StringComparison.Ordinal))
                return ParseTrailingNumber(name, "Hour", 1) * 60;
            if (name.StartsWith("Daily", StringComparison.Ordinal))
                return 1440;
            if (name.StartsWith("Day", StringComparison.Ordinal))
                return ParseTrailingNumber(name, "Day", 1) * 1440;
            if (name.StartsWith("Weekly", StringComparison.Ordinal))
                return 10080;
            if (name.StartsWith("Week", StringComparison.Ordinal))
                return ParseTrailingNumber(name, "Week", 1) * 10080;
            if (name.StartsWith("Monthly", StringComparison.Ordinal) || name.StartsWith("Month", StringComparison.Ordinal))
                return 43200;

            return 0;
        }

        private static int ParseTrailingNumber(string value, string prefix, int fallback)
        {
            var suffix = value.Substring(prefix.Length);
            return int.TryParse(suffix, out var parsed) && parsed > 0 ? parsed : fallback;
        }

        // ----------------------------------------------------------------------- DTO contratto API
        // Copie locali (POCO) dei contratti definiti in Piootoo.Shared.Models.Trading, cosi il cBot
        // resta un singolo file senza riferimenti al progetto server. I campi enum-like (Side,
        // OrderType, ExecutionMode, Status, ...) sono trattati come string con il nome esatto del
        // membro enum C# lato server (es. "Buy", "ExternalBroker", "Filled"), perche il server
        // serializza gli enum come stringa via JsonStringEnumConverter senza policy camelCase.

        private sealed class OpenPlanSessionRequestDto
        {
            public string PlanCode { get; set; } = "";
            public string ClientRunMode { get; set; } = "Unknown";
            public string ExecutionKey { get; set; } = "";
            public string AccountNumber { get; set; }

            /// <summary>
            /// False = niente gruppi sulla sessione, quindi POST /bars restituisce intent gia
            /// assegnati invece di template da reclamare.
            /// </summary>
            public bool DistributeToAccounts { get; set; }
        }

        private sealed class SessionDescriptorDto
        {
            public string SessionId { get; set; } = "";
            public string SessionToken { get; set; } = "";
            public string WorkspaceId { get; set; } = "";
            public string PlanCode { get; set; }
            public string ExecutionKey { get; set; }
            public string ExecutionMode { get; set; } = "";
            public string Status { get; set; } = "";
            public string TitanoRunId { get; set; }
            public string TitanoMode { get; set; }
            public string ClientRunMode { get; set; }

            /// <summary>Coppie (simbolo, timeframe) derivate dal masterfilter del workspace del piano.</summary>
            public List<TradingInstrumentDto> Instruments { get; set; } = new();
        }

        private sealed class TradingInstrumentDto
        {
            public string Symbol { get; set; } = "";

            /// <summary>Nome dello strumento sull'account: e' quello del grafico.</summary>
            public string AccountSymbol { get; set; } = "";

            public List<int> TimeframesMinutes { get; set; } = new();
        }

        private sealed class OhlcvDto
        {
            public DateTime DateTime { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
        }

        private sealed class ClosedBarDto
        {
            public string Symbol { get; set; } = "";
            public int TimeframeMinutes { get; set; }
            public DateTime BarTimeUtc { get; set; }
            public long Sequence { get; set; }
            public string IdempotencyKey { get; set; } = "";
            public OhlcvDto Bar { get; set; } = new();
        }

        private sealed class PushBarsRequestDto
        {
            public string SessionId { get; set; } = "";
            public string SessionToken { get; set; } = "";
            public List<ClosedBarDto> Bars { get; set; } = new();
        }

        private sealed class PushBarsResponseDto
        {
            public int AcceptedBars { get; set; }
            public int DuplicateBars { get; set; }
            public List<OrderIntentDto> Intents { get; set; } = new();
        }

        private sealed class OrderIntentDto
        {
            public string IntentId { get; set; } = "";
            public string SessionId { get; set; } = "";
            public string StrategyCode { get; set; } = "";
            public string StrategyName { get; set; } = "";
            public string Symbol { get; set; } = "";
            public DateTime CreatedAtUtc { get; set; }
            public string Side { get; set; } = "";
            public string OrderType { get; set; } = "";
            public decimal Quantity { get; set; }
            public decimal FinalQuantity { get; set; }

            /// <summary>Simbolo del broker risolto dalla tabella di conversione dell'account.</summary>
            public string AccountSymbol { get; set; } = "";

            /// <summary>
            /// Contratti broker per contratto Piootoo, GIA' applicato a FinalQuantity. Serve solo a
            /// riportare l'utile aperto nella grandezza in cui il server dichiara le soglie.
            /// </summary>
            public decimal ContractMultiplier { get; set; } = 1m;

            public decimal Price { get; set; }
            /// <summary>"Entry" oppure "Close"; Close può essere emesso per un segnale ExitOnly.</summary>
            public string Kind { get; set; } = "Entry";
            public bool IsClose { get; set; }

            /// <summary>
            /// "Pending", "Accepted", "PartiallyFilled", "Filled", "Rejected", "Cancelled". Il
            /// server consegna anche intent NON eseguibili (sizing che ha azzerato la quantita,
            /// allocazione Titano nulla, limite di fill per sessione raggiunto): vanno ignorati.
            /// </summary>
            public string Status { get; set; } = "Pending";

            // Specifica di uscita completa: e' l'unica informazione con cui il bot chiude la posizione.
            public decimal? StopLoss { get; set; }
            public decimal? TakeProfit { get; set; }

            /// <summary>Movimento favorevole, in punti, dopo il quale lo stop va spostato all'ingresso.</summary>
            public decimal? BreakEven { get; set; }

            /// <summary>Distanza in punti dal picco favorevole da mantenere come trailing stop.</summary>
            public decimal? TrailingStop { get; set; }

            /// <summary>Timeframe della strategia: le barre di MaxBarsInPosition sono le sue.</summary>
            public int TimeframeMinutes { get; set; }

            public int? MaxBarsInPosition { get; set; }

            /// <summary>Diagnostica: il limite e' applicato dal server, non dal bot.</summary>
            public int? MaxEntriesPerSession { get; set; }
            public DateTime? EntrySessionStartUtc { get; set; }

            public DateTime? ValidFromUtc { get; set; }

            /// <summary>
            /// Ultimo istante di validita dell'ordine. I motori Unger emettono ordini "next bar"
            /// che vivono una barra sola: alla barra dopo l'ordine pending va cancellato.
            /// </summary>
            public DateTime? ExpiresAtUtc { get; set; }
            public DateTime? CloseAtUtc { get; set; }

            /// <summary>Soglia di utile per contratto sotto la quale eseguire la chiusura a tempo.</summary>
            public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; set; }

            /// <summary>Istante da cui sorvegliare lo stallo dell'utile aperto.</summary>
            public DateTime? ProfitStallAfterUtc { get; set; }
            public string Reason { get; set; }
        }

        private sealed class ExternalExecutionReportDto
        {
            public string ReportId { get; set; } = "";
            public string IntentId { get; set; } = "";
            public string ExternalOrderId { get; set; }
            public string Status { get; set; } = "Filled";
            public decimal CumulativeFilledQuantity { get; set; }
            public decimal? FillPrice { get; set; }
            public decimal Commission { get; set; }
            public DateTime EventTimeUtc { get; set; }
        }

        private sealed class ExecutionReportRequestDto
        {
            public string SessionToken { get; set; } = "";
            public ExternalExecutionReportDto Report { get; set; } = new();
        }

        private sealed class CreateExternalCloseIntentRequestDto
        {
            public string SessionToken { get; set; } = "";
            public string StrategyCode { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string AccountNumber { get; set; }
            public decimal Quantity { get; set; }
            public string Reason { get; set; }
        }

        private sealed class AccountGroupMappingDto
        {
            public string AccountNumber { get; set; } = "";
            public string GroupId { get; set; } = "";
        }

        private sealed class SessionStateFileDto
        {
            /// <summary>Sessione a cui appartiene l'ancora: se non coincide, l'ancora si ricalcola.</summary>
            public string SessionId { get; set; } = "";
            public double InitialEquity { get; set; }
            public double PeakEquity { get; set; }
        }

        private sealed class SessionSnapshotDto
        {
            public string SessionId { get; set; } = "";
            public string ExecutionMode { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal Balance { get; set; }
            public decimal Equity { get; set; }
            public int Entries { get; set; }
            public int Fills { get; set; }
        }
    }
}
