using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore RUN: <b>ritorno alla media dopo una serie</b>. Dopo una sequenza di chiusure nella stessa
/// direzione, o con l'RSI di Wilder a pochi periodi oltre una soglia estrema, si entra <b>contro</b> la
/// serie. Il profilo atteso e' l'opposto del trend following: vince spesso e poco, perde di rado e
/// tanto, ed e' per questo che sta nella serie PT6EXO — compone bene con le TF del catalogo. Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §RUN.
///
/// <para><b>Il segnale</b>, scelto da <see cref="Mode"/>:</para>
/// <list type="bullet">
///   <item><c>0</c> — le ultime <see cref="RunBars"/> barre, quella appena chiusa compresa, hanno chiuso
///   ognuna <b>sopra</b> la chiusura precedente (serie al rialzo → short) o ognuna <b>sotto</b> (serie al
///   ribasso → long). Una chiusura uguale alla precedente interrompe la serie in entrambi i versi. Vale
///   "almeno N": una serie di sei con <see cref="RunBars"/> = 3 e' una serie anche alla quarta barra, ma a
///   quel punto il motore e' gia' in posizione e il gate non fa nascere un secondo ingresso.</item>
///   <item><c>1</c> — RSI di Wilder a <see cref="RsiPeriod"/> barre sotto <see cref="RsiLow"/> (long) o
///   sopra <see cref="RsiHigh"/> (short). E' la forma classica a due periodi.</item>
/// </list>
///
/// <para><b>L'RSI e' una funzione della finestra, non uno stato.</b> Lo smoothing di Wilder e' una media
/// esponenziale: il valore dipende da dove si comincia. Partire dall'inizio della finestra ricevuta
/// darebbe per la stessa barra un RSI diverso in backtest e in sessione live, che non ricevono la stessa
/// quantita' di storia. Si parte quindi sempre da <c>RsiPeriod × 10</c> variazioni prima della barra di
/// segnale, con la media semplice delle prime <see cref="RsiPeriod"/> come seme: il peso residuo del seme
/// e' <c>(1 − 1/n)^(9n)</c>, meno di un decimillesimo per ogni periodo, cioe' l'RSI e' gia' convergente e
/// resta identico qualunque sia la lunghezza della finestra. E' anche il motivo del
/// <see cref="RequiredCandles"/> in modalita' RSI.</para>
///
/// <para><b>Filtro di trend</b> opzionale, come nell'IBS: con <see cref="TrendBars"/> il long nasce solo
/// con la chiusura sopra la media semplice delle chiusure, lo short solo sotto. Si compra la debolezza
/// dentro un rialzo, non un crollo.</para>
///
/// <para><b>Ingresso</b> a mercato sulla barra dopo. <b>Uscita</b>: quelle comuni della base (stop,
/// target, <c>MaxBars</c> — l'"uscita a N barre" del catalogo —, fine sessione) e, con
/// <see cref="ExitOnFirstOppositeClose"/>, l'uscita classica della famiglia: il long chiude alla prima
/// barra che chiude sopra la chiusura precedente, lo short alla prima che chiude sotto. E' un segnale di
/// sola uscita (<c>ExitOnly</c>, a mercato sulla barra dopo), lo stesso percorso dell'IBS e dell'incrocio
/// inverso del MAC. L'uscita si valuta anche fuori dalla finestra dichiarata: la finestra decide dove
/// nascono gli ingressi, non dove si puo' uscire.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nessun ingresso.</para>
/// </summary>
public abstract class RunOfClosesEngine : EasyEngineBase
{
    /// <summary>
    /// Variazioni su cui si calcola l'RSI, in multipli del periodo. Dieci periodi bastano perche' il
    /// seme non pesi piu' (vedi la nota sulla classe); non e' una leva, e' la definizione del calcolo.
    /// </summary>
    private const int RsiWarmupFactor = 10;

    /// <summary>Segnale: 0 = serie di chiusure consecutive, 1 = RSI di Wilder oltre una soglia.</summary>
    protected int Mode;

    /// <summary>Chiusure consecutive nello stesso verso che fanno una serie, con <see cref="Mode"/> = 0.</summary>
    protected int RunBars = 3;

