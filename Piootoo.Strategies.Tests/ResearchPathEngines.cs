using Piootoo.Core.Optimization.Sweep;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I percorsi v4 dei motori che hanno un contenitore generico (<c>RC_*</c>): strategia di base, passi
/// nell'ordine della famiglia e griglie (v4.0 §8-§9). Le chiavi sono quelle che il contenitore accetta:
/// <c>ResearchPathEnginesTests</c> prova ogni valore di ogni passo contro il contenitore, perche' una
/// leva che la classe non legge renderebbe il passo inerte in silenzio.
///
/// <para><b>Stop e target in ATR</b> delle sessioni chiuse, come la v5.0: la griglia vale per ogni
/// mercato senza riscalarla. La base parte da 0,8 ATR, il primo valore della griglia di stop v4.</para>
/// </summary>
public static class ResearchPathEngines
{
    private static readonly object[] Hours = Enumerable.Range(-1, 25).Cast<object>().ToArray();
    private static readonly object[] StopAtr = [0.3m, 0.4m, 0.5m, 0.6m, 0.8m, 1.0m, 1.25m, 1.5m, 2.0m, 2.5m, 3.0m];
    private static readonly object[] TargetAtr = [0m, 0.5m, 0.75m, 1.0m, 1.5m, 2.0m, 3.0m, 4.0m];
    private static readonly object[] Days = [-1, 0, 1, 2, 3, 4];
    private static readonly object[] Durations = [0, 2, 4, 6, 8, 12, 20];

    private static object[] Neutral(int sentinel) => new object[] { sentinel }.Concat(Enumerable.Range(1, 54).Cast<object>()).ToArray();

    private static object[] Directional(int sentinel) =>
        new object[] { sentinel }.Concat(Enumerable.Range(1, 51).Cast<object>()).Concat(Enumerable.Range(1, 51).Select(v => (object)(-v))).ToArray();

    private static object[] Fast(int sentinel)
    {
        var values = new List<object> { sentinel };
        values.AddRange(Enumerable.Range(1, 151).Cast<object>());
        if (sentinel == 152) values.Add(153);
        return values.ToArray();
    }

    /// <summary>Famiglia del motore (§9.3): trend following o controtrend.</summary>
    private enum Family { TrendFollowing, CounterTrend }

    /// <summary>Quali pattern legge il motore.</summary>
    private enum Patterns { None, Mirrored, Unmirrored, LevelFader }

    private sealed record Engine(
        string Container, string Label, Family Family, Patterns Patterns, bool HasWindow, bool HasSkipDay,
        PathStep[] Triggers, Dictionary<string, object>? Fixed = null, bool DayPerSide = false);

    private static PathStep Lever(string key, object[] values, bool ordinal = true) =>
        new($"trigger {key}", PathStepKind.Trigger, key, values, Ordinal: ordinal);

    private static PathStep Direction() => Lever("Direction", [0, 1, 2], ordinal: false);

