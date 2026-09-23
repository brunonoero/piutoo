using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Reversal sulle bande di Bollinger,
/// mirrored, su FDAX a 4 ore con il motore nudo — pattern alle sentinelle, nessun filtro orario — e
/// un <c>Initialize</c> che legge ogni leva.
///
/// <para><b>Perche' esiste.</b> E' il secondo motore sulla cella FDAX 4 ore, dopo il breakout di
/// sessione (<c>PT3B_FDAX_SBO_001_240</c>), e l'unico <b>contrarian</b> dei tre: compra la banda
/// inferiore e vende la superiore con un limit. Se l'edge della cella e' del mercato e non del
/// breakout, un reversal dovrebbe vederlo dall'altra parte. La leva strutturale e' la lunghezza
/// delle bande (<c>BbLength</c>); le deviazioni restano a 2,0.</para>
///
/// <para>La direzione la decidono i gate di pattern, qui spenti: il motore emette entrambi i lati.</para>
///
/// <para><b>Non va in nessun piano</b>: vedi <see cref="EasyEngineBase.IsResearchContainer"/>.</para>
/// </summary>
public sealed class PT3B_FDAX_RBM_001_240 : RbbMirroredEngine
{
    public override string Name => "PT3B_FDAX_RBM_001_240";

    public override string Description =>
        "RBB mirrored FDAX 4 ore, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_FDAX_RBM_001_240()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        BollingerLength = 20;          // bb_length, la leva della griglia
        BollingerNumDevs = 2m;         // bb_num_devs
        DayToFilter = -1;

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
        if (parameters.TryGetValue("BbLength", out var bbLength))
            BollingerLength = Convert.ToInt32(bbLength);
        if (parameters.TryGetValue("BbNumDevs", out var bbNumDevs))
            BollingerNumDevs = Convert.ToDecimal(bbNumDevs);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        // SkipDay e' in convenzione pandas (0 = lunedi'); il motore legge DayToFilter in convenzione
        // EasyLanguage (0 = domenica), quindi lunedi' = 1. -1 resta "nessuno".
        if (parameters.TryGetValue("SkipDay", out var skipDay))
        {
            var day = Convert.ToInt32(skipDay);
            DayToFilter = day < 0 ? -1 : day + 1;
        }
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
