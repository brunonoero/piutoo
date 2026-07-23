//using Piootoo.Shared.Enums;
//using Piootoo.Shared.Interfaces;
//using Piootoo.Shared.Models;

//namespace Piootoo.Strategies;

///// <summary>
///// Strategia basata sull'indicatore RSI (Relative Strength Index)
///// </summary>
//public class RsiStrategy : ITradingStrategy
//{
//    private int _period = 14;
//    private decimal _oversoldLevel = 30;
//    private decimal _overboughtLevel = 70;
//    private string _symbol = "@ES";
//    private int _timeframeMinutes = 60; // Default 1 ora
    
//    public string Name => "RSI Strategy";
//    public string Description => "Strategia basata sull'indicatore RSI";
//    public string Symbol => _symbol;
//    public int TimeframeMinutes => _timeframeMinutes;
//    public int RequiredCandles => _period + 1; // RSI ha bisogno del periodo + 1 candela per il calcolo

//    public void Initialize(Dictionary<string, object>? parameters = null)
//    {
//        if (parameters != null)
//        {
//            if (parameters.TryGetValue("Period", out var period))
//                _period = Convert.ToInt32(period);
//            if (parameters.TryGetValue("OversoldLevel", out var oversoldLevel))
//                _oversoldLevel = Convert.ToDecimal(oversoldLevel);
//            if (parameters.TryGetValue("OverboughtLevel", out var overboughtLevel))
//                _overboughtLevel = Convert.ToDecimal(overboughtLevel);
//            if (parameters.TryGetValue("Symbol", out var symbol))
//                _symbol = symbol?.ToString() ?? "@ES";
//            if (parameters.TryGetValue("TimeframeMinutes", out var timeframe))
//                _timeframeMinutes = Convert.ToInt32(timeframe);
//        }
//    }

//    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
//    {
//        var currentIndex = Array.FindIndex(data, d => d.DateTime.Date == currentDate.Date);
        
//        if (currentIndex < 0)
//        {
//            currentIndex = data.Length - 1;
//        }
        
//        if (currentIndex < _period + 1)
//        {
//            return new TradeSignal
//            {
//                Date = currentDate,
//                Type = SignalType.Hold,
//                Price = data[currentIndex].Close,
//                StrategyName = Name,
//                Reason = "Dati insufficienti per il calcolo RSI"
//            };
//        }

//        var rsi = CalculateRSI(data, currentIndex, _period);
        
//        SignalType signal = SignalType.Hold;
//        string? reason = null;

//        if (rsi < _oversoldLevel)
//        {
//            signal = SignalType.Buy;
//            reason = $"RSI in oversold ({rsi:F2} < {_oversoldLevel})";
//        }
//        else if (rsi > _overboughtLevel)
//        {
//            signal = SignalType.Sell;
//            reason = $"RSI in overbought ({rsi:F2} > {_overboughtLevel})";
//        }

//        return new TradeSignal
//        {
//            Date = currentDate,
//            Type = signal,
//            Price = data[currentIndex].Close,
//            StrategyName = Name,
//            Reason = reason
//        };
//    }

//    private decimal CalculateRSI(OhlcvData[] data, int endIndex, int period)
//    {
//        decimal gainSum = 0;
//        decimal lossSum = 0;

//        for (int i = endIndex - period + 1; i <= endIndex; i++)
//        {
//            var change = data[i].Close - data[i - 1].Close;
//            if (change > 0)
//                gainSum += change;
//            else
//                lossSum -= change;
//        }

//        var avgGain = gainSum / period;
//        var avgLoss = lossSum / period;

//        if (avgLoss == 0) return 100;

//        var rs = avgGain / avgLoss;
//        return 100 - (100 / (1 + rs));
//    }
//}
