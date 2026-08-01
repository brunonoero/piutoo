using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_872 — BIAS con finestre orarie separate e breakout di sessione, su CL a 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_872_CL_15__7.txt</c>. Le finestre e le
/// pause restano esplicite nell'hook BIAS: i confronti stretti sono intenzionali e
/// <c>MyStartLPause &gt; MyEndLPause</c> è conservato esattamente come nella sorgente.</para>
/// </summary>
public sealed class Easy_872_CL_15 : BiasBarCountEngine
{
    // Input della sorgente.
    private const int MyStartLETrade = 1045;
    private const int MyEndLETrade = 1430;
    private const int MyStartLPause = 1200;
    private const int MyEndLPause = 1100;
    private const int MyLXTime = 1645;
    private const int MyStartSETrade = 445;
    private const int MyEndSETrade = 700;
    private const int MyStartSPause = 1200;
    private const int MyEndSPause = 1100;
    private const int MySXTime = 200;
    private const int MyPtnLY = 25;
    private const int MyPtnSY = 39;
    private const int MyPtnLN = 8;
    private const int MyPtnSN = 7;
    private const int MyNotLEDay = 0;
    private const int MyNotSEDay = 0;

    public Easy_872_CL_15()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;
        PatternLibrary = EasyPatternLibrary.BaseSA;
        StopMoney = 1500;
        ProfitMoney = 0;
    }

    public override string Name => "TOP_UA_872";
    public override string Description => "BIAS session breakout con finestre separate, CL 15m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 15;

    protected override bool UsesCustomEntryRules => true;

    protected override void AddCustomEntries(
        OhlcvData[] data,
        DateTime barTime,
        DateTime nextBarTime,
        decimal[] ohlc,
        List<TradeSignal> entries)
    {
        // La sorgente dichiara MyNotLEDay/MyNotSEDay ma non li usa nella condizione di ingresso:
        // non applicarli qui è parte della parità, non un'omissione.
        if (InSourceWindow(Hhmm(barTime), MyStartLETrade, MyEndLETrade) &&
            OutsideSourcePause(Hhmm(barTime), MyStartLPause, MyEndLPause) &&
            Pattern(MyPtnLY, ohlc) && !Pattern(MyPtnLN, ohlc))
        {
            entries.Add(WithExitTime(
                EntryStopNextBar(SignalType.Buy, ohlc[1], data, barTime, "LE_STP"),
                barTime,
                MyLXTime));
        }

        if (InSourceWindow(Hhmm(barTime), MyStartSETrade, MyEndSETrade) &&
            OutsideSourcePause(Hhmm(barTime), MyStartSPause, MyEndSPause) &&
            Pattern(MyPtnSY, ohlc) && !Pattern(MyPtnSN, ohlc))
        {
            entries.Add(WithExitTime(
                EntryStopNextBar(SignalType.Sell, ohlc[2], data, barTime, "SE_STP"),
                barTime,
                MySXTime));
        }
    }

    private static bool InSourceWindow(int time, int start, int end) =>
        start > end ? time > start || time < end : time > start && time < end;

    private static bool OutsideSourcePause(int time, int start, int end) =>
        time < start || time > end;
}

