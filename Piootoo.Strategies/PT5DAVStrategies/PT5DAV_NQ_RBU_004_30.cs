using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_RBU_004_30</b> — RBB_U su NQ 30m, codice della ricerca <c>NQ-30M-RBBU-eb1d0a</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 359 trade, netto $647,899; broker (27/08/2025 → 09/09/2026)
/// 30 trade, netto $28,119. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-30M-RBBU-eb1d0a.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, lati indipendenti.* Stesso ingresso del RBB_M, con filtri separati per long e short.</para>
/// <para>Da sapere. Come per il TF_U, due insiemi di filtri = piu' spazio di ricerca = piu' rischio di trovare un caso fortunato, e un lato si puo' spegnere mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $647,899 su 359 trade · $1,805 a trade · drawdown $131,559 · netto/DD 4.92 · anni in perdita 4 su 14 · notti a mercato 6.49 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $294,267 su 359 trade, drawdown $64,873.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (30 barre, 2 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 03:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 03:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (230 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_NQ_RBU_004_30 : Pt5DavBollingerUnmirroredEngine
{
    public override string Name => "PT5DAV_NQ_RBU_004_30";

    public override string Description => "RBB_U NQ 30m, ricerca PT5DAV NQ-30M-RBBU-eb1d0a";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "NQ-30M-RBBU-eb1d0a";

    public PT5DAV_NQ_RBU_004_30()
    {
        TradingWindow = ResearchWindow(-1, 3);  // start_hour -1, end_hour 3, verbatim
        BollingerLength = 30;                   // bb_length
        BollingerNumDevs = 2m;                  // bb_num_devs
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 230;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
