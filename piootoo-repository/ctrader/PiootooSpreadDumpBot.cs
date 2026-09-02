using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using cAlgo.API;
using Directory = System.IO.Directory;
using File = System.IO.File;
using HttpMethod = System.Net.Http.HttpMethod;
using Path = System.IO.Path;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot MISURATORE DI SPREAD. Di un piano legge solo quali strumenti tocca — come fa
    /// <c>PiootooDatafeedSyncBot</c>, e con lo stesso parametro <c>Codice piano</c> — poi per ogni
    /// simbolo scarica i tick di una finestra (un mese di default) e li riversa in un <b>CSV per
    /// simbolo</b> con una riga per tick: istante UTC, bid, ask, spread in prezzo e spread in pip.
    ///
    /// <para><b>Perche' esiste.</b> Il costo di transazione non e' un parametro del backtest: e' una
    /// misura, e cambia per simbolo, per ora del giorno e per broker. Le barre non lo contengono —
    /// il feed e' bid o mid — quindi l'unico modo di conoscerlo e' guardare i tick del conto vero.
    /// Questo bot produce il dato grezzo, non la conclusione: chi vuole lo spread medio della
    /// finestra di trading di una strategia se lo calcola dal CSV, dove ogni riga ha il proprio
    /// istante.</para>
    ///
    /// <para><b>Cosa NON fa.</b> Non apre posizioni, non apre sessioni e non spedisce niente al
    /// server: al server chiede soltanto l'elenco degli strumenti del piano
    /// (<c>GET api/datafeed-external/plan-instruments</c>), che e' una lettura pura. Non scrive
    /// nemmeno in <c>datafeed-external/</c>: quello e' il feed di barre e tick del repository,
    /// scritto dal raccoglitore attraverso il server, e non va contaminato con file di misura. Qui
    /// i CSV finiscono nella cartella di output del bot.</para>
    ///
    /// <para><b>Un simbolo alla volta, e per una ragione.</b> Un mese di tick di uno strumento
    /// liquido sono milioni di righe, e la serie tick sta in RAM per intero mentre la si carica.
    /// Il bot quindi non lavora a giro: prende un simbolo, lo carica a blocchi corti restituendo il
    /// thread alla piattaforma fra un blocco e l'altro, lo scrive a fette, e solo allora passa al
    /// successivo. Con venti simboli in parallelo la piattaforma finirebbe la memoria a metà del
    /// primo mese, e morendo non lascerebbe nemmeno un CSV.</para>
    ///
    /// <para><b>Prima il downloader.</b> Se la cache di cTrader non ha ancora quei tick, il primo
    /// giro li deve tirare giu' dal broker e ci mette molto. Conviene lanciare prima
    /// <c>PiootooTickDownloaderBot</c> sulla stessa finestra: fa solo quello, e a quel punto qui i
    /// tick si trovano già in cache.</para>
    /// </summary>
    public enum LivelloLogSpread
    {
        /// <summary>Solo avvio, riepiloghi ed errori.</summary>
        Minimo,

        /// <summary>Una riga per blocco caricato e per file chiuso. E' il livello di esercizio.</summary>
        Operativo,

        /// <summary>Anche i singoli giri di caricamento e le fette di scrittura.</summary>
        Diagnostico
    }

    // `partial` per la stessa ragione degli altri bot del repository: cTrader genera una propria
    // dichiarazione della classe e senza questo la build si ferma con CS0260.
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public partial class PiootooSpreadDumpBot : Robot
    {
        // Versione propria e non PiootooVersion: questo bot non tocca il contratto di esecuzione
        // (sessioni, intent, report di fill). Legarlo a quella versione farebbe comparire un finto
        // disallineamento nel log a ogni release del server.
        private const string BotVersion = "1.0.0";

        /// <summary>
        /// Tetto ai giri di <c>LoadMoreHistory</c> in un solo battito di timer. Senza, un simbolo con
        /// storia profonda terrebbe il thread dell'algoritmo occupato per minuti e la piattaforma lo
        /// leggerebbe come un bot piantato.
        /// </summary>
        private const int MaxHistoryLoadsPerTick = 20;

        [Parameter("Server Base Url", DefaultValue = "http://localhost:5142", Group = "Server")]
        public string ServerBaseUrl { get; set; }

        [Parameter("Http Timeout (secondi)", DefaultValue = 60, MinValue = 5, Group = "Server")]
        public int HttpTimeoutSeconds { get; set; }

        /// <summary>
        /// Codice del piano da cui prendere gli strumenti. Valorizzato, <b>vince su
        /// <see cref="SymbolList"/></b>, che viene ignorato: i simboli li dichiara il masterfilter del
        /// workspace del piano, e il nome che ognuno ha su questo conto arriva dalla tabella di
        /// conversione dell'account — cosi' non c'e' niente da mappare a mano. Il codice piano e'
        /// globale: non serve ne' workspace ne' account, e non apre nessuna sessione.
        ///
        /// <para><b>Niente Titano.</b> Gli strumenti vengono dal masterfilter e non dalla rotazione
        /// corrente: lo spread di uno strumento va misurato anche mentre le sue strategie sono
        /// spente, perche' serve a decidere se riaccenderle.</para>
        ///
        /// <para>I timeframe che il piano dichiara per ogni strumento qui non contano: si lavora sui
        /// tick, e i tick non hanno timeframe.</para>
        /// </summary>
        [Parameter("Codice piano (vuoto = usa l'elenco simboli)", DefaultValue = "", Group = "Cosa misurare")]
        public string PlanCode { get; set; }

        /// <summary>
        /// Elenco dei simboli, separati da virgola, usato solo se <see cref="PlanCode"/> e' vuoto. Si
        /// accetta anche la forma <c>NAS100=@NQ</c> del raccoglitore, cosi' lo stesso elenco si
        /// incolla nei due bot: la parte a sinistra e' il nome sul broker, quella a destra il nome
        /// Piootoo con cui si chiama il file. Vuoto = solo il simbolo del grafico.
        /// </summary>
        [Parameter("Simboli (broker[=@PIOOTOO], separati da virgola)", DefaultValue = "", Group = "Cosa misurare")]
        public string SymbolList { get; set; }

        /// <summary>
        /// Codice del broker: entra nel nome dei file, perche' due broker sullo stesso simbolo NON
        /// hanno lo stesso spread — e' proprio il confronto per cui questo bot esiste. Vuoto =
        /// dedotto da <c>Account.BrokerName</c>, con la stessa ripulitura del raccoglitore.
        /// </summary>
        [Parameter("Codice broker (vuoto = dedotto dal conto)", DefaultValue = "", Group = "Cosa misurare")]
        public string BrokerCode { get; set; }

        /// <summary>
        /// Ampiezza della finestra, contata all'indietro da <see cref="EndDateText"/>. Ignorata se si
        /// valorizza <see cref="StartDateText"/>.
        /// </summary>
        [Parameter("Mesi da misurare", DefaultValue = 1, MinValue = 1, MaxValue = 24, Group = "Finestra di date")]
        public int MonthsBack { get; set; }

        /// <summary>Inizio della finestra (yyyy-MM-dd, UTC, incluso). Vuoto = ricavato dai mesi.</summary>
        [Parameter("Data inizio (yyyy-MM-dd, vuoto = usa i mesi)", DefaultValue = "", Group = "Finestra di date")]
        public string StartDateText { get; set; }

        /// <summary>Fine della finestra (yyyy-MM-dd, UTC, giorno incluso). Vuoto = adesso.</summary>
        [Parameter("Data fine (yyyy-MM-dd, vuoto = adesso)", DefaultValue = "", Group = "Finestra di date")]
        public string EndDateText { get; set; }

        /// <summary>
        /// Passo con cui si cammina all'indietro nella storia dei tick. Un giorno, non cinque come per
        /// le barre: un giorno di tick di uno strumento liquido e' gia' centinaia di migliaia di
        /// righe, e il passo serve a dare alla piattaforma un momento di respiro fra un pezzo e
        /// l'altro. Se e' troppo largo, il respiro non arriva mai.
        /// </summary>
        [Parameter("Giorni per blocco", DefaultValue = 1, MinValue = 1, MaxValue = 90, Group = "Ritmo")]
        public int ChunkDays { get; set; }

        [Parameter("Secondi fra due battiti", DefaultValue = 1, MinValue = 1, MaxValue = 60, Group = "Ritmo")]
        public int SecondsBetweenChunks { get; set; }

        /// <summary>
        /// Righe scritte in un solo battito. La scrittura e' a fette per la stessa ragione per cui il
        /// caricamento e' a blocchi: riversare cinque milioni di righe in una volta blocca il thread
        /// dell'algoritmo per decine di secondi.
        /// </summary>
        [Parameter("Righe scritte per battito", DefaultValue = 200000, MinValue = 1000, MaxValue = 5000000, Group = "Ritmo")]
        public int RowsPerTick { get; set; }

        /// <summary>
        /// Tetto di sicurezza sui tick tenuti in memoria per simbolo. Raggiunto, il caricamento di
        /// quel simbolo si ferma, il CSV viene scritto con quello che c'e' e il file dichiara da
        /// quando parte: meglio una finestra coperta a meta' e detta, che cTrader che muore
        /// portandosi via anche il lavoro già fatto.
        /// </summary>
        [Parameter("Tick massimi in memoria per simbolo (milioni)", DefaultValue = 20, MinValue = 1, MaxValue = 500, Group = "Ritmo")]
        public int MaxMillionTicksPerSymbol { get; set; }

        /// <summary>
        /// Cartella dei CSV. Vuoto = <c>%AppData%\PiootooSpreadDump</c>. Non puntarla dentro
        /// <c>piootoo-repository/datafeed-external/</c>: quella cartella e' il feed scritto dal
        /// server, e mescolarci file di misura fa sembrare dati di feed quello che non lo e'.
        /// </summary>
        [Parameter("Cartella di output", DefaultValue = "", Group = "Output")]
        public string OutputFolder { get; set; }

        /// <summary>
        /// Scrive anche <c>spread-summary.csv</c>: una riga per simbolo con conteggio, primo e ultimo
        /// tick, spread medio, minimo e massimo. E' un riassunto, non la misura: gli spread non si
        /// distribuiscono normalmente e la media di un mese intero comprende l'apertura asiatica e le
        /// news. Serve per accorgersi a occhio di un simbolo fuori scala, poi si torna al CSV.
        /// </summary>
        [Parameter("Scrivi il riepilogo per simbolo", DefaultValue = true, Group = "Output")]
        public bool WriteSummary { get; set; }

        [Parameter("Livello di log", DefaultValue = LivelloLogSpread.Operativo, Group = "Diagnostica")]
        public LivelloLogSpread LivelloDiLog { get; set; }

        private HttpClient _http;
        private readonly JsonSerializerOptions _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly List<SpreadStream> _streams = new List<SpreadStream>();

        private string _brokerCode;
        private string _outputFolder;
        private DateTime _windowStartUtc;
        private DateTime _windowEndUtc;
        private DateTime _startedAtUtc;
        private int _current;
        private bool _stopped;

        private bool LogOperativo { get { return LivelloDiLog >= LivelloLogSpread.Operativo; } }
        private bool LogDiagnostico { get { return LivelloDiLog >= LivelloLogSpread.Diagnostico; } }

        // -----------------------------------------------------------------------------------------
        // Avvio
        // -----------------------------------------------------------------------------------------

        protected override void OnStart()
        {
            Print("Piootoo Spread Dump v{0} — un CSV bid/ask/spread per simbolo, niente ordini.", BotVersion);

            // I tick arrivano nel fuso dichiarato dall'attributo [Robot] con Kind Unspecified: prima
            // di etichettarli UTC bisogna essere certi che l'etichetta sia vera. Se qualcuno cambiasse
            // l'attributo, la finestra sarebbe confrontata con orari locali e i CSV nascerebbero
            // spostati di ore senza che niente lo segnali.
            if (Server.Time != Server.TimeInUtc)
            {
                StopWithError(string.Format(
                    "Il robot non sta girando in UTC (Server.Time={0:O}, Server.TimeInUtc={1:O}). " +
                    "L'attributo [Robot(TimeZone = TimeZones.UTC)] e' obbligatorio: gli istanti dei " +
                    "tick verrebbero scritti con un orario falso.",
                    Server.Time, Server.TimeInUtc));
                return;
            }

            _brokerCode = ResolveBrokerCode();
            if (string.IsNullOrEmpty(_brokerCode))
            {
                StopWithError(string.Format(
                    "Codice broker non ricavabile da '{0}': valorizzare a mano il parametro 'Codice broker'.",
                    Account.BrokerName));
                return;
            }

            string windowError;
            if (!TryParseWindow(out windowError))
            {
                StopWithError(windowError);
                return;
            }

            try
            {
                _outputFolder = string.IsNullOrWhiteSpace(OutputFolder)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooSpreadDump")
                    : OutputFolder.Trim();
                Directory.CreateDirectory(_outputFolder);
            }
            catch (Exception failure)
            {
                StopWithError(string.Format("Cartella di output '{0}' non utilizzabile: {1}", OutputFolder, failure.Message));
                return;
            }

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(5, HttpTimeoutSeconds))
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string streamError;
            if (!TryBuildStreams(out streamError))
            {
                StopWithError(streamError);
                return;
            }

            _startedAtUtc = Server.TimeInUtc;
            Print("Broker {0} (conto {1} presso '{2}'). Finestra {3:yyyy-MM-dd} -> {4:yyyy-MM-dd} su {5} simboli, " +
                  "blocchi da {6} giorni. Output in {7}",
                _brokerCode, Account.Number, Account.BrokerName,
                _windowStartUtc, _windowEndUtc, _streams.Count, ChunkDays, _outputFolder);

            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, SecondsBetweenChunks)));
        }

        /// <summary>
        /// Stessa ripulitura del raccoglitore — solo lettere, cifre, <c>-</c> e <c>_</c> in maiuscolo —
        /// perche' i file di questo bot e le cartelle di quello si confrontano a occhio: "IC Markets"
        /// deve diventare <c>ICMARKETS</c> in entrambi.
        /// </summary>
        private string ResolveBrokerCode()
        {
            var source = string.IsNullOrWhiteSpace(BrokerCode) ? Account.BrokerName : BrokerCode;
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            var builder = new StringBuilder(source.Length);
            foreach (var character in source.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                    builder.Append(character);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Giorni di calendario UTC: inizio incluso, fine a mezzanotte del giorno dopo — chi scrive
        /// "2026-08-30" come fine intende avere anche il 30 per intero. Senza date esplicite la
        /// finestra sono gli ultimi <see cref="MonthsBack"/> mesi.
        /// </summary>
        private bool TryParseWindow(out string error)
        {
            error = null;
            _windowEndUtc = Server.TimeInUtc;

            if (!string.IsNullOrWhiteSpace(EndDateText))
            {
                DateTime end;
                if (!TryParseDay(EndDateText, out end))
                {
                    error = string.Format("Data fine '{0}' non valida: attesa nella forma yyyy-MM-dd.", EndDateText);
                    return false;
                }

                _windowEndUtc = end.AddDays(1);
            }

            var now = Server.TimeInUtc;
            if (_windowEndUtc > now)
                _windowEndUtc = now;

            if (string.IsNullOrWhiteSpace(StartDateText))
            {
                _windowStartUtc = _windowEndUtc.AddMonths(-Math.Max(1, MonthsBack));
            }
            else
            {
                DateTime start;
                if (!TryParseDay(StartDateText, out start))
                {
                    error = string.Format("Data inizio '{0}' non valida: attesa nella forma yyyy-MM-dd.", StartDateText);
                    return false;
                }

                _windowStartUtc = start;
            }

            if (_windowEndUtc <= _windowStartUtc)
            {
                error = string.Format(
                    "Finestra vuota: inizio {0:yyyy-MM-dd HH:mm} non precede la fine {1:yyyy-MM-dd HH:mm}.",
                    _windowStartUtc, _windowEndUtc);
                return false;
            }

            return true;
        }

        private static bool TryParseDay(string text, out DateTime day)
        {
            DateTime parsed;
            if (DateTime.TryParseExact(text.Trim(), new[] { "yyyy-MM-dd", "yyyyMMdd", "dd/MM/yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                day = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
                return true;
            }

            day = default(DateTime);
            return false;
        }

        /// <summary>
        /// Apre una serie tick per simbolo. Un simbolo che il broker non conosce non ferma il run: si
        /// segnala e si prosegue, perche' in un elenco di venti uno sbagliato non deve costare la
        /// misura degli altri diciannove.
        /// </summary>
        private bool TryBuildStreams(out string error)
        {
            error = null;

            List<SpreadRequest> requests;
            if (string.IsNullOrWhiteSpace(PlanCode))
                requests = BuildRequestsFromParameters();
            else
                requests = BuildRequestsFromPlan(out error);

            if (requests == null)
                return false;

            foreach (var request in requests)
            {
                Symbol symbol = null;
                try
                {
                    symbol = Symbols.GetSymbol(request.BrokerSymbol);
                }
                catch (Exception failure)
                {
                    Print("Simbolo '{0}' non disponibile su questo account: {1}. Saltato.", request.BrokerSymbol, failure.Message);
                    continue;
                }

                if (symbol == null)
                {
                    Print("Simbolo '{0}' non disponibile su questo account. Saltato.", request.BrokerSymbol);
                    continue;
                }

                var series = MarketData.GetTicks(symbol.Name);
                if (series == null)
                {
                    Print("Serie tick di '{0}' non disponibile. Saltato.", symbol.Name);
                    continue;
                }

                _streams.Add(new SpreadStream
                {
                    BrokerSymbol = symbol.Name,
                    PiootooSymbol = request.PiootooSymbol,
                    Digits = symbol.Digits,
                    PipSize = symbol.PipSize,
                    Series = series,
                    CursorEndUtc = _windowEndUtc
                });
            }

            if (_streams.Count == 0)
            {
                error = string.IsNullOrWhiteSpace(PlanCode)
                    ? "Nessun simbolo valido: controllare l'elenco."
                    : string.Format("Nessun simbolo valido: nessuno strumento del piano '{0}' e' " +
                                    "disponibile su questo account.", PlanCode);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gli strumenti li dichiara il PIANO: si chiedono al server, che li ricava dal masterfilter
        /// del workspace e li restituisce già con il nome che hanno su questo conto. E' la stessa
        /// chiamata del raccoglitore, cosi' i due bot misurano e raccolgono esattamente lo stesso
        /// insieme di strumenti — due elenchi separati divergerebbero in silenzio.
        /// </summary>
        private List<SpreadRequest> BuildRequestsFromPlan(out string error)
        {
            error = null;

            if (!string.IsNullOrWhiteSpace(SymbolList))
                Print("Codice piano impostato: 'Simboli' viene IGNORATO, gli strumenti li dichiara il piano '{0}'.", PlanCode);

            var uri = string.Format("api/datafeed-external/plan-instruments?planCode={0}&accountNumber={1}",
                Uri.EscapeDataString(PlanCode.Trim()), Uri.EscapeDataString(Account.Number.ToString()));

            PlanInstrumentsDto plan;
            try
            {
                using (var response = _http.Send(new HttpRequestMessage(HttpMethod.Get, uri)))
                {
                    var body = ReadBody(response);
                    if (!response.IsSuccessStatusCode)
                    {
                        error = string.Format("Strumenti del piano '{0}' non ottenibili: {1} {2}",
                            PlanCode, (int)response.StatusCode, Truncate(body, 300));
                        return null;
                    }

                    plan = JsonSerializer.Deserialize<PlanInstrumentsDto>(body, _json);
                }
            }
            catch (Exception failure)
            {
                error = string.Format("Strumenti del piano '{0}' non ottenibili: {1}", PlanCode, failure.Message);
                return null;
            }

            if (plan == null || plan.Instruments == null || plan.Instruments.Count == 0)
            {
                error = string.Format("Il piano '{0}' non dichiara alcuno strumento.", PlanCode);
                return null;
            }

            var requests = new List<SpreadRequest>();
            foreach (var instrument in plan.Instruments)
            {
                if (string.IsNullOrWhiteSpace(instrument.Symbol))
                    continue;

                var brokerSymbol = string.IsNullOrWhiteSpace(instrument.AccountSymbol)
                    ? instrument.Symbol
                    : instrument.AccountSymbol;

                // Lo stesso strumento compare una volta per timeframe nel masterfilter: qui i
                // timeframe non contano e la serie tick e' una sola, quindi si deduplica.
                if (requests.Any(existing => string.Equals(existing.BrokerSymbol, brokerSymbol, StringComparison.OrdinalIgnoreCase)))
                    continue;

                requests.Add(new SpreadRequest
                {
                    BrokerSymbol = brokerSymbol,
                    PiootooSymbol = NormalizePiootooSymbol(instrument.Symbol)
                });
            }

            Print("Piano '{0}' ({1}), workspace '{2}', conto {3}: {4} simboli distinti dal masterfilter.",
                plan.PlanCode, plan.PlanName, plan.WorkspaceId, plan.AccountNumber, requests.Count);
            foreach (var request in requests)
                Print("   {0} -> {1}", request.BrokerSymbol, request.PiootooSymbol);

            return requests;
        }

        /// <summary>I simboli li dichiara il parametro, nella stessa forma del raccoglitore.</summary>
        private List<SpreadRequest> BuildRequestsFromParameters()
        {
            var requests = new List<SpreadRequest>();

            foreach (var piece in (SymbolList ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var entry = piece.Trim();
                if (entry.Length == 0)
                    continue;

                string brokerName, piootooSymbol;
                var separator = entry.IndexOf('=');
                if (separator > 0)
                {
                    brokerName = entry.Substring(0, separator).Trim();
                    piootooSymbol = entry.Substring(separator + 1).Trim();
                }
                else
                {
                    brokerName = entry;
                    piootooSymbol = entry;
                }

                if (requests.Any(existing => string.Equals(existing.BrokerSymbol, brokerName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                requests.Add(new SpreadRequest
                {
                    BrokerSymbol = brokerName,
                    PiootooSymbol = NormalizePiootooSymbol(piootooSymbol)
                });
            }

            if (requests.Count == 0)
            {
                requests.Add(new SpreadRequest
                {
                    BrokerSymbol = SymbolName,
                    PiootooSymbol = NormalizePiootooSymbol(SymbolName)
                });
            }

            return requests;
        }

        private static string NormalizePiootooSymbol(string symbol)
        {
            return "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();
        }

        // -----------------------------------------------------------------------------------------
        // Ciclo: un simbolo alla volta, un'unita' di lavoro per battito
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Ogni battito fa <b>una</b> cosa sola sul simbolo corrente — un blocco di caricamento oppure
        /// una fetta di scrittura — e poi restituisce il thread alla piattaforma. E' tutta qui la
        /// tecnica: il lavoro arriva comunque in fondo, ma nessun singolo passo e' abbastanza lungo da
        /// far sembrare il bot piantato o da mandare in timeout la richiesta di storia.
        /// </summary>
        protected override void OnTimer()
        {
            if (_stopped)
                return;

            while (_current < _streams.Count && _streams[_current].Stage == Stage.Fatto)
                _current++;

            if (_current >= _streams.Count)
            {
                Finish();
                return;
            }

            var stream = _streams[_current];
            if (stream.Stage == Stage.Caricamento)
                LoadChunk(stream);
            else
                WriteSlice(stream);
        }

        /// <summary>
        /// Estende la serie tick all'indietro di un blocco. E' il verso in cui il broker consegna la
        /// storia: <c>LoadMoreHistory</c> allunga la serie verso il passato, non verso il presente.
        /// </summary>
        private void LoadChunk(SpreadStream stream)
        {
            var maxTicks = (long)MaxMillionTicksPerSymbol * 1_000_000L;
            var loads = 0;

            while (true)
            {
                var chunkStart = stream.CursorEndUtc.AddDays(-ChunkDays);
                if (chunkStart < _windowStartUtc)
                    chunkStart = _windowStartUtc;

                if (stream.Series.Count > 0 && Oldest(stream) <= chunkStart)
                {
                    // Blocco già coperto: succede a ogni riavvio, perche' la cache di cTrader tiene i
                    // tick già scaricati. Si avanza il cursore e si RESTA nel giro: un battito per
                    // giorno solo per riconoscere dati che ci sono già vorrebbe dire mezzo minuto di
                    // bot fermo su una finestra di un mese.
                    stream.CursorEndUtc = chunkStart;

                    // Un blocco arrivato dal broker si annuncia al livello di esercizio; uno che era
                    // già in cache no, altrimenti su una finestra lunga il log e' solo quello.
                    if (loads > 0 ? LogOperativo : LogDiagnostico)
                        Print("{0}: storia fino a {1:yyyy-MM-dd HH:mm} — {2} tick in memoria.",
                            stream, Oldest(stream), stream.Series.Count);

                    if (stream.CursorEndUtc <= _windowStartUtc)
                    {
                        if (LogOperativo)
                            Print("{0}: finestra coperta, {1} tick in memoria dal {2:yyyy-MM-dd HH:mm}.",
                                stream, stream.Series.Count, Oldest(stream));

                        stream.Stage = Stage.Scrittura;
                        return;
                    }

                    continue;
                }

                if (stream.Series.Count >= maxTicks)
                {
                    Print("{0}: raggiunto il tetto di {1} milioni di tick in memoria — la finestra e' " +
                          "coperta solo da {2:yyyy-MM-dd HH:mm}. Il CSV viene scritto con quello che c'e'.",
                        stream, MaxMillionTicksPerSymbol, Oldest(stream));
                    stream.Truncated = true;
                    stream.Stage = Stage.Scrittura;
                    return;
                }

                if (loads >= MaxHistoryLoadsPerTick)
                    return; // freno: si riprende dallo stesso punto al prossimo battito

                loads++;
                int loaded;
                try
                {
                    loaded = stream.Series.LoadMoreHistory();
                }
                catch (Exception failure)
                {
                    // Un errore di caricamento non deve costare la misura degli altri simboli: si
                    // scrive quello che si e' preso e si passa avanti.
                    Print("{0}: caricamento fallito ({1}). Si scrive quello che c'e'.", stream, failure.Message);
                    stream.Truncated = true;
                    stream.Stage = Stage.Scrittura;
                    return;
                }

                if (loaded <= 0)
                {
                    if (LogOperativo)
                        Print("{0}: il broker non ha tick prima di {1}.", stream,
                            stream.Series.Count > 0
                                ? Oldest(stream).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                                : "mai (nessun tick)");

                    stream.Stage = Stage.Scrittura;
                    return;
                }

                stream.Loaded += loaded;
                if (LogDiagnostico)
                    Print("{0}: +{1} tick (totale {2}, il piu' vecchio {3:yyyy-MM-dd HH:mm:ss}).",
                        stream, loaded, stream.Series.Count, Oldest(stream));
            }
        }

        /// <summary>
        /// Scrive una fetta di righe. Il file resta aperto fra un battito e l'altro: chiuderlo e
        /// riaprirlo a ogni fetta significherebbe un fsync ogni duecentomila righe su un file che
        /// nessuno legge finche' il bot non ha finito.
        /// </summary>
        private void WriteSlice(SpreadStream stream)
        {
            if (stream.Writer == null && !OpenWriter(stream))
                return;

            var written = 0;
            while (stream.Cursor < stream.Series.Count && written < RowsPerTick)
            {
                var tick = stream.Series[stream.Cursor++];
                var time = DateTime.SpecifyKind(tick.Time, DateTimeKind.Utc);

                if (time < _windowStartUtc)
                    continue;

                if (time >= _windowEndUtc)
                {
                    // La serie e' ordinata: oltre la fine finestra non c'e' piu' niente da scrivere.
                    stream.Cursor = stream.Series.Count;
                    break;
                }

                var spread = tick.Ask - tick.Bid;

                stream.Writer.Write(time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
                stream.Writer.Write(',');
                stream.Writer.Write(tick.Bid.ToString(stream.PriceFormat, CultureInfo.InvariantCulture));
                stream.Writer.Write(',');
                stream.Writer.Write(tick.Ask.ToString(stream.PriceFormat, CultureInfo.InvariantCulture));
                stream.Writer.Write(',');
                stream.Writer.Write(spread.ToString(stream.PriceFormat, CultureInfo.InvariantCulture));
                stream.Writer.Write(',');
                stream.Writer.Write((stream.PipSize > 0 ? spread / stream.PipSize : 0d).ToString("F2", CultureInfo.InvariantCulture));
                stream.Writer.Write('\n');

                stream.Rows++;
                written++;
                stream.SpreadSum += spread;
                if (spread < stream.SpreadMin) stream.SpreadMin = spread;
                if (spread > stream.SpreadMax) stream.SpreadMax = spread;
                if (spread <= 0) stream.NonPositiveSpreads++;
                if (stream.FirstRowUtc == null) stream.FirstRowUtc = time;
                stream.LastRowUtc = time;
            }

            if (LogDiagnostico && written > 0)
                Print("{0}: scritte {1} righe (totale {2}).", stream, written, stream.Rows);

            if (stream.Cursor >= stream.Series.Count)
                CloseWriter(stream);
        }

        private bool OpenWriter(SpreadStream stream)
        {
            stream.Path = Path.Combine(_outputFolder, string.Format(CultureInfo.InvariantCulture,
                "{0}_{1}_{2:yyyyMMdd}-{3:yyyyMMdd}.csv",
                _brokerCode, stream.PiootooSymbol.TrimStart('@'), _windowStartUtc, _windowEndUtc.AddDays(-1)));

            try
            {
                stream.Writer = new StreamWriter(stream.Path, false, new UTF8Encoding(false), 1 << 20);
            }
            catch (Exception failure)
            {
                Print("{0}: impossibile scrivere '{1}': {2}. Simbolo saltato.", stream, stream.Path, failure.Message);
                stream.Stage = Stage.Fatto;
                stream.Note = "file non scrivibile";
                return false;
            }

            // Intestazione commentata prima di quella vera: il CSV deve dire da solo di che broker,
            // simbolo e finestra parla, perche' un file di spread senza broker non significa niente e
            // il nome del file lo si perde alla prima copia.
            stream.Writer.Write(string.Format(CultureInfo.InvariantCulture,
                "# Piootoo Spread Dump v{0} — broker {1} (conto {2}), simbolo {3} ({4} sul broker)\n" +
                "# Finestra UTC {5:yyyy-MM-ddTHH:mm:ssZ} -> {6:yyyy-MM-ddTHH:mm:ssZ}, pipSize {7}, digits {8}\n" +
                "# spread = ask - bid in prezzo; spreadPips = spread / pipSize\n",
                BotVersion, _brokerCode, Account.Number, stream.PiootooSymbol, stream.BrokerSymbol,
                _windowStartUtc, _windowEndUtc,
                stream.PipSize.ToString(CultureInfo.InvariantCulture), stream.Digits));
            stream.Writer.Write("timeUtc,bid,ask,spread,spreadPips\n");

            return true;
        }

        private void CloseWriter(SpreadStream stream)
        {
            try
            {
                if (stream.Writer != null)
                {
                    stream.Writer.Flush();
                    stream.Writer.Dispose();
                }
            }
            catch (Exception failure)
            {
                Print("{0}: chiusura del file fallita: {1}", stream, failure.Message);
            }

            stream.Writer = null;
            stream.Stage = Stage.Fatto;

            if (stream.Rows == 0)
            {
                // Un CSV con la sola intestazione e' un risultato, non un errore: dice che in quella
                // finestra il broker non ha consegnato tick per quel simbolo. Va detto a voce, perche'
                // a colpo d'occhio un file da 300 byte sembra un file scritto.
                Print("{0}: NESSUN tick nella finestra. Il file contiene solo l'intestazione ({1}).",
                    stream, stream.Path);
                stream.Note = "nessun tick nella finestra";
                return;
            }

            Print("{0}: {1} righe da {2:yyyy-MM-dd HH:mm} a {3:yyyy-MM-dd HH:mm}. " +
                  "Spread medio {4}, min {5}, max {6}{7}. File: {8}",
                stream, stream.Rows, stream.FirstRowUtc, stream.LastRowUtc,
                Format(stream.SpreadSum / stream.Rows, stream.PriceFormat),
                Format(stream.SpreadMin, stream.PriceFormat),
                Format(stream.SpreadMax, stream.PriceFormat),
                stream.NonPositiveSpreads > 0
                    ? string.Format(CultureInfo.InvariantCulture, " — ATTENZIONE: {0} tick con spread <= 0", stream.NonPositiveSpreads)
                    : string.Empty,
                stream.Path);
        }

        // -----------------------------------------------------------------------------------------
        // Chiusura
        // -----------------------------------------------------------------------------------------

        private void Finish()
        {
            if (WriteSummary)
                WriteSummaryFile();

            Report();
            Print("Misura completata. Il bot si ferma.");
            _stopped = true;
            Stop();
        }

        private void WriteSummaryFile()
        {
            var path = Path.Combine(_outputFolder, string.Format(CultureInfo.InvariantCulture,
                "{0}_spread-summary_{1:yyyyMMdd}-{2:yyyyMMdd}.csv",
                _brokerCode, _windowStartUtc, _windowEndUtc.AddDays(-1)));

            try
            {
                var text = new StringBuilder();
                text.Append("broker,symbol,brokerSymbol,ticks,firstTickUtc,lastTickUtc,")
                    .Append("avgSpread,minSpread,maxSpread,avgSpreadPips,nonPositiveSpreads,truncated\n");

                foreach (var stream in _streams)
                {
                    var average = stream.Rows > 0 ? stream.SpreadSum / stream.Rows : 0d;
                    text.Append(_brokerCode).Append(',')
                        .Append(stream.PiootooSymbol).Append(',')
                        .Append(stream.BrokerSymbol).Append(',')
                        .Append(stream.Rows.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(stream.FirstRowUtc.HasValue ? stream.FirstRowUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : string.Empty).Append(',')
                        .Append(stream.LastRowUtc.HasValue ? stream.LastRowUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : string.Empty).Append(',')
                        .Append(Format(average, stream.PriceFormat)).Append(',')
                        .Append(stream.Rows > 0 ? Format(stream.SpreadMin, stream.PriceFormat) : string.Empty).Append(',')
                        .Append(stream.Rows > 0 ? Format(stream.SpreadMax, stream.PriceFormat) : string.Empty).Append(',')
                        .Append((stream.PipSize > 0 ? average / stream.PipSize : 0d).ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                        .Append(stream.NonPositiveSpreads.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(stream.Truncated ? "true" : "false").Append('\n');
                }

                File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
                Print("Riepilogo scritto in {0}", path);
            }
            catch (Exception failure)
            {
                Print("Riepilogo non scritto: {0}", failure.Message);
            }
        }

        private void Report()
        {
            var elapsed = Server.TimeInUtc - _startedAtUtc;
            Print("--- Riepilogo ({0:hh\\:mm\\:ss}) ---", elapsed);
            foreach (var stream in _streams)
            {
                Print("   {0}: {1} righe su {2} tick scaricati dal broker{3}{4}",
                    stream, stream.Rows, stream.Loaded,
                    stream.Truncated ? " (finestra coperta solo in parte)" : string.Empty,
                    string.IsNullOrEmpty(stream.Note) ? string.Empty : " — " + stream.Note);
            }
        }

        private static string Format(double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static DateTime Oldest(SpreadStream stream)
        {
            return DateTime.SpecifyKind(stream.Series[0].Time, DateTimeKind.Utc);
        }

        private static string ReadBody(HttpResponseMessage response)
        {
            using (var stream = response.Content.ReadAsStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text;

            return text.Substring(0, max) + "...";
        }

        private void StopWithError(string message)
        {
            Print("ERRORE FATALE: {0}", message);
            _stopped = true;
            Stop();
        }

        protected override void OnStop()
        {
            // Spegnimento a metà: i file aperti vanno chiusi, altrimenti l'ultimo megabyte di buffer
            // resta in RAM e il CSV finisce troncato a metà riga.
            foreach (var stream in _streams.Where(candidate => candidate.Writer != null))
            {
                stream.Truncated = true;
                CloseWriter(stream);
            }

            Print("Piootoo Spread Dump fermato. Righe scritte in totale: {0}.", _streams.Sum(stream => (long)stream.Rows));
        }

        // -----------------------------------------------------------------------------------------
        // Stato e DTO
        // -----------------------------------------------------------------------------------------

        private enum Stage
        {
            Caricamento,
            Scrittura,
            Fatto
        }

        /// <summary>Cosa misurare, prima di aprire le serie: da parametri o dal piano.</summary>
        private sealed class SpreadRequest
        {
            public string BrokerSymbol;
            public string PiootooSymbol;
        }

        private sealed class SpreadStream
        {
            public string BrokerSymbol;
            public string PiootooSymbol;
            public int Digits;
            public double PipSize;
            public Ticks Series;

            /// <summary>Fine (esclusa) del prossimo blocco: cammina all'indietro verso l'inizio finestra.</summary>
            public DateTime CursorEndUtc;

            public Stage Stage = Stage.Caricamento;
            public long Loaded;
            public bool Truncated;
            public string Note;

            public StreamWriter Writer;
            public string Path;

            /// <summary>Indice della prossima riga da scrivere nella serie tick.</summary>
            public int Cursor;

            public int Rows;
            public double SpreadSum;
            public double SpreadMin = double.MaxValue;
            public double SpreadMax = double.MinValue;
            public int NonPositiveSpreads;
            public DateTime? FirstRowUtc;
            public DateTime? LastRowUtc;

            /// <summary>Prezzi scritti con le cifre decimali dichiarate dal simbolo, non con quelle di
            /// <c>double.ToString()</c>: "1.09" e "1.0900" sono lo stesso prezzo, ma un CSV con un
            /// numero di decimali variabile riga per riga e' illeggibile per chi lo importa.</summary>
            public string PriceFormat { get { return "F" + Digits.ToString(CultureInfo.InvariantCulture); } }

            public override string ToString()
            {
                return PiootooSymbol;
            }
        }

        private sealed class PlanInstrumentDto
        {
            public string Symbol { get; set; }
            public string AccountSymbol { get; set; }
            public List<int> TimeframesMinutes { get; set; }
        }

        private sealed class PlanInstrumentsDto
        {
            public string PlanCode { get; set; }
            public string PlanName { get; set; }
            public string WorkspaceId { get; set; }
            public string AccountNumber { get; set; }
            public List<PlanInstrumentDto> Instruments { get; set; }
        }
    }
}
