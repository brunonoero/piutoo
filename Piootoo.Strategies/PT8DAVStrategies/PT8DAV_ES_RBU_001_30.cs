using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_RBU_001_30</b> — RBB_U su ES 30m, codice della ricerca <c>ES-30M-RBBU-101fb7</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 1738 trade, netto 310,435; fuori campione 06/2022 → 05/2025 netto
/// 77,365; broker (16/09/2025 → 09/09/2026) 135 trade, netto -27,469.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-30M-RBBU-101fb7.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, lati indipendenti.* Stesso ingresso del RBB_M, con filtri separati per long e short.</para>
/// <para>Da sapere. Come per il TF_U, due insiemi di filtri = piu' spazio di ricerca = piu' rischio di trovare un caso fortunato, e un lato si puo' spegnere mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $310,435 su 1738 trade · $179 a trade · drawdown $70,217 · netto/DD 4.42 · anni in perdita 2 su 14 · notti a mercato 0.63 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $79,618 su 1738 trade, drawdown $48,098.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (50 barre, 2.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 18:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 18:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di venerdì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (46 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.40 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_ES_RBU_001_30 : Pt5DavBollingerUnmirroredEngine
{
    public override string Name => "PT8DAV_ES_RBU_001_30";

    public override string Description => "RBB_U ES 30m, ricerca v5.1 ES-30M-RBBU-101fb7";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "ES-30M-RBBU-101fb7";

    public PT8DAV_ES_RBU_001_30()
    {
        TradingWindow = ResearchWindow(-1, 18); // start_hour -1, end_hour 18, verbatim
        BollingerLength = 50;                   // bb_length
        BollingerNumDevs = 2.5m;                // bb_num_devs
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = 4;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 46;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.4m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
