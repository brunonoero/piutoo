using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Price Channel su NQ a 15 minuti con
/// il motore nudo — pattern alle sentinelle, nessun filtro orario, nessuna uscita a tempo — e un
/// <c>Initialize</c> che legge ogni leva. Esiste perche' la griglia grossa (<c>CoarseGridStudy</c>)
/// ha bisogno di una classe con simbolo e timeframe giusti, e la sola PC NQ 15 del catalogo
/// (<c>PTS_NQ_PCH_001_15</c>) sta sul percorso legacy del motore, con chiavi e semantica diverse.
///
/// <para>Porta il numero <c>001</c> solo perche' la convenzione dei nomi vuole i progressivi
/// contigui da 001 per ogni tripla (serie, simbolo, motore): <b>non va in nessun piano</b>. La prima
/// cella NQ 15 che regge la validazione sara' la <c>002</c>, con i parametri trovati e i numeri
/// nel commento, come <c>PT3B_FDAX_PCH_002_240</c>.</para>
///
/// <para>Etichetta della barra sull'apertura come tutte le PT3B: e' la convenzione con cui girano
/// la griglia e la sweep, e va dichiarata nella classe perche' nessun resoconto la stampa.</para>
/// </summary>
public sealed class PT3B_NQ_PCH_001_15 : PriceChannelEngine
{
    public override string Name => "PT3B_NQ_PCH_001_15";

    public override string Description =>
        "PC NQ 15 minuti, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 15;

    public PT3B_NQ_PCH_001_15()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        ChannelBars = 20;
        OffsetTicks = 0;
        TickSize = 0.25m;              // tick NQ
        Direction = 0;
        DvolMin = 0m;
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        MaxEntriesPerSession = 1;

        StopMoney = 1000;              // $ per contratto = 50 punti NQ
        ProfitMoney = 2000;
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
        if (parameters.TryGetValue("ChannelBars", out var channelBars))
            ChannelBars = Convert.ToInt32(channelBars);
        if (parameters.TryGetValue("OffsetTicks", out var offsetTicks))
            OffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("Direction", out var direction))
            Direction = Convert.ToInt32(direction);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = Convert.ToInt32(exitHour) < 0
                ? null
                : new TimeOnly(Convert.ToInt32(exitHour), 0);
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("DvolMin", out var dvolMin))
            DvolMin = Convert.ToDecimal(dvolMin);
        if (parameters.TryGetValue("StopAtr", out var stopAtr))
            StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr))
            TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}
