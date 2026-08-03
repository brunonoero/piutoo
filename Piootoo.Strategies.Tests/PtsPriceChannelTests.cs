using System.Reflection;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Verifica la specifica PC di PTS_NQ_PCH_001: canale inclusivo della barra corrente, buffer NQ,
/// condizioni di uscita autocontenute e trailing stop monetario per contratto.
/// </summary>
public sealed class PtsPriceChannelTests
{
    [Fact]
    public void PtsPch001_EmitsLongStopAboveCurrentInclusive100BarChannel()
    {
        var strategy = new PTS_NQ_PCH_001_15();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["PtnNeutNo"] = 56,
            ["PtnDirYes"] = 52
        });
        var bars = BuildBars();
        var last = bars[^1];

        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = last.DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = "PTS_NQ_PCH_001_15",
                Symbol = "NQ",
                BarTimeUtc = last.DateTime
            }
        });

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Null(signal.CompanionSignals);
        Assert.Equal("@NQ", signal.Symbol);
        Assert.Equal("PTS_NQ_PCH_001_15", signal.StrategyCode);
        // Massimo delle 100 barre incl. quella corrente (999) + 2 tick di buffer + 1 tick di
        // penetrazione del livello, che è la convenzione del motore Python.
        Assert.Equal(999.75m, signal.Price);
        Assert.Equal(last.DateTime.AddMinutes(15), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        // La sessione è il giorno di calendario UTC, non la CME 17:00–16:00: con quest'ultima il
        // secchio sarebbe il 7 gennaio alle 17:00 e un ingresso serale bloccherebbe il pomeriggio
        // successivo, che nel motore di riferimento è invece una sessione a sé.
        Assert.Equal(new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
        Assert.Equal(250m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(5000m, signal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(1000m, signal.TrailingStopMoneyPerFutureContract);
        Assert.Equal(1000m, signal.BreakEvenMoneyPerFutureContract);
        Assert.Null(signal.MaxBarsInPosition);
        Assert.Null(signal.CloseAtUtc); // intraday_only = 0: nessuna chiusura di fine sessione.
    }

    /// <summary>
    /// <c>PriceChannelEngine.IntradayOnly</c> vale <c>true</c> per default: le PC multiday devono
    /// disattivarlo esplicitamente. Dimenticarlo mette <c>CloseAtUtc</c> alle 16:00 su ogni segnale
    /// senza rompere nessun contratto, quindi la regressione passerebbe inosservata nei test e si
    /// vedrebbe solo nei risultati.
    /// </summary>
    [Theory]
    [InlineData(typeof(PTS_NQ_PCH_001_15))]
    [InlineData(typeof(PTS_NQ_PCH_002_15))]
    public void PcPtsStrategies_DoNotDeclareSessionEndClose(Type strategyType)
    {
        var strategy = Activator.CreateInstance(strategyType)!;
        var field = typeof(PriceChannelEngine).GetField(
            "IntradayOnly",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        Assert.False((bool)field!.GetValue(strategy)!);
    }

    [Fact]
    public void Engine_ClosesLongAtTrailingStopFromFavorableHigh()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 2, 13, 0, 0, DateTimeKind.Utc);
        var fillTime = signalTime.AddMinutes(15);
        var exitTime = fillTime.AddMinutes(15);
        var signal = new TradeSignal
        {
            Date = signalTime,
            Type = SignalType.Buy,
            Price = 100m,
            Symbol = "@NQ",
            StrategyName = "PTS_NQ_PCH_001_15",
            StrategyCode = "PTS_NQ_PCH_001_15",
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = fillTime,
            ExpiresAtUtc = fillTime,
            Quantity = 1m,
            StopLossMoneyPerFutureContract = 5000m,
            TrailingStopMoneyPerFutureContract = 1000m
        };

        service.ProcessSignals(
            [signal],
            Prices(99m),
            Bars(signalTime, 99m, 99m, 98m, 99m),
            signalTime);
        service.UpdateMarketPrices(
            Prices(101m),
            Bars(fillTime, 99m, 101m, 99m, 100m),
            fillTime);
        service.UpdateMarketPrices(
            Prices(120m),
            Bars(exitTime, 101m, 170m, 119m, 120m),
            exitTime);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TrailingStop, trade.ExitReason);
        Assert.Equal(120m, trade.ExitPrice); // 170 - ($1.000 / $20 per punto).
        Assert.Equal(400m, trade.GrossProfit);
    }

    [Fact]
    public void Engine_SameFillBarTouchingStopAndTarget_PrioritizesStopLoss()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var barTime = new DateTime(2024, 1, 2, 13, 15, 0, DateTimeKind.Utc);
        var signal = new TradeSignal
        {
            Date = barTime.AddMinutes(-15),
            Type = SignalType.Buy,
            Price = 100m,
            Symbol = "@NQ",
            StrategyName = "PTS_NQ_PCH_001_15",
            StrategyCode = "PTS_NQ_PCH_001_15",
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = barTime,
            ExpiresAtUtc = barTime,
            Quantity = 1m,
            StopLoss = 10m,
            TakeProfit = 10m
        };

        // La barra raggiunge il livello di ingresso, poi contiene entrambi i livelli di uscita.
        // Senza dati tick l'engine applica deliberatamente la politica conservativa: SL prima di TP.
        service.ProcessSignals(
            [signal],
            Prices(100m),
            Bars(barTime, 100m, 115m, 85m, 100m),
            barTime);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal(90m, trade.ExitPrice);
        Assert.Equal(-200m, trade.GrossProfit);
    }

    [Fact]
    public void Engine_BlocksSecondFilledEntryInTheSameConfiguredSession()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var sessionStart = new DateTime(2024, 1, 1, 17, 0, 0, DateTimeKind.Utc);
        var firstBar = new DateTime(2024, 1, 2, 13, 15, 0, DateTimeKind.Utc);
        var secondBar = firstBar.AddMinutes(15);
        TradeSignal CreateSignal(DateTime time) => new()
        {
            Date = time,
            Type = SignalType.Buy,
            Price = 100m,
            Symbol = "@NQ",
            StrategyName = "PTS_NQ_PCH_001_15",
            StrategyCode = "PTS_NQ_PCH_001_15",
            Quantity = 1m,
            StopLoss = 10m,
            MaxEntriesPerSession = 1,
            EntrySessionStartUtc = sessionStart
        };

        // Il primo fill si chiude nello stesso bar. Il secondo setup avrebbe le
        // medesime condizioni, ma non deve produrre un nuovo trade.
        service.ProcessSignals(
            [CreateSignal(firstBar)],
            Prices(100m),
            Bars(firstBar, 100m, 100m, 90m, 95m),
            firstBar);
        service.ProcessSignals(
            [CreateSignal(secondBar)],
            Prices(100m),
            Bars(secondBar, 100m, 100m, 90m, 95m),
            secondBar);

        Assert.Single(service.GetClosedTrades());
        Assert.Equal(0, service.GetSnapshot().OpenPositionsCount);
    }

    private static OhlcvData[] BuildBars()
    {
        var bars = new OhlcvData[577];
        var lastTime = new DateTime(2024, 1, 8, 13, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < bars.Length; index++)
        {
            var price = 100m + index;
            bars[index] = new OhlcvData
            {
                DateTime = lastTime.AddMinutes((index - bars.Length + 1) * 15),
                Open = price - 1m,
                High = index == bars.Length - 1 ? 999m : price,
                Low = price - 2m,
                Close = price - 0.5m,
                Volume = 1m
            };
        }

        return bars;
    }

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 1m
            }
        };
}
