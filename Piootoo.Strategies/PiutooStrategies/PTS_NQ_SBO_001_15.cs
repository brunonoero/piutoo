using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_SBO_001_15 — BO su NQ a 15 minuti, famiglia 04 della consegna
/// <c>run_20260814_1453</c>.
///
/// <para>Breakout sugli estremi delle ultime N sessioni chiuse, ordine stop valido solo sulla barra
/// successiva.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono
/// ricostruite dalle barre intraday con confine a mezzanotte, come nella ricerca: la
/// sessione è il giorno di calendario del feed, non la sessione CME 17:00–16:00. Per questo
/// <c>SessionStartTime</c> = 0 e <c>SessionEndTime</c> = 2359. Lo stesso confine governa il
/// secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern e limite di fill insieme.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 47: <c>(H_d1-L_d1) &lt; ((H_d2-L_d2) + (H_d3-L_d3)) / 2</c></description></item>
/// <item><description>deve essere FALSO — neutrale 7: <c>|O_d1-C_d1| &gt; 0.75 * (H_d1-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -1: <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.25</c></description></item>
/// <item><description>deve essere FALSO — direzionale 38: <c>H_d1 - C_d1 &lt; 0.2 * (H_d1-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -1: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.25</c></description></item>
/// <item><description>deve essere FALSO — direzionale 38: <c>C_d1 - L_d1 &lt; 0.2 * (H_d1-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 05:00 e 04:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $5,000 per contratto = 250.00 pt</description></item>
/// <item><description>Take profit: $3,000 = 150.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 644 barre (6.7 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$260.7</description></item>
/// <item><term>Out-of-sample</term><description>$95,754 su 124 trade &#183; drawdown $28,286 &#183; profit factor 1.56 &#183; $772 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.58</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.34</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$50,457</description></item>
/// </list>
/// </summary>
public sealed class PTS_NQ_SBO_001_15 : SessionBreakoutEngine
{
    public override string Name => "PTS_NQ_SBO_001_15";
    public override string Description =>
        "BO NQ 15m: famiglia 04 run 20260814, finestra 05:00–04:00, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_SBO_001_15()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 0;
        SessionEndTime = 2359;
        Contracts = 1;

        Sessions = 4;                  // n_sess
        IncludeCurrentSession = false; // lev_include_sess0
        BreakoutOffsetTicks = 0;       // breakout_offset_ticks
        TickSize = 0.25m;              // tick NQ

        StartTime = 500; // start_hour
        EndTime = 400;   // end_hour
        SkipDay = -1;    // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 47;     // ptn_neut_yes
        NeutralNo = 7;       // ptn_neut_no
        DirectionalYes = -1; // ptn_dir_yes
        DirectionalNo = 38;  // ptn_dir_no

        IntradayOnly = false; // intraday_only

        StopMoney = 5000;   // stop_loss, $ per contratto = 250.00 pt
        ProfitMoney = 3000; // take_profit, $ per contratto
        MaxBars = 644;      // max_bars
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("Sessions", out var sessions))
            Sessions = Convert.ToInt32(sessions);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            StartTime = Convert.ToInt32(startHour) * 100;
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndTime = Convert.ToInt32(endHour) * 100;
    }
}
