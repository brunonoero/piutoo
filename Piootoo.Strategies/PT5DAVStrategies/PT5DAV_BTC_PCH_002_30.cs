using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_PCH_002_30</b> — PC su BTC 30m, codice della ricerca <c>BTC-30M-PC-c8276f</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 213 trade, netto $861,253; broker (27/08/2025 → 09/09/2026)
/// 37 trade, netto $-50,392. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-30M-PC-c8276f.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $861,253 su 213 trade · $4,043 a trade · drawdown $318,237 · netto/DD 2.71 · anni in perdita 1 su 8 · notti a mercato 7.84 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $382,708 su 213 trade, drawdown $155,468.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 20 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 20 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 20 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 19: '|O_d5-C_d1| &gt; 1.0 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni di giovedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (460 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.80 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_PCH_002_30 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_BTC_PCH_002_30";

    public override string Description => "PC BTC 30m, ricerca PT5DAV BTC-30M-PC-c8276f";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "BTC-30M-PC-c8276f";

    public PT5DAV_BTC_PCH_002_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        ChannelBars = 20;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 19;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 3;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 460;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.8m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
