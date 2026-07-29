using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Proietta un <see cref="TradeSignal"/> nel contratto persistito, conservando
/// tutte le condizioni di uscita (punti, USD/contratto, break even, tempo, barre).
/// </summary>
public static class PersistedSignalMapper
{
    public static PersistedSignal FromTradeSignal(
        TradeSignal signal,
        string signalId,
        string? correlationId = null,
        string? accountId = null,
        string? accountSymbol = null,
        decimal contractMultiplier = 1m)
    {
        ArgumentNullException.ThrowIfNull(signal);

        return new PersistedSignal
        {
            SignalId = signalId,
            CorrelationId = correlationId,
            TimestampUtc = TradingDateTime.ToFeedUtc(signal.Date),
            StrategyCode = signal.StrategyCode,
            StrategyName = signal.StrategyName,
            Symbol = NormalizeSymbol(signal.Symbol),
            AccountId = accountId ?? string.Empty,
            AccountSymbol = string.IsNullOrWhiteSpace(accountSymbol)
                ? NormalizeSymbol(signal.Symbol)
                : accountSymbol,
            ContractMultiplier = contractMultiplier,
            Side = signal.Type,
            OrderType = signal.OrderType,
            TriggerPrice = signal.Price,
            Quantity = signal.Quantity,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            StopLossMoneyPerFutureContract = signal.StopLossMoneyPerFutureContract,
            TakeProfitMoneyPerFutureContract = signal.TakeProfitMoneyPerFutureContract,
            BreakEven = signal.BreakEven,
            TimeExitUtc = signal.CloseAtUtc,
            MaxBarsInPosition = signal.MaxBarsInPosition,
            Reason = signal.Reason
        };
    }

    private static string NormalizeSymbol(string symbol) =>
        symbol.Trim().TrimStart('@').ToUpperInvariant();
}
