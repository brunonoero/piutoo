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

    /// <summary>Inizio della finestra operativa, nell'orologio di sessione. <c>null</c> = da inizio giornata.</summary>
    protected TimeOnly? StartTime;

    /// <summary>Fine della finestra operativa. <c>null</c> = fino a fine giornata.</summary>
    protected TimeOnly? EndTime;

    /// <summary>True per estremi inclusivi; false per la semantica <c>tw()</c> con fine esclusiva.</summary>
    protected bool TradingWindowInclusive = true;

    /// <summary>Inizio pausa intraday. <c>null</c> = nessuna pausa.</summary>
    protected TimeOnly? PauseStart;

    /// <summary>Fine pausa intraday. <c>null</c> = nessuna pausa.</summary>
    protected TimeOnly? PauseEnd;

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

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

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
    protected TimeOnly? MaxDaysFlatTime;

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
        // Il gate di posizione non ha corrispondente nel motore Python, che emette la maschera su
        // tutta la serie e lascia decidere al simulatore. Non e' pero' ridondante rispetto al blocco
        // dell'engine (compare-0041, che agisce al FILL): dentro un tick le uscite precedono i fill,
        // quindi senza questo gate l'ordine emesso mentre si era in posizione si riempirebbe nel
        // tick stesso in cui lo stop la chiude — una ripartenza immediata al livello del canale.
        // Misurato il 20/09/2026 su PT2_NQ_PCH_001_240, feed interno 2022-01-24 → 2025-05-31:
        // toglierlo porta da 861 trade e $231.822 a 890 e $136.295, cioe' 29 trade che valgono
        // -$95.527. La lista di riferimento della ricerca ha piu' trade *e* piu' profitto (1023 e
        // $507.868): il divario residuo non e' qui, e queste ripartenze non ne fanno parte.
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
            var entry = WithPythonSettings(
                EntryStopNextBar(SignalType.Buy, HighestChannelHigh(data) + offset, data, barTime, "PC_LE"));
            if (entry is not null)
                entries.Add(entry);
        }

        if (Direction != 1 &&
            PassesDirectionalGates(-1, ohlc))
        {
            var entry = WithPythonSettings(
                EntryStopNextBar(SignalType.Sell, LowestChannelLow(data) - offset, data, barTime, "PC_SE"));
            if (entry is not null)
                entries.Add(entry);
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

    /// <summary>
    /// Restituisce <c>null</c> quando l'ingresso nascerebbe <b>dopo</b> la propria ora di chiusura:
    /// con <see cref="EasyEngineBase.SessionExitTime"/> valorizzato, la finestra utile della
    /// sessione finisce li', e un ordine che non potrebbe stare aperto nemmeno un minuto non e' un
    /// ordine. Senza questo scarto la deadline slitterebbe alla sessione dopo e la strategia
    /// diventerebbe overnight proprio dove ha dichiarato di non esserlo.
    /// </summary>
    private TradeSignal? WithPythonSettings(TradeSignal signal)
    {
        signal.TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null;
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = SessionKey(signal.ValidFromUtc!.Value);

        if (AppliesSessionExit)
        {
            // SessionEnd resta il default: e' il comportamento del motore di ricerca, e senza un
            // orario proprio nulla cambia rispetto a prima.
            var exitTime = SessionExitTime ?? SessionEnd;
            var closeAt = ResolveCloseAtUtc(
                signal.ValidFromUtc.Value, exitTime, rollToNextSession: SessionExitTime is null);

            if (closeAt is null)
                return null;

            signal.CloseAtUtc = closeAt;
        }

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

        if (MaxDaysInTrade > 0 && MaxDaysFlatTime is not null)
            signal.CloseAtUtc = ResolveMaxDaysCloseAt(signal.ValidFromUtc!.Value);

        return signal;
    }

    private bool InTradingWindow(DateTime barTime)
    {
        var time = ParamTime(barTime);
        return InWindow(StartTime, EndTime, time, TradingWindowInclusive) && !InPause(PauseStart, PauseEnd, time);
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

        // Python: session_atr(df, 14, shift=1). Lo stesso ATR delle sessioni chiuse che dal
        // 22/09/2026 serve anche allo stop in ATR (EasyEngineBase.StopAtrMultiplier): il punto
        // valore trasforma i punti nel valore monetario richiesto da dvol_min.
        var atr = ClosedSessionAtrPoints(data, barTime);
        return atr.HasValue && atr.Value * InstrumentRegistry.PointValue(Symbol) >= DvolMin;
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
        var flatTime = MaxDaysFlatTime ?? TimeOnly.MinValue;
        var target = Clock.SessionInstantUtc(
            entryValidFrom.AddDays(Math.Max(0, MaxDaysInTrade - 1)), flatTime);
        return target > entryValidFrom
            ? target
            : Clock.SessionInstantUtc(entryValidFrom.AddDays(Math.Max(1, MaxDaysInTrade)), flatTime);
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStart);
        return timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStart)
            : sessionStart;
    }

    private bool InPythonTradingWindow(DateTime barTime)
    {
        if (InDeclaredWindow(barTime) is { } declared)
            return declared;

        // Il motore Python confronta l'orario completo con gli estremi "HH:00", fine inclusa.
        // Confrontare le sole ore allargava la finestra fino a HH:59: con end_hour = 4 entravano
        // anche le barre 04:15–04:45, che nella fonte non producono segnali.
        return InWindow(StartTime, EndTime, ParamTime(barTime), inclusiveEnd: true);
    }

    private int PythonDayOfWeek(DateTime instantUtc) => PythonWeekday(instantUtc);

    private DateTime SessionKey(DateTime time)
    {
        var start = Clock.SessionInstantUtc(time, SessionStart);
        return SessionStart > SessionEnd && time < start
            ? Clock.SessionInstantUtc(time.AddDays(-1), SessionStart)
            : start;
    }
}
