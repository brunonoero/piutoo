using Piootoo.Shared.Configuration;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Risponde a una domanda sola: in questo istante lo strumento <b>negoziava</b>?
///
/// <para><b>È il punto unico che decide.</b> Prima della maschera il calendario sapeva già che
/// alcune barre erano fuori sessione — <c>FeedCalendarReport</c> le contava nel
/// <c>backtest-summary.json</c> — ma nessuno le toglieva, e l'unico consumatore che <i>decideva</i>
/// era il contatore di <c>MaxBarsInPosition</c>. Qui la risposta è una e la usano tutti e tre i
/// percorsi: prefill del backtest interno, backtest su feed di broker, sessione realtime.</para>
///
/// <para><b>Cosa NON fa.</b> Non tocca la griglia dei bucket: quella resta ancorata a
/// <see cref="SymbolCalendar.SessionStartHour"/> nell'orologio della ricerca, perché è la griglia su
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
    /// L'istante UTC in cui cade <paramref name="edge"/> per la giornata che si apre il
    /// <paramref name="day"/>. Un bordo ancorato in locale passa dal fuso di borsa; uno ancorato in
    /// UTC è già un istante e non va convertito — è il caso dell'apertura notturna del FDAX.
    /// </summary>
    private DateTime Resolve(WindowEdge edge, DateOnly day)
    {
        var hours = edge.Hhmm / 100;
        var minutes = edge.Hhmm % 100;

        return edge.Anchor == PhaseAnchor.Utc
            ? new DateTime(day.Year, day.Month, day.Day, hours, minutes, 0, DateTimeKind.Utc)
            : _exchange.ToUtc(day.ToDateTime(TimeOnly.MinValue).AddHours(hours).AddMinutes(minutes));
    }

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