    private static readonly Dictionary<string, Engine> Table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PCH"] = new("RC_PCH", "Price Channel", Family.TrendFollowing, Patterns.Mirrored, true, true,
            [Lever("ChannelBars", [1, 5, 10, 15, 20, 30, 40, 50, 75, 100]), Lever("OffsetTicks", [0, 2, 5, 10]), Direction()]),
        ["TFM"] = new("RC_TFM", "Trend following mirrored", Family.TrendFollowing, Patterns.Mirrored, true, true, []),
        ["TFU"] = new("RC_TFU", "Trend following unmirrored", Family.TrendFollowing, Patterns.Unmirrored, true, true, []),
        ["BO"] = new("RC_SBO", "Breakout di N sessioni", Family.TrendFollowing, Patterns.Mirrored, true, true,
            [Lever("Sessions", [1, 2, 3, 4, 5, 7, 10]), Lever("BreakoutOffsetTicks", [0, 2, 5, 10, 20]),
             Lever("IncludeCurrentSession", [0, 1], ordinal: false)],
            new() { ["LevelSource"] = 0, ["IncludeCurrentSession"] = 0 }),
        ["BOS"] = new("RC_SBO", "Breakout della sessione in corso", Family.TrendFollowing, Patterns.Mirrored, true, true,
            [Lever("BreakoutOffsetTicks", [0, 2, 5, 10, 20])],
            new() { ["LevelSource"] = 1 }),
        ["VBO"] = new("RC_VBO", "Volatility breakout", Family.TrendFollowing, Patterns.Mirrored, true, true,
            [Lever("AtrMultiplierLong", [0.3m, 0.5m, 0.7m, 1.0m, 1.5m, 2.0m]), Direction()]),
        ["MAC"] = new("RC_MAC", "Incrocio di medie", Family.TrendFollowing, Patterns.None, false, false,
            [Lever("FastPeriod", [5, 10, 15, 20, 30]), Lever("SlowPeriod", [30, 50, 100, 150, 200]), Direction()]),
        ["RBM"] = new("RC_RBM", "Reversal Bollinger mirrored", Family.CounterTrend, Patterns.Mirrored, true, true,
            [Lever("BbLength", [10, 15, 20, 30, 50]), Lever("BbNumDevs", [1.5m, 2.0m, 2.5m, 3.0m])]),
        ["RBU"] = new("RC_RBU", "Reversal Bollinger unmirrored", Family.CounterTrend, Patterns.Unmirrored, true, true,
            [Lever("BbLength", [10, 15, 20, 30, 50]), Lever("BbNumDevs", [1.5m, 2.0m, 2.5m, 3.0m])]),
        ["RHL"] = new("RC_RHL", "Reversal sui livelli di ieri", Family.CounterTrend, Patterns.Mirrored, true, true,
            [Lever("LevelOffsetTicks", [0, 2, 5, 10, 20]), Direction()]),
        ["LF"] = new("RC_LFD", "Level fader sul pivot di ieri", Family.CounterTrend, Patterns.LevelFader, false, false,
            [Lever("LevelShift", [0, 2, 5, 10, 20])], new() { ["LevelChoice"] = 1 }, DayPerSide: true),
        ["LFHL"] = new("RC_LFD", "Level fader sugli estremi di ieri", Family.CounterTrend, Patterns.LevelFader, false, false,
            [Lever("LevelShift", [0, 2, 5, 10, 20])], new() { ["LevelChoice"] = 2 }, DayPerSide: true)
    };

    public static IReadOnlyCollection<string> Keys => Table.Keys;

    public static string Container(string engine) => Table[engine].Container;

    public static string Label(string engine) => Table[engine].Label;

    /// <summary>Il percorso di un motore su un simbolo e un timeframe.</summary>
    public static PathDefinition Definition(string engineKey, string symbol, int timeframeMinutes)
    {
        var engine = Table[engineKey];
        var counterTrend = engine.Family == Family.CounterTrend;
        var barsPerSession = Math.Max(1, 1380 / timeframeMinutes);

        // §9.2: target, trailing e breakeven spenti, stop al primo valore della griglia, durata 4 barre
        // per i controtrend, tutte le ore, pattern e filtri spenti.
        var baseParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Symbol"] = symbol,
            ["TimeframeMinutes"] = timeframeMinutes,
            ["StopLoss"] = 0, ["TakeProfit"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0,
            ["StopAtr"] = 0.8m, ["TargetAtr"] = 0m,
            ["MaxBars"] = counterTrend ? 4 : 0,
            ["IntradayOnly"] = 1,
            ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = -1
        };
        foreach (var (key, value) in engine.Fixed ?? [])
            baseParameters[key] = value;

        var sentinels = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var patternSteps = new List<PathStep>();
        switch (engine.Patterns)
        {
            case Patterns.Mirrored:
                Add(patternSteps, sentinels, baseParameters, new("YES", PathStepKind.Filter, "PtnNeutYes", Neutral(55), 55, Pattern: true));
                Add(patternSteps, sentinels, baseParameters, new("NO", PathStepKind.Filter, "PtnNeutNo", Neutral(56), 56, Pattern: true));
                Add(patternSteps, sentinels, baseParameters, new("direzionale YES", PathStepKind.Filter, "PtnDirYes", Directional(52), 52, Pattern: true, Directional: true));
                Add(patternSteps, sentinels, baseParameters, new("direzionale NO", PathStepKind.Filter, "PtnDirNo", Directional(53), 53, Pattern: true, Directional: true,
                    OnlyIfOff: ("PtnDirYes", 52)));
                break;
            case Patterns.Unmirrored:
                Add(patternSteps, sentinels, baseParameters, new("YES long", PathStepKind.Filter, "FastYesLong", Fast(152), 152, Pattern: true, AlwaysFalse: 153));
                Add(patternSteps, sentinels, baseParameters, new("YES short", PathStepKind.Filter, "FastYesShort", Fast(152), 152, Pattern: true, AlwaysFalse: 153));
                Add(patternSteps, sentinels, baseParameters, new("NO long", PathStepKind.Filter, "FastNoLong", Fast(153), 153, Pattern: true));
                Add(patternSteps, sentinels, baseParameters, new("NO short", PathStepKind.Filter, "FastNoShort", Fast(153), 153, Pattern: true));
                break;
            case Patterns.LevelFader:
                Add(patternSteps, sentinels, baseParameters, new("YES", PathStepKind.Filter, "PtnNeutYes", Neutral(55), 55, Pattern: true));
                Add(patternSteps, sentinels, baseParameters, new("NO", PathStepKind.Filter, "PtnNeutNo", Neutral(56), 56, Pattern: true));
                Add(patternSteps, sentinels, baseParameters, new("direzionale YES", PathStepKind.Filter, "PtnDirYes", Directional(52), 52, Pattern: true, Directional: true));
                break;
        }

        var window = engine.HasWindow
            ? new[]
            {
                new PathStep("finestra: inizio", PathStepKind.Window, "StartHour", Hours, -1, Ordinal: true, Circular: true),
                new PathStep("finestra: fine", PathStepKind.Window, "EndHour", Hours, -1, Ordinal: true, Circular: true)
            }
            : [];

        var stop = new PathStep("stop", PathStepKind.Stop, "StopAtr", StopAtr, Ordinal: true);
        var target = new PathStep("target", PathStepKind.Filter, "TargetAtr", TargetAtr, 0m, Ordinal: true);

        // §9.9: nella famiglia TF, passare a overnight senza una durata porta la durata a una sessione.
        var holding = new PathStep("intraday o overnight", PathStepKind.Choice, "IntradayOnly", [1, 0])
        {
            Adjust = counterTrend
                ? null
                : (value, parameters) =>
                {
                    var next = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
                    var maxBars = Convert.ToInt32(next["MaxBars"]);
                    if (Convert.ToInt32(value) == 0 && maxBars == 0) next["MaxBars"] = barsPerSession;
                    if (Convert.ToInt32(value) == 1 && maxBars == barsPerSession) next["MaxBars"] = 0;
                    return next;
                }
        };

        var calendar = new List<PathStep>();
        if (engine.HasSkipDay)
        {
            calendar.Add(new PathStep("calendario", PathStepKind.Filter, "SkipDay", Days, -1));
            baseParameters["SkipDay"] = -1;
        }
        else if (engine.DayPerSide)
        {
            calendar.Add(new PathStep("calendario long", PathStepKind.Filter, "NotEntryDayLong", Days, -1));
            calendar.Add(new PathStep("calendario short", PathStepKind.Filter, "NotEntryDayShort", Days, -1));
            baseParameters["NotEntryDayLong"] = -1;
            baseParameters["NotEntryDayShort"] = -1;
        }

        // Le leve del trigger si scelgono insieme (PathStep.Grid): una alla volta, il canale si sceglieva
        // prima della direzione e sul DAX lo short in perdita decideva la lunghezza del canale.
        var steps = new List<PathStep>();
        if (engine.Triggers.Length > 0)
            steps.Add(PathStep.Grid("trigger", engine.Triggers.Select(lever => (lever.Key, lever.Values.ToArray())).ToList()));
        steps.AddRange(window);
        steps.Add(stop);
        if (counterTrend)
        {
            steps.Add(target);
            steps.AddRange(patternSteps);
        }
        else
        {
            steps.Add(holding);
            steps.AddRange(patternSteps);
        }

        // R9 dopo l'ultimo passo prima di calendario e filtri; se il motore non ha passi, dopo il trigger.
        var r9After = steps.Count > 0 ? steps[^1].Name : null;
        steps.AddRange(calendar);

        // Uscita a un'ora fissa di Roma: DEVIAZIONE dichiarata da v4.0, che non ha questo parametro. E' la
        // leva che ha fatto PT3B_FDAX_PCH_002_240, e tiene le posizioni lontane dal rollover del broker.
        var exitHour = new PathStep("uscita a ora fissa", PathStepKind.Filter, "ExitHour",
            [-1, 15, 16, 17, 18, 19, 20, 21, 22], -1, Ordinal: true);

        if (counterTrend)
        {
            steps.Add(holding);
            steps.Add(exitHour);
            steps.Add(stop with { Name = "affinamento stop", Refine = true });
            steps.Add(target with { Name = "affinamento target", Refine = true });
            steps.Add(new PathStep("durata", PathStepKind.Duration, "MaxBars", Durations, 0, Ordinal: true));
        }
        else
        {
            steps.Add(exitHour);
            steps.Add(target);
            steps.Add(stop with { Name = "affinamento stop", Refine = true });
        }

        return new PathDefinition($"{engineKey} {engine.Label}", baseParameters, steps, r9After)
        {
            PatternSentinels = sentinels,
            PlateauGrids = engine.Triggers.Where(lever => lever.Ordinal)
                .ToDictionary(lever => lever.Key, lever => lever.Values, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void Add(List<PathStep> steps, Dictionary<string, object> sentinels, Dictionary<string, object> baseParameters, PathStep step)
    {
        steps.Add(step);
        sentinels[step.Key] = step.Off!;
        baseParameters[step.Key] = step.Off!;
    }
}
