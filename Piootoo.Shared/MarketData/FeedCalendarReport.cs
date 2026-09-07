using Piootoo.Shared.Models;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Cosa il layer ha da dire su una serie caricata da disco: su che griglia sta, quante sessioni
/// contiene, e quali barre non tornano con il calendario del suo simbolo.
///
/// <para><b>Perché serve.</b> Fino al 07/09/2026 un feed poteva essere sbagliato in modi che
/// nessun controllo vedeva, perché non producono barre <i>malformate</i> — producono barre
/// <i>diverse</i>, tutte plausibili. Le due che si sono viste davvero:</para>
///
/// <list type="bullet">
/// <item><description><b>Griglia sbagliata o doppia.</b> La deduplica di un feed raccolto è
/// sull'istante di apertura, quindi due raccolte con ancoraggi diversi non si sovrascrivono, si
/// sommano: <c>@KC_240</c> conteneva 1.760 barre, l'unione esatta di due ancoraggi, 880 ciascuno.
/// Un backtest ci girava sopra leggendo il doppio delle barre.</description></item>
/// <item><description><b>Sessioni che il future non ha.</b> Un CFD quota anche quando il future è
/// chiuso — tipicamente la domenica sera. Il dossier lo misura sul DAX: <b>11% del P&amp;L</b>,
/// perché quelle barre non generano trade ma spezzano la sessione e con l'uscita di fine sessione
/// chiudono posizioni ancora valide.</description></item>
/// </list>
///
/// <para><b>Descrive, non decide.</b> Non scarta niente e non ferma niente: conta e riporta. Il
/// giudizio — se un run vada fermato — sta nel consumatore, ed è giusto così finché anche il feed
/// del vendor ha barre fuori griglia (una all'anno per file giornaliero: la domenica in cui
/// l'orologio torna indietro, dove <c>aggregate_flat_feed.py</c> conta minuti locali su un istante
/// UTC).</para>
/// </summary>
public sealed record FeedCalendarReport
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>Barre analizzate.</summary>
    public int Bars { get; init; }

    /// <summary>Sessioni distinte che la serie copre.</summary>
    public int Sessions { get; init; }

    /// <summary>
    /// Barre la cui etichetta <b>non</b> coincide con l'inizio del bucket che le contiene, sulla
    /// griglia dichiarata dal calendario del simbolo. Zero è la condizione normale: qualunque
    /// numero diverso significa che la serie è nata su un ancoraggio diverso da quello su cui le
    /// strategie sono state trovate.
    /// </summary>
    public int BarsOffGrid { get; init; }

    /// <summary>
    /// Barre che cadono in un giorno in cui il calendario dichiara che lo strumento <b>non</b> ha
    /// sessione. Resta zero quando i giorni non sono dichiarati: non si inventa.
    /// </summary>
    public int BarsOnNonSessionDay { get; init; }

    /// <summary>Barre con OHLC tutti uguali: quotazione ferma, tipicamente su CFD.</summary>
    public int StaleBars { get; init; }

    /// <summary>
    /// Discontinuità nella serie, cioè quante volte fra due barre consecutive è passato più di un
    /// bucket. Non è un difetto: le pause di mercato, i fine settimana e i festivi sono buchi
    /// legittimi. È un numero da confrontare fra due feed dello stesso simbolo.
    /// </summary>
    public int Gaps { get; init; }

    public DateTime? FirstSessionId { get; init; }
    public DateTime? LastSessionId { get; init; }

    /// <summary>Il calendario dichiara i giorni di sessione di questo simbolo.</summary>
    public bool SessionDaysDeclared { get; init; }

    /// <summary>La griglia su cui il report è stato calcolato, per esteso.</summary>
    public string Grid { get; init; } = string.Empty;

    /// <summary>Vero quando c'è qualcosa che vale la pena guardare.</summary>
    public bool HasFindings => BarsOffGrid > 0 || BarsOnNonSessionDay > 0;

    /// <summary>
    /// Analizza una serie già ordinata e già in UTC. Non la modifica e non ne tiene copia: i
    /// contatori si accumulano in una passata, perché su <c>@NQ_15</c> sono mezzo milione di barre
    /// e trattenerne il contesto costerebbe più dell'analisi.
    /// </summary>
    public static FeedCalendarReport Analyze(
        SymbolCalendar calendar, int timeframeMinutes, IReadOnlyList<OhlcvData> bars)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        ArgumentNullException.ThrowIfNull(bars);

        var grid = new SessionGrid(calendar);
        var gridLabel =
            $"{timeframeMinutes}m ancorati a {calendar.SessionStartHour:00}:00 {calendar.ResearchTimeZone}";

        if (bars.Count == 0)
        {
            return new FeedCalendarReport
            {
                Symbol = calendar.Symbol,
                TimeframeMinutes = timeframeMinutes,
                SessionDaysDeclared = calendar.DeclaresSessionDays,
                Grid = gridLabel
            };
        }

        var segmenter = new SessionSegmenter(grid, timeframeMinutes);

        var offGrid = 0;
        var nonSessionDay = 0;
        var stale = 0;
        var gaps = 0;
        var sessions = 0;
        DateTime? first = null;
        DateTime? last = null;

        for (var i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            var context = segmenter.Next(new AggregatedBar(bar, MinuteCount: 0, Complete: false));

            if (context.StartsNewSession)
                sessions++;

            // La prima barra e' sempre "dopo un buco" perche' di cio' che c'era prima non si sa
            // nulla: contarla falserebbe il confronto fra due feed dello stesso simbolo.
            if (context.IsFirstBarAfterGap && i > 0)
                gaps++;

            if (grid.BucketStartUtc(bar.DateTime, timeframeMinutes) != bar.DateTime)
                offGrid++;

            if (context.IsSessionDay is false)
                nonSessionDay++;

            if (context.Quality.HasFlag(BarQualityFlags.Stale))
                stale++;

            first ??= context.SessionId;
            last = context.SessionId;
        }

        return new FeedCalendarReport
        {
            Symbol = calendar.Symbol,
            TimeframeMinutes = timeframeMinutes,
            Bars = bars.Count,
            Sessions = sessions,
            BarsOffGrid = offGrid,
            BarsOnNonSessionDay = nonSessionDay,
            StaleBars = stale,
            Gaps = gaps,
            FirstSessionId = first,
            LastSessionId = last,
            SessionDaysDeclared = calendar.DeclaresSessionDays,
            Grid = gridLabel
        };
    }
}
