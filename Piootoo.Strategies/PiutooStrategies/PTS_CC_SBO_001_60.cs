using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_CC_SBO_001_60 - BO su CC a 60 minuti, <b>S30</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S30.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>CC_1h</c>, famiglia
/// <c>fam01</c>, motore <c>BO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout sugli estremi delle ultime N sessioni.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern cominciano alle
/// <b>01:00 dell'orologio della ricerca</b> (CET) e durano fino alla stessa ora del giorno dopo:
/// e' quanto la tabella §2.1 del dossier dichiara per CC, cioe' <c>session_start_hour = 1</c> nel
/// taglio <c>(timestamp - 1 min - session_start_hour).normalize()</c> del motore Python. Non e' la
/// sessione ICE. Gli orari della finestra operativa sono riportati <b>verbatim</b> dalla ricerca,
/// mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 5 sessioni complete e della sessione corrente</b>, + 5 tick (5,00 pt)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 5 sessioni complete e della sessione corrente</b>, - 5 tick</description></item>
/// <item><description><c>n_sess = 5</c>, <c>lev_include_sess0 = 1</c>, <c>breakout_offset_ticks = 5</c></description></item>
/// <item><description>Gli estremi della sessione corrente escludono la barra in valutazione: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'e' look-ahead</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>47</c>: <c>(H_d1-L_d1) &lt; ((H_d2-L_d2) + (H_d3-L_d3)) / 2</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>17</c>: <c>|O_d5-C_d1| &gt; 0.5 * (H_d5-L_d1)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>2</c>: long <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.5</c>, short <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.5</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>10</c>: long <c>(C_d1 &gt; C_d2) E (C_d2 &gt; C_d3) E (C_d3 &gt; C_d4)</c>, short la stessa a specchio</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>11:00 e 19:00</b>, ora della ricerca (CET)</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$1.750</b> per contratto = <b>175.00 pt</b></description></item>
/// <item><description>Take profit: <b>nessuno</b> (<c>take_profit = 0</c>)</description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>92 barre</b> (3,8 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> CC, $10 per punto (dollari per tonnellata su 10
/// tonnellate), tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$397</description></item>
/// <item><term>Fuori campione</term><description>$41.186 su 43 trade</description></item>
/// <item><term>Drawdown</term><description>$16.790</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>CC_1h/consegna/trades/fam01_BO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_CC_SBO_001_60 : SessionBreakoutEngine
{
    public override string Name => "PTS_CC_SBO_001_60";
    public override string Description =>
        "BO CC 60m: S30 del dossier, run CC_1h, finestra 11:00-19:00 CET, multiday";
    public override string Symbol => "@CC";
    public override int TimeframeMinutes => 60;

    public PTS_CC_SBO_001_60()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Sui softs la fascia 00:00-01:00 non ha barre, quindi il
        // taglio stretto t > 0100 di OHLCMulti5 non ne perde nessuna.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(11, 19);   // start_hour 11, end_hour 19

        Sessions = 5;                  // n_sess
        IncludeCurrentSession = true;  // lev_include_sess0 = 1
        BreakoutOffsetTicks = 5;       // breakout_offset_ticks = 5 -> 5,00 pt su tick 1
        TickSize = 1m;                 // tick CC
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 47;      // ptn_neut_yes
        NeutralNo = 17;       // ptn_neut_no
        DirectionalYes = 2;   // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 10;   // ptn_dir_no

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 1750;        // stop_loss, $ per contratto = 175.00 pt
        ProfitMoney = 0;         // take_profit = 0: nessun target
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 92;           // max_bars
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
