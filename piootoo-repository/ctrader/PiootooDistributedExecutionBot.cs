using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using cAlgo.API;
using File = System.IO.File;
using HttpMethod = System.Net.Http.HttpMethod;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot "live" collegato al trading system Piootoo (api/v1/trading-sessions):
    ///  - il simbolo e il timeframe del grafico a cui è agganciato il bot sono IRRILEVANTI: gli stream
    ///    operativi sono le coppie (simbolo, timeframe) che arrivano dal piano del server. Per ognuna il
    ///    bot apre la serie nativa cTrader, ne carica la storia all'indietro con <c>LoadMoreHistory</c> e
    ///    si sottoscrive al suo <c>Bars.BarOpened</c>: alla chiusura di una barra di QUEL flusso invia al
    ///    server una finestra di candele e reclama subito l'eventuale segnale. Non aggrega mai barre
    ///    artificialmente, e non usa la barra del grafico come orologio comune;
    ///  - le candele viaggiano in due tempi. All'avvio, una volta per stream, parte il RISCALDAMENTO:
    ///    tutta la storia che le strategie richiedono (<c>RequiredCandles</c>, dichiarate dal
    ///    descriptor; per una strategia a 15 minuti sono 576 barre) con l'ordine di non valutare nulla,
    ///    perché sono barre già passate e valutarle produrrebbe intent sul passato. Poi, a ogni barra
    ///    chiusa, una finestra corta (<c>IncrementalWindowBars</c>, default 20) di cui il server valuta
    ///    l'ultima candela. Serve perché il server, in ExternalBroker, non ha un datafeed proprio: la
    ///    storia di uno stream è solo ciò che gli è stato spinto, quindi ogni giro perso lascerebbe un
    ///    buco permanente. Il server accoda solo le candele che non ha, quindi la sovrapposizione non
    ///    duplica niente e ricuce da sola fino a 19 barre consecutive perse; oltre, rifiuta la finestra
    ///    invece di accodare una serie bucata;
    ///  - fa polling periodico chiedendo al server "qual è il prossimo segnale per il MIO account";
    ///  - apre e chiude posizioni su QUALSIASI simbolo configurato (non solo quello del grafico);
    ///  - ogni posizione e ogni ordine porta una label <c>PiootooLive:{StrategyCode}:{IntentId}</c>: dal
    ///    solo stato della piattaforma si risale sempre al segnale che li ha creati, anche dopo un
    ///    riavvio del cBot e senza consultare lo stato locale;
    ///  - si autolimita PER SIMBOLO: mentre ha una posizione aperta su un simbolo non ne chiede/accetta una
    ///    seconda sullo stesso simbolo, ma può gestire in parallelo posizioni su simboli diversi;
    ///  - ogni intent di ingresso porta con sé la specifica di uscita completa e il cBot la applica per
    ///    intero: Stop Loss/Take Profit come livelli nativi sull'ordine (li applica il broker), BreakEven
    ///    e TrailingStop come modifiche dello stop nativo sorvegliate a ogni tick, CloseAtUtc (con
    ///    l'eventuale condizione ProfitBelow), ProfitStallAfterUtc e MaxBarsInPosition sorvegliati a ogni
    ///    OnBar. Gli ordini di ingresso possono essere Market, Stop o Limit (semantica "next bar" dei
    ///    motori Unger: l'ordine pending scade alla barra successiva ed è ricancellato/riemesso a ogni
    ///    signal). Il server NON invia mai segnali di chiusura come intent separati sganciati da una
    ///    strategia ExitOnly: le strategie che deciderebbero l'uscita a runtime sono escluse dal catalogo;
    ///  - qualunque sia la causa della chiusura (Stop Loss/Take Profit del broker, scadenza CloseAtUtc,
    ///    limite di barre) l'evento Positions.Closed la intercetta sempre: il bot registra un intent
    ///    di chiusura (POST intents/close-external) e vi riporta contro l'esito reale del trade
    ///    (prezzo di chiusura, quantità, commissioni) via execution-report, così i dati confluiscono
    ///    in trades.json e alimentano le rotazioni Titano;
    ///  - il server garantisce che, all'interno dello stesso gruppo (es. stessa prop firm), lo stesso
    ///    segnale non venga mai distribuito a due account diversi (anti copy-trading). Account di gruppi
    ///    diversi possono ricevere lo stesso segnale, ciascuno in modo indipendente.
    ///
    /// NOTA SUL BACKTESTING: durante il backtest cAlgo esegue tutto su un unico thread deterministico e
    /// non tollera che l'API del robot (posizioni, ordini) venga toccata da un thread diverso da quello
    /// dell'algoritmo: per questo tutte le chiamate HTTP qui sotto sono SINCRONE (HttpClient.Send, mai
    /// async/await o Task.Run) e con un timeout esplicito, così il bot funziona nello stesso modo sia in
    /// live sia in backtest. In backtest va abilitato il supporto multi-simbolo/multi-timeframe di
    /// cTrader: gli stream del piano sono per definizione diversi da quello del grafico, sul quale il bot
    /// non fa più alcun affidamento.
    ///
    /// Un'istanza di questo cBot rappresenta UN account cTrader. Il codice piano risolve la sessione e
    /// il profilo account/gruppo; cBot di account diversi possono condividere la sessione del piano.
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooDistributedExecutionBot : Robot
    {
        private const string LabelPrefix = "PiootooLive";
        private const char LabelSeparator = ':';

        /// <summary>
        /// Tetto ai giri di <c>LoadMoreHistory</c> per stream: il broker risponde a blocchi e senza un
        /// limite un simbolo con poca storia terrebbe l'avvio in un ciclo infinito.
        /// </summary>
        private const int MaxHistoryLoadAttempts = 50;

        /// <summary>
        /// Dopo tanti invii falliti di fila, senza uno riuscito in mezzo, il bot si ferma. Un errore
        /// di configurazione — piano che punta a una rotazione inesistente, sessione fermata, token
        /// scaduto — non si risolve da solo: continuare significa solo riempire il log della stessa
        /// riga per tutta la durata del backtest e accorgersene alla fine.
        /// </summary>
        private const int MaxConsecutivePushFailures = 20;

        private const string BotVersion = "2.0.0"; // aggiornare qui ad ogni release
        private const string StatusChartObjectName = "PiootooConnectionStatus";

        [Parameter("Server Base Url", DefaultValue = "https://localhost:5000")]
        public string ServerBaseUrl { get; set; }

        [Parameter("Codice piano")]
        public string PlanCode { get; set; }

        // A regime la storia del server è già completa: della finestra serve solo il margine che
        // ricuce le barre eventualmente perse (chiamata fallita, server irraggiungibile per qualche
        // giro). Venti barre coprono diciannove buchi consecutivi; oltre, il server rifiuta la
        // finestra invece di accodare una serie bucata, e lo si vede subito nel log.
        [Parameter("Barre per finestra a regime", DefaultValue = 20, MinValue = 2)]
        public int IncrementalWindowBars { get; set; }

        [Parameter("Polling segnali (secondi)", DefaultValue = 2, MinValue = 1)]
        public int PollingSeconds { get; set; }

        [Parameter("Max Entry Slippage (Pips)", DefaultValue = 5.0, MinValue = 0)]
        public double MaxEntrySlippagePips { get; set; }

        [Parameter("Http Timeout (secondi)", DefaultValue = 10, MinValue = 1)]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("History Window (giorni)", DefaultValue = 30, MinValue = 1)]
        public int HistoryWindowDays { get; set; }

        [Parameter("Log dettagliato", DefaultValue = false)]
        public bool VerboseLogging { get; set; }

        // Traccia su file, una riga JSON per risposta, tutto cio' che il server Piootoo restituisce
        // (apertura sessione, poll segnale, chiusura esterna): serve a diagnosticare da cliente senza
        // dover riprodurre il problema con VerboseLogging attivo, che stampa ma non persiste.
        [Parameter("Log JSON risposte server su file", DefaultValue = false, Group = "Diagnostica")]
        public bool LogServerResponses { get; set; }

        // Regola operativa: nel fine settimana non restano ne' posizioni ne' ordini. Vive nel bot, e
        // non lato server, perche' e' una regola di sicurezza e deve tenere anche quando il server e'
        // irraggiungibile.
        [Parameter("Flat nel fine settimana", DefaultValue = true, Group = "Fine settimana")]
        public bool FlatAtWeekEnd { get; set; }

        // Un'ora UTC di venerdi invece della chiusura CME reale (16:00 di Chicago): quest'ultima cade
        // alle 21:00 oppure alle 22:00 UTC secondo l'ora legale americana, quindi un default prudente
        // prima della piu' presta delle due vale in entrambi i periodi dell'anno senza gestire il fuso.
        [Parameter("Flat da venerdi (HHMM UTC)", DefaultValue = 2045, MinValue = 0, MaxValue = 2359, Group = "Fine settimana")]
        public int WeekEndFlatFromUtc { get; set; }

        [Parameter("Operativo da domenica (HHMM UTC)", DefaultValue = 2300, MinValue = 0, MaxValue = 2359, Group = "Fine settimana")]
        public int WeekEndFlatUntilUtc { get; set; }

        private HttpClient _http;
        private string _accountNumber;
        private string _sessionId;
        private string _sessionToken;
        private string _localStatePath;
        private string _jsonLogPath;
        // Stato di connessione mostrato a chart: riflette l'esito dell'ultima chiamata HTTP al
        // server Piootoo (open-plan, push barre, polling segnale), non solo l'apertura iniziale.
        private bool _isConnectedToServer;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Un flusso (simbolo Piootoo, timeframe) del piano. Il simbolo ha due nomi: <see cref="PiootooSymbol"/>
        /// è la chiave con cui il SERVER indicizza barre, strategie e posizioni; <see cref="AccountSymbol"/> è
        /// il nome sul BROKER, quello su cui si legge la serie e si piazzano gli ordini. Coincidono solo se
        /// l'account non converte i simboli.
        /// </summary>
        private sealed class Pair
        {
            public string PiootooSymbol;
            public string AccountSymbol;
            public int TimeframeMinutes;
            public Bars Series;
            public DateTime? LastPushedBarTimeUtc;

            /// <summary>
            /// true quando il server ha ricevuto la finestra di riscaldamento di questo stream. Finché
            /// è false le finestre a regime sarebbero troppo corte perché il server valuti, quindi il
            /// riscaldamento viene ritentato: all'avvio il server può essere ancora irraggiungibile.
            /// </summary>
            public bool WarmedUp;

            /// <summary>
            /// Candele che le strategie del server richiedono su questo stream (il massimo dei loro
            /// <c>RequiredCandles</c>, dal descriptor). È la profondità della finestra inviata a ogni
            /// barra e la quantità di storia da caricare dal broker prima di partire.
            /// </summary>
            public int RequiredCandles;

            /// <summary>Handler di <c>Series.BarOpened</c>, conservato per potersi disiscrivere in OnStop.</summary>
            public Action<BarOpenedEventArgs> BarHandler;

            public override string ToString() =>
                NormalizeSymbol(PiootooSymbol) == NormalizeSymbol(AccountSymbol)
                    ? $"{PiootooSymbol}/{TimeframeMinutes}m"
                    : $"{AccountSymbol} [{PiootooSymbol}]/{TimeframeMinutes}m";
        }

        /// <summary>
        /// Barra in cui è stato piazzato l'ordine pending di ciascuna label, per gli intent Stop/Limit che
        /// dichiarano una scadenza. Un ordine "next bar" vive una barra sola: alla successiva va cancellato,
        /// altrimenti resta a mercato e se ne accumula uno per barra.
        /// </summary>
        private sealed class PendingOrderMark
        {
            public Pair Stream;
            public int BarCount;
        }

        /// <summary>Contesto di una posizione aperta da questo bot, per il reporting alla chiusura.</summary>
        private sealed class OpenPositionContext
        {
            public int PositionId { get; set; }
            public string EntryIntentId { get; set; }
            public string StrategyCode { get; set; }
            public string Symbol { get; set; }
            /// <summary>Timeframe della strategia: le barre vengono contate solo su questo stream.</summary>
            public int TimeframeMinutes { get; set; }
            /// <summary>Soglia in punti per spostare lo stop al prezzo di ingresso.</summary>
            public decimal? BreakEven { get; set; }
            /// <summary>Distanza in punti dal massimo/minimo favorevole per il trailing stop.</summary>
            public decimal? TrailingStop { get; set; }
            public DateTime? CloseAtUtc { get; set; }
            /// <summary>Condiziona la chiusura a CloseAtUtc all'utile per contratto già raggiunto.</summary>
            public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; set; }
            /// <summary>Da questo istante si sorveglia l'utile aperto e si chiude se non fa nuovo massimo.</summary>
            public DateTime? ProfitStallAfterUtc { get; set; }
            /// <summary>Limite di barre in posizione dichiarato dall'intent di ingresso. 0 = nessun limite.</summary>
            public int MaxBarsInPosition { get; set; }
            /// <summary>Barre trascorse, persistite per non perdere il limite dopo un riavvio.</summary>
            public int BarsInPosition { get; set; }
            /// <summary>Rapporto contratto broker / contratto Piootoo, per convertire NetProfit in utile per contratto.</summary>
            public decimal ContractMultiplier { get; set; } = 1m;
        }

        private sealed class LocalSessionState
        {
            public string PlanCode { get; set; }
            public string AccountNumber { get; set; }
            public string SessionId { get; set; }
            public List<OpenPositionContext> Positions { get; set; } = new();
        }

        private readonly List<Pair> _pairs = new();

        // Sottoscrizioni tick per simbolo del piano: break-even e trailing devono reagire su TUTTI gli
        // strumenti, non solo su quello del grafico (l'OnTick del robot vede solo quello).
        private readonly Dictionary<Symbol, Action<SymbolTickEventArgs>> _tickHandlers = new();

        // Posizioni attualmente aperte da questo bot, per Id posizione cTrader.
        private readonly Dictionary<int, OpenPositionContext> _openPositions = new();

        // Intent già in gestione in questo avvio: evita di ri-eseguire ordini ad ogni poll finché il
        // server non registra l'esito (il poll è idempotente e ripropone lo stesso intent finché Pending).
        private readonly HashSet<string> _submittedIntentIds = new();
        private readonly Dictionary<int, OrderIntentDto> _serverCloseIntents = new();

        // Traccia, per label, l'ultimo intent di apertura inviato: serve a risolvere il fill quando la
        // posizione nasce in modo asincrono (ordine pending Stop/Limit), via Positions.Opened.
        private readonly Dictionary<string, OrderIntentDto> _lastOpenIntentByLabel = new();

        // Barra in cui è stato piazzato l'ordine pending di ciascuna label, per gli intent con scadenza.
        private readonly Dictionary<string, PendingOrderMark> _pendingOrderBar = new();

        // Stream per cui è già stato segnalato che il server non ha storia sufficiente: il messaggio
        // va detto una volta, non a ogni barra.
        private readonly HashSet<string> _insufficientHistoryReported = new(StringComparer.OrdinalIgnoreCase);

        // Ultimo errore di invio stampato, per stream: se si ripete identico non lo si ristampa.
        private readonly Dictionary<string, string> _lastPushError = new(StringComparer.OrdinalIgnoreCase);

        // Invii falliti di fila, azzerato dal primo che riesce. Vedi MaxConsecutivePushFailures.
        private int _consecutivePushFailures;

        // Ultimo motivo per cui il claim non ha restituito un intent: stampato una volta sola finché
        // non cambia, altrimenti riempirebbe il log a ogni poll.
        private string _lastPollReason;

        // Massimo utile per contratto osservato dopo ProfitStallAfterUtc, per posizione.
        private readonly Dictionary<int, decimal> _peakProfitAfterStall = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(PlanCode))
            {
                Print("Codice piano non impostato.");
                Stop();
                return;
            }

            _accountNumber = Account.Number.ToString();
            if (LogServerResponses && !IsBacktesting)
            {
                _jsonLogPath = BuildJsonLogPath(PlanCode, _accountNumber);
                Print("Log JSON risposte server: {0}", _jsonLogPath);
            }
            UpdateConnectionStatus(false); // visibile a chart fin dal primo istante, prima ancora di tentare la connessione

            Print("Connessione al server Piootoo: {0} (account={1}, piano='{2}')...",
                ServerBaseUrl, _accountNumber, PlanCode);

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(1, HttpTimeoutSeconds))
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Print("Apertura sessione per il piano '{0}'...", PlanCode);
            var openResponse = PostJson("api/v1/trading-sessions/open-plan", new OpenTradingPlanSessionRequestDto
            {
                PlanCode = PlanCode.Trim(),
                ClientRunMode = IsBacktesting ? "Backtest" : "Realtime",
                ExecutionKey = IsBacktesting ? $"BT-{Server.TimeInUtc:yyyyMMddHHmmss}" : "LIVE",
                AccountNumber = _accountNumber
            });
            if (!openResponse.IsSuccessStatusCode)
            {
                Print("Impossibile aprire il piano '{0}': {1}", PlanCode, ReadError(openResponse));
                UpdateConnectionStatus(false);
                Stop();
                return;
            }
            var openBody = ReadBody(openResponse);
            LogJsonResponse("open-plan", openBody);
            var descriptor = JsonSerializer.Deserialize<TradingSessionDescriptorDto>(openBody, _json);
            _sessionId = descriptor?.SessionId;
            _sessionToken = descriptor?.SessionToken;
            Print("Sessione aperta: SessionId={0}.", _sessionId);

            var pairs = new List<Pair>();
            var error = "descriptor sessione mancante";
            if (descriptor == null ||
                !BuildPairs(descriptor.Instruments, out pairs, out error))
            {
                Print("Configurazione strumenti del piano non valida: {0}", error);
                UpdateConnectionStatus(false);
                Stop();
                return;
            }
            _pairs.AddRange(pairs);
            Print("Strumenti configurati: {0}.", string.Join("; ", pairs));
            if (!IsBacktesting)
                _localStatePath = BuildLocalStatePath(PlanCode, _accountNumber);

            // Serie letta sul nome BROKER: sul nome Piootoo, quando l'account converte il simbolo, la
            // ricerca fallirebbe o restituirebbe lo strumento sbagliato. Il simbolo/timeframe del grafico
            // non entra qui: gli stream sono e restano quelli del piano.
            foreach (var pair in _pairs)
            {
                // Simbolo non abilitato sull'account: fallire subito e in modo esplicito, invece di
                // scoprirlo al primo intent quando il segnale è già perso.
                if (Symbols.GetSymbol(pair.AccountSymbol) is null)
                {
                    Print("Simbolo '{0}' non disponibile su questo account: stream {1} non avviabile.",
                        pair.AccountSymbol, pair);
                    UpdateConnectionStatus(false);
                    Stop();
                    return;
                }

                pair.Series = MarketData.GetBars(ToTimeFrame(pair.TimeframeMinutes), pair.AccountSymbol);
                if (pair.Series is null)
                {
                    Print("Serie {0} non disponibile su questo account.", pair);
                    UpdateConnectionStatus(false);
                    Stop();
                    return;
                }

                // Storia caricata all'indietro PRIMA di partire: cTrader tiene in serie solo le barre
                // che gli servono per il grafico, e senza questo la prima finestra spedita al server
                // sarebbe più corta di RequiredCandles. Il server non valuterebbe nulla e il run
                // resterebbe muto per le prime centinaia di barre.
                LoadHistoryBackwards(pair);

                // Riscaldamento: la storia richiesta parte subito, e parte senza far valutare nulla.
                // Le barre della finestra sono già passate: valutarle produrrebbe intent sul passato,
                // che il bot eseguirebbe al prezzo di adesso.
                SendWarmUpWindow(pair);

                // Una sottoscrizione per stream: la barra di ciascuna coppia (simbolo, timeframe) fa
                // scattare le chiamate al server per quella coppia, indipendentemente dalle altre.
                var stream = pair;
                stream.BarHandler = _ => OnStreamBarClosed(stream);
                stream.Series.BarOpened += stream.BarHandler;
            }

            SubscribeSymbolTicks();

            RestoreLocalState();
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, PollingSeconds)));

            UpdateConnectionStatus(true);
            Print("{0} v{1} avviato. Account={2} Session={3} Strumenti={4}",
                nameof(PiootooDistributedExecutionBot), BotVersion, _accountNumber, _sessionId,
                string.Join("; ", _pairs));
        }

        /// <summary>
        /// Riquadro statico in alto a destra sul chart con account, piano e stato della connessione
        /// al server Piootoo: è la prima cosa che deve poter vedere chi guarda il grafico, senza
        /// dover aprire i log. Va aggiornato ad ogni cambio di stato, non solo all'avvio, perché una
        /// chiamata HTTP fallita a runtime deve riflettersi subito sul chart.
        /// </summary>
        private void UpdateConnectionStatus(bool connected)
        {
            _isConnectedToServer = connected;
            var text = string.Format(
                "Piootoo\nAccount: {0}\nPiano: {1}\nConnesso: {2}",
                string.IsNullOrEmpty(_accountNumber) ? "-" : _accountNumber,
                string.IsNullOrWhiteSpace(PlanCode) ? "-" : PlanCode,
                connected ? "Si" : "No");
            Chart.DrawStaticText(StatusChartObjectName, text, VerticalAlignment.Top, HorizontalAlignment.Right,
                connected ? Color.LightGreen : Color.OrangeRed);
        }

        /// <summary>
        /// Una barra di UNO stream del piano si è chiusa (l'evento è <c>BarOpened</c> della sua serie:
        /// quando si apre la barra n, la n-1 è chiusa). Tutto il ciclo — cancellazione degli ordini
        /// scaduti, push della barra, conteggio barre in posizione, reclamo del segnale — è fatto per
        /// questo stream soltanto: gli altri hanno il loro orologio e scattano per conto proprio.
        /// </summary>
        private void OnStreamBarClosed(Pair stream)
        {
            // Prima si ritira l'ordine della barra appena chiusa: l'ordine "next bar" ha esaurito la sua
            // unica barra di validità, e riemettere il signal senza cancellarlo ne accumulerebbe uno per
            // barra a livelli diversi, tutti eseguibili.
            CancelExpiredPendingOrders(stream);

            var pushed = TryPushClosedBar(stream);
            if (pushed)
            {
                // Solo le posizioni che vivono su QUESTO stream avanzano di una barra: contarle sulla
                // barra di un altro timeframe falserebbe MaxBarsInPosition.
                var streamKey = MakeStreamKey(stream.PiootooSymbol, stream.TimeframeMinutes);
                foreach (var context in _openPositions.Values)
                    if (MakeStreamKey(context.Symbol, context.TimeframeMinutes) == streamKey)
                        context.BarsInPosition++;
            }

            // La barra è già stata pubblicata (la storia del server non deve avere buchi), ma
            // dentro la finestra di flat non si reclama nessun intent: sarebbe un ingresso che
            // HandleEntryIntent scarterebbe comunque, e il polling costa una chiamata.
            if (EnforceWeekEndFlat())
            {
                SaveLocalState();
                return;
            }

            // Il server ha appena valutato questo stream: reclamare subito l'eventuale intent, senza
            // aspettare il prossimo polling periodico.
            if (pushed)
                PollNextSignal();

            MoveStopsToBreakEven();
            MoveTrailingStops();
            CloseExpiredPositions();
            SaveLocalState();
        }

        /// <summary>
        /// Un tick su uno dei simboli del piano. Il prezzo può raggiungere e perdere la soglia dentro
        /// la stessa barra, quindi break-even e trailing vanno verificati a ogni tick e non al solo
        /// bar-close. La sottoscrizione è per simbolo perché <c>OnTick</c> del robot riporta i tick del
        /// solo simbolo del grafico, che qui non ha alcun ruolo.
        /// </summary>
        private void OnSymbolTick(SymbolTickEventArgs args)
        {
            // Dentro la finestra di fine settimana non c'e' nulla da proteggere: va solo chiuso.
            if (EnforceWeekEndFlat())
                return;

            MoveStopsToBreakEven();
            MoveTrailingStops();
        }

        /// <summary>
        /// Chiede al broker le barre precedenti finché la serie non copre <c>RequiredCandles</c> più la
        /// barra in formazione. <c>LoadMoreHistory</c> restituisce quante barre ha aggiunto e 0 quando
        /// non c'è altro da caricare: è la condizione di uscita insieme al numero di barre raggiunto.
        /// Il tetto sui giri serve solo a non bloccare l'avvio se il broker risponde a piccoli blocchi.
        /// </summary>
        private void LoadHistoryBackwards(Pair pair)
        {
            var target = pair.RequiredCandles + 1;
            var attempts = 0;
            while (pair.Series.Count < target && attempts++ < MaxHistoryLoadAttempts)
            {
                if (pair.Series.LoadMoreHistory() <= 0)
                    break;
            }

            if (pair.Series.Count < target)
                Print("Storia insufficiente per {0}: {1} barre su {2} richieste. " +
                      "Il server non valuterà le strategie di questo stream finché non ne accumula abbastanza.",
                    pair, pair.Series.Count, target);
            else if (VerboseLogging)
                Print("Storia {0}: {1} barre disponibili (finestra richiesta {2}).",
                    pair, pair.Series.Count, pair.RequiredCandles);
        }

        /// <summary>Una sottoscrizione per simbolo broker distinto, anche se serve più stream.</summary>
        private void SubscribeSymbolTicks()
        {
            foreach (var name in _pairs
                .Select(pair => pair.AccountSymbol)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var symbol = Symbols.GetSymbol(name);
                if (symbol is null || _tickHandlers.ContainsKey(symbol))
                    continue;

                Action<SymbolTickEventArgs> handler = OnSymbolTick;
                _tickHandlers[symbol] = handler;
                symbol.Tick += handler;
            }
        }

        /// <summary>
        /// Uscite che il broker non sa gestire nativamente, entrambe prese dal segnale di ingresso:
        /// scadenza a tempo (CloseAtUtc) e limite di barre (MaxBarsInPosition). Stop Loss e Take
        /// Profit non passano di qui: sono livelli nativi già applicati all'ordine.
        ///
        /// La chiusura effettiva viene poi riportata al server da OnPositionClosed, come qualunque
        /// altra chiusura.
        /// </summary>
        private void CloseExpiredPositions()
        {
            if (_openPositions.Count == 0)
                return;

            // Server.TimeInUtc e' UTC per definizione: non dipende dal TimeZone del [Robot] ne'
            // dall'impostazione di cTrader, a differenza di SpecifyKind su Server.Time che
            // etichetterebbe come UTC un orario locale se l'attributo cambiasse.
            var nowUtc = Server.TimeInUtc;
            foreach (var kvp in _openPositions.ToArray())
            {
                var ctx = kvp.Value;
                var position = Positions.FirstOrDefault(p => p.Id == kvp.Key);
                if (position is null)
                {
                    _openPositions.Remove(kvp.Key); // già chiusa, per qualche altra via
                    _peakProfitAfterStall.Remove(kvp.Key);
                    continue;
                }

                // Utile aperto per singolo contratto Piootoo, nella stessa grandezza in cui il server
                // dichiara le soglie: senza dividere per il volume a mercato (in contratti broker) e
                // riportarlo al contratto Piootoo con ContractMultiplier, la soglia scatterebbe a un
                // livello sbagliato di un fattore pari al moltiplicatore.
                var contractMultiplier = ctx.ContractMultiplier > 0 ? ctx.ContractMultiplier : 1m;
                var brokerVolume = (decimal)position.VolumeInUnits;
                var profitPerContract = brokerVolume > 0
                    ? (decimal)position.NetProfit / brokerVolume * contractMultiplier
                    : 0m;

                string reason = null;
                if (ctx.CloseAtUtc is { } closeAt && closeAt <= nowUtc)
                {
                    // La chiusura a tempo può essere condizionata all'utile: alcune strategie escono
                    // all'ora prevista solo se sono sotto, altre lasciano correre il vincente che ha
                    // già raggiunto la soglia.
                    if (!ctx.TimeExitOnlyIfProfitBelowMoneyPerContract.HasValue ||
                        profitPerContract < ctx.TimeExitOnlyIfProfitBelowMoneyPerContract.Value)
                    {
                        reason = "scadenza (CloseAtUtc)";
                    }
                }

                if (reason is null && ctx.MaxBarsInPosition > 0 && ctx.BarsInPosition >= ctx.MaxBarsInPosition)
                    reason = "limite barre (MaxBarsInPosition)";

                // Uscita per stallo dell'utile: dopo la deadline si tiene il massimo osservato e si
                // chiude alla prima barra che non lo supera. Il picco è memoria di esecuzione locale,
                // non parte dell'intent.
                if (reason is null && ctx.ProfitStallAfterUtc.HasValue && nowUtc >= ctx.ProfitStallAfterUtc.Value)
                {
                    if (!_peakProfitAfterStall.TryGetValue(kvp.Key, out var peak) || profitPerContract > peak)
                        _peakProfitAfterStall[kvp.Key] = profitPerContract;
                    else
                        reason = "stallo dell'utile (ProfitStallAfterUtc)";
                }

                if (reason is null)
                    continue;

                var result = ClosePosition(position);
                if (!result.IsSuccessful)
                    Print("Errore chiusura per {0} posizione {1}: {2}", reason, position.Id, result.Error);
            }
        }

        /// <summary>
        /// Quando il movimento favorevole raggiunge il break-even dell'intent,
        /// sposta lo stop nativo del broker al prezzo di ingresso. La distanza
        /// dell'intent è in unità di prezzo, non in pips.
        /// </summary>
        private void MoveStopsToBreakEven()
        {
            foreach (var context in _openPositions.Values.ToArray())
            {
                var position = Positions.FirstOrDefault(item => item.Id == context.PositionId);
                if (position is null)
                    continue;

                if (!context.BreakEven.HasValue || context.BreakEven.Value <= 0)
                    continue;

                var symbol = Symbols.GetSymbol(position.SymbolName);
                if (symbol is null)
                    continue;

                var threshold = (double)context.BreakEven.Value;
                var favorableMove = position.TradeType == TradeType.Buy
                    ? symbol.Bid - position.EntryPrice
                    : position.EntryPrice - symbol.Ask;
                if (favorableMove < threshold)
                    continue;

                var stopAlreadyAtEntry = position.TradeType == TradeType.Buy
                    ? position.StopLoss.HasValue && position.StopLoss.Value >= position.EntryPrice
                    : position.StopLoss.HasValue && position.StopLoss.Value <= position.EntryPrice;
                if (stopAlreadyAtEntry)
                    continue;

                var result = ModifyPosition(position, position.EntryPrice, position.TakeProfit);
                if (!result.IsSuccessful && VerboseLogging)
                    Print("Impossibile spostare a break-even {0}/{1}: {2}",
                        context.Symbol, context.StrategyCode, result.Error);
            }
        }

        /// <summary>
        /// Mantiene lo stop nativo del broker alla distanza dichiarata dal
        /// massimo/minimo favorevole corrente. Il livello viene aggiornato
        /// soltanto in direzione protettiva, quindi un ritracciamento non
        /// allarga mai lo stop già piazzato.
        /// </summary>
        private void MoveTrailingStops()
        {
            foreach (var context in _openPositions.Values.ToArray())
            {
                if (!context.TrailingStop.HasValue || context.TrailingStop.Value <= 0)
                    continue;

                var position = Positions.FirstOrDefault(item => item.Id == context.PositionId);
                if (position is null)
                    continue;

                var symbol = Symbols.GetSymbol(position.SymbolName);
                if (symbol is null)
                    continue;

                var distance = (double)context.TrailingStop.Value;
                var candidate = position.TradeType == TradeType.Buy
                    ? symbol.Bid - distance
                    : symbol.Ask + distance;
                var improvesStop = position.TradeType == TradeType.Buy
                    ? !position.StopLoss.HasValue || candidate > position.StopLoss.Value
                    : !position.StopLoss.HasValue || candidate < position.StopLoss.Value;
                if (!improvesStop)
                    continue;

                var result = ModifyPosition(position, candidate, position.TakeProfit);
                if (!result.IsSuccessful && VerboseLogging)
                    Print("Impossibile aggiornare trailing stop {0}/{1}: {2}",
                        context.Symbol, context.StrategyCode, result.Error);
            }
        }

        /// <summary>
        /// Cancella gli ordini pending la cui barra di validità è passata. I motori Unger emettono
        /// ordini "next bar": vivono la sola barra successiva al segnale. Senza questa cancellazione ne
        /// resta a mercato uno per ogni barra della finestra operativa, a livelli diversi, tutti
        /// eseguibili.
        /// </summary>
        /// <param name="stream">
        /// Stream la cui barra si è appena chiusa: si valutano soltanto gli ordini piazzati su di esso,
        /// perché sono gli unici la cui validità è misurata su questa serie.
        /// </param>
        private void CancelExpiredPendingOrders(Pair stream)
        {
            if (_pendingOrderBar.Count == 0)
                return;

            foreach (var entry in _pendingOrderBar.ToList())
            {
                var mark = entry.Value;
                if (!ReferenceEquals(mark?.Stream, stream))
                    continue;
                if (mark.Stream.Series == null || mark.Stream.Series.Count <= mark.BarCount)
                    continue;

                CancelPendingOrders(entry.Key, "scaduto (valido una barra sola)");
                _pendingOrderBar.Remove(entry.Key);
            }
        }

        private void CancelPendingOrders(string label, string reason)
        {
            foreach (var order in PendingOrders.Where(o => o.Label == label).ToList())
            {
                var result = CancelPendingOrder(order);
                if (result.IsSuccessful)
                    Print("Ordine pending {0} ({1}) cancellato: {2}.", order.Id, label, reason);
                else
                    Print("Impossibile cancellare l'ordine pending {0} ({1}): {2}", order.Id, label, result.Error);
            }
        }

        /// <summary>
        /// Cancella tutti gli ordini pending di una strategia, qualunque sia l'intent che li ha piazzati.
        /// Serve perché la label porta l'IntentId: il segnale nuovo non ha la stessa label del vecchio,
        /// e cercare per label esatta lascerebbe a mercato l'ordine della barra precedente.
        /// </summary>
        private void CancelStrategyPendingOrders(string strategyCode, string reason)
        {
            var prefix = MakeStrategyLabelPrefix(strategyCode);
            foreach (var order in PendingOrders
                .Where(o => o.Label != null && o.Label.StartsWith(prefix, StringComparison.Ordinal))
                .ToList())
            {
                var result = CancelPendingOrder(order);
                if (result.IsSuccessful)
                    Print("Ordine pending {0} ({1}) cancellato: {2}.", order.Id, order.Label, reason);
                else
                    Print("Impossibile cancellare l'ordine pending {0} ({1}): {2}", order.Id, order.Label, result.Error);
            }

            foreach (var key in _pendingOrderBar.Keys
                .Where(label => label.StartsWith(prefix, StringComparison.Ordinal))
                .ToList())
                _pendingOrderBar.Remove(key);
        }

        /// <summary>
        /// Stream di un intent, per contare le barre di validità dell'ordine pending. Il match è sul
        /// simbolo Piootoo (quello dell'intent), con il timeframe della strategia a disambiguare quando
        /// lo stesso simbolo gira su più timeframe.
        /// </summary>
        private Pair FindPair(string piootooSymbol, int timeframeMinutes)
        {
            var normalized = NormalizeSymbol(piootooSymbol);
            var candidates = _pairs.Where(pair => NormalizeSymbol(pair.PiootooSymbol) == normalized).ToList();
            if (candidates.Count == 0)
                return null;

            if (timeframeMinutes > 0)
            {
                var exact = candidates.FirstOrDefault(pair => pair.TimeframeMinutes == timeframeMinutes);
                if (exact != null)
                    return exact;
            }

            return candidates.OrderBy(pair => pair.TimeframeMinutes).First();
        }

        protected override void OnTimer()
        {
            // Senza questa guardia il polling periodico continuerebbe a reclamare intent nel fine
            // settimana: verrebbero scartati da HandleEntryIntent, ma a costo di una chiamata ognuno.
            if (EnforceWeekEndFlat())
                return;

            PollNextSignal();

            // Le uscite a tempo non possono più appoggiarsi alla barra del grafico: senza questo
            // controllo periodico, su un piano di soli stream lenti CloseAtUtc verrebbe valutato una
            // volta per barra di quello stream, cioè con ore di ritardo.
            CloseExpiredPositions();
        }

        /// <summary>
        /// Porta il conto a flat quando la finestra di fine settimana e' aperta: prima cancella gli
        /// ordini, poi chiude le posizioni. L'ordine conta: chiudere per primo lascerebbe a mercato
        /// un ordine che puo' riaprire nel frattempo.
        ///
        /// <para>Restituisce true quando la finestra e' attiva, cosi' il chiamante sa che in questa
        /// passata non si apre nulla. A conto gia' piatto i due cicli sono vuoti e non stampa nulla,
        /// quindi puo' essere chiamato a ogni tick e a ogni timer senza sporcare il log.</para>
        /// </summary>
        private bool EnforceWeekEndFlat()
        {
            if (!FlatAtWeekEnd || !IsWeekEndFlatWindow(Server.TimeInUtc))
                return false;

            foreach (var order in PendingOrders
                .Where(o => o.Label != null && o.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
            {
                var cancel = CancelPendingOrder(order);
                if (cancel.IsSuccessful)
                    Print("Ordine pending {0} cancellato per il flat di fine settimana.", order.Id);
                else
                    Print("Impossibile cancellare l'ordine {0} per il fine settimana: {1}", order.Id, cancel.Error);
            }
            _pendingOrderBar.Clear();

            foreach (var position in Positions
                .Where(p => p.Label != null && p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
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

        protected override void OnStop()
        {
            SaveLocalState();
            Timer.Stop();

            foreach (var pair in _pairs)
                if (pair.Series != null && pair.BarHandler != null)
                    pair.Series.BarOpened -= pair.BarHandler;

            foreach (var entry in _tickHandlers)
                entry.Key.Tick -= entry.Value;
            _tickHandlers.Clear();

            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;
            _http?.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // Invio della finestra di candele per ogni coppia simbolo/timeframe configurata
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Invia al server la finestra di candele dello stream e gli fa valutare l'ultima, cioè la
        /// barra appena chiusa. Le candele precedenti sono il margine di sovrapposizione: il server
        /// accoda solo quelle che non ha, quindi rispedirle non duplica nulla.
        ///
        /// <para>Il margine non è un dettaglio di banda, è ciò che impedisce i buchi. Il server, nelle
        /// sessioni ExternalBroker, non ha un datafeed proprio: la storia di uno stream è soltanto ciò
        /// che gli è stato spinto. Mandando la sola barra chiusa, ogni giro perso — chiamata fallita,
        /// server irraggiungibile — lascerebbe nella sua serie un vuoto che nessuno colmerebbe più, e
        /// le strategie girerebbero su dati bucati senza accorgersene. Con
        /// <see cref="IncrementalWindowBars"/> barre il buco si ricuce da solo fino a quel numero di
        /// barre consecutive; oltre, il server rifiuta la finestra invece di accodarla.</para>
        ///
        /// <para>La storia profonda (<c>RequiredCandles</c>) non passa di qui: la consegna una volta
        /// sola <see cref="SendWarmUpWindow"/> all'avvio.</para>
        /// </summary>
        private bool TryPushClosedBar(Pair pair)
        {
            var series = pair.Series;
            if (series == null || series.Count < 2)
                return false;

            // Il [Robot] dichiara TimeZone=UTC: gli orari di ogni serie letta da MarketData sono
            // già in UTC, manca solo il flag Kind. TimeZone è un attributo di compilazione, non
            // l'impostazione di visualizzazione di cTrader, quindi il feed resta UTC comunque sia
            // configurata la piattaforma. Il SpecifyKind non è cosmetico: senza il flag il JSON parte
            // senza il suffisso "Z" e ValidateBar sul server rifiuta la barra.
            // Last(1) è l'ultima candela chiusa, Last(0) quella aperta.
            var barTimeUtc = DateTime.SpecifyKind(series.Last(1).OpenTime, DateTimeKind.Utc);
            if (pair.LastPushedBarTimeUtc == barTimeUtc)
                return false;

            // Riscaldamento non riuscito all'avvio (server ancora giù): senza storia profonda il
            // server non valuterebbe comunque, quindi si ritenta qui invece di procedere a vuoto.
            if (!pair.WarmedUp)
                SendWarmUpWindow(pair);

            if (!SendWindow(pair, Math.Max(2, IncrementalWindowBars), evaluateLastCandle: true))
                return false;

            pair.LastPushedBarTimeUtc = barTimeUtc;
            return true;
        }

        /// <summary>
        /// Consegna al server tutta la storia che le strategie richiedono, senza fargli valutare
        /// niente: le barre della finestra sono già passate, e valutarle produrrebbe intent sul
        /// passato che il bot eseguirebbe al prezzo di adesso.
        /// </summary>
        private void SendWarmUpWindow(Pair pair)
        {
            if (SendWindow(pair, pair.RequiredCandles, evaluateLastCandle: false))
            {
                pair.WarmedUp = true;
                if (VerboseLogging)
                    Print("Riscaldamento {0} inviato ({1} candele richieste).", pair, pair.RequiredCandles);
            }
        }

        /// <summary>
        /// Spedisce le ultime <paramref name="depth"/> barre chiuse dello stream, o quante ne ha la
        /// serie se il broker non ne ha altre.
        /// </summary>
        private bool SendWindow(Pair pair, int depth, bool evaluateLastCandle)
        {
            try
            {
                var series = pair.Series;
                if (series == null || series.Count < 2)
                    return false;

                // Indice 1 = ultima barra chiusa, indice 0 = barra in formazione, da escludere.
                var available = series.Count - 1;
                var count = Math.Min(Math.Max(1, depth), available);
                var barTimeUtc = DateTime.SpecifyKind(series.Last(1).OpenTime, DateTimeKind.Utc);

                var candles = new List<OhlcvDto>(count);
                for (var offset = count; offset >= 1; offset--)
                {
                    var bar = series.Last(offset);
                    candles.Add(new OhlcvDto
                    {
                        DateTime = DateTime.SpecifyKind(bar.OpenTime, DateTimeKind.Utc),
                        Open = (decimal)bar.Open,
                        High = (decimal)bar.High,
                        Low = (decimal)bar.Low,
                        Close = (decimal)bar.Close,
                        Volume = (decimal)bar.TickVolume
                    });
                }

                var window = new ClosedBarWindowDto
                {
                    // Nome Piootoo: è con quello che il server indicizza le serie, non il nome broker.
                    Symbol = pair.PiootooSymbol,
                    TimeframeMinutes = pair.TimeframeMinutes,
                    Candles = candles,
                    // Sequenza basata sul timestamp: monotona per lo stream a prescindere da quale
                    // account/cBot la invii per primo (più account pushano le stesse barre di mercato).
                    Sequence = (long)(barTimeUtc - DateTime.UnixEpoch).TotalMilliseconds,
                    IdempotencyKey = $"{pair.PiootooSymbol}|{pair.TimeframeMinutes}|{barTimeUtc:O}",
                    EvaluateLastCandle = evaluateLastCandle
                };

                var request = new PushBarWindowRequestDto
                {
                    SessionId = _sessionId,
                    SessionToken = _sessionToken,
                    Windows = new[] { window }
                };

                var response = PostJson($"api/v1/trading-sessions/{_sessionId}/bars/window", request);
                if (!response.IsSuccessStatusCode)
                {
                    // Un errore qui non è rumore: il server rifiuta la finestra anche quando non si
                    // sovrappone alla sua storia, cioè quando si sta per aprire un buco.
                    OnPushFailed(pair, $"{count} candele rifiutate: {ReadError(response)}");
                    return false;
                }
                if (!_isConnectedToServer)
                    UpdateConnectionStatus(true);

                OnPushSucceeded(pair);
                ReportWindowStatus(pair, ReadBody(response));
                return true;
            }
            catch (Exception ex)
            {
                OnPushFailed(pair, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Registra un invio fallito: stampa il messaggio solo se diverso dall'ultimo di quello
        /// stream, e ferma il bot dopo <see cref="MaxConsecutivePushFailures"/> fallimenti di fila.
        ///
        /// <para>Senza queste due cose un piano mal configurato produce la stessa riga a ogni barra
        /// per l'intero backtest — centinaia di righe identiche — e il run arriva in fondo senza aver
        /// mai valutato niente. Meglio fermarsi presto e rumorosamente.</para>
        /// </summary>
        private void OnPushFailed(Pair pair, string message)
        {
            UpdateConnectionStatus(false);

            var key = MakeStreamKey(pair.PiootooSymbol, pair.TimeframeMinutes);
            if (!_lastPushError.TryGetValue(key, out var previous) || previous != message)
            {
                _lastPushError[key] = message;
                Print("Invio finestra {0} fallito: {1}", pair, message);
            }

            if (++_consecutivePushFailures < MaxConsecutivePushFailures)
                return;

            Print("{0} invii falliti di fila su tutti gli stream: il bot si ferma. " +
                  "L'ultimo errore è: {1}", _consecutivePushFailures, message);
            Stop();
        }

        private void OnPushSucceeded(Pair pair)
        {
            _consecutivePushFailures = 0;

            var key = MakeStreamKey(pair.PiootooSymbol, pair.TimeframeMinutes);
            if (_lastPushError.Remove(key))
                Print("Invio finestra {0} tornato a funzionare.", pair);
        }

        /// <summary>
        /// Stampa, una volta sola per stream, il motivo per cui il server non sta ancora valutando:
        /// senza questo "nessun segnale" e "storia troppo corta per valutare" sono indistinguibili dal
        /// grafico e dal log, che è esattamente il modo in cui un run muto passa inosservato.
        /// </summary>
        private void ReportWindowStatus(Pair pair, string body)
        {
            LogJsonResponse("bars/window", body);
            var payload = JsonSerializer.Deserialize<PushBarWindowResponseDto>(body, _json);
            var status = payload?.Streams?.FirstOrDefault();
            if (status is null)
                return;

            var key = MakeStreamKey(pair.PiootooSymbol, pair.TimeframeMinutes);
            if (status.SkippedForInsufficientHistory > 0)
            {
                if (_insufficientHistoryReported.Add(key))
                    Print("{0}: il server ha {1} candele su {2} richieste, {3} strategie non valutate. " +
                          "Servono ancora {4} barre.",
                        pair, status.HistoryBars, status.RequiredCandles,
                        status.SkippedForInsufficientHistory,
                        Math.Max(0, status.RequiredCandles - status.HistoryBars));
                return;
            }

            if (_insufficientHistoryReported.Remove(key))
                Print("{0}: storia sufficiente, {1} strategie ora valutate a ogni barra.",
                    pair, status.EvaluatedStrategies);
        }

        // ---------------------------------------------------------------------------------------
        // Polling segnali per il proprio account ed esecuzione
        // ---------------------------------------------------------------------------------------

        private void PollNextSignal()
        {
            try
            {
                var historyFromUtc = Server.TimeInUtc.AddDays(-Math.Max(1, HistoryWindowDays));
                var platformState = new AccountSignalPollRequestDto
                {
                    SessionToken = _sessionToken,
                    Positions = Positions
                        .Select(p => new { Position = p, Parsed = ParseLabel(p.Label) })
                        .Where(x => x.Parsed != null)
                        .Select(x => new BrokerPositionSnapshotDto
                        {
                            PositionId = x.Position.Id.ToString(),
                            Symbol = x.Position.SymbolName,
                            StrategyCode = x.Parsed.StrategyCode,
                            IntentId = x.Parsed.IntentId
                        })
                        .ToList(),
                    Orders = PendingOrders
                        .Select(o => new { Order = o, Parsed = ParseLabel(o.Label) })
                        .Where(x => x.Parsed != null)
                        .Select(x => new BrokerOrderSnapshotDto
                        {
                            OrderId = x.Order.Id.ToString(),
                            Symbol = x.Order.SymbolName,
                            StrategyCode = x.Parsed.StrategyCode,
                            IntentId = x.Parsed.IntentId
                        })
                        .ToList(),
                    Trades = History
                        .Where(t => t.Label != null &&
                                    t.Label.StartsWith(LabelPrefix + LabelSeparator, StringComparison.Ordinal) &&
                                    t.ClosingTime >= historyFromUtc)
                        .Select(t => new BrokerTradeSnapshotDto
                        {
                            PositionId = t.PositionId.ToString(),
                            ClosingTimeUtc = DateTime.SpecifyKind(t.ClosingTime, DateTimeKind.Utc)
                        })
                        .ToList()
                };
                var response = PostJson(
                    $"api/v1/trading-sessions/{_sessionId}/accounts/{Uri.EscapeDataString(_accountNumber)}/signal",
                    platformState);

                if (!response.IsSuccessStatusCode)
                {
                    if (VerboseLogging) Print("Poll segnale fallito: {0}", ReadError(response));
                    UpdateConnectionStatus(false);
                    return;
                }
                if (!_isConnectedToServer)
                    UpdateConnectionStatus(true);

                var body = ReadBody(response);
                LogJsonResponse("signal", body);
                var payload = JsonSerializer.Deserialize<AccountSignalResponseDto>(body, _json);
                if (payload?.Intent is null)
                {
                    // Il motivo si stampa sempre, non solo con il log dettagliato, ma una volta sola
                    // finché non cambia: il poll gira a ogni barra e ogni pochi secondi. Un bot che
                    // tace mentre il server genera segnali è il modo più efficace di perdere un run
                    // intero senza accorgersene.
                    var reason = string.IsNullOrWhiteSpace(payload?.ReasonDetail)
                        ? payload?.Reason
                        : payload.ReasonDetail;
                    if (!string.IsNullOrWhiteSpace(reason) && _lastPollReason != reason)
                    {
                        _lastPollReason = reason;
                        Print("Nessun intent per l'account: {0}", reason);
                    }
                    return;
                }

                if (_lastPollReason != null)
                {
                    _lastPollReason = null;
                    Print("Intent ricevuti di nuovo dal server.");
                }

                var intent = payload.Intent;
                if (_submittedIntentIds.Contains(intent.IntentId))
                    return; // già in gestione, aspettiamo l'esito dell'ordine già inviato

                if (intent.IsClose || string.Equals(intent.Kind, "Close", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCloseIntent(intent);
                    return;
                }

                HandleEntryIntent(intent);
            }
            catch (Exception ex)
            {
                Print("Errore polling segnale: {0}", ex.Message);
                UpdateConnectionStatus(false);
            }
        }

        private void HandleEntryIntent(OrderIntentDto intent)
        {
            // Ultima barriera: un intent reclamato appena prima del taglio non deve aprire nulla.
            // Gli intent di chiusura non passano di qui, quindi la riduzione di rischio resta libera.
            if (FlatAtWeekEnd && IsWeekEndFlatWindow(Server.TimeInUtc))
            {
                Print("Ingresso {0}/{1} scartato: finestra di flat di fine settimana.",
                    intent.Symbol, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            // Verso il broker si usa il nome dello strumento sull'account, verso il server quello
            // Piootoo: la tabella di conversione del conto traduce l'uno nell'altro.
            var brokerSymbolName = ResolveIntentSymbol(intent);

            // Autolimitazione lato client PER SIMBOLO (oltre a quella già garantita dal server): se il bot
            // ha già una posizione aperta su QUESTO simbolo, non ne apre una seconda; può però tradare in
            // parallelo altri simboli configurati.
            var alreadyOpenOnSymbol = Positions.Any(p =>
                p.SymbolName.Equals(brokerSymbolName, StringComparison.OrdinalIgnoreCase) &&
                p.Label != null &&
                p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal));
            if (alreadyOpenOnSymbol)
            {
                if (VerboseLogging)
                    Print("Ingresso {0}/{1} ignorato: il bot ha già una posizione aperta su questo simbolo.", intent.Symbol, intent.StrategyCode);
                return;
            }

            var symbol = Symbols.GetSymbol(brokerSymbolName);
            if (symbol is null)
            {
                Print("Simbolo '{0}' non disponibile/non abilitato su questo account: ingresso {1} scartato.", brokerSymbolName, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            // Solo per gli ordini a mercato. Uno Stop o un Limit sta per definizione LONTANO dal
            // prezzo corrente — è il livello a cui si vuole entrare, non quello a cui si è — quindi
            // misurarne la distanza come slippage scarta esattamente gli ordini che i motori Unger
            // emettono sempre: un breakout di Donchian a 40 punti dal prezzo verrebbe rifiutato ogni
            // volta. Lo slippage di un pending lo governa il broker al fill, non il bot al
            // piazzamento.
            if (intent.OrderType == TradeOrderTypeDto.Market && MaxEntrySlippagePips > 0 && intent.Price > 0)
            {
                var currentPrice = intent.Side == SignalTypeDto.Buy ? symbol.Ask : symbol.Bid;
                var distancePips = Math.Abs(currentPrice - (double)intent.Price) / symbol.PipSize;
                if (distancePips > MaxEntrySlippagePips)
                {
                    Print("Ingresso {0}/{1} scartato per slippage ({2:0.0} pips).", intent.Symbol, intent.StrategyCode, distancePips);
                    ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                    return;
                }
            }

            _submittedIntentIds.Add(intent.IntentId);
            var tradeType = intent.Side == SignalTypeDto.Buy ? TradeType.Buy : TradeType.Sell;
            var rawVolume = Math.Max(0.01, (double)intent.FinalQuantity);
            var volume = symbol.NormalizeVolumeInUnits(rawVolume, RoundingMode.Down);

            // La label porta l'IntentId: posizione e ordine restano riconducibili al segnale che li ha
            // creati leggendo la sola piattaforma, senza dipendere dallo stato locale del bot.
            var label = MakeLabel(intent.StrategyCode, intent.IntentId);

            // Stop Loss/Take Profit del segnale applicati come livelli nativi sull'ordine: li gestisce
            // il broker; l'eventuale chiusura risultante viene comunque intercettata e riportata al
            // server da OnPositionClosed (vedi nota in testa al file).
            var stopLossPips = ToPips(symbol, intent.StopLoss);
            var takeProfitPips = ToPips(symbol, intent.TakeProfit);

            // Il segnale precedente della stessa strategia è scaduto nel momento in cui ne arriva uno
            // nuovo: il motore riemette l'ordine a ogni barra col livello ricalcolato, quindi il vecchio
            // ordine pending non è un secondo ordine, è lo stesso ordine da sostituire. Il match è per
            // strategia e non per label esatta: la label del segnale nuovo porta un IntentId diverso.
            CancelStrategyPendingOrders(intent.StrategyCode, "sostituito dal signal successivo");

            // Gli intent precedenti della stessa strategia sono stati appena cancellati a mercato: le
            // loro label non si apriranno più, e senza questa potatura la mappa crescerebbe di una voce
            // per barra (prima la chiave era la sola strategia e si sovrascriveva da sé).
            foreach (var stale in _lastOpenIntentByLabel.Keys
                .Where(key => key.StartsWith(MakeStrategyLabelPrefix(intent.StrategyCode), StringComparison.Ordinal))
                .ToList())
                _lastOpenIntentByLabel.Remove(stale);

            _lastOpenIntentByLabel[label] = intent;

            TradeResult result;
            switch (intent.OrderType)
            {
                case TradeOrderTypeDto.Stop:
                    result = PlaceStopOrder(tradeType, brokerSymbolName, volume, (double)intent.Price, label, stopLossPips, takeProfitPips);
                    break;
                case TradeOrderTypeDto.Limit:
                    result = PlaceLimitOrder(tradeType, brokerSymbolName, volume, (double)intent.Price, label, stopLossPips, takeProfitPips);
                    break;
                default:
                    result = ExecuteMarketOrder(tradeType, brokerSymbolName, volume, label, stopLossPips, takeProfitPips, intent.Reason);
                    break;
            }

            if (!result.IsSuccessful)
            {
                Print("Errore apertura posizione {0}/{1}: {2}", brokerSymbolName, intent.StrategyCode, result.Error);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                _lastOpenIntentByLabel.Remove(label);
                _submittedIntentIds.Remove(intent.IntentId);
                return;
            }

            // Scadenza dell'ordine pending: la barra corrente è l'unica in cui può essere eseguito, come
            // "next bar at ... stop" di EasyLanguage. Il conteggio è sulla serie dello stream della
            // strategia, non su quella del grafico.
            if (intent.ExpiresAtUtc.HasValue && result.PendingOrder != null)
            {
                var stream = FindPair(intent.Symbol, intent.TimeframeMinutes);
                if (stream?.Series != null)
                    _pendingOrderBar[label] = new PendingOrderMark { Stream = stream, BarCount = stream.Series.Count };
            }
            else
            {
                _pendingOrderBar.Remove(label);
            }

            // Il fill — sia esso immediato (mercato) o differito (pending Stop/Limit) — viene riportato
            // da OnPositionOpened: un solo punto di reporting, così market e pending non duplicano né
            // dimenticano l'apertura.
        }

        /// <summary>
        /// Una posizione di questo bot si è aperta: risolve l'intent che l'ha originata (ordine a
        /// mercato o pending appena riempito) e riporta il fill al server. Serve perché
        /// <c>PlaceStopOrder</c>/<c>PlaceLimitOrder</c> non restituiscono una posizione sincrona.
        /// </summary>
        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == null || !position.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                return;
            if (_openPositions.ContainsKey(position.Id))
                return;
            if (!_lastOpenIntentByLabel.TryGetValue(position.Label, out var intent))
            {
                Print("Posizione {0} aperta ({1}) senza un intent locale associato: nessun report inviato.", position.Id, position.Label);
                return;
            }

            _openPositions[position.Id] = new OpenPositionContext
            {
                PositionId = position.Id,
                EntryIntentId = intent.IntentId,
                StrategyCode = intent.StrategyCode,
                Symbol = intent.Symbol,
                TimeframeMinutes = intent.TimeframeMinutes,
                BreakEven = intent.BreakEven,
                TrailingStop = intent.TrailingStop,
                CloseAtUtc = intent.CloseAtUtc,
                TimeExitOnlyIfProfitBelowMoneyPerContract = intent.TimeExitOnlyIfProfitBelowMoneyPerContract,
                ProfitStallAfterUtc = intent.ProfitStallAfterUtc,
                MaxBarsInPosition = intent.MaxBarsInPosition ?? 0,
                BarsInPosition = 0,
                ContractMultiplier = intent.ContractMultiplier > 0 ? intent.ContractMultiplier : 1m
            };
            _pendingOrderBar.Remove(position.Label);
            _lastOpenIntentByLabel.Remove(position.Label); // l'intent ha prodotto la sua posizione: esaurito
            SaveLocalState();

            ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Filled,
                (decimal)position.VolumeInUnits, (decimal)position.EntryPrice, position.Id.ToString());
        }

        private void HandleCloseIntent(OrderIntentDto intent)
        {
            // La posizione da chiudere porta la label del suo intent di INGRESSO, diverso da quello di
            // chiusura appena ricevuto: il match è quindi sul prefisso di strategia.
            var strategyPrefix = MakeStrategyLabelPrefix(intent.StrategyCode);
            var position = Positions.FirstOrDefault(candidate =>
                candidate.SymbolName.Equals(ResolveIntentSymbol(intent), StringComparison.OrdinalIgnoreCase) &&
                candidate.Label != null &&
                candidate.Label.StartsWith(strategyPrefix, StringComparison.Ordinal));
            if (position is null)
            {
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            _submittedIntentIds.Add(intent.IntentId);
            _serverCloseIntents[position.Id] = intent;
            var result = ClosePosition(position);
            if (!result.IsSuccessful)
            {
                _serverCloseIntents.Remove(position.Id);
                _submittedIntentIds.Remove(intent.IntentId);
                Print("Errore chiusura posizione {0} per intent {1}: {2}", position.Id, intent.IntentId, result.Error);
            }
        }

        /// <summary>
        /// Evento cAlgo: una posizione si è effettivamente chiusa, per qualunque causa (Stop Loss/Take
        /// Profit del broker, scadenza CloseAtUtc gestita in locale, o una ClosePosition() nostra su
        /// richiesta del server). Legge l'esito reale del trade dallo storico e lo invia al server.
        /// </summary>
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (!_openPositions.TryGetValue(position.Id, out var ctx))
                return; // posizione non aperta da questo bot: ignorata
            _openPositions.Remove(position.Id);
            _peakProfitAfterStall.Remove(position.Id);
            SaveLocalState();

            var trade = History.LastOrDefault(h => h.PositionId == position.Id);
            var closePrice = (decimal?)trade?.ClosingPrice;
            var quantity = (decimal)(trade?.VolumeInUnits ?? position.VolumeInUnits);
            var commission = (decimal)(trade?.Commissions ?? 0);

            if (_serverCloseIntents.Remove(position.Id, out var closeIntent))
                ReportExecution(closeIntent.IntentId, position.SymbolName, ExecutionReportStatusDto.Filled,
                    quantity, closePrice, position.Id.ToString(), commission);
            else
                RegisterExternalCloseAndReport(ctx, position, quantity, closePrice, commission, args.Reason.ToString());
        }

        private void RegisterExternalCloseAndReport(
            OpenPositionContext ctx, Position position, decimal quantity, decimal? closePrice, decimal commission, string reason)
        {
            try
            {
                var closeIntentRequest = new CreateExternalCloseIntentRequestDto
                {
                    SessionToken = _sessionToken,
                    StrategyCode = ctx.StrategyCode,
                    Symbol = ctx.Symbol,
                    AccountNumber = _accountNumber,
                    Quantity = quantity,
                    Reason = $"LocalExit:{reason}"
                };
                using var request = BuildRequest(HttpMethod.Post, $"api/v1/trading-sessions/{_sessionId}/intents/close-external");
                request.Content = new StringContent(JsonSerializer.Serialize(closeIntentRequest, _json), Encoding.UTF8, "application/json");
                var response = _http.Send(request);
                if (!response.IsSuccessStatusCode)
                {
                    Print("Registrazione chiusura esterna fallita per {0}/{1}: {2}", ctx.Symbol, ctx.StrategyCode, ReadError(response));
                    return;
                }

                var closeBody = ReadBody(response);
                LogJsonResponse("intents/close-external", closeBody);
                var closeIntent = JsonSerializer.Deserialize<OrderIntentDto>(closeBody, _json);
                ReportExecution(closeIntent.IntentId, position.SymbolName, ExecutionReportStatusDto.Filled, quantity, closePrice, null, commission);
            }
            catch (Exception ex)
            {
                Print("Errore registrazione chiusura esterna {0}/{1}: {2}", ctx.Symbol, ctx.StrategyCode, ex.Message);
            }
        }

        private void ReportExecution(
            string intentId, string symbol, ExecutionReportStatusDto status, decimal filledQuantity,
            decimal? fillPrice, string externalOrderId = null, decimal commission = 0)
        {
            try
            {
                var request = new ExecutionReportRequestDto
                {
                    SessionToken = _sessionToken,
                    Report = new ExternalExecutionReportDto
                    {
                        ReportId = $"{intentId}-{Guid.NewGuid():N}",
                        IntentId = intentId,
                        ExternalOrderId = externalOrderId,
                        Status = status,
                        CumulativeFilledQuantity = filledQuantity,
                        FillPrice = fillPrice,
                        Commission = commission,
                        EventTimeUtc = Server.TimeInUtc
                    }
                };
                var response = PostJson($"api/v1/trading-sessions/{_sessionId}/execution-reports", request);
                if (!response.IsSuccessStatusCode)
                    Print("Invio execution report fallito per {0} ({1}): {2}", intentId, symbol, ReadError(response));
            }
            catch (Exception ex)
            {
                Print("Errore invio execution report {0} ({1}): {2}", intentId, symbol, ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Helper HTTP / parsing / conversioni
        // ---------------------------------------------------------------------------------------

        private static string BuildLocalStatePath(string planCode, string accountNumber)
        {
            var invalid = Path.GetInvalidFileNameChars();
            string Safe(string value) => new string((value ?? string.Empty)
                .Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PiootooLiveTradingBot");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"state-{Safe(planCode)}-{Safe(accountNumber)}.json");
        }

        private static string BuildJsonLogPath(string planCode, string accountNumber)
        {
            var invalid = Path.GetInvalidFileNameChars();
            string Safe(string value) => new string((value ?? string.Empty)
                .Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PiootooLiveTradingBot");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"json-log-{Safe(planCode)}-{Safe(accountNumber)}.jsonl");
        }

        /// <summary>
        /// Una riga JSON per risposta ricevuta dal server, con timestamp ed endpoint: append-only,
        /// niente fsync (non e' l'artefatto finale, e qui gira dentro OnBar/Timer quindi va veloce).
        /// Attivo solo con <see cref="LogServerResponses"/>, mai in backtest (nessun valore, solo I/O).
        /// </summary>
        private void LogJsonResponse(string endpoint, string json)
        {
            if (string.IsNullOrWhiteSpace(_jsonLogPath))
                return;
            try
            {
                var line = JsonSerializer.Serialize(new
                {
                    TimestampUtc = DateTime.UtcNow,
                    Endpoint = endpoint,
                    Body = json
                });
                File.AppendAllText(_jsonLogPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Print("Log JSON su file fallito: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Ripristina solo contesti appartenenti alla sessione realtime appena riaperta e ancora
        /// presenti sulla piattaforma. In questo modo CloseAtUtc e MaxBarsInPosition sopravvivono
        /// al riavvio del cBot senza associare per errore una posizione a una nuova sessione.
        /// </summary>
        private void RestoreLocalState()
        {
            if (string.IsNullOrWhiteSpace(_localStatePath) || !File.Exists(_localStatePath))
                return;
            try
            {
                var state = JsonSerializer.Deserialize<LocalSessionState>(
                    File.ReadAllText(_localStatePath), _json);
                if (state == null || !string.Equals(state.SessionId, _sessionId, StringComparison.Ordinal))
                {
                    Print("Stato locale ignorato: appartiene a una sessione diversa.");
                    return;
                }

                var platformIds = new HashSet<int>(Positions
                    .Where(position => position.Label != null &&
                                       position.Label.StartsWith(LabelPrefix + LabelSeparator, StringComparison.Ordinal))
                    .Select(position => position.Id));
                foreach (var context in state.Positions ?? new List<OpenPositionContext>())
                    if (platformIds.Contains(context.PositionId))
                        _openPositions[context.PositionId] = context;

                SaveLocalState(); // elimina dal file le posizioni non più presenti sul broker
                Print("Ripristinate {0} condizioni di uscita dalla sessione locale.", _openPositions.Count);
            }
            catch (Exception ex)
            {
                Print("Stato locale non leggibile: {0}", ex.Message);
            }
        }

        private void SaveLocalState()
        {
            if (string.IsNullOrWhiteSpace(_localStatePath))
                return;
            try
            {
                var state = new LocalSessionState
                {
                    PlanCode = PlanCode,
                    AccountNumber = _accountNumber,
                    SessionId = _sessionId,
                    Positions = _openPositions.Values.OrderBy(context => context.PositionId).ToList()
                };
                var temporary = _localStatePath + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(state, _json));
                if (File.Exists(_localStatePath))
                    File.Replace(temporary, _localStatePath, null);
                else
                    File.Move(temporary, _localStatePath);
            }
            catch (Exception ex)
            {
                Print("Salvataggio stato locale fallito: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Costruisce i flussi (simbolo, timeframe) del piano dagli strumenti del descriptor di sessione.
        /// Non c'è un parametro di configurazione locale degli strumenti di proposito: duplicherebbe il
        /// masterfilter, e le due liste divergerebbero in silenzio.
        /// </summary>
        private static bool BuildPairs(IReadOnlyList<TradingInstrumentDto> instruments, out List<Pair> pairs, out string error)
        {
            pairs = new List<Pair>();
            error = null;
            if (instruments == null || instruments.Count == 0)
            {
                error = "nessuno strumento configurato.";
                return false;
            }

            foreach (var instrument in instruments)
            {
                if (string.IsNullOrWhiteSpace(instrument.Symbol))
                    continue;

                var accountSymbol = string.IsNullOrWhiteSpace(instrument.AccountSymbol)
                    ? instrument.Symbol
                    : instrument.AccountSymbol;

                foreach (var tf in instrument.TimeframesMinutes ?? Array.Empty<int>())
                {
                    if (tf <= 0)
                        continue;

                    // Profondità della finestra: la dichiara il server, che conosce il masterfilter.
                    // Un default locale sarebbe una seconda verità destinata a divergere.
                    var required = 0;
                    if (instrument.RequiredCandlesByTimeframe != null)
                        instrument.RequiredCandlesByTimeframe.TryGetValue(tf, out required);

                    pairs.Add(new Pair
                    {
                        PiootooSymbol = instrument.Symbol,
                        AccountSymbol = accountSymbol,
                        TimeframeMinutes = tf,
                        RequiredCandles = Math.Max(1, required)
                    });
                }
            }

            if (pairs.Count == 0)
            {
                error = "nessuno strumento valido trovato.";
                return false;
            }
            return true;
        }

        private HttpResponseMessage PostJson<T>(string uri, T body)
        {
            var request = BuildRequest(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
            return _http.Send(request);
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string uri)
        {
            var request = new HttpRequestMessage(method, uri);
            if (!string.IsNullOrWhiteSpace(_sessionToken))
                request.Headers.Add("X-Session-Token", _sessionToken);
            return request;
        }

        private static string ReadBody(HttpResponseMessage response)
        {
            using var stream = response.Content.ReadAsStream();
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string ReadError(HttpResponseMessage response)
        {
            try { return $"{(int)response.StatusCode} {ReadBody(response)}"; }
            catch { return response.StatusCode.ToString(); }
        }

        private static double? ToPips(Symbol symbol, decimal? priceDistance)
        {
            if (!priceDistance.HasValue || priceDistance.Value <= 0)
                return null;
            return (double)priceDistance.Value / symbol.PipSize;
        }

        /// <summary>
        /// Label di posizioni e ordini: <c>PiootooLive:{StrategyCode}:{IntentId}</c>. È l'unico legame
        /// fra ciò che sta sulla piattaforma e il segnale che l'ha generato che sopravvive a un riavvio
        /// del cBot e alla perdita dello stato locale, quindi ci va l'IntentId per intero.
        /// </summary>
        private static string MakeLabel(string strategyCode, string intentId) =>
            $"{LabelPrefix}{LabelSeparator}{strategyCode}{LabelSeparator}{intentId}";

        /// <summary>Prefisso comune a tutte le label di una strategia, per i match che ignorano l'intent.</summary>
        private static string MakeStrategyLabelPrefix(string strategyCode) =>
            $"{LabelPrefix}{LabelSeparator}{strategyCode}{LabelSeparator}";

        /// <summary>
        /// Scompone una label del bot. Tollera le label del formato precedente
        /// (<c>PiootooLive:{StrategyCode}</c>, senza intent) restituendo un IntentId vuoto: sono le
        /// posizioni aperte da una versione più vecchia e ancora a mercato.
        /// </summary>
        private static ParsedLabel ParseLabel(string label)
        {
            if (string.IsNullOrEmpty(label) ||
                !label.StartsWith(LabelPrefix + LabelSeparator, StringComparison.Ordinal))
                return null;

            var rest = label.Substring(LabelPrefix.Length + 1);
            var separator = rest.IndexOf(LabelSeparator);
            var strategyCode = separator < 0 ? rest : rest.Substring(0, separator);
            if (strategyCode.Length == 0)
                return null;

            return new ParsedLabel
            {
                StrategyCode = strategyCode,
                IntentId = separator < 0 ? string.Empty : rest.Substring(separator + 1)
            };
        }

        private sealed class ParsedLabel
        {
            public string StrategyCode { get; set; }
            public string IntentId { get; set; }
        }

        /// <summary>
        /// Nome dello strumento sul broker: quello risolto dalla tabella di conversione dell'account
        /// se il server lo ha valorizzato, altrimenti il nome Piootoo (conti senza conversione).
        /// </summary>
        private static string ResolveIntentSymbol(OrderIntentDto intent) =>
            string.IsNullOrWhiteSpace(intent.AccountSymbol) ? intent.Symbol : intent.AccountSymbol;

        private static string NormalizeSymbol(string symbol) =>
            (symbol ?? string.Empty).Trim().TrimStart('@').ToUpperInvariant();

        private static string MakeStreamKey(string symbol, int timeframeMinutes) =>
            $"{NormalizeSymbol(symbol)}|{timeframeMinutes}";

        private static TimeFrame ToTimeFrame(int minutes) => minutes switch
        {
            1 => TimeFrame.Minute,
            2 => TimeFrame.Minute2,
            3 => TimeFrame.Minute3,
            4 => TimeFrame.Minute4,
            5 => TimeFrame.Minute5,
            10 => TimeFrame.Minute10,
            15 => TimeFrame.Minute15,
            20 => TimeFrame.Minute20,
            30 => TimeFrame.Minute30,
            45 => TimeFrame.Minute45,
            60 => TimeFrame.Hour,
            120 => TimeFrame.Hour2,
            180 => TimeFrame.Hour3,
            240 => TimeFrame.Hour4,
            360 => TimeFrame.Hour6,
            480 => TimeFrame.Hour8,
            720 => TimeFrame.Hour12,
            1440 => TimeFrame.Daily,
            _ => throw new ArgumentException($"Timeframe non supportato: {minutes} minuti.")
        };

        // ---------------------------------------------------------------------------------------
        // DTO minimi, allineati (per nome/forma JSON) ai contratti Piootoo.Shared.Models.Trading.
        // Duplicati qui perché un cBot cTrader è un singolo file senza riferimenti di progetto.
        // ---------------------------------------------------------------------------------------

        [JsonConverter(typeof(JsonStringEnumConverter))]
        private enum SignalTypeDto { Buy, Sell, Hold }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        private enum TradeOrderTypeDto { Market, Stop, Limit }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        private enum ExecutionReportStatusDto { Accepted, PartiallyFilled, Filled, Rejected, Cancelled }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        private enum OrderIntentStatusDto { Pending, Accepted, PartiallyFilled, Filled, Rejected, Cancelled }

        private sealed class OhlcvDto
        {
            public DateTime DateTime { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
        }

        /// <summary>
        /// Finestra di candele di uno stream: l'ultima è la barra da valutare, le precedenti servono
        /// al server per avere la storia che le strategie richiedono.
        /// </summary>
        private sealed class ClosedBarWindowDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public IReadOnlyList<OhlcvDto> Candles { get; set; }
            public long Sequence { get; set; }
            public string IdempotencyKey { get; set; }

            /// <summary>false = solo riscaldamento: il server accoda e non valuta nulla.</summary>
            public bool EvaluateLastCandle { get; set; } = true;
        }

        private sealed class PushBarWindowRequestDto
        {
            public string SessionId { get; set; }
            public string SessionToken { get; set; }
            public IReadOnlyList<ClosedBarWindowDto> Windows { get; set; }
        }

        private sealed class StreamHistoryStatusDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public int HistoryBars { get; set; }
            public int RequiredCandles { get; set; }
            public int EvaluatedStrategies { get; set; }
            public int SkippedForInsufficientHistory { get; set; }
        }

        private sealed class PushBarWindowResponseDto
        {
            public int AcceptedBars { get; set; }
            public int DuplicateBars { get; set; }
            public int BackfilledBars { get; set; }
            public IReadOnlyList<StreamHistoryStatusDto> Streams { get; set; }
        }

        private sealed class OpenTradingPlanSessionRequestDto
        {
            public string PlanCode { get; set; }
            public string ClientRunMode { get; set; }
            public string ExecutionKey { get; set; }
            public string AccountNumber { get; set; }
        }

        private sealed class TradingSessionDescriptorDto
        {
            public string SessionId { get; set; }
            public string SessionToken { get; set; }
            public IReadOnlyList<TradingInstrumentDto> Instruments { get; set; }
        }

        private sealed class TradingInstrumentDto
        {
            /// <summary>Simbolo Piootoo: chiave con cui il server indicizza barre, strategie e posizioni.</summary>
            public string Symbol { get; set; }

            /// <summary>
            /// Nome dello stesso strumento sull'account che esegue la sessione, risolto dalla tabella di
            /// conversione. È quello con cui va letta la serie e piazzato l'ordine sul broker; vuoto o
            /// uguale a <see cref="Symbol"/> quando l'account non converte quel simbolo.
            /// </summary>
            public string AccountSymbol { get; set; }
            public IReadOnlyList<int> TimeframesMinutes { get; set; }

            /// <summary>
            /// Per timeframe, il massimo <c>RequiredCandles</c> fra le strategie del masterfilter su
            /// questa coppia: sotto quella soglia il server non valuta e la sessione resta muta.
            /// </summary>
            public Dictionary<int, int> RequiredCandlesByTimeframe { get; set; }
        }

        private sealed class OrderIntentDto
        {
            public string IntentId { get; set; }
            public string StrategyCode { get; set; }

            /// <summary>Simbolo Piootoo: è la chiave con cui il server indicizza tutto.</summary>
            public string Symbol { get; set; }

            /// <summary>
            /// Simbolo dello stesso strumento sull'account, risolto dalla tabella di conversione:
            /// è quello con cui va inoltrato l'ordine al broker.
            /// </summary>
            public string AccountSymbol { get; set; }
            public SignalTypeDto Side { get; set; }
            public TradeOrderTypeDto OrderType { get; set; }
            public decimal FinalQuantity { get; set; }
            public decimal Price { get; set; }
            /// <summary>"Entry" oppure "Close"; Close può essere emesso per un segnale ExitOnly.</summary>
            public string Kind { get; set; } = "Entry";
            public bool IsClose { get; set; }
            // Specifica di uscita completa: e' l'unica informazione con cui il bot chiude la posizione.
            public decimal? StopLoss { get; set; }
            public decimal? TakeProfit { get; set; }
            public decimal? BreakEven { get; set; }
            public decimal? TrailingStop { get; set; }
            public int TimeframeMinutes { get; set; }
            public int? MaxBarsInPosition { get; set; }
            public DateTime? CloseAtUtc { get; set; }

            /// <summary>Condiziona la chiusura a CloseAtUtc all'utile aperto per contratto Piootoo. Null = incondizionata.</summary>
            public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; set; }

            /// <summary>Da questo istante si sorveglia l'utile aperto e si chiude alla prima barra senza un nuovo massimo.</summary>
            public DateTime? ProfitStallAfterUtc { get; set; }

            /// <summary>Istante da cui l'ordine pending è valido (semantica "next bar" dei motori Unger).</summary>
            public DateTime? ValidFromUtc { get; set; }

            /// <summary>Scadenza dell'ordine pending: oltre questo istante va cancellato, non eseguito.</summary>
            public DateTime? ExpiresAtUtc { get; set; }

            /// <summary>Rapporto contratto broker / contratto Piootoo, per riportare NetProfit a utile per contratto.</summary>
            public decimal ContractMultiplier { get; set; } = 1m;

            public string Reason { get; set; }
            public OrderIntentStatusDto Status { get; set; }
            public decimal Quantity { get; set; }
            public string AssignedAccountNumber { get; set; }
            public string AssignedGroupId { get; set; }
        }

        private sealed class CreateExternalCloseIntentRequestDto
        {
            public string SessionToken { get; set; }
            public string StrategyCode { get; set; }
            public string Symbol { get; set; }
            public string AccountNumber { get; set; }
            public decimal Quantity { get; set; }
            public string Reason { get; set; }
        }

        private sealed class AccountSignalResponseDto
        {
            public OrderIntentDto Intent { get; set; }
            public string Reason { get; set; }

            /// <summary>Quale filtro del claim ha scartato i template, in chiaro.</summary>
            public string ReasonDetail { get; set; }
            public int OpenPositions { get; set; }
            public int PendingOrders { get; set; }
            public int MaxConcurrentTrades { get; set; }
        }

        private sealed class AccountSignalPollRequestDto
        {
            public string SessionToken { get; set; }
            public List<BrokerPositionSnapshotDto> Positions { get; set; } = new();
            public List<BrokerOrderSnapshotDto> Orders { get; set; } = new();
            public List<BrokerTradeSnapshotDto> Trades { get; set; } = new();
        }

        private sealed class BrokerPositionSnapshotDto
        {
            public string PositionId { get; set; }
            public string Symbol { get; set; }
            public string StrategyCode { get; set; }

            /// <summary>Intent di ingresso letto dalla label; vuoto per posizioni con label di formato precedente.</summary>
            public string IntentId { get; set; }
        }

        private sealed class BrokerTradeSnapshotDto
        {
            public string PositionId { get; set; }
            public DateTime ClosingTimeUtc { get; set; }
        }

        private sealed class BrokerOrderSnapshotDto
        {
            public string OrderId { get; set; }
            public string Symbol { get; set; }
            public string StrategyCode { get; set; }

            /// <summary>Intent che ha piazzato l'ordine, letto dalla label.</summary>
            public string IntentId { get; set; }
        }

        private sealed class ExternalExecutionReportDto
        {
            public string ReportId { get; set; }
            public string IntentId { get; set; }
            public string ExternalOrderId { get; set; }
            public ExecutionReportStatusDto Status { get; set; }
            public decimal CumulativeFilledQuantity { get; set; }
            public decimal? FillPrice { get; set; }
            public decimal Commission { get; set; }
            public DateTime EventTimeUtc { get; set; }
        }

        private sealed class ExecutionReportRequestDto
        {
            public string SessionToken { get; set; }
            public ExternalExecutionReportDto Report { get; set; }
        }
    }
}
