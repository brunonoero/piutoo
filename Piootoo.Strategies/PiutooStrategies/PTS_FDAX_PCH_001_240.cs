using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_FDAX_PCH_001_240 - PC su FDAX a 4 ore, <b>S08</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S08.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1500</c>, famiglia
/// <c>fam02</c>, motore <c>PC</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non e' la sessione del broker: le due coincidono quasi sempre, ma non nelle settimane in cui
/// l'ora legale americana ed europea non sono allineate. Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 1 barre</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 1 barre</b></description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// <item><description>Opera solo se l'ATR di sessione a 14 periodi, convertito in dollari, e' >= <b>$3.000</b></description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>12</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>8</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>47</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>-5</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>07:00 e 13:00</b>, ora della ricerca (CET)</description></item>
/// <item><description><b>Non apre</b> posizioni di venerdi' (<c>skip_day = 4</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (nessun overnight): <c>intraday_only = 1</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>10.00 pt</b></description></item>
/// <item><description>Take profit: <b>$10.000</b> = <b>400.00 pt</b></description></item>
/// <item><description>Trailing stop: <b>$2.000</b></description></item>
/// <item><description>Breakeven a <b>$1.000</b> di utile</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b></description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 EUR per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$965</description></item>
/// <item><term>Fuori campione</term><description>$330.157 su 342 trade</description></item>
/// <item><term>Drawdown</term><description>$15.704</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1500/consegna/trades/fam02_PC.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>4h fam02-2, 4h fam02-3, 4h fam02-5, 4h fam02-6, 4h fam02-7, 4h fam02-8, 4h fam02-9</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_FDAX_PCH_001_240 : PriceChannelEngine
{
    public override string Name => "PTS_FDAX_PCH_001_240";
    public override string Description =>
        "PC FDAX 4 ore: S08 del dossier, run run_20260824_1500, finestra 07:00-13:00 CET, intraday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 240;

    public PTS_FDAX_PCH_001_240()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(7, 13);   // start_hour 7, end_hour 13

        ChannelBars = 1;               // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: offset esatto, nessun tick implicito
        TickSize = 1m;              // tick FDAX
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 3000m;                   // dvol_min, $ di ATR di sessione a 14 periodi; 0 disattiva
        SkipDay = 4;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 12;      // ptn_neut_yes
        NeutralNo = 8;       // ptn_neut_no
        DirectionalYes = 47;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = -5;   // ptn_dir_no

        IntradayOnly = true;    // intraday_only = 1
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 250;        // stop_loss, $ per contratto = 10.00 pt
        ProfitMoney = 10000;      // take_profit, $ per contratto = 400.00 pt
        TrailingStopMoney = 2000;  // trailing_stop, $ per contratto
        BreakEvenMoney = 1000;     // breakeven, $ di utile
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
