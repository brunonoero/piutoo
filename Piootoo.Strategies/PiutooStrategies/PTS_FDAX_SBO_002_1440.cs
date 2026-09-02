using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_FDAX_SBO_002_1440 - BO su FDAX a 1 giorno, <b>S10</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S10.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260825_1615</c>, famiglia
/// <c>fam02</c>, motore <c>BO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout sugli estremi delle ultime N sessioni.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00 nell'orologio della ricerca, come tutte le
/// altre FDAX del catalogo. La tabella §2.1 del dossier dichiara <c>session_start_hour = 1</c>
/// per FDAX, ma <c>ZonedWindow.ResearchSession(1)</c> oggi non lo traduce fedelmente: la
/// compensazione dell'etichettatura all'apertura del feed, in <c>EasyLib.OHLCMulti5</c>, vale solo
/// per <c>session_start_hour = 0</c>, e con inizio alle 01:00 resta il confronto stretto
/// <c>t &gt; 0100</c> che lascia fuori da ogni sessione le barre fino alle 01:00 incluse. Sui feed
/// FDAX in uso le due forme coincidono - la daily e' etichettata alla chiusura europea, Roma
/// 22:00/23:00 - ma quella dichiarata qui e' l'unica che regge anche su un feed con barre in
/// quella fascia, e l'unica che rende le sette FDAX confrontabili fra loro. Non e' la sessione
/// del broker. Gli orari della finestra operativa sono riportati <b>verbatim</b> dalla ricerca,
/// mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 5 sessioni complete</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 5 sessioni complete</b></description></item>
/// <item><description><c>n_sess = 5</c>, <c>lev_include_sess0 = 0</c>: la sessione in corso <b>non</b> entra nel livello, che resta fermo per tutta la sessione</description></item>
/// <item><description><c>breakout_offset_ticks = 0</c>: l'offset e' esattamente <c>0 x tick</c>, senza tick impliciti</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>34</c>: <c>(H_d0-L_d0) &gt; L_d0 * 0.015</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>36</c>: <c>(H_d0-L_d0) &gt; L_d0 * 0.025</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>-1</c>: long <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.25</c>, short <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.25</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>38</c>: long <c>H_d1 - C_d1 &lt; 0.2 * (H_d1-L_d1)</c>, short <c>C_d1 - L_d1 &lt; 0.2 * (H_d1-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$1.000</b> per contratto = <b>40.00 pt</b></description></item>
/// <item><description>Take profit: <b>$15.000</b> = <b>600.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>5 barre</b> (5 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$1.057</description></item>
/// <item><term>Fuori campione</term><description>$79.179 su 49 trade</description></item>
/// <item><term>Drawdown</term><description>$25.429</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260825_1615/consegna/trades/fam02_BO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_FDAX_SBO_002_1440 : SessionBreakoutEngine
{
    public override string Name => "PTS_FDAX_SBO_002_1440";
    public override string Description =>
        "BO FDAX day: S10 del dossier, run run_20260825_1615, finestra 24h, multiday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 1440;

    public PTS_FDAX_SBO_002_1440()
    {
        // Giorno di calendario europeo, 00:00 -> 00:00, come le altre FDAX del catalogo.
        // La tabella §2.1 del dossier dichiara 01:00 per FDAX, ma ResearchSession(1) oggi non lo
        // traduce fedelmente: OHLCMulti5 compensa l'etichettatura all'apertura del feed solo per
        // session_start_hour = 0 (ramo calendarDaySession) e con start 01:00 tiene il confronto
        // stretto t > 0100, che lascia fuori da OGNI sessione le barre fino alle 01:00 incluse.
        // Misurato: su @FDAX_1440 (FTMO, barre a Roma 22:00/23:00) le due forme sono identiche,
        // ma su @FDAX_240 interno ci sono 1.635 barre alle 00:00 che il taglio a 01:00 perderebbe.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Nessun filtro orario nel run: finestra piena, dichiarata comunque nell'orologio della
        // ricerca perche' ogni PTS deve dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        Sessions = 5;                   // n_sess
        IncludeCurrentSession = false;  // lev_include_sess0 = 0
        BreakoutOffsetTicks = 0;        // breakout_offset_ticks
        TickSize = 1m;                  // tick FDAX
        SkipDay = -1;                   // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 34;      // ptn_neut_yes
        NeutralNo = 36;       // ptn_neut_no
        DirectionalYes = -1;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 38;   // ptn_dir_no

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 1000;        // stop_loss, $ per contratto = 40.00 pt
        ProfitMoney = 15000;     // take_profit, $ per contratto = 600.00 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 5;            // max_bars
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
        if (parameters.TryGetValue("TrailingStop", out var trailing))
            TrailingStopMoney = Convert.ToInt32(trailing);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
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
        if (parameters.TryGetValue("Sessions", out var sessions))
            Sessions = Convert.ToInt32(sessions);
        if (parameters.TryGetValue("BreakoutOffsetTicks", out var offsetTicks))
            BreakoutOffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
