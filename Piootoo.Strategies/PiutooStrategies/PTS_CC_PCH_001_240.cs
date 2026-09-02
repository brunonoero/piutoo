using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_CC_PCH_001_240 - PC su CC a 4 ore, <b>S56</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S56.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>CC_4h</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre.</para>
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
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 1 barre</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 1 barre</b></description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// <item><description><c>breakout_offset_ticks = 0</c>: nessun tick di buffer</description></item>
/// <item><description>Nessun filtro di volatilita' (<c>dvol_min = 0</c>)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>11</c>: <c>|O_d5-C_d1| &lt; 0.5 * (H_d5-L_d1)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>54</c>: <c>(H_d1-L_d1) &gt; (H_d2-L_d2)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>27</c>: long <c>L_d0 &gt; L_d1</c>, short <c>H_d0 &lt; H_d1</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>53</c> <i>(sentinella: nessun divieto dichiarato dal dossier)</i></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>00:00 e 14:00</b>, ora della ricerca (CET)</description></item>
/// <item><description><b>Non apre</b> posizioni di venerdi' (<c>skip_day = 4</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>25.00 pt</b></description></item>
/// <item><description>Take profit: <b>$2.000</b> = <b>200.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b> (4 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> CC, $10 per punto (dollari per tonnellata su 10
/// tonnellate), tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$213</description></item>
/// <item><term>Fuori campione</term><description>$18.978 su 89 trade</description></item>
/// <item><term>Drawdown</term><description>$6.496</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>CC_4h/consegna/trades/fam01_PC.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>4h fam01-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_CC_PCH_001_240 : PriceChannelEngine
{
    public override string Name => "PTS_CC_PCH_001_240";
    public override string Description =>
        "PC CC 4 ore: S56 del dossier, run CC_4h, finestra 00:00-14:00 CET, intraday";
    public override string Symbol => "@CC";
    public override int TimeframeMinutes => 240;

    public PTS_CC_PCH_001_240()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Sui softs la fascia 00:00-01:00 non ha barre, quindi il
        // taglio stretto t > 0100 di OHLCMulti5 non ne perde nessuna.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(0, 14);   // start_hour 0, end_hour 14

        ChannelBars = 1;               // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: offset esatto, nessun tick implicito
        TickSize = 1m;                 // tick CC
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min = 0: filtro di volatilita' disattivo
        SkipDay = 4;                   // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 11;      // ptn_neut_yes
        NeutralNo = 54;       // ptn_neut_no
        DirectionalYes = 27;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 53;   // sentinella direzionale: nessun divieto dichiarato dal dossier

        IntradayOnly = true;     // intraday_only = 1
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 250;         // stop_loss, $ per contratto = 25.00 pt
        ProfitMoney = 2000;      // take_profit, $ per contratto = 200.00 pt
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
        if (parameters.TryGetValue("ChannelBars", out var channelBars))
            ChannelBars = Convert.ToInt32(channelBars);
        if (parameters.TryGetValue("OffsetTicks", out var offsetTicks))
            OffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
