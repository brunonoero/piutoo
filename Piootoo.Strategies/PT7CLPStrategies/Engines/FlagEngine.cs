using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT7CLPStrategies.Engines;

/// <summary>
/// Motore FLG: la <b>bandiera</b> (flag) dell'analisi tecnica classica, prima figura della serie PT7CLP
/// (classic pattern). Un movimento forte — il palo — seguito da un breve canale a rette parallele
/// inclinato <b>contro</b> il palo; si entra quando il prezzo rompe il canale nel verso del palo. Vedi
/// <c>docs/domini/catalogo-pattern-classici.md</c> §FLG.
///
/// <para><b>La figura</b> (rialzista; la ribassista e' lo specchio). Per una lunghezza di bandiera F fra
/// <see cref="FlagMinBars"/> e <see cref="FlagMaxBars"/>, la barra p = ultima − F e' la cima del palo e le
/// F barre dopo, quella appena chiusa compresa, sono la bandiera:</para>
/// <list type="number">
///   <item><b>palo</b>: il massimo di p e' il piu' alto delle <see cref="PoleBars"/> barre che finiscono in
///   p, e dista dal loro minimo almeno <see cref="PoleAtr"/> volte l'ATR delle barre;</item>
///   <item><b>la bandiera non supera la cima</b>: nessun massimo della bandiera oltre quello di p;</item>
///   <item><b>poco profonda</b>: il minimo della bandiera non scende sotto la cima meno
///   <see cref="MaxRetrace"/> volte l'altezza del palo;</item>
///   <item><b>controtrend</b>: le rette di regressione dei massimi e dei minimi della bandiera hanno
///   pendenza media nulla o negativa — il rettangolo orizzontale e' ancora una bandiera;</item>
///   <item><b>parallele</b>: le due pendenze differiscono al massimo di <see cref="ParallelTolerance"/>
///   volte l'ATR per barra. Una tolleranza larga ammette le rette convergenti, cioe' il pennant;</item>
///   <item><b>non ancora rotta</b>: la barra appena chiusa chiude sotto la retta dei massimi.</item>
/// </list>
/// <para>Le lunghezze si provano dalla piu' corta: la prima valida e' la bandiera in corso.</para>
///
/// <para><b>L'ATR</b> e' la media semplice del true range delle <see cref="AtrBars"/> barre che finiscono
/// <b>prima</b> del palo, sul timeframe della strategia: il palo non gonfia la misura con cui e' giudicato,
/// e non e' l'ATR delle sessioni della base, che su una cella oraria varrebbe una giornata intera.</para>
///
/// <para><b>L'ordine.</b> Buy stop sulla retta dei massimi proiettata sulla barra dopo, piu'
/// <see cref="OffsetTicks"/> tick, arrotondato al tick verso l'esterno. Il motore non tiene stato: a ogni
/// barra ricerca la figura e riemette l'ordine "next bar" finche' la bandiera resta valida, al massimo
/// <see cref="FlagMaxBars"/> barre dopo la cima; e' la validita' dell'ordine scritta senza memoria. Una
/// retta gia' scavalcata segue <c>CrossedLevelPolicy</c> del segnale (default <c>Reject</c>), come gli
/// altri motori a stop.</para>
///
/// <para><b>Stop e target.</b> Quelli comuni della base, oppure: con <see cref="StopAtFlag"/> lo stop sta
/// oltre l'estremo opposto della bandiera di <see cref="ExtremeBufferTicks"/> tick, misurato dal livello
/// d'ingresso; con <see cref="TargetPole"/> il target e' l'altezza del palo per quel multiplo, anch'essa
/// dal livello d'ingresso — il "measured move" della letteratura.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla.</para>
/// </summary>
public abstract class FlagEngine : EasyEngineBase
{
    /// <summary>Barre entro cui si misura il palo, cima compresa.</summary>
    protected int PoleBars = 5;

    /// <summary>Altezza minima del palo, in multipli dell'ATR delle barre.</summary>
    protected decimal PoleAtr = 3m;

    /// <summary>Barre dell'ATR, prima del palo.</summary>
    protected int AtrBars = 20;

    /// <summary>Barre minime della bandiera dopo la cima.</summary>
    protected int FlagMinBars = 3;

