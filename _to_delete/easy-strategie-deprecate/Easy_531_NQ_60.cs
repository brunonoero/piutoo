using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: ingresso solo all'orario MyTime con stop su H/L della barra corrente.

/// <summary>
/// TOP_UA_531 — ingresso a orologio con stop su estremi barra, NQ 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_531_NQ_60__7.txt</c>. Alle 08:00 UTC
/// arma stop long su <c>high</c> e stop short su <c>low</c> della barra corrente, validi solo
/// sulla barra successiva.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $2.500, target $6.000,
/// uscita forzata dopo 10 giorni alle 14:00.</para>
/// </summary>
public sealed class Easy_531_NQ_60 : EasyEngineBase
{
    private const int ExitTime = 1400; // sessionEndTimeC(1600) - 200

    public override string Name => "Easy_531_NQ_60";
    public override string Description => "Clock-time stop su H/L barra, NQ 60m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 60;

    private int EntryClockTime { get; set; } = 800;
    private int FastYesLong { get; set; } = 142;
    private int FastNoLong { get; set; } = 92;
    private int FastYesShort { get; set; } = 64;
    private int FastNoShort { get; set; } = 87;
    private int NotEntryDayLong { get; set; } = -1;
    private int NotEntryDayShort { get; set; } = -1;
    private int NotEntryMonthLong { get; set; } = -1;
    private int NotEntryMonthShort { get; set; } = -1;

    public Easy_531_NQ_60()
    {
        SessionStartTime = 1700;  // sessionStartTimeC
        SessionEndTime = 1600;    // sessionEndTimeC
        Contracts = 1;

        StopMoney = 2500;    // MyStop
        ProfitMoney = 6000;  // MyProfit
        MaxDaysInTrade = 10; // MaxdaysIntrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        if (Hhmm(barTime) != EntryClockTime || CurrentMP != 0)
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);

        if (EasyDayOfWeek(barTime) != NotEntryDayLong &&
            barTime.Month != NotEntryMonthLong &&
            EasyLib.PatternFast(FastYesLong, ohlc) && !EasyLib.PatternFast(FastNoLong, ohlc))
        {
            entries.Add(WithMaxDaysClose(EntryStopNextBar(SignalType.Buy, bar.High, data, barTime, "LE"), barTime));
        }

        if (EasyDayOfWeek(barTime) != NotEntryDayShort &&
            barTime.Month != NotEntryMonthShort &&
            EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc))
        {
            entries.Add(WithMaxDaysClose(EntryStopNextBar(SignalType.Sell, bar.Low, data, barTime, "SE"), barTime));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal WithMaxDaysClose(TradeSignal signal, DateTime barTime)
    {
        if (MaxDaysInTrade > 0)
            signal.CloseAtUtc = ResolveCloseAtUtc(barTime.AddDays(MaxDaysInTrade - 1), ExitTime);
        return signal;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("Mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
        if (parameters.TryGetValue("MaxdaysIntrade", out var maxDays))
            MaxDaysInTrade = Convert.ToInt32(maxDays);
    }
}
