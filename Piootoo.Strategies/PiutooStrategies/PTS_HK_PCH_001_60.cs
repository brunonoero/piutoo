using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HK_PCH_001_60 - PC su HK a 60 minuti, <b>S84</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S84.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>HK_1h</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>.</para>
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
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 30 barre</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 30 barre</b></description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// <item><description><c>breakout_offset_ticks = 0</c>: nessun tick di buffer</description></item>
/// <item><description>Nessun filtro di volatilita' (<c>dvol_min = 0</c>)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier, con il segno che il dossier
/// scrive; il motore specchia il verso, quindi si dichiarano una volta sola. Le sentinelle
/// disattivano il gate - neutrale 55/56, direzionale 52/53, fast 152/153.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>28</c>: <c>|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>56</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere VERO - direzionale <c>52</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-3</c>: long <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.75</c>, short <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.75</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>04:00 e 03:00</b>, ora della ricerca (CET): la finestra attraversa la mezzanotte</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (<c>intraday_only = 1</c>): nessun overnight</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$3.000</b> per contratto = <b>468,02 pt</b></description></item>
/// <item><description>Take profit: <b>$2.500</b> = <b>390,02 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>48 barre</b> (2,0 giorni di calendario)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HK, $6,41 per punto, tick 1. Il valore per punto e' il
/// solo del registro che dipende da un cambio (HKD 50 per punto a 7,8 HKD/USD): se l'HKD uscisse
/// dalla banda, stop e target andrebbero rimisurati. E' con questo cambio che tornano le
/// conversioni del dossier: $3.000 = 468,02 pt e $2.500 = 390,02 pt.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$119</description></item>
/// <item><term>Fuori campione</term><description>$42.574 su 258 trade</description></item>
/// <item><term>Drawdown</term><description>$15.670</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>HK_1h/consegna/trades/fam01_PC.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_HK_PCH_001_60 : PriceChannelEngine
{
    public override string Name => "PTS_HK_PCH_001_60";
    public override string Description =>
        "PC HK 60m: S84 del dossier, run HK_1h, finestra 04:00-03:00 CET, intraday";
    public override string Symbol => "@HK";
    public override int TimeframeMinutes => 60;

    public PTS_HK_PCH_001_60()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Su HK la fascia 00:00-01:00 ha barre, e il taglio stretto
        // t > 0100 di OHLCMulti5 le assegna alla sessione precedente - esattamente come il
        // (timestamp - 1 min - 1h).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(4, 3);   // start_hour 4, end_hour 3, a cavallo della mezzanotte

        ChannelBars = 30;              // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: offset esatto, nessun tick implicito
        TickSize = 1m;                 // tick HK
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min = 0: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 28;      // ptn_neut_yes
        NeutralNo = 56;       // sentinella neutrale: nessun requisito dichiarato dal dossier
        DirectionalYes = 52;  // sentinella direzionale: nessun requisito dichiarato dal dossier
        DirectionalNo = -3;   // ptn_dir_no (il segno lo applica il motore per verso)

        IntradayOnly = true;     // intraday_only = 1
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 3000;        // stop_loss, $ per contratto = 468,02 pt
        ProfitMoney = 2500;      // take_profit, $ per contratto = 390,02 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 48;           // max_bars: 48 barre da 60 minuti, 2,0 giorni di calendario
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
