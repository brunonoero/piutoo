using Piootoo.Shared.Models;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Difetti di qualità di una barra. Si <b>marcano</b>, non si eliminano: la strategia decide se
/// ignorarle, l'analisi vuole vederle.
///
/// <para>Oggi c'è un solo flag, ed è l'unico che <see cref="OhlcvData"/> permette di calcolare
/// senza inventare niente. Spread e conteggio tick — gli altri due criteri utili — non viaggiano
/// nella barra, quindi non si deducono: aggiungerli richiede prima portarli nel dato.</para>
/// </summary>
[Flags]
public enum BarQualityFlags
{
    None = 0,

    /// <summary>
    /// OHLC tutti uguali. Su un CFD è tipicamente una quotazione ferma, non un mercato fermo, ed è
    /// il segnale che il feed sta ripetendo l'ultimo prezzo invece di riportarne di nuovi. Su un
    /// future poco liquido può però essere una barra vera con uno scambio solo: è marcatura, non
    /// verdetto.
    /// </summary>
    Stale = 1
}

/// <summary>
/// Quel che il layer consegna alla strategia: la barra <b>più il contesto già calcolato</b>.
///
/// <para><b>Perché non solo la barra.</b> Se la strategia ricostruisce la sessione dai timestamp,
/// il calendario finisce duplicato in n posti e prima o poi divergono — che è la situazione da cui
/// questo refactor parte, con sei helper di <c>EasyEngineBase</c> che fanno aritmetica a giorni di
/// calendario (<c>AddDays(±1)</c>) e sbagliano ogni volta che di mezzo c'è un fine settimana o un
/// festivo. Il layer è l'unica fonte di verità sulle sessioni; la strategia consuma e basta.</para>
/// </summary>
/// <param name="Bar">La barra, etichettata sull'apertura, in UTC.</param>
/// <param name="SessionId">
/// La data della <b>sessione</b>, non del calendario, nell'orologio della ricerca. Su FDAX le due
/// quasi coincidono; su NQ la sessione comincia il giorno prima, e senza questo campo ogni logica
/// giornaliera si rompe in silenzio.
/// </param>
/// <param name="SessionOpenUtc">Istante di apertura della sessione a cui la barra appartiene.</param>
/// <param name="BarIndexInSession">Indice della barra dentro la sessione, 1-based.</param>
/// <param name="MinutesSinceSessionOpen">Minuti fra l'apertura della sessione e quella della barra.</param>
/// <param name="PreviousSessionCloseUtc">
/// Apertura dell'ultima barra della sessione precedente, o <c>null</c> se questa è la prima
/// sessione vista. È il riferimento con cui si misura un gap di riapertura.
/// </param>
/// <param name="IsSessionDay">
/// Se il calendario dichiara che il simbolo ha una sessione in questo giorno. <c>null</c> = giorni
/// non dichiarati. È il campo che permette a un CFD di non fabbricare la sessione domenicale che il
/// future non ha — misurata sul DAX all'11% del P&amp;L.
/// </param>
/// <param name="IsFirstBarAfterGap">
/// La barra precedente non è contigua a questa: fra le due c'è almeno un bucket senza dati. Vero
/// anche sulla prima barra della serie, perché di cosa c'era prima non si sa nulla.
/// </param>
/// <param name="Phase">Fase della giornata. <c>null</c> finché le fasi non sono popolate.</param>
/// <param name="MinuteCount">Minuti che hanno composto la barra. Vedi <see cref="AggregatedBar"/>.</param>
/// <param name="Complete">L'input copriva tutto l'arco del bucket. Vedi <see cref="AggregatedBar"/>.</param>
/// <param name="Quality">Difetti di qualità rilevati.</param>
public readonly record struct BarContext(
    OhlcvData Bar,
    DateTime SessionId,
    DateTime SessionOpenUtc,
    int BarIndexInSession,
    int MinutesSinceSessionOpen,
    DateTime? PreviousSessionCloseUtc,
    bool? IsSessionDay,
    bool IsFirstBarAfterGap,
    string? Phase,
    int MinuteCount,
    bool Complete,
    BarQualityFlags Quality)
{
    /// <summary>La barra apre una sessione nuova rispetto alla precedente.</summary>
    public bool StartsNewSession => BarIndexInSession == 1;

    /// <summary>Comodità: l'istante di apertura della barra.</summary>
    public DateTime OpenUtc => Bar.DateTime;
}

/// <summary>
/// Assegna a ogni barra la sessione a cui appartiene e il contesto che ne discende.
///
/// <para><b>Zero look-ahead.</b> Ogni campo dipende solo dalla barra corrente e da quelle già
/// viste. È una proprietà da difendere quando si aggiungeranno i flag di qualità: un quantile
/// calcolato sull'intero dataset è look-ahead travestito da statistica, e va sostituito da una
/// finestra rolling.</para>
///
/// <para><b>Non è thread-safe</b> e ha stato: una istanza per stream, come
/// <see cref="BarAggregator"/>.</para>
/// </summary>
public sealed class SessionSegmenter
{
    private readonly SessionGrid _grid;
    private readonly int _timeframeMinutes;

