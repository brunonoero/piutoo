using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Price Channel su BP a 60 minuti con
/// il motore nudo — pattern alle sentinelle, nessun filtro orario, nessuna uscita a tempo — e un
/// <c>Initialize</c> che legge ogni leva. Esiste perche' la griglia grossa
/// (<c>CoarseGridStudy</c>) ha bisogno di una classe con simbolo e timeframe giusti, e le due BP del
/// catalogo (<c>PTS_BP_TFM_001_60</c> e <c>PTS_BP_TFM_002_15</c>) sono trend following mirrored,
/// cioe' un altro motore.
///
/// <para>Porta il numero <c>001</c> solo perche' la convenzione dei nomi vuole i progressivi
/// contigui da 001 per ogni tripla (serie, simbolo, motore): <b>non va in nessun piano</b>. La prima
/// cella BP che regge la validazione sara' la <c>002</c>, con i parametri trovati e i numeri nel
/// commento, come <c>PT3B_FDAX_PCH_002_240</c>.</para>
///
/// <para><b>Perche' 60 minuti.</b> L'archivio ICS ha <c>@BP_1</c>, <c>@BP_15</c> e <c>@BP_60</c> dal
/// 2014-09. I 60 minuti danno 71.514 barre su dodici anni, abbastanza per un campione lungo senza il
/// costo dei 15 minuti, che ne hanno quattro volte tante.</para>
///
/// <para><b>Il denaro su BP.</b> Il future CME 6B e' 62.500 sterline quotate in dollari: <b>62.500
/// dollari per punto</b> e 6,25 per tick (0,0001, cioe' un pip). Uno <c>StopMoney</c> di 625 e' un
/// centesimo di cambio, cioe' 100 pip. I valori della griglia (100-1.200) coprono da 16 a 192 pip,
/// contro un range medio della barra da 60 minuti di 22 pip nel campione.</para>
///
/// <para><b>La soglia che morde e' quella dei tick, non quella del range.</b> Su questa cella il 15%
/// del range medio vale 20 dollari mentre 6-7 tick ne valgono 38-44: e' il contrario di quanto
/// succede su indici e metalli, dove il range e' largo e la soglia dei tick e' irrilevante. Un
/// average trade sotto i 40 dollari qui non basta, per quanto il range della barra sia stretto.</para>
///
/// <para>Etichetta della barra sull'apertura come tutte le PT3B: e' la convenzione con cui girano
/// la griglia e la sweep, e va dichiarata nella classe perche' nessun resoconto la stampa.</para>
/// </summary>
public sealed class PT3B_BP_PCH_001_60 : PriceChannelEngine
{
    public override string Name => "PT3B_BP_PCH_001_60";

    public override string Description =>
        "PC BP 60 minuti, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 60;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_BP_PCH_001_60()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        ChannelBars = 20;
        OffsetTicks = 0;
        TickSize = 0.0001m;            // tick BP: un pip, 6,25 dollari a contratto
        Direction = 0;
        DvolMin = 0m;
        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;
        MaxEntriesPerSession = 1;

        StopMoney = 400;               // $ per contratto = 64 pip
        ProfitMoney = 800;
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
