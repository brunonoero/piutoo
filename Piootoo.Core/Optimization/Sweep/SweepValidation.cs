using System.Diagnostics;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>Come si giudica una configurazione fuori campione.</summary>
public sealed record SweepValidationOptions
{
    /// <summary>
    /// Quanta parte del punteggio in campione deve sopravvivere fuori campione. Mezzo e' severo
    /// quanto basta: una configurazione che fuori campione rende meno della meta' di quello che
    /// prometteva non e' "un po' peggio", e' stata trovata su un campione che non si ripete.
    /// </summary>
    public decimal MinScoreRetention { get; init; } = 0.5m;

    /// <summary>Trade minimi fuori campione perche' la misura voglia dire qualcosa.</summary>
    public int MinOutOfSampleTrades { get; init; } = 20;

    /// <summary>
    /// In quante finestre dividere il fuori campione per il walk-forward di <b>stabilita'</b>.
    /// Zero lo salta. Non e' la Walk-Forward Optimization vera — quella ri-ottimizza a ogni finestra
    /// ed e' un piano sopra questo: qui la configurazione resta ferma e si guarda se regge nel tempo
    /// o se tutto l'utile viene da un tratto solo.
    /// </summary>
    public int StabilityWindows { get; init; } = 4;

    /// <summary>
    /// Quante finestre devono essere in utile perche' la stabilita' sia superata. Con quattro
    /// finestre, tre: una negativa e' normale, due sono meta' del periodo.
    /// </summary>
    public int MinProfitableWindows { get; init; } = 3;
}

/// <summary>Come e' andata una finestra del walk-forward di stabilita'.</summary>
public sealed record SweepWindowResult(DateTime FromUtc, DateTime ToUtc, int Trades, decimal NetProfit);

/// <summary>
/// Il verdetto su una configurazione: cosa ha fatto in campione, cosa fuori, e se sopravvive.
/// </summary>
public sealed record SweepValidation
{
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }
    public required SweepOutcome InSample { get; init; }
    public decimal? InSampleScore { get; init; }
    public required SweepOutcome OutOfSample { get; init; }
    public decimal? OutOfSampleScore { get; init; }

    /// <summary>
    /// Quanta parte del punteggio sopravvive: <c>fuori / dentro</c>. Sopra 1 la configurazione fuori
    /// campione ha fatto meglio, e non e' necessariamente una buona notizia — spesso vuol dire che
    /// il periodo fuori campione e' stato piu' facile, non che la strategia sia robusta.
    /// </summary>
    public decimal? ScoreRetention => InSampleScore is > 0m && OutOfSampleScore.HasValue
        ? OutOfSampleScore.Value / InSampleScore.Value
        : null;

    public IReadOnlyList<SweepWindowResult> StabilityWindows { get; init; } = [];

    /// <summary>Quante finestre del walk-forward sono in utile.</summary>
    public int ProfitableWindows => StabilityWindows.Count(window => window.NetProfit > 0m);

    /// <summary>Se la configurazione ha superato tutti i criteri.</summary>
    public required bool Passed { get; init; }

    /// <summary>Perche' e' passata o dove si e' fermata, in una riga leggibile.</summary>
    public required string Verdict { get; init; }

    public override string ToString() =>
        $"{(Passed ? "PASSA" : "SCARTA")} — IS {InSample.Trades} trade {InSample.NetProfit:N0} " +
        $"(punteggio {InSampleScore:N2}) | OOS {OutOfSample.Trades} trade {OutOfSample.NetProfit:N0} " +
        $"(punteggio {OutOfSampleScore:N2}, tenuta {ScoreRetention:P0}) | {Verdict}";
}

