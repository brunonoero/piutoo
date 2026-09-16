using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ordine "next bar" nato sull'ultima barra del venerdì porta una chiusura di fine sessione
/// risolta sulla barra <b>proiettata</b> (sabato), non su quella vera su cui si riempie (lunedì).
/// Su NQ la proiezione cade nella sessione di domenica, che il calendario dichiara: la deadline
/// è quindi già passata quando l'ordine si riempie il lunedì, e la posizione moriva nello stesso
/// minuto del fill. La ricerca chiude a fine della sessione del fill: qui l'engine fa lo stesso.
/// </summary>
public sealed class SessionExitAfterGapFillTests
{
    private const string Code = "PTS_TEST_GAP_240";

    [Fact]
    public void ADeadlineAlreadyPassedAtFillBecomesTheEndOfTheFillSession()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Venerdì 05/01/2024, ultima barra 4h (20:00Z = 21:00 Roma): la proiezione è sabato 00:00Z
        // e la deadline di fine sessione risolta lì è domenica 22:59Z (00:59 Roma di lunedì).
        var signalTime = new DateTime(2024, 1, 5, 20, 0, 0, DateTimeKind.Utc);
        var projected = new DateTime(2024, 1, 6, 0, 0, 0, DateTimeKind.Utc);
        var staleDeadline = new DateTime(2024, 1, 7, 22, 59, 0, DateTimeKind.Utc);
        var level = 16_000m;

        service.ProcessSignals(
            [new TradeSignal
            {
                Date = signalTime, Type = SignalType.Buy, Price = level, Symbol = "@NQ",
                StrategyName = Code, StrategyCode = Code, Quantity = 1m,
                OrderType = TradeOrderType.Stop, ValidFromUtc = projected, ExpiresAtUtc = projected,
                TimeframeMinutes = 240, CloseAtUtc = staleDeadline, Reason = "test"
            }],
            Prices(level - 10m), Bars(signalTime, level - 10m, level - 5m, level - 15m, level - 8m), signalTime);

        // Lunedì 08/01/2024 00:00Z (01:00 Roma): la prima barra vera, tocca il livello.
        var fillBar = new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);
        service.UpdateMarketPrices(Prices(level + 5m), Bars(fillBar, level - 2m, level + 5m, level - 3m, level + 2m), fillBar);

        var open = service.GetExecutionSnapshot(Code, "NQ", fillBar).Position;
        Assert.NotNull(open);
        Assert.Empty(service.GetClosedTrades());

        // Fine della sessione che contiene il fill: la sessione di lunedì 8 finisce alle 00:00 Roma
        // di martedì 9 = 23:00Z, e la deadline è il minuto prima.
        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@NQ"));
        var expected = grid.SessionOpenUtc(grid.SessionDayOf(fillBar).AddDays(1)).AddMinutes(-1);
        Assert.Equal(new DateTime(2024, 1, 8, 22, 59, 0, DateTimeKind.Utc), expected);

        // Un minuto prima della deadline la posizione è ancora aperta; alla deadline chiude a tempo.
        service.UpdateMarketPrices(Prices(level + 3m), Bars(expected.AddMinutes(-1), level + 3m, level + 4m, level + 2m, level + 3m), expected.AddMinutes(-1));
        Assert.Empty(service.GetClosedTrades());
        service.UpdateMarketPrices(Prices(level + 3m), Bars(expected, level + 3m, level + 4m, level + 2m, level + 3m), expected);
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TimeExit, trade.ExitReason);
        Assert.Equal(expected, trade.ExitDate);
    }

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };
}
