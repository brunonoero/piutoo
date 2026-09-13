using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'engine interno non inverte su segnale opposto (compare-0041). Mentre una posizione e' aperta
/// per (strategia, simbolo) l'ingresso opposto e' bloccato, come fanno il cBot
/// (<c>alreadyOpenOnStrategy</c> annulla l'intent) e il server (<c>AccountHasEntryInFlight</c>,
/// OCO); la posizione esce solo per le regole dichiarate nel segnale di ingresso — stop, target,
/// tempo — o per un segnale <c>ExitOnly</c> della strategia.
///
/// <para><b>Cosa costava.</b> Il ramo di inversione chiudeva la posizione per
/// <c>OppositeSignal</c> e ne apriva una contraria: 110 inversioni sull'anno di compare-0041, zero
/// nei trade di riferimento della ricerca, e nessuna nel cBot.</para>
/// </summary>
public sealed class OppositeSignalDoesNotReverseTests
{
    private const string Code = "PTS_TEST_60";
    private static readonly DateTime SessionStart = new(2025, 3, 3, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void OpenLongWithOppositeShortPending_IsNeitherClosedNorReversed_UntilItsStopLoss()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m, commissionPerContract: 0m);

        // Barra 1: stop buy a 105 con stop loss di 5 punti e target di 20.
        var t1 = SessionStart.AddHours(9);
        var entry = StopEntry(SignalType.Buy, 105m, t1);
        entry.StopLoss = 5m;
        entry.TakeProfit = 20m;
        service.ProcessSignals([entry], Prices(100m), Bar(t1, 100m, 101m, 99m, 100m), t1);

        // Barra 2: il prezzo rompe al rialzo e il long entra a 105.
        var t2 = t1.AddHours(1);
        service.UpdateMarketPrices(Prices(106m), Bar(t2, 100m, 107m, 101m, 106m), t2);
        var open = service.GetExecutionSnapshot(Code, "NQ", t2).Position;
        Assert.NotNull(open);
        Assert.Equal(SignalType.Buy, open!.Direction);

        // Barra 3: la stessa strategia emette lo short, sia a mercato sia come stop pending a 101
        // (sopra lo stop loss del long, che sta a 100).
        var t3 = t2.AddHours(1);
        service.ProcessSignals(
            [
                new TradeSignal
                {
                    Date = t3, Type = SignalType.Sell, OrderType = TradeOrderType.Market, Price = 104m,
                    Symbol = "NQ", StrategyCode = Code, StrategyName = Code, Quantity = 1
                },
                StopEntry(SignalType.Sell, 101m, t3)
            ],
            Prices(104m), Bar(t3, 106m, 106m, 103m, 104m), t3);
        Assert.Equal(SignalType.Buy, service.GetExecutionSnapshot(Code, "NQ", t3).Position?.Direction);

        // Barra 4: il prezzo attraversa il livello dello short (101) senza toccare lo stop del long.
        var t4 = t3.AddHours(1);
        service.UpdateMarketPrices(Prices(102m), Bar(t4, 104m, 104m, 100.5m, 102m), t4);

        var stillOpen = service.GetExecutionSnapshot(Code, "NQ", t4).Position;
        Assert.NotNull(stillOpen);
        Assert.Equal(SignalType.Buy, stillOpen!.Direction);
        Assert.Empty(service.GetClosedTrades());

        // Barra 5: il prezzo tocca lo stop loss. Il long esce per stop e non si apre nessuno short.
        var t5 = t4.AddHours(1);
        service.UpdateMarketPrices(Prices(99m), Bar(t5, 102m, 102m, 98m, 99m), t5);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", t5).Position);
        var closed = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, closed.ExitReason);
        Assert.DoesNotContain(service.GetClosedTrades(), trade => trade.ExitReason == TradeExitReason.OppositeSignal);
    }

    private static TradeSignal StopEntry(SignalType side, decimal level, DateTime barTime) => new()
    {
        Date = barTime,
        Type = side,
        Price = level,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        ValidFromUtc = barTime.AddHours(1),
        ExpiresAtUtc = barTime.AddHours(1),
        TimeframeMinutes = 60,
        MaxEntriesPerSession = 1,
        EntrySessionStartUtc = SessionStart
    };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bar(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };
}
