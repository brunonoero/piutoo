using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Workspaces;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per l'esecuzione del backtesting
/// </summary>
public class PiootooBacktestingService : IPiootooBacktestingService
{
    private readonly ConcurrentDictionary<string, BacktestingJob> _jobs = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellations = new();
    private readonly ConcurrentDictionary<string, string> _activeOutputPaths =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Ogni quante barre i file incrementali (signals/trades) vengono riscritti durante il run.
    /// Scriverli a ogni barra costava un fsync per barra ed era la voce di costo dominante
    /// dell'intero backtest; il checkpoint serve solo a rendere ispezionabile un run lungo mentre
    /// è in corso, la scrittura autorevole è quella finale.
    /// </summary>
    private const int PersistCheckpointBars = 5_000;

    /// <summary>
    /// Istanza unica per le strategie non multi-timeframe: era un dizionario vuoto allocato per
    /// strategia per barra, cioe' centinaia di migliaia di oggetti buttati via subito.
    /// </summary>
    private static readonly Dictionary<int, OhlcvData[]> NoAdditionalTimeframes = new();

    private readonly IPiootooSettingsService _settingsService;
    private readonly IPiootooDataFeedService _dataFeedService;
    private readonly IDatafeedCatalog _datafeedCatalog;
    private readonly IBacktestingExecutionHook _executionHook;

    /// <summary>
    /// Serve solo a risolvere il broker del piano di <see cref="BacktestingRequest.PlanCode"/> e la
    /// sua tabella di conversione. Opzionale: un run senza piano non deve dipendere dall'anagrafica.
    /// </summary>
    private readonly WorkspaceService? _workspaces;

    /// <summary>
    /// Il registro dei piani, per la stessa ragione di <see cref="_workspaces"/>: senza
    /// <see cref="BacktestingRequest.PlanCode"/> non viene mai toccato.
    /// </summary>
    private readonly TradingPlanService? _plans;

