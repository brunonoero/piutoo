using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_VBO_002_240</b> — VBO su BTC 4h, codice della ricerca <c>BTC-4H-VBO-6983da</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 110 trade, netto $1,069,760; broker (27/08/2025 → 09/09/2026)
/// 26 trade, netto $-91,085. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-4H-VBO-6983da.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $1,069,760 su 110 trade · $9,725 a trade · drawdown $135,273 · netto/DD 7.91 · anni in perdita 0 su 8 · notti a mercato 5.17 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $362,815 su 110 trade, drawdown $101,227.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.7 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 9.5 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &gt; C_d2'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 13: 'C_d1 &lt; C_d2'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di martedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (30 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.90 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_VBO_002_240 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT5DAV_BTC_VBO_002_240";

    public override string Description => "VBO BTC 4h, ricerca PT5DAV BTC-4H-VBO-6983da";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BTC-4H-VBO-6983da";

    public PT5DAV_BTC_VBO_002_240()
    {
        TradingWindow = ResearchWindow(-1, 12); // start_hour -1, end_hour 12, verbatim
        VolatilitySource = 1;                   // vol_source: 1 range d1, 3 ATR di barra
        AtrLength = 5;                          // atr_len (letto solo con vol_source 3)
        MultiplierLong = 0.7m;                  // vol_mult
        MultiplierShort = 9.5m;                 // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 13;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 1;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 30;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.9m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
