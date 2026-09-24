using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_TFM_003_60</b> — TF_M su BTC 1h, codice della ricerca <c>BTC-1H-TFM-613144</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 476 trade, netto $859,576; broker (27/08/2025 → 09/09/2026)
/// 71 trade, netto $101,701. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-1H-TFM-613144.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Trend following simmetrico.* Compra quando il prezzo sale sopra il massimo di ieri, vende quando scende sotto il minimo di ieri. L'idea e' che chi rompe l'estremo del giorno prima continui nella stessa direzione.</para>
/// <para>Da sapere. Long e short usano lo stesso pattern a specchio: non si possono filtrare in modo indipendente. Se serve, il motore da usare e' TF_U.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $859,576 su 476 trade · $1,806 a trade · drawdown $235,471 · netto/DD 3.65 · anni in perdita 2 su 8 · notti a mercato 2.09 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $358,562 su 476 trade, drawdown $137,998.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 04:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 04:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di martedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (115 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.30 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_TFM_003_60 : Pt5DavTfMirroredEngine
{
    public override string Name => "PT5DAV_BTC_TFM_003_60";

    public override string Description => "TF_M BTC 1h, ricerca PT5DAV BTC-1H-TFM-613144";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "BTC-1H-TFM-613144";

    public PT5DAV_BTC_TFM_003_60()
    {
        TradingWindow = ResearchWindow(-1, 4);  // start_hour -1, end_hour 4, verbatim
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 1;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 115;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.3m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
