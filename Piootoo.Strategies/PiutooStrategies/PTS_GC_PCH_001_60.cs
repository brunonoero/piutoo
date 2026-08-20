using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_GC_PCH_001_60 — PC su GC a 60 minuti, famiglia 01 strategia 1 di 3 della consegna
/// <c>run_20260819_0659</c> (run 003).
///
/// <para>Breakout del canale di Donchian a 30 barre, calcolato sulle <b>barre del timeframe</b> e
/// non sulle sessioni. Il canale include la barra appena chiusa che produce il segnale — come
/// <c>highest(high, 30)</c> EasyLanguage e <c>donchian(shift=0)</c> del motore Python — e l'ordine
/// stop vale solo dalla barra successiva, quindi non c'è look-ahead.</para>
///
/// <para><b>Sessione e fuso.</b> La ricerca ricostruisce le sessioni <c>d0..d5</c> dalle barre
/// intraday con confine a <b>mezzanotte CET</b>, che è <b>le 18:00 di New York</b>, cioè la
/// riapertura COMEX: per questo <c>SessionStartTime</c> = 1800 e <c>SessionEndTime</c> = 1700, lo
/// stesso istante scritto nell'orologio di borsa dello strumento. Lo stesso confine governa il
/// secchio di <c>MaxEntriesPerSession</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 2: <c>|O_d1-C_d1| &lt; 0.25 * (H_d1-L_d1)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 30: <c>|O_d5-C_d1| &gt; 0.75 * (HH5-LL5)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -14: <c>C_d1 &lt; O_d1</c></description></item>
/// <item><description>deve essere FALSO — direzionale -21: <c>L_d0 &lt; L_d1</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -14: <c>C_d1 &gt; O_d1</c></description></item>
/// <item><description>deve essere FALSO — direzionale -21: <c>H_d0 &gt; H_d1</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 06:00 e 05:00 ora dei dati (CET) = 00:00–23:00 New York</description></item>
/// <item><description>Nessun giorno escluso</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $2,250 per contratto = 22.50 pt</description></item>
/// <item><description>Take profit: $4,000 = 40.00 pt</description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto, tick 0,1 punti. L'offset di
/// breakout è di 2 tick = 0.20 pt, senza tick impliciti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$215.0</description></item>
/// <item><term>Out-of-sample</term><description>$60,204 su 81 trade &#183; drawdown $19,202 &#183; profit factor 1.63 &#183; $743 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.56</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.29</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$34,728</description></item>
/// </list>
///
/// <para><b>Gli orari sono in ora di borsa (America/New_York), non nell'orologio del feed.</b>
/// Il motore Python lavorava su barre in ora europea e dichiarava gli orari in CET, che per GC è
/// New York più sei ore. Vedi <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Vincolo operativo.</b> Emette gli stessi ordini di entrata di
/// <c>PTS_GC_PCH_002_60</c> e <c>PTS_GC_PCH_003_60</c>, che per questo nascono disabilitate: non
/// vanno messe su conti separati insieme a questa.</para>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-03-agosto/consegna/trades/fam01_PC.csv</c>. Non ancora eseguita: manca il
/// datafeed <c>@GC</c> a 60 minuti.</para>
/// </summary>
public sealed class PTS_GC_PCH_001_60 : PriceChannelEngine
{
    public override string Name => "PTS_GC_PCH_001_60";
    public override string Description =>
        "PC GC 60m: famiglia 01 run 20260819_0659, Donchian 30, 2 tick, finestra 00:00–23:00 New York, multiday";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public PTS_GC_PCH_001_60()
    {
        // Sessione della ricerca (00:00 CET) scritta in ora di borsa GC.
        SessionStartTime = 1800;  // riapertura COMEX, ora di New York
        SessionEndTime = 1700;    // chiusura COMEX, ora di New York
        Contracts = 1;

        ChannelBars = 30; // channel_len
        Direction = 0;    // direction: 0 = long e short
        OffsetTicks = 2;  // breakout_offset_ticks
        TickSize = 0.1m;  // tick GC
        DvolMin = 0m;     // dvol_min: filtro di volatilità disattivo

        StartTime = 0;    // start_hour 6 CET
        EndTime = 2300;   // end_hour 5 CET
        TradingWindowInclusive = true;
        SkipDay = -1;     // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 2;       // ptn_neut_yes
        NeutralNo = 30;       // ptn_neut_no
        DirectionalYes = -14; // ptn_dir_yes
        DirectionalNo = -21;  // ptn_dir_no

        IntradayOnly = false; // intraday_only = 0

        StopMoney = 2250;      // stop_loss, $ per contratto = 22.50 pt
        ProfitMoney = 4000;    // take_profit, $ per contratto = 40.00 pt
        TrailingStopMoney = 0; // trailing_stop
        BreakEvenMoney = 0;    // breakeven
        MaxBars = 0;           // max_bars (0 = nessuna uscita a tempo)
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
