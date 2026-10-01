using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_RBU_002_240</b> — RBB_U su NQ 4h, codice della ricerca <c>NQ-4H-RBBU-2d5d5a</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 140 trade, netto 140,640; fuori campione 06/2022 → 05/2025 netto
/// 8,398; broker (16/09/2025 → 09/09/2026) 14 trade, netto 11,955.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-4H-RBBU-2d5d5a.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, lati indipendenti.* Stesso ingresso del RBB_M, con filtri separati per long e short.</para>
/// <para>Da sapere. Come per il TF_U, due insiemi di filtri = piu' spazio di ricerca = piu' rischio di trovare un caso fortunato, e un lato si puo' spegnere mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $140,640 su 140 trade · $1,005 a trade · drawdown $22,505 · netto/DD 6.25 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $35,242 su 140 trade, drawdown $14,879.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (20 barre, 3 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 04:00 e 08:00, CET: ordini emessi sulle barre che chiudono fra le 04:00 e le 08:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_NQ_RBU_002_240 : Pt5DavBollingerUnmirroredEngine
{
    public override string Name => "PT8DAV_NQ_RBU_002_240";

    public override string Description => "RBB_U NQ 4h, ricerca v5.1 NQ-4H-RBBU-2d5d5a";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "NQ-4H-RBBU-2d5d5a";

    public PT8DAV_NQ_RBU_002_240()
    {
        TradingWindow = ResearchWindow(4, 8);   // start_hour 4, end_hour 8, verbatim
        BollingerLength = 20;                   // bb_length
        BollingerNumDevs = 3m;                  // bb_num_devs
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
