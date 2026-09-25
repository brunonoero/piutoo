using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore DXH: <b>griglia giorno × ora</b>. Per ogni coppia (giorno della settimana, ora locale) una
/// regola dice long, short o niente; si entra all'ora della regola e si tiene <see cref="HoldHours"/>
/// ore. E' stagionalita' pura, una delle idee <b>bizzarre</b> della serie PT6EXO, e la famiglia con il
/// <b>rischio di adattamento piu' alto</b> del catalogo: cinque giorni per ventiquattro ore per tre
/// esiti sono centinaia di gradi di liberta', e una tabella scelta sul campione ricorda il campione.
/// Si prova per ultima, con uno split fuori campione severo, <b>solo</b> sulle celle in cui HOD ha gia'
/// mostrato qualcosa, e sempre contro il controllo a ingresso casuale RAN con le stesse uscite. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §DXH.
///
/// <para><b>La tabella</b> arriva come stringa, <see cref="Rules"/>, perche' e' una leva di griglia e
/// una griglia porta scalari: elementi <c>giorno-ora:L|S</c> separati da <c>;</c>, con giorno 0 =
/// lunedi' … 4 = venerdi' e ora 0..23 intera, per esempio <c>0-10:L;2-15:S;4-20:S</c>. Spazi attorno
/// agli elementi e un <c>;</c> finale sono ammessi; la stringa vuota e' "nessuna regola". Un elemento
/// malformato, fuori intervallo o ripetuto <b>ferma</b> la valutazione con un errore che lo nomina:
/// una regola ignorata in silenzio sarebbe una leva inerte, e la griglia misurerebbe una tabella
/// diversa da quella che crede.</para>
///
/// <para><b>Si interpreta una volta sola.</b> La stringa si traduce in una tabella 5 × 24 alla prima
/// valutazione e la tabella resta in un campo insieme alla stringa da cui viene: si ricostruisce solo
/// se <see cref="Rules"/> cambia, come l'orologio di <c>HourOfDayEngine</c>. Il percorso caldo legge
/// una cella dell'array, senza LINQ e senza stringhe.</para>
///
/// <para><b>L'orologio e la barra</b> sono quelli di <c>HourOfDayEngine</c>: ora e giorno si leggono
/// sull'orologio dichiarato da <see cref="ScheduleClock"/> (Roma della ricerca, o l'ora di borsa del
/// simbolo), mai in UTC; si entra sulla barra la cui <b>apertura</b> cade sull'ora piena della regola,
/// con l'ordine emesso sulla barra prima; la barra deve esistere (giorno di sessione e finestra di
/// negoziazione); la tenuta si conta in ora locale e un'uscita di sessione piu' stretta vince. Un'ora
/// che non e' l'apertura di una barra del timeframe non scatta mai.</para>
///
/// <para><b>Due regole vicine non si concatenano.</b> Se l'ora di una regola cade dentro la tenuta di
/// un'altra, o proprio alla sua fine, la posizione e' ancora aperta quando la barra prima si valuta, e
/// l'ingresso non nasce: una posizione alla volta. <c>0-10:L;0-11:L</c> con un'ora di tenuta e' un
/// trade solo, alle 10.</para>
/// </summary>
public abstract class DayHourGridEngine : EasyEngineBase
{
    /// <summary>La tabella delle regole, nel formato <c>giorno-ora:L|S;…</c>. Vuota = nessuna regola.</summary>
    protected string Rules = string.Empty;

    /// <summary>Ore di tenuta in ora locale. 0 = nessuna uscita a tempo propria, restano le uscite comuni.</summary>
    protected int HoldHours = 1;

    /// <summary>In quale orologio sono scritti giorni, ore e tenuta.</summary>
    protected InstrumentClock ScheduleClock = InstrumentClock.Research;

    private const int Days = 5;
    private const int Hours = 24;

    private SessionClock? _scheduleClockInstance;
    private InstrumentClock _scheduleClockResolved;

    private string? _rulesSource;
    private sbyte[,]? _rulesTable;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// L'orologio dell'orario, con il fuso dal calendario del simbolo. Si ricostruisce se
    /// <see cref="ScheduleClock"/> cambia dopo la prima lettura.
    /// </summary>
    private SessionClock LocalClock
    {
        get
        {
            if (_scheduleClockInstance is null || _scheduleClockResolved != ScheduleClock)
            {
                var calendar = MarketCalendarRegistry.Current.Get(Symbol);
                _scheduleClockInstance = new SessionClock(ScheduleClock == InstrumentClock.Exchange
                    ? calendar.ExchangeTimeZone
                    : calendar.ResearchTimeZone);
                _scheduleClockResolved = ScheduleClock;
            }

            return _scheduleClockInstance;
        }
    }

