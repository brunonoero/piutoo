using Piootoo.Shared.Models;

namespace Piootoo.Shared.Interfaces;

/// <summary>
/// Optional extension for strategies that need additional OHLCV streams besides their execution timeframe.
/// </summary>
public interface IMultiTimeframeTradingStrategy : ITradingStrategy
{
    /// <summary>
    /// Additional timeframes, in minutes, required for the same strategy symbol.
    /// The primary <see cref="ITradingStrategy.TimeframeMinutes" /> is supplied separately.
    /// </summary>
    IReadOnlyCollection<int> AdditionalTimeframes { get; }

    /// <summary>
    /// Generates a signal with access to the primary timeframe and optional additional timeframe data.
    /// The dictionary key is the timeframe in minutes.
    /// </summary>
    TradeSignal GenerateSignal(
        OhlcvData[] data,
        IReadOnlyDictionary<int, OhlcvData[]> additionalData,
        DateTime currentDate);
}
