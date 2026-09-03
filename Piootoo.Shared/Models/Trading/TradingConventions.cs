namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Costanti di convenzione condivise fra backtest interno, sessioni e client.
/// </summary>
public static class TradingConventions
{
    /// <summary>
    /// Capitale rispetto al quale le strategie dichiarano le proprie quantità: un segnale da un
    /// contratto vale un contratto su un conto da un milione.
    ///
    /// <para>Serve a due posti che devono restare d'accordo: è il capitale iniziale proposto dal
    /// backtest interno (dove la size resta quella dichiarata dalla strategia, cioè 1) ed è il
    /// denominatore di <c>AccountSymbolConversion.BalanceScale</c>, che nelle sessioni riporta
    /// quella size al saldo reale del conto. Due letterali separati si sarebbero disallineati senza
    /// produrre alcun errore: solo percentuali e scale che non parlano della stessa cosa.</para>
    /// </summary>
    public const decimal StrategyReferenceBalance = 1_000_000m;

    /// <summary>
    /// Venerdi', ora UTC in formato HHMM da cui il conto deve essere piatto: niente posizioni e
    /// niente ordini fino alla riapertura.
    ///
    /// <para>Un'ora fissa invece della chiusura CME reale (16:00 di Chicago, cioe' 21:00 o 22:00
    /// UTC secondo l'ora legale americana): un valore prudente prima della piu' presta delle due
    /// vale in entrambi i periodi dell'anno senza gestire il fuso. E' lo stesso default che il cBot
    /// aveva gia' come proprio parametro.</para>
    /// </summary>
    public const int WeekEndFlatFromUtcHhmm = 2045;

    /// <summary>Domenica, ora UTC HHMM da cui si torna operativi.</summary>
    public const int WeekEndFlatUntilUtcHhmm = 2300;

    /// <summary>
    /// Ora UTC HHMM del flat giornaliero, usata dai piani che vietano l'overnight (conti prop che
    /// impongono di chiudere ogni sera). Vale lo stesso default del venerdi': se un conto deve
    /// essere piatto una volta al giorno, l'ora piu' prudente e' quella che gia' vale per il
    /// fine settimana. Vedi <see cref="AccountHoldingPolicy"/>.
    /// </summary>
    public const int SessionFlatFromUtcHhmm = 2045;
}

/// <summary>
/// La finestra di flat del fine settimana: da venerdi' all'ora dichiarata fino alla domenica
/// all'ora di riapertura, sabato sempre dentro.
///
/// <para><b>Perche' e' un tipo condiviso e non un parametro per motore.</b> Fino al 26/08/2026 la
/// regola viveva in due posti che non si parlavano: il cBot con il proprio parametro a 20:45 UTC,
/// e il backtest che non aveva nessun orario e chiudeva sull'<i>ultimo slot dell'orologio
/// sintetico</i> prima di sabato — venerdi' 23:30 con timeframe minimo a 30 minuti, tutto l'anno.
/// Due ore e tre quarti di venerdi' che il backtest teneva e il conto vero no, su quasi meta' dei
/// trade del confronto. Una regola che uno solo dei due motori conosce e' una regola che garantisce
/// divergenza: qui il numero e' uno, e chi lo consuma e' backtest, sessione e client.</para>
/// </summary>
public sealed record WeekEndFlatPolicy(int FromUtcHhmm, int UntilUtcHhmm)
{
    public static WeekEndFlatPolicy Default { get; } = new(
        TradingConventions.WeekEndFlatFromUtcHhmm,
        TradingConventions.WeekEndFlatUntilUtcHhmm);

    /// <summary>Vero quando l'istante indicato cade nella finestra di flat.</summary>
    public bool IsInsideWindow(DateTime instantUtc)
    {
        var hhmm = instantUtc.Hour * 100 + instantUtc.Minute;
        return instantUtc.DayOfWeek switch
        {
            DayOfWeek.Friday => hhmm >= FromUtcHhmm,
            DayOfWeek.Saturday => true,
            DayOfWeek.Sunday => hhmm < UntilUtcHhmm,
            _ => false
        };
    }

    /// <summary>
    /// Vero sulla barra in cui il flat SCATTA: dentro la finestra adesso, fuori un istante prima.
    ///
    /// <para>Serve al backtest, che deve chiudere una volta sola e non a ogni barra del fine
    /// settimana. Il confine si misura sul tick precedente e non sul calendario, cosi' vale anche
    /// quando il venerdi' non ha una barra esattamente all'ora dichiarata: chiude la prima barra
    /// utile dopo, che e' quanto di piu' vicino il feed consenta.</para>
    /// </summary>
    public bool IsFlatTrigger(DateTime instantUtc, DateTime previousInstantUtc) =>
        IsInsideWindow(instantUtc) && !IsInsideWindow(previousInstantUtc);

    /// <summary>
    /// Il primo istante da <paramref name="referenceUtc"/> in poi in cui il flat del fine settimana
    /// e' in vigore: il venerdi' a <see cref="FromUtcHhmm"/>, oppure <paramref name="referenceUtc"/>
    /// stesso se la finestra e' gia' aperta.
    ///
    /// <para>Serve a <see cref="HoldingResolver"/> per trasformare la finestra in una <b>deadline</b>
    /// da mettere sul segnale. La finestra resta quello che e' — un intervallo in cui non si sta a
    /// mercato — ma un ordine che nasce prima deve gia' sapere quando morira', altrimenti la
    /// chiusura dipende da chi la applica: il loop di backtest a modo suo, il cBot al proprio.</para>
    /// </summary>
    public DateTime ResolveNextFlatUtc(DateTime referenceUtc)
    {
        if (IsInsideWindow(referenceUtc)) return referenceUtc;

        var daysToFriday = ((int)DayOfWeek.Friday - (int)referenceUtc.DayOfWeek + 7) % 7;
        var day = referenceUtc.Date.AddDays(daysToFriday);
        var target = DateTime.SpecifyKind(
            day.AddHours(FromUtcHhmm / 100).AddMinutes(FromUtcHhmm % 100), DateTimeKind.Utc);

        // Stessa convenzione di AccountHoldingPolicy.ResolveSessionFlatUtc: la deadline e'
        // strettamente successiva all'istante di riferimento, mai coincidente.
        return target <= referenceUtc ? target.AddDays(7) : target;
    }

    /// <summary>Un HHMM plausibile come orario di flat: 0000-2359, minuti veri.</summary>
    public static bool IsValidHhmm(int value) =>
        value >= 0 && value <= 2359 && value % 100 < 60;
}
