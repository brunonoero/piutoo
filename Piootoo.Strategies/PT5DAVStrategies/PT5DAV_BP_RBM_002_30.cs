using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_RBM_002_30</b> — RBB_M su BP 30m, codice della ricerca <c>BP-30M-RBBM-014ead</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 1539 trade, netto $35,954; broker (27/08/2025 → 09/09/2026)
/// 109 trade, netto $3,398. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-30M-RBBM-014ead.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, simmetrico.* Scommette contro il movimento: quando il prezzo si allontana dalla media, mette un ordine a prezzo migliore sulla banda, aspettando che il prezzo torni indietro.</para>
/// <para>Da sapere. I pattern direzionali entrano col segno INVERTITO rispetto ai motori di trend: qui un pattern rialzista e' un motivo per vendere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $35,954 su 1539 trade · $23 a trade · drawdown $16,380 · netto/DD 2.19 · anni in perdita 4 su 14 · notti a mercato 1.40 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $27,476 su 1539 trade, drawdown $19,114.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (10 barre, 2.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 06:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 06:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di lunedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (230 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BP_RBM_002_30 : Pt5DavBollingerMirroredEngine
{
    public override string Name => "PT5DAV_BP_RBM_002_30";

    public override string Description => "RBB_M BP 30m, ricerca PT5DAV BP-30M-RBBM-014ead";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "BP-30M-RBBM-014ead";

    public PT5DAV_BP_RBM_002_30()
    {
        TradingWindow = ResearchWindow(-1, 6);  // start_hour -1, end_hour 6, verbatim
        BollingerLength = 10;                   // bb_length
        BollingerNumDevs = 2.5m;                // bb_num_devs
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 0;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 230;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
