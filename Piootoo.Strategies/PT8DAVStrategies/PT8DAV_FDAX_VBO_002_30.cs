using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_FDAX_VBO_002_30</b> — VBO su FDAX 30m, codice della ricerca <c>FDAX-30M-VBO-5c3d7d</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 336 trade, netto 406,670; fuori campione 06/2022 → 05/2025 netto
/// 35,207; broker (16/09/2025 → 09/09/2026) 26 trade, netto 20,942.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/FDAX-30M-VBO-5c3d7d.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $406,670 su 336 trade · $1,210 a trade · drawdown $60,339 · netto/DD 6.74 · anni in perdita 1 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $153,470 su 336 trade, drawdown $23,561.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = l'ATR giornaliero a 5 periodi: l'ATR calcolato sulla serie delle SESSIONI complete — non sulle barre del timeframe — e riportato su ogni barra della sessione corrente</para>
/// <para>· LONG: stop buy a O_d0 + 0.3 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 8.5 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 26: '|O_d5-C_d1| &lt; 0.75 * (HH5-LL5)'</para>
/// <para>· deve essere FALSO — neutrale 39: '(H_d0-L_d0) &lt; L_d0 * 0.0075'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale -9: 'O_d0 - L_d0 &lt; O_d1 - L_d1'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale -9: 'H_d0 - O_d0 &lt; H_d1 - O_d1'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 10:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 10:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.75 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_FDAX_VBO_002_30 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_FDAX_VBO_002_30";

    public override string Description => "VBO FDAX 30m, ricerca v5.1 FDAX-30M-VBO-5c3d7d";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "FDAX-30M-VBO-5c3d7d";

    public PT8DAV_FDAX_VBO_002_30()
    {
        TradingWindow = ResearchWindow(-1, 10); // start_hour -1, end_hour 10, verbatim
        VolatilitySource = 2;                   // vol_source: ATR di Wilder su atr_len sessioni
        AtrLength = 5;                          // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.3m;                  // vol_mult
        MultiplierShort = 8.5m;                 // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 26;                        // ptn_neut_yes
        NeutralNo = 39;                         // ptn_neut_no
        DirectionalYes = -9;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.75m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
