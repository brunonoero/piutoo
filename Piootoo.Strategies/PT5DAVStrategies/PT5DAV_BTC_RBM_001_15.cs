using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_RBM_001_15</b> — RBB_M su BTC 15m, codice della ricerca <c>BTC-15M-RBBM-8b34eb</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 72 trade, netto $94,555; broker (27/08/2025 → 09/09/2026)
/// 287 trade, netto $-166,366. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-15M-RBBM-8b34eb.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, simmetrico.* Scommette contro il movimento: quando il prezzo si allontana dalla media, mette un ordine a prezzo migliore sulla banda, aspettando che il prezzo torni indietro.</para>
/// <para>Da sapere. I pattern direzionali entrano col segno INVERTITO rispetto ai motori di trend: qui un pattern rialzista e' un motivo per vendere.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $94,555 su 72 trade · $1,313 a trade · drawdown $27,596 · netto/DD 3.43 · anni in perdita 0 su 8 · notti a mercato 0.31 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $41,512 su 72 trade, drawdown $14,395.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (10 barre, 1.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 23:00 e 00:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 23:00 e le 00:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (92 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.30 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.35 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_BTC_RBM_001_15 : Pt5DavBollingerMirroredEngine
{
    public override string Name => "PT5DAV_BTC_RBM_001_15";

    public override string Description => "RBB_M BTC 15m, ricerca PT5DAV BTC-15M-RBBM-8b34eb";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "BTC-15M-RBBM-8b34eb";

    public PT5DAV_BTC_RBM_001_15()
    {
        TradingWindow = ResearchWindow(23, 0);  // start_hour 23, end_hour 0, verbatim
        BollingerLength = 10;                   // bb_length
        BollingerNumDevs = 1.5m;                // bb_num_devs
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 92;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.3m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.35m;                      // take_profit_atr
    }
}
