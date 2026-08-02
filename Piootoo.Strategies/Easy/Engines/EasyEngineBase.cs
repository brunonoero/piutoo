using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Base comune ai motori Unger portati da EasyLanguage.
///
/// <para><b>Perché esiste.</b> Le prime 44 strategie sono state tradotte una per una, a mano, e
/// hanno riprodotto gli stessi errori 40 volte: <c>OrderType</c> mai impostato (ogni
/// <c>next bar at ... stop</c> diventava un market sulla barra corrente), <c>ValidFromUtc</c>
/// assente, stop loss in denaro scritti sul campo che l'engine legge come punti, uscite decise a
/// runtime invece che dichiarate all'ingresso. Questa classe rende quegli errori non
/// esprimibili: un ingresso si costruisce solo passando dai metodi qui sotto, che impongono
/// tipo d'ordine, validità e specifica di uscita completa.</para>
///
/// <para><b>Idempotenza rispetto al contratto.</b> Stop, target e breakeven si dichiarano in
/// <b>denaro sul contratto di riferimento</b>, esattamente come <c>setstopcontract</c> +
/// <c>setstoploss(N)</c> dell'originale. La conversione in punti avviene una volta sola nel
/// confine di esecuzione, usando <c>InstrumentRegistry</c>. La strategia non sa, e non deve
/// sapere, se sta girando su future, mini, micro o CFD: quella differenza la assorbono i punti
/// e il moltiplicatore di quantità dell'account.</para>
/// </summary>
public abstract class EasyEngineBase : StatelessEasyStrategyBase
{
    // ------------------------------------------------------------------ parametri di sessione

    /// <summary>Orario HHMM di inizio sessione (es. 1800 per la sessione GC 18:00–17:00).</summary>
    protected int SessionStartTime = 1800;

    /// <summary>Orario HHMM di fine sessione.</summary>
    protected int SessionEndTime = 1700;

    /// <summary>Contratti dichiarati dalla strategia, prima di sizing e conversione account.</summary>
    protected int Contracts = 1;

    // ------------------------------------------------------------------ specifica di uscita

    /// <summary>Perdita massima in denaro per contratto di riferimento. 0 = nessuno stop.</summary>
    protected int StopMoney;

    /// <summary>Target in denaro per contratto di riferimento. 0 = nessun target.</summary>
    protected int ProfitMoney;

    /// <summary>Soglia di breakeven in denaro per contratto di riferimento. 0 = disattivo.</summary>
    protected int BreakEvenMoney;

    /// <summary>Trailing stop in denaro per contratto di riferimento. 0 = disattivo.</summary>
    protected int TrailingStopMoney;

    /// <summary>
    /// Massimo numero di fill per sessione. 0 = nessun limite. Viene dichiarato sul segnale e
    /// applicato dall'engine al fill, così uno stop non eseguito può essere riemesso.
    /// </summary>
    protected int MaxEntriesPerSession;

    /// <summary>Numero massimo di barre in posizione. 0 = nessun limite.</summary>
    protected int MaxBars;

    /// <summary>
    /// Giorni di calendario massimi in posizione (<c>MaxDaysInTrade</c>). 0 = nessun limite.
    ///
    /// <para>Nell'originale è un contatore incrementato al cambio di giornata e confrontato a ogni
    /// barra; qui diventa una deadline <c>CloseAtUtc</c> calcolata all'ingresso, così l'engine e
    /// il cBot la applicano senza interrogare la strategia.</para>
    /// </summary>
    protected int MaxDaysInTrade;

    /// <summary>
    /// Soglia di utile per contratto sotto la quale eseguire la chiusura a tempo. Null = chiusura
    /// incondizionata. Vedi <c>TradeSignal.TimeExitOnlyIfProfitBelowMoneyPerContract</c>.
    /// </summary>
    protected decimal? TimeExitOnlyIfProfitBelow;

    // ------------------------------------------------------------------ stato per barra

    // I due campi seguenti sono popolati per riflessione da StatelessEasyStrategyBase prima di
    // ogni valutazione, e la ricerca avviene per NOME: devono chiamarsi esattamente così.
    // Sono la lettura autorevole dello stato broker — la strategia non deve mai scriverli.

    /// <summary>Posizione corrente vista dall'engine: +1 long, -1 short, 0 flat.</summary>
    protected int _currentMP;

    /// <summary>Ingressi già eseguiti nella sessione corrente, forniti dall'engine.</summary>
    protected int _entriesToday;

