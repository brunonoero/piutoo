using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Piootoo.Strategies.PiutooStrategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Audit deterministico di PTS_NQ_TFM_001_60 + motore interno: forma del segnale,
/// conversione $/contratto → punti NQ, fill stop, SL/TP e P&amp;L.
/// Nessuna correzione: i test documentano anche i comportamenti errati confermati.
/// </summary>
public sealed class PtsEngineAuditTests
{
    private const decimal NqDollarsPerPoint = 20m;
    private const decimal StopMoney = 1000m;
    private const decimal TakeProfitMoney = 3000m;

    [Fact]
    public void PtsTfm001_EmitsNextBarStop_WithMoneyRiskPerContract()
    {
        var strategy = CreatePtsWithNeutralPatterns();
        // TfMirroredEngine richiede sei sessioni piene (144 barre a 60m).
        var bars = BuildPtsBars(200);
        var bar = bars[^1];

        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bar.DateTime,
            Execution = Snapshot("PTS_NQ_TFM_001_60", bar.DateTime)
        });

        Assert.True(signal.Type is SignalType.Buy or SignalType.Sell,
            $"Atteso ingresso stop, ottenuto {signal.Type}. Reason={signal.Reason}");
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal("PTS_NQ_TFM_001_60", signal.StrategyCode);
        Assert.Equal("@NQ", signal.Symbol);
        Assert.Equal(1m, signal.Quantity);
        Assert.Equal(StopMoney, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(TakeProfitMoney, signal.TakeProfitMoneyPerFutureContract);
        Assert.Null(signal.StopLoss);
        Assert.Null(signal.TakeProfit);
        Assert.Equal(bar.DateTime.AddMinutes(60), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    [Fact]
    public void Engine_ConvertsPtsMoneyRiskToNqPoints_AndHitsTakeProfit()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);
        var exitBar = fillBar.AddMinutes(60);
        var stopPrice = 15_000m;

        service.ProcessSignals(
            [PtsStopSignal(SignalType.Buy, stopPrice, signalTime, fillBar)],
            Prices(stopPrice - 10m),
            Bars(signalTime, stopPrice - 10m, stopPrice - 5m, stopPrice - 15m, stopPrice - 8m),
            signalTime);

        Assert.Null(service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", signalTime).Position);

        decimal? openedStopPoints = null;
        decimal? openedTakeProfitPoints = null;
        decimal? entryPrice = null;
        service.PositionOpened += ev =>
        {
            openedStopPoints = ev.StopLossPoints;
            openedTakeProfitPoints = ev.TakeProfitPoints;
            entryPrice = ev.EntryPrice;
        };

        // Fill allo stop: high tocca il livello.
        service.UpdateMarketPrices(
            Prices(stopPrice + 5m),
            Bars(fillBar, stopPrice - 2m, stopPrice + 5m, stopPrice - 3m, stopPrice + 2m),
            fillBar);

        var open = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position;
        Assert.NotNull(open);
        Assert.Equal(SignalType.Buy, open!.Direction);
        Assert.Equal(1m, open.Contracts);
        Assert.Equal(StopMoney / NqDollarsPerPoint, openedStopPoints);       // 50 punti
        Assert.Equal(TakeProfitMoney / NqDollarsPerPoint, openedTakeProfitPoints); // 150 punti

        // Barra successiva: high raggiunge TP (entry + 150).
        var entry = entryPrice ?? open.EntryPrice;
        var tpPrice = entry + 150m;
        service.UpdateMarketPrices(
            Prices(tpPrice),
            Bars(exitBar, entry + 10m, tpPrice + 5m, entry - 5m, tpPrice),
            exitBar);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TakeProfit, trade.ExitReason);
        Assert.Equal(tpPrice, trade.ExitPrice);
        Assert.Equal(TakeProfitMoney, trade.GrossProfit); // 150 × 1 × 20
        Assert.Equal(0m, trade.Commission);
        Assert.Equal(TakeProfitMoney, trade.NetProfit);
    }

    [Fact]
    public void Engine_HitsStopLoss_WithExpectedMoneyLoss()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);
        var exitBar = fillBar.AddMinutes(60);
        var stopPrice = 15_000m;

        service.ProcessSignals(
            [PtsStopSignal(SignalType.Buy, stopPrice, signalTime, fillBar)],
            Prices(stopPrice - 10m),
            Bars(signalTime, stopPrice - 10m, stopPrice - 5m, stopPrice - 15m, stopPrice - 8m),
            signalTime);

        service.UpdateMarketPrices(
            Prices(stopPrice + 2m),
            Bars(fillBar, stopPrice, stopPrice + 2m, stopPrice - 1m, stopPrice + 1m),
            fillBar);

        var entry = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position!.EntryPrice;
        var slPrice = entry - 50m;
        service.UpdateMarketPrices(
            Prices(slPrice),
            Bars(exitBar, entry - 10m, entry + 5m, slPrice - 2m, slPrice),
            exitBar);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal(slPrice, trade.ExitPrice);
        Assert.Equal(-StopMoney, trade.GrossProfit);
    }

    [Fact]
    /// <summary>
    /// Con il filtro spento — semantica TradeStation, quella del motore di ricerca — uno stop il
    /// cui livello e' gia' scavalcato all'apertura si riempie all'apertura.
    /// </summary>
    public void Engine_GapAwareStopFill_UsesMaxOpenAndLevel()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.RejectWrongSideLevels = false;

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);
        var stopPrice = 15_010m;

        service.ProcessSignals(
            [PtsStopSignal(SignalType.Buy, stopPrice, signalTime, fillBar)],
            Prices(15_000m),
            Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m),
            signalTime);

        service.UpdateMarketPrices(
            Prices(15_025m),
            Bars(fillBar, open: 15_020m, high: 15_025m, low: 15_018m, close: 15_022m),
            fillBar);

        var open = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position;
        Assert.NotNull(open);
        Assert.Equal(15_020m, open!.EntryPrice); // max(open, stop)
    }

    /// <summary>
    /// Lo stesso scenario col filtro ACCESO, che e' il default: il cBot quel livello non lo piazza
    /// nemmeno (<c>RejectWrongSideLevels</c>, "dal lato sbagliato"), quindi il trade nel conto vero
    /// non esiste e non deve esistere neanche qui. E' la differenza che il confronto del 26/08/2026
    /// ha misurato: 53 giornate, 644 scarti nel log del bot, tutte su una sola strategia.
    /// </summary>
    [Fact]
    public void Engine_StopGiaScavalcatoAllApertura_NonVienePiazzato()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        Assert.True(service.RejectWrongSideLevels); // default allineato al cBot

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);

        service.ProcessSignals(
            [PtsStopSignal(SignalType.Buy, 15_010m, signalTime, fillBar)],
            Prices(15_000m),
            Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m),
            signalTime);

        service.UpdateMarketPrices(
            Prices(15_025m),
            Bars(fillBar, open: 15_020m, high: 15_025m, low: 15_018m, close: 15_022m),
            fillBar);

        Assert.Null(service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position);
        Assert.Equal(1, service.WrongSideLevelsRejected);
    }

    /// <summary>
    /// Il livello ESATTAMENTE sull'apertura non e' "gia' superato": e' il breakout che comincia li',
    /// il fill sarebbe comunque l'apertura, e scartarlo toglierebbe trade sani.
    /// </summary>
    [Fact]
    public void Engine_StopEsattamenteSullApertura_VienePiazzato()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);

        service.ProcessSignals(
            [PtsStopSignal(SignalType.Buy, 15_020m, signalTime, fillBar)],
            Prices(15_000m),
            Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m),
            signalTime);

        service.UpdateMarketPrices(
            Prices(15_025m),
            Bars(fillBar, open: 15_020m, high: 15_025m, low: 15_018m, close: 15_022m),
            fillBar);

        var open = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position;
        Assert.NotNull(open);
        Assert.Equal(15_020m, open!.EntryPrice);
        Assert.Equal(0, service.WrongSideLevelsRejected);
    }

    [Fact]
    public void Engine_CompanionOco_CancelsOppositePendingAfterFill()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddMinutes(60);
        var later = fillBar.AddMinutes(60);
        var longStop = 15_100m;
        var shortStop = 14_900m;

        // SL enorme: così toccare lo short non chiude prima per stop loss monetario.
        // ExpiresAtUtc oltre la barra later: altrimenti lo short scadrebbe da solo e non
        // verificheremmo il CancelPendingOrders OCO.
        var longSignal = PtsStopSignal(SignalType.Buy, longStop, signalTime, fillBar);
        longSignal.StopLossMoneyPerFutureContract = 100_000m;
        longSignal.ExpiresAtUtc = later.AddHours(1);
        var shortSignal = PtsStopSignal(SignalType.Sell, shortStop, signalTime, fillBar);
        shortSignal.StopLossMoneyPerFutureContract = 100_000m;
        shortSignal.ExpiresAtUtc = later.AddHours(1);
        // Il backtest espande i companion; ProcessSignals riceve la lista già piatta.
        service.ProcessSignals(
            [longSignal, shortSignal],
            Prices(15_000m),
            Bars(signalTime, 15_000m, 15_050m, 14_950m, 15_000m),
            signalTime);

        // Solo il long viene toccato.
        service.UpdateMarketPrices(
            Prices(15_110m),
            Bars(fillBar, 15_000m, 15_110m, 14_990m, 15_100m),
            fillBar);

        var snap = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar);
        Assert.NotNull(snap.Position);
        Assert.Equal(SignalType.Buy, snap.Position!.Direction);

        // Barra che toccherebbe lo short: con OCO il pending short è già cancellato,
        // quindi la posizione resta long (non reverse).
        service.UpdateMarketPrices(
            Prices(14_850m),
            Bars(later, 15_050m, 15_060m, 14_850m, 14_880m),
            later);

        var after = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", later).Position;
        Assert.NotNull(after);
        Assert.Equal(SignalType.Buy, after!.Direction);
        Assert.Empty(service.GetClosedTrades());
    }

    [Fact]
    public void AccountConversion_FractionalMultiplier_PreservesDecimalQuantity()
    {
        // Conversione account: 1 contratto Piootoo × 0.01 = 0.01 contratti account.
        var conversion = AccountSymbolConversion.FromAccount(
            new WorkspaceAccount
            {
                Id = "micro-nq",
                Name = "Micro NQ",
                InitialBalance = 50_000m
            },
            new SymbolConversion
            {
                Code = "micro-futures",
                Name = "Micro futures",
                Mappings =
                [
                    new AccountSymbolMapping
                    {
                        Symbol = "@NQ",
                        AccountSymbol = "MNQ",
                        ContractMultiplier = 0.01m,
                        Enabled = true
                    }
                ]
            });

        Assert.Equal(0.01m, conversion.GetContractMultiplier("NQ"));
        Assert.Equal("MNQ", conversion.GetAccountSymbol("@NQ"));

        var signal = PtsStopSignal(SignalType.Buy, 15_000m,
            new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc));
        signal.Quantity *= conversion.GetContractMultiplier(signal.Symbol);
        Assert.Equal(0.01m, signal.Quantity);

        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        var fillBar = signal.ValidFromUtc!.Value;

        service.ProcessSignals(
            [signal],
            Prices(14_990m),
            Bars(signal.Date, 14_990m, 14_995m, 14_980m, 14_990m),
            signal.Date);

        service.UpdateMarketPrices(
            Prices(15_010m),
            Bars(fillBar, 15_000m, 15_010m, 14_995m, 15_005m),
            fillBar);

        var open = service.GetExecutionSnapshot("PTS_NQ_TFM_001_60", "NQ", fillBar).Position;
        Assert.NotNull(open);
        Assert.Equal(0.01m, open!.Contracts);

        var exitBar = fillBar.AddMinutes(60);
        var tp = open.EntryPrice + 150m;
        service.UpdateMarketPrices(
            Prices(tp),
            Bars(exitBar, open.EntryPrice + 10m, tp + 1m, open.EntryPrice - 5m, tp),
            exitBar);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(30m, trade.GrossProfit); // 0.01 × $3000
    }

    [Fact]
    public void AccountConversion_DisabledSymbol_IsRejectedByLookupRules()
    {
        var conversion = AccountSymbolConversion.FromAccount(
            new WorkspaceAccount
            {
                Id = "disabled-nq",
                Name = "Disabled",
                InitialBalance = 100_000m
            },
            new SymbolConversion
            {
                Code = "cfd-disabled",
                Name = "CFD disabled",
                Mappings =
                [
                    new AccountSymbolMapping
                    {
                        Symbol = "@NQ",
                        AccountSymbol = "USDTEC",
                        ContractMultiplier = 1m,
                        Enabled = false
                    }
                ]
            });

        Assert.False(conversion.IsSymbolEnabled("@NQ"));
        Assert.True(conversion.IsSymbolEnabled("@GC")); // assente → non bloccato
    }

    private static PTS_NQ_TFM_001_60 CreatePtsWithNeutralPatterns()
    {
        var strategy = new PTS_NQ_TFM_001_60();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["PtnNeutYes"] = 55, // sempre true
            ["PtnNeutNo"] = 99,  // sempre false
            ["PtnDirYes"] = 52,  // sempre true
            ["PtnDirNo"] = 99,   // sempre false
            ["StopLoss"] = 1000,
            ["TakeProfit"] = 3000,
            ["Contracts"] = 1
        });
        return strategy;
    }

    private static TradeSignal PtsStopSignal(
        SignalType side, decimal price, DateTime signalTime, DateTime validFrom) =>
        new()
        {
            Date = signalTime,
            Type = side,
            Price = price,
            Symbol = "@NQ",
            StrategyName = "PTS_NQ_TFM_001_60",
            StrategyCode = "PTS_NQ_TFM_001_60",
            Quantity = 1m,
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = validFrom,
            ExpiresAtUtc = validFrom,
            StopLossMoneyPerFutureContract = StopMoney,
            TakeProfitMoneyPerFutureContract = TakeProfitMoney,
            Reason = "audit"
        };

    private static StrategyExecutionSnapshot Snapshot(string code, DateTime barTime) =>
        new()
        {
            StrategyCode = code,
            Symbol = "NQ",
            BarTimeUtc = barTime,
            EntriesToday = 0
        };

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
                Volume = 1
            }
        };

    /// <summary>
    /// Costruisce sessioni CME 17:00–16:00 con barre orarie, chiudendo in finestra operativa 16:00–03:00.
    /// </summary>
    private static OhlcvData[] BuildPtsBars(int count)
    {
        var bars = new List<OhlcvData>(count);
        // Parte da una sera precedente per dare a OHLCMulti5 sessioni complete.
        var cursor = new DateTime(2024, 1, 1, 17, 0, 0, DateTimeKind.Utc);
        decimal price = 15_000m;
        while (bars.Count < count)
        {
            var hhmm = cursor.Hour * 100 + cursor.Minute;
            var inSession = hhmm >= 1700 || hhmm <= 1600;
            if (inSession)
            {
                bars.Add(new OhlcvData
                {
                    DateTime = cursor,
                    Open = price,
                    High = price + 20m,
                    Low = price - 20m,
                    Close = price + 5m,
                    Volume = 10
                });
                price += 5m;
            }

            cursor = cursor.AddHours(1);
        }

        // Assicura che l'ultima barra sia nella finestra operativa 16:00–03:00.
        var last = bars[^1];
        if (last.DateTime.Hour is < 16 and > 3)
        {
            bars[^1] = new OhlcvData
            {
                DateTime = last.DateTime.Date.AddHours(16),
                Open = last.Open,
                High = last.High,
                Low = last.Low,
                Close = last.Close,
                Volume = last.Volume
            };
        }

        return bars.OrderBy(b => b.DateTime).ToArray();
    }
}
