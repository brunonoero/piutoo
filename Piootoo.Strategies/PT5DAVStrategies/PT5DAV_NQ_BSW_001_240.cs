using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_BSW_001_240</b> — BIASW su NQ 4h, codice della ricerca <c>NQ-4H-BIASW-cff083</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 676 trade, netto $962,237; broker (27/08/2025 → 09/09/2026)
/// 48 trade, netto $135,067. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-4H-BIASW-cff083.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ciclo settimanale.* Entra un certo giorno della settimana a una certa ora e esce un altro giorno a un'altra ora. Serve a esprimere i cicli che durano piu' di una giornata, che il BIAS intraday non puo' rappresentare.</para>
/// <para>Da sapere. Un lato si spegne mettendo il suo giorno d'entrata a -1: la strategia opera in una sola direzione. Tiene sempre la posizione la notte, per costruzione. Se la barra d'uscita non esiste perche' e' un festivo, la posizione resta aperta fino alla settimana dopo — come nell'originale EasyLanguage. Fino al 2026-09-11 l'entrata cadeva una barra PRIMA dell'ora scritta, perche' giorno e ora si leggevano sulla chiusura della barra; ora "entra alle T" significa fill alle T. Le strategie salvate prima conservano il vecchio comportamento, per non cambiare i loro numeri.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $962,237 su 676 trade · $1,423 a trade · drawdown $91,477 · netto/DD 10.52 · anni in perdita 2 su 14 · notti a mercato 3.04 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $378,766 su 676 trade, drawdown $32,545.</para>
/// <para><b>Ciclo settimanale a giorno e ora fissi</b></para>
/// <para>· LONG: MARKET alle 00:00 di lunedì (apertura della barra da 240 minuti che chiude alle 04:00 di lunedì)</para>
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
/// <para>· Uscita LONG: giovedì alle 04:00, market alla chiusura della barra che termina a quell'ora.</para>
/// <para>· Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: nessuno — la posizione si chiude solo con le uscite qui sopra</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_NQ_BSW_001_240 : Pt5DavBiasWeeklyEngine
{
    public override string Name => "PT5DAV_NQ_BSW_001_240";

    public override string Description => "BIASW NQ 4h, ricerca PT5DAV NQ-4H-BIASW-cff083";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "NQ-4H-BIASW-cff083";

    public PT5DAV_NQ_BSW_001_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        EntryDayLong = 0;                       // le_day, pandas (-1 = long spento)
        EntryTimeLong = new TimeOnly(4, 0);     // le_time 400: apre la barra d'ingresso
        ExitDayLong = 3;                        // lx_day, pandas
        ExitTimeLong = new TimeOnly(4, 0);      // lx_time 400: chiude la barra d'uscita
        EntryDayShort = -1;                     // se_day, pandas (-1 = short spento)
        EntryTimeShort = new TimeOnly(4, 0);    // se_time 400
        ExitDayShort = 0;                       // sx_day, pandas
        ExitTimeShort = new TimeOnly(4, 0);     // sx_time 400
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        StopAtr = 0m;                           // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
