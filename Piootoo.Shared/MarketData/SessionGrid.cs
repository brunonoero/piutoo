using Piootoo.Shared.Configuration;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// La griglia di sessioni e di bucket di un simbolo: dove comincia la sessione che contiene un
/// istante, e dove comincia il bucket di un dato timeframe.
///
/// <para><b>È il punto unico.</b> Fino al 07/09/2026 la stessa aritmetica viveva in quattro copie —
/// <c>minutes_into_bucket</c> in <c>aggregate_flat_feed.py</c> e <c>BucketStartUtc</c> in ciascuno
/// dei tre cBot — più la segmentazione implicita di <c>EasyLib.FullDaySessionDay</c>. Le copie non
/// erano nemmeno d'accordo: il Python prende la scorciatoia <c>openUtc − resto</c>, i cBot no, e le
/// due divergono sui quattro giorni all'anno in cui l'ora legale cambia.</para>
///
/// <para><b>L'orologio è quello della ricerca, non quello di borsa.</b> I bucket e il taglio delle
/// sessioni <c>d0..d5</c> sono ancorati a <see cref="SymbolCalendar.SessionStartHour"/> nel fuso
/// <see cref="SymbolCalendar.ResearchTimeZone"/> — la mezzanotte europea, o l'01:00 per FDAX, CC,
/// CT, KC e SB. Non è la sessione del broker né quella dell'exchange: è la scelta di modello che
/// la ricerca ha fatto, ed è quella che il port deve riprodurre.</para>
///
/// <para><b>Non è thread-safe</b>, come <see cref="SessionClock"/> che incapsula: una istanza per
/// consumatore.</para>
/// </summary>
public sealed class SessionGrid
{
    private readonly SessionClock _clock;

    public SessionGrid(SymbolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        Calendar = calendar;
        _clock = new SessionClock(calendar.ResearchTimeZone);
        RequireWholeHourOffsets(calendar);
    }

    /// <summary>
    /// Il fuso dell'ancoraggio deve avere scarti da UTC a <b>ore piene</b>. È ciò che rende
    /// identiche la griglia locale e quella UTC per ogni timeframe che divide l'ora, e quindi ciò
    /// che autorizza <see cref="BucketStartUtc"/> a calcolare in UTC sotto l'ora — dove il giro
    /// attraverso l'orario locale fonderebbe l'ora ambigua del ritorno all'ora solare.
    ///
    /// <para>Un fuso a mezz'ora (India, Nepal, Lord Howe) taglierebbe a metà le barre orarie e ogni
    /// bucket ne erediterebbe una in più o in meno, senza che niente lo segnali. Il cBot ha lo
    /// stesso controllo per la stessa ragione; qui vale per tutto il sistema.</para>
    /// </summary>
    private static void RequireWholeHourOffsets(SymbolCalendar calendar)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(calendar.ResearchTimeZone);

        // Gennaio e luglio: bastano a vedere entrambe le stagioni.
        foreach (var month in (int[])[1, 7])
        {
            var sample = new DateTime(DateTime.UtcNow.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var offset = zone.GetUtcOffset(sample);
            if (offset.Minutes == 0 && offset.Seconds == 0)
                continue;

            throw new MarketCalendarException(
                $"Il fuso '{calendar.ResearchTimeZone}' di {calendar.Symbol} ha uno scarto di " +
                $"{offset} da UTC il {sample:yyyy-MM-dd}, che non è un numero intero di ore. " +
                "I confini dei bucket cadrebbero dentro una barra da un minuto e le due griglie " +
                "— locale e UTC — smetterebbero di coincidere sotto l'ora.");
        }
    }

    /// <summary>Costruisce la griglia del simbolo dal calendario in vigore.</summary>
    public static SessionGrid For(string symbol) =>
        new(MarketCalendarRegistry.Current.Get(symbol));

    public SymbolCalendar Calendar { get; }

    /// <summary>Ora di inizio sessione, nell'orologio della ricerca.</summary>
    public int SessionStartHour => Calendar.SessionStartHour;

    /// <summary>
    /// Un timeframe è utilizzabile solo se divide il giorno. Un timeframe che non lo divide farebbe
    /// scivolare i bucket di giorno in giorno rispetto alla sessione, e due run sullo stesso feed
    /// darebbero barre diverse a seconda di dove hanno cominciato. È lo stesso rifiuto che
    /// l'aggregatore Python e i cBot applicano già; qui è scritto una volta.
    /// </summary>
    public static bool DividesTheDay(int timeframeMinutes) =>
        timeframeMinutes > 0 && 1440 % timeframeMinutes == 0;

    /// <summary>
    /// Il giorno di sessione a cui appartiene un istante: la data locale nell'orologio della
    /// ricerca, arretrata di un giorno quando l'istante cade <b>prima</b> dell'ancoraggio.
    ///
    /// <para>Il feed etichetta le barre sull'<b>apertura</b>, quindi la sessione ancorata a
    /// <c>h:00</c> va da <c>h:00</c> del giorno <c>D</c> a <c>h:00</c> del giorno <c>D+1</c>
    /// escluso, e ogni barra appartiene a una sessione. È la stessa regola di
    /// <c>EasyLib.FullDaySessionDay</c>, che questa funzione è destinata a sostituire.</para>
    /// </summary>
    public DateTime SessionDayOf(DateTime instantUtc)
    {
        var day = _clock.SessionDay(instantUtc);
        return _clock.Hhmm(instantUtc) >= SessionStartHour * 100 ? day : day.AddDays(-1);
    }

