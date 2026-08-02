using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_303 — Trend Developer su rottura highd1/lowd1 con gate ADX, GC 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_303_GC_15____1440__7.txt</c>.
/// <c>ID = 1</c> disattiva la chiusura di fine sessione; restano stop, target e breakeven.</para>
///
/// <para><b>Uscita a giorni: approssimata.</b> L'originale usa <c>ExitModeDaysMax = 0</c>, che
/// dopo <c>MaxDaysinTrade</c> giorni memorizza l'utile aperto e chiude solo quando peggiora o
/// passa in perdita. Qui è resa come <c>MaxDaysInTrade = 9</c>, cioè il ramo
/// <c>ExitModeDaysMax = 1</c> (chiusura incondizionata): serve il supporto alle uscite dipendenti
/// dall'utile aperto per replicare il ramo 0.</para>
///
/// <para><b>Gate extra.</b> Oltre al neutro <c>PtnNeutYes</c> servono il pattern 43, la
/// combinazione <c>(not PtnNeutNo and not 5) or not 23</c>, ADX sotto soglia e in crescita
/// rispetto a cinque <i>sessioni</i> fa, e i pattern direzionali ±9 per verso.</para>
///
/// <para><b>ADX su data2.</b> L'originale è un grafico a 15 minuti con <c>data2</c> giornaliero e
/// scrive <c>ADX(5) data2</c>: l'ADX vive sulle barre di sessione, non su quelle a 15 minuti. La
/// serie giornaliera è ricostruita da <see cref="EasyLib.BuildSessionSeries"/> aggregando il feed
/// intraday sugli stessi confini 18:00–17:00 usati dal resto della strategia, quindi non serve un
/// datafeed a 1440 separato. Anche <c>ADXPastvalue = 5</c> conta sessioni.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.400, target $4.500,
/// breakeven $2.650.</para>
/// </summary>
public sealed class Easy_303_GC_15 : TrendDeveloperEngine
{
    private const int AdxLength = 5;

    /// <summary><c>ADXPastvalue</c>: sessioni indietro, non barre a 15 minuti.</summary>
    private const int AdxPastSessions = 5;

    private const int AdxThreshold = 60;

    /// <summary>
    /// Sessioni di rodaggio dell'ADX. Lo smoothing di Wilder è ricorsivo e parte da zero: con
    /// periodo 5 la memoria effettiva è di poche barre, ma il valore va lasciato convergere prima
    /// di confrontarlo con una soglia. Vanno aggiunte le sessioni consumate dal confronto con il
    /// passato più una di margine, perché la prima sessione della finestra è quasi sempre troncata.
    /// </summary>
    private const int AdxWarmupSessions = 25;

    /// <summary><c>PtnNeutNo</c>: compare solo dentro il gate composto, non come veto autonomo.</summary>
    private const int NeutralNoInComposite = 1;

    private decimal _adxValue;
    private decimal _adxPastValue;

    public override string Name => "Easy_303_GC_15";
    public override string Description => "Trend Developer ADX, rottura estremi sessione precedente, GC 15m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 15;

    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        SessionsToCandles(AdxWarmupSessions + AdxPastSessions + 1));

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
        // L'originale non ha un veto neutro autonomo: PtnNeutNo entra solo nel gate composto,
        // quindi qui serve la sentinella "sempre falso" per neutralizzare quello dell'engine.
        NeutralNo = 56;
        DirectionalYes = -47;   // PtnDirYes

        NotEntryDayLong = 1;        // NotDayLE — lunedì
        NotEntryDayShort = 0;       // NotDaySE — domenica
        NotEntryMonthLong = 11;     // NotMonthLE
        NotEntryMonthShort = 8;     // NotMonthSE

        StopMoney = 2400;       // MyStop
        ProfitMoney = 4500;     // MyProfit
        BreakEvenMoney = 2650;  // MyBE
        MaxDaysInTrade = 9;     // MaxDaysinTrade — vedi nota sull'approssimazione di ExitModeDaysMax
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        UpdateAdx(data, currentDate);
        return EvaluateCore(data, currentDate);
    }

    protected override bool PassesExtraGates(decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        EasyLib.PatternNeutralFast(43, ohlc) &&
        ((!EasyLib.PatternNeutralFast(NeutralNoInComposite, ohlc) && !EasyLib.PatternNeutralFast(5, ohlc)) ||
         !EasyLib.PatternNeutralFast(23, ohlc)) &&
        _adxValue <= AdxThreshold &&
        _adxValue > _adxPastValue;

    protected override bool PassesDirectionalExtraGates(
        SignalType side, decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        side == SignalType.Buy
            ? EasyLib.PatternDirectionalFast(-9, ohlc)
            : EasyLib.PatternDirectionalFast(9, ohlc);

    private void UpdateAdx(OhlcvData[] data, DateTime currentDate)
    {
        // L'ultima barra della serie è la sessione in formazione, come `ADX(5) data2` su una barra
        // intraday in TradeStation.
        var sessions = EasyLib.BuildSessionSeries(SessionStartTime, SessionEndTime, data, currentDate);

        // Senza sessioni a sufficienza per il confronto col passato i due valori restano a zero: il
        // gate chiede `ADX > ADXpast`, quindi la strategia non entra invece di entrare su un ADX
        // ancora in rodaggio confrontato con uno zero.
        if (sessions.Length - 1 - AdxPastSessions < 1)
        {
            _adxValue = 0m;
            _adxPastValue = 0m;
            return;
        }

        _adxValue = CalculateBarAdx(sessions, sessions.Length - 1);
        _adxPastValue = CalculateBarAdx(sessions, sessions.Length - 1 - AdxPastSessions);
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
