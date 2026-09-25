using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore CDL: <b>candela di rifiuto su un livello</b>. Una barra tocca un livello e ne viene respinta —
/// una pin bar con l'ombra lunga dal lato del livello, o un engulfing di colore opposto — e chiude dalla
/// parte opposta: si entra nel verso del rifiuto. Il trigger e' la <b>forma</b> della barra, non la
/// distanza dal livello come nei LevelFader. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §CDL.
///
/// <para><b>I livelli</b> (<see cref="LevelKind"/>), dalla sessione della ricerca
/// (<see cref="EasyEngineBase.BuildSessionOhlc"/>):</para>
/// <list type="bullet">
///   <item>0: <b>massimo e minimo della sessione precedente</b>. Lo short si cerca sul massimo, il long
///   sul minimo;</item>
///   <item>1: <b>apertura della sessione corrente</b>, lo stesso livello per i due lati: una barra che
///   lo tocca dall'alto e chiude sotto e' uno short, una che lo tocca dal basso e chiude sopra un long.
///   Sulla prima barra della sessione non vale: il livello nasce con quella barra, e toccarlo e'
///   inevitabile.</item>
/// </list>
///
/// <para><b>Le candele</b> (<see cref="CandleKind"/>), descritte per lo short (il long e' lo specchio):</para>
/// <list type="bullet">
///   <item>0, <b>pin bar</b>: l'ombra superiore (massimo − il piu' alto fra apertura e chiusura) e' almeno
///   <see cref="WickRatio"/> volte il corpo. Un corpo nullo (doji) vale un tick, altrimenti ogni doji
///   sarebbe una pin bar a rapporto infinito;</item>
///   <item>1, <b>engulfing ribassista</b>: barra rossa, precedente verde, e il corpo della barra contiene
///   quello della precedente ed e' piu' grande.</item>
/// </list>
/// <para>In entrambi i casi la barra deve <b>toccare</b> il livello — massimo almeno al livello meno
/// <see cref="ToleranceTicks"/> tick — e <b>chiudere sotto</b>. Una barra che soddisfa i due lati insieme
/// (una doji che tocca massimo e minimo di ieri) non dice in che verso e' stata respinta: nessun ingresso.</para>
///
/// <para><b>L'ingresso</b> e' a mercato sulla barra dopo: il rifiuto si conosce alla chiusura. Lo stop e'
/// quello comune della base (denaro o ATR) oppure, con <see cref="StopAtExtreme"/>, oltre l'estremo della
/// candela di <see cref="ExtremeBufferTicks"/> — come nel <c>FailedBreakoutEngine</c>: se il prezzo torna
/// oltre l'estremo il rifiuto non c'e' stato. La distanza si misura dalla chiusura della barra di segnale.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla.</para>
/// </summary>
public abstract class RejectionCandleEngine : EasyEngineBase
{
    /// <summary>0 = massimo e minimo della sessione precedente; 1 = apertura della sessione corrente.</summary>
    protected int LevelKind;

    /// <summary>0 = pin bar; 1 = engulfing.</summary>
    protected int CandleKind;

    /// <summary>Pin bar: ombra dal lato del livello, in multipli del corpo.</summary>
    protected decimal WickRatio = 2m;

    /// <summary>Tolleranza del tocco, in tick: la barra "tocca" se arriva a tanti tick dal livello.</summary>
    protected int ToleranceTicks;

    /// <summary>1 = stop oltre l'estremo della candela; 0 = lo stop comune della base (denaro o ATR).</summary>
    protected int StopAtExtreme;

    /// <summary>Margine oltre l'estremo della candela, in tick, quando <see cref="StopAtExtreme"/> e' acceso.</summary>
    protected int ExtremeBufferTicks;

