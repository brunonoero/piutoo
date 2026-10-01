using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_TFM_001_240</b> — TF_M su ES 4h, codice della ricerca <c>ES-4H-TFM-77198b</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 112 trade, netto 85,944; fuori campione 06/2022 → 05/2025 netto
/// 9,462; broker (16/09/2025 → 09/09/2026) 13 trade, netto 1,774.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-4H-TFM-77198b.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Trend following simmetrico.* Compra quando il prezzo sale sopra il massimo di ieri, vende quando scende sotto il minimo di ieri. L'idea e' che chi rompe l'estremo del giorno prima continui nella stessa direzione.</para>
/// <para>Da sapere. Long e short usano lo stesso pattern a specchio: non si possono filtrare in modo indipendente. Se serve, il motore da usare e' TF_U.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $85,944 su 112 trade · $767 a trade · drawdown $17,956 · netto/DD 4.79 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $31,302 su 112 trade, drawdown $10,221.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 1: '|O_d1-C_d1| &lt; 0.1 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 18: '|O_d5-C_d1| &gt; 0.75 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_ES_TFM_001_240 : Pt5DavTfMirroredEngine
{
    public override string Name => "PT8DAV_ES_TFM_001_240";

    public override string Description => "TF_M ES 4h, ricerca v5.1 ES-4H-TFM-77198b";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "ES-4H-TFM-77198b";

    public PT8DAV_ES_TFM_001_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        NeutralYes = 1;                         // ptn_neut_yes
        NeutralNo = 18;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 1.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