    private readonly PiootooSettings _settings;
    private readonly string _resultsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PiootooBacktestingService(
        IPiootooSettingsService settingsService,
        IPiootooDataFeedService dataFeedService,
        IDatafeedCatalog datafeedCatalog,
        PiootooSettings settings,
        IBacktestingExecutionHook executionHook,
        WorkspaceService? workspaces = null,
        TradingPlanService? plans = null)
    {
        _settingsService = settingsService;
        _dataFeedService = dataFeedService;
        _datafeedCatalog = datafeedCatalog;
        _executionHook = executionHook;
        _workspaces = workspaces;
        _plans = plans;
        _settings = settings;

        _resultsPath = Path.Combine(settings.GetSettingsPath(), "results");
        if (!Directory.Exists(_resultsPath))
        {
            Directory.CreateDirectory(_resultsPath);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public string StartBacktesting(BacktestingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
        {
            throw new ArgumentException("WorkspaceId è obbligatorio.", nameof(request));
        }

        if (request.SelectedStrategyIds == null || request.SelectedStrategyIds.Count == 0)
        {
            throw new ArgumentException(
                "Il workspace non contiene strategie abilitate nel masterfilter.",
                nameof(request));
        }

        // Il broker si verifica prima di creare la cartella: un archivio inesistente e' lo stesso
        // errore del datafeed mancante, e va detto adesso invece che dopo aver azzerato l'output.
        // ResolveRoot alza ArgumentException/DirectoryNotFoundException, che il controller traduce.
        request.DatafeedBroker = NormalizeBroker(request.DatafeedBroker);
        _datafeedCatalog.ResolveRoot(request.DatafeedBroker);

        // Il piano si risolve qui per la stessa ragione del broker del datafeed: un piano che non
        // esiste e' lo stesso errore dell'archivio mancante, e va detto prima di creare la cartella
        // invece che a run avviato. Quello che porta entra SUBITO nella richiesta, cosi' il log di
        // avvio e il summary dichiarano i valori che hanno davvero governato l'esecuzione e non
        // quelli che il client aveva proposto.
        var plan = ResolvePlan(request);
        if (plan is not null)
        {
            request.Holding = plan.Holding;
            request.CommissionPerContract = plan.CommissionPerContract;
        }

        request.BacktestFolderName = WorkspaceBacktestPaths.NormalizeFolderName(request.BacktestFolderName);
        var workspacePath = ResolveWorkspacePath(request.WorkspaceId);
        var outputPath = WorkspaceBacktestPaths.ResolveBacktestPath(workspacePath, request.BacktestFolderName);
        var jobId = Guid.NewGuid().ToString();
        if (!_activeOutputPaths.TryAdd(outputPath, jobId))
            throw new InvalidOperationException(
                $"Il backtest '{request.BacktestFolderName}' è già in esecuzione.");

        try
        {
        if (Directory.Exists(outputPath))
        {
            if (!request.OverwriteExistingBacktest)
                throw new InvalidOperationException(
                    $"Il backtest '{request.BacktestFolderName}' esiste già nel workspace. Conferma esplicitamente la sostituzione.");
            Directory.Delete(outputPath, recursive: true);
        }
        Directory.CreateDirectory(outputPath);

        // Dichiarato alla creazione: la cartella convive con quelle prodotte dalle sessioni
        // dell'engine esterno, e dedurre l'origine dai file presenti sbaglierebbe sui run
        // interrotti prima di scrivere il summary. La serie di prezzi sta qui e non solo nel
        // summary per la stessa ragione: due run interni su feed diversi sono indistinguibili
        // guardando i trade, e il summary manca appena il run si interrompe.
        WorkspaceService.WriteBacktestOrigin(outputPath, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.Internal,
            CreatedUtc = DateTime.UtcNow,
            PriceSource = RunPriceSource.FromDatafeedBroker(request.DatafeedBroker),
            EngineVersion = PiootooVersion.Current
        });

        var job = new BacktestingJob
        {
            JobId = jobId,
            Status = BacktestingJobStatus.Pending,
            Phase = "Pending",
            ProgressMessage = "In attesa di avvio"
        };
        var cancellation = new CancellationTokenSource();

        _jobs[job.JobId] = job;
        _jobCancellations[job.JobId] = cancellation;

        // Avvia il backtesting in background
        _ = Task.Run(() => ExecuteBacktesting(job, request, plan, outputPath, cancellation.Token));

        return job.JobId;
        }
        catch
        {
            _activeOutputPaths.TryRemove(outputPath, out _);
            throw;
        }
    }

    /// <summary>
    /// Broker in forma canonica: stringa vuota e spazi diventano null, cioe' "datafeed interno".
    /// Serve perche' il client manda una stringa vuota quando non seleziona nulla, e un ""
    /// finirebbe nel summary come se fosse un broker senza nome.
    /// </summary>
    private static string? NormalizeBroker(string? broker)
        => string.IsNullOrWhiteSpace(broker) ? null : broker.Trim();

    /// <summary>
    /// Il piano dichiarato dal run, gia' normalizzato sulla richiesta; null quando non ce n'e' uno,
    /// che e' il run neutro sull'intero masterfilter.
    ///
    /// <para><b>Un piano che non esiste fa fallire l'avvio.</b> Vale la stessa regola del datafeed
    /// mancante e del broker inesistente: ripiegare sul masterfilter intero darebbe un run
    /// plausibile e sbagliato — piu' strategie di quante il piano ne opererebbe, e per giunta con
    /// la tenuta e la commissione della richiesta invece che le sue.</para>
    /// </summary>
    private TradingPlan? ResolvePlan(BacktestingRequest request)
    {
        request.PlanCode = string.IsNullOrWhiteSpace(request.PlanCode) ? null : request.PlanCode.Trim();
        if (request.PlanCode is null) return null;

        var plans = _plans ?? throw new InvalidOperationException(
            "Il run dichiara un piano ma il servizio di backtesting non ha il registro dei piani: " +
            "non puo' risolverne broker, tenuta e strategie spente.");

        try
        {
            return plans.Get(request.WorkspaceId, request.PlanCode);
        }
        catch (KeyNotFoundException ex)
        {
            // Tradotta perche' il controller sa gia' rendere InvalidOperationException all'utente:
            // una KeyNotFoundException finirebbe nel 500 generico, che di un piano sbagliato non
            // dice niente.
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public BacktestingJob? GetJobStatus(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return null;

        lock (job)
        {
            return new BacktestingJob
            {
                JobId = job.JobId,
                Status = job.Status,
                ProgressPercent = job.ProgressPercent,
                Phase = job.Phase,
                ProgressMessage = job.ProgressMessage,
                CancellationRequested = job.CancellationRequested,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                // Lo status deve restare leggero: le serie complete sono disponibili
                // esclusivamente tramite l'endpoint result/output.
                Result = null,
                ErrorMessage = job.ErrorMessage
            };
        }
    }

    public BacktestingJob? CancelBacktesting(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return null;

        _jobCancellations.TryGetValue(jobId, out var cancellation);
        lock (job)
        {
            if (job.Status is BacktestingJobStatus.Completed
                or BacktestingJobStatus.Failed
                or BacktestingJobStatus.Cancelled)
                return GetJobStatus(jobId);

            job.CancellationRequested = true;
            job.ProgressMessage = "Interruzione in corso…";
            if (cancellation != null)
            {
                try { cancellation.Cancel(); }
                catch (ObjectDisposedException) { }
            }
        }

        return GetJobStatus(jobId);
    }

    public BacktestingResult? GetResult(string jobId)
    {
        // Prova prima a ottenere dal job attivo
        var job = GetJobStatus(jobId);
        if (job?.Result != null)
        {
            Console.WriteLine($"Risultato trovato nel job attivo per JobId: {jobId}");
            return job.Result;
        }
        if (job != null && job.Status != BacktestingJobStatus.Completed)
        {
            return null;
        }

        // Se non trovato nel job, cerca nei file salvati
        Console.WriteLine($"Cercando risultato nei file salvati per JobId: {jobId}");
        var resultFromFileId = GetResultByFileId(jobId);
        if (resultFromFileId != null)
        {
            Console.WriteLine($"Risultato trovato tramite nome file per id: {jobId}");
            return resultFromFileId;
        }

        var completedBacktestings = GetCompletedBacktestings();
        Console.WriteLine($"Trovati {completedBacktestings.Count} backtesting completati");
        
        // Log dei primi 10 risultati per debug
        foreach (var result in completedBacktestings.Take(10))
        {
            Console.WriteLine($"Backtesting trovato - JobId: '{result.JobId}' (lunghezza: {result.JobId?.Length ?? 0}), SetupName: '{result.SetupName}', StartDate: {result.StartDate}");
        }
        
        // Cerca per JobId esatto (case-sensitive)
        var found = completedBacktestings.FirstOrDefault(r => 
            !string.IsNullOrEmpty(r.JobId) && 
            string.Equals(r.JobId, jobId, StringComparison.OrdinalIgnoreCase));
            
        if (found != null)
        {
            Console.WriteLine($"Risultato trovato nei file per JobId: {jobId}");
            // Popola StrategiesInfo se non presente (per retrocompatibilità)
            if ((found.StrategiesInfo == null || !found.StrategiesInfo.Any()) && found.StrategiesUsed.Any())
            {
                found.StrategiesInfo = PopulateStrategiesInfo(found.StrategiesUsed);
            }
            return found;
        }
        
        Console.WriteLine($"Risultato NON trovato nei file per JobId: {jobId}");
        return null;
    }

    public List<BacktestingResult> GetCompletedBacktestings()
    {
        var results = new List<BacktestingResult>();

        var files = EnumerateResultFiles().ToArray();
        Console.WriteLine($"Trovati {files.Length} file di backtesting");
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var result = JsonSerializer.Deserialize<BacktestingResult>(json, _jsonOptions);
                if (result != null)
                {
                    result.ResultFilePath = file;
                    // Popola StrategiesInfo se non presente (per retrocompatibilità)
                    if ((result.StrategiesInfo == null || !result.StrategiesInfo.Any()) && result.StrategiesUsed.Any())
                    {
                        result.StrategiesInfo = PopulateStrategiesInfo(result.StrategiesUsed);
                    }
                    // Se il JobId è vuoto, prova a estrarlo dal nome del file o usa un valore di default
                    if (string.IsNullOrEmpty(result.JobId))
                    {
                        Console.WriteLine($"Attenzione: JobId vuoto nel file {file}, SetupName: {result.SetupName}");
                    }
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la deserializzazione del file {file}: {ex.Message}");
                // Ignora file corrotti
            }
        }

        // Ordina per CreatedAt (data di creazione del risultato) discendente
        // Fallback a StartDate per vecchi risultati senza CreatedAt
        return results.OrderByDescending(r => r.CreatedAt != default ? r.CreatedAt : r.StartDate).ToList();
    }

    public List<BacktestingResult> GetCompletedBacktestingSummaries()
    {
        return EnumerateResultFiles()
            .Select(CreateBacktestingSummaryFromFile)
            .OrderByDescending(result => result.CreatedAt)
            .ToList();
    }

    private BacktestingResult CreateBacktestingSummaryFromFile(string file)
    {
        var fileId = Path.GetFileNameWithoutExtension(file);
        var (setupName, createdAt) = ParseBacktestFileId(fileId);
        var htmlPath = Path.ChangeExtension(file, ".html");
        var tradeSignalsPath = Path.Combine(
            Path.GetDirectoryName(file) ?? _resultsPath,
            $"{Path.GetFileNameWithoutExtension(file)}_signals.json");

        return new BacktestingResult
        {
            JobId = fileId,
            SetupName = setupName,
            CreatedAt = createdAt == default ? File.GetLastWriteTimeUtc(file) : createdAt,
            ResultFilePath = file,
            HtmlReportFilePath = File.Exists(htmlPath) ? htmlPath : null,
            TradeSignalsFilePath = File.Exists(tradeSignalsPath) ? tradeSignalsPath : null
        };
    }

    private BacktestingResult? GetResultByFileId(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        var normalizedFileId = Path.GetFileNameWithoutExtension(fileId.Trim());
        var file = EnumerateResultFiles()
            .FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate)
                .Equals(normalizedFileId, StringComparison.OrdinalIgnoreCase));

        if (file == null)
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(file);
            var result = JsonSerializer.Deserialize<BacktestingResult>(json, _jsonOptions);
            if (result == null)
            {
                return null;
            }

            result.ResultFilePath = file;
            if ((result.StrategiesInfo == null || !result.StrategiesInfo.Any()) && result.StrategiesUsed.Any())
            {
                result.StrategiesInfo = PopulateStrategiesInfo(result.StrategiesUsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante la lettura del file {file}: {ex.Message}");
            return null;
        }
    }

    private static (string SetupName, DateTime CreatedAt) ParseBacktestFileId(string fileId)
    {
        const string prefix = "backtest_";
        var nameAndTimestamp = fileId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? fileId[prefix.Length..]
            : fileId;

        var lastSeparator = nameAndTimestamp.LastIndexOf('_');
        if (lastSeparator > 0)
        {
            var timestamp = nameAndTimestamp[(lastSeparator + 1)..];
            if (DateTime.TryParseExact(
                    timestamp,
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var createdAt))
            {
                return (nameAndTimestamp[..lastSeparator], createdAt);
            }
        }

        return (nameAndTimestamp, default);
    }

    public bool DeleteBacktesting(string jobId)
    {
        try
        {
            // Cerca il file del risultato
            var files = EnumerateResultFiles().ToArray();
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var result = JsonSerializer.Deserialize<BacktestingResult>(json, _jsonOptions);
                    if (result != null && result.JobId == jobId)
                    {
                        File.Delete(file);
                        var htmlFile = Path.ChangeExtension(file, ".html");
                        if (File.Exists(htmlFile))
                        {
                            File.Delete(htmlFile);
                        }

                        var tradeSignalsFile = Path.Combine(
                            Path.GetDirectoryName(file) ?? _resultsPath,
                            $"{Path.GetFileNameWithoutExtension(file)}_signals.json");
                        if (File.Exists(tradeSignalsFile))
                        {
                            File.Delete(tradeSignalsFile);
                        }

                        Console.WriteLine($"Backtesting eliminato: {file}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante la lettura del file {file}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Backtesting con JobId {jobId} non trovato");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'eliminazione del backtesting: {ex.Message}");
            return false;
        }
    }

    private async Task ExecuteBacktesting(
        BacktestingJob job,
        BacktestingRequest request,
        TradingPlan? plan,
        string outputPath,
        CancellationToken cancellationToken)
    {
        // Dichiarati fuori dal try: servono anche ai rami di errore e al finally.
        BacktestDiagnosticsLogger? diagnostics = null;
        var startedAtUtc = DateTime.UtcNow;

        try
        {
            lock (job)
            {
                job.Status = BacktestingJobStatus.Running;
                job.Phase = "LoadingData";
                job.ProgressMessage = "Preparazione strategie e caricamento dati";
                job.StartedAt = startedAtUtc;
            }
            await _executionHook.OnJobRunningAsync(job.JobId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            request.StartDate = TradingDateTime.ToFeedUtc(request.StartDate);
            request.EndDate = TradingDateTime.ToFeedUtc(request.EndDate);

            Console.WriteLine($"[Backtesting] Range UTC: {request.StartDate:yyyy-MM-dd HH:mm}Z -> {request.EndDate:yyyy-MM-dd HH:mm}Z");

            // Gli ID ricevuti sono già il masterfilter risolto dal chiamante/controller:
            // sono l'unica fonte autorevole della selezione esecutiva.
            var selectedStrategyIds = request.SelectedStrategyIds
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var strategies = StrategyFactory.GetRegisteredStrategies()
                .Where(strategy => selectedStrategyIds.Contains(strategy.Id))
                .ToList();
            var resolvedIds = strategies
                .Select(strategy => strategy.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingStrategyIds = selectedStrategyIds
                .Where(id => !resolvedIds.Contains(id))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingStrategyIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Strategie del masterfilter non presenti nel catalogo: {string.Join(", ", missingStrategyIds)}");
            }

            // L'universo operativo del conto, quando il run ne dichiara uno: le strategie su simboli
            // che la sua tabella di conversione non prevede non girano affatto. Va applicato QUI,
            // prima che le istanze vengano create e prima che i simboli finiscano in
            // SelectedSymbols: cosi' il run non pretende nemmeno il datafeed di uno strumento che
            // non opererebbe.
            var masterfilterStrategies = strategies.Count;
            var catalogStrategies = StrategyFactory.GetRegisteredStrategies().ToList();
            var strategiesNotInMasterfilter = catalogStrategies
                .Where(strategy => !selectedStrategyIds.Contains(strategy.Id))
                .Select(strategy => strategy.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var (strategiesForPlan, excludedByBroker, disabledByPlan, planUniverse) =
                ApplyPlanUniverse(strategies, plan);
            strategies = strategiesForPlan;

            Console.WriteLine(
                $"[Backtesting] Catalogo {catalogStrategies.Count} classi, masterfilter " +
                $"{masterfilterStrategies}, schedulate {strategies.Count}. Universo del piano: " +
                (planUniverse.PlanCode is null
                    ? "nessun piano (run neutro)."
                    : planUniverse.AppliedAsNeutralUniverse
                        ? $"piano '{planUniverse.PlanCode}' (broker '{planUniverse.BrokerCode ?? "-"}') " +
                          "senza tabella di conversione, ammessi tutti i simboli."
                        : $"piano '{planUniverse.PlanCode}', broker '{planUniverse.BrokerCode}', tabella " +
                          $"'{planUniverse.SymbolConversionCode}' con {planUniverse.MappedSymbols} simboli " +
                          $"({planUniverse.EnabledSymbols} abilitati)."));

            if (disabledByPlan.Count > 0)
            {
                Console.WriteLine(
                    $"[Backtesting] Piano '{request.PlanCode}': {disabledByPlan.Count} strategie " +
                    $"spente dal piano — {string.Join(", ", disabledByPlan)}");
            }

            if (excludedByBroker.Count > 0)
            {
                Console.WriteLine(
                    $"[Backtesting] Piano '{request.PlanCode}': {excludedByBroker.Count} strategie " +
                    $"escluse perche' il simbolo non e' nella tabella di conversione del broker " +
                    $"'{planUniverse.BrokerCode}' — {string.Join(", ", excludedByBroker)}");
            }

            if (strategies.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Il piano '{request.PlanCode}' non lascia nessuna strategia del masterfilter: " +
                    $"{disabledByPlan.Count} spente dal piano, {excludedByBroker.Count} su simboli che " +
                    "la tabella di conversione del suo broker non prevede. Il run non produrrebbe un " +
                    "solo segnale.");
            }

            request.SelectedSymbols = strategies
                .Select(strategy => strategy.Symbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Console.WriteLine($"[Backtesting] Strategie risolte dal masterfilter: {strategies.Count}");
            
            if (!strategies.Any())
            {
                // Log dei simboli disponibili per debug
                var allStrategies = StrategyFactory.GetRegisteredStrategies();
                var availableSymbols = allStrategies.Select(s => s.Symbol).Distinct().ToList();
                Console.WriteLine($"[Backtesting] Simboli disponibili nelle strategie C# registrate: {string.Join(", ", availableSymbols)}");
                throw new InvalidOperationException("Nessuna strategia del masterfilter è disponibile nel catalogo.");
            }

            // Log delle strategie trovate
            foreach (var strategyDef in strategies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"[Backtesting] Strategia trovata: Name='{strategyDef.Name}', Symbol='{strategyDef.Symbol}', Timeframe={strategyDef.TimeframeMinutes}, FileName='{strategyDef.FileName}'");
            }

            // Crea istanze delle strategie C# o EasyLanguage convertite
            var createdStrategies = new List<(StrategyDefinition Definition, ITradingStrategy Instance)>();
            foreach (var strategyDef in strategies)
            {
                Console.WriteLine($"[Backtesting] Tentativo di creare strategia: Name='{strategyDef.Name}', Symbol='{strategyDef.Symbol}', Timeframe={strategyDef.TimeframeMinutes}");
                
                // Prova a creare la strategia usando il nome dalla definizione
                var strategy = StrategyFactory.CreateStrategy(strategyDef.Name, strategyDef.Symbol, strategyDef.TimeframeMinutes, strategyDef.Parameters);
                
                if (strategy == null)
                {
                    throw new InvalidOperationException(
                        $"Impossibile creare la strategia '{strategyDef.Id}' ({strategyDef.Name}) del masterfilter.");
                }

                Console.WriteLine($"[Backtesting] Strategia creata con successo: {strategy.Name} (Type: {strategy.GetType().Name}), Symbol: {strategy.Symbol}, Timeframe: {strategy.TimeframeMinutes}");
                createdStrategies.Add((strategyDef, strategy));
            }

            var strategyInstances = createdStrategies.Select(item => item.Instance).ToList();
            if (!strategyInstances.Any())
            {
                throw new InvalidOperationException("Nessuna strategia C# disponibile");
            }

            Console.WriteLine($"[Backtesting] Totale strategie create: {strategyInstances.Count}");

            // Calcola il minimo timeframe tra tutte le strategie
            var strategyMinTimeframe = strategyInstances.Min(s => s.TimeframeMinutes);
            Console.WriteLine($"Timeframe minimo calcolato: {strategyMinTimeframe} minuti per {strategyInstances.Count} strategie");

            // L'orologio del loop: di norma il timeframe piu' corto delle strategie, ma la richiesta
            // puo' chiederne uno piu' fitto. Non e' un dettaglio di prestazione — da questa barra
            // esce il prezzo di riempimento, quindi e' una convenzione di fill a tutti gli effetti.
            var minTimeframeMinutes = BacktestClock.Resolve(
                request.ClockTimeframeMinutes,
                strategyMinTimeframe,
                strategyInstances.Select(instance => (instance.Name, instance.TimeframeMinutes)));
            var clockIsFiner = minTimeframeMinutes < strategyMinTimeframe;
            if (clockIsFiner)
            {
                Console.WriteLine(
                    $"[Backtesting] Orologio del loop forzato a {minTimeframeMinutes} minuti " +
                    $"(le strategie piu' corte sono a {strategyMinTimeframe}): i riempimenti si " +
                    "valutano sulle barre dell'orologio, non su quelle delle strategie.");
            }

            Directory.CreateDirectory(outputPath);
            var tradingJsonStore = new TradingJsonStore(outputPath);
            tradingJsonStore.Initialize();

            // Un motore di trading PER JOB: PiootooTradingService è mutabile e non thread-safe,
            // condividerlo tra backtest concorrenti mescolerebbe posizioni e trade.
            var tradingService = new PiootooTradingService();
            tradingService.Initialize(request.InitialCapital, request.CommissionPerContract);
            tradingService.RejectWrongSideLevels = request.RejectWrongSideLevels;
            if (request.StopFillSlippagePoints is { Count: > 0 } slippage)
                foreach (var (sym, points) in slippage)
                    tradingService.StopFillSlippagePoints[sym] = points;

            // Lo spread misurato dal broker prima, le correzioni a mano dopo: la tabella e' un
            // default per simbolo e la richiesta puo' scavalcarne uno senza rifare la misura. Un
            // broker senza misura fa fallire l'avvio come un datafeed mancante — un run che ripiega
            // in silenzio su "nessuno spread" e' indistinguibile da uno con lo spread, e vale
            // un'altra cosa.
            var spreadSource = "nessuno";
            if (!string.IsNullOrWhiteSpace(request.SpreadBroker))
            {
                var spreadTable = SpreadTable.Load(
                    _settings.GetSpreadPath(),
                    request.SpreadBroker,
                    request.SpreadStatistic,
                    request.SpreadResolution);

                foreach (var (sym, points) in spreadTable.Points)
                    tradingService.SpreadPoints[sym] = points;

                // Le ore quando il run le chiede. La costante resta comunque caricata: e' il ripiego
                // dei simboli che nel file per ora non ci sono, ed e' il numero che il summary e il
                // report mostrano come misura di riferimento.
                foreach (var (sym, hours) in spreadTable.PointsByHour)
                    tradingService.SpreadPointsByHour[sym] = hours;

                spreadSource = spreadTable.Describe();

                // Gli avvisi non fermano il run: la misura c'e', e' solo meno solida di quanto
                // sembri (finestra coperta a meta', simbolo senza tick). Vanno pero' detti, perche'
                // nel summary lo spread e' un numero e un numero non dice su cosa e' stato misurato.
                foreach (var warning in spreadTable.Warnings)
                    Console.WriteLine($"[Backtesting][spread] {warning}");
            }

            // Uno spread negativo farebbe entrare meglio del mercato: non e' un modello ottimista,
            // e' un errore di compilazione della richiesta, e in silenzio produrrebbe un run che
            // sembra normale. Zero invece e' legittimo — e' il run senza spread.
            if (request.SpreadPoints is { Count: > 0 } spreads)
            {
                foreach (var (sym, points) in spreads)
                {
                    if (points < 0m)
                    {
                        throw new ArgumentException(
                            "SpreadPoints non puo' essere negativo: " + sym + " = " +
                            points.ToString(CultureInfo.InvariantCulture) + ".",
                            nameof(request));
                    }

                    // Normalizzato come le chiavi della tabella: il motore normalizza comunque in
                    // lettura, ma il report affianca questi simboli a quelli del run e "@NQ" e "NQ"
                    // gli sembrerebbero due strumenti diversi.
                    var key = StrategyKeys.NormalizeSymbol(sym);
                    tradingService.SpreadPoints[key] = points;

                    // Un valore scritto a mano scavalca la misura, e la misura comprende le ore: se
                    // restassero, il motore leggerebbe la tabella oraria e la correzione sarebbe
                    // ignorata in silenzio — cioe' il contrario di cio' che chi la scrive si aspetta.
                    tradingService.SpreadPointsByHour.Remove(key);
                }

                spreadSource = spreadSource == "nessuno"
                    ? "richiesta (valori scritti a mano)"
                    : spreadSource + " + correzioni dalla richiesta";
            }

            // Il cBot dichiara lo stesso passo minimo fra 0 e 1 (MinValue/MaxValue sul parametro):
            // fuori da quell'intervallo il numero non ha un significato che i due motori
            // condividano, e un run che lo usasse non sarebbe confrontabile con nulla.
            if (request.TrailingMinStepFraction < 0m || request.TrailingMinStepFraction > 1m)
            {
                throw new ArgumentException(
                    "TrailingMinStepFraction deve stare fra 0 e 1: ricevuto " +
                    request.TrailingMinStepFraction.ToString(CultureInfo.InvariantCulture) + ".",
                    nameof(request));
            }

            tradingService.TrailingMinStepFraction = request.TrailingMinStepFraction;

            // Stessa policy del descriptor di sessione e del cBot: e' l'unico modo perche' backtest
            // e conto vero taglino negli stessi istanti.
            var holding = request.Holding ?? AccountHoldingPolicy.Default;
            holding.Validate();
            var weekEndFlat = holding.WeekEnd;

            diagnostics = new BacktestDiagnosticsLogger(outputPath, job.JobId);
            tradingService.PositionOpened = diagnostics.LogEntry;
            tradingService.PositionClosed = diagnostics.LogExit;
            foreach (var (definition, instance) in createdStrategies)
            {
                diagnostics.RegisterStrategy(
                    instance.Name, definition.Name, instance.Symbol, instance.TimeframeMinutes,
                    instance.RequiredCandles);
            }

            diagnostics.LogRun("avvio job", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Quale binario ha prodotto questo run. Senza, un backtest lanciato contro un
                // server non ricompilato e' indistinguibile da uno aggiornato, e il confronto
                // con l'esterno misura un motore che non e' piu' quello del sorgente.
                ["engineVersion"] = PiootooVersion.Current,
                ["workspaceId"] = request.WorkspaceId,
                ["backtestFolder"] = request.BacktestFolderName,
                ["startUtc"] = request.StartDate.ToString("O"),
                ["endUtc"] = request.EndDate.ToString("O"),
                ["initialCapital"] = request.InitialCapital.ToString(CultureInfo.InvariantCulture),
                ["commissionPerContract"] = request.CommissionPerContract.ToString(CultureInfo.InvariantCulture),
                ["minTimeframeMinutes"] = minTimeframeMinutes.ToString(),
                // L'orologio accanto al minimo delle strategie: quando i due numeri differiscono i
                // riempimenti escono da barre diverse, e due run cosi' non sono confrontabili.
                ["strategyMinTimeframeMinutes"] = strategyMinTimeframe.ToString(),
                ["clockTimeframeMinutes"] = minTimeframeMinutes.ToString(),
                ["strategies"] = strategyInstances.Count.ToString(),
                ["allowOvernight"] = holding.AllowOvernight ? "true" : "false",
                ["sessionFlatFromUtc"] = holding.AllowOvernight
                    ? "-"
                    : holding.SessionFlatUtcHhmm.ToString("0000"),
                ["allowOverweek"] = holding.AllowOverweek ? "true" : "false",
                ["weekEndFlatFromUtc"] = weekEndFlat.FromUtcHhmm.ToString("0000"),
                ["rejectWrongSideLevels"] = request.RejectWrongSideLevels ? "true" : "false",
                ["trailingMinStepFraction"] = request.TrailingMinStepFraction.ToString(CultureInfo.InvariantCulture),
                // Le convenzioni di riempimento cambiano il risultato quanto gli orari di tenuta e
                // non lasciano traccia nei trade: vanno dichiarate qui e nel summary, altrimenti due
                // run non confrontabili sono indistinguibili a posteriori.
                ["intrabarPriority"] = "ProtectiveBeforeTarget",
                ["trailingPeakIncludesCurrentBar"] = tradingService.TrailingPeakIncludesCurrentBar ? "true" : "false",
                ["stopFillSlippageSymbols"] = tradingService.StopFillSlippagePoints.Count == 0
                    ? "-"
                    : string.Join(",", tradingService.StopFillSlippagePoints.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
                // Con i valori e non i soli simboli: lo spread cambia l'esito in proporzione alla
                // propria misura, e "c'era lo spread su NQ" non dice se il run e' confrontabile con
                // quello di ieri.
                ["entrySpreadSource"] = spreadSource,
                ["entrySpreadPoints"] = tradingService.SpreadPoints.Count == 0
                    ? "-"
                    : string.Join(",", tradingService.SpreadPoints
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(entry => entry.Key + "=" + entry.Value.ToString(CultureInfo.InvariantCulture))),
                // Con la risoluzione per ora i valori sopra sono il ripiego, non quello che si paga:
                // qui va l'escursione fra le 24 ore, che e' il numero da cui si vede se la scelta ha
                // cambiato qualcosa. Un simbolo che va da 1,5 a 12 non e' lo stesso strumento a
                // seconda di quando entra.
                ["entrySpreadResolution"] = tradingService.SpreadPointsByHour.Count == 0
                    ? "per simbolo"
                    : "per ora UTC",
                ["entrySpreadHourRange"] = tradingService.SpreadPointsByHour.Count == 0
                    ? "-"
                    : string.Join(",", tradingService.SpreadPointsByHour
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(entry => entry.Key + "=" +
                                         entry.Value.Min().ToString(CultureInfo.InvariantCulture) + ".." +
                                         entry.Value.Max().ToString(CultureInfo.InvariantCulture))),
                // Il piano governa universo, tenuta e commissione: senza il suo codice qui, due run
                // con piani diversi hanno lo stesso log di avvio e differiscono solo nei risultati.
                ["planCode"] = plan?.Code ?? "-",
                ["planBroker"] = planUniverse.BrokerCode ?? "-",
                ["planDisabledStrategies"] = disabledByPlan.Count.ToString(CultureInfo.InvariantCulture),
                ["symbolConversionCode"] = planUniverse.SymbolConversionCode ?? "-",
                ["symbolConversionSymbols"] = planUniverse.MappedSymbols.ToString(CultureInfo.InvariantCulture),
                ["catalogStrategies"] = catalogStrategies.Count.ToString(CultureInfo.InvariantCulture),
                ["masterfilterStrategies"] = masterfilterStrategies.ToString(CultureInfo.InvariantCulture)
            });

            var result = new BacktestingResult
            {
                JobId = job.JobId,
                SetupName = request.Name,
                SetupId = string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                InitialCapital = request.InitialCapital,
                CreatedAt = DateTime.UtcNow,
                // Stessa sorgente del marcatore scritto alla creazione della cartella: il report
                // HTML lo stampa sotto il titolo, e un run che non dice su quali prezzi e' girato
                // non e' confrontabile con nessun altro.
                PriceSource = RunPriceSource.FromDatafeedBroker(request.DatafeedBroker),
                StrategiesUsed = createdStrategies.Select(item => item.Definition.Name).ToList(),
                StrategiesInfo = createdStrategies.Select(item => new Piootoo.Shared.Models.Backtesting.StrategyInfo
                {
                    Name = item.Definition.Name,
                    // StrategyCode è il codice di ESECUZIONE (ITradingStrategy.Name), lo stesso che
                    // finisce nei segnali, nei trade e nelle chiavi di posizione. Usare qui l'Id di
                    // classe rompeva ogni join a valle: equity per strategia piatta, zero trade nel
                    // report. Vedi docs/PROGETTO.md §3.2.
                    StrategyCode = item.Instance.Name,
                    Symbol = item.Definition.Symbol,
                    TimeframeMinutes = item.Definition.TimeframeMinutes
                }).DistinctBy(s => new { s.StrategyCode, s.Symbol, s.TimeframeMinutes }).ToList()
            };

            // Precalcolato una volta: dentro il loop questo elenco veniva rigenerato con
            // GroupBy+OrderBy a ogni barra.
            var orderedStrategyInfos = result.StrategiesInfo
                .GroupBy(info => MakeStrategyKey(info.Symbol, GetStrategyCode(info)), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(info => info.Symbol, StringComparer.Ordinal)
                .ThenBy(info => GetStrategyCode(info), StringComparer.Ordinal)
                .ToList();
            var strategyEquityCache = orderedStrategyInfos
                .ToDictionary(info => MakeStrategyKey(info.Symbol, GetStrategyCode(info)),
                    _ => result.InitialCapital, StringComparer.OrdinalIgnoreCase);

            // Traccia per strategia: chiave e codice precalcolati (MakeStrategyKey allocava quattro
            // stringhe per strategia per barra per ricostruire un valore costante) e l'ultimo giorno
            // in cui e' stata scritta una riga, che serve al battito di cui sotto.
            var strategyTracks = orderedStrategyInfos
                .Select(info => new StrategyEquityTrack(
                    info,
                    MakeStrategyKey(info.Symbol, GetStrategyCode(info)),
                    GetStrategyCode(info)))
                .ToArray();
            var emittedTradeSignals = new List<TradeSignal>();

            // Arrotonda StartDate al timeframe minimo più vicino (verso il basso)
            var roundedStartDate = TradingDateTime.RoundDownToTimeframeUtc(request.StartDate, minTimeframeMinutes);
            Console.WriteLine($"[Backtesting] Date UTC: Start={request.StartDate:yyyy-MM-dd HH:mm}Z, End={request.EndDate:yyyy-MM-dd HH:mm}Z, RoundedStart={roundedStartDate:yyyy-MM-dd HH:mm}Z");

            // ========== PREFILL DATASOURCE ==========
            // Un cursore per combinazione (Symbol, Timeframe). Il cursore sostituisce il vecchio
            // Where+OrderBy+Take su tutta la serie a ogni barra: la serie è ordinata e l'orologio
            // del loop è monotono, quindi basta far avanzare un indice.
            var cursors = new Dictionary<(string Symbol, int Timeframe), CandleWindowCursor>();

            // Per ogni simbolo, il cursore con il timeframe più fine disponibile: è quello che dà
            // il prezzo di mark-to-market più accurato a ogni barra del loop.
            var markCursors = new Dictionary<string, (int Timeframe, CandleWindowCursor Cursor)>(StringComparer.OrdinalIgnoreCase);

            var uniqueDataSources = strategyInstances
                .SelectMany(GetStrategyDataRequirements)
                .GroupBy(x => (Symbol: NormalizeSymbolWithPrefix(x.Symbol), x.Timeframe))
                .Select(g => (g.Key.Symbol, g.Key.Timeframe, MaxRequiredCandles: g.Max(x => x.RequiredCandles)))
                .OrderBy(x => x.Symbol, StringComparer.Ordinal)
                .ThenBy(x => x.Timeframe)
                .ToList();

            // Le barre dell'orologio, uno stream per simbolo, in piu' di quelli che le strategie
            // chiedono. Passano dallo stesso caricamento: cosi' ereditano diagnostica, avvisi di
            // copertura e — soprattutto — il fail fast sul datasource vuoto. Un simbolo senza le
            // barre dell'orologio resterebbe altrimenti al proprio timeframe, e il run mescolerebbe
            // due risoluzioni di riempimento senza dirlo.
            if (clockIsFiner)
            {
                var clockSymbols = strategyInstances
                    .Select(instance => NormalizeSymbolWithPrefix(instance.Symbol))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(symbol => !uniqueDataSources.Any(ds =>
                        ds.Timeframe == minTimeframeMinutes &&
                        string.Equals(ds.Symbol, symbol, StringComparison.OrdinalIgnoreCase)))
                    // Il lookback serve alle strategie per riempire la propria finestra di candele;
                    // l'orologio non ne ha una, gli basta il periodo del run. Chiedere qui le stesse
                    // centinaia di barre delle strategie moltiplicherebbe per il timeframe la
                    // memoria di uno stream che e' gia' il piu' pesante dell'archivio.
                    .Select(symbol => (Symbol: symbol, Timeframe: minTimeframeMinutes, MaxRequiredCandles: 1))
                    .ToList();

                uniqueDataSources.AddRange(clockSymbols);
                uniqueDataSources = uniqueDataSources
                    .OrderBy(x => x.Symbol, StringComparer.Ordinal)
                    .ThenBy(x => x.Timeframe)
                    .ToList();

                Console.WriteLine(
                    $"[Backtesting] Orologio: {clockSymbols.Count} stream a {minTimeframeMinutes}m in piu' " +
                    "di quelli delle strategie.");
            }

            Console.WriteLine($"[Backtesting] Pre-caricamento {uniqueDataSources.Count} datasource unici " +
                              $"da datafeed {_datafeedCatalog.Describe(request.DatafeedBroker)}...");

            var emptyDataSources = new List<string>();
            var legIrraggiungibili = new List<string>();
            var loadedDataSources = 0;
            foreach (var ds in uniqueDataSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Il lookback va espresso in giorni di CALENDARIO, non in barre: i future hanno
                // sessioni non continue e weekend, quindi N barre coprono molto più di
                // N*timeframe minuti. Con un fattore 1 il feed veniva tagliato e la prima parte
                // del backtest restava senza dati.
                var lookbackDays = Math.Max(30d, ds.MaxRequiredCandles * ds.Timeframe / (24d * 60d) * 3d);
                var candles = await _dataFeedService.GetCandlesRangeAsync(
                    ds.Symbol,
                    request.StartDate.AddDays(-lookbackDays),
                    request.EndDate,
                    ds.Timeframe,
                    request.DatafeedBroker);

                var cursor = new CandleWindowCursor(candles);
                var normalizedSymbol = NormalizeSymbol(ds.Symbol);
                cursors[(normalizedSymbol, ds.Timeframe)] = cursor;

                // Il BIASW entra ed esce a giorno e ora fissi con un confronto esatto: se il feed
                // non ha una sola barra a quell'istante la leg non esiste, e il motore non lo
                // segnala in alcun modo — non e' uno skip, e' niente. Il controllo va qui, sulla
                // serie appena caricata, prima che il run parta: dopo si vedrebbe solo l'assenza
                // di segnali. Vedi compare-0017 §3.3.
                foreach (var (_, instance) in createdStrategies)
                {
                    if (instance is not BiasWeeklyEngine settimanale ||
                        instance.TimeframeMinutes != ds.Timeframe ||
                        !string.Equals(NormalizeSymbol(instance.Symbol), normalizedSymbol,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var legMorte = settimanale.UnreachableScheduleLegs(candles);
                    if (legMorte.Count == 0)
                        continue;

                    var messaggio =
                        $"{instance.Name}: il feed non ha nessuna barra all'istante programmato di " +
                        $"{legMorte.Count} leg su {normalizedSymbol}/{ds.Timeframe}m — " +
                        string.Join(", ", legMorte) +
                        ". Quelle leg non possono produrre nulla per tutto il run.";
                    Console.WriteLine($"[Backtesting][anomalia] {messaggio}");
                    diagnostics.LogAnomaly(messaggio, null, instance.Name, normalizedSymbol);
                    diagnostics.AddRunDiagnostic("[calendario] " + messaggio);
                    legIrraggiungibili.Add(messaggio);
                }

                if (candles.Length > 0 &&
                    (!markCursors.TryGetValue(normalizedSymbol, out var existing) || ds.Timeframe < existing.Timeframe))
                {
                    markCursors[normalizedSymbol] = (ds.Timeframe, cursor);
                }

                var coversRange = candles.Length > 0 &&
                                  cursor.FirstBarUtc <= request.StartDate &&
                                  cursor.LastBarUtc >= request.EndDate.AddDays(-3);
                var warning = candles.Length == 0
                    ? $"nessuna candela per {ds.Symbol}/{ds.Timeframe}m: file feed assente o vuoto"
                    : coversRange
                        ? null
                        : $"copertura parziale: {cursor.FirstBarUtc:yyyy-MM-dd} → {cursor.LastBarUtc:yyyy-MM-dd}";

                // Il layer legge la serie e dice su che griglia sta. E' l'unica forma di errore
                // del feed che il conteggio delle candele e la copertura NON vedono: una serie nata
                // su un ancoraggio diverso ha il numero di barre giusto e copre l'intervallo
                // giusto — sono semplicemente barre altre. Descrive e non decide: il run non si
                // ferma, perche' anche il feed del vendor ha una barra fuori griglia all'anno per
                // file giornaliero. Vedi docs/domini/layer-barre-e-calendario.md.
                var calendarSummary = DescribeFeedCalendar(normalizedSymbol, ds.Timeframe, candles, diagnostics);

                diagnostics.LogDataSource(new BacktestDataSourceSummary
                {
                    Symbol = normalizedSymbol,
                    TimeframeMinutes = ds.Timeframe,
                    CandleCount = candles.Length,
                    FirstBarUtc = cursor.FirstBarUtc,
                    LastBarUtc = cursor.LastBarUtc,
                    CoversRequestedRange = coversRange,
                    Warning = warning,
                    Calendar = calendarSummary
                });

                if (candles.Length == 0)
                {
                    emptyDataSources.Add($"{normalizedSymbol}/{ds.Timeframe}m");
                }

                Console.WriteLine($"[Backtesting] {normalizedSymbol}/{ds.Timeframe}m: {candles.Length} candele" +
                                  (warning is null ? "" : $" — {warning}"));

                loadedDataSources++;
                lock (job)
                {
                    job.ProgressPercent = uniqueDataSources.Count == 0
                        ? 0
                        : Math.Clamp((int)(loadedDataSources * 5.0 / uniqueDataSources.Count), 0, 5);
                    job.ProgressMessage = $"Caricamento dati {loadedDataSources}/{uniqueDataSources.Count}";
                }
            }

            // Fail fast: proseguire con un datasource vuoto significa un backtest che gira per ore
            // e produce zero trade senza dire perché.
            if (emptyDataSources.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Datafeed {_datafeedCatalog.Describe(request.DatafeedBroker)} mancante per: " +
                    string.Join(", ", emptyDataSources) +
                    $". Scarica i file corrispondenti in {_datafeedCatalog.ResolveRoot(request.DatafeedBroker)} " +
                    "oppure rimuovi dal masterfilter le strategie su queste coppie simbolo/timeframe.");
            }
            // Fine effettiva della copertura dati: massimo fra le ultime barre dei cursori. Oltre
            // questo punto l'orologio sintetico continua fino a EndDate ma non arriva più alcun
            // prezzo, quindi l'equity resta piatta. Serve ai resoconti annuale/mensile per non
            // stampare mesi vuoti facendoli passare per mesi senza operatività.
            var coverageEnds = cursors.Values
                .Select(cursor => cursor.LastBarUtc)
                .Where(last => last.HasValue)
                .Select(last => last!.Value)
                .ToList();
            result.DataCoverageEndUtc = coverageEnds.Count > 0 ? coverageEnds.Max() : (DateTime?)null;

            // ========== FINE PREFILL ==========

            // Iterazione usando il timeframe minimo
            var currentDate = roundedStartDate;
            var totalMinutes = (int)(request.EndDate - roundedStartDate).TotalMinutes;
            var totalIterations = totalMinutes > 0 ? totalMinutes / minTimeframeMinutes : 0;
            var processedIterations = 0;
            var iterationCount = 0; // Contatore per calcolare l'allineamento delle strategie
            var markedToMarketBars = 0L;
            var weekEndCancelledOrders = 0L;
            var lastPersistedIteration = 0;
            // Segnaposto della persistenza incrementale: quanti segnali e quanti trade sono gia'
            // finiti nel journal. Entrambe le liste sono append-only, quindi basta l'indice.
            var persistedSignals = 0;
            var persistedTrades = 0;

            Console.WriteLine($"[Backtesting] Loop configurato: TotalMinutes={totalMinutes}, TotalIterations={totalIterations}, MinTimeframe={minTimeframeMinutes}");

            while (currentDate <= request.EndDate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (job.Phase != "Running")
                {
                    lock (job)
                    {
                        job.Phase = "Running";
                        job.ProgressMessage = "Esecuzione strategie";
                    }
                }
                // Il fine settimana NON si salta per calendario. La sessione della ricerca e' il
                // giorno di calendario europeo, quindi la riapertura del lunedi' cade alle
                // 22:00 (ora legale) o 23:00 (ora solare) UTC di DOMENICA: saltare il sabato e la
                // domenica UTC toglieva al motore l'apertura di ogni lunedi' — niente valutazione,
                // niente fill, niente mark-to-market — e su BTC, che quota 24/7, due giorni pieni
                // a settimana. Nel feed interno le barre di sabato/domenica UTC sono il 21,0% su
                // @NQ_1440 (sono tutti i lunedi'), il 3,6% sui 4h CME e l'1,4% sugli intraday;
                // sul confronto compare-0021 il motore interno aveva ZERO trade nel fine settimana
                // UTC contro 110 su 1.273 del cBot sullo stesso feed e sulla stessa finestra, e
                // sul giornaliero 3 lunedi' contro 12.
                //
                // Resta il salto quando e' il PIANO a vietare l'overweek: li' il conto deve essere
                // piatto e senza ordini fino alla riapertura, quindi non c'e' niente da valutare.
                // Il tick su cui il flat SCATTA passa — e' quello che chiude le posizioni e
                // cancella i pending, in fondo al corpo del loop — e vengono saltati solo quelli
                // dopo, fino a WeekEndFlatPolicy.UntilUtcHhmm.
                if (IterationIsSkippedByWeekEndFlat(holding, currentDate, minTimeframeMinutes))
                {
                    currentDate = currentDate.AddMinutes(minTimeframeMinutes);
                    iterationCount++;
                    processedIterations++;
                    if (totalIterations > 0)
                    {
                        var progress = Math.Clamp(
                            5 + (int)(processedIterations * 94.0 / totalIterations),
                            5,
                            99);
                        if (progress != job.ProgressPercent)
                        {
                            lock (job)
                            {
                                job.ProgressPercent = progress;
                                job.ProgressMessage = $"Elaborazione {progress}%";
                            }
                        }
                    }
                    continue;
                }

                var signals = new List<TradeSignal>();

                // Prezzi e candele di TUTTI i simboli del portafoglio a questa barra, calcolati
                // prima di valutare le strategie e indipendentemente da quali strategie sono
                // allineate adesso. Prima venivano popolati solo dalle strategie effettivamente
                // valutate: sulle barre "vuote" il mark-to-market non veniva eseguito e stop loss,
                // take profit e time exit scattavano in ritardo o su un simbolo solo.
                var currentPrices = new Dictionary<string, decimal>(markCursors.Count, StringComparer.OrdinalIgnoreCase);
                var currentBars = new Dictionary<string, OhlcvData>(markCursors.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var (symbol, mark) in markCursors)
                {
                    var bar = mark.Cursor.LastCandle(currentDate);
                    if (bar is null) continue;

                    // Il prezzo di mark-to-market è sempre l'ultimo noto, anche stantio: senza
                    // di esso stop e time exit non potrebbero essere valutati affatto. La barra
                    // invece entra in currentBars solo se appartiene a questo tick, perché è
                    // quella che l'esecuzione usa per far scattare trigger e riempimenti.
                    currentPrices[symbol] = bar.Close;
                    if (BelongsToCurrentTick(bar.DateTime, currentDate, minTimeframeMinutes))
                    {
                        currentBars[symbol] = bar;
                    }
                }

                foreach (var strategy in strategyInstances)
                {
                    var strategySymbol = strategy.Symbol;
                    var strategyCode = strategy.Name;

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Una strategia viene valutata quando il numero di iterazioni è un multiplo
                        // del rapporto tra il suo timeframe e il minimo del portafoglio.
                        if (!ShouldEvaluateStrategy(currentDate, iterationCount, strategy.TimeframeMinutes, minTimeframeMinutes))
                        {
                            continue;
                        }

                        diagnostics.CountScheduled(strategySymbol, strategyCode);

                        var requiredCandles = (int)(strategy.RequiredCandles * 1.2);
                        if (!cursors.TryGetValue((NormalizeSymbol(strategySymbol), strategy.TimeframeMinutes), out var cursor))
                        {
                            diagnostics.CountSkipNoData(strategySymbol, strategyCode);
                            continue;
                        }

                        // O(requiredCandles) invece di O(candele totali): il cursore avanza con
                        // l'orologio del loop e copia solo la finestra richiesta.
                        var candles = cursor.Window(currentDate, requiredCandles);
                        if (candles.Length < strategy.RequiredCandles)
                        {
                            if (candles.Length == 0) diagnostics.CountSkipNoData(strategySymbol, strategyCode);
                            else diagnostics.CountSkipNotEnoughCandles(strategySymbol, strategyCode);
                            continue;
                        }

                        var currentBar = candles[^1];
                        if (IsStrategyCandleStale(strategy.TimeframeMinutes, currentBar.DateTime, currentDate))
                        {
                            diagnostics.CountSkipStaleCandle(strategySymbol, strategyCode);
                            continue;
                        }

                        var normalizedSymbol = NormalizeSymbol(strategySymbol);
                        if (!currentPrices.ContainsKey(normalizedSymbol))
                        {
                            currentPrices[normalizedSymbol] = currentBar.Close;
                        }

                        if (!currentBars.ContainsKey(normalizedSymbol) &&
                            BelongsToCurrentTick(currentBar.DateTime, currentDate, minTimeframeMinutes))
                        {
                            currentBars[normalizedSymbol] = currentBar;
                        }

                        diagnostics.CountEvaluation(strategySymbol, strategyCode);

                        var execution = tradingService.GetExecutionSnapshot(strategyCode, strategySymbol, currentDate);
                        // Percorso unico anche per le multi-timeframe: invocarle direttamente su
                        // GenerateSignal saltava Evaluate, e con esso l'iniezione di posizione
                        // corrente, RuntimeState e rischio in denaro. Le serie aggiuntive
                        // viaggiano ora dentro la request.
                        var signal = strategy.Evaluate(new StrategyEvaluationRequest
                        {
                            Ohlcv = candles,
                            BarTimeUtc = currentDate,
                            Execution = execution,
                            AdditionalOhlcv = strategy is IMultiTimeframeTradingStrategy multiTimeframeStrategy
                                ? GetAdditionalTimeframeData(multiTimeframeStrategy, cursors, currentDate)
                                : NoAdditionalTimeframes
                        });

                        if (signal?.RuntimeState is not null)
                        {
                            tradingService.CaptureStrategyRuntimeState(strategyCode, strategySymbol, signal.RuntimeState);
                        }

                        if (signal is null)
                        {
                            diagnostics.CountHold(strategySymbol, strategyCode);
                            continue;
                        }

                        TradingDateTime.NormalizeSignalToUtc(signal);

                        if (signal.Type == SignalType.Hold)
                        {
                            diagnostics.CountHold(strategySymbol, strategyCode);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(signal.Symbol)) signal.Symbol = strategySymbol;
                        if (string.IsNullOrWhiteSpace(signal.StrategyCode)) signal.StrategyCode = strategyCode;
                        if (string.IsNullOrWhiteSpace(signal.StrategyName)) signal.StrategyName = strategyCode;
                        ScaleSignalMaxBarsInPosition(signal, strategy.TimeframeMinutes, minTimeframeMinutes);
                        // Prima di essere accodato e prima di essere persistito: signals.json deve
                        // riportare la deadline che verra' davvero eseguita, non quella che la
                        // strategia avrebbe voluto se il piano gliela avesse concessa.
                        ApplyAccountHolding(signal, holding);

                        signals.Add(signal);
                        emittedTradeSignals.Add(CloneTradeSignal(signal));
                        diagnostics.LogSignal(signal, strategyCode, strategySymbol, strategy.TimeframeMinutes, currentDate);

                        if (signal.CompanionSignals is not null)
                        {
                            foreach (var companion in signal.CompanionSignals)
                            {
                                if (string.IsNullOrWhiteSpace(companion.Symbol)) companion.Symbol = strategySymbol;
                                if (string.IsNullOrWhiteSpace(companion.StrategyCode)) companion.StrategyCode = strategyCode;
                                if (string.IsNullOrWhiteSpace(companion.StrategyName)) companion.StrategyName = strategyCode;
                                ScaleSignalMaxBarsInPosition(companion, strategy.TimeframeMinutes, minTimeframeMinutes);
                                ApplyAccountHolding(companion, holding);
                                signals.Add(companion);
                                emittedTradeSignals.Add(CloneTradeSignal(companion));
                                diagnostics.LogSignal(companion, strategyCode, strategySymbol, strategy.TimeframeMinutes, currentDate);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // L'errore di una strategia non ferma il portafoglio, ma viene contato e
                        // registrato come anomalia invece di scorrere via sulla console.
                        diagnostics.CountError(strategySymbol, strategyCode, currentDate, ex);
                    }
                }

                if (currentPrices.Count == 0)
                {
                    // Nessun prezzo disponibile su nessun simbolo: niente da valutare né da marcare.
                    currentDate = currentDate.AddMinutes(minTimeframeMinutes);
                    iterationCount++;
                    processedIterations++;
                    continue;
                }

                if (signals.Count > 0)
                {
                    tradingService.ProcessSignals(signals, currentPrices, currentBars, currentDate);
                }

                // Mark-to-market su ogni barra: è qui che vengono verificati stop loss, take
                // profit, time exit e riempimento degli ordini pendenti.
                var snapshot = tradingService.UpdateMarketPrices(currentPrices, currentBars, currentDate);
                markedToMarketBars++;
                AppendStrategyEquityResults(result, snapshot, currentDate, signals, strategyEquityCache, strategyTracks);

                // Il flat scatta sulla PRIMA barra dentro la finestra dichiarata, non sull'ultimo
                // slot dell'orologio sintetico prima di sabato: quello cadeva alle 23:30 del
                // venerdi' con timeframe minimo a 30 minuti, mentre il conto vero e' gia' piatto
                // dalle 20:45. Vedi WeekEndFlatPolicy.
                if (!holding.AllowOverweek &&
                    weekEndFlat.IsFlatTrigger(currentDate, currentDate.AddMinutes(-minTimeframeMinutes)))
                {
                    // Anche senza posizioni aperte c'è da fare: uno stop emesso su questa barra
                    // scade sulla prossima, che è la prima della settimana dopo, e riempirebbe sul
                    // gap di riapertura. La regola è flat di posizioni *e* di ordini.
                    var cancelled = tradingService.CancelAllPendingOrders();
                    if (cancelled > 0)
                        weekEndCancelledOrders += cancelled;

                    if (snapshot.OpenPositionsCount > 0)
                    {
                        snapshot = tradingService.CloseAllOpenPositions(
                            currentPrices, currentBars, currentDate, TradeExitReason.WeekEnd);
                        AppendStrategyEquityResults(result, snapshot, currentDate, signals, strategyEquityCache, strategyTracks);
                    }
                }

                // Checkpoint periodico invece di una riscrittura completa (con fsync) a ogni barra:
                // era la voce di costo dominante dell'intero backtest.
                if (processedIterations - lastPersistedIteration >= PersistCheckpointBars)
                {
                    lastPersistedIteration = processedIterations;

                    // Si accoda al journal solo cio' che e' nato dall'ultimo checkpoint. Prima si
                    // riscrivevano per intero signals.json e trades.json: un costo pari a TUTTO il
                    // backtest gia' fatto, pagato a ogni checkpoint, che su un run di un anno
                    // (decine di migliaia di segnali, decine di MB) rendeva il run visibilmente
                    // piu' lento verso la fine che all'inizio. Gli array veri li materializza la
                    // scrittura autorevole di fine run, poco piu' sotto.
                    tradingJsonStore.AppendSignals(
                        ToPersistedSignals(job.JobId, emittedTradeSignals, persistedSignals));
                    persistedSignals = emittedTradeSignals.Count;

                    var closedSoFar = tradingService.ClosedTradesCount;
                    if (closedSoFar > persistedTrades)
                    {
                        tradingJsonStore.AppendTrades(
                            ToPersistedTrades(job.JobId, tradingService.GetClosedTrades(), persistedTrades));
                        persistedTrades = closedSoFar;
                    }

                    diagnostics.Flush();
                }

                result.HourlyResults.Add(new HourlyResult
                {
                    DateTime = TradingDateTime.ToFeedUtc(currentDate),
                    Equity = snapshot.Equity,
                    Balance = snapshot.Balance,
                    Drawdown = snapshot.Drawdown,
                    Profit = snapshot.Profit,
                    OpenPositionsCount = snapshot.OpenPositionsCount
                });

                // Aggiorna progresso
                processedIterations++;
                iterationCount++;
                if (totalIterations > 0)
                {
                    var progress = Math.Clamp(
                        5 + (int)(processedIterations * 94.0 / totalIterations),
                        5,
                        99);
                    if (progress != job.ProgressPercent)
                    {
                        lock (job)
                        {
                            job.ProgressPercent = progress;
                            job.ProgressMessage = $"Elaborazione {progress}%";
                        }
                    }
                }

                currentDate = currentDate.AddMinutes(minTimeframeMinutes);
            }

            cancellationToken.ThrowIfCancellationRequested();
            lock (job)
            {
                job.Phase = "WritingArtifacts";
                job.ProgressPercent = 99;
                job.ProgressMessage = "Scrittura artifact";
            }

            // Calcola aggregati settimanali
            CalculateWeeklyResults(result);

            var closedTrades = tradingService.GetClosedTrades();
            var finalSnapshot = tradingService.GetSnapshot();

            // Calcola metriche finali
            result.FinalEquity = result.HourlyResults.LastOrDefault()?.Equity ?? request.InitialCapital;
            result.TotalProfit = result.FinalEquity - request.InitialCapital;
            result.MaxDrawdown = result.HourlyResults.Count == 0 ? 0m : result.HourlyResults.Max(hr => hr.Drawdown);
            // Il conteggio dei trade viene dai trade realmente chiusi dall'engine, non dal numero
            // di righe di equity con un segnale: quest'ultimo dipendeva da un join per chiave che,
            // se disallineato, restituiva sempre zero.
            result.TotalTrades = closedTrades.Count;

            // Salva risultato su file
            var fileNamePrefix = $"backtest_{request.BacktestFolderName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var fileName = $"{fileNamePrefix}.json";
            var filePath = Path.Combine(outputPath, fileName);
            var htmlReportPath = Path.Combine(outputPath, $"{fileNamePrefix}.html");
            BacktestHtmlReport.Write(
                htmlReportPath,
                result,
                closedTrades.Select(trade => BacktestReportTrade.From(trade)).ToList(),
                // Lo spread nel report per la stessa ragione del piano e del feed: cambia l'equity
                // senza comparire in un solo trade, e due report identici in tutto il resto possono
                // descrivere run con costi di transazione diversi.
                spread: new BacktestSpreadReportInfo(
                    spreadSource,
                    tradingService.SpreadPoints
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase),
                    tradingService.SpreadPointsByHour
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => (IReadOnlyList<decimal>)entry.Value.ToList(),
                            StringComparer.OrdinalIgnoreCase)),
                // Il piano nel report per la stessa ragione per cui sta nel summary: universo,
                // spente, orari e commissione cambiano l'equity senza comparire in un trade. I
                // numeri sono quelli che il run ha applicato, non quelli che il piano dice adesso.
                plan: plan is null
                    ? null
                    : new BacktestPlanReportInfo(
                        plan.Code,
                        plan.Name,
                        planUniverse.BrokerCode,
                        planUniverse.SymbolConversionCode,
                        // Le attive per nome di esecuzione — quello che si ritrova nei trade — e non
                        // per Id di catalogo: il report parla di esecuzioni (CLAUDE.md, «Id ≠ Name»).
                        strategies
                            .Select(strategy => strategy.Name)
                            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                        holding,
                        request.CommissionPerContract));
            result.HtmlReportFilePath = htmlReportPath;

            // Scrittura autorevole: qui sì, durabile.
            tradingJsonStore.WriteSignals(ToPersistedSignals(job.JobId, emittedTradeSignals));
            tradingJsonStore.WriteTrades(ToPersistedTrades(job.JobId, closedTrades));
            result.TradeSignalsFilePath = tradingJsonStore.SignalsPath;
            result.ResultFilePath = filePath;

            if (weekEndCancelledOrders > 0)
                diagnostics.LogRun(
                    $"Flat settimanale dalle {weekEndFlat.FromUtcHhmm:0000} UTC del venerdi': " +
                    $"{weekEndCancelledOrders} ordini pendenti cancellati.",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["weekEndCancelledOrders"] = weekEndCancelledOrders.ToString(CultureInfo.InvariantCulture),
                        ["weekEndFlatFromUtc"] = weekEndFlat.FromUtcHhmm.ToString("0000")
                    });

            // Un numero alto qui non e' un difetto: e' la misura di quanti ingressi il backtest
            // avrebbe preso e il conto vero no. Se e' zero e il log del cBot invece scarta, i due
            // non stanno guardando lo stesso mercato.
            if (tradingService.WrongSideLevelsRejected > 0)
                diagnostics.LogRun(
                    $"Livelli dal lato sbagliato scartati: {tradingService.WrongSideLevelsRejected}.",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["wrongSideLevelsRejected"] =
                            tradingService.WrongSideLevelsRejected.ToString(CultureInfo.InvariantCulture)
                    });

            var summary = diagnostics.Complete(new BacktestRunSummary
            {
                JobId = job.JobId,
                SetupName = request.Name,
                WorkspaceId = request.WorkspaceId,
                BacktestFolder = request.BacktestFolderName,
                RequestedStartUtc = request.StartDate,
                RequestedEndUtc = request.EndDate,
                DurationSeconds = (DateTime.UtcNow - startedAtUtc).TotalSeconds,
                MinTimeframeMinutes = minTimeframeMinutes,
                PlannedIterations = totalIterations,
                ProcessedIterations = processedIterations,
                MarkedToMarketBars = markedToMarketBars,
                InitialCapital = request.InitialCapital,
                FinalEquity = result.FinalEquity,
                TotalNetProfit = result.TotalProfit,
                MaxDrawdown = result.MaxDrawdown,
                OpenPositionsAtEnd = finalSnapshot.OpenPositionsCount,
                WrongSideLevelsRejected = tradingService.WrongSideLevelsRejected,
                Holding = holding,
                DatafeedBroker = NormalizeBroker(request.DatafeedBroker),
                PlanCode = plan?.Code,
                BrokerCode = planUniverse.BrokerCode,
                StrategiesNotSupportedByBroker = excludedByBroker,
                StrategiesDisabledByPlan = disabledByPlan,
                PlanUniverse = planUniverse,
                FillConventions = new BacktestFillConventions
                {
                    IntrabarPriority = "ProtectiveBeforeTarget",
                    TrailingPeakIncludesCurrentBar = tradingService.TrailingPeakIncludesCurrentBar,
                    TrailingMinStepFraction = tradingService.TrailingMinStepFraction,
                    RejectWrongSideLevels = tradingService.RejectWrongSideLevels,
                    StopFillSlippageSymbols = tradingService.StopFillSlippagePoints.Keys
                        .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    SpreadPoints = tradingService.SpreadPoints
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase),
                    SpreadPointsByHour = tradingService.SpreadPointsByHour
                        .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => (IReadOnlyList<decimal>)entry.Value.ToList(),
                            StringComparer.OrdinalIgnoreCase),
                    SpreadResolution = tradingService.SpreadPointsByHour.Count == 0
                        ? "per simbolo"
                        : "per ora UTC",
                    SpreadSource = spreadSource,
                    ClockTimeframeMinutes = minTimeframeMinutes,
                    ClockFinerThanStrategies = clockIsFiner
                },
                CatalogStrategies = catalogStrategies.Count,
                MasterfilterStrategies = masterfilterStrategies,
                StrategiesNotInMasterfilter = strategiesNotInMasterfilter,
                Outcome = "Completed"
            });
            result.DiagnosticsLogFilePath = diagnostics.LogPath;
            result.DiagnosticsSummaryFilePath = diagnostics.SummaryPath;

            foreach (var diagnostic in summary.Diagnostics)
            {
                Console.WriteLine($"[Backtesting][diagnosi] {diagnostic}");
            }

            Console.WriteLine($"[Backtesting] Job {result.JobId}: {closedTrades.Count} trade, " +
                              $"equity finale {result.FinalEquity:F2}, salvataggio in {fileName}");
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            cancellationToken.ThrowIfCancellationRequested();
            AtomicFileWriter.WriteAllText(filePath, json);

            lock (job)
            {
                cancellationToken.ThrowIfCancellationRequested();
                job.Result = result;
                job.Status = BacktestingJobStatus.Completed;
                job.Phase = "Completed";
                job.ProgressMessage = "Backtest completato";
                job.CompletedAt = DateTime.UtcNow;
                job.ProgressPercent = 100;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Gli artifact incrementali (signals/trades) sono sempre scritti atomicamente e
            // restano utilizzabili; un report/risultato finale eventualmente prodotto nella
            // stretta race con Cancel non deve essere pubblicato come completato.
            foreach (var path in Directory.EnumerateFiles(outputPath, "backtest_*", SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            // Il riepilogo diagnostico invece resta: dice fin dove è arrivato il run interrotto.
            diagnostics?.Complete(BuildAbortedSummary(job, request, startedAtUtc, "Cancelled", null));

            lock (job)
            {
                job.Result = null;
                job.Status = BacktestingJobStatus.Cancelled;
                job.Phase = "Cancelled";
                job.ProgressMessage = "Backtest interrotto";
                job.CompletedAt = DateTime.UtcNow;
                job.CancellationRequested = true;
            }
        }
        catch (Exception ex)
        {
            var message = $"{ex.GetType().Name}: {ex.Message}";
            diagnostics?.LogAnomaly($"job fallito — {message}");
            diagnostics?.Complete(BuildAbortedSummary(job, request, startedAtUtc, "Failed", message));

            lock (job)
            {
                job.Status = BacktestingJobStatus.Failed;
                job.Phase = "Failed";
                job.ProgressMessage = "Backtest fallito";
                job.ErrorMessage = message;
                job.CompletedAt = DateTime.UtcNow;
            }
            Console.Error.WriteLine(
                $"[Backtesting] Job {job.JobId} fallito in {RedactPath(outputPath)}: {ex}");
        }
        finally
        {
            diagnostics?.Dispose();
            _activeOutputPaths.TryRemove(outputPath, out _);
            if (_jobCancellations.TryRemove(job.JobId, out var cancellation))
                cancellation.Dispose();
        }
    }

    /// <summary>
    /// Riepilogo minimo per un run che non è arrivato in fondo. Serve a non lasciare la cartella
    /// del backtest senza spiegazioni quando il job fallisce durante il pre-caricamento dati.
    /// </summary>
    private static BacktestRunSummary BuildAbortedSummary(
        BacktestingJob job, BacktestingRequest request, DateTime startedAtUtc, string outcome, string? error) =>
        new()
        {
            JobId = job.JobId,
            SetupName = request.Name,
            WorkspaceId = request.WorkspaceId,
            BacktestFolder = request.BacktestFolderName,
            RequestedStartUtc = request.StartDate,
            RequestedEndUtc = request.EndDate,
            DurationSeconds = (DateTime.UtcNow - startedAtUtc).TotalSeconds,
            InitialCapital = request.InitialCapital,
            Outcome = outcome,
            ErrorMessage = error
        };

    private static string RedactPath(string path)
        => Path.Combine("...", Path.GetFileName(Path.GetDirectoryName(path)) ?? "workspace", Path.GetFileName(path));

    private string ResolveWorkspacePath(string workspaceId)
    {
        var workspaceService = new WorkspaceService(_settings);
        var path = workspaceService.GetWorkspacePath(workspaceId);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Workspace '{workspaceId}' non trovato.");
        return path;
    }

    private IEnumerable<string> EnumerateResultFiles()
    {
        if (Directory.Exists(_resultsPath))
        {
            foreach (var file in Directory.EnumerateFiles(_resultsPath, "backtest_*.json", SearchOption.TopDirectoryOnly))
                yield return file;
        }

        var workspacesPath = _settings.GetWorkspacesPath();
        if (!Directory.Exists(workspacesPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(
                     workspacesPath,
                     "backtest_*.json",
                     SearchOption.AllDirectories))
            yield return file;
    }

    private void CalculateWeeklyResults(BacktestingResult result)
    {
        var hourlyByWeek = result.HourlyResults
            .GroupBy(hr => GetWeekStart(hr.DateTime))
            .ToList();

        // Un solo passaggio su StrategyResults invece di un Where su TUTTA la lista per ogni
        // settimana: con 52 settimane erano 52 scansioni complete della serie, cioe' un costo
        // quadratico nella lunghezza del run pagato nella fase finale "WritingArtifacts".
        // Il criterio di appartenenza e' identico a prima — [weekStart, weekStart+6g] — incluso il
        // fatto che una riga della domenica dopo la mezzanotte non cade in nessuna settimana.
        var strategyResultsByWeek = new Dictionary<DateTime, List<StrategyHourlyResult>>();
        foreach (var strategyResult in result.StrategyResults)
        {
            var bucket = GetWeekStart(strategyResult.DateTime);
            if (strategyResult.DateTime > bucket.AddDays(6)) continue;
            if (!strategyResultsByWeek.TryGetValue(bucket, out var list))
                strategyResultsByWeek[bucket] = list = [];
            list.Add(strategyResult);
        }

        foreach (var weekGroup in hourlyByWeek)
        {
            var weekStart = weekGroup.Key;
            var weekEnd = weekStart.AddDays(6);
            var weekData = weekGroup.OrderBy(hr => hr.DateTime).ToList();

            var weeklyResult = new WeeklyResult
            {
                Year = weekStart.Year,
                Week = GetWeekNumber(weekStart),
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                WeeklyProfit = weekData.Last().Equity - weekData.First().Equity,
                WeeklyEquity = weekData.Last().Equity,
                WeeklyDrawdown = weekData.Max(hr => hr.Drawdown)
            };

            // Calcola win rate dai trade delle strategie
            List<StrategyHourlyResult> weekStrategyResults =
                strategyResultsByWeek.TryGetValue(weekStart, out var bucketed) ? bucketed : [];

            var profitableHours = weekStrategyResults.Count(sr => sr.Profit > 0);
            weeklyResult.TotalTrades = weekStrategyResults.Count(sr => sr.Signal.HasValue && sr.Signal != SignalType.Hold);
            weeklyResult.WinningTrades = profitableHours;
            weeklyResult.WinRate = weeklyResult.TotalTrades > 0 
                ? (decimal)weeklyResult.WinningTrades / weeklyResult.TotalTrades 
                : 0;

            result.WeeklyResults.Add(weeklyResult);
        }
    }

    /// <param name="strategyInfos">
    /// Elenco già deduplicato e ordinato, calcolato una volta sola dal chiamante: rigenerarlo con
    /// GroupBy+OrderBy a ogni barra costava più della valutazione delle strategie stesse.
    /// </param>
    /// <summary>
    /// Stato per strategia della serie di equity: identita' precalcolata + ultimo giorno scritto.
    /// </summary>
    private sealed class StrategyEquityTrack(
        Piootoo.Shared.Models.Backtesting.StrategyInfo info, string key, string code)
    {
        public Piootoo.Shared.Models.Backtesting.StrategyInfo Info { get; } = info;

        /// <summary>Chiave (Symbol, StrategyCode) normalizzata, calcolata una volta sola.</summary>
        public string Key { get; } = key;

        /// <summary>Codice strategia, calcolato una volta sola.</summary>
        public string Code { get; } = code;

        /// <summary>Giorno dell'ultima riga emessa, per il battito giornaliero. default = mai.</summary>
        public DateTime LastEmittedDay { get; set; }
    }

    /// <summary>
    /// Accoda la serie di equity per strategia, scrivendo <b>solo le righe che dicono qualcosa</b>.
    ///
    /// <para>Prima si scriveva una riga per strategia per barra, sempre: su un anno a 15 minuti con
    /// 26 strategie sono ~650.000 oggetti, ~100 MB di heap vivo di seconda generazione, con il
    /// backing array della lista in LOH. Non era solo memoria: a ogni barra si scriveva un
    /// riferimento fresco dentro un array enorme gia' promosso, quindi ogni raccolta gen0/gen1
    /// doveva scansionare sempre piu' card sporche. Il costo per barra cresceva con le barre gia'
    /// fatte — la ragione per cui il run rallentava andando avanti.</para>
    ///
    /// <para>Una riga viene emessa quando l'equity della strategia e' cambiata, quando c'e' un
    /// segnale, oppure — battito — al primo giro di ogni giornata di calendario. Le righe scartate
    /// sono quelle con <c>Profit = 0</c> e nessun segnale: ripetono l'equity gia' nota e non
    /// entrano in nessuna delle aggregazioni a valle, che sommano <c>Profit</c> o contano i segnali
    /// (<see cref="CalculateWeeklyResults"/>, AdvancedStrategyFilter, BasicStrategyFilter,
    /// PiootooOptimizationService, PiootooSapiooService). Il battito giornaliero serve proprio a
    /// loro: garantisce che ogni strategia resti presente in ogni settimana del run anche se non ha
    /// mai operato, cosi' le finestre "ultime N settimane" contano le settimane ferme come zero
    /// invece di saltarle.</para>
    /// </summary>
    private void AppendStrategyEquityResults(
        BacktestingResult result,
        TradingSnapshot snapshot,
        DateTime currentDate,
        IReadOnlyList<TradeSignal> signals,
        Dictionary<string, decimal> strategyEquityCache,
        StrategyEquityTrack[] tracks)
    {
        Dictionary<string, TradeSignal>? signalsByKey = null;
        if (signals.Count > 0)
        {
            signalsByKey = new Dictionary<string, TradeSignal>(signals.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var signal in signals)
                signalsByKey[MakeStrategyKey(signal.Symbol, GetSignalStrategyCode(signal))] = signal;
        }

        var day = currentDate.Date;

        foreach (var track in tracks)
        {
            var strategyKey = track.Key;
            TradeSignal? signal = null;
            signalsByKey?.TryGetValue(strategyKey, out signal);
            strategyEquityCache.TryGetValue(strategyKey, out var previousEquity);
            if (previousEquity == 0)
            {
                previousEquity = result.InitialCapital;
            }

            var equity = snapshot.StrategyEquities.TryGetValue(strategyKey, out var snapshotEquity)
                ? snapshotEquity
                : previousEquity;
            strategyEquityCache[strategyKey] = equity;

            var profit = equity - previousEquity;
            var isHeartbeat = track.LastEmittedDay != day;
            if (profit == 0m && signal is null && !isHeartbeat)
            {
                // Ripeterebbe l'equity gia' nota senza aggiungere informazione.
                continue;
            }

            track.LastEmittedDay = day;

            result.StrategyResults.Add(new StrategyHourlyResult
            {
                StrategyName = track.Info.Name,
                StrategyCode = track.Code,
                Symbol = track.Info.Symbol,
                DateTime = currentDate,
                Equity = equity,
                Profit = profit,
                Contracts = signal?.Quantity ?? 0m,
                Signal = signal?.Type,
                EntryPrice = signal?.Price
            });
        }
    }

    /// <summary>
    /// Il backtest interno è neutro rispetto agli account: nessuna conversione di simbolo,
    /// <c>ContractMultiplier</c> e <c>AccountBalanceScale</c> fissi a 1, quantità identica a quella
    /// dichiarata dalla strategia. I campi account restano nel contratto persistito perché il
    /// formato di <c>signals.json</c> è unico con quello prodotto dalle sessioni, dove invece la
    /// conversione è essenziale (vedi <c>docs/domini/account-e-conversione-symbol.md</c>).
    /// </summary>
    /// <param name="from">
    /// Indice da cui partire. Serve ai checkpoint incrementali, che accodano solo i segnali nati
    /// dall'ultimo giro: l'indice resta quello assoluto perche' e' lui a formare il SignalId, e
    /// due chiamate con <c>from</c> diversi devono produrre gli stessi id della chiamata completa.
    /// </param>
    private static IEnumerable<PersistedSignal> ToPersistedSignals(
        string jobId,
        IReadOnlyList<TradeSignal> signals,
        int from = 0)
    {
        for (var index = Math.Max(0, from); index < signals.Count; index++)
            yield return PersistedSignalMapper.FromTradeSignal(
                signals[index],
                signalId: $"{jobId}-signal-{index + 1:D10}",
                correlationId: jobId);
    }

    /// <inheritdoc cref="ToPersistedSignals" path="/param[@name='from']"/>
    private static IEnumerable<PersistedTrade> ToPersistedTrades(
        string jobId,
        IReadOnlyList<TradingResult> trades,
        int from = 0)
    {
        for (var index = Math.Max(0, from); index < trades.Count; index++)
            yield return ToPersistedTrade(jobId, trades[index], index);
    }

    private static PersistedTrade ToPersistedTrade(string jobId, TradingResult trade, int index) =>
        new()
        {
            TradeId = $"{jobId}-trade-{index + 1:D10}",
            CorrelationId = jobId,
            StrategyCode = trade.StrategyCode,
            StrategyName = trade.StrategyName,
            Symbol = NormalizeSymbol(trade.Symbol),
            Direction = trade.Direction,
            Quantity = trade.Quantity,
            EntryTimeUtc = TradingDateTime.ToFeedUtc(trade.EntryDate),
            ExitTimeUtc = TradingDateTime.ToFeedUtc(trade.ExitDate),
            EntryPrice = trade.EntryPrice,
            ExitPrice = trade.ExitPrice,
            ExitReason = trade.ExitReason.ToString(),
            GrossProfit = trade.GrossProfit,
            NetProfit = trade.NetProfit,
            Commission = trade.Commission
        };

    /// <summary>
    /// Applica al segnale la parola finale del piano: cio' che il conto non concede diventa una
    /// deadline sul segnale — il flat di sessione se vieta l'overnight, l'apertura della finestra
    /// del fine settimana se vieta l'overweek — salvo che la strategia ne dichiari gia' una piu'
    /// stretta.
    ///
    /// <para>E' lo stesso <see cref="HoldingResolver"/> che la sessione chiama sul percorso live:
    /// un'unica composizione per i due motori, altrimenti backtest e conto vero tornano a tagliare
    /// in istanti diversi — che e' la classe di divergenza che questa gerarchia chiude.</para>
    ///
    /// <para>Il cortocircuito guarda <b>entrambi</b> i permessi: guardava solo l'overnight, e da
    /// quando anche il fine settimana e' una deadline quel ramo saltava proprio il caso piu' comune
    /// (overnight libero, overweek vietato) lasciando il taglio al solo <c>IsFlatTrigger</c> del
    /// loop.</para>
    /// </summary>
    private static void ApplyAccountHolding(TradeSignal signal, AccountHoldingPolicy holding)
    {
        if (holding.AllowOvernight && holding.AllowOverweek) return;

        var decision = HoldingResolver.Resolve(
            signal.CloseAtUtc, signal.ValidFromUtc ?? signal.Date, holding);
        signal.CloseAtUtc = decision.AtUtc;
        signal.TimeExitFromAccountPolicy = decision.FromAccountPolicy;
    }

    /// <summary>
    /// Restringe le strategie del masterfilter a quelle che il piano dichiarato dal run lascia
    /// operare: prima toglie quelle che il piano tiene spente, poi quelle il cui simbolo non
    /// compare, abilitato, nella tabella di conversione del suo <b>broker</b>.
    ///
    /// <para>Senza piano (il run neutro) non tocca niente e restituisce l'elenco intero: il
    /// backtest resta la misura delle strategie, non del conto con cui verrebbero operate.</para>
    ///
    /// <para><b>Le due esclusioni restano separate.</b> Producono lo stesso effetto — la strategia
    /// non gira — da cause opposte: una scelta operativa reversibile, contro uno strumento che quel
    /// broker non opera. Sommarle manderebbe a cercare la tabella di conversione per una strategia
    /// che qualcuno ha semplicemente spento.</para>
    ///
    /// <para>Un piano <i>senza</i> broker — quelli scritti prima dell'anagrafica — non restringe i
    /// simboli e vale come universo neutro; le sue strategie spente si applicano lo stesso. Un
    /// broker che il registro non conosce fa invece fallire il run, come il datafeed mancante.</para>
    /// </summary>
    private (List<StrategyDefinition> Strategies,
             IReadOnlyList<string> ExcludedByBroker,
             IReadOnlyList<string> DisabledByPlan,
             BacktestPlanUniverse Universe)
        ApplyPlanUniverse(List<StrategyDefinition> strategies, TradingPlan? plan)
    {
        if (plan is null)
            return (strategies, [], [], new BacktestPlanUniverse { AppliedAsNeutralUniverse = true });

        // Le spente il piano le nomina per Id di catalogo — il nome della classe — perche' li' si
        // sta selezionando dal catalogo come fa il masterfilter, non nominando esecuzioni
        // (CLAUDE.md, «Id ≠ Name»). Nel summary finisce invece il nome di esecuzione, che e' quello
        // che si ritrova in signals.json e trades.json.
        var disabledIds = plan.DisabledStrategies.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var disabledByPlan = new List<string>();
        if (disabledIds.Count > 0)
        {
            var kept = new List<StrategyDefinition>(strategies.Count);
            foreach (var strategy in strategies)
            {
                if (disabledIds.Contains(strategy.Id)) disabledByPlan.Add(strategy.Name);
                else kept.Add(strategy);
            }

            disabledByPlan.Sort(StringComparer.OrdinalIgnoreCase);
            strategies = kept;
        }

        var brokerCode = string.IsNullOrWhiteSpace(plan.BrokerCode) ? null : plan.BrokerCode.Trim();
        if (brokerCode is null)
        {
            return (strategies, [], disabledByPlan, new BacktestPlanUniverse
            {
                PlanCode = plan.Code,
                AppliedAsNeutralUniverse = true
            });
        }

        var workspaces = _workspaces ?? throw new InvalidOperationException(
            "Il run dichiara un piano ma il servizio di backtesting non ha il registro delle " +
            "anagrafiche: non puo' risolvere la tabella di conversione del suo broker.");

        var broker = workspaces.FindBroker(brokerCode)
            ?? throw new InvalidOperationException(
                $"Il piano '{plan.Code}' dichiara il broker '{brokerCode}', che non e' nel registro: " +
                "il run non puo' applicarne l'universo operativo.");

        var conversionCode = string.IsNullOrWhiteSpace(broker.SymbolConversionCode)
            ? null
            : broker.SymbolConversionCode.Trim();
        var table = workspaces.ResolveSymbolConversion(conversionCode);
        var mappings = table.Mappings ?? [];
        var conversion = AccountSymbolConversion.FromTable(table);

        // Un broker che DICHIARA una tabella e ne risolve zero righe non e' il broker neutro: e' una
        // configurazione rotta. Distinguerli conta perche' SupportsSymbol ammette tutto quando la
        // tabella e' vuota, quindi i due casi producevano lo stesso artefatto — nessuna esclusione —
        // con run completamente diversi. In compare-0017 lo stesso file su disco ha dato tre
        // esclusioni diverse in tre run senza che niente lo segnalasse. Vale la regola del broker
        // inesistente sul datafeed: si fallisce all'avvio, non si ripiega in silenzio.
        if (conversionCode is not null && !conversion.HasSymbolTable)
        {
            throw new InvalidOperationException(
                $"Il broker '{brokerCode}' del piano '{plan.Code}' dichiara la tabella di conversione " +
                $"'{conversionCode}' ma non contiene nessun simbolo. Senza tabella il run ammetterebbe " +
                "ogni simbolo del masterfilter, compresi quelli che quel broker non opera: e' un run " +
                "che descrive un conto che non esiste. Correggi la tabella, oppure togli il piano " +
                "dalla richiesta per eseguire il masterfilter intero come run neutro.");
        }

        var universe = new BacktestPlanUniverse
        {
            PlanCode = plan.Code,
            BrokerCode = brokerCode,
            SymbolConversionCode = conversionCode,
            MappedSymbols = mappings.Count,
            EnabledSymbols = mappings.Count(mapping => mapping.Enabled),
            AppliedAsNeutralUniverse = !conversion.HasSymbolTable
        };

        if (!conversion.HasSymbolTable)
            return (strategies, [], disabledByPlan, universe);

        var supported = new List<StrategyDefinition>(strategies.Count);
        var excluded = new List<string>();
        foreach (var strategy in strategies)
        {
            if (conversion.SupportsSymbol(strategy.Symbol)) supported.Add(strategy);
            else excluded.Add(strategy.Name);
        }

        excluded.Sort(StringComparer.OrdinalIgnoreCase);
        return (supported, excluded, disabledByPlan, universe);
    }

    private static TradeSignal CloneTradeSignal(TradeSignal signal)
    {
        var clone = new TradeSignal
        {
            Date = signal.Date,
            Type = signal.Type,
            Price = signal.Price,
            Symbol = signal.Symbol,
            StrategyCode = signal.StrategyCode,
            StrategyName = signal.StrategyName,
            Reason = signal.Reason,
            Quantity = signal.Quantity,
            QuantityBeforeAccountConversion = signal.QuantityBeforeAccountConversion,
            OrderType = signal.OrderType,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            CloseAtUtc = signal.CloseAtUtc,
            TimeExitFromAccountPolicy = signal.TimeExitFromAccountPolicy,
            StopLossMoneyPerFutureContract = signal.StopLossMoneyPerFutureContract,
            TakeProfitMoneyPerFutureContract = signal.TakeProfitMoneyPerFutureContract,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            BreakEven = signal.BreakEven,
            BreakEvenMoneyPerFutureContract = signal.BreakEvenMoneyPerFutureContract,
            TrailingStopMoneyPerFutureContract = signal.TrailingStopMoneyPerFutureContract,
            MaxBarsInPosition = signal.MaxBarsInPosition,
            // Da qui in giu' sono i campi che PersistedSignalMapper legge e che il clone lasciava
            // indietro: il segnale eseguito li aveva, signals.json no. TimeframeMinutes usciva 0 su
            // tutti i segnali di ogni run, e 0 e' proprio il valore che fa concludere a chi legge
            // che un pending muore dopo un tick invece che dopo la propria barra — un'ora di
            // indagine sprecata in compare-0022. Il clone serve solo a persistere: aggiungere
            // verita' qui non cambia nessuna esecuzione.
            TimeframeMinutes = signal.TimeframeMinutes,
            MaxEntriesPerSession = signal.MaxEntriesPerSession,
            EntrySessionStartUtc = signal.EntrySessionStartUtc,
            TimeExitOnlyIfProfitBelowMoneyPerContract = signal.TimeExitOnlyIfProfitBelowMoneyPerContract,
            ProfitStallAfterUtc = signal.ProfitStallAfterUtc
        };
        TradingDateTime.NormalizeSignalToUtc(clone);
        return clone;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0) daysToSubtract += 7;
        return date.AddDays(-daysToSubtract).Date;
    }

    // Le chiavi di strategia stanno in StrategyKeys: le usano anche i report, e due copie della
    // stessa regola avrebbero raggruppato gli stessi trade in modo diverso. Qui restano solo gli
    // inoltri, per non toccare le decine di chiamate del loop.
    private static string NormalizeSymbol(string symbol) => StrategyKeys.NormalizeSymbol(symbol);

    private static string NormalizeSymbolWithPrefix(string symbol) => StrategyKeys.NormalizeSymbolWithPrefix(symbol);

    private static string MakeStrategyKey(string symbol, string strategyCode)
        => StrategyKeys.MakeStrategyKey(symbol, strategyCode);

    private static string ExtractSymbol(string strategyKey)
    {
        var parts = strategyKey.Split('|', 2);
        return parts.Length == 2 ? parts[0] : string.Empty;
    }

    private static string ExtractStrategyCode(string strategyKey)
    {
        var parts = strategyKey.Split('|', 2);
        return parts.Length == 2 ? parts[1] : strategyKey;
    }

    private static string GetSignalStrategyCode(TradeSignal signal)
    {
        return !string.IsNullOrWhiteSpace(signal.StrategyCode)
            ? signal.StrategyCode
            : signal.StrategyName;
    }

    private static string GetStrategyCode(StrategyHourlyResult result) => StrategyKeys.CodeOf(result);

    private static string GetStrategyCode(Piootoo.Shared.Models.Backtesting.StrategyInfo result)
        => StrategyKeys.CodeOf(result);

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    /// <summary>
    /// Determina se una strategia deve essere valutata all'iterazione corrente
    /// Una strategia viene valutata quando il numero di iterazioni è un multiplo del rapporto tra il suo timeframe e il minimo
    /// </summary>
    private bool ShouldEvaluateStrategy(DateTime currentDate, int iterationCount, int strategyTimeframeMinutes, int minTimeframeMinutes)
    {
        // Se il timeframe della strategia è uguale al minimo, valuta sempre
        if (strategyTimeframeMinutes == minTimeframeMinutes)
        {
            return true;
        }

        // Verifica se il timeframe della strategia è un multiplo del minimo
        if (strategyTimeframeMinutes % minTimeframeMinutes != 0)
        {
            // Niente log qui: e' un percorso chiamato per strategia per barra e Console.Out e'
            // sincrono e serializzato. La condizione e' statica per strategia, quindi va segnalata
            // una volta sola in fase di setup, non dentro il loop.
            return false;
        }

        // Strategie daily+ : allinea alla mezzanotte UTC (evita drift da iterazioni weekend)
        if (strategyTimeframeMinutes >= 1440)
        {
            return currentDate is { Hour: 0, Minute: 0 };
        }

        // Calcola quanti periodi minimi corrispondono a un periodo della strategia
        var multiplier = strategyTimeframeMinutes / minTimeframeMinutes;
        
        // Valuta la strategia quando il numero di iterazioni è un multiplo del multiplier
        var shouldEvaluate = iterationCount % multiplier == 0;
        
        return shouldEvaluate;
    }

    private static IEnumerable<(string Symbol, int Timeframe, int RequiredCandles)> GetStrategyDataRequirements(ITradingStrategy strategy)
    {
        yield return (strategy.Symbol, strategy.TimeframeMinutes, (int)(strategy.RequiredCandles * 1.2));

        if (strategy is not IMultiTimeframeTradingStrategy multiTimeframeStrategy)
        {
            yield break;
        }

        foreach (var timeframe in multiTimeframeStrategy.AdditionalTimeframes.Where(timeframe => timeframe != strategy.TimeframeMinutes))
        {
            var requiredCandles = timeframe >= 1440 ? 8 : (int)(strategy.RequiredCandles * 1.2);
            yield return (strategy.Symbol, timeframe, requiredCandles);
        }
    }

    /// <summary>
    /// Stream aggiuntivi per le strategie multi-timeframe. Come per il timeframe primario si usa
    /// un cursore e si restituisce solo la coda necessaria: la versione precedente ricostruiva
    /// l'intero prefisso della serie a ogni barra.
    /// </summary>
    private static IReadOnlyDictionary<int, OhlcvData[]> GetAdditionalTimeframeData(
        IMultiTimeframeTradingStrategy strategy,
        Dictionary<(string Symbol, int Timeframe), CandleWindowCursor> cursors,
        DateTime currentDate)
    {
        var result = new Dictionary<int, OhlcvData[]>();
        var symbol = NormalizeSymbol(strategy.Symbol);

        foreach (var timeframe in strategy.AdditionalTimeframes.Where(timeframe => timeframe != strategy.TimeframeMinutes))
        {
            if (!cursors.TryGetValue((symbol, timeframe), out var cursor))
            {
                result[timeframe] = Array.Empty<OhlcvData>();
                continue;
            }

            var required = timeframe >= 1440 ? 8 : (int)(strategy.RequiredCandles * 1.2);
            result[timeframe] = cursor.Window(currentDate, required);
        }

        return result;
    }

    /// <summary>
    /// Una barra appartiene al tick corrente se è stata chiusa dentro l'intervallo che il tick
    /// rappresenta.
    ///
    /// <para>Serve perché l'orologio del loop è una griglia regolare mentre il feed ha buchi
    /// (pausa di sessione, festivi, giornate corte): sui tick vuoti il cursore restituisce
    /// l'ultima barra disponibile, indistinguibile da una fresca. Usarla per far scattare uno
    /// stop significa eseguire su un intervallo di cui non conosciamo i prezzi, e siccome il
    /// livello di un breakout coincide spesso con l'estremo della barra che lo ha generato, il
    /// fill risulta a un prezzo mai scambiato.</para>
    /// </summary>
    private static bool BelongsToCurrentTick(DateTime barTimeUtc, DateTime currentDate, int tickMinutes) =>
        (currentDate - barTimeUtc).TotalMinutes < Math.Max(1, tickMinutes);

    /// <summary>
    /// Se il loop deve saltare questo tick perche' il conto e' dentro la finestra di flat del fine
    /// settimana.
    ///
    /// <para><b>Non e' piu' il calendario.</b> Fino al 06/09/2026 il loop saltava sabato e domenica
    /// UTC per ogni run. Ma la sessione della ricerca e' il giorno di calendario <b>europeo</b>: la
    /// riapertura del lunedi' cade alle 22:00 (ora legale) o 23:00 (ora solare) UTC di
    /// <b>domenica</b>, quindi quel salto toglieva al motore l'apertura di ogni lunedi' e, su un
    /// mercato che quota 24/7 come BTC, due giorni pieni a settimana.</para>
    ///
    /// <para>Resta il salto quando e' il <b>piano</b> a vietare l'overweek: li' il conto deve essere
    /// piatto e senza ordini fino alla riapertura, quindi non c'e' niente da valutare. Il tick su
    /// cui il flat <b>scatta</b> non si salta — e' quello che chiude le posizioni e cancella i
    /// pending — e con <c>AllowOverweek</c> il fine settimana si percorre tutto, che e' la
    /// condizione dei run di parita' con la ricerca.</para>
    /// </summary>
    public static bool IterationIsSkippedByWeekEndFlat(
        AccountHoldingPolicy holding, DateTime instantUtc, int tickMinutes)
    {
        if (holding.AllowOverweek)
            return false;

        var weekEnd = holding.WeekEnd;
        return weekEnd.IsInsideWindow(instantUtc) &&
               !weekEnd.IsFlatTrigger(instantUtc, instantUtc.AddMinutes(-Math.Max(1, tickMinutes)));
    }

    /// <summary>
    /// Se l'ultima candela della strategia e' troppo vecchia perche' valutarla abbia senso.
    ///
    /// <para><b>Intraday.</b> Dentro un buco del feed — pausa di sessione, festivo, fine settimana —
    /// il cursore restituisce sempre la stessa ultima barra chiusa, quindi la strategia verrebbe
    /// rivalutata a ogni tick dell'orologio su input identici e riemetterebbe lo stesso segnale
    /// decine di volte. Quei doppioni non producono trade (nascono gia' scaduti e
    /// <c>ProcessSignals</c> li scarta) ma finiscono in <c>signals.json</c> e costano un giro di
    /// motore ciascuno: da quando il loop non salta piu' il fine settimana sarebbero circa
    /// quarantanove ore di tick a vuoto la settimana, per ogni strategia.</para>
    ///
    /// <para>La soglia e' <b>due barre della strategia</b>, non una: <c>ShouldEvaluateStrategy</c>
    /// allinea per conteggio di iterazioni, non sulla griglia della serie, quindi una strategia a
    /// 4 ore su orologio da 15 minuti puo' legittimamente essere valutata qualche tick dopo
    /// l'apertura della propria barra. Un margine di una barra piena lascia passare quel caso e
    /// taglia solo i buchi veri.</para>
    ///
    /// <para><b>Daily e oltre.</b> Restano a giorni di calendario: li' <c>ShouldEvaluateStrategy</c>
    /// impone la mezzanotte UTC, che con la barra ancorata alla mezzanotte europea cade gia'
    /// un'ora dopo l'apertura, e una soglia in barre le spegnerebbe tutte.</para>
    /// </summary>
    /// <summary>
    /// Passa una serie caricata attraverso il layer e ne riporta la struttura di sessione nel
    /// riepilogo, segnalando fra i <c>diagnostics</c> le due anomalie che contano.
    ///
    /// <para><b>Barre fuori griglia.</b> La serie e' nata su un ancoraggio diverso da quello
    /// dichiarato per il simbolo. Non produce barre malformate: produce barre <i>diverse</i>, e un
    /// backtest ci gira sopra senza accorgersene. Nel caso peggiore visto — <c>@KC_240</c> —
    /// il file conteneva DUE griglie, 880 barre ciascuna, perche' la deduplica e' sull'istante di
    /// apertura e due raccolte con ancoraggi diversi si sommano invece di sovrascriversi.</para>
    ///
    /// <para><b>Barre in giorni senza sessione.</b> Un feed CFD quota anche quando il future e'
    /// chiuso. Quelle barre non generano trade ma spezzano la sessione, e con l'uscita di fine
    /// sessione chiudono posizioni ancora valide: il dossier lo misura sul DAX all'11% del
    /// P&amp;L.</para>
    ///
    /// <para><b>Non ferma il run.</b> Un simbolo senza calendario verificato non e' un errore qui —
    /// lo e' gia' altrove, dove serve il <c>PointValue</c> — e il feed del vendor ha una barra
    /// fuori griglia all'anno per ogni file giornaliero (la domenica in cui l'orologio torna
    /// indietro). Trasformare questo controllo in un blocco fermerebbe run che oggi girano, e va
    /// fatto quando i feed saranno rigenerati dal minuto, non prima.</para>
    /// </summary>
    private static BacktestFeedCalendarSummary? DescribeFeedCalendar(
        string symbol,
        int timeframeMinutes,
        OhlcvData[] candles,
        BacktestDiagnosticsLogger diagnostics)
    {
        if (candles.Length == 0 || !SessionGrid.DividesTheDay(timeframeMinutes))
            return null;

        if (!MarketCalendarRegistry.Current.TryGet(symbol, out var calendar))
            return null;

        FeedCalendarReport report;
        try
        {
            report = FeedCalendarReport.Analyze(calendar, timeframeMinutes, candles);
        }
        catch (Exception failure)
        {
            // Una serie non ordinata fa lanciare il segmentatore. E' una diagnosi, non un
            // requisito del run: si segnala e si prosegue.
            diagnostics.AddRunDiagnostic(
                $"[calendario] {symbol}/{timeframeMinutes}m: struttura di sessione non calcolabile " +
                $"({failure.Message}).");
            return null;
        }

        if (report.BarsOffGrid > 0)
        {
            diagnostics.AddRunDiagnostic(
                $"[calendario] {symbol}/{timeframeMinutes}m: {report.BarsOffGrid} barre su {report.Bars} " +
                $"NON stanno sulla griglia dichiarata ({report.Grid}). Il feed e' nato su un " +
                "ancoraggio diverso da quello su cui le strategie sono state trovate: le barre non " +
                "sono malformate, sono altre, e il run gira su una serie che non corrisponde a " +
                "nessun run di ricerca.");
        }

        if (report.BarsOnNonSessionDay > 0)
        {
            diagnostics.AddRunDiagnostic(
                $"[calendario] {symbol}/{timeframeMinutes}m: {report.BarsOnNonSessionDay} barre su " +
                $"{report.Bars} cadono in giorni in cui {symbol} non ha sessione. Sono sessioni che " +
                "il feed fabbrica e il future non ha: non generano trade, ma spezzano la sessione e " +
                "con l'uscita di fine sessione chiudono posizioni ancora valide.");
        }

        return new BacktestFeedCalendarSummary
        {
            Grid = report.Grid,
            Sessions = report.Sessions,
            BarsOffGrid = report.BarsOffGrid,
            BarsOnNonSessionDay = report.BarsOnNonSessionDay,
            StaleBars = report.StaleBars,
            Gaps = report.Gaps,
            FirstSessionId = report.FirstSessionId,
            LastSessionId = report.LastSessionId,
            SessionDaysDeclared = report.SessionDaysDeclared
        };
    }

    private static bool IsStrategyCandleStale(int timeframeMinutes, DateTime lastCandleTime, DateTime currentDate)
    {
        if (timeframeMinutes < 1440)
        {
            return timeframeMinutes > 0 &&
                   (currentDate - lastCandleTime).TotalMinutes >= 2.0 * timeframeMinutes;
        }

        var maxAgeDays = timeframeMinutes >= 10080 ? 10 : 4;
        return (currentDate.Date - lastCandleTime.Date).TotalDays > maxAgeDays;
    }

    private static void ScaleSignalMaxBarsInPosition(TradeSignal signal, int strategyTimeframeMinutes, int minTimeframeMinutes)
    {
        if (!signal.MaxBarsInPosition.HasValue || signal.MaxBarsInPosition.Value <= 0)
        {
            return;
        }

        if (strategyTimeframeMinutes <= minTimeframeMinutes)
        {
            return;
        }

        if (strategyTimeframeMinutes % minTimeframeMinutes != 0)
        {
            return;
        }

        var scale = strategyTimeframeMinutes / minTimeframeMinutes;
        signal.MaxBarsInPosition = signal.MaxBarsInPosition.Value * scale;
    }

    /// <summary>
    /// Popola StrategiesInfo cercando le strategie nel repository basandosi sui nomi
    /// </summary>
    private List<Piootoo.Shared.Models.Backtesting.StrategyInfo> PopulateStrategiesInfo(List<string> strategyNames)
    {
        var strategiesInfo = new List<Piootoo.Shared.Models.Backtesting.StrategyInfo>();
        
        if (strategyNames == null || !strategyNames.Any())
            return strategiesInfo;

        // Ottieni tutte le strategie C# registrate disponibili
        var allStrategies = StrategyFactory.GetRegisteredStrategies();
        
        // Per ogni nome strategia, cerca corrispondenze nel repository
        var uniqueStrategyNames = strategyNames.Distinct().ToList();
        foreach (var strategyName in uniqueStrategyNames)
        {
            // Cerca strategie che corrispondono al nome (case-insensitive)
            var matchingStrategies = allStrategies
                .Where(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (matchingStrategies.Any())
            {
                // Aggiungi tutte le varianti (potrebbero esserci più strategie con lo stesso nome ma symbol/timeframe diversi)
                foreach (var strategy in matchingStrategies)
                {
                    strategiesInfo.Add(new Piootoo.Shared.Models.Backtesting.StrategyInfo
                    {
                        Name = strategy.Name,
                        StrategyCode = strategy.Name,
                        Symbol = strategy.Symbol.Replace("@", ""), // Rimuovi @ per consistenza
                        TimeframeMinutes = strategy.TimeframeMinutes
                    });
                }
            }
            else
            {
                // Se non trovata, aggiungi comunque con informazioni vuote
                strategiesInfo.Add(new Piootoo.Shared.Models.Backtesting.StrategyInfo
                {
                    Name = strategyName,
                    StrategyCode = strategyName,
                    Symbol = "",
                    TimeframeMinutes = 0
                });
            }
        }
        
        // Rimuovi duplicati (stesso nome, symbol e timeframe)
        return strategiesInfo
            .DistinctBy(s => new { s.Name, s.Symbol, s.TimeframeMinutes })
            .ToList();
    }
}
