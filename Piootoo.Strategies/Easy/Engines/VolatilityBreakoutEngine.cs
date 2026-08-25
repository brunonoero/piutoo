using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>Livello su cui armare il breakout di volatilità.</summary>
public enum VolatilityBreakoutLevel
{
    /// <summary>Apertura della sessione corrente più o meno un multiplo dell'ATR.</summary>
    SessionOpenAtrBand,

    /// <summary>Massimo/minimo della sessione precedente.</summary>
    PreviousSessionExtremes,

    /// <summary>Massimo/minimo delle ultime <see cref="VolatilityBreakoutEngine.RangeBars"/> barre.</summary>
    RecentBarExtremes
}

/// <summary>
/// Motore VBO allineato a <c>easy_engine_py/volatility_breakout.py</c>.
///
/// <para>Arma stop <i>next bar</i> a <c>O_d0 ± k × VOL</c>, dove VOL è il range della
/// sessione precedente, l'ATR delle sessioni chiuse oppure l'ATR intraday della barra precedente.
/// I gate neutri e direzionali sono speculari fra long e short.</para>
///
/// <para>Le varianti EasyLanguage preesistenti non equivalenti al VBO Python restano disponibili
/// soltanto impostando esplicitamente <see cref="UseLegacyVariant"/>.</para>
/// </summary>
public abstract class VolatilityBreakoutEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ intent e livelli

    /// <summary>Abilita la semantica storica non-Python della sottoclasse.</summary>
    protected bool UseLegacyVariant;

    /// <summary>1 = range d1, 2 = ATR delle sessioni chiuse, 3 = ATR intraday chiuso.</summary>
    protected int VolatilitySource = 1;

    /// <summary>
    /// Tipo di intent d'ingresso. <c>Market</c> replica 342; <c>Stop</c> replica 643 e 666.
    /// Sono ammessi solo questi due tipi perché la famiglia sorgente non emette limit entry.
    /// </summary>
    protected TradeOrderType EntryOrderType = TradeOrderType.Market;

    /// <summary>Sorgente del livello stop quando <see cref="EntryOrderType"/> è <c>Stop</c>.</summary>
    protected VolatilityBreakoutLevel EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;

    /// <summary>Numero di barre per gli estremi recenti (HighRange/LowRange di 643).</summary>
    protected int RangeBars = 1;

    /// <summary>Periodo dell'ATR. 0 disattiva la banda ATR.</summary>
    protected int AtrLength;

    /// <summary>Moltiplicatore ATR della banda long.</summary>
    protected decimal AtrMultiplierLong;

    /// <summary>Moltiplicatore ATR della banda short.</summary>
    protected decimal AtrMultiplierShort = -1m;

    /// <summary>0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>0 = spento, 1 = C_d1/C_d2, 2 = O_d0/C_d1.</summary>
    protected int Momentum;

    /// <summary>Giorno Python/pandas da escludere (0 = lunedì). -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>
    /// Richiede che il close abbia già oltrepassato la banda apertura ± ATR. È il trigger market
    /// di 342; può restare attivo anche per uno stop se la variante lo richiede.
    /// </summary>
    protected bool RequireCloseBeyondAtrBand;

    /// <summary>
    /// Fattore minimo dell'espansione rialzista d0 rispetto a d1. 0 disattiva il gate
    /// (<c>(highd0-opend0) &gt; (highd1-opend1) × factor</c>).
    /// </summary>
    protected decimal UpRangeFactor;

    /// <summary>Analogo ribassista di <see cref="UpRangeFactor"/>.</summary>
    protected decimal DownRangeFactor;

    // ------------------------------------------------------------------ tempo e frequenza

    /// <summary>Inizio della finestra operativa HHMM; -1 = non limitare.</summary>
    protected int StartTrade = -1;

    /// <summary>Fine esclusiva della finestra operativa HHMM, come <c>tw()</c>.</summary>
    protected int EndTrade = -1;

    /// <summary>Inizio pausa HHMM. -1 = nessuna pausa.</summary>
    protected int PauseStart = -1;

    /// <summary>Fine pausa HHMM. Una coppia invertita mantiene la pausa inattiva, come le sorgenti.</summary>
    protected int PauseEnd = -1;

    // MaxEntriesPerSession è dichiarato in EasyEngineBase e applicato da BuildEntry.

    /// <summary>
    /// Disarma un verso dopo che è entrato fino alla prossima sessione. Replica i flag
    /// <c>okl</c>/<c>oks</c> di 666; non blocca la riemissione dell'ordine finché esso non viene fillato.
    /// </summary>
    protected bool OneEntryPerSessionPerSide;

    /// <summary>
    /// Se true la posizione viene chiusa a fine sessione sui timeframe intraday. Corrisponde a
    /// <c>intraday_only</c> del motore Python, come nei motori TF, PC e BO, e vale <b>true per
    /// default</b>: un candidato con <c>intraday_only = 0</c> che non lo disattiva diventa una
    /// strategia di sessione senza che nessun test se ne accorga.
    /// </summary>
    protected bool IntradayOnly = true;

    // ------------------------------------------------------------------ pattern e calendario

    /// <summary>Pattern neutro richiesto. 55 è la sentinella sempre vera.</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che inibisce l'operatività. 56 è la sentinella sempre falsa.</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto, speculare per long e short.</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che inibisce, speculare per long e short.</summary>
    protected int DirectionalNo = 53;

    // Campi della variante storica: non sono parte del contratto Python.
    /// <summary>Gate PatternFast richiesto per il long.</summary>
    protected int FastYesLong = 152;

    /// <summary>Gate PatternFast che inibisce il long.</summary>
    protected int FastNoLong = 153;

    /// <summary>Gate PatternFast richiesto per lo short.</summary>
    protected int FastYesShort = 152;

    /// <summary>Gate PatternFast che inibisce lo short.</summary>
    protected int FastNoShort = 153;

    /// <summary>Gate PtnBaseSA2 richiesto per il long.</summary>
    protected int BaseYesLong = 41;

    /// <summary>Gate PtnBaseSA2 che inibisce il long.</summary>
    protected int BaseNoLong = 42;

    /// <summary>Gate PtnBaseSA2 richiesto per lo short.</summary>
    protected int BaseYesShort = 41;

    /// <summary>Gate PtnBaseSA2 che inibisce lo short.</summary>
    protected int BaseNoShort = 42;

    /// <summary>Giorno EasyLanguage escluso per lo short. -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Giorno EasyLanguage escluso per il long. -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Primo mese escluso (1-12). -1 = nessuno.</summary>
    protected int ExcludedMonthOne = -1;

    /// <summary>Secondo mese escluso (1-12). -1 = nessuno.</summary>
    protected int ExcludedMonthTwo = -1;

    // ------------------------------------------------------------------ stato sessione

    private bool _longArmed = true;
    private bool _shortArmed = true;

    /// <summary>
    /// L'ATR daily richiede <c>AtrLength + 1</c> sessioni chiuse (la prima fornisce la close
    /// precedente per il true range); l'ATR intraday richiede una barra ulteriore per lo shift.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        VolatilitySource == 2
            ? SessionsToCandles(Math.Max(1, AtrLength) + 1)
            : Math.Max(2, AtrLength + 1));

    /// <summary>Valutazione comune, da richiamare dal <c>GenerateSignal</c> della sottoclasse.</summary>
    protected TradeSignal EvaluateCore(OhlcvData[] data, DateTime currentDate) =>
        UseLegacyVariant ? EvaluateLegacyCore(data, currentDate) : EvaluatePythonParityCore(data, currentDate);

    private TradeSignal EvaluatePythonParityCore(OhlcvData[] data, DateTime currentDate)
    {
        var required = Math.Max(RequiredCandles, Math.Max(2, AtrLength + 1));
        if (data is null || data.Length < required)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);
        var sessionOpen = ohlc[0];
        var previousOpen = ohlc[4];
        var previousClose = ohlc[7];
        var previousPreviousClose = ohlc[11];
        if (sessionOpen <= 0m || previousOpen <= 0m)
            return Hold(bar.Close, barTime, "OHLC di sessione non disponibile");

        if (!InPythonTradingWindow(barTime) ||
            PythonDayOfWeek(barTime) == SkipDay ||
            (MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession) ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var volatility = ResolvePythonVolatility(data, barTime, ohlc);
        if (!volatility.HasValue || volatility.Value <= 0m)
            return Hold(bar.Close, barTime, "Volatilità non disponibile");

        var shortMultiplier = AtrMultiplierShort < 0m ? AtrMultiplierLong : AtrMultiplierShort;
        var longLevel = sessionOpen + AtrMultiplierLong * volatility.Value;
        var shortLevel = sessionOpen - shortMultiplier * volatility.Value;
        var momentumLong = Momentum switch
        {
            1 => previousClose > previousPreviousClose,
            2 => sessionOpen > previousClose,
            _ => true
        };
        var momentumShort = Momentum switch
        {
            1 => previousClose < previousPreviousClose,
            2 => sessionOpen < previousClose,
            _ => true
        };

        var entries = new List<TradeSignal>(2);
        if (CurrentMP != 1 && Direction != 2 && momentumLong &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSessionLimit(
                EntryStopNextBar(SignalType.Buy, longLevel, data, barTime, "LE VBO")));
        }

        if (CurrentMP != -1 && Direction != 1 && momentumShort &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSessionLimit(
                EntryStopNextBar(SignalType.Sell, shortLevel, data, barTime, "SE VBO")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal EvaluateLegacyCore(OhlcvData[] data, DateTime currentDate)
    {
        var required = Math.Max(RequiredCandles, Math.Max(AtrLength + 1, Math.Max(1, RangeBars)));
        if (data is null || data.Length < required)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);
        if (isStartOfSession)
        {
            _longArmed = true;
            _shortArmed = true;
        }

        // d0 e d1 devono essere completi: gli zeri sono il sentinella di OHLCMulti5 per storia
        // insufficiente e non vanno trasformati in livelli operativi.
        var sessionOpen = ohlc[0];
        var previousOpen = ohlc[4];
        if (sessionOpen <= 0m || previousOpen <= 0m)
            return Hold(bar.Close, barTime, "OHLC di sessione non disponibile");

        if (!EasyLib.TimeWindow(Clock, StartTrade, EndTrade, barTime) || IsInPause(barTime))
            return Hold(bar.Close, barTime);

        if (MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession)
            return Hold(bar.Close, barTime, "Tetto ingressi di sessione raggiunto");

        if (Clock.SessionDay(barTime).Month == ExcludedMonthOne || Clock.SessionDay(barTime).Month == ExcludedMonthTwo ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var atr = AtrLength > 0 ? EasyLib.AvgTrueRange(data, AtrLength) : 0m;
        if (AtrLength > 0 && atr <= 0m)
            return Hold(bar.Close, barTime, "ATR non disponibile");

        var longBand = sessionOpen + AtrMultiplierLong * atr;
        var shortBand = sessionOpen - AtrMultiplierShort * atr;
        var highd0 = ohlc[1];
        var lowd0 = ohlc[2];
        var highd1 = ohlc[5];
        var lowd1 = ohlc[6];
        var longRangePasses = UpRangeFactor <= 0m ||
                              highd0 - sessionOpen > (highd1 - previousOpen) * UpRangeFactor;
        var shortRangePasses = DownRangeFactor <= 0m ||
                               sessionOpen - lowd0 > (previousOpen - lowd1) * DownRangeFactor;

        var entries = new List<TradeSignal>(2);
        if ((!OneEntryPerSessionPerSide || _longArmed) &&
            CurrentMP != 1 &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            longRangePasses &&
            (!RequireCloseBeyondAtrBand || bar.Close > longBand) &&
            PassesLongPatterns(ohlc))
        {
            entries.Add(BuildEntry(SignalType.Buy, ResolveLevel(true, ohlc, data, longBand),
                data, barTime, "LE VBO"));
        }

        if ((!OneEntryPerSessionPerSide || _shortArmed) &&
            CurrentMP != -1 &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            shortRangePasses &&
            (!RequireCloseBeyondAtrBand || bar.Close < shortBand) &&
            PassesShortPatterns(ohlc))
        {
            entries.Add(BuildEntry(SignalType.Sell, ResolveLevel(false, ohlc, data, shortBand),
                data, barTime, "SE VBO"));
        }

        if (CurrentMP == 1) _longArmed = false;
        if (CurrentMP == -1) _shortArmed = false;
        return Combine(entries, Hold(bar.Close, barTime));
    }

    private decimal? ResolvePythonVolatility(OhlcvData[] data, DateTime barTime, decimal[] ohlc)
    {
        return VolatilitySource switch
        {
            1 => ohlc[5] - ohlc[6],
            2 => ClosedSessionAtr(data, barTime),
            3 => PreviousBarAtr(data),
            _ => null
        };
    }

    // Python: atr(df, n).shift(1). La barra corrente è quindi esclusa completamente.
    private decimal? PreviousBarAtr(OhlcvData[] data)
    {
        if (AtrLength <= 0 || data.Length < AtrLength + 1)
            return null;

        decimal sum = 0m;
        var first = data.Length - AtrLength - 1;
        for (var index = first; index < data.Length - 1; index++)
            sum += EasyLib.TrueRange(data[index], index > 0 ? data[index - 1] : null);
        return sum / AtrLength;
    }

    // Python: session_atr(df, n, shift=1). Aggrega solo sessioni finite e non legge d0.
    private decimal? ClosedSessionAtr(OhlcvData[] data, DateTime barTime)
    {
        if (AtrLength <= 0)
            return null;

        var currentSession = SessionKey(barTime);
        var sessions = new List<(DateTime Key, decimal High, decimal Low, decimal Close)>();
        DateTime? key = null;
        decimal high = 0m, low = 0m, close = 0m;
        foreach (var candidate in data)
        {
            var candidateKey = SessionKey(candidate.DateTime);
            if (candidateKey >= currentSession)
                break;

            if (key != candidateKey)
            {
                if (key.HasValue)
                    sessions.Add((key.Value, high, low, close));
                key = candidateKey;
                high = candidate.High;
                low = candidate.Low;
            }
            else
            {
                high = Math.Max(high, candidate.High);
                low = Math.Min(low, candidate.Low);
            }

            close = candidate.Close;
        }

        if (key.HasValue)
            sessions.Add((key.Value, high, low, close));
        if (sessions.Count < AtrLength + 1)
            return null;

        decimal sum = 0m;
        var first = sessions.Count - AtrLength;
        for (var index = first; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var previousClose = sessions[index - 1].Close;
            sum += Math.Max(session.High - session.Low,
                Math.Max(Math.Abs(session.High - previousClose), Math.Abs(session.Low - previousClose)));
        }

        return sum / AtrLength;
    }

    private TradeSignal WithPythonSessionLimit(TradeSignal signal)
    {
        // VBO Python richiede un solo fill per sessione; il contatore è dell'engine, non locale.
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = SessionKey(signal.ValidFromUtc!.Value);

        if (IntradayOnly && TimeframeMinutes < 1440)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc!.Value, SessionEndTime);

        return signal;
    }

    private bool InPythonTradingWindow(DateTime barTime)
    {
        // La finestra dichiarata vince: porta con se' il proprio fuso e i propri estremi HHMM,
        // quindi non va convertita ne' arrotondata all'ora piena.
        if (InDeclaredWindow(barTime) is { } declared)
            return declared;

        if (StartTrade < 0 && EndTrade < 0)
            return true;

        // Confronto su HHMM pieni, fine inclusa — la stessa semantica di PriceChannelEngine e
        // SessionBreakoutEngine. Prima si confrontavano le sole ore: la finestra si allargava
        // fino a HH:59 e prendeva barre che la fonte non prende.
        var start = StartTrade < 0 ? 0 : StartTrade;
        var end = EndTrade < 0 ? 2359 : EndTrade;
        var time = WindowClock.Hhmm(barTime);
        return start <= end ? time >= start && time <= end : time >= start || time <= end;
    }

    private int PythonDayOfWeek(DateTime value) => PythonWeekday(value);

    private DateTime SessionKey(DateTime time)
    {
        var start = Clock.SessionInstantUtc(time, SessionStartTime);
        return SessionStartTime > SessionEndTime && time <= start
            ? Clock.SessionInstantUtc(time.AddDays(-1), SessionStartTime)
            : start;
    }

    private TradeSignal BuildEntry(
        SignalType side, decimal level, OhlcvData[] data, DateTime barTime, string reason) =>
        EntryOrderType == TradeOrderType.Stop
            ? EntryStopNextBar(side, level, data, barTime, reason)
            : EntryMarketNextBar(side, level, data, barTime, reason);

    private decimal ResolveLevel(bool isLong, decimal[] ohlc, OhlcvData[] data, decimal atrBand)
    {
        if (EntryLevel == VolatilityBreakoutLevel.SessionOpenAtrBand)
            return atrBand;

        if (EntryLevel == VolatilityBreakoutLevel.PreviousSessionExtremes)
            return isLong ? ohlc[5] : ohlc[6];

        var level = isLong ? decimal.MinValue : decimal.MaxValue;
        var count = Math.Min(Math.Max(1, RangeBars), data.Length);
        for (var index = data.Length - count; index < data.Length; index++)
            level = isLong ? Math.Max(level, data[index].High) : Math.Min(level, data[index].Low);
        return level;
    }

    private bool IsInPause(DateTime barTime)
    {
        if (PauseStart < 0 || PauseEnd < 0) return false;
        var time = Hhmm(barTime);
        return time >= PauseStart && time <= PauseEnd;
    }

    private bool PassesLongPatterns(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesLong, ohlc) &&
        !EasyLib.PatternFast(FastNoLong, ohlc) &&
        EasyLib.PtnBaseSA2(BaseYesLong, ohlc) &&
        !EasyLib.PtnBaseSA2(BaseNoLong, ohlc);

    private bool PassesShortPatterns(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesShort, ohlc) &&
        !EasyLib.PatternFast(FastNoShort, ohlc) &&
        EasyLib.PtnBaseSA2(BaseYesShort, ohlc) &&
        !EasyLib.PtnBaseSA2(BaseNoShort, ohlc);
}
