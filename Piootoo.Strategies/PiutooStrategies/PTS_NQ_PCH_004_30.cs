using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_PCH_004_30 — PC su NQ a 30 minuti, famiglia 05 della consegna
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
/// <item><description>deve essere VERO — neutrale 3: <c>|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>deve essere FALSO — direzionale 46: <c>(C_d1 &gt; O_d1) E (C_d2 &lt; O_d2)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &gt; O_d0 * 0.995</c></description></item>
/// <item><description>deve essere FALSO — direzionale 46: <c>(C_d1 &lt; O_d1) E (C_d2 &gt; O_d2)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 11:00 e 10:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $2,250 per contratto = 112.50 pt</description></item>
/// <item><description>Take profit: $10,000 = 500.00 pt</description></item>
/// <item><description>Trailing stop: $2,000 = 100.00 pt</description></item>
/// <item><description>Breakeven a $500 = 25.00 pt di utile</description></item>
/// <item><description>Nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$140.1</description></item>
/// <item><term>Out-of-sample</term><description>$67,233 su 178 trade &#183; drawdown $16,660 &#183; profit factor 1.71 &#183; $378 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.76</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.37</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$24,847</description></item>
/// </list>
/// </summary>
public sealed class PTS_NQ_PCH_004_30 : PriceChannelEngine
{
    public override string Name => "PTS_NQ_PCH_004_30";
    public override string Description =>
        "PC NQ 30m: famiglia 05 run 20260815, finestra 11:00–10:00, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public PTS_NQ_PCH_004_30()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 0;
        SessionEndTime = 2359;
        Contracts = 1;

        ChannelBars = 50; // channel_len
        Direction = 1;    // direction: 1 = solo long
        OffsetTicks = 2;  // breakout_offset_ticks
        TickSize = 0.25m; // tick NQ
        DvolMin = 0m;     // dvol_min: filtro di volatilità disattivo

        StartTime = 1100; // start_hour
        EndTime = 1000;   // end_hour
        SkipDay = -1;     // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 3;       // ptn_neut_yes
        NeutralNo = 56;       // ptn_neut_no
        DirectionalYes = -48; // ptn_dir_yes
        DirectionalNo = 46;   // ptn_dir_no

        IntradayOnly = false; // intraday_only

        StopMoney = 2250;         // stop_loss, $ per contratto = 112.50 pt
        ProfitMoney = 10000;      // take_profit, $ per contratto
        TrailingStopMoney = 2000; // trailing_stop
        BreakEvenMoney = 500;     // breakeven
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
