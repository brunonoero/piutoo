using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_796 — Trend Developer con ingresso a mercato su estensione ATR da apertura di
/// sessione, NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_796_NQ_15____1440__7.txt</c>.
/// <c>MyTrigger = 1</c> ma l'originale entra <c>next bar at market</c> quando il close ha
/// superato <c>opend0 ± ATR(23) × 0,35</c>, con un secondo gate su <c>ATR(9) ≤ 35</c>.</para>
///
/// <para><b>I due ATR sono su serie diverse, ed è il punto delicato.</b> L'originale è un grafico a
/// 15 minuti con <c>data2</c> giornaliero: <c>ATRvalue = averagetruerange(23) data2</c> misura la
/// volatilità in <i>giorni</i> ed è quella moltiplicata per 0,35 nel confronto con l'apertura di
/// sessione, mentre <c>ATRvalueID = averagetruerange(9)</c> — senza <c>data2</c> — resta sulle barre
/// del grafico. Il primo passa quindi dalla serie di sessione
/// (<see cref="EasyLib.BuildSessionSeries"/>), il secondo no.</para>
///
/// <para><b>Uscite.</b> <c>ID = 1</c> senza chiusura di sessione; stop, target, breakeven e
/// <c>MaxDaysInTrade = 9</c> (<c>ExitModeDaysMax = 1</c>) sono dichiarabili all'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $2.120, target $5.160,
/// breakeven $3.130.</para>
/// </summary>
public sealed class Easy_796_NQ_15 : TrendDeveloperEngine
{
    private const int AtrIdLength = 9;
    private const int AtrIdThreshold = 35;

    /// <summary><c>ATRLength</c>: sessioni, perché l'ATR vive su <c>data2</c> giornaliero.</summary>
    private const int AtrSessions = 23;

    public override string Name => "Easy_796_NQ_15";
    public override string Description => "Trend Developer ingresso a mercato su gate ATR, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    // L'ATR su data2 consuma 23 sessioni più quella che serve al primo true range; si aggiunge una
    // sessione di margine perché la prima della finestra è quasi sempre troncata.
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        SessionsToCandles(AtrSessions + 2));

    public Easy_796_NQ_15()
    {
        SessionStartTime = 1700;  // sessionStartTimeA
        SessionEndTime = 1559;    // sessionEndTimeA
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;  // MyTrigger = 1
        MarketEntry = true;

        StartTrade = 730;   // MyStartTrade
        EndTrade = 1530;    // MyEndTrade
        InclusiveWindowEnd = false;
        MaxTradesPerDay = 4;

        NeutralYes = 33;      // PtnNeutYes
        NeutralNo = 23;       // PtnNeutNo
        DirectionalYes = 52;  // PtnDirYes

        BaseYesShort = 19;  // PtnSY
        BaseNoShort = 32;   // PtnSN

        AtrGateLength = AtrSessions;      // ATRLength — su data2, quindi sessioni
        AtrGateOnSessionSeries = true;    // `averagetruerange(ATRLength) data2`
        AtrGateMultiplierLong = 0.35m;    // ATRMult
        AtrGateMultiplierShort = 0.35m;

        StopMoney = 2120;       // MyStop
        ProfitMoney = 5160;     // MyProfit
        BreakEvenMoney = 3130;  // MyBE
        MaxDaysInTrade = 9;     // ExitModeDaysMax = 1
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    protected override bool PassesExtraGates(decimal[] ohlc, OhlcvData[] data, DateTime barTime)
    {
        if (data.Length < AtrIdLength + 1)
            return false;

        var atrId = EasyLib.AvgTrueRange(data, AtrIdLength);
        return atrId <= AtrIdThreshold;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