    /// <summary>Barre massime della bandiera dopo la cima: oltre, la figura e l'ordine muoiono.</summary>
    protected int FlagMaxBars = 10;

    /// <summary>Ritracciamento massimo della bandiera, in frazione dell'altezza del palo.</summary>
    protected decimal MaxRetrace = 0.5m;

    /// <summary>Differenza massima fra le pendenze delle due rette, in multipli dell'ATR per barra.</summary>
    protected decimal ParallelTolerance = 0.1m;

    /// <summary>Margine dell'ordine stop oltre la retta, in tick.</summary>
    protected int OffsetTicks;

    /// <summary>1 = stop oltre l'estremo opposto della bandiera; 0 = lo stop comune della base.</summary>
    protected int StopAtFlag;

    /// <summary>Margine oltre l'estremo della bandiera, in tick, quando <see cref="StopAtFlag"/> e' acceso.</summary>
    protected int ExtremeBufferTicks;

    /// <summary>Target in multipli dell'altezza del palo dal livello d'ingresso; 0 = il target comune della base.</summary>
    protected decimal TargetPole;

    /// <summary>Dimensione del tick dello strumento.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>La bandiera piu' lunga, il palo, le barre dell'ATR e la chiusura prima della prima di esse.</summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(1, FlagMaxBars) + Math.Max(1, PoleBars) + Math.Max(1, AtrBars) + 1);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (PoleBars < 1 || PoleAtr <= 0m || AtrBars < 1 || FlagMinBars < 2 || FlagMaxBars < FlagMinBars ||
            MaxRetrace <= 0m || MaxRetrace > 1m || ParallelTolerance < 0m || OffsetTicks < 0 ||
            ExtremeBufferTicks < 0 || TargetPole < 0m || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(PoleBars),
                $"{Name}: configurazione FLG incoerente (palo {PoleBars} barre e {PoleAtr} ATR, ATR {AtrBars}, " +
                $"bandiera {FlagMinBars}-{FlagMaxBars}, ritracciamento {MaxRetrace}, parallelismo {ParallelTolerance}, " +
                $"margini {OffsetTicks}/{ExtremeBufferTicks}, target {TargetPole}, lato {Direction}); servono palo " +
                "e ATR di almeno una barra, altezza positiva, bandiera di almeno 2 barre con massimo non sotto il " +
                "minimo, ritracciamento in (0, 1], tolleranze e margini non negativi, lato 0, 1 o 2.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);

        if (Direction != 2 && Find(data, bullish: true) is { } bull)
            AddEntry(entries, Entry(SignalType.Buy, bull, data, barTime, "LE FLG"));

        if (Direction != 1 && Find(data, bullish: false) is { } bear)
            AddEntry(entries, Entry(SignalType.Sell, bear, data, barTime, "SE FLG"));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>La bandiera trovata: livello d'ingresso, estremo opposto e altezza del palo.</summary>
    private readonly record struct Flag(decimal Level, decimal Extreme, decimal PoleHeight);

    /// <summary>
    /// La bandiera in corso nel verso chiesto, dalla lunghezza piu' corta, o <c>null</c>. Il ribasso e' lo
    /// specchio del rialzo: <c>s</c> vale 1 o −1 e ogni prezzo si confronta come <c>s × prezzo</c>, cosi'
    /// la figura si scrive una volta sola.
    /// </summary>
    private Flag? Find(OhlcvData[] data, bool bullish)
    {
        var s = bullish ? 1m : -1m;
        var last = data.Length - 1;

        for (var length = FlagMinBars; length <= FlagMaxBars; length++)
        {
            var top = last - length;
            var poleStart = top - PoleBars + 1;

            // Nel verso del palo la cima e' l'estremo "alto" (il massimo per il rialzo, il minimo per il
            // ribasso); l'estremo "basso" e' l'altro.
            var peak = s * Far(data[top], s);

            // Cima = estremo del palo, e la bandiera non la supera.
            var valid = true;
            var poleBase = peak;
            for (var index = poleStart; index < top && valid; index++)
            {
                if (s * Far(data[index], s) > peak) valid = false;
                poleBase = Math.Min(poleBase, s * Near(data[index], s));
            }

            poleBase = Math.Min(poleBase, s * Near(data[top], s));
            if (!valid) continue;

            var flagDeepest = decimal.MaxValue;
            for (var index = top + 1; index <= last && valid; index++)
            {
                if (s * Far(data[index], s) > peak) valid = false;
                flagDeepest = Math.Min(flagDeepest, s * Near(data[index], s));
            }

            if (!valid) continue;

            var atr = BarAtr(data, poleStart);
            var height = peak - poleBase;
            if (atr <= 0m || height < PoleAtr * atr) continue;
            if (flagDeepest < peak - MaxRetrace * height) continue;

            // Rette di regressione sulla bandiera, x = 0 sulla prima barra dopo la cima.
            var (farSlope, farMean) = Regression(data, top + 1, length, far: true, s);
            var (nearSlope, _) = Regression(data, top + 1, length, far: false, s);

            if (farSlope + nearSlope > 0m) continue;
            if (Math.Abs(farSlope - nearSlope) > ParallelTolerance * atr) continue;

            var center = (length - 1) / 2m;
            var farAtLast = farMean + farSlope * (length - 1 - center);
            if (s * data[last].Close >= farAtLast) continue;

            var farNext = farMean + farSlope * (length - center);
            var level = s * RoundOutward(farNext + OffsetTicks * TickSize);
            return new Flag(level, s * flagDeepest, height);
        }

        return null;
    }

