using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedWorker.Dto
{
    public class CandleResponseDto
    {
        public string? Symbol { get; set; }
        public string? BarType { get; set; }
        public DateTime? BarEnd { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int CandleCount { get; set; }
        public List<CandleDto> Candles { get; set; } = new();
    }
}