    /// <summary>Alias leggibile di <see cref="_currentMP"/>.</summary>
    protected int CurrentMP => _currentMP;

    /// <summary>Alias leggibile di <see cref="_entriesToday"/>.</summary>
    protected int EntriesTodayCount => _entriesToday;

    // ------------------------------------------------------------------ helper di sessione

    /// <summary>
    /// Ricostruisce gli OHLC di sessione (d0..d5) e dice se la barra corrente apre una sessione
    /// nuova. Wrapper su <see cref="EasyLib.OHLCMulti5"/> con i parametri di sessione del motore.
    /// </summary>
    protected bool BuildSessionOhlc(OhlcvData[] data, DateTime barTime, out decimal[] ohlc) =>
        EasyLib.OHLCMulti5(SessionStartTime, SessionEndTime, data, barTime, out ohlc);

    /// <summary>Orario HHMM della barra.</summary>
    protected static int Hhmm(DateTime barTime) => EasyLib.GetHhmm(barTime);

    /// <summary>
    /// Giorno della settimana nella convenzione EasyLanguage <c>dayofweek()</c>:
    /// 0 = domenica … 6 = sabato. Coincide con <see cref="DayOfWeek"/> di .NET, ma va detto
    /// esplicitamente perché i motori Unger portati dal Python usano invece 0 = lunedì.
    /// </summary>
    protected static int EasyDayOfWeek(DateTime barTime) => (int)barTime.DayOfWeek;

    // ------------------------------------------------------------------ costruttori di segnale

    /// <summary>
    /// Ordine stop valido <b>esclusivamente sulla barra successiva</b>, come
    /// <c>buy next bar at LEVEL stop</c>. Il trigger non viene valutato qui: la strategia
    /// descrive l'intent, l'engine decide il fill (gap-aware, a <c>max(open, livello)</c> per un
    /// long). Questo è il punto in cui la traduzione a mano sbagliava sistematicamente,
    /// verificando il livello sulla barra corrente e degradando l'ordine a market.
    /// </summary>
    protected TradeSignal EntryStopNextBar(
        SignalType side, decimal level, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, level, TradeOrderType.Stop, data, barTime, reason);

    /// <summary>Ordine limit valido solo sulla barra successiva (<c>next bar at LEVEL limit</c>).</summary>
    protected TradeSignal EntryLimitNextBar(
        SignalType side, decimal level, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, level, TradeOrderType.Limit, data, barTime, reason);

    /// <summary>
    /// Ordine a mercato eseguito all'apertura della barra successiva
    /// (<c>next bar at market</c>). Il prezzo indicato è solo di riferimento: l'engine riempie
    /// all'apertura effettiva.
    /// </summary>
    protected TradeSignal EntryMarketNextBar(
        SignalType side, decimal referencePrice, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, referencePrice, TradeOrderType.Market, data, barTime, reason);

    private TradeSignal BuildEntry(
        SignalType side,
        decimal level,
        TradeOrderType orderType,
        OhlcvData[] data,
        DateTime barTime,
        string reason)
    {
        var nextBar = EasyLib.EstimateNextBarUtc(data, barTime);
        var signal = new TradeSignal
        {
            Date = barTime,
            Type = side,
            Price = level,
            StrategyName = Name,
            Quantity = Contracts,
            OrderType = orderType,
            // L'ordine nasce alla chiusura della barra di segnale, vive una sola barra e scade
            // con essa: è la semantica di "next bar" di EasyLanguage, dove l'ordine viene
            // riemesso a ogni barra finché la condizione resta valida.
            ValidFromUtc = nextBar,
            ExpiresAtUtc = nextBar,
            StopLossMoneyPerFutureContract = StopMoney > 0 ? StopMoney : null,
            TakeProfitMoneyPerFutureContract = ProfitMoney > 0 ? ProfitMoney : null,
            BreakEvenMoneyPerFutureContract = BreakEvenMoney > 0 ? BreakEvenMoney : null,
            TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null,
            MaxBarsInPosition = MaxBars > 0 ? MaxBars : null,
            CloseAtUtc = MaxDaysInTrade > 0 ? barTime.Date.AddDays(MaxDaysInTrade) : null,
            TimeExitOnlyIfProfitBelowMoneyPerContract = TimeExitOnlyIfProfitBelow,
            Reason = reason
        };

        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(nextBar);
        }

