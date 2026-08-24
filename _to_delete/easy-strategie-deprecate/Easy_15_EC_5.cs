using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>TOP_UA_15 — BIASW long EC 5m, da lunedì mattina a martedì mattina.</summary>
public sealed class Easy_15_EC_5 : BiasWeeklyEngine
{
    public override string Name => "Easy_15_EC_5";
    public override string Description => "BIASW long + trailing stop, EC 5m";
    public override string Symbol => "@EC";
    public override int TimeframeMinutes => 5;

    public Easy_15_EC_5()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
        Contracts = 1;
        EnableShort = false;
        LongSchedules = [new WeeklySchedule(0, 700, 700, 1, 300)];
        LongPatternRules =
        [
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 40, true),
            new WeeklyPatternRule(WeeklyPatternKind.NeutralFast, 2, false),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, 52, true),
            new WeeklyPatternRule(WeeklyPatternKind.DirectionalFast, 10, false)
        ];
        StopMoneyLong = 1500;
        ProfitMoneyLong = 1400;
        TrailingMoneyLong = 1500; // highest(close, barssinceentry + 1) - MyStop / bigpointvalue
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters?.TryGetValue("Contracts", out var contracts) == true)
            Contracts = Convert.ToInt32(contracts);
    }
}
