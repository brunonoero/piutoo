using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_BP_VBO_001_15</b> — VBO su BP 15m, codice della ricerca <c>BP-15M-VBO-366341</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 5 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 508 trade, netto 51,421; fuori campione 06/2022 → 05/2025 netto
/// 16,107; broker (16/09/2025 → 09/09/2026) 38 trade, netto -836.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/BP-15M-VBO-366341.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Rottura di volatilita' dall'apertura.* Prende l'apertura della sessione e ci aggiunge (o sottrae) una frazione di quanto il mercato si muove di solito. Se il prezzo copre quella distanza, entra.</para>
/// <para>Da sapere. Il long e lo short possono avere moltiplicatori diversi: una VBO puo' essere molto piu' facile da innescare al rialzo che al ribasso.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $51,421 su 508 trade · $101 a trade · drawdown $11,823 · netto/DD 4.35 · anni in perdita 2 su 14 · notti a mercato 6.83 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $53,855 su 508 trade, drawdown $11,139.</para>
/// <para><b>Ordine STOP sull'apertura di sessione più un multiplo di volatilità</b></para>
/// <para>· Sia 'VOL' = il range della sessione precedente: 'H_d1 − L_d1'</para>
/// <para>· LONG: stop buy a O_d0 + 0.5 × VOL</para>
/// <para>· SHORT: stop sell a O_d0 − 0.5 × VOL</para>
/// <para>· 'O_d0' è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 12:00 e 13:00, CET: ordini emessi sulle barre che chiudono fra le 12:00 e le 13:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (460 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_BP_VBO_001_15 : Pt5DavVolatilityBreakoutEngine
{
    public override string Name => "PT8DAV_BP_VBO_001_15";

    public override string Description => "VBO BP 15m, ricerca v5.1 BP-15M-VBO-366341";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "BP-15M-VBO-366341";

    public PT8DAV_BP_VBO_001_15()
    {
        TradingWindow = ResearchWindow(12, 13); // start_hour 12, end_hour 13, verbatim
        VolatilitySource = 1;                   // vol_source: range della sessione d1
        AtrLength = 14;                         // atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)
        MultiplierLong = 0.5m;                  // vol_mult
        MultiplierShort = -1m;                  // vol_mult_short (-1 = come il long)
        Momentum = 0;                           // momentum
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 460;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
