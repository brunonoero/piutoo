using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_VBO_002_30</b> — VBO su ES 30m, codice della ricerca <c>ES-30M-VBO-a33891</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 192 trade, netto 86,956; fuori campione 06/2022 → 05/2025 netto
/// 2,258; broker (16/09/2025 → 09/09/2026) 21 trade, netto -615.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-30M-VBO-a33891.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $86,956 su 192 trade · $453 a trade · drawdown $20,560 · netto/DD 4.23 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $21,121 su 192 trade, drawdown $14,681.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.7 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 3 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 6: '|O_d1-C_d1| &gt; 0.5 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 39: '(H_d0-L_d0) &lt; L_d0 * 0.0075'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 16:00 e 20:00, CET: ordini emessi sulle barre che chiudono fra le 16:00 e le 20:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_ES_VBO_002_30 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_ES_VBO_002_30";

    public override string Description => "VBO ES 30m, ricerca v5.1 ES-30M-VBO-a33891";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "ES-30M-VBO-a33891";

    public PT8DAV_ES_VBO_002_30()
    {
        TradingWindow = ResearchWindow(16, 20); // start_hour 16, end_hour 20, verbatim
        VolatilitySource = 1;                   // vol_source: range della sessione d1
        AtrLength = 5;                          // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.7m;                  // vol_mult
        MultiplierShort = 3m;                   // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 6;                         // ptn_neut_yes
        NeutralNo = 39;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
