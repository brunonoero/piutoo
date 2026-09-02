using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_CT_TFU_001_240 - TF_U su CT a 4 ore, <b>S80</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S80.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>CT_4h</c>, famiglia
/// <c>fam01</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: stesse entrate del TF_M (massimo e
/// minimo della sessione precedente), ma il filtro del long e quello dello short sono
/// indipendenti, quindi una delle due direzioni puo' risultare spenta.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern vanno da
/// <b>01:00 a 01:00</b> nell'orologio della ricerca (<c>session_start_hour = 1</c>, tabella §2.1
/// del dossier), come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c>. Non e' la sessione ICE.</para>
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
/// <item><description>LONG deve essere VERO - fast <c>5</c>: <c>|O_d1-C_d1| &gt; 0.25 * (H_d1-L_d1)</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>114</c>: <c>H_d1 - C_d1 &lt; 0.2 * (H_d1-L_d1)</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>150</c>: <c>close &lt; O_d0 * 0.995</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>41</c>: <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.5</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$3.000</b> per contratto = <b>6,00 pt</b></description></item>
/// <item><description>Take profit: <b>$1.500</b> = <b>3,00 pt</b></description></item>
/// <item><description>Lo stop e' il <b>doppio</b> del target: e' voluto, il dossier lo riporta cosi'</description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> CT, $500 per centesimo di libbra, tick 0,01. La
/// quotazione e' in <b>centesimi</b> per libbra su un contratto da 50.000 libbre: il valore per
/// "punto" e' il dollaro per centesimo, ed e' cosi' che tornano le conversioni del dossier
/// ($3.000 = 6,00 e $1.500 = 3,00).</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$133</description></item>
/// <item><term>Fuori campione</term><description>$37.111 su 88 trade</description></item>
/// <item><term>Drawdown</term><description>$14.835</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>CT_4h/consegna/trades/fam01_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_CT_TFU_001_240 : TfUnmirroredEngine
{
    public override string Name => "PTS_CT_TFU_001_240";
    public override string Description =>
        "TF_U CT 4 ore: S80 del dossier, run CT_4h, finestra 24h, multiday";
    public override string Symbol => "@CT";
    public override int TimeframeMinutes => 240;

    public PTS_CT_TFU_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Sui softs la fascia 00:00-01:00 non ha barre, quindi il
        // taglio stretto t > 0100 di OHLCMulti5 non ne perde nessuna.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Il run non dichiara filtro orario: la finestra e' piena, nell'orologio della ricerca,
        // perche' ogni PTS deve dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        FastYesLong = 5;       // ptn_fast_yes_long
        FastNoLong = 114;      // ptn_fast_no_long
        FastYesShort = 150;    // ptn_fast_yes_short
        FastNoShort = 41;      // ptn_fast_no_short
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 3000;        // stop_loss, $ per contratto = 6,00 pt
        ProfitMoney = 1500;      // take_profit, $ per contratto = 3,00 pt
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
