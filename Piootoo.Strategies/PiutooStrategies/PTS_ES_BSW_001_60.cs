using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_BSW_001_60 — BIASW su ES a 60 minuti, strategia <b>S01</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260820_0012</c>,
/// famiglia 01).
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
/// fuso e il confronto passa dall'istante assoluto della barra: il feed dichiara il suo orologio
/// in <c>datafeed/feed-clocks.json</c> e viene convertito a UTC vero al caricamento. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Ciclo settimanale.</b></para>
/// <list type="bullet">
/// <item><description>LONG: MARKET all'apertura della barra delle <b>02:00 di lunedì</b> (ora dei dati, CET)</description></item>
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
/// aperta di progetto (voce in <c>docs/decisioni.md</c>) e non va compensata strategia per
/// strategia. In verifica del porting attendersi lo scarto di una barra già misurato sulle NQ.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 106: <c>L_d1 &lt; L_d5</c></description></item>
/// <item><description>deve essere FALSO — fast 130: <c>(H_d2 &gt; H_d1) E (L_d2 &lt; L_d1)</c></description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Uscita LONG programmata: <b>lunedì alle 01:00</b>, market all'apertura di quella barra (la settimana successiva all'ingresso). È l'uscita principale: stop e target agiscono solo se scattano prima.</description></item>
/// <item><description>Stop loss: $5,000 per contratto = 100.00 pt</description></item>
/// <item><description>Take profit: $6,000 = 120.00 pt</description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// <item><description>Il motore BIASW <b>non chiude mai per fine sessione</b> e non c'è un parametro che lo cambi: la posizione è multiday per costruzione</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$390</description></item>
/// <item><term>Out-of-sample</term><description>$51,386 su 88 trade &#183; drawdown $24,881 (01/06/2021 → 30/05/2025)</description></item>
/// </list>
///
/// <para><b>Vincolo operativo.</b> Emette gli stessi ordini di entrata della BIASW 15m
/// <c>fam02</c> di <c>run_20260819_1008</c> (S02 di <c>run-04-agosto</c>), che per questo
/// <b>non è stata tradotta</b>: metterle su conti separati sarebbe copy trading.</para>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S01_1h_BIASW.csv</c>
/// (<c>run_20260820_0012/consegna/trades/fam01_BIASW.csv</c>). Contano le <b>entrate</b>:
/// timestamp e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di
/// slippage per lato.</para>
/// </summary>
public sealed class PTS_ES_BSW_001_60 : BiasWeeklyEngine
{
    public override string Name => "PTS_ES_BSW_001_60";
    public override string Description =>
        "BIASW ES 60m: S01 run 20260820_0012, long lunedì 02:00 → lunedì 01:00 CET, solo long, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 60;

    public PTS_ES_BSW_001_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();

        // Il BIASW non ha finestra operativa: giorno e ora di ingresso sono gia' la regola di
        // entrata. La finestra e' dichiarata piena, nell'orologio della ricerca, perche' ogni
        // PTS deve dichiarare l'orologio in cui legge gli orari (StrategyClockConformanceTests).
        TradingWindow = ZonedWindow.Research(0, 2359);

        Contracts = 1;

        EnableLong = true;
        EnableShort = false;

        EntryDayLong = 0;     // le_day: 0 = lunedi' (convenzione pandas)
        EntryTimeLong = 200;  // le_time: 02:00 ora della ricerca (CET)
        ExitDayLong = 0;      // lx_day: lunedi'
        ExitTimeLong = 100;   // lx_time: 01:00 - risolto alla settimana successiva

        EntryDayShort = -1;   // se_day: lato short spento
        ExitDayShort = -1;

        FastYesLong = 106;    // ptn_ly_yes: L_d1 < L_d5
        FastNoLong = 130;     // ptn_ly_no:  (H_d2 > H_d1) E (L_d2 < L_d1)
        FastYesShort = 152;   // sentinella sempre vera (lato spento)
        FastNoShort = 153;    // sentinella sempre falsa (lato spento)

        StopMoneyLong = 5000m;    // stop_loss, $ per contratto = 100.00 pt
        ProfitMoneyLong = 6000m;  // take_profit, $ per contratto = 120.00 pt
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
