using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_LFH_002_60</b> — LF_HL su YM 1h, codice della ricerca <c>YM-1H-LFHL-1bef3a</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>overnight 10 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 59 trade, netto 289,466; fuori campione 06/2022 → 05/2025 netto
/// 33,632; broker (16/09/2025 → 09/09/2026) 2 trade, netto -2,822.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-1H-LFHL-1bef3a.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sugli estremi di ieri.* Identico al LF, ma i livelli da sfondare e riattraversare sono il massimo e il minimo di ieri invece dei pivot.</para>
/// <para>Da sapere. Stesso codice del LF: l'unica differenza e' quale livello si guarda.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $289,466 su 59 trade · $4,906 a trade · drawdown $12,201 · netto/DD 23.72 · anni in perdita 1 su 14 · notti a mercato 8.78 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $135,998 su 59 trade, drawdown $5,526.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· livello LONG = L_d1 − 0.20 × ATR50; livello SHORT = H_d1 + 0.20 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 17: '|O_d5-C_d1| &gt; 0.5 * (H_d5-L_d1)'</para>
/// <para>· deve essere FALSO — neutrale 26: '|O_d5-C_d1| &lt; 0.75 * (HH5-LL5)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 13:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 13:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (230 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.65 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_YM_LFH_002_60 : Pt5DavHighLowFaderEngine
{
    public override string Name => "PT8DAV_YM_LFH_002_60";

    public override string Description => "LF_HL YM 1h, ricerca v5.1 YM-1H-LFHL-1bef3a";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "YM-1H-LFHL-1bef3a";

    public PT8DAV_YM_LFH_002_60()
    {
        TradingWindow = ResearchWindow(-1, 13); // start_hour -1, end_hour 13, verbatim
        LevelShiftAtr = 0.2m;                   // level_shift_atr
        NeutralYes = 17;                        // ptn_neut_yes
        NeutralNo = 26;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 230;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.65m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
