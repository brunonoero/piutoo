using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_CL_VBO_002_60</b> — VBO su CL 1h, codice della ricerca <c>CL-1H-VBO-d7d890</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 51 trade, netto $112,093; broker (27/08/2025 → 09/09/2026)
/// 9 trade, netto $5,945. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/CL-1H-VBO-d7d890.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $112,093 su 51 trade · $2,198 a trade · drawdown $7,360 · netto/DD 15.23 · anni in perdita 3 su 12 · notti a mercato 0.92 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $51,198 su 51 trade, drawdown $5,160.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 1 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 0.7 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para>· Solo short: il lato long non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 37: '(H_d0-L_d0) &gt; L_d0 * 0.03'</para>
/// <para>· deve essere FALSO — neutrale 2: '|O_d1-C_d1| &lt; 0.25 * (H_d1-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 03:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 03:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (23 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_CL_VBO_002_60 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT5DAV_CL_VBO_002_60";

    public override string Description => "VBO CL 1h, ricerca PT5DAV CL-1H-VBO-d7d890";

    public override string Symbol => "@CL";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "CL-1H-VBO-d7d890";

    public PT5DAV_CL_VBO_002_60()
    {
        TradingWindow = ResearchWindow(3, 12);  // start_hour 3, end_hour 12, verbatim
        VolatilitySource = 1;                   // vol_source: 1 range d1, 3 ATR di barra
        AtrLength = 5;                          // atr_len (letto solo con vol_source 3)
        MultiplierLong = 1m;                    // vol_mult
        MultiplierShort = 0.7m;                 // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 2;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 37;                        // ptn_neut_yes
        NeutralNo = 2;                          // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 23;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
