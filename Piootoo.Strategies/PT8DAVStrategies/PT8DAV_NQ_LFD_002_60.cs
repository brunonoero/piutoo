using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_LFD_002_60</b> — LF su NQ 1h, codice della ricerca <c>NQ-1H-LF-25505f</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>overnight 1 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 162 trade, netto 457,637; fuori campione 06/2022 → 05/2025 netto
/// 67,695; broker (16/09/2025 → 09/09/2026) 26 trade, netto -41,087.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-1H-LF-25505f.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sui pivot.* Guarda i livelli di supporto e resistenza calcolati sulla sessione di ieri. Quando il prezzo li sfonda e poi li riattraversa dalla parte opposta, entra nella direzione del rientro: tratta lo sfondamento come falso.</para>
/// <para>Da sapere. E' l'unico motore che usa la libreria di pattern UAPtnBase, e la usa per lato. Il livello viene dai pivot di ieri; per la variante sugli estremi di ieri esiste LF_HL.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $457,637 su 162 trade · $2,825 a trade · drawdown $43,334 · netto/DD 10.56 · anni in perdita 3 su 14 · notti a mercato 1.29 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $143,138 su 162 trade, drawdown $21,636.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· 'pivot = (H_d1 + L_d1 + C_d1) / 3'</para>
/// <para>· 'R1 = 2 × pivot − L_d1'  e  'S1 = 2 × pivot − H_d1'</para>
/// <para>· livello LONG = S1 − 0.10 × ATR50; livello SHORT = R1 + 0.10 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 3: '|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 39: '(H_d0-L_d0) &lt; L_d0 * 0.0075'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 16:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 16:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 1 giornate (23 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.70 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT8DAV_NQ_LFD_002_60 : Pt5DavPivotFaderEngine
{
    public override string Name => "PT8DAV_NQ_LFD_002_60";

    public override string Description => "LF NQ 1h, ricerca v5.1 NQ-1H-LF-25505f";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "NQ-1H-LF-25505f";

    public PT8DAV_NQ_LFD_002_60()
    {
        TradingWindow = ResearchWindow(-1, 16); // start_hour -1, end_hour 16, verbatim
        LevelShiftAtr = 0.1m;                   // level_shift_atr
        NeutralYes = 3;                         // ptn_neut_yes
        NeutralNo = 39;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 1 giornate)
        MaxBars = 23;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.7m;                       // take_profit_atr
    }
}
