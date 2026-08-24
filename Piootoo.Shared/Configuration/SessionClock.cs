namespace Piootoo.Shared.Configuration;

/// <summary>
/// Converte l'istante UTC di una barra nell'ora in cui è espresso l'orario di sessione dello
/// strumento, ed è l'unico punto del sistema in cui un fuso diverso da UTC compare.
///
/// <para><b>Perché serve.</b> Le sorgenti EasyLanguage dichiarano la sessione come due numeri
/// <c>HHMM</c> — NQ <c>1700</c>→<c>1600</c>, GC <c>1800</c>→<c>1700</c>, FDAX <c>0800</c>→<c>2200</c>
/// — e quei numeri sono corretti nell'ora di borsa dello strumento, non in UTC. Confrontarli con
/// l'ora UTC della barra sposta il confine di sessione di sei o sette ore: misurato su @NQ,
/// significa il 20% di sessioni in più e i livelli <c>highd1</c>/<c>lowd1</c> diversi nel 97%
/// delle barre. Vedi <c>docs/decisioni.md</c>, voce del 2026-08-02.</para>
///
/// <para><b>Perché non basta riscrivere gli orari in UTC.</b> L'ora legale muove la sessione
/// rispetto a UTC: la riapertura di NQ (17:00 a Chicago) cade alle 23:00 UTC in ora solare e alle
/// 22:00 in ora legale. Le due finestre ammesse per il confine sono adiacenti e disgiunte, quindi
/// nessun valore UTC fisso è corretto tutto l'anno. L'unica forma esatta è tenere l'orario nell'ora
/// di borsa e convertire l'istante della barra al momento del confronto.</para>
///
/// <para><b>Non è thread-safe</b>, per scelta: memorizza l'offset dell'ultimo giorno visto, e
/// l'ipotesi è un'istanza per strategia, come già vale per il motore di trading. Il
/// <see cref="TimeZoneInfo"/> sottostante è immutabile e condiviso fra le istanze.</para>
/// </summary>
public sealed class SessionClock
{
    private static readonly Dictionary<string, TimeZoneInfo> Zones = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object ZonesLock = new();

    private readonly TimeZoneInfo _zone;

    // Offset dell'ultimo giorno UTC visto. Le barre arrivano in ordine, quindi una cache di un solo
    // elemento copre tutte le barre della giornata: senza di essa si pagherebbe una ricerca sul
    // fuso per ogni barra e per ogni strategia.
    private DateTime _cachedDay = DateTime.MinValue;
    private TimeSpan _cachedOffset;
    private bool _dayHasUniformOffset;

    public SessionClock(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Identificatore di fuso vuoto.", nameof(timeZoneId));

        TimeZoneId = timeZoneId;
        _zone = Resolve(timeZoneId);
    }

    /// <summary>Identificatore IANA, come dichiarato nel registro strumenti.</summary>
    public string TimeZoneId { get; }

    /// <summary>Orologio che non sposta nulla, per i test e per i dati già in ora di borsa.</summary>
    public static SessionClock Utc { get; } = new("UTC");

    /// <summary>
    /// Istante della barra letto nell'ora di borsa. Il valore in ingresso è interpretato come UTC
    /// qualunque sia il suo <see cref="DateTime.Kind"/>: nel dominio Piootoo tutte le date sono UTC,
    /// e un <c>Kind</c> perso in una serializzazione non deve cambiare il risultato.
    /// </summary>
    public DateTime ToSessionTime(DateTime instantUtc)
    {
        var day = instantUtc.Date;
        if (day != _cachedDay)
        {
            _cachedDay = day;
            _cachedOffset = OffsetOf(day);

            // Nei due giorni all'anno in cui l'ora legale cambia, l'offset non è costante per tutta
            // la giornata UTC: lì la cache non è utilizzabile, altrimenti metà delle barre di quel
            // giorno userebbe l'offset dell'altra metà.
            _dayHasUniformOffset = _cachedOffset == OffsetOf(day.AddDays(1).AddTicks(-1));
        }

        return _dayHasUniformOffset
            ? instantUtc + _cachedOffset
            : instantUtc + OffsetOf(instantUtc);
    }

