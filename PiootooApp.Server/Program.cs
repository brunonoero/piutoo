using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared;
using Piootoo.Shared.Configuration;

// Prima riga del log, prima ancora dell'host: è il numero da confrontare con quello che il cBot
// distribuito stampa al proprio avvio. Sono la stessa versione tenuta allineata a mano — vedi
// PiootooVersion — e vederle entrambe nei log è l'unico modo per accorgersi di un disallineamento.
Console.WriteLine($"[Piootoo] Server v{PiootooVersion.Current} — avvio.");

var builder = WebApplication.CreateBuilder(args);

// Istanza costruita subito, non risolta pigramente: cattura l'avvio del processo. Vedi ServerRuntime.
builder.Services.AddSingleton(new PiootooApp.Server.ServerRuntime());

// Add services to the container.

// Configurazione Piootoo
builder.Services.Configure<PiootooSettings>(builder.Configuration.GetSection("Piootoo"));

// Risolvi PiootooSettings per i servizi
builder.Services.AddSingleton<PiootooSettings>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PiootooSettings>>().Value;
    settings.ResolvePaths();
    return settings;
});

// Registra i servizi
// I servizi usati da BacktestingService/OptimizationService devono essere Singleton
// per permettere il mantenimento dei job in memoria
builder.Services.AddSingleton<IPiootooSettingsService, PiootooSettingsService>();
builder.Services.AddSingleton<IPiootooDataFeedService, PiootooDataFeedService>();
// NB: questa istanza è condivisa. Il backtesting NON la usa: crea un motore per job, perché
// PiootooTradingService è mutabile e due backtest concorrenti si corromperebbero a vicenda.
builder.Services.AddSingleton<IPiootooTradingService, PiootooTradingService>();
builder.Services.AddSingleton<IBacktestingExecutionHook, NoOpBacktestingExecutionHook>();
builder.Services.AddSingleton<PiootooBacktestingService>();
builder.Services.AddSingleton<IPiootooBacktestingService>(sp => sp.GetRequiredService<PiootooBacktestingService>());
builder.Services.AddSingleton<IPiootooSapiooService, PiootooSapiooService>();
builder.Services.AddSingleton<PiootooOptimizationService>();
builder.Services.AddSingleton<TitanoRotationSetupService>();
builder.Services.AddSingleton<TitanoRotationService>();
builder.Services.AddSingleton<WorkspaceService>();
builder.Services.AddSingleton<TradingPlanService>();
builder.Services.AddSingleton<IStrategyEvaluationService, StrategyEvaluationService>();
builder.Services.AddSingleton<IPositionSizingService, PositionSizingService>();
builder.Services.AddSingleton<ITradingSessionService, TradingSessionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Lifetime.ApplicationStarted.Register(() =>
{
    // Ripetuta qui perché la riga di Console sopra esce prima che il logging sia configurato: senza
    // questa la versione non finisce nel log strutturato, cioè in quello che si allega a un ticket.
    app.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Piootoo")
        .LogInformation("Piootoo Server v{Version} avviato.", PiootooVersion.Current);

    var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
        .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses
        ?? Enumerable.Empty<string>();
    foreach (var address in addresses)
    {
        Console.WriteLine($"[Piootoo] In ascolto su: {address}");
    }
});

app.Run();

public partial class Program;
