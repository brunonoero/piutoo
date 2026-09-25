using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>Che tipo di scelta fa un passo del percorso. Le regole sono quelle di v4.0 §9.</summary>
public enum PathStepKind
{
    /// <summary>Leva del motore (§9.6): vince l'average trade smussato fra le configurazioni che guadagnano quasi quanto la migliore.</summary>
    Trigger,

    /// <summary>Estremo della finestra oraria (§9.7): come il trigger, ordinale circolare, spento = -1.</summary>
    Window,

    /// <summary>Stop con la regola D2 (§9.8): il piu' piccolo col 95% del miglior netto smussato.</summary>
    Stop,

    /// <summary>
    /// Filtro (§9.10): pattern, direzionale, calendario, filtri del motore, target. Si accende solo se
    /// batte lo spento con le regole del metodo, altrimenti resta spento.
    /// </summary>
    Filter,

    /// <summary>Scelta non ordinale senza spento (§9.9, intraday o overnight): come il trigger.</summary>
    Choice,

    /// <summary>Durata in barre (§9.11): come il trigger, ordinale con spento.</summary>
    Duration
}

/// <summary>
/// Un passo del percorso: muove <b>un</b> parametro, tutto il resto fermo.
/// </summary>
/// <param name="Name">Come lo stampa il resoconto.</param>
/// <param name="Key">Chiave di <c>Initialize</c> del contenitore.</param>
/// <param name="Values">La griglia, spento compreso.</param>
/// <param name="Off">Il valore spento, se il parametro ne ha uno.</param>
/// <param name="Ordinal">Ha senso un vicino di griglia (per lo smussamento).</param>
/// <param name="Circular">Ordinale su un cerchio: le 23 confinano con le 0.</param>
/// <param name="Pattern">Filtro pattern: soglia 2/3 e test dei pattern casuali.</param>
/// <param name="Directional">Filtro direzionale: deve anche abbassare il drawdown.</param>
/// <param name="AlwaysFalse">Il valore "sempre falso" della libreria, che non e' un candidato.</param>
/// <param name="Refine">
/// Affinamento: si provano solo i due valori sopra e i due sotto quello scelto (§9.11), piu' lo spento
/// per un filtro.
/// </param>
/// <param name="OnlyIfOff">
/// Il passo si fa solo se questo parametro e' rimasto al proprio spento: il direzionale vietato
/// si cerca solo se quello richiesto non si e' acceso (§9.10).
/// </param>
public sealed record PathStep(
    string Name,
    PathStepKind Kind,
    string Key,
    IReadOnlyList<object> Values,
    object? Off = null,
    bool Ordinal = false,
    bool Circular = false,
    bool Pattern = false,
    bool Directional = false,
    object? AlwaysFalse = null,
    bool Refine = false,
    (string Key, object Off)? OnlyIfOff = null)
{
    /// <summary>
    /// Ritocca i parametri insieme al valore del passo. Serve al passo intraday/overnight della famiglia
    /// trend following: passare a overnight senza una durata porta la durata a una sessione (§9.9).
    /// </summary>
    public Func<object, IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>>? Adjust { get; init; }

    /// <summary>
    /// Un trigger scelto su <b>piu' leve insieme</b>: ogni valore e' un dizionario di parametri. Deviazione
    /// dichiarata da v4.0 §9.6, che le sceglie una alla volta: scegliendo il canale prima della direzione,
    /// sul DAX lo short in perdita affondava tutti i canali e il percorso del 25/09/2026 ha preso 100
    /// barre per "meno peggio". Il prodotto delle leve di un trigger costa 15-120 simulazioni.
    /// </summary>
    public static PathStep Grid(string name, IReadOnlyList<(string Key, object[] Values)> levers)
    {
        IEnumerable<Dictionary<string, object>> combinations = [new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)];
        foreach (var (key, values) in levers)
        {
            combinations = combinations.SelectMany(partial => values.Select(value =>
                new Dictionary<string, object>(partial, StringComparer.OrdinalIgnoreCase) { [key] = value }));
        }

        return new PathStep(name, PathStepKind.Trigger, string.Join("+", levers.Select(lever => lever.Key)),
            combinations.Cast<object>().ToList());
    }
}

