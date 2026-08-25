using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFU_001_15 — TF_U su NQ a 15 minuti, famiglia 07 della consegna
/// <c>run_20260814_1453</c>.
///
/// <para><b>Codice sorgente: S48.</b> E' l'identificativo con cui questa strategia
/// compare nel <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>: e' da li' che si
/// risale a condizioni, filtri e parametri per un controllo contro la sorgente, senza
/// riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260814_1453</c>, famiglia
/// <c>fam07</c>; i trade di riferimento stanno in
/// <c>run_20260814_1453/consegna/trades/fam07_TF_U.csv</c>.</para>
///
/// <para>Trend following non simmetrico: stop buy su <c>H_d1</c>, stop sell su <c>L_d1</c>. I quattro
/// gate <c>PatternFast</c> sono indipendenti per long e short.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate. Lo stesso
/// confine governa il secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern e limite di
/// fill insieme.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e finestra dichiarano il
/// proprio fuso e il confronto passa dall'istante assoluto della barra: il feed dichiara il suo
/// orologio in <c>datafeed/feed-clocks.json</c> e viene convertito a UTC vero al caricamento.
/// Vedi <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 28: <c>|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO — fast 77: <c>C_d1 &gt; C_d2 * (1 + 0.005)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 114: <c>H_d1 - C_d1 &lt; 0.2 * (H_d1-L_d1)</c></description></item>
/// <item><description>deve essere FALSO — fast 39: <c>H_d0 - O_d0 &lt; H_d1 - O_d1</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 17:00 e 10:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Non apre posizioni di venerdì</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $4,000 per contratto = 200.00 pt</description></item>
/// <item><description>Take profit: $5,000 = 250.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 368 barre (3.8 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$155.9</description></item>
/// <item><term>Out-of-sample</term><description>$85,485 su 155 trade &#183; drawdown $29,837 &#183; profit factor 1.29 &#183; $552 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.43</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.28</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$56,245</description></item>
/// </list>
/// </summary>
public sealed class PTS_NQ_TFU_001_15 : TfUnmirroredEngine
{
    public override string Name => "PTS_NQ_TFU_001_15";
    public override string Description =>
        "TF_U NQ 15m: famiglia 07 run 20260814, finestra 17:00–10:00 CET, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_TFU_001_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(17, 10);
        SkipDay = 4;    // skip_day (0 = lunedì, -1 = nessuno)

        FastYesLong = 28;   // ptn_ly_yes
        FastNoLong = 77;    // ptn_ly_no
        FastYesShort = 114; // ptn_sy_yes
        FastNoShort = 39;   // ptn_sy_no

        IntradayOnly = false; // intraday_only

        StopMoney = 4000;   // stop_loss, $ per contratto = 200.00 pt
        ProfitMoney = 5000; // take_profit, $ per contratto
        MaxBars = 368;      // max_bars
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("PtnLyYes", out var lyYes))
            FastYesLong = Convert.ToInt32(lyYes);
        if (parameters.TryGetValue("PtnLyNo", out var lyNo))
            FastNoLong = Convert.ToInt32(lyNo);
        if (parameters.TryGetValue("PtnSyYes", out var syYes))
            FastYesShort = Convert.ToInt32(syYes);
        if (parameters.TryGetValue("PtnSyNo", out var syNo))
            FastNoShort = Convert.ToInt32(syNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
