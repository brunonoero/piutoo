using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_LFD_004_240</b> — LF su NQ 4h, codice della ricerca <c>NQ-4H-LF-89bec8</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 262 trade, netto $368,310; broker (27/08/2025 → 09/09/2026)
/// 33 trade, netto $26,229. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-4H-LF-89bec8.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $368,310 su 262 trade · $1,406 a trade · drawdown $41,442 · netto/DD 8.89 · anni in perdita 2 su 14 · notti a mercato 2.14 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $144,275 su 262 trade, drawdown $20,228.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1 − 0.10 × ATR50; livello SHORT = R1 + 0.10 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni SHORT di venerdì</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (18 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.90 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 0.75 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_NQ_LFD_004_240 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT5DAV_NQ_LFD_004_240";

    public override string Description => "LF NQ 4h, ricerca PT5DAV NQ-4H-LF-89bec8";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "NQ-4H-LF-89bec8";

    public PT5DAV_NQ_LFD_004_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        LevelShiftAtr = 0.1m;                   // level_shift_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = 4;                   // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 18;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.9m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0.75m;                      // take_profit_atr
    }
}
