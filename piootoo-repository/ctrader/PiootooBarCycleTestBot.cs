using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo.Robots
{
    // ------------------------------------------------------------------------------------------
    // PiootooBarCycleTestBot
    //
    // cBot di test minimo, senza server e senza piano: serve solo a verificare che l'esecuzione
    // multi-symbol funzioni sull'account cTrader.
    //
    // Cosa fa, per ciascuno dei due stream configurati (default EURUSD h1 e USTEC m15):
    //   - all'apertura di ogni nuova barra chiude la posizione aperta sulla barra precedente
    //     (cioe' la chiusura avviene a fine barra, al prezzo di apertura della barra nuova);
    //   - subito dopo apre a mercato una nuova posizione di Lots lotti sul proprio simbolo.
    // Il ciclo e' quindi "una posizione per barra, lunga esattamente una barra".
    //
    // NON lavora sul chart aperto: il grafico su cui il bot viene attaccato e' irrilevante, sia
    // come simbolo che come timeframe. Ogni stream ha la propria serie ottenuta con
    // MarketData.GetBars e si sveglia da solo tramite l'evento Bars.BarOpened. Per lo stesso
    // motivo qui non si usa OnBar(), che seguirebbe l'orologio del grafico.
    //
    // Nessuno stop loss, nessun take profit, nessuna gestione del rischio: e' un test di
    // esecuzione, non una strategia. Da non usare su conto reale.
    // ------------------------------------------------------------------------------------------

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class PiootooBarCycleTestBot : Robot
    {
        private const string BotName = "PiootooBarCycleTestBot";
        private const string BotVersion = "1.0.0";

        [Parameter("Simbolo 1", DefaultValue = "EURUSD", Group = "Stream 1")]
        public string Symbol1 { get; set; }

        [Parameter("Timeframe 1 (minuti)", DefaultValue = 60, MinValue = 1, Group = "Stream 1")]
        public int Timeframe1Minutes { get; set; }

        [Parameter("Simbolo 2", DefaultValue = "USTEC", Group = "Stream 2")]
        public string Symbol2 { get; set; }

        [Parameter("Timeframe 2 (minuti)", DefaultValue = 15, MinValue = 1, Group = "Stream 2")]
        public int Timeframe2Minutes { get; set; }

        [Parameter("Lotti", DefaultValue = 1.0, MinValue = 0.01, Group = "Esecuzione")]
        public double Lots { get; set; }

        // Il test non ha logica di segnale: la direzione e' fissa e la si dichiara qui invece di
        // nasconderla nel codice, cosi si puo' provare anche il lato short.
        [Parameter("Direzione", DefaultValue = TradeType.Buy, Group = "Esecuzione")]
        public TradeType Direction { get; set; }

        [Parameter("Chiudi le posizioni allo stop", DefaultValue = true, Group = "Esecuzione")]
        public bool CloseOnStop { get; set; }

        private readonly List<BarStream> _streams = new List<BarStream>();

        protected override void OnStart()
        {
            Print("{0} v{1} - avvio.", BotName, BotVersion);

            _streams.Add(CreateStream(Symbol1, Timeframe1Minutes));
            _streams.Add(CreateStream(Symbol2, Timeframe2Minutes));

            foreach (var stream in _streams)
            {
                // La sottoscrizione per serie e' il punto chiave: ogni stream reagisce alle
                // PROPRIE barre, indipendentemente dal grafico su cui gira il bot.
                stream.Series.BarOpened += OnStreamBarOpened;
                Print("Stream attivo: {0} a {1} minuti, {2} lotti {3}.",
                    stream.SymbolName, stream.TimeframeMinutes, Lots, Direction);
            }
        }

        protected override void OnStop()
        {
            foreach (var stream in _streams)
            {
                if (stream.Series != null)
                    stream.Series.BarOpened -= OnStreamBarOpened;
            }

            if (!CloseOnStop)
                return;

            foreach (var stream in _streams)
                CloseStreamPosition(stream, "stop del bot");
        }

        private BarStream CreateStream(string symbolName, int timeframeMinutes)
        {
            var name = (symbolName ?? string.Empty).Trim();
            if (name.Length == 0)
                throw new InvalidOperationException("il nome del simbolo non puo' essere vuoto.");

            var brokerSymbol = Symbols.GetSymbol(name);
            if (brokerSymbol == null)
            {
                throw new InvalidOperationException(
                    $"lo strumento '{name}' non esiste su questo account cTrader. " +
                    "Controlla il nome esatto nella watchlist del broker (es. USTEC, NAS100, US100).");
            }

            return new BarStream
            {
                SymbolName = brokerSymbol.Name,
                TimeframeMinutes = timeframeMinutes,
                BrokerSymbol = brokerSymbol,
                Series = MarketData.GetBars(ToTimeFrame(timeframeMinutes), brokerSymbol.Name),
                // Label distinta per stream: e' cosi che il bot ritrova le proprie posizioni
                // senza toccare quelle degli altri bot sullo stesso account.
                Label = $"{BotName}_{brokerSymbol.Name}_{timeframeMinutes}"
            };
        }

        /// <summary>
        /// Una nuova barra si e' aperta su una delle serie: la barra precedente e' quindi chiusa.
        /// Prima si chiude la posizione della barra precedente, poi se ne apre una nuova.
        /// L'ordine dei due passi conta: invertirlo terrebbe aperte due posizioni insieme.
        /// </summary>
        private void OnStreamBarOpened(BarOpenedEventArgs args)
        {
            var stream = _streams.FirstOrDefault(candidate => ReferenceEquals(candidate.Series, args.Bars));
            if (stream == null)
                return;

            CloseStreamPosition(stream, "fine barra");
            OpenStreamPosition(stream);
        }

        private void OpenStreamPosition(BarStream stream)
        {
            var volume = stream.BrokerSymbol.QuantityToVolumeInUnits(Lots);
            volume = stream.BrokerSymbol.NormalizeVolumeInUnits(volume, RoundingMode.ToNearest);
            if (volume <= 0)
            {
                Print("{0}: volume normalizzato a zero per {1} lotti, nessun ordine.",
                    stream.SymbolName, Lots);
                return;
            }

            var result = ExecuteMarketOrder(Direction, stream.SymbolName, volume, stream.Label);
            if (!result.IsSuccessful)
            {
                Print("{0}: apertura fallita ({1}).", stream.SymbolName, result.Error);
                return;
            }

            stream.PositionId = result.Position.Id;
            Print("{0}: aperta {1} {2} unita a {3} (barra {4}).",
                stream.SymbolName, Direction, volume, result.Position.EntryPrice,
                stream.Series.OpenTimes.LastValue);
        }

        private void CloseStreamPosition(BarStream stream, string reason)
        {
            var position = FindStreamPosition(stream);
            if (position == null)
            {
                stream.PositionId = null;
                return;
            }

            var result = ClosePosition(position);
            if (!result.IsSuccessful)
            {
                Print("{0}: chiusura fallita ({1}).", stream.SymbolName, result.Error);
                return;
            }

            Print("{0}: chiusa per {1}, profitto lordo {2}.",
                stream.SymbolName, reason, position.GrossProfit);
            stream.PositionId = null;
        }

        /// <summary>
        /// La posizione dello stream si cerca prima per Id (l'identita non cambia) e in seconda
        /// battuta per label + simbolo, cosi il bot ritrova la propria posizione anche dopo un
        /// riavvio a mercato aperto.
        /// </summary>
        private Position FindStreamPosition(BarStream stream)
        {
            if (stream.PositionId.HasValue)
            {
                var byId = Positions.FirstOrDefault(position => position.Id == stream.PositionId.Value);
                if (byId != null)
                    return byId;
            }

            return Positions.FirstOrDefault(position =>
                position.Label == stream.Label &&
                string.Equals(position.SymbolName, stream.SymbolName, StringComparison.OrdinalIgnoreCase));
        }

        private static TimeFrame ToTimeFrame(int minutes)
        {
            switch (minutes)
            {
                case 1: return TimeFrame.Minute;
                case 2: return TimeFrame.Minute2;
                case 3: return TimeFrame.Minute3;
                case 4: return TimeFrame.Minute4;
                case 5: return TimeFrame.Minute5;
                case 10: return TimeFrame.Minute10;
                case 15: return TimeFrame.Minute15;
                case 20: return TimeFrame.Minute20;
                case 30: return TimeFrame.Minute30;
                case 45: return TimeFrame.Minute45;
                case 60: return TimeFrame.Hour;
                case 120: return TimeFrame.Hour2;
                case 240: return TimeFrame.Hour4;
                case 480: return TimeFrame.Hour8;
                case 720: return TimeFrame.Hour12;
                case 1440: return TimeFrame.Daily;
                default:
                    throw new InvalidOperationException(
                        $"timeframe non supportato: {minutes} minuti.");
            }
        }

        private sealed class BarStream
        {
            public string SymbolName { get; set; }
            public int TimeframeMinutes { get; set; }
            public Symbol BrokerSymbol { get; set; }
            public Bars Series { get; set; }
            public string Label { get; set; }
            public int? PositionId { get; set; }
        }
    }
}
