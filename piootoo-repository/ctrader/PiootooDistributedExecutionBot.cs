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
    ///  - fa polling periodico chiedendo al server "qual è il prossimo segnale per il MIO account". In
    ///    live a ogni battito del timer, perché è l'unico canale che scopre i template nati dalla push
    ///    del bot di un altro account e la prima chiamata che si accorge del server tornato su; in
    ///    backtest solo dopo una push che dichiara qualcosa da consegnare, o dopo un evento locale che
    ///    può aver liberato un lucchetto (vedi <c>ShouldPollOnTimer</c>);
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
    /// <summary>
    /// Che run sta aprendo il cBot. E' l'unico interruttore fra i due backtest che il progetto
    /// distingue, e li NOMINA invece di farli dedurre da una combinazione di flag sparsi fra piano e
    /// sessione. Deve corrispondere a <c>TradingRunProfile</c> lato server.
    /// </summary>
    public enum RunProfileParam
    {
        /// <summary>Decide il piano, com'e' sempre stato. In live e' l'unico valore ammesso.</summary>
        DalPiano,

        /// <summary>
        /// Backtest sorgente: tutte le strategie del masterfilter, nessuna rotazione Titano e
        /// nessun lucchetto di concorrenza. E' il run che produce il trades.json su cui Titano
        /// calcola le rotazioni, quindi ogni segnale deve diventare un intent.
        /// </summary>
        BacktestSorgente,

        /// <summary>
        /// Backtest a filtro statico: strategie del masterfilter come nel sorgente, ma con i
        /// lucchetti di concorrenza e distribuzione ATTIVI. E' il termine di paragone che isola il
        /// merito della rotazione: fra questo e BacktestTitano cambia solo il filtro — statico
        /// contro dinamico — e non i vincoli operativi. Non legge nessuna cartella di run Titano.
        /// </summary>
        BacktestStaticFilter,

        /// <summary>
        /// Backtest filtrato con le rotazioni storiche gia' generate da Titano, e con i lucchetti di
        /// distribuzione attivi: serve a misurare cosa avrebbe fatto il sistema *con* il filtro.
        /// </summary>
        BacktestTitano
    }

    /// <summary>
    /// Quanto deve parlare il bot. Sostituisce il vecchio flag "Log dettagliato": i livelli sono
    /// cumulativi e ordinati, cosi' il confronto e' un <c>&gt;=</c> e non una collezione di booleani
    /// che si contraddicono.
    ///
    /// <para>Il vincolo che detta la scala e' il buffer di log della piattaforma: in backtest si
    /// riempie in fretta e, quando lo fa, cTrader butta via le righe piu' VECCHIE — cioe' proprio
    /// quelle dell'avvio, dove stanno le cause. Per questo il livello effettivo in backtest e'
    /// tagliato a <see cref="Minimo"/> (vedi <c>_livelloEffettivo</c>): la diagnostica ripetuta a
    /// ogni barra ha senso in live, dove il log scorre in tempo reale e la sessione dura un giorno,
    /// non su tre anni di storia.</para>
    ///
    /// <para><b>Fuori scala, e sempre attivo a qualunque livello:</b> tutto cio' che riguarda un
    /// segnale — intent ricevuto con Bid/Ask, ingresso scartato o annullato, anomalia sul livello,
    /// fill con lo spread, errore di apertura o chiusura. E' l'unica traccia di *perche'* il bot ha
    /// fatto o non ha fatto un trade, ed e' proporzionale ai trade e non alle barre: anche su un
    /// backtest lungo resta un ordine di grandezza sotto il rumore che satura il buffer. Il livello
    /// governa il contorno — riscaldamento, finestre, poll — non i segnali.</para>
    /// </summary>
    public enum LivelloLog
    {
        /// <summary>
        /// Solo i segnali (sempre attivi, vedi sopra) piu' avvio e riepiloghi di fine run. Nessuna
        /// riga legata al ciclo delle barre. E' il livello a regime, e l'unico che gira in backtest.
        /// </summary>
        Minimo,

        /// <summary>
        /// Aggiunge il ciclo di alimentazione degli stream: riscaldamento inviato, storia disponibile
        /// per stream, finestre accodate. Serve a capire se il server sta ricevendo le candele, non
        /// se sta tradando bene.
        /// </summary>
        Operativo,

        /// <summary>
        /// Tutto: poll falliti, break-even e trailing non riusciti, e ogni altro dettaglio del giro
        /// HTTP. E' il livello dei test di verifica del bot, non quello di esercizio.
        /// </summary>
        Diagnostico
    }

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
        /// Dopo tanti invii falliti di fila, senza uno riuscito in mezzo, il bot lo dice a chiare
        /// lettere: log e pannello passano da "connessione persa" a "il server non risponde da N
        /// invii", perche' un errore di configurazione — piano che punta a una rotazione inesistente,
        /// sessione fermata, token scaduto — non si risolve da solo e va distinto da un buco di rete.
        ///
        /// <para>In live NON ferma il bot: un cBot con posizioni aperte che si spegne le lascia senza
        /// nessuno che le gestisca, che e' peggio del server irraggiungibile. Si continua a provare a
        /// ogni barra e si riparte da soli quando il server torna. Lo stop resta solo in backtest,
        /// dove non c'e' niente da proteggere e un run mutilato e' solo un risultato falso.</para>
        /// </summary>
        private const int MaxConsecutivePushFailures = 20;

        /// <summary>
        /// Secondi minimi fra due tentativi di riaggancio in live. A server giu' il tentativo fallisce
        /// in fretta — un socket rifiutato non costa quasi niente — ma ripeterlo a ogni barra di ogni
        /// stream riempirebbe il log e allungherebbe il thread della piattaforma senza guadagnarci
        /// nulla: la connessione non torna perche' la si chiede piu' spesso.
        /// </summary>
        private const int ReopenCooldownSeconds = 30;

        /// <summary>
        /// Tetto agli intent reclamati in un solo giro di drenaggio (solo a lucchetti spenti). Non e'
        /// un limite operativo — con i lucchetti spenti si vogliono TUTTI i segnali della barra — ma
        /// una rete contro un server che continuasse a consegnare: il claim gira sul thread della
        /// piattaforma, e un ciclo che non finisce blocca il cBot invece di far apparire un errore.
        /// </summary>
        private const int MaxSignalsPerDrain = 200;

        // 4.0.1 (03/09/2026) — il pannello del grafico non elenca piu' le strategie una per una: su
        // un piano vero erano decine di righe che coprivano il grafico, e le strategie sullo stesso
        // stream ripetevano lo stesso conteggio di candele, perche' la finestra e' una proprieta'
        // dello stream. Restano il NUMERO di strategie caricate e una riga sola sulla storia, che
        // conta gli stream pronti e nomina solo quelli corti — l'unico avviso che non si poteva
        // perdere, perche' su uno stream corto il server salta le strategie in silenzio.
        //
        // 3.9.0 (26/08/2026) — solo lato server, il bot non cambia: il poll riconcilia le posizioni
        // aperte con lo snapshot che il bot manda, e quello che il broker non elenca piu' viene
        // tolto dai registri. Prima una chiusura non riportata — stop loss nativo, evento perso,
        // chiamata fallita — lasciava una posizione fantasma e la strategia non apriva piu' niente
        // per il resto del run. Resta da chiudere il lato bot: CloseExpiredPositions scarta dal
        // registro locale le posizioni gia' sparite da Positions senza riportarle a nessuno.
        //
        // 2.4.1 (15/08/2026) — via l'estensione dei pending identici (2.3.0) e l'attesa prima del
        // ritiro (2.3.1), con i loro due parametri. Il ritiro degli ordini scaduti torna a essere la
        // PRIMA cosa della barra: chiusura, push, richiesta dei segnali nuovi. Non e' una preferenza
        // di stile, e' l'unico ordine possibile — il server non rilascia il template finche' l'intent
        // vecchio e' Pending, e l'intent vecchio resta Pending finche' il cBot non ne riporta la
        // cancellazione. Il cBot aspettava il segnale per rinnovare l'ordine e il server aspettava il
        // report per consegnare il segnale: in un mese di backtest la riga "non riemesso: ordine
        // gia' a mercato" non e' comparsa nemmeno una volta, e l'attesa della 2.3.1 ha solo aggiunto
        // 8 secondi di latenza a ogni piazzamento senza togliere una sola coppia cancella/ripiazza.
        // Vedi docs/decisioni.md 2026-08-15.
        //
        // 2.4.0 (15/08/2026) — nuovo profilo BacktestStaticFilter: strategie del masterfilter come
        // nel sorgente, lucchetti attivi come in BacktestTitano. E' il termine di paragone che
        // mancava — fra questo e BacktestTitano cambia solo il filtro, statico contro dinamico,
        // quindi la differenza fra i due run misura il merito della rotazione e non l'effetto del
        // tetto di concorrenza. I profili espliciti ora DICHIARANO i lucchetti e il piano non li
        // contraddice: prima solo BacktestSorgente era blindato, e un piano con
        // EnforceConcurrencyLimits=false rendeva BacktestTitano un run senza vincoli che continuava
        // a chiamarsi Titano. Vedi docs/decisioni.md 2026-08-15.
        //
        // 2.3.1 (15/08/2026) — il ritiro dei pending scaduti diventa differito di
        // PendingRetirementGraceSeconds invece che immediato. Senza l'attesa, "Estendi i pending
        // identici" (2.3.0) non lavorava mai: il ritiro stava dopo il poll post-push, ma quel poll
        // torna quasi sempre a vuoto e l'intent della barra nuova arriva col polling periodico
        // qualche centinaio di ms dopo, con l'ordine da riconoscere gia' cancellato. Nei log 2.3.0
        // la riga "non riemesso: ordine gia' a mercato" non compare mai e resta una coppia
        // cancella/ripiazza per barra sullo stesso prezzo. Vedi docs/decisioni.md 2026-08-15.
        //
        // 2.3.0 (13/08/2026) — esecuzione difensiva e consuntivo dei trade. Gli intent con il livello
        // dal lato sbagliato, troppo lontani dal mercato o con lo spread troppo pesante sullo stop
        // ora vengono SCARTATI e non piu' soltanto segnalati; il trailing ha un passo minimo e un
        // intervallo minimo fra le modifiche; un pending identico a quello gia' a mercato viene
        // esteso invece che cancellato e ripiazzato; l'"eta" dell'intent, che era calcolata su un
        // istante futuro per costruzione, diventa ritardo (dalla barra del segnale) e attesa (alla
        // validita'); a ogni chiusura si stampa il consuntivo del trade e a fine run la tabella per
        // strategia. Vedi docs/decisioni.md 2026-08-13.
        //
        // 2.2.0 (12/08/2026) — diagnostica dei segnali: Bid/Ask, distanza dal lato di ingresso, eta'
        // dell'intent e coerenza del livello pending stampati all'arrivo di ogni intent (sempre, a
        // qualunque livello) e scritti sul JSONL. Il flag "Log dettagliato" diventa il parametro a
        // scala "Livello di log", tagliato a Minimo in backtest.
        //
        // 2.1.0 (11/08/2026) — l'autolimitazione locale passa da (simbolo) a (strategia, simbolo),
        // tetto locale sulle posizioni prima dell'invio, cancellazione OCO degli ordini rimasti in
        // modalita' PositionsOnly. Vedi docs/decisioni.md 2026-08-11.
        // ATTENZIONE: questa versione e' condivisa con il server, ma il contratto e' major.minor:
        // e' quella parte che deve restare uguale a Piootoo.Shared.PiootooVersion.Current. La patch
        // e' per le fix e puo' divergere — il server puo' passare da 3.11.0 a 3.11.1 senza che
        // questo bot vada ricompilato e ridistribuito su ogni macchina.
        // Server e bot non condividono una build (questo file lo compila cTrader, che non referenzia
        // le assembly della solution), quindi la sincronia e' manuale: la verifica VersioneDelProgettoTests
        // leggendo questo sorgente.
        // Il disallineamento non blocca nulla: entrambi stampano la propria versione all'avvio, e
        // il confronto si fa leggendo i due log.
        private const string BotVersion = "5.0.0"; // major.minor deve seguire PiootooVersion
        private const string StatusChartObjectName = "PiootooConnectionStatus";

        // Riquadro rosso al centro del grafico, separato dal pannello di stato: e' l'errore fatale
        // che ha spento il bot. Deve restare visibile a run finito, quando il log del backtest ha
        // gia' scartato le righe piu' vecchie.
        private const string FatalErrorChartObjectName = "PiootooFatalError";

        [Parameter("Server Base Url", DefaultValue = "http://localhost:5000")]
        public string ServerBaseUrl { get; set; }

        [Parameter("Codice piano")]
        public string PlanCode { get; set; }

        // Il run mode (Backtest/Realtime) lo dichiara la piattaforma, non l'utente: qui si sceglie
        // solo QUALE backtest. In live il server rifiuta i profili Backtest* invece di eseguirli,
        // cosi non e' possibile mandare a mercato un run configurato come campione sorgente.
        [Parameter("Profilo di esecuzione", DefaultValue = RunProfileParam.DalPiano)]
        public RunProfileParam RunProfile { get; set; }

        // A regime la storia del server è già completa: della finestra serve solo il margine che
        // ricuce le barre eventualmente perse (chiamata fallita, server irraggiungibile per qualche
        // giro). Venti barre coprono diciannove buchi consecutivi; oltre, il server rifiuta la
        // finestra invece di accodare una serie bucata, e lo si vede subito nel log.
        [Parameter("Barre per finestra a regime", DefaultValue = 20, MinValue = 2)]
        public int IncrementalWindowBars { get; set; }

        [Parameter("Polling segnali (secondi)", DefaultValue = 2, MinValue = 1)]
        public int PollingSeconds { get; set; }

        // Interruttore di confronto, non di taratura: rimette il poll a ogni battito del timer anche
        // in backtest, cioe' il comportamento fino alla 3.8.0. Serve a poter fare A/B con lo STESSO
        // binario — stesso run due volte, un solo parametro diverso, e trades.json a confronto — e a
        // riavere il vecchio comportamento senza ricompilare se un giorno saltasse fuori un caso che
        // ShouldPollOnTimer non prevede. In live non ha alcun effetto: li' il poll a timer c'e' sempre.
        [Parameter("Poll a timer anche in backtest", DefaultValue = false)]
        public bool PollOnTimerInBacktest { get; set; }

        [Parameter("Max Entry Slippage (Pips)", DefaultValue = 5.0, MinValue = 0)]
        public double MaxEntrySlippagePips { get; set; }

        // Un livello pending dal lato sbagliato del mercato non e' una condizione di mercato: e' un
        // difetto di prezzatura del server (barra vecchia, segnale ricalcolato male). Eseguirlo
        // trasforma uno stop in un market a prezzo peggiore, cioe' esattamente il contrario di quello
        // che la strategia chiedeva. Fino alla 2.2.0 il bot lo segnalava e lo piazzava lo stesso.
        [Parameter("Scarta livelli dal lato sbagliato", DefaultValue = true, Group = "Filtri di ingresso")]
        public bool RejectWrongSideLevels { get; set; }

        // Distanza massima fra il prezzo corrente e il livello di un pending, in punti dello
        // strumento. Serve a scartare un intent vecchio che il server continua a riproporre, non a
        // giudicare quanto lontano una strategia mette il proprio ingresso: quello e' un fatto di
        // progetto della strategia, non un difetto.
        //
        // <para>Fino alla 3.0.0 la misura era in multipli dello stop, e quella scala era sbagliata.
        // Una strategia con stop stretto e ingresso sul massimo della sessione precedente — SBO_003,
        // stop 25 punti, livello 150-280 punti sopra il mercato — veniva ammessa solo quando il
        // breakout era gia' vicino, e bloccata quando era lontano. Non e' un filtro: e' una
        // selezione sistematica delle condizioni favorevoli, che nella sorgente Python non
        // esiste.</para>
        //
        // Zero = nessun limite.
        [Parameter("Distanza max pending (punti)", DefaultValue = 500.0, MinValue = 0, Group = "Filtri di ingresso")]
        public double MaxEntryDistancePoints { get; set; }

        // Peso massimo dello spread sullo stop della strategia, in percentuale. Su uno stop da 12,5
        // punti uno spread di 2,5 e' il 20%: lo stop deve reggere un quinto di respiro in meno di
        // quanto la strategia ha ipotizzato. Non e' un difetto del bot, e' una coppia
        // strategia/strumento sbagliata in quell'istante. Zero = nessun limite.
        [Parameter("Spread max sullo stop (%)", DefaultValue = 20.0, MinValue = 0, Group = "Filtri di ingresso")]
        public double MaxSpreadPercentOfStop { get; set; }

        // Passo minimo di un aggiornamento del trailing, come frazione della distanza di trailing
        // dichiarata dall'intent. Senza questo il bot insegue il Bid tick per tick e produce decine
        // di ModifyPosition da un decimo di punto, alcune nello stesso secondo: in backtest e' solo
        // rumore, in live e' rate limit del broker e reject.
        [Parameter("Passo minimo trailing (frazione)", DefaultValue = 0.10, MinValue = 0, MaxValue = 1, Group = "Trailing")]
        public double TrailingMinStepFraction { get; set; }

        [Parameter("Intervallo minimo trailing (secondi)", DefaultValue = 5, MinValue = 0, Group = "Trailing")]
        public int TrailingMinIntervalSeconds { get; set; }

        [Parameter("Http Timeout (secondi)", DefaultValue = 10, MinValue = 1)]
        public int HttpTimeoutSeconds { get; set; }

        [Parameter("History Window (giorni)", DefaultValue = 30, MinValue = 1)]
        public int HistoryWindowDays { get; set; }

        // Default Diagnostico finche' dura la verifica del comportamento del bot; a regime si scende
        // a Operativo (o Minimo), senza perdere le righe dei segnali che sono fuori scala. In
        // backtest il valore viene comunque tagliato a Minimo, vedi LivelloLog e _livelloEffettivo.
        [Parameter("Livello di log", DefaultValue = LivelloLog.Diagnostico, Group = "Diagnostica")]
        public LivelloLog LivelloDiLog { get; set; }

        // Traccia su file, una riga JSON per risposta, tutto cio' che il server Piootoo restituisce
        // (apertura sessione, poll segnale, chiusura esterna): serve a diagnosticare da cliente senza
        // dover riprodurre il problema con il log a Diagnostico, che stampa ma non persiste — e senza
        // dipendere dal buffer della piattaforma, che le righe piu' vecchie le butta.
        [Parameter("Log JSON risposte server su file", DefaultValue = false, Group = "Diagnostica")]
        public bool LogServerResponses { get; set; }

        // Nessun parametro di chiusura forzata: overnight, overweek e i loro orari li dichiara il
        // PIANO e li consegna il descriptor. Finche' sono vissuti qui, il bot poteva contraddire il
        // piano che diceva di eseguire — un parametro spento a mano operava il venerdi' sera contro
        // un backtest che quei trade li tagliava, e la differenza non compariva da nessuna parte.
        //
        // Il flat resta comunque una regola di SICUREZZA di questo bot, non un ordine che il server
        // impartisce barra per barra: la policy ricevuta all'apertura continua a valere anche a
        // server muto, ed e' per questo che vive in campi locali invece che in una chiamata.

        // Policy di tenuta risolta dal descriptor. I default riproducono il comportamento storico
        // (overnight libero, fine settimana sempre piatto) e valgono nella finestra fra l'avvio e
        // la prima apertura di sessione riuscita, quando il piano non e' ancora noto.
        private bool _allowOvernight = true;
        private bool _allowOverweek;
        private int _sessionFlatUtcHhmm = 2045;

        private HttpClient _http;
        private string _accountNumber;
        private string _sessionId;
        private string _sessionToken;

        // Configurazione RISOLTA dal server, letta dal descriptor all'apertura. Serve al pannello e
        // decide se ha senso drenare la coda dei segnali (vedi PollNextSignal).
        private string _runProfile;
        private string _titanoMode;
        private string _serverRunMode;
        private bool _enforceConcurrency = true;
        private int _maxConcurrentTrades;

        /// <summary>
        /// Orario di flat del fine settimana in vigore, in HHMM UTC. Nasce dai parametri e viene
        /// sovrascritto dal descriptor appena la sessione si apre: il numero e' del server, perche'
        /// deve essere lo stesso che usa il backtest. I parametri restano la rete per il caso in cui
        /// il server non lo dichiari (versione vecchia) e per la finestra prima dell'apertura.
        /// </summary>
        private int _weekEndFlatFromUtc = 2045;
        private int _weekEndFlatUntilUtc = 2300;

        /// <summary>
        /// Il piano conta solo le posizioni riempite: gli ordini pendenti non consumano budget lato
        /// server, quindi al raggiungimento del tetto tocca a questo bot spegnere quelli rimasti.
        /// E' l'unica parte del limite di concorrenza che vive sul client, e vive qui perche' e'
        /// l'unico posto che sa, nell'istante del fill, cosa c'e' ancora a mercato.
        /// </summary>
        private bool _cancelPendingAtCap;
        private IReadOnlyList<SessionStrategyDto> _strategies = new List<SessionStrategyDto>();

        /// <summary>
        /// Spread misurati ai fill, per strategia. Non influenza nessuna decisione: serve al
        /// riepilogo di fine run, che e' il numero con cui si decide se una strategia ha senso su
        /// questo strumento.
        /// </summary>
        private readonly Dictionary<string, SpreadStats> _spreadByStrategy =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Esito dei trade chiusi, per strategia. Come gli spread non decide niente: e' il
        /// consuntivo con cui a fine run si risponde a "ha guadagnato?", che dai soli fill non si
        /// risponde.
        /// </summary>
        private readonly Dictionary<string, TradeStats> _tradeStats =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Quanto il server ha dichiarato di avere da consegnare nell'ultimo push. Null = non
        /// dichiarato, quindi si polla. Zero = si puo' saltare il poll.
        /// </summary>
        private int? _lastPushClaimable;

        /// <summary>Poll saltati grazie alla guardia, per poterne stampare il totale allo stop.</summary>
        private long _skippedPolls;

        /// <summary>
        /// Un evento locale ha reso il lucchetto piu' largo di quanto fosse all'ultimo claim andato a
        /// vuoto: una posizione chiusa (slot di concorrenza libero, ingresso della strategia non piu'
        /// "in volo") o un execution report che assesta un intent lato server.
        ///
        /// <para>E' la sola cosa che, in backtest, puo' far passare un claim da "no" a "si'" senza che
        /// sia arrivata una barra nuova: vedi <see cref="ShouldPollOnTimer"/>.</para>
        /// </summary>
        private bool _claimRetryPending;

        /// <summary>Chiamate HTTP per endpoint, per misurare il traffico di un run allo stop.</summary>
        private long _pushCalls;
        private long _pollCalls;

        /// <summary>
        /// Quante volte ha battuto il timer. Non serve al bot: serve a rendere visibile il rapporto fra
        /// battiti e barre, che e' l'unico modo di sapere quanto costava davvero il poll periodico in
        /// backtest — il timer li' gira sull'orologio simulato, e quel numero non lo si puo' dedurre.
        /// </summary>
        private long _timerTicks;
        private string _localStatePath;
        private string _jsonLogPath;

        /// <summary>
        /// Livello di log realmente in vigore: il parametro in live, tagliato a
        /// <see cref="LivelloLog.Minimo"/> in backtest. Il taglio sta qui e non ai punti di stampa
        /// perche' e' una regola sola, e sparpagliarla come <c>&amp;&amp; !IsBacktesting</c> su venti
        /// righe e' il modo per dimenticarsene sulla ventunesima.
        /// </summary>
        private LivelloLog _livelloEffettivo = LivelloLog.Minimo;

        /// <summary>Il ciclo di alimentazione degli stream va stampato.</summary>
        private bool LogOperativo => _livelloEffettivo >= LivelloLog.Operativo;

        /// <summary>Il dettaglio del giro HTTP va stampato.</summary>
        private bool LogDiagnostico => _livelloEffettivo >= LivelloLog.Diagnostico;

        // Stato di connessione mostrato a chart: riflette l'esito dell'ultima chiamata HTTP al
        // server Piootoo (open-plan, push barre, polling segnale), non solo l'apertura iniziale.
        private bool _isConnectedToServer;

        /// <summary>Ultimo testo disegnato nel riquadro, per non ridisegnarlo identico a ogni barra.</summary>
        private string _lastStatusText;

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

            /// <summary>
            /// Candele che il SERVER dichiara di avere su questo stream, dall'ultima risposta a
            /// <c>bars/window</c>. È l'unico numero che conta per sapere se le strategie vengono
            /// valutate: la serie del broker può essere lunga e la finestra arrivare comunque corta
            /// (push fallito, finestra rifiutata perché non sovrapposta). Null finché il server non
            /// ha ancora risposto: in quel caso sul pannello si ripiega sul conteggio locale.
            /// </summary>
            public int? ServerHistoryBars;

            /// <summary>
            /// Candele che il server dichiara di richiedere su questo stream. Normalmente coincide con
            /// <see cref="RequiredCandles"/>; se diverge vince questo, perché è quello su cui il server
            /// sta davvero decidendo se saltare la valutazione.
            /// </summary>
            public int? ServerRequiredCandles;

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
            /// <summary>Lato dell'ordine: la label non lo porta, e la potatura per strategia deve distinguerlo.</summary>
            public TradeType Side;
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

            /// <summary>
            /// Ultimo istante in cui il trailing ha davvero modificato lo stop. Con
            /// <c>TrailingMinIntervalSeconds</c> e' cio' che impedisce la raffica di modifiche nello
            /// stesso secondo.
            /// </summary>
            public DateTime? LastTrailingUpdateUtc { get; set; }

            /// <summary>Quante volte il trailing ha spostato lo stop. Va nel log di chiusura: dice se la protezione ha lavorato o se il trade e' morto sullo stop iniziale.</summary>
            public int TrailingUpdates { get; set; }

            // --- Misure per il consuntivo del trade. Non influenzano nessuna decisione. ---

            /// <summary>Prezzo a cui la posizione si e' aperta davvero, non quello dell'intent.</summary>
            public double EntryPrice { get; set; }

            /// <summary>Apertura della posizione, per misurarne la durata alla chiusura.</summary>
            public DateTime? OpenTimeUtc { get; set; }

            /// <summary>
            /// MFE: massimo movimento a favore, in punti, mai raggiunto mentre la posizione era
            /// aperta. Confrontato col take profit dice se il target era irraggiungibile o se il
            /// trade era in utile e lo ha restituito.
            /// </summary>
            public double MaxFavorablePoints { get; set; }

            /// <summary>
            /// MAE: massimo movimento contrario, in punti. Confrontato con lo stop dice quanto
            /// margine e' rimasto: un MAE sistematicamente vicino allo stop sui trade vincenti
            /// significa che lo stop e' appena sufficiente, non che e' giusto.
            /// </summary>
            public double MaxAdversePoints { get; set; }
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

        /// <summary>Ultimo tentativo di riaggancio, per rispettare <see cref="ReopenCooldownSeconds"/>.</summary>
        private DateTime? _lastReopenAttemptUtc;

        /// <summary>
        /// Riaggancio in corso: il riscaldamento che spedisce puo' fallire e rientrare in
        /// <see cref="OnPushFailed"/>, che chiederebbe un altro riaggancio. Questa e' la guardia.
        /// </summary>
        private bool _reopenInProgress;

        // Ultimo motivo per cui il claim non ha restituito un intent: stampato una volta sola finché
        // non cambia, altrimenti riempirebbe il log a ogni poll.
        private string _lastPollReason;

        // Massimo utile per contratto osservato dopo ProfitStallAfterUtc, per posizione.
        private readonly Dictionary<int, decimal> _peakProfitAfterStall = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(PlanCode))
            {
                StopWithError("Codice piano non impostato: valorizzare il parametro 'Codice piano'.");
                return;
            }

            // Il taglio in backtest e' silenzioso solo se non lo si dice: senza questa riga, chi ha
            // lasciato il parametro su Diagnostico e non vede il dettaglio pensa a un bug.
            _livelloEffettivo = IsBacktesting ? LivelloLog.Minimo : LivelloDiLog;
            if (IsBacktesting && LivelloDiLog != _livelloEffettivo)
                Print("Livello di log {0} ridotto a {1} in backtest: restano le righe dei segnali. " +
                      "Il buffer della piattaforma scarta le righe piu' vecchie, cioe' proprio quelle dell'avvio.",
                    LivelloDiLog, _livelloEffettivo);

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

            // All'AVVIO il server deve esserci: senza descriptor il bot non conosce ne' strategie ne'
            // strumenti, quindi non c'e' niente da far partire e niente da proteggere. Si spegne, e lo
            // scrive sul grafico. E' l'unico caso in cui la mancanza di connessione e' fatale in live:
            // a run avviato invece si resta accesi e si riaggancia (vedi TryReopenSession).
            if (!TryOpenSession(out var descriptor, out var openError))
            {
                StopWithError("Avvio annullato: " + openError);
                return;
            }
            LogSessionDescriptor();

            var pairs = new List<Pair>();
            var error = "descriptor sessione mancante";
            if (descriptor == null ||
                !BuildPairs(descriptor.Instruments, out pairs, out error))
            {
                StopWithError("Configurazione strumenti del piano non valida:\n" + error);
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
                    StopWithError("Simbolo '" + pair.AccountSymbol + "' non disponibile su questo account:\n" +
                                  "stream " + pair + " non avviabile.");
                    return;
                }

                pair.Series = MarketData.GetBars(ToTimeFrame(pair.TimeframeMinutes), pair.AccountSymbol);
                if (pair.Series is null)
                {
                    StopWithError("Serie " + pair + " non disponibile su questo account.");
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
        /// Una singola <c>open-plan</c>: apre la sessione sul server e ne applica il descriptor.
        /// Usata sia all'avvio sia dal riaggancio a run in corso, perche' devono chiedere esattamente
        /// la stessa cosa — un secondo punto di costruzione della richiesta e' un secondo posto dove
        /// dimenticarsi un campo.
        ///
        /// <para>Non stampa e non spegne niente: chi la chiama decide se un fallimento e' fatale
        /// (avvio) o solo l'ennesimo tentativo andato a vuoto (riaggancio).</para>
        /// </summary>
        private bool TryOpenSession(out TradingSessionDescriptorDto descriptor, out string error)
        {
            descriptor = null;
            error = null;
            HttpResponseMessage response;
            try
            {
                response = PostJson("api/v1/trading-sessions/open-plan", new OpenTradingPlanSessionRequestDto
                {
                    PlanCode = PlanCode.Trim(),
                    ClientRunMode = IsBacktesting ? "Backtest" : "Realtime",
                    ExecutionKey = IsBacktesting ? $"BT-{Server.TimeInUtc:yyyyMMddHHmmss}" : "LIVE",
                    AccountNumber = _accountNumber,
                    // Null invece di "DalPiano": un campo assente lascia decidere il piano, ed e'
                    // esattamente il comportamento storico per i server che non conoscono il campo.
                    RunProfile = RunProfile == RunProfileParam.DalPiano ? null : RunProfile.ToString()
                });
            }
            catch (Exception ex)
            {
                // Nessuna risposta HTTP affatto: server spento, url o porta sbagliati, rete assente.
                // Senza questo catch l'eccezione risale a cTrader, che spegne il cBot con un errore di
                // piattaforma e lascia il grafico muto.
                error = "nessuna connessione al server Piootoo (" + Or(ServerBaseUrl) + ").\n" +
                        DescribeTransportFailure(ex) + "\n" +
                        "Verificare che il server sia avviato e che 'Server Base Url' sia corretto.";
                UpdateConnectionStatus(false);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                error = "il server ha rifiutato il piano '" + PlanCode + "':\n" + ReadError(response);
                UpdateConnectionStatus(false);
                return false;
            }

            var body = ReadBody(response);
            LogJsonResponse("open-plan", body);
            descriptor = JsonSerializer.Deserialize<TradingSessionDescriptorDto>(body, _json);
            if (descriptor == null)
            {
                error = "il server ha risposto con un descriptor di sessione vuoto o illeggibile.";
                UpdateConnectionStatus(false);
                return false;
            }

            ApplyDescriptor(descriptor);
            return true;
        }

        /// <summary>
        /// Copia nel bot come il server ha RISOLTO il run: se il piano contraddice il parametro vince
        /// il server, e il pannello deve mostrare cio' che gira davvero, non cio' che e' stato chiesto.
        /// </summary>
        private void ApplyDescriptor(TradingSessionDescriptorDto descriptor)
        {
            _sessionId = descriptor.SessionId;
            _sessionToken = descriptor.SessionToken;
            _runProfile = descriptor.RunProfile;
            _titanoMode = descriptor.TitanoMode;
            _serverRunMode = descriptor.ClientRunMode;
            _enforceConcurrency = descriptor.EnforceConcurrencyLimits;
            _maxConcurrentTrades = descriptor.MaxConcurrentTrades;
            _cancelPendingAtCap = string.Equals(
                descriptor.ConcurrencyCountMode, "PositionsOnly", StringComparison.OrdinalIgnoreCase);
            _strategies = descriptor.Strategies ?? new List<SessionStrategyDto>();

            ApplyHolding(descriptor.Holding);
        }

        /// <summary>
        /// Cosa il conto concede, come lo ha deciso il piano. E' l'unico modo perche' backtest
        /// interno e conto vero taglino negli stessi istanti: finche' i numeri sono vissuti nel bot,
        /// il backtest ne aveva altri (l'ultimo slot del proprio orologio prima di sabato, le 23:30
        /// contro le 20:45) e i due run non erano confrontabili.
        ///
        /// <para>Un descriptor senza policy, o con orari implausibili, lascia i default storici
        /// invece di spegnere il flat: un campo mancante non e' un permesso.</para>
        ///
        /// <para><b>L'overnight non ha logica qui.</b> Il piano lo esegue stampando la deadline
        /// nell'intent (<c>TimeExitUtc</c>), che questo bot gia' rispetta in
        /// <see cref="CloseExpiredPositions"/>: la gerarchia si risolve una volta sola sul server.
        /// I due campi servono al pannello, e a sapere cosa si sta eseguendo.</para>
        /// </summary>
        private void ApplyHolding(HoldingDto holding)
        {
            if (holding == null)
                return;

            _allowOvernight = holding.AllowOvernight;
            _allowOverweek = holding.AllowOverweek;
            if (IsValidHhmm(holding.SessionFlatUtcHhmm))
                _sessionFlatUtcHhmm = holding.SessionFlatUtcHhmm;

            if (holding.WeekEnd != null &&
                IsValidHhmm(holding.WeekEnd.FromUtcHhmm) &&
                IsValidHhmm(holding.WeekEnd.UntilUtcHhmm))
            {
                _weekEndFlatFromUtc = holding.WeekEnd.FromUtcHhmm;
                _weekEndFlatUntilUtc = holding.WeekEnd.UntilUtcHhmm;
            }
        }

        private static bool IsValidHhmm(int hhmm) =>
            hhmm >= 0 && hhmm <= 2359 && hhmm % 100 < 60;

        private void LogSessionDescriptor()
        {
            Print("Sessione aperta: SessionId={0} profilo={1} Titano={2} concorrenza={3} maxTrade={4}.",
                _sessionId, _runProfile ?? "-", _titanoMode ?? "-",
                _enforceConcurrency ? "attiva" : "OFF",
                _maxConcurrentTrades > 0 ? _maxConcurrentTrades.ToString() : "illimitati");
            Print("  tenuta: {0}", DescribeHolding());
            foreach (var strategy in _strategies)
                Print("  strategia {0} su {1}/{2}m  ({3})", strategy.StrategyCode, strategy.Symbol,
                    strategy.TimeframeMinutes, DescribeStrategyHolding(strategy));
        }

        /// <summary>
        /// Riaggancio a run in corso (solo live). Riapre la sessione e ributta giu' il riscaldamento
        /// di ogni stream.
        ///
        /// <para>Il solo "riprovare a spingere la barra" non basta e questo e' il punto: se il server
        /// e' stato riavviato, SessionId e token che il bot ha in mano non esistono piu' e ogni push
        /// continuerebbe a essere rifiutato per sempre, anche a rete perfettamente tornata. E anche a
        /// sessione ancora valida, il server nelle sessioni ExternalBroker non ha un datafeed proprio:
        /// la sua storia e' solo cio' che gli e' stato spinto, e durante la caduta si e' fermata. Per
        /// questo si azzera <c>WarmedUp</c>: alla prima barra utile ogni stream rispedisce la finestra
        /// profonda e il server riparte con la storia completa invece di restare muto in attesa.</para>
        ///
        /// <para>Throttlato a <see cref="ReopenCooldownSeconds"/>: a server giu' il tentativo fallisce
        /// in fretta, ma non ha senso ritentarlo a ogni singola barra di ogni stream.</para>
        /// </summary>
        private void TryReopenSession()
        {
            // Il riscaldamento chiamato qui sotto passa da SendWindow, che in caso di errore rientra
            // in OnPushFailed: senza questa guardia il riaggancio si richiamerebbe da solo.
            if (_reopenInProgress)
                return;

            var now = Server.TimeInUtc;
            if (_lastReopenAttemptUtc.HasValue &&
                (now - _lastReopenAttemptUtc.Value).TotalSeconds < ReopenCooldownSeconds)
                return;

            _lastReopenAttemptUtc = now;
            _reopenInProgress = true;
            try
            {
                var previousSessionId = _sessionId;
                if (!TryOpenSession(out _, out var error))
                {
                    // Silenzioso di proposito: a server giu' questo tentativo fallisce ogni
                    // ReopenCooldownSeconds per tutta la durata della caduta, e il motivo e' gia'
                    // stampato da OnPushFailed. Col log diagnostico si vede comunque.
                    if (LogDiagnostico)
                        Print("Riaggancio non riuscito: {0}", error);
                    return;
                }

                Print("Riagganciato al server: SessionId={0}{1}. Riscaldamento degli stream in corso.",
                    _sessionId,
                    string.Equals(previousSessionId, _sessionId, StringComparison.Ordinal)
                        ? " (stessa sessione)"
                        : " (sessione nuova, la precedente era " + Or(previousSessionId) + ")");
                LogSessionDescriptor();

                // Storia del server da ricostruire: quella vecchia o non esiste piu' (server riavviato)
                // o si e' fermata al momento della caduta. In entrambi i casi le finestre incrementali
                // non si sovrapporrebbero e verrebbero rifiutate.
                foreach (var pair in _pairs)
                {
                    pair.WarmedUp = false;
                    pair.ServerHistoryBars = null;
                    pair.ServerRequiredCandles = null;
                    SendWarmUpWindow(pair);
                }

                if (_pairs.All(pair => pair.WarmedUp))
                {
                    _consecutivePushFailures = 0;
                    _lastPushError.Clear();
                    UpdateConnectionStatus(true);
                    Print("Riscaldamento completato su tutti gli stream: il bot e' di nuovo operativo.");
                }
                else
                {
                    // Riagganciato ma senza storia completa: gli stream non riscaldati ritentano da
                    // soli in TryPushClosedBar alla barra successiva.
                    Print("Riagganciato, ma il riscaldamento di alcuni stream non e' andato a buon fine: " +
                          "si ritenta alla prossima barra.");
                }
            }
            finally
            {
                _reopenInProgress = false;
            }
        }

        /// <summary>
        /// Errore fatale: lo scrive sul grafico e spegne il bot. In backtest il log della piattaforma
        /// e' un buffer circolare che scarta le righe piu' vecchie — cioe' proprio quelle dell'avvio —
        /// e un run senza server finirebbe senza un solo trade e senza una spiegazione visibile. Il
        /// riquadro sul chart sopravvive alla fine del run ed e' la prima cosa che si vede.
        ///
        /// <para>Il pannello di stato viene forzato a "non connesso" prima di disegnare l'errore, cosi'
        /// i due riquadri non si contraddicono.</para>
        /// </summary>
        private void StopWithError(string message)
        {
            Print("ERRORE FATALE: {0}", message);

            UpdateConnectionStatus(false);

            var text = new StringBuilder();
            text.AppendLine("PIOOTOO — BOT FERMATO");
            text.AppendLine(IsBacktesting ? "(backtest interrotto)" : "(esecuzione interrotta)");
            text.AppendLine();
            text.AppendLine("Server:  " + Or(ServerBaseUrl));
            text.AppendLine("Piano:   " + (string.IsNullOrWhiteSpace(PlanCode) ? "-" : PlanCode.Trim()));
            text.AppendLine();
            text.Append(message);

            Chart.DrawStaticText(FatalErrorChartObjectName, text.ToString(),
                VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);

            Stop();
        }

        /// <summary>
        /// Messaggio di una chiamata HTTP fallita a livello di trasporto: server spento, url sbagliato,
        /// timeout. <see cref="HttpRequestException"/> annida la causa vera (socket, DNS) nella inner,
        /// e senza srotolarla si legge solo un generico "An error occurred while sending the request".
        /// </summary>
        private static string DescribeTransportFailure(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner.Message == ex.Message ? ex.Message : ex.Message + " (" + inner.Message + ")";
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
            RedrawStatusPanel();
        }

        /// <summary>
        /// Ridisegna il pannello con lo stato corrente. Separato da
        /// <see cref="UpdateConnectionStatus"/> perché la copertura della storia cambia a ogni barra
        /// senza che la connessione cambi, e il pannello deve seguirla.
        ///
        /// <para>Il testo viene confrontato con l'ultimo disegnato e si ridisegna solo se è cambiato:
        /// a regime il riquadro è identico barra dopo barra, e in backtest sarebbero decine di
        /// migliaia di <c>DrawStaticText</c> inutili.</para>
        /// </summary>
        private void RedrawStatusPanel()
        {
            var text = BuildStatusText(_isConnectedToServer);
            if (text == _lastStatusText)
                return;

            _lastStatusText = text;
            Chart.DrawStaticText(StatusChartObjectName, text,
                VerticalAlignment.Top, HorizontalAlignment.Right,
                _isConnectedToServer ? Color.LightGreen : Color.OrangeRed);
        }

        /// <summary>
        /// Il testo del pannello. Riporta la configurazione con cui il bot sta effettivamente
        /// lavorando — profilo del run, filtro Titano, lucchetti di concorrenza, limite di trade — il
        /// numero di strategie caricate e la copertura della storia degli stream.
        ///
        /// <para>Sono tutti valori presi dal DESCRIPTOR, cioe' da come il server ha risolto il run, non
        /// dai parametri del cBot. Un bot che dichiara un piano e ne esegue un altro, o un parametro
        /// che il piano contraddice, sono invisibili finche' non si leggono i trade: qui si vedono
        /// sul grafico prima ancora che arrivi il primo segnale.</para>
        ///
        /// <para>Prima della connessione i campi risolti sono vuoti e si stampa "-": il pannello deve
        /// esistere fin dal primo istante, anche per dire che non si e' ancora collegato.</para>
        /// </summary>
        private string BuildStatusText(bool connected)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Piootoo " + BotVersion);
            builder.AppendLine("Account:   " + (string.IsNullOrEmpty(_accountNumber) ? "-" : _accountNumber));
            builder.AppendLine("Piano:     " + (string.IsNullOrWhiteSpace(PlanCode) ? "-" : PlanCode.Trim()));
            // Sotto il nome del piano perche' e' una proprieta' del piano, ed e' la riga che spiega
            // le uscite che altrimenti sembrano della strategia: chi guarda il grafico deve poter
            // dire "questa e' stata chiusa dal conto" senza aprire un file.
            builder.AppendLine("Tenuta:    " + DescribeHolding());
            builder.AppendLine("Connesso:  " + (connected ? "Si" : "No"));
            builder.AppendLine("Run:       " + Or(_serverRunMode) + " / " + Or(_runProfile));
            builder.AppendLine("Titano:    " + DescribeTitano());
            builder.AppendLine("Concorr.:  " + DescribeConcurrency());

            builder.AppendLine("Strategie: " + (_strategies.Count == 0 ? "-" : _strategies.Count.ToString()));
            builder.Append("Storia:    " + DescribeHistoryCoverageSummary());
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// La copertura della storia di TUTTI gli stream in una riga sola. Sostituisce l'elenco
        /// strategia per strategia, che su un piano vero era lungo decine di righe e copriva il
        /// grafico: le strategie sullo stesso stream ripetevano comunque lo stesso numero, perche' la
        /// finestra e' una proprieta' dello stream, non della strategia.
        ///
        /// <para>Quello che non si puo' perdere e' l'avviso: finche' uno stream e' corto il server
        /// salta in silenzio le sue strategie e il run sembra semplicemente "senza segnali". Quindi
        /// gli stream pronti si contano e basta, e si nominano solo quelli che non lo sono.</para>
        /// </summary>
        private string DescribeHistoryCoverageSummary()
        {
            if (_pairs.Count == 0)
                return "-";

            var pending = new List<string>();
            foreach (var pair in _pairs)
            {
                if (IsHistoryReady(pair))
                    continue;
                pending.Add(pair.PiootooSymbol + "/" + pair.TimeframeMinutes + "m " +
                            DescribeHistoryCoverage(pair));
            }

            var ready = _pairs.Count - pending.Count;
            var head = ready + "/" + _pairs.Count + " stream pronti";
            return pending.Count == 0 ? head + " ok" : head + " - " + string.Join("; ", pending);
        }

        /// <summary>
        /// Uno stream e' "pronto" solo quando lo dice il SERVER: e' lui a decidere se valutare, e la
        /// serie del broker puo' essere lunga con la finestra arrivata comunque corta. Finche' non ha
        /// risposto lo stream resta fra quelli da nominare, anche se il conteggio locale basterebbe.
        /// </summary>
        private static bool IsHistoryReady(Pair pair)
        {
            var required = pair.ServerRequiredCandles ?? pair.RequiredCandles;
            if (required <= 0)
                return true;
            return pair.ServerHistoryBars.HasValue && pair.ServerHistoryBars.Value >= required;
        }

        /// <summary>
        /// "caricate/richieste" per lo stream di una strategia: dice se la finestra è già completa,
        /// cioè se quella strategia sta davvero venendo valutata oppure se il server la sta saltando
        /// in silenzio in attesa di storia. È la domanda che altrimenti si risponde solo scavando nel
        /// log, e nel frattempo il run sembra semplicemente "senza segnali".
        ///
        /// <para>Il numero preferito è quello del SERVER (<c>HistoryBars</c> dell'ultima risposta):
        /// è lui a decidere se valutare. Finché non ha risposto si mostra il conteggio locale della
        /// serie del broker, prefissato da <c>~</c> per non farlo scambiare per una conferma.</para>
        /// </summary>
        private static string DescribeHistoryCoverage(Pair pair)
        {
            if (pair is null)
                return "(stream non configurato)";

            // Il richiesto del server vince: se diverge da quello letto nel descriptor, è il suo che
            // sta decidendo i salti.
            var required = pair.ServerRequiredCandles ?? pair.RequiredCandles;
            if (required <= 0)
                return string.Empty;

            if (pair.ServerHistoryBars is null)
            {
                var local = pair.Series?.Count ?? 0;
                return "~" + local + "/" + required + (local >= required ? " (non confermate)" : " IN ATTESA");
            }

            var loaded = pair.ServerHistoryBars.Value;
            return loaded >= required
                ? loaded + "/" + required + " ok"
                : loaded + "/" + required + " MANCANO " + (required - loaded);
        }

        private static string Or(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        /// <summary>
        /// Cosa il piano concede, in una riga. Si stampa sempre, anche quando non taglia nulla:
        /// "overnight e overweek liberi" e' un'informazione, non un'assenza di informazione, e su un
        /// conto prop e' la riga che dovrebbe far fermare chi guarda.
        /// </summary>
        private string DescribeHolding()
        {
            if (!_allowOvernight)
                return "flat di sessione " + Hhmm(_sessionFlatUtcHhmm) + " UTC (niente overnight)";
            if (!_allowOverweek)
                return "overnight SI, flat weekend ven " + Hhmm(_weekEndFlatFromUtc) +
                       " -> dom " + Hhmm(_weekEndFlatUntilUtc) + " UTC";
            return "overnight e overweek liberi";
        }

        /// <summary>
        /// Cosa la strategia dichiarava, e se il piano gliela sta togliendo. Il "TRONCATA" e' il
        /// punto: una multiday su un piano che vieta l'overnight produce trade che non somigliano
        /// a quelli della ricerca, e senza questa parola la differenza si scopre solo confrontando
        /// le liste.
        /// </summary>
        private string DescribeStrategyHolding(SessionStrategyDto strategy)
        {
            var holding = strategy == null ? null : strategy.Holding;
            if (holding == null)
                return "";
            if (!holding.Overnight)
                return "[intraday]";
            if (!_allowOvernight)
                return "[multiday TRONCATA]";
            if (holding.Overweek && !_allowOverweek)
                return "[multiday, flat weekend]";
            return "[multiday]";
        }

        private static string Hhmm(int value) =>
            (value / 100).ToString("00") + ":" + (value % 100).ToString("00");

        /// <summary>
        /// "Disabled" e' il nome del contratto ma dice poco a chi guarda il grafico: qui interessa
        /// sapere se il filtro sta togliendo strategie oppure no.
        /// </summary>
        private string DescribeTitano()
        {
            if (string.IsNullOrWhiteSpace(_titanoMode))
                return "-";
            if (string.Equals(_titanoMode, "Disabled", StringComparison.OrdinalIgnoreCase))
                return "OFF (nessun filtro)";
            return "ON (" + _titanoMode + ")";
        }

        /// <summary>
        /// Un solo campo per i lucchetti e il limite: sono la stessa scelta, e mostrarli separati
        /// invita a leggere MaxConcurrentTrades come se valesse anche a lucchetti spenti.
        /// </summary>
        private string DescribeConcurrency()
        {
            if (!_enforceConcurrency)
                return "OFF (tutti i segnali)";
            if (_maxConcurrentTrades <= 0)
                return "ON, trade illimitati";
            // Cosa venga contato cambia cosa si vede a mercato, quindi si stampa: con lo stesso
            // numero, "posizioni" lascia vivere tutti gli stop pendenti e "pos+ordini" no.
            return "ON, max " + _maxConcurrentTrades +
                   (_cancelPendingAtCap ? " posizioni" : " fra posizioni e ordini");
        }

        /// <summary>
        /// Una barra di UNO stream del piano si è chiusa (l'evento è <c>BarOpened</c> della sua serie:
        /// quando si apre la barra n, la n-1 è chiusa). Tutto il ciclo — cancellazione degli ordini
        /// scaduti, push della barra, conteggio barre in posizione, reclamo del segnale — è fatto per
        /// questo stream soltanto: gli altri hanno il loro orologio e scattano per conto proprio.
        /// </summary>
        private void OnStreamBarClosed(Pair stream)
        {
            // L'ordine della barra appena chiusa va ritirato PRIMA di tutto il resto: "next bar" vuol
            // dire una barra sola, e senza il ritiro se ne accumulerebbe uno per barra a livelli
            // diversi, tutti eseguibili. Chiusura, poi push, poi richiesta dei segnali nuovi: e' anche
            // l'unico ordine possibile, non solo il piu' prudente.
            //
            // <para><b>Perche' e' l'unico.</b> La 2.3.0 aveva provato a spostarlo DOPO il poll, per
            // riconoscere il caso frequentissimo in cui il segnale della barra nuova riemette lo stesso
            // identico livello e tenere l'ordine invece di cancellarlo e ripiazzarlo. Non puo'
            // funzionare: il server non rilascia il template finche' l'intent vecchio e' Pending (il
            // lucchetto "l'account ha gia' un ingresso in corso per quella strategia su quel simbolo"),
            // e l'intent vecchio resta Pending finche' il cBot non ne riporta la cancellazione. Il cBot
            // aspetta il segnale per rinnovare l'ordine, il server aspetta il report per consegnare il
            // segnale: nessuno dei due si muove per primo. Nei log 2.3.0 la riga "non riemesso: ordine
            // gia' a mercato" non compare nemmeno una volta in un mese di backtest, e la 2.3.1 che
            // provava a sbloccarlo con un'attesa ha solo ritardato di 8 secondi ogni piazzamento
            // lasciando il cancella/ripiazza dov'era. Vedi docs/decisioni.md 2026-08-15.</para>
            //
            // <para>La coppia cancella/ripiazza per barra sullo stesso prezzo resta, ed e' il costo
            // accettato: due ordini al broker per barra finche' il livello del canale non si muove.
            // Toglierla davvero richiede di separare "riporto l'intent cancellato" da "cancello
            // l'ordine dal broker" — il report libera il server, l'ordine fisico resta a mercato e
            // viene esteso o modificato quando il segnale arriva — che e' un cambio della semantica
            // del reporting, non di questo metodo.</para>
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
            // aspettare il prossimo polling periodico. Se pero' il push ha appena detto che la
            // sessione non ha NIENTE di reclamabile, il poll e' una chiamata HTTP il cui esito e'
            // gia' noto: dai log reali la grande maggioranza delle barre non produce segnali, quindi
            // e' quasi meta' del traffico di un backtest.
            if (pushed && ShouldPollAfterPush())
                PollNextSignal();

            TrackExcursions();
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
            // Break-even e trailing esistono solo per posizioni aperte. Senza questa uscita anticipata
            // ogni tick — in un backtest tick-based sono ordini di grandezza piu' delle barre —
            // pagherebbe un ToArray sul dizionario e una scansione di Positions per non fare niente.
            // La semantica non cambia: le soglie restano valutate a ogni tick sulle posizioni che
            // esistono, che e' il motivo per cui questo lavoro sta qui e non sul bar-close.
            if (_openPositions.Count == 0)
                return;

            // Solo i tick del simbolo su cui abbiamo qualcosa da proteggere: su un piano
            // multi-simbolo il tick di EURUSD non puo' muovere lo stop di una posizione su NQ.
            if (!HasOpenPositionOn(args.SymbolName))
                return;

            // Dentro la finestra di fine settimana non c'e' nulla da proteggere: va solo chiuso.
            if (EnforceWeekEndFlat())
                return;

            TrackExcursions();
            MoveStopsToBreakEven();
            MoveTrailingStops();
        }

        /// <summary>
        /// Il bot ha una posizione aperta su questo simbolo del broker? Il confronto e' sul nome
        /// dell'ACCOUNT, che e' quello con cui arrivano i tick; <c>OpenPositionContext.Symbol</c> e'
        /// il nome Piootoo, quindi si passa dalle posizioni della piattaforma.
        /// </summary>
        private bool HasOpenPositionOn(string accountSymbol)
        {
            foreach (var position in Positions)
            {
                if (string.Equals(position.SymbolName, accountSymbol, StringComparison.OrdinalIgnoreCase) &&
                    _openPositions.ContainsKey(position.Id))
                    return true;
            }

            return false;
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
            else if (LogOperativo)
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
                    // La posizione e' sparita senza passare da OnPositionClosed: e' il caso dello
                    // Stop Loss / Take Profit nativo che chiude fra due giri di questo sweep. Prima
                    // la si toglieva e basta, e il trade non arrivava MAI al server: su un run di due
                    // anni sono spariti 66 trade su 176 da trades.json, per giunta i 56 stoppati e i
                    // 10 a target, cioe' quelli che non torna comodo perdere. Va riportata come
                    // qualunque altra chiusura.
                    RegisterMissedClose(kvp.Key, ctx);
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
            // Anche il bar-close chiama questo metodo: la guardia sta qui e non solo nel tick handler
            // cosi' vale per entrambi i chiamanti, e il ToArray non si paga a vuoto.
            if (_openPositions.Count == 0)
                return;

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
                if (!result.IsSuccessful && LogDiagnostico)
                    Print("Impossibile spostare a break-even {0}/{1}: {2}",
                        context.Symbol, context.StrategyCode, result.Error);
            }
        }

        /// <summary>
        /// Aggiorna MFE e MAE delle posizioni aperte: quanto il prezzo e' andato a favore e quanto
        /// contro, in punti, dal prezzo di ingresso.
        ///
        /// <para>Sono le due misure che spiegano un trade a posteriori, e nessuna delle due e'
        /// ricostruibile dal solo esito: un trade chiuso in pari puo' essere uno che non si e' mai
        /// mosso oppure uno che era a due terzi del target e lo ha restituito, e la conseguenza
        /// operativa e' opposta. Si campiona a ogni tick sul lato di USCITA — Bid per i long, Ask per
        /// gli short — che e' il prezzo a cui quel profitto sarebbe stato davvero incassato.</para>
        /// </summary>
        private void TrackExcursions()
        {
            if (_openPositions.Count == 0)
                return;

            foreach (var context in _openPositions.Values)
            {
                if (context.EntryPrice <= 0)
                    continue;

                var position = Positions.FirstOrDefault(item => item.Id == context.PositionId);
                if (position is null)
                    continue;

                var symbol = Symbols.GetSymbol(position.SymbolName);
                if (symbol is null)
                    continue;

                var movimento = position.TradeType == TradeType.Buy
                    ? symbol.Bid - context.EntryPrice
                    : context.EntryPrice - symbol.Ask;

                if (movimento > context.MaxFavorablePoints)
                    context.MaxFavorablePoints = movimento;
                else if (-movimento > context.MaxAdversePoints)
                    context.MaxAdversePoints = -movimento;
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
            if (_openPositions.Count == 0)
                return;

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

                // Il trailing insegue il prezzo a ogni tick, ma NON deve mandare un ordine di modifica
                // a ogni tick. Senza queste due guardie il bot produce decine di ModifyPosition da un
                // decimo di punto, anche piu' d'una nello stesso secondo: in backtest e' rumore che
                // satura il buffer del log, in live e' traffico che il broker limita e poi rifiuta.
                // Il guadagno di protezione di uno spostamento da 0,1 punti su uno stop da 12,5 e'
                // nullo; il rischio di vedersi rifiutare la modifica *utile* perche' si e' sopra il
                // rate limit non lo e'.
                var passoMinimo = TrailingMinStepFraction > 0 ? distance * TrailingMinStepFraction : 0.0;
                if (passoMinimo > 0 && position.StopLoss.HasValue &&
                    Math.Abs(candidate - position.StopLoss.Value) < passoMinimo)
                    continue;

                var nowUtc = Server.TimeInUtc;
                if (TrailingMinIntervalSeconds > 0 && context.LastTrailingUpdateUtc.HasValue &&
                    (nowUtc - context.LastTrailingUpdateUtc.Value).TotalSeconds < TrailingMinIntervalSeconds)
                    continue;

                var result = ModifyPosition(position, candidate, position.TakeProfit);
                if (result.IsSuccessful)
                {
                    context.LastTrailingUpdateUtc = nowUtc;
                    context.TrailingUpdates++;
                }
                else if (LogDiagnostico)
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
                CancelAndReport(order, reason);
        }

        /// <summary>
        /// Cancella un ordine pending del bot e **riporta al server l'annullamento dell'intent** che lo
        /// aveva piazzato.
        ///
        /// <para>Il report non è contabilità: è ciò che sblocca il conto. Finché l'intent resta
        /// <c>Pending</c>, il server lo considera in carico a questo account e il claim continua a
        /// riproporre sempre lo stesso — <c>GetNextSignalForAccount</c> restituisce per primo l'intent
        /// già assegnato e ancora pendente — mentre i lucchetti (account, simbolo) e
        /// (gruppo, strategia, simbolo) restano chiusi. Il bot lo scarta perché l'ha già gestito, e da
        /// lì in poi non arriva più nessun segnale nuovo per tutto il run: un ordine solo, all'inizio,
        /// e poi silenzio. L'IntentId si legge dalla label, che è esattamente il motivo per cui ce
        /// l'ha.</para>
        /// </summary>
        private void CancelAndReport(PendingOrder order, string reason)
        {
            var label = order.Label;
            var symbol = order.SymbolName;
            var result = CancelPendingOrder(order);
            if (!result.IsSuccessful)
            {
                Print("Impossibile cancellare l'ordine pending {0} ({1}): {2}", order.Id, label, result.Error);
                return;
            }

            Print("Ordine pending {0} ({1}) cancellato: {2}.", order.Id, label, reason);

            var parsed = ParseLabel(label);
            if (parsed is null || string.IsNullOrEmpty(parsed.IntentId))
                return; // label di formato precedente: nessun intent a cui riferirsi

            _submittedIntentIds.Remove(parsed.IntentId);
            _lastOpenIntentByLabel.Remove(label);
            ReportExecution(parsed.IntentId, symbol, ExecutionReportStatusDto.Cancelled, 0, null);
        }

        /// <summary>
        /// Cancella gli ordini pending di una strategia <b>su un lato solo</b>, qualunque sia l'intent
        /// che li ha piazzati. Serve perché la label porta l'IntentId: il segnale nuovo non ha la stessa
        /// label del vecchio, e cercare per label esatta lascerebbe a mercato l'ordine della barra
        /// precedente.
        ///
        /// <para><b>Perché il lato è parte della chiave.</b> Le strategie non simmetriche — TF_U in
        /// testa — emettono sulla stessa barra un bracket a due gambe: stop buy su <c>H_d1</c> e stop
        /// sell su <c>L_d1</c>. Sono due ordini distinti con due motivi d'ingresso opposti, non uno la
        /// sostituzione dell'altro. Cancellando per sola strategia la seconda gamba uccideva la prima e
        /// il bracket non poteva mai stare a mercato intero: nel confronto Feb–Mag 2014 su
        /// <c>PTS_GC_TFU_001_30</c>, su 1501 ordini piazzati dal bot le due gambe non risultano
        /// pendenti insieme nemmeno una volta, mentre il backtest le tiene entrambe su cinque sessioni.
        /// Vedi <c>docs/decisioni.md</c>, 2026-08-26.</para>
        ///
        /// <para>Il <c>_pendingOrderBar</c> si pota con lo stesso criterio: potarlo per sola strategia
        /// toglieva il segno di scadenza anche alla gamba opposta, che sarebbe rimasta a mercato oltre
        /// la sua barra.</para>
        /// </summary>
        private void CancelStrategyPendingOrders(string strategyCode, TradeType side, string reason)
        {
            var prefix = MakeStrategyLabelPrefix(strategyCode);
            foreach (var order in PendingOrders
                .Where(o => o.Label != null &&
                            o.Label.StartsWith(prefix, StringComparison.Ordinal) &&
                            o.TradeType == side)
                .ToList())
                CancelAndReport(order, reason);

            foreach (var entry in _pendingOrderBar
                .Where(e => e.Key.StartsWith(prefix, StringComparison.Ordinal) && e.Value.Side == side)
                .ToList())
                _pendingOrderBar.Remove(entry.Key);
        }

        /// <summary>
        /// OCO fra le due gambe di un bracket: la gamba che si riempie ritira l'altra.
        ///
        /// <para>Le strategie non simmetriche (<c>TfUnmirroredEngine</c>) mandano sulla stessa barra uno
        /// stop buy su <c>H_d1</c> e uno stop sell su <c>L_d1</c>: sono due ipotesi alternative su dove
        /// romperà il prezzo, non la richiesta di stare a mercato in entrambi i sensi. cTrader non lega
        /// fra loro ordini piazzati separatamente — non esiste un OCO nativo da chiedere al broker —
        /// quindi il legame lo tiene il bot, qui, nel solo istante in cui l'esito di una gamba è noto.</para>
        ///
        /// <para>Prima del 26/08/2026 il problema non si poneva perché la seconda gamba non arrivava mai
        /// a mercato: <c>CancelStrategyPendingOrders</c> cancellava per sola strategia e la seconda
        /// uccideva la prima. Sistemato quello, il bracket sta a mercato intero e serve chi lo scioglie.</para>
        ///
        /// <para><b>Cosa questo non copre.</b> Se il prezzo attraversa entrambi i livelli <b>prima</b> che
        /// l'evento della prima apertura sia servito — una barra che spazza tutto il bracket — le due
        /// gambe si riempiono comunque, e la strategia resta long e short insieme. Non è correggibile da
        /// qui: quando il codice gira, il secondo fill è già avvenuto. Il caso viene però riconosciuto e
        /// stampato, perché un OCO che ha ceduto in silenzio è indistinguibile da uno che ha tenuto.</para>
        /// </summary>
        private void EnforceBracketOco(Position position)
        {
            var parsed = ParseLabel(position.Label);
            if (parsed is null)
                return;

            var altroLato = position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;

            // Va prima della risoluzione dell'intent in OnPositionOpened: se l'intent locale è perduto
            // (riavvio del bot con un pending già a mercato) il report salta, ma la gamba opposta va
            // ritirata lo stesso — è a mercato e si riempirebbe.
            CancelStrategyPendingOrders(
                parsed.StrategyCode,
                altroLato,
                $"OCO: la gamba {position.TradeType} della stessa strategia si e' riempita");

            var prefix = MakeStrategyLabelPrefix(parsed.StrategyCode);
            var gambaOpposta = Positions.FirstOrDefault(p =>
                p.Id != position.Id &&
                p.Label != null &&
                p.Label.StartsWith(prefix, StringComparison.Ordinal) &&
                p.TradeType == altroLato &&
                p.SymbolName.Equals(position.SymbolName, StringComparison.OrdinalIgnoreCase));
            if (gambaOpposta is null)
                return;

            Print("OCO CEDUTO su {0}/{1}: le posizioni {2} ({3}) e {4} ({5}) sono aperte insieme. " +
                  "La barra ha attraversato entrambi i livelli del bracket prima che il primo fill fosse servito.",
                position.SymbolName, parsed.StrategyCode,
                gambaOpposta.Id, gambaOpposta.TradeType, position.Id, position.TradeType);
            LogJsonEvent("oco/ceduto", new
            {
                StrategyCode = parsed.StrategyCode,
                Symbol = position.SymbolName,
                PrimaPositionId = gambaOpposta.Id,
                PrimoLato = gambaOpposta.TradeType.ToString(),
                PrimoPrezzo = gambaOpposta.EntryPrice,
                SecondaPositionId = position.Id,
                SecondoLato = position.TradeType.ToString(),
                SecondoPrezzo = position.EntryPrice,
                ServerTimeUtc = Server.TimeInUtc
            });
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
            _timerTicks++;

            if (EnforceWeekEndFlat())
                return;

            if (ShouldPollOnTimer())
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
            if (_allowOverweek || !IsWeekEndFlatWindow(Server.TimeInUtc))
                return false;

            foreach (var order in PendingOrders
                .Where(o => o.Label != null && o.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
                CancelAndReport(order, "flat di fine settimana");
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
                    return hhmm >= _weekEndFlatFromUtc;
                case DayOfWeek.Saturday:
                    return true;
                case DayOfWeek.Sunday:
                    return hhmm < _weekEndFlatUntilUtc;
                default:
                    return false;
            }
        }

        protected override void OnStop()
        {
            SaveLocalState();
            Timer.Stop();

            // Quanto e' servita la guardia sul poll: senza questo numero non si sa se il risparmio
            // c'e' stato, e nemmeno se la guardia sta invece tacendo qualcosa che andava reclamato.
            if (_skippedPolls > 0)
                Print("Poll saltati perche' il server non aveva nulla da consegnare: {0}.", _skippedPolls);

            // Il traffico del run in due numeri. Serve a rendere confrontabili due backtest identici:
            // le push devono restare le stesse (una per barra per stream), i poll devono crollare, e
            // trades.json deve essere lo stesso file. Se i poll crollano ma le push cambiano, non e'
            // la guardia che ha funzionato: e' il run che e' diverso.
            Print("Chiamate al server: {0} push di barre, {1} claim. Battiti del timer: {2}.",
                _pushCalls, _pollCalls, _timerTicks);

            PrintSpreadSummary();
            PrintTradeSummary();

            foreach (var pair in _pairs)
                if (pair.Series != null && pair.BarHandler != null)
                    pair.Series.BarOpened -= pair.BarHandler;

            foreach (var entry in _tickHandlers)
                entry.Key.Tick -= entry.Value;
            _tickHandlers.Clear();

            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;
            CloseBacktestSessionOnServer();
            _http?.Dispose();
        }

        /// <summary>
        /// Chiude la sessione lato server alla fine di un BACKTEST. Il server, allo stop, forza la
        /// scrittura completa e durabile degli artefatti e fonde il journal incrementale: senza
        /// questa chiamata la cartella del run resta con un <c>.jsonl</c> aperto e una sessione in
        /// stato Running che nessuno chiudera' mai.
        ///
        /// <para>In LIVE non si chiude nulla, di proposito. La execution key e' costante ("LIVE"), e
        /// una sessione lasciata aperta e' cio' che permette a un cBot riavviato — o a cTrader
        /// riaperto — di riprendere lo stesso run invece di aprirne uno nuovo accanto al primo.</para>
        ///
        /// <para>Best effort: qui si sta gia' spegnendo, e un server irraggiungibile in questo punto
        /// non deve produrre un errore di piattaforma sull'ultima riga del run.</para>
        /// </summary>
        private void CloseBacktestSessionOnServer()
        {
            if (!IsBacktesting || _http is null || string.IsNullOrWhiteSpace(_sessionId))
                return;

            try
            {
                var response = PostJson($"api/v1/trading-sessions/{_sessionId}/stop", new { });
                if (response.IsSuccessStatusCode)
                    Print("Sessione {0} chiusa sul server: artefatti del run scritti in modo definitivo.",
                        _sessionId);
                else
                    Print("Chiusura della sessione {0} rifiutata dal server: {1}. Gli artefatti del run " +
                          "restano come li ha lasciati l'ultimo checkpoint.", _sessionId, ReadError(response));
            }
            catch (Exception ex)
            {
                Print("Impossibile chiudere la sessione {0} sul server: {1}. Gli artefatti del run " +
                      "restano come li ha lasciati l'ultimo checkpoint.", _sessionId, DescribeTransportFailure(ex));
            }
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
                if (LogOperativo)
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

                _pushCalls++;
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
        /// stream — altrimenti un piano mal configurato produce la stessa riga a ogni barra — e
        /// segnala sul pannello che la connessione al server non c'e' piu'.
        ///
        /// <para>In BACKTEST si ferma subito: ogni barra spedita a vuoto e' una barra non valutata, e
        /// il run arriverebbe in fondo con un'equity plausibile costruita su una frazione dei segnali.</para>
        ///
        /// <para>In LIVE non si ferma mai. Il bot resta acceso e continua a provare a ogni barra: le
        /// posizioni a mercato hanno condizioni di uscita che girano in locale (stop, break-even,
        /// trailing, flat di fine settimana) e continuano a essere gestite anche a server muto. Al
        /// superamento di <see cref="MaxConsecutivePushFailures"/> si alza il tono del log una volta
        /// sola, poi si tace fino al rientro.</para>
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

            // In backtest non ha senso tollerare i fallimenti: il server e' locale e o risponde o non
            // c'e'. Ogni barra spedita a vuoto e' una barra non valutata, e il run arriverebbe in fondo
            // con un equity plausibile ma costruito su una frazione dei segnali. Meglio fermarsi alla
            // prima. In live invece la tolleranza resta: un buco di rete di qualche secondo non deve
            // spegnere un bot che ha posizioni aperte.
            if (IsBacktesting)
            {
                StopWithError("Connessione al server persa durante il backtest.\n" +
                              "Stream " + pair + ": " + message + "\n" +
                              "Il run e' stato interrotto: i risultati parziali non sono validi.");
                return;
            }

            // Live: si continua a girare, sempre. Un cBot spento non gestisce piu' le uscite delle
            // posizioni aperte, ed e' un danno peggiore di un server irraggiungibile. Il contatore
            // serve solo ad alzare il tono del log una volta sola, quando la caduta smette di
            // sembrare un buco di rete e inizia a sembrare un guasto.
            _consecutivePushFailures++;
            if (_consecutivePushFailures == MaxConsecutivePushFailures)
                Print("ATTENZIONE: {0} invii falliti di fila, il server non risponde. Il bot RESTA " +
                      "ACCESO e continua a riprovare; le uscite delle posizioni aperte sono gestite " +
                      "in locale. Nessun nuovo segnale finche' il server non torna. Ultimo errore: {1}",
                    _consecutivePushFailures, message);

            // Riaggancio, non semplice ritentativo: la sessione lato server puo' non esistere piu'.
            // Throttlato al suo interno, quindi si puo' chiamare a ogni fallimento.
            TryReopenSession();
        }

        private void OnPushSucceeded(Pair pair)
        {
            // Il rientro dopo una caduta lunga va detto per quello che e': fra l'ultimo invio riuscito
            // e questo ci sono barre che il server non ha mai visto, quindi segnali che non sono mai
            // stati generati. Chi legge il log deve poterlo sapere senza contare le righe di errore.
            if (_consecutivePushFailures >= MaxConsecutivePushFailures)
                Print("Connessione al server ristabilita dopo {0} invii falliti. Le barre di quella " +
                      "finestra non sono state valutate: eventuali segnali di quel periodo sono persi.",
                    _consecutivePushFailures);

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

            // Guardia sul poll: quanto il server dice di avere da consegnare. Null (campo assente,
            // risposta illeggibile, server vecchio) vale "non so" e fa pollare come prima.
            _lastPushClaimable = payload?.ClaimableIntents;

            var status = payload?.Streams?.FirstOrDefault();
            if (status is null)
                return;

            // Copertura della storia sul chart: si aggiorna a ogni risposta, non solo quando il server
            // dichiara di aver saltato qualcosa, altrimenti la riga resterebbe ferma sull'ultimo
            // valore "cattivo" anche dopo che la finestra si è completata.
            pair.ServerHistoryBars = status.HistoryBars;
            pair.ServerRequiredCandles = status.RequiredCandles;
            RedrawStatusPanel();

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

        /// <summary>
        /// Vale la pena reclamare dopo questo push?
        ///
        /// <para>Si salta solo su una dichiarazione ESPLICITA del server che non ha nulla da
        /// consegnare. Il conteggio arriva da lui e non dal client perche' solo lui sa dei template
        /// di barre precedenti ancora vivi e degli intent gia' assegnati da un giro anteriore:
        /// dedurlo dagli intent di <i>questa</i> barra salterebbe un poll che aveva qualcosa.</para>
        ///
        /// <para>Il verso dell'errore e' scelto: <c>null</c> — campo assente, risposta illeggibile,
        /// server piu' vecchio del client — vale "non so" e polla. Un poll a vuoto costa una
        /// chiamata; un poll saltato a torto costa un segnale, e non lascia traccia.</para>
        /// </summary>
        private bool ShouldPollAfterPush()
        {
            if (_lastPushClaimable is not 0)
                return true;

            _skippedPolls++;
            return false;
        }

        /// <summary>
        /// Vale la pena reclamare al battito del timer?
        ///
        /// <para><b>In live si', sempre.</b> Il timer e' l'unico canale che scopre i template nati
        /// dalla push del bot di un ALTRO account, ed e' anche la prima chiamata che si accorge che il
        /// server e' tornato su dopo un'interruzione (<c>TryReopenSession</c> vive nel percorso del
        /// poll). Su uno stream a 60 minuti toglierlo vorrebbe dire riagganciarsi un'ora dopo. Costa
        /// una chiamata ogni <c>PollingSeconds</c> di tempo reale: niente.</para>
        ///
        /// <para><b>In backtest quasi mai</b>, e non per risparmiare: perche' l'esito e' gia' noto. Il
        /// timer li' batte sull'orologio SIMULATO, quindi su un run di un anno scatta milioni di volte
        /// contro le decine di migliaia di barre — ed e' quasi tutto traffico che non puo' scoprire
        /// niente. Tre proprieta' del server lo rendono deducibile senza chiederglielo:</para>
        /// <list type="number">
        ///   <item>i template nascono solo dalla valutazione di una barra, cioe' da una push;</item>
        ///   <item>l'orologio del server e' l'ultima barra valutata, non l'ora di sistema, e
        ///   <c>PurgeExpiredTemplates</c> gira sulla stessa valutazione: fra due push nessun template
        ///   nasce e nessuno scade;</item>
        ///   <item>in backtest questo bot e' l'unico attore della sessione: nessun altro account
        ///   reclama, riporta o spinge barre.</item>
        /// </list>
        /// <para>Resta un solo modo per cui un claim rifiutato diventerebbe accettabile senza barre
        /// nuove: lo stato del broker che il claim stesso trasporta. Un tetto di concorrenza pieno che
        /// si libera, un ingresso della stessa strategia che smette di essere "in volo". Sono eventi
        /// locali, e <see cref="_claimRetryPending"/> li registra: solo allora si polla. Il numero di
        /// poll passa cosi' da "quanti secondi simulati dura il run" a "quante barre ha il run".</para>
        ///
        /// <para>Il verso dell'errore resta quello di <see cref="ShouldPollAfterPush"/>: se il server
        /// non ha dichiarato il conteggio (<c>null</c>) si polla, perche' un poll a vuoto costa una
        /// chiamata e un poll saltato a torto costa un segnale.</para>
        /// </summary>
        private bool ShouldPollOnTimer()
        {
            if (!IsBacktesting || PollOnTimerInBacktest)
                return true;

            if (_lastPushClaimable is null)
                return true;

            if (_lastPushClaimable is 0 || !_claimRetryPending)
            {
                _skippedPolls++;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reclama i segnali disponibili per questo account.
        ///
        /// <para>Il claim consegna UN intent per chiamata: e' un protocollo pull, e ogni giro passa
        /// dal lock della sessione lato server. Con i lucchetti attivi un intent per poll e' anche il
        /// massimo che l'account puo' detenere (un solo intent pendente per volta), quindi una sola
        /// chiamata basta e ripeterla otterrebbe soltanto lo stesso intent.</para>
        ///
        /// <para>Con i lucchetti spenti — il backtest sorgente — il vincolo non c'e' piu' e la barra
        /// puo' aver prodotto un segnale per ogni strategia. Fermarsi al primo significherebbe
        /// eseguire una strategia per barra e produrre un campione sorgente monco: qui si drena la
        /// coda finche' il server non risponde "nessun segnale". Il tetto e' una rete di sicurezza —
        /// se il server continuasse a restituire intent per un difetto suo, il bot smette invece di
        /// bloccare il thread della piattaforma.</para>
        /// </summary>
        private void PollNextSignal()
        {
            // Lo stato del broker che ha motivato il tentativo sta per essere offerto al server: da qui
            // in poi il retry va rimeritato da un evento nuovo. Azzerato PRIMA e non dopo, perche' le
            // chiusure che scattano durante il drenaggio arrivano dai callback della piattaforma sullo
            // stesso thread, e devono poter rialzare la bandiera per il giro successivo.
            _claimRetryPending = false;

            if (_enforceConcurrency)
            {
                PollNextSignalOnce();
                return;
            }

            for (var claimed = 0; claimed < MaxSignalsPerDrain; claimed++)
            {
                if (!PollNextSignalOnce())
                    return;
            }

            Print("Drenaggio segnali interrotto al tetto di {0} intent per barra: " +
                  "il server continua a consegnare segnali. Verifica la sessione.", MaxSignalsPerDrain);
        }

        /// <summary>
        /// Un giro di claim. Restituisce true se ha preso in carico un intent, cioe' se ha senso
        /// richiedere il successivo; false quando il server non ha piu' niente per questo account,
        /// quando la chiamata fallisce, o quando l'intent restituito era gia' in gestione.
        /// </summary>
        private bool PollNextSignalOnce()
        {
            _pollCalls++;
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
                    if (LogDiagnostico) Print("Poll segnale fallito: {0}", ReadError(response));
                    UpdateConnectionStatus(false);
                    // Il poll gira a timer, le barre no: su un timeframe lungo sarebbe questa la prima
                    // chiamata ad accorgersi che il server e' tornato. Throttlato al suo interno.
                    if (!IsBacktesting) TryReopenSession();
                    return false;
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
                    return false;
                }

                if (_lastPollReason != null)
                {
                    _lastPollReason = null;
                    Print("Intent ricevuti di nuovo dal server.");
                }

                var intent = payload.Intent;
                if (_submittedIntentIds.Contains(intent.IntentId))
                    // Già in gestione: aspettiamo l'esito dell'ordine inviato. Si ferma anche il
                    // drenaggio, altrimenti con i lucchetti attivi il passo 1 riproporrebbe questo
                    // stesso intent a ogni giro e il ciclo girerebbe a vuoto fino al tetto.
                    return false;

                if (intent.IsClose || string.Equals(intent.Kind, "Close", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCloseIntent(intent);
                    return true;
                }

                HandleEntryIntent(intent);
                return true;
            }
            catch (Exception ex)
            {
                Print("Errore polling segnale: {0}", ex.Message);
                UpdateConnectionStatus(false);
                if (!IsBacktesting) TryReopenSession();
                return false;
            }
        }

        private void HandleEntryIntent(OrderIntentDto intent)
        {
            // Ultima barriera: un intent reclamato appena prima del taglio non deve aprire nulla.
            // Gli intent di chiusura non passano di qui, quindi la riduzione di rischio resta libera.
            if (!_allowOverweek && IsWeekEndFlatWindow(Server.TimeInUtc))
            {
                Print("Ingresso {0}/{1} scartato: finestra di flat di fine settimana.",
                    intent.Symbol, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            // Verso il broker si usa il nome dello strumento sull'account, verso il server quello
            // Piootoo: la tabella di conversione del conto traduce l'uno nell'altro.
            var brokerSymbolName = ResolveIntentSymbol(intent);

            // Autolimitazione lato client per (STRATEGIA, SIMBOLO), non per simbolo soltanto: due
            // strategie diverse sullo stesso strumento sono due motivi di ingresso indipendenti e
            // devono poter stare a mercato insieme. Quante ne stiano lo governa MaxConcurrentTrades
            // sul server, che conta sull'insieme delle strategie e non per simbolo. Un secondo
            // ordine della STESSA strategia sullo stesso simbolo resta invece bloccato: non e'
            // concorrenza, e' rischio doppio sullo stesso motivo di ingresso.
            //
            // Fino al 11/08/2026 il controllo era per simbolo, e su una sessione a simbolo singolo
            // rendeva inapplicabile qualunque valore di MaxConcurrentTrades: la seconda strategia
            // non arrivava mai a mercato.
            var alreadyOpenOnStrategy = Positions.Any(p =>
                p.SymbolName.Equals(brokerSymbolName, StringComparison.OrdinalIgnoreCase) &&
                p.Label != null &&
                p.Label.StartsWith(MakeStrategyLabelPrefix(intent.StrategyCode), StringComparison.Ordinal));
            if (alreadyOpenOnStrategy)
            {
                // Annullato, non ignorato: un intent lasciato Pending sul server resta assegnato a
                // questo account, viene riproposto a ogni poll e tiene chiusi i lucchetti finché il
                // run non finisce. E comunque non andrebbe eseguito più tardi: il segnale di un
                // motore Unger vale la sua barra, non quella in cui la strategia tornerà libera.
                Print("Ingresso {0}/{1} annullato: posizione già aperta per questa strategia.",
                    intent.Symbol, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Cancelled, 0, null);
                return;
            }

            // Tetto locale sulle posizioni riempite. Il server ha gia' applicato il proprio budget
            // al claim, ma in modalita' PositionsOnly quel budget non contava gli ordini pendenti:
            // fra il claim e adesso un altro stop puo' essersi riempito, e questo ingresso
            // sfonderebbe il tetto. E' l'ultima barriera, e sta qui perche' qui si conosce lo stato
            // del broker nell'istante esatto in cui si sta per mandare l'ordine.
            if (_enforceConcurrency && _maxConcurrentTrades > 0 &&
                CountPiootooPositions() >= _maxConcurrentTrades)
            {
                Print("Ingresso {0}/{1} annullato: raggiunto il massimo di {2} posizioni contemporanee.",
                    intent.Symbol, intent.StrategyCode, _maxConcurrentTrades);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Cancelled, 0, null);
                return;
            }

            var symbol = Symbols.GetSymbol(brokerSymbolName);
            if (symbol is null)
            {
                Print("Simbolo '{0}' non disponibile/non abilitato su questo account: ingresso {1} scartato.", brokerSymbolName, intent.StrategyCode);
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            // Fotografia del mercato nell'istante in cui l'intent arriva: senza questa riga, di un
            // ingresso resta solo il fill (o il nulla di uno scarto) e non si puo' piu' distinguere
            // un bot che sbaglia da un mercato che si e' mosso. Va prima di ogni filtro, cosi' la
            // riga c'e' anche per gli intent che vengono scartati subito dopo.
            LogIntentMarket(intent, symbol);

            // Filtri di sanita' sull'intent: livello dal lato sbagliato, livello troppo lontano,
            // spread troppo pesante sullo stop. Stanno DOPO la riga diagnostica di proposito, cosi'
            // di un ingresso scartato resta comunque la fotografia del mercato che lo ha fatto
            // scartare, e prima di qualunque effetto sul broker.
            if (RejectUnsoundIntent(intent, symbol))
                return;

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
                    // I prezzi nel messaggio, non solo la distanza: "scartato per 3,2 pips" non dice
                    // se il server ha prezzato su una barra vecchia, se il mercato e' scappato o se
                    // lo spread era anomalo. Bid/Ask e prezzo dell'intent lo dicono.
                    Print("Ingresso {0}/{1} scartato per slippage ({2:0.0} pips): intent {3} contro {4} {5:0.#####} " +
                          "(Bid {6:0.#####} / Ask {7:0.#####}).",
                        intent.Symbol, intent.StrategyCode, distancePips, intent.Price,
                        intent.Side == SignalTypeDto.Buy ? "Ask" : "Bid", currentPrice,
                        symbol.Bid, symbol.Ask);
                    LogJsonEvent("intent/scartato-slippage", new
                    {
                        intent.IntentId,
                        intent.StrategyCode,
                        intent.Symbol,
                        IntentPrice = intent.Price,
                        symbol.Bid,
                        symbol.Ask,
                        DistanzaPips = distancePips,
                        MaxEntrySlippagePips
                    });
                    ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                    return;
                }
            }

            var tradeType = intent.Side == SignalTypeDto.Buy ? TradeType.Buy : TradeType.Sell;
            // FinalQuantity arriva dal server nei contratti/lotti del broker; le API di cTrader
            // ragionano in UNITA' dello strumento (per XAUUSD un lotto vale symbol.LotSize unita').
            // La conversione va fatta PRIMA della normalizzazione: passando i lotti a
            // NormalizeVolumeInUnits si confrontano grandezze diverse, e poiche' il metodo alza al
            // minimo invece di azzerare, 0,1 lotti diventavano 1 unita' = 0,01 lotti — un decimo
            // della size prevista, senza alcun errore visibile.
            var rawVolume = symbol.QuantityToVolumeInUnits((double)intent.FinalQuantity);
            var volume = symbol.NormalizeVolumeInUnits(rawVolume, RoundingMode.Down);

            // [SIZE] Diagnostica del dimensionamento. Stampata sempre e con 6 decimali: il formato
            // corto non distingue uno zero vero da un valore frazionario troncato in stampa, ed e'
            // esattamente l'ambiguita' che rende illeggibili i run senza operazioni.
            //
            // LottiEffettivi e' il controllo che conta: e' la size che finisce davvero a mercato,
            // riportata nell'unita' in cui il segnale l'ha dichiarata. Se non coincide con
            // FinalQuantity, la conversione lotti/unita' e' di nuovo fuori posto.
            var lottiEffettivi = symbol.VolumeInUnitsToQuantity(volume);
            Print($"[SIZE] {intent.StrategyCode} {brokerSymbolName}: " +
                  $"FinalQuantity={intent.FinalQuantity:F6} rawVolume={rawVolume:F6} " +
                  $"volumeNormalizzato={volume:F6} LottiEffettivi={lottiEffettivi:F6} | " +
                  $"LotSize={symbol.LotSize:F6} VolumeMin={symbol.VolumeInUnitsMin:F6} " +
                  $"VolumeStep={symbol.VolumeInUnitsStep:F6} VolumeMax={symbol.VolumeInUnitsMax:F6}");

            // Scostamento oltre l'1% fra size richiesta ed eseguibile: non e' un dettaglio di
            // arrotondamento ma una size diversa da quella su cui il segnale e' stato dimensionato.
            if (intent.FinalQuantity > 0 &&
                Math.Abs(lottiEffettivi - (double)intent.FinalQuantity) > 0.01 * (double)intent.FinalQuantity)
                Print($"[SIZE] {intent.StrategyCode} {brokerSymbolName}: ATTENZIONE size eseguita " +
                      $"{lottiEffettivi:F6} diversa dalla richiesta {intent.FinalQuantity:F6} " +
                      $"(minimo broker {symbol.VolumeInUnitsToQuantity(symbol.VolumeInUnitsMin):F6}).");

            if (volume <= 0)
            {
                // Ramo di scarto nominato: senza questo, un ordine che non parte per volume nullo
                // e' indistinguibile da un ordine rifiutato dal broker.
                Print($"[SIZE] {intent.StrategyCode} {brokerSymbolName}: SCARTATO da volume-nullo-dopo-normalizzazione " +
                      $"(rawVolume={rawVolume:F6} < VolumeMin={symbol.VolumeInUnitsMin:F6}).");
                LogJsonEvent("intent/scartato-volume", new
                {
                    intent.IntentId,
                    intent.StrategyCode,
                    intent.Symbol,
                    intent.FinalQuantity,
                    RawVolume = rawVolume,
                    VolumeNormalizzato = volume,
                    symbol.LotSize,
                    symbol.VolumeInUnitsMin,
                    symbol.VolumeInUnitsStep,
                    LottiEffettivi = lottiEffettivi
                });
                ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
                return;
            }

            _submittedIntentIds.Add(intent.IntentId);

            // La label porta l'IntentId: posizione e ordine restano riconducibili al segnale che li ha
            // creati leggendo la sola piattaforma, senza dipendere dallo stato locale del bot.
            var label = MakeLabel(intent.StrategyCode, intent.IntentId);

            // Stop Loss/Take Profit del segnale applicati come livelli nativi sull'ordine: li gestisce
            // il broker; l'eventuale chiusura risultante viene comunque intercettata e riportata al
            // server da OnPositionClosed (vedi nota in testa al file).
            var stopLossPips = ToPips(symbol, intent.StopLoss);
            var takeProfitPips = ToPips(symbol, intent.TakeProfit);

            // Il segnale precedente della stessa strategia SULLO STESSO LATO è scaduto nel momento in cui
            // ne arriva uno nuovo: il motore riemette l'ordine a ogni barra col livello ricalcolato,
            // quindi il vecchio ordine pending non è un secondo ordine, è lo stesso ordine da sostituire.
            // Il match è per strategia e lato, non per label esatta: la label del segnale nuovo porta un
            // IntentId diverso. Il lato serve perché una strategia non simmetrica manda le due gambe del
            // bracket sulla stessa barra, e non sono l'una la sostituzione dell'altra.
            CancelStrategyPendingOrders(intent.StrategyCode, tradeType, "sostituito dal signal successivo");

            // Gli intent precedenti della stessa strategia sullo stesso lato sono stati appena cancellati
            // a mercato: le loro label non si apriranno più, e senza questa potatura la mappa crescerebbe
            // di una voce per barra (prima la chiave era la sola strategia e si sovrascriveva da sé).
            // Il filtro sul lato è obbligatorio: potando anche la gamba opposta, il suo fill arriverebbe a
            // OnPositionOpened senza intent associato e la posizione non verrebbe riportata al server.
            foreach (var stale in _lastOpenIntentByLabel
                .Where(e => e.Key.StartsWith(MakeStrategyLabelPrefix(intent.StrategyCode), StringComparison.Ordinal) &&
                            e.Value.Side == intent.Side)
                .Select(e => e.Key)
                .ToList())
                _lastOpenIntentByLabel.Remove(stale);

            _lastOpenIntentByLabel[label] = intent;

            LogRischioDichiarato(intent, symbol, volume, stopLossPips);

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
                    _pendingOrderBar[label] = new PendingOrderMark { Stream = stream, BarCount = stream.Series.Count, Side = tradeType };
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
        /// Una posizione di questo bot si è aperta: ritira la gamba opposta del bracket (OCO), risolve
        /// l'intent che l'ha originata (ordine a mercato o pending appena riempito) e riporta il fill al
        /// server. Serve perché <c>PlaceStopOrder</c>/<c>PlaceLimitOrder</c> non restituiscono una
        /// posizione sincrona.
        /// </summary>
        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == null || !position.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                return;
            if (_openPositions.ContainsKey(position.Id))
                return;

            EnforceBracketOco(position);

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
                ContractMultiplier = intent.ContractMultiplier > 0 ? intent.ContractMultiplier : 1m,
                EntryPrice = position.EntryPrice,
                OpenTimeUtc = Server.TimeInUtc
            };
            _pendingOrderBar.Remove(position.Label);
            _lastOpenIntentByLabel.Remove(position.Label); // l'intent ha prodotto la sua posizione: esaurito
            SaveLocalState();

            RiancoraBracketAlFill(position, intent);

            // Lo spread va letto ADESSO: e' il costo di esecuzione di QUESTO ingresso, e fra due
            // minuti vale un altro numero. Non serve alla contabilita' — il P&L viene dai prezzi —
            // ma senza non e' misurabile quanto lo strumento si mangia del margine operativo.
            var spread = MeasureSpreadAtFill(position, intent);

            ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Filled,
                ToContractQuantity(position.SymbolName, position.VolumeInUnits),
                (decimal)position.EntryPrice, position.Id.ToString(),
                spreadAtFill: spread);

            CancelPendingOrdersAtCap();
        }

        /// <summary>
        /// Una posizione si e' appena aperta: se il piano conta solo le posizioni riempite e il
        /// tetto e' stato raggiunto, spegne gli ordini pendenti rimasti.
        ///
        /// <para>E' la meta' client del limite di concorrenza, ed e' l'unica che poteva stare qui.
        /// In modalita' <c>PositionsOnly</c> il server distribuisce tutti gli intent della barra
        /// senza contare gli ordini a mercato — di proposito: su un motore breakout non si sa
        /// quale livello verra' toccato, e bloccarne uno per "occupazione di slot" significa
        /// perdere il solo che sarebbe partito. Il tetto viene quindi fatto valere a valle, nel
        /// momento in cui si scopre quale ordine e' entrato davvero: il primo fill spegne gli
        /// altri, come un OCO.</para>
        ///
        /// <para>Il disaccoppiamento regge perche' il bot non riceve mai un comando: legge dal
        /// descriptor un parametro di configurazione, decide da solo guardando la propria
        /// piattaforma, e comunica al server solo il fatto compiuto — un <c>Cancelled</c> sullo
        /// stesso canale degli ordini scaduti, che libera gli slot senza che il server debba sapere
        /// perche'. Il server continua a decidere <i>cosa</i>, il broker <i>se e a che prezzo</i>.</para>
        /// </summary>
        private void CancelPendingOrdersAtCap()
        {
            if (!_cancelPendingAtCap || !_enforceConcurrency || _maxConcurrentTrades <= 0)
                return;
            if (CountPiootooPositions() < _maxConcurrentTrades)
                return;

            var stale = PendingOrders
                .Where(o => o.Label != null && o.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList();
            if (stale.Count == 0)
                return;

            Print("Raggiunto il massimo di {0} posizioni contemporanee: cancello {1} ordini pendenti.",
                _maxConcurrentTrades, stale.Count);
            foreach (var order in stale)
            {
                // La scadenza a una barra non serve piu' a un ordine che non esiste: senza questa
                // potatura CancelExpiredPendingOrders ritenterebbe di cancellarlo a ogni barra.
                _pendingOrderBar.Remove(order.Label);
                CancelAndReport(order, "tetto di posizioni contemporanee raggiunto");
            }
        }

        /// <summary>Posizioni aperte da questo bot, su tutti i simboli e tutte le strategie.</summary>
        private int CountPiootooPositions() =>
            Positions.Count(p => p.Label != null && p.Label.StartsWith(LabelPrefix, StringComparison.Ordinal));

        /// <summary>
        /// Bid, Ask e distanza dal prezzo dell'intent nell'istante in cui l'intent arriva al bot.
        ///
        /// <para>Serve a rispondere alla domanda "il bot sta lavorando bene?", che dai soli fill non
        /// si risponde: al fill si vede il prezzo ottenuto, non quello che il server aveva in mente
        /// ne' quello che il mercato offriva quando l'ordine e' partito. Le tre anomalie che questa
        /// riga rende visibili sono: (1) <b>ritardo</b> — <c>eta</c> misura quanto tempo e' passato da
        /// <c>ValidFromUtc</c>, cioe' dalla barra che ha generato il segnale, e se cresce il collo di
        /// bottiglia e' nel giro poll/valutazione, non nel broker; (2) <b>prezzo del server fuori
        /// mercato</b> — una distanza sistematicamente grande su ordini a mercato significa che il
        /// server sta prezzando su una barra vecchia (vedi
        /// <c>docs/domini/orologio-barre-e-fill.md</c>); (3) <b>livello dal lato sbagliato</b> — uno
        /// Stop long sotto l'Ask o un Limit long sopra l'Ask e' un ordine che si riempie
        /// immediatamente invece di aspettare il breakout, ed e' un bug, non un evento di mercato.</para>
        ///
        /// <para>Lo spread e' calcolato come <c>Ask − Bid</c> e non con <c>symbol.Spread</c> per
        /// coerenza con i due prezzi stampati sulla stessa riga: sono letti nello stesso istante.</para>
        ///
        /// <para>Stampa <b>sempre, a qualunque <see cref="LivelloLog"/> e anche in backtest</b>: e' una
        /// riga per intent di ingresso, quindi proporzionale ai trade e non alle barre, e spegnerla
        /// significherebbe scoprire il problema senza avere piu' i dati per spiegarlo. Con
        /// <see cref="LogServerResponses"/> scrive anche il record strutturato sul JSONL: il buffer
        /// della piattaforma scarta le righe vecchie, il file resta ed e' greppabile.</para>
        /// </summary>
        private void LogIntentMarket(OrderIntentDto intent, Symbol symbol)
        {
            var bid = symbol.Bid;
            var ask = symbol.Ask;
            var spread = ask - bid;

            // Il lato che conta e' quello su cui si ENTRA: Ask per i long, Bid per gli short. E'
            // l'unico confronto sensato col prezzo dell'intent, e lo stesso che usa il filtro
            // slippage qui sopra.
            var riferimento = intent.Side == SignalTypeDto.Buy ? ask : bid;
            var prezzo = (double)intent.Price;

            double? distanzaPips = prezzo > 0 && symbol.PipSize > 0
                ? Math.Abs(riferimento - prezzo) / symbol.PipSize
                : (double?)null;
            double? spreadPips = symbol.PipSize > 0 ? spread / symbol.PipSize : (double?)null;

            // Due grandezze diverse, che fino alla 2.2.0 erano schiacciate in un unico "eta" calcolato
            // su ValidFromUtc — cioe' su un istante FUTURO per costruzione, il bordo della barra
            // successiva. Ne uscivano numeri senza senso (3900s, -3599s, -188100s) che non
            // misuravano ne' l'una ne' l'altra cosa:
            //  - RITARDO e' il tempo passato dalla barra che ha prodotto il segnale. E' l'unico
            //    numero che dice se il collo di bottiglia sta nel giro push/valutazione/claim: su un
            //    segnale sano vale meno di un paio di secondi, e se cresce il problema e' nostro.
            //  - ATTESA e' quanto manca all'attivazione del pending. Negativa significa gia' attivo.
            //    Un'attesa di piu' di una barra su un ordine "next bar" e' un intent vecchio
            //    riproposto, non un segnale nuovo.
            double? ritardoSecondi = intent.CreatedAtUtc.HasValue
                ? (Server.TimeInUtc - intent.CreatedAtUtc.Value).TotalSeconds
                : (double?)null;
            double? attesaSecondi = intent.ValidFromUtc.HasValue
                ? (intent.ValidFromUtc.Value - Server.TimeInUtc).TotalSeconds
                : (double?)null;

            // Coerenza del livello col lato del mercato. Vale solo per i pending: un ordine a mercato
            // non ha un livello da rispettare.
            bool? livelloCoerente = null;
            if (prezzo > 0)
            {
                if (intent.OrderType == TradeOrderTypeDto.Stop)
                    livelloCoerente = intent.Side == SignalTypeDto.Buy ? prezzo > ask : prezzo < bid;
                else if (intent.OrderType == TradeOrderTypeDto.Limit)
                    livelloCoerente = intent.Side == SignalTypeDto.Buy ? prezzo < ask : prezzo > bid;
            }

            Print("Intent {0}/{1} {2} {3}: prezzo {4:0.#####} | Bid {5:0.#####} Ask {6:0.#####} " +
                  "spread {7:0.#####} ({8}) | distanza da {9} {10} | ritardo {11} | attesa {12} | qty {13}",
                intent.Symbol, intent.StrategyCode, intent.Side, intent.OrderType, intent.Price,
                bid, ask, spread,
                spreadPips.HasValue ? $"{spreadPips.Value:0.#} pip" : "pip n/d",
                intent.Side == SignalTypeDto.Buy ? "Ask" : "Bid",
                distanzaPips.HasValue ? $"{distanzaPips.Value:0.#} pip" : "n/d",
                ritardoSecondi.HasValue ? $"{ritardoSecondi.Value:0.#}s" : "n/d",
                attesaSecondi.HasValue ? $"{attesaSecondi.Value:0.#}s" : "n/d",
                intent.FinalQuantity);

            // Un'attesa che supera la barra della strategia non e' un pending consegnato in anticipo:
            // e' un intent di una barra passata che il server continua a riproporre. Senza questa
            // riga si vede solo lo stesso livello ripiazzato per ore, senza capire da dove venga.
            if (attesaSecondi.HasValue && intent.TimeframeMinutes > 0 &&
                Math.Abs(attesaSecondi.Value) > intent.TimeframeMinutes * 60.0)
                Print("  ATTENZIONE {0}/{1}: attesa {2:0}s oltre la barra da {3} minuti " +
                      "(ValidFrom {4:yyyy-MM-dd HH:mm:ss}Z, ora server {5:yyyy-MM-dd HH:mm:ss}Z): intent non allineato alla barra corrente.",
                    intent.Symbol, intent.StrategyCode, attesaSecondi.Value, intent.TimeframeMinutes,
                    intent.ValidFromUtc, Server.TimeInUtc);

            // Riga separata e in chiaro: e' un difetto del sistema, non una condizione di mercato, e
            // deve saltare all'occhio anche in mezzo a un log fitto.
            if (livelloCoerente == false)
                Print("  ATTENZIONE {0}/{1}: livello {2} {3} {4:0.#####} dalla parte sbagliata del mercato " +
                      "(Bid {5:0.#####} / Ask {6:0.#####}): si riempirebbe subito invece di attendere.",
                    intent.Symbol, intent.StrategyCode, intent.Side, intent.OrderType, intent.Price, bid, ask);

            LogJsonEvent("intent/ricevuto", new
            {
                intent.IntentId,
                intent.StrategyCode,
                intent.Symbol,
                BrokerSymbol = symbol.Name,
                Side = intent.Side.ToString(),
                OrderType = intent.OrderType.ToString(),
                IntentPrice = intent.Price,
                Bid = bid,
                Ask = ask,
                Spread = spread,
                SpreadPips = spreadPips,
                DistanzaPips = distanzaPips,
                LivelloCoerente = livelloCoerente,
                RitardoSecondi = ritardoSecondi,
                AttesaSecondi = attesaSecondi,
                intent.CreatedAtUtc,
                intent.ValidFromUtc,
                intent.ExpiresAtUtc,
                intent.FinalQuantity,
                intent.StopLoss,
                intent.TakeProfit,
                ServerTimeUtc = Server.TimeInUtc
            });
        }

        /// <summary>
        /// Vero quando il run e' il campione sorgente, cioe' quando i filtri di ingresso di questo
        /// bot vanno sospesi.
        ///
        /// <para>Il profilo <c>BacktestSorgente</c> serve a misurare la fedelta' della traduzione
        /// C# rispetto alle strategie Python. Qualunque filtro lato client che la sorgente non
        /// prevede sporca quella misura: davanti a una differenza non si saprebbe piu' dire se viene
        /// dal porting o dal filtro. Il filtro sul lato del livello resta attivo anche qui, perche'
        /// non e' discrezionale — un pending dal lato sbagliato e' un errore di prezzatura, non una
        /// scelta di strategia.</para>
        /// </summary>
        private bool FiltriIngressoSospesi =>
            string.Equals(_runProfile, nameof(RunProfileParam.BacktestSorgente),
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Scarta gli intent che non sono eseguibili come la strategia li ha pensati. Restituisce
        /// true quando l'intent e' stato rifiutato e riportato al server, e il chiamante deve
        /// fermarsi.
        ///
        /// <para>Sono tre condizioni distinte, tutte disattivabili dai rispettivi parametri, e
        /// nessuna delle tre e' un evento di mercato. Le ultime due sono discrezionali e restano
        /// spente nel profilo <c>BacktestSorgente</c>: vedi <see cref="FiltriIngressoSospesi"/>.</para>
        /// <list type="number">
        /// <item><b>Livello dal lato sbagliato.</b> Uno Stop long sotto l'Ask (o uno Stop short sopra
        /// il Bid) si riempie all'istante: non e' piu' il breakout che la strategia aspettava, e' un
        /// ordine a mercato al prezzo peggiore dei due. Nei log fino alla 2.2.0 questo produceva fill
        /// entro il millisecondo dal piazzamento, con l'avviso stampato e l'ordine mandato lo stesso.</item>
        /// <item><b>Livello troppo lontano.</b> Un pending "next bar" vale la sua barra: oltre una
        /// certa distanza quel livello non verra' toccato, e l'ordine e' solo un intent vecchio che
        /// il server continua a riproporre. La misura e' in punti dello strumento: legarla allo stop,
        /// come si faceva fino alla 3.0.0, penalizzava le strategie con stop stretto e ingresso
        /// lontano proprio per come sono progettate.</item>
        /// <item><b>Spread troppo pesante.</b> Su un long si entra sull'Ask e lo stop e' valutato sul
        /// Bid: uno spread pari a un quinto dello stop si mangia un quinto del respiro prima ancora
        /// che il trade cominci. Si misura qui, al piazzamento, perche' e' l'ultimo istante in cui
        /// non fare il trade e' ancora un'opzione.</item>
        /// </list>
        /// </summary>
        private bool RejectUnsoundIntent(OrderIntentDto intent, Symbol symbol)
        {
            var bid = symbol.Bid;
            var ask = symbol.Ask;
            var prezzo = (double)intent.Price;
            var isPending = intent.OrderType == TradeOrderTypeDto.Stop ||
                            intent.OrderType == TradeOrderTypeDto.Limit;

            string motivo = null;

            if (RejectWrongSideLevels && isPending && prezzo > 0)
            {
                var coerente = intent.OrderType == TradeOrderTypeDto.Stop
                    ? (intent.Side == SignalTypeDto.Buy ? prezzo > ask : prezzo < bid)
                    : (intent.Side == SignalTypeDto.Buy ? prezzo < ask : prezzo > bid);
                if (!coerente)
                    motivo = $"livello {intent.OrderType} {intent.Side} {prezzo:0.#####} dal lato sbagliato " +
                             $"(Bid {bid:0.#####} / Ask {ask:0.#####})";
            }

            // Attivazione oltre la barra corrente, e solo IN AVANTI. Non e' discrezionale: come il
            // lato del livello e' un errore di sistema, non una scelta di strategia. Un ordine
            // "next bar" che si attiva due giorni dopo porta il livello di una barra che non e'
            // quella in cui vivra', e resta appeso sul broker a un regime di mercato diverso.
            // Un'attesa NEGATIVA e' invece il caso normale — il pending e' gia' attivo quando
            // l'intent arriva — e trattarla allo stesso modo scarterebbe quasi tutti gli intent sani.
            //
            // La causa a monte stava in EasyLib.EstimateNextBarUtc, che deduceva il timeframe dalla
            // distanza fra le ultime due barre: sulla prima barra dopo il fine settimana quella
            // distanza e' il buco (circa 49 ore sull'oro), e ValidFromUtc nasceva due giorni avanti.
            // Corretta il 25/08/2026 usando il timeframe dichiarato dalla strategia; questo resta la
            // rete sotto, perche' l'effetto sul broker non si vedeva nel log finche' non lo si e'
            // cercato: l'ordine veniva piazzato lo stesso, con solo un avviso stampato.
            if (motivo is null && intent.ValidFromUtc.HasValue && intent.TimeframeMinutes > 0)
            {
                var attesa = (intent.ValidFromUtc.Value - Server.TimeInUtc).TotalSeconds;
                if (attesa > intent.TimeframeMinutes * 60.0)
                    motivo = $"attivazione {attesa:0}s in avanti, oltre la barra da " +
                             $"{intent.TimeframeMinutes} minuti " +
                             $"(ValidFrom {intent.ValidFromUtc.Value:yyyy-MM-dd HH:mm:ss}Z)";
            }

            var stop = (double)(intent.StopLoss ?? 0m);

            // Nel profilo sorgente ogni segnale deve diventare un ordine: e' il run che misura la
            // fedelta' rispetto al Python, e un filtro che la sorgente non ha falsa la misura.
            var filtriDiscrezionaliAttivi = !FiltriIngressoSospesi;

            if (motivo is null && filtriDiscrezionaliAttivi &&
                MaxEntryDistancePoints > 0 && isPending && prezzo > 0)
            {
                var riferimento = intent.Side == SignalTypeDto.Buy ? ask : bid;
                var distanza = Math.Abs(riferimento - prezzo);
                if (distanza > MaxEntryDistancePoints)
                    motivo = $"livello a {distanza:0.##} punti dal mercato, oltre il tetto di " +
                             $"{MaxEntryDistancePoints:0.##} punti";
            }

            if (motivo is null && filtriDiscrezionaliAttivi && MaxSpreadPercentOfStop > 0 && stop > 0)
            {
                var spread = ask - bid;
                var peso = spread / stop * 100.0;
                if (peso > MaxSpreadPercentOfStop)
                    motivo = $"spread {spread:0.##} punti pari al {peso:0.#}% dello stop da {stop:0.##}, " +
                             $"oltre il tetto del {MaxSpreadPercentOfStop:0.#}%";
            }

            if (motivo is null)
                return false;

            Print("Ingresso {0}/{1} scartato: {2}.", intent.Symbol, intent.StrategyCode, motivo);
            LogJsonEvent("intent/scartato-filtro", new
            {
                intent.IntentId,
                intent.StrategyCode,
                intent.Symbol,
                Side = intent.Side.ToString(),
                OrderType = intent.OrderType.ToString(),
                IntentPrice = intent.Price,
                Bid = bid,
                Ask = ask,
                intent.StopLoss,
                Motivo = motivo,
                ServerTimeUtc = Server.TimeInUtc
            });

            // Rifiutato e non semplicemente ignorato: un intent lasciato Pending resta assegnato a
            // questo account, tiene chiusi i lucchetti e viene riproposto a ogni poll.
            ReportExecution(intent.IntentId, intent.Symbol, ExecutionReportStatusDto.Rejected, 0, null);
            return true;
        }

        /// <summary>
        /// Spread dello strumento nell'istante del fill, e il suo peso sullo stop della strategia.
        ///
        /// <para>Su un CFD long si entra sull'<b>Ask</b> e lo stop e' valutato sul <b>Bid</b>: la
        /// perdita in denaro quando lo stop salta resta quella dichiarata, ma il Bid deve scendere
        /// solo di <c>(distanza stop − spread)</c> per farlo saltare. Il rapporto
        /// <c>spread / distanza stop</c> e' quindi quanto respiro lo strumento si prende, e cambia per
        /// strategia: su uno stop da 12,5 punti uno spread di 2 vale il 16%, su uno da 50 il 4%.</para>
        ///
        /// <para>Si stampa a ogni fill e si accumula per il riepilogo di <c>OnStop</c>: un singolo
        /// spread non dice niente, la media per strategia su decine di fill si'.</para>
        /// </summary>
        private decimal? MeasureSpreadAtFill(Position position, OrderIntentDto intent)
        {
            var symbol = Symbols.GetSymbol(position.SymbolName);
            if (symbol == null)
                return null;

            var spread = (decimal)symbol.Spread;
            if (spread <= 0)
                return null;

            var stop = intent.StopLoss ?? 0m;
            if (!_spreadByStrategy.TryGetValue(intent.StrategyCode, out var stats))
                _spreadByStrategy[intent.StrategyCode] = stats = new SpreadStats { StopDistance = stop };
            stats.Fills++;
            stats.SpreadTotal += spread;

            if (stop > 0)
                Print("Fill {0} {1}: spread {2:0.##} punti su stop {3:0.##} = {4:0.#}% del respiro.",
                    intent.StrategyCode, position.SymbolName, spread, stop, spread / stop * 100m);
            else
                Print("Fill {0} {1}: spread {2:0.##} punti (la strategia non dichiara uno stop).",
                    intent.StrategyCode, position.SymbolName, spread);

            return spread;
        }

        /// <summary>
        /// Consuntivo di un trade appena chiuso: com'e' entrato, come e' uscito, e quanto ha reso.
        ///
        /// <para>E' la riga che mancava del tutto fino alla 2.2.0. Il log raccontava per intero come
        /// nascevano gli ordini — intent, spread, fill — e poi taceva: delle chiusure restava solo il
        /// silenzio, quindi da un log di backtest si poteva dire se il bot aveva <i>eseguito</i> ma non
        /// se aveva <i>guadagnato</i>. Le due domande sono indipendenti: un'esecuzione perfetta di una
        /// strategia sbagliata produce un log pulito e un conto vuoto.</para>
        ///
        /// <para>I quattro numeri che spiegano il trade, oltre al P&amp;L: <b>motivo</b> (chi ha chiuso —
        /// stop, target, tempo, flat del fine settimana), <b>MFE/MAE</b> (dove era arrivato a favore e
        /// contro), <b>durata</b> in minuti, <b>modifiche di trailing</b>. Insieme dicono se un trade in
        /// perdita e' stato un'entrata sbagliata o un'uscita gestita male, che dal solo P&amp;L non si
        /// distingue.</para>
        /// </summary>
        /// <param name="position">
        /// Puo' essere null: quando la chiusura la scopre <see cref="CloseExpiredPositions"/> la
        /// posizione non esiste piu' e l'unica fonte e' lo storico.
        /// </param>
        private void LogTradeOutcome(
            OpenPositionContext ctx, Position position, HistoricalTrade trade, string reason)
        {
            var netProfit = (decimal)(trade?.NetProfit ?? position?.NetProfit ?? 0);
            var grossProfit = (decimal)(trade?.GrossProfit ?? position?.GrossProfit ?? 0);
            var commission = (decimal)(trade?.Commissions ?? 0);
            var swap = (decimal)(trade?.Swap ?? position?.Swap ?? 0);
            var entryPrice = trade?.EntryPrice ?? ctx.EntryPrice;
            var closePrice = trade?.ClosingPrice ?? 0.0;
            // Quantita' nei contratti del broker, non nelle unita' della piattaforma: e' la stessa
            // grandezza in cui il server ha dichiarato l'intent, ed e' quella che si aspetta indietro.
            var quantity = ToContractQuantity(
                trade?.SymbolName ?? position?.SymbolName, trade?.VolumeInUnits ?? position?.VolumeInUnits ?? 0);

            var durataMinuti = ctx.OpenTimeUtc.HasValue
                ? (Server.TimeInUtc - ctx.OpenTimeUtc.Value).TotalMinutes
                : (double?)null;

            // Utile per contratto Piootoo: il conto e' in contratti broker, le rotazioni Titano
            // ragionano in contratti Piootoo. Senza questa riduzione due account con conversioni
            // diverse produrrebbero numeri non confrontabili sulla stessa strategia.
            var perContratto = ctx.ContractMultiplier > 0 ? netProfit / ctx.ContractMultiplier : netProfit;

            if (!_tradeStats.TryGetValue(ctx.StrategyCode, out var stats))
                _tradeStats[ctx.StrategyCode] = stats = new TradeStats();
            stats.Register(netProfit);

            Print("Chiuso {0} {1} {2}: {3:0.#####} -> {4:0.#####} qty {5} | esito {6} | " +
                  "lordo {7:0.00} commissioni {8:0.00} swap {9:0.00} netto {10:0.00} ({11:0.00}/contratto) | " +
                  "MFE {12:0.##} MAE {13:0.##} punti | {14} | trailing {15}x",
                ctx.StrategyCode, ctx.Symbol, trade?.TradeType ?? position?.TradeType,
                entryPrice, closePrice, quantity, reason,
                grossProfit, commission, swap, netProfit, perContratto,
                ctx.MaxFavorablePoints, ctx.MaxAdversePoints,
                durataMinuti.HasValue ? $"{durataMinuti.Value:0} min ({ctx.BarsInPosition} barre)" : $"{ctx.BarsInPosition} barre",
                ctx.TrailingUpdates);

            LogJsonEvent("trade/chiuso", new
            {
                ctx.StrategyCode,
                ctx.Symbol,
                PositionId = ctx.PositionId,
                EntryIntentId = ctx.EntryIntentId,
                Side = (trade?.TradeType ?? position?.TradeType)?.ToString(),
                EntryPrice = entryPrice,
                ClosePrice = closePrice,
                Quantity = quantity,
                Motivo = reason,
                GrossProfit = grossProfit,
                Commission = commission,
                Swap = swap,
                NetProfit = netProfit,
                NetProfitPerContract = perContratto,
                MfePoints = ctx.MaxFavorablePoints,
                MaePoints = ctx.MaxAdversePoints,
                DurataMinuti = durataMinuti,
                ctx.BarsInPosition,
                ctx.TrailingUpdates,
                OpenTimeUtc = ctx.OpenTimeUtc,
                CloseTimeUtc = Server.TimeInUtc
            });
        }

        /// <summary>
        /// Esito dei trade chiusi, per strategia. Come <see cref="SpreadStats"/> non influenza
        /// nessuna decisione: e' il consuntivo di fine run.
        /// </summary>
        private sealed class TradeStats
        {
            public int Trades;
            public int Vincenti;
            public decimal Totale;
            public decimal SommaVincite;
            public decimal SommaPerdite;
            public decimal Migliore;
            public decimal Peggiore;

            public void Register(decimal netProfit)
            {
                Trades++;
                Totale += netProfit;
                if (netProfit >= 0)
                {
                    Vincenti++;
                    SommaVincite += netProfit;
                }
                else
                {
                    SommaPerdite += -netProfit;
                }

                if (Trades == 1 || netProfit > Migliore) Migliore = netProfit;
                if (Trades == 1 || netProfit < Peggiore) Peggiore = netProfit;
            }

            public decimal Media => Trades > 0 ? Totale / Trades : 0m;
            public double WinRate => Trades > 0 ? Vincenti * 100.0 / Trades : 0.0;

            /// <summary>
            /// Profit factor: vincite su perdite. Senza perdite non e' definito — si restituisce null
            /// invece di un infinito o di un numero enorme, che in una tabella si legge come un
            /// risultato eccezionale mentre significa solo "campione troppo piccolo".
            /// </summary>
            public decimal? ProfitFactor => SommaPerdite > 0 ? SommaVincite / SommaPerdite : (decimal?)null;
        }

        /// <summary>
        /// Consuntivo per strategia a fine run: e' la tabella con cui si decide se una strategia
        /// merita di restare nel piano. Sta accanto al riepilogo degli spread di proposito — costo di
        /// esecuzione e risultato sono le due meta' della stessa domanda.
        /// </summary>
        private void PrintTradeSummary()
        {
            if (_tradeStats.Count == 0)
            {
                Print("--- Nessun trade chiuso in questo run ---");
                return;
            }

            var totale = 0m;
            var trades = 0;
            Print("--- Esito dei trade per strategia ---");
            foreach (var entry in _tradeStats.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var s = entry.Value;
                totale += s.Totale;
                trades += s.Trades;
                Print("  {0}: {1} trade, {2:0.#}% vincenti, netto {3:0.00} (media {4:0.00}), " +
                      "PF {5}, migliore {6:0.00}, peggiore {7:0.00}",
                    entry.Key, s.Trades, s.WinRate, s.Totale, s.Media,
                    s.ProfitFactor.HasValue ? s.ProfitFactor.Value.ToString("0.00") : "n/d",
                    s.Migliore, s.Peggiore);
            }

            Print("  TOTALE: {0} trade, netto {1:0.00}.", trades, totale);
        }

        /// <summary>Spread misurati ai fill, per strategia. Serve solo al riepilogo diagnostico.</summary>
        private sealed class SpreadStats
        {
            public int Fills;
            public decimal SpreadTotal;
            public decimal StopDistance;
            public decimal Average => Fills > 0 ? SpreadTotal / Fills : 0m;
        }

        /// <summary>
        /// Riepilogo dei costi di esecuzione a fine run. E' il numero che serve per decidere se una
        /// strategia ha senso su questo strumento: uno stop stretto su uno spread largo non e' un
        /// difetto del sistema, e' una coppia strategia/strumento sbagliata.
        /// </summary>
        private void PrintSpreadSummary()
        {
            if (_spreadByStrategy.Count == 0)
                return;

            Print("--- Costo di esecuzione misurato ai fill ---");
            foreach (var entry in _spreadByStrategy.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var stats = entry.Value;
                if (stats.StopDistance > 0)
                    Print("  {0}: {1} fill, spread medio {2:0.###} punti, stop {3:0.##} -> {4:0.#}% del respiro.",
                        entry.Key, stats.Fills, stats.Average, stats.StopDistance,
                        stats.Average / stats.StopDistance * 100m);
                else
                    Print("  {0}: {1} fill, spread medio {2:0.###} punti.",
                        entry.Key, stats.Fills, stats.Average);
            }
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
        /// Rimette Stop Loss e Take Profit alla distanza dichiarata dal segnale, misurata dal prezzo di
        /// fill invece che da quello richiesto.
        ///
        /// <para><b>Perche' serve.</b> <c>PlaceStopOrder</c>/<c>PlaceLimitOrder</c> prendono la distanza
        /// in pip e la fissano rispetto al prezzo dell'ORDINE. Quando l'ordine slitta, il bracket resta
        /// dov'era: lo stop si allarga esattamente di quanto e' slittato l'ingresso e il target si
        /// avvicina della stessa quantita'. Sugli ordini stop lo slippage e' per costruzione
        /// sfavorevole, quindi si paga sempre in quella direzione — misurato su un run 2022-2023:
        /// mediana +0,39 punti di stop in piu', code fino a +4,58, e una posizione su cinque oltre il
        /// punto pieno di rischio non dichiarato. Sui limit accade il contrario e lo stop si stringe.</para>
        ///
        /// <para>Riancorare al fill e' l'unico modo perche' il rischio a mercato sia quello che il
        /// server ha dichiarato, che e' anche quello che il backtest applica.</para>
        /// </summary>
        private void RiancoraBracketAlFill(Position position, OrderIntentDto intent)
        {
            if (!intent.StopLoss.HasValue && !intent.TakeProfit.HasValue)
                return;

            var symbol = Symbols.GetSymbol(position.SymbolName);
            if (symbol is null)
                return;

            var entry = position.EntryPrice;
            var verso = position.TradeType == TradeType.Buy ? 1 : -1;

            double? Livello(decimal? distanza, int direzione)
            {
                if (!distanza.HasValue || distanza.Value <= 0)
                    return null;
                return Math.Round(entry + direzione * verso * (double)distanza.Value, symbol.Digits);
            }

            var nuovoSl = Livello(intent.StopLoss, -1) ?? position.StopLoss;
            var nuovoTp = Livello(intent.TakeProfit, +1) ?? position.TakeProfit;

            // Se il fill e' avvenuto al prezzo richiesto il bracket e' gia' giusto: non si spende una
            // ModifyPosition per niente (in backtest e' gratis, in reale e' una richiesta al broker).
            var slGiaGiusto = !nuovoSl.HasValue || (position.StopLoss.HasValue &&
                Math.Abs(position.StopLoss.Value - nuovoSl.Value) < symbol.TickSize / 2);
            var tpGiaGiusto = !nuovoTp.HasValue || (position.TakeProfit.HasValue &&
                Math.Abs(position.TakeProfit.Value - nuovoTp.Value) < symbol.TickSize / 2);
            if (slGiaGiusto && tpGiaGiusto)
                return;

            var result = ModifyPosition(position, nuovoSl, nuovoTp);
            if (!result.IsSuccessful)
            {
                Print("Riancoraggio bracket fallito per {0}/{1} (posizione {2}): {3}",
                    intent.Symbol, intent.StrategyCode, position.Id, result.Error);
                return;
            }

            Print("Bracket riancorato al fill {0}/{1}: ingresso {2:0.#####} (richiesto {3:0.#####}) " +
                  "SL {4:0.#####} TP {5:0.#####}",
                intent.Symbol, intent.StrategyCode, entry, intent.Price, nuovoSl, nuovoTp);
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

            // Uno slot di concorrenza si e' liberato e la strategia non ha piu' un ingresso "in volo"
            // su quel simbolo: e' l'unico modo in cui, senza barre nuove, un template gia' rifiutato
            // diventa reclamabile. Vedi ShouldPollOnTimer.
            _claimRetryPending = true;

            _openPositions.Remove(position.Id);
            _peakProfitAfterStall.Remove(position.Id);
            SaveLocalState();

            var trade = History.LastOrDefault(h => h.PositionId == position.Id);
            var closePrice = (decimal?)trade?.ClosingPrice;
            // Quantita' nei contratti del broker, non nelle unita' della piattaforma: e' la stessa
            // grandezza in cui il server ha dichiarato l'intent, ed e' quella che si aspetta indietro.
            var quantity = ToContractQuantity(position.SymbolName, trade?.VolumeInUnits ?? position.VolumeInUnits);
            var commission = (decimal)(trade?.Commissions ?? 0);
            // Stessa fonte che LogTradeOutcome usa per stampare lo swap: senza inoltrarlo al server
            // il netto persistito ignorava gli interessi di finanziamento, che su una posizione
            // multigiorno valgono piu' della commissione.
            var swap = (decimal)(trade?.Swap ?? position.Swap);

            LogTradeOutcome(ctx, position, trade, args.Reason.ToString());

            if (_serverCloseIntents.Remove(position.Id, out var closeIntent))
                ReportExecution(closeIntent.IntentId, position.SymbolName, ExecutionReportStatusDto.Filled,
                    quantity, closePrice, position.Id.ToString(), commission, swap: swap);
            else
                RegisterExternalCloseAndReport(ctx, position.SymbolName, quantity, closePrice, commission, swap,
                    args.Reason.ToString(), (decimal?)trade?.GrossProfit, (decimal?)trade?.NetProfit);
        }

        /// <summary>
        /// Chiusura scoperta in ritardo: la posizione non e' piu' fra le <c>Positions</c> ma
        /// <see cref="OnPositionClosed"/> non l'ha vista passare. Succede con Stop Loss e Take Profit
        /// nativi, che il broker esegue per conto suo. L'esito reale sta nello storico: si legge da
        /// li' e si riporta al server, altrimenti il trade non esiste da nessuna parte.
        /// </summary>
        private void RegisterMissedClose(int positionId, OpenPositionContext ctx)
        {
            _openPositions.Remove(positionId);
            _peakProfitAfterStall.Remove(positionId);
            SaveLocalState();
            _claimRetryPending = true;

            var trade = History.LastOrDefault(h => h.PositionId == positionId);
            if (trade is null)
            {
                // Senza storico non c'e' niente da riportare, ma tacere e' peggio: e' esattamente il
                // buco che ha svuotato trades.json senza lasciare traccia nel log.
                Print("ATTENZIONE: posizione {0} ({1}/{2}) chiusa dal broker ma assente dallo storico: " +
                      "trade NON riportato al server.", positionId, ctx.Symbol, ctx.StrategyCode);
                return;
            }

            // Il motivo vero, non un generico "Closed": e' quello che distingue uno stop da un target
            // in trades.json, e senza il quale il confronto con il backtest non e' leggibile.
            var reason = DeduceCloseReason(trade);

            LogTradeOutcome(ctx, null, trade, reason);

            var quantity = ToContractQuantity(trade.SymbolName, trade.VolumeInUnits);
            RegisterExternalCloseAndReport(
                ctx, trade.SymbolName, quantity, (decimal)trade.ClosingPrice, (decimal)trade.Commissions,
                (decimal)trade.Swap, reason, (decimal)trade.GrossProfit, (decimal)trade.NetProfit,
                prefissoMotivo: "BrokerExit");
        }

        /// <summary>
        /// Distingue uno Stop Loss da un Take Profit guardando da che parte il prezzo di chiusura sta
        /// rispetto all'ingresso. cAlgo non espone il motivo sullo storico, e il segno del risultato e'
        /// l'unica informazione disponibile: basta a separare i due casi che contano.
        /// </summary>
        private static string DeduceCloseReason(HistoricalTrade trade)
        {
            if (trade.NetProfit > 0) return "TakeProfit";
            if (trade.NetProfit < 0) return "StopLoss";
            return "BrokerClose";
        }

        private void RegisterExternalCloseAndReport(
            OpenPositionContext ctx, string brokerSymbolName, decimal quantity, decimal? closePrice, decimal commission,
            decimal swap, string reason, decimal? grossProfit = null, decimal? netProfit = null,
            string prefissoMotivo = "LocalExit")
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
                    // LocalExit: l'ha chiusa il bot. BrokerExit: l'ha chiusa il broker sul bracket.
                    // I due casi vanno distinti in trades.json, perche' il secondo e' proprio quello
                    // che prima non ci arrivava affatto.
                    Reason = $"{prefissoMotivo}:{reason}"
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
                ReportExecution(closeIntent.IntentId, brokerSymbolName, ExecutionReportStatusDto.Filled, quantity, closePrice, null, commission,
                    swap: swap, grossProfit: grossProfit, netProfit: netProfit);
            }
            catch (Exception ex)
            {
                Print("Errore registrazione chiusura esterna {0}/{1}: {2}", ctx.Symbol, ctx.StrategyCode, ex.Message);
            }
        }

        /// <summary>
        /// Converte un volume espresso nelle UNITA' della piattaforma nei contratti del broker, cioe'
        /// nella stessa grandezza con cui il server dichiara <c>FinalQuantity</c> sull'intent.
        ///
        /// <para>Serve su ogni numero che torna indietro al server. Un fill di 0,1 lotti su XAUUSD
        /// vale 10 unita': riportare il 10 fa fallire la validazione dell'execution report, il
        /// server non registra l'apertura, e da quel momento rifiuta ogni nuovo segnale della stessa
        /// strategia perche' crede di avere un ingresso ancora in corso. Un errore di unita' qui non
        /// sbaglia un numero: blocca la strategia per il resto del run.</para>
        ///
        /// <para>Simbolo non risolvibile: si restituisce il volume invariato. E' il caso di una
        /// posizione su uno strumento non piu' fra quelli configurati, dove non c'e' un
        /// <c>LotSize</c> da cui convertire — meglio un numero grezzo di un'eccezione mentre si sta
        /// chiudendo una posizione.</para>
        /// </summary>
        private decimal ToContractQuantity(string brokerSymbolName, double volumeInUnits)
        {
            var symbol = Symbols.GetSymbol(brokerSymbolName);
            return symbol is null
                ? (decimal)volumeInUnits
                : (decimal)symbol.VolumeInUnitsToQuantity(volumeInUnits);
        }

        private void ReportExecution(
            string intentId, string symbol, ExecutionReportStatusDto status, decimal filledQuantity,
            decimal? fillPrice, string externalOrderId = null, decimal commission = 0,
            decimal? spreadAtFill = null, decimal swap = 0,
            decimal? grossProfit = null, decimal? netProfit = null)
        {
            // Un report assesta un intent lato server, e un intent assestato libera i lucchetti che
            // teneva occupati: e' uno dei due eventi che rendono sensato riprovare un claim senza
            // aspettare una barra nuova (l'altro e' la chiusura di una posizione). La bandiera si alza
            // per QUALUNQUE esito, anche per un fill di ingresso che i lucchetti li stringe: sbagliare
            // in questo verso costa un poll in piu' per esecuzione — un numero legato ai trade del run,
            // non ai secondi simulati — mentre dimenticarne uno costerebbe un segnale.
            _claimRetryPending = true;

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
                        Swap = swap,
                        EventTimeUtc = Server.TimeInUtc,
                        SpreadAtFill = spreadAtFill,
                        GrossProfit = grossProfit,
                        NetProfit = netProfit
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
        /// Come <see cref="LogJsonResponse"/> ma per eventi del bot invece che per risposte del
        /// server: il payload e' un oggetto, non una stringa gia' serializzata, cosi' i campi
        /// restano interrogabili sul JSONL invece di finire annidati come testo dentro <c>Body</c>.
        /// </summary>
        private void LogJsonEvent(string endpoint, object payload)
        {
            if (string.IsNullOrWhiteSpace(_jsonLogPath))
                return;
            try
            {
                var line = JsonSerializer.Serialize(new
                {
                    TimestampUtc = DateTime.UtcNow,
                    Endpoint = endpoint,
                    Payload = payload
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

        /// <summary>
        /// Confronta il rischio che sta per andare a mercato con quello dichiarato dalla strategia.
        ///
        /// <para>La strategia dichiara lo stop in denaro per contratto future di riferimento; il
        /// server lo ha diviso una volta sola per il valore punto di QUEL contratto e ha spedito
        /// dei punti. Qui si rifa' il conto nell'altro verso, con il valore pip di questo
        /// strumento e il volume che si sta davvero inviando: se i due numeri non si somigliano, la
        /// catena simbolo -> contratto -> size ha un fattore di troppo, ed e' esattamente il caso
        /// che restava invisibile finche' il denaro si fermava sul server.</para>
        ///
        /// <para>Solo diagnostica: non altera l'ordine. Il rapporto e' informativo anche quando e'
        /// 1 per costruzione — e' il caso in cui il conto torna, e vederlo scritto vale quanto
        /// vedere il caso in cui non torna.</para>
        /// </summary>
        private void LogRischioDichiarato(OrderIntentDto intent, Symbol symbol, double volume, double? stopLossPips)
        {
            if (!intent.StopLossMoneyPerFutureContract.HasValue || !stopLossPips.HasValue || volume <= 0)
                return;

            var dichiarato = intent.StopLossMoneyPerFutureContract.Value;

            // Denaro a rischio sull'ordine, nella valuta del conto: pip a rischio per valore del pip.
            var rischioOrdine = (decimal)(stopLossPips.Value * symbol.PipValue * volume);

            // Riportato al contratto Piootoo: e' il numero confrontabile con quello dichiarato,
            // perche' la quantita' e' l'unica cosa che il moltiplicatore di contratto ha toccato.
            // Si usano i lotti EFFETTIVI, non FinalQuantity: la normalizzazione del volume puo'
            // aver tagliato, e un rapporto calcolato su una taglia che non e' quella inviata
            // direbbe che il conto non torna proprio quando torna.
            var lottiEffettivi = (decimal)symbol.VolumeInUnitsToQuantity(volume);
            var quantita = lottiEffettivi != 0 ? lottiEffettivi : 1m;
            var perContrattoPiootoo = intent.ContractMultiplier > 0
                ? rischioOrdine / quantita * intent.ContractMultiplier
                : rischioOrdine / quantita;

            var rapporto = dichiarato != 0 ? perContrattoPiootoo / dichiarato : 0m;

            Print("  rischio {0}/{1}: dichiarato {2:0.##} per contratto, a mercato {3:0.##} " +
                  "(ordine {4:0.##}, rapporto {5:0.###})",
                  intent.Symbol, intent.StrategyCode, dichiarato, perContrattoPiootoo,
                  rischioOrdine, rapporto);

            LogJsonEvent("intent/rischio", new
            {
                intent.IntentId,
                intent.StrategyCode,
                intent.Symbol,
                BrokerSymbol = symbol.Name,
                DichiaratoPerContratto = dichiarato,
                intent.ReferenceDollarsPerPoint,
                intent.StopLoss,
                intent.PriceScale,
                intent.ContractMultiplier,
                intent.FinalQuantity,
                LottiEffettivi = lottiEffettivi,
                StopLossPips = stopLossPips.Value,
                PipValue = symbol.PipValue,
                Volume = volume,
                RischioOrdine = rischioOrdine,
                RischioPerContrattoPiootoo = perContrattoPiootoo,
                Rapporto = rapporto
            });
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

            /// <summary>
            /// Quante cose la sessione potrebbe consegnare a un claim, contate dal SERVER: template
            /// pendenti non scaduti piu' intent gia' assegnati e ancora pendenti. Zero = il poll non
            /// puo' restituire niente, quindi si puo' saltare.
            ///
            /// <para><b>Nullable di proposito.</b> Un server che non conosce il campo lo omette, e
            /// deserializzato su un <c>int</c> varrebbe 0, cioe' "non pollare mai": il bot smetterebbe
            /// di reclamare segnali per tutto il run, in silenzio. Con il nullable l'assenza resta
            /// distinguibile dallo zero e vale "non so", quindi si polla.</para>
            /// </summary>
            public int? ClaimableIntents { get; set; }
        }

        private sealed class OpenTradingPlanSessionRequestDto
        {
            public string PlanCode { get; set; }
            public string ClientRunMode { get; set; }
            public string ExecutionKey { get; set; }
            public string AccountNumber { get; set; }

            /// <summary>Nome del <c>TradingRunProfile</c>. Null = comportamento storico.</summary>
            public string RunProfile { get; set; }
        }

        private sealed class TradingSessionDescriptorDto
        {
            public string SessionId { get; set; }
            public string SessionToken { get; set; }
            public IReadOnlyList<TradingInstrumentDto> Instruments { get; set; }

            // Come il server ha effettivamente risolto il run. Sono i valori che il pannello a chart
            // mostra: non quelli che il bot ha chiesto, ma quelli che il server ha applicato. Se un
            // piano contraddice il parametro, la differenza si vede sul grafico invece che nei trade.
            public string RunProfile { get; set; }
            public string TitanoMode { get; set; }
            public string ClientRunMode { get; set; }
            public bool EnforceConcurrencyLimits { get; set; }
            public int MaxConcurrentTrades { get; set; }

            /// <summary>
            /// Cosa conta MaxConcurrentTrades: "PositionsAndPendingOrders" (default) oppure
            /// "PositionsOnly". Nel secondo caso gli ordini pendenti non consumano budget lato
            /// server, e tocca a questo bot spegnere quelli rimasti quando le posizioni riempite
            /// raggiungono il tetto. E' configurazione consegnata all'apertura: il server non
            /// chiedera' mai di cancellare un ordine specifico.
            /// </summary>
            public string ConcurrencyCountMode { get; set; }

            public IReadOnlyList<SessionStrategyDto> Strategies { get; set; }

            /// <summary>
            /// Cosa il conto puo' tenere e quando taglia, deciso dal piano. Questo bot non ha piu'
            /// parametri propri sull'argomento: vedi <see cref="ApplyHolding"/>.
            /// </summary>
            public HoldingDto Holding { get; set; }
        }

        /// <summary>La policy di tenuta del piano, come la dichiara il server.</summary>
        private sealed class HoldingDto
        {
            public bool AllowOvernight { get; set; }
            public bool AllowOverweek { get; set; }
            public int SessionFlatUtcHhmm { get; set; }
            public WeekEndFlatDto WeekEnd { get; set; }
        }

        /// <summary>Finestra di flat del fine settimana, in HHMM UTC, come la dichiara il server.</summary>
        private sealed class WeekEndFlatDto
        {
            public int FromUtcHhmm { get; set; }
            public int UntilUtcHhmm { get; set; }
        }

        /// <summary>Una strategia in sessione: codice di esecuzione, simbolo, timeframe, tenuta.</summary>
        private sealed class SessionStrategyDto
        {
            public string StrategyCode { get; set; }
            public string Symbol { get; set; }
            public int TimeframeMinutes { get; set; }

            /// <summary>Cosa la strategia dichiara di voler tenere; il piano puo' troncarla.</summary>
            public StrategyHoldingDto Holding { get; set; }
        }

        /// <summary>Overnight e overweek dichiarati dal motore o dalla strategia.</summary>
        private sealed class StrategyHoldingDto
        {
            public bool Overnight { get; set; }
            public bool Overweek { get; set; }
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

            /// <summary>
            /// Istante della barra che ha prodotto il segnale (<c>TradeSignal.Date</c> lato server).
            /// E' l'UNICO campo con cui si misura il ritardo vero della catena
            /// valutazione → claim → ordine: <see cref="ValidFromUtc"/> non serve, perche' e' il
            /// bordo della barra successiva e quindi un istante FUTURO per costruzione. Confonderli
            /// e' ciò che produceva le "eta" a 3900s e -188100s nei log fino alla 2.2.0.
            /// </summary>
            public DateTime? CreatedAtUtc { get; set; }

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

            /// <summary>
            /// Fattore con cui il server ha convertito le distanze di prezzo nei punti di QUESTO
            /// strumento. StopLoss/TakeProfit/BreakEven/TrailingStop arrivano gia' scalati: questo
            /// numero serve solo a ricostruire la distanza Piootoo in diagnostica. Vale 1 quando il
            /// broker quota lo strumento nella stessa unita' delle strategie, cioe' quasi sempre.
            /// </summary>
            public decimal PriceScale { get; set; } = 1m;

            // Rischio come la ricerca l'ha dichiarato: denaro per contratto future di riferimento.
            // Non e' da eseguire — i punti qui sopra lo sono — ma e' l'unico modo che il bot ha di
            // dire se il rischio che sta per mettere a mercato e' quello dichiarato. La divisione
            // denaro -> punti e' avvenuta UNA volta sola sul server, con ReferenceDollarsPerPoint;
            // rifarla qui col valore punto dello strumento del broker e' l'errore che questi campi
            // rendono visibile.
            public decimal? StopLossMoneyPerFutureContract { get; set; }
            public decimal? TakeProfitMoneyPerFutureContract { get; set; }
            public decimal? TrailingStopMoneyPerFutureContract { get; set; }
            public decimal? BreakEvenMoneyPerFutureContract { get; set; }

            /// <summary>Valore in denaro di un punto del contratto future di RIFERIMENTO, non di quello del broker.</summary>
            public decimal ReferenceDollarsPerPoint { get; set; } = 1m;

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

            /// <summary>Interessi di finanziamento, con segno: negativo e' un costo, positivo un accredito.</summary>
            public decimal Swap { get; set; }

            /// <summary>
            /// Utile lordo e netto come li conta il broker, nella VALUTA DEL CONTO. Il server li
            /// ricavava dai prezzi (punti x valore punto), che e' la valuta dello STRUMENTO: su un
            /// conto in EUR con XAUUSD il lordo usciva gonfiato del cambio EURUSD (~7% mediano su un
            /// run 2022-2023, con segno variabile mese per mese) mentre commissione e swap erano gia'
            /// in EUR — cioe' un P&amp;L con due valute dentro. Null quando lo storico non e'
            /// disponibile: in quel caso il server ricalcola come prima.
            /// </summary>
            public decimal? GrossProfit { get; set; }

            public decimal? NetProfit { get; set; }

            public DateTime EventTimeUtc { get; set; }

            /// <summary>Spread dello strumento nell'istante del fill, in unita' di prezzo.</summary>
            public decimal? SpreadAtFill { get; set; }
        }

        private sealed class ExecutionReportRequestDto
        {
            public string SessionToken { get; set; }
            public ExternalExecutionReportDto Report { get; set; }
        }
    }
}
