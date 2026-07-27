using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using cAlgo.API;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot "live" collegato al trading system Piootoo (api/v1/trading-sessions):
    ///  - il grafico a cui è agganciato il bot deve avere il timeframe = BaseTimeframeMinutes (default 5
    ///    min): ad ogni sua barra chiusa (OnBar) il bot aggiorna un contatore per ciascun simbolo/timeframe
    ///    configurato in "Instruments" e, quando il contatore raggiunge il rapporto timeframe/base, invia
    ///    al server la candela aggregata di quel timeframe (es. XAUUSD 15min viene inviato ogni 3 OnBar);
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
    /// Un'istanza di questo cBot rappresenta UN account cTrader. Per collegare più account allo stesso
    /// gruppo/prop-firm basta usare lo stesso SessionId/SessionToken su più cBot (uno per account) e
    /// configurare Account -> Gruppo nel tab "Trading Session" del client desktop Piootoo.
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooLiveTradingBot : Robot
    {
        private const string LabelPrefix = "PiootooLive";

        [Parameter("Server Base Url", DefaultValue = "https://localhost:7116")]
        public string ServerBaseUrl { get; set; }

        [Parameter("Session Id")]
        public string SessionId { get; set; }

        [Parameter("Session Token")]
        public string SessionToken { get; set; }

        [Parameter("Account Number (vuoto = usa Account.Number)", DefaultValue = "")]
        public string AccountNumberOverride { get; set; }

        [Parameter("Timeframe base del grafico (minuti)", DefaultValue = 5, MinValue = 1)]
        public int BaseTimeframeMinutes { get; set; }

        [Parameter("Strumenti: SIMBOLO:tf1,tf2;SIMBOLO2:tf3,...", DefaultValue = "XAUUSD:5,15;EURUSD:5,60")]
        public string InstrumentsConfig { get; set; }

        [Parameter("Polling segnali (secondi)", DefaultValue = 2, MinValue = 1)]
        public int PollingSeconds { get; set; }

        [Parameter("Max Entry Slippage (Pips)", DefaultValue = 5.0, MinValue = 0)]
        public double MaxEntrySlippagePips { get; set; }

        [Parameter("Http Timeout (secondi)", DefaultValue = 10, MinValue = 1)]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("Log dettagliato", DefaultValue = false)]
        public bool VerboseLogging { get; set; }

        private HttpClient _http;
        private string _accountNumber;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private sealed class Pair
        {
            public string Symbol;
            public int TimeframeMinutes;
            public int TicksNeeded;
            public int TickCounter;
        }

        /// <summary>Contesto di una posizione aperta da questo bot, per il reporting alla chiusura.</summary>
        private sealed class OpenPositionContext
        {
            public string EntryIntentId;
            public string StrategyCode;
            public string Symbol;
            public DateTime? CloseAtUtc;
            /// <summary>Limite di barre in posizione dichiarato dall'intent di ingresso. 0 = nessun limite.</summary>
            public int MaxBarsInPosition;
            /// <summary>Indice di barra all'apertura, per applicare <see cref="MaxBarsInPosition"/>.</summary>
            public int EntryBarIndex;
        }

        // Una serie a timeframe BASE per ciascun simbolo configurato (non per ogni coppia): da qui si
        // aggregano le candele dei timeframe multipli quando il rispettivo contatore scatta.
        private readonly Dictionary<string, Bars> _baseSeriesBySymbol = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Pair> _pairs = new();

        // Posizioni attualmente aperte da questo bot, per Id posizione cTrader.
        private readonly Dictionary<int, OpenPositionContext> _openPositions = new();

        // Intent già in gestione in questo avvio: evita di ri-eseguire ordini ad ogni poll finché il
        // server non registra l'esito (il poll è idempotente e ripropone lo stesso intent finché Pending).
        private readonly HashSet<string> _submittedIntentIds = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(SessionId) || string.IsNullOrWhiteSpace(SessionToken))
            {
                Print("SessionId/SessionToken non impostati.");
                Stop();
                return;
            }

            _accountNumber = string.IsNullOrWhiteSpace(AccountNumberOverride)
                ? Account.Number.ToString()
                : AccountNumberOverride.Trim();

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(1, HttpTimeoutSeconds))
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!ParseInstruments(InstrumentsConfig, BaseTimeframeMinutes, out var pairs, out var error))
            {
                Print("Configurazione Instruments non valida: {0}", error);
                Stop();
                return;
            }
            _pairs.AddRange(pairs);

            foreach (var symbol in _pairs.Select(p => p.Symbol).Distinct(StringComparer.OrdinalIgnoreCase))
                _baseSeriesBySymbol[symbol] = MarketData.GetBars(ToTimeFrame(BaseTimeframeMinutes), symbol);

            Positions.Closed += OnPositionClosed;
            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, PollingSeconds)));

            Print("Piootoo live bot avviato. Account={0} Session={1} Strumenti={2}",
                _accountNumber, SessionId, string.Join("; ", _pairs.Select(p => $"{p.Symbol}/{p.TimeframeMinutes}m")));
        }

        protected override void OnBar()
        {
            foreach (var pair in _pairs)
            {
                pair.TickCounter++;
                if (pair.TickCounter < pair.TicksNeeded)
                    continue;
                pair.TickCounter = 0;

                if (!_baseSeriesBySymbol.TryGetValue(pair.Symbol, out var series) || series.Count < pair.TicksNeeded)
                    continue; // storico non ancora sufficiente (es. appena avviato)

                PushAggregatedBar(pair, series);
            }

            CloseExpiredPositions();
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

            var nowUtc = DateTime.SpecifyKind(Server.Time, DateTimeKind.Utc);
            foreach (var kvp in _openPositions.ToArray())
            {
                var ctx = kvp.Value;

                string reason = null;
                if (ctx.CloseAtUtc is { } closeAt && closeAt <= nowUtc)
                    reason = "scadenza (CloseAtUtc)";
                else if (ctx.MaxBarsInPosition > 0 && Bars.Count - ctx.EntryBarIndex >= ctx.MaxBarsInPosition)
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

        protected override void OnTimer() => PollNextSignal();

        protected override void OnStop()
        {
            Timer.Stop();
            Positions.Closed -= OnPositionClosed;
            _http?.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // Invio barre chiuse al server (aggregate dal timeframe base a quello configurato)
        // ---------------------------------------------------------------------------------------

        private void PushAggregatedBar(Pair pair, Bars series)
        {
            try
            {
                var count = pair.TicksNeeded;
                var startIndex = series.Count - count;
                var lastIndex = series.Count - 1;

                double high = series.HighPrices[startIndex];
                double low = series.LowPrices[startIndex];
                decimal volume = 0;
                for (var i = startIndex; i <= lastIndex; i++)
                {
                    high = Math.Max(high, series.HighPrices[i]);
                    low = Math.Min(low, series.LowPrices[i]);
                    volume += (decimal)series.TickVolumes[i];
                }

                // Il [Robot] è agganciato a un grafico con TimeZone=UTC e timeframe=BaseTimeframeMinutes:
                // gli orari della serie sono già in UTC, manca solo il flag Kind.
                var barTimeUtc = DateTime.SpecifyKind(series.OpenTimes[startIndex], DateTimeKind.Utc);
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
                        Open = (decimal)series.OpenPrices[startIndex],
                        High = (decimal)high,
                        Low = (decimal)low,
                        Close = (decimal)series.ClosePrices[lastIndex],
                        Volume = volume
                    }
                };

                var request = new PushBarsRequestDto
                {
                    SessionId = SessionId,
                    SessionToken = SessionToken,
                    Bars = new[] { closedBar }
                };

                var response = PostJson($"api/v1/trading-sessions/{SessionId}/bars", request);
                if (!response.IsSuccessStatusCode && VerboseLogging)
                    Print("Push barra {0}/{1}m fallito: {2}", pair.Symbol, pair.TimeframeMinutes, ReadError(response));
            }
            catch (Exception ex)
            {
                Print("Errore invio barra {0}/{1}m: {2}", pair.Symbol, pair.TimeframeMinutes, ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Polling segnali per il proprio account ed esecuzione
        // ---------------------------------------------------------------------------------------

        private void PollNextSignal()
        {
            try
            {
                var response = _http.Send(BuildRequest(HttpMethod.Post,
                    $"api/v1/trading-sessions/{SessionId}/accounts/{Uri.EscapeDataString(_accountNumber)}/signal"));

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
                    // Un intent di chiusura esiste solo come registrazione di una chiusura gia eseguita
                    // da questo bot: riceverlo dal polling significherebbe che il server ha deciso
                    // un'uscita, cosa non piu prevista.
                    if (VerboseLogging)
                        Print("Intent di chiusura {0} ignorato: le uscite sono gestite in locale con la " +
                              "specifica dell'intent di ingresso.", intent.IntentId);
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
                EntryIntentId = intent.IntentId,
                StrategyCode = intent.StrategyCode,
                Symbol = intent.Symbol,
                CloseAtUtc = intent.CloseAtUtc,
                MaxBarsInPosition = intent.MaxBarsInPosition ?? 0,
                EntryBarIndex = Bars.Count
            };

            ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Filled,
                (decimal)result.Position.VolumeInUnits, (decimal)result.Position.EntryPrice, result.Position.Id.ToString());
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

            var trade = History.LastOrDefault(h => h.PositionId == position.Id);
            var closePrice = (decimal?)trade?.ClosingPrice;
            var quantity = (decimal)(trade?.VolumeInUnits ?? position.VolumeInUnits);
            var commission = (decimal)(trade?.Commissions ?? 0);

            // Canale unico: ogni chiusura è decisa in locale applicando la specifica dell'intent di
            // ingresso (SL/TP nativi del broker, CloseAtUtc, MaxBarsInPosition). La registriamo lato
            // server con intents/close-external e poi riportiamo il fill contro quell'intent, così il
            // trade confluisce in trades.json e alimenta le rotazioni Titano.
            RegisterExternalCloseAndReport(ctx, position, quantity, closePrice, commission, args.Reason.ToString());
        }

        private void RegisterExternalCloseAndReport(
            OpenPositionContext ctx, Position position, decimal quantity, decimal? closePrice, decimal commission, string reason)
        {
            try
            {
                var closeIntentRequest = new CreateExternalCloseIntentRequestDto
                {
                    SessionToken = SessionToken,
                    StrategyCode = ctx.StrategyCode,
                    Symbol = ctx.Symbol,
                    AccountNumber = _accountNumber,
                    Quantity = quantity,
                    Reason = $"LocalExit:{reason}"
                };
                using var request = BuildRequest(HttpMethod.Post, $"api/v1/trading-sessions/{SessionId}/intents/close-external");
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
                    SessionToken = SessionToken,
                    Report = new ExternalExecutionReportDto
                    {
                        ReportId = $"{intentId}-{Guid.NewGuid():N}",
                        IntentId = intentId,
                        ExternalOrderId = externalOrderId,
                        Status = status,
                        CumulativeFilledQuantity = filledQuantity,
                        FillPrice = fillPrice,
                        Commission = commission,
                        EventTimeUtc = DateTime.SpecifyKind(Server.Time, DateTimeKind.Utc)
                    }
                };
                var response = PostJson($"api/v1/trading-sessions/{SessionId}/execution-reports", request);
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

        private static bool ParseInstruments(string config, int baseMinutes, out List<Pair> pairs, out string error)
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
                    if (tf % baseMinutes != 0)
                    {
                        error = $"il timeframe {tf}m di {symbol} non è multiplo del timeframe base ({baseMinutes}m).";
                        return false;
                    }
                    pairs.Add(new Pair { Symbol = symbol, TimeframeMinutes = tf, TicksNeeded = tf / baseMinutes, TickCounter = 0 });
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
            request.Headers.Add("X-Session-Token", SessionToken);
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

        private sealed class OrderIntentDto
        {
            public string IntentId { get; set; }
            public string StrategyCode { get; set; }
            public string Symbol { get; set; }
            public SignalTypeDto Side { get; set; }
            public TradeOrderTypeDto OrderType { get; set; }
            public decimal FinalQuantity { get; set; }
            public decimal Price { get; set; }
            /// <summary>"Entry" oppure "Close". Il server emette solo intent di ingresso.</summary>
            public string Kind { get; set; } = "Entry";
            public bool IsClose { get; set; }
            // Specifica di uscita completa: e' l'unica informazione con cui il bot chiude la posizione.
            public decimal? StopLoss { get; set; }
            public decimal? TakeProfit { get; set; }
            public decimal? BreakEven { get; set; }
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
