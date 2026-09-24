using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_PCH_002_60</b> — PC su BP 1h, codice della ricerca <c>BP-1H-PC-8146e0</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 239 trade, netto $24,841; broker (27/08/2025 → 09/09/2026)
/// 12 trade, netto $229. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-1H-PC-8146e0.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $24,841 su 239 trade · $104 a trade · drawdown $3,776 · netto/DD 6.58 · anni in perdita 2 su 14 · notti a mercato 1.42 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $22,513 su 239 trade, drawdown $4,331.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 155 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 155 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 155 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 4: '|O_d1-C_d1| &lt; 0.75 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 25: '|O_d5-C_d1| &lt; 0.5 * (HH5-LL5)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere FALSO — direzionale 21: 'H_d0 &gt; H_d1'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere FALSO — direzionale 21: 'L_d0 &lt; L_d1'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 09:00 e 16:00, CET: ordini emessi sulle barre che chiudono fra le 09:00 e le 16:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (23 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.25 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BP_PCH_002_60 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_BP_PCH_002_60";

    public override string Description => "PC BP 1h, ricerca PT5DAV BP-1H-PC-8146e0";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "BP-1H-PC-8146e0";

    public PT5DAV_BP_PCH_002_60()
    {
        TradingWindow = ResearchWindow(9, 16);  // start_hour 9, end_hour 16, verbatim
        ChannelBars = 155;                      // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 4;                         // ptn_neut_yes
        NeutralNo = 25;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 21;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 23;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.25m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
