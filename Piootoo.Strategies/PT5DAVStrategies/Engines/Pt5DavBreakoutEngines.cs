using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// Gate comuni ai breakout della ricerca PT5DAV (PC, BO, BO_S, VBO): neutri per entrambi i lati,
/// direzionali col segno per verso (+n long, −n short), sentinelle 55/56 e 52/53.
/// </summary>
public abstract class Pt5DavPatternGatedEngine : Pt5DavEngineBase
{
    /// <summary><c>ptn_neut_yes</c>; 55 = sempre vero.</summary>
    protected int NeutralYes = 55;

    /// <summary><c>ptn_neut_no</c>; 56 = sempre falso.</summary>
    protected int NeutralNo = 56;

    /// <summary><c>ptn_dir_yes</c>; 52 = sempre vero.</summary>
    protected int DirectionalYes = 52;

    /// <summary><c>ptn_dir_no</c>; 53 = sempre falso.</summary>
    protected int DirectionalNo = 53;

    /// <summary>0 = entrambe le direzioni, 1 = solo long, 2 = solo short (<c>direction</c>).</summary>
    protected int Direction;

    protected bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);

    protected bool PassesDirectionalGates(int sign, decimal[] ohlc) =>
        EasyLib.PatternDirectionalFast(sign * DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(sign * DirectionalNo, ohlc);

    protected bool LongAllowed => Direction != 2;

    protected bool ShortAllowed => Direction != 1;
}

/// <summary>
/// PC della ricerca PT5DAV: stop oltre il massimo/minimo delle ultime <c>channel_len</c> barre,
/// barra di segnale inclusa, spostato di <c>breakout_offset_atr</c> × ATR50. Come il PC condiviso,
/// non emette mentre e' in posizione: sui trade della ricerca il PC non rientra mai sulla barra
/// dopo un'uscita (0 casi), a differenza di BO, BO_S e VBO.
/// </summary>
public abstract class Pt5DavPriceChannelEngine : Pt5DavPatternGatedEngine
{
    /// <summary><c>channel_len</c>: barre del canale, barra di segnale inclusa.</summary>
    protected int ChannelBars = 1;

    /// <summary><c>breakout_offset_atr</c>: distanza del livello oltre il canale, in ATR50.</summary>
    protected decimal OffsetAtr;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (CurrentMP != 0 ||
            !InTradingWindow(barTime) ||
            IsExcludedDay(barTime, SkipDay) ||
            !PassesNeutralGates(ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var (high, low) = RecentBarsRange(data, ChannelBars);
        var offset = OffsetAtr * SessionAtr;
        var entries = new List<TradeSignal>(2);

        if (LongAllowed && PassesDirectionalGates(+1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Buy, high + offset, data, barTime, "LE PC"), oneEntryPerSessionPerSide: true));

        if (ShortAllowed && PassesDirectionalGates(-1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Sell, low - offset, data, barTime, "SE PC"), oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }
}

/// <summary>
/// BO della ricerca PT5DAV (<c>level_source = 0</c>): stop oltre il massimo/minimo delle ultime
/// <c>n_sess</c> sessioni complete e, con <c>lev_include_sess0 = 1</c>, della sessione in corso
/// <b>esclusa</b> la barra di segnale; livello spostato di <c>breakout_offset_atr</c> × ATR50.
/// </summary>
public abstract class Pt5DavSessionBreakoutEngine : Pt5DavPatternGatedEngine
{
    /// <summary><c>n_sess</c>: sessioni complete del canale (1..5).</summary>
    protected int Sessions = 1;

    /// <summary><c>lev_include_sess0</c>: include la sessione in corso, barra di segnale esclusa.</summary>
    protected bool IncludeCurrentSession;

    /// <summary><c>breakout_offset_atr</c>.</summary>
    protected decimal OffsetAtr;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) || IsExcludedDay(barTime, SkipDay) || !PassesNeutralGates(ohlc))
            return Hold(bar.Close, barTime);

        var (high, low) = ClosedSessionsRange(Math.Clamp(Sessions, 1, 5));
        if (IncludeCurrentSession && CurrentSessionRangeBeforeBar is { } current)
        {
            high = Math.Max(high, current.High);
            low = Math.Min(low, current.Low);
        }

        var offset = OffsetAtr * SessionAtr;
        var entries = new List<TradeSignal>(2);

        if (LongAllowed && PassesDirectionalGates(+1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Buy, high + offset, data, barTime, "LE BO"), oneEntryPerSessionPerSide: true));

        if (ShortAllowed && PassesDirectionalGates(-1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Sell, low - offset, data, barTime, "SE BO"), oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }
}

