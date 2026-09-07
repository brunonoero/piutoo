namespace Piootoo.Shared.Configuration;

/// <summary>
/// In quale dei due orologi di uno strumento sono scritti gli orari di una finestra.
///
/// <para><b>Perché un simbolo ha due orologi.</b> <c>1700</c> in una sorgente EasyLanguage su NQ
/// sono le 17:00 di <i>Chicago</i>; <c>17</c> in un report di ricerca sullo stesso NQ sono le 17:00
/// <i>CET</i>. Sei o sette ore di differenza, stesso simbolo. Il calendario di mercato dichiara
/// entrambi (<c>exchangeTz</c> e <c>researchTz</c>) e la finestra dice quale dei due usare.</para>
///
/// <para><b>Perché non basta il simbolo.</b> L'orologio di una finestra è una proprietà della sua
/// <b>provenienza</b>, non dello strumento: due strategie sullo stesso simbolo possono venire una
/// da un run di ricerca e una da una sorgente EasyLanguage. Se il tipo non sapesse esprimere la
/// differenza, portare la seconda costringerebbe a convertire gli orari a mano — che è esatto
/// <i>tranne</i> nelle settimane in cui l'ora legale americana ed europea non sono allineate, ed è
/// già stato la causa di una divergenza reale. Vedi
/// <c>docs/domini/porting-da-report-sweep.md</c> §2.1ter.</para>
/// </summary>
public enum InstrumentClock
{
    /// <summary>
    /// L'orologio in cui i run di ricerca Python hanno scritto le finestre operative: CET, <b>per
    /// ogni simbolo</b>, qualunque sia la borsa dello strumento. È il caso di tutte le
    /// <c>PTS_*</c> del catalogo.
    /// </summary>
    Research = 0,

    /// <summary>
    /// L'ora di borsa dello strumento, cioè l'orologio in cui tornano gli orari delle sorgenti
    /// EasyLanguage (<c>SessBegin = 1700</c>, <c>StartTrade</c>/<c>EndTrade</c>). Nessuna strategia
    /// del catalogo la usa oggi: esiste perché portarne una senza poterlo dichiarare
    /// obbligherebbe alla conversione a mano.
    /// </summary>
    Exchange = 1
}

/// <summary>
/// Finestra oraria che dichiara <b>in quale orologio</b> è scritta: <c>(HHMM di inizio, HHMM di
/// fine, orologio)</c>. Il fuso vero — l'identificatore IANA — non sta qui: lo dichiara il
/// calendario di mercato del simbolo, che è l'unico posto in cui un fuso è verificato.
///
/// <para><b>Perché esiste.</b> Un <c>1700</c> da solo è un numero di cui nessuno sa l'orologio, e
/// questo ha già prodotto due classi di errori. Il primo: gli orari venivano confrontati con l'ora
/// grezza della barra, quindi il risultato dipendeva da come il feed era stampato — e il feed
/// <c>@NQ</c> è stampato in ora europea pur essendo etichettato <c>Z</c>. Il secondo: i run di
/// ricerca scrivono le finestre operative <b>sempre in CET</b>, per ogni simbolo, e costringerle
/// nell'ora di borsa dello strumento obbligava a convertirle a mano — meno sette ore per NQ, meno
/// sei per GC — con il risultato di essere esatti tranne nelle settimane in cui l'ora legale
/// americana ed europea non sono allineate.</para>
///
/// <para><b>La regola.</b> Una strategia non legge mai l'ora di una barra: legge il suo
/// <b>istante</b> e lo confronta con una finestra che dichiara in che orologio è scritta. Il
/// confronto passa da <see cref="SessionClock"/>, che è l'unico punto del sistema in cui compare un
/// fuso diverso da UTC.</para>
///
/// <para><b>Il fuso non è più una stringa.</b> Fino al 07/09/2026 questo tipo portava un
/// identificatore IANA libero, duplicando quello che il calendario dichiara già per simbolo — con
/// la possibilità che i due divergessero, e che un refuso spostasse in silenzio ogni confronto
/// orario. Ora la finestra dichiara <see cref="InstrumentClock"/> e il fuso lo risolve chi conosce il
/// simbolo.</para>
///
/// <para><b>Il nome non contiene "sessione"</b> di proposito: in
/// <c>Piootoo.Shared/Models/Trading/TradingSessionContracts.cs</c> "sessione" indica già il run di
/// trading applicativo, e propagare la collisione costerebbe più che scegliere un altro nome.</para>
/// </summary>
/// <param name="StartHhmm">Orario di inizio in formato <c>HHMM</c>, nell'orologio dichiarato.</param>
/// <param name="EndHhmm">Orario di fine in formato <c>HHMM</c>, nell'orologio dichiarato.</param>
/// <param name="Clock">In quale dei due orologi dello strumento sono scritti i due orari.</param>
public sealed record ZonedWindow(int StartHhmm, int EndHhmm, InstrumentClock Clock = InstrumentClock.Research)
{
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
    /// alla sessione <b>precedente</b>. <c>SessionGrid.SessionDayOf</c> la riproduce.</para>
    ///
    /// <para><b>Non si chiama più da una strategia:</b> l'ancoraggio lo dichiara il calendario del
    /// simbolo. Resta il modo in cui l'engine costruisce la finestra di sessione una volta risolto
    /// l'ancoraggio.</para>
    /// </summary>
    public static ZonedWindow ResearchSession(int sessionStartHour = 0) =>
        new(sessionStartHour * 100, 2359);

    /// <summary>
    /// Finestra scritta nell'orologio della ricerca. È la forma in cui vanno riportati
    /// <c>start_hour</c>/<c>end_hour</c> di <c>parametri.csv</c>: <b>verbatim, senza convertirli</b>.
    ///
    /// <para>Il filtro orario del motore Python confronta l'orario <b>della barra stessa</b> con la
    /// finestra (<c>filters.py</c>: <c>minuti = index.hour * 60 + index.minute</c>), senza alcun
    /// riferimento a dove inizi la sessione. Le due cose sono indipendenti.</para>
    /// </summary>
    public static ZonedWindow Research(int startHhmm, int endHhmm) =>
        new(startHhmm, endHhmm);

    /// <summary>Come <see cref="Research(int,int)"/> ma partendo dalle ore piene dei run.</summary>
    public static ZonedWindow ResearchHours(int startHour, int endHour) =>
        new(startHour * 100, endHour * 100);

    /// <summary>
    /// Finestra scritta nell'<b>ora di borsa</b> dello strumento: è la forma degli orari di una
    /// sorgente EasyLanguage (<c>StartTrade</c>/<c>EndTrade</c>). Nessuna strategia del catalogo la
    /// usa oggi.
    ///
    /// <para>Gli orari si riportano <b>verbatim</b> anche qui: è il <see cref="Clock"/> a dire in
    /// che orologio leggerli, e convertirli a mano nell'orologio della ricerca sarebbe lo stesso
    /// errore fatto nel verso opposto.</para>
    /// </summary>
    public static ZonedWindow Exchange(int startHhmm, int endHhmm) =>
        new(startHhmm, endHhmm, InstrumentClock.Exchange);

    /// <summary>
    /// Vero quando la finestra attraversa la mezzanotte, cioè quando l'orario di fine è minore di
    /// quello di inizio. Non è un caso degenere: è la forma normale delle finestre serali.
    /// </summary>
    public bool CrossesMidnight => StartHhmm > EndHhmm;

    public override string ToString() =>
        $"{StartHhmm:0000}->{EndHhmm:0000} ({(Clock == InstrumentClock.Exchange ? "ora di borsa" : "orologio della ricerca")})";
}
