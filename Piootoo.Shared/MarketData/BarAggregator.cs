using Piootoo.Shared.Models;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Una barra aggregata, con quel che si sa su come è stata costruita.
/// </summary>
/// <param name="Bar">La barra, etichettata sull'inizio del bucket, intervallo semiaperto.</param>
/// <param name="MinuteCount">
/// Quante barre da un minuto l'hanno composta. È un fatto, non un giudizio: su un bucket da 240
/// minuti sarà quasi sempre minore di 240, perché il mercato ha pause — l'ora di manutenzione CME,
/// i festivi, la notte. Serve a distinguere un bucket denso da uno che contiene mezz'ora di nulla.
/// </param>
/// <param name="Complete">
/// Vero quando l'aggregatore ha visto <b>tutto l'arco</b> del bucket, cioè l'input cominciava a o
/// prima del suo inizio ed è arrivato a o oltre la sua fine.
///
/// <para><b>Non significa "senza buchi".</b> Un buco interno — il mercato chiuso — non rende
/// incompleto un bucket: il mercato chiuso non è storia mancante. Significa invece che il bucket
/// non è troncato dai bordi dell'input, ed è esattamente ciò che distingue una barra vera dalle due
/// che il metro trova sempre diverse: la prima di un feed, tagliata perché il journal comincia più
/// tardi, e l'ultima, ancora in formazione.</para>
/// </param>
public readonly record struct AggregatedBar(OhlcvData Bar, int MinuteCount, bool Complete);

/// <summary>
/// Piega barre da un minuto nei bucket di un timeframe, sulla griglia di
/// <see cref="SessionGrid"/>.
///
/// <para><b>Perché una sola implementazione.</b> È il punto in cui backtest interno, backtest su
/// feed di broker e sessione realtime devono per forza coincidere: se aggregano in due modi, le
/// barre su cui gira il live non sono quelle su cui è stato misurato il backtest, e nessun numero
/// lo segnala. Prima l'aggregazione era scritta due volte in due linguaggi (il Python del vendor e
/// il C# dei cBot) e una terza volta non era scritta affatto — in sessione il server accodava
/// quello che il cBot gli spingeva, qualunque griglia avesse.</para>
///
/// <para><b>L'API è incrementale</b>, e la variante batch la incapsula. Non è una comodità: è ciò
/// che rende letteralmente lo stesso codice quello che gira sul file storico e quello che gira sul
/// minuto che arriva adesso.</para>
///
/// <para><b>Non è thread-safe</b> e ha stato: una istanza per stream.</para>
/// </summary>
public sealed class BarAggregator
{
    private readonly SessionGrid _grid;
    private readonly int _timeframeMinutes;

    private DateTime _bucketStart = DateTime.MinValue;
    private DateTime _firstInputUtc = DateTime.MinValue;
    private DateTime _lastMinuteUtc = DateTime.MinValue;
    private bool _hasBucket;

    private decimal _open, _high, _low, _close, _volume;
    private int _minutes;

    public BarAggregator(SymbolCalendar calendar, int timeframeMinutes)
        : this(new SessionGrid(calendar), timeframeMinutes)
    {
    }

