using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore RHL (reversal su massimo/minimo della sessione precedente).
///
/// <para>Port fedele di <c>easy_engine_py/reversal_hl.py</c>: long limit a
/// <c>L_d1 - offset</c>, short limit a <c>H_d1 + offset</c>; i gate sono la famiglia
/// neutral + directional mirrored, con il verso direzionale invertito perché è un reversal.</para>
///
/// <para>Gli ingressi sono riemessi sulla barra successiva finché la direzione non è già
/// entrata nella sessione. Stop, target e limite di barre sono dichiarati sul segnale
/// d'ingresso tramite <see cref="EasyEngineBase"/>.</para>
/// </summary>
public abstract class RhlEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ sessione e finestra

    /// <summary>
    /// Inizializza la sessione OHLC dell'originale (<c>SessBegin=1700</c>,
    /// <c>SessEnd=1600</c>). I campi sono ereditati: non vanno nascosti, perché
    /// <see cref="EasyEngineBase.BuildSessionOhlc"/> li legge direttamente.
    /// </summary>
    protected RhlEngine()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
    }

    // ------------------------------------------------------------------ livelli e gate

    /// <summary>Ampiezza di un tick del simbolo.</summary>
    protected decimal TickSize = 1m;

    /// <summary>
    /// Offset del limit long in tick: positivo richiede un minimo più profondo
    /// (<c>long_offset_ticks</c>).
    /// </summary>
    protected int LongLevelOffsetTicks;

    /// <summary>
    /// Offset del limit short in tick: positivo richiede un massimo più alto
    /// (<c>short_offset_ticks</c>).
    /// </summary>
    protected int ShortLevelOffsetTicks;

    /// <summary>Direzione consentita: 0 entrambe, 1 long, 2 short.</summary>
    protected int Direction;

    /// <summary>Gate neutral richiesto e inibitore.</summary>
    protected int NeutralYes = 55;
    protected int NeutralNo = 56;

    /// <summary>Gate directional mirrored richiesto e inibitore.</summary>
    protected int DirectionalYes = 52;
    protected int DirectionalNo = 53;

    /// <summary>
    /// Finestra oraria in ore UTC, inclusiva; <c>-1</c> disabilita il rispettivo limite,
    /// come <c>start_hour</c>/<c>end_hour</c> Python.
    /// </summary>
    protected int StartHour = -1;
    protected int EndHour = -1;

    /// <summary>Giorno da escludere: 0 = lunedì … 4 = venerdì; -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    // ------------------------------------------------------------------ stato di sessione

    private bool _okLong = true;
    private bool _okShort = true;

    /// <summary>Genera gli eventuali due limit RHL validi sulla sola barra successiva.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
        {
            _okLong = true;
            _okShort = true;
        }

        // L'ingresso nella direzione già posseduta spegne solo quel verso fino al reset.
        if (CurrentMP == 1) _okLong = false;
        if (CurrentMP == -1) _okShort = false;

        if (!InTradingWindow(barTime) || IsSkippedPythonWeekday(barTime))
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);
        var lowD1 = ohlc[6];
        var highD1 = ohlc[5];

        if (_okLong && lowD1 > 0m &&
            EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
            !EasyLib.PatternNeutralFast(NeutralNo, ohlc) &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc) &&
            Direction != 2)
        {
            var level = lowD1 - LongLevelOffsetTicks * TickSize;
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Buy, level, data, barTime, "LE RHL")));
        }

        if (_okShort && highD1 > 0m &&
            EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
            !EasyLib.PatternNeutralFast(NeutralNo, ohlc) &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc) &&
            Direction != 1)
        {
            var level = highD1 + ShortLevelOffsetTicks * TickSize;
            entries.Add(WithPythonSettings(
                EntryLimitNextBar(SignalType.Sell, level, data, barTime, "SE RHL")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>
    /// Policy <c>single_entry_per_session=True</c> e <c>exit_on_session_end</c> del motore
    /// Python, dichiarate sul segnale invece che dedotte da un contatore locale: un limit non
    /// riempito deve poter essere riemesso alla barra dopo, e in sessione <c>ExternalBroker</c>
    /// il server emette solo intent di ingresso, quindi l'uscita di fine sessione va scritta qui.
    /// </summary>
    private TradeSignal WithPythonSettings(TradeSignal signal)
    {
        signal.MaxEntriesPerSession = 1;
        signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(signal.ValidFromUtc!.Value);

        if (AppliesSessionExit)
            signal.CloseAtUtc = ResolveCloseAtUtc(signal.ValidFromUtc.Value, SessionEndTime);

        return signal;
    }

    private bool InTradingWindow(DateTime barTime)
    {
        if (InDeclaredWindow(barTime) is { } declared)
            return declared;

        if (StartHour < 0 && EndHour < 0)
            return true;

        var startTime = Math.Max(0, StartHour) * 100;
        var endTime = EndHour < 0 ? 2359 : EndHour * 100;
        return EasyLib.TimeWindowInclusive(Clock, startTime, endTime, barTime);
    }

    private bool IsSkippedPythonWeekday(DateTime barTime) =>
        SkipDay >= 0 && PythonWeekday(barTime) == SkipDay;
}