/// <summary>
/// Il percorso di un motore: la strategia di base e i passi in ordine, fino a R9 e dopo R9.
/// </summary>
/// <param name="Engine">Nome del motore nel resoconto.</param>
/// <param name="Base">La strategia di base (§9.2): parametri completi.</param>
/// <param name="Steps">I passi, nell'ordine della famiglia (§9.3).</param>
/// <param name="R9After">Nome del passo dopo il quale si controlla che la base guadagni su tutta la storia.</param>
public sealed record PathDefinition(
    string Engine,
    IReadOnlyDictionary<string, object> Base,
    IReadOnlyList<PathStep> Steps,
    string? R9After = null)
{
    /// <summary>I parametri pattern e la loro sentinella: servono a "pattern utile" e ai pattern casuali.</summary>
    public IReadOnlyDictionary<string, object> PatternSentinels { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Le griglie ordinali che il plateau guarda oltre a quelle dei passi: le leve di un trigger scelto
    /// insieme (<see cref="PathStep.Grid"/>) non hanno un passo proprio, ma i vicini li hanno.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<object>> PlateauGrids { get; init; } =
        new Dictionary<string, IReadOnlyList<object>>();
}

/// <summary>Le costanti del percorso (v4.0 §9.1).</summary>
public sealed record PathOptions
{
    public int MinTrades { get; init; } = 50;
    public decimal KeepFraction { get; init; } = 0.75m;
    public decimal PatternKeepFraction { get; init; } = 2m / 3m;
    public int FilterAttempts { get; init; } = 3;
    public decimal StopQuota { get; init; } = 0.95m;
    public int RefineSteps { get; init; } = 2;

    /// <summary>Quota della storia su cui si sceglie; il resto e' la fetta di conferma.</summary>
    public decimal ChoiceFraction { get; init; } = 2m / 3m;

    /// <summary>Soglia di average trade della cella, in denaro: serve a UngerFit nei cancelli.</summary>
    public decimal AverageTradeThreshold { get; init; }

    /// <summary>Estrazioni del test dei pattern casuali nei cancelli finali. Zero lo salta.</summary>
    public int RandomPatternDraws { get; init; } = 200;

    public int MaxDegreeOfParallelism { get; init; } = Math.Max(1, Environment.ProcessorCount - 1);

    /// <summary>Scrive una riga per passo: il percorso dura minuti e deve dire dove sta.</summary>
    public Action<string>? Log { get; init; }
}

/// <summary>Una decisione del percorso, come la riga di <c>percorso_v4.csv</c>.</summary>
public sealed record PathDecision(
    string Step,
    string Key,
    object? Before,
    object? After,
    bool Accepted,
    string Reason,
    int Tried,
    decimal? NetBefore = null,
    decimal? NetAfter = null);

/// <summary>Un cancello finale (v4.0 §10), con il numero che l'ha deciso.</summary>
public sealed record PathGate(string Name, bool Passed, string Detail);

/// <summary>L'esito del percorso su un motore.</summary>
public sealed record PathResult
{
    public required string Engine { get; init; }
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }
    public required IReadOnlyList<PathDecision> Decisions { get; init; }

    /// <summary>La strategia su tutta la storia di ricerca. Null se il percorso si e' fermato prima.</summary>
    public SweepOutcome? History { get; init; }

    /// <summary>Perche' il motore e' stato abbandonato. Null se il percorso e' arrivato in fondo.</summary>
    public string? Abandoned { get; init; }

    public IReadOnlyList<PathGate> Gates { get; init; } = [];
    public bool PassedAllGates => Abandoned is null && Gates.Count > 0 && Gates.All(gate => gate.Passed);
    public TimeSpan Elapsed { get; init; }
    public int Runs { get; init; }
}

