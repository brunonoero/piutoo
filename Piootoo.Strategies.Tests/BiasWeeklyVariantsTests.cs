using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class BiasWeeklyVariantsTests
{

    [Fact]
    public void SkipMonth_SuppressesOnlyThatSchedule()
    {
        var strategy = new VariantBiasWeekly();
        var entryTime = Utc(2024, 8, 5, 10, 0); // Monday, excluded by the first schedule

        var signal = strategy.GenerateSignal(Bars(entryTime), entryTime);

        Assert.Equal(SignalType.Hold, signal.Type);
    }


    private static OhlcvData[] Bars(DateTime current) =>
    [
        Bar(current.AddMinutes(-5), 100m),
        Bar(current, 101m)
    ];

    private static OhlcvData Bar(DateTime time, decimal open) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = open + 1m,
            Low = open - 1m,
            Close = open,
            Volume = 1m
        };

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private sealed class VariantBiasWeekly : BiasWeeklyEngine
    {
        public VariantBiasWeekly()
        {
            LongSchedules =
            [
                new WeeklySchedule(0, 1000, 1000, 3, 1500, SkipMonth: 8),
                new WeeklySchedule(0, 1005, 1010, 3, 1500)
            ];
            LongPatternRules = [new WeeklyPatternRule(WeeklyPatternKind.Fast, 153, false)];
            StopMoneyLong = 1500;
            ProfitMoneyLong = 1400;
            BreakEvenMoneyLong = 200;
            TrailingMoneyLong = 300;
            MaxEntriesPerSession = 1;
        }

        public override string Name => "BIASW-variant-test";
        public override string Description => "BIASW variant test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 5;
        public override int RequiredCandles => 2;
    }
}
