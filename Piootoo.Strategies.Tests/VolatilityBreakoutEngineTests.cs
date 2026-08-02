using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class VolatilityBreakoutEngineTests
{
    [Fact]
    public void PythonVbo_UsesPreviousSessionRange_AsNextBarStopsWithSingleSessionLimit()
    {
        var bars = BuildBars();
        SetSessionRange(bars, new DateTime(2024, 1, 18, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 19, 17, 0, 0, DateTimeKind.Utc), 140m, 80m);
        var strategy = new TestVbo(volatilitySource: 1);

        var signal = Evaluate(strategy, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(161m, signal.Price); // O_d0 101 + (H_d1 140 - L_d1 80).
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        Assert.Equal(new DateTime(2024, 1, 19, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);

        var shortSignal = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(41m, shortSignal.Price);
    }

    [Fact]
    public void PythonVbo_UsesClosedVolatilityAndHonorsMomentumTimeDayAndDirection()
    {
        var bars = BuildBars();
        var strategy = new TestVbo(volatilitySource: 3, momentum: 2, direction: 1, startHour: 12, endHour: 12);

        var signal = Evaluate(strategy, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.CompanionSignals);

        var currentRangeDoesNotLeak = (OhlcvData[])bars.Clone();
        currentRangeDoesNotLeak[^1].High = 9_999m;
        currentRangeDoesNotLeak[^1].Low = 1m;
        Assert.Equal(signal.Price, Evaluate(strategy, currentRangeDoesNotLeak).Price);

        var blockedByDay = new TestVbo(volatilitySource: 3, skipDay: 5); // Sabato, convenzione pandas.
        Assert.Equal(SignalType.Hold, Evaluate(blockedByDay, bars).Type);

        // Solo long: con C_d1 alzato a 150, O_d0 < C_d1 spegne il momentum long.
        // Senza Direction=1 lo short resterebbe valido (momentum short invertito).
        var blockedByMomentum = new TestVbo(volatilitySource: 3, momentum: 2, direction: 1);
        bars.Single(bar => bar.DateTime == new DateTime(2024, 1, 19, 16, 0, 0, DateTimeKind.Utc)).Close = 150m;
        Assert.Equal(SignalType.Hold, Evaluate(blockedByMomentum, bars).Type);
    }

    [Fact]
    public void PythonVbo_DailyAtr_IgnoresCurrentSessionRange()
    {
        var bars = BuildBars();
        var strategy = new TestVbo(volatilitySource: 2, atrLength: 2);

        var baseline = Evaluate(strategy, bars);
        Assert.Equal(SignalType.Buy, baseline.Type);
        bars[^1].High = 9_999m;
        bars[^1].Low = 1m;

        Assert.Equal(baseline.Price, Evaluate(strategy, bars).Price);
    }

    private static TradeSignal Evaluate(TestVbo strategy, OhlcvData[] bars) =>
        // Chiamata diretta: evita il clone di Evaluate che, nei test, non è necessario
        // e maschera i parametri impostati sul costruttore specializzato.
        strategy.GenerateSignal(bars, bars[^1].DateTime);

    private static OhlcvData[] BuildBars()
    {
        var first = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var bars = new OhlcvData[469]; // ultima barra: sabato 20 gennaio, ore 12.
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = first.AddHours(index),
                Open = 100m,
                High = 110m,
                Low = 90m,
                Close = 100m,
                Volume = 1m
            };
        }

        // O_d0 > C_d1: abilita il momentum 2 long.
        bars.Single(bar => bar.DateTime == new DateTime(2024, 1, 19, 18, 0, 0, DateTimeKind.Utc)).Open = 101m;
        bars[^1].Close = 101m;
        return bars;
    }

    private static void SetSessionRange(OhlcvData[] bars, DateTime from, DateTime to, decimal high, decimal low)
    {
        foreach (var bar in bars)
        {
            if (bar.DateTime >= from && bar.DateTime <= to)
            {
                bar.High = high;
                bar.Low = low;
            }
        }
    }

    private sealed class TestVbo : VolatilityBreakoutEngine
    {
        // Costruttore senza parametri obbligatorio: Evaluate clona via Activator.
        public TestVbo()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            AtrMultiplierLong = 1m;
            AtrMultiplierShort = -1m;
        }

        public TestVbo(int volatilitySource, int atrLength = 2, int momentum = 0, int direction = 0,
            int startHour = -1, int endHour = -1, int skipDay = -1) : this()
        {
            VolatilitySource = volatilitySource;
            AtrLength = atrLength;
            Momentum = momentum;
            Direction = direction;
            StartTrade = startHour < 0 ? -1 : startHour * 100;
            EndTrade = endHour < 0 ? -1 : endHour * 100;
            SkipDay = skipDay;
        }

        public override string Name => "TEST_VBO_NQ_60";
        public override string Description => "Strategia di prova VBO Python";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;

        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
            EvaluateCore(data, currentDate);
    }
}
