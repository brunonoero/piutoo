using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_LFD_001_60</b> — LF su ES 1h, codice della ricerca <c>ES-1H-LF-96f483</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>overnight 5 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 359 trade, netto 477,723; fuori campione 06/2022 → 05/2025 netto
/// 50,786; broker (16/09/2025 → 09/09/2026) 30 trade, netto -49,191.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-1H-LF-96f483.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $477,723 su 359 trade · $1,331 a trade · drawdown $43,502 · netto/DD 10.98 · anni in perdita 3 su 14 · notti a mercato 6.74 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $216,340 su 359 trade, drawdown $21,030.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1 − 0.05 × ATR50; livello SHORT = R1 + 0.05 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 31: '(H_d0-L_d0) &gt; L_d0 * 0.005'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 01:00 e 19:00, CET: ordini emessi sulle barre che chiudono fra le 01:00 e le 19:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni SHORT di mercoledì</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (115 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_ES_LFD_001_60 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT8DAV_ES_LFD_001_60";

    public override string Description => "LF ES 1h, ricerca v5.1 ES-1H-LF-96f483";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "ES-1H-LF-96f483";

    public PT8DAV_ES_LFD_001_60()
    {
        TradingWindow = ResearchWindow(1, 19);  // start_hour 1, end_hour 19, verbatim
        LevelShiftAtr = 0.05m;                  // level_shift_atr
        NeutralYes = 31;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = 2;                   // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 115;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
