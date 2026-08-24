using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: long con stop su highd1-10, short a mercato sul cross di lowd1+35.

/// <summary>
/// TOP_UA_956 — media mobile + pattern con ingressi asimmetrici, NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_956_NQ_15__7.txt</c>. Il long usa
/// <c>next bar (highd1-10) stop</c>; lo short entra a mercato sul cross sotto
/// <c>lowd1+35</c>.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.300, target $3.000,
/// max 8 giorni in posizione con chiusura alle 15:00.</para>
/// </summary>
public sealed class Easy_956_NQ_15 : EasyEngineBase
{
    private const int AverageLongLength = 65;
    private const int AverageShortLength = 5;
    private const int ShortCrossOffset = 35;
    private const int FlatTime = 1500;

    private decimal _prevClose;

    public override string Name => "Easy_956_NQ_15";
    public override string Description => "MA + pattern, long stop / short market, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    private int StartTrade { get; set; } = 1100;
    private int EndTrade { get; set; } = 1500;
    private int NeutralYes { get; set; } = 32;
    private int NeutralNo { get; set; } = 45;
    private int FastYesShort { get; set; } = 25;
    private int FastNoShort { get; set; } = 61;

    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, AverageLongLength + 1);

    public Easy_956_NQ_15()
    {
        SessionStartTime = 1700;  // SessionStartTimeA
        SessionEndTime = 1600;    // sessionEndTimeA
        Contracts = 1;

        MaxEntriesPerSession = 1;  // MaxEntriesPerDay
        StopMoney = 1300;          // MyStop
        ProfitMoney = 3000;        // MyProfit
        MaxDaysInTrade = 8;        // MaxDaysInTrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        if (!EasyLib.TimeWindow(StartTrade, EndTrade, barTime) ||
            (MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession))
        {
            _prevClose = bar.Close;
            return Hold(bar.Close, barTime);
        }

        var highD1 = ohlc[5];
        var lowD1 = ohlc[6];
        var avgLong = AverageClose(data, AverageLongLength);
        var avgShort = AverageClose(data, AverageShortLength);
        var entries = new List<TradeSignal>(2);

        if (CurrentMP <= 0 &&
            bar.Close > avgLong &&
            EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
            !EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            entries.Add(WithExitSettings(
                EntryStopNextBar(SignalType.Buy, highD1 - 10m, data, barTime, "LE")));
        }

        var shortTrigger = lowD1 + ShortCrossOffset;
        if (CurrentMP >= 0 &&
            bar.Close < avgShort &&
            _prevClose > shortTrigger && bar.Close <= shortTrigger &&
            EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc))
        {
            entries.Add(WithExitSettings(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE")));
        }

        _prevClose = bar.Close;
        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal WithExitSettings(TradeSignal signal)
    {
        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(signal.ValidFromUtc!.Value);
        }

        if (MaxDaysInTrade > 0)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc!.Value, FlatTime)
                .AddDays(MaxDaysInTrade - 1);

        return signal;
    }

    private static decimal AverageClose(OhlcvData[] data, int length)
    {
        var count = Math.Min(length, data.Length);
        decimal sum = 0m;
        for (var index = data.Length - count; index < data.Length; index++)
            sum += data[index].Close;
        return sum / count;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MyContracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
    }
}