        return signal;
    }

    /// <summary>
    /// Inizio della sessione di trading che contiene <paramref name="timeUtc"/>. Usa
    /// <see cref="SessionStartTime"/>/<see cref="SessionEndTime"/> del motore, così il limite
    /// di fill per sessione coincide con il calendario dei pattern.
    /// </summary>
    protected virtual DateTime ResolveEntrySessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = EasyLib.CombineDateAndHhmm(timeUtc.Date, SessionStartTime);
        return SessionStartTime > SessionEndTime && timeUtc < sessionStart
            ? sessionStart.AddDays(-1)
            : sessionStart;
    }

    /// <summary>
    /// Unisce due ingressi contemporanei (long e short armati sulla stessa barra) in un segnale
    /// primario con companion. L'engine li tratta come intent indipendenti in OCO: il fill di
    /// uno cancella l'altro.
    /// </summary>
    protected static TradeSignal Combine(List<TradeSignal> entries, TradeSignal fallbackHold)
    {
        if (entries.Count == 0) return fallbackHold;
        var primary = entries[0];
        if (entries.Count > 1)
            primary.CompanionSignals = entries.GetRange(1, entries.Count - 1);
        return primary;
    }

    /// <summary>Segnale neutro.</summary>
    protected TradeSignal Hold(decimal price, DateTime barTime, string? reason = null) =>
        new()
        {
            Date = barTime,
            Type = SignalType.Hold,
            Price = price,
            StrategyName = Name,
            Reason = reason
        };

    /// <summary>
    /// Deadline di chiusura a un orario HHMM, risolta sul calendario della sessione corrente.
    /// Serve a esprimere <c>setexitonclose</c> e le uscite di fine sessione come
    /// <c>CloseAtUtc</c> sull'ingresso, invece che come segnale di chiusura a runtime — che in
    /// <c>ExternalBroker</c> non verrebbe mai eseguito, perché il server emette solo intent di
    /// ingresso.
    /// </summary>
    protected static DateTime ResolveCloseAtUtc(DateTime barTime, int hhmm)
    {
        var target = EasyLib.CombineDateAndHhmm(barTime.Date, hhmm);
        return target <= barTime ? target.AddDays(1) : target;
    }

    // ------------------------------------------------------------------ contratto ITradingStrategy

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Symbol { get; }
    public abstract int TimeframeMinutes { get; }

    /// <summary>
    /// Barre di storia necessarie. Il default copre sei sessioni piene al timeframe dichiarato,
    /// perché <see cref="EasyLib.OHLCMulti5"/> ricostruisce d0..d5 dalla sola finestra ricevuta e
    /// riparte da zero a ogni valutazione: con una finestra più corta le sessioni più vecchie
    /// risultano troncate e i pattern che leggono d4/d5 lavorano su valori parziali.
    /// </summary>
    public virtual int RequiredCandles => SessionsToCandles(6);

    /// <summary>Barre necessarie a coprire <paramref name="sessions"/> sessioni piene.</summary>
    protected int SessionsToCandles(int sessions)
    {
        var barsPerDay = Math.Max(1, 1440 / Math.Max(1, TimeframeMinutes));
        return sessions * barsPerDay;
    }

    /// <summary>
    /// Orario UTC della barra numero <paramref name="sessionBarIndex"/> (1-based) della sessione
    /// che contiene <paramref name="barTime"/>.
    ///
    /// <para><b>Limite noto e voluto.</b> L'originale EasyLanguage conta <i>barre</i>, non
    /// orologio, su un grafico TradeStation continuo. Il feed Piootoo ha buchi (la pausa CME, i
    /// giorni corti), quindi indice di barra e orario divergono. Proiettare sull'orologio è
    /// deterministico e non dipende da quante barre il feed ha effettivamente consegnato: è la
    /// scelta più stabile delle due, ma va sapendo che su una sessione con barre mancanti la
    /// chiusura cade sull'orario atteso, non sulla N-esima barra ricevuta.</para>
    /// </summary>
    protected DateTime SessionBarToUtc(DateTime barTime, int sessionBarIndex)
    {
        var offsetMinutes = (sessionBarIndex - 1) * TimeframeMinutes;
        var sessionStart = EasyLib.CombineDateAndHhmm(barTime.Date, SessionStartTime);

        // Sessione che attraversa la mezzanotte: se la barra corrente è prima dell'orario di
        // apertura, la sessione in corso è iniziata il giorno precedente.
        if (SessionStartTime > SessionEndTime && Hhmm(barTime) < SessionStartTime)
            sessionStart = sessionStart.AddDays(-1);

        var target = sessionStart.AddMinutes(offsetMinutes);
        return target <= barTime ? target.AddDays(1) : target;
    }
}
