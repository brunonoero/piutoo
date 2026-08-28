namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Cosa una strategia <b>vuole</b> tenere: la notte (oltre la fine della propria sessione) e il
/// fine settimana. E' una dichiarazione del motore, eventualmente sovrascritta dalla singola
/// strategia, e non decide nulla da sola.
///
/// <para><b>Perche' esiste, visto che l'uscita e' gia' nel segnale.</b> L'uscita di sessione viveva
/// solo come effetto: <c>IntradayOnly</c> valorizzava <c>CloseAtUtc</c> e finiva li'. Per sapere se
/// una strategia tiene overnight bisognava aprire il <c>.cs</c> — il catalogo non lo esponeva, il
/// masterfilter nemmeno, e il piano non aveva alcun modo di saperlo. Qui la stessa informazione
/// diventa un dato: entra nel catalogo, si vede in griglia, e permette al piano di dire in anticipo
/// quali strategie tagliera'. Il taglio vero resta meccanico (vince la deadline piu' stretta): questa
/// e' la dichiarazione, non l'esecuzione.</para>
/// </summary>
public sealed record StrategyHolding(bool Overnight, bool Overweek)
{
    /// <summary>Chiude entro la propria sessione: non tiene ne' la notte ne' il fine settimana.</summary>
    public static StrategyHolding Intraday { get; } = new(false, false);

    /// <summary>Puo' restare aperta oltre la sessione e oltre il fine settimana.</summary>
    public static StrategyHolding Multiday { get; } = new(true, true);

    /// <summary>
    /// Overweek senza overnight non significa niente: tenere il fine settimana e' un caso
    /// particolare di tenere oltre la sessione. Normalizza invece di lasciar circolare la coppia
    /// impossibile.
    /// </summary>
    public StrategyHolding Normalized() => Overnight ? this : Intraday;

    /// <summary>Etichetta breve per griglie e pannelli: "intraday", "overnight", "overnight+overweek".</summary>
    public string Describe() => (Overnight, Overweek) switch
    {
        (false, _) => "intraday",
        (true, false) => "overnight",
        (true, true) => "overnight+overweek"
    };
}

/// <summary>
/// Cosa il <b>conto</b> permette di tenere, e a che ora taglia quando non lo permette. Vive sul
/// piano di trading, scende nella sessione, esce nel descriptor e lo esegue il cBot; la stessa
/// policy va nella <see cref="Backtesting.BacktestingRequest"/>.
///
/// <para><b>La gerarchia.</b> Decide prima il piano: un conto prop che impone il flat di sessione o
/// di fine settimana taglia a prescindere da cosa la strategia vorrebbe. Solo se il piano
/// <i>permette</i> di tenere, la parola passa a motore e strategia — che possono comunque chiudere
/// prima, mai dopo. In una riga: <c>tiene = pianoPermette &amp;&amp; strategiaVuole</c>. Il permesso
/// non e' un obbligo: il piano non puo' forzare un overnight su una strategia intraday, perche' e'
/// la strategia a sapere quando la sua edge muore.</para>
///
/// <para><b>Perche' l'ora del taglio e' del piano e non della strategia.</b> Per la stessa ragione
/// per cui lo e' gia' il flat del fine settimana (vedi <see cref="WeekEndFlatPolicy"/>): la prop
/// dice "piatto alle 20:45 UTC", non "piatto alla fine della sessione del motore TF". Se ogni
/// strategia tagliasse alla propria fine sessione il conto non sarebbe piatto in nessun istante,
/// che e' esattamente cio' che il vincolo chiede di garantire.</para>
/// </summary>
public sealed record AccountHoldingPolicy
{
    /// <summary>
    /// Il conto puo' restare in posizione oltre la fine della sessione. Quando e' falso ogni
    /// posizione riceve una deadline a <see cref="SessionFlatUtcHhmm"/>, salvo che la strategia ne
    /// dichiari gia' una piu' stretta.
    /// </summary>
    public bool AllowOvernight { get; init; } = true;

    /// <summary>
    /// Il conto puo' attraversare il fine settimana. Quando e' falso vale la finestra di
    /// <see cref="WeekEnd"/>: niente posizioni e niente ordini fino alla riapertura.
    /// </summary>
    public bool AllowOverweek { get; init; }

    /// <summary>
    /// Ora UTC HHMM del flat giornaliero, usata solo quando <see cref="AllowOvernight"/> e' falso.
    /// Un numero solo per tutto il conto: vedi la nota di tipo.
    /// </summary>
    public int SessionFlatUtcHhmm { get; init; } = TradingConventions.SessionFlatFromUtcHhmm;

    /// <summary>Finestra di flat del fine settimana, usata quando <see cref="AllowOverweek"/> e' falso.</summary>
    public WeekEndFlatPolicy WeekEnd { get; init; } = WeekEndFlatPolicy.Default;

    /// <summary>
    /// Il comportamento storico del sistema, e quindi il default di ogni piano gia' scritto:
    /// overnight libero, fine settimana sempre piatto.
    /// </summary>
    public static AccountHoldingPolicy Default { get; } = new();

    /// <summary>Nessun vincolo di conto: serve ai run di parita' con il motore di ricerca.</summary>
    public static AccountHoldingPolicy Unrestricted { get; } = new()
    {
        AllowOvernight = true,
        AllowOverweek = true
    };

