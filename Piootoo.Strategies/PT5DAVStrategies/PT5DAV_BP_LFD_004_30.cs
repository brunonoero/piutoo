using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_LFD_004_30</b> — LF su BP 30m, codice della ricerca <c>BP-30M-LF-68837b</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 400 trade, netto $38,291; broker (27/08/2025 → 09/09/2026)
/// 39 trade, netto $-1,110. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-30M-LF-68837b.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $38,291 su 400 trade · $96 a trade · drawdown $3,369 · netto/DD 11.37 · anni in perdita 1 su 14 · notti a mercato 1.98 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $33,407 su 400 trade, drawdown $4,282.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1 − 0.50 × ATR50; livello SHORT = R1 + 0.50 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 14:00 e 10:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 14:00 e le 10:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (92 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.40 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BP_LFD_004_30 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT5DAV_BP_LFD_004_30";

    public override string Description => "LF BP 30m, ricerca PT5DAV BP-30M-LF-68837b";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "BP-30M-LF-68837b";

    public PT5DAV_BP_LFD_004_30()
    {
        TradingWindow = ResearchWindow(14, 10); // start_hour 14, end_hour 10, verbatim
        LevelShiftAtr = 0.5m;                   // level_shift_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 92;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.4m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