    /// <summary>Dimensione del tick dello strumento, per tolleranza, margine e corpo minimo.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (LevelKind is not (0 or 1) || CandleKind is not (0 or 1) || WickRatio <= 0m || ToleranceTicks < 0 ||
            StopAtExtreme is not (0 or 1) || ExtremeBufferTicks < 0 || TickSize <= 0m || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(LevelKind),
                $"{Name}: configurazione CDL non valida (LevelKind {LevelKind}, CandleKind {CandleKind}, " +
                $"WickRatio {WickRatio}, ToleranceTicks {ToleranceTicks}, StopAtExtreme {StopAtExtreme}, " +
                $"ExtremeBufferTicks {ExtremeBufferTicks}, TickSize {TickSize}, Direction {Direction}).");
        }

        if (data is null || data.Length < Math.Max(RequiredCandles, 2))
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // ohlc[0] apertura della sessione corrente, ohlc[5]/ohlc[6] massimo e minimo della precedente.
        var startsSession = BuildSessionOhlc(data, barTime, out var ohlc);
        if (LevelKind == 1 && startsSession)
            return Hold(bar.Close, barTime);

        var upper = LevelKind == 0 ? ohlc[5] : ohlc[0];
        var lower = LevelKind == 0 ? ohlc[6] : ohlc[0];
        if (upper <= 0m || lower <= 0m)
            return Hold(bar.Close, barTime);

        var tolerance = ToleranceTicks * TickSize;
        var previous = data[^2];

        var shortRejected = Direction != 1 &&
                            bar.High >= upper - tolerance &&
                            bar.Close < upper &&
                            (CandleKind == 0 ? IsUpperPin(bar) : IsBearishEngulfing(bar, previous));

        var longRejected = Direction != 2 &&
                           bar.Low <= lower + tolerance &&
                           bar.Close > lower &&
                           (CandleKind == 0 ? IsLowerPin(bar) : IsBullishEngulfing(bar, previous));

        // Respinta dai due lati insieme: non si sa in che verso, quindi niente.
        if (shortRejected == longRejected)
            return Hold(bar.Close, barTime);

        var side = shortRejected ? SignalType.Sell : SignalType.Buy;
        return Entry(side, data, barTime) ?? Hold(bar.Close, barTime);
    }

    /// <summary>Il corpo, con un tick come minimo: una doji non ha rapporto infinito.</summary>
    private decimal EffectiveBody(OhlcvData bar) => Math.Max(Math.Abs(bar.Close - bar.Open), TickSize);

    private bool IsUpperPin(OhlcvData bar) =>
        bar.High - Math.Max(bar.Open, bar.Close) >= WickRatio * EffectiveBody(bar);

    private bool IsLowerPin(OhlcvData bar) =>
        Math.Min(bar.Open, bar.Close) - bar.Low >= WickRatio * EffectiveBody(bar);

    /// <summary>Rossa dopo una verde, con il corpo che contiene quello della precedente ed e' piu' grande.</summary>
    private static bool IsBearishEngulfing(OhlcvData bar, OhlcvData previous) =>
        bar.Close < bar.Open &&
        previous.Close > previous.Open &&
        bar.Open >= previous.Close &&
        bar.Close <= previous.Open &&
        bar.Open - bar.Close > previous.Close - previous.Open;

    /// <summary>Verde dopo una rossa, con il corpo che contiene quello della precedente ed e' piu' grande.</summary>
    private static bool IsBullishEngulfing(OhlcvData bar, OhlcvData previous) =>
        bar.Close > bar.Open &&
        previous.Close < previous.Open &&
        bar.Open <= previous.Close &&
        bar.Close >= previous.Open &&
        bar.Close - bar.Open > previous.Open - previous.Close;

    private TradeSignal? Entry(SignalType side, OhlcvData[] data, DateTime barTime)
    {
        var bar = data[^1];
        var signal = EntryMarketNextBar(side, bar.Close, data, barTime, side == SignalType.Buy ? "LE CDL" : "SE CDL");

        if (StopAtExtreme != 0)
        {
            var buffer = ExtremeBufferTicks * TickSize;
            var distance = side == SignalType.Sell
                ? bar.High + buffer - bar.Close
                : bar.Close - (bar.Low - buffer);

            // Una distanza nulla non e' uno stop: l'estremo coincide con la chiusura solo con il
            // margine a zero su una barra che chiude sul proprio estremo, e li' vale lo stop comune.
            if (distance > 0m)
                signal.StopLossMoneyPerFutureContract =
                    Math.Round(distance * InstrumentRegistry.PointValue(Symbol), 2);
        }

        return WithSessionExit(signal);
    }
}
