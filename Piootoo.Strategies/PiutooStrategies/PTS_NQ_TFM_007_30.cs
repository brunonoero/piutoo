using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFM_007_30 — TF_M su NQ a 30 minuti, famiglia 02 della consegna
/// <c>run_20260815_1021</c>.
///
/// <para>Trend following simmetrico: stop buy su <c>H_d1</c>, stop sell su <c>L_d1</c>. Long e short
/// condividono i gate neutri e usano i direzionali a specchio.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono
/// ricostruite dalle barre intraday con confine a mezzanotte, come nella ricerca: la
/// sessione è il giorno di calendario del feed, non la sessione CME 17:00–16:00. Per questo
/// <c>SessionStartTime</c> = 0 e <c>SessionEndTime</c> = 2359. Lo stesso confine governa il
/// secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern e limite di fill insieme.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 47: <c>(H_d1-L_d1) &lt; ((H_d2-L_d2) + (H_d3-L_d3)) / 2</c></description></item>
/// <item><description>deve essere FALSO — neutrale 48: <c>((H_d1-L_d1) &lt; (H_d2-L_d2)) E ((H_d2-L_d2) &lt; (H_d3-L_d3))</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale 50: <c>close &gt; O_d0 * 1.005</c></description></item>
/// <item><description>deve essere FALSO — direzionale 7: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 2.5</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale 50: <c>close &lt; O_d0 * 0.995</c></description></item>
/// <item><description>deve essere FALSO — direzionale 7: <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 2.5</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 02:00 e 01:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $5,000 per contratto = 250.00 pt</description></item>
/// <item><description>Take profit: $3,000 = 150.00 pt</description></item>
/// <item><description>Uscita a tempo dopo 24 barre (12 ore)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$464.1</description></item>
/// <item><term>Out-of-sample</term><description>$113,244 su 244 trade &#183; drawdown $28,296 &#183; profit factor 1.52 &#183; $464 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.8</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>1.0</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$37,627</description></item>
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
public sealed class PTS_NQ_TFM_007_30 : TfMirroredEngine
{
    public override string Name => "PTS_NQ_TFM_007_30";
    public override string Description =>
        "TF_M NQ 30m: famiglia 02 run 20260815, finestra 19:00–18:00 Chicago, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public PTS_NQ_TFM_007_30()
    {
        // Sessione = giorno di calendario del feed, come la ricerca.
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        Contracts = 1;

        StartHour = 19; // start_hour
        EndHour = 18;   // end_hour
        SkipDay = -1;  // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 47;     // ptn_neut_yes
        NeutralNo = 48;      // ptn_neut_no
        DirectionalYes = 50; // ptn_dir_yes, specchiato dal motore
        DirectionalNo = 7;   // ptn_dir_no, specchiato dal motore

        IntradayOnly = false; // intraday_only

        StopMoney = 5000;   // stop_loss, $ per contratto = 250.00 pt
        ProfitMoney = 3000; // take_profit, $ per contratto
        MaxBars = 24;       // max_bars
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
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            StartHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndHour = Convert.ToInt32(endHour);
    }
}
