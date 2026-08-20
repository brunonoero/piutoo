using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_PCH_001_60 — PC su ES a 60 minuti, strategia <b>S04</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260820_0012</c>,
/// famiglia 02).
///
/// <para>Breakout del canale di Donchian a <b>20 barre</b>, calcolato sulle <b>barre del
/// timeframe</b> e non sulle sessioni. Il canale include la barra appena chiusa che produce il
/// segnale — come <c>highest(high, 20)</c> EasyLanguage e <c>donchian(shift=0)</c> del motore
/// Python — e l'ordine stop vale solo dalla barra successiva, quindi non c'è look-ahead.</para>
///
/// <para><b>Solo long.</b> Il lato short non opera mai (<c>Direction = 1</c>): le condizioni
/// short del motore non vengono mai valutate e non sono dichiarate qui.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate. Lo stesso
/// confine governa il secchio di <c>MaxEntriesPerSession</c>.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e finestra dichiarano il
/// proprio fuso e il confronto passa dall'istante assoluto della barra. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 24: <c>|O_d5-C_d1| &lt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 38: <c>(H_d0-L_d0) &lt; L_d0 * 0.005</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — direzionale 49: <c>close &gt; O_d0</c></description></item>
/// <item><description>deve essere FALSO — direzionale -45: <c>(C_d1 &lt; O_d1) E (C_d2 &lt; O_d2)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>03:00 e 02:00</b> (a cavallo della mezzanotte), ora dei dati (CET), estremi inclusi su HHMM pieni</description></item>
/// <item><description><b>Non apre posizioni di venerdì</b> (<c>skip_day = 4</c>, convenzione pandas con 0 = lunedì)</description></item>
/// <item><description>Può restare aperta <b>oltre la sessione</b> (multiday): <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura. ⚠ Il
/// <b>trailing stop</b> va applicato anche dal client live: sulle PC del catalogo è la causa di
/// uscita di circa un trade su tre. Vedi <c>docs/domini/trading-sessions-api.md</c>.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $4,000 per contratto = 80.00 pt</description></item>
/// <item><description>Take profit: $7,500 = 150.00 pt</description></item>
/// <item><description>Trailing stop: $1,000 = 20.00 pt</description></item>
/// <item><description>Nessun breakeven, nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti. Nessun offset di
/// breakout: il livello è il massimo del canale, senza tick impliciti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$181</description></item>
/// <item><term>Out-of-sample</term><description>$46,003 su 93 trade &#183; drawdown $8,602 (01/06/2021 → 30/05/2025)</description></item>
/// </list>
///
/// <para><b>Vincolo operativo.</b> Emette gli stessi ordini di entrata della PC 1h
/// <c>fam02-2</c> dello stesso run, che per questo <b>non è stata tradotta</b>: metterle su
/// conti separati sarebbe copy trading.</para>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S04_1h_PC.csv</c>
/// (<c>run_20260820_0012/consegna/trades/fam02_PC.csv</c>). Contano le <b>entrate</b>: timestamp
/// e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di slippage per
/// lato, che l'engine non applica e va rettificato al confronto.</para>
/// </summary>
public sealed class PTS_ES_PCH_001_60 : PriceChannelEngine
{
    public override string Name => "PTS_ES_PCH_001_60";
    public override string Description =>
        "PC ES 60m: S04 run 20260820_0012, Donchian 20, solo long, finestra 03:00–02:00 CET, niente venerdì, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 60;

    public PTS_ES_PCH_001_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        ChannelBars = 20; // channel_len: canale INCLUSA la barra di segnale
        Direction = 1;    // direction: 1 = solo long
        OffsetTicks = 0;  // breakout_offset_ticks: nessun offset, nessun tick implicito
        TickSize = 0.25m; // tick ES
        DvolMin = 0m;     // dvol_min: filtro di volatilita' disattivo

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(3, 2);
        TradingWindowInclusive = true;
        SkipDay = 4;      // skip_day: 4 = venerdi' (0 = lunedi'), "non apre di venerdi'"

        NeutralYes = 24;      // ptn_neut_yes
        NeutralNo = 38;       // ptn_neut_no
        DirectionalYes = 49;  // ptn_dir_yes
        DirectionalNo = -45;  // ptn_dir_no

        IntradayOnly = false; // multiday: niente CloseAtUtc di fine sessione

        StopMoney = 4000;         // stop_loss, $ per contratto = 80.00 pt
        ProfitMoney = 7500;       // take_profit, $ per contratto = 150.00 pt
        TrailingStopMoney = 1000; // trailing_stop, $ per contratto = 20.00 pt
        BreakEvenMoney = 0;       // breakeven
        MaxBars = 0;              // max_bars (0 = nessuna uscita a tempo)
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
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("TrailingStop", out var trailingStop))
            TrailingStopMoney = Convert.ToInt32(trailingStop);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("ChannelLength", out var channelLength))
            ChannelBars = Convert.ToInt32(channelLength);
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
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
