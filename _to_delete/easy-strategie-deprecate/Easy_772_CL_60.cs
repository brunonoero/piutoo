using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_772 — crossover SMA con filtro daily e pendenza, CL 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_772_CL_60__7.txt</c>. La logica vive in
/// <see cref="MovingAverageCrossoverEngine"/>; ingresso solo da flat dopo setup daily della
/// sessione precedente e controllo del gradiente.</para>
///
/// <para><b>Uscite.</b> Stop monetario, reverse sul cross opposto e chiusura del venerdì a fine
/// sessione sono gestiti dal motore MAC. Nessuna uscita close-dependent sulla strategia.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, $1.000 per punto. Stop $1.500.</para>
/// </summary>
public sealed class Easy_772_CL_60 : MovingAverageCrossoverEngine
{
    public override string Name => "Easy_772_CL_60";
    public override string Description => "SMA cross con filtro daily e gradiente, CL 60m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 60;

    public override int RequiredCandles =>
        Math.Max(base.RequiredCandles, SessionsToCandles(6));

    public Easy_772_CL_60()
    {
        SessionEndTime = 1700;  // chiusura venerdì 16:00–17:00

        FastPeriod = 12;   // myFastLength
        SlowPeriod = 24;   // mySlowLength
        GradientPeriod = 2;  // myGradientLength
        GradientFactor = 1.6m;  // myGradientFactor

        RequireFlatPosition = true;
        UseDailyFilter = true;
        DailyBodyFactor = 0.5m;  // myDailyFactor

        StopMoney = 1500;  // myStop
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
