using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>Da dove viene il livello di ingresso (<c>MyTrigger</c> dell'originale).</summary>
public enum TrendTrigger
{
    /// <summary>Estremi della sessione in corso letti dal grafico (<c>highs(0)</c>/<c>lows(0)</c>).</summary>
    CurrentSessionExtremes = 0,

    /// <summary>Estremi della sessione in corso ricostruiti da <c>_OHLCMulti5</c> (d0).</summary>
    CurrentSessionOhlc = 1,

    /// <summary>Estremi della sessione precedente (d1).</summary>
    PreviousSessionOhlc = 2
}

/// <summary>
/// Motore "Trend Developer": stop di rottura su un livello di sessione, dentro una finestra
/// oraria, con gate di pattern neutri e direzionali e un tetto di ingressi giornalieri.
///
/// <para>La forma è la più diffusa del catalogo. Il livello long è il massimo della sessione
/// corrente o di quella precedente secondo <see cref="Trigger"/>; lo short usa il minimo
/// corrispondente. Finché la finestra è aperta e i gate passano, l'ordine viene riemesso a ogni
/// barra, valido solo per la successiva.</para>
///
/// <para>Copre <c>TOP_UA_291</c> e, con i gate aggiuntivi, <c>TOP_UA_303</c>; la stessa
/// struttura serve 10, 152, 246 e 796.</para>
/// </summary>
public abstract class TrendDeveloperEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ livello

    /// <summary>Sorgente del livello di ingresso.</summary>
    protected TrendTrigger Trigger = TrendTrigger.CurrentSessionOhlc;

    // ------------------------------------------------------------------ finestra e limiti

    /// <summary>Inizio finestra operativa HHMM (<c>MyStartTrade</c>).</summary>
    protected int StartTrade;

    /// <summary>Fine finestra operativa HHMM (<c>MyEndTrade</c>).</summary>
    protected int EndTrade = 2359;

    /// <summary>
    /// Se true la fine finestra è inclusa. L'originale 291 usa <c>tw()</c>, che esclude la fine;
    /// la 303 riscrive la condizione a mano e la include. Sono una barra di differenza sul
    /// bordo, e vanno tenute distinte.
    /// </summary>
    protected bool InclusiveWindowEnd;

    /// <summary>Numero massimo di ingressi nella giornata (<c>MaxTradesPerDay</c>). 0 = illimitato.</summary>
    protected int MaxTradesPerDay;

    // ------------------------------------------------------------------ gate di pattern

    /// <summary>Pattern neutro richiesto (<c>PtnNeutYes</c>).</summary>
    protected int NeutralYes = 55;

    /// <summary>Pattern neutro che impedisce l'operatività (<c>PtnNeutNo</c>).</summary>
    protected int NeutralNo = 56;

    /// <summary>
    /// Pattern direzionale richiesto. Il long usa <c>+DirectionalYes</c>, lo short
    /// <c>-DirectionalYes</c>: se il parametro è già negativo i due versi si scambiano, ed è
    /// voluto (la 303 usa -47).
    /// </summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che impedisce l'ingresso (<c>ptnDirNo</c>).</summary>
    protected int DirectionalNo = 53;

    /// <summary>Giorno della settimana escluso per il long, 0 = domenica. -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno della settimana escluso per lo short, 0 = domenica. -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    /// <summary>Mese escluso per il long (1-12). -1 = nessuno.</summary>
    protected int NotEntryMonthLong = -1;

    /// <summary>Mese escluso per lo short (1-12). -1 = nessuno.</summary>
    protected int NotEntryMonthShort = -1;

    // ------------------------------------------------------------------ gate PtnBaseSA2

    // Molte varianti aggiungono un secondo gate per verso sulla libreria PtnBaseSA2
    // (alias UAPtnBase). I default sono le sentinelle: 41 = sempre vero, 42 = sempre falso,
    // così un gate non configurato non filtra nulla.

    /// <summary>Pattern PtnBaseSA2 richiesto per il long (<c>PtnLY</c>).</summary>
    protected int BaseYesLong = 41;

    /// <summary>Pattern PtnBaseSA2 che impedisce il long (<c>PtnLN</c>).</summary>
    protected int BaseNoLong = 42;

    /// <summary>Pattern PtnBaseSA2 richiesto per lo short (<c>PtnSY</c>).</summary>
    protected int BaseYesShort = 41;

    /// <summary>Pattern PtnBaseSA2 che impedisce lo short (<c>PtnSN</c>).</summary>
    protected int BaseNoShort = 42;

    // ------------------------------------------------------------------ chiusura di fine sessione

    /// <summary>
    /// Orario HHMM in cui la posizione va chiusa, quando l'originale ha <c>ID = 0</c> e quindi
    /// abilita l'uscita di fine sessione. -1 = nessuna chiusura a tempo.
    ///
    /// <para>Nell'originale è un ordine emesso a runtime; qui diventa <c>CloseAtUtc</c> sul
    /// segnale d'ingresso, così l'engine la applica da solo e la stessa strategia si comporta
    /// identica in backtest e in <c>ExternalBroker</c>, dove i segnali di sola chiusura non
    /// verrebbero mai eseguiti.</para>
    /// </summary>
    protected int CloseAtTime = -1;

    // ------------------------------------------------------------------ estensione per sottoclassi

    /// <summary>
    /// Gate aggiuntivo comune ai due versi, valutato dopo finestra, tetto ingressi e pattern
    /// neutri. Serve alle varianti che aggiungono filtri propri (ADX, pattern extra).
    /// </summary>
    protected virtual bool PassesExtraGates(
        decimal[] ohlc, OhlcvData[] data, DateTime barTime) => true;

    /// <summary>Gate aggiuntivo specifico del verso.</summary>
    protected virtual bool PassesDirectionalExtraGates(
        SignalType side, decimal[] ohlc, OhlcvData[] data, DateTime barTime) => true;

    // ------------------------------------------------------------------ valutazione

    protected TradeSignal EvaluateCore(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        BuildSessionOhlc(data, barTime, out var ohlc);

        var (longLevel, shortLevel) = Trigger switch
        {
            TrendTrigger.PreviousSessionOhlc => (ohlc[5], ohlc[6]),   // highd1 / lowd1
            _ => (ohlc[1], ohlc[2])                                    // highd0 / lowd0
        };

        if (longLevel <= 0m || shortLevel <= 0m)
            return Hold(bar.Close, barTime, "Livelli di sessione non disponibili");

        if (!InWindow(barTime))
            return Hold(bar.Close, barTime);

        if (MaxTradesPerDay > 0 && EntriesTodayCount >= MaxTradesPerDay)
            return Hold(bar.Close, barTime, "Tetto ingressi giornalieri raggiunto");

        if (!EasyLib.PatternNeutralFast(NeutralYes, ohlc) ||
            EasyLib.PatternNeutralFast(NeutralNo, ohlc))
        {
            return Hold(bar.Close, barTime);
        }

        if (!PassesExtraGates(ohlc, data, barTime))
            return Hold(bar.Close, barTime);

        var entries = new List<TradeSignal>(2);

        if (EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc) &&
            EasyLib.PtnBaseSA2(BaseYesLong, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoLong, ohlc) &&
            EasyDayOfWeek(barTime) != NotEntryDayLong &&
            barTime.Month != NotEntryMonthLong &&
            PassesDirectionalExtraGates(SignalType.Buy, ohlc, data, barTime))
        {
            entries.Add(WithSessionClose(
                EntryStopNextBar(SignalType.Buy, longLevel, data, barTime, "LE"), barTime));
        }

        if (EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc) &&
            EasyLib.PtnBaseSA2(BaseYesShort, ohlc) &&
            !EasyLib.PtnBaseSA2(BaseNoShort, ohlc) &&
            EasyDayOfWeek(barTime) != NotEntryDayShort &&
            barTime.Month != NotEntryMonthShort &&
            PassesDirectionalExtraGates(SignalType.Sell, ohlc, data, barTime))
        {
            entries.Add(WithSessionClose(
                EntryStopNextBar(SignalType.Sell, shortLevel, data, barTime, "SE"), barTime));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private TradeSignal WithSessionClose(TradeSignal signal, DateTime barTime)
    {
        if (CloseAtTime >= 0)
            signal.CloseAtUtc = ResolveCloseAtUtc(barTime, CloseAtTime);
        return signal;
    }

    private bool InWindow(DateTime barTime) => InclusiveWindowEnd
        ? EasyLib.TimeWindowInclusive(StartTrade, EndTrade, barTime)
        : EasyLib.TimeWindow(StartTrade, EndTrade, barTime);
}
