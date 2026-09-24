using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// LF e LF_HL della ricerca PT5DAV: fade del falso sfondamento. I livelli si calcolano dalla sessione
/// precedente e restano fissi per la sessione; quando il close della barra precedente sta oltre il
/// livello e quello della corrente rientra, ingresso a mercato all'apertura della barra dopo.
/// Nessun limite di ingressi per sessione (schede LF e LF_HL: "dopo un'uscita un nuovo segnale
/// riapre").
///
/// <para>La finestra operativa ha gli estremi esatti, letti sulla chiusura della barra. Il motore
/// condiviso <c>LevelFaderEngine</c> la allarga fino a <c>HH:59</c> sulla fine: qui no, perche' la
/// ricerca v5.0 confronta l'orario pieno come per gli altri motori.</para>
/// </summary>
public abstract class Pt5DavLevelFaderEngineBase : Pt5DavEngineBase
{
    /// <summary><c>level_shift_atr</c>: il livello si allontana dal pivot/estremo di questi ATR50.</summary>
    protected decimal LevelShiftAtr;

    /// <summary><c>ptn_neut_yes</c>.</summary>
    protected int NeutralYes = 55;

    /// <summary><c>ptn_neut_no</c>.</summary>
    protected int NeutralNo = 56;

    /// <summary><c>ptn_dir_yes</c>, +n sul long e −n sullo short.</summary>
    protected int DirectionalYes = 52;

    /// <summary><c>ptn_ly_yes</c>/<c>ptn_ly_no</c>/<c>ptn_sy_yes</c>/<c>ptn_sy_no</c>, libreria UAPtnBase: 41 = sempre vero, 42 = sempre falso.</summary>
    protected int BaseYesLong = 41, BaseNoLong = 42, BaseYesShort = 41, BaseNoShort = 42;

