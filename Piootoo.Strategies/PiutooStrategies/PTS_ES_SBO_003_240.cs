using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_SBO_003_240 - BO su ES a 4 ore, <b>S18</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S18.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1847</c>, famiglia
/// <c>fam02</c>, motore <c>BO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout sugli estremi delle ultime N sessioni.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non e' la sessione del broker: le due coincidono quasi sempre, ma non nelle settimane in cui
/// l'ora legale americana ed europea non sono allineate. Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 3 sessioni complete</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 3 sessioni complete</b></description></item>
/// <item><description><c>n_sess = 3</c>, <c>lev_include_sess0 = 0</c>, <c>breakout_offset_ticks = 0</c>: l'offset e' esattamente <c>0 x tick</c>, senza tick impliciti</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>14</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>1</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>34</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-35</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>14:00 e 09:00</b> (a cavallo della mezzanotte), ora della ricerca (CET)</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$4.000</b> per contratto = <b>80.00 pt</b></description></item>
/// <item><description>Take profit: <b>$6.000</b> = <b>120.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>50 barre</b></description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$571</description></item>
/// <item><term>Fuori campione</term><description>$94.126 su 78 trade</description></item>
/// <item><term>Drawdown</term><description>$28.958</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1847/consegna/trades/fam02_BO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>4h fam02-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_ES_SBO_003_240 : SessionBreakoutEngine
{
    public override string Name => "PTS_ES_SBO_003_240";
    public override string Description =>
        "BO ES 4 ore: S18 del dossier, run run_20260824_1847, finestra 14:00-09:00 CET, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 240;

    public PTS_ES_SBO_003_240()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(14, 9);   // start_hour 14, end_hour 9

        Sessions = 3;                  // n_sess
        IncludeCurrentSession = false;; // lev_include_sess0 = 0
        BreakoutOffsetTicks = 0;       // breakout_offset_ticks
        TickSize = 0.25m;              // tick ES
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 14;      // ptn_neut_yes
        NeutralNo = 1;       // ptn_neut_no
        DirectionalYes = 34;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -35;   // ptn_dir_no

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 4000;        // stop_loss, $ per contratto = 80.00 pt
        ProfitMoney = 6000;      // take_profit, $ per contratto = 120.00 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 50;           // max_bars
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
