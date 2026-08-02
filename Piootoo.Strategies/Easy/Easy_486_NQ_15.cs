using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_486 — VBO legacy: ingresso a mercato quando il close supera <c>OpenS0 ± ATR × k</c>,
/// NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_486_NQ_15__7.txt</c>. Un solo ingresso
/// al giorno nella finestra 09:00–13:00; chiusura forzata alle 15:30 espressa come
/// <c>CloseAtUtc</c> sull'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.300, target $4.000.</para>
/// </summary>
public sealed class Easy_486_NQ_15 : VolatilityBreakoutEngine
{
    private const int SessionFlatTime = 1530;

    public override string Name => "Easy_486_NQ_15";
    public override string Description => "Ingresso a mercato su banda ATR da apertura sessione, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public override int RequiredCandles => Math.Max(SessionsToCandles(6), 520);

    public Easy_486_NQ_15()
    {
        UseLegacyVariant = true;
        SessionStartTime = 1700;  // sessionStartTimeA
        SessionEndTime = 1600;    // sessionEndTimeA
        Contracts = 1;

        EntryOrderType = TradeOrderType.Market;
        EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;
        RequireCloseBeyondAtrBand = true;

        StartTrade = 900;   // MyStartTrade
        EndTrade = 1300;    // MyEndTrade
        MaxEntriesPerSession = 1;

        NeutralYes = 55;
        NeutralNo = 56;

        FastYesLong = 26;    // PtnFastLongYes
        FastNoLong = 62;     // PtnFastLongNo
        FastYesShort = 57;   // PtnFastShortYes
        FastNoShort = 23;    // PtnFastShortNo

        AtrLength = 500;            // MyAtrLen
        AtrMultiplierLong = 6m;     // ATRFactorL
        AtrMultiplierShort = 8.5m;  // ATRFactorS

        StopMoney = 1300;    // StopLoss
        ProfitMoney = 4000;  // TakeProfit
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var signal = EvaluateCore(data, currentDate);
        if (signal.Type == SignalType.Hold)
            return signal;

        signal.CloseAtUtc = ResolveCloseAtUtc(signal.Date, SessionFlatTime);
        if (signal.CompanionSignals is not null)
        {
            foreach (var companion in signal.CompanionSignals)
                companion.CloseAtUtc = ResolveCloseAtUtc(companion.Date, SessionFlatTime);
        }

        return signal;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MySize", out var size))
            Contracts = Convert.ToInt32(size);
    }
}
