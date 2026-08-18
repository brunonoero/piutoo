using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFU_002_15 — TF_U su NQ a 15 minuti, famiglia 09 della consegna
/// <c>run_20260814_1453</c>.
///
/// <para>Trend following non simmetrico: stop buy su <c>H_d1</c>, stop sell su <c>L_d1</c>. I quattro
/// gate <c>PatternFast</c> sono indipendenti per long e short.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono
/// ricostruite dalle barre intraday con confine a mezzanotte, come nella ricerca: la
/// sessione è il giorno di calendario del feed, non la sessione CME 17:00–16:00. Per questo
/// <c>SessionStartTime</c> = 0 e <c>SessionEndTime</c> = 2359. Lo stesso confine governa il
/// secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern e limite di fill insieme.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 31: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.25</c></description></item>
/// <item><description>deve essere FALSO — fast 52: <c>(H_d1 &lt; H_d2) E (L_d1 &lt; L_d2)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 52: <c>(H_d1 &lt; H_d2) E (L_d1 &lt; L_d2)</c></description></item>
/// <item><description>deve essere FALSO — fast 15: <c>|O_d5-C_d1| &lt; 2.0 * (H_d5-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 17:00 e 07:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $1,250 per contratto = 62.50 pt</description></item>
/// <item><description>Take profit: nessuno</description></item>
/// <item><description>Uscita a tempo dopo 184 barre (1.9 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$85.8</description></item>
/// <item><term>Out-of-sample</term><description>$91,035 su 300 trade &#183; drawdown $29,723 &#183; profit factor 1.32 &#183; $303 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.63</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.28</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$48,925</description></item>
/// </list>
///
/// <para><b>Gli orari sono in ora di borsa (America/Chicago), non nell'orologio del feed.</b>
/// La sessione e' la giornata CME 17:00–16:00 e la finestra operativa e' la stessa della ricerca,
/// riespressa: il motore Python lavorava su barre in ora europea e dichiarava gli orari in CET,
/// che e' Chicago piu' sette ore. Il motore converte l'istante UTC della barra in ora di Chicago
/// e confronta li', quindi il risultato non dipende piu' da come e' stampato il feed. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c> e <c>docs/domini/mappa-strategie-pts.md</c>.</para>
///
/// <para><b>Residuo noto.</b> Mezzanotte CET e le 17:00 di Chicago sono lo stesso istante tranne
/// nelle circa quattro settimane l'anno in cui l'ora legale americana ed europea non sono
/// allineate. In quelle giornate — il 6,6% dei trade delle liste di riferimento — questa classe
/// segue la sessione CME vera e diverge dalla ricerca, deliberatamente.</para>
/// </summary>
public sealed class PTS_NQ_TFU_002_15 : TfUnmirroredEngine
{
    public override string Name => "PTS_NQ_TFU_002_15";
    public override string Description =>
        "TF_U NQ 15m: famiglia 09 run 20260814, finestra 10:00–00:00 Chicago, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_TFU_002_15()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        Contracts = 1;

        StartHour = 10; // start_hour
        EndHour = 0;    // end_hour
        SkipDay = -1;   // skip_day (0 = lunedì, -1 = nessuno)

        FastYesLong = 31;  // ptn_ly_yes
        FastNoLong = 52;   // ptn_ly_no
        FastYesShort = 52; // ptn_sy_yes
        FastNoShort = 15;  // ptn_sy_no

        IntradayOnly = false; // intraday_only

        StopMoney = 1250; // stop_loss, $ per contratto = 62.50 pt
        ProfitMoney = 0;  // take_profit, $ per contratto  (0 = nessun target)
        MaxBars = 184;    // max_bars
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
            StartHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndHour = Convert.ToInt32(endHour);
    }
}
