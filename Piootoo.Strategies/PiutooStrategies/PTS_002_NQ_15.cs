using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_002 — PC long-only su NQ a 15 minuti: rottura stop del massimo del
/// canale Donchian delle ultime 100 barre, inclusa quella appena chiusa, con
/// buffer di 2 tick.
///
/// <para>
/// Le barre e gli orari sono UTC. <b>La sessione è il giorno di calendario UTC,
/// non la sessione CME 17:00–16:00</b>, e la finestra di ingresso inclusiva
/// 13:00–04:00 la attraversa: un'occorrenza della finestra ricade su due
/// sessioni. Non sono esclusi giorni e non è applicato alcun filtro di
/// volatilità daily.
/// </para>
///
/// <para>
/// Il confine di mezzanotte è misurato sul motore di riferimento, non scelto:
/// raggruppando i suoi ingressi per giorno di calendario si ottiene esattamente
/// un ingresso per sessione su 120 trade, mentre con il confine CME sette
/// sessioni ne mostrerebbero due. Un confine nella zona morta della finestra
/// (05:00–12:00) non produce lo stesso risultato, quindi la sessione è davvero
/// il giorno e non l'occorrenza della finestra. Il parametro governa insieme il
/// secchio di <c>MaxEntriesPerSession</c> e gli OHLC d0..d5 su cui girano i
/// pattern, quindi vale per entrambi.
/// </para>
///
/// <para>
/// Il setup richiede l'assenza del pattern neutro 24 e il pattern direzionale
/// rialzista 2. Il valore neutro 55 e il direzionale esclusivo 53 sono
/// sentinelle del motore: rispettivamente sempre vero e sempre falso, quindi
/// non introducono ulteriori filtri. La strategia è esclusivamente long.
/// </para>
///
/// <para>
/// A chiusura di una barra valida, calcola il massimo delle ultime 100 barre,
/// <b>inclusa</b> quella che ha prodotto il segnale, come il motore Python, e
/// invia uno stop buy a quel livello più 0,50 punti (2 tick NQ da 0,25).
/// L'ordine è valido solo sulla barra successiva; fill e gap sono responsabilità
/// dell'engine. Finché è flat, il livello viene ricalcolato e l'ordine viene
/// riemesso a ogni barra valida.
/// </para>
///
/// <para>
/// La posizione può restare overnight e non ha un limite di barre. Ogni
/// ingresso dichiara, per contratto NQ, stop loss $250 (12,5 punti), take
/// profit $5.000 (250 punti), breakeven $1.000 (50 punti) e trailing stop
/// $1.000 (50 punti dal massimo favorevole). Le uscite sono autocontenute nel
/// segnale e vengono gestite dall'engine o dal broker, mai da segnali di uscita
/// emessi dalla strategia. Al massimo un ordine può essere eseguito per giorno
/// di calendario: il limite è verificato dall'engine sul fill, quindi gli stop
/// non eseguiti continuano a essere riemessi.
/// </para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader>
/// <term>Metrica</term>
/// <description>Valore</description>
/// </listheader>
/// <item><term>WF mean</term><description>7</description></item>
/// <item><term>UngerScore In-Sample</term><description>16</description></item>
/// <item><term>UngerScore Out-of-Sample</term><description>15</description></item>
/// <item><term>CAGR In-Sample</term><description>7,8%</description></item>
/// <item><term>Rendimento atteso realistico (Walk-Forward)</term><description>3,4%</description></item>
/// <item><term>CAGR Out-of-Sample</term><description>14,1%</description></item>
/// <item><term>Avg Trade combinato IS + OOS</term><description>$221</description></item>
/// <item><term>Degradazione Walk-Forward IS → OOS</term><description>94%</description></item>
/// <item><term>Walk-Forward folds positivi</term><description>5/5</description></item>
/// </list>
/// </summary>
public sealed class PTS_002_NQ_15 : PriceChannelEngine
{
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;

    public PTS_002_NQ_15()
    {
        SessionStartTime = 0;
        SessionEndTime = 2359;
        ChannelBars = 100;
        EnableLong = true;
        EnableShort = false;
        Direction = 1;
        OffsetTicks = 2;
        TickSize = 0.25m;
        StartTime = 1300;
        EndTime = 400;
        TradingWindowInclusive = true;
        NeutralYes = 55;
        NeutralNo = 24;
        DirectionalYes = 2;
        DirectionalNo = 53;
        NotEntryDayLong = -1;
        IntradayOnly = false;
        StopMoney = 250;
        ProfitMoney = 5000;
        BreakEvenMoney = 1000;
        TrailingStopMoney = 1000;
        MaxBars = 0;
        MaxEntriesPerSession = 1;
        Contracts = 1;
    }

    public override string Name => "PTS_002_NQ_15";
    public override string Description => "PC NQ 15: Donchian 100 long-only, buffer 2 tick, multiday";
    public override string Symbol => _symbol;
    public override int TimeframeMinutes => _timeframeMinutes;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null)
            return;

        if (parameters.TryGetValue("Symbol", out var symbol))
            _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe))
            _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("SessionStartTime", out var sessionStart))
            SessionStartTime = Convert.ToInt32(sessionStart);
        if (parameters.TryGetValue("SessionEndTime", out var sessionEnd))
            SessionEndTime = Convert.ToInt32(sessionEnd);
        if (parameters.TryGetValue("StartHour", out var startHour))
            StartTime = Convert.ToInt32(startHour) * 100;
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndTime = Convert.ToInt32(endHour) * 100;
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            NotEntryDayLong = ToEasyLanguageDayOfWeek(Convert.ToInt32(skipDay));
        if (parameters.TryGetValue("PtnNeutYes", out var ptnNeutYes))
            NeutralYes = Convert.ToInt32(ptnNeutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var ptnNeutNo))
            NeutralNo = Convert.ToInt32(ptnNeutNo);
        if (parameters.TryGetValue("PtnDirYes", out var ptnDirYes))
            DirectionalYes = Convert.ToInt32(ptnDirYes);
        if (parameters.TryGetValue("PtnDirNo", out var ptnDirNo))
            DirectionalNo = Convert.ToInt32(ptnDirNo);
        if (parameters.TryGetValue("OffsetTicks", out var offsetTicks))
            OffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("ChannelLength", out var channelLength))
            ChannelBars = Convert.ToInt32(channelLength);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("TrailingStop", out var trailingStop))
            TrailingStopMoney = Convert.ToInt32(trailingStop);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MaxEntriesPerSession", out var maxEntriesPerSession))
            MaxEntriesPerSession = Convert.ToInt32(maxEntriesPerSession);
    }

    private static int ToEasyLanguageDayOfWeek(int pythonDayOfWeek) =>
        pythonDayOfWeek < 0 ? -1 : (pythonDayOfWeek + 1) % 7;
}
