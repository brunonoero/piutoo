using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_GC_VBO_001_15</b> — VBO su GC 15m, codice della ricerca <c>GC-15M-VBO-3d44fd</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 530 trade, netto 372,779; fuori campione 06/2022 → 05/2025 netto
/// 74,940; broker (16/09/2025 → 09/09/2026) 27 trade, netto 110,416.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/GC-15M-VBO-3d44fd.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $372,779 su 530 trade · $703 a trade · drawdown $65,422 · netto/DD 5.70 · anni in perdita 2 su 14 · notti a mercato 1.02 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $145,937 su 530 trade, drawdown $26,478.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.7 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 2 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere FALSO — direzionale -15: 'C_d1 &lt; C_d2 * (1 - 0.005)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere FALSO — direzionale -15: 'C_d1 &gt; C_d2 * (1 + 0.005)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 03:00 e 16:00, CET: ordini emessi sulle barre che chiudono fra le 03:00 e le 16:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (92 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_GC_VBO_001_15 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_GC_VBO_001_15";

    public override string Description => "VBO GC 15m, ricerca v5.1 GC-15M-VBO-3d44fd";

    public override string Symbol => "@GC";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "GC-15M-VBO-3d44fd";

    public PT8DAV_GC_VBO_001_15()
    {
        TradingWindow = ResearchWindow(3, 16);  // start_hour 3, end_hour 16, verbatim
        VolatilitySource = 1;                   // vol_source: range della sessione d1
        AtrLength = 5;                          // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.7m;                  // vol_mult
        MultiplierShort = 2m;                   // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = -15;                    // ptn_dir_no
        SkipDay = 2;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 92;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
