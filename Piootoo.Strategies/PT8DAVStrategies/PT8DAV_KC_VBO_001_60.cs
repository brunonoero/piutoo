using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_KC_VBO_001_60</b> — VBO su KC 1h, codice della ricerca <c>KC-1H-VBO-eb33b9</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 227 trade, netto 79,727; fuori campione 06/2022 → 05/2025 netto
/// 9,279; broker (16/09/2025 → 09/09/2026) 26 trade, netto -10,211.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/KC-1H-VBO-eb33b9.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $79,727 su 227 trade · $351 a trade · drawdown $19,551 · netto/DD 4.08 · anni in perdita 4 su 14 · notti a mercato 0.82 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $45,480 su 227 trade, drawdown $16,652.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.5 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 0.5 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 29: 'L_d0 &gt; L_d1 * (1 + 0.01)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 29: 'H_d0 &lt; H_d1 * (1 - 0.01)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 04:20-13:30, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (10 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.65 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_KC_VBO_001_60 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_KC_VBO_001_60";

    public override string Description => "VBO KC 1h, ricerca v5.1 KC-1H-VBO-eb33b9";

    public override string Symbol => "@KC";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "KC-1H-VBO-eb33b9";

    public PT8DAV_KC_VBO_001_60()
    {
        TradingWindow = ResearchWindow(-1, 12); // start_hour -1, end_hour 12, verbatim
        VolatilitySource = 1;                   // vol_source: range della sessione d1
        AtrLength = 14;                         // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.5m;                  // vol_mult
        MultiplierShort = -1m;                  // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 29;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 2;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 10;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.65m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
