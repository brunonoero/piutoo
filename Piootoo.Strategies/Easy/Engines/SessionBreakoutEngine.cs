using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore breakout sugli estremi delle ultime N sessioni.
///
/// <para>Per default replica <c>easy_engine_py/breakout.py</c>: il canale usa solo sessioni
/// chiuse e, se richiesto, gli estremi della sessione corrente <em>prima</em> della barra in
/// valutazione. Gli stop sono quindi sempre validi dalla barra successiva e non leggono mai
/// high/low della barra che li genera.</para>
///
/// <para>Una direzione si disarma appena entra in posizione (<c>OKL</c>/<c>OKS</c>): un solo
/// ingresso per sessione e per verso, come nell'originale.</para>
///
/// <para>Copre <c>TOP_UA_287</c>; la stessa struttura serve 120, 298 e 736.</para>
/// </summary>
public abstract class SessionBreakoutEngine : EasyEngineBase
{
    /// <summary>
    /// Abilita la traduzione EasyLanguage storica della sottoclasse. I suoi gate ADX, pause,
    /// calendario EasyLanguage e disarmo per verso non appartengono al contratto Python e sono
    /// disponibili soltanto con questo opt-in esplicito.
    /// </summary>
    protected bool UseLegacyVariant;

    // ------------------------------------------------------------------ livelli

    /// <summary>Numero di sessioni chiuse su cui calcolare massimo e minimo (<c>nSess</c>).</summary>
    protected int Sessions = 1;

    /// <summary>
    /// Se true i livelli inglobano anche gli estremi della sessione in corso
    /// (<c>levIncludeSess0</c>): il livello si allarga barra dopo barra.
    /// </summary>
    protected bool IncludeCurrentSession = true;

    /// <summary>Offset del livello Python, espresso in tick.</summary>
    protected int BreakoutOffsetTicks;

    /// <summary>Dimensione tick usata dall'offset Python; il default replica il motore Python.</summary>
    protected decimal TickSize = 0.1m;

    /// <summary>
    /// Se true, il trade viene chiuso alla fine della sessione per i timeframe intraday.
    /// Corrisponde a <c>intraday_only</c> del motore Python.
    /// </summary>
    protected bool IntradayOnly = true;

    /// <summary>Giorno da escludere nella convenzione pandas: 0 = lunedì, -1 = nessuno.</summary>
    protected int SkipDay = -1;

    // ------------------------------------------------------------------ filtro ADX

    /// <summary>Periodo ADX (<c>ADXLen</c>). 0 disattiva il filtro.</summary>
    protected int AdxLength;

    /// <summary>Soglia massima di ADX oltre la quale non si opera (<c>ADXTH</c>).</summary>
    protected decimal AdxThreshold = 100m;

    // ------------------------------------------------------------------ finestra oraria

    /// <summary>Inizio finestra operativa HHMM (<c>MyStartTime</c>).</summary>
    protected int StartTime;

    /// <summary>Fine finestra operativa HHMM, esclusa, come <c>tw()</c> (<c>MyEndTime</c>).</summary>
    protected int EndTime = 2359;

    /// <summary>Inizio della pausa in cui non si opera (<c>MyStartPause</c>).</summary>
    protected int PauseStart = -1;

    /// <summary>Fine della pausa (<c>MyEndPause</c>).</summary>
    protected int PauseEnd = -1;

    // ------------------------------------------------------------------ gate di pattern

    /// <summary>Pattern neutro richiesto (<c>PtnNeutYes</c>).</summary>
    protected int NeutralYes = 55;

    /// <summary>Secondo pattern neutro richiesto (<c>PtnNeutYes2</c>).</summary>
    protected int NeutralYes2 = 55;

    /// <summary>Pattern neutro che impedisce l'operatività (<c>PtnNeutNo</c>).</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto, con segno applicato al verso (<c>ptnDirYes</c>).</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che impedisce l'ingresso (<c>ptnDirNo</c>).</summary>
    protected int DirectionalNo = 53;

    /// <summary>Sessione della settimana da saltare per il long (<c>SkipSessL</c>). -1 = nessuna.</summary>
    protected int SkipSessionLong = -1;

    /// <summary>Sessione della settimana da saltare per lo short (<c>SkipSessS</c>).</summary>
    protected int SkipSessionShort = -1;

    // Varianti che sostituiscono la coppia neutro/direzionale con quattro gate PtnBaseSA2, uno
    // per verso. Sentinelle: 41 = sempre vero, 42 = sempre falso.