    /// <summary>Istante UTC in cui si apre la sessione del giorno <paramref name="sessionDay"/>.</summary>
    public DateTime SessionOpenUtc(DateTime sessionDay) =>
        _clock.ToUtc(sessionDay.Date.AddHours(SessionStartHour));

    /// <summary>
    /// Vero se il simbolo ha una sessione in quel giorno. <c>null</c> quando il calendario non
    /// dichiara i giorni: chi legge non deve inventarli.
    /// </summary>
    public bool? IsSessionDay(DateTime sessionDay) =>
        Calendar.HasSessionOn(sessionDay.DayOfWeek);

    /// <summary>
    /// Inizio del bucket a cui appartiene una barra che si apre in <paramref name="openUtc"/>.
    ///
    /// <para><b>La regola.</b> I bucket cadono ogni <paramref name="timeframeMinutes"/> a partire
    /// dall'ancoraggio nell'orologio della ricerca, e una barra appartiene al bucket che contiene
    /// la sua <b>apertura</b>. L'etichetta è l'inizio del bucket: è la chiave con cui il feed
    /// deduplica ed è la convenzione di tutto il sistema.</para>
    ///
    /// <para><b>Perché qui non compare il "meno un minuto"</b> di <c>aggregate_flat_feed.py</c> e
    /// del <c>resample_ohlcv</c> della ricerca: lì il timestamp di una riga è la <i>fine</i> del
    /// minuto, e quelle formule (<c>bin = (minuti − 1) / 240</c>, etichetta a
    /// <c>inizio + (bin+1)·240</c>) sono la stessa cosa scritta su etichette di chiusura. Contando
    /// dall'apertura i confini sono identici e l'etichetta è già quella giusta.</para>
    ///
    /// <para><b>Il confine si calcola in ora locale e si riconverte</b>, mai sottraendo il resto
    /// all'istante UTC. La scorciatoia conta minuti locali su un istante UTC e sul salto in avanti
    /// dell'ora legale scavalca l'ora che non esiste: la barra che apre alle 03:00 locali della
    /// domenica di marzo finisce in un bucket etichettato <i>prima</i> di quello della barra
    /// precedente, e un feed con due barre che si scavalcano non è più ordinato. Misurata su due
    /// anni di barre orarie <c>Europe/Rome</c>, la scorciatoia coincide su 34.988 righe su 35.088 e
    /// diverge sui quattro giorni di transizione.</para>
    ///
    /// <para><b>I secondi si buttano prima di contare.</b> Non tutti gli istanti che arrivano qui
    /// sono orari di barra: un confine di finestra si porta dietro secondi e frazioni, e la
    /// sottrazione toglie minuti interi, quindi quei secondi sopravvivrebbero e il confine
    /// uscirebbe a <c>inizio bucket + qualche secondo</c>. Nel cBot questo difetto ha prodotto un
    /// bucket monco ogni blocco di backfill: il 19,7% dei giornalieri e il 3% dei 4h
    /// dell'archivio FTMOPLATFORM raccolto con la 1.2.0.</para>
    /// </summary>
    public DateTime BucketStartUtc(DateTime openUtc, int timeframeMinutes)
    {
        if (!DividesTheDay(timeframeMinutes))
        {
            throw new MarketCalendarException(
                $"{timeframeMinutes} minuti non divide il giorno: i bucket scivolerebbero di " +
                "giorno in giorno rispetto alla sessione, e due run sullo stesso feed darebbero " +
                "barre diverse secondo dove hanno cominciato.");
        }

        var truncated = openUtc.AddTicks(-(openUtc.Ticks % TimeSpan.TicksPerMinute));

        // Fino all'ora il bucket si calcola in UTC, e non e' una scorciatoia: e' l'unica forma
        // corretta. I fusi del calendario hanno scarti a ore piene, quindi per un timeframe che
        // divide l'ora la griglia locale e quella UTC sono lo stesso insieme di confini — tranne
        // nell'ora che esiste DUE volte al ritorno dell'ora solare, dove il giro attraverso
        // l'orario locale manda entrambe le occorrenze sulla stessa etichetta e le fonde in una
        // barra sola. Sui future non si vede, perche' il cambio d'ora cade di domenica a mercato
        // chiuso; su BTC, che quota 24/7, si vede eccome: nell'archivio FTMOPLATFORM sono le barre
        // del 26/10/2025 dall'01:00 all'01:45 UTC, quattro sul 15 minuti, due sul 30, una sull'ora.
        // E' anche il comportamento che i file raccolti hanno gia', perche' fino a sessanta minuti
        // il cBot spedisce la serie nativa della piattaforma senza piegarla.
        if (60 % timeframeMinutes == 0)
            return truncated.AddMinutes(-(truncated.Minute % timeframeMinutes));

        // Oltre l'ora l'ancoraggio conta, quindi il confine si calcola sull'orologio locale e si
        // riconverte. L'ora ambigua resta un caso limite anche qui — un bucket locale di quel
        // giorno vale cinque ore invece di quattro — ma e' esattamente cio' che fa il motore di
        // ricerca, che segmenta sul giorno di calendario locale, e il port deve riprodurlo.
        var local = _clock.ToSessionTime(truncated);

        var minutesFromAnchor = (int)local.TimeOfDay.TotalMinutes - SessionStartHour * 60;
        if (minutesFromAnchor < 0)
            minutesFromAnchor += 1440;

        return _clock.ToUtc(local.AddMinutes(-(minutesFromAnchor % timeframeMinutes)));
    }
}
