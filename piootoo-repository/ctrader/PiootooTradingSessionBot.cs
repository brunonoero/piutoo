using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using cAlgo.API;

namespace cAlgo.Robots
{
    // ------------------------------------------------------------------------------------------
    // PiootooTradingSessionBot
    //
    // cBot "live" per la Trading sessions API v1 di Piootoo (vedi docs/domini/trading-sessions-api.md).
    // A differenza di PiootooSignalReplayBot (che rilegge segnali da file), questo cBot dialoga
    // direttamente con PiootooApp.Server:
    //
    //   1) OnStart  -> crea (o riprende) una trading session in modalita ExternalBroker,
    //                  inviando l'account number come ClientSessionToken e il simbolo/le
    //                  metadata dello strumento del grafico corrente, poi la avvia (/start).
    //   2) OnBar    -> ad ogni barra chiusa invia SOLO quella barra (account/simbolo gia legati
    //                  alla sessione) a POST /{sessionId}/bars e riceve gli OrderIntent generati
    //                  dal server (segnali di ingresso/uscita, incluse le uscite "pattern based"
    //                  decise interamente lato server).
    //   3) Ogni OrderIntent viene eseguito cosi com'e: apertura a mercato/pending oppure chiusura
    //      (CloseOnly). Il cBot NON tenta di replicare pattern di uscita: quella logica resta sul
    //      server (StrategyEvaluationService). Il cBot gestisce in locale solo le condizioni
    //      "meccaniche": Stop Loss e Take Profit (applicati come livelli nativi cTrader presi
    //      dall'intent) e un numero massimo di barre in posizione (parametro locale).
    //   4) Ogni fill (apertura o chiusura) viene riportato al server via
    //      POST /{sessionId}/execution-reports. Le chiusure guidate da un intent CloseOnly
    //      generano un PersistedTrade lato server (GET /{sessionId}/trades), che e il meccanismo
    //      con cui la sessione "invia" la lista dei trade usata per le rotazioni Titano
    //      (vedi docs/domini/titano-rotation.md, "In ExternalBroker un trade nasce esclusivamente
    //      dai fill di chiusura autorevoli").
    //
    // Nota: le uscite decise SOLO in locale (Stop Loss/Take Profit nativi del broker, oppure il limite
    // di barre) non hanno un OrderIntent CloseOnly del server a cui agganciare direttamente un execution
    // report (il server rifiuterebbe un report che referenzia un intent con CloseOnly=false, trattandolo
    // come aggiornamento di apertura anziche di chiusura). Per questi casi il cBot chiama prima
    // POST {sessionId}/intents/close-external (vedi RegisterExternalCloseAndReport), che registra lato
    // server un intent CloseOnly "client-originated" per la posizione aperta corrispondente, e poi
    // riporta il fill contro quell'intent con il normale POST {sessionId}/execution-reports. Cosi anche
    // queste chiusure confluiscono in trades.json e alimentano le rotazioni Titano.
    //
    //   5) Riavvio: il cBot non tiene lo storico dei segnali su file. All'avvio salva/ricarica solo
    //      SessionId+SessionToken da un piccolo file locale (uno per account/simbolo/timeframe), cosi
    //      un riavvio riprende automaticamente la STESSA sessione lato server senza doverla reimpostare
    //      a mano nei parametri. Una volta nota la sessione, il cBot chiede al controller
    //      (GET {sessionId}/intents) i signal gia emessi e li riconcilia con le Position/PendingOrder
    //      ancora aperte in cTrader per ricostruire in memoria le condizioni di uscita (entry bar per il
    //      limite barre; l'intent di apertura per gli ordini pending non ancora eseguiti). Stop
    //      Loss/Take Profit non vanno ricostruiti: restano attivi come ordini nativi sulla posizione
    //      anche se il cBot viene riavviato.
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
        private const string BotVersion = "1.1.0"; // aggiornare qui ad ogni release
        private const string ChartInfoObjectName = "PiootooTradingSessionBot_InfoPanel";

        // ---------------------------------------------------------------- Connessione / sessione

        [Parameter("API Base Url", DefaultValue = "http://localhost:5142", Group = "Connessione")]
        public string ApiBaseUrl { get; set; }

        [Parameter("Workspace Id", DefaultValue = "", Group = "Connessione")]
        public string WorkspaceId { get; set; }

        [Parameter("Execution Mode", DefaultValue = SessionExecutionModeParam.ExternalBroker, Group = "Connessione")]
        public SessionExecutionModeParam SessionMode { get; set; }

