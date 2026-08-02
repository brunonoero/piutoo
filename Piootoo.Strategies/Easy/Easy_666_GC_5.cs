using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_666 — breakout stop su highd1/lowd1 con filtro di espansione intraday, GC 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_666_GC_5__7.txt</c>. Variante legacy di
/// <see cref="VolatilityBreakoutEngine"/> con <c>OneEntryPerSessionPerSide</c> come i flag
/// <c>okl</c>/<c>oks</c> dell'originale.</para>
///
/// <para><b>Finestra.</b> Opera fuori dalla fascia 15:30–21:00 (<c>StartTrade = 2100</c>,
/// <c>EndTrade = 1530</c>, fine esclusa). Esclude luglio e agosto.</para>
///
/// <para><b>Uscite.</b> Stop, breakeven e target in denaro per contratto
/// (<c>StopLoss×1,2×BPV</c>, <c>BreakEven×BPV</c>, <c>TakeProfit×BPV</c>). La chiusura forzata
/// alle 16:54 non è replicata dal motore VBO: restano i tre livelli monetari.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $900, breakeven $1.500,
/// target $4.500.</para>
/// </summary>
public sealed class Easy_666_GC_5 : VolatilityBreakoutEngine
{
    public override string Name => "Easy_666_GC_5";
    public override string Description => "Breakout highd1/lowd1 con filtro range, GC 5m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 5;

    public Easy_666_GC_5()
    {
        UseLegacyVariant = true;
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1715;    // sessionEndTimeA
        Contracts = 1;            // MyContracts

        EntryOrderType = Shared.Enums.TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.PreviousSessionExtremes;

        StartTrade = 2100;  // fuori fascia 15:30–21:00
        EndTrade = 1530;

        OneEntryPerSessionPerSide = true;

        UpRangeFactor = 0.75m;
        DownRangeFactor = 0.75m;

        ExcludedMonthOne = 7;   // MonthUno
        ExcludedMonthTwo = 8;   // MonthDue
        NotEntryDayLong = 0;    // MyDOW: esclusa domenica
        NotEntryDayShort = 3;   // MyDOW: escluso mercoledì

        StopMoney = 900;        // StopLoss(7.5) × 1.2 × BPV
        BreakEvenMoney = 1500;  // BreakEven(15) × BPV
        ProfitMoney = 4500;     // TakeProfit(45) × BPV
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
