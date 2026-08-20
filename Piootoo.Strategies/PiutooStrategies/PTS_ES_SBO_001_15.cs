using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_SBO_001_15 — BO su ES a 15 minuti, strategia <b>S02</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260819_1008</c>,
/// famiglia 01 strategia 1 di <c>run-04-agosto/parametri.csv</c>).
///
/// <para>Breakout sugli estremi delle ultime <b>3 sessioni chiuse</b>, ordine stop valido solo
/// sulla barra successiva e riemesso finché la condizione regge (cancel &amp; replace a ogni
/// barra). Il canale <b>non</b> ingloba la sessione in costruzione
/// (<c>lev_include_sess0 = 0</c>).</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate. Lo stesso
/// confine governa il secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern, livelli e
/// limite di fill insieme.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e finestra dichiarano il
/// proprio fuso e il confronto passa dall'istante assoluto della barra. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 3 sessioni complete</b> + 2 tick (0.50 pt)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 3 sessioni complete</b> − 2 tick (0.50 pt)</description></item>
/// <item><description>L'offset è esattamente <c>2 × tick</c>: nessun tick implicito (rettifica del 17/08/2026)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 12: <c>|O_d5-C_d1| &lt; 0.75 * (H_d5-L_d1)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 1: <c>|O_d1-C_d1| &lt; 0.1 * (H_d1-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale 34: <c>H_d1 &lt; H_d5</c></description></item>
/// <item><description>deve essere FALSO — direzionale -48: <c>close &lt; O_d0 * 1.005</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale 34: <c>L_d1 &gt; L_d5</c></description></item>
/// <item><description>deve essere FALSO — direzionale -48: <c>close &gt; O_d0 * 0.995</c></description></item>
/// </list>
/// <para>Il segno del pattern direzionale lo applica il motore per verso: si dichiara il numero
/// una volta sola, <c>34</c> e <c>-48</c>, come nel report.</para>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>03:00 e 02:00</b> (a cavallo della mezzanotte), ora dei dati (CET), estremi inclusi su HHMM pieni</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Può restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione — limite sul <b>fill</b>, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $4,000 per contratto = 80.00 pt</description></item>
/// <item><description>Take profit: $6,000 = 120.00 pt</description></item>
/// <item><description>Uscita a tempo dopo <b>920 barre</b> (9.6 giorni di calendario)</description></item>
/// <item><description>Nessun trailing, nessun breakeven</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$268.1</description></item>
/// <item><term>Out-of-sample</term><description>$90,062 su 72 trade &#183; drawdown $27,908 &#183; profit factor 1.75 &#183; $1,251 per trade</description></item>
/// <item><term>Plateau minimo</term><description>0.41</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.21</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$36,561</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S02_15m_BO.csv</c>
/// (= <c>run-engine/run-04-agosto/trades/fam01_BO.csv</c>). Contano le <b>entrate</b>: timestamp
/// e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di slippage per
/// lato, che l'engine non applica e va rettificato al confronto.</para>
/// </summary>
public sealed class PTS_ES_SBO_001_15 : SessionBreakoutEngine
{
    public override string Name => "PTS_ES_SBO_001_15";
    public override string Description =>
        "BO ES 15m: famiglia 01 run 20260819_1008, canale 3 sessioni + 2 tick, finestra 03:00–02:00 CET, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 15;

    public PTS_ES_SBO_001_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        Sessions = 3;                  // n_sess
        IncludeCurrentSession = false; // lev_include_sess0 = 0
        BreakoutOffsetTicks = 2;       // breakout_offset_ticks = 2 -> 0.50 pt
        TickSize = 0.25m;              // tick ES

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(3, 2);
        SkipDay = -1;         // skip_day (0 = lunedi', -1 = nessuno)

        NeutralYes = 12;      // ptn_neut_yes
        NeutralNo = 1;        // ptn_neut_no
        DirectionalYes = 34;  // ptn_dir_yes
        DirectionalNo = -48;  // ptn_dir_no

        IntradayOnly = false; // intraday_only = 0: multiday, niente CloseAtUtc di fine sessione

        StopMoney = 4000;     // stop_loss, $ per contratto = 80.00 pt
        ProfitMoney = 6000;   // take_profit, $ per contratto = 120.00 pt
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 920;        // max_bars
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
        if (parameters.TryGetValue("Sessions", out var sessions))
            Sessions = Convert.ToInt32(sessions);
        if (parameters.TryGetValue("BreakoutOffsetTicks", out var offsetTicks))
            BreakoutOffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
