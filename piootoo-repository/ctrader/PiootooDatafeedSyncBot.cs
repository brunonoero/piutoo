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
    /// feed del vendor: <c>@NQ_1.json</c>. Il codice broker lo deduce dal conto
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
    /// timer si fa <b>un solo blocco</b> (default due giorni, al massimo 5000 barre), lo si
    /// spedisce e si passa allo stream successivo. Se il bot muore a meta', quello che e' arrivato
    /// e' gia' sul disco del server, e al riavvio si riprende da dove si era rimasti: la prima cosa
    /// che il bot chiede per ogni stream e' <c>GET status</c>, cioe' "cosa hai gia'".</para>
    ///
    /// <para><b>Cosa NON fa.</b> Non inventa barre e non ricuce buchi: le sovrapposizioni le elimina
    /// il server deduplicando sull'istante di apertura della barra, e i buchi li <i>dichiara</i>
    /// nella status invece di riempirli. Se il broker non ha un periodo, quel periodo resta vuoto e
    /// si vede.</para>
    ///
    /// <para><b>Cosa raccogliere lo dichiara il piano, e nient'altro.</b> Il <c>Codice piano</c> e'
    /// obbligatorio: gli strumenti arrivano dal masterfilter del suo workspace, gia' con il nome che
    /// ognuno ha su questo conto, e i timeframe da far derivare al server vengono dalla stessa
    /// lista. Dalla 7.1.0 non c'e' piu' un elenco di simboli ne' di timeframe da scrivere a mano:
    /// erano una seconda copia di cio' che il masterfilter dice gia', e due liste della stessa cosa
    /// divergono in silenzio. Per rifare un pezzo di storia si stringe la <b>finestra di date</b>,
    /// non l'elenco dei simboli: la raccolta e' idempotente e ripassare su tutto il piano non
    /// riscrive niente di gia' presente.</para>
    ///
    /// <para><b>Raccoglie SOLO barre da un minuto UTC.</b> Dalla 2.0.0 il bot non costruisce piu'
    /// nessun timeframe: chiede alla piattaforma la serie da un minuto, la spedisce cosi' com'e', e
    /// tutto cio' che sta sopra lo deriva il server dal minuto. Il minuto e' il solo dato su cui non
    /// c'e' niente da decidere — nessun ancoraggio, nessun fuso, nessuna convenzione sul cambio
    /// d'ora — quindi e' il solo che un bot possa raccogliere senza poter sbagliare.</para>
    ///
    /// <para><b>Perche' e' cambiato.</b> La griglia dei timeframe alti dipende dall'ora di inizio
    /// sessione dello strumento, che e' un dato del calendario di mercato: una tabella che il server
    /// ha e che cTrader non vede. Il bot ne teneva una copia, e finche' le due sono rimaste
    /// d'accordo ha funzionato. Quando hanno smesso, il risultato non e' stato un errore: e' stato un
    /// file con DUE griglie dentro — la deduplica e' sull'istante di apertura, quindi due raccolte
    /// con ancoraggi diversi non si sovrascrivono, si sommano — meta' barre ciascuna, tutte
    /// plausibili. Sull'archivio FTMOPLATFORM erano <c>@KC_240</c>, <c>@KC_1440</c> e
    /// <c>@CT_1440</c>. Vedi <c>docs/domini/layer-barre-e-calendario.md</c>.</para>
    ///
    /// <para><b>Gli aggregati li chiede a fine backfill</b>
    /// (<c>POST api/datafeed-external/rebuild-from-minutes</c>), cosi' una raccolta non lascia
    /// l'archivio con il solo minuto e i backtest a mani vuote. Quali timeframe derivare lo dice il
    /// masterfilter del piano, o il parametro apposito quando un piano non c'e'.</para>
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
        // Dalla 7.1.0 il raccoglitore segue la numerazione del progetto, come i due bot operativi:
        // una sola linea di versioni per tutto cio' che si distribuisce in cTrader, cosi' "quale
        // release ho su questo grafico" ha una risposta sola. Fino alla 3.0.0 aveva una
        // numerazione propria, sull'argomento che non tocca il contratto di esecuzione e che
        // ogni release del server gli avrebbe fatto comparire un finto disallineamento; il costo
        // di quella scelta era pero' avere due scale di versioni per tre bot, e la versione finisce
        // nel campo `source` del feed — dove un 2.x accanto a un 7.x non dice piu' niente a nessuno.
        //
        // Storia delle rotture, che restano leggibili nel `source` degli archivi gia' raccolti:
        // - 2.0.0: il bot raccoglie SOLO barre da un minuto. Spariti i tre parametri della griglia
        //   (fuso, ora di inizio sessione, timeframe base) e il codice che piegava i bucket: quella
        //   regola vive adesso in un punto solo, lato server (Piootoo.Shared/MarketData/SessionGrid).
        //   Un archivio raccolto con la 1.x contiene aggregati costruiti dal bot, uno raccolto dopo
        //   contiene il minuto e aggregati derivati dal server, e i due non sono la stessa cosa.
        // - 7.1.0 (era 3.0.0): il PIANO e' l'unica fonte. Spariti 'Simboli' e 'Timeframe da far
        //   derivare al server': erano una seconda lista delle stesse cose che il masterfilter
        //   dichiara gia', e l'elenco timeframe senza il simbolo produceva una derivazione che non
        //   costruiva niente e lo riportava come "zero stream" — indistinguibile da un successo. La
        //   derivazione passa sempre da planCode ed e' rifatta anche allo stop, per la coda raccolta
        //   in sincronia. Due parametri salvati nelle istanze spariscono, e un'istanza senza codice
        //   piano non parte piu': va riconfigurata, non solo ricompilata.
        private const string BotVersion = "7.2.0";

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
        /// Tolleranza usata per decidere cos'e' un buco quando non la si dichiara: quattro giorni.
        /// Un fine settimana lungo con un festivo attaccato ci sta dentro; una storia davvero
        /// mancante no.
        /// </summary>
        private const int DefaultGapToleranceMinutes = 4 * 24 * 60;

        /// <summary>
        /// L'unico timeframe che questo bot raccoglie. Il minuto e' il dato autorevole: tutto cio'
        /// che sta sopra e' derivato, e derivarlo e' compito del server, in un punto solo.
        /// </summary>
        private const int CollectedTimeframeMinutes = 1;

        /// <summary>
        /// Blocchi gia' coperti consumati in un solo battito. Serve a scorrere in fretta un periodo
        /// gia' raccolto senza pero' tenere il thread occupato all'infinito su una finestra enorme.
        /// </summary>
        private const int MaxSkipsPerTick = 500;

        [Parameter("Server Base Url", DefaultValue = "http://localhost:5142", Group = "Server")]
        public string ServerBaseUrl { get; set; }

        /// <summary>
        /// Codice del piano da cui prendere gli strumenti. <b>Obbligatorio</b>: e' l'unica fonte
        /// degli strumenti da raccogliere e dei timeframe da far derivare al server.
        ///
        /// <para><b>Perche' e' l'unica.</b> Gli strumenti li dichiara il masterfilter del workspace
        /// del piano, e il nome che ognuno ha su questo conto arriva dalla tabella di conversione
        /// dell'account: non c'e' niente da mappare a mano. Un elenco scritto qui accanto sarebbe
        /// una seconda lista della stessa cosa, e divergerebbe in silenzio il giorno in cui si
        /// aggiunge una strategia su un simbolo nuovo — il feed mancherebbe proprio dove serve.</para>
        ///
        /// <para>Il codice piano e' globale, quindi basta questo: niente workspace, niente account.
        /// E non apre nessuna sessione — un raccoglitore e' una lettura pura e non deve avere
        /// effetti sull'operativita'.</para>
        ///
        /// <para><b>Niente Titano.</b> Gli strumenti vengono dal masterfilter, non dalla rotazione
        /// corrente: il datafeed di uno strumento serve anche mentre le sue strategie sono spente,
        /// altrimenti alla riaccensione mancherebbe la storia della pausa.</para>
        ///
        /// <para><b>Per rifare un simbolo solo</b> si usa la finestra di date, non un filtro sui
        /// simboli: la raccolta e' idempotente — la chiave e' l'istante di apertura della barra —
        /// quindi ripassare su tutto il piano non riscrive niente di gia' presente.</para>
        /// </summary>
        [Parameter("Codice piano (obbligatorio)", DefaultValue = "", Group = "Cosa raccogliere")]
        public string PlanCode { get; set; }

        /// <summary>
        /// A fine backfill chiede al server di riscrivere gli aggregati dal minuto appena raccolto
        /// (<c>POST api/datafeed-external/rebuild-from-minutes</c>).
        ///
        /// <para>Acceso di default perche' altrimenti una raccolta su un archivio nuovo lascerebbe
        /// <b>solo</b> il minuto, e ogni backtest a 15, 60 o 240 minuti troverebbe il datafeed
        /// mancante senza che nulla spieghi il perche'.</para>
        /// </summary>
        [Parameter("Fai derivare gli aggregati a fine backfill", DefaultValue = true, Group = "Aggregati")]
        public bool RebuildAggregates { get; set; }

        /// <summary>
        /// Inizio della finestra raccolta in questo run (yyyy-MM-dd, UTC, incluso). Vuoto = tutta la
        /// storia che il broker consegna.
        /// </summary>
        [Parameter("Data inizio (yyyy-MM-dd, vuoto = tutta la storia)", DefaultValue = "", Group = "Finestra di date")]
        public string StartDateText { get; set; }

        /// <summary>Fine della finestra (yyyy-MM-dd, UTC, esclusa). Vuoto = adesso.</summary>
        [Parameter("Data fine (yyyy-MM-dd, vuoto = adesso)", DefaultValue = "", Group = "Finestra di date")]
        public string EndDateText { get; set; }

        [Parameter("Giorni per blocco", DefaultValue = 2, MinValue = 1, MaxValue = 3650, Group = "Finestra di date")]
        public int ChunkDays { get; set; }

        [Parameter("Barre massime per invio", DefaultValue = 5000, MinValue = 50, MaxValue = 20000, Group = "Finestra di date")]
        public int MaxBarsPerPost { get; set; }

        /// <summary>
        /// Salta i blocchi che il server dichiara gia' coperti (nessun buco al loro interno). Vale la
        /// pena spegnerlo solo per riscrivere di proposito un periodo che si sospetta sbagliato.
        /// </summary>
        [Parameter("Salta i periodi gia' presenti sul server", DefaultValue = true, Group = "Finestra di date")]
        public bool SkipCovered { get; set; }

        /// <summary>
        /// Da quanti minuti in su un vuoto fra due barre e' un <b>buco</b> invece che mercato
        /// chiuso. <c>0</c> = quattro giorni, che e' la scelta giusta per una serie a un minuto:
        /// alzarla fa dare per coperti periodi che mancano, abbassarla fa rispedire ogni volta
        /// tutta la storia perche' ogni notte diventa un buco.
        /// </summary>
        [Parameter("Tolleranza buchi in minuti (0 = quattro giorni)", DefaultValue = 0, MinValue = 0, Group = "Finestra di date")]
        public int GapToleranceMinutes { get; set; }

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
        /// <summary>
        /// Dalla serie della piattaforma agli stream che ne pendono. E' una LISTA perche' una sola
        /// serie base ne alimenta piu' d'uno: <c>@FDAX_60</c> e <c>@FDAX_240</c> vengono entrambi
        /// dalle barre orarie, e con una mappa uno-a-uno il secondo perderebbe il regime in silenzio.
        /// </summary>
        private readonly Dictionary<Bars, List<SyncStream>> _bySeries = new Dictionary<Bars, List<SyncStream>>();
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
            //
            // Le due proprieta' sono letture indipendenti dell'orologio, non due viste dello stesso
            // istante: fra l'una e l'altra il tempo avanza, e il confronto secco falliva a caso anche
            // su un bot davvero in UTC. Si e' visto in produzione con le due date IDENTICHE nel
            // messaggio d'errore — che le rilegge, e la seconda volta ricadevano nello stesso tick.
            // Si legge una volta sola e si confronta con una tolleranza: il fuso piu' vicino a UTC
            // che esista dista quindici minuti, quindi un minuto separa senza ambiguita' il
            // disallineamento vero dall'orologio che e' avanzato fra le due letture.
            var serverTime = Server.Time;
            var serverTimeUtc = Server.TimeInUtc;
            if ((serverTime - serverTimeUtc).Duration() > TimeSpan.FromMinutes(1))
            {
                StopWithError(string.Format(
                    "Il robot non sta girando in UTC (Server.Time={0:O}, Server.TimeInUtc={1:O}). " +
                    "L'attributo [Robot(TimeZone = TimeZones.UTC)] e' obbligatorio per questo bot: " +
                    "le barre verrebbero salvate con un orario falso.",
                    serverTime, serverTimeUtc));
                return;
            }

            if (string.IsNullOrWhiteSpace(PlanCode))
            {
                StopWithError(
                    "'Codice piano' e' obbligatorio: e' da li' che arrivano gli strumenti da " +
                    "raccogliere e i timeframe che il server deve derivare. Senza, il bot non " +
                    "saprebbe che cosa raccogliere e partirebbe a vuoto.");
                return;
            }

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

            Print("Finestra richiesta: {0:yyyy-MM-dd} -> {1:yyyy-MM-dd} — {2} simboli a UN MINUTO, " +
                  "blocchi da {3} giorni (max {4} barre).",
                _windowStartUtc, _windowEndUtc, _streams.Count, ChunkDays, MaxBarsPerPost);

            // La RAM e' il vincolo vero di una raccolta a un minuto, ed e' bene saperlo prima e non
            // a meta'. La serie resta in memoria per intero mentre si cammina all'indietro: un anno
            // sono circa 370.000 barre per simbolo contro le 1.500 di una 240. Su molti simboli
            // insieme conviene spezzare per finestre di date — che e' esattamente a cosa servono
            // 'Data inizio' e 'Data fine' — invece di chiedere tutta la storia in un run solo.
            var giorni = Math.Max(1, (_windowEndUtc - _windowStartUtc).TotalDays);
            Print("Attese fino a ~{0:N0} barre per simbolo ({1:N0} complessive): la serie da un minuto " +
                  "resta in RAM mentre si cammina all'indietro. Se la piattaforma rallenta, spezzare " +
                  "la finestra di date invece di allargare i blocchi.",
                giorni * 1440, giorni * 1440 * _streams.Count);

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
        /// Apre uno stream da un minuto per ogni strumento del piano. Uno stream che il broker non
        /// conosce non ferma il run: si segnala e si prosegue con gli altri, perche' in un piano di
        /// venti simboli uno non quotato su questo conto non deve costare la raccolta degli altri
        /// diciannove.
        /// </summary>
        private bool TryBuildStreams(out string error)
        {
            var requests = BuildRequestsFromPlan(out error);

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

                // Uno stream per SIMBOLO, non per coppia (simbolo, timeframe): si raccoglie il
                // minuto e basta, e i timeframe che il piano dichiara servono al server per
                // derivarli, non a questo bot per raccoglierli.
                if (_streams.Any(existing => string.Equals(
                        existing.BrokerSymbol, brokerSymbol.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var series = MarketData.GetBars(TimeFrame.Minute, brokerSymbol.Name);
                if (series == null)
                {
                    Print("{0}: serie da un minuto non disponibile su questo account. Stream saltato.",
                        request.PiootooSymbol);
                    continue;
                }

                var stream = new SyncStream
                {
                    BrokerSymbol = brokerSymbol.Name,
                    PiootooSymbol = request.PiootooSymbol,
                    TimeframeMinutes = CollectedTimeframeMinutes,
                    Series = series,
                    CursorEndUtc = _windowEndUtc
                };

                _streams.Add(stream);

                List<SyncStream> pendenti;
                if (!_bySeries.TryGetValue(series, out pendenti))
                {
                    _bySeries[series] = pendenti = new List<SyncStream>();
                    series.BarOpened += OnSeriesBarOpened;
                }

                pendenti.Add(stream);
            }

            if (_streams.Count == 0)
            {
                error = string.Format("Nessuno stream valido: nessuno strumento del piano '{0}' e' " +
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
                // I timeframe del piano NON si guardano piu' qui: si raccoglie il minuto per ogni
                // strumento del masterfilter, e quali aggregati derivarne lo dice il piano al
                // server (POST rebuild-from-minutes?planCode=...). Uno strumento senza timeframe
                // resta comunque uno strumento da raccogliere: il minuto serve lo stesso.
                if (string.IsNullOrWhiteSpace(instrument.Symbol))
                    continue;

                requests.Add(new StreamRequest
                {
                    // Il nome sul broker lo dichiara il server, dalla tabella di conversione del
                    // conto: e' il motivo per cui con il piano non serve mappare niente a mano.
                    BrokerSymbol = string.IsNullOrWhiteSpace(instrument.AccountSymbol)
                        ? instrument.Symbol
                        : instrument.AccountSymbol,
                    PiootooSymbol = NormalizePiootooSymbol(instrument.Symbol)
                });
            }

            if (string.IsNullOrWhiteSpace(plan.DatafeedBroker))
            {
                error = string.Format(
                    "Il server non dichiara la cartella del datafeed per il conto {0} del piano '{1}': " +
                    "il conto non ha un broker in anagrafica, oppure il server e' precedente alla 7.2.0. " +
                    "Senza, non c'e' una cartella in cui scrivere le barre.",
                    plan.AccountNumber, plan.PlanCode);
                return null;
            }

            _brokerCode = plan.DatafeedBroker.Trim().ToUpperInvariant();

            Print("Piano '{0}' ({1}), workspace '{2}', conto {3}: {4} strumenti dal masterfilter.",
                plan.PlanCode, plan.PlanName, plan.WorkspaceId, plan.AccountNumber, requests.Count);
            Print("Codice broker dichiarato dal server: {0} — i feed andranno in datafeed-external/{0}/.",
                _brokerCode);

            foreach (var request in requests)
                Print("   {0} -> {1}", request.BrokerSymbol, request.PiootooSymbol);

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
        /// <summary>
        /// Cosa il server ha gia' per questo stream. La risposta decide cosa chiedere al broker e
        /// cosa saltare.
        ///
        /// <para><b>La tolleranza sui buchi va dichiarata, a un minuto.</b> Il default del server e'
        /// due volte il passo dominante — a un minuto, due minuti — e su una serie a un minuto vera
        /// ogni notte e' un buco: la pausa di manutenzione CME, la chiusura serale degli europei, i
        /// festivi. Un anno ne produce centinaia, l'elenco che il server restituisce viene troncato
        /// a duecento, e un elenco troncato fa dire a <see cref="IsAlreadyCovered"/> "non so" per
        /// ogni blocco. Risultato: <c>Salta i periodi gia' presenti</c> non salterebbe mai niente e
        /// ogni run rispedirebbe milioni di barre che il server contera' come duplicate.</para>
        ///
        /// <para>Con <see cref="GapToleranceMinutes"/> a zero si usano quattro giorni, che coprono un
        /// fine settimana lungo e un festivo attaccato: sotto quella soglia il mercato era chiuso,
        /// sopra manca davvero della storia.</para>
        /// </summary>
        private void FetchStatus(SyncStream stream)
        {
            var tolerance = GapToleranceMinutes > 0 ? GapToleranceMinutes : DefaultGapToleranceMinutes;
            var uri = string.Format(
                "api/datafeed-external/status?broker={0}&symbol={1}&timeframeMinutes={2}&gapToleranceMinutes={3}",
                Uri.EscapeDataString(_brokerCode), Uri.EscapeDataString(stream.PiootooSymbol),
                stream.TimeframeMinutes, tolerance);

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

            // Niente da allineare: a un minuto il confine del blocco cade sempre fra due barre, e
            // una barra appartiene a un blocco solo. L'arrotondamento ai confini dei bucket serviva
            // a non spedire mezzo bucket, e i bucket qui non esistono piu'.
            if (!EnsureHistoryReaches(stream, chunkStart))
                return; // il broker sta ancora consegnando: si riprende al prossimo battito

            DateTime oldestSent;
            bool truncated;
            var candles = CollectBackwards(stream, chunkStart, chunkEnd, MaxBarsPerPost, out oldestSent, out truncated);

            if (candles.Count > 0)
            {
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
            Print("Backfill completato su {0} simboli: {1} barre da un minuto spedite, {2} nuove sul server.{3}",
                _streams.Count, totalSent, totalNew,
                KeepInSync ? " Si resta in ascolto delle barre nuove." : string.Empty);

            RequestAggregateRebuild();
        }

        /// <summary>
        /// Chiede al server di derivare gli aggregati dal minuto appena raccolto.
        ///
        /// <para><b>Perche' il bot lo chiede e non lo fa.</b> La griglia dei timeframe alti dipende
        /// dall'ancoraggio di sessione dello strumento, che e' un dato del calendario di mercato —
        /// una tabella che il server ha e cTrader no. Fino alla 1.3.1 il bot ne teneva una copia e
        /// piegava i bucket da se': due copie della stessa tabella, e quando hanno smesso di essere
        /// d'accordo il risultato non e' stato un errore ma un file con DUE griglie dentro, meta'
        /// barre ciascuna, tutte plausibili. Vedi docs/domini/layer-barre-e-calendario.md §7bis.</para>
        ///
        /// <para><b>Quali timeframe.</b> Li dichiara il masterfilter del piano, che e' la stessa
        /// fonte da cui vengono gli strumenti da raccogliere: una sola lista, che non puo'
        /// divergere da se stessa.</para>
        ///
        /// <para><b>Il simbolo va detto, e lo dice il server.</b> Broker, simbolo e timeframe
        /// insieme sono dei <i>bersagli</i>, che il server costruisce anche quando il file non
        /// esiste ancora; se manca anche uno solo dei tre restano dei <i>filtri</i> su cio' che sul
        /// disco c'e' gia'. Una richiesta senza simbolo, su un archivio appena raccolto, non
        /// costruisce quindi niente e lo riporta come "zero stream" — indistinguibile da un
        /// successo. Passando da <c>planCode</c> il simbolo lo mette il server, uno per strumento
        /// del masterfilter, e il caso non si presenta.</para>
        ///
        /// <para><b>Quando.</b> A fine backfill e di nuovo allo stop: con 'Resta in sincronia' fra
        /// i due momenti possono passare giorni di minuti raccolti, che altrimenti non finirebbero
        /// in nessun aggregato.</para>
        ///
        /// <para>Un fallimento qui non e' fatale: il minuto — che e' il dato che conta — e' gia'
        /// salvato, e la ricostruzione si puo' rifare a mano con la stessa chiamata. Ma va detto,
        /// perche' senza aggregati ogni backtest sopra il minuto trova il datafeed mancante.</para>
        /// </summary>
        private void RequestAggregateRebuild()
        {
            if (!RebuildAggregates)
            {
                Print("Aggregati NON derivati (parametro spento): sul disco c'e' il solo minuto. " +
                      "Un backtest a 15, 60 o 240 minuti trovera' il datafeed mancante finche' non si " +
                      "chiama POST api/datafeed-external/rebuild-from-minutes.");
                return;
            }

            // Il simbolo lo mette il server, ciclando sugli strumenti del masterfilter del piano:
            // broker, simbolo e timeframe insieme sono per lui dei BERSAGLI, che costruisce anche
            // quando il file non esiste ancora. Una richiesta senza simbolo puo' invece soltanto
            // filtrare cio' che sul disco c'e' gia', e su un archivio appena raccolto non
            // costruirebbe niente riportandolo come "zero stream" — indistinguibile da un successo.
            PostAggregateRebuild(
                "api/datafeed-external/rebuild-from-minutes?broker=" +
                Uri.EscapeDataString(_brokerCode) +
                "&planCode=" + Uri.EscapeDataString(PlanCode.Trim()),
                true);
        }

        /// <summary>
        /// Esegue una chiamata di derivazione e ne stampa l'esito.
        /// </summary>
        /// <param name="declaresTargets">
        /// Vero quando la richiesta dichiara dei bersagli — un piano, oppure simbolo e timeframe —
        /// e quindi "zero stream" e' un difetto da spiegare. Falso quando si stanno soltanto
        /// riscrivendo gli aggregati esistenti, dove zero vuol dire che non ce n'erano.
        /// </param>
        private void PostAggregateRebuild(string uri, bool declaresTargets)
        {
            Print("Derivazione degli aggregati dal minuto: {0}", uri);

            try
            {
                using (var response = _http.Send(BuildRequest(HttpMethod.Post, uri)))
                {
                    var body = ReadBody(response);
                    if (!response.IsSuccessStatusCode)
                    {
                        Print("Derivazione degli aggregati NON riuscita: {0}. Il minuto e' salvato: " +
                              "si puo' rifare con la stessa chiamata.", Truncate(body, 300));
                        return;
                    }

                    var esito = JsonSerializer.Deserialize<RebuildResponseDto>(body, _json);
                    if (esito == null || esito.Streams == null || esito.Streams.Count == 0)
                    {
                        if (declaresTargets)
                        {
                            Print("Derivazione degli aggregati: nessuno stream costruito, benche' la " +
                                  "richiesta dichiarasse dei bersagli. Il simbolo potrebbe non essere " +
                                  "nel calendario di mercato del server, oppure il minuto non essere " +
                                  "arrivato.");
                        }
                        else
                        {
                            Print("Derivazione degli aggregati: nessuno stream costruito. Sul disco non " +
                                  "c'era alcun aggregato da riscrivere, e senza bersagli non se ne " +
                                  "creano: serve il codice piano, oppure il parametro 'Timeframe da far " +
                                  "derivare al server'.");
                        }

                        return;
                    }

                    foreach (var stream in esito.Streams)
                    {
                        if (stream.Rebuilt)
                            Print("   {0}/{1}m: {2} barre (prima {3}, di cui {4} fuori griglia).",
                                stream.Symbol, stream.TimeframeMinutes, stream.BarsAfter,
                                stream.BarsBefore, stream.OffGridBefore);
                        else
                            Print("   {0}/{1}m: saltato — {2}", stream.Symbol, stream.TimeframeMinutes, stream.Skipped);
                    }
                }
            }
            catch (Exception failure)
            {
                Print("Derivazione degli aggregati NON riuscita: {0}. Il minuto e' salvato: " +
                      "si puo' rifare con la stessa chiamata.", failure.Message);
            }
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
            List<SyncStream> pendenti;
            if (!_bySeries.TryGetValue(args.Bars, out pendenti))
                return;

            foreach (var stream in pendenti)
            {
                if (!KeepInSync || !stream.StatusFetched || !stream.BackfillDone)
                    continue;

                if (stream.Series.Count < 2)
                    continue;

                // La barra in formazione e' l'ultima della serie: tutto cio' che comincia prima di
                // lei e' definitivamente chiuso, ed e' la frontiera. Non c'e' piu' un bucket da
                // aspettare, perche' il bucket coincide con la barra.
                var frontier = DateTime.SpecifyKind(stream.Series.OpenTimes[stream.Series.Count - 1], DateTimeKind.Utc);
                if (frontier <= stream.LastLiveFrontierUtc)
                    continue;

                var from = frontier.AddMinutes(-(double)(LiveHealingBars + 2));

                DateTime oldest;
                bool truncated;
                var candles = CollectBackwards(stream, from, frontier, LiveHealingBars, out oldest, out truncated);
                if (candles.Count == 0)
                    continue;

                stream.LastLiveFrontierUtc = frontier;

                // A regime si compatta a ogni invio: sono poche barre, e il file su disco deve essere
                // sempre quello vero — e' il motivo per cui si tiene acceso il bot.
                SendBars(stream, candles, candles[0].DateTime, candles[candles.Count - 1].DateTime, compact: true);
            }
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
                        Source = SourceTag(stream),
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

        // -----------------------------------------------------------------------------------------
        // Lettura della serie
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Percorre la serie da un minuto all'indietro fra due istanti e restituisce le barre in
        /// ordine <b>cronologico</b> — come le vuole il server — insieme all'apertura della piu'
        /// vecchia emessa.
        ///
        /// <para><b>All'indietro</b> perche' e' il verso in cui il broker consegna la storia, e
        /// perche' e' il verso in cui si tronca: <paramref name="maxCandles"/> ferma il giro e il
        /// cursore riparte esattamente da li'.</para>
        ///
        /// <para><b>L'ultima barra della serie non si guarda mai</b>: si parte da <c>Count - 2</c>.
        /// E' quella in formazione, e una barra a meta' salvata nel feed e' un dato falso che poi
        /// nessuno distingue piu' da uno vero.</para>
        ///
        /// <para><b>Non c'e' piu' niente da piegare.</b> Fino alla 1.3.1 questo metodo si chiamava
        /// <c>FoldBackwards</c> e costruiva qui i bucket dei timeframe alti, con l'ancoraggio, il
        /// fuso e le convenzioni sul cambio d'ora replicati dentro il bot — una delle quattro copie
        /// della stessa regola. Adesso il bot raccoglie il minuto e basta: la griglia la costruisce
        /// il server, in un punto solo. Vedi <c>docs/domini/layer-barre-e-calendario.md</c>.</para>
        /// </summary>
        private List<CandleDto> CollectBackwards(SyncStream stream, DateTime fromUtc, DateTime toUtc,
            int maxCandles, out DateTime oldestSentUtc, out bool truncated)
        {
            var candles = new List<CandleDto>();
            oldestSentUtc = fromUtc;
            truncated = false;

            var series = stream.Series;

            for (var i = series.Count - 2; i >= 0; i--)
            {
                var openTime = DateTime.SpecifyKind(series.OpenTimes[i], DateTimeKind.Utc);
                if (openTime >= toUtc)
                    continue;
                if (openTime < fromUtc)
                    break;

                candles.Add(new CandleDto
                {
                    DateTime = openTime,
                    Open = (decimal)series.OpenPrices[i],
                    High = (decimal)series.HighPrices[i],
                    Low = (decimal)series.LowPrices[i],
                    Close = (decimal)series.ClosePrices[i],
                    Volume = (decimal)series.TickVolumes[i]
                });

                oldestSentUtc = openTime;

                if (candles.Count >= maxCandles)
                {
                    truncated = true;
                    break;
                }
            }

            candles.Reverse();
            return candles;
        }

        /// <summary>
        /// La targa che il server scrive nel campo <c>source</c> del feed.
        ///
        /// <para>Non dichiara piu' alcuna griglia, e non e' una perdita di informazione: a un
        /// minuto la griglia non esiste: il bucket coincide con la barra. La griglia la dichiara
        /// l'aggregato, che ora lo produce il server ed e' lui a firmarlo.</para>
        /// </summary>
        private string SourceTag(SyncStream stream)
        {
            return string.Format("PiootooDatafeedSyncBot/{0}@{1} solo 1m UTC",
                BotVersion, Account.BrokerName);
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

            // Una sottoscrizione per serie, non per stream: vedi TryBuildStreams.
            foreach (var series in _bySeries.Keys)
                series.BarOpened -= OnSeriesBarOpened;

            // Di nuovo allo stop, non solo a fine backfill: con 'Resta in sincronia' il bot
            // continua a spedire minuti per ore o giorni dopo che il backfill e' finito, e quella
            // coda non sarebbe in nessun aggregato. La chiamata e' idempotente — riscrive dal
            // minuto, che e' il dato autorevole — quindi rifarla non costa correttezza.
            if (_backfillReported)
                RequestAggregateRebuild();

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

            /// <summary>
            /// Apertura della barra in formazione all'invio precedente. Serve a non rispedire — e a
            /// non far ricompattare al server — le stesse barre piu' di una volta.
            /// </summary>
            public DateTime LastLiveFrontierUtc;

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

            /// <summary>
            /// La cartella di datafeed-external/ in cui va questo feed. La decide il registro dei
            /// broker lato server, che e' lo stesso posto da cui il server la rilegge: un nome
            /// costruito qui da Account.BrokerName sarebbe un secondo nome per la stessa cosa.
            /// </summary>
            public string DatafeedBroker { get; set; }

            public List<PlanInstrumentDto> Instruments { get; set; }
        }

        /// <summary>Cosa raccogliere, prima di aprire le serie: da parametri o dal piano.</summary>
        private sealed class StreamRequest
        {
            public string BrokerSymbol;
            public string PiootooSymbol;
        }

        private sealed class RebuildStreamDto
        {
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }
            public bool Rebuilt { get; set; }
            public string Skipped { get; set; }
            public int BarsBefore { get; set; }
            public int BarsAfter { get; set; }
            public int OffGridBefore { get; set; }
        }

        private sealed class RebuildResponseDto
        {
            public List<RebuildStreamDto> Streams { get; set; }
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
