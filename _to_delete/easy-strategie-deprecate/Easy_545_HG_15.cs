using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>TOP_UA_545 — BIASW HG 15m: short a inizio settimana, long a fine settimana.</summary>
public sealed class Easy_545_HG_15 : BiasWeeklyEngine
{
    public override string Name => "Easy_545_HG_15";
    public override string Description => "BIASW long/short, HG 15m";
    public override string Symbol => "@HG";
    public override int TimeframeMinutes => 15;

    public Easy_545_HG_15()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1659;
        Contracts = 1;
        LongSchedules = [new WeeklySchedule(3, 1145, 1145, 4, 1615)];
        ShortSchedules = [new WeeklySchedule(6, 2300, 2300, 1, 730)];
        LongPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 32, true),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 49, false),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, 52, true)
        ];
        ShortPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 21, true),
            new WeeklyPatternRule(WeeklyPatternKind.BaseSA2, 22, false)
        ];
        StopMoneyLong = 1300;
        StopMoneyShort = 1300;
        ProfitMoneyLong = 4300;
        ProfitMoneyShort = 2400;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters?.TryGetValue("Contracts", out var contracts) == true)
            Contracts = Convert.ToInt32(contracts);
    }
}
