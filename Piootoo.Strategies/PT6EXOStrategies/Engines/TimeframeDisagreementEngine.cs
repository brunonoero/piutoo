using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore MTF: <b>timeframe in disaccordo</b>. Il timeframe alto dice il verso, quello basso
/// l'occasione: con il trend alto su si compra l'ipervenduto del timeframe basso, con il trend alto
/// giu' si vende l'ipercomprato. E' il ritracciamento dentro un trend, non l'inseguimento del trend:
/// guarda due orologi insieme, ed e' per questo che sta nella serie PT6EXO. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §MTF.
///
/// <para><b>Il trend alto.</b> La chiusura dell'ultima barra alta <b>chiusa</b> contro la media
/// semplice delle ultime <see cref="TrendBars"/> chiusure alte chiuse, quella compresa: sopra e' trend
/// su, sotto trend giu', uguale nessun trend. La serie alta arriva come timeframe aggiuntivo
/// (<see cref="AdditionalTimeframes"/>, <see cref="HigherTimeframeMinutes"/>).</para>
///
/// <para><b>La barra alta in corso non si legge.</b> In backtest la serie aggiuntiva arriva da un
/// cursore che restituisce ogni barra la cui <i>apertura</i> e' passata
/// (<c>CandleWindowCursor.Window</c>): la barra giornaliera di oggi c'e' gia', con il massimo, il minimo e
/// la chiusura di fine giornata — il futuro. La regola e' quindi scritta qui e non affidata a chi
/// consegna la serie: una barra alta conta solo se la sua chiusura cade entro la chiusura della barra
/// bassa di segnale. La chiusura e' l'apertura piu' <see cref="HigherTimeframeMinutes"/> e, per la
/// giornaliera, non prima della fine della sua sessione sulla griglia del simbolo: nel giorno del
/// cambio d'ora che dura 25 ore l'apertura piu' 24 ore cadrebbe un'ora prima della chiusura vera. Nel
/// giorno da 23 ore la regola e' prudente di un'ora, mai in anticipo. La barra bassa che chiude
/// insieme alla barra alta la vede gia' chiusa: le due chiusure sono lo stesso istante.</para>
///
/// <para><b>L'eccesso basso.</b> RSI di Wilder a <see cref="RsiPeriod"/> barre del timeframe della
/// strategia: sotto <see cref="RsiLow"/> e' ipervenduto, sopra <see cref="RsiHigh"/> ipercomprato. L'RSI
/// e' una funzione della finestra, non uno stato: si calcola sempre su <c>RsiPeriod × 10</c> variazioni
/// prima della barra di segnale, con la media semplice delle prime <see cref="RsiPeriod"/> come seme,
/// cosi' backtest e sessione live, che ricevono quantita' di storia diverse, danno lo stesso numero per
/// la stessa barra. Senza perdite vale 100, senza movimento 50.</para>
///
/// <para><b>L'ingresso</b> e' a mercato sulla barra dopo. Una posizione alla volta: in posizione non
/// nasce nulla. Senza la serie alta, o con una serie alta che non ha <see cref="TrendBars"/> barre
/// chiuse, il motore non valuta e lo dice nel motivo del segnale neutro.</para>
///
/// <para><b>Dubbio aperto del catalogo, verificato il 25/09/2026 sul codice e NON risolto.</b> Il
/// catalogo chiede di verificare che sessione live e cBot servano i timeframe aggiuntivi come il
/// backtest. Oggi no:</para>
/// <list type="bullet">
///   <item>il <b>backtest</b> (<c>PiootooBacktestingService.GetAdditionalTimeframeData</c>) li serve da un
///   cursore sul datafeed <c>@SYM_{minuti}.json</c>, ma per un timeframe alto da 1440 in su consegna
///   <b>8 barre</b> soltanto (le altre ricevono <c>RequiredCandles × 1,2</c>): con il default
///   <see cref="TrendBars"/> = 50 sulla giornaliera il motore non valuta mai;</item>
///   <item>la <b>sessione live</b> (<c>TradingSessionService</c>) costruisce la
///   <c>StrategyEvaluationRequest</c> senza <c>AdditionalOhlcv</c>: la strategia riceve il dizionario
///   vuoto e resta ferma, in silenzio salvo il motivo del segnale neutro;</item>
///   <item>la <b>sweep</b> (<c>SweepRunner</c>) nemmeno, e il <b>descriptor dei cBot</b> non ha un
///   campo per sottoscrivere una seconda serie.</item>
/// </list>
/// <para>Finche' questi tre punti non ci sono, una classe MTF si ricerca solo in backtest e con un
/// timeframe alto sotto il giornaliero, o con <see cref="TrendBars"/> sotto 8 sulla giornaliera, e non
/// entra in nessun piano.</para>
/// </summary>
public abstract class TimeframeDisagreementEngine : EasyEngineBase, IMultiTimeframeTradingStrategy
{
    /// <summary>
    /// Variazioni di chiusura su cui si calcola l'RSI, in multipli del periodo: il peso residuo del seme
    /// e' <c>(1 − 1/n)^(9n)</c>, sotto il decimillesimo per ogni periodo.
    /// </summary>
    private const int RsiWarmupFactor = 10;

