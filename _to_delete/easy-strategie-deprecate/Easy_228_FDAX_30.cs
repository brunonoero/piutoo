using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: gap + recross su highd0/lowd0 con gate PatternFast estesi; non mappabile su LevelFader
// (livelli d1) senza perdere la semantica del gap intraday.

/// <summary>
/// TOP_UA_228 — mean reversion su gap con recross degli estremi d0, FDAX 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_228_FDAX_30__7.txt</c>. Dopo un gap
/// ribassista/rialzista entra al recross di <c>highd0</c>/<c>lowd0</c> a mercato sulla barra
/// successiva.</para>
///
/// <para><b>Contratto di riferimento:</b> FDAX, €25 per punto. Stop long €1.800 / short €1.600,
/// target €6.500, breakeven long €1.500 / short €2.000, max 1 giorno in posizione.</para>
/// </summary>
public sealed class Easy_228_FDAX_30 : EasyEngineBase
{
    private bool _gapLong;
    private bool _gapShort;
    private decimal _longTrigger;
    private decimal _shortTrigger;

    public override string Name => "Easy_228_FDAX_30";
    public override string Description => "Gap reversal con recross estremi d0, FDAX 30m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 30;

    private int BaseYesLong { get; set; } = 41;
    private int BaseNoLong { get; set; } = 42;
    private int BaseYesShort { get; set; } = 41;
    private int BaseNoShort { get; set; } = 42;
    private int FastYesLong { get; set; } = 26;
    private int FastNoLong { get; set; } = 43;
    private int FastYesShort { get; set; } = 152;
    private int FastNoShort { get; set; } = 153;
    private int StartTrade { get; set; } = 800;
    private int EndTrade { get; set; } = 1430;
    private int PauseStart { get; set; } = 1200;
    private int PauseEnd { get; set; } = 1100;
    private int SkipSessionLong { get; set; } = -1;
    private int SkipSessionShort { get; set; } = 5;

    public Easy_228_FDAX_30()
    {
        SessionStartTime = 800;   // sessionStartTimeA
        SessionEndTime = 2200;    // sessionEndTimeA
        Contracts = 1;

        MaxEntriesPerSession = 1;  // MaxTradesPerDay
        MaxDaysInTrade = 1;        // MaxDaysLong / MaxDaysShort

        StopMoney = 1800;       // max(MyStopL, MyStopS)
        ProfitMoney = 6500;     // MyProfitL / MyProfitS
        BreakEvenMoney = 2000;  // max(MyBreakevenL, MyBreakevenS)
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
        {
            var openD0 = ohlc[0];
            var highD1 = ohlc[5];
            var lowD1 = ohlc[6];
            _gapLong = openD0 < lowD1;
            _gapShort = openD0 > highD1;
            _longTrigger = ohlc[1];
            _shortTrigger = ohlc[2];
        }

        if (!InTradingWindow(barTime))
            return Hold(bar.Close, barTime);

        if (MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession)
            return Hold(bar.Close, barTime);

        var previousClose = data[^2].Close;
        var sow = EasyDayOfWeek(barTime);
        var entries = new List<TradeSignal>(2);

        var allowLong = CurrentMP != 1;
        var allowShort = CurrentMP != -1;

        if (allowLong && _gapLong &&
            EasyLib.UAPtnBase(BaseYesLong, ohlc) && !EasyLib.UAPtnBase(BaseNoLong, ohlc) &&
            EasyLib.PatternFast(FastYesLong, ohlc) && !EasyLib.PatternFast(FastNoLong, ohlc) &&
            sow != SkipSessionLong &&
            previousClose < _longTrigger && bar.Close > _longTrigger)
        {
            entries.Add(WithSessionSettings(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE")));
        }

        if (allowShort && _gapShort &&
            EasyLib.UAPtnBase(BaseYesShort, ohlc) && !EasyLib.UAPtnBase(BaseNoShort, ohlc) &&
            EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc) &&
            sow != SkipSessionShort &&
            previousClose > _shortTrigger && bar.Close < _shortTrigger)
        {
            entries.Add(WithSessionSettings(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool InTradingWindow(DateTime barTime)
    {
        var time = Hhmm(barTime);
        if (!EasyLib.TimeWindow(StartTrade, EndTrade, barTime))
            return false;

        return time < PauseStart || time > PauseEnd;
    }

    private TradeSignal WithSessionSettings(TradeSignal signal)
    {
        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(signal.ValidFromUtc!.Value);
        }

        if (MaxDaysInTrade > 0)
            signal.CloseAtUtc = signal.ValidFromUtc!.Value.Date.AddDays(MaxDaysInTrade);

        return signal;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
    }
}
