using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore FIB: <b>conte di Fibonacci nel tempo</b>. Si individua l'ultimo swing — un pivot di
/// <see cref="PivotBars"/> barre per lato — e si entra esattamente <see cref="CountBars"/> barre dopo,
/// contro il movimento partito dal pivot (o a favore). E' una delle idee <b>bizzarre</b> della serie
/// PT6EXO: non ha un'ipotesi economica, e il suo solo metro e' il controllo a ingresso casuale RAN con
/// le stesse uscite. Se la cella non batte con chiarezza la distribuzione dei semi, il guadagno viene
/// dalle uscite e non dalla conta. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §FIB.
///
/// <para><b>Il pivot.</b> Una barra e' un pivot di massimo se il suo alto e' <b>strettamente</b>
/// maggiore degli alti delle <see cref="PivotBars"/> barre prima e dopo; di minimo, lo specchio sui
/// bassi. Stretto perche' su una serie piatta ogni barra pareggerebbe le vicine e sarebbe un pivot.
/// Un pivot e' <b>confermato</b> solo quando le <see cref="PivotBars"/> barre dopo sono chiuse: prima
/// non si sa, e un motore che lo usasse guarderebbe il futuro. Una barra che e' insieme pivot di
/// massimo e di minimo (una outside bar in mezzo a barre strette) non dice in che verso sia partito il
/// movimento, e non conta.</para>
///
/// <para><b>L'ultimo swing.</b> La conta parte dal pivot confermato piu' recente, di qualunque dei due
/// tipi: se dopo il pivot ne e' stato confermato un altro, lo swing e' cambiato e la conta ricomincia
/// da li'. Il segnale nasce sulla barra che chiude la <see cref="CountBars"/>-esima barra dopo il pivot
/// e l'ingresso e' a mercato sulla barra dopo, come il <c>barssince = N then buy next bar</c> di
/// EasyLanguage: la conta si completa alla chiusura, non all'apertura.</para>
///
/// <para><b>Si contano le barre, non il tempo.</b> "13 barre dopo" sono tredici barre stampate della
/// serie, qualunque buco le separi (notte, festivo, fine settimana): e' la stessa regola di
/// <c>MaxBarsInPosition</c>. Contare il tempo darebbe un'altra idea, e sulle barre che mancano
/// entrerebbe in istanti in cui il mercato non c'e'.</para>
///
/// <para><b>Senza stato.</b> Il pivot si cerca da capo a ogni valutazione, e solo nelle ultime
/// <see cref="CountBars"/> + <see cref="PivotBars"/> + 1 barre: il pivot sta esattamente
/// <see cref="CountBars"/> barre indietro e gli servono <see cref="PivotBars"/> barre prima di lui. Il
/// costo e' O(<see cref="CountBars"/> × <see cref="PivotBars"/>) confronti per barra, senza LINQ e
/// senza memoria fra una barra e l'altra: backtest e sessione live vedono la stessa cosa con qualunque
/// lunghezza di storia.</para>
///
/// <para><b>Perche' la conta e' vincolata ai numeri di Fibonacci.</b> <see cref="CountBars"/> accetta
/// solo 3, 5, 8, 13, 21, 34, 55. Un intero libero trasformerebbe la famiglia in "entra N barre dopo uno
/// swing", un'altra idea con molti piu' gradi di liberta': una griglia su 3..55 prova 53 valori invece
/// di 7, e il migliore dei 53 esce per caso con una probabilita' molto piu' alta. Se l'idea non regge
/// sui suoi numeri, non la si salva con quelli in mezzo.</para>
/// </summary>
public abstract class FibonacciTimeEngine : EasyEngineBase
{
    /// <summary>Barre per lato che definiscono un pivot, e che servono a confermarlo.</summary>
    protected int PivotBars = 3;

    /// <summary>Barre dopo il pivot su cui nasce l'ingresso: 3, 5, 8, 13, 21, 34 o 55.</summary>
    protected int CountBars = 13;

    /// <summary>
    /// 0 = contro il movimento partito dal pivot (dopo un minimo si vende, dopo un massimo si compra);
    /// 1 = a favore.
    /// </summary>
    protected int Mode;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>Il pivot, le sue barre prima e la conta.</summary>
    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, Math.Max(1, CountBars) + Math.Max(1, PivotBars) + 1);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        ValidateConfiguration();

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var last = data.Length - 1;
        var pivot = last - CountBars;

        // Nessun pivot confermato dopo quello della conta: altrimenti lo swing e' cambiato. I pivot
        // delle ultime PivotBars barre non sono ancora confermati e non contano.
        for (var index = last - PivotBars; index > pivot; index--)
        {
            if (IsPivotHigh(data, index) || IsPivotLow(data, index))
                return Hold(bar.Close, barTime);
        }

        var high = IsPivotHigh(data, pivot);
        var low = IsPivotLow(data, pivot);

        // Nessun pivot, oppure una barra che e' pivot da entrambi i lati: il verso del movimento non
        // e' definito.
        if (high == low)
            return Hold(bar.Close, barTime);

        // Dopo un pivot di minimo il movimento partito dal pivot e' al rialzo.
        var moveUp = low;
        var side = (Mode == 1) == moveUp ? SignalType.Buy : SignalType.Sell;

        if ((Direction == 1 && side != SignalType.Buy) || (Direction == 2 && side != SignalType.Sell))
            return Hold(bar.Close, barTime);

        var signal = WithSessionExit(EntryMarketNextBar(side, bar.Close, data, barTime,
            side == SignalType.Buy ? "LE FIB" : "SE FIB"));

        return signal ?? Hold(bar.Close, barTime);
    }

    /// <summary>Alto strettamente maggiore di quelli delle <see cref="PivotBars"/> barre per lato.</summary>
    private bool IsPivotHigh(OhlcvData[] data, int index)
    {
        var high = data[index].High;
        for (var offset = 1; offset <= PivotBars; offset++)
        {
            if (data[index - offset].High >= high || data[index + offset].High >= high)
                return false;
        }

        return true;
    }

    /// <summary>Basso strettamente minore di quelli delle <see cref="PivotBars"/> barre per lato.</summary>
    private bool IsPivotLow(OhlcvData[] data, int index)
    {
        var low = data[index].Low;
        for (var offset = 1; offset <= PivotBars; offset++)
        {
            if (data[index - offset].Low <= low || data[index + offset].Low <= low)
                return false;
        }

        return true;
    }

    private void ValidateConfiguration()
    {
        if (CountBars is not (3 or 5 or 8 or 13 or 21 or 34 or 55))
        {
            throw new ArgumentOutOfRangeException(nameof(CountBars), CountBars,
                $"{Name}: CountBars deve essere un numero di Fibonacci fra 3, 5, 8, 13, 21, 34 e 55.");
        }

        if (PivotBars < 1 || PivotBars > CountBars)
        {
            throw new ArgumentOutOfRangeException(nameof(PivotBars), PivotBars,
                $"{Name}: PivotBars deve stare fra 1 e CountBars ({CountBars}): un pivot che si conferma " +
                "dopo la fine della conta non e' ancora noto quando l'ingresso dovrebbe nascere.");
        }

        if (Mode is not (0 or 1) || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: Mode {Mode} (0 contro, 1 a favore) o Direction {Direction} (0, 1, 2) non validi.");
        }
    }
}
