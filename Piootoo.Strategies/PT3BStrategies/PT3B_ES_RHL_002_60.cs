using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Reversal sul minimo di ieri, solo long, sull'S&amp;P 500 a 1 ora.</b> Compra a limite 10 tick (2,5 punti)
/// sotto il minimo della sessione precedente, a qualunque ora, tranne la domenica e quando il pattern
/// direzionale 1 e' vero; stop a 0,8 ATR, nessun target, uscita dopo 4 barre o alle 21 dell'orologio della
/// ricerca, intraday. Il livello gia' superato si esegue a mercato all'apertura della barra.
///
/// <para><b>Da dove viene.</b> Percorso di ricerca v4 sul contenitore <c>RC_RHL</c>, cella ES 1h, ricerca sul
/// feed FTMO 09/11/2020 → 01/09/2024 con la tenuta dei piani PT3B (niente overnight, flat alle 20:45 UTC):
/// <c>ricerca/percorso/es-60-ricerca-ftmo-flat.md</c> (29/09/2026). Nella ricerca: 128 trade, +26.177,
/// net/DD 3,20.</para>
///
/// <para><b>Cosa regge, su periodi che la ricerca non ha visto</b> (stessi parametri, un contratto,
/// <c>ricerca/percorso/es-60-rhl-flat-verifiche.md</c>):</para>
/// <list type="table">
///   <item><term>prova FTMO 09/2024 → 09/2026</term><description>50 trade, +15.688, net/DD 5,42, average trade 314 (soglia 114), 3/4 finestre</description></item>
///   <item><term>feed interno 2006 → 11/2020</term><description>309 trade, +32.481, net/DD 4,38, 14 anni su 15 in utile (orologio a 60 minuti: l'S&amp;P interno non ha il minuto)</description></item>
///   <item><term>altri indici su FTMO dal 2020</term><description>5 su 7 con net/DD ≥ 1 (Nasdaq 2,28, EuroStoxx 1,94, Dow 1,51, FTSE 1,24, CAC 1,03)</description></item>
///   <item><term>controllo RAN (100 long casuali con le stesse uscite)</term><description>98-99° percentile sulla prova, 100° sul 2006-2020</description></item>
/// </list>
///
/// <para><b>Riserve.</b> Pochi trade, 25-35 l'anno. La regola senza filtri perde: il vantaggio sta nel pattern
/// direzionale 1 e nella domenica esclusa, che il caso e la storia lunga dicono non essere rumore. Non duplica
/// <see cref="PT3B_ES_RHL_001_240"/>: sulla prova nessun ingresso entro 5 minuti, correlazione giornaliera 0,11.</para>
///
/// <para><b>Etichetta della barra.</b> <c>ResearchLabelsBarsOnOpen = true</c>, come ogni PT3B: gli orari si
/// confrontano con l'apertura della barra.</para>
/// </summary>
public sealed class PT3B_ES_RHL_002_60 : RhlEngine
{
    public override string Name => "PT3B_ES_RHL_002_60";

    public override string Description =>
        "RHL ES 1 ora, solo long: limit 10 tick sotto il minimo di ieri, niente domenica ne' pattern direzionale 1, " +
        "stop 0,8 ATR, uscita dopo 4 barre o alle 21, intraday";

    public override string Symbol => "@ES";

    public override int TimeframeMinutes => 60;

    public PT3B_ES_RHL_002_60()
    {
        Contracts = 1;

        // Come il contenitore della ricerca: gli orari si confrontano con l'APERTURA della barra.
        ResearchLabelsBarsOnOpen = true;

        // start_hour -1, end_hour -1: tutta la giornata.
        TradingWindow = ZonedWindow.AllDay;

        TickSize = 0.25m;               // tick ES dal registro
        LongLevelOffsetTicks = 10;      // LevelOffsetTicks 10: limit 2,5 punti sotto il minimo di ieri
        ShortLevelOffsetTicks = 10;     // stesso valore del contenitore; lo short e' spento da Direction
        Direction = 1;                  // solo long
        SkipDay = 0;                    // SkipDay 0: domenica esclusa

        NeutralYes = 55;                // sentinella sempre vera
        NeutralNo = 56;                 // sentinella: nessun divieto
        DirectionalYes = 52;            // sentinella sempre vera
        DirectionalNo = 1;              // vietato il pattern direzionale 1

        IntradayOnly = true;            // chiude dentro la sessione
        SessionExitTime = new TimeOnly(21, 0); // ExitHour 21

        StopMoney = 0;                  // StopLoss 0: lo stop e' in ATR
        ProfitMoney = 0;                // TakeProfit 0
        StopAtrMultiplier = 0.8m;       // StopAtr 0,8
        TargetAtrMultiplier = 0m;       // TargetAtr 0: nessun target
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 4;                    // uscita dopo 4 barre da un'ora
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