    /// <summary>
    /// Rifiuta la combinazione impossibile invece di risolverla in silenzio: permettere il fine
    /// settimana mentre si vieta la notte non descrive alcun conto reale, ed e' quasi sempre una
    /// spunta dimenticata.
    /// </summary>
    public void Validate()
    {
        if (AllowOverweek && !AllowOvernight)
            throw new InvalidOperationException(
                "Un piano non puo' permettere l'overweek vietando l'overnight: tenere il fine " +
                "settimana e' un caso particolare di tenere oltre la sessione.");

        if (!AllowOvernight && !WeekEndFlatPolicy.IsValidHhmm(SessionFlatUtcHhmm))
            throw new InvalidOperationException(
                $"Orario di flat di sessione non valido: {SessionFlatUtcHhmm}. Atteso HHMM UTC, es. 2045.");
    }

    /// <summary>
    /// La prima occorrenza di <see cref="SessionFlatUtcHhmm"/> <b>successiva</b> all'istante di
    /// riferimento, che e' la barra su cui l'ordine e' valido. Stessa convenzione di
    /// <c>EasyEngineBase.ResolveCloseAtUtc</c>, ma su orologio UTC puro: questo orario e' del conto,
    /// non della borsa, quindi non passa da <c>SessionClock</c> e non ha fuso da risolvere.
    /// </summary>
    public DateTime ResolveSessionFlatUtc(DateTime referenceUtc)
    {
        var day = referenceUtc.Date;
        var target = DateTime.SpecifyKind(
            day.AddHours(SessionFlatUtcHhmm / 100).AddMinutes(SessionFlatUtcHhmm % 100),
            DateTimeKind.Utc);
        return target <= referenceUtc ? target.AddDays(1) : target;
    }

    /// <summary>Etichetta compatta per pannelli e log: dice cosa il conto concede, non come e' scritto.</summary>
    public string Describe() => (AllowOvernight, AllowOverweek) switch
    {
        (false, _) => $"flat di sessione {Hhmm(SessionFlatUtcHhmm)} UTC",
        (true, false) => $"overnight, flat weekend ven {Hhmm(WeekEnd.FromUtcHhmm)} → dom {Hhmm(WeekEnd.UntilUtcHhmm)} UTC",
        (true, true) => "overnight e overweek liberi"
    };

    private static string Hhmm(int value) => $"{value / 100:00}:{value % 100:00}";
}

/// <summary>
/// Il punto unico che risolve la gerarchia piano → motore → strategia in una deadline.
///
/// <para>Il taglio e' meccanico: <b>vince la scadenza piu' stretta</b>. La strategia porta la
/// propria (<c>CloseAtUtc</c>, che puo' non esserci), il piano porta la sua quando vieta
/// l'overnight, e la posizione muore alla prima delle due. Il fine settimana non passa di qui: e'
/// una finestra e non una deadline, e lo applicano il loop di backtest e il cBot con
/// <see cref="WeekEndFlatPolicy.IsFlatTrigger"/>.</para>
///
/// <para>Sta in <c>Piootoo.Shared</c> e non in un servizio perche' lo chiamano due motori diversi —
/// il backtest interno e la sessione che costruisce gli intent per il cBot — e una regola di
/// composizione implementata due volte e' una regola che prima o poi diverge. E' esattamente
/// l'errore gia' pagato sull'orario del flat del venerdi'.</para>
/// </summary>
public static class HoldingResolver
{
    /// <summary>La deadline effettiva di una posizione e chi l'ha imposta.</summary>
    public readonly record struct TimeExitDecision(DateTime? AtUtc, bool FromAccountPolicy);

    /// <summary>
    /// Compone la deadline della strategia con quella del piano.
    /// </summary>
    /// <param name="strategyCloseAtUtc">Deadline dichiarata dalla strategia sul segnale; <c>null</c> se non ne ha.</param>
    /// <param name="referenceUtc">Barra su cui l'ordine e' valido: e' da li' che si misura il prossimo flat.</param>
    /// <param name="policy">Cosa il conto permette.</param>
    public static TimeExitDecision Resolve(
        DateTime? strategyCloseAtUtc, DateTime referenceUtc, AccountHoldingPolicy policy)
    {
        if (policy.AllowOvernight)
            return new TimeExitDecision(strategyCloseAtUtc, false);

        var accountFlat = policy.ResolveSessionFlatUtc(referenceUtc);
        return strategyCloseAtUtc.HasValue && strategyCloseAtUtc.Value <= accountFlat
            ? new TimeExitDecision(strategyCloseAtUtc, false)
            : new TimeExitDecision(accountFlat, true);
    }

    /// <summary>
    /// Le strategie del masterfilter che il piano taglierebbe, con il motivo. Alimenta l'avviso del
    /// dettaglio piano: una divergenza fra quel che la strategia vuole e quel che il conto concede
    /// va mostrata <b>prima</b> di aprire la sessione, non spiegata dopo guardando i trade.
    /// </summary>
    public static IReadOnlyList<HoldingConflict> FindConflicts(
        IEnumerable<(string StrategyId, string StrategyCode, StrategyHolding Holding)> strategies,
        AccountHoldingPolicy policy)
    {
        var conflicts = new List<HoldingConflict>();
        foreach (var (id, code, holding) in strategies)
        {
            var cutOvernight = holding.Overnight && !policy.AllowOvernight;
            var cutOverweek = holding.Overweek && !policy.AllowOverweek && !cutOvernight;
            if (cutOvernight || cutOverweek)
                conflicts.Add(new HoldingConflict(id, code, holding, cutOvernight, cutOverweek));
        }
        return conflicts;
    }
}

/// <summary>Una strategia multiday incontrata da un piano che non le concede di esserlo.</summary>
public sealed record HoldingConflict(
    string StrategyId,
    string StrategyCode,
    StrategyHolding Holding,
    bool CutAtSessionFlat,
    bool CutAtWeekEnd);
