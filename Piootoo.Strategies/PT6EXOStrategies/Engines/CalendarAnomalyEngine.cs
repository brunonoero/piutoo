using System.Globalization;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore CAL: <b>anomalie di calendario</b>. Entra all'apertura di un giorno di sessione scelto dal
/// calendario — la fine del mese, la settimana della scadenza, la vigilia di un festivo — e tiene per un
/// numero fisso di sessioni. Non guarda il prezzo: e' parente di <c>BiasWeeklyEngine</c> ma con un ciclo
/// mensile invece che settimanale. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §CAL.
///
/// <para><b>Si contano giorni di negoziazione, non giorni di calendario.</b> "L'ultimo giorno del mese"
/// e' l'ultimo in cui il simbolo ha davvero una sessione, e un giorno lo e' quando:</para>
/// <list type="number">
///   <item>il calendario del simbolo lo dichiara giorno di sessione (<see cref="SessionGrid.IsSessionDay"/>;
///   un calendario che non dichiara i giorni li fa contare tutti);</item>
///   <item>non e' fra i festivi del calendario (<c>SymbolCalendar.Holidays</c>, oggi vuoti per tutti);</item>
///   <item>la finestra di negoziazione del simbolo (<see cref="SessionMask"/>) tocca la sua sessione,
///   quando e' dichiarata.</item>
/// </list>
/// <para>Il terzo punto non e' un dettaglio. La domenica e' un giorno di sessione di NQ, ma la sessione
/// domenicale della ricerca ha barre solo nelle settimane in cui l'ora legale americana ed europea sono
/// sfasate (le 17:00 di Chicago cadono alle 23:00 di Roma invece che a mezzanotte): contarla sempre
/// sposterebbe di un giorno ogni conteggio che la attraversa. Allo stesso modo SB dichiara il sabato,
/// su cui la finestra non apre mai. I festivi vengono solo dal calendario, mai da una lista nel motore.</para>
///
/// <para><b>Le tre anomalie</b> (<see cref="Mode"/>), lette sul giorno di sessione della griglia
/// (<see cref="SessionGrid.SessionDayOf"/>) — il mese e il giorno della settimana sono quelli del giorno
/// di calendario europeo della ricerca, non dell'istante UTC:</para>
/// <list type="bullet">
///   <item>0, <b>turn-of-month</b>: si entra il <see cref="DaysBeforeMonthEnd"/>-esimo ultimo giorno di
///   negoziazione del mese (1 = l'ultimo) e la tenuta attraversa il cambio di mese;</item>
///   <item>1, <b>settimana della scadenza</b>: si entra il primo giorno di negoziazione della settimana
///   (lunedi'–domenica) che contiene il terzo venerdi' del mese, cioe' il venerdi' che cade fra il 15 e il
///   21. Ogni mese, non solo le scadenze trimestrali;</item>
///   <item>2, <b>vigilia di festivo</b>: si entra il giorno di negoziazione dopo il quale il primo giorno
///   feriale (lunedi'–venerdi') non e' di negoziazione. <b>Oggi e' inerte</b>: nessun calendario dichiara
///   festivi, e nessuno toglie un giorno feriale. Si accende da solo quando il calendario li portera'.</item>
/// </list>
///
/// <para><b>Quando nasce l'ordine.</b> Come <c>BiasWeeklyEngine</c> e <c>HourOfDayEngine</c>, sulla
/// barra <b>prima</b>: l'ultima barra negoziata della sessione che precede quella dell'ingresso, con un
/// ordine a mercato che apre alla prima barra della sessione dell'ingresso. "Ultima barra negoziata" e'
/// piu' di <see cref="EasyEngineBase.IsLastBarOfSession"/>: su NQ la sessione della ricerca finisce a
/// mezzanotte di Roma ma il future chiude alle 16:00 di Chicago, un'ora prima, e la barra delle 23:00
/// di Roma non esiste. Dopo <see cref="EasyEngineBase.IsLastBarOfSession"/> si scorrono quindi le barre
/// che restano nella sessione e si chiede alla finestra di negoziazione se ne esiste una.</para>
///
/// <para><b>L'ultima barra del venerdi'.</b> La barra dopo, proiettata di un timeframe, cade nel fine
/// settimana, dove non c'e' nulla. Per questo il giorno dell'ingresso non si ricava dalla proiezione ma
/// dai giorni di negoziazione: dal venerdi' si salta il sabato (non di sessione) e la domenica (di
/// sessione per NQ, ma senza barre in una settimana normale) e si arriva al lunedi'. L'ordine dichiara
/// <c>ValidFromUtc</c> = <c>ExpiresAtUtc</c> = apertura della sessione del lunedi' — non la proiezione
/// — cosi' motore interno e cBot sanno entrambi che l'ordine non vale prima; il motore interno lo
/// riempie comunque alla prima barra vera, come ogni market "next bar". Dove non c'e' un buco
/// (sessione senza pausa finale) le due cose coincidono.</para>
///
/// <para><b>L'uscita</b> e' l'apertura della sessione che viene <see cref="HoldSessions"/> giorni di
/// negoziazione dopo quello dell'ingresso, come <c>CloseAtUtc</c> sull'ingresso: la posizione vive
/// esattamente <see cref="HoldSessions"/> sessioni. Una scadenza piu' stretta gia' sul segnale
/// (<c>MaxDaysInTrade</c>) vince.</para>
///
/// <para><b>Multiday per natura.</b> Una tenuta di piu' sessioni non si chiude a fine sessione: questo
/// motore non applica l'uscita di sessione (<see cref="EasyEngineBase.AppliesSessionExit"/> resta falso
/// nella base) e la sua <see cref="EasyEngineBase.Holding"/> e' <c>Multiday</c>. <c>IntradayOnly</c> non
/// ha effetto; un'ora di uscita di sessione (<c>ExitHour</c>) dichiarata e' un errore di
/// configurazione, perche' sarebbe una leva inerte. Il fine settimana resta una decisione del piano.</para>
///
/// <para>La finestra dichiarata, se c'e', si legge sulla barra di segnale — cioe' sull'ultima barra della
/// sessione — e su questo motore ha poco senso: i contenitori la lasciano a tutta la giornata.</para>
/// </summary>
public abstract class CalendarAnomalyEngine : EasyEngineBase
{
    /// <summary>0 = turn-of-month, 1 = settimana della scadenza, 2 = vigilia di festivo.</summary>
    protected int Mode;