/// <summary>
/// BO_S della ricerca PT5DAV (<c>level_source = 1</c>): stop oltre il massimo/minimo in
/// costruzione della sessione in corso, barra di segnale <b>inclusa</b>, spostato di
/// <c>breakout_offset_atr</c> × ATR50. Non c'e' look-ahead: l'ordine vive solo sulla barra dopo.
/// </summary>
public abstract class Pt5DavCurrentSessionBreakoutEngine : Pt5DavPatternGatedEngine
{
    /// <summary><c>breakout_offset_atr</c>.</summary>
    protected decimal OffsetAtr;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) || IsExcludedDay(barTime, SkipDay) || !PassesNeutralGates(ohlc))
            return Hold(bar.Close, barTime);

        var (high, low) = CurrentSessionRange;
        var offset = OffsetAtr * SessionAtr;
        var entries = new List<TradeSignal>(2);

        if (LongAllowed && PassesDirectionalGates(+1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Buy, high + offset, data, barTime, "LE BO_S"), oneEntryPerSessionPerSide: true));

        if (ShortAllowed && PassesDirectionalGates(-1, ohlc))
            AddEntry(entries, Finish(
                EntryStopNextBar(SignalType.Sell, low - offset, data, barTime, "SE BO_S"), oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }
}

/// <summary>
/// VBO della ricerca PT5DAV: stop a <c>O_d0 ± k × VOL</c>. VOL e' il range della sessione precedente
/// (<c>vol_source = 1</c>) o l'ATR a media semplice delle ultime <c>atr_len</c> barre chiuse prima
/// della corrente (<c>vol_source = 3</c>, <c>atr(df, n).shift(1)</c>); <c>vol_mult_short = -1</c>
/// vuol dire lo stesso moltiplicatore del long. Come il VBO condiviso, non riemette nel verso gia'
/// in posizione.
/// </summary>
public abstract class Pt5DavVolatilityBreakoutEngine : Pt5DavPatternGatedEngine
{
    /// <summary><c>vol_source</c>: 1 = range d1, 3 = ATR di barra.</summary>
    protected int VolatilitySource = 1;

    /// <summary><c>atr_len</c>, per <c>vol_source = 3</c>.</summary>
    protected int AtrLength;

    /// <summary><c>vol_mult</c>.</summary>
    protected decimal MultiplierLong;

    /// <summary><c>vol_mult_short</c>; -1 = uguale a <see cref="MultiplierLong"/>.</summary>
    protected decimal MultiplierShort = -1m;

    /// <summary><c>momentum</c>: 0 = spento, 1 = C_d1 contro C_d2, 2 = O_d0 contro C_d1.</summary>
    protected int Momentum;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) || IsExcludedDay(barTime, SkipDay) || !PassesNeutralGates(ohlc))
            return Hold(bar.Close, barTime);

        var volatility = VolatilitySource switch
        {
            1 => ohlc[5] - ohlc[6],
            3 => PreviousBarsAtr(data),
            _ => (decimal?)null
        };
        if (volatility is not > 0m)
            return Hold(bar.Close, barTime, "Volatilita' non disponibile");

        var sessionOpen = ohlc[0];
        var shortMultiplier = MultiplierShort < 0m ? MultiplierLong : MultiplierShort;
        var momentumLong = Momentum switch { 1 => ohlc[7] > ohlc[11], 2 => sessionOpen > ohlc[7], _ => true };
        var momentumShort = Momentum switch { 1 => ohlc[7] < ohlc[11], 2 => sessionOpen < ohlc[7], _ => true };

        var entries = new List<TradeSignal>(2);
        if (CurrentMP != 1 && LongAllowed && momentumLong && PassesDirectionalGates(+1, ohlc))
            AddEntry(entries, Finish(EntryStopNextBar(
                SignalType.Buy, sessionOpen + MultiplierLong * volatility.Value, data, barTime, "LE VBO"),
                oneEntryPerSessionPerSide: true));

        if (CurrentMP != -1 && ShortAllowed && momentumShort && PassesDirectionalGates(-1, ohlc))
            AddEntry(entries, Finish(EntryStopNextBar(
                SignalType.Sell, sessionOpen - shortMultiplier * volatility.Value, data, barTime, "SE VBO"),
                oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>Media semplice del range vero delle <see cref="AtrLength"/> barre prima della corrente.</summary>
    private decimal? PreviousBarsAtr(OhlcvData[] data)
    {
        if (AtrLength <= 0 || RecentMarketBars(data, AtrLength + 1, barsAgo: 1) is not { } bars)
            return null;

        decimal sum = 0m;
        for (var index = 1; index < bars.Length; index++)
            sum += EasyLib.TrueRange(bars[index], bars[index - 1]);
        return sum / AtrLength;
    }
}
