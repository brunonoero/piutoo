using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Price Channel su FDAX a <b>1 ora</b>
/// con il motore nudo — pattern alle sentinelle, nessun filtro orario — e un <c>Initialize</c> che
/// legge ogni leva.
///
/// <para><b>Perche' esiste.</b> FDAX 4 ore e' la sola cella con un edge sopra le soglie del metodo, e
/// la domanda seguente e' se quell'edge sopravvive a una barra piu' fitta sullo stesso mercato e con
/// gli stessi costi. Il catalogo non ha un Price Channel FDAX a 1 ora da usare come veicolo, quindi
/// serve un contenitore.</para>
///
/// <para><b>Sul numero.</b> Porta <c>003</c> perche' la convenzione dei nomi vuole i progressivi
/// contigui per tripla (serie, simbolo, motore) senza distinguere il timeframe, e la 001 e la 002
/// sono a 4 ore. La finalista che la ricerca della regione FDAX 4 ore sta cercando — quella che nei
/// documenti del 22/09 e' chiamata «la 003» — nascera' quindi come <c>004</c>. Il numero e' un
/// progressivo, non un grado.</para>
///
/// <para><b>Non va in nessun piano</b>: vedi <see cref="EasyEngineBase.IsResearchContainer"/>.</para>
/// </summary>
public sealed class PT3B_FDAX_PCH_003_60 : PriceChannelEngine
{
    public override string Name => "PT3B_FDAX_PCH_003_60";

    public override string Description =>
        "PC FDAX 1 ora, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 60;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_FDAX_PCH_003_60()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        ChannelBars = 20;
        OffsetTicks = 0;
        TickSize = 1m;                 // tick FDAX: 1 punto
        Direction = 0;
        DvolMin = 0m;
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        MaxEntriesPerSession = 1;

        StopMoney = 2500;              // $ per contratto = 100 punti FDAX
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
            SessionExitTime = ResearchExitHourOrOff(exitHour);
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
