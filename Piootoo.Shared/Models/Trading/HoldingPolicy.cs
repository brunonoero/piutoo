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
    /// posizione riceve una deadline a <see cref="SessionFlatUtc"/>, salvo che la strategia ne
    /// dichiari gia' una piu' stretta.
    /// </summary>
    public bool AllowOvernight { get; init; } = true;

    /// <summary>
    /// Il conto puo' attraversare il fine settimana. Quando e' falso vale la finestra di
    /// <see cref="WeekEnd"/>: niente posizioni e niente ordini fino alla riapertura.
    /// </summary>
    public bool AllowOverweek { get; init; }

    /// <summary>
    /// Ora UTC del flat giornaliero, usata solo quando <see cref="AllowOvernight"/> e' falso.
    /// Un orario solo per tutto il conto: vedi la nota di tipo. E' UTC perche' e' del conto, non
    /// di una borsa: non passa da <c>SessionClock</c> e non ha fuso da risolvere.
    /// </summary>
    public TimeOnly SessionFlatUtc { get; init; } = TradingConventions.SessionFlatFromUtc;

    /// <summary>
    /// Durata in minuti della finestra di flat giornaliero che parte da <see cref="SessionFlatUtc"/>.
    /// Dentro la finestra il conto e' piatto e <b>non nascono ingressi</b>: e' la stessa forma della
    /// regola del fine settimana, applicata a tutti i giorni.
    ///
    /// <para><b>Perche' una finestra e non un istante.</b> Con il solo istante, un ordine valido fra il
    /// flat e il rollover del broker — la barra delle 20:45 di una strategia a 15 minuti — riceveva la
    /// deadline del giorno <i>dopo</i> e attraversava proprio il rollover che il flat esiste per
    /// evitare. Il fine della finestra va messo <b>oltre il rollover</b>: 30 minuti dalle 20:45 sono
    /// le 21:15, contro un rollover alle 20:59 (FTMO) o 21:00 (ICS).</para>
    ///
    /// <para><b>Perche' una durata e non un'ora di fine.</b> Un orario di fine puo' cadere prima
    /// dell'inizio — un <c>plans.json</c> con il flat alle 21:30 e il default di fine alle 21:15 —
    /// e la finestra si rovescerebbe in silenzio; una durata non puo'. La regola e' una sola:
    /// <c>[SessionFlatUtc, SessionFlatUtc + minuti)</c>, anche a cavallo della mezzanotte.</para>
    /// </summary>
    public int SessionFlatWindowMinutes { get; init; } = TradingConventions.SessionFlatWindowMinutes;

    /// <summary>Fine della finestra di flat giornaliero, come ora del giorno UTC. Derivata, non si salva.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public TimeOnly SessionFlatUntilUtc => SessionFlatUtc.AddMinutes(SessionFlatWindowMinutes);

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

        // Una finestra vuota o di mezza giornata non descrive alcun conto: la prima riaprirebbe
        // in silenzio il buco fra flat e rollover, la seconda terrebbe il conto fermo per ore.
        if (SessionFlatWindowMinutes is < 1 or > 720)
            throw new InvalidOperationException(
                $"La finestra del flat di sessione deve durare fra 1 e 720 minuti: " +
                $"ricevuti {SessionFlatWindowMinutes}.");
    }

    /// <summary>
    /// Vero quando l'istante cade nella finestra di flat giornaliero
    /// <c>[SessionFlatUtc, SessionFlatUtc + SessionFlatWindowMinutes)</c>. La finestra puo' passare
    /// la mezzanotte, quindi la distanza dall'inizio si misura modulo un giorno.
    /// </summary>
    public bool IsInsideSessionFlatWindow(DateTime instantUtc)
    {
        var minutesSinceFlat = (int)((instantUtc.TimeOfDay - SessionFlatUtc.ToTimeSpan()).TotalMinutes + 1440) % 1440;
        return minutesSinceFlat < SessionFlatWindowMinutes;
    }

    /// <summary>
    /// Vero sul tick in cui il flat giornaliero <b>scatta</b>: l'ora del flat cade dopo il tick
    /// precedente e non oltre questo. Serve al backtest e alla sweep per cancellare i pending una
    /// volta sola, sulla prima barra utile, come <see cref="WeekEndFlatPolicy.IsFlatTrigger"/>.
    ///
    /// <para>Si misura l'<i>attraversamento</i> dell'ora e non "dentro adesso, fuori prima" perche'
    /// la finestra dura mezz'ora e l'orologio del loop puo' essere piu' largo: con un tick a quattro
    /// ore nessun tick cadrebbe dentro e il flat non scatterebbe mai. Presuppone due tick a meno di
    /// un giorno di distanza, che e' vero per ogni orologio del sistema.</para>
    /// </summary>
    public bool IsSessionFlatTrigger(DateTime instantUtc, DateTime previousInstantUtc)
    {
        var flat = DateTime.SpecifyKind(instantUtc.Date.Add(SessionFlatUtc.ToTimeSpan()), DateTimeKind.Utc);
        if (flat > instantUtc) flat = flat.AddDays(-1);
        return flat > previousInstantUtc;
    }

    /// <summary>
    /// Vero se l'ora di rollover del broker cade <b>dentro</b> la finestra di flat: e' la condizione
    /// perche' il flat eviti davvero il finanziamento. Un rollover prima dell'inizio della finestra
    /// viene pagato da ogni posizione ancora aperta; uno alla fine o dopo puo' essere attraversato da
    /// un ingresso nato appena la finestra si chiude.
    /// </summary>
    public bool SessionFlatWindowCoversRollover(TimeOnly rolloverUtc)
    {
        var minutesSinceFlat = (int)((rolloverUtc.ToTimeSpan() - SessionFlatUtc.ToTimeSpan()).TotalMinutes + 1440) % 1440;
        return minutesSinceFlat < SessionFlatWindowMinutes;
    }

    /// <summary>
    /// Il primo istante da <paramref name="referenceUtc"/> in poi in cui il flat giornaliero e' in
    /// vigore: <see cref="SessionFlatUtc"/> stesso se la finestra e' aperta, altrimenti la prossima
    /// occorrenza <b>successiva</b> all'istante di riferimento, che e' la barra su cui l'ordine e'
    /// valido. Stessa convenzione di <c>WeekEndFlatPolicy.ResolveNextFlatUtc</c>, su orologio UTC
    /// puro: questo orario e' del conto, non della borsa, quindi non passa da <c>SessionClock</c> e
    /// non ha fuso da risolvere.
    /// </summary>
    public DateTime ResolveSessionFlatUtc(DateTime referenceUtc)
    {
        if (IsInsideSessionFlatWindow(referenceUtc)) return referenceUtc;

        var target = DateTime.SpecifyKind(referenceUtc.Date.Add(SessionFlatUtc.ToTimeSpan()), DateTimeKind.Utc);
        return target <= referenceUtc ? target.AddDays(1) : target;
    }

    /// <summary>La finestra giornaliera come si legge nei pannelli: <c>20:45 → 21:15 UTC</c>.</summary>
    public string DescribeSessionFlat() => $"{SessionFlatUtc:HH\\:mm} → {SessionFlatUntilUtc:HH\\:mm} UTC";

    /// <summary>Etichetta compatta per pannelli e log: dice cosa il conto concede, non come e' scritto.</summary>
    public string Describe() => (AllowOvernight, AllowOverweek) switch
    {
        (false, _) => $"flat di sessione {DescribeSessionFlat()}",
        (true, false) => $"overnight, flat weekend {WeekEnd.Describe()}",
        (true, true) => "overnight e overweek liberi"
    };
}

