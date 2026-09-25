namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Gli spazi di ricerca dei motori PT5DAV, per i contenitori <c>RC5_*</c>. Le chiavi sono le colonne di
/// <c>strategie_224.csv</c>, le stesse che i contenitori leggono.
///
/// <para><b>Griglie RICOSTRUITE, non verbatim.</b> Il codice Python della ricerca v5.0 (griglie e
/// giri) non e' nella consegna: le griglie sono i valori che le 224 strategie usano, con qualche
/// valore intermedio dove i valori usati lasciavano buchi larghi, e l'ordine delle fasi e' quello dei
/// giri che le schede raccontano ("Come e' stata scelta"): 1 trigger, 2 finestra, 3 stop, 4 tenuta,
/// 5-7 pattern neutri e direzionali, 7b lati, 8 calendario, 9 target, 10 stop finale. E' una
/// deviazione dichiarata, da sostituire con le griglie vere quando arrivano dal server della ricerca;
/// il resoconto della sweep lo scrive.</para>
///
/// <para><b>Le librerie fast e base non si cercano.</b> Nessuna delle 224 accende un pattern
/// <c>ptn_ly_*</c>/<c>ptn_sy_*</c>: le schede dicono "fasce non misurabili, nel dubbio niente filtro", e
/// le colonne servono solo a <b>spegnere un lato</b> (152 acceso, 153 spento, giro "7b lati"). Qui e'
/// la stessa scelta: acceso o spento, non 150 pattern.</para>
/// </summary>
public static class Pt5DavSweepSpaces
{
    /// <summary>I codici di <c>--engine</c> e il contenitore che ciascuno cerca.</summary>
    public static readonly IReadOnlyDictionary<string, string> Containers =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["P5-TFM"] = "RC5_TFM", ["P5-TFU"] = "RC5_TFU", ["P5-PCH"] = "RC5_PCH", ["P5-SBO"] = "RC5_SBO",
            ["P5-BOS"] = "RC5_BOS", ["P5-VBO"] = "RC5_VBO", ["P5-LFD"] = "RC5_LFD", ["P5-LFH"] = "RC5_LFH",
            ["P5-RHL"] = "RC5_RHL", ["P5-RBM"] = "RC5_RBM", ["P5-RBU"] = "RC5_RBU", ["P5-BIA"] = "RC5_BIA",
            ["P5-BRT"] = "RC5_BRT", ["P5-BBO"] = "RC5_BBO", ["P5-MAC"] = "RC5_MAC", ["P5-BSW"] = "RC5_BSW"
        };

    public static bool IsPt5Dav(string engine) => Containers.ContainsKey(engine);

    // ------------------------------------------------------------------ griglie comuni

    private static readonly object[] Hours = Enumerable.Range(-1, 25).Cast<object>().ToArray();

    // Primo valore = default della sweep. La base della ricerca parte senza stop ne' target ("0 base:
    // i default del motore"); lo stop si sceglie al giro 3 e si rifinisce al 10.
    private static readonly object[] StopAtr =
        [0m, 0.2m, 0.3m, 0.4m, 0.5m, 0.6m, 0.65m, 0.75m, 0.8m, 0.9m, 1m, 1.15m, 1.25m, 1.6m, 2.2m, 3m];

    private static readonly object[] TargetAtr =
        [0m, 0.2m, 0.35m, 0.5m, 0.75m, 1m, 1.3m, 1.7m, 2.2m, 3m, 4m, 5.5m, 8m];

    private static readonly object[] Days = [-1, 0, 1, 2, 3, 4];

    private static readonly object[] OffsetAtr = [0m, 0.02m, 0.05m, 0.1m, 0.15m, 0.2m, 0.3m, 0.4m, 0.5m];

    private static object[] NeutralYes() => new object[] { 55 }.Concat(Enumerable.Range(1, 54).Cast<object>()).ToArray();

    private static object[] NeutralNo() => new object[] { 56 }.Concat(Enumerable.Range(1, 54).Cast<object>()).ToArray();

    private static object[] Directional(int sentinel)
    {
        var values = new List<object> { sentinel };
        values.AddRange(Enumerable.Range(1, 51).Cast<object>());
        values.AddRange(Enumerable.Range(1, 51).Select(value => (object)(-value)));
        return values.ToArray();
    }

    /// <summary>
    /// La giornata della ricerca in barre: 92, 46, 23 e 6 a 15, 30, 60 e 240 minuti (le colonne
    /// <c>max_bars</c> della consegna sono multipli di questi).
    /// </summary>
    private static int BarsPerDay(int timeframeMinutes) => timeframeMinutes switch
    {
        15 => 92,
        30 => 46,
        60 => 23,
        240 => 6,
        _ => Math.Max(1, 1380 / Math.Max(1, timeframeMinutes))
    };

    /// <summary>Tenuta: nessun limite, o 1, 2, 3, 5 e 10 giornate (le "notti" delle schede).</summary>
    private static object[] MaxBars(int timeframeMinutes) =>
        new[] { 0, 1, 2, 3, 5, 10 }.Select(days => (object)(days * BarsPerDay(timeframeMinutes))).ToArray();

    // ------------------------------------------------------------------ spazi

    public static SweepSpace For(string engine, int timeframeMinutes)
    {
        var code = engine.ToUpperInvariant();
        return code switch
        {
            "P5-TFM" => Mirrored(code, timeframeMinutes, [], withDirectionalNo: true, days: "skip"),
            "P5-TFU" => Sides(code, timeframeMinutes, [], days: "skip", sentinelOn: 152, sentinelOff: 153),
            "P5-PCH" => Mirrored(code, timeframeMinutes,
            [
                new("channel_len", [20, 1, 5, 10, 15, 30, 50, 75, 100, 155]),
                new("breakout_offset_atr", OffsetAtr, OffSentinel: 0m),
                new("direction", [0, 1, 2], Categorical: true)
            ], withDirectionalNo: true, days: "skip"),
            "P5-SBO" => Mirrored(code, timeframeMinutes,
            [
                new("n_sess", [2, 1, 3, 4, 5]),
                new("lev_include_sess0", [0, 1], Categorical: true),
                new("breakout_offset_atr", OffsetAtr, OffSentinel: 0m)
            ], withDirectionalNo: true, days: "skip"),
            "P5-BOS" => Mirrored(code, timeframeMinutes,
            [
                new("breakout_offset_atr", OffsetAtr, OffSentinel: 0m)
            ], withDirectionalNo: true, days: "skip"),
            "P5-VBO" => Mirrored(code, timeframeMinutes,
            [
                new("vol_source", [1, 3], Categorical: true),
                new("atr_len", [14, 5, 10, 20]),
                new("vol_mult", [0.5m, 0.3m, 0.7m, 1m, 1.5m, 2m, 3m]),
                new("vol_mult_short", [-1m, 0.3m, 0.7m, 1m, 2m, 3m, 4m, 6m, 9.5m]),
                new("direction", [0, 1, 2], Categorical: true)
            ], withDirectionalNo: true, days: "skip"),
            "P5-LFD" or "P5-LFH" => Mirrored(code, timeframeMinutes,
            [
                new("level_shift_atr", [0m, 0.02m, 0.05m, 0.1m, 0.15m, 0.2m, 0.3m, 0.4m, 0.5m], OffSentinel: 0m)
            ], withDirectionalNo: false, days: "sides"),
            "P5-RHL" => Mirrored(code, timeframeMinutes,
            [
                new("long_offset_atr", OffsetAtr, OffSentinel: 0m),
                new("short_offset_atr", OffsetAtr, OffSentinel: 0m),
                new("direction", [0, 1, 2], Categorical: true)
            ], withDirectionalNo: true, days: "skip"),
            "P5-RBM" => Mirrored(code, timeframeMinutes,
            [
                new("bb_length", [20, 10, 14, 30, 50]),
                new("bb_num_devs", [2m, 1.5m, 2.5m, 3m])
            ], withDirectionalNo: true, days: "skip"),
            "P5-RBU" => Sides(code, timeframeMinutes,
            [
                new("bb_length", [20, 10, 14, 30, 50]),
                new("bb_num_devs", [2m, 1.5m, 2.5m, 3m])
            ], days: "skip", sentinelOn: 152, sentinelOff: 153),
            "P5-MAC" => Fixed(code, timeframeMinutes,
            [
                new("fast", [12, 5, 8, 16, 20]),
                new("slow", [30, 20, 24, 40, 50]),
                new("gradient_length", [2, 1, 3, 5]),
                new("gradient_factor", [1.2m, 0.8m, 1.6m, 2m]),
                new("daily_factor", [0.5m, 0.3m, 0.7m, 1m]),
                new("direction", [0, 1, 2], Categorical: true)
            ]),
            "P5-BIA" or "P5-BRT" or "P5-BBO" => Bias(code, timeframeMinutes, withBreakoutBars: code != "P5-BIA"),
            "P5-BSW" => BiasWeekly(code, timeframeMinutes),
            _ => throw new ArgumentException($"motore PT5DAV sconosciuto: {engine}")
        };
    }

    /// <summary>Motori con pattern neutri e direzionali speculari, finestra, tenuta e giorni.</summary>
    private static SweepSpace Mirrored(
        string engine, int timeframeMinutes, SweepParameter[] trigger, bool withDirectionalNo, string days)
    {
        var parameters = new List<SweepParameter>(trigger);
        parameters.AddRange(Common(timeframeMinutes, days));
        parameters.Add(new("ptn_neut_yes", NeutralYes(), Categorical: true));
        parameters.Add(new("ptn_neut_no", NeutralNo(), Categorical: true));
        parameters.Add(new("ptn_dir_yes", Directional(52), Categorical: true));
        if (withDirectionalNo)
            parameters.Add(new("ptn_dir_no", Directional(53), Categorical: true));

        var phases = new List<SweepPhase>();
        if (trigger.Length > 0)
            phases.Add(new("1 trigger", trigger.Select(p => p.Key).ToArray()));
        phases.AddRange(WindowStopHolding());
        phases.Add(new("5 YES neutro", ["ptn_neut_yes"]));
        phases.Add(new("6 NO neutro", ["ptn_neut_no"]));
        phases.Add(new("7 YES direzionale", ["ptn_dir_yes"]));
        if (withDirectionalNo)
            phases.Add(new("7 NO direzionale", ["ptn_dir_no"]));
        phases.AddRange(Tail(days));

        var sentinels = new Dictionary<string, object>
        {
            ["ptn_neut_yes"] = 55, ["ptn_neut_no"] = 56, ["ptn_dir_yes"] = 52
        };
        if (withDirectionalNo)
            sentinels["ptn_dir_no"] = 53;

        return new SweepSpace(engine, parameters, phases, patternSentinels: sentinels);
    }

    /// <summary>
    /// Motori con i lati indipendenti (TF_U, RBB_U): il giro "7b lati" accende o spegne ciascun lato.
    /// Spenti entrambi la strategia non opera: resta una configurazione legittima, che il criterio
    /// scarta da solo perche' non ha trade.
    /// </summary>
    private static SweepSpace Sides(
        string engine, int timeframeMinutes, SweepParameter[] trigger, string days, int sentinelOn, int sentinelOff)
    {
        var parameters = new List<SweepParameter>(trigger);
        parameters.AddRange(Common(timeframeMinutes, days));
        parameters.Add(new("ptn_ly_yes", [sentinelOn, sentinelOff], Categorical: true));
        parameters.Add(new("ptn_sy_yes", [sentinelOn, sentinelOff], Categorical: true));

        var phases = new List<SweepPhase>();
        if (trigger.Length > 0)
            phases.Add(new("1 trigger", trigger.Select(p => p.Key).ToArray()));
        phases.AddRange(WindowStopHolding());
        phases.Add(new("7b lati", ["ptn_ly_yes", "ptn_sy_yes"]));
        phases.AddRange(Tail(days));

        return new SweepSpace(engine, parameters, phases);
    }

    /// <summary>MAC: niente finestra, niente tenuta, niente pattern; esce sull'incrocio inverso.</summary>
    private static SweepSpace Fixed(string engine, int timeframeMinutes, SweepParameter[] trigger)
    {
        var parameters = new List<SweepParameter>(trigger)
        {
            Timeframe(timeframeMinutes),
            new("stop_atr", StopAtr, OffSentinel: 0m),
            new("take_profit_atr", TargetAtr, OffSentinel: 0m)
        };

        return new SweepSpace(engine, parameters,
        [
            new("1 trigger", trigger.Select(p => p.Key).ToArray()),
            new("3 stop", ["stop_atr"]),
            new("9 target", ["take_profit_atr"]),
            new("10 stop finale", ["stop_atr"])
        ]);
    }

    /// <summary>
    /// BIAS a conteggio di barre: i trigger si cercano per lato (i giri 1 trigger[L] e [S] delle
    /// schede), i lati si accendono al 7b, i giorni per lato all'8. Sempre intraday, senza finestra.
    /// </summary>
    private static SweepSpace Bias(string engine, int timeframeMinutes, bool withBreakoutBars)
    {
        var day = BarsPerDay(timeframeMinutes);
        object[] Bars(params int[] fractions) =>
            fractions.Select(value => (object)Math.Max(1, value)).Distinct().ToArray();

        // Indici di barra dentro la giornata: l'inizio, qualche barra dopo, e frazioni della giornata.
        var arm = Bars(5, 1, 2, 3, 9, 10, day / 4, day / 2, (3 * day) / 4);
        var exit = Bars(14, 3, 6, 7, 17, 28, 34, 41, 55, day / 2, day - 1);
        var end = Bars(day / 2, 2, 7, 12, 14, 28, 45, day - 1);

        var parameters = new List<SweepParameter>
        {
            Timeframe(timeframeMinutes),
            new("le_bar", arm), new("lx_bar", exit), new("end_long", end),
            new("se_bar", arm), new("sx_bar", exit), new("end_short", end),
            new("stop_atr", StopAtr, OffSentinel: 0m),
            new("take_profit_atr", TargetAtr, OffSentinel: 0m),
            new("ptn_ly_yes", [152, 153], Categorical: true),
            new("ptn_sy_yes", [152, 153], Categorical: true),
            new("not_le_day", Days, Categorical: true),
            new("not_se_day", Days, Categorical: true)
        };

        var triggerLong = new List<string> { "le_bar", "lx_bar", "end_long" };
        var triggerShort = new List<string> { "se_bar", "sx_bar", "end_short" };
        if (withBreakoutBars)
        {
            parameters.Add(new("nhigh", [1, 2, 3, 5]));
            parameters.Add(new("nlow", [1, 2, 3, 5]));
            triggerLong.Add("nhigh");
            triggerShort.Add("nlow");
        }

        return new SweepSpace(engine, parameters,
        [
            new("1 trigger[L]", triggerLong),
            new("1 trigger[S]", triggerShort),
            new("3 stop", ["stop_atr"]),
            new("7b lati", ["ptn_ly_yes", "ptn_sy_yes"]),
            new("8 calendario", ["not_le_day", "not_se_day"]),
            new("9 target", ["take_profit_atr"]),
            new("10 stop finale", ["stop_atr"])
        ]);
    }

    /// <summary>BIAS settimanale: giorno e ora d'ingresso e d'uscita, per lato.</summary>
    private static SweepSpace BiasWeekly(string engine, int timeframeMinutes)
    {
        object[] Times() => Enumerable.Range(0, 24).Select(hour => (object)(hour * 100)).ToArray();
        object[] Weekdays(int first) => new object[] { first }.Concat(Enumerable.Range(0, 5).Where(d => d != first).Cast<object>()).ToArray();

        var parameters = new List<SweepParameter>
        {
            Timeframe(timeframeMinutes),
            new("le_day", Weekdays(0), Categorical: true), new("le_time", Times()),
            new("lx_day", Weekdays(4), Categorical: true), new("lx_time", Times()),
            new("se_day", new object[] { -1 }.Concat(Enumerable.Range(0, 5).Cast<object>()).ToArray(), Categorical: true),
            new("se_time", Times()),
            new("sx_day", Weekdays(4), Categorical: true), new("sx_time", Times()),
            new("stop_atr", StopAtr, OffSentinel: 0m),
            new("take_profit_atr", TargetAtr, OffSentinel: 0m)
        };

        return new SweepSpace(engine, parameters,
        [
            new("1 trigger[L] giorni", ["le_day", "lx_day"]),
            new("1 trigger[L] ore", ["le_time", "lx_time"]),
            new("1 trigger[S] giorni", ["se_day", "sx_day"]),
            new("1 trigger[S] ore", ["se_time", "sx_time"]),
            new("3 stop", ["stop_atr"]),
            new("9 target", ["take_profit_atr"]),
            new("10 stop finale", ["stop_atr"])
        ]);
    }

    // ------------------------------------------------------------------ pezzi comuni

    /// <summary>Il timeframe del contenitore: un solo valore, non si cerca.</summary>
    private static SweepParameter Timeframe(int timeframeMinutes) => new("TimeframeMinutes", [timeframeMinutes]);

    private static IEnumerable<SweepParameter> Common(int timeframeMinutes, string days)
    {
        yield return Timeframe(timeframeMinutes);
        yield return new("start_hour", Hours, OffSentinel: -1);
        yield return new("end_hour", Hours, OffSentinel: -1);
        yield return new("stop_atr", StopAtr, OffSentinel: 0m);
        yield return new("take_profit_atr", TargetAtr, OffSentinel: 0m);
        yield return new("intraday_only", [1, 0], Categorical: true);
        yield return new("max_bars", MaxBars(timeframeMinutes), OffSentinel: 0);
        if (days == "skip")
        {
            yield return new("skip_day", Days, Categorical: true);
        }
        else
        {
            yield return new("not_le_day", Days, Categorical: true);
            yield return new("not_se_day", Days, Categorical: true);
        }
    }

    private static IEnumerable<SweepPhase> WindowStopHolding() =>
    [
        new("2 finestra", ["start_hour", "end_hour"]),
        new("3 stop", ["stop_atr"]),
        new("4 tenuta", ["intraday_only", "max_bars"])
    ];

    private static IEnumerable<SweepPhase> Tail(string days) =>
    [
        new("8 calendario", days == "skip" ? ["skip_day"] : ["not_le_day", "not_se_day"]),
        new("9 target", ["take_profit_atr"]),
        new("10 stop finale", ["stop_atr"])
    ];
}
