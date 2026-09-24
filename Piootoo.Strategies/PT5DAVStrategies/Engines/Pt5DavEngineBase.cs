using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// Base comune dei motori PT5DAV: le regole che la ricerca v5.0 (consegna del 23/09/2026,
/// <c>piootoo-repository/PT5DAV/CONVENZIONI.md</c>) applica a tutte le 224 strategie e che i motori
/// condivisi con le PT3B non hanno.
///
/// <para><b>Perche' motori propri e non quelli di <c>Easy/Engines</c>.</b> Formule dei livelli,
/// segnali, tipi d'ordine e pattern sono gli stessi — e i pattern vengono da <c>EasyLib</c>, non
/// sono riscritti. Cambiano pero' cose che nei motori condivisi non si possono dichiarare senza
/// modificarli, e i motori condivisi non si toccano (decisione del 24/09/2026):</para>
/// <list type="bullet">
///   <item>stop, target e offset dei livelli in multipli di <b>ATR50 di Wilder</b> sulle sessioni
///   chiuse — i motori condivisi hanno un ATR a 14 sessioni a media semplice, costante;</item>
///   <item><b>rodaggio</b>: nessun ingresso finche' l'ATR50 non esiste, invece del ripiego sul
///   denaro fisso (che qui vale zero, cioe' un ingresso senza stop);</item>
///   <item>la <b>sessione della ricerca</b>: le barre della domenica sera stanno nella sessione del
///   lunedi' (misurato sui trade BIAS: gli indici di barra del lunedi' sono spostati), e il DAX esiste
///   solo fra le 08:00 e le 22:00 con la sessione che parte alle 08:00;</item>
///   <item>l'<b>uscita intraday al limite del CFD</b>, un orario di New York: alla chiusura
///   dell'ultima barra che finisce entro le 16:50 NY (17:00 per BP e BTC, 13:30 per CC e KC), non a
///   fine sessione della ricerca;</item>
///   <item><b>nessun ingresso a CFD chiuso</b>, con gli orari misurati sul broker.</item>
/// </list>
///
/// <para><b>Cosa resta uguale, di proposito.</b> L'etichetta della barra: la ricerca v5.0 etichetta le
/// candele sull'apertura ma scrive gli orari delle regole sulla <b>chiusura</b> ("ordini emessi sulle
/// barre che chiudono fra le 11:00 e le 13:00") — cioe' esattamente il default del sistema,
/// <see cref="EasyEngineBase.ResearchLabelsBarsOnOpen"/> a <c>false</c>. Misurato sui trade: con
/// finestra 11-13 su BP 15m gli ingressi vanno dalla barra delle 11:00 a quella delle 13:00 incluse.
/// Gli orari si riportano verbatim, come sempre.</para>
///
/// <para><b>Stato.</b> OHLC di sessione e ATR50 vivono in <see cref="Pt5DavSessionState"/>, che
/// viaggia fra le valutazioni come <c>RuntimeState</c> (in backtest, in sweep e in sessione
/// <c>ExternalBroker</c>). Quando manca si ricostruisce dalla finestra ricevuta: la media di Wilder
/// riparte allora da una media semplice delle prime 50 sessioni della finestra, ed e' meno precisa
/// finche' non ha visto altre sessioni — il motivo di <see cref="WarmupSessions"/>.</para>
/// </summary>
public abstract class Pt5DavEngineBase : EasyEngineBase
{
    /// <summary>Sessioni dell'ATR della ricerca: 50, media di Wilder, range vero.</summary>
    public const int AtrPeriod = 50;