/// <summary>
/// Il punto unico che risolve la gerarchia piano → motore → strategia in una deadline.
///
/// <para>Il taglio e' meccanico: <b>vince la scadenza piu' stretta</b>. La strategia porta la
/// propria (<c>CloseAtUtc</c>, che puo' non esserci) e il piano porta le sue — il flat di sessione
/// quando vieta l'overnight, l'apertura della finestra del fine settimana quando vieta l'overweek —
/// e la posizione muore alla prima di tutte.</para>
///
/// <para><b>Il permesso attivo non impone niente.</b> Con <c>AllowOvernight</c> e
/// <c>AllowOverweek</c> entrambi veri il segnale esce con la sola uscita dichiarata dalla strategia:
/// il piano concede, non obbliga. E' il verso opposto — il permesso <i>mancante</i> — a scrivere una
/// deadline sul segnale.</para>
///
/// <para><b>Perche' il fine settimana e' anche una deadline.</b> Restava solo una finestra, applicata
/// dal loop di backtest e dal cBot con <see cref="WeekEndFlatPolicy.IsFlatTrigger"/>: due
/// implementazioni della stessa regola, in due processi diversi, su due orologi diversi. Portandola
/// sul segnale come <c>CloseAtUtc</c> l'istante e' deciso una volta sola da chi conosce il piano — e
/// viaggia con l'ordine, quindi lo stesso intent muore nello stesso momento ovunque venga eseguito.
/// La finestra resta comunque in vigore in entrambi i motori come rete di sicurezza: deve reggere
/// anche su una posizione che il server non ha mai visto nascere.</para>
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
        var accountDeadline = ResolveAccountDeadline(referenceUtc, policy);

        // Il piano non vieta niente: vale quello che la strategia ha dichiarato, anche se e' nulla.
        if (accountDeadline is not { } deadline)
            return new TimeExitDecision(strategyCloseAtUtc, false);

        return strategyCloseAtUtc.HasValue && strategyCloseAtUtc.Value <= deadline
            ? new TimeExitDecision(strategyCloseAtUtc, false)
            : new TimeExitDecision(deadline, true);
    }

    /// <summary>
    /// Vero se un ingresso valido da <paramref name="referenceUtc"/> nasce <b>dentro</b> una finestra
    /// di flat del conto e quindi non deve nascere affatto: il flat giornaliero quando il piano vieta
    /// l'overnight, quello del fine settimana quando vieta l'overweek.
    ///
    /// <para><b>Perche' scartare e non datare.</b> Un ordine valido fra il flat e il rollover riceveva
    /// da <see cref="Resolve"/> la deadline del giorno dopo — la prossima occorrenza del flat — e
    /// viveva una notte intera attraversando proprio il rollover che il flat esiste per evitare. La
    /// finestra chiude il buco alla sorgente, nello stesso punto per backtest, sweep e sessione; il
    /// cBot ripete il controllo come ultima barriera, com'e' gia' per il fine settimana. Le uscite
    /// (<c>ExitOnly</c>) non passano di qui: ridurre il rischio e' sempre permesso.</para>
    /// </summary>
    public static bool BlocksEntry(DateTime referenceUtc, AccountHoldingPolicy policy) =>
        (!policy.AllowOvernight && policy.IsInsideSessionFlatWindow(referenceUtc)) ||
        (!policy.AllowOverweek && policy.WeekEnd.IsInsideWindow(referenceUtc));

    /// <summary>
    /// I simboli il cui rollover cade <b>fuori</b> dalla finestra di flat del piano: per ciascuno il
    /// flat non evita il finanziamento, e il run va corretto o letto sapendolo. Vuoto quando il piano
    /// permette l'overnight, perche' li' non promette niente.
    /// </summary>
    public static IReadOnlyList<string> RolloversOutsideSessionFlatWindow(
        AccountHoldingPolicy policy, IEnumerable<SwapSpec> swaps)
    {
        if (policy.AllowOvernight) return [];

        return swaps
            .Where(swap => !policy.SessionFlatWindowCoversRollover(swap.RolloverUtc))
            .Select(swap => $"{swap.Symbol} rollover {swap.RolloverUtc:HH\\:mm} UTC")
            .ToList();
    }

    /// <summary>
    /// La scadenza imposta dal <b>conto</b>, cioe' la piu' stretta fra i divieti che il piano
    /// dichiara. Null quando il piano concede tutto: li' non c'e' nessuna deadline di conto e la
    /// parola resta alla strategia.
    /// </summary>
    private static DateTime? ResolveAccountDeadline(DateTime referenceUtc, AccountHoldingPolicy policy)
    {
        DateTime? deadline = policy.AllowOvernight ? null : policy.ResolveSessionFlatUtc(referenceUtc);

        if (!policy.AllowOverweek)
        {
            var weekEnd = policy.WeekEnd.ResolveNextFlatUtc(referenceUtc);
            if (deadline is not { } current || weekEnd < current)
                deadline = weekEnd;
        }

        return deadline;
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
