using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_ES_BSW_001_15</b> — BIASW su ES 15m, codice della ricerca <c>ES-15M-BIASW-4870eb</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>una per settimana e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 674 trade, netto 361,874; fuori campione 06/2022 → 05/2025 netto
/// 76,285; broker (16/09/2025 → 09/09/2026) 48 trade, netto 66,803.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/ES-15M-BIASW-4870eb.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ciclo settimanale.* Entra un certo giorno della settimana a una certa ora e esce un altro giorno a un'altra ora. Serve a esprimere i cicli che durano piu' di una giornata, che il BIAS intraday non puo' rappresentare.</para>
/// <para>Da sapere. Un lato si spegne mettendo il suo giorno d'entrata a -1: la strategia opera in una sola direzione. Tiene sempre la posizione la notte, per costruzione. Se la barra d'uscita non esiste perche' e' un festivo, la posizione resta aperta fino alla settimana dopo — come nell'originale EasyLanguage. Fino al 2026-09-11 l'entrata cadeva una barra PRIMA dell'ora scritta, perche' giorno e ora si leggevano sulla chiusura della barra; ora "entra alle T" significa fill alle T. Le strategie salvate prima conservano il vecchio comportamento, per non cambiare i loro numeri.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $361,874 su 674 trade · $537 a trade · drawdown $59,661 · netto/DD 6.07 · anni in perdita 1 su 14 · notti a mercato 3.04 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $186,866 su 674 trade, drawdown $25,030.</para>
/// <para><b>Ciclo settimanale a giorno e ora fissi</b></para>
/// <para>· LONG: MARKET alle 01:45 di lunedì (apertura della barra da 15 minuti che chiude alle 02:00 di lunedì)</para>
/// <para>· SHORT: spento — questa strategia non apre mai al ribasso</para>
/// <para>· Orari in CET. Le candele sono etichettate al loro inizio: il fill è all'apertura della barra che inizia all'orario scritto sopra.</para>
/// <para>· I filtri pattern si valutano alla chiusura della barra precedente.</para>
/// <para>· Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata</para>
/// <para>· Tiene la posizione oltre la fine della sessione: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi</para>
/// <para>· Al massimo una entrata per settimana e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita LONG: giovedì alle 01:00, market alla chiusura della barra che termina a quell'ora.</para>
/// <para>· Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: nessuno — la posizione si chiude solo con le uscite qui sopra</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_ES_BSW_001_15 : Pt5DavBiasWeeklyEngine
{
    public override string Name => "PT8DAV_ES_BSW_001_15";

    public override string Description => "BIASW ES 15m, ricerca v5.1 ES-15M-BIASW-4870eb";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "ES-15M-BIASW-4870eb";

    public PT8DAV_ES_BSW_001_15()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        EntryDayLong = 0;                       // le_day, pandas (-1 = long spento)
        EntryTimeLong = new TimeOnly(2, 0);     // le_time 200: apre la barra d'ingresso
        ExitDayLong = 3;                        // lx_day, pandas
        ExitTimeLong = new TimeOnly(1, 0);      // lx_time 100: chiude la barra d'uscita
        EntryDayShort = -1;                     // se_day, pandas (-1 = short spento)
        EntryTimeShort = new TimeOnly(1, 0);    // se_time 100
        ExitDayShort = 0;                       // sx_day, pandas
        ExitTimeShort = new TimeOnly(1, 0);     // sx_time 100
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        StopAtr = 0m;                           // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
