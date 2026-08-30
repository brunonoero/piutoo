using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using cAlgo.API;
using HttpMethod = System.Net.Http.HttpMethod;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot RACCOGLITORE: non apre posizioni e non chiede segnali. Di un piano legge solo quali
    /// strumenti tocca. L'unica cosa che fa e' portare al server le barre (e, se richiesto, i tick)
    /// dei simboli che gli si elencano, perche' il server li scriva in
    /// <c>piootoo-repository/datafeed-external/{CODICE-BROKER}/</c> con la stessa convenzione dei
    /// feed del vendor: <c>@NQ_60.json</c>, <c>@ES_15.json</c>. Il codice broker lo deduce dal conto
    /// (<c>Account.BrokerName</c>) e lo si puo' forzare: e' cio' che tiene separati i dati di due
    /// broker, che per lo stesso simbolo NON producono la stessa serie.
    ///
    /// <para><b>Perche' esiste.</b> In sessione <c>ExternalBroker</c> il server non ha datafeed
    /// proprio: la storia e' solo quella che il client gli spinge, e resta in RAM. Il datafeed su
    /// disco e' compito di un bot dedicato — questo — cosi' che raccogliere dati e mandare ordini
    /// restino due mestieri separati: un raccoglitore puo' girare per giorni su venti simboli senza
    /// rischiare di toccare niente di operativo.</para>
    ///
    /// <para><b>Perche' a blocchi.</b> Lo storico di uno strumento sono decine di migliaia di barre e
    /// il broker le consegna poche alla volta (<c>LoadMoreHistory</c>). Caricarle tutte in
    /// <c>OnStart</c> significa bloccare il thread dell'algoritmo per minuti — la piattaforma lo
    /// interpreta come un bot piantato — e un invio unico da centomila barre finisce in timeout
    /// HTTP, lasciando a terra tutto il lavoro fatto. Qui il ciclo e' l'opposto: a ogni battito di
    /// timer si fa <b>un solo blocco</b> (default cinque giorni, al massimo 2000 barre), lo si
    /// spedisce e si passa allo stream successivo. Se il bot muore a meta', quello che e' arrivato
    /// e' gia' sul disco del server, e al riavvio si riprende da dove si era rimasti: la prima cosa
    /// che il bot chiede per ogni stream e' <c>GET status</c>, cioe' "cosa hai gia'".</para>
    ///
    /// <para><b>Cosa NON fa.</b> Non inventa barre e non ricuce buchi: le sovrapposizioni le elimina
    /// il server deduplicando sull'istante di apertura della barra, e i buchi li <i>dichiara</i>
    /// nella status invece di riempirli. Se il broker non ha un periodo, quel periodo resta vuoto e
    /// si vede.</para>
    ///
    /// <para><b>Cosa raccogliere lo si dichiara in due modi.</b> O a mano (<c>Simboli</c> +
    /// <c>Timeframe in minuti</c>), o con un <c>Codice piano</c>: in quel caso le coppie (simbolo,
    /// timeframe) arrivano dal masterfilter del workspace del piano, gia' con il nome che ogni
    /// simbolo ha su questo conto. Il piano vince e i due parametri manuali vengono ignorati —
    /// tenerli entrambi vivi significherebbe due liste destinate a divergere in silenzio.</para>
    ///
    /// <para><b>Finestra di date.</b> <c>Data inizio</c> e <c>Data fine</c> limitano cosa si
    /// raccoglie in questo run. Sono il modo previsto per spezzare un backfill lungo in piu'
    /// sessioni corte — un anno per volta, magari di notte — senza che i pezzi si pestino: quello
    /// che arriva due volte viene contato come duplicato e non riscritto.</para>
    /// </summary>
    public enum LivelloLogSync
    {
        /// <summary>Solo avvio, riepiloghi ed errori.</summary>
        Minimo,

        /// <summary>Una riga per blocco spedito. E' il livello di esercizio.</summary>
        Operativo,

        /// <summary>Tutto, compresi i blocchi saltati perche' gia' presenti sul server.</summary>
        Diagnostico
    }

    // `partial` perche' cTrader genera una propria dichiarazione della classe del cBot: senza,
    // la build si ferma con CS0260. Non cambia niente per chi legge questo file — resta l'unico
    // posto in cui c'e' del codice.
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public partial class PiootooDatafeedSyncBot : Robot
    {
        // Versione PROPRIA, non quella di Piootoo.Shared.PiootooVersion — e il perche' va detto,
        // visto che il bot distribuito invece la segue. Quel numero e' la sintesi del contratto di
        // esecuzione (sessioni, intent, report di fill): muoverlo significa che server e bot
        // operativi devono essere aggiornati insieme. Questo raccoglitore non tocca quel contratto:
        // parla solo con api/datafeed-external, che e' additivo e retrocompatibile per costruzione
        // (i blocchi sono idempotenti). Legarlo alla versione del progetto vorrebbe dire che ogni
        // release del server fa comparire un finto disallineamento nel log di un bot che non e'
        // cambiato — o costringe a ri-deployarlo per niente.
        private const string BotVersion = "1.1.0";

        /// <summary>
        /// Tetto ai giri di <c>LoadMoreHistory</c> in un solo battito di timer. Il broker risponde a
        /// blocchi e senza un tetto un simbolo con storia profonda terrebbe il thread occupato per
        /// minuti: e' esattamente il blocco che questo bot esiste per evitare. Superato il tetto si
        /// molla e si riprende al battito successivo, senza perdere il punto in cui si era.
        /// </summary>
        private const int MaxHistoryLoadsPerChunkAttempt = 40;

        /// <summary>Barre chiuse rispedite a ogni nuova barra a regime: ricuce un invio perso.</summary>
        private const int LiveHealingBars = 3;

        /// <summary>
        /// Blocchi gia' coperti consumati in un solo battito. Serve a scorrere in fretta un periodo
        /// gia' raccolto senza pero' tenere il thread occupato all'infinito su una finestra enorme.
        /// </summary>
        private const int MaxSkipsPerTick = 500;

        [Parameter("Server Base Url", DefaultValue = "http://localhost:5142", Group = "Server")]
        public string ServerBaseUrl { get; set; }

        /// <summary>
        /// Codice del piano da cui prendere gli strumenti. Valorizzato, <b>vince su
        /// <see cref="SymbolList"/> e <see cref="TimeframeList"/></b>, che vengono ignorati: le coppie
        /// (simbolo, timeframe) le dichiara il masterfilter del workspace del piano, e il nome che
        /// ogni simbolo ha su questo conto arriva dalla tabella di conversione dell'account — cosi'
        /// non c'e' niente da mappare a mano.
        ///
        /// <para>Il codice piano e' globale, quindi basta questo: niente workspace, niente account.
        /// E non apre nessuna sessione — un raccoglitore e' una lettura pura e non deve avere
        /// effetti sull'operativita'.</para>
        ///
        /// <para><b>Niente Titano.</b> Gli strumenti vengono dal masterfilter, non dalla rotazione
        /// corrente: il datafeed di uno strumento serve anche mentre le sue strategie sono spente,
        /// altrimenti alla riaccensione mancherebbe la storia della pausa.</para>
        /// </summary>
        [Parameter("Codice piano (vuoto = usa l'elenco simboli)", DefaultValue = "", Group = "Cosa raccogliere")]
        public string PlanCode { get; set; }

        /// <summary>
        /// Elenco dei simboli da raccogliere, separati da virgola. Ignorato se e' valorizzato
        /// <see cref="PlanCode"/>. Ogni voce e' il nome del simbolo
        /// <b>del broker</b>, opzionalmente seguito dal simbolo Piootoo con cui salvarlo:
        /// <c>NAS100=@NQ, XAUUSD=@GC, US500=@ES</c>. Senza mappatura si usa il nome del broker
        /// preceduto da <c>@</c>. Vuoto = solo il simbolo del grafico.
        /// </summary>
        [Parameter("Simboli (broker[=@PIOOTOO], separati da virgola)", DefaultValue = "", Group = "Cosa raccogliere")]
        public string SymbolList { get; set; }

        [Parameter("Timeframe in minuti (separati da virgola)", DefaultValue = "15,60", Group = "Cosa raccogliere")]
        public string TimeframeList { get; set; }

        /// <summary>
        /// Codice del broker: e' la sottocartella in cui il server salva questi feed
        /// (<c>datafeed-external/ICMARKETS/@NQ_60.json</c>). Vuoto = dedotto da
        /// <c>Account.BrokerName</c>, ripulito dei caratteri non ammessi in un nome di cartella.
        ///
        /// <para>Esiste l'override perche' il nome che il broker dichiara non e' un identificatore
        /// stabile: cambia fra conto demo e reale, e fra due server dello stesso broker. Il codice
        /// invece e' il nome di una cartella che contiene anni di storico — se cambia da solo, il
        /// backfill riparte da zero in una cartella nuova e nessuno se ne accorge finche' non manca
        /// meta' feed. Il valore dedotto viene stampato all'avvio proprio per poterlo fissare.</para>
        /// </summary>
        [Parameter("Codice broker (vuoto = dedotto dal conto)", DefaultValue = "", Group = "Cosa raccogliere")]
        public string BrokerCode { get; set; }

        /// <summary>
        /// Inizio della finestra raccolta in questo run (yyyy-MM-dd, UTC, incluso). Vuoto = tutta la
        /// storia che il broker consegna.
        /// </summary>
        [Parameter("Data inizio (yyyy-MM-dd, vuoto = tutta la storia)", DefaultValue = "", Group = "Finestra di date")]
        public string StartDateText { get; set; }

        /// <summary>Fine della finestra (yyyy-MM-dd, UTC, esclusa). Vuoto = adesso.</summary>
        [Parameter("Data fine (yyyy-MM-dd, vuoto = adesso)", DefaultValue = "", Group = "Finestra di date")]
        public string EndDateText { get; set; }

        [Parameter("Giorni per blocco", DefaultValue = 5, MinValue = 1, MaxValue = 3650, Group = "Finestra di date")]
        public int ChunkDays { get; set; }

        [Parameter("Barre massime per invio", DefaultValue = 2000, MinValue = 50, MaxValue = 20000, Group = "Finestra di date")]
        public int MaxBarsPerPost { get; set; }

        /// <summary>
        /// Salta i blocchi che il server dichiara gia' coperti (nessun buco al loro interno). Vale la
        /// pena spegnerlo solo per riscrivere di proposito un periodo che si sospetta sbagliato.
        /// </summary>
        [Parameter("Salta i periodi gia' presenti sul server", DefaultValue = true, Group = "Finestra di date")]
        public bool SkipCovered { get; set; }

        [Parameter("Resta in ascolto dopo il backfill", DefaultValue = true, Group = "Regime")]
        public bool KeepInSync { get; set; }

        [Parameter("Sincronizza i tick", DefaultValue = false, Group = "Tick")]
        public bool SyncTicks { get; set; }

        [Parameter("Tick per invio", DefaultValue = 500, MinValue = 50, MaxValue = 20000, Group = "Tick")]
        public int TicksPerPost { get; set; }

        [Parameter("Secondi massimi fra due invii di tick", DefaultValue = 10, MinValue = 1, Group = "Tick")]
        public int TickFlushSeconds { get; set; }

        [Parameter("Http Timeout (secondi)", DefaultValue = 60, MinValue = 5, Group = "Server")]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("Secondi fra due blocchi", DefaultValue = 1, MinValue = 1, MaxValue = 60, Group = "Server")]
        public int SecondsBetweenChunks { get; set; }

        [Parameter("Livello di log", DefaultValue = LivelloLogSync.Operativo, Group = "Diagnostica")]
        public LivelloLogSync LivelloDiLog { get; set; }

        private HttpClient _http;
        private readonly List<SyncStream> _streams = new List<SyncStream>();
        private readonly Dictionary<Bars, SyncStream> _bySeries = new Dictionary<Bars, SyncStream>();
        private readonly Dictionary<string, List<TickDto>> _tickBuffers =
            new Dictionary<string, List<TickDto>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Symbol, Action<SymbolTickEventArgs>> _tickHandlers =
            new Dictionary<Symbol, Action<SymbolTickEventArgs>>();

        private readonly JsonSerializerOptions _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private string _brokerCode;
        private DateTime _windowStartUtc;
        private DateTime _windowEndUtc;
        private DateTime _lastTickFlushUtc;
        private int _roundRobin;
        private bool _backfillReported;
        private bool _stopped;

        private bool LogOperativo => LivelloDiLog >= LivelloLogSync.Operativo;
        private bool LogDiagnostico => LivelloDiLog >= LivelloLogSync.Diagnostico;

        // -----------------------------------------------------------------------------------------
        // Avvio
        // -----------------------------------------------------------------------------------------

        protected override void OnStart()
        {
            Print("Piootoo Datafeed Sync v{0} — server {1}", BotVersion, ServerBaseUrl);

            // Le barre della piattaforma arrivano nel fuso dichiarato dall'attributo [Robot]. Qui e'
            // UTC, ma il Kind resta Unspecified: prima di spedirle bisogna etichettarle, e prima di
            // etichettarle bisogna essere certi che l'etichetta sia vera. Se qualcuno cambia
            // l'attributo, SpecifyKind trasformerebbe in silenzio un orario locale in "UTC" e il
            // feed nascerebbe sfalsato di un'ora per sempre. Meglio non partire.
            if (Server.Time != Server.TimeInUtc)
            {
                StopWithError(string.Format(
                    "Il robot non sta girando in UTC (Server.Time={0:O}, Server.TimeInUtc={1:O}). " +
                    "L'attributo [Robot(TimeZone = TimeZones.UTC)] e' obbligatorio per questo bot: " +
                    "le barre verrebbero salvate con un orario falso.",
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

            Print("Codice broker: {0} (conto {1} presso '{2}') — i feed andranno in datafeed-external/{0}/.",
                _brokerCode, Account.Number, Account.BrokerName);

            if (!TryParseWindow(out var windowError))
            {
                StopWithError(windowError);
                return;
            }

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(5, HttpTimeoutSeconds))
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!TryBuildStreams(out var streamError))
            {
                StopWithError(streamError);
                return;
            }

            Print("Finestra richiesta: {0:yyyy-MM-dd} -> {1:yyyy-MM-dd} — {2} stream, blocchi da {3} giorni (max {4} barre).",
                _windowStartUtc, _windowEndUtc, _streams.Count, ChunkDays, MaxBarsPerPost);

            if (SyncTicks)
                SubscribeTicks();

            _lastTickFlushUtc = Server.TimeInUtc;
            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, SecondsBetweenChunks)));
        }

        /// <summary>
        /// Il codice broker con cui il server separa le cartelle. Se non e' stato forzato a mano si
        /// deduce da <c>Account.BrokerName</c> tenendo solo lettere e cifre in maiuscolo:
        /// "IC Markets" -> <c>ICMARKETS</c>, "Pepperstone Ltd" -> <c>PEPPERSTONELTD</c>. La stessa
        /// ripulitura la rifa' il server sul valore ricevuto, quindi i due non possono divergere.
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
        /// La finestra e' l'unico posto in cui questo bot interpreta delle date scritte a mano.
        /// Vengono lette come giorni di calendario UTC: inizio incluso, fine esclusa (la fine e'
        /// mezzanotte del giorno indicato + 1, cosi' "fine = oggi" comprende oggi per intero).
        /// </summary>
        private bool TryParseWindow(out string error)
        {
            error = null;
            _windowEndUtc = Server.TimeInUtc;
            _windowStartUtc = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (!string.IsNullOrWhiteSpace(StartDateText))
            {
                if (!TryParseDay(StartDateText, out var start))
                {
                    error = string.Format("Data inizio '{0}' non valida: attesa nella forma yyyy-MM-dd.", StartDateText);
                    return false;
                }

                _windowStartUtc = start;
            }

            if (!string.IsNullOrWhiteSpace(EndDateText))
            {
                if (!TryParseDay(EndDateText, out var end))
                {
                    error = string.Format("Data fine '{0}' non valida: attesa nella forma yyyy-MM-dd.", EndDateText);
                    return false;
                }

                // Fine esclusa a mezzanotte del giorno DOPO: chi scrive "2026-08-30" intende avere
                // anche il 30, non fermarsi alla mezzanotte che lo apre.
                _windowEndUtc = end.AddDays(1);
            }

            var now = Server.TimeInUtc;
            if (_windowEndUtc > now)
                _windowEndUtc = now;

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
        /// Espande l'elenco simboli x l'elenco timeframe. Uno stream che il broker non conosce non
        /// ferma il run: si segnala e si prosegue con gli altri, perche' in un elenco di venti
        /// simboli uno sbagliato non deve costare la raccolta degli altri diciannove.
        /// </summary>
        private bool TryBuildStreams(out string error)
        {
            var requests = string.IsNullOrWhiteSpace(PlanCode)
                ? BuildRequestsFromParameters(out error)
                : BuildRequestsFromPlan(out error);

            if (requests == null)
                return false;

            foreach (var request in requests)
            {
                Symbol brokerSymbol = null;
                try
                {
                    brokerSymbol = Symbols.GetSymbol(request.BrokerSymbol);
                }
                catch (Exception failure)
                {
                    Print("Simbolo '{0}' non disponibile su questo account: {1}. Stream saltato.",
                        request.BrokerSymbol, failure.Message);
                    continue;
                }

                if (brokerSymbol == null)
                {
                    Print("Simbolo '{0}' non disponibile su questo account. Stream saltato.", request.BrokerSymbol);
                    continue;
                }

                foreach (var minutes in request.TimeframesMinutes)
                {
                    TimeFrame timeFrame;
                    if (!TryToTimeFrame(minutes, out timeFrame))
                    {
                        Print("{0}: timeframe {1} minuti non ha un equivalente cTrader. Stream saltato.",
                            request.PiootooSymbol, minutes);
                        continue;
                    }

                    var stream = new SyncStream
                    {
                        BrokerSymbol = brokerSymbol.Name,
                        PiootooSymbol = request.PiootooSymbol,
                        TimeframeMinutes = minutes,
                        Series = MarketData.GetBars(timeFrame, brokerSymbol.Name),
                        CursorEndUtc = _windowEndUtc
                    };

                    if (stream.Series == null)
                    {
                        Print("{0}: serie {1} minuti non disponibile. Stream saltato.", request.PiootooSymbol, minutes);
                        continue;
                    }

                    _streams.Add(stream);
                    _bySeries[stream.Series] = stream;
                    stream.Series.BarOpened += OnSeriesBarOpened;
                }
            }

            if (_streams.Count == 0)
            {
                error = string.IsNullOrWhiteSpace(PlanCode)
                    ? "Nessuno stream valido: controllare l'elenco simboli e i timeframe."
                    : string.Format("Nessuno stream valido: nessuno strumento del piano '{0}' e' " +
                                    "disponibile su questo account.", PlanCode);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gli strumenti li dichiara il PIANO: si chiedono al server, che li ricava dal masterfilter
        /// del workspace e li restituisce già con il nome che hanno su questo conto.
        ///
        /// <para><b>Dal masterfilter, non dalla rotazione Titano.</b> Titano abilita e disabilita
        /// strategie ogni periodo, ma il datafeed di uno strumento serve <i>sempre</i>: anche mentre
        /// è spento, perché quando torna attivo la sua storia deve esserci già. Seguendo la
        /// rotazione, il feed si fermerebbe a ogni disabilitazione e lascerebbe un buco lungo
        /// quanto la pausa.</para>
        ///
        /// <para><b>Nessuna sessione.</b> Il bot distribuito apre una sessione per avere il
        /// descriptor; un raccoglitore no — è una lettura pura, e non deve avere alcun effetto
        /// sull'operatività. Non c'è nemmeno un elenco locale di ripiego: duplicherebbe il
        /// masterfilter e le due liste divergerebbero in silenzio.</para>
        /// </summary>
        private List<StreamRequest> BuildRequestsFromPlan(out string error)
        {
            error = null;

            if (!string.IsNullOrWhiteSpace(SymbolList) || !string.IsNullOrWhiteSpace(TimeframeList))
                Print("Codice piano impostato: 'Simboli' e 'Timeframe in minuti' vengono IGNORATI, " +
                      "gli strumenti li dichiara il piano '{0}'.", PlanCode);

            var uri = string.Format("api/datafeed-external/plan-instruments?planCode={0}&accountNumber={1}",
                Uri.EscapeDataString(PlanCode.Trim()), Uri.EscapeDataString(Account.Number.ToString()));

            PlanInstrumentsDto plan;
            try
            {
                using (var response = _http.Send(BuildRequest(HttpMethod.Get, uri)))
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

            var requests = new List<StreamRequest>();
            foreach (var instrument in plan.Instruments)
            {
                if (string.IsNullOrWhiteSpace(instrument.Symbol) ||
                    instrument.TimeframesMinutes == null || instrument.TimeframesMinutes.Count == 0)
                    continue;

                requests.Add(new StreamRequest
                {
                    // Il nome sul broker lo dichiara il server, dalla tabella di conversione del
                    // conto: e' il motivo per cui con il piano non serve mappare niente a mano.
                    BrokerSymbol = string.IsNullOrWhiteSpace(instrument.AccountSymbol)
                        ? instrument.Symbol
                        : instrument.AccountSymbol,
                    PiootooSymbol = NormalizePiootooSymbol(instrument.Symbol),
                    TimeframesMinutes = instrument.TimeframesMinutes
                });
            }

            Print("Piano '{0}' ({1}), workspace '{2}', conto {3}: {4} strumenti dal masterfilter.",
                plan.PlanCode, plan.PlanName, plan.WorkspaceId, plan.AccountNumber, requests.Count);
            foreach (var request in requests)
                Print("   {0} -> {1} [{2}]", request.BrokerSymbol, request.PiootooSymbol,
                    string.Join(", ", request.TimeframesMinutes));

            return requests;
        }

        /// <summary>Gli strumenti li dichiarano i parametri: elenco simboli x elenco timeframe.</summary>
        private List<StreamRequest> BuildRequestsFromParameters(out string error)
        {
            error = null;

            var timeframes = new List<int>();
            foreach (var piece in (TimeframeList ?? string.Empty).Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int minutes;
                if (!int.TryParse(piece.Trim(), out minutes) || minutes <= 0)
                {
                    error = string.Format("Timeframe '{0}' non valido: attesi minuti interi (es. '15,60,240').", piece);
                    return null;
                }

                if (!TryToTimeFrame(minutes, out _))
                {
                    error = string.Format("Timeframe {0} minuti non ha un equivalente cTrader.", minutes);
                    return null;
                }

                if (!timeframes.Contains(minutes))
                    timeframes.Add(minutes);
            }

            if (timeframes.Count == 0)
            {
                error = "Nessun timeframe indicato: valorizzare 'Timeframe in minuti', " +
                        "oppure impostare 'Codice piano' e lasciare che gli strumenti li dichiari il piano.";
                return null;
            }

            var entries = new List<string>();
            foreach (var piece in (SymbolList ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length > 0)
                    entries.Add(trimmed);
            }

            if (entries.Count == 0)
                entries.Add(SymbolName);

            var requests = new List<StreamRequest>();
            foreach (var entry in entries)
            {
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

                requests.Add(new StreamRequest
                {
                    BrokerSymbol = brokerName,
                    PiootooSymbol = NormalizePiootooSymbol(piootooSymbol),
                    TimeframesMinutes = timeframes
                });
            }

            return requests;
        }

        private static string NormalizePiootooSymbol(string symbol)
        {
            return "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();
        }

        private void SubscribeTicks()
        {
            foreach (var name in _streams.Select(stream => stream.BrokerSymbol).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var symbol = Symbols.GetSymbol(name);
                if (symbol == null || _tickHandlers.ContainsKey(symbol))
                    continue;

                Action<SymbolTickEventArgs> handler = OnSymbolTick;
                _tickHandlers[symbol] = handler;
                symbol.Tick += handler;
                _tickBuffers[PiootooSymbolOf(name)] = new List<TickDto>();
            }

            Print("Raccolta tick attiva su {0} simboli (invio ogni {1} tick o {2} secondi).",
                _tickHandlers.Count, TicksPerPost, TickFlushSeconds);
        }

        // -----------------------------------------------------------------------------------------
        // Ciclo: un'unita' di lavoro per battito
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Il cuore del bot. Ogni battito fa <b>una</b> cosa sola — una status, oppure un blocco di
        /// barre, oppure uno svuotamento del buffer tick — e poi restituisce il thread alla
        /// piattaforma. E' quello che tiene il bot reattivo e le chiamate HTTP corte.
        /// </summary>
        protected override void OnTimer()
        {
            if (_stopped)
                return;

            if (SyncTicks && ShouldFlushTicks())
            {
                FlushTicks();
                return;
            }

            var stream = NextStreamNeedingWork();
            if (stream == null)
            {
                ReportBackfillOnce();
                if (!KeepInSync && !SyncTicks)
                {
                    Print("Backfill completato e nessun compito a regime: il bot si ferma.");
                    _stopped = true;
                    Stop();
                }

                return;
            }

            if (!stream.StatusFetched)
            {
                FetchStatus(stream);
                return;
            }

            ProcessNextChunk(stream);
        }

        private SyncStream NextStreamNeedingWork()
        {
            for (var i = 0; i < _streams.Count; i++)
            {
                var candidate = _streams[(_roundRobin + i) % _streams.Count];
                if (candidate.StatusFetched && candidate.BackfillDone)
                    continue;

                _roundRobin = (_roundRobin + i + 1) % _streams.Count;
                return candidate;
            }

            return null;
        }

        /// <summary>
        /// Chiede al server cosa ha gia' di questo stream. E' la chiamata che rende il bot
        /// riprendibile: senza, ogni riavvio ricomincerebbe il backfill da capo e riverserebbe
        /// megabyte di barre gia' presenti solo per farsele contare come duplicate.
        /// </summary>
        private void FetchStatus(SyncStream stream)
        {
            var uri = string.Format(
                "api/datafeed-external/status?broker={0}&symbol={1}&timeframeMinutes={2}",
                Uri.EscapeDataString(_brokerCode), Uri.EscapeDataString(stream.PiootooSymbol), stream.TimeframeMinutes);

            try
            {
                using (var response = _http.Send(BuildRequest(HttpMethod.Get, uri)))
                {
                    var body = ReadBody(response);
                    if (!response.IsSuccessStatusCode)
                    {
                        Print("{0}: status non disponibile ({1} {2}). Si procede come se il server fosse vuoto.",
                            stream, (int)response.StatusCode, Truncate(body, 200));
                    }
                    else
                    {
                        var status = JsonSerializer.Deserialize<FeedStatusDto>(body, _json);
                        if (status != null && status.Coverage != null)
                        {
                            stream.ServerCandles = status.Coverage.TotalCandles;
                            stream.ServerFirstUtc = status.Coverage.FirstCandleUtc;
                            stream.ServerLastUtc = status.Coverage.LastCandleUtc;
                            stream.ServerGapsTruncated = status.GapsTruncated;
                            stream.ServerGaps = status.Gaps ?? new List<FeedGapDto>();
                        }

                        Print("{0}: il server ha {1} barre{2}.",
                            stream, stream.ServerCandles,
                            stream.ServerFirstUtc.HasValue
                                ? string.Format(" ({0:yyyy-MM-dd} -> {1:yyyy-MM-dd}, {2} buchi)",
                                    stream.ServerFirstUtc, stream.ServerLastUtc, stream.ServerGaps.Count)
                                : string.Empty);
                    }
                }
            }
            catch (Exception failure)
            {
                Print("{0}: status fallita ({1}). Si procede come se il server fosse vuoto.", stream, failure.Message);
            }

            stream.StatusFetched = true;
        }

        /// <summary>
        /// Un blocco: si cammina all'indietro dalla fine della finestra verso l'inizio, perche' e'
        /// il verso in cui il broker consegna la storia (<c>LoadMoreHistory</c> estende la serie
        /// all'indietro, non in avanti).
        /// </summary>
        private void ProcessNextChunk(SyncStream stream)
        {
            DateTime chunkEnd, chunkStart;

            // I blocchi gia' coperti si consumano QUI, in serie, e non uno per battito: su una
            // finestra di vent'anni con blocchi da cinque giorni sarebbero millequattrocento
            // battiti — mezz'ora di bot che non fa niente prima di arrivare al primo dato che
            // manca davvero. Saltare non costa I/O: e' solo aritmetica sull'elenco dei buchi.
            var skips = 0;
            while (true)
            {
                chunkEnd = stream.CursorEndUtc;
                chunkStart = chunkEnd.AddDays(-ChunkDays);
                if (chunkStart < _windowStartUtc)
                    chunkStart = _windowStartUtc;

                if (chunkEnd <= _windowStartUtc)
                {
                    CompleteBackfill(stream, "finestra coperta");
                    return;
                }

                if (!SkipCovered || !IsAlreadyCovered(stream, chunkStart, chunkEnd))
                    break;

                if (LogDiagnostico)
                    Print("{0}: {1:yyyy-MM-dd} -> {2:yyyy-MM-dd} gia' sul server, saltato.", stream, chunkStart, chunkEnd);

                stream.SkippedChunks++;
                AdvanceCursor(stream, chunkStart);
                if (stream.BackfillDone)
                    return;

                if (++skips >= MaxSkipsPerTick)
                    return; // si riprende dallo stesso punto al prossimo battito
            }

            if (!EnsureHistoryReaches(stream, chunkStart))
                return; // il broker sta ancora consegnando: si riprende al prossimo battito

            var candles = new List<CandleDto>();
            var oldestSent = chunkStart;
            var truncated = false;

            // Dall'ultima barra CHIUSA all'indietro: l'ultima della serie e' quella in formazione e
            // non va spedita mai. Una barra a meta' salvata nel feed e' un dato falso che poi nessuno
            // distingue piu' da uno vero.
            for (var i = stream.Series.Count - 2; i >= 0; i--)
            {
                var openTime = DateTime.SpecifyKind(stream.Series.OpenTimes[i], DateTimeKind.Utc);
                if (openTime >= chunkEnd)
                    continue;
                if (openTime < chunkStart)
                    break;

                candles.Add(new CandleDto
                {
                    DateTime = openTime,
                    Open = (decimal)stream.Series.OpenPrices[i],
                    High = (decimal)stream.Series.HighPrices[i],
                    Low = (decimal)stream.Series.LowPrices[i],
                    Close = (decimal)stream.Series.ClosePrices[i],
                    Volume = (decimal)stream.Series.TickVolumes[i]
                });

                oldestSent = openTime;
                if (candles.Count >= MaxBarsPerPost)
                {
                    truncated = true;
                    break;
                }
            }

            if (candles.Count > 0)
            {
                candles.Reverse(); // cronologico: e' come il server se le aspetta e come le scrive
                if (!SendBars(stream, candles, chunkStart, chunkEnd))
                    return; // invio fallito: si ritenta lo STESSO blocco al prossimo battito
            }
            else if (LogDiagnostico)
            {
                Print("{0}: nessuna barra fra {1:yyyy-MM-dd} e {2:yyyy-MM-dd}.", stream, chunkStart, chunkEnd);
            }

            // Se si e' troncato, il cursore si ferma alla barra piu' vecchia spedita (esclusa dal
            // blocco successivo): il resto del periodo lo prende il prossimo giro.
            AdvanceCursor(stream, truncated ? oldestSent : chunkStart);

            if (stream.BrokerExhausted && !stream.BackfillDone)
                CompleteBackfill(stream, "il broker non ha storia piu' vecchia");
        }

        private void AdvanceCursor(SyncStream stream, DateTime newEndUtc)
        {
            stream.CursorEndUtc = newEndUtc;
            if (stream.CursorEndUtc <= _windowStartUtc)
                CompleteBackfill(stream, "finestra coperta");
        }

        private void CompleteBackfill(SyncStream stream, string reason)
        {
            if (stream.BackfillDone)
                return;

            stream.BackfillDone = true;

            // Compattazione esplicita: da qui in poi questo stream riceve al massimo una barra alla
            // volta, quindi il journal non si svuoterebbe piu' da solo per un pezzo, e chi va a
            // leggere il file su disco lo troverebbe indietro.
            RequestCompact(stream);

            Print("{0}: backfill concluso ({1}). Spedite {2} barre in {3} blocchi ({4} nuove, {5} duplicate, {6} blocchi saltati).",
                stream, reason, stream.SentBars, stream.SentChunks, stream.AcceptedBars, stream.DuplicateBars, stream.SkippedChunks);
        }

        private void ReportBackfillOnce()
        {
            if (_backfillReported)
                return;

            _backfillReported = true;
            var totalSent = _streams.Sum(stream => stream.SentBars);
            var totalNew = _streams.Sum(stream => stream.AcceptedBars);
            Print("Backfill completato su {0} stream: {1} barre spedite, {2} nuove sul server.{3}",
                _streams.Count, totalSent, totalNew,
                KeepInSync ? " Si resta in ascolto delle barre nuove." : string.Empty);
        }

        /// <summary>
        /// Estende la serie all'indietro finche' non copre l'inizio del blocco. Restituisce false se
        /// il broker sta ancora consegnando: in quel caso NON si avanza il cursore e si riprova, cosi'
        /// un blocco non viene mai spedito a meta' solo perche' la storia non era ancora arrivata.
        /// </summary>
        private bool EnsureHistoryReaches(SyncStream stream, DateTime chunkStart)
        {
            if (stream.Series.Count == 0)
            {
                stream.BrokerExhausted = true;
                return true;
            }

            var loads = 0;
            while (DateTime.SpecifyKind(stream.Series.OpenTimes[0], DateTimeKind.Utc) > chunkStart)
            {
                if (loads++ >= MaxHistoryLoadsPerChunkAttempt)
                    return false;

                if (stream.Series.LoadMoreHistory() <= 0)
                {
                    stream.BrokerExhausted = true;
                    if (LogOperativo)
                        Print("{0}: il broker non ha storia prima di {1:yyyy-MM-dd HH:mm}.",
                            stream, DateTime.SpecifyKind(stream.Series.OpenTimes[0], DateTimeKind.Utc));
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Un blocco e' gia' coperto se cade dentro l'intervallo che il server dichiara e nessuno dei
        /// buchi che il server ha elencato lo tocca. Se l'elenco dei buchi era troncato non si salta
        /// niente: meglio rispedire dati che il server contera' come duplicati, che dare per coperto
        /// un periodo su un elenco incompleto.
        /// </summary>
        private bool IsAlreadyCovered(SyncStream stream, DateTime chunkStart, DateTime chunkEnd)
        {
            if (stream.ServerGapsTruncated || !stream.ServerFirstUtc.HasValue || !stream.ServerLastUtc.HasValue)
                return false;

            if (chunkStart < stream.ServerFirstUtc.Value || chunkEnd > stream.ServerLastUtc.Value)
                return false;

            foreach (var gap in stream.ServerGaps)
            {
                // Un buco di fine settimana non e' storia mancante: e' il mercato chiuso, e chiederla
                // al broker all'infinito non la fa comparire.
                if (gap.SpansWeekend)
                    continue;

                if (gap.ToUtc > chunkStart && gap.FromUtc < chunkEnd)
                    return false;
            }

            return true;
        }

        // -----------------------------------------------------------------------------------------
        // Regime: una barra alla volta
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Si apre una barra nuova = quella prima si e' chiusa. Si spediscono le ultime chiuse
        /// (non solo l'ultima) perche' un invio fallito non lasci un buco permanente: sono barre gia'
        /// note al server, che le conta come duplicate e non riscrive niente.
        /// </summary>
        private void OnSeriesBarOpened(BarOpenedEventArgs args)
        {
            SyncStream stream;
            if (!_bySeries.TryGetValue(args.Bars, out stream))
                return;

            if (!KeepInSync || !stream.StatusFetched || !stream.BackfillDone)
                return;

            var candles = new List<CandleDto>();
            for (var i = stream.Series.Count - 2; i >= 0 && candles.Count < LiveHealingBars; i--)
            {
                var openTime = DateTime.SpecifyKind(stream.Series.OpenTimes[i], DateTimeKind.Utc);
                candles.Add(new CandleDto
                {
                    DateTime = openTime,
                    Open = (decimal)stream.Series.OpenPrices[i],
                    High = (decimal)stream.Series.HighPrices[i],
                    Low = (decimal)stream.Series.LowPrices[i],
                    Close = (decimal)stream.Series.ClosePrices[i],
                    Volume = (decimal)stream.Series.TickVolumes[i]
                });
            }

            if (candles.Count == 0)
                return;

            candles.Reverse();

            // A regime si compatta a ogni invio: sono poche barre, e il file su disco deve essere
            // sempre quello vero — e' il motivo per cui si tiene acceso il bot.
            SendBars(stream, candles, candles[0].DateTime, candles[candles.Count - 1].DateTime, compact: true);
        }

        // -----------------------------------------------------------------------------------------
        // Tick
        // -----------------------------------------------------------------------------------------

        private void OnSymbolTick(SymbolTickEventArgs args)
        {
            if (!SyncTicks)
                return;

            var key = PiootooSymbolOf(args.SymbolName);
            List<TickDto> buffer;
            if (!_tickBuffers.TryGetValue(key, out buffer))
                _tickBuffers[key] = buffer = new List<TickDto>();

            buffer.Add(new TickDto
            {
                TimeUtc = Server.TimeInUtc,
                Bid = (decimal)args.Bid,
                Ask = (decimal)args.Ask
            });
        }

        private bool ShouldFlushTicks()
        {
            if (_tickBuffers.Count == 0)
                return false;

            foreach (var buffer in _tickBuffers.Values)
            {
                if (buffer.Count >= TicksPerPost)
                    return true;
            }

            return (Server.TimeInUtc - _lastTickFlushUtc).TotalSeconds >= TickFlushSeconds &&
                   _tickBuffers.Values.Any(buffer => buffer.Count > 0);
        }

        private void FlushTicks()
        {
            _lastTickFlushUtc = Server.TimeInUtc;

            foreach (var entry in _tickBuffers.ToList())
            {
                if (entry.Value.Count == 0)
                    continue;

                // Il buffer si svuota PRIMA dell'invio: se la chiamata fallisce si perdono dei tick,
                // ma tenerli accumulerebbe memoria senza limite finche' il server e' giu', ed e' un
                // prezzo peggiore. Le barre — che sono il dato che conta — non si perdono mai, perche'
                // quelle si rileggono dal broker.
                var batch = entry.Value;
                _tickBuffers[entry.Key] = new List<TickDto>();

                var request = new IngestTicksRequestDto
                {
                    Broker = _brokerCode,
                    Symbol = entry.Key,
                    Source = string.Format("PiootooDatafeedSyncBot/{0}@{1}", BotVersion, Account.BrokerName),
                    ChunkId = string.Format("{0}_{1:yyyyMMddHHmmss}", entry.Key, Server.TimeInUtc),
                    Ticks = batch
                };

                try
                {
                    using (var response = PostJson("api/datafeed-external/ticks", request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Print("Invio tick {0} fallito: {1}", entry.Key, ReadError(response));
                        }
                        else if (LogDiagnostico)
                        {
                            var body = JsonSerializer.Deserialize<IngestTicksResponseDto>(ReadBody(response), _json);
                            Print("Tick {0}: {1} spediti, {2} scritti, {3} sovrapposti.",
                                entry.Key, batch.Count, body == null ? 0 : body.Accepted, body == null ? 0 : body.Stale);
                        }
                    }
                }
                catch (Exception failure)
                {
                    Print("Invio tick {0} fallito: {1}", entry.Key, failure.Message);
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        // HTTP
        // -----------------------------------------------------------------------------------------

        private bool SendBars(SyncStream stream, List<CandleDto> candles, DateTime fromUtc, DateTime toUtc, bool compact = false)
        {
            var request = new IngestBarsRequestDto
            {
                Compact = compact,
                Chunks = new List<BarChunkDto>
                {
                    new BarChunkDto
                    {
                        Broker = _brokerCode,
                        Symbol = stream.PiootooSymbol,
                        TimeframeMinutes = stream.TimeframeMinutes,
                        Source = string.Format("PiootooDatafeedSyncBot/{0}@{1}", BotVersion, Account.BrokerName),
                        ChunkId = string.Format("{0}_{1}_{2:yyyyMMdd}-{3:yyyyMMdd}",
                            stream.PiootooSymbol, stream.TimeframeMinutes, fromUtc, toUtc),
                        Candles = candles
                    }
                }
            };

            try
            {
                using (var response = PostJson("api/datafeed-external/bars", request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Print("{0}: invio {1:yyyy-MM-dd}->{2:yyyy-MM-dd} fallito: {3}",
                            stream, fromUtc, toUtc, ReadError(response));
                        return false;
                    }

                    var payload = JsonSerializer.Deserialize<IngestBarsResponseDto>(ReadBody(response), _json);
                    stream.SentBars += candles.Count;
                    stream.SentChunks++;
                    if (payload != null)
                    {
                        stream.AcceptedBars += payload.TotalAccepted;
                        stream.DuplicateBars += payload.TotalDuplicates;
                        if (payload.TotalRejected > 0)
                        {
                            var reasons = payload.Streams != null && payload.Streams.Count > 0 && payload.Streams[0].RejectReasons != null
                                ? string.Join("; ", payload.Streams[0].RejectReasons)
                                : "motivo non riportato";
                            Print("{0}: {1} barre SCARTATE dal server ({2}).", stream, payload.TotalRejected, reasons);
                        }
                    }

                    if (LogOperativo)
                        Print("{0}: {1:yyyy-MM-dd} -> {2:yyyy-MM-dd}, {3} barre spedite ({4} nuove, {5} duplicate).",
                            stream, fromUtc, toUtc, candles.Count,
                            payload == null ? 0 : payload.TotalAccepted,
                            payload == null ? 0 : payload.TotalDuplicates);

                    return true;
                }
            }
            catch (Exception failure)
            {
                Print("{0}: invio {1:yyyy-MM-dd}->{2:yyyy-MM-dd} fallito: {3}", stream, fromUtc, toUtc, failure.Message);
                return false;
            }
        }

        private void RequestCompact(SyncStream stream)
        {
            var uri = string.Format("api/datafeed-external/compact?broker={0}&symbol={1}&timeframeMinutes={2}",
                Uri.EscapeDataString(_brokerCode), Uri.EscapeDataString(stream.PiootooSymbol), stream.TimeframeMinutes);

            try
            {
                using (var response = _http.Send(BuildRequest(HttpMethod.Post, uri)))
                {
                    if (!response.IsSuccessStatusCode)
                        Print("{0}: compattazione finale non riuscita: {1}", stream, ReadError(response));
                }
            }
            catch (Exception failure)
            {
                Print("{0}: compattazione finale non riuscita: {1}", stream, failure.Message);
            }
        }

        private HttpResponseMessage PostJson<T>(string uri, T body)
        {
            var request = BuildRequest(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
            return _http.Send(request);
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string uri)
        {
            return new HttpRequestMessage(method, uri);
        }

        private static string ReadBody(HttpResponseMessage response)
        {
            using (var stream = response.Content.ReadAsStream())
            using (var reader = new System.IO.StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static string ReadError(HttpResponseMessage response)
        {
            try
            {
                return string.Format("{0} {1}", (int)response.StatusCode, Truncate(ReadBody(response), 300));
            }
            catch
            {
                return response.StatusCode.ToString();
            }
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text;

            return text.Substring(0, max) + "...";
        }

        // -----------------------------------------------------------------------------------------
        // Supporto
        // -----------------------------------------------------------------------------------------

        private string PiootooSymbolOf(string brokerSymbolName)
        {
            foreach (var stream in _streams)
            {
                if (string.Equals(stream.BrokerSymbol, brokerSymbolName, StringComparison.OrdinalIgnoreCase))
                    return stream.PiootooSymbol;
            }

            return "@" + brokerSymbolName.TrimStart('@').ToUpperInvariant();
        }

        private static bool TryToTimeFrame(int minutes, out TimeFrame timeFrame)
        {
            switch (minutes)
            {
                case 1: timeFrame = TimeFrame.Minute; return true;
                case 2: timeFrame = TimeFrame.Minute2; return true;
                case 3: timeFrame = TimeFrame.Minute3; return true;
                case 5: timeFrame = TimeFrame.Minute5; return true;
                case 10: timeFrame = TimeFrame.Minute10; return true;
                case 15: timeFrame = TimeFrame.Minute15; return true;
                case 30: timeFrame = TimeFrame.Minute30; return true;
                case 60: timeFrame = TimeFrame.Hour; return true;
                case 120: timeFrame = TimeFrame.Hour2; return true;
                case 240: timeFrame = TimeFrame.Hour4; return true;
                case 360: timeFrame = TimeFrame.Hour6; return true;
                case 720: timeFrame = TimeFrame.Hour12; return true;
                case 1440: timeFrame = TimeFrame.Daily; return true;
                case 10080: timeFrame = TimeFrame.Weekly; return true;
                default: timeFrame = TimeFrame.Hour; return false;
            }
        }

        private void StopWithError(string message)
        {
            Print("ERRORE FATALE: {0}", message);
            _stopped = true;
            Stop();
        }

        protected override void OnStop()
        {
            if (SyncTicks)
            {
                // Ultimo giro: i tick raccolti dopo l'ultimo invio andrebbero persi allo spegnimento.
                try { FlushTicks(); } catch (Exception failure) { Print("Svuotamento tick finale fallito: {0}", failure.Message); }
            }

            foreach (var stream in _streams.Where(stream => stream.SentBars > 0 && !stream.BackfillDone))
            {
                // Il bot si e' fermato a meta' backfill: quello che e' arrivato va materializzato sul
                // file piatto, altrimenti resta nel journal e chi legge il feed non lo vede.
                RequestCompact(stream);
            }

            foreach (var handler in _tickHandlers)
                handler.Key.Tick -= handler.Value;

            foreach (var stream in _streams)
                stream.Series.BarOpened -= OnSeriesBarOpened;

            Print("Piootoo Datafeed Sync fermato. Barre spedite in totale: {0}.", _streams.Sum(stream => stream.SentBars));
        }

        // -----------------------------------------------------------------------------------------
        // Stato e DTO (allineati per forma JSON a Piootoo.Shared.Models.Datafeed)
        // -----------------------------------------------------------------------------------------

        private sealed class SyncStream
        {
            public string BrokerSymbol;
            public string PiootooSymbol;
            public int TimeframeMinutes;
            public Bars Series;

            /// <summary>Fine (esclusa) del prossimo blocco: cammina all'indietro verso l'inizio finestra.</summary>
            public DateTime CursorEndUtc;

            public bool StatusFetched;
            public bool BackfillDone;
            public bool BrokerExhausted;

            public int ServerCandles;
            public DateTime? ServerFirstUtc;
            public DateTime? ServerLastUtc;
            public bool ServerGapsTruncated;
            public List<FeedGapDto> ServerGaps = new List<FeedGapDto>();

            public int SentBars;
            public int SentChunks;
            public int SkippedChunks;
            public int AcceptedBars;
            public int DuplicateBars;

            public override string ToString()
            {
                return string.Format("{0}/{1}m", PiootooSymbol, TimeframeMinutes);
            }
        }

        private sealed class CandleDto
        {
            public DateTime DateTime { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
        }

        private sealed class BarChunkDto
        {
            public string Broker { get; set; }
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public string Source { get; set; }
            public string ChunkId { get; set; }
            public List<CandleDto> Candles { get; set; }
        }

        private sealed class IngestBarsRequestDto
        {
            public List<BarChunkDto> Chunks { get; set; }
            public bool Compact { get; set; }
        }

        private sealed class StreamIngestResultDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public int Received { get; set; }
            public int Accepted { get; set; }
            public int Updated { get; set; }
            public int Duplicates { get; set; }
            public int Rejected { get; set; }
            public List<string> RejectReasons { get; set; }
            public int PendingJournalCandles { get; set; }
            public bool Compacted { get; set; }
        }

        private sealed class IngestBarsResponseDto
        {
            public List<StreamIngestResultDto> Streams { get; set; }
            public int TotalAccepted { get; set; }
            public int TotalDuplicates { get; set; }
            public int TotalRejected { get; set; }
        }

        private sealed class FeedCoverageDto
        {
            public int TotalCandles { get; set; }
            public DateTime? FirstCandleUtc { get; set; }
            public DateTime? LastCandleUtc { get; set; }
            public int? DominantStepMinutes { get; set; }
        }

        private sealed class FeedGapDto
        {
            public DateTime FromUtc { get; set; }
            public DateTime ToUtc { get; set; }
            public int MinutesMissing { get; set; }
            public int EstimatedMissingCandles { get; set; }
            public bool SpansWeekend { get; set; }
        }

        private sealed class FeedStatusDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public FeedCoverageDto Coverage { get; set; }
            public int PendingJournalCandles { get; set; }
            public int GapCount { get; set; }
            public List<FeedGapDto> Gaps { get; set; }
            public bool GapsTruncated { get; set; }
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

        /// <summary>Cosa raccogliere, prima di aprire le serie: da parametri o dal piano.</summary>
        private sealed class StreamRequest
        {
            public string BrokerSymbol;
            public string PiootooSymbol;
            public List<int> TimeframesMinutes;
        }

        private sealed class TickDto
        {
            public DateTime TimeUtc { get; set; }
            public decimal Bid { get; set; }
            public decimal Ask { get; set; }
        }

        private sealed class IngestTicksRequestDto
        {
            public string Broker { get; set; }
            public string Symbol { get; set; }
            public string Source { get; set; }
            public string ChunkId { get; set; }
            public List<TickDto> Ticks { get; set; }
        }

        private sealed class IngestTicksResponseDto
        {
            public string Symbol { get; set; }
            public int Received { get; set; }
            public int Accepted { get; set; }
            public int Stale { get; set; }
            public int Rejected { get; set; }
            public DateTime? LastTickUtc { get; set; }
        }
    }
}
