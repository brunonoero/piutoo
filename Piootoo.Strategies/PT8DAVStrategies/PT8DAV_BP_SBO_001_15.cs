using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_BP_SBO_001_15</b> — BO su BP 15m, codice della ricerca <c>BP-15M-BO-58a5fd</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 141 trade, netto 10,709; fuori campione 06/2022 → 05/2025 netto
/// 1,085; broker (16/09/2025 → 09/09/2026) 5 trade, netto 420.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/BP-15M-BO-58a5fd.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $10,709 su 141 trade · $76 a trade · drawdown $1,210 · netto/DD 8.85 · anni in perdita 3 su 14 · notti a mercato 0.52 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $9,522 su 141 trade, drawdown $1,292.</para>
/// <para><b>Ordine STOP sul canale a 5 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 3: '|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 12: '|O_d5-C_d1| &lt; 0.75 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 11:00 e 13:00, CET: ordini emessi sulle barre che chiudono fra le 11:00 e le 13:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 17:00 America/New_York (rollover (CFD sempre aperto)) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.30 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.75 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_BP_SBO_001_15 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT8DAV_BP_SBO_001_15";

    public override string Description => "BO BP 15m, ricerca v5.1 BP-15M-BO-58a5fd";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "BP-15M-BO-58a5fd";

    public PT8DAV_BP_SBO_001_15()
    {
        TradingWindow = ResearchWindow(11, 13); // start_hour 11, end_hour 13, verbatim
        Sessions = 5;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 3;                         // ptn_neut_yes
        NeutralNo = 12;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.3m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.75m;                      // take_profit_atr
    }
}
