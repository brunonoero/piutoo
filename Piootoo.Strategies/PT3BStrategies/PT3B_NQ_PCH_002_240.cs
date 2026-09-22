using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Price Channel su NQ a 4 ore con il
/// motore nudo — pattern alle sentinelle, nessun filtro orario, nessuna uscita a tempo — e un
/// <c>Initialize</c> che legge ogni leva. Gemello a 4 ore di <c>PT3B_NQ_PCH_001_15</c>, da cui
/// eredita il numero progressivo: la convenzione numera per tripla (serie, simbolo, motore), non per
/// timeframe, quindi questa e' la <b>seconda</b> PC su NQ della serie PT3B.
///
/// <para><b>Non va in nessun piano</b>, e il server lo impedisce: vedi
/// <see cref="EasyEngineBase.IsResearchContainer"/>.</para>
///
/// <para><b>Perche' serve.</b> NQ a 4 ore e' stato cercato solo sul 2022-2026, con la sweep e con il
/// criterio nuovo, e in entrambi i casi nessuna finalista e' sopravvissuta. Quella e' una risposta
/// su <i>quattro anni</i>: l'archivio ICS arriva al 2014-07, e la cella non e' mai stata guardata sul
/// periodo lungo. La classe che la sweep usava, <c>PT2_NQ_PCH_001_240</c>, e' stata rimossa con il
/// resto della serie PT2 il 22/09/2026.</para>
///
/// <para><b>Il denaro.</b> Un contratto NQ e' 20 dollari per punto e 5 per tick (0,25). Il range
/// medio della barra da 4 ore e' 42 punti nel campione 2014-2020 e 124 nel 2021-2026: la volatilita'
/// e' triplicata, quindi la soglia del 15% passa da 125 a 373 dollari e i due periodi non si leggono
/// con lo stesso metro.</para>
///
/// <para>Etichetta della barra sull'apertura come tutte le PT3B.</para>
/// </summary>
public sealed class PT3B_NQ_PCH_002_240 : PriceChannelEngine
{
    public override string Name => "PT3B_NQ_PCH_002_240";

    public override string Description =>
        "PC NQ 4 ore, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_NQ_PCH_002_240()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        ChannelBars = 20;
        OffsetTicks = 0;
        TickSize = 0.25m;              // tick NQ: 5 dollari a contratto
        Direction = 0;
        DvolMin = 0m;
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        MaxEntriesPerSession = 1;

        StopMoney = 2000;              // $ per contratto = 100 punti NQ
        ProfitMoney = 4000;
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
