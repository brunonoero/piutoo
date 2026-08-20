using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_GC_RHL_002_60 — RHL su GC a 60 minuti, famiglia 03 della consegna
/// <c>run_20260819_0659</c> (run 003).
///
/// <para>Stessa meccanica di <see cref="PTS_GC_RHL_001_60"/> — limit buy a <c>L_d1 − 20 tick</c>
/// (−2.00 pt), limit sell a <c>H_d1 + 80 tick</c> (+8.00 pt), fill solo con penetrazione stretta
/// del livello — ma filtri diversi: qui c'è il neutrale inibitore 12 e non c'è il direzionale
/// richiesto (<c>ptn_dir_yes = 52</c> è la sentinella sempre vera).</para>
///
/// <para><b>Solo long.</b> <c>direction = 1</c>: il lato short non opera mai.</para>
///
/// <para><b>Sessione e fuso.</b> Sessioni <c>d0..d5</c> con confine a mezzanotte CET = 18:00 di
/// New York, cioè la riapertura COMEX: <c>SessionStartTime</c> = 1800, <c>SessionEndTime</c> =
/// 1700 nell'orologio di borsa dello strumento.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 46: <c>(H_d0 &lt; H_d1) E (L_d0 &gt; L_d1)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 12: <c>|O_d5-C_d1| &lt; 0.75 * (H_d5-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere FALSO — direzionale -5: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 1.5</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b> — non operativo (<c>direction = 1</c>)</para>
/// <list type="bullet">
/// <item><description>deve essere FALSO — direzionale -5: <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 1.5</c></description></item>
/// </list>
///
/// <para><b>Il segno dei pattern direzionali è quello del reversal</b>, come nella 001: i campi
/// portano il valore grezzo di <c>parametri.csv</c> e l'inversione la applica il motore.</para>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 13:00 e 12:00 (a cavallo della mezzanotte), ora dei dati (CET) = 07:00–06:00 New York</description></item>
/// <item><description>Nessun giorno escluso</description></item>
/// <item><description>Chiude tutto a fine sessione: nessun overnight</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b></para>
/// <list type="bullet">
/// <item><description>Stop loss: $2,000 per contratto = 20.00 pt</description></item>
/// <item><description>Take profit: $5,000 = 50.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 12 barre (12 ore)</description></item>
/// <item><description>Chiusura di fine sessione (<c>intraday_only</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto, tick 0,1 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$91.7</description></item>
/// <item><term>Out-of-sample</term><description>$21,820 su 80 trade &#183; drawdown $7,516 &#183; profit factor 1.59 &#183; $273 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.68</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.34</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$14,765</description></item>
/// </list>
///
/// <para><b>Vincolo operativo.</b> Con <c>PTS_GC_RHL_001_60</c> condivide il 55% degli ordini di
/// entrata: sotto la soglia del 70%, ma è la coppia più vicina del run.</para>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-03-agosto/consegna/trades/fam03_RHL.csv</c>. Non ancora eseguita: manca il
/// datafeed <c>@GC</c> a 60 minuti.</para>
/// </summary>
public sealed class PTS_GC_RHL_002_60 : RhlEngine
{
    public override string Name => "PTS_GC_RHL_002_60";
    public override string Description =>
        "RHL GC 60m: famiglia 03 run 20260819_0659, limit L_d1-20t solo long, filtro neutrale 46/12";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public PTS_GC_RHL_002_60()
    {
        SessionStartTime = 1800;  // riapertura COMEX, ora di New York
        SessionEndTime = 1700;    // chiusura COMEX, ora di New York
        Contracts = 1;

        TickSize = 0.1m;             // tick GC
        LongLevelOffsetTicks = 20;   // long_offset_ticks = 2.00 pt sotto L_d1
        ShortLevelOffsetTicks = 80;  // short_offset_ticks = 8.00 pt sopra H_d1
        Direction = 1;               // direction: 1 = solo long

        NeutralYes = 46;      // ptn_neut_yes
        NeutralNo = 12;       // ptn_neut_no
        DirectionalYes = 52;  // ptn_dir_yes: sentinella sempre vera = nessun filtro
        DirectionalNo = -5;   // ptn_dir_no (valore grezzo: il verso lo applica il motore)

        StartHour = 7; // start_hour 13 CET
        EndHour = 6;   // end_hour 12 CET
        SkipDay = -1;  // skip_day

        IntradayOnly = true; // chiude a fine sessione

        StopMoney = 2000;   // stop_loss = 20.00 pt
        ProfitMoney = 5000; // take_profit = 50.00 pt
        MaxBars = 12;       // max_bars
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
        if (parameters.TryGetValue("LongOffsetTicks", out var longOffset))
            LongLevelOffsetTicks = Convert.ToInt32(longOffset);
        if (parameters.TryGetValue("ShortOffsetTicks", out var shortOffset))
            ShortLevelOffsetTicks = Convert.ToInt32(shortOffset);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            StartHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndHour = Convert.ToInt32(endHour);
    }
}
