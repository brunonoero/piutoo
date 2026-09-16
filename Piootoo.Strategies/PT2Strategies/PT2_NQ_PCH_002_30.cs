using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT2Strategies;

/// <summary>
/// PT2_NQ_PCH_002_30 - PC su NQ a 30 minuti, <b>S04</b> del dossier
/// <c>run-engine-v2/DOSSIER_PANIERE_001.md</c> (paniere del 14/09/2026, quattro strategie).
///
/// <para><b>Codice sorgente: S04 (run-engine-v2, DOSSIER_PANIERE_001).</b> E' l'identificativo con cui
/// questa strategia compare nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un
/// controllo, senza riaprire i CSV a tentativi. La cella di ricerca e' <c>NQ_30m</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>. La serie <c>PT2_*</c> nasce dal rifacimento dell'analisi: le classi
/// <c>PTS_*</c> restano e non descrivono queste righe.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian a 10 barre calcolato sulle barre, non
/// sulle sessioni. <b>Solo long</b>.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano pattern e limite di ingressi
/// sono il <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1.1 del dossier). Lo risolve il calendario del simbolo, non la classe. Non e' la
/// sessione del broker: le due coincidono quasi sempre, ma non nelle settimane in cui l'ora legale
/// americana ed europea non sono allineate.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 10 barre</b></description></item>
/// <item><description>SHORT: il lato short <b>non opera mai</b> (<c>direction = 1</c>); il dossier riporta il livello per completezza</description></item>
/// <item><description>Nessun offset (<c>breakout_offset_ticks = 0</c>)</description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> <i>Nessun filtro pattern</i>: il motore entra su ogni segnale
/// strutturale. I gate restano alle sentinelle - neutrale 55/56, direzionale 52/53 - che <b>non
/// filtrano nulla</b>.</para>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore (<c>ZonedWindow.AllDay</c>)</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$1.045</b> per contratto = <b>52,25 pt</b></description></item>
/// <item><description>Take profit: <b>nessuno</b></description></item>
/// <item><description>Nessun trailing, nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>46 barre</b> (23 ore), contate sulle barre vere della strategia</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$70</description></item>
/// <item><term>Fuori campione</term><description>$109.565 su 995 trade (24/01/2022 → 30/05/2025)</description></item>
/// <item><term>Drawdown</term><description>$48.609</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento dichiarata dal dossier:
/// <c>NQ_30m/consegna/trades/fam01_PC.csv</c>, che <b>non e' nel repository</b>: in
/// <c>run-engine-v2/</c> c'e' il solo dossier. Contano le <b>entrate</b>: timestamp e prezzo. Il
/// riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che l'engine non
/// applica: va rettificato al confronto, non compensato sul livello. Il datafeed <c>@NQ_30</c>
/// esiste.</para>
/// </summary>
public sealed class PT2_NQ_PCH_002_30 : PriceChannelEngine
{
    public override string Name => "PT2_NQ_PCH_002_30";
    public override string Description =>
        "PC NQ 30m: S04 di run-engine-v2/DOSSIER_PANIERE_001, canale 10 barre, solo long, tutte le 24 ore, multiday, uscita a 46 barre";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public PT2_NQ_PCH_002_30()
    {
        Contracts = 1;

        // I run di run-engine-v2 etichettano le barre all'INIZIO (§2.6 del dossier). Qui non c'e'
        // finestra ne' giorno escluso, quindi non cambia nulla, ma la serie lo dichiara sempre:
        // un parametro orario aggiunto domani sarebbe letto con l'etichetta giusta.
        ResearchLabelsBarsOnOpen = true;

        // Nessun filtro orario: il run non ha filtrato per ora. La finestra e' dichiarata piena,
        // nell'orologio della ricerca, perche' ogni strategia del catalogo deve dichiarare
        // l'orologio in cui legge gli orari (StrategyClockConformanceTests).
        TradingWindow = ZonedWindow.AllDay;

        ChannelBars = 10;              // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: nessun offset
        TickSize = 0.25m;              // tick NQ
        Direction = 1;                 // direction: 1 = solo long
        DvolMin = 0m;                  // dvol_min: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day: nessun giorno escluso

        NeutralYes = 55;               // ptn_neut_yes: sentinella sempre vera (nessun filtro)
        NeutralNo = 56;                // ptn_neut_no:  sentinella sempre falsa (nessun filtro)
        DirectionalYes = 52;           // ptn_dir_yes:  sentinella sempre vera (nessun filtro)
        DirectionalNo = 53;            // ptn_dir_no:   sentinella sempre falsa (nessun filtro)

        IntradayOnly = false;          // intraday_only = 0: multiday
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 1045;              // stop_loss, $ per contratto = 52,25 pt
        ProfitMoney = 0;               // take_profit: nessuno
        TrailingStopMoney = 0;         // nessun trailing
        BreakEvenMoney = 0;            // nessun breakeven
        MaxBars = 46;                  // max_bars: uscita a tempo dopo 46 barre (23 ore)
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
            TradingWindow = TradingWindow! with { Start = new TimeOnly(Convert.ToInt32(startHour), 0) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = new TimeOnly(Convert.ToInt32(endHour), 0) };
    }
}