        [Parameter("Account Number Override", DefaultValue = 0, Group = "Connessione")]
        public int AccountNumberOverride { get; set; }

        [Parameter("Existing Session Id", DefaultValue = "", Group = "Connessione")]
        public string ExistingSessionId { get; set; }

        [Parameter("Existing Session Token", DefaultValue = "", Group = "Connessione")]
        public string ExistingSessionToken { get; set; }

        [Parameter("Titano Run Id", DefaultValue = "", Group = "Connessione")]
        public string TitanoRunId { get; set; }

        [Parameter("Titano Backtest Folder", DefaultValue = "", Group = "Connessione")]
        public string TitanoBacktestFolder { get; set; }

        [Parameter("Http Timeout (s)", DefaultValue = 15, MinValue = 1, Group = "Connessione")]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("Persist Session To File", DefaultValue = true, Group = "Connessione")]
        public bool PersistSessionToFile { get; set; }

        [Parameter("Force New Session", DefaultValue = false, Group = "Connessione")]
        public bool ForceNewSession { get; set; }

        // -------------------------------------------------------------------------------- Sizing

        [Parameter("Initial Capital", DefaultValue = 100000, Group = "Sizing")]
        public double InitialCapital { get; set; }

        [Parameter("Commission Per Contract", DefaultValue = 2, Group = "Sizing")]
        public double CommissionPerContract { get; set; }

        [Parameter("Dollars Per Point", DefaultValue = 1, Group = "Sizing")]
        public double DollarsPerPoint { get; set; }

        [Parameter("Minimum Quantity", DefaultValue = 0.01, Group = "Sizing")]
        public double MinimumQuantity { get; set; }

        [Parameter("Quantity Step", DefaultValue = 0.01, Group = "Sizing")]
        public double QuantityStep { get; set; }

        [Parameter("Quantity Rounding Mode", DefaultValue = QuantityRoundingModeParam.BrokerVolumeStep, Group = "Sizing")]
        public QuantityRoundingModeParam RoundingModeParam { get; set; }

        [Parameter("Volume Per Quantity Unit", DefaultValue = 1.0, MinValue = 0.0001, Group = "Sizing")]
        public double VolumePerQuantityUnit { get; set; }

        // --------------------------------------------------------------- Uscite gestite in locale

        [Parameter("Use Intent Stop Loss", DefaultValue = true, Group = "Uscite locali")]
        public bool UseIntentStopLoss { get; set; }

        [Parameter("Use Intent Take Profit", DefaultValue = true, Group = "Uscite locali")]
        public bool UseIntentTakeProfit { get; set; }

        [Parameter("Max Bars In Position (0 = disattivo)", DefaultValue = 0, MinValue = 0, Group = "Uscite locali")]
        public int MaxBarsInPosition { get; set; }

        // ------------------------------------------------------------------------------- Stato

        private HttpClient _http;
        private JsonSerializerOptions _json;
        private string _sessionId;
        private string _sessionToken;
        private int _timeframeMinutes;
        private long _accountNumber;
        private bool _sessionReady;
        private double _initialEquity;
        private double _peakEquity;

        // Traccia, per label, l'ultimo intent di apertura inviato (serve a risolvere il fill
        // quando la posizione nasce in modo asincrono, es. ordine pending Stop/Limit).
        private readonly Dictionary<string, OrderIntentDto> _lastOpenIntentByLabel = new();

        // Traccia l'intent (di apertura) che ha originato ciascuna posizione, per calcolare le
        // barre trascorse (uscita locale "numero di barre").
        private readonly Dictionary<long, OrderIntentDto> _positionIntent = new();
        private readonly Dictionary<long, int> _positionEntryBar = new();

        // Traccia l'intent CloseOnly (se presente) che ha guidato la chiusura di una posizione,
        // cosi OnPositionClosed sa se puo generare un execution report valido lato server.
        private readonly Dictionary<long, OrderIntentDto> _closingIntentByPosition = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(WorkspaceId))
            {
                Print("Workspace Id non impostato.");
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

            try
            {
                string resumeSessionId = null;
                string resumeSessionToken = null;
                SessionStateFileDto savedState = null;

                if (!ForceNewSession)
                {
                    if (!string.IsNullOrWhiteSpace(ExistingSessionId) && !string.IsNullOrWhiteSpace(ExistingSessionToken))
                    {
                        resumeSessionId = ExistingSessionId.Trim();
                        resumeSessionToken = ExistingSessionToken.Trim();
                    }
                    else if (PersistSessionToFile)
                    {
                        savedState = LoadSessionState();
                        if (savedState != null && !string.IsNullOrWhiteSpace(savedState.SessionId) && !string.IsNullOrWhiteSpace(savedState.SessionToken))
                        {
                            resumeSessionId = savedState.SessionId;
                            resumeSessionToken = savedState.SessionToken;
                            Print("Trovato stato sessione salvato su file: {0}.", resumeSessionId);
                        }
                    }
                }

                var resumed = false;
                if (resumeSessionId != null)
                {
                    _sessionId = resumeSessionId;
                    _sessionToken = resumeSessionToken;
                    try
                    {
                        StartSession();
                        resumed = true;
                        Print("Sessione {0} ripresa per account {1}.", _sessionId, _accountNumber);
                    }
                    catch (Exception ex)
                    {
                        Print("Impossibile riprendere la sessione salvata {0} ({1}): ne creo una nuova.", resumeSessionId, ex.Message);
                        _sessionId = null;
                        _sessionToken = null;
                        savedState = null;
                    }
                }

                if (!resumed)
                {
                    CreateSession();
                    StartSession();
                }

                // Ancora per il pannello profit/drawdown a chart: se la sessione e stata ripresa da uno
                // stato salvato con equity iniziale nota, la riusiamo cosi il pannello non si azzera ad
                // ogni riavvio del cBot; per una sessione nuova l'equity attuale e il nuovo punto zero.
                _initialEquity = resumed && savedState != null ? savedState.InitialEquity : Account.Equity;
                _peakEquity = resumed && savedState != null ? Math.Max(savedState.PeakEquity, Account.Equity) : Account.Equity;

                SaveSessionState();
                _sessionReady = true;
                Print("Sessione {0} attiva su {1} ({2} min), account {3}, workspace {4}.",
                    _sessionId, SymbolName, _timeframeMinutes, _accountNumber, WorkspaceId);

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
            if (_sessionReady)
                UpdateChartDisplay();
        }

        protected override void OnBar()
        {
            if (!_sessionReady)
                return;

            try
            {
                PushClosedBar();
                CloseExpiredPositions();
            }
            catch (Exception ex)
            {
                Print("Errore elaborazione barra: {0}", ex.Message);
            }
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
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
            ReportOpeningFill(intent, position);
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (!IsOurs(position))
                return;

            if (_closingIntentByPosition.TryGetValue(position.Id, out var closingIntent))
            {
                ReportClosingFill(closingIntent, position);
            }
            else
            {
                // Chiusura decisa solo in locale (SL/TP nativo o limite barre): nessun intent CloseOnly dal
                // server. Registriamo prima un intent CloseOnly "client-originated" (POST
                // {id}/intents/close-external) e poi lo referenziamo nel report, cosi la chiusura confluisce
                // comunque in trades.json/Titano.
                RegisterExternalCloseAndReport(position, args.Reason);
            }

            _positionIntent.Remove(position.Id);
            _positionEntryBar.Remove(position.Id);
            _closingIntentByPosition.Remove(position.Id);
        }

        protected override void OnStop()
        {
            if (!_sessionReady || _http == null)
                return;

            try
            {
                SendJson<SessionDescriptorDto>(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/stop");
            }
            catch (Exception ex)
            {
                Print("Impossibile fermare la sessione lato server: {0}", ex.Message);
            }
        }

        // ------------------------------------------------------------------ Stato sessione su file

        // Non persistiamo lo storico dei segnali: la sessione lato server (GET {sessionId}/intents)
        // resta l'unica fonte di verita per i signal. Il file locale serve solo a ricordare QUALE
        // sessione riprendere dopo un riavvio del cBot (SessionId+SessionToken), evitando di doverli
        // reimpostare a mano nei parametri "Existing Session Id/Token".
        private string ResolveSessionStateFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooTradingSessionBot");
            Directory.CreateDirectory(folder);
            var safeSymbol = NormalizeSymbol(SymbolName).Replace('/', '_').Replace('\\', '_');
            return Path.Combine(folder, $"session-{_accountNumber}-{safeSymbol}-{_timeframeMinutes}.json");
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
            if (!PersistSessionToFile)
                return;

            try
            {
                var path = ResolveSessionStateFilePath();
                var json = JsonSerializer.Serialize(
                    new SessionStateFileDto
                    {
                        SessionId = _sessionId,
                        SessionToken = _sessionToken,
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
                .Where(i => !i.CloseOnly &&
                            string.Equals(i.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase) &&
                            NormalizeSymbol(i.Symbol) == NormalizeSymbol(SymbolName))
                .OrderByDescending(i => i.CreatedAtUtc)
                .FirstOrDefault();

        private int ResolveBarIndexForTime(DateTime timeUtc)
        {
            for (var i = Bars.Count - 1; i >= 0; i--)
            {
                if (Bars.OpenTimes[i] <= timeUtc)
                    return i;
            }

            return 0;
        }

        // ------------------------------------------------------------------------ Sessione HTTP

        private void CreateSession()
        {
            var request = new CreateSessionRequestDto
            {
                WorkspaceId = WorkspaceId.Trim(),
                ExecutionMode = SessionMode.ToString(),
                InitialCapital = (decimal)InitialCapital,
                CommissionPerContract = (decimal)CommissionPerContract,
                ClientSessionToken = $"cTrader-{_accountNumber}",
                TitanoRunId = string.IsNullOrWhiteSpace(TitanoRunId) ? null : TitanoRunId.Trim(),
                TitanoBacktestFolder = string.IsNullOrWhiteSpace(TitanoBacktestFolder) ? null : TitanoBacktestFolder.Trim(),
                Instruments = new List<InstrumentMetadataDto>
                {
                    new InstrumentMetadataDto
                    {
                        Symbol = SymbolName,
                        DollarsPerPoint = (decimal)DollarsPerPoint,
                        MinimumQuantity = (decimal)MinimumQuantity,
                        QuantityStep = (decimal)QuantityStep,
                        RoundingMode = RoundingModeParam.ToString()
                    }
                }
            };

            var descriptor = SendJson<SessionDescriptorDto>(HttpMethod.Post, "api/v1/trading-sessions", request, includeToken: false);
            _sessionId = descriptor.SessionId;
            _sessionToken = descriptor.SessionToken;
            Print("Sessione creata: {0} (account {1}, workspace {2}).", _sessionId, _accountNumber, WorkspaceId);
        }

        private void StartSession()
        {
            SendJson<SessionDescriptorDto>(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/start");
        }

        private void PushClosedBar()
        {
            var closedBar = Bars.Last(1);
            var openTimeUtc = closedBar.OpenTime;

            var payload = new PushBarsRequestDto
            {
                SessionId = _sessionId,
                SessionToken = _sessionToken,
                Bars = new List<ClosedBarDto>
                {
                    new ClosedBarDto
                    {
                        Symbol = SymbolName,
                        TimeframeMinutes = _timeframeMinutes,
                        BarTimeUtc = openTimeUtc,
                        Sequence = new DateTimeOffset(openTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
                        IdempotencyKey = $"{SymbolName}:{_timeframeMinutes}:{openTimeUtc:O}",
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
                if (NormalizeSymbol(intent.Symbol) != NormalizeSymbol(SymbolName))
                    continue;

                ApplyIntent(intent);
            }
        }

        // -------------------------------------------------------------------------- Gestione segnali

        private void ApplyIntent(OrderIntentDto intent)
        {
            if (string.Equals(intent.Side, "Hold", StringComparison.OrdinalIgnoreCase) && !intent.CloseOnly)
                return;

            var label = MakeLabel(intent.StrategyCode);

            if (intent.CloseOnly)
            {
                ApplyCloseIntent(intent, label);
                return;
            }

            ApplyOpenIntent(intent, label);
        }

        private void ApplyCloseIntent(OrderIntentDto intent, string label)
        {
            var matches = Positions.FindAll(label, SymbolName);
            if (matches.Length == 0)
            {
                Print("Intent di chiusura {0} ({1}) ricevuto ma nessuna posizione aperta trovata.", intent.IntentId, label);
                return;
            }

            foreach (var position in matches)
            {
                _closingIntentByPosition[position.Id] = intent;
                var result = ClosePosition(position);
                if (!result.IsSuccessful)
                {
                    Print("Errore chiusura posizione {0} da intent {1}: {2}", position.Id, intent.IntentId, result.Error);
                    _closingIntentByPosition.Remove(position.Id);
                }
            }
        }

        private void ApplyOpenIntent(OrderIntentDto intent, string label)
        {
            var tradeType = string.Equals(intent.Side, "Sell", StringComparison.OrdinalIgnoreCase) ? TradeType.Sell : TradeType.Buy;

            if (Positions.Find(label, SymbolName, tradeType) != null)
            {
                Print("Intent {0} ({1} {2}) ignorato: posizione gia aperta su questa label.", intent.IntentId, tradeType, label);
                return;
            }

            var rawQuantity = intent.FinalQuantity > 0 ? intent.FinalQuantity : intent.Quantity;
            if (rawQuantity <= 0)
            {
                Print("Intent {0} scartato: quantita non valida ({1}).", intent.IntentId, rawQuantity);
                return;
            }

            var rawVolume = (double)rawQuantity * VolumePerQuantityUnit;
            var volume = Symbol.NormalizeVolumeInUnits(rawVolume, RoundingMode.Down);
            if (volume <= 0)
            {
                Print("Intent {0} scartato: volume normalizzato non valido.", intent.IntentId);
                return;
            }

            double? stopLossPips = UseIntentStopLoss ? ToPips(intent.StopLoss) : null;
            double? takeProfitPips = UseIntentTakeProfit ? ToPips(intent.TakeProfit) : null;

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

            // Se il risultato porta gia una Position (ordine a mercato), il fill effettivo viene
            // comunque riportato da OnPositionOpened per avere un solo punto di reporting ed
            // evitare doppi execution-report verso il server.
        }

        private void CloseExpiredPositions()
        {
            if (MaxBarsInPosition <= 0)
                return;

            foreach (var position in Positions
                .Where(p => p.SymbolName == SymbolName && p.Label != null && p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
            {
                if (!_positionEntryBar.TryGetValue(position.Id, out var entryBar))
                    continue;

                if (Bars.Count - entryBar < MaxBarsInPosition)
                    continue;

                // Chiusura puramente locale (limite barre): nessun intent CloseOnly dal server.
                // OnPositionClosed la rilevera e registrera un intent client-originated tramite
                // RegisterExternalCloseAndReport (vedi commento in testa al file).
                var result = ClosePosition(position);
                if (!result.IsSuccessful)
                    Print("Errore chiusura per limite barre su posizione {0}: {1}", position.Id, result.Error);
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
                    Symbol = SymbolName,
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

        // ------------------------------------------------------------------------------- Enum UI

        public enum SessionExecutionModeParam
        {
            ServerSimulated,
            ExternalBroker
        }

        public enum QuantityRoundingModeParam
        {
            FuturesContracts,
            BrokerVolumeStep
        }

        // ----------------------------------------------------------------------- DTO contratto API
        // Copie locali (POCO) dei contratti definiti in Piootoo.Shared.Models.Trading, cosi il cBot
        // resta un singolo file senza riferimenti al progetto server. I campi enum-like (Side,
        // OrderType, ExecutionMode, Status, ...) sono trattati come string con il nome esatto del
        // membro enum C# lato server (es. "Buy", "ExternalBroker", "Filled"), perche il server
        // serializza gli enum come stringa via JsonStringEnumConverter senza policy camelCase.

        private sealed class CreateSessionRequestDto
        {
            public string WorkspaceId { get; set; } = "";
            public string ExecutionMode { get; set; } = "ExternalBroker";
            public decimal InitialCapital { get; set; } = 100000m;
            public decimal CommissionPerContract { get; set; } = 2m;
            public string ClientSessionToken { get; set; }
            public string TitanoRunId { get; set; }
            public string TitanoBacktestFolder { get; set; }
            public List<InstrumentMetadataDto> Instruments { get; set; } = new();
        }

        private sealed class InstrumentMetadataDto
        {
            public string Symbol { get; set; } = "";
            public decimal DollarsPerPoint { get; set; } = 1m;
            public decimal MinimumQuantity { get; set; } = 1m;
            public decimal QuantityStep { get; set; } = 1m;
            public string RoundingMode { get; set; } = "BrokerVolumeStep";
        }

        private sealed class SessionDescriptorDto
        {
            public string SessionId { get; set; } = "";
            public string SessionToken { get; set; } = "";
            public string WorkspaceId { get; set; } = "";
            public string ExecutionMode { get; set; } = "";
            public string Status { get; set; } = "";
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
            public decimal Price { get; set; }
            public bool CloseOnly { get; set; }
            public decimal? StopLoss { get; set; }
            public decimal? TakeProfit { get; set; }
            public DateTime? ExpiresAtUtc { get; set; }
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

        private sealed class SessionStateFileDto
        {
            public string SessionId { get; set; } = "";
            public string SessionToken { get; set; } = "";
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
