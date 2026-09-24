using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_KC_LFD_001_15</b> — LF su KC 15m, codice della ricerca <c>KC-15M-LF-dc9d32</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 67 trade, netto $91,272; broker (27/08/2025 → 09/09/2026)
/// 7 trade, netto $-1,578. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/KC-15M-LF-dc9d32.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $91,272 su 67 trade · $1,362 a trade · drawdown $5,308 · netto/DD 17.20 · anni in perdita 4 su 14 · notti a mercato 2.31 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $44,987 su 67 trade, drawdown $2,634.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1; livello SHORT = R1</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 5: '|O_d1-C_d1| &gt; 0.25 * (H_d1-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale -9: 'O_d0 - L_d0 &lt; O_d1 - L_d1'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale -9: 'H_d0 - O_d0 &lt; H_d1 - O_d1'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 19:00 e 11:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 19:00 e le 11:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 04:20-13:30, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (74 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.40 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_KC_LFD_001_15 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT5DAV_KC_LFD_001_15";

    public override string Description => "LF KC 15m, ricerca PT5DAV KC-15M-LF-dc9d32";

    public override string Symbol => "@KC";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "KC-15M-LF-dc9d32";

    public PT5DAV_KC_LFD_001_15()
    {
        TradingWindow = ResearchWindow(19, 11); // start_hour 19, end_hour 11, verbatim
        LevelShiftAtr = 0m;                     // level_shift_atr
        NeutralYes = 5;                         // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = -9;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 74;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.4m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