    /// <summary>Turn-of-month: quale ultimo giorno di negoziazione del mese, contando da 1 = l'ultimo.</summary>
    protected int DaysBeforeMonthEnd = 1;

    /// <summary>Sessioni di tenuta: l'uscita e' all'apertura della sessione che viene tanti giorni di negoziazione dopo l'ingresso.</summary>
    protected int HoldSessions = 4;

    /// <summary>Verso: 1 long, 2 short. Il calendario non dice in che verso entrare: 0 e' un errore.</summary>
    protected int Direction = 1;

    /// <summary>
    /// Quanti giorni senza negoziazione di fila si tollerano cercando il giorno successivo: due settimane
    /// coprono ogni chiusura vera. Oltre, il calendario non dichiara piu' sessioni e il motore non emette.
    /// </summary>
    private const int MaxClosedDays = 14;

    private SessionMask? _mask;
    private string? _maskSymbol;

    /// <summary>
    /// La finestra di negoziazione del simbolo. Si ricostruisce se il simbolo cambia dopo la prima
    /// lettura: un contenitore lo riceve in <c>Initialize</c>, dopo il costruttore.
    /// </summary>
    private SessionMask Mask
    {
        get
        {
            if (_mask is null || _maskSymbol != Symbol)
            {
                _mask = SessionMask.For(Symbol);
                _maskSymbol = Symbol;
            }

            return _mask;
        }
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (Mode is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode,
                $"{Name}: Mode deve valere 0 (fine mese), 1 (settimana della scadenza) o 2 (vigilia di festivo).");
        }

