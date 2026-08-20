using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_GC_TFU_001_30 — TF_U su GC a 30 minuti, famiglia 01 della consegna
/// <c>run_20260819_0201</c> (run 002).
///
/// <para>Trend following non simmetrico: stop buy su <c>H_d1</c>, stop sell su <c>L_d1</c>. I quattro
/// gate <c>PatternFast</c> sono indipendenti per long e short, quindi una delle due direzioni
/// può restare spenta per intere fasi di mercato.</para>
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
/// <item><description>deve essere VERO — fast 34: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 1.0</c></description></item>
/// <item><description>deve essere FALSO — fast 25: <c>|O_d5-C_d1| &lt; 0.5 * (HH5-LL5)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 128: <c>(H_d1-L_d1) &lt; (H_d2 - L_d2 + H_d3 - L_d3) / 3</c></description></item>
/// <item><description>deve essere FALSO — fast 1: <c>|O_d1-C_d1| &lt; 0.1 * (H_d1-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 16:00 e 08:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Nessun giorno escluso</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $1,750 per contratto = 17.50 pt</description></item>
/// <item><description>Take profit: $7,500 = 75.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 460 barre (9.6 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto, tick 0,1 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$889.5</description></item>
/// <item><term>Out-of-sample</term><description>$176,500 su 150 trade &#183; drawdown $20,054 &#183; profit factor 2.04 &#183; $1,177 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.62</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.76</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$52,903</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-02-agosto/consegna/trades/fam01_TF_U.csv</c>. Non ancora eseguita: manca il
/// datafeed <c>@GC</c> a 30 minuti.</para>
/// </summary>
public sealed class PTS_GC_TFU_001_30 : TfUnmirroredEngine
{
    public override string Name => "PTS_GC_TFU_001_30";
    public override string Description =>
        "TF_U GC 30m: famiglia 01 run 20260819_0201, finestra 16:00–08:00 CET, multiday";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 30;

    public PTS_GC_TFU_001_30()
    {
        // Sessione della ricerca (00:00 CET) scritta in ora di borsa GC.
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(16, 8);
        SkipDay = -1;   // skip_day (0 = lunedì, -1 = nessuno)

        FastYesLong = 34;   // ptn_ly_yes
        FastNoLong = 25;    // ptn_ly_no
        FastYesShort = 128; // ptn_sy_yes
        FastNoShort = 1;    // ptn_sy_no

        IntradayOnly = false; // intraday_only = 0

        StopMoney = 1750;   // stop_loss, $ per contratto = 17.50 pt
        ProfitMoney = 7500; // take_profit, $ per contratto = 75.00 pt
        MaxBars = 460;      // max_bars
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
