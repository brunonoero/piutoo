using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_BOS_001_240</b> — BO_S su BTC 4h, codice della ricerca <c>BTC-4H-BOS-a0b818</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 485 trade, netto $1,782,424; broker (27/08/2025 → 09/09/2026)
/// 103 trade, netto $112,109. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-4H-BOS-a0b818.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura del massimo della sessione in corso.* Compra quando il prezzo supera il massimo che la giornata di oggi ha fatto finora. Il livello si alza durante la sessione, mano a mano che il massimo sale.</para>
/// <para>Da sapere. Non esiste sul giornaliero: una sessione e' una barra, e il massimo in costruzione non vuol dire niente.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $1,782,424 su 485 trade · $3,675 a trade · drawdown $228,961 · netto/DD 7.78 · anni in perdita 2 su 8 · notti a mercato 3.38 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $1,040,188 su 485 trade, drawdown $153,453.</para>
/// <para><b>Ordine STOP sul canale a 1 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo in costruzione della sessione corrente</para>
/// <para>· SHORT: stop sell sul minimo in costruzione della sessione corrente</para>
/// <para>· Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 04:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 04:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di martedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (18 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.80 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 3.00 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_BTC_BOS_001_240 : Pt5DavCurrentSessionBreakoutEngine
{
    public override string Name => "PT5DAV_BTC_BOS_001_240";

    public override string Description => "BO_S BTC 4h, ricerca PT5DAV BTC-4H-BOS-a0b818";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BTC-4H-BOS-a0b818";

    public PT5DAV_BTC_BOS_001_240()
    {
        TradingWindow = ResearchWindow(-1, 4);  // start_hour -1, end_hour 4, verbatim
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 1;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 18;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.8m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 3m;                         // take_profit_atr
    }
}
