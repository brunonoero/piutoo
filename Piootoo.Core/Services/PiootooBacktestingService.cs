using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

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

    private readonly IPiootooSettingsService _settingsService;
    private readonly IPiootooDataFeedService _dataFeedService;
    private readonly IBacktestingExecutionHook _executionHook;

    /// <summary>
    /// Serve solo alla modalità <see cref="TitanoFilterMode.BacktestRotationFile"/>. Opzionale: un
    /// backtest senza filtro non deve dipendere da Titano.
    /// </summary>
    private readonly TitanoRotationService? _titano;

    /// <summary>
    /// Serve solo a risolvere la tabella di conversione dell'account indicato in
    /// <see cref="BacktestingRequest.AccountId"/>. Opzionale: senza account il run è 1 a 1.
    /// </summary>
    private readonly WorkspaceService? _workspaces;
    private readonly PiootooSettings _settings;
    private readonly string _resultsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PiootooBacktestingService(
        IPiootooSettingsService settingsService,
        IPiootooDataFeedService dataFeedService,
        PiootooSettings settings,
        IBacktestingExecutionHook executionHook,
        TitanoRotationService? titano = null,
        WorkspaceService? workspaces = null)
    {
        _settingsService = settingsService;
        _dataFeedService = dataFeedService;
        _executionHook = executionHook;
        _titano = titano;
        _workspaces = workspaces;
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
        _ = Task.Run(() => ExecuteBacktesting(job, request, outputPath, cancellation.Token));

        return job.JobId;
        }
        catch
        {
            _activeOutputPaths.TryRemove(outputPath, out _);
            throw;
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
            var minTimeframeMinutes = strategyInstances.Min(s => s.TimeframeMinutes);
            Console.WriteLine($"Timeframe minimo calcolato: {minTimeframeMinutes} minuti per {strategyInstances.Count} strategie");

            Directory.CreateDirectory(outputPath);
            var tradingJsonStore = new TradingJsonStore(outputPath);
            tradingJsonStore.Initialize();

            // Un motore di trading PER JOB: PiootooTradingService è mutabile e non thread-safe,
            // condividerlo tra backtest concorrenti mescolerebbe posizioni e trade.
            var tradingService = new PiootooTradingService();
            tradingService.Initialize(request.InitialCapital, request.CommissionPerContract);

            diagnostics = new BacktestDiagnosticsLogger(outputPath, job.JobId);
            tradingService.PositionOpened = diagnostics.LogEntry;
            tradingService.PositionClosed = diagnostics.LogExit;
            foreach (var (definition, instance) in createdStrategies)
            {
                diagnostics.RegisterStrategy(instance.Name, definition.Name, instance.Symbol, instance.TimeframeMinutes);
            }

            diagnostics.LogRun("avvio job", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workspaceId"] = request.WorkspaceId,
                ["backtestFolder"] = request.BacktestFolderName,
                ["startUtc"] = request.StartDate.ToString("O"),
                ["endUtc"] = request.EndDate.ToString("O"),
                ["initialCapital"] = request.InitialCapital.ToString(CultureInfo.InvariantCulture),
                ["commissionPerContract"] = request.CommissionPerContract.ToString(CultureInfo.InvariantCulture),
                ["minTimeframeMinutes"] = minTimeframeMinutes.ToString(),
                ["strategies"] = strategyInstances.Count.ToString(),
                ["closeAllPositionsAtWeekEnd"] = request.CloseAllPositionsAtWeekEnd ? "true" : "false"
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
                StrategiesUsed = createdStrategies.Select(item => item.Definition.Name).ToList(),
                StrategiesInfo = createdStrategies.Select(item => new Piootoo.Shared.Models.Backtesting.StrategyInfo
                {
                    Name = item.Definition.Name,
                    // StrategyCode è il codice di ESECUZIONE (ITradingStrategy.Name), lo stesso che
                    // finisce nei segnali, nei trade e nelle chiavi di posizione. Usare qui l'Id di
                    // classe rompeva ogni join a valle: equity per strategia piatta, zero trade nel
                    // report, Titano senza dati. Vedi docs/PROGETTO.md §3.2.
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
            var emittedTradeSignals = new List<TradeSignal>();

            // Tabella di conversione dell'account risolta una volta sola: dentro il loop serve solo
            // un lookup su dizionario per simbolo.
            var accountConversion = ResolveAccountConversion(request);
            if (!accountConversion.IsIdentity)
                diagnostics.LogRun(
                    $"Conversione account '{accountConversion.AccountName}' attiva sul run.",
                    new Dictionary<string, string>
                    {
                        ["accountId"] = accountConversion.AccountId,
                        ["initialBalance"] = accountConversion.InitialBalance.ToString("F2")
                    });

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

            Console.WriteLine($"[Backtesting] Pre-caricamento {uniqueDataSources.Count} datasource unici...");

            var emptyDataSources = new List<string>();
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
                    ds.Timeframe);

                var cursor = new CandleWindowCursor(candles);
                var normalizedSymbol = NormalizeSymbol(ds.Symbol);
                cursors[(normalizedSymbol, ds.Timeframe)] = cursor;

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

                diagnostics.LogDataSource(new BacktestDataSourceSummary
                {
                    Symbol = normalizedSymbol,
                    TimeframeMinutes = ds.Timeframe,
                    CandleCount = candles.Length,
                    FirstBarUtc = cursor.FirstBarUtc,
                    LastBarUtc = cursor.LastBarUtc,
                    CoversRequestedRange = coversRange,
                    Warning = warning
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
                    "Datafeed mancante per: " + string.Join(", ", emptyDataSources) +
                    ". Scarica i file corrispondenti in piootoo-repository/datafeed oppure rimuovi " +
                    "dal masterfilter le strategie su queste coppie simbolo/timeframe.");
            }
            // ========== FINE PREFILL ==========

            // Filtro Titano del run: null in modalità Disabled.
            var titanoFilter = CreateTitanoFilter(request);

            // Iterazione usando il timeframe minimo
            var currentDate = roundedStartDate;
            var totalMinutes = (int)(request.EndDate - roundedStartDate).TotalMinutes;
            var totalIterations = totalMinutes > 0 ? totalMinutes / minTimeframeMinutes : 0;
            var processedIterations = 0;
            var iterationCount = 0; // Contatore per calcolare l'allineamento delle strategie
            var markedToMarketBars = 0L;
            var weekEndCancelledOrders = 0L;
            var lastPersistedIteration = 0;

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
                // Skip weekend
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
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

                // Filtro Titano della barra corrente. In Disabled resta null e vengono valutate tutte
                // le strategie del masterfilter; in BacktestRotationFile contiene i codici abilitati
                // dal periodo di rotazione che copre questa barra.
                var titanoEnabled = titanoFilter?.EnabledCodesAt(currentDate);

                foreach (var strategy in strategyInstances)
                {
                    var strategySymbol = strategy.Symbol;
                    var strategyCode = strategy.Name;

                    if (titanoEnabled is not null &&
                        !titanoEnabled.Contains(strategyCode, StringComparer.OrdinalIgnoreCase))
                    {
                        // Disabilitata dalla rotazione su questo periodo: non è uno skip tecnico,
                        // è una decisione. Non viene contata tra le valutazioni mancate.
                        continue;
                    }

                    var titanoAllocation = titanoFilter?.AllocationFor(strategyCode) ?? 1m;

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
                                : new Dictionary<int, OhlcvData[]>()
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
                        signal.Quantity *= titanoAllocation;

                        // La conversione dell'account scala la size prima che il motore veda il
                        // segnale, così trade ed equity riflettono i contratti effettivi.
                        if (!TryApplyAccountConversion(signal, accountConversion))
                        {
                            diagnostics.LogAnomaly(
                                $"Segnale scartato: il symbol '{strategySymbol}' non è operativo sull'account " +
                                $"'{accountConversion.AccountName}' o la size convertita è nulla.",
                                currentDate, strategyCode, strategySymbol);
                            continue;
                        }

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
                                companion.Quantity *= titanoAllocation;
                                if (!TryApplyAccountConversion(companion, accountConversion))
                                    continue;
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
                AppendStrategyEquityResults(result, snapshot, currentDate, signals, strategyEquityCache, orderedStrategyInfos);

                var nextTradingDate = GetNextTradingDateUtc(currentDate, minTimeframeMinutes);
                if (request.CloseAllPositionsAtWeekEnd &&
                    IsLastBarOfTradingWeek(currentDate, nextTradingDate))
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
                        AppendStrategyEquityResults(result, snapshot, currentDate, signals, strategyEquityCache, orderedStrategyInfos);
                    }
                }

                // Checkpoint periodico invece di una riscrittura completa (con fsync) a ogni barra:
                // era la voce di costo dominante dell'intero backtest.
                if (processedIterations - lastPersistedIteration >= PersistCheckpointBars)
                {
                    lastPersistedIteration = processedIterations;
                    tradingJsonStore.WriteSignals(ToPersistedSignals(job.JobId, emittedTradeSignals, accountConversion), durable: false);
                    tradingJsonStore.WriteTrades(ToPersistedTrades(job.JobId, tradingService.GetClosedTrades()), durable: false);
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
            GenerateStrategyEquityHtmlReport(result, closedTrades, htmlReportPath);
            result.HtmlReportFilePath = htmlReportPath;

            // Scrittura autorevole: qui sì, durabile.
            tradingJsonStore.WriteSignals(ToPersistedSignals(job.JobId, emittedTradeSignals, accountConversion));
            tradingJsonStore.WriteTrades(ToPersistedTrades(job.JobId, closedTrades));
            result.TradeSignalsFilePath = tradingJsonStore.SignalsPath;
            result.ResultFilePath = filePath;

            if (weekEndCancelledOrders > 0)
                diagnostics.LogRun(
                    $"Flat settimanale: {weekEndCancelledOrders} ordini pendenti cancellati sull'ultima barra della settimana.",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["weekEndCancelledOrders"] = weekEndCancelledOrders.ToString(CultureInfo.InvariantCulture)
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
            var weekStrategyResults = result.StrategyResults
                .Where(sr => sr.DateTime >= weekStart && sr.DateTime <= weekEnd)
                .ToList();

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
    private void AppendStrategyEquityResults(
        BacktestingResult result,
        TradingSnapshot snapshot,
        DateTime currentDate,
        IReadOnlyList<TradeSignal> signals,
        Dictionary<string, decimal> strategyEquityCache,
        IReadOnlyList<Piootoo.Shared.Models.Backtesting.StrategyInfo> strategyInfos)
    {
        Dictionary<string, TradeSignal>? signalsByKey = null;
        if (signals.Count > 0)
        {
            signalsByKey = new Dictionary<string, TradeSignal>(signals.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var signal in signals)
                signalsByKey[MakeStrategyKey(signal.Symbol, GetSignalStrategyCode(signal))] = signal;
        }

        foreach (var strategyInfo in strategyInfos)
        {
            var strategyKey = MakeStrategyKey(strategyInfo.Symbol, GetStrategyCode(strategyInfo));
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

            result.StrategyResults.Add(new StrategyHourlyResult
            {
                StrategyName = strategyInfo.Name,
                StrategyCode = GetStrategyCode(strategyInfo),
                Symbol = strategyInfo.Symbol,
                DateTime = currentDate,
                Equity = equity,
                Profit = equity - previousEquity,
                Contracts = signal?.Quantity ?? 0m,
                Signal = signal?.Type,
                EntryPrice = signal?.Price
            });
        }
    }

    /// <summary>
    /// Applica la rotazione Titano al loop di backtest. Risolve una volta per periodo, non una volta
    /// per barra: il periodo è un blocco di giorni e ririsolverlo a ogni barra costerebbe una lookup
    /// nel manifest per ciascuna delle centinaia di migliaia di iterazioni del loop.
    /// </summary>
    private sealed class TitanoBacktestFilter
    {
        private readonly TitanoRotationService _titano;
        private readonly string _workspaceId;
        private readonly string _backtestFolder;
        private readonly string _runId;

        private DateTime _validFromUtc = DateTime.MaxValue;
        private DateTime _validToUtc = DateTime.MinValue;
        private HashSet<string> _enabled = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, decimal> _allocations = new(StringComparer.OrdinalIgnoreCase);

        public TitanoBacktestFilter(
            TitanoRotationService titano, string workspaceId, string backtestFolder, string runId)
        {
            _titano = titano;
            _workspaceId = workspaceId;
            _backtestFolder = backtestFolder;
            _runId = runId;
        }

        public IReadOnlySet<string> EnabledCodesAt(DateTime timestampUtc)
        {
            if (timestampUtc >= _validFromUtc && timestampUtc < _validToUtc)
                return _enabled;

            var effective = _titano.Resolve(
                _workspaceId, _backtestFolder, _runId, TradingDateTime.ToFeedUtc(timestampUtc),
                TitanoFilterMode.BacktestRotationFile);

            if (!effective.HasActivePeriod)
                throw new InvalidOperationException(
                    $"Nessun periodo Titano copre la barra {timestampUtc:O}: il run '{_runId}' copre " +
                    $"{effective.ManifestFromUtc:O} → {effective.ManifestToUtc:O}. Rigenera la rotazione " +
                    "su un backtest che copra l'intervallo richiesto, oppure esegui in modalità Disabled.");

            _enabled = new HashSet<string>(effective.EffectiveStrategies, StringComparer.OrdinalIgnoreCase);
            _allocations = effective.StrategyStates
                .Where(state => state.AllocationMultiplier > 0m)
                .ToDictionary(
                    state => state.StrategyCode,
                    state => state.AllocationMultiplier,
                    StringComparer.OrdinalIgnoreCase);

            // La finestra di validità della cache è il periodo stesso: fuori da qui si ririsolve.
            _validFromUtc = effective.PeriodFromUtc ?? timestampUtc;
            _validToUtc = effective.PeriodToUtc ?? timestampUtc.AddTicks(1);
            return _enabled;
        }

        public decimal AllocationFor(string strategyCode) =>
            _allocations.TryGetValue(strategyCode, out var allocation) ? allocation : 0m;
    }

    private TitanoBacktestFilter? CreateTitanoFilter(BacktestingRequest request)
    {
        if (request.TitanoMode == TitanoFilterMode.Disabled)
            return null;

        if (request.TitanoMode == TitanoFilterMode.Realtime)
            throw new ArgumentException(
                "TitanoFilterMode.Realtime non è applicabile a un backtest: usa BacktestRotationFile " +
                "per filtrare con le rotazioni calcolate offline, oppure Disabled per non filtrare.");

        if (string.IsNullOrWhiteSpace(request.TitanoRunId) || string.IsNullOrWhiteSpace(request.TitanoBacktestFolder))
            throw new ArgumentException(
                "La modalità BacktestRotationFile richiede TitanoRunId e TitanoBacktestFolder.");

        var titano = _titano ?? throw new InvalidOperationException("Servizio Titano non disponibile.");
        return new TitanoBacktestFilter(
            titano, request.WorkspaceId, request.TitanoBacktestFolder!, request.TitanoRunId!);
    }

    /// <summary>
    /// Risolve una volta per run la tabella di conversione dell'account richiesto. Un account
    /// inesistente è un errore esplicito: proseguire 1 a 1 falserebbe silenziosamente le size.
    /// </summary>
    private AccountSymbolConversion ResolveAccountConversion(BacktestingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId))
            return AccountSymbolConversion.Identity;

        if (_workspaces is null)
            throw new InvalidOperationException(
                "Servizio workspace non disponibile: impossibile risolvere la tabella di conversione dell'account.");

        return AccountSymbolConversion.FromAccount(
            _workspaces.GetAccount(request.AccountId!));
    }

    /// <summary>
    /// Applica la tabella di conversione a un segnale già normalizzato: scala la size con il
    /// moltiplicatore contratto dell'account. Restituisce false se il simbolo è disabilitato
    /// sull'account o se la size scalata si annulla, casi in cui il segnale non va emesso.
    /// </summary>
    private static bool TryApplyAccountConversion(TradeSignal signal, AccountSymbolConversion conversion)
    {
        if (conversion.IsIdentity)
            return true;

        if (!conversion.IsSymbolEnabled(signal.Symbol))
            return false;

        var multiplier = conversion.GetContractMultiplier(signal.Symbol);
        if (multiplier == 1m)
            return true;

        signal.Quantity *= multiplier;
        return signal.Quantity > 0;
    }

    private static IEnumerable<PersistedSignal> ToPersistedSignals(
        string jobId,
        IReadOnlyList<TradeSignal> signals,
        AccountSymbolConversion conversion) =>
        signals.Select((signal, index) => PersistedSignalMapper.FromTradeSignal(
            signal,
            signalId: $"{jobId}-signal-{index + 1:D10}",
            correlationId: jobId,
            accountId: conversion.AccountId,
            accountSymbol: conversion.GetAccountSymbol(signal.Symbol),
            contractMultiplier: conversion.GetContractMultiplier(signal.Symbol)));

    private static IEnumerable<PersistedTrade> ToPersistedTrades(
        string jobId,
        IReadOnlyList<TradingResult> trades) =>
        trades.Select((trade, index) => new PersistedTrade
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
        });

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
            OrderType = signal.OrderType,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            CloseAtUtc = signal.CloseAtUtc,
            StopLossMoneyPerFutureContract = signal.StopLossMoneyPerFutureContract,
            TakeProfitMoneyPerFutureContract = signal.TakeProfitMoneyPerFutureContract,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            BreakEven = signal.BreakEven,
            BreakEvenMoneyPerFutureContract = signal.BreakEvenMoneyPerFutureContract,
            TrailingStopMoneyPerFutureContract = signal.TrailingStopMoneyPerFutureContract,
            MaxBarsInPosition = signal.MaxBarsInPosition
        };
        TradingDateTime.NormalizeSignalToUtc(clone);
        return clone;
    }

    private void GenerateStrategyEquityHtmlReport(
        BacktestingResult result,
        IReadOnlyList<TradingResult> closedTrades,
        string filePath)
    {
        var series = result.StrategyResults
            .Where(row => row.Equity != 0)
            .GroupBy(row => MakeStrategyKey(row.Symbol, GetStrategyCode(row)))
            .Select(group => new
            {
                key = group.Key,
                label = group.Key,
                points = group
                    .OrderBy(row => row.DateTime)
                    .Select(row => new
                    {
                        t = row.DateTime.ToString("O"),
                        equity = row.Equity,
                        profit = row.Profit,
                        signal = row.Signal?.ToString()
                    })
                    .ToList()
            })
            .Where(group => group.points.Any())
            .ToList();

        var chartJson = JsonSerializer.Serialize(series, _jsonOptions);
        var globalSeries = result.HourlyResults
            .OrderBy(row => row.DateTime)
            .Select(row => new
            {
                t = row.DateTime.ToString("O"),
                equity = row.Equity,
                profit = row.Profit,
                drawdown = row.Drawdown
            })
            .ToList();
        var globalChartJson = JsonSerializer.Serialize(globalSeries, _jsonOptions);
        var title = System.Net.WebUtility.HtmlEncode($"{result.SetupName} - Equity per strategia");
        var symbols = result.StrategiesInfo
            .Select(info => NormalizeSymbol(info.Symbol))
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol)
            .ToList();
        if (!symbols.Any())
        {
            symbols = result.StrategyResults
                .Select(row => NormalizeSymbol(row.Symbol))
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(symbol => symbol)
                .ToList();
        }
        var symbolsText = symbols.Any() ? string.Join(", ", symbols) : "N/D";
        // Un segnale non implica un trade: uno stop può scadere senza fill e un ingresso può
        // restare aperto. Il report usa esclusivamente i trade chiusi dall'engine, che sono poi
        // persistiti in trades.json.
        var totalTrades = closedTrades.Count;
        var strategyCount = result.StrategiesInfo
            .Select(info => MakeStrategyKey(info.Symbol, GetStrategyCode(info)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (strategyCount == 0)
        {
            strategyCount = series.Count;
        }
        var html = new StringBuilder();

        if (!series.Any() && !globalSeries.Any())
        {
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html lang=\"it\"><head><meta charset=\"utf-8\">");
            html.AppendLine($"<title>{title}</title>");
            html.AppendLine("<style>body{font-family:Arial,Helvetica,sans-serif;margin:24px;background:#0f172a;color:#e5e7eb}.card{background:#111827;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:16px}.muted{color:#94a3b8}.metrics{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}.metric{background:#020617;border:1px solid #334155;border-radius:10px;padding:10px 12px}.metric b{display:block;color:#f8fafc}.summary-table{width:100%;border-collapse:collapse;margin-top:10px}.summary-table th,.summary-table td{border-bottom:1px solid #334155;padding:9px 10px;text-align:right}.summary-table th:first-child,.summary-table td:first-child{text-align:left}.positive{color:#22c55e}.negative{color:#fb7185}</style>");
            html.AppendLine("</head><body>");
            html.AppendLine($"<h1>{title}</h1>");
            AppendBacktestSummaryHtml(html, result, symbolsText, totalTrades, strategyCount);
            AppendYearlySummaryHtml(html, result, closedTrades);
            AppendMonthlySummaryHtml(html, result, closedTrades);
            html.AppendLine("<div class=\"card\"><p class=\"muted\">Nessuna equity per strategia disponibile: il backtest non ha prodotto trade gestiti dal motore.</p></div>");
            html.AppendLine("</body></html>");
            AtomicFileWriter.WriteAllText(filePath, html.ToString());
            return;
        }

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"it\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"  <title>{title}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body{font-family:Arial,Helvetica,sans-serif;margin:24px;background:#0f172a;color:#e5e7eb}");
        html.AppendLine("    .card{background:#111827;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:16px}");
        html.AppendLine("    canvas{width:100%;height:560px;background:#020617;border-radius:10px}");
        html.AppendLine("    .legend{display:flex;flex-wrap:wrap;gap:12px;margin-top:14px}");
        html.AppendLine("    .legend span{display:inline-flex;align-items:center;gap:6px;font-size:13px}");
        html.AppendLine("    .swatch{width:14px;height:3px;display:inline-block}");
        html.AppendLine("    .muted{color:#94a3b8}");
        html.AppendLine("    .metrics{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0 18px}");
        html.AppendLine("    .metric{background:#020617;border:1px solid #334155;border-radius:10px;padding:10px 12px;min-width:150px}");
        html.AppendLine("    .metric span{display:block;color:#94a3b8;font-size:12px}");
        html.AppendLine("    .metric b{display:block;color:#f8fafc;font-size:15px;margin-top:3px}");
        html.AppendLine("    .summary-table{width:100%;border-collapse:collapse;margin-top:10px}");
        html.AppendLine("    .summary-table th,.summary-table td{border-bottom:1px solid #334155;padding:9px 10px;text-align:right}");
        html.AppendLine("    .summary-table th:first-child,.summary-table td:first-child{text-align:left}");
        html.AppendLine("    .positive{color:#22c55e}");
        html.AppendLine("    .negative{color:#fb7185}");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"  <h1>{title}</h1>");
        AppendBacktestSummaryHtml(html, result, symbolsText, totalTrades, strategyCount);
        AppendYearlySummaryHtml(html, result, closedTrades);
        AppendMonthlySummaryHtml(html, result, closedTrades);
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Equity globale</h2>");
        html.AppendLine("    <canvas id=\"globalEquityChart\" width=\"1400\" height=\"560\"></canvas>");
        html.AppendLine("    <div id=\"globalLegend\" class=\"legend\"></div>");
        html.AppendLine("  </div>");
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Equity per strategia</h2>");
        html.AppendLine("    <canvas id=\"equityChart\" width=\"1400\" height=\"560\"></canvas>");
        html.AppendLine("    <div id=\"legend\" class=\"legend\"></div>");
        html.AppendLine("  </div>");
        html.AppendLine("  <script>");
        html.AppendLine($"    const series = {chartJson};");
        html.AppendLine($"    const globalSeries = {globalChartJson};");
        html.AppendLine("    const colors = ['#38bdf8','#f97316','#22c55e','#e879f9','#facc15','#fb7185','#a78bfa','#2dd4bf','#c084fc','#f87171'];");
        html.AppendLine("    function drawChart(canvasId, legendId, chartSeries, showDrawdown = false) {");
        html.AppendLine("      const canvas = document.getElementById(canvasId);");
        html.AppendLine("      const legend = document.getElementById(legendId);");
        html.AppendLine("      if (!chartSeries.length) { legend.innerHTML = '<span>Nessun dato disponibile</span>'; return; }");
        html.AppendLine("      const ctx = canvas.getContext('2d');");
        html.AppendLine("      const pad = {left: 74, right: showDrawdown ? 74 : 24, top: 28, bottom: 54};");
        html.AppendLine("      const allPoints = chartSeries.flatMap(s => s.points.map(p => ({...p, time: new Date(p.t).getTime()})));");
        html.AppendLine("      const minTime = Math.min(...allPoints.map(p => p.time));");
        html.AppendLine("      const maxTime = Math.max(...allPoints.map(p => p.time));");
        html.AppendLine("      const minEquity = Math.min(...allPoints.map(p => p.equity));");
        html.AppendLine("      const maxEquity = Math.max(...allPoints.map(p => p.equity));");
        html.AppendLine("      const yMin = minEquity === maxEquity ? minEquity - 1 : minEquity;");
        html.AppendLine("      const yMax = minEquity === maxEquity ? maxEquity + 1 : maxEquity;");
        html.AppendLine("      const x = t => pad.left + ((t - minTime) / Math.max(1, maxTime - minTime)) * (canvas.width - pad.left - pad.right);");
        html.AppendLine("      const y = v => canvas.height - pad.bottom - ((v - yMin) / Math.max(1, yMax - yMin)) * (canvas.height - pad.top - pad.bottom);");
        html.AppendLine("      ctx.clearRect(0,0,canvas.width,canvas.height);");
        html.AppendLine("      ctx.strokeStyle = '#334155'; ctx.lineWidth = 1; ctx.fillStyle = '#94a3b8'; ctx.font = '12px Arial';");
        html.AppendLine("      for (let i=0;i<=5;i++){ const yy = pad.top + i*(canvas.height-pad.top-pad.bottom)/5; ctx.beginPath(); ctx.moveTo(pad.left,yy); ctx.lineTo(canvas.width-pad.right,yy); ctx.stroke(); const val = yMax - i*(yMax-yMin)/5; ctx.fillText(val.toFixed(2), 8, yy+4); }");
        html.AppendLine("      if (showDrawdown) { const dd = chartSeries[0].points; const maxDd = Math.max(0, ...dd.map(p => Math.abs(p.drawdown || 0))); const plotH = canvas.height-pad.top-pad.bottom; const barW = Math.max(1, (canvas.width-pad.left-pad.right)/Math.max(1,dd.length)*0.8); ctx.fillStyle='rgba(239,68,68,0.28)'; dd.forEach(p=>{ const h=maxDd===0?0:Math.abs(p.drawdown||0)/maxDd*plotH; ctx.fillRect(x(new Date(p.t).getTime())-barW/2,canvas.height-pad.bottom-h,barW,h); }); ctx.fillStyle='#fca5a5'; for(let i=0;i<=5;i++){ const val=maxDd*(5-i)/5; const yy=pad.top+i*plotH/5; ctx.fillText(val.toFixed(2),canvas.width-pad.right+8,yy+4); } }");
        html.AppendLine("      chartSeries.forEach((s, idx) => { const color = colors[idx % colors.length]; ctx.strokeStyle = color; ctx.lineWidth = 2; ctx.beginPath(); s.points.forEach((p, i) => { const xx = x(new Date(p.t).getTime()); const yy = y(p.equity); if(i===0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); }); ctx.stroke(); });");
        html.AppendLine("      legend.innerHTML = chartSeries.map((s,idx)=>`<span><i class=\"swatch\" style=\"background:${colors[idx % colors.length]}\"></i>${s.label}</span>`).join('') + (showDrawdown ? '<span><i class=\"swatch\" style=\"background:rgba(239,68,68,.55)\"></i>Drawdown globale (scala destra)</span>' : '');");
        html.AppendLine("    }");
        html.AppendLine("    drawChart('globalEquityChart', 'globalLegend', [{ label: 'Equity globale', points: globalSeries }], true);");
        html.AppendLine("    drawChart('equityChart', 'legend', series);");
        html.AppendLine("  </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        AtomicFileWriter.WriteAllText(filePath, html.ToString());
    }

    private static void AppendBacktestSummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        string symbolsText,
        int totalTrades,
        int strategyCount)
    {
        var maxDrawdownPercent = CalculateMaxDrawdownPercent(result.HourlyResults, result.InitialCapital);
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Riepilogo simulazione</h2>");
        html.AppendLine("    <div class=\"metrics\">");
        html.AppendLine($"      <div class=\"metric\"><span>Range simulazione</span><b>{result.StartDate:yyyy-MM-dd HH:mm} - {result.EndDate:yyyy-MM-dd HH:mm}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Symbol usati</span><b>{System.Net.WebUtility.HtmlEncode(symbolsText)}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Strategie</span><b>{strategyCount}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Trade effettuati</span><b>{totalTrades}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Capitale iniziale</span><b>{result.InitialCapital:F2}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Profit totale</span><b>{result.TotalProfit:F2}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Max drawdown</span><b>{result.MaxDrawdown:F2} ({maxDrawdownPercent:F2}%)</b></div>");
        html.AppendLine("    </div>");
        html.AppendLine("  </div>");
    }

    private static void AppendYearlySummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        IReadOnlyList<TradingResult> closedTrades)
    {
        var orderedRows = result.HourlyResults
            .Where(row => row.Equity != 0)
            .OrderBy(row => row.DateTime)
            .ToList();

        if (!orderedRows.Any())
        {
            return;
        }

        var previousYearEndEquity = result.InitialCapital;
        var yearlyRows = new List<(int Year, decimal StartEquity, decimal EndEquity, decimal Profit, decimal MaxDrawdown, decimal ReturnPct, int WinningTrades, int LosingTrades)>();

        foreach (var yearGroup in orderedRows.GroupBy(row => row.DateTime.Year).OrderBy(group => group.Key))
        {
            var yearRows = yearGroup.OrderBy(row => row.DateTime).ToList();
            var endEquity = yearRows.Last().Equity;
            var profit = endEquity - previousYearEndEquity;
            var maxDrawdown = CalculateMaxDrawdown(yearRows, previousYearEndEquity);
            var returnPct = previousYearEndEquity != 0 ? profit / previousYearEndEquity * 100m : 0m;
            var yearTrades = closedTrades
                .Where(trade => trade.ExitDate.Year == yearGroup.Key)
                .ToList();
            var winningTrades = yearTrades.Count(trade => trade.NetProfit > 0);
            var losingTrades = yearTrades.Count(trade => trade.NetProfit < 0);

            yearlyRows.Add((yearGroup.Key, previousYearEndEquity, endEquity, profit, maxDrawdown, returnPct, winningTrades, losingTrades));
            previousYearEndEquity = endEquity;
        }

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Resoconto annuale</h2>");
        html.AppendLine("    <table class=\"summary-table\">");
        html.AppendLine("      <thead><tr><th>Anno</th><th>Equity iniziale</th><th>Equity finale</th><th>Profit</th><th>Return %</th><th>Max DD anno</th><th>Trade win</th><th>Trade persi</th></tr></thead>");
        html.AppendLine("      <tbody>");

        foreach (var row in yearlyRows)
        {
            var profitClass = row.Profit >= 0 ? "positive" : "negative";
            html.AppendLine(
                $"        <tr><td>{row.Year}</td><td>{row.StartEquity:F2}</td><td>{row.EndEquity:F2}</td><td class=\"{profitClass}\">{row.Profit:F2}</td><td class=\"{profitClass}\">{row.ReturnPct:F2}%</td><td class=\"negative\">{row.MaxDrawdown:F2}</td><td>{row.WinningTrades}</td><td>{row.LosingTrades}</td></tr>");
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
    }

    private static void AppendMonthlySummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        IReadOnlyList<TradingResult> closedTrades)
    {
        var orderedRows = result.HourlyResults
            .Where(row => row.Equity != 0)
            .OrderBy(row => row.DateTime)
            .ToList();

        if (!orderedRows.Any())
        {
            return;
        }

        var previousMonthEndEquity = result.InitialCapital;

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Resoconto mensile</h2>");
        html.AppendLine("    <table class=\"summary-table\">");
        html.AppendLine("      <thead><tr><th>Mese</th><th>Equity iniziale</th><th>Equity finale</th><th>Profit</th><th>Return %</th><th>Max DD mese</th><th>Trade win</th><th>Trade persi</th></tr></thead>");
        html.AppendLine("      <tbody>");

        foreach (var monthGroup in orderedRows.GroupBy(row => new { row.DateTime.Year, row.DateTime.Month }).OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month))
        {
            var monthRows = monthGroup.OrderBy(row => row.DateTime).ToList();
            var endEquity = monthRows.Last().Equity;
            var profit = endEquity - previousMonthEndEquity;
            var maxDrawdown = CalculateMaxDrawdown(monthRows, previousMonthEndEquity);
            var returnPct = previousMonthEndEquity != 0 ? profit / previousMonthEndEquity * 100m : 0m;
            var monthTrades = closedTrades
                .Where(trade => trade.ExitDate.Year == monthGroup.Key.Year && trade.ExitDate.Month == monthGroup.Key.Month)
                .ToList();
            var profitClass = profit >= 0 ? "positive" : "negative";

            html.AppendLine(
                $"        <tr><td>{monthGroup.Key.Year}-{monthGroup.Key.Month:00}</td><td>{previousMonthEndEquity:F2}</td><td>{endEquity:F2}</td><td class=\"{profitClass}\">{profit:F2}</td><td class=\"{profitClass}\">{returnPct:F2}%</td><td class=\"negative\">{maxDrawdown:F2}</td><td>{monthTrades.Count(trade => trade.NetProfit > 0)}</td><td>{monthTrades.Count(trade => trade.NetProfit < 0)}</td></tr>");

            previousMonthEndEquity = endEquity;
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
    }

    private static decimal CalculateMaxDrawdown(IEnumerable<HourlyResult> yearRows, decimal initialPeak)
    {
        var peak = initialPeak;
        var maxDrawdown = 0m;

        foreach (var row in yearRows.OrderBy(item => item.DateTime))
        {
            if (row.Equity > peak)
            {
                peak = row.Equity;
            }

            var drawdown = peak - row.Equity;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return maxDrawdown;
    }

    private static decimal CalculateMaxDrawdownPercent(IEnumerable<HourlyResult> rows, decimal initialPeak)
    {
        var peak = initialPeak;
        var maximum = 0m;
        foreach (var row in rows.OrderBy(item => item.DateTime))
        {
            peak = Math.Max(peak, row.Equity);
            if (peak != 0)
                maximum = Math.Max(maximum, (peak - row.Equity) / Math.Abs(peak));
        }
        return maximum * 100m;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0) daysToSubtract += 7;
        return date.AddDays(-daysToSubtract).Date;
    }

    private static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().TrimStart('@').ToUpperInvariant();
    }

    private static string NormalizeSymbolWithPrefix(string symbol)
    {
        var normalized = NormalizeSymbol(symbol);
        return string.IsNullOrEmpty(normalized) ? normalized : $"@{normalized}";
    }

    private static string MakeStrategyKey(string symbol, string strategyCode)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var normalizedStrategyCode = strategyCode.Trim();

        if (string.IsNullOrEmpty(normalizedSymbol))
        {
            return normalizedStrategyCode;
        }

        if (string.IsNullOrEmpty(normalizedStrategyCode))
        {
            return normalizedSymbol;
        }

        return $"{normalizedSymbol}|{normalizedStrategyCode}";
    }

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

    private static string GetStrategyCode(StrategyHourlyResult result)
    {
        return !string.IsNullOrWhiteSpace(result.StrategyCode)
            ? result.StrategyCode
            : result.StrategyName;
    }

    private static string GetStrategyCode(Piootoo.Shared.Models.Backtesting.StrategyInfo result)
    {
        return !string.IsNullOrWhiteSpace(result.StrategyCode)
            ? result.StrategyCode
            : result.Name;
    }

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
            // Log solo quando necessario per evitare spam
            if (iterationCount % 1000 == 0)
            {
                Console.WriteLine($"[Backtesting] Strategia con timeframe {strategyTimeframeMinutes} non è multiplo di {minTimeframeMinutes}, skip");
            }
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
        
        // Log solo quando necessario
        if (shouldEvaluate && iterationCount % 100 == 0)
        {
            Console.WriteLine($"[Backtesting] Valutazione strategia: iterationCount={iterationCount}, strategyTF={strategyTimeframeMinutes}, minTF={minTimeframeMinutes}, multiplier={multiplier}, evaluate={shouldEvaluate}");
        }
        
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

    private static bool IsStrategyCandleStale(int timeframeMinutes, DateTime lastCandleTime, DateTime currentDate)
    {
        if (timeframeMinutes < 1440)
        {
            return false;
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

    private static DateTime GetNextTradingDateUtc(DateTime currentDate, int minTimeframeMinutes)
    {
        var next = currentDate.AddMinutes(minTimeframeMinutes);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            next = next.AddMinutes(minTimeframeMinutes);
        }

        return next;
    }

    private static bool IsLastBarOfTradingWeek(DateTime currentDate, DateTime nextTradingDate)
    {
        return GetWeekStartUtc(currentDate) != GetWeekStartUtc(nextTradingDate);
    }

    private static DateTime GetWeekStartUtc(DateTime date)
    {
        var daysToSubtract = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-daysToSubtract);
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