/// <summary>
/// Il percorso di ricerca v4 (<c>metodo/Mat_didattico/SISTEMA_RICERCA_v4.0.md</c> §9-§10) sopra
/// <see cref="SweepRunner"/>: un passo alla volta, un parametro alla volta, ogni simulazione al minuto.
///
/// <para><b>Perche' non la sweep a fasi.</b> La sweep ordina tutto su un punteggio unico e tiene le
/// migliori: su diecimila combinazioni la migliore e' in buona parte rumore, e la sweep FDAX del
/// 21/09/2026 ha consegnato sei finaliste tutte in perdita fuori campione. Il percorso invece accende
/// un filtro solo se batte lo spento con regole esplicite — guadagna quasi quanto, migliora il rapporto
/// netto/drawdown, e un pattern batte i pattern presi a caso — e ogni passo si conferma sull'ultimo
/// terzo della storia, che le scelte non vedono: se li' peggiora, il passo si annulla. E' il percorso
/// con cui la ricerca v5.0 ha trovato le 224 strategie da cui vengono le PT5DAV.</para>
///
/// <para><b>Deviazioni dichiarate</b> rispetto a v4.0: i pattern per lato dei motori unmirrored si
/// giudicano sulle metriche della strategia intera e non del lato; i candidati NO si scelgono con la
/// stessa regola dei YES su tutta la libreria invece che fra i dieci YES peggiori; il plateau guarda i
/// soli parametri ordinali del percorso.</para>
/// </summary>
public sealed class ResearchPath
{
    private readonly SweepSeries _history;
    private readonly SweepSeries _choice;
    private readonly SweepSeries _confirm;
    private readonly PathOptions _options;
    private readonly ConcurrentDictionary<SweepSeries, ConcurrentBag<SweepRunner>> _runners = new();
    private int _runs;

    public ResearchPath(SweepSeries history, PathOptions? options = null)
    {
        _options = options ?? new PathOptions();
        _history = history;
        var split = ChoiceEnd(history, _options.ChoiceFraction);
        _choice = history.Between(history.StartUtc, split);
        _confirm = history.Between(split, history.EndUtc);
    }

    /// <summary>Dove finisce la fetta di scelta e comincia quella di conferma.</summary>
    public DateTime ConfirmFromUtc => _confirm.StartUtc;

    private static DateTime ChoiceEnd(SweepSeries series, decimal fraction) =>
        series.StartUtc + TimeSpan.FromTicks((long)((series.EndUtc - series.StartUtc).Ticks * (double)fraction));

    public PathResult Run(SweepJob template, PathDefinition definition, CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.StartNew();
        var parameters = new Dictionary<string, object>(definition.Base, StringComparer.OrdinalIgnoreCase);
        var decisions = new List<PathDecision>();

        foreach (var step in definition.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (step.OnlyIfOff is { } guard &&
                parameters.TryGetValue(guard.Key, out var guardValue) && !Same(guardValue, guard.Off))
            {
                decisions.Add(new PathDecision(step.Name, step.Key, parameters.GetValueOrDefault(step.Key), null, false,
                    $"saltato: {guard.Key} e' acceso", 0));
                continue;
            }

            var decision = Step(template, parameters, step, cancellationToken);
            decisions.Add(decision);
            Log($"  {decision.Step}: {Show(decision.Before)} -> {Show(decision.After)} " +
                $"({(decision.Accepted ? "accettato" : "resta")}; {decision.Reason}; {decision.Tried} prove)");

            if (definition.R9After is not null && string.Equals(step.Name, definition.R9After, StringComparison.Ordinal))
            {
                var r9 = Measure(_history, template, parameters);
                if (r9.Trades == 0 || r9.NetProfit <= 0m)
                {
                    var reason = $"R9: la base non guadagna su tutta la storia ({r9.Trades} trade, netto {r9.NetProfit:N0})";
                    Log($"  {reason}: motore abbandonato");
                    return new PathResult
                    {
                        Engine = definition.Engine, Parameters = parameters, Decisions = decisions,
                        History = r9, Abandoned = reason, Elapsed = started.Elapsed, Runs = _runs
                    };
                }
            }
        }

        var history = Measure(_history, template, parameters);
        var gates = Gates(template, definition, parameters, history, cancellationToken);
        return new PathResult
        {
            Engine = definition.Engine,
            Parameters = parameters,
            Decisions = decisions,
            History = history,
            Gates = gates,
            Elapsed = started.Elapsed,
            Runs = _runs
        };
    }

    // ------------------------------------------------------------------ passi

