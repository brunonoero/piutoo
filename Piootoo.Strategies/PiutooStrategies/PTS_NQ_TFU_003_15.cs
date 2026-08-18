using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFU_003_15 — TF_U su NQ a 15 minuti, famiglia 10 della consegna
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
/// <item><description>deve essere VERO — fast 63: <c>H_d0 &lt; L_d0 * (1 + 0.015)</c></description></item>
/// <item><description>deve essere FALSO — fast 79: <c>C_d1 &gt; C_d2 * (1 + 0.015)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — fast 37: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 2.5</c></description></item>
/// <item><description>deve essere FALSO — fast 137: <c>(C_d1 &lt; O_d1) E (C_d2 &gt; O_d2)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 17:00 e 03:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Non apre posizioni di venerdì</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $1,750 per contratto = 87.50 pt</description></item>
/// <item><description>Take profit: $2,500 = 125.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 48 barre (12 ore)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$49.9</description></item>
/// <item><term>Out-of-sample</term><description>$50,526 su 286 trade &#183; drawdown $21,924 &#183; profit factor 1.29 &#183; $177 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.71</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.28</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$25,876</description></item>
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
public sealed class PTS_NQ_TFU_003_15 : TfUnmirroredEngine
{
    public override string Name => "PTS_NQ_TFU_003_15";
    public override string Description =>
        "TF_U NQ 15m: famiglia 10 run 20260814, finestra 10:00–20:00 Chicago, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_TFU_003_15()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        Contracts = 1;

        StartHour = 10; // start_hour
        EndHour = 20;    // end_hour
        SkipDay = 4;    // skip_day (0 = lunedì, -1 = nessuno)

        FastYesLong = 63;  // ptn_ly_yes
        FastNoLong = 79;   // ptn_ly_no
        FastYesShort = 37; // ptn_sy_yes
        FastNoShort = 137; // ptn_sy_no

        IntradayOnly = false; // intraday_only

        StopMoney = 1750;   // stop_loss, $ per contratto = 87.50 pt
        ProfitMoney = 2500; // take_profit, $ per contratto
        MaxBars = 48;       // max_bars
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
