using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_GC_PCH_002_60 — PC su GC a 60 minuti, famiglia 01 strategia 2 di 3 della consegna
/// <c>run_20260819_0659</c> (run 003).
///
/// <para>Identica a <see cref="PTS_GC_PCH_001_60"/> tranne <c>ptn_dir_no</c>, che qui vale 53:
/// il 53 è la <b>sentinella sempre falsa</b> di <c>PatternDirectionalFast</c>, quindi la
/// strategia semplicemente <b>non ha</b> il filtro direzionale inibitore. Non è un filtro con
/// soglia altissima.</para>
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
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -14: <c>C_d1 &gt; O_d1</c></description></item>
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
/// <item><term>Atteso per trade</term><description>$209.2</description></item>
/// <item><term>Out-of-sample</term><description>$66,528 su 92 trade &#183; drawdown $15,218 &#183; profit factor 1.61 &#183; $723 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.47</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.29</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$37,352</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>La consegna la classifica come <b>equivalente</b> di <c>PTS_GC_PCH_001_60</c>: le due
/// condividono più del 70% degli ordini di entrata, quindi per il broker sono lo stesso ordine
/// mandato due volte. Tenerle entrambe attive non diversifica, raddoppia la size sullo stesso
/// segnale, e su conti separati è copy trading. L'attributo la toglie dal catalogo lasciandola
/// istanziabile per nome, come richiede la procedura di porting.</para>
/// </remarks>
[StrategiaDisabilitata(
    "Doppione di PTS_GC_PCH_001_60: stessi ordini di entrata (run_20260819_0659, IMPLEMENTAZIONE.md §1).")]
public sealed class PTS_GC_PCH_002_60 : PriceChannelEngine
{
    public override string Name => "PTS_GC_PCH_002_60";
    public override string Description =>
        "PC GC 60m: famiglia 01/2 run 20260819_0659, Donchian 30, senza filtro direzionale inibitore";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public PTS_GC_PCH_002_60()
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
        NeutralNo = 30;       // ptn_neut_no
        DirectionalYes = -14; // ptn_dir_yes
        DirectionalNo = 53;   // ptn_dir_no: sentinella sempre falsa = nessun filtro

        IntradayOnly = false; // intraday_only = 0

        StopMoney = 2250;      // stop_loss = 22.50 pt
        ProfitMoney = 4000;    // take_profit = 40.00 pt
        TrailingStopMoney = 0; // trailing_stop
        BreakEvenMoney = 0;    // breakeven
        MaxBars = 0;           // max_bars
    }
}
