using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_PCH_003_30 — PC su NQ a 30 minuti, famiglia 04 della consegna
/// <c>run_20260815_1021</c>.
///
/// <para>Breakout del canale di Donchian calcolato sulle barre, non sulle sessioni.</para>
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
/// <item><description>deve essere FALSO — neutrale 8: <c>|O_d1-C_d1| &gt; 0.9 * (H_d1-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>deve essere FALSO — direzionale 16: <c>C_d1 &gt; C_d2 * (1 + 0.01)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &gt; O_d0 * 0.995</c></description></item>
/// <item><description>deve essere FALSO — direzionale 16: <c>C_d1 &lt; C_d2 * (1 - 0.01)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 14:00 e 04:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $2,500 per contratto = 125.00 pt</description></item>
/// <item><description>Take profit: $10,000 = 500.00 pt</description></item>
/// <item><description>Trailing stop: $2,000 = 100.00 pt</description></item>
/// <item><description>Breakeven a $1,000 = 50.00 pt di utile</description></item>
/// <item><description>Nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$201.5</description></item>
/// <item><term>Out-of-sample</term><description>$151,529 su 279 trade &#183; drawdown $27,120 &#183; profit factor 1.83 &#183; $543 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.44</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.37</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$25,384</description></item>
/// </list>
/// </summary>
public sealed class PTS_NQ_PCH_003_30 : PriceChannelEngine
{
    public override string Name => "PTS_NQ_PCH_003_30";
    public override string Description =>
        "PC NQ 30m: famiglia 04 run 20260815, finestra 14:00–04:00, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public PTS_NQ_PCH_003_30()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 0;
        SessionEndTime = 2359;
        Contracts = 1;

        ChannelBars = 50; // channel_len
        Direction = 1;    // direction: 1 = solo long
        OffsetTicks = 0;  // breakout_offset_ticks
        TickSize = 0.25m; // tick NQ
        DvolMin = 0m;     // dvol_min: filtro di volatilità disattivo

        StartTime = 1400; // start_hour
        EndTime = 400;    // end_hour
        SkipDay = -1;     // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 55;      // ptn_neut_yes
        NeutralNo = 8;        // ptn_neut_no
        DirectionalYes = -48; // ptn_dir_yes
        DirectionalNo = 16;   // ptn_dir_no

        IntradayOnly = false; // intraday_only

        StopMoney = 2500;         // stop_loss, $ per contratto = 125.00 pt
        ProfitMoney = 10000;      // take_profit, $ per contratto
        TrailingStopMoney = 2000; // trailing_stop
        BreakEvenMoney = 1000;    // breakeven
        MaxBars = 0;              // max_bars  (0 = nessuna uscita a tempo)
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
        if (parameters.TryGetValue("TrailingStop", out var trailingStop))
            TrailingStopMoney = Convert.ToInt32(trailingStop);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("ChannelLength", out var channelLength))
            ChannelBars = Convert.ToInt32(channelLength);
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
