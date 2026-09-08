namespace Piootoo.Shared.MarketData;

/// <summary>
/// Un bordo di una finestra di negoziazione: un orario e l'orologio in cui va letto.
///
/// <para><b>Perché l'ancoraggio sta sul bordo e non sulla finestra.</b> Sul FDAX l'apertura
/// notturna è agganciata a Singapore ed è fissa alle <b>00:15 UTC</b>, mentre la chiusura segue
/// l'orologio di Berlino e resta alle 22:00 locali. Misurato sul minuto del vendor, 2024+: il primo
/// minuto cade alle 00:16 UTC (etichetta a fine minuto) <i>in entrambe le stagioni</i>, mentre
/// l'ultimo si sposta — 21:02 UTC d'inverno, 20:01 d'estate. Due ancoraggi diversi dentro la stessa
/// finestra: modellarli entrambi in locale sbaglia l'apertura per metà anno, entrambi in UTC
/// sbaglia tutto il resto.</para>
/// </summary>
/// <param name="Hhmm">Orario come <c>HHMM</c>, nell'orologio di <paramref name="Anchor"/>.</param>
/// <param name="Anchor">In che orologio è scritto <paramref name="Hhmm"/>.</param>
public readonly record struct WindowEdge(int Hhmm, PhaseAnchor Anchor);

/// <summary>
/// La finestra in cui lo strumento <b>negozia davvero</b>: quando si apre una giornata di
/// negoziazione, quando si chiude, e in che giorni può aprirsene una.
///
/// <para><b>A cosa serve.</b> Un feed CFD quota anche quando il future è chiuso, e quelle barre non
/// vanno alle strategie: non generano trade da sole ma spezzano la sessione, spostano
/// <c>O_d0</c>/<c>H_d0</c>/<c>L_d0</c> e fanno avanzare l'orologio a barre. Misurato sull'archivio
/// <c>FTMOPLATFORM</c>: BTC quota il sabato e l'ora di manutenzione CME (17,6% delle sue barre),
/// FDAX l'ora prima dell'apertura Eurex e quella dopo la chiusura (11,0%), BP gira 24/5 continuo
/// perché è un CFD FX e non un CFD su future (3,5%). Sugli altri undici simboli dell'archivio la
/// finestra non toglie <b>nessuna</b> barra: il broker rispetta già l'orario del future.</para>
///
/// <para><b>Gli orari sono in ora di borsa, l'ora legale non si dichiara.</b> È il fuso a
/// risolverla. Le quattro configurazioni che nascono dal disallineamento fra ora legale europea e
/// americana — le settimane in cui il blackout comune si sposta di un'ora — sono una
/// <i>conseguenza</i> di due fusi e due ancoraggi, non quattro periodi da scrivere: dichiararle per
/// data sarebbe una copia delle regole DST dentro il repository, e le date cambiano ogni anno.</para>
///
/// <para><b><see cref="From"/>/<see cref="To"/> servono ad altro:</b> ai cambi di orario che una
/// borsa decide, non a quelli che il calendario gregoriano impone. Oggi nessun simbolo ne ha
/// bisogno — le finestre in ora di borsa sono stabili tutto l'anno, verificato sui dati — e ogni
/// simbolo ne dichiara una sola.</para>
/// </summary>
public sealed record TradingWindow
{
    /// <summary>Apertura della giornata di negoziazione.</summary>
    public required WindowEdge Open { get; init; }

    /// <summary>
    /// Chiusura. Quando cade allo stesso orario o prima dell'apertura la finestra <b>scavalca la
    /// mezzanotte</b> e chiude il giorno dopo: è la forma normale dei future CME
    /// (<c>17:00 → 16:00</c>) e del cotone ICE (<c>21:00 → 14:20</c>), non un caso degenere.
    /// </summary>
    public required WindowEdge Close { get; init; }

    /// <summary>
    /// I giorni, letti sul calendario dell'<b>ora di borsa</b>, in cui una giornata di negoziazione
    /// può aprirsi.
    ///
    /// <para><b>Non è <see cref="SymbolCalendar.SessionDays"/>, e non va confuso con esso.</b>
    /// Quello è nell'orologio della <i>ricerca</i> e dice quali sessioni <c>d0..d5</c> esistono;
    /// questo è nell'orologio della <i>borsa</i> e dice quando il mercato apre. Sul cotone i due
    /// danno risposte diverse e sono entrambe giuste: la giornata che si apre domenica alle 21:00 di
    /// New York è la sessione di <b>lunedì</b> della ricerca, ed è per questo che il dossier del
    /// paniere conta zero sessioni domenicali su CT. Scrivere questa lista nell'orologio della
    /// ricerca avrebbe rotto CT senza che niente lo segnalasse.</para>
    /// </summary>
    public required IReadOnlySet<DayOfWeek> OpensOn { get; init; }

    /// <summary>Primo giorno dell'anno in cui la finestra vale, come <c>MM-dd</c>. <c>null</c> = sempre.</summary>
    public string? From { get; init; }

    /// <summary>Ultimo giorno dell'anno in cui la finestra vale, come <c>MM-dd</c>. <c>null</c> = sempre.</summary>
    public string? To { get; init; }

    /// <summary>Ragione della scelta e come è stata accertata. Facoltativa, solo documentazione.</summary>
    public string? Source { get; init; }

    /// <summary>Vero quando la finestra vale tutto l'anno.</summary>
    public bool IsAllYear => From is null && To is null;
}