    private PathDecision Step(
        SweepJob template, Dictionary<string, object> parameters, PathStep step, CancellationToken cancellationToken)
    {
        var before = step.Values.Count > 0 && step.Values[0] is IReadOnlyDictionary<string, object> first
            ? first.Keys.ToDictionary(key => key, key => parameters.GetValueOrDefault(key) ?? "-", StringComparer.OrdinalIgnoreCase)
            : parameters.GetValueOrDefault(step.Key);
        var values = CandidateValues(step, before);
        var tried = Evaluate(_choice, template, values.Select(value => With(parameters, step, value)).ToList(), cancellationToken);
        var results = values.Zip(tried, (value, outcome) => (Value: value, Outcome: outcome)).ToList();

        var (chosen, reason) = step.Kind switch
        {
            PathStepKind.Stop => ChooseStop(results),
            PathStepKind.Filter => ChooseFilter(step, results),
            _ => ChooseRanked(step, results)
        };

        if (chosen is null || Same(chosen, before))
            return new PathDecision(step.Name, step.Key, before, before, false, reason, values.Count);

        var next = With(parameters, step, chosen);

        // Conferma sull'ultimo terzo (§9.4): tutti i passi tranne il trigger. Se dopo il passo la fetta
        // che le scelte non vedono guadagna meno di prima, il passo si annulla.
        if (step.Kind != PathStepKind.Trigger)
        {
            var confirmBefore = Measure(_confirm, template, parameters);
            var confirmAfter = Measure(_confirm, template, next);
            if (confirmAfter.NetProfit < confirmBefore.NetProfit)
            {
                return new PathDecision(step.Name, step.Key, before, chosen, false,
                    $"{reason}; annullato dall'ultimo terzo ({confirmBefore.NetProfit:N0} -> {confirmAfter.NetProfit:N0})",
                    values.Count, confirmBefore.NetProfit, confirmAfter.NetProfit);
            }
        }

        foreach (var (key, value) in next)
            parameters[key] = value;
        return new PathDecision(step.Name, step.Key, before, chosen, true, reason, values.Count);
    }

    /// <summary>La griglia del passo, o i gradini attorno al valore attuale per un affinamento.</summary>
    private List<object> CandidateValues(PathStep step, object? current)
    {
        if (!step.Refine)
            return step.Values.ToList();

        var ordered = Ordered(step.Values.Where(value => step.Off is null || !Same(value, step.Off)).ToList());
        var index = ordered.FindIndex(value => Same(value, current));
        var values = new List<object>();
        if (index < 0)
        {
            values.AddRange(ordered);
        }
        else
        {
            for (var i = Math.Max(0, index - _options.RefineSteps); i <= Math.Min(ordered.Count - 1, index + _options.RefineSteps); i++)
                values.Add(ordered[i]);
        }

        if (step.Off is not null && !values.Any(value => Same(value, step.Off)))
            values.Insert(0, step.Off);
        return values;
    }

    /// <summary>
    /// La classifica del passo (§9.5) e la scelta di trigger, finestra, scelta e durata: fra le
    /// configurazioni in utile con abbastanza trade che guadagnano almeno il 75% della migliore, vince
    /// l'average trade smussato. Nessuna in utile: il netto massimo fra quelle con abbastanza trade.
    /// </summary>
    private (object? Chosen, string Reason) ChooseRanked(PathStep step, List<(object Value, SweepOutcome Outcome)> results)
    {
        var ranking = Rank(step, results, _options.KeepFraction);
        if (ranking.Count > 0)
        {
            var best = ranking[0];
            return (best.Value, $"avg smussato {best.Score:N1}, netto {best.Outcome.NetProfit:N0}, {best.Outcome.Trades} trade");
        }

        var minimum = results.Any(result => result.Outcome.Trades >= _options.MinTrades) ? _options.MinTrades : 1;
        var fallback = results
            .Where(result => result.Outcome.Trades >= minimum)
            .OrderByDescending(result => result.Outcome.NetProfit)
            .FirstOrDefault();
        if (fallback.Outcome is null)
            return (null, "nessuna configurazione con trade");

        // Il trigger sceglie comunque (il giudizio lo da' R9); finestra e durata no: senza utile
        // restare spenti e' la scelta con meno vincoli.
        return step.Kind is PathStepKind.Trigger or PathStepKind.Choice || step.Kind == PathStepKind.Window
            ? (fallback.Value, $"nessuna in utile: netto massimo {fallback.Outcome.NetProfit:N0}")
            : (null, "nessuna in utile");
    }

