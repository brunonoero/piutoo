using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Base comune delle varianti TF: breakout stop sugli estremi della sessione precedente.
/// Traduce <c>easy_engine_py/tf_mirrored.py</c> e <c>tf_unmirrored.py</c>: OHLC d1,
/// finestra oraria Python, filtro giorno, un fill per lato/sessione e session exit.
/// </summary>
public abstract class TfEngineBase : EasyEngineBase
{
    /// <summary>
    /// Inizio della finestra operativa in ore UTC (<c>start_hour</c> Python).
    /// <c>-1</c> disabilita il relativo limite.
    /// </summary>
    protected int StartHour = -1;

    /// <summary>
    /// Fine esclusiva della finestra operativa in ore UTC (<c>end_hour</c> Python).
    /// <c>-1</c> disabilita il relativo limite.
    /// </summary>
    protected int EndHour = -1;

    /// <summary>Giorno Python da escludere: 0 = lunedì … 4 = venerdì; -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>
    /// Se <c>1</c>, l'eventuale posizione viene chiusa alla fine della sessione.
    /// Su D1 il motore Python non applica questa uscita.
    /// </summary>
    protected bool IntradayOnly = true;

    protected TfEngineBase()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1659;
    }

    /// <summary>Valutazione comune; i gate sono definiti dalla variante mirrored/unmirrored.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        // Gli zeri sono sentinelle di OHLCMulti5 quando d1 non è interamente disponibile.
        var highD1 = ohlc[5];
        var lowD1 = ohlc[6];
        if (highD1 <= 0m || lowD1 <= 0m || highD1 < lowD1)
            return Hold(bar.Close, barTime, "Livelli d1 non disponibili");

        if (!InTradingWindow(barTime) || IsSkippedPythonWeekday(barTime))
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);
        if (PassesLongGates(ohlc))
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Buy, highD1, data, barTime, "LE TF")));

        if (PassesShortGates(ohlc))
            entries.Add(WithPythonSettings(
                EntryStopNextBar(SignalType.Sell, lowD1, data, barTime, "SE TF")));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>Gate specifico del long.</summary>
    protected abstract bool PassesLongGates(decimal[] ohlc);

    /// <summary>Gate specifico dello short.</summary>
    protected abstract bool PassesShortGates(decimal[] ohlc);

    private bool InTradingWindow(DateTime barTime)
    {
        if (StartHour < 0 && EndHour < 0)
            return true;

        // Estremi INCLUSI, su HHMM pieni. Prima si usava EasyLib.TimeWindow, che ha la fine
        // esclusiva come tw(): la barra di segnale a esattamente end_hour:00 veniva scartata e con
        // essa i suoi ingressi. La semantica giusta e' misurata sui trade di riferimento della
        // ricerca — su 15m con finestra 17-10 esiste un ingresso alle 10:15, cioe' un segnale alle
        // 10:00; su 30m con finestra 09-19 un ingresso alle 19:30, cioe' un segnale alle 19:00 —
        // e coincide con quella gia' adottata da PriceChannelEngine.
        return EasyLib.TimeWindowInclusive(Clock, 
            StartHour < 0 ? 0 : StartHour * 100,
            EndHour < 0 ? 2400 : EndHour * 100,
            barTime);
    }

    private bool IsSkippedPythonWeekday(DateTime barTime) =>
        SkipDay >= 0 && ((int)Clock.SessionDay(barTime).DayOfWeek + 6) % 7 == SkipDay;

    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        // Python impone una sola entrata eseguita per lato/sessione. Il limite va
        // dichiarato sul segnale, non dedotto dal contatore giornaliero locale:
        // uno stop non riempito deve infatti poter essere riemesso alla barra dopo.
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = GetSessionStartUtc(signal.ValidFromUtc!.Value);

        if (IntradayOnly && TimeframeMinutes < 1440)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc!.Value, SessionEndTime);

        return signal;
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStartTime);
        return timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStartTime)
            : sessionStart;
    }
}

/// <summary>
/// Motore riutilizzabile per <c>s__UA_Mirrored_TF__7.txt</c>.
/// I gate neutri sono comuni; quelli direzionali sono speculari, con segno positivo per il long
/// e negativo per lo short.
/// </summary>
public abstract class TfMirroredEngine : TfEngineBase
{
    /// <summary>Pattern neutro richiesto (<c>PtnNeutYes</c>).</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che inibisce l'operatività (<c>PtnNeutNo</c>).</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto (<c>PtnDirYes</c>).</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che inibisce l'ingresso (<c>PtnDirNo</c>).</summary>
    protected int DirectionalNo = 53;

    protected override bool PassesLongGates(decimal[] ohlc) =>
        PassesNeutralGates(ohlc) &&
        EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        PassesNeutralGates(ohlc) &&
        EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc);

    private bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);
}

/// <summary>
/// Motore riutilizzabile per <c>s__UA_Unmirrored_TF__7.txt</c>.
/// I quattro gate <see cref="EasyLib.PatternFast"/> sono indipendenti per long e short.
/// </summary>
public abstract class TfUnmirroredEngine : TfEngineBase
{
    /// <summary>Pattern richiesto per il long (<c>MyPtnLY</c>).</summary>
    protected int FastYesLong = 152;

    /// <summary>Pattern che inibisce il long (<c>MyPtnLN</c>).</summary>
    protected int FastNoLong = 153;

    /// <summary>Pattern richiesto per lo short (<c>MyPtnSY</c>).</summary>
    protected int FastYesShort = 152;

    /// <summary>Pattern che inibisce lo short (<c>MyPtnSN</c>).</summary>
    protected int FastNoShort = 153;

    protected override bool PassesLongGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesLong, ohlc) &&
        !EasyLib.PatternFast(FastNoLong, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesShort, ohlc) &&
        !EasyLib.PatternFast(FastNoShort, ohlc);
}
