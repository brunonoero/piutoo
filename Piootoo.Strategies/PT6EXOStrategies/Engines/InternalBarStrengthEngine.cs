using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore IBS: <b>forza interna della barra</b>. <c>IBS = (C − L) / (H − L)</c> dice dove la barra ha
/// chiuso dentro il proprio range: 0 sul minimo, 1 sul massimo. Una chiusura sul minimo compra, una sul
/// massimo vende. E' ritorno alla media su <b>una barra sola</b>, non su una banda come RBB: il profilo
/// atteso e' di molti trade brevi con win rate alto, l'opposto delle trend following del catalogo, ed
/// e' il motivo per cui sta nella serie PT6EXO. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §IBS.
///
/// <para><b>Ingresso.</b> A mercato sulla barra dopo, quando l'IBS della barra chiusa e' sotto
/// <see cref="LowThreshold"/> (long) o sopra <see cref="HighThreshold"/> (short), dentro la finestra
/// dichiarata. Con <see cref="TrendBars"/> il lato deve stare dalla parte della media semplice delle
/// chiusure: si compra la debolezza solo sopra la media e si vende la forza solo sotto, che e' la forma
/// in cui l'anomalia e' documentata sugli indici.</para>
///
/// <para><b>Uscita.</b> Le uscite comuni della base (stop, target, <c>MaxBars</c>, fine sessione) e, con
/// <see cref="ExitIbs"/>, l'uscita classica: il long chiude alla prima barra che chiude con IBS almeno
/// <see cref="ExitIbs"/>, lo short alla prima con IBS al massimo <c>1 − ExitIbs</c>. E' un'uscita a
/// segnale (<c>ExitOnly</c>, a mercato sulla barra dopo), come l'incrocio inverso del MAC: il motore
/// interno e il cBot la eseguono gia'.</para>
///
/// <para>Una barra con massimo uguale al minimo non ha un IBS e non produce nulla.</para>
/// </summary>
public abstract class InternalBarStrengthEngine : EasyEngineBase
{
    /// <summary>IBS sotto cui si compra.</summary>
    protected decimal LowThreshold = 0.2m;

    /// <summary>IBS sopra cui si vende.</summary>
    protected decimal HighThreshold = 0.8m;

    /// <summary>
    /// Le due soglie in una leva sola, simmetriche attorno a 0,5: basso = valore, alto = 1 − valore. Serve
    /// alla griglia, che varia una leva per volta; fuori da [0; 0,5] le soglie si incrocerebbero e la
    /// valutazione si ferma con l'errore delle soglie incoerenti.
    /// </summary>
    protected decimal SymmetricThreshold
    {
        set
        {
            LowThreshold = value;
            HighThreshold = 1m - value;
        }
    }

    /// <summary>Barre della media semplice delle chiusure che fa da filtro di trend. 0 = nessun filtro.</summary>
    protected int TrendBars;

    /// <summary>
    /// IBS di uscita del long; lo short esce a <c>1 − ExitIbs</c>. 0 = nessuna uscita a segnale, restano
    /// le uscite comuni.
    /// </summary>
    protected decimal ExitIbs;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <inheritdoc />
    public override int RequiredCandles => Math.Max(base.RequiredCandles, TrendBars + 1);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (LowThreshold < 0m || HighThreshold > 1m || LowThreshold > HighThreshold || ExitIbs < 0m || ExitIbs > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(LowThreshold),
                $"{Name}: soglie IBS incoerenti (basso {LowThreshold}, alto {HighThreshold}, uscita {ExitIbs}); " +
                "servono 0 <= basso <= alto <= 1 e uscita fra 0 e 1.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var range = bar.High - bar.Low;
        if (range <= 0m)
            return Hold(bar.Close, barTime);

        var ibs = (bar.Close - bar.Low) / range;

        // In posizione si guarda solo l'uscita: un ingresso nuovo non nasce finche' non si e' flat.
        if (CurrentMP != 0)
        {
            if (ExitIbs > 0m && CurrentMP == 1 && ibs >= ExitIbs)
                return Exit(SignalType.Sell, data, barTime, "LX IBS");
            if (ExitIbs > 0m && CurrentMP == -1 && ibs <= 1m - ExitIbs)
                return Exit(SignalType.Buy, data, barTime, "SX IBS");
            return Hold(bar.Close, barTime);
        }

        if (InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var average = TrendBars > 0 ? SimpleAverageClose(data, TrendBars) : 0m;

        if (Direction != 2 && ibs < LowThreshold && (TrendBars == 0 || bar.Close > average))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE IBS"))
                   ?? Hold(bar.Close, barTime);
        }

        if (Direction != 1 && ibs > HighThreshold && (TrendBars == 0 || bar.Close < average))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE IBS"))
                   ?? Hold(bar.Close, barTime);
        }

        return Hold(bar.Close, barTime);
    }

    /// <summary>Uscita a mercato sulla barra dopo, che non apre mai nel verso opposto.</summary>
    private TradeSignal Exit(SignalType side, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryMarketNextBar(side, data[^1].Close, data, barTime, reason);
        signal.ExitOnly = true;
        return signal;
    }

    /// <summary>Media semplice delle ultime <paramref name="bars"/> chiusure, barra di segnale compresa.</summary>
    private static decimal SimpleAverageClose(OhlcvData[] data, int bars)
    {
        var sum = 0m;
        for (var index = data.Length - bars; index < data.Length; index++)
            sum += data[index].Close;
        return sum / bars;
    }
}
