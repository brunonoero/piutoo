using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_BSW_002_15 — BIASW su ES a 15 minuti, strategia <b>S03</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260819_1008</c>,
/// famiglia 03 strategia 3 di <c>run-04-agosto/parametri.csv</c>).
///
/// <para>Bias settimanale: un solo ingresso market programmato a giorno e ora fissi, con uscita
/// programmata a giorno e ora fissi. Il lato short è spento.</para>
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
/// <item><description>LONG: MARKET all'apertura della barra delle <b>03:00 di venerdì</b> (ora dei dati, CET)</description></item>
/// <item><description>SHORT: spento — questa strategia non apre mai al ribasso</description></item>
/// <item><description>I filtri pattern si valutano alla chiusura della barra precedente (shift(1) del motore Python)</description></item>
/// <item><description>Se quella barra non esiste (festivo, mercato chiuso) la settimana salta</description></item>
/// <item><description>Al massimo una entrata per settimana e per direzione</description></item>
/// </list>
///
/// <para>⚠ <b>Etichetta della barra.</b> Il dossier dichiara che l'orario è l'<b>etichetta di
/// chiusura</b> della barra, mentre il datafeed Piootoo etichetta ogni barra sull'<b>apertura</b>.
/// Gli orari sono riportati <b>verbatim</b>, come impone
/// <c>docs/domini/porting-da-report-sweep.md</c>: la convenzione di etichettatura è una questione
/// aperta di progetto e non va compensata strategia per strategia.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 94: <c>H_d1 &lt; H_d5</c></description></item>
/// <item><description>deve essere FALSO — fast 112: <c>(C_d1 &gt; C_d2) E (C_d2 &gt; C_d3) E (O_d0 &gt; C_d1)</c></description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso.</para>
/// <list type="bullet">
/// <item><description>Uscita LONG programmata: <b>venerdì alle 01:00</b> della settimana successiva, market all'apertura di quella barra. È l'uscita principale: stop e target agiscono solo se scattano prima.</description></item>
/// <item><description>Stop loss: $3,000 per contratto = 60.00 pt</description></item>
/// <item><description>Take profit: $4,500 = 90.00 pt</description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// <item><description>Il motore BIASW <b>non chiude mai per fine sessione</b>: la posizione è multiday per costruzione</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$192.8</description></item>
/// <item><term>Out-of-sample</term><description>$57,538 su 81 trade &#183; drawdown $15,424 &#183; profit factor 1.50 &#183; $710 per trade</description></item>
/// <item><term>Plateau minimo</term><description>0.89</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.27</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$36,913</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S03_15m_BIASW.csv</c>
/// (= <c>run-engine/run-04-agosto/trades/fam03_BIASW.csv</c>). Contano le <b>entrate</b>:
/// timestamp e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di
/// slippage per lato.</para>
/// </summary>
public sealed class PTS_ES_BSW_002_15 : BiasWeeklyEngine
{
    public override string Name => "PTS_ES_BSW_002_15";
    public override string Description =>
        "BIASW ES 15m: famiglia 03 run 20260819_1008, long venerdì 03:00 → venerdì 01:00 CET, solo long, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 15;

    public PTS_ES_BSW_002_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession();

        // Il BIASW non ha finestra operativa: giorno e ora di ingresso sono gia' la regola di
        // entrata. Dichiarata piena nell'orologio della ricerca per l'invariante degli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);

        Contracts = 1;

        EnableLong = true;
        EnableShort = false;

        EntryDayLong = 4;     // le_day: 4 = venerdi' (convenzione pandas)
        EntryTimeLong = 300;  // le_time: 03:00 ora della ricerca (CET)
        ExitDayLong = 4;      // lx_day: venerdi'
        ExitTimeLong = 100;   // lx_time: 01:00 - risolto alla settimana successiva

        EntryDayShort = -1;   // se_day: lato short spento
        ExitDayShort = -1;

        FastYesLong = 94;     // ptn_ly_yes: H_d1 < H_d5
        FastNoLong = 112;     // ptn_ly_no:  (C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)
        FastYesShort = 152;   // sentinella sempre vera (lato spento)
        FastNoShort = 153;    // sentinella sempre falsa (lato spento)

        StopMoneyLong = 3000m;    // stop_loss, $ per contratto = 60.00 pt
        ProfitMoneyLong = 4500m;  // take_profit, $ per contratto = 90.00 pt
        StopMoneyShort = 0m;
        ProfitMoneyShort = 0m;
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
            StopMoneyLong = Convert.ToDecimal(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoneyLong = Convert.ToDecimal(takeProfit);
        if (parameters.TryGetValue("EntryDayLong", out var entryDayLong))
            EntryDayLong = Convert.ToInt32(entryDayLong);
        if (parameters.TryGetValue("EntryTimeLong", out var entryTimeLong))
            EntryTimeLong = Convert.ToInt32(entryTimeLong);
        if (parameters.TryGetValue("ExitDayLong", out var exitDayLong))
            ExitDayLong = Convert.ToInt32(exitDayLong);
        if (parameters.TryGetValue("ExitTimeLong", out var exitTimeLong))
            ExitTimeLong = Convert.ToInt32(exitTimeLong);
        if (parameters.TryGetValue("PtnLyYes", out var lyYes))
            FastYesLong = Convert.ToInt32(lyYes);
        if (parameters.TryGetValue("PtnLyNo", out var lyNo))
            FastNoLong = Convert.ToInt32(lyNo);
    }
}
