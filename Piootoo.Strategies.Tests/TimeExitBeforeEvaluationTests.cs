using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'ordine dentro un tick del loop: <b>prima</b> la strategia guarda la propria posizione, <b>poi</b>
/// <c>UpdateMarketPrices</c> applica le uscite a tempo. Su una barra che chiude esattamente a fine
/// sessione la strategia si vede quindi ancora in posizione e non emette l'ordine per la prima
/// barra della sessione dopo.
///
/// <para><b>Sembra un difetto ed e' una parita' misurata.</b> Il 16/09/2026 (7.4.6) l'ordine e'
/// stato invertito, con l'uscita dovuta eseguita prima della valutazione: sul future 2022-2025
/// <c>PT2_FDAX_PCH_001_240</c> e' passata da 850 trade e +274.000 — la scheda della ricerca ne ha
/// 884 e $264.639 — a 969 trade e +100.524, con 433 ingressi in piu' sulla barra notturna
/// 01:00-05:00 al posto di quelli delle 05:00-09:00. Il motore di ricerca fa come faceva il loop,
/// e la 7.4.7 lo ha ripristinato. Questo test tiene fermo l'ordine: chi lo cambia deve rimisurare.</para>
/// </summary>
public sealed class TimeExitBeforeEvaluationTests
{
    private const string Code = "PTS_TEST_TE_240";

    [Fact]
    public void OnTheDeadlineTickTheStrategyStillSeesItsPositionUntilTheMarkToMarket()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 8, 6, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddHours(4);
        var deadline = fillBar.AddHours(8);
        var level = 16_000m;

        service.ProcessSignals(
            [new TradeSignal
            {
                Date = signalTime, Type = SignalType.Buy, Price = level, Symbol = "@NQ",
                StrategyName = Code, StrategyCode = Code, Quantity = 1m,
                OrderType = TradeOrderType.Stop, ValidFromUtc = fillBar, ExpiresAtUtc = fillBar,
                TimeframeMinutes = 240, CloseAtUtc = deadline, Reason = "test"
            }],
            Prices(level - 10m), Bars(signalTime, level - 10m, level - 5m, level - 15m, level - 8m), signalTime);
        service.UpdateMarketPrices(Prices(level + 5m), Bars(fillBar, level - 2m, level + 5m, level - 3m, level + 2m), fillBar);
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", fillBar).Position);

        // Sul tick della deadline, PRIMA del mark-to-market, la posizione e' ancora li': e' cio'
        // che la strategia vede quando viene valutata su quella barra.
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", deadline).Position);

        // Il mark-to-market dello stesso tick la chiude a tempo.
        service.UpdateMarketPrices(Prices(level + 3m), Bars(deadline, level + 3m, level + 4m, level + 2m, level + 3m), deadline);
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", deadline).Position);
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TimeExit, trade.ExitReason);
        Assert.Equal(deadline, trade.ExitDate);
    }

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };
}
