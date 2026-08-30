using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using cAlgo.API;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot SCARICATORE DI TICK. Non parla con il server Piootoo, non salva file, non apre
    /// posizioni: l'unica cosa che fa e' chiedere a cTrader la storia dei tick dei simboli che gli
    /// si elencano, <b>un pezzo alla volta</b>, finche' non copre la finestra di date richiesta.
    ///
    /// <para><b>A cosa serve.</b> cTrader consegna i tick a blocchi e li tiene in una propria cache
    /// locale. Chiederne un anno in una volta — da un backtest, da un altro bot, dalla piattaforma —
    /// significa una singola richiesta lunghissima che va in timeout, e che morendo non lascia
    /// niente. Qui la stessa storia viene tirata giu' a passi corti, con il thread
    /// dell'algoritmo restituito alla piattaforma fra un passo e l'altro: piu' lento in assoluto,
    /// ma <i>arriva in fondo</i>. Quando ha finito, i tick sono nella cache di cTrader e chi li
    /// chiede dopo li trova gia' li'.</para>
    ///
    /// <para><b>Cosa NON fa.</b> Non spedisce niente a nessuno e non scrive alcun feed: la cache e'
    /// di cTrader, non di Piootoo. Per portare i tick nel repository serve
    /// <c>PiootooDatafeedSyncBot</c> con <c>Sincronizza i tick</c> acceso — e conviene farlo dopo,
    /// proprio perche' a quel punto la storia e' gia' scaricata.</para>
    ///
    /// <para><b>Attenzione alla RAM.</b> La serie tick resta in memoria per intero mentre la si
    /// carica, e i tick di un simbolo liquido sono milioni al mese. La finestra di date va tenuta
    /// stretta e allargata a tappe: il bot stampa quanti tick ha in pancia a ogni blocco proprio
    /// per poterlo sorvegliare.</para>
    /// </summary>
    public enum LivelloLogTick
    {
        /// <summary>Solo avvio, riepiloghi ed errori.</summary>
        Minimo,

        /// <summary>Una riga per blocco scaricato. E' il livello di esercizio.</summary>
        Operativo,

        /// <summary>Anche i singoli giri di caricamento.</summary>
        Diagnostico
    }

    // `partial` per la stessa ragione dell'altro raccoglitore: cTrader genera una propria
    // dichiarazione della classe del cBot e senza questo la build si ferma con CS0260.
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public partial class PiootooTickDownloaderBot : Robot
    {
        // Versione propria: questo bot non ha alcun contratto con il server Piootoo — non lo
        // contatta nemmeno — quindi non ha senso legarlo a PiootooVersion.
        private const string BotVersion = "1.0.0";

        [Parameter("Simboli (separati da virgola, vuoto = simbolo del grafico)", DefaultValue = "", Group = "Cosa scaricare")]
        public string SymbolList { get; set; }

        /// <summary>Inizio della finestra (yyyy-MM-dd, UTC, incluso). Vuoto = tutto quello che c'e'.</summary>
        [Parameter("Data inizio (yyyy-MM-dd, vuoto = tutta la storia)", DefaultValue = "", Group = "Finestra di date")]
        public string StartDateText { get; set; }

        /// <summary>Fine della finestra (yyyy-MM-dd, UTC). Vuoto = adesso.</summary>
        [Parameter("Data fine (yyyy-MM-dd, vuoto = adesso)", DefaultValue = "", Group = "Finestra di date")]
        public string EndDateText { get; set; }

        /// <summary>
        /// Ampiezza del passo con cui si cammina all'indietro. Un giorno di default e non cinque
        /// come per le barre: su un simbolo liquido un giorno di tick e' gia' centinaia di migliaia
        /// di righe, e il passo serve a dare alla piattaforma un momento di respiro fra un pezzo e
        /// l'altro — se e' troppo largo, il respiro non arriva mai.
        /// </summary>
        [Parameter("Giorni per blocco", DefaultValue = 1, MinValue = 1, MaxValue = 365, Group = "Finestra di date")]
        public int ChunkDays { get; set; }

        /// <summary>
        /// Quante richieste di storia al massimo in un solo battito di timer. E' il freno che
        /// impedisce al bot di diventare esattamente il problema che risolve: superato il tetto si
        /// molla il thread e si riprende dallo stesso punto al battito dopo.
        /// </summary>
        [Parameter("Giri di caricamento per battito", DefaultValue = 20, MinValue = 1, MaxValue = 500, Group = "Ritmo")]
        public int LoadsPerTick { get; set; }

        [Parameter("Secondi fra due battiti", DefaultValue = 1, MinValue = 1, MaxValue = 60, Group = "Ritmo")]
        public int SecondsBetweenChunks { get; set; }

        /// <summary>
        /// Tetto di sicurezza sui tick tenuti in memoria per simbolo. Raggiunto, quel simbolo si
        /// ferma con un messaggio esplicito invece di far esaurire la RAM alla piattaforma: meglio
        /// una finestra scaricata a meta' e dichiarata, che cTrader che muore portandosi via anche
        /// quello che aveva gia' preso.
        /// </summary>
        [Parameter("Tick massimi in memoria per simbolo (milioni)", DefaultValue = 20, MinValue = 1, MaxValue = 500, Group = "Ritmo")]
        public int MaxMillionTicksPerSymbol { get; set; }

        [Parameter("Livello di log", DefaultValue = LivelloLogTick.Operativo, Group = "Diagnostica")]
        public LivelloLogTick LivelloDiLog { get; set; }

        private readonly List<TickStream> _streams = new List<TickStream>();
        private DateTime _windowStartUtc;
        private DateTime _windowEndUtc;
        private DateTime _startedAtUtc;
        private int _roundRobin;
        private bool _stopped;

        private bool LogOperativo { get { return LivelloDiLog >= LivelloLogTick.Operativo; } }
        private bool LogDiagnostico { get { return LivelloDiLog >= LivelloLogTick.Diagnostico; } }

        // -----------------------------------------------------------------------------------------
        // Avvio
        // -----------------------------------------------------------------------------------------

        protected override void OnStart()
        {
            Print("Piootoo Tick Downloader v{0} — scarica solo nella cache di cTrader, non invia nulla.", BotVersion);

            // Le date della finestra si leggono come UTC e i tempi dei tick arrivano nel fuso
            // dichiarato dall'attributo [Robot]. Se quello non fosse UTC, la finestra verrebbe
            // confrontata con orari locali e si scaricherebbe un periodo spostato di ore senza che
            // niente lo segnali.
            if (Server.Time != Server.TimeInUtc)
            {
                StopWithError(string.Format(
                    "Il robot non sta girando in UTC (Server.Time={0:O}, Server.TimeInUtc={1:O}). " +
                    "L'attributo [Robot(TimeZone = TimeZones.UTC)] e' obbligatorio: la finestra di " +
                    "date verrebbe interpretata in un fuso diverso da quello dei tick.",
                    Server.Time, Server.TimeInUtc));
                return;
            }

            string windowError;
            if (!TryParseWindow(out windowError))
            {
                StopWithError(windowError);
                return;
            }

            var names = new List<string>();
            foreach (var piece in (SymbolList ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = piece.Trim();
                // Si accetta anche la forma "BROKER=@PIOOTOO" del bot raccoglitore, cosi' lo stesso
                // elenco si puo' incollare nei due bot; qui la parte Piootoo non serve e si ignora,
                // perche' non si salva niente da nessuna parte.
                var separator = trimmed.IndexOf('=');
                if (separator > 0)
                    trimmed = trimmed.Substring(0, separator).Trim();

                if (trimmed.Length > 0 && !names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    names.Add(trimmed);
            }

            if (names.Count == 0)
                names.Add(SymbolName);

            foreach (var name in names)
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

                var series = MarketData.GetTicks(symbol.Name);
                if (series == null)
                {
                    Print("Serie tick di '{0}' non disponibile. Saltato.", symbol.Name);
                    continue;
                }

                _streams.Add(new TickStream
                {
                    SymbolName = symbol.Name,
                    Series = series,
                    CursorEndUtc = _windowEndUtc
                });
            }

            if (_streams.Count == 0)
            {
                StopWithError("Nessun simbolo valido: controllare l'elenco.");
                return;
            }

            _startedAtUtc = Server.TimeInUtc;
            Print("Finestra richiesta: {0:yyyy-MM-dd} -> {1:yyyy-MM-dd} su {2} simboli, " +
                  "passi da {3} giorni, max {4} caricamenti per battito.",
                _windowStartUtc, _windowEndUtc, _streams.Count, ChunkDays, LoadsPerTick);

            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, SecondsBetweenChunks)));
        }

        /// <summary>
        /// Giorni di calendario UTC: inizio incluso, fine a mezzanotte del giorno dopo — chi scrive
        /// "2026-08-30" come fine intende avere anche il 30 per intero.
        /// </summary>
        private bool TryParseWindow(out string error)
        {
            error = null;
            _windowEndUtc = Server.TimeInUtc;
            _windowStartUtc = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (!string.IsNullOrWhiteSpace(StartDateText))
            {
                DateTime start;
                if (!TryParseDay(StartDateText, out start))
                {
                    error = string.Format("Data inizio '{0}' non valida: attesa nella forma yyyy-MM-dd.", StartDateText);
                    return false;
                }

                _windowStartUtc = start;
            }

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

            if (_windowEndUtc <= _windowStartUtc)
            {
                error = string.Format(
                    "Finestra vuota: inizio {0:yyyy-MM-dd} non precede la fine {1:yyyy-MM-dd}.",
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

        // -----------------------------------------------------------------------------------------
        // Ciclo: un pezzo per battito
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Ogni battito lavora su UN solo simbolo e per un numero limitato di caricamenti, poi
        /// restituisce il thread alla piattaforma. E' tutta qui la tecnica: la storia arriva
        /// comunque, ma nessuna singola richiesta e' abbastanza lunga da andare in timeout.
        /// </summary>
        protected override void OnTimer()
        {
            if (_stopped)
                return;

            var stream = NextStreamNeedingWork();
            if (stream == null)
            {
                Report();
                Print("Scaricamento completato: i tick sono nella cache di cTrader. Il bot si ferma.");
                _stopped = true;
                Stop();
                return;
            }

            Advance(stream);
        }

        private TickStream NextStreamNeedingWork()
        {
            for (var i = 0; i < _streams.Count; i++)
            {
                var candidate = _streams[(_roundRobin + i) % _streams.Count];
                if (candidate.Done)
                    continue;

                _roundRobin = (_roundRobin + i + 1) % _streams.Count;
                return candidate;
            }

            return null;
        }

        private void Advance(TickStream stream)
        {
            var chunkStart = stream.CursorEndUtc.AddDays(-ChunkDays);
            if (chunkStart < _windowStartUtc)
                chunkStart = _windowStartUtc;

            var maxTicks = (long)MaxMillionTicksPerSymbol * 1_000_000L;
            var loads = 0;

            while (true)
            {
                if (stream.Series.Count > 0 && Oldest(stream) <= chunkStart)
                {
                    // Pezzo coperto: si sposta il cursore indietro e si molla il thread. Il prossimo
                    // battito riparte da qui.
                    stream.CursorEndUtc = chunkStart;
                    stream.Chunks++;
                    if (LogOperativo)
                        Print("{0}: fino a {1:yyyy-MM-dd HH:mm} — {2} tick in memoria.",
                            stream.SymbolName, Oldest(stream), stream.Series.Count);

                    if (stream.CursorEndUtc <= _windowStartUtc)
                        Complete(stream, "finestra coperta");
                    return;
                }

                if (stream.Series.Count >= maxTicks)
                {
                    Complete(stream, string.Format(
                        "raggiunto il tetto di {0} milioni di tick in memoria — la finestra e' " +
                        "coperta solo fino a {1:yyyy-MM-dd HH:mm}. Restringere 'Data inizio' e " +
                        "rilanciare per il resto.",
                        MaxMillionTicksPerSymbol, stream.Series.Count > 0 ? Oldest(stream) : _windowEndUtc));
                    return;
                }

                if (loads >= LoadsPerTick)
                    return; // freno: si riprende dallo stesso punto al prossimo battito

                loads++;
                int loaded;
                try
                {
                    loaded = stream.Series.LoadMoreHistory();
                }
                catch (Exception failure)
                {
                    // Un errore di caricamento non deve far cadere gli altri simboli: si chiude
                    // questo e si prosegue.
                    Complete(stream, "caricamento fallito: " + failure.Message);
                    return;
                }

                stream.Loads++;
                if (loaded <= 0)
                {
                    Complete(stream, stream.Series.Count > 0
                        ? string.Format("il broker non ha tick prima di {0:yyyy-MM-dd HH:mm}", Oldest(stream))
                        : "il broker non ha tick per questo simbolo");
                    return;
                }

                stream.Loaded += loaded;
                if (LogDiagnostico)
                    Print("{0}: +{1} tick (totale {2}, il piu' vecchio {3:yyyy-MM-dd HH:mm:ss}).",
                        stream.SymbolName, loaded, stream.Series.Count, Oldest(stream));
            }
        }

        private static DateTime Oldest(TickStream stream)
        {
            return DateTime.SpecifyKind(stream.Series[0].Time, DateTimeKind.Utc);
        }

        private void Complete(TickStream stream, string reason)
        {
            if (stream.Done)
                return;

            stream.Done = true;
            stream.Reason = reason;
            Print("{0}: finito ({1}). {2} tick in memoria, {3} caricamenti, {4} blocchi.",
                stream.SymbolName, reason, stream.Series.Count, stream.Loads, stream.Chunks);
        }

        private void Report()
        {
            var elapsed = Server.TimeInUtc - _startedAtUtc;
            Print("--- Riepilogo ({0:hh\\:mm\\:ss}) ---", elapsed);
            foreach (var stream in _streams)
            {
                Print("   {0}: {1} tick, dal {2} — {3}",
                    stream.SymbolName,
                    stream.Series.Count,
                    stream.Series.Count > 0
                        ? Oldest(stream).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                        : "nessun tick",
                    stream.Reason ?? "completato");
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
            if (_streams.Count > 0)
                Report();
            Print("Piootoo Tick Downloader fermato.");
        }

        private sealed class TickStream
        {
            public string SymbolName;
            public Ticks Series;

            /// <summary>Fine (esclusa) del prossimo pezzo: cammina all'indietro verso l'inizio finestra.</summary>
            public DateTime CursorEndUtc;

            public bool Done;
            public string Reason;
            public int Loads;
            public int Chunks;
            public long Loaded;
        }
    }
}
