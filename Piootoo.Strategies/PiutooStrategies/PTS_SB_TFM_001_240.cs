using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_SB_TFM_001_240 - TF_M su SB a 4 ore, <b>S89</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S89.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>SB_4h</c>, famiglia
/// <c>fam01</c>, motore <c>TF_M</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following simmetrico: long e short usano lo stesso pattern, a specchio.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern vanno da
/// <b>01:00 a 01:00</b> nell'orologio della ricerca (<c>session_start_hour = 1</c>, tabella §2.1
/// del dossier), come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c>. Non e' la sessione ICE: gli
/// orari della finestra operativa sono riportati <b>verbatim</b> dalla ricerca.</para>
///
/// <para>SB e' l'unico strumento della tabella §2.1 con sessioni del <b>sabato</b> (14): non e'
/// un difetto del feed, e un port che le scarta perde quelle barre.</para>
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
/// <item><description>deve essere VERO - neutrale <c>34</c>: <c>(H_d0-L_d0) &gt; L_d0 * 0.015</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>22</c>: <c>|O_d5-C_d1| &gt; 2.5 * (H_d5-L_d1)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>48</c>: long <c>close &gt; O_d0 * 0.995</c>, short <c>close &lt; O_d0 * 1.005</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-11</c>: long <c>(C_d1 &lt; C_d2) E (C_d2 &lt; C_d3) E (C_d3 &lt; C_d4) E (C_d4 &lt; C_d5)</c>, short la stessa a specchio</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>13:00 e 23:59</b>, ora della ricerca (CET). La fine e' <c>23:59</c> e non un'ora piena: e' scritta cosi' nella ricerca e va riportata verbatim, per questo la finestra usa <c>ZonedWindow.Research</c> invece di <c>ResearchHours</c></description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$2.250</b> per contratto = <b>2,01 pt</b></description></item>
/// <item><description>Take profit: <b>$3.000</b> = <b>2,68 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b> (4,0 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> SB, $1.120 per centesimo di libbra, tick 0,01. La
/// quotazione e' in <b>centesimi</b> per libbra su un contratto da 112.000 libbre: il valore per
/// "punto" e' il dollaro per centesimo, ed e' cosi' che tornano le conversioni del dossier
/// ($2.250 = 2,01 e $3.000 = 2,68).</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$112</description></item>
/// <item><term>Fuori campione</term><description>$34.954 su 104 trade</description></item>
/// <item><term>Drawdown</term><description>$4.344</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>SB_4h/consegna/trades/fam01_TF_M.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_SB_TFM_001_240 : TfMirroredEngine
{
    public override string Name => "PTS_SB_TFM_001_240";
    public override string Description =>
        "TF_M SB 4 ore: S89 del dossier, run SB_4h, finestra 13:00-23:59 CET, multiday";
    public override string Symbol => "@SB";
    public override int TimeframeMinutes => 240;

    public PTS_SB_TFM_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Sui softs la fascia 00:00-01:00 non ha barre, quindi il
        // taglio stretto t > 0100 di OHLCMulti5 non ne perde nessuna.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.Research(1300, 2359);   // start_hour 13, fine a 23:59

        NeutralYes = 34;      // ptn_neut_yes
        NeutralNo = 22;       // ptn_neut_no
        DirectionalYes = 48;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -11;  // ptn_dir_no
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 2250;        // stop_loss, $ per contratto = 2,01 pt
        ProfitMoney = 3000;      // take_profit, $ per contratto = 2,68 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 24;           // max_bars: 24 barre da 4 ore, 4,0 giorni di calendario
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
