using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_YM_RBU_002_240</b> — RBB_U su YM 4h, codice della ricerca <c>YM-4H-RBBU-ab5182</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 468 trade, netto $69,899; broker (27/08/2025 → 09/09/2026)
/// 35 trade, netto $218. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/YM-4H-RBBU-ab5182.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, lati indipendenti.* Stesso ingresso del RBB_M, con filtri separati per long e short.</para>
/// <para>Da sapere. Come per il TF_U, due insiemi di filtri = piu' spazio di ricerca = piu' rischio di trovare un caso fortunato, e un lato si puo' spegnere mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $69,899 su 468 trade · $149 a trade · drawdown $97,429 · netto/DD 0.72 · anni in perdita 4 su 14 · notti a mercato 6.31 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $75,200 su 468 trade, drawdown $36,140.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (50 barre, 2 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 12:00 e 23:59, CET: ordini emessi sulle barre che chiudono fra le 12:00 e le 23:59 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (30 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_YM_RBU_002_240 : Pt5DavBollingerUnmirroredEngine
{
    public override string Name => "PT5DAV_YM_RBU_002_240";

    public override string Description => "RBB_U YM 4h, ricerca PT5DAV YM-4H-RBBU-ab5182";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-RBBU-ab5182";

    public PT5DAV_YM_RBU_002_240()
    {
        TradingWindow = ResearchWindow(12, -1); // start_hour 12, end_hour -1, verbatim
        BollingerLength = 50;                   // bb_length
        BollingerNumDevs = 2m;                  // bb_num_devs
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 30;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
