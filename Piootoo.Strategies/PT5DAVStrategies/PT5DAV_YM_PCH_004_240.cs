using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_YM_PCH_004_240</b> — PC su YM 4h, codice della ricerca <c>YM-4H-PC-ae2a80</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 340 trade, netto $99,603; broker (27/08/2025 → 09/09/2026)
/// 28 trade, netto $-12,715. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/YM-4H-PC-ae2a80.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $99,603 su 340 trade · $293 a trade · drawdown $77,457 · netto/DD 1.29 · anni in perdita 4 su 14 · notti a mercato 10.52 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $108,997 su 340 trade, drawdown $44,943.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 1 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 1 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 1 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 04:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 04:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di lunedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (60 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_YM_PCH_004_240 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_YM_PCH_004_240";

    public override string Description => "PC YM 4h, ricerca PT5DAV YM-4H-PC-ae2a80";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-PC-ae2a80";

    public PT5DAV_YM_PCH_004_240()
    {
        TradingWindow = ResearchWindow(-1, 4);  // start_hour -1, end_hour 4, verbatim
        ChannelBars = 1;                        // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 0;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 60;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
