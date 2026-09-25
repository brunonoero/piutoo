using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore XMK: <b>tra mercati</b>. Opera sul proprio simbolo e decide guardando un altro mercato,
/// <see cref="ReferenceSymbol"/>, sullo stesso timeframe. E' la fonte di scorrelazione piu' vera del
/// catalogo PT6EXO: nessun altro motore sa che esiste un secondo mercato. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §XMK.
///
/// <para><b>Tre modi</b> (<see cref="Mode"/>):</para>
/// <list type="number">
///   <item><b>0, anticipo.</b> Il riferimento chiude oltre il proprio canale delle
///   <see cref="LookbackBars"/> barre precedenti: si entra sul simbolo operato nello stesso verso. E'
///   l'idea "NQ segue quando GC ha rotto".</item>
///   <item><b>1, divergenza.</b> Sulle ultime <see cref="LookbackBars"/> barre il riferimento si e'
///   mosso di almeno <see cref="MoveThresholdPct"/> per cento e il simbolo operato nel verso opposto:
///   si entra nel verso del riferimento, scommettendo che il ritardatario lo segua.</item>
///   <item><b>2, rapporto.</b> Lo z-score del logaritmo del rapporto fra i due prezzi sulle ultime
///   <see cref="LookbackBars"/> barre oltre <see cref="ZEntry"/>: rapporto alto → il simbolo operato e'
///   caro → short; basso → long. E' il rapporto oro/argento o petrolio/gas.</item>
/// </list>
///
/// <para><b>Le barre si accoppiano per istante di apertura, e solo quelle.</b> I due mercati hanno
/// pause, festivi e finestre diverse: una barra del riferimento che manca non si sostituisce con la
/// precedente. Se manca proprio la barra di segnale — il riferimento non ha stampato in quel bucket —
/// la strategia <b>non valuta</b>; nelle finestre di calcolo entrano solo le coppie che esistono tutte e
/// due.</para>
///
/// <para><b>Dove gira.</b> Le serie di riferimento arrivano oggi dalla sola sweep
/// (<c>SweepRunner</c> con le serie di riferimento). Backtest completo, sessione live e cBot non le
/// portano ancora: qui una serie dichiarata e assente e' un <b>errore</b>, non un "niente segnale",
/// perche' un motore che tace per mancanza di dati e' indistinguibile da uno che non trova occasioni.</para>
/// </summary>
public abstract class CrossMarketEngine : EasyEngineBase, IMultiSymbolTradingStrategy
{
    /// <summary>Il mercato che si guarda, nella forma delle strategie (<c>@GC</c>).</summary>
    protected string ReferenceSymbol = "@ES";

    /// <summary>0 anticipo, 1 divergenza, 2 rapporto.</summary>
    protected int Mode;

    /// <summary>Barre accoppiate su cui si misura canale, movimento o z-score.</summary>
    protected int LookbackBars = 20;

    /// <summary>Movimento minimo del riferimento, in percento, per la divergenza.</summary>
    protected decimal MoveThresholdPct = 1m;

    /// <summary>Soglia dello z-score del rapporto.</summary>
    protected decimal ZEntry = 2m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// Il doppio delle coppie che servono: i due mercati non stampano le stesse barre, e una finestra
    /// primaria appena sufficiente lascerebbe il motore senza coppie a ogni pausa del riferimento.
    /// </summary>
    public override int RequiredCandles => Math.Max(base.RequiredCandles, 2 * (LookbackBars + 1));

    /// <inheritdoc />
    public IReadOnlyCollection<string> ReferenceSymbols => [NormalizedReference];

    private string NormalizedReference => "@" + ReferenceSymbol.Trim().TrimStart('@').ToUpperInvariant();

