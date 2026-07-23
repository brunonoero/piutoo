using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models;

/// <summary>
/// Risultato di un singolo trade
/// </summary>
public class TradingResult
{
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyCode { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public DateTime ExitDate { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public SignalType Direction { get; set; } // Buy = Long, Sell = Short
    public decimal ContractPointValue { get; set; } = 1m;
    
    // Calcoli automatici
    public decimal GrossProfit => Direction == SignalType.Buy 
        ? (ExitPrice - EntryPrice) * Quantity * ContractPointValue
        : (EntryPrice - ExitPrice) * Quantity * ContractPointValue;
    
    public decimal Commission { get; set; }
    public decimal NetProfit => GrossProfit - Commission;
    public decimal ReturnPercent => EntryPrice != 0 ? (NetProfit / (EntryPrice * Quantity)) * 100 : 0;
    public bool IsWinner => NetProfit > 0;
    public TimeSpan Duration => ExitDate - EntryDate;
}
