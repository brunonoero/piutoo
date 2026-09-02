using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_JY_TFU_003_30 - TF_U su JY a 30 minuti, <b>S37</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S37.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>JY_30m</c>, famiglia
/// <c>fam01</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: stesse entrate del TF_M (massimo e
/// minimo della sessione precedente), ma il filtro del long e quello dello short sono
/// indipendenti, quindi una delle due direzioni puo' risultare spenta.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: JY parte a 00:00 CET). Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di Chicago.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo della sessione precedente</b> (H_d1)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo della sessione precedente</b> (L_d1)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier. Le sentinelle disattivano il
/// gate: fast <c>152</c> per un gate "deve essere VERO", fast <c>153</c> per un gate "deve
/// essere FALSO". Il dossier non dichiara nulla per il gate negativo del long, che resta quindi
/// alla sentinella e <b>non filtra nulla</b>.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>55</c>: <c>H_d0 &gt; L_d0 * (1 + 0.01)</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>153</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>100</c>: <c>L_d0 &gt; L_d1</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>12</c>: <c>|O_d5-C_d1| &lt; 0.75 * (H_d5-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>04:00 e 03:00</b>, ora della ricerca (CET): la finestra attraversa la mezzanotte</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$1.000</b> per contratto = <b>0,01 pt</b></description></item>
/// <item><description>Take profit: <b>$3.000</b> = <b>0,02 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> JY, $125.000 per punto, tick 0,00005.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$317</description></item>
/// <item><term>Fuori campione</term><description>$36.346 su 77 trade</description></item>
/// <item><term>Drawdown</term><description>$7.730</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>JY_30m/consegna/trades/fam01_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>30m fam01-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
[StrategiaDisabilitata(
    "Nessuna InstrumentSpec verificata per JY. Il dossier dichiara $125.000 per punto con tick 0,00005, cioe' la quotazione scalata x100 rispetto al 6J CME (12.500.000 per punto, tick 0,0000005): le due danno lo stesso valore di tick ($6,25) ma convertono in modo diverso gli stop in denaro. Finche' non c'e' un feed @JY su cui accertare la scala, un PointValue scelto a caso falserebbe stop, target e P&L senza produrre alcun errore visibile.")]
public sealed class PTS_JY_TFU_003_30 : TfUnmirroredEngine
{
    public override string Name => "PTS_JY_TFU_003_30";
    public override string Description =>
        "TF_U JY 30m: S37 del dossier, run JY_30m, finestra 04:00-03:00 CET, multiday";
    public override string Symbol => "@JY";
    public override int TimeframeMinutes => 30;

    public PTS_JY_TFU_003_30()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(4, 3);   // start_hour 4, end_hour 3, a cavallo della mezzanotte

        FastYesLong = 55;      // ptn_fast_yes_long
        FastNoLong = 153;      // sentinella: il dossier non dichiara un gate negativo per il long
        FastYesShort = 100;    // ptn_fast_yes_short
        FastNoShort = 12;      // ptn_fast_no_short
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 1000;        // stop_loss, $ per contratto = 0,01 pt
        ProfitMoney = 3000;      // take_profit, $ per contratto = 0,02 pt
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
        if (parameters.TryGetValue("FastYesLong", out var fastYesLong))
            FastYesLong = Convert.ToInt32(fastYesLong);
        if (parameters.TryGetValue("FastNoLong", out var fastNoLong))
            FastNoLong = Convert.ToInt32(fastNoLong);
        if (parameters.TryGetValue("FastYesShort", out var fastYesShort))
            FastYesShort = Convert.ToInt32(fastYesShort);
        if (parameters.TryGetValue("FastNoShort", out var fastNoShort))
            FastNoShort = Convert.ToInt32(fastNoShort);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
