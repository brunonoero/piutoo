using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HK_SBO_001_240 - BO su HK a 4 ore, <b>S111</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S111.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260828_1933</c>, famiglia
/// <c>fam03</c>, motore <c>BO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Come il TF_M, ma il livello e' la rottura del canale delle ultime N
/// sessioni - o del massimo e minimo in costruzione della sessione corrente.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern vanno da
/// <b>01:00 a 01:00</b> nell'orologio della ricerca (<c>session_start_hour = 1</c>, tabella §2.1
/// del dossier), come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c>. Il confine conta due volte qui:
/// e' anche quello che definisce le "sessioni complete" del canale. Gli orari della finestra
/// operativa sono riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di Hong Kong.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo dell'ultima sessione completa</b> e del massimo della sessione corrente, <b>escludendo la barra in corso</b>, piu' <b>10 tick</b> (10 pt)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo dell'ultima sessione completa</b> e del minimo della sessione corrente, <b>escludendo la barra in corso</b>, meno <b>10 tick</b> (10 pt)</description></item>
/// <item><description><c>n_sess = 1</c> e <c>lev_include_sess0 = 1</c>: il livello si allarga barra dopo barra dentro la sessione in corso</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153. Attenzione: il
/// neutrale <c>52</c> qui e' un pattern vero e proprio (la barra esterna), non una sentinella -
/// la sentinella direzionale ha lo stesso numero ma vive su un'altra libreria.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>28</c>: <c>|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>52</c>: <c>(H_d0 &gt; H_d1) E (L_d0 &lt; L_d1)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>2</c>: long <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.5</c>, short <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.5</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-45</c>: long <c>(C_d1 &lt; O_d1) E (C_d2 &lt; O_d2)</c>, short la stessa a specchio</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>00:00 e 09:00</b>, ora della ricerca (CET). La finestra sta a cavallo dell'inizio sessione delle 01:00, e le due cose sono indipendenti: il filtro orario guarda l'orario della barra, non da dove parte la sessione</description></item>
/// <item><description><b>Non apre</b> posizioni di venerdi' (<c>skip_day = 4</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>39,00 pt</b></description></item>
/// <item><description>Take profit: <b>$4.000</b> = <b>624,02 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HK, $6,41 per punto, tick 1. Il valore per punto e' il
/// solo del registro che dipende da un cambio (HKD 50 per punto a 7,8 HKD/USD): se l'HKD uscisse
/// dalla banda, stop e target andrebbero rimisurati. E' con questo cambio che tornano le
/// conversioni del dossier: $250 = 39,00 pt e $4.000 = 624,02 pt.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$71</description></item>
/// <item><term>Fuori campione</term><description>$53.573 su 219 trade</description></item>
/// <item><term>Drawdown</term><description>$5.657</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260828_1933/consegna/trades/fam03_BO.csv</c>. Contano le <b>entrate</b>: timestamp e
/// prezzo. Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>,
/// che l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_HK_SBO_001_240 : SessionBreakoutEngine
{
    public override string Name => "PTS_HK_SBO_001_240";
    public override string Description =>
        "BO HK 4 ore: S111 del dossier, run run_20260828_1933, finestra 00:00-09:00 CET, intraday";
    public override string Symbol => "@HK";
    public override int TimeframeMinutes => 240;

    public PTS_HK_SBO_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Su HK la fascia 00:00-01:00 ha barre, e il taglio stretto
        // t > 0100 di OHLCMulti5 le assegna alla sessione precedente - esattamente come il
        // (timestamp - 1 min - 1h).normalize() del motore Python. Qui il confine definisce anche
        // le "sessioni complete" del canale, quindi un'ora sbagliata sposta il livello.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(0, 9);   // start_hour 0, end_hour 9

        Sessions = 1;                  // n_sess
        IncludeCurrentSession = true;  // lev_include_sess0 = 1
        BreakoutOffsetTicks = 10;      // breakout_offset_ticks: 10 tick = 10 pt su HK
        TickSize = 1m;                 // tick HK
        SkipDay = 4;                   // skip_day: venerdi' (convenzione pandas, 0 = lunedi')

        NeutralYes = 28;      // ptn_neut_yes
        NeutralNo = 52;       // ptn_neut_no: pattern vero, non la sentinella
        DirectionalYes = 2;   // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -45;  // ptn_dir_no

        IntradayOnly = true;     // intraday_only = 1

        StopMoney = 250;         // stop_loss, $ per contratto = 39,00 pt
        ProfitMoney = 4000;      // take_profit, $ per contratto = 624,02 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 0;            // max_bars = 0: nessuna uscita a tempo
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
