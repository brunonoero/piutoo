using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Risponde a una domanda sola: in questo istante lo strumento <b>negoziava</b>?
///
/// <para><b>È il punto unico che decide.</b> Prima della maschera il calendario sapeva già che
/// alcune barre erano fuori sessione — <c>FeedCalendarReport</c> le contava nel
/// <c>backtest-summary.json</c> — ma nessuno le toglieva, e l'unico consumatore che <i>decideva</i>
/// era il contatore di <c>MaxBarsInPosition</c>. Qui la risposta è una.</para>
///
/// <para><b>Chi la applica, dal 16/09/2026 (7.5.0).</b> Nata l'08/09 con la finestra misurata e un
/// test che ne contava l'effetto, per otto giorni non l'ha usata nessuno: la classe diceva «la usano
/// tutti e tre i percorsi» e nessun percorso la chiamava. Misurato su <c>PT2_FDAX_PCH_001_240</c>,
/// FTMO 09/2025-08/2026: l'11% dei minuti del CFD sta fuori dall'orario Eurex (22:00-01:15 di Roma),
/// finisce nelle barre 4h delle 21:00 e delle 01:00 e sposta canale e massimi di sessione; con la
/// maschera la strategia passa da −99.377 a −40.491 e il paniere da +5,0% a +11,1%. Ora la
/// applicano: <see cref="BarAggregator"/>, che scarta i minuti fuori finestra prima di piegarli in
/// bucket (ricostruzione degli archivi di broker e riscaldamento dal disco); la sessione live, che
/// scarta le barre spinte che non toccano la finestra; il backtest, allo stesso modo, sulle serie
/// caricate; il cBot, che non piega nelle proprie candele le barre base fuori finestra e riceve la
/// finestra dal descriptor. Il feed da un minuto — il feed di <i>rischio</i>, che alimenta mark,
/// stop e uscite a tempo — non si maschera: un conto vero quelle ore le vive.</para>
///
/// <para><b>Cosa NON fa.</b> Non tocca la griglia dei bucket: quella resta ancorata a
/// <see cref="SymbolCalendar.SessionStart"/> nell'orologio della ricerca, perché è la griglia su
/// cui le strategie sono state trovate. Mascherare barre e ri-ancorare la griglia sono due cose
/// diverse, e confonderle invaliderebbe il porting in silenzio. Vedi
/// <c>docs/domini/layer-barre-e-calendario.md</c>.</para>
///
/// <para><b>Non è thread-safe</b>, come il <see cref="SessionClock"/> che incapsula: una istanza per
/// consumatore.</para>
/// </summary>
public sealed class SessionMask
{
    private readonly SessionClock _exchange;
    private readonly IReadOnlyList<TradingWindow> _windows;

    public SessionMask(SymbolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        Calendar = calendar;
        _exchange = new SessionClock(calendar.ExchangeTimeZone);
        _windows = calendar.TradingWindows;
    }

    /// <summary>Costruisce la maschera del simbolo dal calendario in vigore.</summary>
    public static SessionMask For(string symbol) =>
        new(MarketCalendarRegistry.Current.Get(symbol));

    public SymbolCalendar Calendar { get; }

    /// <summary>
    /// Vero se il calendario dichiara quando questo strumento negozia. Quando è falso
    /// <see cref="IsOpen"/> risponde <c>null</c> e chi legge <b>non deve inventare</b>: lasciar
    /// passare tutto è la scelta giusta, perché una maschera che non c'è non deve spegnere uno
    /// strumento.
    /// </summary>
    public bool DeclaresWindow => _windows.Count > 0;

    /// <summary>
    /// Se <paramref name="instantUtc"/> cade dentro una giornata di negoziazione.
    /// <c>null</c> quando la finestra non è dichiarata.
    ///
    /// <para><b>Perché si provano tre date.</b> Una giornata di negoziazione può aprirsi il giorno
    /// prima (i future CME aprono alle 17:00 e chiudono alle 16:00 del giorno dopo) e, quando i due
    /// bordi hanno ancoraggi diversi, la data UTC dell'istante e quella locale della giornata che lo
    /// contiene <b>divergono</b>. Accoppiare apertura e chiusura sulla stessa data di calendario è
    /// l'errore naturale da fare qui: sul FDAX d'estate lascia le barre della prima mattina fuori da
    /// ogni finestra, e quelle risultano "dentro" per omissione invece che per regola.</para>
    /// </summary>
    public bool? IsOpen(DateTime instantUtc)
    {
        if (!DeclaresWindow)
            return null;

        var utc = DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc);

