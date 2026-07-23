//using Piootoo.Shared.Enums;
//using Piootoo.Shared.Interfaces;
//using Piootoo.Shared.Models;

//namespace Piootoo.Strategies;

///// <summary>
///// Strategia basata sull'incrocio di medie mobili (Golden Cross / Death Cross)
///// </summary>
//public class MovingAverageStrategy : ITradingStrategy
//{
//    private int _shortPeriod = 10;
//    private int _longPeriod = 30;
//    private string _symbol = "@ES";
//    private int _timeframeMinutes = 60; // Default 1 ora
    
//    public string Name => "Moving Average Crossover";
//    public string Description => "Strategia basata sull'incrocio di medie mobili";
//    public string Symbol => _symbol;
//    public int TimeframeMinutes => _timeframeMinutes;
//    public int RequiredCandles => _longPeriod + 1; // La media lunga determina il numero minimo di candele necessarie

//    public void Initialize(Dictionary<string, object>? parameters = null)
//    {
//        if (parameters != null)
//        {
//            if (parameters.TryGetValue("ShortPeriod", out var shortPeriod))
//                _shortPeriod = Convert.ToInt32(shortPeriod);
//            if (parameters.TryGetValue("LongPeriod", out var longPeriod))
//                _longPeriod = Convert.ToInt32(longPeriod);
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
//            // Prova a trovare il dato più vicino
//            currentIndex = data.Length - 1;
//        }
        
//        if (currentIndex < _longPeriod)
//        {
//            return new TradeSignal
//            {
//                Date = currentDate,
//                Type = SignalType.Hold,
//                Price = data[currentIndex].Close,
//                StrategyName = Name,
//                Reason = "Dati insufficienti per il calcolo"
//            };
//        }

//        var shortMA = CalculateMA(data, currentIndex, _shortPeriod);
//        var longMA = CalculateMA(data, currentIndex, _longPeriod);
//        var prevShortMA = CalculateMA(data, currentIndex - 1, _shortPeriod);
//        var prevLongMA = CalculateMA(data, currentIndex - 1, _longPeriod);

//        SignalType signal = SignalType.Hold;
//        string? reason = null;

//        // Golden Cross: MA corta incrocia sopra MA lunga
//        if (prevShortMA <= prevLongMA && shortMA > longMA)
//        {
//            signal = SignalType.Buy;
//            reason = $"Golden Cross (MA{_shortPeriod} > MA{_longPeriod})";
//        }
//        // Death Cross: MA corta incrocia sotto MA lunga
//        else if (prevShortMA >= prevLongMA && shortMA < longMA)
//        {
//            signal = SignalType.Sell;
//            reason = $"Death Cross (MA{_shortPeriod} < MA{_longPeriod})";
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

//    private decimal CalculateMA(OhlcvData[] data, int endIndex, int period)
//    {
//        decimal sum = 0;
//        for (int i = endIndex - period + 1; i <= endIndex; i++)
//        {
//            sum += data[i].Close;
//        }
//        return sum / period;
//    }
//}
