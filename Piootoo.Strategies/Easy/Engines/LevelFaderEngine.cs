using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>Famiglia di livelli usata dal Level Fader.</summary>
public enum LevelFaderLevel
{
    /// <summary>Supporto S1 e resistenza R1 calcolati dalla sessione d1.</summary>
    PreviousSessionPivot = 1,

    /// <summary>Minimo e massimo della sessione d1.</summary>
    PreviousSessionExtremes = 2
}

/// <summary>
/// Motore riutilizzabile per il "Level Fader": dopo una falsa rottura di un livello della
/// sessione precedente entra al recross del close, a mercato sulla barra successiva.
///
/// <para>Replica <c>s__UA_LevelFader__7.txt</c>. All'apertura della sessione fissa i livelli
/// d1; non li aggiorna con la sessione corrente. Il long richiede che il close precedente sia
/// sotto il supporto e quello corrente lo recuperi, mentre lo short richiede l'analogo rientro
/// sotto la resistenza.</para>
///
/// <para>Quando <see cref="StrategyId"/> è zero, la chiusura di fine sessione dell'originale è
/// dichiarata sull'ingresso tramite <see cref="TradeSignal.CloseAtUtc"/>. Questo conserva il
/// comportamento anche nel broker esterno, dove i segnali di sola chiusura non sono emessi.</para>
/// </summary>
public abstract class LevelFaderEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ livelli

    /// <summary>Selezione dei livelli d1 (<c>LevelChoice</c>).</summary>
    protected LevelFaderLevel LevelChoice = LevelFaderLevel.PreviousSessionPivot;

    /// <summary>Scostamento dai livelli, espresso in tick (<c>LevelShift</c>).</summary>
    protected decimal LevelShift;

    /// <summary>Dimensione del tick dello strumento (<c>MyTick</c>).</summary>
    protected decimal TickSize;

    // ------------------------------------------------------------------ finestra operativa

    /// <summary>
    /// Inizio della finestra operativa nell'orologio della finestra; il confronto è sull'ora
    /// piena (0–23), come <c>start_hour</c> Python. <c>null</c> disabilita il limite.
    /// </summary>
    protected TimeOnly? StartTrade;

    /// <summary>
    /// Fine della finestra operativa, confrontata sull'ora piena (0–23). <c>null</c> disabilita il
    /// limite; gli estremi attivi sono inclusivi, come <c>time_window</c> del port Python.
    /// </summary>
    protected TimeOnly? EndTrade;

    /// <summary>
    /// Usa la finestra di test della sorgente: due ore dopo l'apertura e due ore prima della
    /// chiusura di sessione (<c>sesstest = 1</c>).
    /// </summary>
    protected bool UseSessionTestWindow;

    // ------------------------------------------------------------------ gate di pattern e calendario

    /// <summary>Pattern neutro richiesto (<c>PtnNeutYes</c>).</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che impedisce l'operatività (<c>PtnNeutNo</c>).</summary>
    protected int NeutralNo = 56;

    /// <summary>Gate UAPtnBase richiesto per il long (<c>PtnLY</c>).</summary>
    protected int BaseYesLong = 41;

    /// <summary>Gate UAPtnBase che impedisce il long (<c>PtnLN</c>).</summary>
    protected int BaseNoLong = 42;

    /// <summary>Gate UAPtnBase richiesto per lo short (<c>PtnSY</c>).</summary>
    protected int BaseYesShort = 41;

    /// <summary>Gate UAPtnBase che impedisce lo short (<c>PtnSN</c>).</summary>
    protected int BaseNoShort = 42;

    /// <summary>
    /// Pattern direzionale richiesto; il long usa il segno positivo e lo short il negativo
    /// (<c>PtnDirYes</c>).
    /// </summary>
    protected int DirectionalYes = 52;

    /// <summary>Giorno Python/pandas escluso per il long, 0 = lunedì. -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno Python/pandas escluso per lo short, 0 = lunedì. -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    // ------------------------------------------------------------------ chiusura

    /// <summary>
    /// Identificatore della strategia (<c>ID</c>). Solo zero abilita la chiusura a tempo della
    /// sorgente.
    /// </summary>
    protected int StrategyId;

    /// <summary>
    /// Orario di chiusura. <c>null</c> usa la fine della sessione effettiva, come
    /// <c>UACalcEndTime(sessionStartTimeA, endsession)</c>.
    /// </summary>
    protected TimeOnly? CloseAtTime;

    // ------------------------------------------------------------------ stato di sessione

    private decimal _longTrigger;
    private decimal _shortTrigger;
    private bool _levelsReady;

    /// <summary>Valuta il recross dei livelli e costruisce gli ingressi next-bar a mercato.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);
        // Il DataFrame Python espone H_d1/L_d1/C_d1 su ogni riga della sessione.
        // Ricalcolarlo qui rende la stessa proprietà anche quando la valutazione C#
        // inizia a sessione già aperta, senza dipendere da uno stato precedente.
        SetLevels(ohlc);

        if (!_levelsReady)
            return Hold(bar.Close, barTime, "Livelli d1 non disponibili");

        if (!InTradingWindow(barTime) ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var previousClose = data[^2].Close;
        var entries = new List<TradeSignal>(2);

        if (PythonDayOfWeek(barTime) != NotEntryDayLong &&
            EasyLib.UAPtnBase(BaseYesLong, ohlc) &&
            !EasyLib.UAPtnBase(BaseNoLong, ohlc) &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            previousClose < _longTrigger &&
            bar.Close > _longTrigger)
        {
            entries.Add(WithCloseTime(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE")));
        }

        if (PythonDayOfWeek(barTime) != NotEntryDayShort &&
            EasyLib.UAPtnBase(BaseYesShort, ohlc) &&
            !EasyLib.UAPtnBase(BaseNoShort, ohlc) &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            previousClose > _shortTrigger &&
            bar.Close < _shortTrigger)
        {
            entries.Add(WithCloseTime(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private void SetLevels(decimal[] ohlc)
    {
        var high = ohlc[5];
        var low = ohlc[6];
        var close = ohlc[7];
        if (high <= 0m || low <= 0m || close <= 0m || high < low)
        {
            _levelsReady = false;
            return;
        }

        var shift = LevelShift * TickSize;
        if (LevelChoice == LevelFaderLevel.PreviousSessionPivot)
        {
            var pivot = (high + low + close) / 3m;
            _shortTrigger = 2m * pivot - low + shift;   // R1
            _longTrigger = 2m * pivot - high - shift;   // S1
        }
        else
        {
            _shortTrigger = high + shift;
            _longTrigger = low - shift;
        }

        _levelsReady = true;
    }

    private bool InTradingWindow(DateTime barTime)
    {
        if (!UseSessionTestWindow)
            return InPythonTradingWindow(barTime);

        var start = SessionStart.AddHours(2);
        var end = EffectiveSessionEndTime.AddHours(-2);
        return EasyLib.TimeWindow(start, end, ParamTime(barTime));
    }

    private bool InPythonTradingWindow(DateTime barTime)
    {
        if (StartTrade is null && EndTrade is null)
            return true;

        // Il port Python confronta le ORE piene, estremi inclusi: `hour >= start && hour <= end`.
        // In orari pieni e' la finestra da start:00 all'ultimo istante dell'ora di end, letta
        // sull'orologio della finestra, mai su quello grezzo della barra.
        var start = StartTrade ?? TimeOnly.MinValue;
        var end = (EndTrade ?? new TimeOnly(23, 0))
            .Add(TimeSpan.FromHours(1))
            .Add(TimeSpan.FromTicks(-1));
        return EasyLib.TimeWindowInclusive(start, end, WindowParamTime(barTime));
    }

    private int PythonDayOfWeek(DateTime value) => PythonWeekday(value);

    private TradeSignal WithCloseTime(TradeSignal signal)
    {
        if (StrategyId == 0)
        {
            var closeTime = CloseAtTime ?? EffectiveSessionEndTime;
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc!.Value, closeTime);
        }

        return signal;
    }

    /// <summary>
    /// La fine di sessione "vera": su una sessione a giornata piena è l'ancoraggio stesso, cioè
    /// l'istante in cui riapre la successiva (<c>UACalcEndTime</c> della sorgente).
    /// </summary>
    private TimeOnly EffectiveSessionEndTime =>
        SessionEnd == ZonedWindow.EndOfDay ? SessionStart : SessionEnd;
}
