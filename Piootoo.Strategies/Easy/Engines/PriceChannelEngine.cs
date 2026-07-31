using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore riutilizzabile per breakout Price Channel / Donchian.
///
/// <para>Il canale è calcolato sulle ultime <see cref="ChannelBars"/> barre, inclusa la barra
/// appena chiusa che produce il segnale, come <c>highest(high, N)</c> EasyLanguage e il motore
/// Python. Non introduce look-ahead: gli OHLC della barra sono noti alla sua chiusura e l'ordine
/// stop resta valido solo dalla barra successiva tramite <see cref="EasyEngineBase.EntryStopNextBar"/>.</para>
///
/// <para>I gate di sessione, orario, pattern e fattore daily sono valutati prima del canale. Il
/// limite di ingressi non viene gestito con contatori locali: viene dichiarato sul segnale e
/// applicato dal motore di esecuzione al fill, così uno stop non eseguito può essere riemesso.</para>
/// </summary>
public abstract class PriceChannelEngine : EasyEngineBase
{
    /// <summary>
    /// Abilita la traduzione EasyLanguage storica della sottoclasse. I suoi filtri daily,
    /// calendario EasyLanguage e limiti configurabili non fanno parte del contratto Python e
    /// restano disponibili soltanto con questo opt-in esplicito.
    /// </summary>
    protected bool UseLegacyVariant;

    // ------------------------------------------------------------------ canale e direzioni

    /// <summary>Numero di barre che formano il canale, inclusa la barra appena chiusa.</summary>
    protected int ChannelBars = 20;

    /// <summary>Abilita gli stop buy nella variante EasyLanguage storica.</summary>
    protected bool EnableLong = true;

    /// <summary>Abilita gli stop sell nella variante EasyLanguage storica.</summary>
    protected bool EnableShort = true;

    /// <summary>0 = entrambi, 1 = solo long, 2 = solo short, come il motore Python.</summary>
    protected int Direction;

    /// <summary>Buffer espresso in tick, sommato al buffer in punti.</summary>
    protected int OffsetTicks;

    /// <summary>Buffer additivo espresso in punti.</summary>
    protected decimal OffsetPoints;

    /// <summary>Dimensione del tick dello strumento, necessaria solo per <see cref="OffsetTicks"/>.</summary>
    protected decimal TickSize;

    // ------------------------------------------------------------------ gate temporali e daily

    /// <summary>Inizio della finestra operativa HHMM.</summary>
    protected int StartTime;

    /// <summary>Fine della finestra operativa HHMM.</summary>
    protected int EndTime = 2359;

    /// <summary>True per estremi inclusivi; false per la semantica <c>tw()</c> con fine esclusiva.</summary>
    protected bool TradingWindowInclusive = true;

    /// <summary>Giorno EasyLanguage escluso per il long (0 = domenica). -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno EasyLanguage escluso per lo short (0 = domenica). -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Giorno pandas escluso: 0 = lunedì, -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>
    /// Soglia minima dell'ATR a 14 sessioni chiuse, espressa in dollari per contratto.
    /// 0 = filtro disattivo.
    /// </summary>
    protected decimal DvolMin;

    /// <summary>
    /// Se true, per timeframe intraday dichiara la chiusura al termine della sessione.
    /// Corrisponde a <c>intraday_only</c> del motore Python.
    /// </summary>
    protected bool IntradayOnly = true;

    /// <summary>
    /// Fattore opzionale sul corpo della sessione chiusa precedente. Se valorizzato, richiede
    /// <c>abs(openD(1) - closeD(1)) &lt; valore × (highD(1) - lowD(1))</c>.
    /// </summary>
    protected decimal? DailyFactorValue;

    // ------------------------------------------------------------------ gate pattern

    /// <summary>Pattern neutro richiesto. 55 è la sentinella sempre vera.</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che blocca l'operatività. 56 è una sentinella sempre falsa.</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto per il long. Il segno è applicato dal motore.</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che blocca l'ingresso. 53 può essere usato come sentinella falsa.</summary>
    protected int DirectionalNo = 53;

    // ------------------------------------------------------------------ entrate e uscite

    /// <summary>Massimo numero di fill per sessione. 0 = nessun limite.</summary>
    protected int MaxEntriesPerSession;

    /// <summary>Trailing stop monetario per contratto di riferimento. 0 = disattivo.</summary>
    protected int TrailingStopMoney;

