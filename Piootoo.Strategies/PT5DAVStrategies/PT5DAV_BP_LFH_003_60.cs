using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_LFH_003_60</b> — LF_HL su BP 1h, codice della ricerca <c>BP-1H-LFHL-a6a2ea</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 346 trade, netto $26,570; broker (27/08/2025 → 09/09/2026)
/// 30 trade, netto $-391. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-1H-LFHL-a6a2ea.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Fade del falso sfondamento, sugli estremi di ieri.* Identico al LF, ma i livelli da sfondare e riattraversare sono il massimo e il minimo di ieri invece dei pivot.</para>
/// <para>Da sapere. Stesso codice del LF: l'unica differenza e' quale livello si guarda.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $26,570 su 346 trade · $77 a trade · drawdown $3,003 · netto/DD 8.85 · anni in perdita 2 su 14 · notti a mercato 1.05 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $22,282 su 346 trade, drawdown $3,379.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· livello LONG = L_d1 − 0.40 × ATR50; livello SHORT = H_d1 + 0.40 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 16:00 e 05:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 16:00 e le 05:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni LONG di mercoledì</para>
/// <para>· Non apre posizioni SHORT di mercoledì</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (23 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BP_LFH_003_60 : Pt5DavHighLowFaderEngine
{
    public override string Name => "PT5DAV_BP_LFH_003_60";

    public override string Description => "LF_HL BP 1h, ricerca PT5DAV BP-1H-LFHL-a6a2ea";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "BP-1H-LFHL-a6a2ea";

    public PT5DAV_BP_LFH_003_60()
    {
        TradingWindow = ResearchWindow(16, 5);  // start_hour 16, end_hour 5, verbatim
        LevelShiftAtr = 0.4m;                   // level_shift_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = 2;                    // not_le_day, pandas
        NotEntryDayShort = 2;                   // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 23;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
