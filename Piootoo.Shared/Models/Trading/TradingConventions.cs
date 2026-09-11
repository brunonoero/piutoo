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
    /// Venerdi', ora UTC da cui il conto deve essere piatto: niente posizioni e niente ordini fino
    /// alla riapertura.
    ///
    /// <para>Un'ora fissa invece della chiusura CME reale (16:00 di Chicago, cioe' 21:00 o 22:00
    /// UTC secondo l'ora legale americana): un valore prudente prima della piu' presta delle due
    /// vale in entrambi i periodi dell'anno senza gestire il fuso. E' lo stesso default che il cBot
    /// aveva gia' come proprio parametro.</para>
    /// </summary>
    public static readonly TimeOnly WeekEndFlatFromUtc = new(20, 45);

    /// <summary>Domenica, ora UTC da cui si torna operativi.</summary>
    public static readonly TimeOnly WeekEndFlatUntilUtc = new(23, 0);

    /// <summary>
    /// Ora UTC del flat giornaliero, usata dai piani che vietano l'overnight (conti prop che
    /// impongono di chiudere ogni sera). Vale lo stesso default del venerdi': se un conto deve
    /// essere piatto una volta al giorno, l'ora piu' prudente e' quella che gia' vale per il
    /// fine settimana. Vedi <see cref="AccountHoldingPolicy"/>.
    /// </summary>
    public static readonly TimeOnly SessionFlatFromUtc = new(20, 45);
}

/// <summary>
/// La finestra di flat del fine settimana: da venerdi' all'ora dichiarata fino alla domenica
/// all'ora di riapertura, sabato sempre dentro. Gli orari sono UTC: sono del conto, non di una
/// borsa, e non hanno fuso da risolvere.
///
/// <para><b>Perche' e' un tipo condiviso e non un parametro per motore.</b> Fino al 26/08/2026 la
/// regola viveva in due posti che non si parlavano: il cBot con il proprio parametro a 20:45 UTC,
/// e il backtest che non aveva nessun orario e chiudeva sull'<i>ultimo slot dell'orologio
/// sintetico</i> prima di sabato — venerdi' 23:30 con timeframe minimo a 30 minuti, tutto l'anno.
/// Due ore e tre quarti di venerdi' che il backtest teneva e il conto vero no, su quasi meta' dei
/// trade del confronto. Una regola che uno solo dei due motori conosce e' una regola che garantisce
/// divergenza: qui il numero e' uno, e chi lo consuma e' backtest, sessione e client.</para>
///
/// <para><b>Perche' non e' un record posizionale.</b> Un <c>plans.json</c> o un
/// <c>session-state.json</c> scritto prima di questo campo non lo contiene, e un parametro di
/// costruttore mancante varrebbe mezzanotte: un flat alle 00:00 sarebbe un difetto silenzioso. Le
/// proprieta' con default riproducono il comportamento storico quando il file tace.</para>
/// </summary>
public sealed record WeekEndFlatPolicy
{
    public WeekEndFlatPolicy()
    {
    }

    public WeekEndFlatPolicy(TimeOnly fromUtc, TimeOnly untilUtc)
    {
        FromUtc = fromUtc;
        UntilUtc = untilUtc;
    }

    /// <summary>Venerdi', ora UTC da cui il conto e' piatto.</summary>
    public TimeOnly FromUtc { get; init; } = TradingConventions.WeekEndFlatFromUtc;

    /// <summary>Domenica, ora UTC da cui si torna operativi.</summary>
    public TimeOnly UntilUtc { get; init; } = TradingConventions.WeekEndFlatUntilUtc;

    public static WeekEndFlatPolicy Default { get; } = new();

    /// <summary>Vero quando l'istante indicato cade nella finestra di flat.</summary>
    public bool IsInsideWindow(DateTime instantUtc)
    {
        var time = TimeOnly.FromDateTime(instantUtc);
        return instantUtc.DayOfWeek switch
        {
            DayOfWeek.Friday => time >= FromUtc,
            DayOfWeek.Saturday => true,
            DayOfWeek.Sunday => time < UntilUtc,
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
    /// e' in vigore: il venerdi' a <see cref="FromUtc"/>, oppure <paramref name="referenceUtc"/>
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
        var target = DateTime.SpecifyKind(day.Add(FromUtc.ToTimeSpan()), DateTimeKind.Utc);

        // Stessa convenzione di AccountHoldingPolicy.ResolveSessionFlatUtc: la deadline e'
        // strettamente successiva all'istante di riferimento, mai coincidente.
        return target <= referenceUtc ? target.AddDays(7) : target;
    }

    /// <summary>La finestra come si legge nei pannelli: <c>ven 20:45 → dom 23:00 UTC</c>.</summary>
    public string Describe() => $"ven {FromUtc:HH\\:mm} → dom {UntilUtc:HH\\:mm} UTC";
}