    /// <summary>Timeframe alto, in minuti: la serie da cui si legge il trend. Deve superare quello della strategia.</summary>
    protected int HigherTimeframeMinutes = 1440;

    /// <summary>Barre alte chiuse della media semplice che definisce il trend.</summary>
    protected int TrendBars = 50;

    /// <summary>Periodo dell'RSI di Wilder sul timeframe della strategia.</summary>
    protected int RsiPeriod = 2;

    /// <summary>RSI sotto cui il timeframe basso e' ipervenduto: long, se il trend alto e' su.</summary>
    protected decimal RsiLow = 10m;

    /// <summary>RSI sopra cui il timeframe basso e' ipercomprato: short, se il trend alto e' giu'.</summary>
    protected decimal RsiHigh = 90m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>La serie alta: la chiede al backtest come timeframe aggiuntivo dello stesso simbolo.</summary>
    public IReadOnlyCollection<int> AdditionalTimeframes => new[] { HigherTimeframeMinutes };

    /// <summary>Le variazioni dell'RSI piu' la chiusura da cui partono.</summary>
    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, Math.Max(1, RsiPeriod) * RsiWarmupFactor + 1);

    /// <summary>
    /// Valutazione senza serie alta: e' il percorso di chi non consegna timeframe aggiuntivi. Il motore
    /// non ha un trend da leggere e non emette nulla.
    /// </summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, null, currentDate);

    /// <inheritdoc />
    public TradeSignal GenerateSignal(
        OhlcvData[] data,
        IReadOnlyDictionary<int, OhlcvData[]> additionalData,
        DateTime currentDate) =>
        EvaluateCore(data, additionalData, currentDate);

    private TradeSignal EvaluateCore(
        OhlcvData[] data,
        IReadOnlyDictionary<int, OhlcvData[]>? additionalData,
        DateTime currentDate)
    {
        if (HigherTimeframeMinutes <= TimeframeMinutes || TrendBars < 1 || RsiPeriod < 1 ||
            RsiLow < 0m || RsiHigh > 100m || RsiLow > RsiHigh || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(HigherTimeframeMinutes),
                $"{Name}: configurazione MTF incoerente (timeframe alto {HigherTimeframeMinutes} su " +
                $"{TimeframeMinutes}, media {TrendBars}, periodo RSI {RsiPeriod}, soglie RSI {RsiLow}/{RsiHigh}, " +
                $"lato {Direction}); servono timeframe alto maggiore di quello della strategia, media e periodo " +
                "almeno 1, 0 <= soglia bassa <= soglia alta <= 100 e lato 0, 1 o 2.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (additionalData is null ||
            !additionalData.TryGetValue(HigherTimeframeMinutes, out var higher) ||
            higher is null || higher.Length == 0)
        {
            return Hold(bar.Close, barTime, $"Serie alta assente ({HigherTimeframeMinutes}m)");
        }

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var lastClosed = LastClosedHigherBar(higher, barTime.AddMinutes(TimeframeMinutes));
        if (lastClosed + 1 < TrendBars)
        {
            return Hold(bar.Close, barTime,
                $"Serie alta troppo corta ({lastClosed + 1} barre chiuse da {HigherTimeframeMinutes}m, ne servono {TrendBars})");
        }

        var trendClose = higher[lastClosed].Close;
        var average = AverageClose(higher, lastClosed, TrendBars);
        var rsi = WilderRsi(data, RsiPeriod, RsiPeriod * RsiWarmupFactor);

        if (Direction != 2 && trendClose > average && rsi < RsiLow)
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE MTF"))
                   ?? Hold(bar.Close, barTime);
        }

        if (Direction != 1 && trendClose < average && rsi > RsiHigh)
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE MTF"))
                   ?? Hold(bar.Close, barTime);
        }

        return Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Indice dell'ultima barra alta chiusa entro <paramref name="lowerCloseUtc"/>, -1 se nessuna. La serie
    /// e' in ordine cronologico: si parte dalla fine e ci si ferma alla prima chiusa, che di norma e' la
    /// penultima.
    /// </summary>
    private int LastClosedHigherBar(OhlcvData[] higher, DateTime lowerCloseUtc)
    {
        for (var index = higher.Length - 1; index >= 0; index--)
        {
            if (HigherBarCloseUtc(higher[index].DateTime) <= lowerCloseUtc)
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Chiusura di una barra alta: apertura piu' il timeframe e, per la giornaliera, non prima della fine
    /// della sua sessione sulla griglia. Il massimo dei due non anticipa mai: se la barra del feed non e'
    /// allineata all'ancoraggio (una giornaliera del broker sul suo orologio) vale l'apertura piu' 24 ore.
    /// </summary>
    private DateTime HigherBarCloseUtc(DateTime openUtc)
    {
        var close = openUtc.AddMinutes(HigherTimeframeMinutes);
        if (HigherTimeframeMinutes != 1440)
            return close;

        var sessionClose = Grid.SessionOpenUtc(Grid.SessionDayOf(openUtc).AddDays(1));
        return sessionClose > close ? sessionClose : close;
    }

    /// <summary>Media semplice di <paramref name="bars"/> chiusure che finiscono in <paramref name="last"/> compreso.</summary>
    private static decimal AverageClose(OhlcvData[] series, int last, int bars)
    {
        var sum = 0m;
        for (var index = last - bars + 1; index <= last; index++)
            sum += series[index].Close;
        return sum / bars;
    }

    /// <summary>
    /// RSI di Wilder sulle ultime <paramref name="changes"/> variazioni di chiusura: media semplice delle
    /// prime <paramref name="period"/> come seme, poi <c>media = (media × (n − 1) + valore) / n</c>. Senza
    /// perdite vale 100, senza movimento 50 (nessun eccesso in nessun verso).
    /// </summary>
    private static decimal WilderRsi(OhlcvData[] data, int period, int changes)
    {
        var first = data.Length - 1 - changes;
        var gain = 0m;
        var loss = 0m;

        for (var index = first + 1; index <= first + period; index++)
        {
            var change = data[index].Close - data[index - 1].Close;
            if (change > 0m) gain += change;
            else loss -= change;
        }

        gain /= period;
        loss /= period;

        for (var index = first + period + 1; index < data.Length; index++)
        {
            var change = data[index].Close - data[index - 1].Close;
            gain = (gain * (period - 1) + (change > 0m ? change : 0m)) / period;
            loss = (loss * (period - 1) + (change < 0m ? -change : 0m)) / period;
        }

        if (loss == 0m)
            return gain == 0m ? 50m : 100m;

        return 100m - 100m / (1m + gain / loss);
    }
}