    public BarAggregator(SessionGrid grid, int timeframeMinutes)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!SessionGrid.DividesTheDay(timeframeMinutes))
        {
            throw new ArgumentException(
                $"Timeframe {timeframeMinutes}: deve essere positivo e dividere il giorno, " +
                "altrimenti i bucket scivolano rispetto alla sessione.",
                nameof(timeframeMinutes));
        }

        _grid = grid;
        _timeframeMinutes = timeframeMinutes;
    }

    /// <summary>Il timeframe prodotto, in minuti.</summary>
    public int TimeframeMinutes => _timeframeMinutes;

    /// <summary>C'è un bucket in formazione che <see cref="Flush"/> restituirebbe.</summary>
    public bool HasPendingBucket => _hasBucket;

    /// <summary>
    /// Aggiunge una barra da un minuto. Restituisce <c>true</c> — valorizzando
    /// <paramref name="closed"/> — quando questa barra appartiene a un bucket nuovo e quindi ne
    /// chiude uno precedente.
    ///
    /// <para>Un bucket chiuso così è sempre <see cref="AggregatedBar.Complete"/> dal lato destro:
    /// è arrivata una barra oltre la sua fine. Resta incompleto solo se era il primo e l'input
    /// cominciava dopo il suo inizio.</para>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// La barra non è successiva alla precedente. Un input non ordinato produrrebbe bucket
    /// sovrapposti senza che nulla lo segnali, ed è meglio fermarsi: la deduplica e l'ordinamento
    /// sono già compito di chi tiene il feed.
    /// </exception>
    public bool TryPush(OhlcvData minute, out AggregatedBar closed)
    {
        ArgumentNullException.ThrowIfNull(minute);

        var openUtc = minute.DateTime;
        if (_lastMinuteUtc != DateTime.MinValue && openUtc <= _lastMinuteUtc)
        {
            throw new ArgumentException(
                $"Barra non ordinata: {openUtc:O} non è successiva a {_lastMinuteUtc:O}. " +
                "Un input non ordinato produce bucket sovrapposti, e un feed con due barre che si " +
                "scavalcano non è più ordinato per nessuno a valle.",
                nameof(minute));
        }

        if (_firstInputUtc == DateTime.MinValue)
            _firstInputUtc = openUtc;
        _lastMinuteUtc = openUtc;

        var start = _grid.BucketStartUtc(openUtc, _timeframeMinutes);

        if (_hasBucket && start == _bucketStart)
        {
            Accumulate(minute);
            closed = default;
            return false;
        }

        var hadBucket = _hasBucket;
        var previous = hadBucket ? Materialize(rightEdgeSeen: true) : default;

        Start(start, minute);

        closed = previous;
        return hadBucket;
    }

    /// <summary>
    /// Chiude il bucket in formazione e lo restituisce, marcato <b>incompleto</b>: l'aggregatore
    /// non ha visto niente oltre la sua fine, quindi non può sapere se è finito.
    ///
    /// <para>In backtest si chiama a fine input, ed è il motivo per cui l'ultima barra di ogni
    /// serie aggregata risulta incompleta — cosa vera, ed è la barra in formazione. In live
    /// <b>non</b> si chiama: un bucket a metà spedito come chiuso è un dato falso che poi nessuno
    /// distingue da uno vero.</para>
    /// </summary>
    public AggregatedBar? Flush()
    {
        if (!_hasBucket)
            return null;

        var bar = Materialize(rightEdgeSeen: false);
        _hasBucket = false;
        return bar;
    }

    /// <summary>
    /// Variante batch: piega un'intera serie di minuti, compreso il bucket finale in formazione.
    /// È la stessa macchina di <see cref="TryPush"/>, non una seconda implementazione.
    /// </summary>
    public IReadOnlyList<AggregatedBar> Aggregate(IEnumerable<OhlcvData> minuteBars)
    {
        ArgumentNullException.ThrowIfNull(minuteBars);

        var result = new List<AggregatedBar>();
        foreach (var minute in minuteBars)
        {
            if (TryPush(minute, out var closed))
                result.Add(closed);
        }

        if (Flush() is { } last)
            result.Add(last);

        return result;
    }

    private void Start(DateTime bucketStart, OhlcvData minute)
    {
        _bucketStart = bucketStart;
        _open = minute.Open;
        _high = minute.High;
        _low = minute.Low;
        _close = minute.Close;
        _volume = minute.Volume;
        _minutes = 1;
        _hasBucket = true;
    }

    private void Accumulate(OhlcvData minute)
    {
        if (minute.High > _high) _high = minute.High;
        if (minute.Low < _low) _low = minute.Low;
        _close = minute.Close;
        _volume += minute.Volume;
        _minutes++;
    }

    private AggregatedBar Materialize(bool rightEdgeSeen)
    {
        // Completo = l'input copriva tutto l'arco del bucket. Il lato sinistro si perde solo sul
        // primo bucket della serie, quando il feed comincia a meta'; il destro solo sull'ultimo,
        // che e' la barra in formazione. Un buco INTERNO non conta: il mercato chiuso non e' storia
        // mancante, ed e' MinuteCount a dirlo.
        var leftEdgeSeen = _firstInputUtc <= _bucketStart;

        var bar = new OhlcvData
        {
            DateTime = _bucketStart,
            Timestamp = new DateTimeOffset(DateTime.SpecifyKind(_bucketStart, DateTimeKind.Utc))
                .ToUnixTimeSeconds(),
            DateTimeFormatted = _bucketStart.ToString("yyyy-MM-dd HH:mm:ss"),
            Open = _open,
            High = _high,
            Low = _low,
            Close = _close,
            Volume = _volume
        };

        return new AggregatedBar(bar, _minutes, leftEdgeSeen && rightEdgeSeen);
    }
}
