using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_587 — breakout su banda <c>MA(5) ± ATR(5) × AVGMoltip/10</c> con filtro ADX, NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_587_NQ_15__7.txt</c>. La variante non
/// coincide con il VBO Python (banda su apertura di sessione): usa una media mobile delle chiusure
/// e stop <c>next bar</c> sui livelli calcolati. I gate direzionali sono specchiati come nel
/// sorgente.</para>
///
/// <para><b>Uscite.</b> Stop, breakeven e chiusura dopo <c>maxdaysintrade = 2</c> sessioni
/// (approssimata con <c>MaxDaysInTrade</c> e <c>CloseAtUtc</c> a <c>flatTime</c>).</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $2.500, breakeven $2.000.</para>
/// </summary>
public sealed class Easy_587_NQ_15 : VolatilityBreakoutEngine
{
    private const int MaLength = 5;
    private const int AdxLength = 5;
    private const int AdxThresholdHigh = 50;
    private const int AdxThresholdLow = 20;
    private const int FlatTime = 1530;

    private bool _longArmed = true;
    private bool _shortArmed = true;
    private decimal _adxValue;
    private decimal _adx0;
    private decimal _adx1;
    private decimal _adx2;
    private decimal _adx3;

    public override string Name => "Easy_587_NQ_15";
    public override string Description => "Breakout banda MA±ATR con ADX, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    private int AtrBandFactor { get; set; } = 17;

    public Easy_587_NQ_15()
    {
        SessionStartTime = 1700;  // SessBegin
        SessionEndTime = 1600;    // SessEnd
        Contracts = 1;

        StartTrade = 800;    // MyStartTime
        EndTrade = 1300;     // MyEndTime
        PauseStart = 1200;   // coppia invertita: pausa inattiva
        PauseEnd = 1100;

        NeutralYes = 32;      // PtnNeutYes
        NeutralNo = 8;        // PtnNeutNo
        DirectionalYes = 49;  // ptnDirYes
        DirectionalNo = 16;   // ptnDirNo

        StopMoney = 2500;       // MyStop
        BreakEvenMoney = 2000;  // MyBreakeven
        ProfitMoney = 0;        // MyProfit
        MaxDaysInTrade = 2;     // maxdaysintrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
        {
            _longArmed = true;
            _shortArmed = true;
            UpdateSessionAdx(ohlc);
        }

        if (!EasyLib.TimeWindow(StartTrade, EndTrade, barTime) || IsInPause(barTime))
            return Hold(bar.Close, barTime);

        if (!EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc) ||
            _adxValue >= AdxThresholdHigh ||
            _adxValue <= AdxThresholdLow)
        {
            return Hold(bar.Close, barTime);
        }

        var average = AverageClose(data, MaLength);
        var atr = EasyLib.AvgTrueRange(data, MaLength);
        var bandOffset = AtrBandFactor / 10m * atr;
        var longLevel = average + bandOffset;
        var shortLevel = average - bandOffset;

        if (CurrentMP == 1) _longArmed = false;
        if (CurrentMP == -1) _shortArmed = false;

        var entries = new List<TradeSignal>(2);

        if (_longArmed &&
            CurrentMP != 1 &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc))
        {
            entries.Add(WithTimedExit(
                EntryStopNextBar(SignalType.Buy, longLevel, data, barTime, "LE ATR Band Breakout")));
        }

        if (_shortArmed &&
            CurrentMP != -1 &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc))
        {
            entries.Add(WithTimedExit(
                EntryStopNextBar(SignalType.Sell, shortLevel, data, barTime, "SE ATR Band Breakout")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal WithTimedExit(TradeSignal signal)
    {
        signal.CloseAtUtc = ResolveCloseAtUtc(signal.Date, FlatTime);
        return signal;
    }

    private void UpdateSessionAdx(decimal[] ohlc)
    {
        var calc = new[] { _adx0, _adx1, _adx2, _adx3 };
        _adxValue = EasyLib.iADXOnArray(
            AdxLength,
            ohlc[5], ohlc[6], ohlc[7],
            ohlc[9], ohlc[10], ohlc[11],
            ref calc) * 100m;
        _adx0 = calc[0];
        _adx1 = calc[1];
        _adx2 = calc[2];
        _adx3 = calc[3];
    }

    private static decimal AverageClose(OhlcvData[] data, int length)
    {
        var count = Math.Min(length, data.Length);
        decimal sum = 0m;
        for (var index = data.Length - count; index < data.Length; index++)
            sum += data[index].Close;
        return sum / count;
    }

    private bool IsInPause(DateTime barTime)
    {
        if (PauseStart < 0 || PauseEnd < 0) return false;
        var time = Hhmm(barTime);
        return time >= PauseStart && time <= PauseEnd;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
        if (parameters.TryGetValue("AVGMoltip", out var avgMoltip))
            AtrBandFactor = Convert.ToInt32(avgMoltip);
    }
}
