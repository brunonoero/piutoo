using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_FDAX_SBO_002_30</b> — BO su FDAX 30m, codice della ricerca <c>FDAX-30M-BO-66b8eb</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 615 trade, netto 476,469; fuori campione 06/2022 → 05/2025 netto
/// 55,486; broker (16/09/2025 → 09/09/2026) 45 trade, netto -10,455.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/FDAX-30M-BO-66b8eb.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $476,469 su 615 trade · $775 a trade · drawdown $55,153 · netto/DD 8.64 · anni in perdita 2 su 14 · notti a mercato 1.35 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $227,775 su 615 trade, drawdown $28,497.</para>
/// <para><b>Ordine STOP sul canale a 5 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso + 0.02 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 5 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso − 0.02 × ATR50</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 12:00 e 16:00, CET: ordini emessi sulle barre che chiudono fra le 12:00 e le 16:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di giovedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (28 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.70 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT8DAV_FDAX_SBO_002_30 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT8DAV_FDAX_SBO_002_30";

    public override string Description => "BO FDAX 30m, ricerca v5.1 FDAX-30M-BO-66b8eb";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "FDAX-30M-BO-66b8eb";

    public PT8DAV_FDAX_SBO_002_30()
    {
        TradingWindow = ResearchWindow(12, 16); // start_hour 12, end_hour 16, verbatim
        Sessions = 5;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0.02m;                      // breakout_offset_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 3;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 28;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.7m;                       // take_profit_atr
    }
}
