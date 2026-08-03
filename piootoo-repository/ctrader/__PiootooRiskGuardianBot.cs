using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using cAlgo.API;

namespace cAlgo.Robots
{
    /// <summary>
    /// cBot "guardiano" di risk-management per conti prop firm (FTMO e simili): NON apre posizioni,
    /// sorveglia in continuo l'intero account (equity/balance, tutte le posizioni e ordini pendenti,
    /// SU TUTTI I SIMBOLI, indipendentemente da quale cBot/operatore li abbia aperti) e:
    ///  - protegge cross-symbol: Account.Equity/Account.Balance sono gia' aggregati su tutte le
    ///    posizioni dell'account qualunque sia il simbolo, e le chiusure (CloseAllPositionsAndOrders)
    ///    iterano su TUTTE le posizioni/ordini pendenti dell'account, non solo sul simbolo del grafico
    ///    a cui il bot e' agganciato;
    ///  - chiude TUTTO quando ci si avvicina al Max Daily Loss (perdita massima giornaliera);
    ///  - chiude TUTTO quando ci si avvicina al Max Loss assoluto (drawdown massimo dal capitale iniziale,
    ///    statico o trailing sull'high-water del balance, a seconda della prop firm);
    ///  - opzionalmente chiude TUTTO quando ci si avvicina al Max Loss settimanale, se la prop firm lo
    ///    prevede (FTMO non lo richiede, ma il parametro resta disponibile e disattivabile);
    ///  - va in "standby" nell'intorno di una news ad alto impatto (letta da un calendario JSON locale,
    ///    cosi' le date/ora sono sempre esplicite in UTC senza ambiguita' locale/UTC), chiudendo le
    ///    posizioni sui simboli coinvolti se configurato (o su tutti i simboli se l'evento e' "ALL");
    ///  - persiste il proprio stato (capitale iniziale, balance/equity di inizio giornata e inizio
    ///    settimana, high-water del balance) su file JSON con orari sempre in UTC, cosi' dopo un
    ///    riavvio del bot o della piattaforma i limiti restano corretti senza dover ripartire da zero
    ///    e senza ambiguita' tra ora locale e UTC;
    ///  - mostra e rinfresca sul grafico a cui e' agganciato: nome del bot, drawdown giornaliero,
    ///    profit giornaliero e profit settimanale (oltre ai limiti e allo stato di standby).
    ///
    /// Regole di riferimento (FTMO, luglio 2026 - vedi https://ftmo.com/en/trading-objectives/):
    ///  - Max Daily Loss: equity non puo' scendere sotto (balance a inizio giornata - 3%/5% del capitale
    ///    iniziale), reset giornaliero a 00:00 CE(S)T;
    ///  - Max Loss: equity non puo' scendere sotto (capitale iniziale - 10%), statico nel 2-Step, oppure
    ///    trailing sull'high-water del balance di fine giornata nel 1-Step;
    ///  - il "gap/news trading" (aprire posizioni a ridosso di news ad alto impatto o entro 2 ore dalla
    ///    chiusura del mercato) e' tra le pratiche vietate: questo bot copre la parte "stare fermi
    ///    intorno alle news" chiudendo/non lasciando esposizione nella finestra configurata.
    ///
    /// NOTA: questo cBot e' un guardiano passivo, non impedisce ad altri cBot di aprire nuove posizioni
    /// durante lo standby; se serve un blocco "hard" delle nuove aperture, gli altri cBot (es.
    /// PiootooDistributedExecutionBot) dovrebbero leggere lo stesso file di stato (vedi RiskState) prima di
    /// inviare un nuovo ordine.
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooRiskGuardianBot : Robot
    {
        private const string BotDisplayName = "Piootoo Risk Guardian";
        private const string BotVersion = "1.0.0";

        // ------------------------------------------------------------------------------- Parametri

        [Parameter("Capitale iniziale (0 = usa Account.Balance al primo avvio)", DefaultValue = 0, Group = "Generale")]
        public double InitialBalanceOverride { get; set; }

        [Parameter("Ora di reset giornaliero (UTC, 0-23)", DefaultValue = 22, MinValue = 0, MaxValue = 23, Group = "Generale")]
        public int DailyResetHourUtc { get; set; }

        [Parameter("Giorno di inizio settimana", DefaultValue = DayOfWeek.Monday, Group = "Generale")]
        public DayOfWeek WeekStartDay { get; set; }

        [Parameter("Intervallo di controllo (secondi)", DefaultValue = 5, MinValue = 1, Group = "Generale")]
        public int CheckIntervalSeconds { get; set; }

        [Parameter("File stato persistente (vuoto = automatico, per account)", DefaultValue = "", Group = "Generale")]
        public string StateFilePath { get; set; }

        [Parameter("Dry run (solo log, nessuna chiusura reale)", DefaultValue = false, Group = "Generale")]
        public bool DryRun { get; set; }

        [Parameter("Log dettagliato", DefaultValue = true, Group = "Generale")]
        public bool VerboseLogging { get; set; }

        [Parameter("Mostra stato su grafico", DefaultValue = true, Group = "Generale")]
        public bool ShowChartStatus { get; set; }

        [Parameter("Abilita Max Daily Loss", DefaultValue = true, Group = "Max Daily Loss")]
        public bool DailyLossEnabled { get; set; }

        [Parameter("Base di calcolo", DefaultValue = LossBasisMode.BalanceStartOfPeriod, Group = "Max Daily Loss")]
        public LossBasisMode DailyLossBasis { get; set; }

        [Parameter("Modalita' soglia", DefaultValue = ThresholdMode.Percent, Group = "Max Daily Loss")]
        public ThresholdMode DailyLossMode { get; set; }

        [Parameter("Valore soglia (% o importo, calcolato sul capitale iniziale)", DefaultValue = 5.0, MinValue = 0, Group = "Max Daily Loss")]
        public double DailyLossValue { get; set; }

        [Parameter("Buffer di sicurezza (% del capitale iniziale, anticipa il trigger)", DefaultValue = 0.5, MinValue = 0, Group = "Max Daily Loss")]
        public double DailyLossSafetyBufferPercent { get; set; }

        [Parameter("Abilita Max Loss assoluto", DefaultValue = true, Group = "Max Loss Assoluto")]
        public bool MaxLossEnabled { get; set; }

        [Parameter("Base di calcolo", DefaultValue = MaxLossBasisMode.StaticFromInitialCapital, Group = "Max Loss Assoluto")]
        public MaxLossBasisMode MaxLossBasis { get; set; }

        [Parameter("Modalita' soglia", DefaultValue = ThresholdMode.Percent, Group = "Max Loss Assoluto")]
        public ThresholdMode MaxLossMode { get; set; }

        [Parameter("Valore soglia (% o importo, calcolato sul capitale iniziale)", DefaultValue = 10.0, MinValue = 0, Group = "Max Loss Assoluto")]
        public double MaxLossValue { get; set; }

        [Parameter("Buffer di sicurezza (% del capitale iniziale, anticipa il trigger)", DefaultValue = 0.5, MinValue = 0, Group = "Max Loss Assoluto")]
        public double MaxLossSafetyBufferPercent { get; set; }

        [Parameter("Abilita Max Loss settimanale (non richiesto da FTMO, disattivo di default)", DefaultValue = false, Group = "Max Loss Settimanale")]
        public bool WeeklyLossEnabled { get; set; }

        [Parameter("Modalita' soglia", DefaultValue = ThresholdMode.Percent, Group = "Max Loss Settimanale")]
        public ThresholdMode WeeklyLossMode { get; set; }

        [Parameter("Valore soglia (% o importo, calcolato sul capitale iniziale)", DefaultValue = 8.0, MinValue = 0, Group = "Max Loss Settimanale")]
        public double WeeklyLossValue { get; set; }

        [Parameter("Buffer di sicurezza (% del capitale iniziale, anticipa il trigger)", DefaultValue = 0.5, MinValue = 0, Group = "Max Loss Settimanale")]
        public double WeeklyLossSafetyBufferPercent { get; set; }

        [Parameter("Abilita standby news", DefaultValue = true, Group = "News Standby")]
        public bool NewsEnabled { get; set; }

        [Parameter("File calendario news JSON (relativo = cartella dati del bot)", DefaultValue = "news_calendar.json", Group = "News Standby")]
        public string NewsFilePath { get; set; }

        [Parameter("Minuti di standby PRIMA della news", DefaultValue = 5, MinValue = 0, Group = "News Standby")]
        public int NewsMinutesBefore { get; set; }

        [Parameter("Minuti di standby DOPO la news", DefaultValue = 5, MinValue = 0, Group = "News Standby")]
        public int NewsMinutesAfter { get; set; }

        [Parameter("Impatto minimo considerato", DefaultValue = NewsImpact.High, Group = "News Standby")]
        public NewsImpact NewsMinImpact { get; set; }

        [Parameter("Chiudi posizioni durante lo standby", DefaultValue = true, Group = "News Standby")]
        public bool NewsFlattenPositions { get; set; }

        // ------------------------------------------------------------------------------- Tipi ausiliari

        public enum LossBasisMode
        {
            /// <summary>Balance registrato all'inizio del periodo (giorno/settimana) - come FTMO.</summary>
            BalanceStartOfPeriod,
            /// <summary>Picco di equity raggiunto durante il periodo corrente (piu' prudente).</summary>
            EquityHighWaterIntraPeriod
        }

        public enum MaxLossBasisMode
        {
            /// <summary>Limite statico calcolato una volta sola sul capitale iniziale (FTMO 2-Step).</summary>
            StaticFromInitialCapital,
            /// <summary>Limite trailing sull'high-water del balance registrato a fine giornata (FTMO 1-Step).</summary>
            TrailingHighWaterBalance
        }

        public enum ThresholdMode
        {
            Percent,
            FixedAmount
        }

        public enum NewsImpact
        {
            Low,
            Medium,
            High
        }

        private sealed class NewsEvent
        {
            public DateTime TimeUtc;
            /// <summary>null = riguarda tutti i simboli.</summary>
            public string[] Symbols;
            public NewsImpact Impact;
            public string Title;
            public string Key => TimeUtc.ToString("O", CultureInfo.InvariantCulture) + "|" + Title;
        }

        /// <summary>
        /// Forma "sul disco" di un evento del calendario news (news_calendar.json): campi tutti stringa/
        /// array cosi' la deserializzazione non fallisce su un valore imprevisto, la validazione/i default
        /// si applicano dopo in ParseNewsJson. TimeUtc va sempre scritto con suffisso "Z" (es.
        /// "2026-08-01T12:30:00Z"): System.Text.Json in quel caso valorizza gia' DateTimeKind.Utc, senza
        /// nessuna ambiguita' tra ora locale e ora UTC.
        /// </summary>
        private sealed class NewsEventFile
        {
            public DateTime TimeUtc { get; set; }
            /// <summary>Array di simboli, oppure ["ALL"] / ["*"] per tutti i simboli.</summary>
            public string[] Symbols { get; set; }
            public string Impact { get; set; } = "High";
            public string Title { get; set; } = "";
        }

        /// <summary>Stato persistito su file JSON tra un riavvio e l'altro del cBot.</summary>
        private sealed class RiskState
        {
            public long AccountNumber { get; set; }
            public double InitialBalance { get; set; }

            public DateTime CurrentTradingDay { get; set; }
            public double BalanceAtDayStart { get; set; }
            public double EquityHighWaterDay { get; set; }
            public bool DailyGuardTriggeredToday { get; set; }

            public DateTime CurrentTradingWeekStart { get; set; }
            public double BalanceAtWeekStart { get; set; }
            public double EquityHighWaterWeek { get; set; }
            public bool WeeklyGuardTriggeredThisWeek { get; set; }

            /// <summary>High-water del balance, usato come base per il Max Loss trailing.</summary>
            public double OverallHighWaterBalance { get; set; }
            /// <summary>Una volta violato il Max Loss assoluto resta true: e' una violazione grave che
            /// richiede intervento manuale (reset dello stato) per essere ripristinata.</summary>
            public bool MaxLossGuardTriggered { get; set; }

            public DateTime LastUpdatedUtc { get; set; }
        }

        // ------------------------------------------------------------------------------- Stato interno

        private RiskState _state;
        private string _resolvedStateFilePath;
        private string _resolvedNewsFilePath;
        private List<NewsEvent> _newsEvents = new();
        private DateTime? _newsFileLastWriteTimeUtc;
        private bool _isInStandby;
        private readonly HashSet<string> _flattenedForEventKeys = new();

        protected override void OnStart()
        {
            ResolvePaths();
            LoadOrInitState(NowUtc());
            LoadNewsFileIfNeeded(force: true);

            Timer.Start(TimeSpan.FromSeconds(Math.Max(1, CheckIntervalSeconds)));

            Print("{0} v{1} avviato. Account={2} CapitaleIniziale={3:0.00} DryRun={4}",
                BotDisplayName, BotVersion, Account.Number, _state.InitialBalance, DryRun);

            // Nome e versione del bot vanno scritti sul grafico una volta sola, qui in OnStart: non
            // cambiano mai durante l'esecuzione, quindi non c'e' motivo di riscriverli ad ogni refresh
            // del pannello (che invece si aggiorna di continuo con equity/drawdown/profit). In backtest
            // restano disattivati insieme al resto della scrittura su chart.
            if (ShowChartStatus && !IsBacktesting)
                Chart.DrawStaticText("PiootooRiskGuardianTitle", string.Format(CultureInfo.InvariantCulture,
                    "{0} v{1}", BotDisplayName, BotVersion), VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);

            RunChecks();
        }

        protected override void OnTimer()
        {
            RunChecks();
        }

        protected override void OnTick()
        {
            // Controllo rapido supplementare tra un tick del Timer e l'altro: l'equity puo' muoversi in
            // fretta sul simbolo del grafico. Il Timer resta comunque la fonte principale perche' copre
            // anche i simboli diversi da quello del grafico.
            CheckDrawdownLimits();
            // Il pannello sul grafico si rinfresca ad ogni tick (non solo ad ogni Timer) cosi' i valori
            // di drawdown/profit restano aggiornati in tempo reale mentre il mercato si muove.
            if (ShowChartStatus) UpdateChartStatus();
        }

        protected override void OnStop()
        {
            Timer.Stop();
            SaveState();
        }

        // ------------------------------------------------------------------------------- Ciclo principale

        private void RunChecks()
        {
            var nowUtc = NowUtc();
            RolloverIfNeeded(nowUtc);
            CheckDrawdownLimits();
            LoadNewsFileIfNeeded(force: false);
            CheckNewsStandby(nowUtc);
            if (ShowChartStatus) UpdateChartStatus();
            SaveState();
        }

        private DateTime NowUtc() => Server.TimeInUtc;

        // ------------------------------------------------------------------------------- Rollover giorno/settimana

        private DateTime GetTradingDay(DateTime nowUtc) => (nowUtc - TimeSpan.FromHours(DailyResetHourUtc)).Date;

        private DateTime GetTradingWeekStart(DateTime tradingDay)
        {
            var diff = ((int)tradingDay.DayOfWeek - (int)WeekStartDay + 7) % 7;
            return tradingDay.AddDays(-diff);
        }

        /// <summary>
        /// Avanza lo stato di un giorno alla volta fino a raggiungere la giornata corrente. Se il bot era
        /// spento per piu' giorni, usa il balance/equity CORRENTI come approssimazione per ogni giorno
        /// saltato (non abbiamo la storia intraday di quando era spento): e' una scelta prudente e
        /// documentata, non un dato esatto.
        /// </summary>
        private void RolloverIfNeeded(DateTime nowUtc)
        {
            var today = GetTradingDay(nowUtc);
            var rolled = false;
            while (_state.CurrentTradingDay < today)
            {
                rolled = true;
                _state.OverallHighWaterBalance = Math.Max(_state.OverallHighWaterBalance, Account.Balance);
                _state.CurrentTradingDay = _state.CurrentTradingDay.AddDays(1);
                _state.BalanceAtDayStart = Account.Balance;
                _state.EquityHighWaterDay = Account.Equity;
                _state.DailyGuardTriggeredToday = false;

                var weekStart = GetTradingWeekStart(_state.CurrentTradingDay);
                if (weekStart > _state.CurrentTradingWeekStart)
                {
                    _state.CurrentTradingWeekStart = weekStart;
                    _state.BalanceAtWeekStart = Account.Balance;
                    _state.EquityHighWaterWeek = Account.Equity;
                    _state.WeeklyGuardTriggeredThisWeek = false;
                }
            }

            if (rolled && VerboseLogging)
                Print("[ROLLOVER] Nuova giornata di trading {0:yyyy-MM-dd}: BalanceAtDayStart={1:0.00}, BalanceAtWeekStart={2:0.00} (settimana dal {3:yyyy-MM-dd}).",
                    _state.CurrentTradingDay, _state.BalanceAtDayStart, _state.BalanceAtWeekStart, _state.CurrentTradingWeekStart);
        }

        // ------------------------------------------------------------------------------- Guardie drawdown

        private double ComputeDailyLimit()
        {
            var referenceStart = DailyLossBasis == LossBasisMode.BalanceStartOfPeriod ? _state.BalanceAtDayStart : _state.EquityHighWaterDay;
            var lossAmount = DailyLossMode == ThresholdMode.Percent ? _state.InitialBalance * DailyLossValue / 100.0 : DailyLossValue;
            var bufferAmount = _state.InitialBalance * DailyLossSafetyBufferPercent / 100.0;
            return referenceStart - lossAmount + bufferAmount;
        }

        private double ComputeWeeklyLimit()
        {
            var referenceStart = _state.BalanceAtWeekStart;
            var lossAmount = WeeklyLossMode == ThresholdMode.Percent ? _state.InitialBalance * WeeklyLossValue / 100.0 : WeeklyLossValue;
            var bufferAmount = _state.InitialBalance * WeeklyLossSafetyBufferPercent / 100.0;
            return referenceStart - lossAmount + bufferAmount;
        }

        private double ComputeMaxLossLimit()
        {
            var referenceBase = MaxLossBasis == MaxLossBasisMode.StaticFromInitialCapital ? _state.InitialBalance : _state.OverallHighWaterBalance;
            var lossAmount = MaxLossMode == ThresholdMode.Percent ? _state.InitialBalance * MaxLossValue / 100.0 : MaxLossValue;
            var bufferAmount = _state.InitialBalance * MaxLossSafetyBufferPercent / 100.0;
            return referenceBase - lossAmount + bufferAmount;
        }

        /// <summary>Drawdown intraday corrente: quanto l'equity e' arretrata dal picco di oggi (>= 0).</summary>
        private double ComputeDailyDrawdown() => Math.Max(0, _state.EquityHighWaterDay - Account.Equity);

        /// <summary>Drawdown corrente nella settimana: quanto l'equity e' arretrata dal picco settimanale (>= 0).</summary>
        private double ComputeWeeklyDrawdown() => Math.Max(0, _state.EquityHighWaterWeek - Account.Equity);

        /// <summary>Profitto/perdita netta da inizio giornata (balance di apertura vs equity corrente).</summary>
        private double ComputeDailyProfit() => Account.Equity - _state.BalanceAtDayStart;

        /// <summary>Profitto/perdita netta da inizio settimana (balance di apertura vs equity corrente).</summary>
        private double ComputeWeeklyProfit() => Account.Equity - _state.BalanceAtWeekStart;

        private void CheckDrawdownLimits()
        {
            if (_state == null) return;

            // Account.Equity/Account.Balance sono gia' cross-symbol: includono TUTTE le posizioni aperte
            // sull'account, su qualsiasi simbolo, non solo quelle sul simbolo del grafico a cui il bot e'
            // agganciato. Le guardie sotto quindi proteggono l'intero account, non un singolo simbolo.
            var equity = Account.Equity;
            var balance = Account.Balance;

            if (equity > _state.EquityHighWaterDay) _state.EquityHighWaterDay = equity;
            if (equity > _state.EquityHighWaterWeek) _state.EquityHighWaterWeek = equity;
            if (balance > _state.OverallHighWaterBalance) _state.OverallHighWaterBalance = balance;

            if (DailyLossEnabled && !_state.DailyGuardTriggeredToday)
            {
                var limit = ComputeDailyLimit();
                if (equity <= limit)
                {
                    _state.DailyGuardTriggeredToday = true;
                    CloseAllPositionsAndOrders(string.Format(CultureInfo.InvariantCulture,
                        "MAX DAILY LOSS: equity {0:0.00} <= limite {1:0.00}", equity, limit));
                }
            }

            if (WeeklyLossEnabled && !_state.WeeklyGuardTriggeredThisWeek)
            {
                var limit = ComputeWeeklyLimit();
                if (equity <= limit)
                {
                    _state.WeeklyGuardTriggeredThisWeek = true;
                    CloseAllPositionsAndOrders(string.Format(CultureInfo.InvariantCulture,
                        "MAX WEEKLY LOSS: equity {0:0.00} <= limite {1:0.00}", equity, limit));
                }
            }

            if (MaxLossEnabled && !_state.MaxLossGuardTriggered)
            {
                var limit = ComputeMaxLossLimit();
                if (equity <= limit)
                {
                    _state.MaxLossGuardTriggered = true;
                    CloseAllPositionsAndOrders(string.Format(CultureInfo.InvariantCulture,
                        "MAX LOSS ASSOLUTO: equity {0:0.00} <= limite {1:0.00} — richiede verifica manuale", equity, limit));
                }
            }
        }

        private void CloseAllPositionsAndOrders(string reason)
        {
            Print("[RISK GUARD] {0}", reason);
            if (DryRun)
            {
                Print("[RISK GUARD] Dry run attivo: nessuna chiusura reale eseguita.");
                return;
            }

            // Positions/PendingOrders enumerano TUTTE le posizioni/ordini dell'account: cross-symbol per
            // costruzione, non serve (e non va) filtrare per SymbolName del grafico corrente.
            foreach (var position in Positions.ToArray())
            {
                var result = ClosePosition(position);
                if (!result.IsSuccessful)
                    Print("[RISK GUARD] Errore chiusura posizione {0} ({1}): {2}", position.Id, position.SymbolName, result.Error);
            }

            foreach (var order in PendingOrders.ToArray())
            {
                var result = CancelPendingOrder(order);
                if (!result.IsSuccessful)
                    Print("[RISK GUARD] Errore cancellazione ordine pendente {0} ({1}): {2}", order.Id, order.SymbolName, result.Error);
            }
        }

        // ------------------------------------------------------------------------------- News standby

        private void LoadNewsFileIfNeeded(bool force)
        {
            if (!NewsEnabled) return;

            try
            {
                if (!File.Exists(_resolvedNewsFilePath))
                {
                    if (force) Print("[NEWS] File calendario non trovato: {0} (standby news disattivo finche' non viene creato).", _resolvedNewsFilePath);
                    return;
                }

                var writeTime = File.GetLastWriteTimeUtc(_resolvedNewsFilePath);
                if (!force && _newsFileLastWriteTimeUtc.HasValue && writeTime <= _newsFileLastWriteTimeUtc.Value)
                    return;

                _newsEvents = ParseNewsJson(_resolvedNewsFilePath);
                _newsFileLastWriteTimeUtc = writeTime;
                Print("[NEWS] Calendario ricaricato: {0} eventi da {1}.", _newsEvents.Count, _resolvedNewsFilePath);
            }
            catch (Exception ex)
            {
                Print("[NEWS] Errore lettura calendario news: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Legge il calendario news da un file JSON (array di oggetti, vedi NewsEventFile): usare sempre
        /// TimeUtc con suffisso "Z" per evitare qualunque ambiguita' tra ora locale e ora UTC (a differenza
        /// di un CSV in testo libero, il parser JSON di .NET gestisce il fuso in modo esplicito e coerente
        /// con quello usato per lo stato persistito).
        /// </summary>
        private List<NewsEvent> ParseNewsJson(string path)
        {
            var list = new List<NewsEvent>();

            List<NewsEventFile> items;
            try
            {
                var json = File.ReadAllText(path);
                items = JsonSerializer.Deserialize<List<NewsEventFile>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<NewsEventFile>();
            }
            catch (Exception ex)
            {
                Print("[NEWS] Errore parsing JSON calendario news: {0}", ex.Message);
                return list;
            }

            foreach (var item in items)
            {
                if (item.TimeUtc == default)
                {
                    Print("[NEWS] Evento scartato, TimeUtc mancante o non valido: '{0}'", item.Title);
                    continue;
                }

                // Se il file non avesse il suffisso "Z" (sconsigliato, ma per sicurezza) forziamo comunque
                // il Kind a Utc: e' responsabilita' di chi genera il file scrivere sempre orari in UTC.
                var timeUtc = DateTime.SpecifyKind(item.TimeUtc, DateTimeKind.Utc);

                string[] symbols = null;
                if (item.Symbols != null && item.Symbols.Length > 0)
                {
                    var isAll = item.Symbols.Length == 1 &&
                        (item.Symbols[0].Equals("ALL", StringComparison.OrdinalIgnoreCase) || item.Symbols[0] == "*");
                    symbols = isAll ? null : item.Symbols.Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                }

                if (!Enum.TryParse<NewsImpact>(item.Impact, true, out var impact))
                    impact = NewsImpact.High;

                list.Add(new NewsEvent
                {
                    TimeUtc = timeUtc,
                    Symbols = symbols,
                    Impact = impact,
                    Title = item.Title ?? ""
                });
            }
            return list;
        }

        private void CheckNewsStandby(DateTime nowUtc)
        {
            if (!NewsEnabled)
            {
                _isInStandby = false;
                return;
            }

            if (_flattenedForEventKeys.Count > 200) _flattenedForEventKeys.Clear();

            NewsEvent active = null;
            foreach (var ev in _newsEvents)
            {
                if (ev.Impact < NewsMinImpact) continue;
                var windowStart = ev.TimeUtc.AddMinutes(-NewsMinutesBefore);
                var windowEnd = ev.TimeUtc.AddMinutes(NewsMinutesAfter);
                if (nowUtc >= windowStart && nowUtc <= windowEnd)
                {
                    active = ev;
                    break;
                }
            }

            var wasInStandby = _isInStandby;
            _isInStandby = active != null;

            if (_isInStandby && !wasInStandby)
            {
                Print("[NEWS] Ingresso in standby per '{0}' ({1}, impatto {2}) alle {3:u}.",
                    active.Title, active.Symbols == null ? "ALL" : string.Join("/", active.Symbols), active.Impact, active.TimeUtc);

                if (NewsFlattenPositions && !_flattenedForEventKeys.Contains(active.Key))
                {
                    var toClose = active.Symbols == null
                        ? Positions.ToArray()
                        : Positions.Where(p => active.Symbols.Any(s => s.Equals(p.SymbolName, StringComparison.OrdinalIgnoreCase))).ToArray();

                    if (toClose.Length > 0)
                    {
                        Print("[NEWS] Chiusura di {0} posizioni per standby news.", toClose.Length);
                        if (!DryRun)
                        {
                            foreach (var position in toClose)
                            {
                                var result = ClosePosition(position);
                                if (!result.IsSuccessful) Print("[NEWS] Errore chiusura posizione {0}: {1}", position.Id, result.Error);
                            }
                        }
                    }
                    _flattenedForEventKeys.Add(active.Key);
                }
            }
            else if (!_isInStandby && wasInStandby && VerboseLogging)
            {
                Print("[NEWS] Uscita da standby.");
            }
        }

        // ------------------------------------------------------------------------------- Persistenza stato

        private void ResolvePaths()
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooRiskGuardian");
            Directory.CreateDirectory(baseDir);

            _resolvedStateFilePath = string.IsNullOrWhiteSpace(StateFilePath)
                ? Path.Combine(baseDir, string.Format(CultureInfo.InvariantCulture, "state_{0}.json", Account.Number))
                : StateFilePath;

            _resolvedNewsFilePath = string.IsNullOrWhiteSpace(NewsFilePath)
                ? Path.Combine(baseDir, "news_calendar.json")
                : (Path.IsPathRooted(NewsFilePath) ? NewsFilePath : Path.Combine(baseDir, NewsFilePath));

            Print("[INIT] File stato: {0}", _resolvedStateFilePath);
            Print("[INIT] File news:  {0}", _resolvedNewsFilePath);
        }

        private void LoadOrInitState(DateTime nowUtc)
        {
            try
            {
                if (File.Exists(_resolvedStateFilePath))
                {
                    var json = File.ReadAllText(_resolvedStateFilePath);
                    _state = JsonSerializer.Deserialize<RiskState>(json);
                }
            }
            catch (Exception ex)
            {
                Print("[STATE] Errore lettura stato precedente, ne creo uno nuovo: {0}", ex.Message);
                _state = null;
            }

            if (_state == null)
            {
                var initialBalance = InitialBalanceOverride > 0 ? InitialBalanceOverride : Account.Balance;
                var tradingDay = GetTradingDay(nowUtc);
                _state = new RiskState
                {
                    AccountNumber = Account.Number,
                    InitialBalance = initialBalance,
                    CurrentTradingDay = tradingDay,
                    BalanceAtDayStart = Account.Balance,
                    EquityHighWaterDay = Account.Equity,
                    CurrentTradingWeekStart = GetTradingWeekStart(tradingDay),
                    BalanceAtWeekStart = Account.Balance,
                    EquityHighWaterWeek = Account.Equity,
                    OverallHighWaterBalance = Math.Max(initialBalance, Account.Balance),
                };
                Print("[STATE] Nuovo stato creato. CapitaleIniziale={0:0.00}", initialBalance);
            }
            else
            {
                Print("[STATE] Stato precedente caricato (ultimo aggiornamento {0:u}). BalanceAtDayStart={1:0.00}, BalanceAtWeekStart={2:0.00}, HighWaterBalance={3:0.00}.",
                    _state.LastUpdatedUtc, _state.BalanceAtDayStart, _state.BalanceAtWeekStart, _state.OverallHighWaterBalance);
            }
        }

        private void SaveState()
        {
            if (_state == null) return;
            try
            {
                _state.LastUpdatedUtc = NowUtc();
                var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
                var tmp = _resolvedStateFilePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(_resolvedStateFilePath)) File.Delete(_resolvedStateFilePath);
                File.Move(tmp, _resolvedStateFilePath);
            }
            catch (Exception ex)
            {
                Print("[STATE] Errore salvataggio stato: {0}", ex.Message);
            }
        }

        // ------------------------------------------------------------------------------- Visualizzazione

        /// <summary>
        /// Pannello di stato disegnato sul grafico a cui il bot e' agganciato: drawdown e profit
        /// giornalieri/settimanali, limiti attivi e stato di standby news. Nome e versione del bot NON
        /// sono qui: vengono scritti una volta sola in OnStart (vedi "PiootooRiskGuardianTitle") perche'
        /// non cambiano mai durante l'esecuzione. Questo pannello invece viene richiamato sia dal Timer
        /// (RunChecks) sia da OnTick, cosi' resta aggiornato in tempo reale ad ogni movimento dell'equity,
        /// non solo ogni CheckIntervalSeconds.
        /// </summary>
        private void UpdateChartStatus()
        {
            // In backtest (visuale, silenzioso o optimization) disegnare sul grafico ad ogni tick/timer
            // rallenta inutilmente l'esecuzione e non serve a nessuno: la scrittura sul chart resta attiva
            // solo in live/demo reale (IsBacktesting == false), indipendentemente dal parametro ShowChartStatus.
            if (_state == null || IsBacktesting) return;

            var dailyDrawdown = ComputeDailyDrawdown();
            var weeklyDrawdown = ComputeWeeklyDrawdown();
            var dailyProfit = ComputeDailyProfit();
            var weeklyProfit = ComputeWeeklyProfit();

            var lines = new List<string>
            {
                string.Format(CultureInfo.InvariantCulture, "Equity {0:0.00}  /  Balance {1:0.00}", Account.Equity, Account.Balance),
                string.Format(CultureInfo.InvariantCulture, "Drawdown giornaliero: {0:0.00} ({1:0.00}%)",
                    dailyDrawdown, _state.InitialBalance > 0 ? dailyDrawdown / _state.InitialBalance * 100.0 : 0),
                string.Format(CultureInfo.InvariantCulture, "Drawdown settimanale: {0:0.00} ({1:0.00}%)",
                    weeklyDrawdown, _state.InitialBalance > 0 ? weeklyDrawdown / _state.InitialBalance * 100.0 : 0),
                string.Format(CultureInfo.InvariantCulture, "Profit giornaliero: {0:+0.00;-0.00;0.00} ({1:+0.00;-0.00;0.00}%)",
                    dailyProfit, _state.InitialBalance > 0 ? dailyProfit / _state.InitialBalance * 100.0 : 0),
                string.Format(CultureInfo.InvariantCulture, "Profit settimanale: {0:+0.00;-0.00;0.00} ({1:+0.00;-0.00;0.00}%)",
                    weeklyProfit, _state.InitialBalance > 0 ? weeklyProfit / _state.InitialBalance * 100.0 : 0)
            };

            if (DailyLossEnabled)
                lines.Add(string.Format(CultureInfo.InvariantCulture, "Limite daily loss: {0:0.00}{1}", ComputeDailyLimit(), _state.DailyGuardTriggeredToday ? "  [TRIGGERATO]" : ""));

            if (WeeklyLossEnabled)
                lines.Add(string.Format(CultureInfo.InvariantCulture, "Limite weekly loss: {0:0.00}{1}", ComputeWeeklyLimit(), _state.WeeklyGuardTriggeredThisWeek ? "  [TRIGGERATO]" : ""));

            if (MaxLossEnabled)
                lines.Add(string.Format(CultureInfo.InvariantCulture, "Limite max loss: {0:0.00}{1}", ComputeMaxLossLimit(), _state.MaxLossGuardTriggered ? "  [VIOLATO]" : ""));

            if (NewsEnabled)
                lines.Add(_isInStandby ? "STANDBY NEWS ATTIVO" : "Nessuna news imminente");

            if (DryRun)
                lines.Add("(DRY RUN: nessuna chiusura reale)");

            Chart.DrawStaticText("PiootooRiskGuardianStatus", string.Join("\n", lines),
                VerticalAlignment.Top, HorizontalAlignment.Right,
                _isInStandby || _state.MaxLossGuardTriggered ? Color.OrangeRed : Color.LightGreen);
        }
    }
}
