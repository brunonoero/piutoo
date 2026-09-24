using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_KC_VBO_001_15</b> — VBO su KC 15m, codice della ricerca <c>KC-15M-VBO-eb0cb9</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 87 trade, netto $68,118; broker (27/08/2025 → 09/09/2026)
/// 4 trade, netto $-3,602. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/KC-15M-VBO-eb0cb9.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $68,118 su 87 trade · $783 a trade · drawdown $6,478 · netto/DD 10.52 · anni in perdita 1 su 13 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $30,420 su 87 trade, drawdown $2,026.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.5 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 0.3 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para>· Solo short: il lato long non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 26: '|O_d5-C_d1| &lt; 0.75 * (HH5-LL5)'</para>
/// <para>· deve essere FALSO — neutrale 33: '(H_d0-L_d0) &gt; L_d0 * 0.01'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &gt; C_d2'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &lt; C_d2'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 12:00 e 13:00, CET: ordini emessi sulle barre che chiudono fra le 12:00 e le 13:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 04:20-13:30, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 13:30 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.00 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_KC_VBO_001_15 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT5DAV_KC_VBO_001_15";

    public override string Description => "VBO KC 15m, ricerca PT5DAV KC-15M-VBO-eb0cb9";

    public override string Symbol => "@KC";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "KC-15M-VBO-eb0cb9";

    public PT5DAV_KC_VBO_001_15()
    {
        TradingWindow = ResearchWindow(12, 13); // start_hour 12, end_hour 13, verbatim
        VolatilitySource = 1;                   // vol_source: 1 range d1, 3 ATR di barra
        AtrLength = 5;                          // atr_len (letto solo con vol_source 3)
        MultiplierLong = 0.5m;                  // vol_mult
        MultiplierShort = 0.3m;                 // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 2;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 26;                        // ptn_neut_yes
        NeutralNo = 33;                         // ptn_neut_no
        DirectionalYes = 13;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 1m;                           // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