        for (var offset = -1; offset <= 1; offset++)
        {
            var day = DateOnly.FromDateTime(utc.Date.AddDays(offset));
            foreach (var window in _windows)
            {
                if (!Covers(window, day) || !window.OpensOn.Contains(day.DayOfWeek))
                    continue;

                var open = Resolve(window.Open, day);
                var close = Resolve(window.Close, day);

                // Chiusura non oltre l'apertura = la finestra scavalca la mezzanotte.
                if (close <= open)
                    close = Resolve(window.Close, day.AddDays(1));

                if (open <= utc && utc < close)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Se una barra che apre in <paramref name="openUtc"/> e dura <paramref name="timeframeMinutes"/>
    /// <b>tocca</b> la finestra di negoziazione: <c>null</c> quando la finestra non è dichiarata.
    ///
    /// <para><b>Perché "tocca" e non "apre dentro".</b> Su un minuto le due cose coincidono. Su una
    /// barra più larga no: il bucket 4h del FDAX ancorato all'01:00 di Roma apre alle 00:00 UTC
    /// d'inverno, un quarto d'ora <i>prima</i> dell'apertura Eurex, ed è una barra vera; l'ora
    /// nativa delle 22:00 di Roma sta invece tutta fuori. La regola sui minuti la applica
    /// <see cref="BarAggregator"/>; questa vale per le barre già piegate — quelle che una sessione
    /// riceve dal cBot e quelle che il backtest legge dal disco — e per le barre base del cBot.</para>
    /// </summary>
    public bool? Overlaps(DateTime openUtc, int timeframeMinutes)
    {
        if (!DeclaresWindow)
            return null;

        if (timeframeMinutes <= 1)
            return IsOpen(openUtc);

        return IsOpen(openUtc) == true || IsOpen(openUtc.AddMinutes(timeframeMinutes - 1)) == true;
    }

    /// <summary>
    /// <b>Le barre fuori dall'orario di negoziazione non esistono per le strategie.</b> Restituisce
    /// la serie senza le barre che non toccano la finestra dichiarata, e dice quante ne ha tolte.
    /// Senza finestra dichiarata non toglie nulla: stessa forma di
    /// <see cref="SessionGrid.DropNonSessionDays"/>, un livello più sotto — là i giorni, qui le ore.
    /// </summary>
    public OhlcvData[] DropOutsideWindow(IReadOnlyList<OhlcvData> bars, int timeframeMinutes, out int dropped)
    {
        ArgumentNullException.ThrowIfNull(bars);
        dropped = 0;
        if (!DeclaresWindow || bars.Count == 0)
            return bars as OhlcvData[] ?? bars.ToArray();

        var kept = new List<OhlcvData>(bars.Count);
        foreach (var bar in bars)
        {
            if (Overlaps(bar.DateTime, timeframeMinutes) == false)
                dropped++;
            else
                kept.Add(bar);
        }

        return dropped == 0 ? bars as OhlcvData[] ?? bars.ToArray() : kept.ToArray();
    }

    /// <summary>
    /// La finestra in parole, per il campo <c>source</c> dei feed e per i log: chi legge un archivio
    /// deve poter vedere con quale finestra è stato costruito. Vuoto se non dichiarata.
    /// </summary>
    public string Describe()
    {
        if (!DeclaresWindow)
            return string.Empty;

        var parts = _windows.Select(window =>
            $"{window.Open.Time:HH\\:mm}{Suffix(window.Open.Anchor)}-{window.Close.Time:HH\\:mm}{Suffix(window.Close.Anchor)}");
        return $"finestra {string.Join('|', parts)} {Calendar.ExchangeTimeZone}";

        static string Suffix(PhaseAnchor anchor) => anchor == PhaseAnchor.Utc ? "Z" : "L";
    }

    /// <summary>
    /// L'istante UTC in cui cade <paramref name="edge"/> per la giornata che si apre il
    /// <paramref name="day"/>. Un bordo ancorato in locale passa dal fuso di borsa; uno ancorato in
    /// UTC è già un istante e non va convertito — è il caso dell'apertura notturna del FDAX.
    /// </summary>
    private DateTime Resolve(WindowEdge edge, DateOnly day) =>
        edge.Anchor == PhaseAnchor.Utc
            ? DateTime.SpecifyKind(day.ToDateTime(edge.Time), DateTimeKind.Utc)
            : _exchange.ToUtc(day.ToDateTime(edge.Time));

    /// <summary>
    /// Se la finestra vale in quel giorno dell'anno. Il confronto è su <c>MM-dd</c> e non
    /// sull'anno, perché un periodo stagionale si ripete: un intervallo che scavalca il capodanno
    /// (<c>"11-01"</c> → <c>"03-14"</c>) è la forma normale, non un errore.
    /// </summary>
    private static bool Covers(TradingWindow window, DateOnly day)
    {
        if (window.IsAllYear)
            return true;

        var current = day.Month * 100 + day.Day;
        var from = window.From is null ? 101 : MonthDay(window.From);
        var to = window.To is null ? 1231 : MonthDay(window.To);

        return from <= to
            ? current >= from && current <= to
            : current >= from || current <= to;
    }

    private static int MonthDay(string monthDay)
    {
        var parts = monthDay.Split('-');
        return int.Parse(parts[0]) * 100 + int.Parse(parts[1]);
    }
}
