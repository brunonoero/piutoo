using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>TOP_UA_452 — BIASW BP 15m con regole long e short indipendenti.</summary>
public sealed class Easy_452_BP_15 : BiasWeeklyEngine
{
    public override string Name => "Easy_452_BP_15";
    public override string Description => "BIASW long/short, BP 15m";
    public override string Symbol => "@BP";
    public override int TimeframeMinutes => 15;

    public Easy_452_BP_15()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
        Contracts = 1;
        MaxEntriesPerSession = 1;
        LongSchedules = [new WeeklySchedule(1, 600, 700, 3, 730)];
        ShortSchedules = [new WeeklySchedule(3, 700, 800, 0, 500)];
        LongPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 55, true),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 56, false),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 68, true),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 59, false)
        ];
        ShortPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 55, true),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 56, false),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 68, true),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 44, false),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 69, false)
        ];
        StopMoneyLong = 1500;
        StopMoneyShort = 1500;
        ProfitMoneyLong = 1400;
        ProfitMoneyShort = 3500;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters?.TryGetValue("Contracts", out var contracts) == true)
            Contracts = Convert.ToInt32(contracts);
    }
}
