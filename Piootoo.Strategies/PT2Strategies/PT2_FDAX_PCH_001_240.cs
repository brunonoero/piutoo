using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT2Strategies;

/// <summary>
/// PT2_FDAX_PCH_001_240 - PC su FDAX a 4 ore, <b>S03</b> del dossier
/// <c>run-engine-v2/DOSSIER_PANIERE_001.md</c> (paniere del 14/09/2026, quattro strategie).
///
/// <para><b>Codice sorgente: S03 (run-engine-v2, DOSSIER_PANIERE_001).</b> E' l'identificativo con cui
/// questa strategia compare nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un
/// controllo, senza riaprire i CSV a tentativi. La cella di ricerca e' <c>FDAX_4h</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>. La serie <c>PT2_*</c> nasce dal rifacimento dell'analisi: le classi
/// <c>PTS_*</c> restano e non descrivono queste righe (<c>PTS_FDAX_PCH_001_240</c> e' un'altra riga,
/// con altri parametri).</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre, non sulle
/// sessioni: con canale a <b>1 barra</b> e' la rottura del massimo o del minimo della barra appena
/// chiusa, piu' un buffer di 10 punti.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano il pattern e il limite di
/// ingressi cominciano alle <b>01:00 dell'orologio della ricerca</b> (CET) e durano fino alla stessa
/// ora del giorno dopo: e' quanto la tabella §2.1.1 del dossier dichiara per FDAX
/// (<c>session_start_hour = 1</c>) e lo risolve il calendario del simbolo, non la classe. Non e' la
/// sessione Eurex. E' lo stesso confine su cui cade la chiusura di fine sessione.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 1 barre</b> + 10 tick (10 pt)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 1 barre</b> - 10 tick (10 pt)</description></item>
/// <item><description>Il canale e' calcolato sulle <b>barre del timeframe</b>, non sulle sessioni, e <b>include la barra di segnale</b>: alla chiusura i suoi OHLC sono noti e l'ordine vale solo dalla barra successiva</description></item>
/// <item><description>L'offset e' esatto: <c>livello + 10 × tick</c>, nessun tick implicito (trappola corretta il 17/08/2026)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier. Le sentinelle disattivano il gate -
/// neutrale 55/56, direzionale 52/53 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>.</para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere FALSO - neutrale <c>7</c>: <c>|O_d1-C_d1| &gt; 0.75 * (H_d1-L_d1)</c>, cioe' non si entra dopo una sessione con il corpo oltre tre quarti del range (<c>EasyLib.PatternNeutralFast</c> caso 7, stessa formula)</description></item>
/// <item><description>Nessun filtro neutrale richiesto (sentinella 55), nessun filtro direzionale (sentinelle 52/53)</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore (<c>ZonedWindow.AllDay</c>)</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (nessun overnight): <c>intraday_only = 1</c>, cioe' <c>CloseAtUtc</c> all'ultimo minuto della sessione ancorata all'01:00</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$3.575</b> per contratto = <b>143,00 pt</b></description></item>
/// <item><description>Take profit: <b>$5.800</b> = <b>232,00 pt</b></description></item>
/// <item><description>Nessun trailing, nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 per punto (il dossier lo scrive in $, il registro
/// strumenti in EUR: la conversione in punti e' la stessa), tick 1 punto.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$299</description></item>
/// <item><term>Fuori campione</term><description>$264.639 su 884 trade (24/01/2022 → 30/05/2025)</description></item>
/// <item><term>Drawdown</term><description>$42.218</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento dichiarata dal dossier:
/// <c>FDAX_4h/consegna/trades/fam01_PC.csv</c>, che <b>non e' nel repository</b>: in
/// <c>run-engine-v2/</c> c'e' il solo dossier. Contano le <b>entrate</b>: timestamp e prezzo. Il
/// riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che l'engine non
/// applica: va rettificato al confronto, non compensato sul livello. Il datafeed <c>@FDAX_240</c>
/// esiste ma e' ancorato a 00:00 invece che a 01:00 (voce in <c>docs/lavori-in-corso.md</c>): va
/// rigenerato prima del confronto, altrimenti canale e sessioni sono su bucket diversi da quelli
/// della ricerca.</para>
/// </summary>
public sealed class PT2_FDAX_PCH_001_240 : PriceChannelEngine
{
    public override string Name => "PT2_FDAX_PCH_001_240";
    public override string Description =>
        "PC FDAX 4 ore: S03 di run-engine-v2/DOSSIER_PANIERE_001, canale 1 barra + 10 pt, neutrale 7 vietato, tutte le 24 ore, intraday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 240;

    public PT2_FDAX_PCH_001_240()
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

        ChannelBars = 1;               // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 10;              // breakout_offset_ticks: 10 tick = 10 pt, offset esatto
        TickSize = 1m;                 // tick FDAX
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day: nessun giorno escluso

        NeutralYes = 55;               // ptn_neut_yes: sentinella sempre vera (nessun filtro richiesto)
        NeutralNo = 7;                 // ptn_neut_no:  |O_d1-C_d1| > 0.75 * (H_d1-L_d1) deve essere FALSO
        DirectionalYes = 52;           // ptn_dir_yes:  sentinella sempre vera (nessun filtro)
        DirectionalNo = 53;            // ptn_dir_no:   sentinella sempre falsa (nessun filtro)

        IntradayOnly = true;           // intraday_only = 1: chiude a fine sessione
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 3575;              // stop_loss, $ per contratto = 143,00 pt
        ProfitMoney = 5800;            // take_profit, $ per contratto = 232,00 pt
        TrailingStopMoney = 0;         // nessun trailing
        BreakEvenMoney = 0;            // nessun breakeven
        MaxBars = 0;                   // max_bars = 0: nessuna uscita a tempo
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
