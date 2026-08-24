using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_BSW_003_15 — BIASW su ES a 15 minuti, strategia <b>S05</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260819_1008</c>,
/// famiglia 04 strategia 4 di <c>run-04-agosto/parametri.csv</c>).
///
/// <para>Bias settimanale a <b>due versi</b>: un ingresso market programmato per il long e uno
/// per lo short, ciascuno con la propria uscita programmata.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e orari dichiarano il proprio
/// fuso e il confronto passa dall'istante assoluto della barra. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Ciclo settimanale.</b></para>
/// <list type="bullet">
/// <item><description>LONG: MARKET all'apertura della barra delle <b>11:00 di lunedì</b> (ora dei dati, CET)</description></item>
/// <item><description>SHORT: MARKET all'apertura della barra delle <b>20:00 di giovedì</b></description></item>
/// <item><description>I filtri pattern si valutano alla chiusura della barra precedente (shift(1) del motore Python)</description></item>
/// <item><description>Se quella barra non esiste (festivo, mercato chiuso) la settimana salta</description></item>
/// <item><description>Al massimo una entrata per settimana e per direzione</description></item>
/// </list>
///
/// <para>⚠ <b>Etichetta della barra.</b> Il dossier dichiara che l'orario è l'<b>etichetta di
/// chiusura</b> della barra, mentre il datafeed Piootoo etichetta ogni barra sull'<b>apertura</b>.
/// Gli orari sono riportati <b>verbatim</b>, come impone
/// <c>docs/domini/porting-da-report-sweep.md</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 65: <c>H_d0 &lt; L_d0 * (1 + 0.025)</c></description></item>
/// <item><description>deve essere FALSO — fast 139: <c>(C_d1 &lt; O_d1) E (C_d2 &lt; O_d2)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 58: <c>H_d0 &gt; L_d0 * (1 + 0.025)</c></description></item>
/// <item><description>deve essere FALSO — fast 73: <c>C_d1 &lt; C_d2 * (1 - 0.015)</c></description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso.</para>
/// <list type="bullet">
/// <item><description>Uscita LONG programmata: <b>lunedì alle 01:00</b> della settimana successiva</description></item>
/// <item><description>Uscita SHORT programmata: <b>lunedì alle 02:00</b>, market all'apertura di quella barra</description></item>
/// <item><description>Stop loss: $3,000 per contratto = 60.00 pt (entrambi i versi)</description></item>
/// <item><description>Take profit: $7,500 = 150.00 pt (entrambi i versi)</description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// <item><description>Il motore BIASW <b>non chiude mai per fine sessione</b>: la posizione è multiday per costruzione</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$148.1</description></item>
/// <item><term>Out-of-sample</term><description>$93,316 su 171 trade &#183; drawdown $20,886 &#183; profit factor 1.40 &#183; $546 per trade</description></item>
/// <item><term>Plateau minimo</term><description>0.90</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.27</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$40,193</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S05_15m_BIASW.csv</c>
/// (= <c>run-engine/run-04-agosto/trades/fam04_BIASW.csv</c>). Contano le <b>entrate</b>:
/// timestamp e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di
/// slippage per lato.</para>
/// </summary>
public sealed class PTS_ES_BSW_003_15 : BiasWeeklyEngine
{
    public override string Name => "PTS_ES_BSW_003_15";
    public override string Description =>
        "BIASW ES 15m: famiglia 04 run 20260819_1008, long lunedì 11:00 → lunedì 01:00, short giovedì 20:00 → lunedì 02:00 CET, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 15;

    public PTS_ES_BSW_003_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession();

        // Il BIASW non ha finestra operativa: giorno e ora di ingresso sono gia' la regola di
        // entrata. Dichiarata piena nell'orologio della ricerca per l'invariante degli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);

        Contracts = 1;

        EnableLong = true;
        EnableShort = true;

        EntryDayLong = 0;      // le_day: 0 = lunedi'
        EntryTimeLong = 1100;  // le_time: 11:00 ora della ricerca (CET)
        ExitDayLong = 0;       // lx_day: lunedi'
        ExitTimeLong = 100;    // lx_time: 01:00 - risolto alla settimana successiva

        EntryDayShort = 3;     // se_day: 3 = giovedi'
        EntryTimeShort = 2000; // se_time: 20:00
        ExitDayShort = 0;      // sx_day: lunedi'
        ExitTimeShort = 200;   // sx_time: 02:00

        FastYesLong = 65;      // ptn_ly_yes: H_d0 < L_d0 * (1 + 0.025)
        FastNoLong = 139;      // ptn_ly_no:  (C_d1 < O_d1) E (C_d2 < O_d2)
        FastYesShort = 58;     // ptn_sy_yes: H_d0 > L_d0 * (1 + 0.025)
        FastNoShort = 73;      // ptn_sy_no:  C_d1 < C_d2 * (1 - 0.015)

        StopMoneyLong = 3000m;     // stop_loss, $ per contratto = 60.00 pt
        StopMoneyShort = 3000m;
        ProfitMoneyLong = 7500m;   // take_profit, $ per contratto = 150.00 pt
        ProfitMoneyShort = 7500m;
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
            StopMoneyShort = Convert.ToDecimal(stopLoss);
        }
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
        {
            ProfitMoneyLong = Convert.ToDecimal(takeProfit);
            ProfitMoneyShort = Convert.ToDecimal(takeProfit);
        }
        if (parameters.TryGetValue("EntryDayLong", out var entryDayLong))
            EntryDayLong = Convert.ToInt32(entryDayLong);
        if (parameters.TryGetValue("EntryTimeLong", out var entryTimeLong))
            EntryTimeLong = Convert.ToInt32(entryTimeLong);
        if (parameters.TryGetValue("EntryDayShort", out var entryDayShort))
            EntryDayShort = Convert.ToInt32(entryDayShort);
        if (parameters.TryGetValue("EntryTimeShort", out var entryTimeShort))
            EntryTimeShort = Convert.ToInt32(entryTimeShort);
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
