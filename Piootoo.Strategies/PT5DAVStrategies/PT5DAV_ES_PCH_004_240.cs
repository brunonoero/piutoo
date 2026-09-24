using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_ES_PCH_004_240</b> — PC su ES 4h, codice della ricerca <c>ES-4H-PC-3671de</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 327 trade, netto $417,157; broker (27/08/2025 → 09/09/2026)
/// 29 trade, netto $5,307. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/ES-4H-PC-3671de.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $417,157 su 327 trade · $1,276 a trade · drawdown $64,865 · netto/DD 6.43 · anni in perdita 3 su 14 · notti a mercato 5.23 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $186,526 su 327 trade, drawdown $31,624.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 1 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 1 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 1 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 11: '|O_d5-C_d1| &lt; 0.5 * (H_d5-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale -27: 'H_d0 &lt; H_d1'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale -27: 'L_d0 &gt; L_d1'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 04:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 04:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (30 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.15 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_ES_PCH_004_240 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_ES_PCH_004_240";

    public override string Description => "PC ES 4h, ricerca PT5DAV ES-4H-PC-3671de";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "ES-4H-PC-3671de";

    public PT5DAV_ES_PCH_004_240()
    {
        TradingWindow = ResearchWindow(-1, 4);  // start_hour -1, end_hour 4, verbatim
        ChannelBars = 1;                        // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 11;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = -27;                   // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 30;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.15m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
