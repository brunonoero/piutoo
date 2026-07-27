using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.ClaudioUnger;

/// <summary>
/// Unger BIAS strategy from report run_20260526_1559, rank #1.
/// Parameters come from the HTML/top_final report; signal timing and pattern semantics follow unger/core/engines/bias.py.
/// </summary>
public class ClaudioUnger_BIAS_1_NQ_Daily : ITradingStrategy
{
    private const decimal NqDollarPerPoint = 20m;
    private const int PatternLongYes = 152;
    private const int PatternLongNo = 51;
    private const int PatternShortYes = 70;
    private const int PatternShortNo = 1;

    private string _symbol = "@NQ";
    private int _timeframeMinutes = 1440;
    private decimal _contracts = 1m;
    private decimal _stopLossDollars = 1000m;
    private decimal? _takeProfitDollars;
    private int _maxBarsInPosition = 1;
    private DayOfWeek _excludedLongDay = DayOfWeek.Friday;
    private DayOfWeek _excludedShortDay = DayOfWeek.Monday;

    private int _marketPosition;
    private int _barsInPosition;
    private DateTime? _entryDate;
    private DateTime? _lastProcessedBarDate;

    public string Name => "ClaudioUnger_BIAS_1_NQ_Daily";
    public string Description => "Unger BIAS #1 for NQ daily: ptn 152/51 long, 70/1 short, $1,000 stop, no take profit, max 1 bar.";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 8;

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null)
        {
            return;
        }

        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe)) _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToDecimal(contracts);
        if (parameters.TryGetValue("StopLossDollars", out var stopLoss)) _stopLossDollars = Convert.ToDecimal(stopLoss);
        if (parameters.TryGetValue("TakeProfitDollars", out var takeProfit)) _takeProfitDollars = ToNullableDecimal(takeProfit);
        if (parameters.TryGetValue("MaxBarsInPosition", out var maxBars)) _maxBarsInPosition = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("ExcludedLongDay", out var excludedLongDay)) _excludedLongDay = ParseDayOfWeek(excludedLongDay, _excludedLongDay);
        if (parameters.TryGetValue("ExcludedShortDay", out var excludedShortDay)) _excludedShortDay = ParseDayOfWeek(excludedShortDay, _excludedShortDay);
        if (parameters.TryGetValue("UngerExcludedLongDay", out var ungerExcludedLongDay)) _excludedLongDay = ParseUngerDayOfWeek(ungerExcludedLongDay, _excludedLongDay);
        if (parameters.TryGetValue("UngerExcludedShortDay", out var ungerExcludedShortDay)) _excludedShortDay = ParseUngerDayOfWeek(ungerExcludedShortDay, _excludedShortDay);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var currentPrice = data?.LastOrDefault()?.Close ?? 0m;

        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, currentPrice, "Dati insufficienti");
        }

        var current = data[^1];
        var barDate = current.DateTime.Date != default ? current.DateTime.Date : currentDate.Date;

        UpdateBarsInPosition(barDate);

        if (_marketPosition != 0 && _barsInPosition >= _maxBarsInPosition)
        {
            var exitPosition = _marketPosition;
            _marketPosition = 0;
            _barsInPosition = 0;
            _entryDate = null;

            return new TradeSignal
            {
                Date = currentDate,
                Type = exitPosition > 0 ? SignalType.Sell : SignalType.Buy,
                Price = current.Close,
                Symbol = _symbol,
                StrategyCode = Name,
                StrategyName = Name,
                Quantity = _contracts,
                Reason = $"Max barre in posizione ({_maxBarsInPosition})"
            };
        }

        var longAllowed = barDate.DayOfWeek != _excludedLongDay &&
            PatternFast(data, data.Length - 2, PatternLongYes) &&
            !PatternFast(data, data.Length - 2, PatternLongNo);
        var shortAllowed = barDate.DayOfWeek != _excludedShortDay &&
            PatternFast(data, data.Length - 2, PatternShortYes) &&
            !PatternFast(data, data.Length - 2, PatternShortNo);

        if (_marketPosition <= 0 && longAllowed)
        {
            _marketPosition = 1;
            _barsInPosition = 0;
            _entryDate = barDate;

            return Entry(currentDate, SignalType.Buy, current.Open, "BIAS daily long: pattern 152/51 con shift(1), entry market open");
        }

        if (_marketPosition >= 0 && shortAllowed)
        {
            _marketPosition = -1;
            _barsInPosition = 0;
            _entryDate = barDate;

            return Entry(currentDate, SignalType.Sell, current.Open, "BIAS daily short: pattern 70/1 con shift(1), entry market open");
        }

        return Hold(currentDate, current.Close, "Nessun setup BIAS valido");
    }

    private void UpdateBarsInPosition(DateTime barDate)
    {
        if (_lastProcessedBarDate == barDate)
        {
            return;
        }

        if (_marketPosition != 0 && _entryDate.HasValue && barDate > _entryDate.Value)
        {
            _barsInPosition++;
        }

        _lastProcessedBarDate = barDate;
    }

    private TradeSignal Entry(DateTime date, SignalType type, decimal price, string reason)
    {
        return new TradeSignal
        {
            Date = date,
            Type = type,
            Price = price,
            Symbol = _symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,
            StopLoss = DollarsToNqPoints(_stopLossDollars),
            TakeProfit = _takeProfitDollars.HasValue ? DollarsToNqPoints(_takeProfitDollars.Value) : null,
            Reason = reason
        };
    }

    private TradeSignal Hold(DateTime date, decimal price, string reason)
    {
        return new TradeSignal
        {
            Date = date,
            Type = SignalType.Hold,
            Price = price,
            Symbol = _symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,
            Reason = reason
        };
    }

    private decimal DollarsToNqPoints(decimal dollars)
    {
        return dollars / (NqDollarPerPoint * Math.Max(_contracts, 1m));
    }

    private static bool PatternFast(OhlcvData[] data, int rowIndex, int pattern)
    {
        if (pattern == 152)
        {
            return true;
        }

        if (pattern <= 0 || pattern > 152 || rowIndex < 0)
        {
            return false;
        }

        var o0 = Get(data, rowIndex, 0)?.Open ?? 0m;
        var h1 = Get(data, rowIndex, 1)?.High ?? 0m;
        var l1 = Get(data, rowIndex, 1)?.Low ?? 0m;
        var o1 = Get(data, rowIndex, 1)?.Open ?? 0m;
        var c1 = Get(data, rowIndex, 1)?.Close ?? 0m;
        var h2 = Get(data, rowIndex, 2)?.High ?? 0m;
        var l2 = Get(data, rowIndex, 2)?.Low ?? 0m;

        return pattern switch
        {
            // pattern_fast(1) delegates to pattern_neutral(1): abs(O_d1-C_d1) < 10% range_d1.
            1 => IsNarrowDoji(Get(data, rowIndex, 1)),

            // pattern_fast(51): H_d1 > H_d2 and L_d1 > L_d2.
            51 => h1 > h2 && l1 > l2,

            // pattern_fast(70): C_d1 > O_d1.
            70 => c1 > o1,

            // Kept for parameter experiments; not used by this report's top #1.
            144 => Get(data, rowIndex, 0)?.Close > o0,
            153 => false,
            _ => false
        };
    }

    private static OhlcvData? Get(OhlcvData[] data, int rowIndex, int daysAgo)
    {
        var index = rowIndex - daysAgo;
        return index >= 0 && index < data.Length ? data[index] : null;
    }

    private static bool IsNarrowDoji(OhlcvData? bar)
    {
        if (bar == null)
        {
            return false;
        }

        var range = bar.High - bar.Low;
        return range > 0m && Math.Abs(bar.Close - bar.Open) < range * 0.10m;
    }

    private static decimal? ToNullableDecimal(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Convert.ToDecimal(value);
    }

    private static DayOfWeek ParseDayOfWeek(object value, DayOfWeek fallback)
    {
        if (value is DayOfWeek day)
        {
            return day;
        }

        if (value is int dayNumber && dayNumber >= 0 && dayNumber <= 6)
        {
            return (DayOfWeek)dayNumber;
        }

        return Enum.TryParse(value.ToString(), ignoreCase: true, out DayOfWeek parsed) ? parsed : fallback;
    }

    private static DayOfWeek ParseUngerDayOfWeek(object value, DayOfWeek fallback)
    {
        var dayNumber = Convert.ToInt32(value);

        return dayNumber switch
        {
            0 => DayOfWeek.Monday,
            1 => DayOfWeek.Tuesday,
            2 => DayOfWeek.Wednesday,
            3 => DayOfWeek.Thursday,
            4 => DayOfWeek.Friday,
            5 => DayOfWeek.Saturday,
            6 => DayOfWeek.Sunday,
            -1 => fallback,
            _ => fallback
        };
    }
}
