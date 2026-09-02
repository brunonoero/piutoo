using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_YM_TFU_001_60 - TF_U su YM a 60 minuti, <b>S82</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S82.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>YM_1h</c>, famiglia
/// <c>fam01</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: stesse entrate del TF_M (massimo e
/// minimo della sessione precedente), ma il filtro del long e quello dello short sono
/// indipendenti, quindi una delle due direzioni puo' risultare spenta.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: YM parte a 00:00 CET). Gli orari della finestra operativa sono
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
/// essere FALSO".</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>4</c>: <c>|O_d1-C_d1| &lt; 0.75 * (H_d1-L_d1)</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>138</c>: <c>(C_d1 &gt; O_d1) E (C_d2 &lt; O_d2)</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>33</c>: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.75</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>137</c>: <c>(C_d1 &lt; O_d1) E (C_d2 &gt; O_d2)</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>19:00 e 17:00</b>, ora della ricerca (CET): la finestra attraversa la mezzanotte</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$2.500</b> per contratto = <b>500,00 pt</b></description></item>
/// <item><description>Take profit: <b>$4.000</b> = <b>800,00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> YM, $5 per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$128</description></item>
/// <item><term>Fuori campione</term><description>$92.720 su 170 trade</description></item>
/// <item><term>Drawdown</term><description>$22.253</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>YM_1h/consegna/trades/fam01_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_YM_TFU_001_60 : TfUnmirroredEngine
{
    public override string Name => "PTS_YM_TFU_001_60";
    public override string Description =>
        "TF_U YM 60m: S82 del dossier, run YM_1h, finestra 19:00-17:00 CET, multiday";
    public override string Symbol => "@YM";
    public override int TimeframeMinutes => 60;

    public PTS_YM_TFU_001_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(19, 17);   // start_hour 19, end_hour 17, a cavallo della mezzanotte

        FastYesLong = 4;       // ptn_fast_yes_long
        FastNoLong = 138;      // ptn_fast_no_long
        FastYesShort = 33;     // ptn_fast_yes_short
        FastNoShort = 137;     // ptn_fast_no_short
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 2500;        // stop_loss, $ per contratto = 500,00 pt
        ProfitMoney = 4000;      // take_profit, $ per contratto = 800,00 pt
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
