using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_GC_RBM_002_60</b> — RBB_M su GC 1h, codice della ricerca <c>GC-1H-RBBM-c99262</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 1457 trade, netto 229,577; fuori campione 06/2022 → 05/2025 netto
/// 8,485; broker (16/09/2025 → 09/09/2026) 148 trade, netto -32,881.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/GC-1H-RBBM-c99262.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, simmetrico.* Scommette contro il movimento: quando il prezzo si allontana dalla media, mette un ordine a prezzo migliore sulla banda, aspettando che il prezzo torni indietro.</para>
/// <para>Da sapere. I pattern direzionali entrano col segno INVERTITO rispetto ai motori di trend: qui un pattern rialzista e' un motivo per vendere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $229,577 su 1457 trade · $158 a trade · drawdown $50,017 · netto/DD 4.59 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $74,304 su 1457 trade, drawdown $26,484.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (14 barre, 1.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 28: 'H_d0 &lt; H_d1 * (1 - 0.005)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 28: 'L_d0 &gt; L_d1 * (1 + 0.005)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 09:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 09:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di giovedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.00 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_GC_RBM_002_60 : Pt5DavBollingerMirroredEngine
{
    public override string Name => "PT8DAV_GC_RBM_002_60";

    public override string Description => "RBB_M GC 1h, ricerca v5.1 GC-1H-RBBM-c99262";

    public override string Symbol => "@GC";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "GC-1H-RBBM-c99262";

    public PT8DAV_GC_RBM_002_60()
    {
        TradingWindow = ResearchWindow(-1, 9);  // start_hour -1, end_hour 9, verbatim
        BollingerLength = 14;                   // bb_length
        BollingerNumDevs = 1.5m;                // bb_num_devs
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 28;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 3;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1m;                         // take_profit_atr
    }
}
