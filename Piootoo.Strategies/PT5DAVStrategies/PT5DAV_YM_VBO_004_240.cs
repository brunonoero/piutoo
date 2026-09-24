using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_YM_VBO_004_240</b> — VBO su YM 4h, codice della ricerca <c>YM-4H-VBO-06a4c2</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 72 trade, netto $85,476; broker (27/08/2025 → 09/09/2026)
/// 3 trade, netto $17,083. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/YM-4H-VBO-06a4c2.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $85,476 su 72 trade · $1,187 a trade · drawdown $15,373 · netto/DD 5.56 · anni in perdita 3 su 12 · notti a mercato 2.57 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $42,788 su 72 trade, drawdown $8,855.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.5 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 6 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 32: '(H_d0-L_d0) &gt; L_d0 * 0.0075'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 04:00 e 08:00, CET: ordini emessi sulle barre che chiudono fra le 04:00 e le 08:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (18 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_YM_VBO_004_240 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT5DAV_YM_VBO_004_240";

    public override string Description => "VBO YM 4h, ricerca PT5DAV YM-4H-VBO-06a4c2";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-VBO-06a4c2";

    public PT5DAV_YM_VBO_004_240()
    {
        TradingWindow = ResearchWindow(4, 8);   // start_hour 4, end_hour 8, verbatim
        VolatilitySource = 1;                   // vol_source: 1 range d1, 3 ATR di barra
        AtrLength = 5;                          // atr_len (letto solo con vol_source 3)
        MultiplierLong = 0.5m;                  // vol_mult
        MultiplierShort = 6m;                   // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 32;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 18;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