    /// <summary>
    /// Regola D2 (§9.8): fra gli stop con abbastanza trade, netto smussato coi vicini; se il migliore
    /// guadagna, lo stop piu' piccolo col 95% del migliore, altrimenti quello col netto massimo.
    /// </summary>
    private (object? Chosen, string Reason) ChooseStop(List<(object Value, SweepOutcome Outcome)> results)
    {
        var ordered = results
            .Where(result => result.Outcome.Trades >= _options.MinTrades)
            .OrderBy(result => Convert.ToDecimal(result.Value, CultureInfo.InvariantCulture))
            .ToList();
        if (ordered.Count == 0)
            return (null, $"nessuno stop con {_options.MinTrades} trade");

        var smoothed = new decimal[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
        {
            var neighbours = new List<decimal> { ordered[i].Outcome.NetProfit };
            if (i > 0) neighbours.Add(ordered[i - 1].Outcome.NetProfit);
            if (i < ordered.Count - 1) neighbours.Add(ordered[i + 1].Outcome.NetProfit);
            smoothed[i] = ordered.Count >= 3 ? neighbours.Average() : ordered[i].Outcome.NetProfit;
        }

        var best = smoothed.Max();
        var index = best > 0m
            ? Array.FindIndex(smoothed, value => value >= _options.StopQuota * best)
            : Array.IndexOf(smoothed, best);
        return (ordered[index].Value,
            $"D2: netto smussato {smoothed[index]:N0} (migliore {best:N0}), {ordered[index].Outcome.Trades} trade");
    }

    /// <summary>
    /// La regola di accettazione di un filtro (§9.10): fra i primi tre valori sopra lo spento nella
    /// classifica, il primo che guadagna, non perde piu' di un quarto (un terzo per i pattern) rispetto
    /// allo spento, migliora netto/drawdown, abbassa il drawdown se e' direzionale e batte i pattern a
    /// caso se e' un pattern. Nessuno: il filtro resta spento.
    /// </summary>
    private (object? Chosen, string Reason) ChooseFilter(PathStep step, List<(object Value, SweepOutcome Outcome)> results)
    {
        var off = results.FirstOrDefault(result => Same(result.Value, step.Off));
        if (off.Outcome is null)
            return (null, "spento non misurato");

        var quota = step.Pattern ? _options.PatternKeepFraction : _options.KeepFraction;
        var ranking = Rank(step, results, quota);
        var offPosition = ranking.FindIndex(entry => Same(entry.Value, step.Off));
        var above = (offPosition < 0 ? ranking : ranking.Take(offPosition))
            .Where(entry => !Same(entry.Value, step.Off) && (step.AlwaysFalse is null || !Same(entry.Value, step.AlwaysFalse)))
            .Take(_options.FilterAttempts)
            .ToList();

        var offNet = off.Outcome.NetProfit;
        var offRatio = Ratio(off.Outcome);
        foreach (var candidate in above)
        {
            var outcome = candidate.Outcome;
            if (outcome.NetProfit <= 0m) continue;
            if (outcome.NetProfit < offNet - (1m - quota) * Math.Abs(offNet)) continue;
            if (Ratio(outcome) <= offRatio) continue;
            if (step.Directional && outcome.MaxClosedTradeDrawdown >= off.Outcome.MaxClosedTradeDrawdown) continue;

            if (step.Pattern)
            {
                var others = results
                    .Where(result => !Same(result.Value, step.Off) && !Same(result.Value, candidate.Value) &&
                                     (step.AlwaysFalse is null || !Same(result.Value, step.AlwaysFalse)))
                    .Select(result => (result.Outcome.Trades, result.Outcome.AverageTrade));
                var test = ResearchCriteria.RandomPatterns(outcome.AverageTrade, outcome.Trades, others);
                // Nel passo la soglia e' su p stesso, non sul Wilson: e' il confronto con i pattern della
                // stessa libreria gia' misurati (§9.10), non un campione estratto.
                if (test.ValidDraws > 0 && test.P > ResearchCriteria.MaxRandomPatternP) continue;
            }

            return (candidate.Value,
                $"batte lo spento: netto {outcome.NetProfit:N0} contro {offNet:N0}, net/DD {Ratio(outcome):N2} contro {offRatio:N2}");
        }

        return (step.Off, above.Count == 0 ? "nessun valore sopra lo spento" : $"nessuno dei {above.Count} candidati batte lo spento");
    }

    private sealed record Ranked(object Value, SweepOutcome Outcome, decimal Score, int GridIndex);

    /// <summary>§9.5: eleggibili, pool sulla quota del miglior netto, ordine per average trade smussato.</summary>
    private List<Ranked> Rank(PathStep step, List<(object Value, SweepOutcome Outcome)> results, decimal quota)
    {
        var eligible = results
            .Select((result, index) => (result.Value, result.Outcome, Index: index))
            .Where(entry => entry.Outcome.NetProfit > 0m && entry.Outcome.Trades >= _options.MinTrades)
            .ToList();
        if (eligible.Count == 0)
            return [];

        var bestNet = eligible.Max(entry => entry.Outcome.NetProfit);
        var smooth = step.Ordinal && results.Count >= 3;
        var ordered = smooth ? Ordered(results.Select(result => result.Value).Where(value => step.Off is null || !Same(value, step.Off)).ToList()) : [];
        var byValue = results.ToDictionary(result => Key(result.Value), result => result.Outcome);

        return eligible
            .Where(entry => entry.Outcome.NetProfit >= quota * bestNet)
            .Select(entry =>
            {
                var score = entry.Outcome.AverageTrade;
                if (smooth && (step.Off is null || !Same(entry.Value, step.Off)))
                {
                    var position = ordered.FindIndex(value => Same(value, entry.Value));
                    var sum = score;
                    var count = 1;
                    foreach (var offset in (int[])[-1, 1])
                    {
                        var neighbour = position + offset;
                        if (step.Circular) neighbour = (neighbour + ordered.Count) % ordered.Count;
                        if (neighbour < 0 || neighbour >= ordered.Count || neighbour == position) continue;
                        if (byValue.TryGetValue(Key(ordered[neighbour]), out var outcome) && outcome.Trades > 0)
                        {
                            sum += outcome.AverageTrade;
                            count++;
                        }
                    }

                    score = sum / count;
                }

                return new Ranked(entry.Value, entry.Outcome, score, entry.Index);
            })
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.GridIndex)
            .ToList();
    }

