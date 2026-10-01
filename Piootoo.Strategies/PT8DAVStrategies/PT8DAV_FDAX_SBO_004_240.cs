using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_FDAX_SBO_004_240</b> — BO su FDAX 4h, codice della ricerca <c>FDAX-4H-BO-e72a1e</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 259 trade, netto 314,224; fuori campione 06/2022 → 05/2025 netto
/// 36,410; broker (16/09/2025 → 09/09/2026) 20 trade, netto -11,173.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/FDAX-4H-BO-e72a1e.csv</c>.</para>
///
/// <para><b>Barre a 4 ore piegate dal motore.</b> La ricerca ha costruito questa strategia su barre
/// 08-12, 12-16, 16-20, 20-22 (ora di Roma), che nel feed non esistono: la griglia a 4 ore del DAX
/// e' ancorata all'01:00, quella delle PT3B. La classe riceve quindi la serie <b>a 60 minuti</b>
/// (<c>TimeframeMinutes</c>) e ragiona su barre da 240 (<c>BarMinutes</c>) che il motore si
/// costruisce: vedi <see cref="Pt5DavEngineBase.BarMinutes"/>. Il nome dice la barra della strategia,
/// non la serie.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $314,224 su 259 trade · $1,213 a trade · drawdown $35,523 · netto/DD 8.85 · anni in perdita 3 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $121,008 su 259 trade, drawdown $19,951.</para>
/// <para><b>Ordine STOP sul canale a 5 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso + 0.10 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso − 0.10 × ATR50</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 3: '|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 28: 'L_d0 &gt; L_d1 * (1 + 0.005)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 28: 'H_d0 &lt; H_d1 * (1 - 0.005)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 08:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 08:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_FDAX_SBO_004_240 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT8DAV_FDAX_SBO_004_240";

    public override string Description => "BO FDAX 4h, ricerca v5.1 FDAX-4H-BO-e72a1e";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 60;

    public override int BarMinutes => 240;

    public override string ResearchCode => "FDAX-4H-BO-e72a1e";

    public PT8DAV_FDAX_SBO_004_240()
    {
        TradingWindow = ResearchWindow(8, 12);  // start_hour 8, end_hour 12, verbatim
        Sessions = 5;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0.1m;                       // breakout_offset_atr
        NeutralYes = 3;                         // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 28;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
