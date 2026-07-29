using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// PTS_001 — trend following mirrored su NQ a 60 minuti.
///
/// <para>
/// Le barre e gli orari sono in UTC. Il contesto giornaliero è costruito sulle
/// sessioni CME 17:00–16:00: per ogni barra, <c>OHLCMulti5</c> ricava gli OHLC
/// della sessione corrente e delle cinque precedenti. In particolare, gli
/// estremi della sessione completa precedente sono <c>H_d1</c> e <c>L_d1</c>.
/// </para>
///
/// <para>
/// Opera soltanto nella finestra inclusiva 16:00–03:00 e non esclude alcun
/// giorno. Per poter operare richiede il regime neutro 47 e l'assenza del
/// regime neutro 1. Il setup long richiede inoltre il pattern direzionale
/// rialzista 50 e l'assenza dell'8; lo short applica gli stessi filtri in modo
/// mirrored, quindi i pattern direzionali ribassisti -50 e -8.
/// </para>
///
/// <para>
/// Quando il setup è valido emette un ordine stop buy a <c>H_d1</c> oppure uno
/// stop sell a <c>L_d1</c>. L'ordine nasce alla chiusura della barra di segnale,
/// è attivo solo dalla barra successiva e scade con essa: il fill, inclusi gap
/// e penetrazione del livello, è responsabilità dell'engine. Se long e short
/// sono validi nella stessa barra, i due intent sono emessi come segnali
/// companion indipendenti.
/// </para>
///
/// <para>
/// È consentita al massimo un'entrata per direzione nella stessa sessione.
/// <c>intraday_only</c> è disattivo, quindi non esiste uscita di fine sessione:
/// una posizione può restare overnight. Ogni ingresso dichiara stop loss di
/// $1.000 e take profit di $3.000 per contratto; nessun limite di barre è
/// applicato. Uscite e posizioni restano gestite dall'engine, non dalla
/// strategia.
/// </para>
/// </summary>
public sealed class PTS_001_NQ_60 : StatelessEasyStrategyBase
{
    private int _sessionStartTime = 1700;
    private int _sessionEndTime = 1600;
    private int _startHour = 16;
    private int _endHour = 3;
    private int _skipDay = -1;
    private int _ptnNeutYes = 47;
    private int _ptnNeutNo = 1;
    private int _ptnDirYes = 50;
    private int _ptnDirNo = 8;
    private int _myStop = 1000;
    private int _myProfit = 3000;
    private int _maxBars = 0;
    private int _myContracts = 1;

    private int _currentMP = 0;
    private bool _longEnteredThisSession;
    private bool _shortEnteredThisSession;

    private string _symbol = "@NQ";
    private int _timeframeMinutes = 60;

    public string Name => "PTS_001";
    public string Description => "TF_M NQ 60: breakout H/L d1, pattern mirrored, multiday";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null)
        {
            return;
        }

        if (parameters.TryGetValue("Symbol", out var symbol))
            _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe))
            _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("SessionStartTime", out var sessionStart))
            _sessionStartTime = Convert.ToInt32(sessionStart);
        if (parameters.TryGetValue("SessionEndTime", out var sessionEnd))
            _sessionEndTime = Convert.ToInt32(sessionEnd);
        if (parameters.TryGetValue("StartHour", out var startHour))
            _startHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            _endHour = Convert.ToInt32(endHour);
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            _skipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("PtnNeutYes", out var ptnNeutYes))
            _ptnNeutYes = Convert.ToInt32(ptnNeutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var ptnNeutNo))
            _ptnNeutNo = Convert.ToInt32(ptnNeutNo);
        if (parameters.TryGetValue("PtnDirYes", out var ptnDirYes))
            _ptnDirYes = Convert.ToInt32(ptnDirYes);
        if (parameters.TryGetValue("PtnDirNo", out var ptnDirNo))
            _ptnDirNo = Convert.ToInt32(ptnDirNo);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            _myStop = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            _myProfit = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            _maxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("Contracts", out var contracts))
            _myContracts = Convert.ToInt32(contracts);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
        {
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");
        }

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = OHLCMulti5(
            _sessionStartTime,
            _sessionEndTime,
            data,
            barTime,
            out var ohlc);

        if (isStartOfSession)
        {
            _longEnteredThisSession = false;
            _shortEnteredThisSession = false;
        }

        if (_currentMP > 0)
            _longEnteredThisSession = true;
        else if (_currentMP < 0)
            _shortEnteredThisSession = true;

        var startTime = _startHour * 100;
        var endTime = _endHour * 100;
        if (!TimeWindowInclusive(startTime, endTime, barTime) ||
            (_skipDay >= 0 && ToPythonDayOfWeek(barTime.DayOfWeek) == _skipDay) ||
            _currentMP != 0)
        {
            return Hold(bar.Close, barTime);
        }

        var neutral = PatternNeutralFast(_ptnNeutYes, ohlc) &&
                      !PatternNeutralFast(_ptnNeutNo, ohlc);
        if (!neutral)
        {
            return Hold(bar.Close, barTime);
        }

        var longSetup = !_longEnteredThisSession &&
                        PatternDirectionalFast(_ptnDirYes, ohlc) &&
                        !PatternDirectionalFast(_ptnDirNo, ohlc);
        var shortSetup = !_shortEnteredThisSession &&
                         PatternDirectionalFast(-_ptnDirYes, ohlc) &&
                         !PatternDirectionalFast(-_ptnDirNo, ohlc);

        var nextBarUtc = EstimateNextBarUtc(data, barTime);
        var signals = new List<TradeSignal>(2);
        if (longSetup)
            signals.Add(CreateStopSignal(SignalType.Buy, ohlc[5], barTime, nextBarUtc, "TF_M LE H_d1"));
        if (shortSetup)
            signals.Add(CreateStopSignal(SignalType.Sell, ohlc[6], barTime, nextBarUtc, "TF_M SE L_d1"));

        if (signals.Count == 0)
        {
            return Hold(bar.Close, barTime);
        }

        var primary = signals[0];
        if (signals.Count > 1)
            primary.CompanionSignals = [signals[1]];
        return primary;
    }

    private TradeSignal CreateStopSignal(
        SignalType type,
        decimal price,
        DateTime barTime,
        DateTime nextBarUtc,
        string reason) =>
        new()
        {
            Date = barTime,
            Type = type,
            Price = price,
            StrategyName = Name,
            Quantity = _myContracts,
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = nextBarUtc,
            ExpiresAtUtc = nextBarUtc,
            StopLossMoneyPerFutureContract = _myStop > 0 ? _myStop : null,
            TakeProfitMoneyPerFutureContract = _myProfit > 0 ? _myProfit : null,
            MaxBarsInPosition = _maxBars > 0 ? _maxBars : null,
            Reason = reason
        };

    private TradeSignal Hold(decimal price, DateTime date, string? reason = null) =>
        new()
        {
            Date = date,
            Type = SignalType.Hold,
            Price = price,
            StrategyName = Name,
            Reason = reason
        };

    private static int ToPythonDayOfWeek(DayOfWeek day) =>
        day == DayOfWeek.Sunday ? 6 : (int)day - 1;
}