    /// <summary>
    /// Sessioni di storia chieste a ogni valutazione (<see cref="RequiredCandles"/>). Devono essere
    /// piu' di <see cref="AtrPeriod"/>, perche' la prima sessione della finestra e' quasi sempre
    /// parziale e non entra nell'ATR; il resto serve a far convergere la media quando lo stato va
    /// ricostruito.
    ///
    /// <para><b>Perche' 60 e non di piu'.</b> A 15 minuti sono 5.760 barre, e il backtest ne copia
    /// 1,2 volte tante a ogni barra: oltre questa soglia l'array finisce nel Large Object Heap e il
    /// costo del run esplode. Lo stato incrementale rende inutile di piu' in backtest; in sessione
    /// conta solo al primo avvio e dopo un riavvio del server.</para>
    /// </summary>
    public const int WarmupSessions = 60;

    /// <summary>Il codice della strategia nella consegna PT5DAV (es. <c>NQ-15M-BOS-f7179d</c>).</summary>
    public abstract string ResearchCode { get; }

    /// <summary>Stop in multipli di ATR50 dal prezzo d'ingresso (<c>stop_atr</c>). 0 = nessuno stop.</summary>
    protected decimal StopAtr;

    /// <summary>Target in multipli di ATR50 dal prezzo d'ingresso (<c>take_profit_atr</c>). 0 = nessun target.</summary>
    protected decimal TargetAtr;

    /// <summary>Giorno escluso per entrambi i lati, pandas 0 = lunedi' (<c>skip_day</c>). -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>Giorno escluso per il long, pandas (<c>not_le_day</c>). -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno escluso per lo short, pandas (<c>not_se_day</c>). -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    private Pt5DavSessionState _pt5;
    private SessionClock? _newYork;

    protected Pt5DavEngineBase()
    {
        // La fascia del DAX della ricerca v5.0 sposta anche l'inizio della sessione: d0, d1, i
        // pattern e l'ATR sono calcolati sulla giornata 08:00-22:00, non su quella del calendario
        // (01:00), con cui sono state trovate le PT3B su FDAX.
        if (Pt5DavMarket.ResearchMarketHours(Symbol) is { } hours)
        {
            OverrideSessionAnchor(
                hours.Opens,
                "PT5DAV v5.0: la ricerca taglia il DAX alla fascia 08:00-22:00 e fa partire la " +
                "sessione alle 08:00 (piootoo-repository/PT5DAV/ORARIO_MERCATO.md)");
        }
    }

    /// <inheritdoc />
    public override int RequiredCandles => SessionsToCandles(WarmupSessions);

    /// <summary>
    /// La tenuta la dichiara <c>intraday_only</c>: intraday chiude al limite del CFD, altrimenti la
    /// strategia tiene al massimo una notte (vedi <see cref="Finish"/>). Il piano resta sopra.
    /// </summary>
    public override StrategyHolding Holding =>
        IntradayOnly ? StrategyHolding.Intraday : StrategyHolding.Multiday;

    /// <summary>
    /// Le PT5DAV non leggono parametri a runtime: sono configurazioni della ricerca, riportate
    /// verbatim nel costruttore. Il metodo esiste perche' il catalogo lo cerca per riflessione.
    /// </summary>
    public void Initialize(Dictionary<string, object>? parameters = null)
    {
    }

    // ------------------------------------------------------------------ valutazione

    /// <summary>
    /// Aggiorna lo stato di sessione con le barre nuove della finestra e, se la barra corrente e'
    /// operabile, passa la valutazione al motore.
    /// </summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length == 0)
            return Hold(0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        Advance(data);

        if (!_pt5.LastBarInMarket)
            return Hold(bar.Close, bar.DateTime, "Barra fuori dalla fascia di mercato della ricerca");
        if (_pt5.SessionsClosed < AtrPeriod)
            return Hold(bar.Close, bar.DateTime, "Rodaggio: ATR50 non ancora disponibile");

        return Evaluate(data, bar, BuildOhlc());
    }

