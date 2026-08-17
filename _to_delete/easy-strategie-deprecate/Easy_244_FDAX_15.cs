using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_244 — BIAS overnight a mercato su FDAX a 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_244_FDAX_15__7.txt</c>. I filtri
/// <c>Massimocondition</c>, <c>Andreacondition</c> e <c>Carloscondition</c> sono mantenuti
/// esplicitamente nell'hook BIAS, così come l'ingresso alle 17:45 e la deadline alle 09:00.</para>
/// </summary>
public sealed class Easy_244_FDAX_15 : BiasBarCountEngine
{
    // Input della sorgente. MyStop è in punti e viene convertito una sola volta nel denaro
    // richiesto da setstopcontract: 120 punti × $25/punto FDAX = $3.000 per contratto.
    private const int MyStopPoints = 120;
    private const int MyPtnLN = 42;
    private const int MyEntryHour = 1745;
    private const int MyExitHour = 900;
    private const decimal Amt = 0.4m;

    public Easy_244_FDAX_15()
    {
        // Il sorgente usa highd/lowd/closed e PtnBase sul grafico exchange-time; qui la
        // ricostruzione giornaliera è quindi calendariale, non la sessione overnight GC.
        SessionStartTime = 0;
        SessionEndTime = 2359;
        Contracts = 1;
        PatternLibrary = EasyPatternLibrary.BaseSA;
        PatternLongNo = MyPtnLN;
        StopMoney = MyStopPoints * 25;
    }

    public override string Name => "TOP_UA_244";
    public override string Description => "BIAS overnight + daily filters, FDAX 15m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 15;

    protected override bool UsesCustomEntryRules => true;

    protected override void AddCustomEntries(
        OhlcvData[] data,
        DateTime barTime,
        DateTime nextBarTime,
        decimal[] ohlc,
        List<TradeSignal> entries)
    {
        if (Hhmm(barTime) != MyEntryHour)
            return;

        var close = data[^1].Close;
        var highD1 = EasyLib.GetDailyHigh(data, barTime, 1);
        var lowD1 = EasyLib.GetDailyLow(data, barTime, 1);
        var closeD1 = EasyLib.GetDailyClose(data, barTime, 1);

        var massimoCondition = close < highD1 && close > lowD1;
        var andreaCondition = close > closeD1 - closeD1 * Amt / 100m;
        var carlosCondition = !Pattern(MyPtnLN, ohlc);
        if (massimoCondition && andreaCondition && carlosCondition && EasyDayOfWeek(barTime) != 5)
            entries.Add(WithExitTime(
                EntryMarketNextBar(SignalType.Buy, close, data, barTime, "LE_Overnight"),
                barTime,
                MyExitHour));
    }
}

