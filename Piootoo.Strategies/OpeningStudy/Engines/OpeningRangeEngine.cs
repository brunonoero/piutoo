using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.OpeningStudy.Engines;

/// <summary>
/// Motore ORB: <b>opening range</b> dell'apertura di borsa. Il range sono i primi
/// <see cref="RangeMinutes"/> minuti dopo <see cref="OpenTime"/>; nella finestra che segue
/// (<see cref="EntryWindowMinutes"/>) si entra sulla sua rottura (<see cref="Mode"/> 0) oppure contro
/// una rottura che rientra (<see cref="Mode"/> 1, la "caccia alla liquidita'" dell'apertura). E' lo
/// studio dell'apertura: vedi <c>docs/domini/studio-apertura.md</c>. Vive con tutto lo studio in
/// <c>Piootoo.Strategies/OpeningStudy/</c>, per potersi togliere in un colpo se non porta a nulla.
///
/// <para><b>Perche' non e' SBO.</b> Il breakout di sessione del catalogo prende i livelli dalla
/// giornata della ricerca, che comincia a mezzanotte (o all'01:00) di Roma: il range della notte, non
/// quello dell'apertura cash. Qui l'orario e' quello della borsa e lo dichiara la strategia.</para>
///
/// <para><b>L'orologio.</b> <see cref="OpenTime"/> e' un orario locale nell'orologio di
/// <see cref="OpenClock"/>, per default l'ora di borsa dello strumento con il fuso dal calendario del
/// simbolo: le 09:00 di Berlino per FDAX, le 08:30 di Chicago — le 09:30 di New York — per NQ. Mai UTC
/// e mai Roma per un mercato americano: nelle settimane in cui l'ora legale americana ed europea non
/// sono allineate l'apertura di New York cade alle 14:30 di Roma, non alle 15:30.</para>
///
/// <para><b>Il range.</b> Le barre che aprono in [apertura, apertura + <see cref="RangeMinutes"/>) nel
/// giorno locale della barra di segnale. Devono esserci <b>tutte</b>: un range con una barra mancante
/// (festivo, buco del feed) non e' un range, e il giorno non opera. Sta sotto l'ora: su barre piu'
/// larghe l'apertura non si isola.</para>
///
/// <para><b>Breakout</b> (<see cref="Mode"/> 0): buy stop sul massimo del range, sell stop sul minimo,
/// piu' <see cref="OffsetTicks"/>, validi la barra dopo e riemessi finche' dura la finestra. Un lato che
/// il prezzo ha gia' toccato dopo il range e' speso: la rottura c'e' stata, eseguita o no, e il motore
/// non la ricompra al secondo passaggio. E' il "una rottura per lato al giorno" scritto senza stato.
/// Con <see cref="StructureStop"/> lo stop sta sul lato opposto del range.</para>
///
/// <para><b>Fade</b> (<see cref="Mode"/> 1): dopo il range, una barra buca il massimo (minimo) e chiude
/// di nuovo dentro, oppure chiude dentro subito dopo una chiusura fuori: si entra contro a mercato sulla
/// barra dopo. Con <see cref="StructureStop"/> lo stop sta oltre l'estremo toccato dopo il range.</para>
///
/// <para>Target: il comune della base, oppure <see cref="TargetRange"/> volte l'altezza del range. Una
/// posizione alla volta; long e short armati insieme vanno in OCO.</para>
/// </summary>
public abstract class OpeningRangeEngine : EasyEngineBase
{
    /// <summary>Orario locale dell'apertura, nell'orologio di <see cref="OpenClock"/>.</summary>
    protected TimeOnly OpenTime = new(9, 0);

    /// <summary>
    /// L'apertura come la scrive una griglia, <c>HHMM</c> (830 = 08:30). E' il solo punto in cui l'orario
    /// entra come numero, e passa dall'unico convertitore della codifica.
    /// </summary>
    protected int OpenHhmm
    {
        set => OpenTime = TimeFromLegacyHhmm(value);
    }

    /// <summary>In quale orologio e' scritta <see cref="OpenTime"/>: per default l'ora di borsa.</summary>
    protected InstrumentClock OpenClock = InstrumentClock.Exchange;

    /// <summary>Durata del range in minuti, multiplo del timeframe.</summary>
    protected int RangeMinutes = 30;

    /// <summary>Minuti dopo la fine del range in cui un ordine puo' vivere.</summary>
    protected int EntryWindowMinutes = 120;

    /// <summary>0 = breakout del range; 1 = fade della rottura che rientra.</summary>
    protected int Mode;

