using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_PCH_001 — PC long-only su NQ a 15 minuti: rottura stop del massimo del
/// canale Donchian delle ultime 100 barre, inclusa quella appena chiusa, con
/// buffer di 2 tick.
///
/// <para>
/// <b>Sessione e orologio.</b> Gli orari sono in ora di borsa: la sessione e' la giornata CME
/// 17:00–16:00 di Chicago e la finestra di ingresso inclusiva e' 06:00–21:00, sempre di Chicago.
/// Il motore converte l'istante UTC della barra prima di confrontare, quindi il comportamento non
/// dipende da come e' stampato il feed.
/// </para>
///
/// <para>
/// <b>Storia di questo confine, perche' spiega una misura che resta valida.</b> Fino al
/// 17/08/2026 la classe dichiarava <c>0</c>/<c>2359</c> con la motivazione che il confine fosse il
/// giorno di calendario. La misura che la sosteneva era corretta — raggruppando gli ingressi del
/// motore di riferimento per giorno si ottiene esattamente un ingresso per sessione su 120 trade,
/// mentre col confine alle 17:00 sette sessioni ne mostrano due — ma la conclusione era
/// incompleta: quel confronto avveniva su un feed stampato in ora europea, dove mezzanotte <i>e'</i>
/// la riapertura CME delle 17:00 di Chicago. Erano lo stesso istante scritto in due orologi. Ora
/// che l'ora di borsa e' esplicita, la misura conferma la sessione CME invece di contraddirla.
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
///
/// <para><b>Gli orari sono in ora di borsa (America/Chicago), non nell'orologio del feed.</b>
/// La sessione e' la giornata CME 17:00–16:00 e la finestra operativa e' la stessa della ricerca,
/// riespressa: il motore Python lavorava su barre in ora europea e dichiarava gli orari in CET,
/// che e' Chicago piu' sette ore. Il motore converte l'istante UTC della barra in ora di Chicago
/// e confronta li', quindi il risultato non dipende piu' da come e' stampato il feed. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c> e <c>docs/domini/mappa-strategie-pts.md</c>.</para>
///
/// <para><b>Residuo noto.</b> Mezzanotte CET e le 17:00 di Chicago sono lo stesso istante tranne
/// nelle circa quattro settimane l'anno in cui l'ora legale americana ed europea non sono
/// allineate. In quelle giornate — il 6,6% dei trade delle liste di riferimento — questa classe
/// segue la sessione CME vera e diverge dalla ricerca, deliberatamente.</para>
/// </summary>
public sealed class PTS_NQ_PCH_001_15 : PriceChannelEngine
{
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;

    public PTS_NQ_PCH_001_15()
    {
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        ChannelBars = 100;
        EnableLong = true;
        EnableShort = false;
        Direction = 1;
        OffsetTicks = 2;
        TickSize = 0.25m;
        StartTime = 600;
        EndTime = 2100;
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

    public override string Name => "PTS_NQ_PCH_001_15";
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
