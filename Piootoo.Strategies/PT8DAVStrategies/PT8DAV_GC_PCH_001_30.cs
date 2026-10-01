using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_GC_PCH_001_30</b> — PC su GC 30m, codice della ricerca <c>GC-30M-PC-6f9fd4</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 897 trade, netto 324,098; fuori campione 06/2022 → 05/2025 netto
/// 136; broker (16/09/2025 → 09/09/2026) 66 trade, netto 73,451.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/GC-30M-PC-6f9fd4.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Canale di prezzo (Donchian).* Compra quando il prezzo rompe il massimo delle ultime N barre. A differenza del BO il canale non e' fatto di sessioni ma di barre, e si ricalcola a ogni barra.</para>
/// <para>Da sapere. Ha un filtro di volatilita' che spegne la strategia quando il mercato si muove troppo poco, e puo' operare su un lato solo.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $324,098 su 897 trade · $361 a trade · drawdown $68,180 · netto/DD 4.75 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $121,977 su 897 trade, drawdown $29,885.</para>
/// <para><b>Ordine STOP sul canale di Donchian a 30 barre</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 30 barre</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 30 barre</para>
/// <para>· Il canale è calcolato sulle barre del timeframe, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 3: '|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 4: 'H_d0 - O_d0 &gt; (H_d1 - O_d1) * 1.0'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 4: 'O_d0 - L_d0 &gt; (O_d1 - L_d1) * 1.0'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 06:00 e 14:00, CET: ordini emessi sulle barre che chiudono fra le 06:00 e le 14:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.80 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_GC_PCH_001_30 : Pt5DavPriceChannelEngine
{
    public override string Name => "PT8DAV_GC_PCH_001_30";

    public override string Description => "PC GC 30m, ricerca v5.1 GC-30M-PC-6f9fd4";

    public override string Symbol => "@GC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "GC-30M-PC-6f9fd4";

    public PT8DAV_GC_PCH_001_30()
    {
        TradingWindow = ResearchWindow(6, 14);  // start_hour 6, end_hour 14, verbatim
        ChannelBars = 30;                       // channel_len, barra di segnale inclusa
        OffsetAtr = 0m;                         // breakout_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 3;                         // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 4;                     // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.8m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
