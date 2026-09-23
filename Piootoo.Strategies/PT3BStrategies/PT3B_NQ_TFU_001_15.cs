using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Contenitore di ricerca, non una strategia da eseguire.</b> Trend Following <i>unmirrored</i> su
/// NQ a 15 minuti con il motore nudo — i quattro gate <c>PatternFast</c> alle sentinelle, nessun
/// filtro orario — e un <c>Initialize</c> che legge ogni leva, compresa l'ora di uscita.
///
/// <para><b>Perche' esiste: e' la seconda prova del trend following, e cambia tre cose insieme.</b>
/// La prima (<c>PT3B_NQ_TFM_001_240</c>, griglia del 22/09) era <i>mirrored</i>, nuda e a 4 ore, e
/// ha dato zero celle ammissibili su 250. Ma la sola trend following coerente del catalogo sui costi
/// veri era <c>PTS_NQ_TFU_003_15</c>: <b>unmirrored</b>, con i pattern, a <b>15 minuti</b>
/// (<c>ricerca/nq-catalogo-costi-veri.md</c>). Questa classe sta su quella cella, e la cerca la
/// <b>sweep</b> (<c>SweepSpaces.TrendFollowingUnmirrored</c>) e non la griglia grossa: i pattern
/// sono la leva principale delle TF e la griglia grossa li tiene fermi alle sentinelle per
/// costruzione.</para>
///
/// <para><b>I gate sono indipendenti per lato</b>, ed e' cio' che rende raggiungibile una TF a
/// senso unico: il pattern 153 e' sempre falso, e messo sul <c>Yes</c> di un lato lo spegne. La
/// direzione, quindi, non e' una leva a parte come nel Price Channel: la scelgono i pattern.</para>
///
/// <para><b>Non va in nessun piano</b>, e il server lo impedisce: vedi
/// <see cref="EasyEngineBase.IsResearchContainer"/>. Se la cella produce una finalista, nasce una
/// classe accanto con i parametri trovati e i numeri nel commento.</para>
/// </summary>
public sealed class PT3B_NQ_TFU_001_15 : TfUnmirroredEngine
{
    public override string Name => "PT3B_NQ_TFU_001_15";

    public override string Description =>
        "TF unmirrored NQ 15 minuti, motore nudo: contenitore per la sweep, NON una strategia da eseguire";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 15;

    /// <inheritdoc />
    public override bool IsResearchContainer => true;

    public PT3B_NQ_TFU_001_15()
    {
        Contracts = 1;
        ResearchLabelsBarsOnOpen = true;
        TradingWindow = ZonedWindow.AllDay;

        SkipDay = -1;

        FastYesLong = 152;             // sentinelle: 152 sempre vero, 153 sempre falso
        FastNoLong = 153;
        FastYesShort = 152;
        FastNoShort = 153;

        IntradayOnly = true;

        StopMoney = 1000;              // $ per contratto = 50 punti NQ, il default della sweep
        ProfitMoney = 3000;
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
        if (parameters.TryGetValue("PtnLyYes", out var lyYes))
            FastYesLong = Convert.ToInt32(lyYes);
        if (parameters.TryGetValue("PtnLyNo", out var lyNo))
            FastNoLong = Convert.ToInt32(lyNo);
        if (parameters.TryGetValue("PtnSyYes", out var syYes))
            FastYesShort = Convert.ToInt32(syYes);
        if (parameters.TryGetValue("PtnSyNo", out var syNo))
            FastNoShort = Convert.ToInt32(syNo);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        // L'ora di uscita vale per ogni motore dal 23/09/2026 (EasyEngineBase.WithSessionExit).
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = ResearchExitHourOrOff(exitHour);
        if (parameters.TryGetValue("StopAtr", out var stopAtr))
            StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr))
            TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}
