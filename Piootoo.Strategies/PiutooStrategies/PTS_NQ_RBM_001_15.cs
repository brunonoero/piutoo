using Piootoo.Shared.Models;
using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_RBM_001_15 — RBB_M su NQ a 15 minuti, famiglia 08 della consegna
/// <c>run_20260814_1453</c>.
///
/// <para>Reversal sulle bande di Bollinger: ordine limit sulla banda, valido solo sulla barra
/// successiva, riemesso finché il close resta dentro le bande.</para>
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
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 53: <c>(H_d1-L_d1) &lt; (H_d2-L_d2)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 46: <c>(H_d0 &lt; H_d1) E (L_d0 &gt; L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &gt; O_d0 * 0.995</c></description></item>
/// <item><description>deve essere FALSO — direzionale 37: <c>(C_d1 &lt; C_d2) E (C_d2 &lt; C_d3) E (O_d0 &lt; C_d1)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale -48: <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>deve essere FALSO — direzionale 37: <c>(C_d1 &gt; C_d2) E (C_d2 &gt; C_d3) E (O_d0 &gt; C_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 07:00 e 06:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Chiude tutto a fine sessione (nessun overnight)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $2,000 per contratto = 100.00 pt</description></item>
/// <item><description>Take profit: $10,000 = 500.00 pt</description></item>
/// <item><description>Nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$110.4</description></item>
/// <item><term>Out-of-sample</term><description>$104,240 su 520 trade &#183; drawdown $27,622 &#183; profit factor 1.21 &#183; $200 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.72</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.55</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$59,890</description></item>
/// </list>
/// </summary>
public sealed class PTS_NQ_RBM_001_15 : RbbMirroredEngine
{
    public override string Name => "PTS_NQ_RBM_001_15";
    public override string Description =>
        "RBB_M NQ 15m: famiglia 08 run 20260814, finestra 07:00–06:00 CET, intraday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_RBM_001_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        BollingerLength = 10;    // bb_length
        BollingerNumDevs = 2.5m; // bb_num_devs

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(7, 6);
        DayToFilter = -1; // skip_day = -1: nessun giorno escluso

        NeutralYes = 53;      // ptn_neut_yes
        NeutralNo = 46;       // ptn_neut_no
        DirectionalYes = -48; // ptn_dir_yes, specchiato dal motore
        DirectionalNo = 37;   // ptn_dir_no, specchiato dal motore

        IntradayOnly = true; // intraday_only

        StopMoney = 2000;    // stop_loss, $ per contratto = 100.00 pt
        ProfitMoney = 10000; // take_profit, $ per contratto
        MaxBars = 0;         // max_bars  (0 = nessuna uscita a tempo)
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

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
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        // Sulla finestra dichiarata, non sui campi interi: quelli il motore non li legge piu'
        // quando TradingWindow c'e', e scriverli sarebbe un no-op silenzioso.
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
        if (parameters.TryGetValue("BbLength", out var bbLength))
            BollingerLength = Convert.ToInt32(bbLength);
        if (parameters.TryGetValue("BbNumDevs", out var bbNumDevs))
            BollingerNumDevs = Convert.ToDecimal(bbNumDevs);
    }
}
