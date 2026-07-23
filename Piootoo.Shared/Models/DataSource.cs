namespace Piootoo.Shared.Models;

/// <summary>
/// Rappresenta un datasource con dati OHLCV
/// </summary>
public class DataSource
{
    public string Symbol { get; set; } = string.Empty;
    public string BarType { get; set; } = string.Empty;
    public string? BarEnd { get; set; }
    public DateTime LastUpdate { get; set; }
    public int CandleCount { get; set; }
    public List<OhlcvData> Candles { get; set; } = new();
}