/// <summary>
/// La validazione fuori campione, che e' cio' che separa una configurazione che ha vinto una gara da
/// una strategia.
///
/// <para><b>Perche' non basta il punteggio della ricerca.</b> L'ottimizzatore elegge il massimo di
/// una funzione su un campione: su decine di migliaia di combinazioni, il massimo e' in buona parte
/// rumore di quel campione. L'unica domanda che conta — funziona su dati che la ricerca non ha
/// visto — si risponde solo rimisurando, e va risposta <b>senza toccare piu' i parametri</b>: se si
/// prova e riprova sul fuori campione finche' uno passa, il fuori campione diventa campione.</para>
///
/// <para>Tre criteri, in ordine di severita': il fuori campione deve essere <b>ammissibile</b> per
/// l'obiettivo, deve conservare una quota del punteggio, e non deve avere tutto l'utile concentrato
/// in un tratto solo (walk-forward di stabilita').</para>
/// </summary>
public sealed class SweepValidator(
    SweepSeries inSample,
    SweepSeries outOfSample,
    ISweepObjective? objective = null,
    SweepValidationOptions? options = null,
    int? accurateClockMinutes = 1)
{
    private readonly ISweepObjective _objective = objective ?? new NetOverDrawdownObjective();
    private readonly SweepValidationOptions _options = options ?? new SweepValidationOptions();

    /// <summary>
    /// Misura una configurazione dentro e fuori campione e dice se sopravvive. <b>Sempre
    /// sull'orologio fitto</b>: una validazione sul percorso veloce confronterebbe due misure
    /// distorte nello stesso verso, e la distorsione non si annulla — cambia con lo stop, che e'
    /// proprio uno dei parametri che la ricerca ha scelto.
    /// </summary>
    public SweepValidation Validate(
        SweepJob template,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var job = template with { Parameters = parameters, ClockTimeframeMinutes = accurateClockMinutes };

        var inside = new SweepRunner(inSample).Run(job);
        var insideScore = _objective.Score(inside);

        var outsideRunner = new SweepRunner(outOfSample);
        var outside = outsideRunner.Run(job);
        var outsideScore = _objective.Score(outside);

        cancellationToken.ThrowIfCancellationRequested();

        var windows = _options.StabilityWindows > 1
            ? Split(outOfSample, job, _options.StabilityWindows, cancellationToken)
            : [];

        var validation = new SweepValidation
        {
            Parameters = parameters,
            InSample = inside,
            InSampleScore = insideScore,
            OutOfSample = outside,
            OutOfSampleScore = outsideScore,
            StabilityWindows = windows,
            Passed = false,
            Verdict = string.Empty
        };

        return validation with { Passed = Judge(validation, out var verdict), Verdict = verdict };
    }

    /// <summary>
    /// Valida in blocco le finaliste di una ricerca, nell'ordine in cui la ricerca le ha classificate.
    /// </summary>
    public IReadOnlyList<SweepValidation> Validate(
        SweepJob template,
        IEnumerable<IReadOnlyDictionary<string, object>> candidates,
        CancellationToken cancellationToken = default) =>
        candidates.Select(parameters => Validate(template, parameters, cancellationToken)).ToList();

    private bool Judge(SweepValidation validation, out string verdict)
    {
        if (!validation.OutOfSampleScore.HasValue)
        {
            verdict = validation.OutOfSample.Trades < _options.MinOutOfSampleTrades
                ? $"fuori campione solo {validation.OutOfSample.Trades} trade"
                : "fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)";
            return false;
        }

        if (validation.OutOfSample.Trades < _options.MinOutOfSampleTrades)
        {
            verdict = $"fuori campione solo {validation.OutOfSample.Trades} trade, ne servono {_options.MinOutOfSampleTrades}";
            return false;
        }

        // Il controllo e' sul NETTO e non sul punteggio: il punteggio dipende dall'obiettivo in uso
        // — quello sul peggior sotto-periodo e' negativo anche per una configurazione che nel
        // complesso guadagna — mentre "fuori campione in perdita" deve voler dire la stessa cosa
        // qualunque criterio si stia usando per cercare.
        if (validation.OutOfSample.NetProfit <= 0m)
        {
            verdict = $"fuori campione in perdita ({validation.OutOfSample.NetProfit:N0})";
            return false;
        }

        // In campione in perdita: la configurazione non e' stata scelta perche' buona, e un fuori
        // campione in utile davanti a un campione in perdita e' un caso, non una conferma.
        if (validation.InSample.NetProfit <= 0m)
        {
            verdict = $"in campione in perdita ({validation.InSample.NetProfit:N0})";
            return false;
        }

        var retention = validation.ScoreRetention;
        if (retention.HasValue && retention.Value < _options.MinScoreRetention)
        {
            verdict = $"tenuta {retention.Value:P0}, sotto il minimo {_options.MinScoreRetention:P0}";
            return false;
        }

        if (validation.StabilityWindows.Count > 1 &&
            validation.ProfitableWindows < _options.MinProfitableWindows)
        {
            verdict = $"solo {validation.ProfitableWindows} finestre su {validation.StabilityWindows.Count} in utile";
            return false;
        }

        verdict = retention.HasValue
            ? $"tenuta {retention.Value:P0}, {validation.ProfitableWindows}/{validation.StabilityWindows.Count} finestre in utile"
            : "fuori campione ammissibile";
        return true;
    }

    /// <summary>
    /// Il walk-forward di stabilita': la stessa configurazione misurata su finestre consecutive del
    /// fuori campione. Non ri-ottimizza niente — serve a vedere se l'utile e' distribuito o se viene
    /// tutto da un tratto, che e' la differenza fra una strategia e un episodio fortunato.
    /// </summary>
    private List<SweepWindowResult> Split(
        SweepSeries series, SweepJob job, int count, CancellationToken cancellationToken)
    {
        var results = new List<SweepWindowResult>(count);
        var span = (series.EndUtc - series.StartUtc) / count;

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var from = series.StartUtc + span * index;
            var to = index == count - 1 ? series.EndUtc : from + span;
            var outcome = new SweepRunner(series.Between(from, to)).Run(job);
            results.Add(new SweepWindowResult(from, to, outcome.Trades, outcome.NetProfit));
        }

        return results;
    }
}

