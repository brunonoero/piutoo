using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_FDAX_LFD_001_30</b> — LF su FDAX 30m, codice della ricerca <c>FDAX-30M-LF-b84c31</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 373 trade, netto 425,308; fuori campione 06/2022 → 05/2025 netto
/// 39,112; broker (16/09/2025 → 09/09/2026) 30 trade, netto 5,710.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/FDAX-30M-LF-b84c31.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $425,308 su 373 trade · $1,140 a trade · drawdown $83,245 · netto/DD 5.11 · anni in perdita 3 su 14 · notti a mercato 1.27 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $182,788 su 373 trade, drawdown $47,053.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1 − 0.20 × ATR50; livello SHORT = R1 + 0.20 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 5: '|O_d1-C_d1| &gt; 0.25 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 40: '(H_d0-L_d0) &lt; L_d0 * 0.01'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 10:00 e 18:00, CET: ordini emessi sulle barre che chiudono fra le 10:00 e le 18:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (28 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.65 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_FDAX_LFD_001_30 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT8DAV_FDAX_LFD_001_30";

    public override string Description => "LF FDAX 30m, ricerca v5.1 FDAX-30M-LF-b84c31";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "FDAX-30M-LF-b84c31";

    public PT8DAV_FDAX_LFD_001_30()
    {
        TradingWindow = ResearchWindow(10, 18); // start_hour 10, end_hour 18, verbatim
        LevelShiftAtr = 0.2m;                   // level_shift_atr
        NeutralYes = 5;                         // ptn_neut_yes
        NeutralNo = 40;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 28;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.65m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
