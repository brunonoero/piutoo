namespace Piootoo.Shared.Configuration;

/// <summary>
/// Finestra oraria che dichiara il proprio fuso: <c>(fuso IANA, HHMM di inizio, HHMM di fine)</c>.
///
/// <para><b>Perché esiste.</b> Un <c>1700</c> da solo è un numero di cui nessuno sa il fuso, e
/// questo ha già prodotto due classi di errori. Il primo: gli orari venivano confrontati con
/// l'ora grezza della barra, quindi il risultato dipendeva da come il feed era stampato — e il
/// feed <c>@NQ</c> è stampato in ora europea pur essendo etichettato <c>Z</c>. Il secondo: i run
/// di ricerca Python scrivono le finestre operative <b>sempre in CET</b>, per ogni simbolo, e
/// costringerle nell'ora di borsa dello strumento obbligava a convertirle a mano — meno sette ore
/// per NQ, meno sei per GC — con il risultato di essere esatti tranne nelle settimane in cui
/// l'ora legale americana ed europea non sono allineate.</para>
///
/// <para><b>La regola.</b> Una strategia non legge mai l'ora di una barra: legge il suo
/// <b>istante</b> e lo confronta con una finestra che dichiara in che orologio è scritta. Il
/// confronto passa da <see cref="SessionClock"/>, che è l'unico punto del sistema in cui compare
/// un fuso diverso da UTC.</para>
///
/// <para><b>Due finestre, due fusi.</b> Una strategia ne dichiara normalmente due e non devono
/// coincidere: il <b>confine di sessione</b>, che vive nell'ora di borsa dello strumento e governa
/// gli OHLC <c>d0..d5</c>, il limite di ingressi per sessione e la chiusura di fine sessione; e la
/// <b>finestra operativa</b>, che vive nell'orologio in cui la ricerca l'ha scritta. Il valore di
/// questo tipo è proprio che i due fusi non vadano più riconciliati a mano.</para>
///
/// <para><b>Il nome non contiene "sessione"</b> di proposito: in
/// <c>Piootoo.Shared/Models/Trading/TradingSessionContracts.cs</c> "sessione" indica già il run di
/// trading applicativo, e propagare la collisione costerebbe più che scegliere un altro nome.</para>
/// </summary>
/// <param name="StartHhmm">Orario di inizio in formato <c>HHMM</c>, nel fuso dichiarato.</param>
/// <param name="EndHhmm">Orario di fine in formato <c>HHMM</c>, nel fuso dichiarato.</param>
/// <param name="TimeZoneId">
/// Identificatore IANA (es. <c>America/Chicago</c>, <c>Europe/Rome</c>). <c>null</c> significa
/// "non dichiarato": chi la usa ricade sul fuso di borsa del simbolo, che è la compatibilità con
/// le classi non ancora migrate. Una strategia nuova lo dichiara sempre.
/// </param>
public sealed record ZonedWindow(int StartHhmm, int EndHhmm, string? TimeZoneId = null)
{
    /// <summary>Fuso in cui i run di ricerca Python scrivono le finestre operative, per ogni simbolo.</summary>
    public const string ResearchTimeZone = "Europe/Rome";

    /// <summary>
    /// Fuso della borsa degli indici CME. <b>Non</b> serve alle strategie portate dai run di
    /// ricerca — quelle usano <see cref="ResearchSession"/> — ma a una strategia che volesse
    /// davvero ancorarsi alla sessione del broker.
    /// </summary>
    public const string CmeChicago = "America/Chicago";

    /// <summary>Fuso di COMEX e NYMEX. Vale la nota di <see cref="CmeChicago"/>.</summary>
    public const string NyComexNymex = "America/New_York";

    /// <summary>
    /// <b>Il confine di sessione dei run di ricerca.</b> Non è la sessione del broker: il motore
    /// Python taglia le sessioni con
    /// <c>(timestamp − 1 minuto − session_start_hour).normalize()</c>, cioè per
    /// <c>session_start_hour = 0</c> il <b>giorno di calendario europeo</b>, 00:00 → 00:00 — non la
    /// sessione CME 17:00→16:00 di New York.
    ///
    /// <para>È una scelta di modello dichiarata dalla ricerca, ed è quella che il port deve
    /// riprodurre. Le due coincidono per gran parte dell'anno — mezzanotte a Roma sono le 17:00 a
    /// Chicago — ma <b>non</b> nelle settimane in cui l'ora legale americana ed europea non sono
    /// allineate, ed è lì che dichiarare la sessione di borsa fa divergere il port dalla fonte.</para>
    ///
    /// <para>Il <c>−1 minuto</c> ha una conseguenza precisa: la barra delle <c>00:00</c> appartiene
    /// alla sessione <b>precedente</b>. <c>EasyLib.OHLCMulti5</c> la riproduce con il confronto
    /// stretto <c>t &gt; sessionStartTime</c>.</para>
    /// </summary>
    public static ZonedWindow ResearchSession(int sessionStartHour = 0) =>
        new(sessionStartHour * 100, 2359, ResearchTimeZone);

    /// <summary>
    /// Finestra scritta nell'orologio della ricerca. È la forma in cui vanno riportati
    /// <c>start_hour</c>/<c>end_hour</c> di <c>parametri.csv</c>: <b>verbatim, senza convertirli</b>.
    ///
    /// <para>Il filtro orario del motore Python confronta l'orario <b>della barra stessa</b> con la
    /// finestra (<c>filters.py</c>: <c>minuti = index.hour * 60 + index.minute</c>), senza alcun
    /// riferimento a dove inizi la sessione. Le due cose sono indipendenti.</para>
    /// </summary>
    public static ZonedWindow Research(int startHhmm, int endHhmm) =>
        new(startHhmm, endHhmm, ResearchTimeZone);

    /// <summary>Come <see cref="Research(int,int)"/> ma partendo dalle ore piene dei run.</summary>
    public static ZonedWindow ResearchHours(int startHour, int endHour) =>
        new(startHour * 100, endHour * 100, ResearchTimeZone);

    /// <summary>
    /// Vero quando la finestra attraversa la mezzanotte, cioè quando l'orario di fine è minore di
    /// quello di inizio. Non è un caso degenere: è la forma normale delle finestre serali.
    /// </summary>
    public bool CrossesMidnight => StartHhmm > EndHhmm;

    /// <summary>Vero quando il fuso è dichiarato, cioè quando la finestra si spiega da sé.</summary>
    public bool HasDeclaredTimeZone => !string.IsNullOrWhiteSpace(TimeZoneId);

    public override string ToString() =>
        $"{StartHhmm:0000}->{EndHhmm:0000} {(HasDeclaredTimeZone ? TimeZoneId : "(fuso del simbolo)")}";
}
