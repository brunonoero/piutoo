using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_CL_PCH_001_30</b> — PC su CL 30m, codice della ricerca <c>CL-30M-PC-65acec</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 114 trade, netto $100,009; broker (27/08/2025 → 09/09/2026)
/// 10 trade, netto $491. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/CL-30M-PC-65acec.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $100,009 su 114 trade · $877 a trade · drawdown $7,415 · netto/DD 13.49 · anni in perdita 2 su 14 · notti a mercato 1.08 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $50,472 su 114 trade, drawdown $7,377.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 20 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 20 barre + 0.40 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 20 barre − 0.40 × ATR50</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para>· Solo short: il lato long non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 13: '|O_d5-C_d1| &lt; 1.0 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 01:00 e 18:00, CET: ordini emessi sulle barre che chiudono fra le 01:00 e le 18:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (46 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 3.00 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_CL_PCH_001_30 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_CL_PCH_001_30";

    public override string Description => "PC CL 30m, ricerca PT5DAV CL-30M-PC-65acec";

    public override string Symbol => "@CL";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "CL-30M-PC-65acec";

    public PT5DAV_CL_PCH_001_30()
    {
        TradingWindow = ResearchWindow(1, 18);  // start_hour 1, end_hour 18, verbatim
        ChannelBars = 20;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0.4m;                       // breakout_offset_atr
        Direction = 2;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 13;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 46;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 3m;                         // take_profit_atr
    }
}
