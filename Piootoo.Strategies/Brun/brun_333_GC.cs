using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Strategies;

public enum Brun333Profile
{
    Strict,
    Medium,
    Loose,
    Adaptive
}

/// <summary>
/// Standalone Gold strategy from the gold-brun notes.
/// It implements ITradingStrategy directly and intentionally excludes any AI validation step.
/// </summary>
public class brun_333_GC : Brun333GcBase
{
    public brun_333_GC()
        : base("brun_333_GC", "strict", Brun333Profile.Strict)
    {
    }
}

public class brun_333_GC_Medium : Brun333GcBase
{
    public brun_333_GC_Medium()
        : base("brun_333_GC_Medium", "medium", Brun333Profile.Medium)
    {
    }
}

public class brun_333_GC_Loose : Brun333GcBase
{
    public brun_333_GC_Loose()
        : base("brun_333_GC_Loose", "loose", Brun333Profile.Loose)
    {
    }
}

public class brun_333_GC_Adaptive : Brun333GcBase
{
    public brun_333_GC_Adaptive()
        : base("brun_333_GC_Adaptive", "adaptive", Brun333Profile.Adaptive)
    {
    }
}

public abstract class Brun333GcBase : ITradingStrategy
{
    private const decimal GoldPip = 0.1m;

    private readonly string _name;
    private readonly string _profileName;

    private string _symbol = "@GC";
    private int _timeframeMinutes = 60;
    private int _contracts = 1;

    private decimal _maxDistanceFromLowPips = 30m;
    private decimal _volumeMultiplier = 1.8m;
    private decimal _minBodyPercent = 65m;
    private decimal _rsiOversold = 35m;
    private int _waitConfirmationBars = 1;
    private int _maxTradesPerDay = 1;

    private int _atrPeriod = 14;
    private decimal _minAtrPips = 25m;
    private decimal _atrMultiplier = 1.2m;
    private int _atrAveragePeriod = 20;

    private int _volumePeriod = 30;
    private int _recentHighLookback = 3;
    private int _emaPeriod = 20;
    private int _rsiPeriod = 14;

    private decimal _minStopPips = 50m;
    private decimal _minTakeProfitPips = 300m;
    private decimal _breakEvenPips = 100m;
    private decimal _stopAtrMultiplier = 1.5m;
    private decimal _takeProfitAtrMultiplier = 6m;

    private int _londonStart = 700;
    private int _londonEnd = 900;
    private int _nyStart = 1300;
    private int _nyEnd = 1500;
    private int _flatTime = 2100;

    private bool _requireAtrExpansion = true;
    private bool _requireVolumeSpike = true;
    private bool _requireBullishReversalPattern = true;
    private bool _requireRecentHighBreakout = true;
    private bool _requireEmaFilter = true;
    private bool _requireMacdTurn = true;
    private bool _useRecentOversoldRsi;
    private int _rsiLookbackBars = 5;
    private decimal _volumeStdDevMultiplier = 2m;
    private decimal _maxUpperShadowPercent = 0.25m;
    private decimal _hammerLowerShadowMultiplier = 1.5m;

    private bool _useAdaptiveScoring;
    private int _minSetupScore = 70;

    private DateTime _lastTradeDate = DateTime.MinValue;
    private int _tradesToday;
    private bool _isLong;
    private PendingSetup? _pendingSetup;
    private decimal _entryPrice;
    private decimal _activeStopLoss;
    private decimal _activeTakeProfit;

    protected Brun333GcBase(string name, string profileName, Brun333Profile profile)
    {
        _name = name;
        _profileName = profileName;
        ApplyProfile(profile);
    }

    public string Name => _name;
    public string Description => $"Gold 1H rule-based daily-low reversal ({_profileName} restrictions)";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 120;

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;

