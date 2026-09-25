using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore REG: <b>il regime di volatilita' sceglie la logica</b>. E' un meta-motore: quando il mercato e'
/// calmo segue la rottura del canale, quando e' agitato va contro l'eccesso sulle bande, in mezzo non fa
/// niente. L'ipotesi e' che le due famiglie del catalogo — breakout e ritorno alla media — non siano
/// buone o cattive in assoluto ma in un regime, e che il regime si legga dalla volatilita'. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §REG.
///
/// <para><b>Il regime.</b> L'ATR di barra — media semplice del true range delle ultime
/// <see cref="AtrBars"/> barre, sul timeframe della strategia — e il suo percentile fra gli ATR delle
/// ultime <see cref="RegimeBars"/> barre, quello corrente compreso. Il percentile e' il rango medio: le
/// barre con ATR minore contano uno, quelle con ATR uguale (lui compreso) mezzo. Con il rango medio un
/// tratto a volatilita' costante sta al 50° percentile, cioe' in mezzo: con il conteggio dei soli minori
/// starebbe allo zero e il motore lo leggerebbe come calmo.</para>
/// <list type="bullet">
///   <item>Percentile <b>sotto</b> <see cref="LowPercentile"/>: regime calmo, <b>breakout</b>. Buy stop
///   al massimo delle ultime <see cref="ChannelBars"/> barre e sell stop al minimo, barra corrente
///   compresa, in OCO.</item>
///   <item>Percentile <b>sopra</b> <see cref="HighPercentile"/>: regime agitato, <b>ritorno alla
///   media</b>. Chiusura sopra la banda superiore di Bollinger (<see cref="BandBars"/> chiusure,
///   <see cref="BandDevs"/> deviazioni standard di popolazione) → short a mercato sulla barra dopo;
///   sotto la banda inferiore → long.</item>
///   <item>In mezzo, niente. Con <see cref="LowPercentile"/> a 0 il ramo calmo e' spento, con
///   <see cref="HighPercentile"/> a 100 quello agitato.</item>
/// </list>
///
/// <para><b>Il costo.</b> La serie degli ATR si calcola in un giro solo con la somma mobile dei true
/// range: <c>O(RegimeBars + AtrBars)</c> per barra, non <c>O(RegimeBars × AtrBars)</c>. Il confronto
/// avviene fra <b>somme</b>, non fra medie: stesso ordine, e in decimale la somma mobile e' esatta, quindi
/// due ATR uguali risultano uguali e il rango medio non dipende da un arrotondamento.</para>
///
/// <para><b>La storia.</b> Una finestra di regime lunga — un anno di barre — e' il
/// <see cref="RequiredCandles"/>: in sessione <c>ExternalBroker</c> il riscaldamento deve portarla tutta,
/// altrimenti la strategia non diventa mai valutabile (<c>everEvaluable</c> resta falso) e non opera in
/// silenzio.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla. Il canale del breakout include la barra
/// corrente, quindi gli stop nascono oltre la chiusura; se un gap li scavalca, vale il default di
/// <c>CrossedLevelPolicy</c> come per ogni stop del catalogo.</para>
/// </summary>
public abstract class VolatilityRegimeEngine : EasyEngineBase
{
    /// <summary>Barre del true range medio (ATR di barra).</summary>
    protected int AtrBars = 14;

    /// <summary>Barre fra i cui ATR si misura il percentile di quello corrente, lui compreso.</summary>
    protected int RegimeBars = 500;

    /// <summary>Percentile sotto cui il regime e' calmo e il motore segue la rottura. 0 = ramo spento.</summary>
    protected decimal LowPercentile = 20m;

    /// <summary>Percentile sopra cui il regime e' agitato e il motore va contro la banda. 100 = ramo spento.</summary>
    protected decimal HighPercentile = 80m;

    /// <summary>Barre del canale del breakout, barra corrente compresa.</summary>
    protected int ChannelBars = 20;

    /// <summary>Chiusure della media e della deviazione delle bande di Bollinger.</summary>
    protected int BandBars = 20;

