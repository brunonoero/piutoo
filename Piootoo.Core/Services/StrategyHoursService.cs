using System.Globalization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Core.Services;

/// <summary>
/// Costruisce la <see cref="StrategyHoursCard"/> di una strategia: i tre piani orari che decidono
/// quando opera, risolti nei fusi veri del suo simbolo.
///
/// <para><b>Perché è un servizio del server e non un calcolo del client.</b> Due dei tre piani
/// vengono dal calendario di mercato, che è un dato del repository: la console parla solo HTTP e
/// non ha modo di risolvere un fuso IANA a partire da un simbolo. Farglielo indovinare
/// ricostruirebbe la seconda fonte di verità che il calendario esiste per eliminare.</para>
///
/// <para><b>Le due stagioni non sono un vezzo.</b> Un orario locale è fisso e il suo istante UTC no:
/// un'ora di differenza fra gennaio e luglio è esattamente lo scarto che separa un run dal grafico
/// del broker, e mostrarne uno solo farebbe credere che il numero sia stabile.</para>
/// </summary>
public sealed class StrategyHoursService
{
    /// <summary>Giorno di riferimento per l'ora solare. La data conta solo per il fuso.</summary>
    private static readonly DateTime WinterReference = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>Giorno di riferimento per l'ora legale.</summary>
    private static readonly DateTime SummerReference = new(2026, 7, 15, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly string[] DayNames =
        ["Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab"];

    /// <summary>
    /// La scheda oraria della strategia identificata dal suo <b>Id di classe</b> o dal suo codice di
    /// esecuzione — <see cref="StrategyFactory.CreateStrategy"/> accetta entrambi.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Se l'identificativo non corrisponde a nessuna strategia.</exception>
    public StrategyHoursCard Build(string strategyId)
    {
        if (string.IsNullOrWhiteSpace(strategyId))
        {
            throw new KeyNotFoundException("Nessuna strategia richiesta: l'identificativo è vuoto.");
        }

        var instance = StrategyFactory.CreateStrategy(strategyId.Trim(), string.Empty, 0)
            ?? throw new KeyNotFoundException(
                $"Strategia '{strategyId}' inesistente: {StrategyFactory.DescribeUnusableId(strategyId.Trim())}.");

        var warnings = new List<string>();
        var barsPerSession = Math.Max(1, 1440 / Math.Max(1, instance.TimeframeMinutes));

        var card = new StrategyHoursCard
        {
            StrategyId = instance.GetType().Name,
            ExecutionCode = instance.Name,
            Symbol = instance.Symbol,
            TimeframeMinutes = instance.TimeframeMinutes,
            RequiredCandles = instance.RequiredCandles,
            RequiredCandlesInSessions = Math.Round((double)instance.RequiredCandles / barsPerSession, 1),
            HoldingLabel = ReadHolding(instance).Describe(),
            Warnings = warnings
        };

        if (!MarketCalendarRegistry.Current.TryGet(instance.Symbol, out var calendar))
        {
            warnings.Add(
                $"Il simbolo '{instance.Symbol}' non è nel calendario di mercato: senza i suoi fusi " +
                "gli orari qui sotto restano numeri senza orologio.");
            return card;
        }

        card.ResearchTimeZone = calendar.ResearchTimeZone;
        card.ExchangeTimeZone = calendar.ExchangeTimeZone;
        card.CalendarSessionStartHour = calendar.SessionStartHour;
        card.DeclaresSessionDays = calendar.DeclaresSessionDays;
        card.SessionDays = calendar.SessionDays is null
            ? []
            : Enum.GetValues<DayOfWeek>()
                .Where(calendar.SessionDays.Contains)
                .Select(day => DayNames[(int)day])
                .ToList();
        card.InstrumentTradingWindows = calendar.TradingWindows
            .Select(window => Describe(window, calendar))
            .ToList();

        if (calendar.TradingWindows.Count == 0)
        {
            warnings.Add(
                $"Il calendario non dichiara quando '{calendar.Symbol}' negozia: la maschera lascia " +
                "passare ogni barra del feed, comprese quelle che il future non avrebbe stampato.");
        }

        if (instance is not EasyEngineBase engine)
        {
            warnings.Add(
                $"'{card.StrategyId}' non deriva da un motore Unger: sessione e finestra operativa " +
                "non sono dichiarate in una forma leggibile da qui.");
            return card;
        }

        card.SessionAnchorOverrideReason = engine.SessionAnchorOverrideReason;
        card.SessionAnchorHour = engine.Session.StartHhmm / 100;
        card.Session = DescribeSession(engine.Session, calendar);

        if (engine.TradingWindow is { } window)
        {
            card.TradingWindow = Describe(window, calendar);
            card.TradingWindowNote = DescribeEffect(window);
        }
        else
        {
            card.TradingWindowNote =
                "Non dichiarata: la strategia può entrare per tutta la sessione. È il caso normale " +
                "dei run che non hanno filtrato per ora.";
        }

        return card;
    }

    /// <summary>
    /// Cosa la finestra operativa esclude, detto a parole.
    ///
    /// <para><b>Una finestra <c>0000-2359</c> non è un filtro</b>: è la forma in cui un run di
    /// ricerca che non ha filtrato per ora scrive <c>start_hour</c>/<c>end_hour</c>, e leggerla come
    /// "esclude dalle 23:59 alle 00:00" farebbe cercare un vincolo che non c'è. La distinzione conta
    /// perché è il caso di quasi metà delle finestre a 1440 del catalogo.</para>
    /// </summary>
    private static string DescribeEffect(ZonedWindow window)
    {
        if (window.StartHhmm == 0 && window.EndHhmm == 2359)
        {
            return "Copre tutta la sessione: il run di ricerca non ha filtrato per ora, " +
                   "quindi la finestra non esclude nulla.";
        }

        return window.CrossesMidnight
            ? $"Entra solo fra le {Hhmm(window.StartHhmm)} e le {Hhmm(window.EndHhmm)} del giorno dopo; " +
              "fuori da quella fascia nessun ingresso nasce."
            : $"Entra solo fra le {Hhmm(window.StartHhmm)} e le {Hhmm(window.EndHhmm)}; " +
              $"dalle {Hhmm(window.EndHhmm)} alle {Hhmm(window.StartHhmm)} nessun ingresso nasce.";
    }

    /// <summary>
    /// La sessione. La forma è sempre <c>(ancoraggio, 2359)</c>, cioè la giornata piena che parte
    /// dall'ancoraggio: l'etichetta lo dice a parole invece di stampare un <c>2359</c> che si legge
    /// come "finisce un minuto prima di mezzanotte" e non è quello che succede.
    /// </summary>
    private static StrategyHoursWindow DescribeSession(ZonedWindow session, SymbolCalendar calendar)
    {
        var zone = ZoneOf(session.Clock, calendar);
        var clock = new SessionClock(zone);
        var start = Hhmm(session.StartHhmm);

        return new StrategyHoursWindow
        {
            StartHhmm = session.StartHhmm,
            EndHhmm = session.EndHhmm,
            Clock = ClockName(session.Clock),
            TimeZoneId = zone,
            Label = $"{start} → {start} del giorno dopo · {zone} (orologio {ClockName(session.Clock)})",
            WinterUtc = $"inizio {InstantOf(clock, WinterReference, session.StartHhmm)}",
            SummerUtc = $"inizio {InstantOf(clock, SummerReference, session.StartHhmm)}"
        };
    }

    private static StrategyHoursWindow Describe(ZonedWindow window, SymbolCalendar calendar)
    {
        var zone = ZoneOf(window.Clock, calendar);
        var clock = new SessionClock(zone);

        return new StrategyHoursWindow
        {
            StartHhmm = window.StartHhmm,
            EndHhmm = window.EndHhmm,
            Clock = ClockName(window.Clock),
            TimeZoneId = zone,
            Label = $"{Hhmm(window.StartHhmm)} → {Hhmm(window.EndHhmm)} · {zone} " +
                    $"(orologio {ClockName(window.Clock)})",
            WinterUtc = $"{InstantOf(clock, WinterReference, window.StartHhmm)} → " +
                        $"{InstantOf(clock, WinterReference, window.EndHhmm)}",
            SummerUtc = $"{InstantOf(clock, SummerReference, window.StartHhmm)} → " +
                        $"{InstantOf(clock, SummerReference, window.EndHhmm)}"
        };
    }

    private static StrategyHoursInstrumentWindow Describe(TradingWindow window, SymbolCalendar calendar)
    {
        var days = Enum.GetValues<DayOfWeek>()
            .Where(window.OpensOn.Contains)
            .Select(day => DayNames[(int)day])
            .ToList();

        var open = DescribeEdge(window.Open, calendar);
        var close = DescribeEdge(window.Close, calendar);
        var period = window.IsAllYear ? "tutto l'anno" : $"dal {window.From} al {window.To}";

        return new StrategyHoursInstrumentWindow
        {
            Open = open,
            Close = close,
            OpensOn = days,
            From = window.From,
            To = window.To,
            Source = window.Source,
            Label = $"{open} → {close}" +
                    (window.Close.Hhmm <= window.Open.Hhmm ? " del giorno dopo" : string.Empty) +
                    $" · apre {(days.Count == 0 ? "mai" : string.Join(" ", days))} · {period}"
        };
    }

    /// <summary>
    /// Un bordo di finestra con il proprio ancoraggio. Sul FDAX l'apertura è fissa in UTC e la
    /// chiusura segue Berlino: scriverli entrambi allo stesso modo nasconderebbe proprio la
    /// differenza per cui il calendario dichiara l'ancoraggio bordo per bordo.
    /// </summary>
    private static string DescribeEdge(WindowEdge edge, SymbolCalendar calendar) =>
        edge.Anchor == PhaseAnchor.Utc
            ? $"{Hhmm(edge.Hhmm)} UTC"
            : $"{Hhmm(edge.Hhmm)} {calendar.ExchangeTimeZone}";

    /// <summary>
    /// L'istante UTC di un orario locale nel giorno di riferimento, detto rispetto a quel giorno:
    /// una sessione ancorata all'01:00 di Roma comincia il <i>giorno prima</i> in UTC per metà anno,
    /// e omettere lo scarto di data farebbe leggere "23:00Z" come se fosse la sera dello stesso
    /// giorno.
    /// </summary>
    private static string InstantOf(SessionClock clock, DateTime localDay, int hhmm)
    {
        var local = localDay.Date.AddHours(hhmm / 100).AddMinutes(hhmm % 100);
        var utc = clock.ToUtc(local);
        var shift = utc.Date.DayNumber() - localDay.Date.DayNumber();

        return utc.ToString("HH:mm", CultureInfo.InvariantCulture) + "Z" + shift switch
        {
            < 0 => " (giorno prima)",
            > 0 => " (giorno dopo)",
            _ => string.Empty
        };
    }

    private static string ZoneOf(InstrumentClock clock, SymbolCalendar calendar) =>
        clock == InstrumentClock.Exchange ? calendar.ExchangeTimeZone : calendar.ResearchTimeZone;

    private static string ClockName(InstrumentClock clock) =>
        clock == InstrumentClock.Exchange ? "borsa" : "ricerca";

    private static string Hhmm(int hhmm) =>
        $"{hhmm / 100:00}:{hhmm % 100:00}";

    /// <summary>
    /// La tenuta dichiarata dalla strategia, con lo stesso ripiego del catalogo: chi non deriva da
    /// un motore non ha la proprietà e vale multiday.
    /// </summary>
    private static StrategyHolding ReadHolding(ITradingStrategy instance)
    {
        var property = instance.GetType().GetProperty(
            "Holding",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        return property?.GetValue(instance) as StrategyHolding is { } holding
            ? holding.Normalized()
            : StrategyHolding.Multiday;
    }
}

/// <summary>Giorni assoluti di una data, per misurare lo scarto fra due giornate senza fusi di mezzo.</summary>
internal static class DateDayNumberExtensions
{
    public static int DayNumber(this DateTime date) =>
        DateOnly.FromDateTime(date).DayNumber;
}
