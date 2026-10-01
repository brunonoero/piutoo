using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_LFH_003_240</b> — LF_HL su YM 4h, codice della ricerca <c>YM-4H-LFHL-addb33</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>overnight 3 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 72 trade, netto 138,727; fuori campione 06/2022 → 05/2025 netto
/// 12,100; broker (16/09/2025 → 09/09/2026) 5 trade, netto 8,159.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-4H-LFHL-addb33.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sugli estremi di ieri.* Identico al LF, ma i livelli da sfondare e riattraversare sono il massimo e il minimo di ieri invece dei pivot.</para>
/// <para>Da sapere. Stesso codice del LF: l'unica differenza e' quale livello si guarda.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $138,727 su 72 trade · $1,927 a trade · drawdown $9,988 · netto/DD 13.89 · anni in perdita 2 su 13 · notti a mercato 2.74 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $66,954 su 72 trade, drawdown $5,044.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· livello LONG = L_d1 − 0.20 × ATR50; livello SHORT = H_d1 + 0.20 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 12:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 12:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni SHORT di lunedì</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (18 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.65 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_YM_LFH_003_240 : Pt5DavHighLowFaderEngine
{
    public override string Name => "PT8DAV_YM_LFH_003_240";

    public override string Description => "LF_HL YM 4h, ricerca v5.1 YM-4H-LFHL-addb33";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-LFHL-addb33";

    public PT8DAV_YM_LFH_003_240()
    {
        TradingWindow = ResearchWindow(-1, 12); // start_hour -1, end_hour 12, verbatim
        LevelShiftAtr = 0.2m;                   // level_shift_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = 0;                   // not_se_day, pandas
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 18;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.65m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