    /// <inheritdoc />
    public override int RequiredCandles => Math.Max(
        Math.Max(base.RequiredCandles, Math.Max(1, ChannelBars) + 1),
        !UseLegacyVariant && DvolMin > 0m ? SessionsToCandles(15) : 0);

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles || ChannelBars <= 0)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        return UseLegacyVariant
            ? GenerateLegacySignal(data, bar, barTime, ohlc)
            : GeneratePythonParitySignal(data, bar, barTime, ohlc);
    }

    private TradeSignal GeneratePythonParitySignal(
        OhlcvData[] data,
        OhlcvData bar,
        DateTime barTime,
        decimal[] ohlc)
    {
        if (CurrentMP != 0 ||
            !InPythonTradingWindow(barTime) ||
            PythonDayOfWeek(barTime) == SkipDay ||
            !PassesNeutralGates(ohlc) ||
            !PassesDailyVolatilityGate(data, barTime))
        {
            return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);
        var offset = OffsetPoints + OffsetTicks * TickSize;

        if (Direction != 2 &&
            PassesDirectionalGates(+1, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Buy, HighestChannelHigh(data) + offset, data, barTime, "PC_LE")));
        }

        if (Direction != 1 &&
            PassesDirectionalGates(-1, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Sell, LowestChannelLow(data) - offset, data, barTime, "PC_SE")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal GenerateLegacySignal(
        OhlcvData[] data,
        OhlcvData bar,
        DateTime barTime,
        decimal[] ohlc)
    {
        if (CurrentMP != 0 ||
            !InTradingWindow(barTime) ||
            !PassesDailyFactor(ohlc) ||
            !PassesNeutralGates(ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);
        var offset = OffsetPoints + OffsetTicks * TickSize;

        if (EnableLong &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            PassesDirectionalGates(+1, ohlc))
        {
            entries.Add(WithLegacySettings(
                EntryStopNextBar(SignalType.Buy, HighestChannelHigh(data) + offset, data, barTime, "PC_LE")));
        }

        if (EnableShort &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            PassesDirectionalGates(-1, ohlc))
        {
            entries.Add(WithLegacySettings(
                EntryStopNextBar(SignalType.Sell, LowestChannelLow(data) - offset, data, barTime, "PC_SE")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        signal.TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null;
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = SessionKey(signal.ValidFromUtc!.Value);

        if (IntradayOnly && TimeframeMinutes < 1440)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc.Value, SessionEndTime);

        return signal;
    }

    private TradeSignal WithLegacySettings(TradeSignal signal)
    {
        signal.TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null;

        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = GetSessionStartUtc(signal.ValidFromUtc!.Value);
        }

        return signal;
    }

    private bool InTradingWindow(DateTime barTime) =>
        TradingWindowInclusive
            ? EasyLib.TimeWindowInclusive(StartTime, EndTime, barTime)
            : EasyLib.TimeWindow(StartTime, EndTime, barTime);

    private bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);

    private bool PassesDirectionalGates(int direction, decimal[] ohlc) =>
        EasyLib.PatternDirectionalFast(direction * DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(direction * DirectionalNo, ohlc);

    private bool PassesDailyFactor(decimal[] ohlc)
    {
        if (!DailyFactorValue.HasValue)
            return true;

        var range = ohlc[5] - ohlc[6];
        return range > 0m && Math.Abs(ohlc[4] - ohlc[7]) < DailyFactorValue.Value * range;
    }

    private bool PassesDailyVolatilityGate(OhlcvData[] data, DateTime barTime)
    {
        if (DvolMin <= 0m)
            return true;

        var atr = ClosedSessionAtr(data, barTime);
        return atr.HasValue && atr.Value * InstrumentRegistry.PointValue(Symbol) >= DvolMin;
    }

    // Python: session_atr(df, 14, shift=1). d0 non entra mai nel calcolo e il punto
    // valore trasforma l'ATR in punti nel valore monetario richiesto da dvol_min.
    private decimal? ClosedSessionAtr(OhlcvData[] data, DateTime barTime)
    {
        const int atrLength = 14;
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
        if (sessions.Count < atrLength + 1)
            return null;

        decimal sum = 0m;
        for (var index = sessions.Count - atrLength; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var previousClose = sessions[index - 1].Close;
            sum += Math.Max(session.High - session.Low,
                Math.Max(Math.Abs(session.High - previousClose), Math.Abs(session.Low - previousClose)));
        }

        return sum / atrLength;
    }

    private decimal HighestChannelHigh(OhlcvData[] data)
    {
        var first = data.Length - ChannelBars;
        var highest = data[first].High;
        for (var index = first + 1; index < data.Length; index++)
        {
            if (data[index].High > highest)
                highest = data[index].High;
        }

        return highest;
    }

    private decimal LowestChannelLow(OhlcvData[] data)
    {
        var first = data.Length - ChannelBars;
        var lowest = data[first].Low;
        for (var index = first + 1; index < data.Length; index++)
        {
            if (data[index].Low < lowest)
                lowest = data[index].Low;
        }

        return lowest;
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = EasyLib.CombineDateAndHhmm(timeUtc.Date, SessionStartTime);
        return timeUtc < sessionStart ? sessionStart.AddDays(-1) : sessionStart;
    }

    private bool InPythonTradingWindow(DateTime barTime)
    {
        if (StartTime < 0 && EndTime < 0)
            return true;

        var start = StartTime < 0 ? 0 : StartTime / 100;
        var end = EndTime < 0 ? 23 : EndTime / 100;
        var hour = barTime.Hour;
        return start <= end ? hour >= start && hour <= end : hour >= start || hour <= end;
    }

    private static int PythonDayOfWeek(DateTime value) => ((int)value.DayOfWeek + 6) % 7;

    private DateTime SessionKey(DateTime time)
    {
        var start = EasyLib.CombineDateAndHhmm(time.Date, SessionStartTime);
        return SessionStartTime > SessionEndTime && time < start ? start.AddDays(-1) : start;
    }
}
