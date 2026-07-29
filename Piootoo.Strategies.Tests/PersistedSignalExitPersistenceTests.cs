using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Verifica che <see cref="PersistedSignal"/> conservi tutte le condizioni di uscita
/// dichiarate da <see cref="TradeSignal"/>, inclusi i limiti monetari per contratto
/// usati da PTS (nessuna conversione percentuale).
/// </summary>
public sealed class PersistedSignalExitPersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-exit-{Guid.NewGuid():N}");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void TradingJsonStore_RoundTripsAllExitConditions()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();

        var timeExit = new DateTime(2024, 1, 4, 16, 0, 0, DateTimeKind.Utc);
        var signal = new PersistedSignal
        {
            SignalId = "signal-exit-1",
            TimestampUtc = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc),
            StrategyCode = "PTS_001_NQ_60",
            StrategyName = "PTS_001_NQ_60",
            Symbol = "NQ",
            Side = SignalType.Buy,
            OrderType = TradeOrderType.Stop,
            TriggerPrice = 15_000m,
            Quantity = 1m,
            ValidFromUtc = new DateTime(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc),
            StopLoss = null,
            TakeProfit = null,
            StopLossMoneyPerFutureContract = 1000m,
            TakeProfitMoneyPerFutureContract = 3000m,
            BreakEven = 25m,
            TimeframeMinutes = 60,
            TimeExitUtc = timeExit,
            MaxBarsInPosition = 12,
            Reason = "TF_M LE H_d1"
        };

        store.WriteSignals([signal]);
        var loaded = Assert.Single(store.ReadSignals());

        Assert.Null(loaded.StopLoss);
        Assert.Null(loaded.TakeProfit);
        Assert.Equal(1000m, loaded.StopLossMoneyPerFutureContract);
        Assert.Equal(3000m, loaded.TakeProfitMoneyPerFutureContract);
        Assert.Equal(25m, loaded.BreakEven);
        Assert.Equal(60, loaded.TimeframeMinutes);
        Assert.Equal(timeExit, loaded.TimeExitUtc);
        Assert.Equal(12, loaded.MaxBarsInPosition);
        Assert.Equal(TradeOrderType.Stop, loaded.OrderType);
        Assert.Equal(signal.ValidFromUtc, loaded.ValidFromUtc);
        Assert.Equal(signal.ExpiresAtUtc, loaded.ExpiresAtUtc);
    }

    [Fact]
    public void PtsMoneyExits_SurviveJsonWithoutBecomingPercentages()
    {
        // Forma tipica di PTS_001: solo USD/contratto, nessuno stop in punti sul segnale.
        var tradeSignal = new TradeSignal
        {
            Date = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc),
            Type = SignalType.Buy,
            Price = 15_000m,
            Symbol = "@NQ",
            StrategyCode = "PTS_001_NQ_60",
            StrategyName = "PTS_001_NQ_60",
            Quantity = 1m,
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = new DateTime(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc),
            StopLossMoneyPerFutureContract = 1000m,
            TakeProfitMoneyPerFutureContract = 3000m,
            Reason = "TF_M LE H_d1"
        };

        var persisted = PersistedSignalMapper.FromTradeSignal(
            tradeSignal,
            signalId: "job-signal-0000000001",
            correlationId: "job");

        var json = JsonSerializer.Serialize(persisted, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("stopLoss").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("takeProfit").ValueKind);
        Assert.Equal(1000m, root.GetProperty("stopLossMoneyPerFutureContract").GetDecimal());
        Assert.Equal(3000m, root.GetProperty("takeProfitMoneyPerFutureContract").GetDecimal());
        Assert.False(json.Contains('%', StringComparison.Ordinal));

        var copy = JsonSerializer.Deserialize<PersistedSignal>(json, JsonOptions)!;
        Assert.Equal(1000m, copy.StopLossMoneyPerFutureContract);
        Assert.Equal(3000m, copy.TakeProfitMoneyPerFutureContract);
        Assert.Null(copy.StopLoss);
        Assert.Null(copy.TakeProfit);
        Assert.Equal("NQ", copy.Symbol);

        // Conversione engine: $1000 / $20pt = 50 punti; $3000 / $20pt = 150 punti.
        Assert.Equal(50m, copy.StopLossMoneyPerFutureContract!.Value / 20m);
        Assert.Equal(150m, copy.TakeProfitMoneyPerFutureContract!.Value / 20m);
    }

    [Fact]
    public void LegacySignalsWithoutMoneyFields_DeserializeWithNullExits()
    {
        // File v2 precedenti non avevano i campi monetari: devono restare leggibili.
        const string legacyJson = """
            [{
              "schemaVersion": 2,
              "signalId": "legacy-1",
              "timestampUtc": "2024-01-03T16:00:00Z",
              "strategyCode": "TOP_UA_218",
              "strategyName": "TOP_UA_218",
              "symbol": "GC",
              "side": "Buy",
              "orderType": "Market",
              "triggerPrice": 2000,
              "quantity": 1,
              "stopLoss": 5,
              "takeProfit": 15,
              "maxBarsInPosition": 10
            }]
            """;

        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, TradingPersistenceSchema.SignalsFileName), legacyJson);

        var store = new TradingJsonStore(_root);
        var loaded = Assert.Single(store.ReadSignals());
        Assert.Equal(5m, loaded.StopLoss);
        Assert.Equal(15m, loaded.TakeProfit);
        Assert.Null(loaded.StopLossMoneyPerFutureContract);
        Assert.Null(loaded.TakeProfitMoneyPerFutureContract);
        Assert.Null(loaded.BreakEven);
        Assert.Equal(10, loaded.MaxBarsInPosition);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
