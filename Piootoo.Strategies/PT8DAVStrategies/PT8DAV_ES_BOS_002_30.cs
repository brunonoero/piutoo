using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_BOS_002_30</b> — BO_S su ES 30m, codice della ricerca <c>ES-30M-BOS-de468f</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 347 trade, netto 245,318; fuori campione 06/2022 → 05/2025 netto
/// 5,252; broker (16/09/2025 → 09/09/2026) 38 trade, netto -46,287.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-30M-BOS-de468f.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del massimo della sessione in corso.* Compra quando il prezzo supera il massimo che la giornata di oggi ha fatto finora. Il livello si alza durante la sessione, mano a mano che il massimo sale.</para>
/// <para>Da sapere. Non esiste sul giornaliero: una sessione e' una barra, e il massimo in costruzione non vuol dire niente.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $245,318 su 347 trade · $707 a trade · drawdown $52,376 · netto/DD 4.68 · anni in perdita 3 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $76,731 su 347 trade, drawdown $25,303.</para>
/// <para><b>Ordine STOP sul canale a 1 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo in costruzione della sessione corrente + 0.10 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo in costruzione della sessione corrente − 0.10 × ATR50</para>
/// <para>· Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 28: '|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)'</para>
/// <para>· deve essere FALSO — neutrale 21: '|O_d5-C_d1| &gt; 2.0 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 01:00 e 07:00, CET: ordini emessi sulle barre che chiudono fra le 01:00 e le 07:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di giovedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.75 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.30 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_ES_BOS_002_30 : Pt5DavCurrentSessionBreakoutEngine
{
    public override string Name => "PT8DAV_ES_BOS_002_30";

    public override string Description => "BO_S ES 30m, ricerca v5.1 ES-30M-BOS-de468f";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "ES-30M-BOS-de468f";

    public PT8DAV_ES_BOS_002_30()
    {
        TradingWindow = ResearchWindow(1, 7);   // start_hour 1, end_hour 7, verbatim
        OffsetAtr = 0.1m;                       // breakout_offset_atr
        NeutralYes = 28;                        // ptn_neut_yes
        NeutralNo = 21;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 3;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.75m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.3m;                       // take_profit_atr
    }
}
