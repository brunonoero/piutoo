using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PTARMStrategies.Engines;

/// <summary>
/// I rapporti di Fibonacci di una figura armonica, per la figura <b>rialzista</b> (X minimo, A massimo, B
/// minimo, C massimo, D minimo; la ribassista e' lo specchio). Gli intervalli sono gli estremi da manuale
/// (Carney); un rapporto puntuale ha i due estremi uguali, e la tolleranza della strategia li allarga.
/// </summary>
/// <param name="BMin">B ritraccia XA: minimo di <c>(A − B) / (A − X)</c>.</param>
/// <param name="BMax">B ritraccia XA: massimo.</param>
/// <param name="CMin">C ritraccia AB: minimo di <c>(C − B) / (A − B)</c>.</param>
/// <param name="CMax">C ritraccia AB: massimo.</param>
/// <param name="CdMin">Estensione di CD su BC: minimo di <c>(C − D) / (C − B)</c>.</param>
/// <param name="CdMax">Estensione di CD su BC: massimo.</param>
/// <param name="D">Il punto D come ritracciamento di XA: <c>D = A − D × (A − X)</c>. Oltre 1, D sta oltre X.</param>
public readonly record struct HarmonicRatios(
    decimal BMin, decimal BMax, decimal CMin, decimal CMax, decimal CdMin, decimal CdMax, decimal D);

/// <summary>
/// Base dei motori armonici della serie PTARM: <b>X-A-B-C-D</b>, quattro gambe alternate i cui rapporti
/// cadono negli intervalli di Fibonacci della figura, e un ordine <b>limit su D</b>, contro l'ultima gamba.
/// Le figure (Gartley, Bat, Butterfly, Crab) differiscono solo per <see cref="Ratios"/>, scritti nella
/// classe e non come leve: sono fissati a priori dalla letteratura, e la griglia non li ottimizza. Vedi
/// <c>docs/domini/catalogo-armonici.md</c>.
///
/// <para><b>Gli swing</b> sono pivot a <see cref="PivotBars"/> barre: il massimo di una barra supera
/// strettamente quelli delle k barre prima e non e' superato dalle k barre dopo (il minimo, specchio). Un
/// pivot si conosce solo k barre dopo, e il motore usa solo pivot <b>confermati</b> entro la barra chiusa.
/// Una barra che e' insieme pivot alto e basso e' ambigua e non conta. Due pivot consecutivi dello stesso
/// tipo sono lo stesso swing: vale il piu' estremo.</para>
///
/// <para><b>Stessa decisione con qualunque finestra.</b> La scansione parte dalla barra piu' recente e va
/// indietro finche' trova C, B, A, X e il pivot opposto <b>prima</b> di X, che chiude lo swing X: senza, X
/// potrebbe essere l'inizio troncato di uno swing piu' lungo, e backtest e live, che ricevono finestre di
/// lunghezza diversa, vedrebbero una X diversa. Se in <see cref="LookbackBars"/> barre non c'e', nessun
/// segnale.</para>
///
/// <para><b>La figura</b> (rialzista): C e' uno swing alto e dopo C nessuna barra lo supera; B ritraccia
/// XA, C ritraccia AB, D = A − <c>D</c> × XA e CD/BC stanno negli intervalli di <see cref="Ratios"/>,
/// allargati di <see cref="Tolerance"/> (estremo basso × (1 − t), alto × (1 + t)) e moltiplicati per
/// <see cref="RatioScale"/>. Dopo C nessuna barra ha ancora toccato D: una D toccata e' una figura
/// consumata — riempita, o persa perche' il prezzo ci e' arrivato prima che C fosse confermato — e non
/// si insegue a posteriori. Da C sono passate al massimo <see cref="MaxDBars"/> barre.</para>
///
/// <para><b>L'ordine</b>: buy limit su D, arrotondato al tick verso l'interno della figura, valido una
/// barra e riemesso finche' la figura regge. Il livello gia' superato si esegue <b>a mercato</b>
/// (<see cref="CrossedLevelPolicy.Market"/>): deciso il 29/09/2026 per la serie.</para>
///
/// <para><b>Uscite</b>: quelle comuni, oppure con <see cref="StopStructure"/> lo stop oltre il piu' esterno
/// fra X e D di <see cref="StopBufferAtr"/> ATR delle barre (su Butterfly e Crab D sta oltre X, e senza
/// margine lo stop cadrebbe sull'ingresso: allora vale quello comune), e con <see cref="TargetAd"/> il
/// target a quella frazione della gamba AD dal livello d'ingresso (0,382 e 0,618 da manuale).</para>
///
/// <para><b>Il controllo</b>: <see cref="RatioScale"/> diverso da 1 cerca la stessa struttura con rapporti
/// falsati. Se la figura falsata guadagna quanto quella vera, il merito non e' di Fibonacci ma del comprare
/// dopo un ritracciamento.</para>
/// </summary>
public abstract class HarmonicEngine : EasyEngineBase
{
    /// <summary>Barre per lato di un pivot.</summary>
    protected int PivotBars = 3;

