using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore riutilizzabile per RBB_M (Mirrored Reversal Bollinger Bands).
///
/// <para>Traduce il motore Python <c>reversal_bb_mirrored.py</c>: finché il close resta sopra la
/// banda inferiore (long) o sotto quella superiore (short), riemette un ordine limit valido sulla
/// sola barra successiva. Il fill richiede che la barra successiva penetri strettamente la banda.</para>
///
/// <para>I gate direzionali sono speculari: gli stessi numeri di pattern sono valutati con segno
/// negativo per il long e positivo per lo short, perché è un reversal.</para>
/// </summary>
public abstract class RbbMirroredEngine : EasyEngineBase
{
    protected RbbMirroredEngine()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1659;
    }

    /// <summary>Periodo delle Bollinger Bands.</summary>
    protected int BollingerLength = 20;

    /// <summary>Numero di deviazioni standard delle Bollinger Bands.</summary>
    protected decimal BollingerNumDevs = 2m;

    /// <summary>Inizio della finestra operativa HHMM.</summary>
    protected int StartTrade = -1;

    /// <summary>Fine della finestra operativa HHMM, con semantica <c>tw()</c>.</summary>
    protected int EndTrade = -1;

    /// <summary>Giorno EasyLanguage escluso (0 = domenica). -1 = nessuno.</summary>
    protected int DayToFilter = -1;

    /// <summary>Pattern neutro richiesto.</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che inibisce l'operatività.</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto; il segno è applicato per verso.</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che inibisce l'ingresso; il segno è applicato per verso.</summary>
    protected int DirectionalNo = 53;

    /// <summary>Se true chiude la posizione al termine della sessione; default Python = true.</summary>
    protected bool IntradayOnly = true;

    /// <inheritdoc />
    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, Math.Max(2, BollingerLength + 1));

    /// <summary>Valutazione comune, da richiamare dalla sottoclasse concreta.</summary>
    protected TradeSignal EvaluateCore(OhlcvData[] data, DateTime currentDate)
    {
        if (!HasRequiredData(data))
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        if (EasyDayOfWeek(barTime) == DayToFilter ||
            !InTradingWindow(barTime) ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        GetBands(data, 0, out var upperBand, out var lowerBand);
        if (!BandsAreTradable(upperBand, lowerBand))
            return Hold(bar.Close, barTime, "Banda piu' stretta di un tick");

        var entries = new List<TradeSignal>(2);

        if (bar.Close < upperBand &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Sell, upperBand, data, barTime, "SE RBB_M")));
        }

        if (bar.Close > lowerBand &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Buy, lowerBand, data, barTime, "LE RBB_M")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool HasRequiredData(OhlcvData[]? data) =>
        BollingerLength > 0 && data is { Length: >= 2 } && data.Length >= RequiredCandles;

    private bool InTradingWindow(DateTime barTime)
    {
        if (StartTrade < 0 && EndTrade < 0)
            return true;

        // Estremi inclusi, come TF e PC: vedi la nota in TfEngineBase.InTradingWindow.
        return EasyLib.TimeWindowInclusive(Clock, 
            StartTrade < 0 ? 0 : StartTrade,
            EndTrade < 0 ? 2400 : EndTrade,
            barTime);
    }

    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = GetSessionStartUtc(signal.ValidFromUtc!.Value);
        if (IntradayOnly)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc.Value, SessionEndTime);
        return signal;
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStartTime);
        return timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStartTime)
            : sessionStart;
    }


    /// <summary>
    /// Le bande devono distare almeno un tick perche' l'ordine si armi.
    ///
    /// <para>Con deviazione standard nulla — una serie piatta, o una finestra di Bollinger tutta
    /// sullo stesso close — le due bande collassano sulla media e i confronti
    /// <c>close &lt; bandaSuperiore</c> e <c>close &gt; bandaInferiore</c> deciderebbero su un
    /// pareggio: il verso dell'ordine dipenderebbe dall'arrotondamento, non dal mercato. Il
    /// vincolo e' dichiarato dalla ricerca (dossier_ctrader_NQ.md, scheda S13) e vale anche per la
    /// variante unmirrored, che ha lo stesso trigger.</para>
    /// </summary>
    private bool BandsAreTradable(decimal upper, decimal lower) =>
        upper - lower >= InstrumentRegistry.TickSize(Symbol);

    private void GetBands(OhlcvData[] data, int barsAgo, out decimal upper, out decimal lower)
    {
        var end = data.Length - 1 - barsAgo;
        var start = end - BollingerLength + 1;
        decimal sum = 0m;
        for (var index = start; index <= end; index++)
            sum += data[index].Close;

        var average = sum / BollingerLength;
        decimal squaredDifferenceSum = 0m;
        for (var index = start; index <= end; index++)
        {
            var difference = data[index].Close - average;
            squaredDifferenceSum += difference * difference;
        }

        var standardDeviation = (decimal)Math.Sqrt((double)(squaredDifferenceSum / BollingerLength));
        upper = average + BollingerNumDevs * standardDeviation;
        lower = average - BollingerNumDevs * standardDeviation;
    }
}

