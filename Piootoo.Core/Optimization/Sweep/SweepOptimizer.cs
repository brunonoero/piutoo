using System.Collections.Concurrent;
using System.Diagnostics;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>Una configurazione misurata.</summary>
/// <param name="Parameters">I parametri completi, non solo quelli della fase.</param>
/// <param name="Outcome">Cosa ha prodotto.</param>
/// <param name="Score">Il punteggio della funzione obiettivo. <c>null</c> = non ammissibile.</param>
/// <param name="SmoothedScore">
/// Il punteggio dopo la plateau analysis: la media con i vicini di griglia. E' su questo che si
/// ordina — un picco isolato fra vicini scarsi e' overfitting, non una scoperta.
/// </param>
public sealed record SweepCandidate(
    IReadOnlyDictionary<string, object> Parameters,
    SweepOutcome Outcome,
    decimal? Score,
    decimal? SmoothedScore);

/// <summary>Cosa e' successo in una fase.</summary>
/// <param name="Seeds">Le configurazioni che passano alla fase successiva: il beam.</param>
/// <param name="Top">
/// Le prime classificate, quante ne chiede <see cref="SweepOptimizerOptions.KeepTopPerPhase"/>.
/// Sono di piu' dei semi perche' servono a un'altra cosa: la validazione va fatta su piu' di una
/// finalista, e la prima classificata in campione e' spesso proprio quella che ha sfruttato meglio
/// il rumore. Con i soli semi si validavano due configurazioni quasi identiche fra loro.
/// </param>
public sealed record SweepPhaseReport(
    string Phase,
    long Combinations,
    int Evaluated,
    int Admissible,
    IReadOnlyList<SweepCandidate> Seeds,
    IReadOnlyList<SweepCandidate> Top,
    TimeSpan Elapsed);

/// <summary>L'esito di una ricerca.</summary>
public sealed record SweepOptimizationResult(
    string Engine,
    string StrategyId,
    string Objective,
    IReadOnlyList<SweepPhaseReport> Phases,
    SweepCandidate? Best,
    IReadOnlyList<string> PatternsDroppedByAblation,
    TimeSpan Elapsed)
{
    /// <summary>I parametri vincenti, pronti per diventare una classe.</summary>
    public IReadOnlyDictionary<string, object> BestParameters =>
        Best?.Parameters ?? new Dictionary<string, object>();
}

/// <summary>Come si comporta la ricerca.</summary>
public sealed record SweepOptimizerOptions
{
    /// <summary>
    /// Quante configurazioni passano da una fase alla successiva. Con 1 la sweep sequenziale e'
    /// cieca alle interazioni fra fasi — sceglie il canale prima di sapere che orari avra' — e con
    /// K ≥ 2 ne recupera una parte al costo di K volte i run.
    /// </summary>
    public int BeamWidth { get; init; } = 2;

    /// <summary>
    /// Smussa il punteggio coi vicini di griglia prima di scegliere. Si applica ai soli parametri
    /// ordinali: su un id di pattern il vicinato non significa niente.
    /// </summary>
    public bool PlateauSmoothing { get; init; } = true;

    /// <summary>
    /// Dopo l'ultima fase, ogni pattern scelto deve battere la propria sentinella a parita' di tutto
    /// il resto: se non la batte, si torna alla sentinella. E' la regola del metodo — un filtro si
    /// tiene solo se i numeri lo sostengono — e senza, la sweep porta a casa pattern che non fanno
    /// nulla e che il porting poi copia.
    /// </summary>
    public bool PatternAblation { get; init; } = true;

    /// <summary>Quanti run in parallelo. Il default usa tutti i core.</summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Guardia sul numero di combinazioni di una singola fase: oltre, la ricerca <b>fallisce</b>
    /// invece di partire per giorni. Va alzata consapevolmente, sapendo quanto costa un run.
    /// </summary>
    public long MaxCombinationsPerPhase { get; init; } = 50_000;

    /// <summary>
    /// L'orologio con cui gira <b>ogni</b> fase: il feed da un minuto, che va caricato nelle serie.
    /// Costa tredici volte un run veloce ed e' l'unico che vede dentro la barra.
    /// </summary>
    public int AccurateClockMinutes { get; init; } = 1;

