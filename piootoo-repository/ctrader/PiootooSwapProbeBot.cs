using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using cAlgo.API;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot SONDA DELLO SWAP. Non e' un bot di trading e non misura niente sul mercato: interroga il
    /// <b>backtester di cTrader</b> per sapere quanto costa davvero tenere una posizione una notte,
    /// notte per notte, su un simbolo e per i due lati.
    ///
    /// <para><b>Perche' esiste.</b> Lo swap e' il costo piu' grande che il motore Piootoo oggi non
    /// conta. Sulla storia di un conto FTMO (1.111 trade, 2023-2026) vale <b>1,36 volte le
    /// commissioni</b>: 48.598 di swap contro 35.617 di commissioni, e su
    /// <c>PT2_NQ_PCH_002_30</c> si mangia il 36% del lordo. Un backtest multiday che lo ignora
    /// sovrastima il risultato, e non di poco.</para>
    ///
    /// <para><b>Perche' non bastano le tariffe dichiarate.</b> <c>SwapLong</c> e <c>SwapShort</c> sono
    /// proprieta' del simbolo e si leggono in un istante, ma sono la tariffa <b>di oggi</b>: cTrader
    /// non espone lo storico, e i backtest si fanno sul passato. Questa sonda gira il passato e
    /// osserva l'addebito vero. Su USTEC la regola si vede a occhio (116,37 a notte, rollover verso
    /// le 21:00-22:00 UTC, triplo il venerdi', spiega il 98% dei trade); su DE40 no — sette notti
    /// aperte e da zero a sei addebiti — ed e' esattamente il caso per cui una sonda serve piu' di un
    /// modello.</para>
    ///
    /// <para><b>Come lo fa: una coppia coperta.</b> Su ogni simbolo apre <b>due</b> posizioni di pari
    /// volume, una long e una short, e le tiene aperte per tutto il periodo. L'esposizione si
    /// annulla, quindi l'equity non si muove e nessuna delle due muore per margine attraversando
    /// quattro anni di indice; ma lo swap le due posizioni lo pagano ciascuna per conto proprio, ed e'
    /// cosi' che si misura anche il lato <b>short</b>, che nella storia di un conto long-only non
    /// esiste. Poi, a ogni battito, legge <c>Position.Swap</c>: quando cambia, quel delta e' un
    /// addebito, e l'istante in cui si vede e' il rollover.</para>
    ///
    /// <para><b>Cosa esce.</b> Un CSV per run, <c>{BROKER}_swap_{da}-{a}.csv</c>, una riga per
    /// addebito: istante, simbolo, lato, delta, cumulato, volume e prezzo. E' il dato grezzo da cui
    /// si ricavano la tariffa per notte, l'ora del rollover e il giorno del triplo. A fine run il
    /// riepilogo a log dice gia' se la tariffa e' <b>costante negli anni</b> — cioe' se cTrader
    /// applica a tutto lo storico la tariffa di oggi — oppure se varia, nel qual caso la storia vera
    /// ce l'hai.</para>
    ///
    /// <para><b>Si lancia nel BACKTESTER</b>, sul periodo che interessa, in modalita' barre da un
    /// minuto: la risoluzione con cui si vede il rollover e' quella del battito, e un minuto basta.
    /// Su conto reale aprirebbe due posizioni vere e le terrebbe aperte: e' rifiutato, a meno di
    /// accenderlo a mano dal parametro apposta.</para>
    ///
    /// <para><b>Cosa NON fa.</b> Non parla col server — questa e' la fase di calibrazione, e il
    /// formato dell'archivio si decide <i>dopo</i> aver visto cosa esce. Non legge piani, non apre
    /// sessioni, non tocca il datafeed.</para>
    /// </summary>
    public enum LivelloLogSwap
    {
        /// <summary>Solo avvio, riepilogo finale ed errori.</summary>
        Minimo,

        /// <summary>Una riga per addebito osservato. E' il livello di esercizio.</summary>
        Operativo,

        /// <summary>Anche l'apertura delle posizioni e i battiti a vuoto.</summary>
        Diagnostico
    }

    // `partial` per la stessa ragione degli altri bot del repository: cTrader genera una propria
    // dichiarazione della classe e senza questo la build si ferma con CS0260.
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public partial class PiootooSwapProbeBot : Robot
    {
        // Versione propria, come per la sonda dello spread: questo bot non tocca il contratto di
        // esecuzione, e legarlo a PiootooVersion farebbe comparire un finto disallineamento a ogni
        // release del server.
        // 1.1.0 (23/09/2026): nome, versione e broker sul grafico, come tutta la suite.
        private const string BotVersion = "1.1.0";

        /// <summary>
        /// Sotto questa soglia una variazione di <c>Position.Swap</c> non e' un addebito ma il
        /// rumore della virgola mobile. Gli addebti veri sono ordini di grandezza sopra: il piu'
        /// piccolo osservato finora e' 120,80.
        /// </summary>
        private const double SwapEpsilon = 1e-6;

        [Parameter("Simboli (separati da virgola, vuoto = simbolo del grafico)", DefaultValue = "", Group = "Cosa misurare")]
        public string SymbolList { get; set; }

        /// <summary>
        /// Volume di ciascuna delle due gambe. Lo swap scala col volume, quindi il numero da portare
        /// via non e' l'addebito ma l'addebito <b>per lotto</b>: il CSV porta entrambi.
        /// </summary>
        [Parameter("Lotti per gamba", DefaultValue = 1.0, MinValue = 0.01, Group = "Cosa misurare")]
        public double Lots { get; set; }

        /// <summary>
        /// Codice del broker: entra nel nome del file, perche' due broker sullo stesso simbolo non
        /// hanno lo stesso swap — e' il confronto per cui la sonda esiste. Vuoto = dedotto da
        /// <c>Account.BrokerName</c>, con la stessa ripulitura del raccoglitore e della sonda dello
        /// spread, cosi' i file dei tre si affiancano a occhio.
        /// </summary>
        [Parameter("Codice broker (vuoto = dedotto dal conto)", DefaultValue = "", Group = "Cosa misurare")]
        public string BrokerCode { get; set; }

        /// <summary>
        /// Ogni quanto si rilegge lo swap delle posizioni. E' anche la <b>risoluzione con cui si vede
        /// il rollover</b>: con un minuto l'ora dell'addebito si conosce al minuto, che e' quanto
        /// serve per distinguere le 21:00 dalle 22:00 — i due candidati che sulla storia di FTMO
        /// spiegano i dati ugualmente bene, perche' in mezzo il mercato e' chiuso.
        /// </summary>
        [Parameter("Secondi fra due letture", DefaultValue = 60, MinValue = 1, MaxValue = 3600, Group = "Ritmo")]
        public int SecondsBetweenSamples { get; set; }

        [Parameter("Cartella di output", DefaultValue = "", Group = "Output")]
        public string OutputFolder { get; set; }

        /// <summary>
        /// Su conto reale questo bot aprirebbe due posizioni vere e le terrebbe aperte finche' non lo
        /// si ferma: paga spread e commissioni di entrambe le gambe per misurare qualcosa che nel
        /// backtester si misura gratis. Rifiutato di default, e l'interruttore sta qui e non in un
        /// commento perche' un giorno potrebbe servire davvero — per confrontare lo swap che cTrader
        /// simula con quello che il broker addebita.
        /// </summary>
        [Parameter("Consenti su conto NON in backtest", DefaultValue = false, Group = "Sicurezza")]
        public bool AllowOutsideBacktest { get; set; }

        [Parameter("Livello di log", DefaultValue = LivelloLogSwap.Operativo, Group = "Diagnostica")]
        public LivelloLogSwap LivelloDiLog { get; set; }

        private readonly List<SwapProbe> _probes = new List<SwapProbe>();
        private readonly List<SwapCharge> _charges = new List<SwapCharge>();

        private string _brokerCode;
        private string _outputFolder;
        private DateTime _startedAtUtc;
        private DateTime? _firstSampleUtc;
        private DateTime? _lastSampleUtc;
        private bool _stopped;

        private bool LogOperativo { get { return LivelloDiLog >= LivelloLogSwap.Operativo; } }
        private bool LogDiagnostico { get { return LivelloDiLog >= LivelloLogSwap.Diagnostico; } }

        // -----------------------------------------------------------------------------------------
        // Avvio
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Nome, versione, broker e conto in alto a destra sul grafico: la prima cosa da vedere per
        /// sapere quale bot gira e su quale broker, senza aprire il log. Il broker e' quello che
        /// dichiara cTrader (<c>Account.BrokerName</c>) ripulito come in tutta la suite: "FTMO
        /// Platform" -> <c>FTMOPLATFORM</c>. E' un'informazione, non una chiave: la cartella dei dati
        /// la decide il registro dei broker del server.
        /// </summary>
        private void DrawIdentity(string title)
        {
            try
            {
                Chart.DrawStaticText("PiootooIdentity",
                    string.Format("{0} v{1}\nBroker: {2}\nConto:  {3}",
                        title, BotVersion, PlatformBrokerCode(Account.BrokerName), Account.Number),
                    VerticalAlignment.Top, HorizontalAlignment.Right, Color.LightGray);
            }
            catch (System.Exception)
            {
                // Senza grafico (ottimizzazione) non c'e' dove scrivere: non e' un motivo per fermarsi.
            }
        }

        private static string PlatformBrokerCode(string brokerName)
        {
            if (string.IsNullOrWhiteSpace(brokerName))
                return "-";

            var builder = new System.Text.StringBuilder(brokerName.Length);
            foreach (var character in brokerName.Trim().ToUpperInvariant())
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                    builder.Append(character);
            return builder.Length == 0 ? "-" : builder.ToString();
        }

        protected override void OnStart()
        {
            DrawIdentity("Piootoo Swap Probe");

            Print("Piootoo Swap Probe v{0} — quanto costa tenere una notte, notte per notte, sui due lati.", BotVersion);

            // Stesso controllo della sonda dello spread: i tempi si scrivono etichettati UTC, e
            // prima di etichettarli bisogna essere certi che l'etichetta sia vera. Le due proprieta'
            // sono letture indipendenti dell'orologio e fra l'una e l'altra il tempo avanza, quindi
            // si confronta con una tolleranza: il fuso piu' vicino a UTC dista quindici minuti.
            var serverTime = Server.Time;
            var serverTimeUtc = Server.TimeInUtc;
            if ((serverTime - serverTimeUtc).Duration() > TimeSpan.FromMinutes(1))
            {
                StopWithError(string.Format(
                    "Il robot non sta girando in UTC (Server.Time={0:O}, Server.TimeInUtc={1:O}). " +
                    "L'attributo [Robot(TimeZone = TimeZones.UTC)] e' obbligatorio: gli istanti degli " +
                    "addebiti verrebbero scritti con un orario falso, e l'ora del rollover e' " +
                    "proprio cio' che questa sonda deve misurare.",
                    serverTime, serverTimeUtc));
                return;
            }

            if (!IsBacktesting && !AllowOutsideBacktest)
            {
                StopWithError(
                    "Questa sonda va lanciata nel BACKTESTER: fuori aprirebbe due posizioni vere e le " +
                    "terrebbe aperte, pagando spread e commissioni per misurare cio' che il backtester " +
                    "misura gratis. Se e' voluto, accendi 'Consenti su conto NON in backtest'.");
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

            try
            {
                _outputFolder = string.IsNullOrWhiteSpace(OutputFolder)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooSwapProbe")
                    : OutputFolder.Trim();
                Directory.CreateDirectory(_outputFolder);
            }
            catch (Exception failure)
            {
                StopWithError(string.Format("Cartella di output '{0}' non utilizzabile: {1}", OutputFolder, failure.Message));
                return;
            }

            if (!TryOpenProbes())
                return;

            _startedAtUtc = Server.TimeInUtc;
            Print("Broker {0} (conto {1} presso '{2}'). {3} simboli, {4} lotti per gamba, lettura ogni {5}s. Output in {6}",
                _brokerCode, Account.Number, Account.BrokerName, _probes.Count, Lots,
                SecondsBetweenSamples, _outputFolder);

            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, SecondsBetweenSamples)));
        }

        /// <summary>
        /// Stessa ripulitura del raccoglitore e della sonda dello spread — solo lettere, cifre,
        /// <c>-</c> e <c>_</c> in maiuscolo — perche' i file dei tre bot si confrontano a occhio:
        /// "IC Markets" deve diventare <c>ICMARKETS</c> in tutti e tre.
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
        /// Apre la coppia coperta su ogni simbolo. Un simbolo che il broker non conosce, o su cui
        /// l'ordine non passa, non ferma il run: si segnala e si prosegue, perche' in un elenco uno
        /// sbagliato non deve costare la misura degli altri.
        /// </summary>
        private bool TryOpenProbes()
        {
            foreach (var name in ResolveSymbolNames())
            {
                Symbol symbol = null;
                try
                {
                    symbol = Symbols.GetSymbol(name);
                }
                catch (Exception failure)
                {
                    Print("Simbolo '{0}' non disponibile su questo account: {1}. Saltato.", name, failure.Message);
                    continue;
                }

                if (symbol == null)
                {
                    Print("Simbolo '{0}' non disponibile su questo account. Saltato.", name);
                    continue;
                }

                // Il volume si normalizza sul passo dello strumento: un volume non valido fa fallire
                // l'ordine, e qui fallirebbero le DUE gambe, cioe' il simbolo intero.
                var volume = symbol.NormalizeVolumeInUnits(symbol.QuantityToVolumeInUnits(Lots), RoundingMode.ToNearest);
                if (volume <= 0)
                {
                    Print("{0}: {1} lotti non sono un volume valido su questo strumento. Saltato.", symbol.Name, Lots);
                    continue;
                }

                // Le tariffe DICHIARATE dal simbolo, lette per reflection come fa
                // PiootooSymbolInfoDumpBot: non si sa a priori come cAlgo le chiami su ogni versione,
                // e un nome sbagliato scritto a mano fermerebbe la build dentro cTrader — dove il
                // sorgente non si puo' compilare prima. Servono al confronto: se l'addebito osservato
                // coincide con la tariffa di oggi su tutto lo storico, cTrader sta applicando al
                // passato il listino di adesso, e la sonda lo dice invece di lasciarlo credere.
                var declared = DescribeDeclaredSwap(symbol);

                var buy = OpenLeg(symbol, TradeType.Buy, volume);
                var sell = OpenLeg(symbol, TradeType.Sell, volume);

                if (buy == null && sell == null)
                {
                    Print("{0}: nessuna delle due gambe e' stata aperta. Saltato.", symbol.Name);
                    continue;
                }

                if (buy == null || sell == null)
                {
                    // Una gamba sola misura il suo lato ma lascia l'esposizione scoperta: su anni di
                    // indice quella posizione puo' morire per margine e portarsi via la misura. Lo si
                    // dice, e si prosegue: mezza misura dichiarata vale piu' di nessuna misura.
                    Print("{0}: ATTENZIONE, aperta una sola gamba ({1}). L'esposizione NON e' coperta: " +
                          "se il margine non regge, la misura si interrompe.",
                        symbol.Name, buy == null ? "short" : "long");
                }

                _probes.Add(new SwapProbe
                {
                    SymbolName = symbol.Name,
                    PiootooSymbol = NormalizePiootooSymbol(symbol.Name),
                    Digits = symbol.Digits,
                    LotSize = symbol.LotSize,
                    VolumeInUnits = volume,
                    Lots = Lots,
                    DeclaredSwap = declared,
                    Symbol = symbol,
                    LongLabel = buy,
                    ShortLabel = sell
                });

                Print("{0}: coppia aperta, {1} lotti per gamba ({2} unita'). Tariffe dichiarate oggi: {3}",
                    symbol.Name, Lots, volume, declared);
            }

            if (_probes.Count == 0)
            {
                StopWithError("Nessun simbolo misurabile: controllare l'elenco e il volume.");
                return false;
            }

            return true;
        }

        private List<string> ResolveSymbolNames()
        {
            var names = new List<string>();

            foreach (var piece in (SymbolList ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var entry = piece.Trim();
                if (entry.Length == 0)
                    continue;

                if (!names.Any(existing => string.Equals(existing, entry, StringComparison.OrdinalIgnoreCase)))
                    names.Add(entry);
            }

            if (names.Count == 0)
                names.Add(SymbolName);

            return names;
        }

        /// <summary>
        /// Apre una gamba e restituisce l'etichetta con cui ritrovarla. L'etichetta, e non l'oggetto
        /// <c>Position</c>: e' la chiave stabile su cui si rilegge la posizione a ogni battito, e
        /// sopravvive a qualunque cosa la piattaforma faccia con i riferimenti.
        /// </summary>
        private string OpenLeg(Symbol symbol, TradeType side, double volume)
        {
            var label = string.Format(CultureInfo.InvariantCulture, "PiootooSwapProbe:{0}:{1}", symbol.Name, side);

            TradeResult result;
            try
            {
                result = ExecuteMarketOrder(side, symbol.Name, volume, label, null, null, "swap probe");
            }
            catch (Exception failure)
            {
                Print("{0}: gamba {1} non aperta ({2}).", symbol.Name, side, failure.Message);
                return null;
            }

            if (result == null || !result.IsSuccessful)
            {
                Print("{0}: gamba {1} non aperta ({2}).", symbol.Name, side,
                    result == null || result.Error == null ? "errore sconosciuto" : result.Error.ToString());
                return null;
            }

            if (LogDiagnostico)
                Print("{0}: gamba {1} aperta a {2}.", symbol.Name, side, result.Position == null ? 0 : result.Position.EntryPrice);

            return label;
        }

        /// <summary>
        /// Le proprieta' di swap che il simbolo dichiara, lette per reflection: qualunque cosa cAlgo
        /// esponga e si chiami "Swap" finisce nella riga, e se non espone niente la riga lo dice.
        /// Sta scritta in testa al CSV, dove serve: senza, un addebito osservato non si puo'
        /// confrontare con niente.
        /// </summary>
        private static string DescribeDeclaredSwap(Symbol symbol)
        {
            var parts = new List<string>();

            foreach (var property in symbol.GetType()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(candidate => candidate.CanRead && candidate.GetIndexParameters().Length == 0)
                         .Where(candidate => candidate.Name.IndexOf("Swap", StringComparison.OrdinalIgnoreCase) >= 0)
                         .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
            {
                object value;
                try
                {
                    value = property.GetValue(symbol);
                }
                catch (Exception failure)
                {
                    value = "<errore: " + failure.Message + ">";
                }

                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1}", property.Name, value));
            }

            return parts.Count == 0 ? "nessuna proprieta' di swap esposta dall'API" : string.Join(" ", parts);
        }

        private static string NormalizePiootooSymbol(string symbol)
        {
            return "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();
        }

        // -----------------------------------------------------------------------------------------
        // Misura: a ogni battito si rilegge lo swap, e ogni variazione e' un addebito
        // -----------------------------------------------------------------------------------------

        protected override void OnTimer()
        {
            Sample();
        }

        /// <summary>
        /// Anche a ogni tick, non solo a timer. Nel backtester il timer e' legato all'orologio
        /// simulato e con certe modalita' di dati batte rado: il tick e' la garanzia che fra
        /// l'addebito e la sua osservazione non passi un giorno. Campionare due volte non costa
        /// niente — la seconda lettura trova lo swap immutato e non scrive.
        /// </summary>
        protected override void OnTick()
        {
            Sample();
        }

        private void Sample()
        {
            if (_stopped)
                return;

            var now = Server.TimeInUtc;
            if (_firstSampleUtc == null)
                _firstSampleUtc = now;
            _lastSampleUtc = now;

            foreach (var probe in _probes)
            {
                SampleLeg(probe, now, TradeType.Buy, probe.LongLabel);
                SampleLeg(probe, now, TradeType.Sell, probe.ShortLabel);
            }
        }

        private void SampleLeg(SwapProbe probe, DateTime now, TradeType side, string label)
        {
            if (label == null)
                return;

            var position = FindPosition(label);
            if (position == null)
            {
                // La gamba non c'e' piu': chiusa dalla piattaforma (stop out, fine dati del simbolo).
                // Si dice una volta sola e si smette di cercarla, altrimenti il log diventa questo.
                if (side == TradeType.Buy)
                {
                    if (probe.LongLabel != null)
                        Print("{0}: la gamba long non e' piu' aperta a {1:yyyy-MM-dd HH:mm} — misura interrotta su quel lato.", probe.SymbolName, now);
                    probe.LongLabel = null;
                }
                else
                {
                    if (probe.ShortLabel != null)
                        Print("{0}: la gamba short non e' piu' aperta a {1:yyyy-MM-dd HH:mm} — misura interrotta su quel lato.", probe.SymbolName, now);
                    probe.ShortLabel = null;
                }

                return;
            }

            var current = position.Swap;
            var previous = side == TradeType.Buy ? probe.LastLongSwap : probe.LastShortSwap;
            var delta = current - previous;

            if (Math.Abs(delta) < SwapEpsilon)
                return;

            if (side == TradeType.Buy)
                probe.LastLongSwap = current;
            else
                probe.LastShortSwap = current;

            var charge = new SwapCharge
            {
                DetectedUtc = now,
                Probe = probe,
                Side = side,
                Delta = delta,
                Cumulative = current,
                Price = probe.Symbol == null ? 0d : probe.Symbol.Bid
            };

            _charges.Add(charge);
            WriteCharge(charge);

            if (LogOperativo)
            {
                Print("{0} {1}: addebito {2} a {3:ddd yyyy-MM-dd HH:mm} UTC (cumulato {4}, {5} lotti).",
                    probe.SymbolName, side,
                    delta.ToString("F2", CultureInfo.InvariantCulture),
                    now, current.ToString("F2", CultureInfo.InvariantCulture), probe.Lots);
            }
        }

        private Position FindPosition(string label)
        {
            var positions = Positions;
            if (positions == null)
                return null;

            foreach (var position in positions)
            {
                if (position != null && string.Equals(position.Label, label, StringComparison.Ordinal))
                    return position;
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // Scrittura
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Il CSV si apre alla prima riga e resta aperto: un backtest di quattro anni produce
        /// qualche centinaio di addebiti, non milioni, quindi si scrive e si scarica subito — se il
        /// backtest si ferma a meta' il file contiene comunque tutto quello che si era misurato.
        /// </summary>
        private void WriteCharge(SwapCharge charge)
        {
            if (_writer == null && !OpenWriter())
                return;

            if (_writer == null)
                return;

            var probe = charge.Probe;
            _writer.Write(charge.DetectedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(charge.DetectedUtc.DayOfWeek.ToString());
            _writer.Write(',');
            _writer.Write(charge.DetectedUtc.Hour.ToString(CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(probe.PiootooSymbol);
            _writer.Write(',');
            _writer.Write(probe.SymbolName);
            _writer.Write(',');
            _writer.Write(charge.Side == TradeType.Buy ? "long" : "short");
            _writer.Write(',');
            _writer.Write(charge.Delta.ToString("F4", CultureInfo.InvariantCulture));
            _writer.Write(',');
            // Lo swap scala col volume: il numero riutilizzabile e' quello per lotto, e averlo gia'
            // diviso evita che chi legge debba ricordarsi con quanti lotti girava la sonda.
            _writer.Write((probe.Lots > 0 ? charge.Delta / probe.Lots : 0d).ToString("F4", CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(charge.Cumulative.ToString("F4", CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(probe.Lots.ToString(CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(probe.VolumeInUnits.ToString(CultureInfo.InvariantCulture));
            _writer.Write(',');
            _writer.Write(charge.Price.ToString(probe.PriceFormat, CultureInfo.InvariantCulture));
            _writer.Write('\n');
            _writer.Flush();
        }

        private StreamWriter _writer;
        private string _path;

        private bool OpenWriter()
        {
            var from = _firstSampleUtc ?? Server.TimeInUtc;
            var to = _lastSampleUtc ?? from;

            // La finestra nel nome e' quella OSSERVATA, non una chiesta da un parametro: il periodo
            // qui lo decide il backtester, e il file deve dire su cosa e' stato misurato. Si apre
            // alla prima riga, quindi la fine e' ancora provvisoria: il nome si chiude a fine run.
            _path = Path.Combine(_outputFolder, string.Format(CultureInfo.InvariantCulture,
                "{0}_swap_{1:yyyyMMdd}-{2:yyyyMMdd}.csv", _brokerCode, from, to));

            try
            {
                _writer = new StreamWriter(_path, false, new UTF8Encoding(false));
            }
            catch (Exception failure)
            {
                Print("CSV '{0}' non scrivibile: {1}. La misura prosegue, il riepilogo finale resta a log.",
                    _path, failure.Message);
                _path = null;
                return false;
            }

            _writer.Write(string.Format(CultureInfo.InvariantCulture,
                "# Piootoo Swap Probe v{0} — broker {1} (conto {2}), {3}\n" +
                "# Una riga per ADDEBITO osservato su una posizione tenuta aperta: delta di Position.Swap.\n" +
                "# detectedUtc e' l'istante in cui l'addebito si vede, cioe' il rollover alla risoluzione del battito ({4}s).\n" +
                "# swapPerLot = swap / lots: e' il numero riutilizzabile, lo swap scala col volume.\n",
                BotVersion, _brokerCode, Account.Number,
                IsBacktesting ? "BACKTEST" : "conto NON in backtest",
                SecondsBetweenSamples));

            foreach (var probe in _probes)
            {
                _writer.Write(string.Format(CultureInfo.InvariantCulture,
                    "# {0} ({1}): {2} lotti = {3} unita', lotSize {4}. Tariffe dichiarate: {5}\n",
                    probe.PiootooSymbol, probe.SymbolName, probe.Lots, probe.VolumeInUnits,
                    probe.LotSize, probe.DeclaredSwap));
            }

            _writer.Write("detectedUtc,weekday,hourUtc,symbol,brokerSymbol,side,swap,swapPerLot,cumulative,lots,volumeInUnits,price\n");
            return true;
        }

        // -----------------------------------------------------------------------------------------
        // Chiusura
        // -----------------------------------------------------------------------------------------

        protected override void OnStop()
        {
            _stopped = true;

            try
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null;
                }
            }
            catch (Exception failure)
            {
                Print("Chiusura del CSV fallita: {0}", failure.Message);
            }

            CloseProbes();
            Report();

            if (_path != null)
                Print("Addebiti scritti in {0}", _path);
        }

        /// <summary>
        /// Chiude le gambe. In backtest e' una formalita' — la simulazione finisce comunque — ma su un
        /// conto acceso a mano lasciare aperte due posizioni coperte sarebbe il modo piu' silenzioso
        /// di pagare commissioni per sempre.
        /// </summary>
        private void CloseProbes()
        {
            foreach (var probe in _probes)
            {
                foreach (var label in new[] { probe.LongLabel, probe.ShortLabel })
                {
                    if (label == null)
                        continue;

                    var position = FindPosition(label);
                    if (position == null)
                        continue;

                    try
                    {
                        ClosePosition(position);
                    }
                    catch (Exception failure)
                    {
                        Print("{0}: chiusura di '{1}' fallita: {2}", probe.SymbolName, label, failure.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Il riepilogo risponde alla domanda per cui la sonda e' stata scritta: <b>la tariffa e'
        /// costante nel tempo?</b> Se gli addebiti distinti sono uno solo su quattro anni, cTrader
        /// applica al passato il listino di oggi e lo storico non ce l'ha nemmeno lui; se sono molti e
        /// cambiano per anno, lo storico e' vero e vale la pena archiviarlo. In entrambi i casi
        /// restano la <b>regola</b> — quali notti pagano, a che ora, quale giorno e' triplo — che e' il
        /// pezzo che oggi manca del tutto.
        /// </summary>
        private void Report()
        {
            var elapsed = Server.TimeInUtc - _startedAtUtc;
            Print("--- Riepilogo sonda swap ({0:hh\\:mm\\:ss} di orologio simulato) ---", elapsed);

            if (_charges.Count == 0)
            {
                Print("NESSUN addebito osservato. O il periodo non contiene rollover, o su questi " +
                      "strumenti il broker non addebita swap: sono due cose diverse e vanno distinte " +
                      "guardando la lunghezza del periodo di backtest.");
                return;
            }

            Print("Finestra osservata: {0:yyyy-MM-dd HH:mm} -> {1:yyyy-MM-dd HH:mm} UTC.",
                _firstSampleUtc, _lastSampleUtc);

            foreach (var probe in _probes)
            {
                foreach (var side in new[] { TradeType.Buy, TradeType.Sell })
                {
                    var rows = _charges
                        .Where(charge => charge.Probe == probe && charge.Side == side)
                        .ToList();

                    if (rows.Count == 0)
                    {
                        Print("{0} {1}: nessun addebito.", probe.PiootooSymbol, side);
                        continue;
                    }

                    var perLot = rows.Select(charge => probe.Lots > 0 ? charge.Delta / probe.Lots : 0d).ToList();
                    var distinct = perLot
                        .Select(value => Math.Round(value, 4))
                        .Distinct()
                        .OrderBy(value => value)
                        .ToList();

                    Print("{0} {1}: {2} addebiti, totale per lotto {3}, medio {4}.",
                        probe.PiootooSymbol, side, rows.Count,
                        perLot.Sum().ToString("F2", CultureInfo.InvariantCulture),
                        (perLot.Sum() / perLot.Count).ToString("F2", CultureInfo.InvariantCulture));

                    Print("   importi distinti per lotto ({0}): {1}", distinct.Count,
                        string.Join(" ", distinct.Take(12).Select(value => value.ToString("F2", CultureInfo.InvariantCulture))) +
                        (distinct.Count > 12 ? " ..." : string.Empty));

                    Print("   {0}", DescribeByYear(rows, probe));
                    Print("   ore UTC: {0}", DescribeByHour(rows));
                    Print("   giorni: {0}", DescribeByWeekday(rows));
                }
            }
        }

        private static string DescribeByYear(List<SwapCharge> rows, SwapProbe probe)
        {
            var parts = rows
                .GroupBy(charge => charge.DetectedUtc.Year)
                .OrderBy(group => group.Key)
                .Select(group => string.Format(CultureInfo.InvariantCulture, "{0}: {1} addebiti, medio {2}",
                    group.Key, group.Count(),
                    (group.Sum(charge => probe.Lots > 0 ? charge.Delta / probe.Lots : 0d) / group.Count())
                        .ToString("F2", CultureInfo.InvariantCulture)));

            return "per anno — " + string.Join(" | ", parts);
        }

        private static string DescribeByHour(List<SwapCharge> rows)
        {
            var parts = rows
                .GroupBy(charge => charge.DetectedUtc.Hour)
                .OrderByDescending(group => group.Count())
                .Select(group => string.Format(CultureInfo.InvariantCulture, "{0:00}:00 x{1}", group.Key, group.Count()));

            return string.Join("  ", parts);
        }

        private static string DescribeByWeekday(List<SwapCharge> rows)
        {
            var parts = rows
                .GroupBy(charge => charge.DetectedUtc.DayOfWeek)
                .OrderBy(group => group.Key)
                .Select(group => string.Format(CultureInfo.InvariantCulture, "{0} x{1}", group.Key, group.Count()));

            return string.Join("  ", parts);
        }

        private void StopWithError(string message)
        {
            Print("ERRORE FATALE: {0}", message);
            _stopped = true;
            Stop();
        }

        // -----------------------------------------------------------------------------------------
        // Stato
        // -----------------------------------------------------------------------------------------

        private sealed class SwapProbe
        {
            public string SymbolName;
            public string PiootooSymbol;
            public int Digits;
            public double LotSize;
            public double VolumeInUnits;
            public double Lots;
            public string DeclaredSwap;
            public Symbol Symbol;

            /// <summary>Etichetta della gamba long, <c>null</c> quando non e' (piu') aperta.</summary>
            public string LongLabel;

            /// <summary>Etichetta della gamba short, <c>null</c> quando non e' (piu') aperta.</summary>
            public string ShortLabel;

            /// <summary>Ultimo swap cumulato letto: la differenza col prossimo e' l'addebito.</summary>
            public double LastLongSwap;

            public double LastShortSwap;

            public string PriceFormat { get { return "F" + Digits.ToString(CultureInfo.InvariantCulture); } }
        }

        private sealed class SwapCharge
        {
            public DateTime DetectedUtc;
            public SwapProbe Probe;
            public TradeType Side;
            public double Delta;
            public double Cumulative;
            public double Price;
        }
    }
}
