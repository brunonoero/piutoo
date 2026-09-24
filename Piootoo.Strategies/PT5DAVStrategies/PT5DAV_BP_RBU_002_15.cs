using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_RBU_002_15</b> — RBB_U su BP 15m, codice della ricerca <c>BP-15M-RBBU-75b198</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 1258 trade, netto $27,238; broker (27/08/2025 → 09/09/2026)
/// 90 trade, netto $-3,563. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-15M-RBBU-75b198.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, lati indipendenti.* Stesso ingresso del RBB_M, con filtri separati per long e short.</para>
/// <para>Da sapere. Come per il TF_U, due insiemi di filtri = piu' spazio di ricerca = piu' rischio di trovare un caso fortunato, e un lato si puo' spegnere mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $27,238 su 1258 trade · $22 a trade · drawdown $13,201 · netto/DD 2.06 · anni in perdita 3 su 14 · notti a mercato 2.23 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $21,928 su 1258 trade, drawdown $13,978.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (30 barre, 2.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 09:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 09:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di martedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (920 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BP_RBU_002_15 : Pt5DavBollingerUnmirroredEngine
{
    public override string Name => "PT5DAV_BP_RBU_002_15";

    public override string Description => "RBB_U BP 15m, ricerca PT5DAV BP-15M-RBBU-75b198";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "BP-15M-RBBU-75b198";

    public PT5DAV_BP_RBU_002_15()
    {
        TradingWindow = ResearchWindow(-1, 9);  // start_hour -1, end_hour 9, verbatim
        BollingerLength = 30;                   // bb_length
        BollingerNumDevs = 2.5m;                // bb_num_devs
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = 1;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 920;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
