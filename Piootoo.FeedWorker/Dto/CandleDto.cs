using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedWorker.Dto
{
    /// <summary>
    /// DTO per candela con DateTime in formato leggibile
    /// </summary>
    public class CandleDto
    {
        public double Timestamp { get; set; }
        public DateTime DateTime { get; set; }
        public string DateTimeFormatted { get; set; } = string.Empty;
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        
        /// <summary>
        /// Volume High: somma dei volumi delle candele verdi (close > open) utilizzate per aggregare questa candela
        /// </summary>
        public double? VolumeHigh { get; set; }
        
        /// <summary>
        /// Volume Low: somma dei volumi delle candele rosse (close < open) utilizzate per aggregare questa candela
        /// </summary>
        public double? VolumeLow { get; set; }
    }
}
