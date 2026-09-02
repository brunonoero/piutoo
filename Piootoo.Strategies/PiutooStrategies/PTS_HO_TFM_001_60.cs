using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HO_TFM_001_60 - TF_M su HO a 60 minuti, <b>S66</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S66.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>HO_1h</c>, famiglia
/// <c>fam02</c>, motore <c>TF_M</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following simmetrico: long e short usano lo stesso pattern, a specchio.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: HO parte a 00:00 CET). Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora NYMEX.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo della sessione precedente</b> (H_d1)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo della sessione precedente</b> (L_d1)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>46</c>: <c>(H_d0 &lt; H_d1) E (L_d0 &gt; L_d1)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>25</c>: <c>|O_d5-C_d1| &lt; 0.5 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>-41</c>: long <c>O_d0 &lt; C_d1 * (1 - 0.005)</c>, short <c>O_d0 &gt; C_d1 * (1 + 0.005)</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-12</c>: long <c>(H_d1 &lt; H_d2) E (L_d1 &lt; L_d2)</c>, short la stessa a specchio</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>05:00 e 04:00</b>, ora della ricerca (CET): la finestra attraversa la mezzanotte</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$4.000</b> per contratto = <b>0,10 pt</b></description></item>
/// <item><description>Take profit: <b>$5.000</b> = <b>0,12 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>12 barre</b> (12 ore)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HO, $42.000 per punto, tick 0,0001.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$173</description></item>
/// <item><term>Fuori campione</term><description>$31.295 su 48 trade</description></item>
/// <item><term>Drawdown</term><description>$9.250</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>HO_1h/consegna/trades/fam02_TF_M.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>1h fam02-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_HO_TFM_001_60 : TfMirroredEngine
{
    public override string Name => "PTS_HO_TFM_001_60";
    public override string Description =>
        "TF_M HO 60m: S66 del dossier, run HO_1h, finestra 05:00-04:00 CET, intraday";
    public override string Symbol => "@HO";
    public override int TimeframeMinutes => 60;

    public PTS_HO_TFM_001_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(5, 4);   // start_hour 5, end_hour 4, a cavallo della mezzanotte

        NeutralYes = 46;      // ptn_neut_yes
        NeutralNo = 25;       // ptn_neut_no
        DirectionalYes = -41; // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -12;  // ptn_dir_no
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = true;     // intraday_only = 1

        StopMoney = 4000;        // stop_loss, $ per contratto = 0,10 pt
        ProfitMoney = 5000;      // take_profit, $ per contratto = 0,12 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 12;           // max_bars
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
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