/// <summary>
/// Motore riutilizzabile per RBB_U (Unmirrored Reversal Bollinger Bands).
///
/// <para>Traduce il motore Python <c>reversal_bb_unmirrored.py</c>. I trigger Bollinger, gli
/// ordini limit next-bar, la finestra oraria e il filtro di calendario sono gli stessi di RBB_M;
/// i quattro gate <see cref="EasyLib.PatternFast"/> restano invece indipendenti per long e short.</para>
/// </summary>
public abstract class RbbUnmirroredEngine : EasyEngineBase
{
    protected RbbUnmirroredEngine()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1659;
    }

    /// <summary>Periodo delle Bollinger Bands.</summary>
    protected int BollingerLength = 20;

    /// <summary>Numero di deviazioni standard delle Bollinger Bands.</summary>
    protected decimal BollingerNumDevs = 2m;

    /// <summary>Inizio della finestra operativa HHMM.</summary>
    protected int StartTrade = -1;

    /// <summary>Fine della finestra operativa HHMM, con semantica <c>tw()</c>.</summary>
    protected int EndTrade = -1;

    /// <summary>Giorno EasyLanguage escluso (0 = domenica). -1 = nessuno.</summary>
    protected int DayToFilter = -1;

    /// <summary>Pattern richiesto per il long.</summary>
    protected int FastYesLong = 152;

    /// <summary>Pattern che inibisce il long.</summary>
    protected int FastNoLong = 153;

    /// <summary>Pattern richiesto per lo short.</summary>
    protected int FastYesShort = 152;

    /// <summary>Pattern che inibisce lo short.</summary>
    protected int FastNoShort = 153;

    /// <summary>Se true chiude la posizione al termine della sessione; default Python = true.</summary>
    protected bool IntradayOnly = true;

    /// <inheritdoc />
    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, Math.Max(2, BollingerLength + 1));

    /// <summary>Valutazione comune, da richiamare dalla sottoclasse concreta.</summary>
    protected TradeSignal EvaluateCore(OhlcvData[] data, DateTime currentDate)
    {
        if (!HasRequiredData(data))
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        if (EasyDayOfWeek(barTime) == DayToFilter ||
            !InTradingWindow(barTime))
        {
            return Hold(bar.Close, barTime);
        }

        GetBands(data, 0, out var upperBand, out var lowerBand);
        if (!BandsAreTradable(upperBand, lowerBand))
            return Hold(bar.Close, barTime, "Banda piu' stretta di un tick");

        var entries = new List<TradeSignal>(2);

        if (bar.Close < upperBand &&
            EasyLib.PatternFast(FastYesShort, ohlc) &&
            !EasyLib.PatternFast(FastNoShort, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Sell, upperBand, data, barTime, "SE RBB_U")));
        }

        if (bar.Close > lowerBand &&
            EasyLib.PatternFast(FastYesLong, ohlc) &&
            !EasyLib.PatternFast(FastNoLong, ohlc))
        {
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Buy, lowerBand, data, barTime, "LE RBB_U")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool HasRequiredData(OhlcvData[]? data) =>
        BollingerLength > 0 && data is { Length: >= 2 } && data.Length >= RequiredCandles;

    private bool InTradingWindow(DateTime barTime)
    {
        if (StartTrade < 0 && EndTrade < 0)
            return true;

        // Estremi inclusi, come TF e PC: vedi la nota in TfEngineBase.InTradingWindow.
        return EasyLib.TimeWindowInclusive(Clock, 
            StartTrade < 0 ? 0 : StartTrade,
            EndTrade < 0 ? 2400 : EndTrade,
            barTime);
    }

    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = GetSessionStartUtc(signal.ValidFromUtc!.Value);
        if (IntradayOnly)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc.Value, SessionEndTime);
        return signal;
    }

    private DateTime GetSessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStartTime);
        return timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStartTime)
            : sessionStart;
    }


    /// <summary>
    /// Le bande devono distare almeno un tick perche' l'ordine si armi.
    ///
    /// <para>Con deviazione standard nulla — una serie piatta, o una finestra di Bollinger tutta
    /// sullo stesso close — le due bande collassano sulla media e i confronti
    /// <c>close &lt; bandaSuperiore</c> e <c>close &gt; bandaInferiore</c> deciderebbero su un
    /// pareggio: il verso dell'ordine dipenderebbe dall'arrotondamento, non dal mercato. Il
    /// vincolo e' dichiarato dalla ricerca (dossier_ctrader_NQ.md, scheda S13) e vale anche per la
    /// variante unmirrored, che ha lo stesso trigger.</para>
    /// </summary>
    private bool BandsAreTradable(decimal upper, decimal lower) =>
        upper - lower >= InstrumentRegistry.TickSize(Symbol);

    private void GetBands(OhlcvData[] data, int barsAgo, out decimal upper, out decimal lower)
    {
        var end = data.Length - 1 - barsAgo;
        var start = end - BollingerLength + 1;
        decimal sum = 0m;
        for (var index = start; index <= end; index++)
            sum += data[index].Close;

        var average = sum / BollingerLength;
        decimal squaredDifferenceSum = 0m;
        for (var index = start; index <= end; index++)
        {
            var difference = data[index].Close - average;
            squaredDifferenceSum += difference * difference;
        }

        var standardDeviation = (decimal)Math.Sqrt((double)(squaredDifferenceSum / BollingerLength));
        upper = average + BollingerNumDevs * standardDeviation;
        lower = average - BollingerNumDevs * standardDeviation;
    }
}
