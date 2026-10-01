using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_SBO_003_60</b> — BO su NQ 1h, codice della ricerca <c>NQ-1H-BO-8d09a9</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 287 trade, netto 194,745; fuori campione 06/2022 → 05/2025 netto
/// 65,079; broker (16/09/2025 → 09/09/2026) 23 trade, netto 15,506.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-1H-BO-8d09a9.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $194,745 su 287 trade · $679 a trade · drawdown $15,500 · netto/DD 12.56 · anni in perdita 1 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $69,203 su 287 trade, drawdown $6,467.</para>
/// <para><b>Ordine STOP sul canale a 2 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 3: '|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &gt; C_d2'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &lt; C_d2'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 18:00 e 18:00, CET: ordini emessi sulle barre che chiudono fra le 18:00 e le 18:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.25 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.75 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_NQ_SBO_003_60 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT8DAV_NQ_SBO_003_60";

    public override string Description => "BO NQ 1h, ricerca v5.1 NQ-1H-BO-8d09a9";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "NQ-1H-BO-8d09a9";

    public PT8DAV_NQ_SBO_003_60()
    {
        TradingWindow = ResearchWindow(18, 18); // start_hour 18, end_hour 18, verbatim
        Sessions = 2;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 3;                         // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 13;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 1.25m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.75m;                      // take_profit_atr
    }
}