    /// <summary>Pattern PtnBaseSA2 richiesto per il long (<c>MyPtnLY</c>).</summary>
    protected int BaseYesLong = 41;

    /// <summary>Pattern PtnBaseSA2 che impedisce il long (<c>MyPtnLN</c>).</summary>
    protected int BaseNoLong = 42;

    /// <summary>Pattern PtnBaseSA2 richiesto per lo short (<c>MyPtnSY</c>).</summary>
    protected int BaseYesShort = 41;

    /// <summary>Pattern PtnBaseSA2 che impedisce lo short (<c>MyPtnSN</c>).</summary>
    protected int BaseNoShort = 42;

    /// <summary>Giorno della settimana escluso per il long, 0 = domenica. -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno della settimana escluso per lo short, 0 = domenica. -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Mese escluso per il long (1-12). -1 = nessuno.</summary>
    protected int NotEntryMonthLong = -1;

    /// <summary>Mese escluso per lo short (1-12). -1 = nessuno.</summary>
    protected int NotEntryMonthShort = -1;

    // ------------------------------------------------------------------ stato di sessione

    private decimal _hh;
    private decimal _ll;
    private bool _okLong;
    private bool _okShort;
    private int _sessionOfWeek = -1;
    private bool _levelsReady;

    // Stato dell'ADX ricorsivo: i quattro accumulatori di iADXOnArray devono sopravvivere fra le
    // sessioni, altrimenti la media mobile riparte da zero e il filtro è privo di significato.
    private decimal _adxValue;
    private decimal _adx0;
    private decimal _adx1;
    private decimal _adx2;
    private decimal _adx3;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        return UseLegacyVariant
            ? GenerateLegacySignal(data, currentDate)
            : GeneratePythonParitySignal(data, currentDate);
    }

    private TradeSignal GeneratePythonParitySignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        if (!TryGetPythonLevels(data, barTime, out var longLevel, out var shortLevel))
            return Hold(bar.Close, barTime, "Livelli BO non disponibili");
        if (!InPythonTradingWindow(barTime))
            return Hold(bar.Close, barTime, "Fuori finestra BO");
        if (PythonDayOfWeek(barTime) == SkipDay)
            return Hold(bar.Close, barTime, "Giorno BO escluso");
        if (!EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime, "Pattern neutro BO non valido");
        }

        var offset = BreakoutOffsetTicks * TickSize;
        var entries = new List<TradeSignal>(2);
        if (EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Buy, longLevel + offset, data, barTime, "LE BO")));
        }

        if (EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Sell, shortLevel - offset, data, barTime, "SE BO")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal GenerateLegacySignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
        {
            UpdateAdx(ohlc);
            ResetLevels(ohlc);
            _okLong = true;
            _okShort = true;

            // L'originale sposta di uno l'indice di giornata quando la sessione attraversa la
            // mezzanotte, perché la sessione "di lunedì" comincia domenica sera.
            _sessionOfWeek = SessionStartTime > SessionEndTime
                ? EasyDayOfWeek(barTime) + 1
                : EasyDayOfWeek(barTime);
        }

        if (!_levelsReady)
            return Hold(bar.Close, barTime, "Livelli di sessione non ancora inizializzati");

        if (IncludeCurrentSession)
        {
            _hh = Math.Max(_hh, bar.High);
            _ll = Math.Min(_ll, bar.Low);
        }

        // Una direzione già in posizione resta disarmata fino alla prossima apertura.
        if (CurrentMP == 1) _okLong = false;
        if (CurrentMP == -1) _okShort = false;

        if (!InTradingWindow(barTime) || !PassesNeutralGates(ohlc))
            return Hold(bar.Close, barTime);

        if (AdxLength > 0 && _adxValue >= AdxThreshold)
            return Hold(bar.Close, barTime, "ADX oltre soglia");

        var entries = new List<TradeSignal>(2);

        if (_okLong &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc) &&
            EasyLib.PtnBaseSA2(BaseYesLong, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoLong, ohlc) &&
            _sessionOfWeek != SkipSessionLong &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            barTime.Month != NotEntryMonthLong)
        {
            entries.Add(EntryStopNextBar(SignalType.Buy, _hh, data, barTime, "LE"));
        }

        if (_okShort &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc) &&
            EasyLib.PtnBaseSA2(BaseYesShort, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoShort, ohlc) &&
            _sessionOfWeek != SkipSessionShort &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            barTime.Month != NotEntryMonthShort)
        {
            entries.Add(EntryStopNextBar(SignalType.Sell, _ll, data, barTime, "SE"));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool TryGetPythonLevels(
        OhlcvData[] data,
        DateTime barTime,
        out decimal longLevel,
        out decimal shortLevel)
    {
        var sessions = Math.Clamp(Sessions, 1, 5);
        longLevel = decimal.MinValue;
        shortLevel = decimal.MaxValue;
        var currentSession = SessionKey(barTime);
        var completed = new List<(DateTime Key, decimal High, decimal Low)>();
        for (var index = 0; index < data.Length; index++)
        {
            var candidate = data[index];
            var key = SessionKey(candidate.DateTime);
            if (key >= currentSession)
                continue;

            if (completed.Count == 0 || completed[^1].Key != key)
            {
                completed.Add((key, candidate.High, candidate.Low));
                continue;
            }

            var previous = completed[^1];
            completed[^1] = (key, Math.Max(previous.High, candidate.High), Math.Min(previous.Low, candidate.Low));
        }

        if (completed.Count < sessions)
            return false;

        for (var index = completed.Count - sessions; index < completed.Count; index++)
        {
            longLevel = Math.Max(longLevel, completed[index].High);
            shortLevel = Math.Min(shortLevel, completed[index].Low);
        }

        if (IncludeCurrentSession)
        {
            // breakout.py usa cummax/cummin.shift(1): la barra corrente non contribuisce mai.
            // L'ordine nascerà per la barra seguente, quindi questo è l'unico punto che evita
            // sia il look-ahead sia un trigger che insegua il massimo appena visto.
            var hasPriorBar = false;
            var currentHigh = decimal.MinValue;
            var currentLow = decimal.MaxValue;
            for (var index = 0; index < data.Length - 1; index++)
            {
                var candidate = data[index];
                if (SessionKey(candidate.DateTime) != currentSession)
                    continue;

                hasPriorBar = true;
                currentHigh = Math.Max(currentHigh, candidate.High);
                currentLow = Math.Min(currentLow, candidate.Low);
            }

            if (hasPriorBar)
            {
                longLevel = Math.Max(longLevel, currentHigh);
                shortLevel = Math.Min(shortLevel, currentLow);
            }
        }

        return longLevel > decimal.MinValue && shortLevel < decimal.MaxValue;
    }

    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        // EngineSignals.single_entry_per_session del Python è un limite sul fill, non
        // sull'emissione dello stop: un ordine non eseguito deve poter essere riemesso.
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = SessionKey(signal.ValidFromUtc!.Value);

        if (IntradayOnly && TimeframeMinutes < 1440)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc.Value, SessionEndTime);

        return signal;
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

    private void ResetLevels(decimal[] ohlc)
    {
        if (Sessions <= 1)
        {
            _hh = ohlc[5];  // highd1
            _ll = ohlc[6];  // lowd1
        }
        else
        {
            _hh = decimal.MinValue;
            _ll = decimal.MaxValue;
            for (var s = 1; s <= Math.Min(Sessions, 5); s++)
            {
                _hh = Math.Max(_hh, ohlc[1 + s * 4]);
                _ll = Math.Min(_ll, ohlc[2 + s * 4]);
            }
        }

        _levelsReady = _hh > decimal.MinValue && _ll < decimal.MaxValue && _hh > 0m && _ll > 0m;
    }

    private void UpdateAdx(decimal[] ohlc)
    {
        if (AdxLength <= 0) return;

        var calc = new[] { _adx0, _adx1, _adx2, _adx3 };
        _adxValue = EasyLib.iADXOnArray(
            AdxLength,
            ohlc[5], ohlc[6], ohlc[7],      // high/low/close della sessione d1
            ohlc[9], ohlc[10], ohlc[11],    // high/low/close della sessione d2
            ref calc) * 100m;

        _adx0 = calc[0];
        _adx1 = calc[1];
        _adx2 = calc[2];
        _adx3 = calc[3];
    }

    private bool InTradingWindow(DateTime barTime)
    {
        // tw() ha fine esclusiva: replicarla è importante perché la variante inclusiva esiste
        // altrove nella stessa libreria e le due differiscono di una barra sul bordo.
        if (!EasyLib.TimeWindow(StartTime, EndTime, barTime))
            return false;

        if (PauseStart < 0 || PauseEnd < 0)
            return true;

        var t = Hhmm(barTime);
        return t < PauseStart || t > PauseEnd;
    }

    private bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        EasyLib.PatternNeutralFast(NeutralYes2, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);
}
