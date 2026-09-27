using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// La regola di <see cref="PT3B_FDAX_RHL_001_240"/> portata <b>identica</b> su altri indici a 4 ore:
/// long a limite 20 tick sotto il minimo della sessione precedente, solo con barre che aprono fino alle
/// 10 dell'orologio della ricerca, solo se il pattern neutro 34 e' vero (range della sessione di
/// riferimento oltre l'1,5% del prezzo), stop e target a 1 ATR delle 14 sessioni chiuse, intraday, e il
/// livello gia' superato eseguito a mercato all'apertura della barra.
///
/// <para><b>Perche' una base.</b> Nessun parametro e' stato scelto su questi mercati: la regola viene dal
/// percorso sul DAX (<c>ricerca/percorso/fdax-240-ricerca-ftmo.md</c>), e per ogni indice tutto il feed
/// FTMO e' fuori campione. Una base comune dice esattamente questo: le classi figlie cambiano solo
/// simbolo e tick. Ritoccare un parametro su un indice ne farebbe una strategia nuova, da validare da
/// capo.</para>
///
/// <para><b>La validazione</b> e' il controllo RAN (<c>RhlFdaxRandomControlStudy</c> con
/// <c>PIOOTOO_RAN_SIMBOLO</c>, 27/09/2026): 100 long casuali con le stesse uscite e la stessa finestra,
/// resoconti in <c>ricerca/percorso/{simbolo}-240-rhl-controllo-ran.md</c>. Il FTSE (92° percentile sul
/// netto, 84° su net/DD) e' rimasto fuori.</para>
///
/// <para><b>Riserva comune</b>: pochi trade per indice (4-11 all'anno). Sono componenti di piano, e gli
/// indici si muovono insieme: quanto i loro giorni si sovrappongono lo misura il costruttore dei piani.</para>
/// </summary>
public abstract class Pt3bRhlLongDipBase : RhlEngine
{
    public override int TimeframeMinutes => 240;

    protected Pt3bRhlLongDipBase(decimal tickSize)
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;                              // come il contenitore della ricerca
        TradingWindow = ZonedWindow.AllDay with { End = new TimeOnly(10, 0) }; // start_hour -1, end_hour 10

        TickSize = tickSize;            // tick del simbolo dal registro
        LongLevelOffsetTicks = 20;      // LevelOffsetTicks 20
        ShortLevelOffsetTicks = 20;
        Direction = 1;                  // solo long
        SkipDay = -1;

        NeutralYes = 34;                // highD0 > lowD0 * 1,015
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        SessionExitTime = null;         // ExitHour -1

        StopMoney = 0;
        ProfitMoney = 0;
        StopAtrMultiplier = 1.0m;       // StopAtr 1,0
        TargetAtrMultiplier = 1.0m;     // TargetAtr 1,0
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 0;
    }

    /// <summary>Il livello gia' superato si esegue a mercato, come nella ricerca: vedi <see cref="PT3B_FDAX_RHL_001_240"/>.</summary>
    public new TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var signal = base.GenerateSignal(data, currentDate);
        foreach (var leg in signal.CompanionSignals is { } companions ? companions.Prepend(signal) : [signal])
        {
            if (leg.Type is SignalType.Buy or SignalType.Sell && !leg.ExitOnly)
                leg.CrossedLevel = CrossedLevelPolicy.Market;
        }

        return signal;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts)) Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss)) StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit)) ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("TrailingStop", out var trailing)) TrailingStopMoney = Convert.ToInt32(trailing);
        if (parameters.TryGetValue("BreakEven", out var breakEven)) BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("MaxBars", out var maxBars)) MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes)) NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo)) NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes)) DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo)) DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("LevelOffsetTicks", out var offset))
        {
            LongLevelOffsetTicks = Convert.ToInt32(offset);
            ShortLevelOffsetTicks = Convert.ToInt32(offset);
        }
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("Direction", out var direction)) Direction = Convert.ToInt32(direction);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly)) IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = Convert.ToInt32(exitHour) < 0 ? null : new TimeOnly(Convert.ToInt32(exitHour), 0);
        if (parameters.TryGetValue("SkipDay", out var skipDay)) SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("StopAtr", out var stopAtr)) StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr)) TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}

