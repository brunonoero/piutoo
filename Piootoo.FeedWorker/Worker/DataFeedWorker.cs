using FeedWorker.Configuration;
using FeedWorker.Dto;
using FeedWorker.Insightsentry;
using FeedWorker.Storage;
using Microsoft.Extensions.Options;
using NCrontab;

namespace FeedWorker.Worker;

public class DataFeedWorker : BackgroundService
{
    private readonly ILogger<DataFeedWorker> _logger;
    private readonly SymbolsOptions _symbolsOptions;
    private readonly InsightSentryClient _insightSentryClient;
    private readonly StorageFeedFacade _storageFeedFacade;
    private CrontabSchedule? _cronSchedule;
    private DateTime? _nextRun;

    public DataFeedWorker(
        ILogger<DataFeedWorker> logger,
        IOptions<SymbolsOptions> symbolsOptions,
        InsightSentryClient insightSentryClient,
        StorageFeedFacade storageFeedFacade)
    {
        _logger = logger;
        _symbolsOptions = symbolsOptions.Value;
        _insightSentryClient = insightSentryClient;
        _storageFeedFacade = storageFeedFacade;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Inizializza la cron expression
        try
        {
            // La cron è valutata in UTC: il worker alimenta un feed i cui timestamp sono UTC, e
            // legarne la schedulazione al fuso della macchina sposterebbe l'orario di polling
            // a ogni deploy su un host diverso, ora legale inclusa.
            _cronSchedule = CrontabSchedule.Parse(_symbolsOptions.CronExpression);
            _nextRun = _cronSchedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Cron expression configurata: {CronExpression} (UTC). Prossima esecuzione: {NextRun}",
                _symbolsOptions.CronExpression, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel parsing della cron expression: {CronExpression}",
                _symbolsOptions.CronExpression);
            return;
        }

        // Verifica che ci siano simboli configurati (usa SymbolsShort da SymbolConfig)
        if (_symbolsOptions.SymbolsForWorker == null || _symbolsOptions.SymbolsForWorker.Count == 0)
        {
            _logger.LogWarning("Nessun simbolo configurato. Il worker non eseguirà alcun polling.");
            return;
        }

        var intervals = _symbolsOptions.GetCandleIntervals();
        var symbolNames = _symbolsOptions.SymbolsForWorker.Select(s => s.Name);
        _logger.LogInformation("Worker avviato. Simboli: {Symbols}, Intervalli configurati: [{IntervalsConfig}], Intervalli parsati: [{Intervals}], CandleLimit: {Limit}",
            string.Join(", ", symbolNames), 
            string.Join(", ", _symbolsOptions.Intervals ?? new List<string>()), 
            string.Join(", ", intervals), 
            _symbolsOptions.CandleLimit);

        // Esegui immediatamente il polling all'avvio per tutti gli intervalli
        _logger.LogInformation("Esecuzione polling iniziale all'avvio per {Count} intervalli", intervals.Count);
        await PollAllSymbolsAsync(intervals, stoppingToken);
        
        // Calcola la prossima esecuzione dopo il polling iniziale
        var now = DateTime.UtcNow;
        _nextRun = _cronSchedule?.GetNextOccurrence(now);
        _logger.LogInformation("Prossima esecuzione programmata: {NextRun}", _nextRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            now = DateTime.UtcNow;

            // Verifica se è il momento di eseguire il polling
            if (_nextRun.HasValue && now >= _nextRun.Value)
            {
                _logger.LogInformation("Esecuzione polling programmato alle {Time} per {Count} intervalli", now, intervals.Count);

                // Esegui il polling per ogni simbolo e ogni intervallo
                await PollAllSymbolsAsync(intervals, stoppingToken);

                // Calcola la prossima esecuzione
                _nextRun = _cronSchedule?.GetNextOccurrence(now);
                _logger.LogInformation("Prossima esecuzione: {NextRun}", _nextRun);
            }

            // Attendi 1 minuto prima di controllare di nuovo
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task PollAllSymbolsAsync(List<CandleInterval> intervals, CancellationToken cancellationToken)
    {
        foreach (var interval in intervals)
        {
            _logger.LogInformation("=== Polling per intervallo: {Interval} ===", interval);
            
            var tasks = _symbolsOptions.SymbolsForWorker.Select(symbolInfo =>
                PollSymbolAsync(symbolInfo, interval, cancellationToken));

            await Task.WhenAll(tasks);
        }
    }

    private async Task PollSymbolAsync(SymbolInfo symbolInfo, CandleInterval interval, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Inizio polling realtime per simbolo: {Name} (API: {DsSymbol}, Storage: {FutureSymbol}), Intervallo: {Interval}, Limit: {Limit}", 
                symbolInfo.Name, symbolInfo.DsSymbol, symbolInfo.FutureSymbol, interval, _symbolsOptions.CandleLimit);

            // Chiama l'API realtime usando DsSymbol per recuperare le candele
            var candles = await _insightSentryClient.GetRealtimeCandlesAsync(
                symbolInfo.DsSymbol,
                interval,
                _symbolsOptions.CandleLimit);

            _logger.LogInformation("Polling completato: recuperate {Count} candele per il simbolo {Name}", candles.Count, symbolInfo.Name);

            if (candles.Count == 0)
            {
                _logger.LogWarning("Nessuna candela recuperata per il simbolo {Name}", symbolInfo.Name);
                return;
            }

            // Salva le candele usando StorageFeedFacade con FutureSymbol per il naming dei file
            await _storageFeedFacade.SaveCandlesAsync(symbolInfo.FutureSymbol, interval, candles);
            
            _logger.LogInformation("Polling e salvataggio completati per il simbolo {Name}", symbolInfo.Name);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore HTTP durante il polling del simbolo {Name}: {Message}", symbolInfo.Name, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il polling del simbolo {Name}: {Message}", symbolInfo.Name, ex.Message);
        }
    }
}