    /// <summary>Livello long e short della sessione in corso, da d1.</summary>
    protected abstract (decimal Long, decimal Short) Levels(decimal[] ohlc, decimal shift);

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (PreviousBarClose is not { } previousClose ||
            !InTradingWindow(barTime) ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var (longLevel, shortLevel) = Levels(ohlc, LevelShiftAtr * SessionAtr);
        var entries = new List<TradeSignal>(2);

        if (!IsExcludedDay(barTime, NotEntryDayLong) &&
            EasyLib.UAPtnBase(BaseYesLong, ohlc) && !EasyLib.UAPtnBase(BaseNoLong, ohlc) &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            previousClose < longLevel && bar.Close > longLevel)
        {
            AddEntry(entries, Finish(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE LF"), oneEntryPerSessionPerSide: false));
        }

        if (!IsExcludedDay(barTime, NotEntryDayShort) &&
            EasyLib.UAPtnBase(BaseYesShort, ohlc) && !EasyLib.UAPtnBase(BaseNoShort, ohlc) &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            previousClose > shortLevel && bar.Close < shortLevel)
        {
            AddEntry(entries, Finish(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE LF"), oneEntryPerSessionPerSide: false));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }
}

/// <summary>
/// LF (<c>level_choice = 1</c>): pivot della sessione precedente. Long su
/// <c>S1 − shift</c>, short su <c>R1 + shift</c>, con <c>pivot = (H+L+C)/3</c>,
/// <c>R1 = 2·pivot − L</c>, <c>S1 = 2·pivot − H</c>.
/// </summary>
public abstract class Pt5DavPivotFaderEngine : Pt5DavLevelFaderEngineBase
{
    protected override (decimal Long, decimal Short) Levels(decimal[] ohlc, decimal shift)
    {
        var (high, low, close) = (ohlc[5], ohlc[6], ohlc[7]);
        var pivot = (high + low + close) / 3m;
        return (2m * pivot - high - shift, 2m * pivot - low + shift);
    }
}

/// <summary>LF_HL (<c>level_choice = 2</c>): estremi della sessione precedente. Long su <c>L_d1 − shift</c>, short su <c>H_d1 + shift</c>.</summary>
public abstract class Pt5DavHighLowFaderEngine : Pt5DavLevelFaderEngineBase
{
    protected override (decimal Long, decimal Short) Levels(decimal[] ohlc, decimal shift) =>
        (ohlc[6] - shift, ohlc[5] + shift);
}

/// <summary>
/// RHL della ricerca PT5DAV: limit buy a <c>L_d1 − long_offset_atr × ATR50</c>, limit sell a
/// <c>H_d1 + short_offset_atr × ATR50</c>, riemesso a ogni barra valida, una entrata per sessione e
/// per direzione. Gate neutri comuni e direzionali <b>invertiti</b> (reversal): il long legge −n.
/// La scheda parla di scostamento "in tick" in un paragrafo, ma la formula stampata e la colonna del
/// CSV sono in ATR50: vale l'ATR.
/// </summary>
public abstract class Pt5DavRhlEngine : Pt5DavEngineBase
{
    /// <summary><c>long_offset_atr</c>.</summary>
    protected decimal LongOffsetAtr;

    /// <summary><c>short_offset_atr</c>.</summary>
    protected decimal ShortOffsetAtr;

    /// <summary><c>direction</c>: 0 entrambe, 1 solo long, 2 solo short.</summary>
    protected int Direction;

    protected int NeutralYes = 55;
    protected int NeutralNo = 56;
    protected int DirectionalYes = 52;
    protected int DirectionalNo = 53;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) ||
            IsExcludedDay(barTime, SkipDay) ||
            !EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        var entries = new List<TradeSignal>(2);
        if (Direction != 2 &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc))
        {
            AddEntry(entries, Finish(EntryLimitNextBar(
                SignalType.Buy, ohlc[6] - LongOffsetAtr * SessionAtr, data, barTime, "LE RHL"),
                oneEntryPerSessionPerSide: true));
        }

        if (Direction != 1 &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc))
        {
            AddEntry(entries, Finish(EntryLimitNextBar(
                SignalType.Sell, ohlc[5] + ShortOffsetAtr * SessionAtr, data, barTime, "SE RHL"),
                oneEntryPerSessionPerSide: true));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }
}

/// <summary>
/// RBB_M e RBB_U della ricerca PT5DAV: limit sulla banda di Bollinger (media semplice dei close ±
/// <c>bb_num_devs</c> deviazioni standard su <c>bb_length</c> barre). Il long si arma finche' il close
/// resta sopra la banda inferiore, lo short finche' resta sotto la superiore; bande piu' strette di
/// un tick non armano. Una entrata per sessione e per direzione.
///
/// <para>Il giorno escluso e' <c>skip_day</c> in convenzione pandas (0 = lunedi'), letto sulla
/// chiusura della barra: il motore condiviso lo legge invece con la convenzione EasyLanguage, che
/// qui escluderebbe il giorno prima (la trappola di <c>porting-da-report-sweep.md</c>).</para>
/// </summary>
public abstract class Pt5DavBollingerEngineBase : Pt5DavEngineBase
{
    /// <summary><c>bb_length</c>.</summary>
    protected int BollingerLength = 20;

    /// <summary><c>bb_num_devs</c>.</summary>
    protected decimal BollingerNumDevs = 2m;

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        if (!InTradingWindow(barTime) || IsExcludedDay(barTime, SkipDay) || !PassesCommonGates(ohlc))
            return Hold(bar.Close, barTime);

        if (Bands(data) is not { } bands || bands.Upper - bands.Lower < InstrumentRegistry.TickSize(Symbol))
            return Hold(bar.Close, barTime, "Bande non disponibili o piu' strette di un tick");

        var entries = new List<TradeSignal>(2);
        if (bar.Close < bands.Upper && PassesShortGates(ohlc))
            AddEntry(entries, Finish(
                EntryLimitNextBar(SignalType.Sell, bands.Upper, data, barTime, "SE RBB"), oneEntryPerSessionPerSide: true));

        if (bar.Close > bands.Lower && PassesLongGates(ohlc))
            AddEntry(entries, Finish(
                EntryLimitNextBar(SignalType.Buy, bands.Lower, data, barTime, "LE RBB"), oneEntryPerSessionPerSide: true));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    protected virtual bool PassesCommonGates(decimal[] ohlc) => true;

    protected abstract bool PassesLongGates(decimal[] ohlc);

    protected abstract bool PassesShortGates(decimal[] ohlc);

    private (decimal Upper, decimal Lower)? Bands(OhlcvData[] data)
    {
        if (BollingerLength < 2 || RecentMarketBars(data, BollingerLength) is not { } bars)
            return null;

        decimal sum = 0m;
        foreach (var candle in bars)
            sum += candle.Close;
        var average = sum / BollingerLength;

        decimal squares = 0m;
        foreach (var candle in bars)
        {
            var difference = candle.Close - average;
            squares += difference * difference;
        }

        // Deviazione di POPOLAZIONE (divisa per N), come il motore condiviso. Misurato sui prezzi
        // d'ingresso della ricerca su quattro RBB di NQ: con N tornano 1.313/1.319, 1.539/1.544,
        // 539/539 e 741/742 ingressi; con N−1 (il default di pandas) quasi nessuno.
        var deviation = (decimal)Math.Sqrt((double)(squares / BollingerLength));
        return (average + BollingerNumDevs * deviation, average - BollingerNumDevs * deviation);
    }
}

/// <summary>RBB_M: neutri comuni, direzionali invertiti (il long legge −n, lo short +n).</summary>
public abstract class Pt5DavBollingerMirroredEngine : Pt5DavBollingerEngineBase
{
    protected int NeutralYes = 55;
    protected int NeutralNo = 56;
    protected int DirectionalYes = 52;
    protected int DirectionalNo = 53;

    protected override bool PassesCommonGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) && !EasyLib.PatternNeutralFast(NeutralNo, ohlc);

    protected override bool PassesLongGates(decimal[] ohlc) =>
        EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
        !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc);
}

/// <summary>RBB_U: quattro gate <c>PatternFast</c> indipendenti per lato.</summary>
public abstract class Pt5DavBollingerUnmirroredEngine : Pt5DavBollingerEngineBase
{
    protected int FastYesLong = 152;
    protected int FastNoLong = 153;
    protected int FastYesShort = 152;
    protected int FastNoShort = 153;

    protected override bool PassesLongGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesLong, ohlc) && !EasyLib.PatternFast(FastNoLong, ohlc);

    protected override bool PassesShortGates(decimal[] ohlc) =>
        EasyLib.PatternFast(FastYesShort, ohlc) && !EasyLib.PatternFast(FastNoShort, ohlc);
}