    /// <summary>
    /// La tabella interpretata da <see cref="Rules"/>. Si ricostruisce se la stringa cambia: la cache
    /// viaggia con i campi della strategia, e una costruita prima di <c>Initialize</c> sarebbe quella
    /// sbagliata.
    /// </summary>
    private sbyte[,] RulesTable
    {
        get
        {
            var rules = Rules ?? string.Empty;
            if (_rulesTable is null || !string.Equals(_rulesSource, rules, StringComparison.Ordinal))
            {
                _rulesTable = ParseRules(rules);
                _rulesSource = rules;
            }

            return _rulesTable;
        }
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (HoldHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HoldHours), HoldHours,
                $"{Name}: HoldHours non puo' essere negativo.");
        }

        var table = RulesTable;

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0)
            return Hold(bar.Close, barTime);

        // L'apertura della barra su cui si entrerebbe, proiettata sul timeframe dichiarato, letta
        // sull'orologio dell'orario: ora piena e giorno feriale, altrimenti non c'e' regola.
        var entryOpen = barTime.AddMinutes(TimeframeMinutes);
        var hour = LocalClock.ToSessionTime(entryOpen).Hour;
        if (LocalClock.TimeOfDay(entryOpen) != new TimeOnly(hour, 0))
            return Hold(bar.Close, barTime);

        var day = ((int)LocalClock.SessionDay(entryOpen).DayOfWeek + 6) % 7;
        if (day >= Days)
            return Hold(bar.Close, barTime);

        var rule = table[day, hour];
        if (rule == 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // La barra pianificata deve esistere, come in HourOfDayEngine: si controlla solo sulle ore
        // che hanno una regola.
        if (Grid.IsSessionDay(Grid.SessionDayOf(entryOpen)) == false ||
            SessionMask.For(Symbol).Overlaps(entryOpen, TimeframeMinutes) == false)
        {
            return Hold(bar.Close, barTime);
        }

        var side = rule > 0 ? SignalType.Buy : SignalType.Sell;
        var signal = WithSessionExit(EntryMarketNextBar(side, bar.Close, data, barTime,
            side == SignalType.Buy ? "LE DXH" : "SE DXH"));
        if (signal is null)
            return Hold(bar.Close, barTime);

        if (HoldHours > 0)
        {
            // Tenuta in ora locale: l'orario di uscita resta quello anche la notte del cambio d'ora.
            var exit = LocalClock.ToUtc(LocalClock.ToSessionTime(entryOpen).AddHours(HoldHours));
            if (signal.CloseAtUtc is not { } earlier || exit < earlier)
                signal.CloseAtUtc = exit;
        }

        return signal;
    }

    /// <summary>
    /// Interpreta una tabella di regole: cella [giorno, ora] a +1 long, -1 short, 0 niente. Pubblica
    /// perche' test e studi devono poter validare una tabella senza far girare un backtest.
    /// </summary>
    /// <exception cref="ArgumentException">Un elemento malformato, fuori intervallo o ripetuto.</exception>
    public static sbyte[,] ParseRules(string? rules)
    {
        var table = new sbyte[Days, Hours];
        if (string.IsNullOrWhiteSpace(rules))
            return table;

        foreach (var raw in rules.Split(';'))
        {
            var element = raw.Trim();
            if (element.Length == 0)
                continue;

            var colon = element.IndexOf(':');
            var dash = element.IndexOf('-');
            if (colon < 0 || dash < 0 || dash > colon)
                throw Malformed(element, "la forma e' giorno-ora:L oppure giorno-ora:S");

            if (!int.TryParse(element.AsSpan(0, dash).Trim(), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var day) || day is < 0 or >= Days)
            {
                throw Malformed(element, "il giorno va da 0 (lunedi') a 4 (venerdi')");
            }

            if (!int.TryParse(element.AsSpan(dash + 1, colon - dash - 1).Trim(), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var hour) || hour is < 0 or >= Hours)
            {
                throw Malformed(element, "l'ora va da 0 a 23");
            }

            var sideText = element[(colon + 1)..].Trim();
            var side = sideText switch
            {
                "L" or "l" => (sbyte)1,
                "S" or "s" => (sbyte)-1,
                _ => throw Malformed(element, "il verso e' L (long) o S (short)")
            };

            if (table[day, hour] != 0)
                throw Malformed(element, "la coppia giorno-ora compare due volte");

            table[day, hour] = side;
        }

        return table;
    }

    private static ArgumentException Malformed(string element, string reason) =>
        new($"Regola DXH '{element}' non valida: {reason}.", nameof(Rules));
}