    private DateTime _sessionDay = DateTime.MinValue;
    private DateTime _sessionOpenUtc;
    private int _barIndex;
    private DateTime _previousBarUtc = DateTime.MinValue;
    private DateTime? _previousSessionCloseUtc;

    public SessionSegmenter(SymbolCalendar calendar, int timeframeMinutes)
        : this(new SessionGrid(calendar), timeframeMinutes)
    {
    }

    public SessionSegmenter(SessionGrid grid, int timeframeMinutes)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!SessionGrid.DividesTheDay(timeframeMinutes))
        {
            throw new ArgumentException(
                $"Timeframe {timeframeMinutes}: deve essere positivo e dividere il giorno.",
                nameof(timeframeMinutes));
        }

        _grid = grid;
        _timeframeMinutes = timeframeMinutes;
    }

    /// <summary>
    /// Contesto della barra successiva. Va chiamato in ordine cronologico: la segmentazione è una
    /// passata in avanti e non torna indietro.
    /// </summary>
    public BarContext Next(AggregatedBar aggregated)
    {
        var bar = aggregated.Bar;
        ArgumentNullException.ThrowIfNull(bar);

        if (_previousBarUtc != DateTime.MinValue && bar.DateTime <= _previousBarUtc)
        {
            throw new ArgumentException(
                $"Barra non ordinata: {bar.DateTime:O} non è successiva a {_previousBarUtc:O}. " +
                "La segmentazione è una passata in avanti.",
                nameof(aggregated));
        }

        var sessionDay = _grid.SessionDayOf(bar.DateTime);

        if (sessionDay != _sessionDay)
        {
            // Sessione nuova. L'ultima barra di quella prima diventa il riferimento con cui si
            // misura il gap di riapertura; sulla primissima sessione non c'e' nulla di precedente e
            // resta null, invece di far finta che la barra prima sia una chiusura.
            if (_previousBarUtc != DateTime.MinValue)
                _previousSessionCloseUtc = _previousBarUtc;

            _sessionDay = sessionDay;
            _sessionOpenUtc = _grid.SessionOpenUtc(sessionDay);
            _barIndex = 0;
        }

        _barIndex++;

        // Contiguita': fra due barre consecutive deve passare esattamente un bucket. Di piu' vuol
        // dire che in mezzo non ci sono dati — la pausa notturna, un festivo, il fine settimana,
        // oppure un invio perso. Il layer non distingue i due casi e non deve: dice che c'e' un
        // buco, e chi consuma decide.
        var firstAfterGap =
            _previousBarUtc == DateTime.MinValue ||
            bar.DateTime > _previousBarUtc.AddMinutes(_timeframeMinutes);

        var context = new BarContext(
            Bar: bar,
            SessionId: sessionDay,
            SessionOpenUtc: _sessionOpenUtc,
            BarIndexInSession: _barIndex,
            MinutesSinceSessionOpen: (int)(bar.DateTime - _sessionOpenUtc).TotalMinutes,
            PreviousSessionCloseUtc: _previousSessionCloseUtc,
            IsSessionDay: _grid.IsSessionDay(sessionDay),
            IsFirstBarAfterGap: firstAfterGap,
            Phase: null,
            MinuteCount: aggregated.MinuteCount,
            Complete: aggregated.Complete,
            Quality: QualityOf(bar));

        _previousBarUtc = bar.DateTime;
        return context;
    }

    /// <summary>Variante batch: la stessa macchina di <see cref="Next"/>, su un'intera serie.</summary>
    public IReadOnlyList<BarContext> Segment(IEnumerable<AggregatedBar> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);

        var result = new List<BarContext>();
        foreach (var bar in bars)
            result.Add(Next(bar));

        return result;
    }

    /// <summary>
    /// Variante per una serie già aggregata altrove, di cui non si sa come è stata costruita:
    /// <c>MinuteCount</c> resta a zero e <c>Complete</c> a falso, che è la sola risposta onesta.
    /// Serve a far passare dal segmentatore il feed già su disco senza fingere di sapere.
    /// </summary>
    public IReadOnlyList<BarContext> Segment(IEnumerable<OhlcvData> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);

        return Segment(bars.Select(bar => new AggregatedBar(bar, MinuteCount: 0, Complete: false)));
    }

    private static BarQualityFlags QualityOf(OhlcvData bar) =>
        bar.Open == bar.High && bar.High == bar.Low && bar.Low == bar.Close
            ? BarQualityFlags.Stale
            : BarQualityFlags.None;
}