    private void ApplyProfile(Brun333Profile profile)
    {
        switch (profile)
        {
            case Brun333Profile.Medium:
                _maxDistanceFromLowPips = 80m;
                _volumeMultiplier = 1.3m;
                _volumeStdDevMultiplier = 1m;
                _minBodyPercent = 50m;
                _maxUpperShadowPercent = 0.4m;
                _hammerLowerShadowMultiplier = 1.0m;
                _rsiOversold = 45m;
                _useRecentOversoldRsi = true;
                _waitConfirmationBars = 0;
                _maxTradesPerDay = 2;
                _minAtrPips = 15m;
                _atrMultiplier = 1.0m;
                _londonStart = 600;
                _londonEnd = 1000;
                _nyStart = 1230;
                _nyEnd = 1600;
                _requireRecentHighBreakout = false;
                _requireEmaFilter = false;
                break;

            case Brun333Profile.Loose:
                _maxDistanceFromLowPips = 150m;
                _volumeMultiplier = 1.0m;
                _volumeStdDevMultiplier = 0m;
                _minBodyPercent = 35m;
                _maxUpperShadowPercent = 0.55m;
                _hammerLowerShadowMultiplier = 0.75m;
                _rsiOversold = 55m;
                _useRecentOversoldRsi = true;
                _rsiLookbackBars = 10;
                _waitConfirmationBars = 0;
                _maxTradesPerDay = 3;
                _minAtrPips = 8m;
                _atrMultiplier = 0.85m;
                _londonStart = 600;
                _londonEnd = 1100;
                _nyStart = 1200;
                _nyEnd = 1700;
                _requireAtrExpansion = false;
                _requireVolumeSpike = false;
                _requireBullishReversalPattern = false;
                _requireRecentHighBreakout = false;
                _requireEmaFilter = false;
                _requireMacdTurn = false;
                break;

            case Brun333Profile.Adaptive:
                _maxDistanceFromLowPips = 50m;
                _volumeMultiplier = 1.5m;
                _volumeStdDevMultiplier = 1.5m;
                _minBodyPercent = 55m;
                _maxUpperShadowPercent = 0.35m;
                _hammerLowerShadowMultiplier = 1.2m;
                _rsiOversold = 40m;
                _useRecentOversoldRsi = true;
                _rsiLookbackBars = 5;
                _waitConfirmationBars = 1;
                _maxTradesPerDay = 2;
                _minAtrPips = 20m;
                _atrMultiplier = 1.1m;
                _londonStart = 700;
                _londonEnd = 900;
                _nyStart = 1300;
                _nyEnd = 1500;
                _requireAtrExpansion = false;
                _requireVolumeSpike = false;
                _requireBullishReversalPattern = false;
                _requireRecentHighBreakout = false;
                _requireEmaFilter = false;
                _requireMacdTurn = false;
                _useAdaptiveScoring = true;
                _minSetupScore = 75;
                break;
        }
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null)
        {
            return;
        }

        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe)) _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MaxDistanceFromLowPips", out var maxDistance)) _maxDistanceFromLowPips = Convert.ToDecimal(maxDistance);
        if (parameters.TryGetValue("VolumeMultiplier", out var volumeMultiplier)) _volumeMultiplier = Convert.ToDecimal(volumeMultiplier);
        if (parameters.TryGetValue("MinBodyPercent", out var bodyPercent)) _minBodyPercent = Convert.ToDecimal(bodyPercent);
        if (parameters.TryGetValue("RsiOversold", out var rsiOversold)) _rsiOversold = Convert.ToDecimal(rsiOversold);
        if (parameters.TryGetValue("WaitConfirmationBars", out var confirmationBars)) _waitConfirmationBars = Convert.ToInt32(confirmationBars);
        if (parameters.TryGetValue("MaxTradesPerDay", out var maxTrades)) _maxTradesPerDay = Convert.ToInt32(maxTrades);
        if (parameters.TryGetValue("MinAtrPips", out var minAtr)) _minAtrPips = Convert.ToDecimal(minAtr);
        if (parameters.TryGetValue("AtrMultiplier", out var atrMultiplier)) _atrMultiplier = Convert.ToDecimal(atrMultiplier);
        if (parameters.TryGetValue("RequireAtrExpansion", out var requireAtrExpansion)) _requireAtrExpansion = Convert.ToBoolean(requireAtrExpansion);
        if (parameters.TryGetValue("RequireVolumeSpike", out var requireVolumeSpike)) _requireVolumeSpike = Convert.ToBoolean(requireVolumeSpike);
        if (parameters.TryGetValue("RequireBullishReversalPattern", out var requirePattern)) _requireBullishReversalPattern = Convert.ToBoolean(requirePattern);
        if (parameters.TryGetValue("RequireRecentHighBreakout", out var requireBreakout)) _requireRecentHighBreakout = Convert.ToBoolean(requireBreakout);
        if (parameters.TryGetValue("RequireEmaFilter", out var requireEma)) _requireEmaFilter = Convert.ToBoolean(requireEma);
        if (parameters.TryGetValue("RequireMacdTurn", out var requireMacd)) _requireMacdTurn = Convert.ToBoolean(requireMacd);
        if (parameters.TryGetValue("UseRecentOversoldRsi", out var recentRsi)) _useRecentOversoldRsi = Convert.ToBoolean(recentRsi);
        if (parameters.TryGetValue("RsiLookbackBars", out var rsiLookback)) _rsiLookbackBars = Convert.ToInt32(rsiLookback);
        if (parameters.TryGetValue("VolumeStdDevMultiplier", out var volumeStdDev)) _volumeStdDevMultiplier = Convert.ToDecimal(volumeStdDev);
        if (parameters.TryGetValue("UseAdaptiveScoring", out var useAdaptiveScoring)) _useAdaptiveScoring = Convert.ToBoolean(useAdaptiveScoring);
        if (parameters.TryGetValue("MinSetupScore", out var minSetupScore)) _minSetupScore = Convert.ToInt32(minSetupScore);
        if (parameters.TryGetValue("StopAtrMultiplier", out var stopAtrMultiplier)) _stopAtrMultiplier = Convert.ToDecimal(stopAtrMultiplier);
        if (parameters.TryGetValue("TakeProfitAtrMultiplier", out var takeProfitAtrMultiplier)) _takeProfitAtrMultiplier = Convert.ToDecimal(takeProfitAtrMultiplier);
        if (parameters.TryGetValue("BreakEvenPips", out var breakEvenPips)) _breakEvenPips = Convert.ToDecimal(breakEvenPips);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        currentDate = TradingDateTime.ToFeedUtc(currentDate);

        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, data?.LastOrDefault()?.Close ?? 0m, "Dati insufficienti");
        }

        ResetDailyCounters(currentDate);

        var current = data[^1];
        var currentPrice = current.Close;
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        SyncOpenPositionWithMarket(current);

        if (_isLong && currentTime >= _flatTime)
        {
            return CloseLong(currentDate, currentPrice, "Exit flat time");
        }

        if (_isLong)
        {
            return Hold(currentDate, currentPrice, "Posizione long gia' aperta");
        }

        var confirmationSignal = TryConfirmPendingSetup(data, currentDate);
        if (confirmationSignal != null)
        {
            return confirmationSignal;
        }

        if (_tradesToday >= _maxTradesPerDay)
        {
            return Hold(currentDate, currentPrice, "Limite trade giornaliero raggiunto");
        }

        var setup = EvaluateSetup(data, currentDate);
        if (!setup.IsValid)
        {
            return Hold(currentDate, currentPrice, setup.Reason);
        }

        if (_waitConfirmationBars > 0)
        {
            _pendingSetup = new PendingSetup(
                currentDate,
                current.High,
                setup.StopLoss,
                setup.TakeProfit,
                setup.BreakEven,
                _waitConfirmationBars);

            return Hold(currentDate, currentPrice, "Setup candidato in attesa di conferma");
        }

        return EnterLong(currentDate, currentPrice, setup, _useAdaptiveScoring ? setup.Reason : "Gold reversal setup");
    }

    private SetupEvaluation EvaluateSetup(OhlcvData[] data, DateTime currentDate)
    {
        if (_useAdaptiveScoring)
        {
            return EvaluateSetupAdaptive(data, currentDate);
        }

        var current = data[^1];
        var previous = data[^2];
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        if (!IsTargetTime(currentTime))
        {
            return SetupEvaluation.Invalid("Fuori fascia oraria");
        }

        var todaysBars = data.Where(bar => TradingDateTime.IsSameUtcDay(bar.DateTime, currentDate)).ToArray();
        if (todaysBars.Length == 0)
        {
            return SetupEvaluation.Invalid("Nessun dato giornaliero");
        }

        var dailyLow = todaysBars.Min(bar => bar.Low);
        var maxDistance = _maxDistanceFromLowPips * GoldPip;
        if (current.Low > dailyLow + maxDistance)
        {
            return SetupEvaluation.Invalid("Prezzo non vicino al minimo giornaliero");
        }

        var atr = CalculateAtr(data, _atrPeriod);
        if (atr <= 0)
        {
            return SetupEvaluation.Invalid("ATR non disponibile");
        }

        var atrPips = atr / GoldPip;
        if (atrPips < _minAtrPips)
        {
            return SetupEvaluation.Invalid("ATR sotto minimo");
        }

        var averageAtr = CalculateAverageAtr(data, _atrPeriod, _atrAveragePeriod);
        if (_requireAtrExpansion && averageAtr > 0 && atr < averageAtr * _atrMultiplier)
        {
            return SetupEvaluation.Invalid("ATR non in espansione");
        }

        if (_requireVolumeSpike && !IsVolumeSpike(data))
        {
            return SetupEvaluation.Invalid("Volume non anomalo");
        }

        if (!IsStrongBullishCandle(current))
        {
            return SetupEvaluation.Invalid("Candela rialzista non abbastanza forte");
        }

        if (_requireBullishReversalPattern && !IsBullishReversalPattern(current, previous))
        {
            return SetupEvaluation.Invalid("Pattern bullish mancante");
        }

        if (_requireRecentHighBreakout)
        {
            var recentHigh = data
                .Skip(Math.Max(0, data.Length - 1 - _recentHighLookback))
                .Take(_recentHighLookback)
                .Max(bar => bar.High);
            if (current.Close <= recentHigh)
            {
                return SetupEvaluation.Invalid("Breakout massimo recente non confermato");
            }
        }

        if (_useRecentOversoldRsi)
        {
            if (!WasRecentlyOversold(data, _rsiPeriod, _rsiLookbackBars, _rsiOversold))
            {
                return SetupEvaluation.Invalid("RSI recente non oversold");
            }
        }
        else
        {
            var rsi = CalculateRsi(data, _rsiPeriod);
            if (rsi > _rsiOversold)
            {
                return SetupEvaluation.Invalid("RSI non oversold");
            }
        }

        if (_requireEmaFilter)
        {
            var ema = CalculateEma(data.Select(bar => bar.Close).ToArray(), _emaPeriod);
            if (ema > 0 && current.Close <= ema)
            {
                return SetupEvaluation.Invalid("Prezzo sotto EMA20");
            }
        }

        if (_requireMacdTurn && !IsMacdTurningPositive(data))
        {
            return SetupEvaluation.Invalid("MACD non in rotazione positiva");
        }

        var stopLoss = Math.Max(_minStopPips * GoldPip, atr * _stopAtrMultiplier);
        var takeProfit = Math.Max(_minTakeProfitPips * GoldPip, atr * _takeProfitAtrMultiplier);
        var breakEven = _breakEvenPips * GoldPip;

        return SetupEvaluation.Valid(stopLoss, takeProfit, breakEven);
    }

    private SetupEvaluation EvaluateSetupAdaptive(OhlcvData[] data, DateTime currentDate)
    {
        var current = data[^1];
        var previous = data[^2];
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        if (!IsTargetTime(currentTime))
        {
            return SetupEvaluation.Invalid("Fuori fascia oraria");
        }

        var todaysBars = data.Where(bar => TradingDateTime.IsSameUtcDay(bar.DateTime, currentDate)).ToArray();
        if (todaysBars.Length == 0)
        {
            return SetupEvaluation.Invalid("Nessun dato giornaliero");
        }

        var dailyLow = todaysBars.Min(bar => bar.Low);
        var maxDistance = _maxDistanceFromLowPips * GoldPip;
        if (current.Low > dailyLow + maxDistance)
        {
            return SetupEvaluation.Invalid("Prezzo non vicino al minimo giornaliero");
        }

        var atr = CalculateAtr(data, _atrPeriod);
        if (atr <= 0)
        {
            return SetupEvaluation.Invalid("ATR non disponibile");
        }

        var atrPips = atr / GoldPip;
        if (atrPips < _minAtrPips)
        {
            return SetupEvaluation.Invalid("ATR sotto minimo");
        }

        if (!IsStrongBullishCandle(current))
        {
            return SetupEvaluation.Invalid("Candela rialzista non abbastanza forte");
        }

        var averageAtr = CalculateAverageAtr(data, _atrPeriod, _atrAveragePeriod);
        var scoreDetails = new List<string>();
        var totalScore = 0;

        var atrScore = ScoreAtrExpansion(atr, averageAtr, scoreDetails);
        var volumeScore = ScoreVolumeSpike(data, scoreDetails);
        var patternScore = ScoreBullishPattern(current, previous, scoreDetails);
        totalScore += atrScore + volumeScore + patternScore;
        totalScore += ScoreRecentOversoldRsi(data, scoreDetails);
        totalScore += ScoreEmaFilter(data, current, scoreDetails);
        totalScore += ScoreMacdTurn(data, scoreDetails);
        totalScore += ScoreRecentHighBreakout(data, current, scoreDetails);

        if (volumeScore == 0 && patternScore == 0)
        {
            return SetupEvaluation.Invalid("Volume e pattern assenti");
        }

        if (totalScore < _minSetupScore)
        {
            return SetupEvaluation.Invalid($"Score setup insufficiente: {totalScore}/{_minSetupScore} [{string.Join(", ", scoreDetails)}]");
        }

        var stopLoss = Math.Max(_minStopPips * GoldPip, atr * _stopAtrMultiplier);
        var takeProfit = Math.Max(_minTakeProfitPips * GoldPip, atr * _takeProfitAtrMultiplier);
        var breakEven = _breakEvenPips * GoldPip;
        var reason = $"Gold reversal adaptive score={totalScore} [{string.Join(", ", scoreDetails)}]";

        return SetupEvaluation.Valid(stopLoss, takeProfit, breakEven, reason);
    }

    private int ScoreAtrExpansion(decimal atr, decimal averageAtr, List<string> details)
    {
        if (averageAtr <= 0)
        {
            details.Add("ATR:10");
            return 10;
        }

        if (atr >= averageAtr * _atrMultiplier)
        {
            details.Add("ATR:20");
            return 20;
        }

        if (atr >= averageAtr * (_atrMultiplier * 0.9m))
        {
            details.Add("ATR:10");
            return 10;
        }

        details.Add("ATR:0");
        return 0;
    }

    private int ScoreVolumeSpike(OhlcvData[] data, List<string> details)
    {
        if (IsVolumeSpike(data))
        {
            details.Add("Vol:15");
            return 15;
        }

        if (data.Length >= _volumePeriod + 1)
        {
            var currentVolume = data[^1].Volume;
            var volumes = data.Skip(data.Length - 1 - _volumePeriod).Take(_volumePeriod).Select(bar => bar.Volume).ToArray();
            var average = volumes.Average();
            if (average > 0 && currentVolume >= average * (_volumeMultiplier * 0.8m))
            {
                details.Add("Vol:8");
                return 8;
            }
        }

        details.Add("Vol:0");
        return 0;
    }

    private int ScoreBullishPattern(OhlcvData current, OhlcvData previous, List<string> details)
    {
        if (IsBullishReversalPattern(current, previous))
        {
            details.Add("Pat:15");
            return 15;
        }

        var range = current.High - current.Low;
        var lowerShadow = Math.Min(current.Open, current.Close) - current.Low;
        var body = Math.Abs(current.Close - current.Open);
        if (range > 0 && lowerShadow >= body && current.Close > current.Open)
        {
            details.Add("Pat:8");
            return 8;
        }

        details.Add("Pat:0");
        return 0;
    }

    private int ScoreRecentOversoldRsi(OhlcvData[] data, List<string> details)
    {
        if (WasRecentlyOversold(data, _rsiPeriod, _rsiLookbackBars, _rsiOversold))
        {
            details.Add("RSI:15");
            return 15;
        }

        var rsi = CalculateRsi(data, _rsiPeriod);
        if (rsi <= _rsiOversold + 10m)
        {
            details.Add("RSI:8");
            return 8;
        }

        details.Add("RSI:0");
        return 0;
    }

    private int ScoreEmaFilter(OhlcvData[] data, OhlcvData current, List<string> details)
    {
        var ema = CalculateEma(data.Select(bar => bar.Close).ToArray(), _emaPeriod);
        if (ema <= 0)
        {
            details.Add("EMA:0");
            return 0;
        }

        if (current.Close > ema)
        {
            details.Add("EMA:15");
            return 15;
        }

        if (current.Close > ema * 0.998m)
        {
            details.Add("EMA:8");
            return 8;
        }

        details.Add("EMA:0");
        return 0;
    }

    private int ScoreMacdTurn(OhlcvData[] data, List<string> details)
    {
        if (IsMacdTurningPositive(data))
        {
            details.Add("MACD:10");
            return 10;
        }

        details.Add("MACD:0");
        return 0;
    }

    private int ScoreRecentHighBreakout(OhlcvData[] data, OhlcvData current, List<string> details)
    {
        var recentHigh = data
            .Skip(Math.Max(0, data.Length - 1 - _recentHighLookback))
            .Take(_recentHighLookback)
            .Max(bar => bar.High);

        if (current.Close > recentHigh)
        {
            details.Add("Brk:10");
            return 10;
        }

        details.Add("Brk:0");
        return 0;
    }

    private TradeSignal? TryConfirmPendingSetup(OhlcvData[] data, DateTime currentDate)
    {
        if (_pendingSetup == null)
        {
            return null;
        }

        var current = data[^1];
        var barsElapsed = (int)Math.Round((currentDate - _pendingSetup.CreatedAt).TotalMinutes / Math.Max(1, _timeframeMinutes));
        if (barsElapsed < _pendingSetup.BarsToWait)
        {
            return null;
        }

        var setup = _pendingSetup;
        _pendingSetup = null;

        if (_tradesToday >= _maxTradesPerDay)
        {
            return Hold(currentDate, current.Close, "Conferma scartata: limite trade");
        }

        if (current.Close > setup.TriggerHigh && IsStrongBullishCandle(current))
        {
            var confirmedSetup = SetupEvaluation.Valid(setup.StopLoss, setup.TakeProfit, setup.BreakEven);
            return EnterLong(currentDate, current.Close, confirmedSetup, "Gold reversal confermato");
        }

        return Hold(currentDate, current.Close, "Conferma setup fallita");
    }

    private TradeSignal EnterLong(DateTime currentDate, decimal price, SetupEvaluation setup, string reason)
    {
        _isLong = true;
        _entryPrice = price;
        _activeStopLoss = setup.StopLoss;
        _activeTakeProfit = setup.TakeProfit;
        _tradesToday++;
        _lastTradeDate = TradingDateTime.ToFeedUtc(currentDate).Date;

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Buy,
            Price = price,
            Symbol = Symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,
            StopLoss = setup.StopLoss,
            TakeProfit = setup.TakeProfit,
            BreakEven = setup.BreakEven,
            Reason = reason
        };
    }

    private TradeSignal Signal(DateTime currentDate, SignalType type, decimal price, string reason, bool closeOnly = false)
    {
        return new TradeSignal
        {
            Date = currentDate,
            Type = type,
            Price = price,
            Symbol = Symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,
            Reason = reason,
            
        };
    }

    private TradeSignal CloseLong(DateTime currentDate, decimal price, string reason)
    {
        ResetLongState();
        return Signal(currentDate, SignalType.Sell, price, reason, closeOnly: true);
    }

    private void SyncOpenPositionWithMarket(OhlcvData current)
    {
        if (!_isLong || _entryPrice <= 0)
        {
            return;
        }

        var move = current.Close - _entryPrice;
        if (_activeStopLoss > 0 && move <= -_activeStopLoss)
        {
            ResetLongState();
            return;
        }

        if (_activeTakeProfit > 0 && move >= _activeTakeProfit)
        {
            ResetLongState();
        }
    }

    private void ResetLongState()
    {
        _isLong = false;
        _entryPrice = 0;
        _activeStopLoss = 0;
        _activeTakeProfit = 0;
        _pendingSetup = null;
    }

    private TradeSignal Hold(DateTime currentDate, decimal price, string reason)
    {
        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = price,
            Symbol = Symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,
            Reason = reason
        };
    }

    private void ResetDailyCounters(DateTime currentDate)
    {
        if (!TradingDateTime.IsSameUtcDay(_lastTradeDate, currentDate))
        {
            _tradesToday = 0;
            _lastTradeDate = TradingDateTime.ToFeedUtc(currentDate).Date;
        }
    }

    private bool IsTargetTime(int hhmm)
    {
        return (hhmm >= _londonStart && hhmm <= _londonEnd) ||
               (hhmm >= _nyStart && hhmm <= _nyEnd);
    }

    private bool IsVolumeSpike(OhlcvData[] data)
    {
        if (data.Length < _volumePeriod + 1)
        {
            return false;
        }

        var currentVolume = data[^1].Volume;
        var volumes = data.Skip(data.Length - 1 - _volumePeriod).Take(_volumePeriod).Select(bar => bar.Volume).ToArray();
        var average = volumes.Average();
        if (average <= 0)
        {
            return false;
        }

        var variance = volumes.Select(volume => (volume - average) * (volume - average)).Average();
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return currentVolume >= average * _volumeMultiplier &&
               currentVolume >= average + stdDev * _volumeStdDevMultiplier;
    }

    private bool IsStrongBullishCandle(OhlcvData candle)
    {
        var range = candle.High - candle.Low;
        if (range <= 0 || candle.Close <= candle.Open)
        {
            return false;
        }

        var bodyPercent = Math.Abs(candle.Close - candle.Open) / range * 100m;
        var upperShadow = candle.High - candle.Close;
        return bodyPercent >= _minBodyPercent && upperShadow <= range * _maxUpperShadowPercent;
    }

    private bool IsBullishReversalPattern(OhlcvData current, OhlcvData previous)
    {
        var range = current.High - current.Low;
        var lowerShadow = Math.Min(current.Open, current.Close) - current.Low;
        var body = Math.Abs(current.Close - current.Open);
        var isHammer = range > 0 && lowerShadow >= body * _hammerLowerShadowMultiplier && current.Close > current.Open;

        var isBullishEngulfing =
            previous.Close < previous.Open &&
            current.Close > current.Open &&
            current.Open <= previous.Close &&
            current.Close >= previous.Open;

        return isHammer || isBullishEngulfing;
    }

    private static decimal CalculateAtr(OhlcvData[] data, int period)
    {
        if (data.Length < period + 1)
        {
            return 0m;
        }

        return CalculateTrueRanges(data).TakeLast(period).Average();
    }

    private static decimal CalculateAverageAtr(OhlcvData[] data, int atrPeriod, int averagePeriod)
    {
        var ranges = CalculateTrueRanges(data).ToArray();
        if (ranges.Length < atrPeriod + averagePeriod)
        {
            return 0m;
        }

        var atrValues = new List<decimal>();
        for (var i = ranges.Length - averagePeriod; i < ranges.Length; i++)
        {
            var start = Math.Max(0, i - atrPeriod + 1);
            atrValues.Add(ranges.Skip(start).Take(i - start + 1).Average());
        }

        return atrValues.Average();
    }

    private static IEnumerable<decimal> CalculateTrueRanges(OhlcvData[] data)
    {
        for (var i = 1; i < data.Length; i++)
        {
            var highLow = data[i].High - data[i].Low;
            var highClose = Math.Abs(data[i].High - data[i - 1].Close);
            var lowClose = Math.Abs(data[i].Low - data[i - 1].Close);
            yield return Math.Max(highLow, Math.Max(highClose, lowClose));
        }
    }

    private static decimal CalculateRsi(OhlcvData[] data, int period)
    {
        if (data.Length < period + 1)
        {
            return 50m;
        }

        decimal gains = 0m;
        decimal losses = 0m;
        var start = data.Length - period;
        for (var i = start; i < data.Length; i++)
        {
            var change = data[i].Close - data[i - 1].Close;
            if (change >= 0)
            {
                gains += change;
            }
            else
            {
                losses += Math.Abs(change);
            }
        }

        if (losses == 0)
        {
            return 100m;
        }

        var rs = gains / losses;
        return 100m - (100m / (1m + rs));
    }

    private static bool WasRecentlyOversold(OhlcvData[] data, int period, int lookbackBars, decimal threshold)
    {
        var checks = Math.Min(Math.Max(1, lookbackBars), data.Length - period);
        for (var i = 0; i < checks; i++)
        {
            var end = data.Length - i;
            var window = data.Take(end).ToArray();
            if (CalculateRsi(window, period) <= threshold)
            {
                return true;
            }
        }

        return false;
    }

    private static decimal CalculateEma(decimal[] values, int period)
    {
        if (values.Length < period)
        {
            return 0m;
        }

        var multiplier = 2m / (period + 1);
        var ema = values.Take(period).Average();
        for (var i = period; i < values.Length; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    private static bool IsMacdTurningPositive(OhlcvData[] data)
    {
        if (data.Length < 35)
        {
            return false;
        }

        var closes = data.Select(bar => bar.Close).ToArray();
        var currentMacd = CalculateEma(closes, 12) - CalculateEma(closes, 26);
        var previousCloses = closes.Take(closes.Length - 1).ToArray();
        var previousMacd = CalculateEma(previousCloses, 12) - CalculateEma(previousCloses, 26);

        return currentMacd > previousMacd;
    }

    private sealed record PendingSetup(
        DateTime CreatedAt,
        decimal TriggerHigh,
        decimal StopLoss,
        decimal TakeProfit,
        decimal BreakEven,
        int BarsToWait);

    private sealed record SetupEvaluation(
        bool IsValid,
        decimal StopLoss,
        decimal TakeProfit,
        decimal BreakEven,
        string Reason)
    {
        public static SetupEvaluation Valid(decimal stopLoss, decimal takeProfit, decimal breakEven, string? reason = null)
        {
            return new SetupEvaluation(true, stopLoss, takeProfit, breakEven, reason ?? "Setup valido");
        }

        public static SetupEvaluation Invalid(string reason)
        {
            return new SetupEvaluation(false, 0m, 0m, 0m, reason);
        }
    }
}
