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
using HttpMethod = System.Net.Http.HttpMethod;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot "live" collegato al trading system Piootoo (api/v1/trading-sessions):
    ///  - il grafico a cui è agganciato il bot deve avere il timeframe = BaseTimeframeMinutes (default 5
    ///    min): ad ogni sua barra chiusa (OnBar) il bot legge la serie cTrader nativa per ciascuna
    ///    coppia simbolo/timeframe configurata e inoltra al server ogni sua barra chiusa non ancora
    ///    trasmessa; non aggrega mai artificialmente barre 5 minuti;
    ///  - fa polling periodico chiedendo al server "qual è il prossimo segnale per il MIO account";
    ///  - apre e chiude posizioni su QUALSIASI simbolo configurato (non solo quello del grafico);
    ///  - si autolimita PER SIMBOLO: mentre ha una posizione aperta su un simbolo non ne chiede/accetta una
    ///    seconda sullo stesso simbolo, ma può gestire in parallelo posizioni su simboli diversi;
    ///  - le condizioni di uscita (Stop Loss, Take Profit, ed eventuale scadenza a tempo CloseAtUtc) sono
    ///    contenute nel segnale di ingresso e vengono gestite interamente dal cBot: SL/TP sono impostati
    ///    come livelli nativi sull'ordine (li applica il broker), CloseAtUtc viene sorvegliato localmente
    ///    ad ogni OnBar, come il limite di barre MaxBarsInPosition. Il server NON invia mai segnali di
    ///    chiusura: le strategie che deciderebbero l'uscita a runtime sono escluse dal catalogo;
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
    /// live sia in backtest (dove serve comunque abilitare il backtesting multi-simbolo/multi-timeframe in
    /// cTrader se si configurano strumenti diversi da quello del grafico).
    ///
    /// Un'istanza di questo cBot rappresenta UN account cTrader. Il codice piano risolve la sessione e
    /// il profilo account/gruppo; cBot di account diversi possono condividere la sessione del piano.
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooLiveTradingBot : Robot
    {
        private const string LabelPrefix = "PiootooLive";

        [Parameter("Server Base Url", DefaultValue = "https://localhost:7116")]
        public string ServerBaseUrl { get; set; }

        [Parameter("Codice piano")]
        public string PlanCode { get; set; }

        [Parameter("Timeframe base del grafico (minuti)", DefaultValue = 5, MinValue = 1)]
        public int BaseTimeframeMinutes { get; set; }

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
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private sealed class Pair
        {
            public string Symbol;
            public int TimeframeMinutes;
            public Bars Series;
            public DateTime? LastPushedBarTimeUtc;
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
            /// <summary>Limite di barre in posizione dichiarato dall'intent di ingresso. 0 = nessun limite.</summary>
            public int MaxBarsInPosition { get; set; }
            /// <summary>Barre trascorse, persistite per non perdere il limite dopo un riavvio.</summary>
            public int BarsInPosition { get; set; }
        }

        private sealed class LocalSessionState
        {
            public string PlanCode { get; set; }
            public string AccountNumber { get; set; }
            public string SessionId { get; set; }
            public List<OpenPositionContext> Positions { get; set; } = new();
        }

        private readonly List<Pair> _pairs = new();

        // Posizioni attualmente aperte da questo bot, per Id posizione cTrader.
        private readonly Dictionary<int, OpenPositionContext> _openPositions = new();

        // Intent già in gestione in questo avvio: evita di ri-eseguire ordini ad ogni poll finché il
        // server non registra l'esito (il poll è idempotente e ripropone lo stesso intent finché Pending).
        private readonly HashSet<string> _submittedIntentIds = new();
        private readonly Dictionary<int, OrderIntentDto> _serverCloseIntents = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(PlanCode))
            {
                Print("Codice piano non impostato.");
                Stop();
                return;
            }

            if (!TimeFrame.Equals(ToTimeFrame(BaseTimeframeMinutes)))
            {
                Print("Il grafico deve usare il timeframe base configurato ({0}m); attuale: {1}.",
                    BaseTimeframeMinutes, TimeFrame);
                Stop();
                return;
            }

            _accountNumber = Account.Number.ToString();

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(1, HttpTimeoutSeconds))
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                Stop();
                return;
            }
            var descriptor = JsonSerializer.Deserialize<TradingSessionDescriptorDto>(ReadBody(openResponse), _json);
            _sessionId = descriptor?.SessionId;
            _sessionToken = descriptor?.SessionToken;
            var pairs = new List<Pair>();
            var error = "descriptor sessione mancante";
            if (descriptor == null ||
                !ParseInstruments(descriptor.InstrumentsConfig, out pairs, out error))
            {
                Print("Configurazione strumenti del piano non valida: {0}", error);
                Stop();
                return;
            }
            _pairs.AddRange(pairs);
            if (!IsBacktesting)
                _localStatePath = BuildLocalStatePath(PlanCode, _accountNumber);

            foreach (var pair in _pairs)
                pair.Series = MarketData.GetBars(ToTimeFrame(pair.TimeframeMinutes), pair.Symbol);

            RestoreLocalState();
            Positions.Closed += OnPositionClosed;
            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, PollingSeconds)));

            Print("Piootoo live bot avviato. Account={0} Session={1} Strumenti={2}",
                _accountNumber, _sessionId, string.Join("; ", _pairs.Select(p => $"{p.Symbol}/{p.TimeframeMinutes}m")));
        }

        protected override void OnBar()
        {
            var pushedStreams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _pairs)
            {
                if (!TryPushClosedBar(pair))
                    continue;

                pushedStreams.Add(MakeStreamKey(pair.Symbol, pair.TimeframeMinutes));
            }

            foreach (var context in _openPositions.Values)
                if (pushedStreams.Contains(MakeStreamKey(context.Symbol, context.TimeframeMinutes)))
                    context.BarsInPosition++;

            // Le barre sono già state pubblicate (la storia del server non deve avere buchi), ma
            // dentro la finestra di flat non si reclama nessun intent: sarebbe un ingresso che
            // HandleEntryIntent scarterebbe comunque, e il polling costa una chiamata.
            if (EnforceWeekEndFlat())
            {
                SaveLocalState();
                return;
            }

            // Il server ha appena valutato tutti gli stream che hanno chiuso una barra: reclamare
            // subito l'eventuale intent, senza aspettare il prossimo polling periodico.
            if (pushedStreams.Count != 0)
                PollNextSignal();

            MoveStopsToBreakEven();
            MoveTrailingStops();
            CloseExpiredPositions();
            SaveLocalState();
        }

        protected override void OnTick()
        {
            // Dentro la finestra di fine settimana non c'e' nulla da proteggere: va solo chiuso.
            if (EnforceWeekEndFlat())
                return;

            // Il prezzo può raggiungere e perdere la soglia all'interno della barra 5m:
            // il break-even va quindi verificato a ogni tick, non solo al bar-close.
            MoveStopsToBreakEven();
            MoveTrailingStops();
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

                string reason = null;
                if (ctx.CloseAtUtc is { } closeAt && closeAt <= nowUtc)
                    reason = "scadenza (CloseAtUtc)";
                else if (ctx.MaxBarsInPosition > 0 && ctx.BarsInPosition >= ctx.MaxBarsInPosition)
                    reason = "limite barre (MaxBarsInPosition)";

                if (reason is null)
                    continue;

                var position = Positions.FirstOrDefault(p => p.Id == kvp.Key);
                if (position is null)
                {
                    _openPositions.Remove(kvp.Key); // già chiusa, per qualche altra via
                    continue;
                }

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

        protected override void OnTimer()
        {
            // Senza questa guardia il polling periodico continuerebbe a reclamare intent nel fine
            // settimana: verrebbero scartati da HandleEntryIntent, ma a costo di una chiamata ognuno.
            if (EnforceWeekEndFlat())
                return;

            PollNextSignal();
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
            Positions.Closed -= OnPositionClosed;
            _http?.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // Invio delle barre chiuse native per ogni coppia simbolo/timeframe configurata
        // ---------------------------------------------------------------------------------------

        private bool TryPushClosedBar(Pair pair)
        {
            try
            {
                var series = pair.Series;
                if (series == null || series.Count < 2)
                    return false;

                // Il [Robot] è agganciato a un grafico con TimeZone=UTC e timeframe=BaseTimeframeMinutes:
                // gli orari della serie sono già in UTC, manca solo il flag Kind. TimeZone è un
                // attributo di compilazione, non l'impostazione di visualizzazione di cTrader, quindi
                // il feed resta UTC comunque sia configurata la piattaforma. Il SpecifyKind non è
                // cosmetico: senza il flag il JSON parte senza il suffisso "Z" e ValidateBar sul
                // server rifiuta la barra. Last(1) è l'ultima candela chiusa, Last(0) quella aperta.
                var nativeClosedBar = series.Last(1);
                var barTimeUtc = DateTime.SpecifyKind(nativeClosedBar.OpenTime, DateTimeKind.Utc);
                if (pair.LastPushedBarTimeUtc == barTimeUtc)
                    return false;

                var closedBar = new ClosedBarDto
                {
                    Symbol = pair.Symbol,
                    TimeframeMinutes = pair.TimeframeMinutes,
                    BarTimeUtc = barTimeUtc,
                    // Sequenza basata sul timestamp: monotona per lo stream a prescindere da quale
                    // account/cBot la invii per primo (più account pushano le stesse barre di mercato).
                    Sequence = (long)(barTimeUtc - DateTime.UnixEpoch).TotalMilliseconds,
                    IdempotencyKey = $"{pair.Symbol}|{pair.TimeframeMinutes}|{barTimeUtc:O}",
                    Bar = new OhlcvDto
                    {
                        DateTime = barTimeUtc,
                        Open = (decimal)nativeClosedBar.Open,
                        High = (decimal)nativeClosedBar.High,
                        Low = (decimal)nativeClosedBar.Low,
                        Close = (decimal)nativeClosedBar.Close,
                        Volume = (decimal)nativeClosedBar.TickVolume
                    }
                };

                var request = new PushBarsRequestDto
                {
                    SessionId = _sessionId,
                    SessionToken = _sessionToken,
                    Bars = new[] { closedBar }
                };

                var response = PostJson($"api/v1/trading-sessions/{_sessionId}/bars", request);
                if (!response.IsSuccessStatusCode)
                {
                    if (VerboseLogging)
                        Print("Push barra {0}/{1}m fallito: {2}", pair.Symbol, pair.TimeframeMinutes, ReadError(response));
                    return false;
                }

                pair.LastPushedBarTimeUtc = barTimeUtc;
                return true;
            }
            catch (Exception ex)
            {
                Print("Errore invio barra {0}/{1}m: {2}", pair.Symbol, pair.TimeframeMinutes, ex.Message);
                return false;
            }
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
                        .Where(p => p.Label != null &&
                                    p.Label.StartsWith(LabelPrefix + ":", StringComparison.Ordinal))
                        .Select(p => new BrokerPositionSnapshotDto
                        {
                            PositionId = p.Id.ToString(),
                            Symbol = p.SymbolName,
                            StrategyCode = p.Label.Substring(LabelPrefix.Length + 1)
                        })
                        .ToList(),
                    Orders = PendingOrders
                        .Where(o => o.Label != null &&
                                    o.Label.StartsWith(LabelPrefix + ":", StringComparison.Ordinal))
                        .Select(o => new BrokerOrderSnapshotDto
                        {
                            OrderId = o.Id.ToString(),
                            Symbol = o.SymbolName,
                            StrategyCode = o.Label.Substring(LabelPrefix.Length + 1)
                        })
                        .ToList(),
                    Trades = History
                        .Where(t => t.Label != null &&
                                    t.Label.StartsWith(LabelPrefix + ":", StringComparison.Ordinal) &&
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
                    return;
                }

                var body = ReadBody(response);
                var payload = JsonSerializer.Deserialize<AccountSignalResponseDto>(body, _json);
                if (payload?.Intent is null)
                {
                    if (VerboseLogging && payload?.Reason != null) Print("Nessuna azione: {0}", payload.Reason);
                    return;
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

            // Autolimitazione lato client PER SIMBOLO (oltre a quella già garantita dal server): se il bot
            // ha già una posizione aperta su QUESTO simbolo, non ne apre una seconda; può però tradare in
            // parallelo altri simboli configurati.
            var alreadyOpenOnSymbol = Positions.Any(p =>
                p.SymbolName.Equals(intent.Symbol, StringComparison.OrdinalIgnoreCase) &&
                p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal));
            if (alreadyOpenOnSymbol)
            {
                if (VerboseLogging)
                    Print("Ingresso {0}/{1} ignorato: il bot ha già una posizione aperta su questo simbolo.", intent.Symbol, intent.StrategyCode);
                return;
            }

            var symbol = Symbols.GetSymbol(intent.Symbol);
            if (symbol is null)
            {
                Print("Simbolo '{0}' non disponibile/non abilitato su questo account: ingresso {1} scartato.", intent.Symbol, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            if (MaxEntrySlippagePips > 0 && intent.Price > 0)
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
            var label = MakeLabel(intent.StrategyCode);

            // Stop Loss/Take Profit del segnale applicati come livelli nativi sull'ordine: li gestisce
            // il broker; l'eventuale chiusura risultante viene comunque intercettata e riportata al
            // server da OnPositionClosed (vedi nota in testa al file).
            var stopLossPips = ToPips(symbol, intent.StopLoss);
            var takeProfitPips = ToPips(symbol, intent.TakeProfit);

            var result = ExecuteMarketOrder(tradeType, intent.Symbol, volume, label, stopLossPips, takeProfitPips, intent.Reason);
            if (!result.IsSuccessful || result.Position is null)
            {
                Print("Errore apertura posizione {0}/{1}: {2}", intent.Symbol, intent.StrategyCode, result.Error);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                _submittedIntentIds.Remove(intent.IntentId);
                return;
            }

            _openPositions[result.Position.Id] = new OpenPositionContext
            {
                PositionId = result.Position.Id,
                EntryIntentId = intent.IntentId,
                StrategyCode = intent.StrategyCode,
                Symbol = intent.Symbol,
                TimeframeMinutes = intent.TimeframeMinutes,
                BreakEven = intent.BreakEven,
                TrailingStop = intent.TrailingStop,
                CloseAtUtc = intent.CloseAtUtc,
                MaxBarsInPosition = intent.MaxBarsInPosition ?? 0,
                BarsInPosition = 0
            };
            SaveLocalState();

            ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Filled,
                (decimal)result.Position.VolumeInUnits, (decimal)result.Position.EntryPrice, result.Position.Id.ToString());
        }

        private void HandleCloseIntent(OrderIntentDto intent)
        {
            var position = Positions.FirstOrDefault(candidate =>
                candidate.SymbolName.Equals(intent.Symbol, StringComparison.OrdinalIgnoreCase) &&
                candidate.Label == MakeLabel(intent.StrategyCode));
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

                var closeIntent = JsonSerializer.Deserialize<OrderIntentDto>(ReadBody(response), _json);
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
                                       position.Label.StartsWith(LabelPrefix + ":", StringComparison.Ordinal))
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

        private static bool ParseInstruments(string config, out List<Pair> pairs, out string error)
        {
            pairs = new List<Pair>();
            error = null;
            if (string.IsNullOrWhiteSpace(config))
            {
                error = "nessuno strumento configurato.";
                return false;
            }

            foreach (var entry in config.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
                {
                    error = $"voce non valida '{entry}' (atteso SIMBOLO:tf1,tf2,...).";
                    return false;
                }

                var symbol = parts[0];
                foreach (var tfText in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!int.TryParse(tfText, out var tf) || tf <= 0)
                    {
                        error = $"timeframe non valido '{tfText}' per {symbol}.";
                        return false;
                    }
                    pairs.Add(new Pair { Symbol = symbol, TimeframeMinutes = tf });
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

        private static string MakeLabel(string strategyCode) => $"{LabelPrefix}:{strategyCode}";

        private static string MakeStreamKey(string symbol, int timeframeMinutes) =>
            $"{symbol.Trim().TrimStart('@').ToUpperInvariant()}|{timeframeMinutes}";

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

        private sealed class ClosedBarDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public DateTime BarTimeUtc { get; set; }
            public long Sequence { get; set; }
            public string IdempotencyKey { get; set; }
            public OhlcvDto Bar { get; set; }
        }

        private sealed class PushBarsRequestDto
        {
            public string SessionId { get; set; }
            public string SessionToken { get; set; }
            public IReadOnlyList<ClosedBarDto> Bars { get; set; }
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
            public string InstrumentsConfig => string.Join(";",
                (Instruments ?? Array.Empty<TradingInstrumentDto>()).Select(instrument =>
                    $"{instrument.Symbol}:{string.Join(",", instrument.TimeframesMinutes ?? Array.Empty<int>())}"));
        }

        private sealed class TradingInstrumentDto
        {
            public string Symbol { get; set; }
            public IReadOnlyList<int> TimeframesMinutes { get; set; }
        }

        private sealed class OrderIntentDto
        {
            public string IntentId { get; set; }
            public string StrategyCode { get; set; }
            public string Symbol { get; set; }
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
