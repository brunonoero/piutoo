using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_156 — Unmirrored Trend Following su estremi d1, NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_156_NQ_15__7.txt</c>. La logica vive in
/// <see cref="TfUnmirroredEngine"/>; i quattro gate <c>PatternFast</c> sono indipendenti per
/// long e short.</para>
///
/// <para><b>Uscite.</b> <c>ID = 0</c> abilita la chiusura di sessione tramite
/// <c>CloseAtUtc</c> sull'ingresso, oltre a stop e target monetari dichiarati staticamente.
/// Non è più close-dependent: l'engine applica le uscite senza segnali a runtime.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.750, target $4.500.</para>
/// </summary>
public sealed class Easy_156_NQ_15 : TfUnmirroredEngine
{
    public override string Name => "Easy_156_NQ_15";
    public override string Description => "Unmirrored TF breakout H/L d1, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    // Come l'originale: 100 barre bastano per ricostruire H_d1/L_d1 nei test di parity.
    public override int RequiredCandles => 100;

    public Easy_156_NQ_15()
    {
        SessionStartTime = 1700;  // sessionStartTimeC
        SessionEndTime = 1600;    // sessionEndTimeC
        Contracts = 1;

        StartHour = 10;  // MyStartTrade 10:00
        EndHour = 15;    // MyEndTrade 15:00 — fine esclusiva via tw()

        FastYesLong = 54;    // MyPtnLY
        FastNoLong = 75;     // MyPtnLN
        FastYesShort = 111;  // MyPtnSY
        FastNoShort = 31;    // MyPtnSN

        IntradayOnly = true;  // ID = 0

        StopMoney = 1750;   // MyStop
        ProfitMoney = 4500; // MyProfit
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MyPtnLY", out var ptnLy))
            FastYesLong = Convert.ToInt32(ptnLy);
        if (parameters.TryGetValue("MyPtnLN", out var ptnLn))
            FastNoLong = Convert.ToInt32(ptnLn);
        if (parameters.TryGetValue("MyPtnSY", out var ptnSy))
            FastYesShort = Convert.ToInt32(ptnSy);
        if (parameters.TryGetValue("MyPtnSN", out var ptnSn))
            FastNoShort = Convert.ToInt32(ptnSn);
    }
}
