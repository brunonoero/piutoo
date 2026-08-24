using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_ES_PCH_002_60 — PC su ES a 60 minuti, strategia <b>S06</b> del dossier
/// <c>run-engine/run-05-agosto/dossier_ctrader_ES.md</c> (run <c>run_20260820_0012</c>,
/// famiglia 03).
///
/// <para>Breakout del canale di Donchian a <b>1 barra</b>, calcolato sulle <b>barre del
/// timeframe</b> e non sulle sessioni: il livello è il massimo della barra appena chiusa. Il
/// canale include la barra di segnale — <c>donchian(shift=0)</c> del motore Python — e l'ordine
/// stop vale solo dalla barra successiva, quindi non c'è look-ahead.</para>
///
/// <para><b>Solo long.</b> Il lato short non opera mai (<c>Direction = 1</c>).</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e finestra dichiarano il
/// proprio fuso e il confronto passa dall'istante assoluto della barra. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 29: <c>|O_d5-C_d1| &gt; 0.5 * (HH5-LL5)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 54: <c>(H_d1-L_d1) &gt; (H_d2-L_d2)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>nessun filtro direzionale obbligatorio: <c>ptn_dir_yes = 52</c> è la <b>sentinella sempre vera</b>, non un filtro</description></item>
/// <item><description>deve essere FALSO — direzionale 16: <c>C_d1 &gt; C_d2 * (1 + 0.01)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>08:00 e 10:00</b>, ora dei dati (CET), estremi inclusi su HHMM pieni</description></item>
/// <item><description>Nessun giorno escluso</description></item>
/// <item><description>Può restare aperta <b>oltre la sessione</b> (multiday): <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker. ⚠ <b>Trailing stop e breakeven</b> vanno applicati anche dal client
/// live: se il cBot non li applica, in produzione gira un'altra strategia. Vedi
/// <c>docs/domini/trading-sessions-api.md</c>.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $4,000 per contratto = 80.00 pt</description></item>
/// <item><description>Take profit: $7,500 = 150.00 pt</description></item>
/// <item><description>Trailing stop: $2,000 = 40.00 pt</description></item>
/// <item><description>Breakeven a $500 = 10.00 pt di utile</description></item>
/// <item><description>Nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> ES, $50 per punto, tick 0,25 punti. Nessun offset di
/// breakout.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$77</description></item>
/// <item><term>Out-of-sample</term><description>$34,473 su 163 trade &#183; drawdown $15,623 (01/06/2021 → 30/05/2025)</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run-engine/run-05-agosto/trades/S06_1h_PC.csv</c>
/// (<c>run_20260820_0012/consegna/trades/fam03_PC.csv</c>). Contano le <b>entrate</b>: timestamp
/// e prezzo. Costi del riferimento: $4,00 di commissione per trade e 1 tick di slippage per
/// lato, che l'engine non applica e va rettificato al confronto.</para>
/// </summary>
public sealed class PTS_ES_PCH_002_60 : PriceChannelEngine
{
    public override string Name => "PTS_ES_PCH_002_60";
    public override string Description =>
        "PC ES 60m: S06 run 20260820_0012, Donchian 1, solo long, finestra 08:00–10:00 CET, trailing 40 pt, multiday";
    public override string Symbol => "@ES";
    public override int TimeframeMinutes => 60;

    public PTS_ES_PCH_002_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        ChannelBars = 1;  // channel_len: canale INCLUSA la barra di segnale
        Direction = 1;    // direction: 1 = solo long
        OffsetTicks = 0;  // breakout_offset_ticks
        TickSize = 0.25m; // tick ES
        DvolMin = 0m;     // dvol_min: filtro di volatilita' disattivo

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(8, 10);
        TradingWindowInclusive = true;
        SkipDay = -1;     // skip_day (0 = lunedi', -1 = nessuno)

        NeutralYes = 29;      // ptn_neut_yes
        NeutralNo = 54;       // ptn_neut_no
        DirectionalYes = 52;  // ptn_dir_yes = 52: sentinella sempre vera, NESSUN filtro
        DirectionalNo = 16;   // ptn_dir_no

        IntradayOnly = false; // multiday: niente CloseAtUtc di fine sessione

        StopMoney = 4000;         // stop_loss, $ per contratto = 80.00 pt
        ProfitMoney = 7500;       // take_profit, $ per contratto = 150.00 pt
        TrailingStopMoney = 2000; // trailing_stop, $ per contratto = 40.00 pt
        BreakEvenMoney = 500;     // breakeven, $ per contratto = 10.00 pt
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
