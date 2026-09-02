using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_KC_SBO_001_240 - BO su KC a 4 ore, <b>S26</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S26.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>KC_4h</c>, famiglia
/// <c>fam01</c>, motore <c>BO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout sugli estremi delle ultime N sessioni.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern cominciano alle
/// <b>01:00 dell'orologio della ricerca</b> (CET) e durano fino alla stessa ora del giorno dopo:
/// e' quanto la tabella §2.1 del dossier dichiara per KC, cioe' <c>session_start_hour = 1</c> nel
/// taglio <c>(timestamp - 1 min - session_start_hour).normalize()</c> del motore Python. Non e' la
/// sessione ICE. Gli orari della finestra operativa sono riportati <b>verbatim</b> dalla ricerca,
/// mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo in costruzione della sessione corrente</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo in costruzione della sessione corrente</b></description></item>
/// <item><description><c>n_sess = 1</c>, <c>lev_include_sess0 = 1</c>, <c>breakout_offset_ticks = 0</c>: l'offset e' esattamente <c>0 x tick</c>, senza tick impliciti</description></item>
/// <item><description>Il livello usa gli estremi della sessione corrente <b>prima</b> della barra in valutazione: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'e' look-ahead</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>29</c>: <c>|O_d5-C_d1| &gt; 0.5 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>56</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere VERO - direzionale <c>-9</c>: long <c>O_d0 - L_d0 &lt; O_d1 - L_d1</c>, short <c>H_d0 - O_d0 &lt; H_d1 - O_d1</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>4</c>: long <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 1.0</c>, short <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 1.0</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>0,67 pt</b></description></item>
/// <item><description>Take profit: <b>$7.500</b> = <b>20.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b> (4 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> KC, $375 per centesimo di dollaro per libbra
/// (37.500 libbre), tick 0,05.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$526</description></item>
/// <item><term>Fuori campione</term><description>$96.872 su 184 trade</description></item>
/// <item><term>Drawdown</term><description>$6.464</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>KC_4h/consegna/trades/fam01_BO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_KC_SBO_001_240 : SessionBreakoutEngine
{
    public override string Name => "PTS_KC_SBO_001_240";
    public override string Description =>
        "BO KC 4 ore: S26 del dossier, run KC_4h, finestra 24h, intraday";
    public override string Symbol => "@KC";
    public override int TimeframeMinutes => 240;

    public PTS_KC_SBO_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca, non dalla mezzanotte e non nell'ora ICE di New York.
        // Sui softs la fascia 00:00-01:00 non ha barre, quindi il taglio stretto t > 0100 di
        // OHLCMulti5 non ne perde nessuna.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Nessun filtro orario nel run: finestra piena, dichiarata comunque nell'orologio della
        // ricerca perche' ogni PTS deve dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        Sessions = 1;                  // n_sess
        IncludeCurrentSession = true;  // lev_include_sess0 = 1
        BreakoutOffsetTicks = 0;       // breakout_offset_ticks
        TickSize = 0.05m;              // tick KC
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 29;      // ptn_neut_yes
        NeutralNo = 56;       // sentinella neutra: nessun divieto dichiarato dal dossier
        DirectionalYes = -9;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 4;    // ptn_dir_no

        IntradayOnly = true;     // intraday_only = 1

        StopMoney = 250;         // stop_loss, $ per contratto = 0,67 pt
        ProfitMoney = 7500;      // take_profit, $ per contratto = 20.00 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 24;           // max_bars
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
