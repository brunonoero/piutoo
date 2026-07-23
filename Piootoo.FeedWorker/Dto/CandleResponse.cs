using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FeedWorker.Dto
{
    /// <summary>
    /// Risposta dell'API per i dati storici
    /// </summary>
    public class CandleResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("bar_type")]
        public string? BarType { get; set; }

        [JsonPropertyName("bar_end")]
        public double? BarEnd { get; set; }

        [JsonPropertyName("last_update")]
        public long? LastUpdate { get; set; }

        [JsonPropertyName("series")]
        public List<Candle>? Series { get; set; }
    }
}
