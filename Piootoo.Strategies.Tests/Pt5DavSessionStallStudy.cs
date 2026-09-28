using System.Text;
using System.Text.Json;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Riproduce in locale la sessione ExternalBroker del piano PT5DAV-S-INDICI che, nel backtest cTrader
/// del 25/09/2026, smette di emettere intent dal 12/09/2025 per tutte le 17 strategie mentre le barre
/// continuano ad arrivare. Spinge le barre FTMO come fa il cBot — riscaldamento con le candele richieste,
/// poi una finestra da 100 candele per ogni barra chiusa di ogni stream — e conta gli intent giorno per
/// giorno. Studio: gira solo con PIOOTOO_STUDI=1.
/// </summary>
public sealed class Pt5DavSessionStallStudy(ITestOutputHelper output) : IDisposable
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-stallo-{Guid.NewGuid():N}");

    private static readonly string[] Strategies =
    [
        "PT5DAV_ES_SBO_001_15", "PT5DAV_ES_MAC_002_60", "PT5DAV_FDAX_BOS_001_15", "PT5DAV_FDAX_LFD_002_30",
        "PT5DAV_FDAX_PCH_002_60", "PT5DAV_NQ_BOS_001_15", "PT5DAV_NQ_RHL_002_15", "PT5DAV_NQ_VBO_001_15",
        "PT5DAV_NQ_RHL_003_60", "PT5DAV_NQ_BSW_001_240", "PT5DAV_NQ_LFD_004_240", "PT5DAV_YM_LFD_002_30",
        "PT5DAV_YM_RBM_002_30", "PT5DAV_YM_LFD_003_60", "PT5DAV_YM_LFH_003_240", "PT5DAV_YM_MAC_004_240",
        "PT5DAV_YM_RHL_003_240"
    ];

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* cartella temporanea */ }
    }

    [Fact]
    public void IndiciSessionKeepsEmittingAfterMidSeptember()
    {
        if (Environment.GetEnvironmentVariable("PIOOTOO_STUDI") != "1")
            return;

        var start = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "stallo-indici", StrategiesFilter = [.. Strategies] });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();
        TestAccountRegistry.Register(workspaces, "1001");
        var plans = new TradingPlanService(workspaces);
        plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = "STALLO-INDICI",
            Name = "stallo",
            AccountNumber = "1001",
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        });

        var sessions = new TradingSessionService(
            workspaces, plans, new StrategyEvaluationService(), positionSizing: new PositionSizingService());
        var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = "STALLO-INDICI",
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "stallo"
        });

        // Stream = (simbolo, timeframe) delle strategie, con le candele richieste dalla piu' esigente.
        StrategyFactory.LogStrategyCreation = false;
        var streams = descriptor.Strategies
            .GroupBy(s => (Symbol: s.Symbol.TrimStart('@').ToUpperInvariant(), s.TimeframeMinutes))
            .ToDictionary(g => g.Key, g => g.Max(s =>
                StrategyFactory.CreateStrategy(s.StrategyCode, s.Symbol, 0, null)!.RequiredCandles));

        var candles = streams.Keys.ToDictionary(key => key, key => Load(key.Symbol, key.TimeframeMinutes));

        // Riscaldamento, come SendWarmUpWindow.
        foreach (var (key, required) in streams)
        {
            var history = candles[key].Where(c => c.DateTime < start).TakeLast(required).ToArray();
            sessions.PushBarWindow(new PushBarWindowRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Windows = [Window(key, history, evaluate: false)]
            });
        }

        // Ogni barra chiusa di ogni stream, in ordine di chiusura.
        var events = streams.Keys
            .SelectMany(key => candles[key]
                .Select((candle, index) => (key, index, close: candle.DateTime.AddMinutes(key.TimeframeMinutes)))
                .Where(e => candles[e.key][e.index].DateTime >= start && candles[e.key][e.index].DateTime < end))
            .OrderBy(e => e.close).ThenBy(e => e.key.TimeframeMinutes)
            .ToList();

        var perDay = new SortedDictionary<DateTime, (int Intents, int Evaluated, int Errors)>();
        var firstErrors = new List<string>();
        foreach (var (key, index, close) in events)
        {
            var window = candles[key].Skip(Math.Max(0, index - 99)).Take(Math.Min(100, index + 1)).ToArray();
            var day = candles[key][index].DateTime.Date;
            var row = perDay.GetValueOrDefault(day);
            try
            {
                var response = sessions.PushBarWindow(new PushBarWindowRequest
                {
                    SessionId = descriptor.SessionId,
                    SessionToken = descriptor.SessionToken,
                    Windows = [Window(key, window, evaluate: true)]
                });
                row.Intents += response.Intents.Count;
                row.Evaluated += response.Streams.Sum(s => s.EvaluatedStrategies);
            }
            catch (Exception error)
            {
                row.Errors++;
                if (firstErrors.Count < 10)
                    firstErrors.Add($"{candles[key][index].DateTime:yyyy-MM-dd HH:mm} {key.Symbol}/{key.TimeframeMinutes}: {error.GetType().Name}: {error.Message}");
            }
            perDay[day] = row;
        }

        var report = new StringBuilder();
        foreach (var (day, row) in perDay)
            report.AppendLine($"{day:yyyy-MM-dd} {day.DayOfWeek,-9} intent {row.Intents,4}  valutate {row.Evaluated,5}  errori {row.Errors,3}");
        report.AppendLine();
        foreach (var line in firstErrors)
            report.AppendLine(line);

        var path = Path.Combine(RepositoryPath, "PT5DAV", "verifica", "stallo-sessione-indici.txt");
        File.WriteAllText(path, report.ToString());
        output.WriteLine(report.ToString());
    }

    private static ClosedBarWindow Window((string Symbol, int TimeframeMinutes) key, OhlcvData[] window, bool evaluate)
    {
        var last = window[^1].DateTime;
        return new ClosedBarWindow
        {
            Symbol = key.Symbol,
            TimeframeMinutes = key.TimeframeMinutes,
            Candles = window,
            Sequence = (long)(last - DateTime.UnixEpoch).TotalMilliseconds,
            IdempotencyKey = $"{key.Symbol}|{key.TimeframeMinutes}|{last:O}",
            EvaluateLastCandle = evaluate
        };
    }

    private static OhlcvData[] Load(string symbol, int timeframeMinutes)
    {
        var path = Path.Combine(RepositoryPath, "datafeed-external", "FTMO", $"@{symbol}_{timeframeMinutes}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("candles").EnumerateArray()
            .Select(c => new OhlcvData
            {
                DateTime = DateTime.SpecifyKind(c.GetProperty("dateTime").GetDateTime().ToUniversalTime(), DateTimeKind.Utc),
                Open = c.GetProperty("open").GetDecimal(),
                High = c.GetProperty("high").GetDecimal(),
                Low = c.GetProperty("low").GetDecimal(),
                Close = c.GetProperty("close").GetDecimal(),
                Volume = c.GetProperty("volume").GetDecimal()
            })
            .ToArray();
    }
}
