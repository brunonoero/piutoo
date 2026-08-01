using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>TOP_UA_196 — BIASW short EC 5m del venerdì.</summary>
public sealed class Easy_196_EC_5 : BiasWeeklyEngine
{
    public override string Name => "Easy_196_EC_5";
    public override string Description => "BIASW short, EC 5m";
    public override string Symbol => "@EC";
    public override int TimeframeMinutes => 5;

    public Easy_196_EC_5()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
        Contracts = 1;
        EnableLong = false;
        ShortSchedules = [new WeeklySchedule(4, 15, 15, 4, 810)];
        ShortPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 40, true),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 33, false),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 47, false),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, 33, true),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, -40, false),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, -36, false)
        ];
        StopMoneyShort = 2000;
        ProfitMoneyShort = 1000;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters?.TryGetValue("Contracts", out var contracts) == true)
            Contracts = Convert.ToInt32(contracts);
    }
}