        if (Direction is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(Direction), Direction,
                $"{Name}: il calendario non dice in che verso entrare, Direction deve valere 1 o 2.");
        }

        if (HoldSessions is < 1 or > 20 || DaysBeforeMonthEnd is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(HoldSessions),
                $"{Name}: HoldSessions {HoldSessions} e DaysBeforeMonthEnd {DaysBeforeMonthEnd} devono stare fra 1 e 20.");
        }

        if (SessionExitTime is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(SessionExitTime), SessionExitTime,
                $"{Name}: il motore CAL e' multiday per natura e non applica l'uscita di sessione; " +
                "un'ora di uscita non avrebbe effetto. La tenuta si dichiara con HoldSessions.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Il segnale nasce solo sull'ultima barra negoziata della sessione: e' il controllo piu'
        // economico e scarta tutte le altre barre prima di qualunque conto sui giorni.
        var sessionDay = Grid.SessionDayOf(barTime);
        if (!IsLastTradedBarOfSession(barTime, sessionDay))
            return Hold(bar.Close, barTime);

        if (NthTradingDayAfter(sessionDay, 1) is not { } entryDay || !IsAnomalyDay(entryDay))
            return Hold(bar.Close, barTime);

        if (NthTradingDayAfter(entryDay, HoldSessions) is not { } exitDay)
            return Hold(bar.Close, barTime);

        var side = Direction == 1 ? SignalType.Buy : SignalType.Sell;
        var signal = EntryMarketNextBar(side, bar.Close, data, barTime, side == SignalType.Buy ? "LE CAL" : "SE CAL");

        // L'ordine vale dall'apertura della sessione dell'ingresso, non dalla proiezione di un
        // timeframe: dall'ultima barra del venerdi' la proiezione cade nel fine settimana.
        var entryOpen = Grid.SessionOpenUtc(entryDay);
        signal.ValidFromUtc = entryOpen;
        signal.ExpiresAtUtc = entryOpen;
        if (MaxEntriesPerSession > 0)
            signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(entryOpen);

        var exit = Grid.SessionOpenUtc(exitDay);
        if (signal.CloseAtUtc is not { } earlier || exit < earlier)
            signal.CloseAtUtc = exit;

        return WithSessionExit(signal) ?? Hold(bar.Close, barTime);
    }

    private bool IsAnomalyDay(DateTime entryDay) => Mode switch
    {
        0 => IsNthLastTradingDayOfMonth(entryDay, DaysBeforeMonthEnd, IsTradingDay),
        1 => IsFirstTradingDayOfExpiryWeek(entryDay, IsTradingDay),
        _ => PrecedesWeekdayWithoutSession(entryDay, IsTradingDay)
    };

    /// <summary>
    /// Vero se nessuna barra negoziata segue <paramref name="barTime"/> dentro la sua sessione. Il
    /// caso comune — la barra dopo esiste — si decide alla prima iterazione.
    /// </summary>
    private bool IsLastTradedBarOfSession(DateTime barTime, DateTime sessionDay)
    {
        if (IsLastBarOfSession(barTime))
            return true;

        var sessionClose = Grid.SessionOpenUtc(sessionDay.AddDays(1));
        var mask = Mask;
        for (var next = barTime.AddMinutes(TimeframeMinutes); next < sessionClose; next = next.AddMinutes(TimeframeMinutes))
        {
            // Senza finestra dichiarata (null) la barra si presume esistente, come fa il resto del sistema.
            if (mask.Overlaps(next, TimeframeMinutes) != false)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Se il simbolo negozia davvero nel giorno di sessione <paramref name="sessionDay"/>: giorno di
    /// sessione del calendario, non festivo, e toccato dalla finestra di negoziazione quando e'
    /// dichiarata. Vedi la nota sulla classe.
    /// </summary>
    private bool IsTradingDay(DateTime sessionDay)
    {
        if (Grid.IsSessionDay(sessionDay) == false)
            return false;

        var date = DateOnly.FromDateTime(sessionDay);
        foreach (var holiday in Grid.Calendar.Holidays)
        {
            if (holiday == date)
                return false;
        }

        var mask = Mask;
        if (!mask.DeclaresWindow)
            return true;

        // A passi di un'ora: gli ancoraggi e gli scarti dei fusi sono a ore piene, e Overlaps guarda
        // tutta l'ora, quindi una finestra che apre alle 17:00 o alle 00:15 non sfugge.
        var close = Grid.SessionOpenUtc(sessionDay.AddDays(1));
        for (var hour = Grid.SessionOpenUtc(sessionDay); hour < close; hour = hour.AddHours(1))
        {
            if (mask.Overlaps(hour, 60) == true)
                return true;
        }

        return false;
    }

    /// <summary>L'<paramref name="n"/>-esimo giorno di negoziazione dopo <paramref name="day"/>, o null se il calendario non ne dichiara.</summary>
    private DateTime? NthTradingDayAfter(DateTime day, int n)
    {
        var found = 0;
        for (var offset = 1; offset <= n + MaxClosedDays; offset++)
        {
            var candidate = day.AddDays(offset);
            if (IsTradingDay(candidate) && ++found == n)
                return candidate;
        }

        return null;
    }

    // ------------------------------------------------------------------ le regole di calendario
    //
    // Funzioni pure del giorno e del predicato "giorno di negoziazione": il motore passa il proprio,
    // che legge il calendario del simbolo. Separate cosi' perche' la vigilia di festivo non si puo'
    // provare sul motore finche' nessun calendario dichiara festivi, e una regola non provata e' una
    // regola di cui non si sa niente. I giorni sono giorni di sessione della griglia (date, non
    // istanti): mese e giorno del mese si leggono su di loro.

    /// <summary>
    /// Turn-of-month: <paramref name="day"/> e' l'<paramref name="n"/>-esimo ultimo giorno di
    /// negoziazione del suo mese (1 = l'ultimo).
    /// </summary>
    protected static bool IsNthLastTradingDayOfMonth(DateTime day, int n, Func<DateTime, bool> isTradingDay)
    {
        var following = 0;
        for (var next = day.AddDays(1); next.Month == day.Month; next = next.AddDays(1))
        {
            if (isTradingDay(next) && ++following >= n)
                return false;
        }

        return following == n - 1;
    }

    /// <summary>
    /// Settimana della scadenza: <paramref name="day"/> e' il primo giorno di negoziazione della
    /// settimana, da lunedi' a domenica, il cui venerdi' e' il terzo del mese (cade fra il 15 e il 21).
    /// </summary>
    protected static bool IsFirstTradingDayOfExpiryWeek(DateTime day, Func<DateTime, bool> isTradingDay)
    {
        var monday = day.AddDays(-DaysFromMonday(day));
        var friday = monday.AddDays(4);
        if (friday.Day is < 15 or > 21)
            return false;

        for (var earlier = monday; earlier < day; earlier = earlier.AddDays(1))
        {
            if (isTradingDay(earlier))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Vigilia di festivo: il primo giorno feriale (lunedi'–venerdi') dopo <paramref name="day"/> non
    /// e' di negoziazione. Il sabato e la domenica si saltano: non sono festivi, sono il fine settimana.
    /// </summary>
    protected static bool PrecedesWeekdayWithoutSession(DateTime day, Func<DateTime, bool> isTradingDay)
    {
        var next = day.AddDays(1);
        while (DaysFromMonday(next) >= 5)
            next = next.AddDays(1);

        return !isTradingDay(next);
    }

    /// <summary>
    /// Giorni dal lunedi' (0 = lunedi' … 6 = domenica) di un giorno di sessione. E' una data della
    /// griglia, gia' nel calendario della ricerca, non l'istante di una barra: per questo si legge dal
    /// calendario gregoriano e non passa da un orologio.
    /// </summary>
    private static int DaysFromMonday(DateTime sessionDay) =>
        ((int)CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(sessionDay) + 6) % 7;
}
