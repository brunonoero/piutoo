using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class VolatilityBreakoutEngineTests
{

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
