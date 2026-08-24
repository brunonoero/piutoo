namespace Piootoo.Strategies.ClaudioUnger;

/// <summary>
/// NQ 15 minute TF_M from Unger run_20260622_1629 top #1.
/// Source folder: D:/Piootoo/hunger-2/ALGO-UNGER/results/run_20260622_1629.
/// Stop entries are emitted only when the current bar touches H_d1/L_d1.
/// </summary>
public class ClaudioUnger_TFM_1_NQ_15 : ClaudioUngerTfMirroredBase
{
    public ClaudioUnger_TFM_1_NQ_15()
        : base(
            name: "ClaudioUnger_TFM_1_NQ_15",
            symbol: "@NQ",
            timeframeMinutes: 15,
            neutralYes: 47,
            neutralNo: 17,
            directionalYes: 44,
            directionalNo: 11,
            startHour: null,
            endHour: null,
            excludedDay: null,
            stopLossDollars: 1000m,
            takeProfitDollars: 3000m,
            maxBarsInPosition: 0)
    {
    }
}
