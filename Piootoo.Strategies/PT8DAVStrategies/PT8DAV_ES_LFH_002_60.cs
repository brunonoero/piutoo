using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_LFH_002_60</b> — LF_HL su ES 1h, codice della ricerca <c>ES-1H-LFHL-8d5600</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 65 trade, netto 151,968; fuori campione 06/2022 → 05/2025 netto
/// 9,670; broker (16/09/2025 → 09/09/2026) 3 trade, netto 6,292.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-1H-LFHL-8d5600.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Fade del falso sfondamento, sugli estremi di ieri.* Identico al LF, ma i livelli da sfondare e riattraversare sono il massimo e il minimo di ieri invece dei pivot.</para>
/// <para>Da sapere. Stesso codice del LF: l'unica differenza e' quale livello si guarda.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $151,968 su 65 trade · $2,338 a trade · drawdown $18,896 · netto/DD 8.04 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $46,018 su 65 trade, drawdown $9,851.</para>
/// <para><b>Rientro dentro un livello di sessione (fade del falso breakout)</b></para>
/// <para>· livello LONG = L_d1 − 0.30 × ATR50; livello SHORT = H_d1 + 0.30 × ATR50</para>
/// <para>· I livelli si calcolano dalla sessione precedente e restano fissi per tutta la sessione corrente.</para>
/// <para>· Segnale LONG: la close della barra precedente era SOTTO il livello LONG e la close della barra corrente è SOPRA — il prezzo rientra.</para>
/// <para>· Segnale SHORT: la close precedente era SOPRA il livello SHORT e quella corrente è SOTTO.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva a quella del segnale. Questo motore non usa né ordini stop né ordini limit.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 07:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 07:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.80 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_ES_LFH_002_60 : Pt5DavHighLowFaderEngine
{
    public override string Name => "PT8DAV_ES_LFH_002_60";

    public override string Description => "LF_HL ES 1h, ricerca v5.1 ES-1H-LFHL-8d5600";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "ES-1H-LFHL-8d5600";

    public PT8DAV_ES_LFH_002_60()
    {
        TradingWindow = ResearchWindow(-1, 7);  // start_hour -1, end_hour 7, verbatim
        LevelShiftAtr = 0.3m;                   // level_shift_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        BaseYesLong = 41;                       // ptn_ly_yes
        BaseNoLong = 42;                        // ptn_ly_no
        BaseYesShort = 41;                      // ptn_sy_yes
        BaseNoShort = 42;                       // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.8m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
