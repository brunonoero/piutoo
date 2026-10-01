using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_SBO_002_30</b> — BO su NQ 30m, codice della ricerca <c>NQ-30M-BO-82f356</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 510 trade, netto 169,225; fuori campione 06/2022 → 05/2025 netto
/// 6,347; broker (16/09/2025 → 09/09/2026) 42 trade, netto 21,425.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-30M-BO-82f356.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $169,225 su 510 trade · $332 a trade · drawdown $45,100 · netto/DD 3.75 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $45,361 su 510 trade, drawdown $17,954.</para>
/// <para><b>Ordine STOP sul canale a 2 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 33: '(H_d0-L_d0) &gt; L_d0 * 0.01'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 18:00 e 19:00, CET: ordini emessi sulle barre che chiudono fra le 18:00 e le 19:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.50 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_NQ_SBO_002_30 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT8DAV_NQ_SBO_002_30";

    public override string Description => "BO NQ 30m, ricerca v5.1 NQ-30M-BO-82f356";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "NQ-30M-BO-82f356";

    public PT8DAV_NQ_SBO_002_30()
    {
        TradingWindow = ResearchWindow(18, 19); // start_hour 18, end_hour 19, verbatim
        Sessions = 2;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 33;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 2;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.5m;                       // take_profit_atr
    }
}
