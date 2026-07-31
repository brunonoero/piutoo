using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore riutilizzabile per i BIAS settimanali a ingresso market programmato.
///
/// <para>Segue <c>easy_engine_py/bias_weekly.py</c>: ogni verso ha un solo ingresso
/// programmato (giorno/orario Python, con lunedì = 0), eseguito all'apertura di quella
/// barra. Il filtro Fast è quindi valutato sulla barra precedente. L'uscita viene allegata
/// al segnale d'ingresso come <see cref="TradeSignal.CloseAtUtc"/> e non dipende da un
/// futuro segnale <c>LX</c>/<c>SX</c>.</para>
///
/// <para>I soli gate sono i <c>PatternFast</c> indipendenti long/short, come nel motore
/// Python: il pattern <c>yes</c> deve essere vero e quello <c>no</c> deve essere falso.</para>
/// </summary>
public abstract class BiasWeeklyEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ abilitazione e calendario

    protected bool EnableLong = true;
    protected bool EnableShort = true;

    /// <summary>Giorno Python (0 = lunedì) dell'ingresso; -1 disabilita il verso.</summary>
    protected int EntryDayLong = -1;
    protected int EntryDayShort = -1;

    /// <summary>Orario HHMM del singolo ingresso long/short programmato.</summary>
    protected int EntryTimeLong;
    protected int EntryTimeShort;

    // ------------------------------------------------------------------ gate Fast

    /// <summary>Gate <c>ptn_ly_yes</c>/<c>ptn_ly_no</c> del motore Python.</summary>
    protected int FastYesLong = 152;
    protected int FastNoLong = 153;

    /// <summary>Gate <c>ptn_sy_yes</c>/<c>ptn_sy_no</c> del motore Python.</summary>
    protected int FastYesShort = 152;
    protected int FastNoShort = 153;

    // ------------------------------------------------------------------ calendario di uscita

    /// <summary>Giorno/orario Python (0 = lunedì) dell'uscita long; -1 disabilita la deadline.</summary>
    protected int ExitDayLong = -1;
    protected int ExitTimeLong;

    /// <summary>Giorno/orario Python (0 = lunedì) dell'uscita short; -1 disabilita la deadline.</summary>
    protected int ExitDayShort = -1;
    protected int ExitTimeShort;

    // ------------------------------------------------------------------ uscite monetarie, per verso

    protected decimal StopMoneyLong;
    protected decimal StopMoneyShort;
    protected decimal ProfitMoneyLong;
    protected decimal ProfitMoneyShort;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        // BIASW entra all'open della barra pianificata. Il Fast deve quindi essere noto alla
        // sua apertura: come lo shift(1) del Python, usiamo soltanto la barra precedente.
        var previousData = data[..^1];
        BuildSessionOhlc(previousData, previousData[^1].DateTime, out var ohlc);

        var entries = new List<TradeSignal>(2);
        if (CanEnterLong(barTime, ohlc))
            entries.Add(BuildEntry(SignalType.Buy, bar, barTime));

        if (CanEnterShort(barTime, ohlc))
            entries.Add(BuildEntry(SignalType.Sell, bar, barTime));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool CanEnterLong(DateTime barTime, decimal[] ohlc) =>
        EnableLong &&
        IsAtScheduledEntry(barTime, EntryDayLong, EntryTimeLong) &&
        PassesFastGates(FastYesLong, FastNoLong, ohlc);

    private bool CanEnterShort(DateTime barTime, decimal[] ohlc) =>
        EnableShort &&
        IsAtScheduledEntry(barTime, EntryDayShort, EntryTimeShort) &&
        PassesFastGates(FastYesShort, FastNoShort, ohlc);

    private TradeSignal BuildEntry(
        SignalType side, OhlcvData bar, DateTime barTime)
    {
        var signal = new TradeSignal
        {
            Date = barTime,
            Type = side,
            Price = bar.Open,
            StrategyName = Name,
            Quantity = Contracts,
            OrderType = TradeOrderType.Market,
            ValidFromUtc = barTime,
            ExpiresAtUtc = barTime,
            Reason = side == SignalType.Buy ? "LE_BIASW" : "SE_BIASW"
        };

        var isLong = side == SignalType.Buy;
        signal.StopLossMoneyPerFutureContract = ValueForSide(StopMoneyLong, StopMoneyShort, isLong);
        signal.TakeProfitMoneyPerFutureContract = ValueForSide(ProfitMoneyLong, ProfitMoneyShort, isLong);

        var exitDay = isLong ? ExitDayLong : ExitDayShort;
        if (exitDay >= 0)
            signal.CloseAtUtc = ResolveScheduledExitUtc(barTime, exitDay, isLong ? ExitTimeLong : ExitTimeShort);

        return signal;
    }

    private static decimal? ValueForSide(decimal longValue, decimal shortValue, bool isLong)
    {
        var value = isLong ? longValue : shortValue;
        return value > 0m ? value : null;
    }

    private static bool PassesFastGates(int yes, int no, decimal[] ohlc) =>
        EasyLib.PatternFast(yes, ohlc) &&
        !EasyLib.PatternFast(no, ohlc);

    private static bool IsAtScheduledEntry(DateTime barTime, int day, int time)
    {
        return day >= 0 &&
               PythonDayOfWeek(barTime) == day &&
               Hhmm(barTime) == time;
    }

    /// <summary>
    /// Trova la prima occorrenza dell'orario di uscita nel giorno Python richiesto, fino a sette
    /// giorni dopo l'ingresso. Gestisce sia le uscite nella stessa settimana sia quelle della
    /// settimana successiva (per esempio venerdì → lunedì).
    /// </summary>
    protected static DateTime ResolveScheduledExitUtc(DateTime entryBarTime, int exitDay, int exitTime)
    {
        for (var offset = 0; offset <= 7; offset++)
        {
            var date = entryBarTime.Date.AddDays(offset);
            if (PythonDayOfWeek(date) != exitDay)
                continue;

            var candidate = EasyLib.CombineDateAndHhmm(date, exitTime);
            if (candidate > entryBarTime)
                return candidate;
        }

        throw new InvalidOperationException("Impossibile risolvere la deadline BIASW.");
    }

    /// <summary>Convenzione del motore Python: lunedì = 0 … domenica = 6.</summary>
    private static int PythonDayOfWeek(DateTime time) => ((int)time.DayOfWeek + 6) % 7;
}
