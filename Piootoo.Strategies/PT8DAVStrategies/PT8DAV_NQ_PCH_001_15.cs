using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_PCH_001_15</b> — PC su NQ 15m, codice della ricerca <c>NQ-15M-PC-1113e7</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 676 trade, netto 607,159; fuori campione 06/2022 → 05/2025 netto
/// 8,983; broker (16/09/2025 → 09/09/2026) 56 trade, netto 6,736.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-15M-PC-1113e7.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $607,159 su 676 trade · $898 a trade · drawdown $100,841 · netto/DD 6.02 · anni in perdita 3 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $142,906 su 676 trade, drawdown $44,441.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 20 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 20 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 20 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 9: 'H_d0 - O_d0 &lt; H_d1 - O_d1'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 9: 'O_d0 - L_d0 &lt; O_d1 - L_d1'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 04:00 e 05:00, CET: ordini emessi sulle barre che chiudono fra le 04:00 e le 05:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.75 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_NQ_PCH_001_15 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT8DAV_NQ_PCH_001_15";

    public override string Description => "PC NQ 15m, ricerca v5.1 NQ-15M-PC-1113e7";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "NQ-15M-PC-1113e7";

    public PT8DAV_NQ_PCH_001_15()
    {
        TradingWindow = ResearchWindow(4, 5);   // start_hour 4, end_hour 5, verbatim
        ChannelBars = 20;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 9;                     // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.75m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
