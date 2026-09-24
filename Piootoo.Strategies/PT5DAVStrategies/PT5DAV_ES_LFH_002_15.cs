using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_ES_LFH_002_15</b> — LF_HL su ES 15m, codice della ricerca <c>ES-15M-LFHL-712658</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 62 trade, netto $185,748; broker (27/08/2025 → 09/09/2026)
/// 3 trade, netto $16,486. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/ES-15M-LFHL-712658.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Fade del falso sfondamento, sugli estremi di ieri.* Identico al LF, ma i livelli da sfondare e riattraversare sono il massimo e il minimo di ieri invece dei pivot.</para>
/// <para>Da sapere. Stesso codice del LF: l'unica differenza e' quale livello si guarda.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $185,748 su 62 trade · $2,996 a trade · drawdown $23,430 · netto/DD 7.93 · anni in perdita 3 su 13 · notti a mercato 1.18 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $68,184 su 62 trade, drawdown $13,828.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· livello LONG = L_d1 − 0.30 × ATR50; livello SHORT = H_d1 + 0.30 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 14: '|O_d5-C_d1| &lt; 1.5 * (H_d5-L_d1)'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale -30: 'H_d0 &lt; H_d1 * (1 - 0.015)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale -30: 'L_d0 &gt; L_d1 * (1 + 0.015)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 10:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 10:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (184 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.40 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_ES_LFH_002_15 : Pt5DavHighLowFaderEngine
{
    public override string Name => "PT5DAV_ES_LFH_002_15";

    public override string Description => "LF_HL ES 15m, ricerca PT5DAV ES-15M-LFHL-712658";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "ES-15M-LFHL-712658";

    public PT5DAV_ES_LFH_002_15()
    {
        TradingWindow = ResearchWindow(-1, 10); // start_hour -1, end_hour 10, verbatim
        LevelShiftAtr = 0.3m;                   // level_shift_atr
        NeutralYes = 14;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = -30;                   // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 184;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.4m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