    /// <summary>
    /// Fa girare sull'orologio veloce le fasi che non dichiarano
    /// <see cref="SweepPhase.RequiresAccurateClock"/>, come si faceva fino al 21/09/2026.
    /// <b>Default spento</b>, e va acceso solo per rimisurare quella scelta.
    ///
    /// <para><b>Perche' e' stato spento.</b> L'architettura a due orologi si reggeva su un
    /// argomento mai verificato: che il percorso veloce sbagliasse i <i>valori</i> ma conservasse
    /// l'<i>ordinamento</i>, perche' dentro una fase lo stop e' fisso e tutte le combinazioni
    /// sbagliano nello stesso verso. <c>SweepFastClockRankingTests</c> lo ha misurato su 22
    /// configurazioni vere di <c>@FDAX 240m</c>, stop fisso a 1500: la correlazione di rango di
    /// Spearman fra i due orologi vale <b>0,021 sul punteggio dell'obiettivo</b> — cioe' zero — e
    /// delle prime tre del veloce solo una sta fra le prime tre del minuto.</para>
    ///
    /// <para><b>Perche' proprio il punteggio.</b> Sul netto la correlazione e' 0,694: debole ma
    /// viva. Il punteggio e' pero' netto <i>diviso</i> il drawdown, e il drawdown e' esattamente
    /// cio' che l'orologio veloce non puo' vedere — non vede i trade stoppati, e non li vede in
    /// misura uguale fra configurazioni, perche' dipende da quante barre volatili ciascuna
    /// attraversa. Numeratore gonfiato di un fattore, denominatore di un altro, e il rapporto
    /// diventa rumore. Poiche' e' il punteggio che ordina e sceglie, le fasi veloci sceglievano a
    /// caso: la sweep FDAX del 21/09 ha consegnato sei finaliste tutte in perdita fuori campione,
    /// battute dalla configurazione di partenza con un solo parametro cambiato a mano.</para>
    ///
    /// <para><b>Cosa costa spegnerlo.</b> Le fasi di ordinamento passano da ~0,3 s a ~0,35 s per
    /// run in parallelo: su @FDAX 240m una ricerca completa va da 37 minuti a circa tre ore. E' il
    /// prezzo per ordinare qualcosa invece che niente.</para>
    /// </summary>
    public bool UseFastClockForOrderingPhases { get; init; }

    /// <summary>
    /// Quante configurazioni conservare per fase nel resoconto, oltre al beam. Non costa run in
    /// piu': sono gia' state valutate tutte.
    /// </summary>
    public int KeepTopPerPhase { get; init; } = 10;

    /// <summary>Una riga per fase sulla console. Spegnerlo serve ai test.</summary>
    public bool Verbose { get; init; } = true;
}