/// <summary>
/// Ricerca in campione e validazione fuori campione in un passaggio solo, che e' il modo in cui
/// vanno usate: un risultato dell'ottimizzatore senza il suo numero fuori campione accanto e' una
/// cifra che invita a sbagliare.
/// </summary>
public static class SweepSearch
{
    /// <summary>
    /// Ottimizza sul periodo in campione e valida le finaliste su quello fuori campione.
    /// </summary>
    /// <param name="series">Le serie complete: i due periodi sono finestre sugli stessi dati.</param>
    /// <param name="inSampleEndUtc">
    /// Dove finisce il campione di ricerca e comincia quello di validazione. Tutto quello che viene
    /// dopo l'ottimizzatore non lo vede.
    /// </param>
    /// <param name="topCandidates">
    /// Quante finaliste validare. Una sola non basta: la prima classificata in campione e' spesso
    /// quella che ha sfruttato meglio il rumore, e la seconda o la terza reggono piu' spesso.
    /// </param>
    public static SweepSearchResult Run(
        SweepSeries series,
        DateTime inSampleEndUtc,
        SweepSpace space,
        SweepJob template,
        ISweepObjective? objective = null,
        SweepOptimizerOptions? optimizerOptions = null,
        SweepValidationOptions? validationOptions = null,
        int topCandidates = 5,
        CancellationToken cancellationToken = default,
        ISweepObjective? validationObjective = null)
    {
        if (inSampleEndUtc <= series.StartUtc || inSampleEndUtc >= series.EndUtc)
        {
            throw new ArgumentException(
                $"il confine fra campione e validazione ({inSampleEndUtc:u}) deve cadere dentro il periodo " +
                $"delle serie ({series.StartUtc:u} → {series.EndUtc:u}).", nameof(inSampleEndUtc));
        }

        var started = Stopwatch.StartNew();
        var inSample = series.Between(series.StartUtc, inSampleEndUtc);
        var outOfSample = series.Between(inSampleEndUtc, series.EndUtc);

        var options = optimizerOptions ?? new SweepOptimizerOptions();
        var optimization = new SweepOptimizer(inSample, space, objective, options)
            .Optimize(template, cancellationToken);

        // Le finaliste sono le prime classificate dell'ultima fase, non i soli semi del beam: con
        // quelli se ne validavano due, quasi identiche fra loro, e la seconda o la terza reggono
        // fuori campione piu' spesso della prima.
        var finalists = optimization.Phases.Count > 0
            ? optimization.Phases[^1].Top.Select(candidate => candidate.Parameters).Take(topCandidates).ToList()
            : [];

        // Il vincitore dopo l'ablation puo' non coincidere con nessun seme: va validato comunque, e
        // per primo.
        if (optimization.Best is not null &&
            !finalists.Any(parameters => Same(parameters, optimization.Best.Parameters)))
        {
            finalists.Insert(0, optimization.Best.Parameters);
        }

        // La validazione misura con un metro SEMPLICE anche quando la ricerca cerca con uno severo.
        // Sono due domande diverse: "quale configurazione preferisco fra diecimila" chiede un
        // criterio che punisca la fortuna, "questa regge fuori campione" chiede di sapere quanto ha
        // reso rispetto a quanto ha rischiato. Usare il criterio severo anche qui sommerebbe due
        // penalizzazioni — la tenuta e il peggior sotto-periodo — su configurazioni che il
        // walk-forward di stabilita' gia' giudica tratto per tratto.
        var validator = new SweepValidator(
            inSample, outOfSample,
            validationObjective ?? new NetOverDrawdownObjective(),
            validationOptions, options.AccurateClockMinutes);
        var validations = validator.Validate(template, finalists, cancellationToken);

        started.Stop();
        return new SweepSearchResult(optimization, validations, inSample.StartUtc, inSampleEndUtc,
            outOfSample.EndUtc, started.Elapsed);
    }

