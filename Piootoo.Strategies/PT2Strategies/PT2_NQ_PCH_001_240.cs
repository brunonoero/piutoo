using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT2Strategies;

/// <summary>
/// PT2_NQ_PCH_001_240 - PC su NQ a 4 ore, <b>S02</b> del dossier
/// <c>run-engine-v2/DOSSIER_PANIERE_001.md</c> (paniere del 14/09/2026, quattro strategie).
///
/// <para><b>Codice sorgente: S02 (run-engine-v2, DOSSIER_PANIERE_001).</b> E' l'identificativo con cui
/// questa strategia compare nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un
/// controllo, senza riaprire i CSV a tentativi. La cella di ricerca e' <c>NQ_4h</c>, famiglia
/// <c>fam01</c>, motore <c>PC</c>. La serie <c>PT2_*</c> nasce dal rifacimento dell'analisi: le classi
/// <c>PTS_*</c> restano e non descrivono queste righe.</para>
///
/// <para><b>Che cosa fa.</b> Breakout del canale di Donchian calcolato sulle barre, non sulle
/// sessioni: con canale a <b>1 barra</b> e' la rottura del massimo o del minimo della barra appena
/// chiusa.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano pattern e limite di ingressi
/// sono il <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1.1 del dossier). Lo risolve il calendario del simbolo, non la classe. Non e' la
/// sessione del broker: le due coincidono quasi sempre, ma non nelle settimane in cui l'ora legale
/// americana ed europea non sono allineate. Gli orari della finestra operativa sono riportati
/// <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle ultime 1 barre</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle ultime 1 barre</b></description></item>
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
/// <item><description>Opera solo fra <b>12:00 e 16:00</b>, ora della ricerca (CET): <c>start_hour = 12</c>, <c>end_hour = 16</c>, verbatim, confrontati con l'<b>apertura</b> della barra perche' i run di <c>run-engine-v2</c> etichettano le candele all'inizio (§2.6; <c>ResearchLabelsBarsOnOpen = true</c>). Su una 4h ancorata a mezzanotte sono le barre <b>12:00-16:00</b> e <b>16:00-20:00</b>, fine inclusa. ⚠ La scheda dice anche «ordini emessi sulle barre che <b>chiudono</b> fra le 12:00 e le 16:00»: e' la frase dei dossier precedenti, che etichettavano sulla chiusura, e con §2.6 non torna. Qui vale §2.6 e il numero verbatim; da confermare sulla lista trade appena c'e'.</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$4.595</b> per contratto = <b>229,75 pt</b></description></item>
/// <item><description>Take profit: <b>nessuno</b></description></item>
/// <item><description>Nessun trailing, nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>5 barre</b> (20 ore), contate sulle barre vere della strategia</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$496</description></item>
/// <item><term>Fuori campione</term><description>$507.868 su 1.023 trade (24/01/2022 → 30/05/2025)</description></item>
/// <item><term>Drawdown</term><description>$92.842</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento dichiarata dal dossier:
/// <c>NQ_4h/consegna/trades/fam01_PC.csv</c>, che <b>non e' nel repository</b>: in
/// <c>run-engine-v2/</c> c'e' il solo dossier. Contano le <b>entrate</b>: timestamp e prezzo. Il
/// riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che l'engine non
/// applica: va rettificato al confronto, non compensato sul livello. Il datafeed <c>@NQ_240</c>
/// esiste.</para>
/// </summary>
public sealed class PT2_NQ_PCH_001_240 : PriceChannelEngine
{
    public override string Name => "PT2_NQ_PCH_001_240";
    public override string Description =>
        "PC NQ 4 ore: S02 di run-engine-v2/DOSSIER_PANIERE_001, canale 1 barra, finestra 12:00-16:00 CET, multiday, uscita a 5 barre";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 240;

    public PT2_NQ_PCH_001_240()
    {
        Contracts = 1;

        // I run di run-engine-v2 etichettano le barre all'INIZIO (§2.6 del dossier): la finestra
        // si confronta con l'apertura della barra, non con la chiusura come per le PTS_*.
        ResearchLabelsBarsOnOpen = true;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso e l'etichetta viaggiano con il dato.
        TradingWindow = ZonedWindow.ResearchHours(12, 16);  // start_hour 12, end_hour 16

        ChannelBars = 1;               // channel_len, canale INCLUSA la barra di segnale
        OffsetTicks = 0;               // breakout_offset_ticks: nessun offset
        TickSize = 0.25m;              // tick NQ
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        DvolMin = 0m;                  // dvol_min: filtro di volatilita' disattivo
        SkipDay = -1;                  // skip_day: nessun giorno escluso

        NeutralYes = 55;               // ptn_neut_yes: sentinella sempre vera (nessun filtro)
        NeutralNo = 56;                // ptn_neut_no:  sentinella sempre falsa (nessun filtro)
        DirectionalYes = 52;           // ptn_dir_yes:  sentinella sempre vera (nessun filtro)
        DirectionalNo = 53;            // ptn_dir_no:   sentinella sempre falsa (nessun filtro)

        IntradayOnly = false;          // intraday_only = 0: multiday
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 4595;              // stop_loss, $ per contratto = 229,75 pt
        ProfitMoney = 0;               // take_profit: nessuno
        TrailingStopMoney = 0;         // nessun trailing
        BreakEvenMoney = 0;            // nessun breakeven
        MaxBars = 5;                   // max_bars: uscita a tempo dopo 5 barre (20 ore)
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
