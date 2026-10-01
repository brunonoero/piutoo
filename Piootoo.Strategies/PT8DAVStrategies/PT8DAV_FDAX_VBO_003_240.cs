using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_FDAX_VBO_003_240</b> — VBO su FDAX 4h, codice della ricerca <c>FDAX-4H-VBO-8b2691</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 457 trade, netto 600,205; fuori campione 06/2022 → 05/2025 netto
/// 42,589; broker (16/09/2025 → 09/09/2026) 30 trade, netto 15,122.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/FDAX-4H-VBO-8b2691.csv</c>.</para>
///
/// <para><b>Barre a 4 ore piegate dal motore.</b> La ricerca ha costruito questa strategia su barre
/// 08-12, 12-16, 16-20, 20-22 (ora di Roma), che nel feed non esistono: la griglia a 4 ore del DAX
/// e' ancorata all'01:00, quella delle PT3B. La classe riceve quindi la serie <b>a 60 minuti</b>
/// (<c>TimeframeMinutes</c>) e ragiona su barre da 240 (<c>BarMinutes</c>) che il motore si
/// costruisce: vedi <see cref="Pt5DavEngineBase.BarMinutes"/>. Il nome dice la barra della strategia,
/// non la serie.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $600,205 su 457 trade · $1,313 a trade · drawdown $48,816 · netto/DD 12.30 · anni in perdita 2 su 14 · notti a mercato 1.13 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $235,673 su 457 trade, drawdown $23,146.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = l'ATR a 14 barre del timeframe, misurato fino alla barra precedente compresa</para>
/// <para>· LONG: stop buy a O_d0 + 0.5 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 2 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 32: '(H_d0-L_d0) &gt; L_d0 * 0.0075'</para>
/// <para>· deve essere FALSO — neutrale 6: '|O_d1-C_d1| &gt; 0.5 * (H_d1-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (4 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.75 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.75 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT8DAV_FDAX_VBO_003_240 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_FDAX_VBO_003_240";

    public override string Description => "VBO FDAX 4h, ricerca v5.1 FDAX-4H-VBO-8b2691";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 60;

    public override int BarMinutes => 240;

    public override string ResearchCode => "FDAX-4H-VBO-8b2691";

    public PT8DAV_FDAX_VBO_003_240()
    {
        TradingWindow = ResearchWindow(-1, 12); // start_hour -1, end_hour 12, verbatim
        VolatilitySource = 3;                   // vol_source: ATR su atr_len barre
        AtrLength = 14;                         // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.5m;                  // vol_mult
        MultiplierShort = 2m;                   // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 32;                        // ptn_neut_yes
        NeutralNo = 6;                          // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 2;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 4;                            // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.75m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.75m;                      // take_profit_atr
    }
}