    /// <summary>Ampiezza delle bande in deviazioni standard di popolazione.</summary>
    protected decimal BandDevs = 2m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// Gli ATR della finestra di regime (il primo ha bisogno di <see cref="AtrBars"/> true range, e il primo
    /// true range della chiusura prima), il canale e le bande.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(RegimeBars + AtrBars + 1, Math.Max(ChannelBars, BandBars)));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (AtrBars < 1 || RegimeBars < 2 || ChannelBars < 1 || BandBars < 2 || BandDevs < 0m ||
            LowPercentile < 0m || HighPercentile > 100m || LowPercentile > HighPercentile)
        {
            throw new ArgumentOutOfRangeException(nameof(LowPercentile),
                $"{Name}: configurazione REG incoerente (ATR {AtrBars}, regime {RegimeBars}, percentili " +
                $"{LowPercentile}/{HighPercentile}, canale {ChannelBars}, bande {BandBars} x {BandDevs}); servono " +
                "0 <= basso <= alto <= 100, ATR e canale di almeno una barra, regime e bande di almeno due, " +
                "deviazioni non negative.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var percentile = AtrPercentile(data);

        if (percentile < LowPercentile)
            return Breakout(data, barTime);

        if (percentile > HighPercentile)
            return Fade(data, barTime);

        return Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Rango medio, in percentuale, dell'ATR corrente fra gli ATR delle ultime <see cref="RegimeBars"/>
    /// barre. Si confrontano le somme dei true range, che hanno tutte lo stesso numero di termini.
    /// </summary>
    private decimal AtrPercentile(OhlcvData[] data)
    {
        var last = data.Length - 1;
        var firstAtr = last - RegimeBars + 1;

        var current = 0m;
        for (var index = last - AtrBars + 1; index <= last; index++)
            current += TrueRange(data, index);

        var sum = 0m;
        for (var index = firstAtr - AtrBars + 1; index <= firstAtr; index++)
            sum += TrueRange(data, index);

        var below = 0;
        var equal = 0;
        for (var index = firstAtr; ; index++)
        {
            if (sum < current) below++;
            else if (sum == current) equal++;

            if (index == last)
                break;

            // La finestra scorre di una barra: entra il true range nuovo, esce il piu' vecchio.
            sum += TrueRange(data, index + 1) - TrueRange(data, index + 1 - AtrBars);
        }

        return (below * 2 + equal) * 50m / RegimeBars;
    }

    /// <summary>True range della barra <paramref name="index"/>: serve la chiusura della barra prima.</summary>
    private static decimal TrueRange(OhlcvData[] data, int index)
    {
        var bar = data[index];
        var previousClose = data[index - 1].Close;
        return Math.Max(bar.High - bar.Low,
            Math.Max(Math.Abs(bar.High - previousClose), Math.Abs(bar.Low - previousClose)));
    }

    /// <summary>Regime calmo: stop sugli estremi del canale, in OCO.</summary>
    private TradeSignal Breakout(OhlcvData[] data, DateTime barTime)
    {
        var high = data[^1].High;
        var low = data[^1].Low;
        for (var index = data.Length - ChannelBars; index < data.Length - 1; index++)
        {
            if (data[index].High > high) high = data[index].High;
            if (data[index].Low < low) low = data[index].Low;
        }

        var entries = new List<TradeSignal>(2);
        if (Direction != 2)
            AddEntry(entries, WithSessionExit(EntryStopNextBar(SignalType.Buy, high, data, barTime, "LE REG BO")));
        if (Direction != 1)
            AddEntry(entries, WithSessionExit(EntryStopNextBar(SignalType.Sell, low, data, barTime, "SE REG BO")));

        return Combine(entries, Hold(data[^1].Close, barTime));
    }

    /// <summary>Regime agitato: contro la chiusura fuori dalle bande, a mercato sulla barra dopo.</summary>
    private TradeSignal Fade(OhlcvData[] data, DateTime barTime)
    {
        var close = data[^1].Close;

        var sum = 0m;
        for (var index = data.Length - BandBars; index < data.Length; index++)
            sum += data[index].Close;
        var mean = sum / BandBars;

        var squares = 0m;
        for (var index = data.Length - BandBars; index < data.Length; index++)
        {
            var difference = data[index].Close - mean;
            squares += difference * difference;
        }

        // Deviazione di popolazione, come le bande dei motori RBB.
        var width = BandDevs * (decimal)Math.Sqrt((double)(squares / BandBars));

        if (Direction != 1 && close > mean + width)
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Sell, close, data, barTime, "SE REG MR"))
                   ?? Hold(close, barTime);
        }

        if (Direction != 2 && close < mean - width)
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Buy, close, data, barTime, "LE REG MR"))
                   ?? Hold(close, barTime);
        }

        return Hold(close, barTime);
    }
}