    /// <summary>Tolleranza relativa sugli intervalli dei rapporti (0,05 = ±5%).</summary>
    protected decimal Tolerance = 0.05m;

    /// <summary>Barre massime fra C e la barra di segnale: oltre, la figura scade.</summary>
    protected int MaxDBars = 30;

    /// <summary>Barre in cui cercare X, A, B, C e lo swing che chiude X.</summary>
    protected int LookbackBars = 300;

    /// <summary>Fattore su tutti i rapporti: 1 = la figura da manuale, altro = il controllo con rapporti falsati.</summary>
    protected decimal RatioScale = 1m;

    /// <summary>1 = stop oltre il piu' esterno fra X e D; 0 = lo stop comune della base.</summary>
    protected int StopStructure;

    /// <summary>Margine dello stop strutturale, in ATR delle barre.</summary>
    protected decimal StopBufferAtr;

    /// <summary>Barre dell'ATR del margine, fino alla barra di segnale.</summary>
    protected int AtrBars = 20;

    /// <summary>Target come frazione della gamba AD dal livello d'ingresso; 0 = il target comune della base.</summary>
    protected decimal TargetAd;

    /// <summary>Dimensione del tick dello strumento.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>I rapporti della figura, per la figura rialzista.</summary>
    protected abstract HarmonicRatios Ratios { get; }

    /// <summary>Sigla della figura nel motivo del segnale.</summary>
    protected abstract string PatternCode { get; }

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>La finestra degli swing, e l'ATR del margine con la chiusura prima.</summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(Math.Max(1, LookbackBars), Math.Max(1, AtrBars) + 1));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (PivotBars < 1 || Tolerance < 0m || Tolerance >= 1m || MaxDBars < 1 || AtrBars < 1 ||
            LookbackBars < 4 * PivotBars + 4 || RatioScale <= 0m || StopBufferAtr < 0m || TargetAd < 0m ||
            Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(PivotBars),
                $"{Name}: configurazione armonica incoerente (pivot {PivotBars}, tolleranza {Tolerance}, D entro " +
                $"{MaxDBars} barre, finestra {LookbackBars}, scala {RatioScale}, margine {StopBufferAtr} su " +
                $"{AtrBars} barre, target {TargetAd}, lato {Direction}); servono pivot e barre positivi, tolleranza " +
                "in [0, 1), una finestra che contenga cinque swing, scala positiva, margini non negativi, lato 0, 1 o 2.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        if (!FindSwings(data, out var c, out var b, out var a, out var x, out var cIsHigh))
            return Hold(bar.Close, barTime);

