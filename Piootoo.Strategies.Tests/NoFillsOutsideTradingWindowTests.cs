using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Fuori dalla finestra di negoziazione del future un ordine di ingresso non esiste (7.5.2). Il
/// FDAX apre alle 00:15 UTC: un pending valido dal bucket dell'01:00 di Roma (00:00 UTC d'inverno)
/// non si riempie sui minuti del CFD fra le 00:00 e le 00:14 UTC, anche se il prezzo tocca il
/// livello, e si riempie sul primo minuto dentro la finestra. Sul DAX di FTMO quei fill valevano 13
/// trade e −13.202 in un anno su <c>PT2_FDAX_PCH_001_240</c>, tutti inesistenti sul future.
/// </summary>
public sealed class NoFillsOutsideTradingWindowTests
{
    private const string Code = "PTS_TEST_WIN_240";

    [Fact]
    public void AStopOrderIsNotFilledOnMinutesBeforeTheEurexOpenAndFillsOnTheFirstMinuteInside()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Mercoledì 14 gennaio 2026, segnale sulla barra delle 21:00 di Roma (20:00 UTC), valido
        // sul bucket successivo che apre alle 00:00 UTC.
        var signalTime = new DateTime(2026, 1, 14, 20, 0, 0, DateTimeKind.Utc);
        var validFrom = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var level = 20_000m;

        service.ProcessSignals(
            [new TradeSignal
            {
                Date = signalTime, Type = SignalType.Buy, Price = level, Symbol = "@FDAX",
                StrategyName = Code, StrategyCode = Code, Quantity = 1m,
                OrderType = TradeOrderType.Stop, ValidFromUtc = validFrom, ExpiresAtUtc = validFrom,
                TimeframeMinutes = 240, Reason = "test"
            }],
            Prices(level - 50m), Minute(signalTime, level - 50m), signalTime);

        // Minuti del CFD prima dell'apertura Eurex: toccano il livello, non riempiono.
        for (var m = 0; m < 15; m++)
        {
            var t = validFrom.AddMinutes(m);
            service.UpdateMarketPrices(Prices(level + 10m), Minute(t, level - 5m, level + 10m, level - 10m, level + 5m), t);
            Assert.Null(service.GetExecutionSnapshot(Code, "FDAX", t).Position);
        }
        Assert.Equal(15, service.FillsHeldOutsideWindow);

        // 00:15 UTC: primo minuto dentro la finestra, l'ordine vive e si riempie.
        var open = validFrom.AddMinutes(15);
        service.UpdateMarketPrices(Prices(level + 10m), Minute(open, level - 5m, level + 10m, level - 10m, level + 5m), open);
        var position = service.GetExecutionSnapshot(Code, "FDAX", open).Position;
        Assert.NotNull(position);
        Assert.Equal(level, position!.EntryPrice);
        Assert.Equal(open, position.EntryTimeUtc);
    }

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["FDAX"] = price };

    private static Dictionary<string, OhlcvData> Minute(DateTime time, decimal price) =>
        Minute(time, price, price, price, price);

    private static Dictionary<string, OhlcvData> Minute(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["FDAX"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };
}
