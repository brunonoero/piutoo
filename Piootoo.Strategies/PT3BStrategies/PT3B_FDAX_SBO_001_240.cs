using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Breakout di sessione su FDAX a 4
/// ore con il motore nudo — pattern alle sentinelle, nessun filtro orario, canale sulle sole
/// sessioni chiuse — e un <c>Initialize</c> che legge ogni leva.
///
/// <para><b>Perche' esiste.</b> FDAX 4 ore e' la sola cella in cui il Price Channel nudo supera le
/// soglie del metodo (<c>ricerca/fdax-4h-griglia-grossa-lunga.md</c>). La domanda seguente e' se
/// l'edge e' del mercato o del motore, e si risponde con altri motori sulla <b>stessa cella, stesso
/// periodo, stessi costi</b>. Il breakout di sessione e' il primo: rompe l'estremo delle ultime N
/// sessioni chiuse, e con N = 1 e' il trend following mirrored. La leva strutturale e' <c>Sessions</c>.</para>
///
/// <para>La direzione la decidono i gate di pattern, che qui sono spenti: il motore emette entrambi
/// i lati e la griglia non passa <c>Direction</c>.</para>
///
/// <para><b>Non va in nessun piano</b>: vedi <see cref="EasyEngineBase.IsResearchContainer"/>.</para>
/// </summary>
public sealed class PT3B_FDAX_SBO_001_240 : SessionBreakoutEngine
{
    public override string Name => "PT3B_FDAX_SBO_001_240";

    public override string Description =>
        "BO FDAX 4 ore, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_FDAX_SBO_001_240()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        LevelSource = 0;               // canale delle sessioni chiuse (level_source 0)
        Sessions = 2;                  // n_sess, la leva della griglia
        IncludeCurrentSession = false; // lev_include_sess0 = 0, il default del motore di ricerca
        BreakoutOffsetTicks = 0;
        TickSize = 1m;                 // tick FDAX: 1 punto
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;

        StopMoney = 5000;              // $ per contratto = 200 punti FDAX
        ProfitMoney = 4500;
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 0;
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
        if (parameters.TryGetValue("Sessions", out var sessions))
            Sessions = Convert.ToInt32(sessions);
        if (parameters.TryGetValue("LevelSource", out var levelSource))
            LevelSource = Convert.ToInt32(levelSource);
        if (parameters.TryGetValue("IncludeCurrentSession", out var includeSess0))
            IncludeCurrentSession = Convert.ToInt32(includeSess0) != 0;
        if (parameters.TryGetValue("BreakoutOffsetTicks", out var offsetTicks))
            BreakoutOffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = ResearchExitHourOrOff(exitHour);
        if (parameters.TryGetValue("StopAtr", out var stopAtr))
            StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr))
            TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}