        // Il verso: C alto = figura rialzista, D sotto, si compra. Nello spazio "s × prezzo" la ribassista
        // diventa rialzista e i controlli si scrivono una volta sola.
        var side = cIsHigh ? SignalType.Buy : SignalType.Sell;
        if ((side == SignalType.Buy && Direction == 2) || (side == SignalType.Sell && Direction == 1))
            return Hold(bar.Close, barTime);

        var s = cIsHigh ? 1m : -1m;
        var last = data.Length - 1;
        if (last - c.Index > MaxDBars)
            return Hold(bar.Close, barTime);

        var xp = s * x.Price;
        var ap = s * a.Price;
        var bp = s * b.Price;
        var cp = s * c.Price;
        var xa = ap - xp;
        var ab = ap - bp;
        var bc = cp - bp;
        if (xa <= 0m || ab <= 0m || bc <= 0m)
            return Hold(bar.Close, barTime);

        var ratios = Ratios;
        if (!Within(ab / xa, ratios.BMin, ratios.BMax) || !Within(bc / ab, ratios.CMin, ratios.CMax))
            return Hold(bar.Close, barTime);

        var dp = ap - ratios.D * RatioScale * xa;
        var cd = cp - dp;
        if (cd <= 0m || !Within(cd / bc, ratios.CdMin, ratios.CdMax))
            return Hold(bar.Close, barTime);

        // Dopo C: nessuna barra oltre C, nessuna barra gia' su D.
        for (var index = c.Index + 1; index <= last; index++)
        {
            var far = cIsHigh ? data[index].High : data[index].Low;
            var near = cIsHigh ? data[index].Low : data[index].High;
            if (s * far > cp || s * near <= dp)
                return Hold(bar.Close, barTime);
        }

        // Verso l'interno della figura: nello spazio s × prezzo e' sempre verso l'alto, cioe' sopra D per
        // il long e sotto D per lo short.
        var level = s * (TickSize > 0m ? Math.Ceiling(dp / TickSize) * TickSize : dp);

        var signal = EntryLimitNextBar(side, level, data, barTime, $"{(cIsHigh ? "LE" : "SE")} {PatternCode}");
        signal.CrossedLevel = CrossedLevelPolicy.Market;

        var pointValue = InstrumentRegistry.PointValue(Symbol);
        var entry = s * level;

        if (StopStructure != 0)
        {
            var buffer = StopBufferAtr > 0m ? StopBufferAtr * BarAtr(data) : 0m;
            var distance = entry - (Math.Min(xp, dp) - buffer);
            if (distance > 0m)
                signal.StopLossMoneyPerFutureContract = Math.Round(distance * pointValue, 2);
        }

        if (TargetAd > 0m)
            signal.TakeProfitMoneyPerFutureContract = Math.Round(TargetAd * (ap - entry) * pointValue, 2);

        var entries = new List<TradeSignal>(1);
        AddEntry(entries, WithSessionExit(signal));
        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>Un punto della figura: indice della barra e prezzo dello swing.</summary>
    private readonly record struct Swing(int Index, decimal Price);

    /// <summary>
    /// C, B, A, X dalla barra piu' recente all'indietro, piu' lo swing opposto che chiude X. Vedi la nota
    /// sulla classe: senza quello swing X non e' stabile, e non c'e' segnale.
    /// </summary>
    private bool FindSwings(OhlcvData[] data, out Swing c, out Swing b, out Swing a, out Swing x, out bool cIsHigh)
    {
        c = b = a = x = default;
        cIsHigh = false;

        Span<Swing> swings = stackalloc Swing[4];
        Span<bool> highs = stackalloc bool[4];
        var count = 0;
        var last = data.Length - 1;
        var oldest = Math.Max(PivotBars, last - LookbackBars + 1);

        for (var index = last - PivotBars; index >= oldest; index--)
        {
            var isHigh = IsPivot(data, index, high: true);
            var isLow = IsPivot(data, index, high: false);
            if (isHigh == isLow)
                continue;

            var price = isHigh ? data[index].High : data[index].Low;
            if (count > 0 && highs[count - 1] == isHigh)
            {
                // Stesso swing: vale il piu' estremo; a pari prezzo resta il piu' recente.
                if (isHigh ? price > swings[count - 1].Price : price < swings[count - 1].Price)
                    swings[count - 1] = new Swing(index, price);
                continue;
            }

            if (count == 4)
            {
                c = swings[0];
                b = swings[1];
                a = swings[2];
                x = swings[3];
                cIsHigh = highs[0];
                return true;
            }

            swings[count] = new Swing(index, price);
            highs[count] = isHigh;
            count++;
        }

        return false;
    }

