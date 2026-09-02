using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HK_TFU_001_240 - TF_U su HK a 4 ore, <b>S57</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S57.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260828_1933</c>, famiglia
/// <c>fam01</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: stesse entrate del TF_M (massimo e
/// minimo della sessione precedente), ma il filtro del long e quello dello short sono
/// indipendenti, quindi una delle due direzioni puo' risultare spenta.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern vanno da
/// <b>01:00 a 01:00</b> nell'orologio della ricerca (<c>session_start_hour = 1</c>, tabella §2.1
/// del dossier), come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c>. Non e' la sessione HKEX: gli
/// orari della finestra operativa sono riportati <b>verbatim</b> dalla ricerca, mai convertiti
/// nell'ora di Hong Kong.</para>
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
/// <item><description>LONG deve essere VERO - fast <c>70</c>: <c>C_d1 &gt; O_d1</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>2</c>: <c>|O_d1-C_d1| &lt; 0.25 * (H_d1-L_d1)</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>119</c>: <c>O_d0 &lt; C_d1 * (1 - 0.0025)</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>6</c>: <c>|O_d1-C_d1| &gt; 0.5 * (H_d1-L_d1)</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>00:00 e 06:00</b>, ora della ricerca (CET). La finestra sta a cavallo dell'inizio sessione delle 01:00, e le due cose sono indipendenti: il filtro orario guarda l'orario della barra, non da dove parte la sessione</description></item>
/// <item><description><b>Non apre</b> posizioni di venerdi' (<c>skip_day = 4</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>39,00 pt</b></description></item>
/// <item><description>Take profit: <b>$7.500</b> = <b>1.170,05 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HK, $6,41 per punto, tick 1. Il valore per punto e' il
/// solo del registro che dipende da un cambio (HKD 50 per punto a 7,8 HKD/USD): se l'HKD uscisse
/// dalla banda, stop e target andrebbero rimisurati. E' con questo cambio che tornano le
/// conversioni del dossier: $250 = 39,00 pt e $7.500 = 1.170,05 pt.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$210</description></item>
/// <item><term>Fuori campione</term><description>$55.872 su 174 trade</description></item>
/// <item><term>Drawdown</term><description>$5.638</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260828_1933/consegna/trades/fam01_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp
/// e prezzo. Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>,
/// che l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_HK_TFU_001_240 : TfUnmirroredEngine
{
    public override string Name => "PTS_HK_TFU_001_240";
    public override string Description =>
        "TF_U HK 4 ore: S57 del dossier, run run_20260828_1933, finestra 00:00-06:00 CET, intraday";
    public override string Symbol => "@HK";
    public override int TimeframeMinutes => 240;

    public PTS_HK_TFU_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Su HK la fascia 00:00-01:00 ha barre, e il taglio stretto
        // t > 0100 di OHLCMulti5 le assegna alla sessione precedente - esattamente come il
        // (timestamp - 1 min - 1h).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(0, 6);   // start_hour 0, end_hour 6

        FastYesLong = 70;      // ptn_fast_yes_long
        FastNoLong = 2;        // ptn_fast_no_long
        FastYesShort = 119;    // ptn_fast_yes_short
        FastNoShort = 6;       // ptn_fast_no_short
        SkipDay = 4;          // skip_day: venerdi' (convenzione pandas, 0 = lunedi')

        IntradayOnly = true;     // intraday_only = 1

        StopMoney = 250;         // stop_loss, $ per contratto = 39,00 pt
        ProfitMoney = 7500;      // take_profit, $ per contratto = 1.170,05 pt
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
