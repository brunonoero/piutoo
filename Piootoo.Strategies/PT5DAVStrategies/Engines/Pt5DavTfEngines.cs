using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// TF_M e TF_U della ricerca PT5DAV: stop buy sul massimo della sessione precedente, stop sell sul
/// minimo, una entrata per sessione e per direzione. Stessa logica di
/// <c>Easy/Engines/TfEngines.cs</c>, sulla base comune PT5DAV (ATR50, sessione della ricerca,
/// uscita al limite del CFD).
/// </summary>
public abstract class Pt5DavTfEngineBase : Pt5DavEngineBase
{
    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) || IsExcludedDay(barTime, SkipDay))
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);
        if (PassesLongGates(ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Buy, ohlc[5], data, barTime, "LE TF"), oneEntryPerSessionPerSide: true));

        if (PassesShortGates(ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Sell, ohlc[6], data, barTime, "SE TF"), oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    protected abstract bool PassesLongGates(decimal[] ohlc);

    protected abstract bool PassesShortGates(decimal[] ohlc);

    protected override bool ApplyResearchParameter(string key, object value) =>
        key == "skip_day" ? ApplyDayParameter(key, value) : base.ApplyResearchParameter(key, value);
}

/// <summary>TF_M: neutri comuni, direzionali speculari (+n long, −n short).</summary>
public abstract class Pt5DavTfMirroredEngine : Pt5DavTfEngineBase
{
    /// <summary><c>ptn_neut_yes</c>; 55 = sempre vero.</summary>
    protected int NeutralYes = 55;

    /// <summary><c>ptn_neut_no</c>; 56 = sempre falso.</summary>
    protected int NeutralNo = 56;

    /// <summary><c>ptn_dir_yes</c>; 52 = sempre vero.</summary>
    protected int DirectionalYes = 52;

    /// <summary><c>ptn_dir_no</c>; 53 = sempre falso.</summary>
    protected int DirectionalNo = 53;

    protected override bool PassesLongGates(decimal[] ohlc) =>
        PassesNeutralGates(ohlc) &&
        EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        PassesNeutralGates(ohlc) &&
        EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc);

    private bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);

    protected override bool ApplyResearchParameter(string key, object value)
    {
        switch (key)
        {
            case "ptn_neut_yes": NeutralYes = ResearchInt(value); return true;
            case "ptn_neut_no": NeutralNo = ResearchInt(value); return true;
            case "ptn_dir_yes": DirectionalYes = ResearchInt(value); return true;
            case "ptn_dir_no": DirectionalNo = ResearchInt(value); return true;
            default: return base.ApplyResearchParameter(key, value);
        }
    }
}

/// <summary>TF_U: quattro gate <c>PatternFast</c> indipendenti per lato (152 = sempre vero, 153 = sempre falso).</summary>
public abstract class Pt5DavTfUnmirroredEngine : Pt5DavTfEngineBase
{
    /// <summary><c>ptn_ly_yes</c>.</summary>
    protected int FastYesLong = 152;

    /// <summary><c>ptn_ly_no</c>.</summary>
    protected int FastNoLong = 153;

    /// <summary><c>ptn_sy_yes</c>.</summary>
    protected int FastYesShort = 152;

    /// <summary><c>ptn_sy_no</c>.</summary>
    protected int FastNoShort = 153;

    protected override bool PassesLongGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesLong, ohlc) && !EasyLib.PatternFast(FastNoLong, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc);

    protected override bool ApplyResearchParameter(string key, object value)
    {
        switch (key)
        {
            case "ptn_ly_yes": FastYesLong = ResearchInt(value); return true;
            case "ptn_ly_no": FastNoLong = ResearchInt(value); return true;
            case "ptn_sy_yes": FastYesShort = ResearchInt(value); return true;
            case "ptn_sy_no": FastNoShort = ResearchInt(value); return true;
            default: return base.ApplyResearchParameter(key, value);
        }
    }
}
