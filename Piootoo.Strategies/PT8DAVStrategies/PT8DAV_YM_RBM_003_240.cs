using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_RBM_003_240</b> — RBB_M su YM 4h, codice della ricerca <c>YM-4H-RBBM-9917ea</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 266 trade, netto 116,581; fuori campione 06/2022 → 05/2025 netto
/// 6,031; broker (16/09/2025 → 09/09/2026) 23 trade, netto -1,340.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-4H-RBBM-9917ea.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sulle bande di Bollinger, simmetrico.* Scommette contro il movimento: quando il prezzo si allontana dalla media, mette un ordine a prezzo migliore sulla banda, aspettando che il prezzo torni indietro.</para>
/// <para>Da sapere. I pattern direzionali entrano col segno INVERTITO rispetto ai motori di trend: qui un pattern rialzista e' un motivo per vendere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $116,581 su 266 trade · $438 a trade · drawdown $24,504 · netto/DD 4.76 · anni in perdita 1 su 14 · notti a mercato 0.47 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $52,830 su 266 trade, drawdown $14,466.</para>
/// <para><b>Ordine LIMITE sulle bande di Bollinger (10 barre, 1.5 deviazioni)</b></para>
/// <para>· LONG: limit buy sulla banda inferiore, armato finché 'close &gt; banda_inf'</para>
/// <para>· SHORT: limit sell sulla banda superiore, armato finché 'close &lt; banda_sup'</para>
/// <para>· Il fill richiede penetrazione stretta del livello, non il semplice tocco.</para>
/// <para>· Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 31: '(H_d0-L_d0) &gt; L_d0 * 0.005'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 28: 'H_d0 &lt; H_d1 * (1 - 0.005)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 28: 'L_d0 &gt; L_d1 * (1 + 0.005)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 12:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 12:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di venerdì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (6 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.40 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.00 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT8DAV_YM_RBM_003_240 : Pt5DavBollingerMirroredEngine
{
    public override string Name => "PT8DAV_YM_RBM_003_240";

    public override string Description => "RBB_M YM 4h, ricerca v5.1 YM-4H-RBBM-9917ea";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-RBBM-9917ea";

    public PT8DAV_YM_RBM_003_240()
    {
        TradingWindow = ResearchWindow(12, 12); // start_hour 12, end_hour 12, verbatim
        BollingerLength = 10;                   // bb_length
        BollingerNumDevs = 1.5m;                // bb_num_devs
        NeutralYes = 31;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 28;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 4;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 6;                            // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.4m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1m;                         // take_profit_atr
    }
}
