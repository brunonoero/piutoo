using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// La famiglia BIAS della ricerca PT5DAV: tutto e' ancorato all'<b>indice della barra nella
/// sessione</b>, contato da 0 sulla sessione della ricerca (domenica sera accodata al lunedi').
///
/// <list type="bullet">
///   <item><b>BIAS</b> (<c>entry_type = 1</c>): market all'apertura della barra <c>le_bar</c>, con i
///   pattern letti alla chiusura della barra prima;</item>
///   <item><b>BIAS_BO</b> (<c>entry_type = 2</c>) e <b>BIAS_RT</b> (<c>entry_type = 3</c>): la
///   direzione si arma alla barra <c>le_bar</c> se i pattern sono veri in quel momento, poi a ogni
///   barra fino a <c>end_long</c> esclusa nasce uno stop (BO) sul massimo delle ultime
///   <c>nhigh</c> barre o un limit (RT) sul minimo delle ultime <c>nlow</c>, vivo sulla barra dopo.
///   Gli estremi sono di barre <b>gia' chiuse prima</b> della barra di segnale: misurato su
///   NQ-4H-BIASBO, 2.506 ingressi su 2.511 cadono li', 357 sulla barra di segnale.</item>
/// </list>
///
/// <para>Una entrata per sessione e per direzione per tutte e tre (dopo il fill la direzione non si
/// riarma). L'uscita e' alla chiusura della barra <c>lx_bar</c>: i trade della ricerca escono li'
/// (BP-15M-BIAS, <c>sx_bar = 55</c>: uscite alla chiusura della barra delle 13:45, lo scarto dal
/// close del nostro feed e' un tick di slippage costante), non all'apertura come scrive la scheda.
/// Se la barra di uscita e' gia' passata o non esiste nella sessione (un 4h ne ha sei), vale
/// l'uscita intraday: tutte le BIAS sono intraday ("Solo intraday" nelle schede).</para>
/// </summary>
public abstract class Pt5DavBiasEngineBase : Pt5DavEngineBase
{
    /// <summary><c>le_bar</c>.</summary>
    protected int ArmBarLong;

    /// <summary><c>lx_bar</c>.</summary>
    protected int ExitBarLong;

    /// <summary><c>end_long</c>: fine esclusa della finestra d'ingresso (tipi 2 e 3).</summary>
    protected int EndLong;

    /// <summary><c>se_bar</c>.</summary>
    protected int ArmBarShort;

    /// <summary><c>sx_bar</c>.</summary>
    protected int ExitBarShort;

    /// <summary><c>end_short</c>.</summary>
    protected int EndShort;

    /// <summary><c>ptn_ly_yes</c>/<c>ptn_ly_no</c>/<c>ptn_sy_yes</c>/<c>ptn_sy_no</c>, libreria fast: 152 = sempre vero, 153 = sempre falso (lato spento).</summary>
    protected int PatternLongYes = 152, PatternLongNo = 153, PatternShortYes = 152, PatternShortNo = 153;

    /// <summary><c>nhigh</c>: barre del massimo rolling.</summary>
    protected int BreakoutBarsHigh = 1;

    /// <summary><c>nlow</c>: barre del minimo rolling.</summary>
    protected int BreakoutBarsLow = 1;

    /// <summary>Il tipo d'ingresso della variante (<c>entry_type</c>).</summary>
    protected abstract BiasEntryType EntryType { get; }

    // Persistiti fra le barre come RuntimeState.
    private bool _armedLong;
    private bool _armedShort;

    protected Pt5DavBiasEngineBase()
    {
        IntradayOnly = true;
    }

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        var index = SessionBarIndex;
        var entries = new List<TradeSignal>(2);

