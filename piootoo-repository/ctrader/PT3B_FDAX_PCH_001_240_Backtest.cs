using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    /// <summary>
    /// PT3B_FDAX_PCH_001_240 — price channel su FDAX a 4 ore, per il BACKTEST in cTrader.
    ///
    /// Bot autonomo: non parla con il server Piootoo e non riceve intent. Serve a rimisurare la
    /// strategia sui dati del broker, su tick, e a confrontare le ENTRATE con quelle del motore
    /// interno. Specifica completa in piootoo-repository/ricerca/PT3B_FDAX_PCH_001_240.md.
    ///
    /// ── LA COSA DA NON SBAGLIARE ─────────────────────────────────────────────────────────────
    /// La griglia a 4 ore NON e' quella di cTrader. L'H4 della piattaforma e' ancorato all'orologio
    /// del broker; questa strategia alla sessione che comincia all'01:00 di Roma. Le barre che ne
    /// escono sono diverse, gli estremi del canale pure, e nessun errore lo segnala: i risultati
    /// semplicemente non coincidono con quelli della ricerca.
    ///
    /// Per questo il bot va avviato su una serie FITTA (m15 consigliato, m5 o m1 vanno bene) e i
    /// bucket da 4 ore se li costruisce in codice, con l'etichetta all'APERTURA e il confine
    /// calcolato in ora locale — non per sottrazione dall'istante UTC, che sbaglia nelle settimane
    /// in cui l'ora legale europea e americana non sono allineate.
    ///
    /// Usare **Tick data (accurate)**: su dati a barre il simulatore di cTrader valuta lo stop anche
    /// contro la barra d'ingresso, percorso pre-entrata incluso, e chiude a prezzi mai esistiti.
    /// ─────────────────────────────────────────────────────────────────────────────────────────
    /// </summary>
    [Robot(AccessRights = AccessRights.None, TimeZone = TimeZones.UTC)]
    public class PT3B_FDAX_PCH_001_240_Backtest : Robot
    {
        // ─────────────────────────────────────────────────────────── parametri della strategia

        [Parameter("Contratti (lotti)", DefaultValue = 1.0, MinValue = 0.01, Group = "Size")]
        public double Contratti { get; set; }

        [Parameter("Ora di inizio sessione (locale)", DefaultValue = 1, MinValue = 0, MaxValue = 23, Group = "Griglia")]
        public int OraInizioSessione { get; set; }

        [Parameter("Minuti del bucket", DefaultValue = 240, MinValue = 5, Group = "Griglia")]
        public int MinutiBucket { get; set; }

        [Parameter("Fuso della ricerca", DefaultValue = "W. Europe Standard Time", Group = "Griglia")]
        public string FusoRicerca { get; set; }

        [Parameter("Barre del canale", DefaultValue = 1, MinValue = 1, Group = "Trigger")]
        public int BarreCanale { get; set; }

        [Parameter("Offset in tick", DefaultValue = 0, MinValue = 0, Group = "Trigger")]
        public int OffsetTick { get; set; }

        [Parameter("Ora minima di apertura del bucket", DefaultValue = 3, MinValue = -1, MaxValue = 23, Group = "Orari")]
        public int OraDa { get; set; }

        [Parameter("Ora massima di apertura del bucket", DefaultValue = 18, MinValue = -1, MaxValue = 23, Group = "Orari")]
        public int OraA { get; set; }

        [Parameter("Stop loss (punti)", DefaultValue = 200.0, MinValue = 0, Group = "Uscite")]
        public double StopPunti { get; set; }

        [Parameter("Take profit (punti)", DefaultValue = 180.0, MinValue = 0, Group = "Uscite")]
        public double TargetPunti { get; set; }

        [Parameter("Massimo bucket in posizione", DefaultValue = 12, MinValue = 0, Group = "Uscite")]
        public int MassimoBucket { get; set; }

        [Parameter("Chiudi a fine sessione", DefaultValue = true, Group = "Uscite")]
        public bool ChiudiAFineSessione { get; set; }

        [Parameter("Filtro: sessione stretta (neutrale 44)", DefaultValue = true, Group = "Filtri")]
        public bool FiltroSessioneStretta { get; set; }

        [Parameter("Filtro: niente outside bar (neutrale 52)", DefaultValue = true, Group = "Filtri")]
        public bool FiltroOutsideBar { get; set; }

        [Parameter("Filtro: niente ingressi dopo il 2% (direzionale -18)", DefaultValue = true, Group = "Filtri")]
        public bool FiltroDopoStrappo { get; set; }

        [Parameter("Scarta i livelli gia' scavalcati", DefaultValue = true, Group = "Esecuzione")]
        public bool ScartaLivelliScavalcati { get; set; }

        private const string Etichetta = "PT3B_FDAX_PCH_001_240";

        // ─────────────────────────────────────────────────────────── stato

        private TimeZoneInfo _fuso;

        /// <summary>I bucket da 4 ore gia' chiusi, il piu' recente in fondo.</summary>
        private readonly List<Bucket> _bucket = new List<Bucket>();

        /// <summary>Il bucket in formazione.</summary>
        private Bucket _bucketCorrente;

        /// <summary>Le sessioni gia' chiuse, la piu' recente in fondo. Servono ai pattern d1 e d2.</summary>
        private readonly List<Sessione> _sessioni = new List<Sessione>();

        /// <summary>La sessione in corso: e' la d0 dei pattern.</summary>
        private Sessione _sessioneCorrente;

        /// <summary>Un ingresso per sessione E PER DIREZIONE: la chiave e' (giorno di sessione, lato).</summary>
        private DateTime _sessioneUltimoLong = DateTime.MinValue;
        private DateTime _sessioneUltimoShort = DateTime.MinValue;

        /// <summary>Quanti bucket sono passati da quando la posizione e' aperta.</summary>
        private int _bucketInPosizione;

        private sealed class Bucket
        {
            public DateTime AperturaLocale;
            public double Open, High, Low, Close;
        }

        private sealed class Sessione
        {
            public DateTime Giorno;
            public double Open, High, Low, Close;
        }

        // ─────────────────────────────────────────────────────────── ciclo di vita

        protected override void OnStart()
        {
            try
            {
                _fuso = TimeZoneInfo.FindSystemTimeZoneById(FusoRicerca);
            }
            catch (Exception)
            {
                // Su installazioni non Windows l'identificativo e' quello IANA.
                _fuso = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
            }

            var minutiSerie = (int)TimeFrame.ToTimeSpan().TotalMinutes;
            if (minutiSerie <= 0 || minutiSerie > 60 || MinutiBucket % minutiSerie != 0)
            {
                Print("ERRORE: avvia il bot su una serie fino a 60 minuti che divida {0} " +
                      "(consigliata m15). I bucket li costruisce il bot, non la piattaforma.", MinutiBucket);
                Stop();
                return;
            }

            Positions.Closed += OnPosizioneChiusa;

            Print("{0}: serie base {1}, bucket {2}m ancorati alle {3}:00 di {4}, finestra {5}-{6}, " +
                  "stop {7} pt, target {8} pt, max {9} bucket.",
                  Etichetta, TimeFrame, MinutiBucket, OraInizioSessione, _fuso.Id,
                  OraDa, OraA, StopPunti, TargetPunti, MassimoBucket);
        }

        /// <summary>
        /// Chiamata alla chiusura di ogni barra della serie base. Il bot accumula la barra nel
        /// bucket e nella sessione, e quando il bucket si chiude valuta la strategia.
        /// </summary>
        protected override void OnBar()
        {
            var indice = Bars.Count - 2; // l'ultima barra CHIUSA
            if (indice < 0) return;

            var aperturaUtc = Bars.OpenTimes[indice];
            var aperturaLocale = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(aperturaUtc, DateTimeKind.Utc), _fuso);

            AggiornaSessione(aperturaLocale, indice);
            AggiornaBucket(aperturaLocale, indice);

            var minutiSerie = (int)TimeFrame.ToTimeSpan().TotalMinutes;
            var prossimaLocale = aperturaLocale.AddMinutes(minutiSerie);

            // Il bucket si chiude quando la barra SUCCESSIVA appartiene a un altro bucket.
            if (AperturaBucket(prossimaLocale) != AperturaBucket(aperturaLocale))
            {
                var chiuso = _bucketCorrente;
                _bucket.Add(chiuso);
                if (_bucket.Count > 50) _bucket.RemoveAt(0);
                _bucketCorrente = null;

                if (Positions.Count(Etichetta) > 0) _bucketInPosizione++;

                GestisciUscite();
                Valuta(chiuso, prossimaLocale);
            }

            // La sessione si chiude quando la barra successiva cade nel giorno di sessione dopo.
            if (GiornoSessione(prossimaLocale) != GiornoSessione(aperturaLocale))
            {
                if (ChiudiAFineSessione) ChiudiTutto("fine sessione");
                CancellaPendenti();

                _sessioni.Add(_sessioneCorrente);
                if (_sessioni.Count > 10) _sessioni.RemoveAt(0);
                _sessioneCorrente = null;
            }
        }

        // ─────────────────────────────────────────────────────────── griglia e sessione

        /// <summary>
        /// L'apertura del bucket che contiene questo istante locale. Il confine si calcola in ORA
        /// LOCALE a partire dall'ancoraggio di sessione: per sottrazione dall'istante UTC sarebbe
        /// sbagliato nelle settimane in cui i due emisferi cambiano ora in giorni diversi.
        /// </summary>
        private DateTime AperturaBucket(DateTime locale)
        {
            var giorno = GiornoSessione(locale);
            var inizio = giorno.AddHours(OraInizioSessione);
            var minutiDaInizio = (int)(locale - inizio).TotalMinutes;
            var indice = minutiDaInizio / MinutiBucket;
            return inizio.AddMinutes(indice * MinutiBucket);
        }

        /// <summary>
        /// Il giorno di sessione: prima dell'ancoraggio si appartiene ancora al giorno precedente.
        /// </summary>
        private DateTime GiornoSessione(DateTime locale)
        {
            return locale.Hour < OraInizioSessione ? locale.Date.AddDays(-1) : locale.Date;
        }

        private void AggiornaBucket(DateTime aperturaLocale, int indice)
        {
            var apertura = AperturaBucket(aperturaLocale);
            if (_bucketCorrente == null || _bucketCorrente.AperturaLocale != apertura)
            {
                _bucketCorrente = new Bucket
                {
                    AperturaLocale = apertura,
                    Open = Bars.OpenPrices[indice],
                    High = Bars.HighPrices[indice],
                    Low = Bars.LowPrices[indice],
                    Close = Bars.ClosePrices[indice]
                };
                return;
            }

            _bucketCorrente.High = Math.Max(_bucketCorrente.High, Bars.HighPrices[indice]);
            _bucketCorrente.Low = Math.Min(_bucketCorrente.Low, Bars.LowPrices[indice]);
            _bucketCorrente.Close = Bars.ClosePrices[indice];
        }

        private void AggiornaSessione(DateTime aperturaLocale, int indice)
        {
            var giorno = GiornoSessione(aperturaLocale);
            if (_sessioneCorrente == null || _sessioneCorrente.Giorno != giorno)
            {
                _sessioneCorrente = new Sessione
                {
                    Giorno = giorno,
                    Open = Bars.OpenPrices[indice],
                    High = Bars.HighPrices[indice],
                    Low = Bars.LowPrices[indice],
                    Close = Bars.ClosePrices[indice]
                };
                return;
            }

            _sessioneCorrente.High = Math.Max(_sessioneCorrente.High, Bars.HighPrices[indice]);
            _sessioneCorrente.Low = Math.Min(_sessioneCorrente.Low, Bars.LowPrices[indice]);
            _sessioneCorrente.Close = Bars.ClosePrices[indice];
        }

        // ─────────────────────────────────────────────────────────── la strategia

        /// <summary>
        /// Valutata alla CHIUSURA del bucket. L'ordine nasce qui e vale per il bucket successivo
        /// soltanto: se non viene toccato scade, e se le condizioni valgono ancora viene riemesso.
        /// </summary>
        private void Valuta(Bucket chiuso, DateTime aperturaProssimoBucket)
        {
            if (_bucket.Count < Math.Max(BarreCanale, 1)) return;
            if (_sessioni.Count < 2) return;              // servono d1 e d2 per i pattern
            if (Positions.Count(Etichetta) > 0) return;   // in posizione non si emette

            // La finestra si confronta con l'APERTURA del bucket appena chiuso, perche' e' cosi' che
            // la ricerca ha scritto start_hour ed end_hour (etichetta all'apertura). Estremi inclusi.
            if (!InFinestra(chiuso.AperturaLocale.Hour)) return;

            if (!PassaFiltriNeutrali()) return;

            // Canale: gli estremi delle ultime N barre, inclusa quella appena chiusa.
            var finestra = _bucket.Skip(Math.Max(0, _bucket.Count - BarreCanale)).ToList();
            var massimo = finestra.Max(b => b.High);
            var minimo = finestra.Min(b => b.Low);
            var offset = OffsetTick * Symbol.TickSize;

            var scadenza = Server.Time.AddMinutes(MinutiBucket);
            var giorno = GiornoSessione(chiuso.AperturaLocale);

            if (_sessioneUltimoLong != giorno && PassaFiltroDirezionale(true))
                Piazza(TradeType.Buy, massimo + offset, scadenza);

            if (_sessioneUltimoShort != giorno && PassaFiltroDirezionale(false))
                Piazza(TradeType.Sell, minimo - offset, scadenza);
        }

        private bool InFinestra(int oraApertura)
        {
            if (OraDa < 0 && OraA < 0) return true;
            var da = OraDa < 0 ? 0 : OraDa;
            var a = OraA < 0 ? 23 : OraA;
            return da <= a
                ? oraApertura >= da && oraApertura <= a
                : oraApertura >= da || oraApertura <= a;   // finestra che attraversa la mezzanotte
        }

        /// <summary>
        /// I due filtri sulle grandezze di sessione. d0 e' la sessione IN CORSO, d1 la precedente.
        /// </summary>
        private bool PassaFiltriNeutrali()
        {
            var d0 = _sessioneCorrente;
            var d1 = _sessioni[_sessioni.Count - 1];
            if (d0 == null) return false;

            // neutrale 44, RICHIESTO: la sessione in corso ha un'escursione sotto il 3%.
            if (FiltroSessioneStretta && !(d0.High < d0.Low * 1.03)) return false;

            // neutrale 52, VIETATO: la sessione in corso ha gia' inglobato la precedente.
            if (FiltroOutsideBar && d0.High > d1.High && d0.Low < d1.Low) return false;

            return true;
        }

        /// <summary>
        /// Direzionale -18, VIETATO: niente ingressi nella direzione in cui il mercato si e' gia'
        /// mosso di oltre il 2% nella sessione precedente. Il segno si applica al lato, quindi il
        /// long guarda una discesa e lo short una salita.
        /// </summary>
        private bool PassaFiltroDirezionale(bool lungo)
        {
            if (!FiltroDopoStrappo) return true;

            var d1 = _sessioni[_sessioni.Count - 1];
            var d2 = _sessioni[_sessioni.Count - 2];

            return lungo
                ? !(d1.Close < d2.Close - d2.Close * 0.02)
                : !(d1.Close > d2.Close + d2.Close * 0.02);
        }

        private void Piazza(TradeType lato, double livello, DateTime scadenza)
        {
            // Un livello gia' scavalcato non e' un ordine: si riempirebbe all'apertura, e il broker
            // vero lo rifiuta. Il riferimento per chi compra e' l'Ask, per chi vende il Bid.
            if (ScartaLivelliScavalcati)
            {
                if (lato == TradeType.Buy && Symbol.Ask >= livello) return;
                if (lato == TradeType.Sell && Symbol.Bid <= livello) return;
            }

            var volume = Symbol.QuantityToVolumeInUnits(Contratti);
            var stop = StopPunti > 0 ? (double?)(StopPunti / Symbol.PipSize) : null;
            var target = TargetPunti > 0 ? (double?)(TargetPunti / Symbol.PipSize) : null;
            var livelloNormalizzato = Math.Round(livello / Symbol.TickSize) * Symbol.TickSize;

            PlaceStopOrder(lato, SymbolName, volume, livelloNormalizzato, Etichetta, stop, target, scadenza);
        }

        // ─────────────────────────────────────────────────────────── uscite

        private void GestisciUscite()
        {
            if (MassimoBucket <= 0) return;

            foreach (var posizione in Positions.FindAll(Etichetta))
            {
                if (_bucketInPosizione >= MassimoBucket)
                    ClosePosition(posizione);
            }
        }

        private void ChiudiTutto(string motivo)
        {
            foreach (var posizione in Positions.FindAll(Etichetta))
            {
                Print("{0}: chiusura per {1}", Etichetta, motivo);
                ClosePosition(posizione);
            }
        }

        private void CancellaPendenti()
        {
            foreach (var ordine in PendingOrders.Where(o => o.Label == Etichetta).ToList())
                CancelPendingOrder(ordine);
        }

        protected override void OnPositionOpened(Position posizione)
        {
            if (posizione.Label != Etichetta) return;

            // OCO: riempita una gamba, l'altra esce di scena.
            CancellaPendenti();
            _bucketInPosizione = 0;

            var giorno = GiornoSessione(TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(Server.Time, DateTimeKind.Utc), _fuso));

            if (posizione.TradeType == TradeType.Buy) _sessioneUltimoLong = giorno;
            else _sessioneUltimoShort = giorno;
        }

        private void OnPosizioneChiusa(PositionClosedEventArgs evento)
        {
            if (evento.Position.Label != Etichetta) return;
            _bucketInPosizione = 0;
        }

        protected override void OnStop()
        {
            Print("{0}: fine. Confronta le ENTRATE (istante e prezzo) con trades.json del motore " +
                  "interno, non il P&L: i P&L sono una conseguenza.", Etichetta);
        }
    }
}
