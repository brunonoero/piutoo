using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_FDAX_TFU_001_1440 - TF_U su FDAX a 1 giorno, <b>S08</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S08.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260825_1615</c>, famiglia
/// <c>fam01</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: il filtro del long e quello dello short sono indipendenti.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern cominciano alle
/// <b>01:00 dell'orologio della ricerca</b> (CET) e durano fino alla stessa ora del giorno dopo:
/// e' quanto la tabella §2.1 del dossier dichiara per FDAX, cioe'
/// <c>session_start_hour = 1</c> nel taglio <c>(timestamp - 1 min - session_start_hour).normalize()</c>
/// del motore Python. Non e' la sessione del broker. Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo della sessione precedente</b> (H_d1)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo della sessione precedente</b> (L_d1)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive. Le sentinelle disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153 -
/// quindi un gate lasciato alla sentinella <b>non filtra nulla</b>, non e' un filtro con soglia
/// altissima.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>3</c>: <c>|O_d1-C_d1| &lt; 0.5 * (H_d1-L_d1)</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>24</c>: <c>|O_d5-C_d1| &lt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>148</c>: <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>149</c>: <c>close &lt; O_d0</c></description></item>
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
/// <item><description>Take profit: <b>$6.000</b> = <b>240.00 pt</b></description></item>
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
/// <item><term>Atteso per trade</term><description>$1.082</description></item>
/// <item><term>Fuori campione</term><description>$219.638 su 203 trade</description></item>
/// <item><term>Drawdown</term><description>$17.519</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260825_1615/consegna/trades/fam01_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>day fam01-6</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_FDAX_TFU_001_1440 : TfUnmirroredEngine
{
    public override string Name => "PTS_FDAX_TFU_001_1440";
    public override string Description =>
        "TF_U FDAX day: S08 del dossier, run run_20260825_1615, finestra 24h, multiday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 1440;

    public PTS_FDAX_TFU_001_1440()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca, non dalla mezzanotte e non nell'ora di borsa di Francoforte.
        // OHLCMulti5 taglia con t > 0100, quindi una barra etichettata fra 00:00 e 01:00 resterebbe
        // fuori dagli aggregati invece di cadere nella sessione precedente: su FDAX quella fascia
        // non ha barre, quindi la differenza non si vede.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Nessun filtro orario nel run: finestra piena, dichiarata comunque nell'orologio della
        // ricerca perche' ogni PTS deve dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        FastYesLong = 3;      // ptn_fast_yes_long
        FastNoLong = 24;      // ptn_fast_no_long
        FastYesShort = 148;   // ptn_fast_yes_short
        FastNoShort = 149;    // ptn_fast_no_short
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 1000;        // stop_loss, $ per contratto = 40.00 pt
        ProfitMoney = 6000;      // take_profit, $ per contratto = 240.00 pt
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