        if (EntryType == BiasEntryType.MarketOnArmBar)
        {
            var nextBar = EasyLib.EstimateNextBarUtc(data, barTime, TimeframeMinutes);
            if (ResearchSessionDay(nextBar) != ResearchSessionDay(barTime))
                return Hold(bar.Close, barTime);

            var entryIndex = index + 1;
            if (CurrentMP != 1 && entryIndex == ArmBarLong &&
                PatternPasses(PatternLongYes, PatternLongNo, ohlc) && !IsExcludedDay(nextBar, NotEntryDayLong))
            {
                AddEntry(entries, WithBarExit(Finish(
                    EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE BIAS"), oneEntryPerSessionPerSide: true),
                    entryIndex, ExitBarLong));
            }

            if (CurrentMP != -1 && entryIndex == ArmBarShort &&
                PatternPasses(PatternShortYes, PatternShortNo, ohlc) && !IsExcludedDay(nextBar, NotEntryDayShort))
            {
                AddEntry(entries, WithBarExit(Finish(
                    EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE BIAS"), oneEntryPerSessionPerSide: true),
                    entryIndex, ExitBarShort));
            }

            return Combine(entries, Hold(bar.Close, barTime));
        }

        // Ogni direzione si disarma all'apertura della sessione, anche quando la finestra "attraversa
        // il cambio di sessione" (start > end): la scheda lo dice, ma i trade no. FDAX-15M-BIASRT
        // (finestra long 41 -> 28) entra solo fra la barra 42 e la fine della sessione, mai la
        // mattina dopo: portare l'armamento oltre la sessione dava 188 ingressi alle 08:15-09:00 che
        // la ricerca non ha.
        if (index == 0)
        {
            _armedLong = false;
            _armedShort = false;
        }

        if (index == ArmBarLong)
            _armedLong = PatternPasses(PatternLongYes, PatternLongNo, ohlc) && !IsExcludedDay(barTime, NotEntryDayLong);
        if (index == ArmBarShort)
            _armedShort = PatternPasses(PatternShortYes, PatternShortNo, ohlc) && !IsExcludedDay(barTime, NotEntryDayShort);

        if (CurrentMP == 1) _armedLong = false;
        if (CurrentMP == -1) _armedShort = false;

        if (_armedLong && InBarWindow(ArmBarLong, EndLong, index))
        {
            var level = EntryType == BiasEntryType.BreakoutStop
                ? PriorBarsRange(data, BreakoutBarsHigh)?.High
                : PriorBarsRange(data, BreakoutBarsLow)?.Low;
            if (level is { } price)
            {
                var signal = EntryType == BiasEntryType.BreakoutStop
                    ? EntryStopNextBar(SignalType.Buy, price, data, barTime, "LE BIAS_BO")
                    : EntryLimitNextBar(SignalType.Buy, price, data, barTime, "LE BIAS_RT");
                AddEntry(entries, WithBarExit(Finish(signal, oneEntryPerSessionPerSide: true), index + 1, ExitBarLong));
            }
        }

        if (_armedShort && InBarWindow(ArmBarShort, EndShort, index))
        {
            var level = EntryType == BiasEntryType.BreakoutStop
                ? PriorBarsRange(data, BreakoutBarsLow)?.Low
                : PriorBarsRange(data, BreakoutBarsHigh)?.High;
            if (level is { } price)
            {
                var signal = EntryType == BiasEntryType.BreakoutStop
                    ? EntryStopNextBar(SignalType.Sell, price, data, barTime, "SE BIAS_BO")
                    : EntryLimitNextBar(SignalType.Sell, price, data, barTime, "SE BIAS_RT");
                AddEntry(entries, WithBarExit(Finish(signal, oneEntryPerSessionPerSide: true), index + 1, ExitBarShort));
            }
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>
    /// L'uscita a indice di barra come numero di barre in posizione, contate dalla barra
    /// d'ingresso: si esce alla chiusura della barra <paramref name="exitBar"/>. Una barra di uscita
    /// gia' passata non aggiunge nulla: resta l'uscita intraday che <see cref="Pt5DavEngineBase.Finish"/>
    /// ha gia' dichiarato. Il limite piu' stretto dei due vince.
    /// </summary>
    private static TradeSignal? WithBarExit(TradeSignal? signal, int entryBar, int exitBar)
    {
        if (signal is null)
            return null;

        // La ricerca controlla l'uscita dalla barra DOPO quella d'ingresso: una barra di uscita
        // uguale a quella d'ingresso non scatta mai, e il trade chiude al limite intraday. Misurato
        // su NQ-4H-BIASBO (short: se_bar 1, ingresso sulla barra 2, sx_bar 2): 249 uscite FORCE.
        // MaxBarsInPosition conta la barra d'ingresso, quindi uscire alla chiusura della barra
        // exitBar vale exitBar − entryBar + 1 barre.
        var bars = exitBar - entryBar + 1;
        if (exitBar > entryBar)
        {
            signal.MaxBarsInPosition = signal.MaxBarsInPosition is { } declared && declared > 0
                ? Math.Min(declared, bars)
                : bars;
        }

        return signal;
    }

    private static bool PatternPasses(int yes, int no, decimal[] ohlc) =>
        EasyLib.PatternFast(yes, ohlc) && !EasyLib.PatternFast(no, ohlc);

    /// <summary>Barra nella finestra <c>[start, end)</c>, che se <c>start &gt; end</c> attraversa il cambio di sessione.</summary>
    private static bool InBarWindow(int start, int end, int index) =>
        start <= end ? index >= start && index < end : index >= start || index < end;

    /// <summary>Massimo e minimo delle <paramref name="bars"/> barre di mercato chiuse prima della barra di segnale.</summary>
    private (decimal High, decimal Low)? PriorBarsRange(OhlcvData[] data, int bars)
    {
        if (bars <= 0 || RecentMarketBars(data, bars, barsAgo: 1) is not { } window)
            return null;

        var high = window[0].High;
        var low = window[0].Low;
        foreach (var candle in window)
        {
            high = Math.Max(high, candle.High);
            low = Math.Min(low, candle.Low);
        }

        return (high, low);
    }
}

/// <summary>BIAS: market a barra fissa (<c>entry_type = 1</c>).</summary>
public abstract class Pt5DavBiasMarketEngine : Pt5DavBiasEngineBase
{
    protected override BiasEntryType EntryType => BiasEntryType.MarketOnArmBar;
}

/// <summary>BIAS_BO: stop sulla rottura degli estremi recenti dentro la finestra di barre (<c>entry_type = 2</c>).</summary>
public abstract class Pt5DavBiasBreakoutEngine : Pt5DavBiasEngineBase
{
    protected override BiasEntryType EntryType => BiasEntryType.BreakoutStop;
}

/// <summary>BIAS_RT: limit sul ritracciamento agli estremi recenti dentro la finestra di barre (<c>entry_type = 3</c>).</summary>
public abstract class Pt5DavBiasRetracementEngine : Pt5DavBiasEngineBase
{
    protected override BiasEntryType EntryType => BiasEntryType.RetracementLimit;
}
