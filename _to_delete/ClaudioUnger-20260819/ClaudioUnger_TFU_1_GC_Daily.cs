namespace Piootoo.Strategies.ClaudioUnger;

/// <summary>
/// GC daily TF_U from Unger run_20260526_1108 top #1.
/// Source folder: D:/Piootoo/hunger-2/ALGO-UNGER/results/run_20260526_1108.
/// Stop entries are emitted only when the current bar touches H_d1/L_d1.
/// </summary>
public class ClaudioUnger_TFU_1_GC_Daily : ClaudioUngerTfUnmirroredBase
{
    public ClaudioUnger_TFU_1_GC_Daily()
        : base(
            name: "ClaudioUnger_TFU_1_GC_Daily",
            symbol: "@GC",
            timeframeMinutes: 1440,
            longYes: 144,
            longNo: 153,
            shortYes: 14,
            shortNo: 116,
            startHour: null,
            endHour: null,
            excludedDay: null,
            stopLossDollars: 1000m,
            takeProfitDollars: 6000m,
            maxBarsInPosition: 5)
    {
    }
}
