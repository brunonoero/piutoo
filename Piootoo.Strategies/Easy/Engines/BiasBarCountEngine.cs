using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>Libreria pattern usata dal motore per i gate di armamento.</summary>
public enum EasyPatternLibrary
{
    /// <summary><c>PatternFast</c> — 152 pattern.</summary>
    Fast,

    /// <summary>
    /// <c>PtnBaseSA</c> / <c>PtnBaseSA2</c> — 43 pattern, formule identiche fra le due.
    /// L'originale <c>PtnBaseSA</c> legge gli OHLC di sessione dal grafico TradeStation
    /// (<c>OpenS/HighS/LowS/CloseS</c>), <c>PtnBaseSA2</c> li riceve dall'array di
    /// <c>_OHLCMulti5</c>. Qui la sorgente è sempre l'array, quindi le due coincidono a patto
    /// che i parametri di sessione del motore siano quelli del grafico originale.
    /// </summary>
    BaseSA
}

/// <summary>Come si entra, una volta che il gate ha armato la direzione.</summary>
public enum BiasEntryType
{
    /// <summary>Mercato alla barra di armamento (<c>entrytype = 1</c>).</summary>
    MarketOnArmBar = 1,

    /// <summary>Stop sul massimo/minimo delle ultime N barre, dentro la finestra (<c>entrytype = 2</c>).</summary>
    BreakoutStop = 2,

    /// <summary>Limit sul minimo/massimo delle ultime N barre, dentro la finestra (<c>entrytype = 3</c>).</summary>
    RetracementLimit = 3
}

