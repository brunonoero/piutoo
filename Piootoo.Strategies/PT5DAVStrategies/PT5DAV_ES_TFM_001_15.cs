using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_ES_TFM_001_15</b> — TF_M su ES 15m, codice della ricerca <c>ES-15M-TFM-8a67d7</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 2240 trade, netto $284,668; broker (27/08/2025 → 09/09/2026)
/// 167 trade, netto $-23,893. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/ES-15M-TFM-8a67d7.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Trend following simmetrico.* Compra quando il prezzo sale sopra il massimo di ieri, vende quando scende sotto il minimo di ieri. L'idea e' che chi rompe l'estremo del giorno prima continui nella stessa direzione.</para>
/// <para>Da sapere. Long e short usano lo stesso pattern a specchio: non si possono filtrare in modo indipendente. Se serve, il motore da usare e' TF_U.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $284,668 su 2240 trade · $127 a trade · drawdown $75,407 · netto/DD 3.78 · anni in perdita 4 su 14 · notti a mercato 1.37 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $134,071 su 2240 trade, drawdown $36,959.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 17:00 e 23:59, CET: ordini emessi sulle barre che chiudono fra le 17:00 e le 23:59 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (184 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.15 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.35 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_ES_TFM_001_15 : Pt5DavTfMirroredEngine
{
    public override string Name => "PT5DAV_ES_TFM_001_15";

    public override string Description => "TF_M ES 15m, ricerca PT5DAV ES-15M-TFM-8a67d7";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "ES-15M-TFM-8a67d7";

    public PT5DAV_ES_TFM_001_15()
    {
        TradingWindow = ResearchWindow(17, -1); // start_hour 17, end_hour -1, verbatim
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 184;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.15m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.35m;                      // take_profit_atr
    }
}
