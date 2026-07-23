namespace Piootoo.Shared.Models;

/// <summary>
/// Rappresenta un dato OHLCV (Open, High, Low, Close, Volume)
/// </summary>
public class OhlcvData
{
    public long Timestamp { get; set; }
    /// <summary>
    /// Timestamp della candela in UTC (allineato al feed JSON).
    /// </summary>
    public DateTime DateTime { get; set; }
    public string DateTimeFormatted { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