    private static bool Same(IReadOnlyDictionary<string, object> left, IReadOnlyDictionary<string, object> right) =>
        left.Count == right.Count &&
        left.All(entry => right.TryGetValue(entry.Key, out var value) && Equals(entry.Value, value));
}

/// <summary>Ricerca e validazione insieme.</summary>
public sealed record SweepSearchResult(
    SweepOptimizationResult Optimization,
    IReadOnlyList<SweepValidation> Validations,
    DateTime InSampleFromUtc,
    DateTime InSampleToUtc,
    DateTime OutOfSampleToUtc,
    TimeSpan Elapsed)
{
    /// <summary>
    /// La prima finalista che sopravvive al fuori campione, o <c>null</c> se non ne sopravvive
    /// nessuna — che e' un esito legittimo e va riportato come tale, non aggirato abbassando le
    /// soglie finche' qualcosa passa.
    /// </summary>
    public SweepValidation? Survivor => Validations.FirstOrDefault(validation => validation.Passed);

    /// <summary>Un resoconto leggibile, da mettere accanto ai numeri quando si decide.</summary>
    public string Describe()
    {
        var lines = new List<string>
        {
            $"ricerca {Optimization.Engine} su {Optimization.StrategyId}, obiettivo {Optimization.Objective}",
            $"campione {InSampleFromUtc:yyyy-MM-dd} → {InSampleToUtc:yyyy-MM-dd}, " +
            $"validazione {InSampleToUtc:yyyy-MM-dd} → {OutOfSampleToUtc:yyyy-MM-dd}, {Elapsed.TotalMinutes:N1} minuti"
        };

        foreach (var phase in Optimization.Phases)
            lines.Add($"  fase {phase.Phase}: {phase.Combinations:N0} combinazioni, {phase.Admissible:N0} ammissibili");

        if (Optimization.PatternsDroppedByAblation.Count > 0)
            lines.Add("  ablation: " + string.Join("; ", Optimization.PatternsDroppedByAblation));

        for (var index = 0; index < Validations.Count; index++)
            lines.Add($"  finalista {index + 1}: {Validations[index]}");

        lines.Add(Survivor is null
            ? "  nessuna finalista sopravvive al fuori campione"
            : "  parametri della sopravvissuta: " +
              string.Join(", ", Survivor.Parameters.Select(entry => $"{entry.Key}={entry.Value}")));

        return string.Join(Environment.NewLine, lines);
    }
}
