using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HO_BSW_003_60 - BIASW su HO a 60 minuti, <b>S107</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S107.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>HO_1h</c>, famiglia
/// <c>fam04</c>, motore <c>BIASW</c>.</para>
///
/// <para><b>Che cosa fa.</b> Bias settimanale: un ingresso market programmato a giorno e ora
/// fissi per verso, con uscita programmata a giorno e ora fissi. Entrambi i lati sono attivi.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: HO parte a 00:00 CET). Giorni e orari del ciclo settimanale sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora NYMEX.</para>
///
/// <para><b>Ciclo settimanale.</b></para>
/// <list type="bullet">
/// <item><description>LONG: MARKET all'apertura della barra delle <b>04:00 di mercoledi'</b> (ora della ricerca, CET)</description></item>
/// <item><description>SHORT: MARKET all'apertura della barra delle <b>07:00 di lunedi'</b></description></item>
/// <item><description>I filtri pattern si valutano alla chiusura della barra precedente (<c>shift(1)</c> del motore Python)</description></item>
/// <item><description>Se quella barra non esiste (festivo, mercato chiuso) la settimana salta</description></item>
/// <item><description>Al massimo una entrata per settimana e per direzione</description></item>
/// </list>
///
/// <para>⚠ <b>Etichetta della barra.</b> Il dossier dichiara che l'orario e' l'<b>etichetta di
/// chiusura</b> della barra, mentre il datafeed Piootoo etichetta ogni barra sull'<b>apertura</b>.
/// Gli orari sono riportati verbatim, come impone <c>docs/domini/porting-da-report-sweep.md</c>:
/// la convenzione di etichettatura e' una questione aperta di progetto e non va compensata
/// strategia per strategia. In verifica del porting attendersi lo scarto di una barra.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>100</c>: <c>L_d0 &gt; L_d1</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>148</c>: <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>73</c>: <c>C_d1 &lt; C_d2 * (1 - 0.015)</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>93</c>: <c>H_d1 &gt; H_d5</c></description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Uscita LONG programmata: <b>venerdi' alle 23:00</b>, market all'apertura di quella barra. E' l'uscita principale: stop e target agiscono solo se scattano prima.</description></item>
/// <item><description>Uscita SHORT programmata: <b>mercoledi' alle 08:00</b>, market all'apertura di quella barra.</description></item>
/// <item><description>Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva</description></item>
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>0,01 pt</b></description></item>
/// <item><description>Take profit: <b>$2.000</b> = <b>0,05 pt</b></description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// <item><description>Il motore BIASW <b>non chiude mai per fine sessione</b>: la posizione e' multiday per costruzione</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HO, $42.000 per punto, tick 0,0001.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$75</description></item>
/// <item><term>Fuori campione</term><description>$15.619 su 85 trade</description></item>
/// <item><term>Drawdown</term><description>$3.413</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>HO_1h/consegna/trades/fam04_BIASW.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_HO_BSW_003_60 : BiasWeeklyEngine
{
    public override string Name => "PTS_HO_BSW_003_60";
    public override string Description =>
        "BIASW HO 60m: S107 del dossier, run HO_1h, long mercoledi' 04:00 -> venerdi' 23:00 CET, multiday";
    public override string Symbol => "@HO";
    public override int TimeframeMinutes => 60;

    public PTS_HO_BSW_003_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();

        // Il BIASW non ha finestra operativa: giorno e ora di ingresso sono gia' la regola di
        // entrata. La finestra e' dichiarata piena, nell'orologio della ricerca, perche' ogni
        // PTS deve dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);

        Contracts = 1;

        EnableLong = true;
        EnableShort = true;

        EntryDayLong = 2;      // le_day: mercoledi' (convenzione pandas, 0 = lunedi')
        EntryTimeLong = 400;   // le_time: 04:00 ora della ricerca (CET)
        ExitDayLong = 4;       // lx_day: venerdi'
        ExitTimeLong = 2300;   // lx_time: 23:00

        EntryDayShort = 0;     // se_day: lunedi'
        EntryTimeShort = 700;  // se_time: 07:00
        ExitDayShort = 2;      // sx_day: mercoledi'
        ExitTimeShort = 800;   // sx_time: 08:00

        FastYesLong = 100;    // ptn_ly_yes: L_d0 > L_d1
        FastNoLong = 148;     // ptn_ly_no:  close < O_d0 * 1.005
        FastYesShort = 73;    // ptn_sy_yes: C_d1 < C_d2 * (1 - 0.015)
        FastNoShort = 93;     // ptn_sy_no:  H_d1 > H_d5

        StopMoneyLong = 250m;     // stop_loss, $ per contratto = 0,01 pt
        ProfitMoneyLong = 2000m;  // take_profit, $ per contratto = 0,05 pt
        StopMoneyShort = 250m;
        ProfitMoneyShort = 2000m;
        BreakEvenMoneyLong = 0m;
        BreakEvenMoneyShort = 0m;
        TrailingMoneyLong = 0m;
        TrailingMoneyShort = 0m;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
        {
            StopMoneyLong = Convert.ToDecimal(stopLoss);
            StopMoneyShort = StopMoneyLong;
        }
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
        {
            ProfitMoneyLong = Convert.ToDecimal(takeProfit);
            ProfitMoneyShort = ProfitMoneyLong;
        }
        if (parameters.TryGetValue("EntryDayLong", out var entryDayLong))
            EntryDayLong = Convert.ToInt32(entryDayLong);
        if (parameters.TryGetValue("EntryTimeLong", out var entryTimeLong))
            EntryTimeLong = Convert.ToInt32(entryTimeLong);
        if (parameters.TryGetValue("ExitDayLong", out var exitDayLong))
            ExitDayLong = Convert.ToInt32(exitDayLong);
        if (parameters.TryGetValue("ExitTimeLong", out var exitTimeLong))
            ExitTimeLong = Convert.ToInt32(exitTimeLong);
        if (parameters.TryGetValue("EntryDayShort", out var entryDayShort))
            EntryDayShort = Convert.ToInt32(entryDayShort);
        if (parameters.TryGetValue("EntryTimeShort", out var entryTimeShort))
            EntryTimeShort = Convert.ToInt32(entryTimeShort);
        if (parameters.TryGetValue("ExitDayShort", out var exitDayShort))
            ExitDayShort = Convert.ToInt32(exitDayShort);
        if (parameters.TryGetValue("ExitTimeShort", out var exitTimeShort))
            ExitTimeShort = Convert.ToInt32(exitTimeShort);
        if (parameters.TryGetValue("PtnLyYes", out var lyYes))
            FastYesLong = Convert.ToInt32(lyYes);
        if (parameters.TryGetValue("PtnLyNo", out var lyNo))
            FastNoLong = Convert.ToInt32(lyNo);
        if (parameters.TryGetValue("PtnSyYes", out var syYes))
            FastYesShort = Convert.ToInt32(syYes);
        if (parameters.TryGetValue("PtnSyNo", out var syNo))
            FastNoShort = Convert.ToInt32(syNo);
    }
}