    /// <summary>Ora del giorno in formato <c>HHMM</c>, come la legge EasyLanguage.</summary>
    public int Hhmm(DateTime instantUtc)
    {
        var local = ToSessionTime(instantUtc);
        return local.Hour * 100 + local.Minute;
    }

    /// <summary>
    /// Giorno di calendario in ora di borsa. Serve alla segmentazione delle sessioni tanto quanto
    /// <see cref="Hhmm"/>: il cambio di giorno è una delle condizioni che aprono una sessione nuova,
    /// e se lo si leggesse in UTC cadrebbe nel mezzo della sessione invece che nella pausa.
    /// </summary>
    public DateTime SessionDay(DateTime instantUtc) => ToSessionTime(instantUtc).Date;

    /// <summary>
    /// Istante UTC di un orario di borsa. E' la direzione inversa di <see cref="ToSessionTime"/> e
    /// serve alle scadenze dichiarate sul segnale — <c>CloseAtUtc</c>, l'inizio della sessione di
    /// appartenenza — che la strategia esprime in ora di borsa e l'engine deve confrontare in UTC.
    ///
    /// <para><b>I due giorni dell'anno in cui l'ora cambia.</b> Un orario locale puo' non esistere
    /// (la notte in cui l'orologio salta avanti) oppure esistere due volte (quando torna
    /// indietro). <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/> lancia nel
    /// primo caso, e nel secondo sceglie da solo senza dirlo. Qui l'orario inesistente viene
    /// spostato avanti dell'ampiezza del salto — la scadenza cade al primo istante che esiste
    /// davvero — e quello ambiguo viene risolto sull'offset precedente, cioe' la prima delle due
    /// occorrenze. Sono convenzioni, non verita': l'importante e' che siano dichiarate e stabili,
    /// perche' un'eccezione a runtime su una scadenza fermerebbe una sessione di trading.</para>
    /// </summary>
    public DateTime ToUtc(DateTime sessionLocal)
    {
        var local = DateTime.SpecifyKind(sessionLocal, DateTimeKind.Unspecified);

        if (_zone.IsInvalidTime(local))
        {
            // Salto in avanti: l'orario non esiste. Lo sposto dell'ampiezza della transizione,
            // che non e' sempre un'ora (Lord Howe usa mezz'ora).
            var delta = _zone.GetUtcOffset(local.AddDays(1)) - _zone.GetUtcOffset(local.AddDays(-1));
            local = local.Add(delta);
        }

        if (_zone.IsAmbiguousTime(local))
        {
            // Doppia occorrenza: prendo la prima, cioe' l'offset piu' grande fra quelli ammessi.
            var offsets = _zone.GetAmbiguousTimeOffsets(local);
            var scelto = offsets[0];
            foreach (var o in offsets)
            {
                if (o > scelto) scelto = o;
            }

            return DateTime.SpecifyKind(local - scelto, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, _zone);
    }

    /// <summary>
    /// Istante UTC dell'orario <paramref name="hhmm"/> nel giorno di borsa che contiene
    /// <paramref name="referenceUtc"/>. Sostituisce <c>EasyLib.CombineDateAndHhmm</c>, che
    /// componeva la data UTC della barra con un HHMM di borsa: due orologi diversi nello stesso
    /// <c>DateTime</c>.
    /// </summary>
    public DateTime SessionInstantUtc(DateTime referenceUtc, int hhmm) =>
        ToUtc(SessionDay(referenceUtc).AddMinutes(hhmm / 100 * 60 + hhmm % 100));

    private TimeSpan OffsetOf(DateTime instantUtc) =>
        _zone.GetUtcOffset(DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc));

    private static TimeZoneInfo Resolve(string timeZoneId)
    {
        lock (ZonesLock)
        {
            if (Zones.TryGetValue(timeZoneId, out var cached))
                return cached;

            TimeZoneInfo zone;
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (Exception error) when (
                error is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new InvalidOperationException(
                    $"Fuso '{timeZoneId}' non riconosciuto. Serve un identificatore IANA " +
                    "(es. 'America/Chicago'): .NET li accetta anche su Windows tramite ICU, ma un " +
                    "identificatore inventato qui falserebbe in silenzio ogni confine di sessione.",
                    error);
            }

            Zones[timeZoneId] = zone;
            return zone;
        }
    }
}
