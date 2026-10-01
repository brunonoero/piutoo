using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_CL_BOS_002_240</b> — BO_S su CL 4h, codice della ricerca <c>CL-4H-BOS-266dca</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 5 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 90 trade, netto 118,295; fuori campione 06/2022 → 05/2025 netto
/// 538; broker (16/09/2025 → 09/09/2026) 14 trade, netto -7,973.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/CL-4H-BOS-266dca.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura del massimo della sessione in corso.* Compra quando il prezzo supera il massimo che la giornata di oggi ha fatto finora. Il livello si alza durante la sessione, mano a mano che il massimo sale.</para>
/// <para>Da sapere. Non esiste sul giornaliero: una sessione e' una barra, e il massimo in costruzione non vuol dire niente.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $118,295 su 90 trade · $1,314 a trade · drawdown $11,286 · netto/DD 10.48 · anni in perdita 5 su 14 · notti a mercato 2.79 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $89,670 su 90 trade, drawdown $9,596.</para>
/// <para><b>Ordine STOP sul canale a 1 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo in costruzione della sessione corrente + 0.50 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo in costruzione della sessione corrente − 0.50 × ATR50</para>
/// <para>· Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 08:00 e 08:00, CET: ordini emessi sulle barre che chiudono fra le 08:00 e le 08:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di lunedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (30 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_CL_BOS_002_240 : Pt5DavCurrentSessionBreakoutEngine
{
    public override string Name => "PT8DAV_CL_BOS_002_240";

    public override string Description => "BO_S CL 4h, ricerca v5.1 CL-4H-BOS-266dca";

    public override string Symbol => "@CL";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "CL-4H-BOS-266dca";

    public PT8DAV_CL_BOS_002_240()
    {
        TradingWindow = ResearchWindow(8, 8);   // start_hour 8, end_hour 8, verbatim
        OffsetAtr = 0.5m;                       // breakout_offset_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 0;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 30;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
