using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore VLM: <b>anomalia di volume</b>, famiglia <b>declassata</b> del catalogo. La barra appena
/// chiusa ha un'attivita' oltre <see cref="SpikeRatio"/> volte la mediana delle
/// <see cref="LookbackBars"/> barre prima di lei: si segue la sua direzione o la si sfuma. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §VLM.
///
/// <para><b>Perche' e' declassata.</b> Il volume di cTrader e' tick volume: quante volte il broker ha
/// aggiornato la quotazione, non quanti contratti sono passati di mano. Quel poco che dice e' in gran
/// parte gia' detto dall'ampiezza della barra e dalla stagionalita' oraria, e il volume vero c'e' solo
/// nel feed interno, che in live non abbiamo. Per questo la cella si ricerca solo sul feed del broker
/// che la esegue, e con la soglia <b>relativa</b> (la mediana della finestra), mai con un livello
/// assoluto.</para>
///
/// <para><b>La leva che decide: <see cref="ActivitySource"/>.</b> Con 0 l'attivita' e' il volume, con 1
/// e' l'ampiezza della barra (<c>H − L</c>) al posto del volume, a parita' di tutto il resto. E' il
/// controllo prescritto dal catalogo e il <b>solo criterio di ammissione</b> della famiglia: una cella
/// sul volume e' ammessa solo se batte la stessa cella sull'ampiezza. Se non la batte, il volume era un
/// travestimento della volatilita' e la cella si scarta, qualunque sia il suo risultato da sola.</para>
///
/// <para><b>Il segnale.</b> La mediana si calcola sulle barre <b>prima</b> di quella di segnale, esclusa
/// lei: una barra anomala non deve alzare la soglia che la misura. La soglia e' stretta (attivita'
/// maggiore di <c>SpikeRatio × mediana</c>). Con <see cref="Mode"/> 0 si segue la barra (chiusura sopra
/// l'apertura → long), con 1 la si sfuma. Una barra con chiusura uguale all'apertura non ha una
/// direzione e non produce nulla; una mediana nulla (feed senza volume) non e' un riferimento e non
/// produce nulla.</para>
///
/// <para><b>L'ingresso</b> e' a mercato sulla barra dopo. Una posizione alla volta: in posizione non
/// nasce nulla.</para>
/// </summary>
public abstract class VolumeSpikeEngine : EasyEngineBase
{
    /// <summary>Multiplo della mediana oltre cui l'attivita' della barra e' un'anomalia.</summary>
    protected decimal SpikeRatio = 2.5m;

    /// <summary>Barre prima di quella di segnale su cui si misura la mediana.</summary>
    protected int LookbackBars = 20;

    /// <summary>0 = segue la direzione della barra anomala, 1 = la sfuma.</summary>
    protected int Mode;

    /// <summary>
    /// Cosa si misura: 0 = volume, 1 = ampiezza della barra (<c>H − L</c>). L'1 e' il controllo che la cella
    /// sul volume deve battere per essere ammessa.
    /// </summary>
    protected int ActivitySource;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>La finestra della mediana piu' la barra di segnale.</summary>
    public override int RequiredCandles => Math.Max(base.RequiredCandles, Math.Max(1, LookbackBars) + 1);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (SpikeRatio <= 0m || LookbackBars < 1 || Mode is < 0 or > 1 || ActivitySource is < 0 or > 1 ||
            Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(SpikeRatio),
                $"{Name}: configurazione VLM incoerente (rapporto {SpikeRatio}, finestra {LookbackBars}, modo {Mode}, " +
                $"attivita' {ActivitySource}, lato {Direction}); servono rapporto positivo, finestra almeno 1, " +
                "modo 0 o 1, attivita' 0 (volume) o 1 (ampiezza) e lato 0, 1 o 2.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Senza direzione non c'e' niente da seguire ne' da sfumare: si esce prima di calcolare la mediana.
        if (bar.Close == bar.Open)
            return Hold(bar.Close, barTime);

        var median = MedianActivityBefore(data, LookbackBars);
        if (median <= 0m || Activity(bar) <= SpikeRatio * median)
            return Hold(bar.Close, barTime);

        var barUp = bar.Close > bar.Open;
        var side = (barUp ^ (Mode == 1)) ? SignalType.Buy : SignalType.Sell;

        if ((side == SignalType.Buy && Direction == 2) || (side == SignalType.Sell && Direction == 1))
            return Hold(bar.Close, barTime);

        var reason = side == SignalType.Buy ? "LE VLM" : "SE VLM";
        return WithSessionExit(EntryMarketNextBar(side, bar.Close, data, barTime, reason))
               ?? Hold(bar.Close, barTime);
    }

    /// <summary>Volume o ampiezza della barra, secondo <see cref="ActivitySource"/>.</summary>
    private decimal Activity(OhlcvData bar) => ActivitySource == 0 ? bar.Volume : bar.High - bar.Low;

    /// <summary>
    /// Mediana dell'attivita' delle <paramref name="bars"/> barre che precedono l'ultima, esclusa l'ultima.
    /// L'array e' locale alla valutazione: niente stato condiviso fra valutazioni, e la strategia viene
    /// comunque clonata a ogni barra. Con un numero pari di barre e' la media delle due centrali.
    /// </summary>
    private decimal MedianActivityBefore(OhlcvData[] data, int bars)
    {
        var values = new decimal[bars];
        var start = data.Length - 1 - bars;
        for (var index = 0; index < bars; index++)
            values[index] = Activity(data[start + index]);

        Array.Sort(values);
        var middle = bars / 2;
        return bars % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2m;
    }
}