    // ------------------------------------------------------------------ cancelli finali

    /// <summary>
    /// I cancelli di v4.0 §9.13 e §10: filtri minimi, ultimo terzo, due fette su tre, outlier, pattern
    /// utile, pattern casuali, plateau. Non bocciano da soli: dicono al lettore cosa regge.
    /// </summary>
    private List<PathGate> Gates(
        SweepJob template, PathDefinition definition, Dictionary<string, object> parameters, SweepOutcome history,
        CancellationToken cancellationToken)
    {
        var gates = new List<PathGate>();
        var trades = history.ClosedTrades;

        gates.Add(new PathGate("trade", history.Trades >= _options.MinTrades, $"{history.Trades} su almeno {_options.MinTrades}"));

        if (_options.AverageTradeThreshold > 0m)
        {
            gates.Add(new PathGate("average trade", history.AverageTrade >= _options.AverageTradeThreshold,
                $"{history.AverageTrade:N0} contro la soglia {_options.AverageTradeThreshold:N0}"));
        }

        var years = ResearchCriteria.Years(trades, _history.StartUtc, _history.EndUtc);
        gates.Add(new PathGate("anni", years.Passes,
            $"{years.YearsWithTrades} anni, minimo {years.MinTradesInYear} trade in un anno, {years.TradesPerYear:N1} all'anno, {years.ProfitableYears} in utile"));

        // §9.13: i trade usciti nell'ultimo terzo del tempo devono guadagnare.
        var lastThirdNet = trades.Where(trade => trade.ExitDate >= _confirm.StartUtc).Sum(trade => trade.NetProfit);
        gates.Add(new PathGate("utile recente", lastThirdNet > 0m, $"trade usciti nell'ultimo terzo: {lastThirdNet:N0}"));

        // §10.2: la strategia ri-simulata sulla sola fetta finale.
        var confirm = Measure(_confirm, template, parameters);
        var confirmRatio = Ratio(confirm);
        gates.Add(new PathGate("ultimo terzo", confirm.NetProfit > 0m && confirmRatio >= 1.3m,
            $"netto {confirm.NetProfit:N0}, net/DD {confirmRatio:N2} (serve 1,3)"));

        // §10.8: tre fette uguali di tutta la storia, almeno due in utile.
        var span = (_history.EndUtc - _history.StartUtc) / 3;
        var slices = Enumerable.Range(0, 3)
            .Select(i => _history.Between(_history.StartUtc + span * i, i == 2 ? _history.EndUtc : _history.StartUtc + span * (i + 1)))
            .ToList();
        var sliceNets = Evaluate(slices, template, parameters).Select(outcome => outcome.NetProfit).ToList();
        gates.Add(new PathGate("due terzi su tre", sliceNets.Count(net => net > 0m) >= 2,
            string.Join(" / ", sliceNets.Select(net => net.ToString("N0", CultureInfo.InvariantCulture)))));

        var shareAll = ResearchCriteria.BestTradeShare(trades);
        var shareLast = ResearchCriteria.BestTradeShare(confirm.ClosedTrades);
        gates.Add(new PathGate("outlier",
            shareAll is { } all && all <= ResearchCriteria.MaxBestTradeShare && (shareLast is null || shareLast <= ResearchCriteria.MaxBestTradeShare),
            $"trade migliore {shareAll:P0} del netto sulla storia, {shareLast:P0} sull'ultimo terzo"));

        var patternsOn = definition.PatternSentinels
            .Where(entry => parameters.TryGetValue(entry.Key, out var value) && !Same(value, entry.Value))
            .ToList();
        if (patternsOn.Count > 0)
        {
            var bare = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, sentinel) in patternsOn)
                bare[key] = sentinel;
            var withoutPatterns = Measure(_confirm, template, bare);
            gates.Add(new PathGate("pattern utile", confirm.AverageTrade > withoutPatterns.AverageTrade,
                $"ultimo terzo: average trade {confirm.AverageTrade:N0} con i pattern, {withoutPatterns.AverageTrade:N0} senza"));