    /// <summary>
    /// Senza serie di riferimento questo motore non ha niente su cui decidere: chi lo chiama per questa
    /// via e' un percorso che non le porta, e deve saperlo.
    /// </summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        throw new InvalidOperationException(
            $"{Name}: strategia tra mercati senza serie di riferimento ({NormalizedReference}). Oggi gira " +
            "solo nella sweep; backtest completo, sessione live e cBot non portano ancora gli altri simboli.");

    public TradeSignal GenerateSignal(
        OhlcvData[] data, IReadOnlyDictionary<string, OhlcvData[]> referenceData, DateTime currentDate)
    {
        if (Mode is < 0 or > 2 || LookbackBars < 2 || MoveThresholdPct <= 0m || ZEntry <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: configurazione non valida (Mode {Mode}, LookbackBars {LookbackBars}, " +
                $"MoveThresholdPct {MoveThresholdPct}, ZEntry {ZEntry}).");
        }

        if (!referenceData.TryGetValue(NormalizedReference, out var reference))
        {
            throw new InvalidOperationException(
                $"{Name}: la serie di riferimento {NormalizedReference} non e' arrivata " +
                $"(presenti: {(referenceData.Count == 0 ? "nessuna" : string.Join(", ", referenceData.Keys))}).");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Coppie dalla piu' recente alla piu' vecchia; la prima deve essere la barra di segnale.
        var pairs = LookbackBars + 1;
        var primary = new decimal[pairs];
        var referenceClose = new decimal[pairs];
        var referenceHigh = new decimal[pairs];
        var referenceLow = new decimal[pairs];
        if (!Align(data, reference, primary, referenceClose, referenceHigh, referenceLow))
            return Hold(bar.Close, barTime, "Barra di riferimento assente");

        var side = Mode switch
        {
            0 => AnticipationSide(referenceClose, referenceHigh, referenceLow),
            1 => DivergenceSide(primary, referenceClose),
            _ => RatioSide(primary, referenceClose)
        };

        if (side is null ||
            (side == SignalType.Buy && Direction == 2) ||
            (side == SignalType.Sell && Direction == 1))
        {
            return Hold(bar.Close, barTime);
        }

        return WithSessionExit(EntryMarketNextBar(side.Value, bar.Close, data, barTime,
                   side == SignalType.Buy ? "LE XMK" : "SE XMK"))
               ?? Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Accoppia le due serie per istante di apertura, a ritroso, finche' non ha <c>pairs</c> coppie.
    /// Falso se la barra di segnale non ha la sua gemella o se le coppie non bastano.
    /// </summary>
    private static bool Align(
        OhlcvData[] data, OhlcvData[] reference,
        decimal[] primary, decimal[] referenceClose, decimal[] referenceHigh, decimal[] referenceLow)
    {
        var i = data.Length - 1;
        var j = reference.Length - 1;
        var found = 0;

        while (i >= 0 && j >= 0 && found < primary.Length)
        {
            var primaryTime = data[i].DateTime;
            var referenceTime = reference[j].DateTime;

            if (primaryTime == referenceTime)
            {
                primary[found] = data[i].Close;
                referenceClose[found] = reference[j].Close;
                referenceHigh[found] = reference[j].High;
                referenceLow[found] = reference[j].Low;
                found++;
                i--;
                j--;
            }
            else if (primaryTime > referenceTime)
            {
                // La barra primaria non ha gemella: se e' quella di segnale, non si valuta.
                if (found == 0)
                    return false;
                i--;
            }
            else
            {
                j--;
            }
        }

        return found == primary.Length;
    }

    /// <summary>Il riferimento chiude oltre il canale delle coppie precedenti: stesso verso.</summary>
    private static SignalType? AnticipationSide(decimal[] close, decimal[] high, decimal[] low)
    {
        var channelHigh = high[1];
        var channelLow = low[1];
        for (var index = 2; index < close.Length; index++)
        {
            if (high[index] > channelHigh) channelHigh = high[index];
            if (low[index] < channelLow) channelLow = low[index];
        }

        if (close[0] > channelHigh) return SignalType.Buy;
        if (close[0] < channelLow) return SignalType.Sell;
        return null;
    }

    /// <summary>Il riferimento si e' mosso oltre la soglia, il simbolo operato nel verso opposto: si segue il riferimento.</summary>
    private SignalType? DivergenceSide(decimal[] primary, decimal[] referenceClose)
    {
        var last = primary.Length - 1;
        if (referenceClose[last] <= 0m || primary[last] <= 0m)
            return null;

        var referenceMove = (referenceClose[0] - referenceClose[last]) / referenceClose[last] * 100m;
        var primaryMove = primary[0] - primary[last];

        if (referenceMove >= MoveThresholdPct && primaryMove < 0m) return SignalType.Buy;
        if (referenceMove <= -MoveThresholdPct && primaryMove > 0m) return SignalType.Sell;
        return null;
    }

    /// <summary>Z-score del logaritmo del rapporto sulle coppie: alto → short, basso → long.</summary>
    private SignalType? RatioSide(decimal[] primary, decimal[] referenceClose)
    {
        var count = primary.Length;
        var sum = 0.0;
        var values = new double[count];
        for (var index = 0; index < count; index++)
        {
            if (primary[index] <= 0m || referenceClose[index] <= 0m)
                return null;
            values[index] = Math.Log((double)primary[index] / (double)referenceClose[index]);
            sum += values[index];
        }

        var mean = sum / count;
        var squares = 0.0;
        for (var index = 0; index < count; index++)
            squares += (values[index] - mean) * (values[index] - mean);

        var deviation = Math.Sqrt(squares / count);
        if (deviation <= 0.0)
            return null;

        var z = (values[0] - mean) / deviation;
        if (z >= (double)ZEntry) return SignalType.Sell;
        if (z <= -(double)ZEntry) return SignalType.Buy;
        return null;
    }
}
