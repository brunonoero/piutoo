using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_CL_BSW_001_15</b> — BIASW su CL 15m, codice della ricerca <c>CL-15M-BIASW-37bccc</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>una per settimana e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 623 trade, netto 103,062; fuori campione 06/2022 → 05/2025 netto
/// 2,385; broker (16/09/2025 → 09/09/2026) 45 trade, netto 47,119.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/CL-15M-BIASW-37bccc.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ciclo settimanale.* Entra un certo giorno della settimana a una certa ora e esce un altro giorno a un'altra ora. Serve a esprimere i cicli che durano piu' di una giornata, che il BIAS intraday non puo' rappresentare.</para>
/// <para>Da sapere. Un lato si spegne mettendo il suo giorno d'entrata a -1: la strategia opera in una sola direzione. Tiene sempre la posizione la notte, per costruzione. Se la barra d'uscita non esiste perche' e' un festivo, la posizione resta aperta fino alla settimana dopo — come nell'originale EasyLanguage. Fino al 2026-09-11 l'entrata cadeva una barra PRIMA dell'ora scritta, perche' giorno e ora si leggevano sulla chiusura della barra; ora "entra alle T" significa fill alle T. Le strategie salvate prima conservano il vecchio comportamento, per non cambiare i loro numeri.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $103,062 su 623 trade · $165 a trade · drawdown $53,946 · netto/DD 1.91 · anni in perdita 5 su 14 · notti a mercato 5.06 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $57,030 su 623 trade, drawdown $38,239.</para>
/// <para><b>Ciclo settimanale a giorno e ora fissi</b></para>
/// <para>· LONG: MARKET alle 21:45 di mercoledì (apertura della barra da 15 minuti che chiude alle 22:00 di mercoledì)</para>
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
/// <para>· Uscita LONG: lunedì alle 06:00, market alla chiusura della barra che termina a quell'ora.</para>
/// <para>· Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_CL_BSW_001_15 : Pt5DavBiasWeeklyEngine
{
    public override string Name => "PT8DAV_CL_BSW_001_15";

    public override string Description => "BIASW CL 15m, ricerca v5.1 CL-15M-BIASW-37bccc";

    public override string Symbol => "@CL";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "CL-15M-BIASW-37bccc";

    public PT8DAV_CL_BSW_001_15()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        EntryDayLong = 2;                       // le_day, pandas (-1 = long spento)
        EntryTimeLong = new TimeOnly(22, 0);    // le_time 2200: apre la barra d'ingresso
        ExitDayLong = 0;                        // lx_day, pandas
        ExitTimeLong = new TimeOnly(6, 0);      // lx_time 600: chiude la barra d'uscita
        EntryDayShort = -1;                     // se_day, pandas (-1 = short spento)
        EntryTimeShort = new TimeOnly(1, 0);    // se_time 100
        ExitDayShort = 0;                       // sx_day, pandas
        ExitTimeShort = new TimeOnly(1, 0);     // sx_time 100
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