    /// <summary>Margine dello stop d'ingresso oltre il range, in tick (solo breakout).</summary>
    protected int OffsetTicks;

    /// <summary>Range massimo in multipli dell'ATR delle sessioni chiuse; 0 = spento.</summary>
    protected decimal MaxRangeAtr;

    /// <summary>1 = stop sulla struttura (lato opposto del range, o oltre l'estremo del fade); 0 = stop comune.</summary>
    protected int StructureStop;

    /// <summary>Margine oltre la struttura dello stop, in tick.</summary>
    protected int ExtremeBufferTicks;

    /// <summary>Target in multipli dell'altezza del range; 0 = il target comune della base.</summary>
    protected decimal TargetRange;

    /// <summary>Dimensione del tick dello strumento.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    private SessionClock? _openClockInstance;
    private InstrumentClock _openClockResolved;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// Dal primo minuto del range all'ultima barra di segnale possibile, e le sessioni dell'ATR se il
    /// filtro sul range e' acceso.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(
            (Math.Max(0, RangeMinutes) + Math.Max(0, EntryWindowMinutes)) / Math.Max(1, TimeframeMinutes) + 2,
            MaxRangeAtr > 0m ? SessionsToCandles(AtrSessions + 2) : 0));

    /// <summary>L'orologio dell'apertura, con il fuso dal calendario del simbolo. Vedi <c>HourOfDayEngine</c>.</summary>
    private SessionClock OpenLocalClock
    {
        get
        {
            if (_openClockInstance is null || _openClockResolved != OpenClock)
            {
                var calendar = MarketCalendarRegistry.Current.Get(Symbol);
                _openClockInstance = new SessionClock(OpenClock == InstrumentClock.Exchange
                    ? calendar.ExchangeTimeZone
                    : calendar.ResearchTimeZone);
                _openClockResolved = OpenClock;
            }

            return _openClockInstance;
        }
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        Validate();

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // L'apertura del giorno locale della barra di segnale, e la finestra misurata sulla barra su
        // cui l'ordine vivrebbe.
        var rangeStart = OpenLocalClock.SessionInstantUtc(barTime, OpenTime);
        var rangeEnd = rangeStart.AddMinutes(RangeMinutes);
        var entryOpen = barTime.AddMinutes(TimeframeMinutes);
        if (entryOpen < rangeEnd || entryOpen >= rangeEnd.AddMinutes(EntryWindowMinutes))
            return Hold(bar.Close, barTime);

        // Range ed estremi dopo il range in un giro solo all'indietro, senza LINQ.
        var last = data.Length - 1;
        decimal rangeHigh = decimal.MinValue, rangeLow = decimal.MaxValue;
        decimal afterHigh = decimal.MinValue, afterLow = decimal.MaxValue;
        var rangeBars = 0;
        for (var index = last; index >= 0; index--)
        {
            var candle = data[index];
            if (candle.DateTime < rangeStart)
                break;

            if (candle.DateTime >= rangeEnd)
            {
                if (candle.High > afterHigh) afterHigh = candle.High;
                if (candle.Low < afterLow) afterLow = candle.Low;
                continue;
            }

            if (candle.High > rangeHigh) rangeHigh = candle.High;
            if (candle.Low < rangeLow) rangeLow = candle.Low;
            rangeBars++;
        }

        if (rangeBars != RangeMinutes / TimeframeMinutes)
            return Hold(bar.Close, barTime);

        var height = rangeHigh - rangeLow;
        if (height <= 0m)
            return Hold(bar.Close, barTime);

        if (MaxRangeAtr > 0m)
        {
            var atr = ClosedSessionAtrPoints(data, barTime);
            if (atr is not { } points || points <= 0m || height > MaxRangeAtr * points)
                return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);
        if (Mode == 0)
            Breakout(entries, data, barTime, rangeHigh, rangeLow, afterHigh, afterLow, height);
        else
            Fade(entries, data, barTime, rangeEnd, rangeHigh, rangeLow, afterHigh, afterLow, height);

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private void Breakout(List<TradeSignal> entries, OhlcvData[] data, DateTime barTime,
        decimal rangeHigh, decimal rangeLow, decimal afterHigh, decimal afterLow, decimal height)
    {
        var offset = OffsetTicks * TickSize;
        var buffer = ExtremeBufferTicks * TickSize;

        var longLevel = RoundUp(rangeHigh + offset);
        if (Direction != 2 && afterHigh < longLevel)
        {
            var signal = EntryStopNextBar(SignalType.Buy, longLevel, data, barTime, "LE ORB");
            Protect(signal, StructureStop != 0 ? longLevel - (rangeLow - buffer) : 0m, height);
            AddEntry(entries, WithSessionExit(signal));
        }

        var shortLevel = RoundDown(rangeLow - offset);
        if (Direction != 1 && afterLow > shortLevel)
        {
            var signal = EntryStopNextBar(SignalType.Sell, shortLevel, data, barTime, "SE ORB");
            Protect(signal, StructureStop != 0 ? rangeHigh + buffer - shortLevel : 0m, height);
            AddEntry(entries, WithSessionExit(signal));
        }
    }

    private void Fade(List<TradeSignal> entries, OhlcvData[] data, DateTime barTime, DateTime rangeEnd,
        decimal rangeHigh, decimal rangeLow, decimal afterHigh, decimal afterLow, decimal height)
    {
        // Il fade guarda una rottura gia' avvenuta: la barra di segnale sta dopo il range.
        if (barTime < rangeEnd)
            return;

        var bar = data[^1];
        var previous = data[^2];
        var previousAfterRange = previous.DateTime >= rangeEnd;
        var buffer = ExtremeBufferTicks * TickSize;

        // Short: rottura sopra il range, prima chiusura di nuovo dentro.
        if (Direction != 1 && afterHigh > rangeHigh && bar.Close < rangeHigh &&
            (bar.High > rangeHigh || (previousAfterRange && previous.Close >= rangeHigh)))
        {
            var signal = EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE ORF");
            Protect(signal, StructureStop != 0 ? afterHigh + buffer - bar.Close : 0m, height);
            AddEntry(entries, WithSessionExit(signal));
        }

        // Long: rottura sotto il range, prima chiusura di nuovo dentro.
        if (Direction != 2 && afterLow < rangeLow && bar.Close > rangeLow &&
            (bar.Low < rangeLow || (previousAfterRange && previous.Close <= rangeLow)))
        {
            var signal = EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE ORF");
            Protect(signal, StructureStop != 0 ? bar.Close - (afterLow - buffer) : 0m, height);
            AddEntry(entries, WithSessionExit(signal));
        }
    }

    /// <summary>
    /// Stop sulla struttura e target sul range, in denaro per contratto. Una distanza nulla o negativa
    /// non e' uno stop: resta quello comune.
    /// </summary>
    private void Protect(TradeSignal signal, decimal stopDistance, decimal height)
    {
        var pointValue = InstrumentRegistry.PointValue(Symbol);
        if (stopDistance > 0m)
            signal.StopLossMoneyPerFutureContract = Math.Round(stopDistance * pointValue, 2);
        if (TargetRange > 0m)
            signal.TakeProfitMoneyPerFutureContract = Math.Round(TargetRange * height * pointValue, 2);
    }

    private decimal RoundUp(decimal level) =>
        TickSize > 0m ? Math.Ceiling(level / TickSize) * TickSize : level;

    private decimal RoundDown(decimal level) =>
        TickSize > 0m ? Math.Floor(level / TickSize) * TickSize : level;

    private void Validate()
    {
        var openMinute = (int)OpenTime.ToTimeSpan().TotalMinutes;
        if (TimeframeMinutes is < 1 or > 60 || RangeMinutes < TimeframeMinutes || RangeMinutes % TimeframeMinutes != 0 ||
            openMinute % TimeframeMinutes != 0 || EntryWindowMinutes < TimeframeMinutes || Mode is < 0 or > 1 ||
            OffsetTicks < 0 || MaxRangeAtr < 0m || StructureStop is < 0 or > 1 || ExtremeBufferTicks < 0 ||
            TargetRange < 0m || Direction is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(RangeMinutes),
                $"{Name}: configurazione ORB incoerente (timeframe {TimeframeMinutes}, apertura {OpenTime:HH\\:mm}, " +
                $"range {RangeMinutes} minuti, finestra {EntryWindowMinutes}, modo {Mode}, margini " +
                $"{OffsetTicks}/{ExtremeBufferTicks}, filtro {MaxRangeAtr}, stop {StructureStop}, target {TargetRange}, " +
                $"lato {Direction}); servono timeframe fino a 60 minuti, range multiplo del timeframe, apertura " +
                "sull'apertura di una barra, finestra di almeno una barra, modo 0 o 1, stop 0 o 1, valori non " +
                "negativi, lato 0, 1 o 2.");
        }
    }
}
