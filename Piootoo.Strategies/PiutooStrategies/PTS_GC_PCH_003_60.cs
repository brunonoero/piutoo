using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_GC_PCH_003_60 — PC su GC a 60 minuti, famiglia 01 strategia 3 di 3 della consegna
/// <c>run_20260819_0659</c> (run 003).
///
/// <para>Identica a <see cref="PTS_GC_PCH_001_60"/> tranne <c>ptn_neut_no</c>, che qui vale 56:
/// il 56 è la <b>sentinella sempre falsa</b> di <c>PatternNeutralFast</c>, quindi la strategia
/// <b>non ha</b> il filtro neutrale inibitore.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 2: <c>|O_d1-C_d1| &lt; 0.25 * (H_d1-L_d1)</c></description></item>
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
/// <para><b>Quando può operare.</b> Come la 001: 06:00–05:00 CET = 00:00–23:00 New York,
/// nessun giorno escluso, multiday, una entrata per sessione e per direzione.</para>
///
/// <para><b>Uscite.</b> Stop loss $2,250 (22.50 pt), take profit $4,000 (40.00 pt), nessun
/// trailing, nessun breakeven, nessuna uscita a tempo.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto, tick 0,1 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$173.2</description></item>
/// <item><term>Out-of-sample</term><description>$57,464 su 96 trade &#183; drawdown $15,218 &#183; profit factor 1.49 &#183; $599 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.52</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.29</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$44,699</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>Equivalente di <c>PTS_GC_PCH_001_60</c> secondo la consegna: più del 70% degli ordini di
/// entrata in comune. Disabilitata per lo stesso motivo della 002.</para>
/// </remarks>
[StrategiaDisabilitata(
    "Doppione di PTS_GC_PCH_001_60: stessi ordini di entrata (run_20260819_0659, IMPLEMENTAZIONE.md §1).")]
public sealed class PTS_GC_PCH_003_60 : PriceChannelEngine
{
    public override string Name => "PTS_GC_PCH_003_60";
    public override string Description =>
        "PC GC 60m: famiglia 01/3 run 20260819_0659, Donchian 30, senza filtro neutrale inibitore";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public PTS_GC_PCH_003_60()
    {
        SessionStartTime = 1800;  // riapertura COMEX, ora di New York
        SessionEndTime = 1700;    // chiusura COMEX, ora di New York
        Contracts = 1;

        ChannelBars = 30; // channel_len
        Direction = 0;    // direction: 0 = long e short
        OffsetTicks = 2;  // breakout_offset_ticks
        TickSize = 0.1m;  // tick GC
        DvolMin = 0m;     // dvol_min

        StartTime = 0;    // start_hour 6 CET
        EndTime = 2300;   // end_hour 5 CET
        TradingWindowInclusive = true;
        SkipDay = -1;     // skip_day

        NeutralYes = 2;       // ptn_neut_yes
        NeutralNo = 56;       // ptn_neut_no: sentinella sempre falsa = nessun filtro
        DirectionalYes = -14; // ptn_dir_yes
        DirectionalNo = -21;  // ptn_dir_no

        IntradayOnly = false; // intraday_only = 0

        StopMoney = 2250;      // stop_loss = 22.50 pt
        ProfitMoney = 4000;    // take_profit = 40.00 pt
        TrailingStopMoney = 0; // trailing_stop
        BreakEvenMoney = 0;    // breakeven
        MaxBars = 0;           // max_bars
    }
}
