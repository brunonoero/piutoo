namespace Piootoo.Strategies.ClaudioUnger;

/// <summary>
/// GC 60 minute RBB_M from Unger run_20260521_1959 top #1.
/// Source folder: D:/Piootoo/hunger-2/ALGO-UNGER/results/run_20260521_1959.
/// Limit entries are emitted only when the current bar touches the Bollinger band.
/// </summary>
public class ClaudioUnger_RBBM_1_GC_60 : ClaudioUngerRbbMirroredBase
{
    public ClaudioUnger_RBBM_1_GC_60()
        : base(
            name: "ClaudioUnger_RBBM_1_GC_60",
            symbol: "@GC",
            timeframeMinutes: 60,
            bbLength: 14,
            bbNumDevs: 3m,
            neutralYes: 55,
            neutralNo: 49,
            directionalYes: 52,
            directionalNo: 41,
            startHour: 14,
            endHour: 22,
            excludedDay: null,
            stopLossDollars: 500m,
            takeProfitDollars: 1000m,
            maxBarsInPosition: 12)
    {
    }
}
