using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_303 — Trend Developer su rottura highd1/lowd1 con gate ADX, GC 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_303_GC_15____1440__7.txt</c>.
/// <c>ID = 1</c> disattiva la chiusura di fine sessione; restano stop, target, breakeven e
/// uscita a <c>MaxDaysInTrade = 9</c> giorni (<c>ExitModeDaysMax = 1</c>).</para>
///
/// <para><b>Gate extra.</b> Oltre ai neutri standard servono il pattern 43, una combinazione
/// su <c>PtnNeutNo</c>/5/23, ADX sotto soglia e in crescita rispetto a cinque barre fa, e i
/// pattern direzionali ±9 per verso.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.400, target $4.500,
/// breakeven $2.650.</para>
/// </summary>
public sealed class Easy_303_GC_15 : TrendDeveloperEngine
{
    private const int AdxLength = 5;
    private const int AdxPastBars = 5;
    private const int AdxThreshold = 60;

    private decimal _adxValue;
    private decimal _adxPastValue;

    public override string Name => "Easy_303_GC_15";
    public override string Description => "Trend Developer ADX, rottura estremi sessione precedente, GC 15m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 15;

    public Easy_303_GC_15()
    {
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1700;    // sessionEndTimeA
        Contracts = 1;

        Trigger = TrendTrigger.PreviousSessionOhlc;  // MyTrigger = 2

        StartTrade = 0;              // MyStartTrade
        EndTrade = 1600;             // MyEndTrade
        InclusiveWindowEnd = true;   // l'originale include la fine della finestra
        MaxTradesPerDay = 1;

        NeutralYes = 26;      // PtnNeutYes
        NeutralNo = 56;       // sentinella — il gate composto è in PassesExtraGates
        DirectionalYes = -47;   // PtnDirYes

        NotEntryDayLong = 1;        // NotDayLE — lunedì
        NotEntryDayShort = 0;       // NotDaySE — domenica
        NotEntryMonthLong = 11;     // NotMonthLE
        NotEntryMonthShort = 8;     // NotMonthSE

        StopMoney = 2400;       // MyStop
        ProfitMoney = 4500;     // MyProfit
        BreakEvenMoney = 2650;  // MyBE
        MaxDaysInTrade = 9;     // ExitModeDaysMax = 1
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        UpdateAdx(data);
        return EvaluateCore(data, currentDate);
    }

    protected override bool PassesExtraGates(decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        EasyLib.PatternNeutralFast(43, ohlc) &&
        ((!EasyLib.PatternNeutralFast(NeutralNo, ohlc) && !EasyLib.PatternNeutralFast(5, ohlc)) ||
         !EasyLib.PatternNeutralFast(23, ohlc)) &&
        _adxValue <= AdxThreshold &&
        _adxValue > _adxPastValue;

    protected override bool PassesDirectionalExtraGates(
        SignalType side, decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        side == SignalType.Buy
            ? EasyLib.PatternDirectionalFast(-9, ohlc)
            : EasyLib.PatternDirectionalFast(9, ohlc);

    private void UpdateAdx(OhlcvData[] data)
    {
        _adxValue = CalculateBarAdx(data, data.Length - 1);
        var pastIndex = Math.Max(1, data.Length - 1 - AdxPastBars);
        _adxPastValue = CalculateBarAdx(data, pastIndex);
    }

    private static decimal CalculateBarAdx(OhlcvData[] data, int endIndex)
    {
        if (endIndex < 1)
            return 0m;

        var calc = new decimal[4];
        for (var index = 1; index <= endIndex; index++)
        {
            _ = EasyLib.iADXOnArray(
                AdxLength,
                data[index].High, data[index].Low, data[index].Close,
                data[index - 1].High, data[index - 1].Low, data[index - 1].Close,
                ref calc);
        }

        return calc[0] * 100m;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