/// <summary>
/// RHL long sul Nasdaq a 4 ore, regola identica a <see cref="PT3B_FDAX_RHL_001_240"/> (tick 0,25: limit
/// 5 punti sotto il minimo di ieri). Controllo RAN: FTMO 05/2022 → 09/2026, 42 trade, +53.555, net/DD
/// 2,66, sopra il 99% dei semi sul netto; feed interno 2008-2020 mai visto, 49 trade, +34.729, net/DD
/// 3,12, sopra il 100%. Vedi <see cref="Pt3bRhlLongDipBase"/>.
/// </summary>
public sealed class PT3B_NQ_RHL_001_240 : Pt3bRhlLongDipBase
{
    public PT3B_NQ_RHL_001_240() : base(0.25m) { }
    public override string Name => "PT3B_NQ_RHL_001_240";
    public override string Description => "RHL NQ 4 ore, solo long: la regola di PT3B_FDAX_RHL_001_240 sul Nasdaq, senza ritocchi";
    public override string Symbol => "@NQ";
}

/// <summary>
/// RHL long sull'S&amp;P 500 a 4 ore, regola identica a <see cref="PT3B_FDAX_RHL_001_240"/> (tick 0,25).
/// Controllo RAN su FTMO 11/2020 → 09/2026: 35 trade, +28.363, net/DD 4,69, sopra il 96% dei semi sul
/// netto e il 99% su net/DD. Vedi <see cref="Pt3bRhlLongDipBase"/>.
/// </summary>
public sealed class PT3B_ES_RHL_001_240 : Pt3bRhlLongDipBase
{
    public PT3B_ES_RHL_001_240() : base(0.25m) { }
    public override string Name => "PT3B_ES_RHL_001_240";
    public override string Description => "RHL ES 4 ore, solo long: la regola di PT3B_FDAX_RHL_001_240 sull'S&P 500, senza ritocchi";
    public override string Symbol => "@ES";
}

/// <summary>
/// RHL long sul Dow Jones a 4 ore, regola identica a <see cref="PT3B_FDAX_RHL_001_240"/> (tick 1).
/// Controllo RAN su FTMO 11/2020 → 09/2026: 24 trade, +23.633, net/DD 5,90, sopra il 100% dei semi sul
/// netto e il 99% su net/DD. Il piu' raro della famiglia: circa 4 trade all'anno. Vedi
/// <see cref="Pt3bRhlLongDipBase"/>.
/// </summary>
public sealed class PT3B_YM_RHL_001_240 : Pt3bRhlLongDipBase
{
    public PT3B_YM_RHL_001_240() : base(1m) { }
    public override string Name => "PT3B_YM_RHL_001_240";
    public override string Description => "RHL YM 4 ore, solo long: la regola di PT3B_FDAX_RHL_001_240 sul Dow, senza ritocchi";
    public override string Symbol => "@YM";
}

/// <summary>
/// RHL long sull'EuroStoxx 50 a 4 ore, regola identica a <see cref="PT3B_FDAX_RHL_001_240"/> (tick 1).
/// Controllo RAN su FTMO 11/2020 → 09/2026: 67 trade, +7.916, net/DD 5,60, sopra il 100% dei semi su
/// netto e net/DD. Vedi <see cref="Pt3bRhlLongDipBase"/>.
/// </summary>
public sealed class PT3B_FESX_RHL_001_240 : Pt3bRhlLongDipBase
{
    public PT3B_FESX_RHL_001_240() : base(1m) { }
    public override string Name => "PT3B_FESX_RHL_001_240";
    public override string Description => "RHL FESX 4 ore, solo long: la regola di PT3B_FDAX_RHL_001_240 sull'EuroStoxx 50, senza ritocchi";
    public override string Symbol => "@FESX";
}
