using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FeedWorker.Dto
{
    /// <summary>
    /// Rappresenta una singola candela OHLCV (risposta API)
    /// </summary>
    public class Candle
    {
        [JsonPropertyName("time")]
        public double Time { get; set; }

        [JsonPropertyName("open")]
        public double Open { get; set; }

        [JsonPropertyName("high")]
        public double High { get; set; }

        [JsonPropertyName("low")]
        public double Low { get; set; }

        [JsonPropertyName("close")]
        public double Close { get; set; }

        [JsonPropertyName("volume")]
        public double Volume { get; set; }

        public DateTime GetDateTime() => DateTimeOffset.FromUnixTimeSeconds((long)Time).UtcDateTime;

        /// <summary>
        /// Converte la candela in DTO con DateTime leggibile
        /// </summary>
        public CandleDto ToDto() => new()
        {
            Timestamp = Time,
            DateTime = GetDateTime(),
            DateTimeFormatted = GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume
        };

        public override string ToString() =>
            $"[{GetDateTime():yyyy-MM-dd HH:mm}] O:{Open:F2} H:{High:F2} L:{Low:F2} C:{Close:F2} V:{Volume:F0}";
    }

}
