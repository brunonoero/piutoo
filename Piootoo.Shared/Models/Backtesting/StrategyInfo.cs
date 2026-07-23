namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Informazioni su una strategia utilizzata nel backtesting
/// </summary>
public class StrategyInfo
{
    /// <summary>
    /// Nome della strategia
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Codice univoco usato dal motore trading per isolare le posizioni
    /// </summary>
    public string StrategyCode { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo della strategia
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe in minuti
    /// </summary>
    public int TimeframeMinutes { get; set; }
}
