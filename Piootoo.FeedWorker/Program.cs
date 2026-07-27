using FeedWorker;
using FeedWorker.Configuration;
using FeedWorker.Insightsentry;
using FeedWorker.Storage;
using FeedWorker.Worker;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "FeedWorker";
});

// Configurazione dell'endpoint
builder.Services.Configure<EndpointOptions>(
    builder.Configuration.GetSection("Endpoint"));

// Configurazione dei simboli
builder.Services.Configure<SymbolsOptions>(
    builder.Configuration.GetSection("Symbols"));

// Configurazione del repository
builder.Services.Configure<RepositoryOptions>(
    builder.Configuration.GetSection("Repository"));

// Registrazione di InsightSentryClient
builder.Services.AddSingleton<InsightSentryClient>(serviceProvider =>
{
    var endpointOptions = serviceProvider.GetRequiredService<IOptions<EndpointOptions>>().Value;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    if (string.IsNullOrWhiteSpace(endpointOptions.BaseUrl))
    {
        logger.LogError("BaseUrl non configurato o vuoto! Verificare la configurazione in appsettings.json");
        throw new InvalidOperationException("BaseUrl non può essere vuoto. Configurare il campo 'Endpoint:BaseUrl' in appsettings.json");
    }
    
    if (string.IsNullOrWhiteSpace(endpointOptions.ApiKey))
    {
        logger.LogWarning("ApiKey non configurata o vuota!");
    }
    else
    {
        logger.LogInformation("ApiKey configurata (lunghezza: {Length} caratteri)", endpointOptions.ApiKey.Length);
    }
    
    logger.LogInformation("BaseUrl configurato: {BaseUrl}", endpointOptions.BaseUrl);
    
    return new InsightSentryClient(endpointOptions.BaseUrl, endpointOptions.ApiKey);
});

// Registrazione di StorageFeedFacade
builder.Services.AddSingleton<StorageFeedFacade>();

builder.Services.AddHostedService<DataFeedWorker>();

var host = builder.Build();
host.Run();
