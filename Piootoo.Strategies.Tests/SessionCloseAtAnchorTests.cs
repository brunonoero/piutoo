using System.Reflection;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La deadline di chiusura di fine sessione contro il <b>vero</b> confine della sessione.
///
/// <para><b>Cosa impongono.</b> La deadline si risolve <b>dentro la sessione</b> che contiene la
/// barra, e <c>2359</c> è la sentinella "ultimo minuto della sessione", non le 23:59 di un giorno.
/// Cade quindi sempre <b>un minuto</b> prima della chiusura della sessione, qualunque sia
/// l'ancoraggio.</para>
///
/// <para><b>Cosa correggono.</b> Fino al 07/09/2026 l'orario veniva risolto sul giorno di
/// <b>calendario</b> mentre la sessione è ancorata a <c>sessionStartHour</c>. Sui cinque simboli ad
/// ancoraggio 1 — FDAX, CC, CT, KC, SB — la deadline cadeva <b>61 minuti</b> prima della fine
/// sessione invece che uno, cioè <i>dentro</i> l'ultimo bucket di una 4h; e per una barra nella
/// fascia 00:00–01:00 locali cadeva <b>22h59m dopo</b> la chiusura della propria sessione, facendo
/// sopravvivere la posizione a una sessione intera. Il secondo caso non era raggiungibile con il
/// catalogo di allora — le quattro strategie coinvolte sono tutte a 240 minuti e nessun bucket apre
/// in quella fascia — ma lo sarebbe diventato con una 60 minuti intraday su uno di quei
/// simboli.</para>
/// </summary>
public sealed class SessionCloseAtAnchorTests(ITestOutputHelper output)
{
    private static DateTime Utc(string text) =>
        DateTime.Parse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal);

    /// <summary>
    /// Le strategie che chiudono davvero a fine sessione su un simbolo ad ancoraggio 1, cioè quelle
    /// che il difetto tocca: <c>IntradayOnly = true</c> più un motore che chiama
    /// <c>ResolveCloseAtUtc(validFrom, SessionEndTime)</c>.
    /// </summary>
    public static TheoryData<Type> AnchorOneWithSessionExit =>
    [
        typeof(PTS_FDAX_PCH_001_240),
        typeof(PTS_FDAX_VBO_001_240),
        typeof(PTS_CC_PCH_001_240),
        typeof(PTS_KC_SBO_001_240),
    ];

    public static TheoryData<Type> AnchorZeroWithSessionExit =>
    [
        typeof(PTS_NQ_PCH_001_15),
        typeof(PTS_ES_PCH_001_60),
    ];

    /// <summary>
    /// Ancoraggio 0: la deadline cade <b>un minuto</b> prima della fine sessione, che è esattamente
    /// la semantica di <c>2359</c> — l'ultimo minuto della sessione. Qui il modello funziona.
    /// </summary>
    [Theory]
    [MemberData(nameof(AnchorZeroWithSessionExit))]
    public void OnAnchorZeroTheDeadlineIsTheLastMinuteOfTheSession(Type type)
    {
        var (deadline, sessionClose, _) = Measure(type, Utc("2026-01-15T10:00:00Z"));

        Assert.Equal(TimeSpan.FromMinutes(1), sessionClose - deadline);
    }

    /// <summary>
    /// Ancoraggio 1: <b>un minuto</b>, come per l'ancoraggio 0. Prima erano 61, perché la sessione
    /// finisce all'01:00 del giorno dopo mentre <c>2359</c> veniva letto come le 23:59 del giorno di
    /// calendario. Toccava <c>PTS_FDAX_PCH_001_240</c>, <c>PTS_FDAX_VBO_001_240</c>,
    /// <c>PTS_CC_PCH_001_240</c> e <c>PTS_KC_SBO_001_240</c>, su ogni trade.
    /// </summary>
    [Theory]
    [MemberData(nameof(AnchorOneWithSessionExit))]
    public void OnAnchorOneTheDeadlineIsAlsoTheLastMinuteOfTheSession(Type type)
    {
        var (deadline, sessionClose, _) = Measure(type, Utc("2026-01-15T10:00:00Z"));

        Assert.Equal(TimeSpan.FromMinutes(1), sessionClose - deadline);
    }

    /// <summary>
    /// Il caso che prima sbagliava di più: una barra nella fascia <b>00:00–01:00 locali</b>
    /// appartiene alla sessione <i>precedente</i> mentre il suo giorno di calendario è già quello
    /// dopo. La deadline cadeva 22h59m <b>dopo</b> la chiusura della propria sessione; ora cade un
    /// minuto prima, come tutte.
    /// </summary>
    [Theory]
    [MemberData(nameof(AnchorOneWithSessionExit))]
    public void OnAnchorOneABarInTheFirstHourClosesWithItsOwnSession(Type type)
    {
        // 2026-01-14 23:30Z = 00:30 locali del 15 gennaio (Europe/Rome e' UTC+1 in gennaio).
        var (deadline, sessionClose, _) = Measure(type, Utc("2026-01-14T23:30:00Z"));

        Assert.Equal(TimeSpan.FromMinutes(1), sessionClose - deadline);
    }

    private (DateTime Deadline, DateTime SessionClose, DateTime SessionOpen) Measure(
        Type type, DateTime barUtc)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;
        var calendar = MarketCalendarRegistry.Current.Get(strategy.Symbol);
        var grid = new SessionGrid(calendar);

        var end = (int)FindProperty(type, "SessionEndTime").GetValue(strategy)!;
        var deadline = (DateTime)FindMethod(type, "ResolveCloseAtUtc").Invoke(strategy, [barUtc, end])!;

        var sessionDay = grid.SessionDayOf(barUtc);
        var sessionOpen = grid.SessionOpenUtc(sessionDay);
        var sessionClose = grid.SessionOpenUtc(sessionDay.AddDays(1));

        output.WriteLine(
            $"{type.Name} [ancoraggio {calendar.SessionStartHour:00}:00, SessionEndTime {end}]");
        output.WriteLine($"    barra      {barUtc:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"    sessione   {sessionOpen:yyyy-MM-dd HH:mm}Z -> {sessionClose:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"    CloseAtUtc {deadline:yyyy-MM-dd HH:mm}Z  (scarto dalla chiusura: {sessionClose - deadline})");

        return (deadline, sessionClose, sessionOpen);
    }

    private static MethodInfo FindMethod(Type type, string name)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic |
                                      BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (m is not null) return m;
        }

        throw new MissingMethodException(type.Name, name);
    }

    private static PropertyInfo FindProperty(Type type, string name)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic |
                                        BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (p is not null) return p;
        }

        throw new MissingMemberException(type.Name, name);
    }
}
