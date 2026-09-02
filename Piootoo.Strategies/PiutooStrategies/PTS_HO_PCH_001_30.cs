using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HO_PCH_001_30 - PC su HO a 30 minuti, <b>S115</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S115.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>HO_30m</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: HO parte a 00:00 CET). Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora NYMEX.</para>
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
/// <item><description>deve essere VERO - neutrale <c>16</c>: <c>|O_d5-C_d1| &gt; 0.25 * (H_d5-L_d1)</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>8</c>: <c>|O_d1-C_d1| &gt; 0.9 * (H_d1-L_d1)</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>52</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-5</c>: long <c>O_d0 - L_d0 &gt; (O_d1 - L_d1) * 1.5</c>, short <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 1.5</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>03:00 e 02:00</b>, ora della ricerca (CET): la finestra attraversa la mezzanotte</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$5.000</b> per contratto = <b>0,12 pt</b></description></item>
/// <item><description>Take profit: <b>$6.000</b> = <b>0,14 pt</b></description></item>
/// <item><description>Trailing stop: <b>$1.000</b> = <b>0,02 pt</b></description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HO, $42.000 per punto, tick 0,0001.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$33</description></item>
/// <item><term>Fuori campione</term><description>$70.437 su 1.067 trade</description></item>
/// <item><term>Drawdown</term><description>$25.965</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>HO_30m/consegna/trades/fam01_PC.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello. Con 1.067
/// trade di riferimento il costo per trade pesa piu' che altrove: e' la strategia del paniere
/// con l'atteso per trade piu' basso.</para>
/// </summary>
public sealed class PTS_HO_PCH_001_30 : PriceChannelEngine
{
    public override string Name => "PTS_HO_PCH_001_30";
    public override string Description =>
        "PC HO 30m: S115 del dossier, run HO_30m, finestra 03:00-02:00 CET, multiday";
    public override string Symbol => "@HO";
    public override int TimeframeMinutes => 30;

    public PTS_HO_PCH_001_30()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(3, 2);   // start_hour 3, end_hour 2, a cavallo della mezzanotte

        ChannelBars = 30;              // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: offset esatto, nessun tick implicito
        TickSize = 0.0001m;            // tick HO
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min = 0: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 16;      // ptn_neut_yes
        NeutralNo = 8;        // ptn_neut_no
        DirectionalYes = 52;  // sentinella direzionale: nessun requisito dichiarato dal dossier
        DirectionalNo = -5;   // ptn_dir_no (il segno lo applica il motore per verso)

        IntradayOnly = false;    // intraday_only = 0
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 5000;        // stop_loss, $ per contratto = 0,12 pt
        ProfitMoney = 6000;      // take_profit, $ per contratto = 0,14 pt
        TrailingStopMoney = 1000;  // trailing_stop, $ per contratto = 0,02 pt
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
