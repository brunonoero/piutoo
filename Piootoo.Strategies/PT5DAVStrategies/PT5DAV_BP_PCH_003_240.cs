using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_PCH_003_240</b> — PC su BP 4h, codice della ricerca <c>BP-4H-PC-e9a443</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 265 trade, netto $18,916; broker (27/08/2025 → 09/09/2026)
/// 14 trade, netto $28. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-4H-PC-e9a443.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $18,916 su 265 trade · $71 a trade · drawdown $2,218 · netto/DD 8.53 · anni in perdita 2 su 14 · notti a mercato 0.09 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $15,628 su 265 trade, drawdown $2,157.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 50 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 50 barre + 0.20 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 50 barre − 0.20 × ATR50</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 29: '|O_d5-C_d1| &gt; 0.5 * (HH5-LL5)'</para>
/// <para>· deve essere FALSO — neutrale 8: '|O_d1-C_d1| &gt; 0.9 * (H_d1-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 04:00 e 20:00, CET: ordini emessi sulle barre che chiudono fra le 04:00 e le 20:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di giovedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 17:00 America/New_York (rollover (CFD sempre aperto)) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_BP_PCH_003_240 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT5DAV_BP_PCH_003_240";

    public override string Description => "PC BP 4h, ricerca PT5DAV BP-4H-PC-e9a443";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BP-4H-PC-e9a443";

    public PT5DAV_BP_PCH_003_240()
    {
        TradingWindow = ResearchWindow(4, 20);  // start_hour 4, end_hour 20, verbatim
        ChannelBars = 50;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0.2m;                       // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 29;                        // ptn_neut_yes
        NeutralNo = 8;                          // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 3;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