    /// <summary>Estremo della barra nel verso del palo: il massimo per il rialzo, il minimo per il ribasso.</summary>
    private static decimal Far(OhlcvData bar, decimal s) => s > 0m ? bar.High : bar.Low;

    /// <summary>L'estremo opposto a <see cref="Far"/>.</summary>
    private static decimal Near(OhlcvData bar, decimal s) => s > 0m ? bar.Low : bar.High;

    /// <summary>Pendenza e media della retta di regressione di <c>s × estremo</c> su <paramref name="count"/> barre.</summary>
    private static (decimal Slope, decimal Mean) Regression(OhlcvData[] data, int start, int count, bool far, decimal s)
    {
        var center = (count - 1) / 2m;
        decimal sum = 0m;
        for (var x = 0; x < count; x++)
            sum += s * (far ? Far(data[start + x], s) : Near(data[start + x], s));

        var mean = sum / count;
        decimal covariance = 0m, variance = 0m;
        for (var x = 0; x < count; x++)
        {
            var dx = x - center;
            var y = s * (far ? Far(data[start + x], s) : Near(data[start + x], s));
            covariance += dx * (y - mean);
            variance += dx * dx;
        }

        return (covariance / variance, mean);
    }

    /// <summary>Media del true range delle <see cref="AtrBars"/> barre che finiscono prima di <paramref name="poleStart"/>.</summary>
    private decimal BarAtr(OhlcvData[] data, int poleStart)
    {
        decimal sum = 0m;
        for (var index = poleStart - AtrBars; index < poleStart; index++)
        {
            var previousClose = data[index - 1].Close;
            sum += Math.Max(data[index].High - data[index].Low,
                Math.Max(Math.Abs(data[index].High - previousClose), Math.Abs(data[index].Low - previousClose)));
        }

        return sum / AtrBars;
    }

    /// <summary>
    /// Arrotonda al tick verso l'esterno della bandiera. Il valore arriva gia' moltiplicato per il verso,
    /// quindi "verso l'alto" vale per entrambi i lati.
    /// </summary>
    private decimal RoundOutward(decimal signedLevel) =>
        TickSize > 0m ? Math.Ceiling(signedLevel / TickSize) * TickSize : signedLevel;

    private TradeSignal? Entry(SignalType side, Flag flag, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryStopNextBar(side, flag.Level, data, barTime, reason);
        var pointValue = InstrumentRegistry.PointValue(Symbol);

        if (StopAtFlag != 0)
        {
            var buffer = ExtremeBufferTicks * TickSize;
            var distance = side == SignalType.Buy
                ? flag.Level - (flag.Extreme - buffer)
                : flag.Extreme + buffer - flag.Level;
            if (distance > 0m)
                signal.StopLossMoneyPerFutureContract = Math.Round(distance * pointValue, 2);
        }

        if (TargetPole > 0m)
            signal.TakeProfitMoneyPerFutureContract = Math.Round(TargetPole * flag.PoleHeight * pointValue, 2);

        return WithSessionExit(signal);
    }
}