    /// <summary>Periodo dell'RSI di Wilder, con <see cref="Mode"/> = 1.</summary>
    protected int RsiPeriod = 2;

    /// <summary>RSI sotto cui si compra.</summary>
    protected decimal RsiLow = 10m;

    /// <summary>RSI sopra cui si vende.</summary>
    protected decimal RsiHigh = 90m;

    /// <summary>Barre della media semplice delle chiusure che fa da filtro di trend. 0 = nessun filtro.</summary>
    protected int TrendBars;

    /// <summary>
    /// 1 = uscita a segnale alla prima chiusura contraria (sopra la precedente per il long, sotto per lo
    /// short); 0 = restano le sole uscite comuni.
    /// </summary>
    protected int ExitOnFirstOppositeClose;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// La serie e la barra prima (o le variazioni dell'RSI piu' la chiusura da cui partono), e la media
    /// del filtro di trend.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(
            TrendBars + 1,
            Mode == 1 ? RsiPeriod * RsiWarmupFactor + 1 : RunBars + 1));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (Mode is < 0 or > 1 || RunBars < 1 || RsiPeriod < 1 || TrendBars < 0 ||
            RsiLow < 0m || RsiHigh > 100m || RsiLow > RsiHigh)
        {
            throw new ArgumentOutOfRangeException(nameof(Mode),
                $"{Name}: configurazione RUN incoerente (modo {Mode}, serie {RunBars}, periodo RSI {RsiPeriod}, " +
                $"soglie RSI {RsiLow}/{RsiHigh}, media {TrendBars}); servono modo 0 o 1, serie e periodo " +
                "almeno 1, media non negativa e 0 <= soglia bassa <= soglia alta <= 100.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var previousClose = data[^2].Close;

        // In posizione si guarda solo l'uscita: un ingresso nuovo non nasce finche' non si e' flat.
        if (CurrentMP != 0)
        {
            if (ExitOnFirstOppositeClose != 0 && CurrentMP == 1 && bar.Close > previousClose)
                return Exit(SignalType.Sell, data, barTime, "LX RUN");
            if (ExitOnFirstOppositeClose != 0 && CurrentMP == -1 && bar.Close < previousClose)
                return Exit(SignalType.Buy, data, barTime, "SX RUN");
            return Hold(bar.Close, barTime);
        }

        if (InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        bool buy, sell;
        if (Mode == 0)
        {
            buy = IsRun(data, RunBars, rising: false);
            sell = IsRun(data, RunBars, rising: true);
        }
        else
        {
            var rsi = WilderRsi(data, RsiPeriod, RsiPeriod * RsiWarmupFactor);
            buy = rsi < RsiLow;
            sell = rsi > RsiHigh;
        }

        if (!buy && !sell)
            return Hold(bar.Close, barTime);

        var average = TrendBars > 0 ? SimpleAverageClose(data, TrendBars) : 0m;

        if (buy && Direction != 2 && (TrendBars == 0 || bar.Close > average))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE RUN"))
                   ?? Hold(bar.Close, barTime);
        }

        if (sell && Direction != 1 && (TrendBars == 0 || bar.Close < average))
        {
            return WithSessionExit(EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE RUN"))
                   ?? Hold(bar.Close, barTime);
        }

        return Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Vero se le ultime <paramref name="bars"/> barre hanno chiuso ognuna oltre la precedente nel verso
    /// chiesto: sopra con <paramref name="rising"/>, sotto altrimenti. Una chiusura uguale interrompe.
    /// </summary>
    private static bool IsRun(OhlcvData[] data, int bars, bool rising)
    {
        for (var index = data.Length - bars; index < data.Length; index++)
        {
            var change = data[index].Close - data[index - 1].Close;
            if (rising ? change <= 0m : change >= 0m)
                return false;
        }

        return true;
    }

    /// <summary>
    /// RSI di Wilder sulle ultime <paramref name="changes"/> variazioni di chiusura: media semplice delle
    /// prime <paramref name="period"/> come seme, poi <c>media = (media × (n − 1) + valore) / n</c>. Senza
    /// perdite l'RSI vale 100, senza movimento 50 (nessun eccesso in nessun verso).
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