            if (_options.RandomPatternDraws > 0)
            {
                var random = new Random(0);
                var grids = definition.Steps
                    .Where(step => definition.PatternSentinels.ContainsKey(step.Key))
                    .GroupBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Values, StringComparer.OrdinalIgnoreCase);
                var draws = Enumerable.Range(0, _options.RandomPatternDraws).Select(_ =>
                {
                    var draw = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
                    foreach (var (key, grid) in grids)
                        draw[key] = grid[random.Next(grid.Count)];
                    return (IReadOnlyDictionary<string, object>)draw;
                }).ToList();
                var drawn = Evaluate(_confirm, template, draws, cancellationToken)
                    .Select(outcome => (outcome.Trades, outcome.AverageTrade));
                var test = ResearchCriteria.RandomPatterns(confirm.AverageTrade, confirm.Trades, drawn);
                gates.Add(new PathGate("pattern casuali", test.Passes,
                    $"{test.DrawsAtLeastAsGood} estrazioni su {test.ValidDraws} fanno almeno altrettanto, p {test.P:N2}, Wilson {test.WilsonLower:N2}"));
            }
        }

        gates.Add(Plateau(template, definition, parameters, history));
        return gates;
    }

    /// <summary>
    /// §10.5: UngerFit dei vicini sui parametri ordinali, tetto 1 per vicino, media ≥ 0,60. Lo spento
    /// non e' mai un vicino, e un parametro spento non si guarda.
    /// </summary>
    private PathGate Plateau(SweepJob template, PathDefinition definition, Dictionary<string, object> parameters, SweepOutcome history)
    {
        var own = ResearchCriteria.UngerFit(history.AverageTrade, Math.Max(1m, _options.AverageTradeThreshold), history.MaxClosedTradeDrawdown);
        if (own is not > 0m)
            return new PathGate("plateau", true, "UngerFit della candidata non positivo: nessun giudizio");

        var neighbours = new List<IReadOnlyDictionary<string, object>>();
        var grids = definition.Steps.Where(step => step.Ordinal && step.Kind != PathStepKind.Window)
            .Select(step => (Key: step.Key, Values: step.Values, Off: step.Off))
            .Concat(definition.PlateauGrids.Select(grid => (Key: grid.Key, Values: grid.Value, Off: (object?)null)))
            .GroupBy(grid => grid.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
        foreach (var step in grids)
        {
            if (!parameters.TryGetValue(step.Key, out var current) || (step.Off is not null && Same(current, step.Off)))
                continue;
            var ordered = Ordered(step.Values.Where(value => step.Off is null || !Same(value, step.Off)).ToList());
            if (ordered.Count < 3) continue;
            var index = ordered.FindIndex(value => Same(value, current));
            if (index < 0) continue;
            foreach (var offset in (int[])[-1, 1])
            {
                var i = index + offset;
                if (i < 0 || i >= ordered.Count) continue;
                neighbours.Add(new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase) { [step.Key] = ordered[i] });
            }
        }

        if (neighbours.Count == 0)
            return new PathGate("plateau", true, "nessun vicino valutabile");

        var ratios = Evaluate(_history, template, neighbours)
            .Select(outcome => ResearchCriteria.UngerFit(outcome.AverageTrade, Math.Max(1m, _options.AverageTradeThreshold), outcome.MaxClosedTradeDrawdown) ?? 0m)
            .Select(fit => Math.Min(1m, fit / own.Value))
            .ToList();
        var mean = ratios.Average();
        return new PathGate("plateau", mean >= 0.6m, $"media {mean:N2} su {ratios.Count} vicini, minimo {ratios.Min():N2}");
    }

    // ------------------------------------------------------------------ misura

    private SweepOutcome Measure(SweepSeries series, SweepJob template, IReadOnlyDictionary<string, object> parameters) =>
        Evaluate(series, template, [parameters])[0];

    private List<SweepOutcome> Evaluate(
        List<SweepSeries> slices, SweepJob template, IReadOnlyDictionary<string, object> parameters)
    {
        var results = new SweepOutcome[slices.Count];
        Parallel.For(0, slices.Count, new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
            index => results[index] = RunOn(slices[index], template, parameters));
        return results.ToList();
    }

    /// <summary>Le configurazioni in parallelo, i risultati nell'ordine della griglia.</summary>
    private List<SweepOutcome> Evaluate(
        SweepSeries series, SweepJob template, IReadOnlyList<IReadOnlyDictionary<string, object>> configurations,
        CancellationToken cancellationToken = default)
    {
        var results = new SweepOutcome[configurations.Count];
        Parallel.For(0, configurations.Count, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        }, index => results[index] = RunOn(series, template, configurations[index]));
        return results.ToList();
    }

    private SweepOutcome RunOn(SweepSeries series, SweepJob template, IReadOnlyDictionary<string, object> parameters)
    {
        var bag = _runners.GetOrAdd(series, _ => new ConcurrentBag<SweepRunner>());
        if (!bag.TryTake(out var runner))
            runner = new SweepRunner(series);
        try
        {
            Interlocked.Increment(ref _runs);
            return runner.Run(template with { Parameters = parameters });
        }
        finally
        {
            bag.Add(runner);
        }
    }

    // ------------------------------------------------------------------ utilita'

    private static IReadOnlyDictionary<string, object> With(IReadOnlyDictionary<string, object> parameters, PathStep step, object value)
    {
        var next = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
        if (value is IReadOnlyDictionary<string, object> levers)
        {
            foreach (var (key, lever) in levers)
                next[key] = lever;
        }
        else
        {
            next[step.Key] = value;
        }

        return step.Adjust is null ? next : step.Adjust(value, next);
    }

    private static decimal Ratio(SweepOutcome outcome) =>
        outcome.MaxClosedTradeDrawdown > 0m
            ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown
            : outcome.NetProfit > 0m ? decimal.MaxValue : 0m;

    private static List<object> Ordered(List<object> values) =>
        values.OrderBy(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture)).ToList();

    private static string Key(object value) => value is IReadOnlyDictionary<string, object> levers
        ? string.Join(", ", levers.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={Convert.ToString(entry.Value, CultureInfo.InvariantCulture)}"))
        : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private static bool Same(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        if (left is IReadOnlyDictionary<string, object> || right is IReadOnlyDictionary<string, object>)
            return Key(left) == Key(right);
        return Convert.ToDecimal(left, CultureInfo.InvariantCulture) == Convert.ToDecimal(right, CultureInfo.InvariantCulture);
    }

    private static string Show(object? value) => value is null ? "-" : Key(value);

    private void Log(string line) => _options.Log?.Invoke(line);
}
