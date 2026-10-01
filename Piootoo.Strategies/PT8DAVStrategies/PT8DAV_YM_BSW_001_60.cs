using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_BSW_001_60</b> — BIASW su YM 1h, codice della ricerca <c>YM-1H-BIASW-6e55f4</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>una per settimana e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 598 trade, netto 194,149; fuori campione 06/2022 → 05/2025 netto
/// 22,855; broker (16/09/2025 → 09/09/2026) 22 trade, netto 30,663.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-1H-BIASW-6e55f4.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ciclo settimanale.* Entra un certo giorno della settimana a una certa ora e esce un altro giorno a un'altra ora. Serve a esprimere i cicli che durano piu' di una giornata, che il BIAS intraday non puo' rappresentare.</para>
/// <para>Da sapere. Un lato si spegne mettendo il suo giorno d'entrata a -1: la strategia opera in una sola direzione. Tiene sempre la posizione la notte, per costruzione. Se la barra d'uscita non esiste perche' e' un festivo, la posizione resta aperta fino alla settimana dopo — come nell'originale EasyLanguage. Fino al 2026-09-11 l'entrata cadeva una barra PRIMA dell'ora scritta, perche' giorno e ora si leggevano sulla chiusura della barra; ora "entra alle T" significa fill alle T. Le strategie salvate prima conservano il vecchio comportamento, per non cambiare i loro numeri.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $194,149 su 598 trade · $325 a trade · drawdown $59,419 · netto/DD 3.27 · anni in perdita 4 su 14 · notti a mercato 6.18 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $156,124 su 598 trade, drawdown $30,254.</para>
/// <para><b>Ciclo settimanale a giorno e ora fissi</b></para>
/// <para>· LONG: MARKET alle 07:00 di lunedì (apertura della barra da 60 minuti che chiude alle 08:00 di lunedì)</para>
/// <para>· SHORT: MARKET alle 08:00 di venerdì (apertura della barra da 60 minuti che chiude alle 09:00 di venerdì)</para>
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
/// <para>· Uscita LONG: giovedì alle 00:00, market alla chiusura della barra che termina a quell'ora.</para>
/// <para>· Uscita SHORT: lunedì alle 01:00, market alla chiusura della barra che termina a quell'ora.</para>
/// <para>· Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_YM_BSW_001_60 : Pt5DavBiasWeeklyEngine
{
    public override string Name => "PT8DAV_YM_BSW_001_60";

    public override string Description => "BIASW YM 1h, ricerca v5.1 YM-1H-BIASW-6e55f4";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "YM-1H-BIASW-6e55f4";

    public PT8DAV_YM_BSW_001_60()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        EntryDayLong = 0;                       // le_day, pandas (-1 = long spento)
        EntryTimeLong = new TimeOnly(8, 0);     // le_time 800: apre la barra d'ingresso
        ExitDayLong = 3;                        // lx_day, pandas
        ExitTimeLong = new TimeOnly(0, 0);      // lx_time 0: chiude la barra d'uscita
        EntryDayShort = 4;                      // se_day, pandas (-1 = short spento)
        EntryTimeShort = new TimeOnly(9, 0);    // se_time 900
        ExitDayShort = 0;                       // sx_day, pandas
        ExitTimeShort = new TimeOnly(1, 0);     // sx_time 100
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
