using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore ERT: <b>il regime di trend temporaneo</b>, letto dall'efficiency ratio di Kaufman. Si entra
/// quando il mercato comincia a muoversi in una direzione sola e si esce quando smette: il regime decide
/// sia l'ingresso sia l'uscita, non un livello di prezzo. Nasce dallo studio di persistenza del
/// 29/09/2026 (<c>ricerca/persistenza-2026-09-29/esito.md</c>): la curva di una strategia non dice se
/// continuera' ad andare bene, quindi il trend temporaneo va cercato nel mercato. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §ERT.
///
/// <para><b>La misura.</b> Efficiency ratio con segno sulle ultime <see cref="ErBars"/> barre:
/// <c>ER = (C[t] − C[t−N]) / Σ |C[i] − C[i−1]|</c>, fra −1 e +1. Vale ±1 quando ogni barra chiude nello
/// stesso verso, zero quando il prezzo torna da dove era partito. Il <b>punteggio</b> e'
/// <c>ER × √N</c>: in una passeggiata casuale |ER| vale circa <c>1/√N</c>, quindi il punteggio ha la stessa
/// scala per ogni N e una soglia vale lo stesso su una finestra di 10 barre e su una di 80. E' questo che
/// permette alla griglia di variare N da sola.</para>
///
/// <para><b>Ingresso.</b> Punteggio almeno <see cref="EntryScore"/> → long, al massimo
/// <c>−EntryScore</c> → short, a mercato sulla barra dopo. Con <see cref="FreshOnly"/> il punteggio deve
/// aver passato la soglia <b>su questa barra</b> (sulla barra prima era sotto): e' il trend che si accende,
/// non uno gia' vecchio; senza, si rientra anche dopo uno stop finche' il regime dura.</para>
///
/// <para><b>Uscita.</b> Le uscite comuni della base e, con <see cref="ExitScore"/> maggiore di zero,
/// quella di regime: il long chiude quando il punteggio scende a <see cref="ExitScore"/> o sotto, lo short
/// quando sale a <c>−ExitScore</c> o sopra. Un'inversione vera passa di li' per forza. E' un'uscita a
/// segnale (<c>ExitOnly</c>, a mercato sulla barra dopo), come quella dell'IBS.</para>
///
/// <para>Senza stato: le due misure si ricalcolano dalla finestra, <c>O(ErBars)</c> per barra. Un percorso
/// nullo (chiusure tutte uguali) non ha un ER e non produce nulla.</para>
/// </summary>
public abstract class TrendEfficiencyEngine : EasyEngineBase
{
    /// <summary>Barre della finestra dell'efficiency ratio.</summary>
    protected int ErBars = 20;

    /// <summary>Punteggio (<c>ER × √N</c>) da cui il trend e' acceso e si entra nel suo verso.</summary>
    protected decimal EntryScore = 2m;

    /// <summary>Punteggio a cui il trend e' spento e la posizione esce. 0 = nessuna uscita di regime.</summary>
    protected decimal ExitScore = 0.5m;

    /// <summary>1 = solo sulla barra in cui il punteggio passa la soglia; 0 = finche' ci resta sopra.</summary>
    protected int FreshOnly = 1;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>La finestra dell'ER sulla barra corrente e su quella prima, piu' la chiusura di partenza.</summary>
    public override int RequiredCandles => Math.Max(base.RequiredCandles, ErBars + 2);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (ErBars < 2 || EntryScore <= 0m || ExitScore < 0m || ExitScore >= EntryScore || FreshOnly is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(EntryScore),
                $"{Name}: configurazione ERT incoerente (finestra {ErBars}, ingresso {EntryScore}, uscita {ExitScore}, " +
                $"solo nuovi {FreshOnly}); servono finestra di almeno due barre, 0 <= uscita < ingresso e solo nuovi 0 o 1.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var scale = (decimal)Math.Sqrt(ErBars);

        var current = SignedEfficiency(data, data.Length - 1);
        if (current is null)
            return Hold(bar.Close, barTime);
        var score = current.Value * scale;

        // In posizione si guarda solo l'uscita di regime: un ingresso nuovo non nasce finche' non si e' flat.
        if (CurrentMP != 0)
        {
            if (ExitScore > 0m && CurrentMP == 1 && score <= ExitScore)
                return Exit(SignalType.Sell, data, barTime, "LX ERT");
            if (ExitScore > 0m && CurrentMP == -1 && score >= -ExitScore)
                return Exit(SignalType.Buy, data, barTime, "SX ERT");
            return Hold(bar.Close, barTime);
        }

        if (InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var previousScore = 0m;
        if (FreshOnly == 1)
        {
            var previous = SignedEfficiency(data, data.Length - 2);
            previousScore = previous is null ? 0m : previous.Value * scale;
        }

        if (Direction != 2 && score >= EntryScore && (FreshOnly == 0 || previousScore < EntryScore))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE ERT"))
                   ?? Hold(bar.Close, barTime);
        }

        if (Direction != 1 && score <= -EntryScore && (FreshOnly == 0 || previousScore > -EntryScore))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE ERT"))
                   ?? Hold(bar.Close, barTime);
        }

        return Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Efficiency ratio con segno della finestra che finisce sulla barra <paramref name="last"/>; null se il
    /// percorso e' nullo.
    /// </summary>
    private decimal? SignedEfficiency(OhlcvData[] data, int last)
    {
        var first = last - ErBars;
        var path = 0m;
        for (var index = first + 1; index <= last; index++)
            path += Math.Abs(data[index].Close - data[index - 1].Close);

        return path == 0m ? null : (data[last].Close - data[first].Close) / path;
    }

    /// <summary>Uscita a mercato sulla barra dopo, che non apre mai nel verso opposto.</summary>
    private TradeSignal Exit(SignalType side, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryMarketNextBar(side, data[^1].Close, data, barTime, reason);
        signal.ExitOnly = true;
        return signal;
    }
}