    /// <summary>
    /// La logica del motore sulla barra appena chiusa. <paramref name="ohlc"/> ha il layout di
    /// <c>EasyLib.OHLCMulti5</c> — d0 (sessione in corso, barra inclusa) e d1..d5 — cosi' i pattern
    /// di <c>EasyLib</c> si usano senza adattarli.
    /// </summary>
    protected abstract TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc);

    // ------------------------------------------------------------------ lettura dello stato

    /// <summary>Indice della barra corrente nella sessione, da 0.</summary>
    protected int SessionBarIndex => _pt5.CurrentBarIndex;

    /// <summary>Massimo e minimo della sessione in corso, barra corrente inclusa.</summary>
    protected (decimal High, decimal Low) CurrentSessionRange => (_pt5.H0, _pt5.L0);

    /// <summary>
    /// Massimo e minimo della sessione in corso <b>esclusa</b> la barra corrente; <c>null</c> se la
    /// barra corrente e' la prima della sessione.
    /// </summary>
    protected (decimal High, decimal Low)? CurrentSessionRangeBeforeBar =>
        _pt5.HasBarBeforeInSession ? (_pt5.H0BeforeBar, _pt5.L0BeforeBar) : null;

    /// <summary>Close della barra precedente (anche della sessione precedente); <c>null</c> se non c'e'.</summary>
    protected decimal? PreviousBarClose => _pt5.HasPreviousBar ? _pt5.PreviousBarClose : null;

    /// <summary>
    /// Massimo e minimo delle ultime <paramref name="sessions"/> sessioni chiuse (1..5).
    /// </summary>
    protected (decimal High, decimal Low) ClosedSessionsRange(int sessions)
    {
        var high = _pt5.H1;
        var low = _pt5.L1;
        if (sessions >= 2) { high = Math.Max(high, _pt5.H2); low = Math.Min(low, _pt5.L2); }
        if (sessions >= 3) { high = Math.Max(high, _pt5.H3); low = Math.Min(low, _pt5.L3); }
        if (sessions >= 4) { high = Math.Max(high, _pt5.H4); low = Math.Min(low, _pt5.L4); }
        if (sessions >= 5) { high = Math.Max(high, _pt5.H5); low = Math.Min(low, _pt5.L5); }
        return (high, low);
    }

    /// <summary>
    /// ATR50 della sessione in corso: le 50 sessioni chiuse prima di essa. E' quello con cui la
    /// ricerca sposta i <b>livelli</b>, fissi per la sessione.
    /// </summary>
    protected decimal SessionAtr => _pt5.Atr;

    /// <summary>
    /// ATR50 della sessione in cui l'ordine si riempirebbe, cioè quello con cui la ricerca misura
    /// <b>stop e target</b> ("vale quella della sessione d'ingresso per tutto il trade"). Coincide con
    /// <see cref="SessionAtr"/> salvo quando la barra di segnale e' l'ultima della sessione: allora
    /// la sessione in corso si chiude prima dell'ingresso ed entra nella media.
    /// </summary>
    protected decimal? EntrySessionAtr(DateTime fillBarUtc)
    {
        if (ResearchSessionDay(fillBarUtc) == _pt5.CurrentDay || _pt5.CurrentIsPartial)
            return _pt5.SessionsClosed >= AtrPeriod ? _pt5.Atr : null;

        var trueRange = TrueRange(_pt5.H0, _pt5.L0, _pt5.HasPreviousSessionClose ? _pt5.PreviousSessionClose : null);
        var (count, _, atr) = AddTrueRange(_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr, trueRange);
        return count >= AtrPeriod ? atr : null;
    }

    /// <summary>
    /// Massimo e minimo delle ultime <paramref name="bars"/> barre di mercato, barra corrente
    /// inclusa. Sul DAX le barre fuori fascia non esistono per la ricerca e si saltano; per gli
    /// altri simboli sono semplicemente le ultime barre.
    /// </summary>
    protected (decimal High, decimal Low) RecentBarsRange(OhlcvData[] data, int bars)
    {
        var high = decimal.MinValue;
        var low = decimal.MaxValue;
        var taken = 0;
        for (var index = data.Length - 1; index >= 0 && taken < bars; index--)
        {
            if (!InResearchMarket(data[index].DateTime))
                continue;

            high = Math.Max(high, data[index].High);
            low = Math.Min(low, data[index].Low);
            taken++;
        }

        return (high, low);
    }

    /// <summary>
    /// Le ultime <paramref name="count"/> barre di mercato, in ordine cronologico, a partire da
    /// <paramref name="barsAgo"/> barre prima della corrente. <c>null</c> se la finestra non basta.
    /// </summary>
    protected OhlcvData[]? RecentMarketBars(OhlcvData[] data, int count, int barsAgo = 0)
    {
        var result = new OhlcvData[count];
        var skipped = 0;
        var filled = count;
        for (var index = data.Length - 1; index >= 0 && filled > 0; index--)
        {
            if (!InResearchMarket(data[index].DateTime))
                continue;
            if (skipped < barsAgo)
            {
                skipped++;
                continue;
            }

            result[--filled] = data[index];
        }

        return filled == 0 ? result : null;
    }

    // ------------------------------------------------------------------ finestra e calendario

    /// <summary>
    /// La finestra operativa dichiarata, estremi inclusi, letta sulla chiusura della barra. Una
    /// PT5DAV la dichiara sempre, anche a giornata piena.
    /// </summary>
    protected bool InTradingWindow(DateTime barTime) => InDeclaredWindow(barTime) ?? true;

    /// <summary>Il giorno pandas della barra (0 = lunedi'), letto sulla chiusura come la ricerca.</summary>
    protected bool IsExcludedDay(DateTime barTime, int excludedDay) =>
        excludedDay >= 0 && PythonWeekday(barTime) == excludedDay;

    /// <summary>
    /// Finestra della ricerca da <c>start_hour</c>/<c>end_hour</c> verbatim; -1 = nessun limite da
    /// quel lato (e -1/-1 = tutta la giornata).
    /// </summary>
    protected static ZonedWindow ResearchWindow(int startHour, int endHour) =>
        ZonedWindow.Research(
            ResearchHourOrOff(startHour, TimeOnly.MinValue),
            ResearchHourOrOff(endHour, ZonedWindow.EndOfDay));

    // ------------------------------------------------------------------ chiusura del segnale

    /// <summary>
    /// Completa un ingresso con le regole comuni della ricerca. <c>null</c> = l'ingresso non nasce.
    /// <list type="bullet">
    ///   <item>nessun ordine vivo a CFD chiuso, ne' fuori dalla fascia del DAX;</item>
    ///   <item>stop e target in ATR50 della sessione d'ingresso, in denaro per contratto come ogni
    ///   altro segnale — chi esegue non sa, e non deve sapere, da dove vengono;</item>
    ///   <item>una entrata per sessione e per direzione, se il motore la prevede, applicata al fill;</item>
    ///   <item>intraday: chiusura all'ultima barra entro il limite del CFD. Un ordine che si
    ///   riempirebbe proprio su quella barra non nasce; uno che si riempie dopo (una 4h delle 20:00,
    ///   la riapertura delle 18:05 NY nelle settimane in cui l'ora legale non e' allineata) chiude a
    ///   fine sessione, come nei trade della ricerca;</item>
    ///   <item>overnight: al massimo una notte (decisione del 24/09/2026, le 135 strategie
    ///   compatibili): chiusura al limite del CFD della sessione seguente, oltre al
    ///   <c>max_bars</c> della ricerca.</item>
    /// </list>
    /// </summary>
    protected TradeSignal? Finish(TradeSignal? signal, bool oneEntryPerSessionPerSide)
    {
        if (signal is null)
            return null;

        // Un ordine emesso mentre si e' in posizione non esiste: la ricerca non rientra mai sulla
        // barra in cui e' uscita (0 casi sui trade), al piu' su quella dopo. Senza questo blocco
        // l'ordine si riempirebbe nella barra stessa in cui lo stop chiude la posizione — misurato
        // su NQ-15M-BOS: il 21/04/2022 short aperto nello stesso quarto d'ora dello stop del long.
        if (CurrentMP != 0)
            return null;

        // Nessun ingresso a CFD chiuso, in due condizioni misurate sui trade della ricerca:
        //  - per tutte, il CFD deve essere aperto all'APERTURA della barra d'ingresso: la prima 4h
        //    di NQ (00:00 Roma = 18:00 NY, prima delle 18:05) non ha mai ingressi, salvo le
        //    settimane di ora legale sfasata in cui apre alle 19:00 NY;
        //  - per le intraday, anche alla CHIUSURA: una posizione intraday non sta a mercato mentre
        //    il CFD e' chiuso. La 4h delle 20:00 di Roma chiude alle 18:00 NY: NQ-4H-RBBU non vi
        //    entra mai (salvo le 20 volte, tutte a ora legale sfasata, in cui chiude alle 19:00 NY),
        //    NQ-4H-TFM, che tiene la notte, si' (47 ingressi).
        var fill = signal.ValidFromUtc!.Value;
        var hours = Pt5DavMarket.Hours(Symbol);
        if (!hours.IsOpenAt(NewYork.TimeOfDay(fill)) || !InResearchMarket(fill))
            return null;
        if (IntradayOnly && !hours.IsOpenAt(NewYork.TimeOfDay(fill.AddMinutes(TimeframeMinutes))))
            return null;

        var atr = EntrySessionAtr(fill);
        if (atr is not > 0m)
            return null;

        var pointValue = InstrumentRegistry.PointValue(Symbol);
        signal.StopLoss = null;
        signal.TakeProfit = null;
        signal.StopLossMoneyPerFutureContract =
            StopAtr > 0m ? Math.Round(atr.Value * pointValue * StopAtr, 2) : null;
        signal.TakeProfitMoneyPerFutureContract =
            TargetAtr > 0m ? Math.Round(atr.Value * pointValue * TargetAtr, 2) : null;

        var day = ResearchSessionDay(fill);
        if (oneEntryPerSessionPerSide)
        {
            signal.MaxEntriesPerSession = 1;
            signal.EntrySessionStartUtc = SessionOpenUtc(day);
        }

        var exit = IntradayExitUtc(day);
        if (IntradayOnly)
        {
            if (fill.AddMinutes(TimeframeMinutes) == exit)
                return null;

            signal.CloseAtUtc = (fill < exit ? exit : SessionEndUtc(day)).AddMinutes(-1);
        }
        else
        {
            signal.CloseAtUtc = IntradayExitUtc(NextResearchDay(day)).AddMinutes(-1);
        }

        // max_bars della ricerca NON conta la barra d'ingresso: con max_bars = 46 si esce alla
        // chiusura della 46a barra dopo quella d'ingresso. MaxBarsInPosition la conta, quindi +1.
        // Misurato sulla riconciliazione NQ: senza, 105 uscite su 116 (NQ-30M-LF) e 9 su 9
        // (NQ-4H-LFHL) cadevano esattamente una barra prima.
        signal.MaxBarsInPosition = MaxBars > 0 ? MaxBars + 1 : null;
        return signal;
    }

    // ------------------------------------------------------------------ sessioni della ricerca

    /// <summary>
    /// Il giorno di sessione della ricerca per una barra: la data di Roma dall'ancoraggio, con
    /// sabato e domenica accodati al lunedi'.
    /// </summary>
    protected DateTime ResearchSessionDay(DateTime barUtc)
    {
        var shifted = barUtc - SessionStart.ToTimeSpan();
        var day = Clock.SessionDay(shifted);
        return Clock.SessionDay(shifted).DayOfWeek switch
        {
            DayOfWeek.Saturday => day.AddDays(2),
            DayOfWeek.Sunday => day.AddDays(1),
            _ => day
        };
    }

    /// <summary>
    /// Il giorno di sessione successivo, saltando il fine settimana. Il giorno e' gia' un giorno di
    /// sessione (una data di Roma), non l'istante di una barra: il giorno della settimana si ricava
    /// per differenza da un lunedi' noto.
    /// </summary>
    protected static DateTime NextResearchDay(DateTime day)
    {
        var next = day.Date.AddDays(1);
        while (DaysSinceMonday(next) >= 5)
            next = next.AddDays(1);
        return next;
    }

    private static readonly DateTime KnownMonday = new(2000, 1, 3);

    /// <summary>Giorno della settimana di un giorno di sessione, pandas: 0 = lunedi' … 6 = domenica.</summary>
    protected static int DaysSinceMonday(DateTime day) =>
        (int)(((day.Date - KnownMonday).Days % 7 + 7) % 7);

    /// <summary>Apertura della sessione del giorno indicato (ancoraggio, ora di Roma).</summary>
    protected DateTime SessionOpenUtc(DateTime day) => Clock.ToUtc(day.Add(SessionStart.ToTimeSpan()));

    /// <summary>Fine della sessione: l'ancoraggio del giorno dopo, o la chiusura della fascia del DAX.</summary>
    protected DateTime SessionEndUtc(DateTime day) =>
        Pt5DavMarket.ResearchMarketHours(Symbol) is { } hours
            ? Clock.ToUtc(day.Add(hours.Closes.ToTimeSpan()))
            : Clock.ToUtc(day.AddDays(1).Add(SessionStart.ToTimeSpan()));

    /// <summary>
    /// Fine dell'ultima barra della sessione che chiude entro il limite intraday del CFD (ora di New
    /// York dello stesso giorno), o la fine della sessione se arriva prima (il DAX alle 22:00). La
    /// griglia delle barre e' quella della sessione della ricerca, ancorata in ora di Roma.
    /// </summary>
    protected DateTime IntradayExitUtc(DateTime day)
    {
        var limitUtc = NewYork.ToUtc(day.Add(Pt5DavMarket.Hours(Symbol).IntradayLimit.ToTimeSpan()));
        var openLocal = day.Add(SessionStart.ToTimeSpan());
        var limitLocal = Clock.ToSessionTime(limitUtc);
        var bars = Math.Floor((limitLocal - openLocal).TotalMinutes / TimeframeMinutes);
        var exitUtc = Clock.ToUtc(openLocal.AddMinutes(bars * TimeframeMinutes));
        var end = SessionEndUtc(day);
        return exitUtc < end ? exitUtc : end;
    }

    /// <summary>Vero se la barra che apre in <paramref name="barUtc"/> sta nella fascia di mercato della ricerca.</summary>
    protected bool InResearchMarket(DateTime barUtc)
    {
        if (Pt5DavMarket.ResearchMarketHours(Symbol) is not { } hours)
            return true;

        var time = Clock.TimeOfDay(barUtc);
        return time >= hours.Opens && time < hours.Closes;
    }

    private SessionClock NewYork => _newYork ??= new SessionClock(Pt5DavMarket.NewYorkTimeZone);

    // ------------------------------------------------------------------ aggiornamento dello stato

    private void Advance(OhlcvData[] data)
    {
        var start = 0;
        if (_pt5.LastBarUtc == default || data[0].DateTime > _pt5.LastBarUtc)
        {
            // Stato assente, o finestra che non contiene piu' l'ultima barra vista: si ricostruisce.
            _pt5 = default;
        }
        else
        {
            start = data.Length;
            while (start > 0 && data[start - 1].DateTime > _pt5.LastBarUtc)
                start--;
        }

        for (var index = start; index < data.Length; index++)
            Fold(data[index]);
    }

    private void Fold(OhlcvData bar)
    {
        _pt5.LastBarUtc = bar.DateTime;
        if (!InResearchMarket(bar.DateTime))
        {
            _pt5.LastBarInMarket = false;
            return;
        }

        _pt5.LastBarInMarket = true;
        var day = ResearchSessionDay(bar.DateTime);

        if (_pt5.CurrentDay == default)
        {
            StartSession(day, bar, partial: true);
            return;
        }

        _pt5.PreviousBarClose = _pt5.C0;
        _pt5.HasPreviousBar = true;

        if (day != _pt5.CurrentDay)
        {
            CloseSession();
            StartSession(day, bar, partial: false);
            return;
        }

        _pt5.H0BeforeBar = _pt5.H0;
        _pt5.L0BeforeBar = _pt5.L0;
        _pt5.HasBarBeforeInSession = true;
        _pt5.H0 = Math.Max(_pt5.H0, bar.High);
        _pt5.L0 = Math.Min(_pt5.L0, bar.Low);
        _pt5.C0 = bar.Close;
        _pt5.CurrentBarIndex++;
    }

    private void StartSession(DateTime day, OhlcvData bar, bool partial)
    {
        _pt5.CurrentDay = day;
        _pt5.CurrentIsPartial = partial;
        _pt5.CurrentBarIndex = 0;
        _pt5.O0 = bar.Open;
        _pt5.H0 = bar.High;
        _pt5.L0 = bar.Low;
        _pt5.C0 = bar.Close;
        _pt5.H0BeforeBar = 0m;
        _pt5.L0BeforeBar = 0m;
        _pt5.HasBarBeforeInSession = false;
    }

    private void CloseSession()
    {
        // Una sessione cominciata prima della finestra ha OHLC parziali: non entra nell'ATR, ma la
        // sua chiusura e' vera e serve al range vero della sessione dopo.
        if (!_pt5.CurrentIsPartial)
        {
            var trueRange = TrueRange(_pt5.H0, _pt5.L0,
                _pt5.HasPreviousSessionClose ? _pt5.PreviousSessionClose : null);
            (_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr) =
                AddTrueRange(_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr, trueRange);
        }

        _pt5.PreviousSessionClose = _pt5.C0;
        _pt5.HasPreviousSessionClose = true;

        (_pt5.O5, _pt5.H5, _pt5.L5, _pt5.C5) = (_pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4);
        (_pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4) = (_pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3);
        (_pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3) = (_pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2);
        (_pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2) = (_pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1);
        (_pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1) = (_pt5.O0, _pt5.H0, _pt5.L0, _pt5.C0);
        _pt5.SessionsInHistory = Math.Min(5, _pt5.SessionsInHistory + 1);
    }

    private static decimal TrueRange(decimal high, decimal low, decimal? previousClose) =>
        previousClose is { } close
            ? Math.Max(high - low, Math.Max(Math.Abs(high - close), Math.Abs(low - close)))
            : high - low;

    /// <summary>
    /// Wilder: media semplice dei primi <see cref="AtrPeriod"/> range veri, poi
    /// <c>atr += (tr − atr) / 50</c>. Sulla storia lunga il seme non si distingue piu' da
    /// <c>ewm(alpha=1/50, adjust=False)</c>: misurato sui 6.728 trade SL di NQ, mediana
    /// distanza di stop / (stop_atr × ATR50) = 1,0045 con entrambi (lo scarto e' lo slippage).
    /// </summary>
    private static (int Count, decimal Sum, decimal Atr) AddTrueRange(
        int count, decimal sum, decimal atr, decimal trueRange)
    {
        count++;
        if (count <= AtrPeriod)
        {
            sum += trueRange;
            if (count == AtrPeriod)
                atr = sum / AtrPeriod;
        }
        else
        {
            atr += (trueRange - atr) / AtrPeriod;
        }

        return (count, sum, atr);
    }

    private decimal[] BuildOhlc() =>
    [
        _pt5.O0, _pt5.H0, _pt5.L0, _pt5.C0,
        _pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1,
        _pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2,
        _pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3,
        _pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4,
        _pt5.O5, _pt5.H5, _pt5.L5, _pt5.C5
    ];
}