    /// <summary>Pivot a <see cref="PivotBars"/> barre: stretto a sinistra, non superato a destra.</summary>
    private bool IsPivot(OhlcvData[] data, int index, bool high)
    {
        var value = high ? data[index].High : data[index].Low;
        for (var offset = 1; offset <= PivotBars; offset++)
        {
            if (high)
            {
                if (data[index - offset].High >= value || data[index + offset].High > value) return false;
            }
            else
            {
                if (data[index - offset].Low <= value || data[index + offset].Low < value) return false;
            }
        }

        return true;
    }

    /// <summary>Il rapporto sta nell'intervallo allargato dalla tolleranza e scalato dal controllo.</summary>
    private bool Within(decimal ratio, decimal min, decimal max) =>
        ratio >= min * RatioScale * (1m - Tolerance) && ratio <= max * RatioScale * (1m + Tolerance);

    /// <summary>Media del true range delle ultime <see cref="AtrBars"/> barre, quella di segnale compresa.</summary>
    private decimal BarAtr(OhlcvData[] data)
    {
        decimal sum = 0m;
        for (var index = data.Length - AtrBars; index < data.Length; index++)
        {
            var previousClose = data[index - 1].Close;
            sum += Math.Max(data[index].High - data[index].Low,
                Math.Max(Math.Abs(data[index].High - previousClose), Math.Abs(data[index].Low - previousClose)));
        }

        return sum / AtrBars;
    }
}

/// <summary>Gartley: B a 0,618 di XA, C fra 0,382 e 0,886 di AB, CD fra 1,27 e 1,618 di BC, D a 0,786 di XA.</summary>
public abstract class GartleyEngine : HarmonicEngine
{
    protected override HarmonicRatios Ratios => new(0.618m, 0.618m, 0.382m, 0.886m, 1.27m, 1.618m, 0.786m);
    protected override string PatternCode => "GAR";
}

/// <summary>Bat: B fra 0,382 e 0,50 di XA, C fra 0,382 e 0,886 di AB, CD fra 1,618 e 2,618 di BC, D a 0,886 di XA.</summary>
public abstract class BatEngine : HarmonicEngine
{
    protected override HarmonicRatios Ratios => new(0.382m, 0.50m, 0.382m, 0.886m, 1.618m, 2.618m, 0.886m);
    protected override string PatternCode => "BAT";
}

/// <summary>Butterfly: B a 0,786 di XA, C fra 0,382 e 0,886 di AB, CD fra 1,618 e 2,24 di BC, D a 1,27 di XA (oltre X).</summary>
public abstract class ButterflyEngine : HarmonicEngine
{
    protected override HarmonicRatios Ratios => new(0.786m, 0.786m, 0.382m, 0.886m, 1.618m, 2.24m, 1.27m);
    protected override string PatternCode => "BUT";
}

/// <summary>Crab: B fra 0,382 e 0,618 di XA, C fra 0,382 e 0,886 di AB, CD fra 2,24 e 3,618 di BC, D a 1,618 di XA (oltre X).</summary>
public abstract class CrabEngine : HarmonicEngine
{
    protected override HarmonicRatios Ratios => new(0.382m, 0.618m, 0.382m, 0.886m, 2.24m, 3.618m, 1.618m);
    protected override string PatternCode => "CRB";
}