/// <summary>
/// La ricerca sequenziale a fasi del metodo Unger, sopra <see cref="SweepRunner"/>.
///
/// <para><b>Come procede.</b> Si parte dai default dello spazio. Ogni fase ottimizza il proprio
/// gruppo di parametri tenendo fermi tutti gli altri al seme che arriva dalla fase precedente, e
/// consegna alla successiva le migliori <see cref="SweepOptimizerOptions.BeamWidth"/>. L'ordine
/// delle fasi e' il metodo: trigger, filtri, orari, e il risk management per ultimo — mai pattern e
/// stop/target insieme.</para>
///
/// <para><b>Cosa non fa.</b> Non valida: IS/OOS e walk-forward sono un piano sopra questo, e vanno
/// fatti rimisurando le finaliste su un periodo che la ricerca non ha visto. Un risultato di questa
/// classe e' una configurazione che ha vinto <i>sul campione</i>, che non e' ancora una strategia.</para>
/// </summary>
public sealed class SweepOptimizer(
    SweepSeries series,
    SweepSpace space,
    ISweepObjective? objective = null,
    SweepOptimizerOptions? options = null)
{
    private readonly ISweepObjective _objective = objective ?? new NetOverDrawdownObjective();
    private readonly SweepOptimizerOptions _options = options ?? new SweepOptimizerOptions();

    public SweepOptimizationResult Optimize(SweepJob template, CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.StartNew();
        var reports = new List<SweepPhaseReport>(space.Phases.Count);
        var seeds = new List<IReadOnlyDictionary<string, object>> { space.Defaults };
        SweepCandidate? best = null;

        foreach (var phase in space.Phases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var combinations = space.CombinationCount(phase);
            if (combinations > _options.MaxCombinationsPerPhase)
            {
                throw new InvalidOperationException(
                    $"{space.Engine}/{phase.Name}: {combinations:N0} combinazioni, oltre il limite di " +
                    $"{_options.MaxCombinationsPerPhase:N0}. Alzalo sapendo quanto costa un run, o " +
                    "restringi la griglia: non e' un caso in cui proseguire in silenzio.");
            }

            var phaseStarted = Stopwatch.StartNew();
            var evaluated = new List<SweepCandidate>((int)Math.Min(combinations * seeds.Count, 200_000));

            // TUTTE le fasi girano sull'orologio fitto dal 21/09/2026. Prima lo facevano solo
            // quelle che decidono lo stop, sull'argomento che il veloce bastasse a ORDINARE anche
            // se sbagliava i valori: misurato, quell'argomento e' falso — Spearman 0,021 sul
            // punteggio. Vedi SweepOptimizerOptions.UseFastClockForOrderingPhases, che riaccende il
            // comportamento vecchio per rimisurarlo.
            var phaseTemplate = phase.RequiresAccurateClock || !_options.UseFastClockForOrderingPhases
                ? template with { ClockTimeframeMinutes = _options.AccurateClockMinutes }
                : template;

            foreach (var seed in seeds)
            {
                var candidates = Evaluate(phaseTemplate, seed, phase, cancellationToken);
                evaluated.AddRange(_options.PlateauSmoothing ? Smooth(candidates, phase) : candidates);
            }

            phaseStarted.Stop();

            var ranked = evaluated
                .Where(candidate => candidate.SmoothedScore.HasValue)
                .OrderByDescending(candidate => candidate.SmoothedScore!.Value)
                .ToList();

            var distinct = Distinct(ranked, phase).ToList();
            var next = distinct.Take(Math.Max(1, _options.BeamWidth)).ToList();
            var top = distinct.Take(Math.Max(_options.BeamWidth, _options.KeepTopPerPhase)).ToList();

            // Una fase senza nemmeno una configurazione ammissibile non fa ripartire da capo: i semi
            // restano quelli, cosi' la ricerca prosegue con la fase dopo invece di fermarsi. Succede
            // sul serio — una fase di pattern che non trova niente sopra la soglia di trade — e il
            // resoconto lo dichiara con Admissible = 0.
            if (next.Count > 0)
            {
                seeds = next.Select(candidate => candidate.Parameters).ToList();
                best = next[0];
            }

            reports.Add(new SweepPhaseReport(
                phase.Name, combinations, evaluated.Count, ranked.Count, next, top, phaseStarted.Elapsed));

            if (_options.Verbose)
            {
                // Quando una fase non produce nulla, il conteggio dei trade dice subito se il
                // problema e' la soglia o se la strategia non sta operando affatto. Sono due cause
                // opposte e senza questo numero si distinguono solo rilanciando.
                var diagnosi = " → nessuna ammissibile, semi invariati";
                if (next.Count > 0)
                {
                    diagnosi = $" → {next[0].Outcome}";
                }
                else if (evaluated.Count > 0)
                {
                    var maxTrade = evaluated.Max(candidate => candidate.Outcome.Trades);
                    diagnosi += maxTrade == 0
                        ? " — NESSUNA combinazione ha prodotto un solo trade: non e' la soglia, la strategia non opera (orario o giorno che il feed non ha?)"
                        : $" — il massimo osservato e' {maxTrade} trade";
                }

                Console.WriteLine(
                    $"[sweep] {space.Engine}/{phase.Name}: {combinations:N0} combinazioni × {Math.Max(1, seeds.Count)} semi, " +
                    $"{ranked.Count:N0} ammissibili, {phaseStarted.Elapsed.TotalSeconds:N1}s{diagnosi}");
            }
        }

        var dropped = new List<string>();
        if (best is not null && _options.PatternAblation)
            best = Ablate(template, best, dropped, cancellationToken);

        started.Stop();
        return new SweepOptimizationResult(
            space.Engine, template.StrategyId, _objective.Describe(), reports, best, dropped, started.Elapsed);
    }

    /// <summary>
    /// Valuta tutte le combinazioni della fase sopra un seme. I run sono indipendenti e girano in
    /// parallelo, ma i risultati restano nell'ordine della griglia: una ricerca che cambia esito
    /// secondo come il sistema operativo ha schedulato i thread non e' riproducibile.
    /// </summary>
    private List<SweepCandidate> Evaluate(
        SweepJob template,
        IReadOnlyDictionary<string, object> seed,
        SweepPhase phase,
        CancellationToken cancellationToken)
    {
        var grid = Combinations(phase).ToArray();
        var results = new SweepCandidate?[grid.Length];
        var runners = new ConcurrentBag<SweepRunner>();

        Parallel.For(0, grid.Length, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, _options.MaxDegreeOfParallelism),
            CancellationToken = cancellationToken
        }, index =>
        {
            if (!runners.TryTake(out var runner))
                runner = new SweepRunner(series);

            try
            {
                var parameters = new Dictionary<string, object>(seed, StringComparer.OrdinalIgnoreCase);
                foreach (var (key, value) in grid[index])
                    parameters[key] = value;

                var outcome = runner.Run(template with { Parameters = parameters });
                var score = _objective.Score(outcome);
                results[index] = new SweepCandidate(parameters, outcome, score, score);
            }
            finally
            {
                runners.Add(runner);
            }
        });

        return results.Where(candidate => candidate is not null).Select(candidate => candidate!).ToList();
    }

    /// <summary>Il prodotto cartesiano della griglia di una fase.</summary>
    private IEnumerable<Dictionary<string, object>> Combinations(SweepPhase phase)
    {
        IEnumerable<Dictionary<string, object>> seed =
            [new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)];

        foreach (var key in phase.Keys)
        {
            var parameter = space.ByKey[key];
            seed = seed.SelectMany(partial => parameter.Values.Select(value =>
            {
                var next = new Dictionary<string, object>(partial, StringComparer.OrdinalIgnoreCase)
                {
                    [key] = value
                };
                return next;
            }));
        }

        return seed;
    }

    /// <summary>
    /// Plateau analysis: il punteggio di una configurazione diventa la media fra il proprio e quello
    /// dei vicini di griglia, cioe' le configurazioni che differiscono per <b>un solo</b> parametro
    /// <b>ordinale</b> di <b>un solo</b> passo.
    ///
    /// <para>Serve a non eleggere un picco isolato. Un massimo circondato da configurazioni scarse
    /// e' quasi sempre rumore del campione: alla barra dopo non c'e' piu'. Un massimo su un altopiano
    /// regge anche se il mercato si sposta un po'.</para>
    ///
    /// <para>Due confini non si attraversano mai: i parametri <b>categorici</b> (id di pattern,
    /// direzione, giorni) non hanno vicini, e il salto fra la sentinella "spento" e il primo valore
    /// attivo non e' un vicinato — sono due strategie diverse, non due tarature vicine.</para>
    ///
    /// <para>Una configurazione <b>non ammissibile</b> resta tale: lo smoothing puo' abbassare il
    /// punteggio di un picco, non promuovere qualcosa che la soglia dei trade ha gia' escluso.</para>
    /// </summary>
    private List<SweepCandidate> Smooth(List<SweepCandidate> candidates, SweepPhase phase)
    {
        var ordinalKeys = phase.Keys
            .Where(key => !space.ByKey[key].Categorical)
            .ToArray();

        if (ordinalKeys.Length == 0 || candidates.Count == 0)
            return candidates;

        var bySignature = candidates.ToDictionary(
            candidate => Signature(candidate.Parameters, phase),
            candidate => candidate,
            StringComparer.Ordinal);

        var smoothed = new List<SweepCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (!candidate.Score.HasValue)
            {
                smoothed.Add(candidate);
                continue;
            }

            var sum = candidate.Score.Value;
            var count = 1;

            foreach (var key in ordinalKeys)
            {
                var parameter = space.ByKey[key];
                var values = parameter.OrderedValues;
                var current = candidate.Parameters[key];
                var position = IndexOf(values, current);
                if (position < 0) continue;

                foreach (var step in (int[])[-1, 1])
                {
                    var neighbour = position + step;
                    if (neighbour < 0 || neighbour >= values.Count) continue;

                    // Il confine con lo "spento" non e' un vicinato, in nessuna delle due direzioni.
                    if (IsOff(parameter, values[neighbour]) || IsOff(parameter, current))
                        continue;

                    var probe = new Dictionary<string, object>(candidate.Parameters, StringComparer.OrdinalIgnoreCase)
                    {
                        [key] = values[neighbour]
                    };

                    if (bySignature.TryGetValue(Signature(probe, phase), out var found) && found.Score.HasValue)
                    {
                        sum += found.Score.Value;
                        count++;
                    }
                }
            }

            smoothed.Add(candidate with { SmoothedScore = sum / count });
        }

        return smoothed;
    }

    /// <summary>
    /// L'ablation del metodo: ogni pattern scelto deve battere la propria sentinella a parita' di
    /// tutto il resto, altrimenti si torna alla sentinella. Un filtro che non migliora i numeri e'
    /// un vincolo in piu' sulla strategia e un'occasione in meno di funzionare fuori campione.
    /// </summary>
    private SweepCandidate Ablate(
        SweepJob template,
        SweepCandidate best,
        List<string> dropped,
        CancellationToken cancellationToken)
    {
        var runner = new SweepRunner(series);

        // L'ablation gira sull'orologio fitto e ci rimisura anche il candidato di partenza: il
        // punteggio che arriva dalle fasi e' quello dell'orologio con cui quella fase e' girata, e
        // confrontarlo con uno misurato diversamente farebbe cadere o sopravvivere i pattern per il
        // motivo sbagliato.
        var accurate = template with { ClockTimeframeMinutes = _options.AccurateClockMinutes };
        var baseline = runner.Run(accurate with { Parameters = best.Parameters });
        var current = best with { Outcome = baseline, Score = _objective.Score(baseline) };

        foreach (var (key, sentinel) in space.PatternSentinels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!current.Parameters.TryGetValue(key, out var chosen) || Equals(chosen, sentinel))
                continue;

            var parameters = new Dictionary<string, object>(current.Parameters, StringComparer.OrdinalIgnoreCase)
            {
                [key] = sentinel
            };

            var outcome = runner.Run(accurate with { Parameters = parameters });
            var score = _objective.Score(outcome);

            // "Non peggiora" basta a far cadere il pattern: a parita' di numeri si tiene la
            // strategia con meno vincoli, che e' il principio.
            if (score.HasValue && current.Score.HasValue && score.Value >= current.Score.Value)
            {
                dropped.Add($"{key}: {chosen} → {sentinel}");
                current = new SweepCandidate(parameters, outcome, score, score);
            }
        }

        if (_options.Verbose && dropped.Count > 0)
            Console.WriteLine($"[sweep] ablation: {dropped.Count} pattern tornati alla sentinella — {string.Join("; ", dropped)}");

        return current;
    }

    /// <summary>
    /// Toglie i doppioni dal beam: due configurazioni che differiscono solo per parametri che questa
    /// fase non ha toccato sono lo stesso punto di partenza, e riempirebbero il beam di copie.
    /// </summary>
    private IEnumerable<SweepCandidate> Distinct(IEnumerable<SweepCandidate> ranked, SweepPhase phase)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in ranked)
        {
            if (seen.Add(Signature(candidate.Parameters, phase)))
                yield return candidate;
        }
    }

    private static string Signature(IReadOnlyDictionary<string, object> parameters, SweepPhase phase) =>
        string.Join('|', phase.Keys.Select(key =>
            parameters.TryGetValue(key, out var value) ? $"{key}={value}" : $"{key}=?"));

    private static int IndexOf(IReadOnlyList<object> values, object value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (Equals(values[index], value))
                return index;
        }

        return -1;
    }

    private static bool IsOff(SweepParameter parameter, object value) =>
        parameter.OffSentinel is not null && Equals(parameter.OffSentinel, value);
}
