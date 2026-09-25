using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ordine Stop o Limit che nasce con il livello gia' superato: lo scarta o lo esegue a mercato
/// secondo quanto dichiara la strategia sul segnale (<see cref="CrossedLevelPolicy"/>).
///
/// <para><b>Perche' questi test esistono.</b> Fino al 25/09/2026 lo decideva solo chi eseguiva, con
/// un interruttore globale (<c>RejectWrongSideLevels</c>), e la risposta era sempre "scarta". Per la
/// serie PT5DAV era sbagliata: il suo simulatore riempie quell'ordine all'apertura della barra, e sulle
/// 41 con tenuta >= 1,5 lo scarto toglieva 229 ingressi e 235 k sull'anno broker. La regola ora sta
/// sul segnale, il cBot legge lo stesso campo dall'intent, e il default resta lo scarto per le serie
/// validate cosi'.</para>
/// </summary>
public sealed class CrossedLevelPolicyTests
{
    private const string Code = "PT5DAV_NQ_TFM_001_60";

    [Fact]
    public void ACrossedStopDeclaredMarketFillsAtTheBarOpen()
    {
        var service = NewService();
        var (signalTime, fillBar) = Times();

        // Stop buy a 15.010, la barra dopo apre a 15.020: livello gia' superato.
        service.ProcessSignals(
            [Signal(SignalType.Buy, TradeOrderType.Stop, 15_010m, signalTime, fillBar, CrossedLevelPolicy.Market)],
            Prices(15_000m), Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m), signalTime);
        service.UpdateMarketPrices(
            Prices(15_022m), Bars(fillBar, open: 15_020m, high: 15_025m, low: 15_018m, close: 15_022m), fillBar);

        var position = service.GetExecutionSnapshot(Code, "NQ", fillBar).Position;
        Assert.NotNull(position);
        Assert.Equal(15_020m, position!.EntryPrice);
        Assert.Equal(1, service.WrongSideLevelsExecutedAtMarket);
        Assert.Equal(0, service.WrongSideLevelsRejected);
    }

    [Fact]
    public void ACrossedStopWithTheDefaultPolicyIsStillRejected()
    {
        var service = NewService();
        var (signalTime, fillBar) = Times();

        var signal = Signal(SignalType.Buy, TradeOrderType.Stop, 15_010m, signalTime, fillBar, CrossedLevelPolicy.Reject);
        Assert.Equal(CrossedLevelPolicy.Reject, new TradeSignal().CrossedLevel);

        service.ProcessSignals(
            [signal], Prices(15_000m), Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m), signalTime);
        service.UpdateMarketPrices(
            Prices(15_022m), Bars(fillBar, open: 15_020m, high: 15_025m, low: 15_018m, close: 15_022m), fillBar);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", fillBar).Position);
        Assert.Equal(1, service.WrongSideLevelsRejected);
        Assert.Equal(0, service.WrongSideLevelsExecutedAtMarket);
    }

    /// <summary>
    /// Limit buy a 15.010 con la barra che apre a 14.990 e non scende oltre: un limite da penetrare
    /// non si riempirebbe mai, ma e' gia' sotto il livello — un broker lo eseguirebbe subito — e la
    /// strategia lo vuole a mercato. Entra all'apertura.
    /// </summary>
    [Fact]
    public void ACrossedLimitDeclaredMarketFillsAtTheBarOpen()
    {
        var service = NewService();
        var (signalTime, fillBar) = Times();

        service.ProcessSignals(
            [Signal(SignalType.Buy, TradeOrderType.Limit, 15_010m, signalTime, fillBar, CrossedLevelPolicy.Market)],
            Prices(15_000m), Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m), signalTime);
        service.UpdateMarketPrices(
            Prices(14_995m), Bars(fillBar, open: 14_990m, high: 15_000m, low: 14_990m, close: 14_995m), fillBar);

        var position = service.GetExecutionSnapshot(Code, "NQ", fillBar).Position;
        Assert.NotNull(position);
        Assert.Equal(14_990m, position!.EntryPrice);
        Assert.Equal(1, service.WrongSideLevelsExecutedAtMarket);
    }

    /// <summary>
    /// Un livello non superato resta un pending qualunque cosa dichiari la strategia: la politica
    /// vale solo per l'ordine che nasce dalla parte sbagliata.
    /// </summary>
    [Fact]
    public void AnUncrossedStopDeclaredMarketStaysAPendingOrder()
    {
        var service = NewService();
        var (signalTime, fillBar) = Times();

        service.ProcessSignals(
            [Signal(SignalType.Buy, TradeOrderType.Stop, 15_050m, signalTime, fillBar, CrossedLevelPolicy.Market)],
            Prices(15_000m), Bars(signalTime, 15_000m, 15_005m, 14_995m, 15_000m), signalTime);
        service.UpdateMarketPrices(
            Prices(15_022m), Bars(fillBar, open: 15_020m, high: 15_040m, low: 15_018m, close: 15_022m), fillBar);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", fillBar).Position);
        Assert.Equal(0, service.WrongSideLevelsExecutedAtMarket);
    }

    private static PiootooTradingService NewService()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        return service;
    }

    private static (DateTime SignalTime, DateTime FillBar) Times()
    {
        var signalTime = new DateTime(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
        return (signalTime, signalTime.AddMinutes(60));
    }

    private static TradeSignal Signal(
        SignalType side, TradeOrderType orderType, decimal price, DateTime signalTime, DateTime validFrom,
        CrossedLevelPolicy crossedLevel) =>
        new()
        {
            Date = signalTime,
            Type = side,
            Price = price,
            Symbol = "@NQ",
            StrategyName = Code,
            StrategyCode = Code,
            Quantity = 1m,
            OrderType = orderType,
            CrossedLevel = crossedLevel,
            ValidFromUtc = validFrom,
            ExpiresAtUtc = validFrom,
            StopLossMoneyPerFutureContract = 5_000m,
            TakeProfitMoneyPerFutureContract = 5_000m,
            Reason = "test"
        };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };
}
