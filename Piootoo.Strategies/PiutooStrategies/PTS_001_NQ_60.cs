using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_001 — trend following mirrored su NQ a 60 minuti.
///
/// <para>Le barre e gli orari sono in UTC. Sessione CME 17:00–16:00; finestra operativa
/// inclusiva 16:00–03:00. Gate neutri 47/1 e direzionali mirrored 50/8.</para>
///
/// <para>Ingresso stop su <c>H_d1</c>/<c>L_d1</c>, valido solo sulla barra successiva. Un fill
/// per lato per sessione è gestito dal motore TF. <c>intraday_only = false</c>: nessuna chiusura
/// di fine sessione, posizioni multiday.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.000, target $3.000.</para>
/// </summary>
public sealed class PTS_001_NQ_60 : TfMirroredEngine
{
    public override string Name => "PTS_001_NQ_60";
    public override string Description => "TF_M NQ 60: breakout H/L d1, pattern mirrored, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 60;

    public PTS_001_NQ_60()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
        Contracts = 1;

        StartHour = 16;
        EndHour = 3;
        SkipDay = -1;

        NeutralYes = 47;
        NeutralNo = 1;
        DirectionalYes = 50;
        DirectionalNo = 8;

        IntradayOnly = false;

        StopMoney = 1000;
        ProfitMoney = 3000;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StopLoss", out var stop))
            StopMoney = Convert.ToInt32(stop);
        if (parameters.TryGetValue("TakeProfit", out var profit))
            ProfitMoney = Convert.ToInt32(profit);
        if (parameters.TryGetValue("StartHour", out var startHour))
            StartHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndHour = Convert.ToInt32(endHour);
    }
}
