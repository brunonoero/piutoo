using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>TOP_UA_100 — BIASW short PL 5m con cicli giovedì e domenica sera.</summary>
public sealed class Easy_100_PL_5 : BiasWeeklyEngine
{
    public override string Name => "Easy_100_PL_5";
    public override string Description => "BIASW short, PL 5m";
    public override string Symbol => "@PL";
    public override int TimeframeMinutes => 5;

    public Easy_100_PL_5()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;
        EnableLong = false;
        ShortSchedules =
        [
            // EL giovedì=4 e domenica=0; BIASW lunedì=0.
            new WeeklySchedule(3, 245, 255, 3, 945, SkipMonth: 8),
            new WeeklySchedule(6, 2015, 2025, 0, 915, SkipMonth: 8)
        ];
        FastYesShort = 87;
        FastNoShort = 77;
        StopMoneyShort = 1400;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters?.TryGetValue("Contracts", out var contracts) == true)
            Contracts = Convert.ToInt32(contracts);
    }
}
