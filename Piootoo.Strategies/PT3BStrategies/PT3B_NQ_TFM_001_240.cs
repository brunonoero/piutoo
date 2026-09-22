using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Trend Following mirrored su NQ a 4
/// ore con il motore nudo — pattern alle sentinelle, nessun filtro orario — e un <c>Initialize</c>
/// che legge ogni leva.
///
/// <para><b>Perche' esiste: e' il primo contenitore di un motore diverso dal Price Channel.</b>
/// Quattro griglie su quattro classi di sottostante — GC, NQ, CL, BP — hanno detto che il Price
/// Channel nudo non produce un average trade sopra soglia da nessuna parte, e il migliore (GC) si
/// ferma al 77%. A quel punto la domanda non e' piu' su quale mercato cercare ma su quale motore, e
/// il confronto che la risponde e' <b>stesso simbolo, stesso timeframe, stesso periodo, stessi
/// costi, motore diverso</b>: questa classe sta apposta su NQ a 4 ore, la stessa cella di
/// <c>PT3B_NQ_PCH_002_240</c>.</para>
///
/// <para><b>Le leve non sono le stesse, ed e' il punto.</b> Il trend following non ha un canale da
/// scegliere: il livello di ingresso e' l'<b>estremo della sessione precedente</b>, che non e' un
/// parametro. Al suo posto la leva strutturale e' <c>MaxBars</c>, cioe' dopo quante barre la
/// posizione muore — su una 4 ore, 6 barre sono un giorno. La direzione non e' una leva: il motore
/// emette entrambi i lati e a filtrarli sono i gate di pattern, che qui sono spenti.</para>
///
/// <para><b>Non va in nessun piano</b>, e il server lo impedisce: vedi
/// <see cref="EasyEngineBase.IsResearchContainer"/>. Se la cella produce una finalista, nasce una
/// classe accanto con i parametri trovati.</para>
/// </summary>
public sealed class PT3B_NQ_TFM_001_240 : TfMirroredEngine
{
    public override string Name => "PT3B_NQ_TFM_001_240";

    public override string Description =>
        "TF mirrored NQ 4 ore, motore nudo: contenitore per la griglia grossa, NON una strategia da eseguire";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_NQ_TFM_001_240()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        SkipDay = -1;

        NeutralYes = 55;               // sentinelle: nessun pattern
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        IntradayOnly = true;

        StopMoney = 3000;              // $ per contratto = 150 punti NQ
        ProfitMoney = 6000;
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
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        // ExitHour NON si legge, di proposito: SessionExitTime e' dichiarata su EasyEngineBase ma la
        // usa il solo PriceChannelEngine — TfEngineBase chiude su SessionEnd e basta. Leggerla qui
        // farebbe credere che la leva esista, e la griglia del 22/09 ci si e' gia' persa mezza corsa:
        // 250 combinazioni che ne misuravano 25. Vedi ricerca/nq-4h-tf-griglia-grossa.md.
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("StopAtr", out var stopAtr))
            StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr))
            TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}
