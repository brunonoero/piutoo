using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HK_PCH_002_240 - PC su HK a 4 ore, <b>S109</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S109.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260828_1933</c>, famiglia
/// <c>fam02</c>, motore <c>PC</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre.</para>
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
/// <item><description>LONG: stop buy sul <b>massimo dell'ultima barra</b> piu' <b>2 tick</b> (2 pt)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo dell'ultima barra</b> meno <b>2 tick</b> (2 pt)</description></item>
/// <item><description>Con <c>channel_len = 1</c> il canale e' la <b>sola barra di segnale</b>: e' un breakout della barra precedente, non di una finestra</description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// <item><description>Nessun filtro di volatilita' (<c>dvol_min = 0</c>)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>55</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere FALSO - neutrale <c>35</c>: <c>(H_d0-L_d0) &gt; L_d0 * 0.02</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>27</c>: long <c>L_d0 &gt; L_d1</c>, short <c>H_d0 &lt; H_d1</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-50</c>: long <c>close &lt; O_d0 * 0.995</c>, short <c>close &gt; O_d0 * 1.005</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>00:00 e 06:00</b>, ora della ricerca (CET). La finestra sta a cavallo dell'inizio sessione delle 01:00, e le due cose sono indipendenti: il filtro orario guarda l'orario della barra, non da dove parte la sessione</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>39,00 pt</b></description></item>
/// <item><description>Take profit: <b>$7.500</b> = <b>1.170,05 pt</b></description></item>
/// <item><description>Trailing stop: <b>$1.000</b> = <b>156,01 pt</b></description></item>
/// <item><description>Breakeven a <b>$500</b> = <b>78,00 pt</b> di utile</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b> (4,0 giorni di calendario)</description></item>
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
/// <item><term>Atteso per trade</term><description>$72</description></item>
/// <item><term>Fuori campione</term><description>$80.681 su 387 trade</description></item>
/// <item><term>Drawdown</term><description>$4.804</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260828_1933/consegna/trades/fam02_PC.csv</c>. Contano le <b>entrate</b>: timestamp e
/// prezzo. Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>,
/// che l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_HK_PCH_002_240 : PriceChannelEngine
{
    public override string Name => "PTS_HK_PCH_002_240";
    public override string Description =>
        "PC HK 4 ore: S109 del dossier, run run_20260828_1933, finestra 00:00-06:00 CET, intraday";
    public override string Symbol => "@HK";
    public override int TimeframeMinutes => 240;

    public PTS_HK_PCH_002_240()
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

        ChannelBars = 1;               // channel_len: il canale e' la sola barra di segnale
        OffsetTicks = 2;               // breakout_offset_ticks: 2 tick = 2 pt su HK
        TickSize = 1m;                 // tick HK
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min = 0: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 55;      // sentinella neutrale: nessun requisito dichiarato dal dossier
        NeutralNo = 35;       // ptn_neut_no
        DirectionalYes = 27;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -50;  // ptn_dir_no

        IntradayOnly = true;     // intraday_only = 1
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 250;         // stop_loss, $ per contratto = 39,00 pt
        ProfitMoney = 7500;      // take_profit, $ per contratto = 1.170,05 pt
        TrailingStopMoney = 1000;  // trailing_stop, $ per contratto = 156,01 pt
        BreakEvenMoney = 500;      // breakeven, $ di utile = 78,00 pt
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
