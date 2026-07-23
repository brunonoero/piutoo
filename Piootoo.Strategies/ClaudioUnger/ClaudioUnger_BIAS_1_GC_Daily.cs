namespace Piootoo.Strategies.ClaudioUnger;

/// <summary>
/// GC daily BIAS from Unger run_20260524_2220 top #1.
/// Source folder: D:/Piootoo/hunger-2/ALGO-UNGER/results/run_20260524_2220.
/// Parameters from top_final.json; engine semantics follow unger/core/engines/bias.py.
/// </summary>
public class ClaudioUnger_BIAS_1_GC_Daily : ClaudioUngerBiasDailyBase
{
    public ClaudioUnger_BIAS_1_GC_Daily()
        : base(
            name: "ClaudioUnger_BIAS_1_GC_Daily",
            symbol: "@GC",
            longYes: 144,
            longNo: 153,
            shortYes: 152,
            shortNo: 153,
            excludedLongDay: null,
            excludedShortDay: null,
            stopLossDollars: 1000m,
            takeProfitDollars: null,
            maxBarsInPosition: 1)
    {
    }
}