/// <summary>
/// Motore BIAS a conteggio barre.
///
/// <para>Ogni decisione è ancorata all'<b>indice della barra dentro la sessione</b>
/// (<c>mycount</c>, azzerato a ogni apertura). A una barra prefissata un gate di pattern arma la
/// direzione; l'ingresso avviene a mercato su quella stessa barra oppure, per i tipi 2 e 3,
/// dentro una finestra di barre come stop o limit riemesso a ogni barra. L'uscita è a un altro
/// indice di barra.</para>
///
/// <para>Copre le varianti BIAS standard a conteggio barre e quelle con filtri proprietari.
/// Le ultime usano gli hook protetti <see cref="UsesCustomEntryRules"/> e
/// <see cref="AddCustomEntries"/>: i relativi adattatori dichiarano esplicitamente i parametri
/// della sorgente senza duplicare la costruzione di ordini e uscite.</para>
///
/// <para><b>Cosa cambia rispetto alla traduzione a mano.</b> Gli ingressi 2 e 3 tornano a essere
/// veri ordini <c>next bar ... stop/limit</c> valutati dall'engine, invece di un confronto
/// <c>close &gt;= highest(high,N)</c> sulla barra corrente — che era vero quasi solo quando il
/// close coincideva col massimo, e riduceva drasticamente il numero di ingressi. Le uscite a
/// indice di barra sono dichiarate come <c>CloseAtUtc</c> sul segnale d'ingresso invece che
/// emesse a runtime, così sopravvivono anche in <c>ExternalBroker</c>, dove il server emette
/// solo intent di ingresso. E l'ingresso non è più bloccato quando esiste una posizione opposta:
/// l'originale in quel caso inverte, e l'engine Piootoo sa farlo.</para>
/// </summary>
public abstract class BiasBarCountEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ parametri del motore

    /// <summary>Indice di barra in cui si arma il long (<c>MyLEBar</c>).</summary>
    protected int ArmBarLong = 16;

    /// <summary>Indice di barra in cui si arma lo short (<c>MySEBar</c>).</summary>
    protected int ArmBarShort = 8;

    /// <summary>Indice di barra dell'uscita long (<c>MyLXBar</c>). 0 = nessuna uscita a tempo.</summary>
    protected int ExitBarLong;

    /// <summary>Indice di barra dell'uscita short (<c>MySXBar</c>). 0 = nessuna uscita a tempo.</summary>
    protected int ExitBarShort;

    /// <summary>Fine della finestra di ingresso long (<c>endlong</c>), usata da <c>twBars</c>.</summary>
    protected int EndLong = 8;

    /// <summary>Fine della finestra di ingresso short (<c>endshort</c>).</summary>
    protected int EndShort = 16;

    /// <summary>Pattern richiesto per armare il long (<c>MyPtnLY</c>).</summary>
    protected int PatternLongYes = 55;

    /// <summary>Pattern che impedisce il long (<c>MyPtnLN</c>).</summary>
    protected int PatternLongNo = 56;

    /// <summary>Pattern richiesto per armare lo short (<c>MyPtnSY</c>).</summary>
    protected int PatternShortYes = 55;

    /// <summary>Pattern che impedisce lo short (<c>MyPtnSN</c>).</summary>
    protected int PatternShortNo = 56;

    /// <summary>Giorno della settimana escluso per il long, convenzione Python/pandas 0 = lunedì.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno della settimana escluso per lo short, convenzione Python/pandas 0 = lunedì.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Barre su cui calcolare il massimo per il breakout long (<c>NHigh</c>).</summary>
    protected int BreakoutBarsHigh = 3;

    /// <summary>Barre su cui calcolare il minimo per il breakout short (<c>NLow</c>).</summary>
    protected int BreakoutBarsLow = 1;

    /// <summary>Tipo di ingresso (<c>entrytype</c>).</summary>
    protected BiasEntryType EntryType = BiasEntryType.BreakoutStop;

    /// <summary>Libreria pattern dei gate di armamento.</summary>
    protected EasyPatternLibrary PatternLibrary = EasyPatternLibrary.Fast;

    /// <summary>
    /// Valore di <c>mycount</c> sulla prima barra della sessione. Vale 1 nella forma più diffusa
    /// (<c>if slb[1] then mycount = 0</c> seguito da <c>mycount+1</c>), ma alcune varianti
    /// azzerano a 1 e incrementano subito dopo, partendo quindi da 2: tutti gli indici di barra
    /// di quelle strategie sono spostati di uno, e trattarle come le altre sposterebbe ogni
    /// ingresso e ogni uscita di una barra.
    /// </summary>
    protected int BarCountStartsAt = 1;

    // ------------------------------------------------------------------ stato di sessione

    // Persistiti fra le barre dall'engine tramite RuntimeState (cattura per nome del campo).
    private int _mycount;
    private bool _okLong;
    private bool _okShort;

    // ------------------------------------------------------------------ valutazione

    public virtual TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);
        if (isStartOfSession)
        {
            _mycount = BarCountStartsAt - 1;
            _okLong = false;
            _okShort = false;
        }

        _mycount++;

        var nextBarTime = EasyLib.EstimateNextBarUtc(data, barTime, TimeframeMinutes);
        if (UsesCustomEntryRules)
        {
            var customEntries = new List<TradeSignal>(2);
            AddCustomEntries(data, barTime, nextBarTime, ohlc, customEntries);
            return Combine(customEntries, Hold(bar.Close, barTime));
        }

        // I tipi 2/3 armano alla barra trigger, poi riemettono l'ordine next-bar finché la
        // finestra è aperta. Il tipo 1 è gestito sotto: il pattern va letto alla chiusura della
        // barra precedente, affinché il market fill cada all'open della barra ArmBar.
        if (EntryType != BiasEntryType.MarketOnArmBar &&
            _mycount == ArmBarLong &&
            Pattern(PatternLongYes, ohlc) && !Pattern(PatternLongNo, ohlc) &&
            PythonDayOfWeek(barTime) != NotEntryDayLong)
        {
            _okLong = true;
        }

        if (EntryType != BiasEntryType.MarketOnArmBar &&
            _mycount == ArmBarShort &&
            Pattern(PatternShortYes, ohlc) && !Pattern(PatternShortNo, ohlc) &&
            PythonDayOfWeek(barTime) != NotEntryDayShort)
        {
            _okShort = true;
        }

        // Una direzione già in posizione si disarma: l'originale fa lo stesso, ed è ciò che
        // impedisce la piramidazione senza vietare l'inversione dalla direzione opposta.
        var windowLong = EasyLib.TwBars(ArmBarLong, EndLong, _mycount);
        var windowShort = EasyLib.TwBars(ArmBarShort, EndShort, _mycount);
        if (windowLong && CurrentMP == 1) _okLong = false;
        if (windowShort && CurrentMP == -1) _okShort = false;

        var entries = new List<TradeSignal>(2);

        if (EntryType == BiasEntryType.MarketOnArmBar)
        {
            // Parità Python: mask.shift(1) legge il pattern alla close precedente e riempie
            // l'open della barra ArmBar. ArmBar = 1 richiede il segnale sull'ultima barra della
            // sessione precedente, riconoscibile dalla prossima apertura di sessione.
            if (CurrentMP != 1 &&
                IsBeforeMarketArmBar(ArmBarLong, nextBarTime) &&
                Pattern(PatternLongYes, ohlc) && !Pattern(PatternLongNo, ohlc) &&
                PythonDayOfWeek(nextBarTime) != NotEntryDayLong)
            {
                entries.Add(WithExit(
                    EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE_MKT"),
                    nextBarTime, ExitBarLong));
            }

            if (CurrentMP != -1 &&
                IsBeforeMarketArmBar(ArmBarShort, nextBarTime) &&
                Pattern(PatternShortYes, ohlc) && !Pattern(PatternShortNo, ohlc) &&
                PythonDayOfWeek(nextBarTime) != NotEntryDayShort)
            {
                entries.Add(WithExit(
                    EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE_MKT"),
                    nextBarTime, ExitBarShort));
            }
        }
        else
        {
            // Tipi 2 e 3: l'ordine viene riemesso a ogni barra finché la finestra è aperta e la
            // direzione resta armata. Non si verifica qui se il livello è stato toccato: è
            // compito dell'engine, che riempie gap-aware.
            if (windowLong && _okLong)
            {
                var closedBars = data[..^1];
                var (side, level, reason) = EntryType == BiasEntryType.BreakoutStop
                    ? (SignalType.Buy, EasyLib.Highest(closedBars, BreakoutBarsHigh, d => d.High), "LE_STP")
                    : (SignalType.Buy, EasyLib.Lowest(closedBars, BreakoutBarsLow, d => d.Low), "LE_LMT");

                var signal = EntryType == BiasEntryType.BreakoutStop
                    ? EntryStopNextBar(side, level, data, barTime, reason)
                    : EntryLimitNextBar(side, level, data, barTime, reason);

                entries.Add(WithExit(signal, barTime, ExitBarLong));
            }

            if (windowShort && _okShort)
            {
                var closedBars = data[..^1];
                var (side, level, reason) = EntryType == BiasEntryType.BreakoutStop
                    ? (SignalType.Sell, EasyLib.Lowest(closedBars, BreakoutBarsLow, d => d.Low), "SE_STP")
                    : (SignalType.Sell, EasyLib.Highest(closedBars, BreakoutBarsHigh, d => d.High), "SE_LMT");

                var signal = EntryType == BiasEntryType.BreakoutStop
                    ? EntryStopNextBar(side, level, data, barTime, reason)
                    : EntryLimitNextBar(side, level, data, barTime, reason);

                entries.Add(WithExit(signal, barTime, ExitBarShort));
            }
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>
    /// Applica l'uscita a indice di barra come deadline sull'ingresso. È la traduzione di
    /// <c>if mycount = MyLXBar then sell next bar at market</c>: nell'originale è un ordine
    /// emesso a runtime, qui diventa una proprietà del segnale d'ingresso, così l'engine la
    /// applica da solo e la stessa strategia funziona identica in backtest e in live.
    /// </summary>
    /// <summary>
    /// Indica che la variante BIAS usa filtri di ingresso diversi dal conteggio barre.
    /// L'adattatore deve esprimerli tramite <see cref="AddCustomEntries"/> e non sovrascrivere
    /// <see cref="GenerateSignal"/>, mantenendo così comune il contratto degli ordini.
    /// </summary>
    protected virtual bool UsesCustomEntryRules => false;

    /// <summary>
    /// Aggiunge gli ingressi delle varianti BIAS con filtri proprietari. Il motore ha già
    /// ricostruito gli OHLC di sessione; gli ingressi vanno creati con i builder next-bar di
    /// <see cref="EasyEngineBase"/> e devono includere ogni uscita nota.
    /// </summary>
    protected virtual void AddCustomEntries(
        OhlcvData[] data,
        DateTime barTime,
        DateTime nextBarTime,
        decimal[] ohlc,
        List<TradeSignal> entries)
    {
    }

    /// <summary>
    /// Applica l'uscita a indice di barra alla specifica dell'ingresso.
    /// </summary>
    protected TradeSignal WithExit(TradeSignal signal, DateTime barTime, int exitBarIndex)
    {
        if (exitBarIndex > 0)
        {
            // BuildSessionOhlc conta la prima candela chiusa dopo SessionStartTime come barra 1
            // (18:00–19:00 ha timestamp 19:00). Allineare la deadline allo stesso riferimento
            // evita un'uscita una barra prima del confronto Python bar_num == lx/sx_bar.
            signal.CloseAtUtc = SessionBarToUtc(barTime, exitBarIndex).AddMinutes(TimeframeMinutes);
        }
        return signal;
    }

    /// <summary>
    /// Applica una deadline oraria alla specifica dell'ingresso. La risoluzione avviene rispetto
    /// alla barra di segnale, quindi un'uscita mattutina successiva a un ingresso serale cade il
    /// giorno seguente.
    /// </summary>
    protected TradeSignal WithExitTime(TradeSignal signal, DateTime barTime, int exitTime)
    {
        signal.CloseAtUtc = ResolveCloseAtUtc(barTime, exitTime);
        return signal;
    }

    /// <summary>Valuta la libreria pattern configurata dalla variante.</summary>
    protected bool Pattern(int number, decimal[] ohlc) => PatternLibrary switch
    {
        EasyPatternLibrary.Fast => EasyLib.PatternFast(number, ohlc),
        EasyPatternLibrary.BaseSA => EasyLib.PtnBaseSA2(number, ohlc),
        _ => false
    };

    private bool IsBeforeMarketArmBar(int armBar, DateTime nextBarTime) =>
        _mycount == armBar - 1 ||
        (armBar == 1 && Hhmm(nextBarTime) == SessionStartTime);

    protected int PythonDayOfWeek(DateTime barTime) =>
        PythonWeekday(barTime);
}
