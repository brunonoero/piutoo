using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_PCH_003_240</b> — PC su BTC 4h, codice della ricerca <c>BTC-4H-PC-c81cdb</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 83 trade, netto $647,007; broker (27/08/2025 → 09/09/2026)
/// 12 trade, netto $-47,278. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-4H-PC-c81cdb.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $647,007 su 83 trade · $7,795 a trade · drawdown $91,702 · netto/DD 7.06 · anni in perdita 2 su 8 · notti a mercato 3.66 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $219,453 su 83 trade, drawdown $45,396.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 20 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 20 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 20 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 2: '|O_d1-C_d1| &lt; 0.25 * (H_d1-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 08:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 08:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di martedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (60 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_PCH_003_240 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_BTC_PCH_003_240";

    public override string Description => "PC BTC 4h, ricerca PT5DAV BTC-4H-PC-c81cdb";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BTC-4H-PC-c81cdb";

    public PT5DAV_BTC_PCH_003_240()
    {
        TradingWindow = ResearchWindow(-1, 8);  // start_hour -1, end_hour 8, verbatim
        ChannelBars = 20;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 2;                         // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 1;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 60;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
