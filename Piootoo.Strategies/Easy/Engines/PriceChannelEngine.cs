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
    /// Un trail sul canale è un'uscita ricalcolata barra per barra. Non è esprimibile nei termini
    /// di un <see cref="TradeSignal"/> di ingresso; le sottoclassi che lo abilitano sono quindi
    /// marcate close-dependent ed escluse dal catalogo eseguibile.
    /// </summary>
    public override bool IsPositionCloseDependent => UseDonchianTrailing;

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

    /// <summary>
    /// Usa gli estremi della sessione corrente (<c>HighD(0)</c>/<c>LowD(0)</c>) per gli ingressi
    /// invece del canale rolling. Il canale rimane disponibile, ad esempio, per il trail originale.
    /// </summary>
    protected bool UseCurrentSessionExtremesForEntries;

    // ------------------------------------------------------------------ gate temporali e daily

    /// <summary>Inizio della finestra operativa HHMM.</summary>
    protected int StartTime;

    /// <summary>Fine della finestra operativa HHMM.</summary>
    protected int EndTime = 2359;

    /// <summary>True per estremi inclusivi; false per la semantica <c>tw()</c> con fine esclusiva.</summary>
    protected bool TradingWindowInclusive = true;

    /// <summary>Inizio pausa intraday. -1 = nessuna pausa.</summary>
    protected int PauseStart = -1;

    /// <summary>Fine pausa intraday. -1 = nessuna pausa.</summary>
    protected int PauseEnd = -1;

    /// <summary>Giorno EasyLanguage escluso per il long (0 = domenica). -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno EasyLanguage escluso per lo short (0 = domenica). -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Giorno pandas escluso: 0 = lunedì, -1 = nessuno.</summary>
    protected int SkipDay = -1;

    // ------------------------------------------------------------------ filtro ADX

    /// <summary>Periodo ADX. 0 disattiva il filtro.</summary>
    protected int AdxLength;

    /// <summary>Soglia massima ADX oltre la quale non si entra.</summary>
    protected decimal AdxThreshold = 100m;

    /// <summary>
    /// Calcola l'ADX una volta all'apertura di sessione da d1/d2, come
    /// <c>iADXOnArray</c>. False usa l'ADX rolling delle barre del grafico.
    /// </summary>
    protected bool UseSessionAdx;

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

    /// <summary>
    /// Replica <c>UseDonchianTrailing</c>: il trail dinamico rende la strategia close-dependent.
    /// </summary>
    protected bool UseDonchianTrailing;

    /// <summary>
    /// Orario HHMM della chiusura dopo <see cref="MaxDaysInTrade"/>. -1 usa la deadline generica.
    /// </summary>
    protected int MaxDaysFlatTime = -1;

    // ------------------------------------------------------------------ gate pattern

    /// <summary>Pattern neutro richiesto. 55 è la sentinella sempre vera.</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che blocca l'operatività. 56 è una sentinella sempre falsa.</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto per il long. Il segno è applicato dal motore.</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che blocca l'ingresso. 53 può essere usato come sentinella falsa.</summary>
    protected int DirectionalNo = 53;

    // MaxEntriesPerSession e TrailingStopMoney vivono in EasyEngineBase e vengono
    // applicati da BuildEntry; i helper sotto ne rafforzano solo la policy Python/legacy.

    // Stato ricorsivo di iADXOnArray sulle sessioni; deve sopravvivere alle valutazioni stateless.
    private decimal _adxValue;
    private decimal _adx0;
    private decimal _adx1;
    private decimal _adx2;
    private decimal _adx3;

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
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        return UseLegacyVariant
            ? GenerateLegacySignal(data, bar, barTime, ohlc, isStartOfSession)
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
        // Offset esattamente come lo dichiara la strategia, senza tick aggiuntivi.
        //
        // Fino al 17/08/2026 qui si sommava un tick in piu' (`(OffsetTicks + 1) * TickSize`), con
        // un commento che lo attribuiva alla convenzione del motore Python. Il sorgente di
        // riferimento dice il contrario: `price_channel.py` calcola `upper + offset * tick` e
        // `breakout.py` fa lo stesso — SessionBreakoutEngine lo riproduceva gia' correttamente.
        // Anche le schede della ricerca concordano: con `breakout_offset_ticks = 2` il livello e'
        // "+ 2 tick (0.5 pt)", non 0,75, e con offset 0 non c'e' alcun buffer.
        //
        // Il tick in piu' non e' un modo valido di compensare lo slippage che il riferimento
        // applica sui fill stop e l'engine no: alzare il livello cambia *se* il breakout scatta,
        // non solo a che prezzo viene riempito. Quella rettifica va fatta al confronto, come
        // descritto in docs/domini/porting-da-report-sweep.md.
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
        decimal[] ohlc,
        bool isStartOfSession)
    {
        if (CurrentMP != 0 ||
            !InTradingWindow(barTime) ||
            !PassesDailyFactor(ohlc) ||
            !PassesNeutralGates(ohlc) ||
            !PassesAdxGate(data, ohlc, isStartOfSession))
        {
            return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);
        var offset = OffsetPoints + OffsetTicks * TickSize;
        GetEntryLevels(data, barTime, out var longLevel, out var shortLevel);

        if (EnableLong &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            PassesDirectionalGates(+1, ohlc))
        {
            entries.Add(WithLegacySettings(
                EntryStopNextBar(SignalType.Buy, longLevel + offset, data, barTime, "PC_LE")));
        }

        if (EnableShort &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            PassesDirectionalGates(-1, ohlc))
        {
            entries.Add(WithLegacySettings(
                EntryStopNextBar(SignalType.Sell, shortLevel - offset, data, barTime, "PC_SE")));
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

        if (MaxDaysInTrade > 0 && MaxDaysFlatTime >= 0)
            signal.CloseAtUtc = ResolveMaxDaysCloseAt(signal.ValidFromUtc!.Value);

        return signal;
    }

    private bool InTradingWindow(DateTime barTime)
    {
        var inWindow = TradingWindowInclusive
            ? EasyLib.TimeWindowInclusive(Clock, StartTime, EndTime, barTime)
            : EasyLib.TimeWindow(Clock, StartTime, EndTime, barTime);
        if (!inWindow || PauseStart < 0 || PauseEnd < 0)
            return inWindow;

        var time = Hhmm(barTime);
        return time < PauseStart || time > PauseEnd;
    }

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

    private bool PassesAdxGate(OhlcvData[] data, decimal[] ohlc, bool isStartOfSession)
    {
        if (AdxLength <= 0)
            return true;

        if (UseSessionAdx)
        {
            if (isStartOfSession)
                UpdateSessionAdx(ohlc);
        }
        else
        {
            _adxValue = CalculateBarAdx(data);
        }

        return _adxValue < AdxThreshold;
    }

    private void UpdateSessionAdx(decimal[] ohlc)
    {
        var calc = new[] { _adx0, _adx1, _adx2, _adx3 };
        _adxValue = EasyLib.iADXOnArray(
            AdxLength,
            ohlc[5], ohlc[6], ohlc[7],
            ohlc[9], ohlc[10], ohlc[11],
            ref calc) * 100m;
        _adx0 = calc[0];
        _adx1 = calc[1];
        _adx2 = calc[2];
        _adx3 = calc[3];
    }

    private decimal CalculateBarAdx(OhlcvData[] data)
    {
        if (data.Length < 2)
            return 0m;

        var calc = new decimal[4];
        for (var index = 1; index < data.Length; index++)
        {
            _ = EasyLib.iADXOnArray(
                AdxLength,
                data[index].High, data[index].Low, data[index].Close,
                data[index - 1].High, data[index - 1].Low, data[index - 1].Close,
                ref calc);
        }

        return calc[0] * 100m;
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

    private void GetEntryLevels(
        OhlcvData[] data,
        DateTime barTime,
        out decimal longLevel,
        out decimal shortLevel)
    {
        if (!UseCurrentSessionExtremesForEntries)
        {
            longLevel = HighestChannelHigh(data);
            shortLevel = LowestChannelLow(data);
            return;
        }

        var session = SessionKey(barTime);
        longLevel = decimal.MinValue;
        shortLevel = decimal.MaxValue;
        foreach (var candidate in data)
        {
            if (SessionKey(candidate.DateTime) != session)
                continue;

            longLevel = Math.Max(longLevel, candidate.High);
            shortLevel = Math.Min(shortLevel, candidate.Low);
        }
    }

    private DateTime ResolveMaxDaysCloseAt(DateTime entryValidFrom)
    {
        var target = Clock.SessionInstantUtc(
            entryValidFrom.AddDays(Math.Max(0, MaxDaysInTrade - 1)), MaxDaysFlatTime);
        return target > entryValidFrom
            ? target
            : Clock.SessionInstantUtc(entryValidFrom.AddDays(Math.Max(1, MaxDaysInTrade)), MaxDaysFlatTime);
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStartTime);
        return timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStartTime)
            : sessionStart;
    }

    private bool InPythonTradingWindow(DateTime barTime)
    {
        if (StartTime < 0 && EndTime < 0)
            return true;

        // Il motore Python confronta l'orario completo con gli estremi "HH:00", fine inclusa.
        // Confrontare le sole ore allargava la finestra fino a HH:59: con end_hour = 4 entravano
        // anche le barre 04:15–04:45, che nella fonte non producono segnali.
        var start = StartTime < 0 ? 0 : StartTime;
        var end = EndTime < 0 ? 2359 : EndTime;
        var time = Hhmm(barTime);
        return start <= end ? time >= start && time <= end : time >= start || time <= end;
    }

    private int PythonDayOfWeek(DateTime instantUtc) =>
        ((int)Clock.SessionDay(instantUtc).DayOfWeek + 6) % 7;

    private DateTime SessionKey(DateTime time)
    {
        var start = Clock.SessionInstantUtc(time, SessionStartTime);
        return SessionStartTime > SessionEndTime && time < start
            ? Clock.SessionInstantUtc(time.AddDays(-1), SessionStartTime)
            : start;
    }
}
