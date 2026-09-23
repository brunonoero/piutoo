using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Volatility breakout su FDAX a 4 ore
/// con il motore nudo — stop a <c>O_d0 ± k × (H_d1 − L_d1)</c>, pattern alle sentinelle, nessun
/// filtro orario, nessun momentum — e un <c>Initialize</c> che legge ogni leva.
///
/// <para><b>Perche' esiste.</b> E' il terzo motore sulla cella FDAX 4 ore, dopo breakout di sessione
/// e reversal di Bollinger. Rompe dall'<b>apertura della sessione</b> e non da un estremo passato:
/// e' un'altra famiglia di trigger, e se anche questa vede l'edge la cella lo ha per conto suo. La
/// leva strutturale e' il moltiplicatore <c>k</c> (<c>AtrMultiplierLong</c>, simmetrico sullo
/// short); la volatilita' e' il range della sessione precedente (<c>vol_source = 1</c>), il default
/// del motore di ricerca e la sola forma senza un secondo parametro.</para>
///
/// <para><b>Non va in nessun piano</b>: vedi <see cref="EasyEngineBase.IsResearchContainer"/>.</para>
/// </summary>
public sealed class PT3B_FDAX_VBO_001_240 : VolatilityBreakoutEngine
{
    public override string Name => "PT3B_FDAX_VBO_001_240";

    public override string Description =>
        "VBO FDAX 4 ore, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_FDAX_VBO_001_240()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        EntryOrderType = TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;
        VolatilitySource = 1;          // range della sessione precedente: nessun ATR da scegliere
        AtrLength = 0;
        AtrMultiplierLong = 0.5m;      // k, la leva della griglia
        AtrMultiplierShort = -1m;      // -1 = simmetrico
        Momentum = 0;
        Direction = 0;
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        MaxEntriesPerSession = 1;

        StopMoney = 5000;              // $ per contratto = 200 punti FDAX
        ProfitMoney = 4500;
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 0;
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

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
        if (parameters.TryGetValue("VolatilitySource", out var volSource))
            VolatilitySource = Convert.ToInt32(volSource);
        if (parameters.TryGetValue("AtrLength", out var atrLength))
            AtrLength = Convert.ToInt32(atrLength);
        if (parameters.TryGetValue("AtrMultiplierLong", out var multLong))
            AtrMultiplierLong = Convert.ToDecimal(multLong);
        if (parameters.TryGetValue("AtrMultiplierShort", out var multShort))
            AtrMultiplierShort = Convert.ToDecimal(multShort);
        if (parameters.TryGetValue("Momentum", out var momentum))
            Momentum = Convert.ToInt32(momentum);
        if (parameters.TryGetValue("Direction", out var direction))
            Direction = Convert.ToInt32(direction);
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
