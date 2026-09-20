namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Un parametro della ricerca: le chiavi sono quelle che la classe accetta in <c>Initialize</c>,
/// i valori la griglia del motore di ricerca.
/// </summary>
/// <param name="Key">Chiave di <c>Initialize</c>.</param>
/// <param name="Values">La griglia. Il <b>primo</b> valore e' il default della sweep, come in Python.</param>
/// <param name="Categorical">
/// Vero per id e scelte discrete, dove "valore adiacente" non significa "strategia simile": pattern,
/// direzione, giorni, sorgenti. Su questi non si fa plateau analysis — due pattern con id vicini non
/// hanno niente in comune, e smussare il punteggio coi vicini mescolerebbe cose scorrelate.
/// </param>
/// <param name="OffSentinel">
/// Il valore "spento" di un parametro ordinale che ce l'ha (<c>-1</c> per gli orari, <c>0</c> per
/// target, trailing, breakeven, max_bars). Il salto fra spento e primo valore attivo <b>non</b> e'
/// un vicinato di griglia: sono strategie molto diverse, e il plateau non lo attraversa mai.
/// </param>
public sealed record SweepParameter(
    string Key,
    IReadOnlyList<object> Values,
    bool Categorical = false,
    object? OffSentinel = null)
{
    /// <summary>Il default della sweep: il primo valore della griglia.</summary>
    public object Default => Values[0];

    /// <summary>
    /// I valori nell'ordine <b>naturale</b>, che e' quello in cui due valori sono "vicini".
    ///
    /// <para><b>Non e' l'ordine di dichiarazione.</b> Nelle griglie della ricerca il primo valore e'
    /// il default della sweep, non il minimo: <c>GRID_STOP_LOSS</c> comincia con 1500 e prosegue da
    /// 250. Cercare i vicini nell'ordine di dichiarazione rende 250 l'adiacente di 1500, e la
    /// plateau analysis finisce per mediare il punteggio di due strategie che non hanno niente in
    /// comune — cioe' fa il contrario di quello per cui esiste.</para>
    ///
    /// <para>I valori non confrontabili restano nell'ordine dichiarato: per un parametro categorico
    /// il vicinato non si usa comunque.</para>
    /// </summary>
    public IReadOnlyList<object> OrderedValues => _ordered ??= Order();

    private IReadOnlyList<object>? _ordered;

    private IReadOnlyList<object> Order()
    {
        if (Categorical || Values.Any(value => value is not IComparable))
            return Values;

        return Values.OrderBy(value => value, Comparer<object>.Create((left, right) =>
            ((IComparable)left).CompareTo(right))).ToArray();
    }
}

/// <summary>
/// Una fase dell'ottimizzazione sequenziale: il gruppo di parametri che si ottimizzano insieme
/// mentre tutti gli altri restano fermi al seme della fase precedente.
/// </summary>
/// <param name="RequiresAccurateClock">
/// La fase deve girare con l'orologio fitto invece che con quello veloce.
///
/// <para><b>Non e' una preferenza, e' un vincolo misurato.</b> Senza il feed da un minuto lo stop
/// viene valutato sulla chiusura della barra della strategia invece che dentro, e la barra che lo
/// avrebbe colpito per poi recuperare non lo colpisce affatto. L'errore ha un verso: piu' lo stop e'
/// stretto, piu' il percorso veloce e' ottimista. Misurato su <c>PT2_NQ_PCH_001_240</c>, feed interno
/// 2022-2025, con lo stesso identico set di parametri a parte lo stop: a $1.000 il veloce dichiara
/// $733.145 contro $186.543 (+$546.602), a $2.500 +$244.033, a $4.595 −$11.641. Una fase di risk
/// management girata sul percorso veloce sceglierebbe <i>sempre</i> lo stop piu' stretto della
/// griglia. Vedi <c>SweepRunnerParityTests.FastClockOverstatesTightStops</c>.</para>
/// </param>
public sealed record SweepPhase(
    string Name,
    IReadOnlyList<string> Keys,
    bool RequiresAccurateClock = false);

/// <summary>
/// Lo spazio di ricerca di un motore: griglie, fasi e sentinelle dei pattern.
///
/// <para><b>E' la traduzione di <c>get_param_space()</c> e <c>get_optimization_phases()</c></b> del
/// motore Python corrispondente, ristretta alle chiavi che le classi espongono da <c>Initialize</c>.
/// L'ordine delle fasi non e' un dettaglio di implementazione ma il metodo: prima il trigger, poi i
/// filtri, poi gli orari, e il risk management per ultimo. Il vincolo che il metodo dichiara — mai
/// pattern e stop/target nella stessa fase — e' rispettato dalla suddivisione, non da un controllo.</para>
/// </summary>
public sealed class SweepSpace
{
    public SweepSpace(
        string engine,
        IReadOnlyList<SweepParameter> parameters,
        IReadOnlyList<SweepPhase> phases,
        IReadOnlyDictionary<string, object>? defaultOverrides = null,
        IReadOnlyDictionary<string, object>? patternSentinels = null)
    {
        Engine = engine;
        Parameters = parameters;
        Phases = phases;
        PatternSentinels = patternSentinels ?? new Dictionary<string, object>();
        ByKey = parameters.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        var defaults = parameters.ToDictionary(p => p.Key, p => p.Default, StringComparer.OrdinalIgnoreCase);
        if (defaultOverrides is not null)
        {
            foreach (var (key, value) in defaultOverrides)
                defaults[key] = value;
        }

        Defaults = defaults;

        var unknown = phases.SelectMany(phase => phase.Keys)
            .Where(key => !ByKey.ContainsKey(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknown.Count > 0)
        {
            throw new ArgumentException(
                $"{engine}: le fasi nominano parametri che lo spazio non dichiara: {string.Join(", ", unknown)}.");
        }
    }

    public string Engine { get; }
    public IReadOnlyList<SweepParameter> Parameters { get; }
    public IReadOnlyList<SweepPhase> Phases { get; }
    public IReadOnlyDictionary<string, SweepParameter> ByKey { get; }

    /// <summary>
    /// Il punto di partenza della sweep: il primo valore di ogni griglia, salvo gli scostamenti
    /// dichiarati dal motore. Non e' una formalita': su intraday una base senza stop ne' target fa
    /// vincere la sentinella a ogni fase, ed e' il motivo per cui <c>get_default_params()</c> in
    /// Python li impone diversi dal primo valore.
    /// </summary>
    public IReadOnlyDictionary<string, object> Defaults { get; }

    /// <summary>
    /// Il valore "nessun filtro" dei parametri pattern. Serve all'ablation finale: un pattern si
    /// tiene solo se batte la propria sentinella a parita' di tutto il resto.
    /// </summary>
    public IReadOnlyDictionary<string, object> PatternSentinels { get; }

    /// <summary>Quante combinazioni ha una fase. Serve a sapere cosa si sta per lanciare.</summary>
    public long CombinationCount(SweepPhase phase) =>
        phase.Keys.Aggregate(1L, (count, key) => count * ByKey[key].Values.Count);

    /// <summary>
    /// Spezza le fasi che ottimizzano <b>due pattern insieme</b> — quello richiesto e quello
    /// vietato — in due fasi consecutive da un pattern ciascuna.
    ///
    /// <para><b>Che cosa si guadagna.</b> Il prodotto diventa una somma: le due fasi pattern del
    /// BIAS settimanale passano da 23.256 combinazioni ciascuna (153 × 152) a 153 e 152, cioe' da
    /// nove ore a pochi minuti su una cella a un'ora di barre. Sul Price Channel i direzionali
    /// passano da 10.609 a 103 + 103.</para>
    ///
    /// <para><b>Che cosa si perde, e va detto.</b> L'interazione fra i due: un pattern richiesto che
    /// rende solo insieme a un certo divieto non viene piu' trovato, perche' quando si sceglie il
    /// primo il secondo e' ancora alla sentinella. Il motore di ricerca Python li ottimizza
    /// <i>insieme</i>, quindi questa e' una <b>deviazione</b> dal metodo, non una sua variante
    /// equivalente: si adotta quando il prodotto completo non e' eseguibile nel tempo disponibile, e
    /// il resoconto del run deve dichiararlo. Il beam K ≥ 2 ne recupera una parte, come per le
    /// interazioni fra fasi.</para>
    /// </summary>
    public SweepSpace SplitPatternPhases()
    {
        var phases = new List<SweepPhase>(Phases.Count + 2);
        foreach (var phase in Phases)
        {
            var patternKeys = phase.Keys
                .Where(key => PatternSentinels.ContainsKey(key))
                .ToArray();

            if (patternKeys.Length < 2 || patternKeys.Length != phase.Keys.Count)
            {
                phases.Add(phase);
                continue;
            }

            foreach (var key in patternKeys)
                phases.Add(phase with { Name = $"{phase.Name}: {key}", Keys = [key] });
        }

        return new SweepSpace(Engine, Parameters, phases, Defaults, PatternSentinels);
    }
}

/// <summary>
/// Gli spazi di ricerca dei motori portati, con le griglie dei rispettivi file Python.
///
/// <para>I numeri sono riportati <b>verbatim</b> da <c>easy_engine_py/</c>, commenti storici
/// compresi dove spiegano perche' una griglia e' cosi': sono il risultato di correzioni misurate
/// (l'audit del 11/07/2026 che ha aggiunto 4500 e 6000 al target, il passo orario a 1h dopo che le
/// TOP_UA cadevano su ore che la griglia rada non poteva raggiungere), e riscriverli "meglio"
/// significa cercare in uno spazio diverso da quello in cui le strategie sono state trovate.</para>
/// </summary>
public static class SweepSpaces
{
    // base.py: orari a passo 1h, -1 = nessun limite da quel lato.
    private static readonly object[] Hours = Enumerable.Range(-1, 25).Cast<object>().ToArray();

    // base.py GRID_STOP_LOSS / GRID_TAKE_PROFIT. Primo valore = default della sweep.
    private static readonly object[] StopLossGrid =
        [1500, 250, 500, 750, 1000, 1250, 1750, 2000, 2250, 2500, 3000, 4000, 5000];

    private static readonly object[] TakeProfitGrid =
        [0, 500, 1000, 1500, 2000, 2500, 3000, 4000, 4500, 5000, 6000, 7500, 10000];

    /// <summary>base.py <c>neutral_values</c>: sweep completa della libreria pattern_neutral (1..54).</summary>
    private static object[] Neutral(int sentinel) =>
        new object[] { sentinel }.Concat(Enumerable.Range(1, 54).Cast<object>()).ToArray();

    /// <summary>
    /// base.py <c>mirrored_dir_values</c>: ±1..51. I negativi sono il mirroring invertito — il long
    /// richiede il pattern ribassista — e servono ai setup contrarian, che senza non esistono.
    /// </summary>
    private static object[] Directional(int sentinel)
    {
        var values = new List<object> { sentinel };
        values.AddRange(Enumerable.Range(1, 51).Cast<object>());
        values.AddRange(Enumerable.Range(1, 51).Select(v => (object)(-v)));
        return values.ToArray();
    }

    /// <summary>
    /// base.py <c>fast_values</c>: 1..151. Le liste <c>*_yes</c> includono anche 153, che e' sempre
    /// falso: e' cosi' che la sweep puo' <b>spegnere</b> una direzione e rendere raggiungibili le
    /// strategie a senso unico.
    /// </summary>
    private static object[] Fast(int sentinel)
    {
        var values = new List<object> { sentinel };
        values.AddRange(Enumerable.Range(1, 151).Cast<object>());
        if (sentinel == 152) values.Add(153);
        return values.ToArray();
    }

    /// <summary>
    /// base.py <c>multiday_max_bars</c>: 2/4/7/10 sessioni in posizione, scalate sul timeframe
    /// (sessione futures ~23h = 1380 minuti).
    /// </summary>
    private static object[] MaxBars(int timeframeMinutes)
    {
        var barsPerDay = Math.Max(1, 1380 / Math.Max(1, timeframeMinutes));
        var candidates = new[] { 0, 12, 24, 48, 2 * barsPerDay, 4 * barsPerDay, 7 * barsPerDay, 10 * barsPerDay };
        return candidates.Distinct().Cast<object>().ToArray();
    }

    /// <summary>
    /// <c>price_channel.py</c>. Lo spazio cambia su daily, dove orari e volatilita' non si
    /// ottimizzano e il canale ha una griglia propria.
    /// </summary>
    public static SweepSpace PriceChannel(int timeframeMinutes)
    {
        var daily = timeframeMinutes >= 1440;

        var parameters = new List<SweepParameter>
        {
            new("ChannelBars", daily
                ? [20, 1, 10, 15, 30, 40, 55]
                // 75/100/155 dopo l'audit: TOP_UA_336 usa Donchian 155, e con il massimo a 50 il suo
                // canale era irraggiungibile. ChannelBars = 1 e' il breakout della singola barra.
                : [20, 1, 10, 15, 30, 40, 50, 75, 100, 155]),
            new("OffsetTicks", daily ? [0, 5, 10, 20] : [0, 2, 5, 10]),
            new("Direction", [0, 1, 2], Categorical: true),
            new("DvolMin", daily ? [0] : [0, 3000, 6000], OffSentinel: 0),
            new("PtnNeutYes", Neutral(55), Categorical: true),
            new("PtnNeutNo", Neutral(56), Categorical: true),
            new("PtnDirYes", Directional(52), Categorical: true),
            new("PtnDirNo", Directional(53), Categorical: true),
            new("StartHour", daily ? [-1] : Hours, OffSentinel: -1),
            new("EndHour", daily ? [-1] : Hours, OffSentinel: -1),
            new("SkipDay", [-1, 4], Categorical: true),
            new("StopLoss", daily ? [3000, 1000, 2000, 5000, 8000] : StopLossGrid),
            new("TakeProfit", daily ? [0, 2000, 4000, 6000, 10000, 15000] : TakeProfitGrid, OffSentinel: 0),
            new("MaxBars", daily ? [0, 5, 10, 20] : MaxBars(timeframeMinutes), OffSentinel: 0),
            new("TrailingStop", daily ? [0, 2000, 4000, 8000] : [0, 500, 1000, 2000], OffSentinel: 0),
            new("BreakEven", daily ? [0, 1000, 2000] : [0, 500, 1000], OffSentinel: 0)
        };

        // intraday_only fa parte del trigger e su daily non si ottimizza: il motore di ricerca non
        // applica l'uscita di sessione su D1, quindi li' il parametro e' inerte.
        if (!daily)
            parameters.Insert(2, new SweepParameter("IntradayOnly", [1, 0], Categorical: true));

        var trigger = daily
            ? new[] { "ChannelBars", "OffsetTicks", "Direction" }
            : ["ChannelBars", "OffsetTicks", "IntradayOnly", "Direction"];

        var phases = new List<SweepPhase>
        {
            // La direzione sta nel trigger e non fra i filtri: giudicare la lunghezza del canale con
            // direction = 0 forzata dimezzava il punteggio del ramo long-only sugli indici
            // long-biased, e la prima fase sceglieva il canale sbagliato.
            new("trigger", trigger),
            new("volatilita'", ["DvolMin"]),
            new("pattern neutrali", ["PtnNeutYes", "PtnNeutNo"]),
            new("pattern direzionali", ["PtnDirYes", "PtnDirNo"]),
            new("orari e giorni", ["StartHour", "EndHour", "SkipDay"]),
            // Stop/target separati da trailing/breakeven: in una fase sola sono troppe combinazioni
            // per il passo "affinare stop e target" del metodo. Entrambe sull'orologio fitto: sono
            // le fasi che decidono dove va lo stop, e sul percorso veloce lo stop stretto vince
            // sempre — vedi RequiresAccurateClock.
            new("stop e target", ["StopLoss", "TakeProfit", "MaxBars"], RequiresAccurateClock: true),
            new("trailing e breakeven", ["TrailingStop", "BreakEven"], RequiresAccurateClock: true)
        };

        // get_default_params(): una base senza stop ne' target fa vincere la sentinella a ogni fase.
        var defaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["StopLoss"] = daily ? 3000 : 1500,
            ["TakeProfit"] = daily ? 6000 : 3000,
            ["MaxBars"] = 0,
            ["TrailingStop"] = 0,
            ["BreakEven"] = 0
        };

        var sentinels = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PtnNeutYes"] = 55,
            ["PtnNeutNo"] = 56,
            ["PtnDirYes"] = 52,
            ["PtnDirNo"] = 53
        };

        return new SweepSpace("PC", parameters, phases, defaults, sentinels);
    }

    /// <summary>
    /// <c>bias_weekly.py</c>: ciclo settimanale, entra giorno X ora Y ed esce giorno Z ora W.
    ///
    /// <para>Gli orari sono interi <c>HHMM</c> come nei report della ricerca — <c>800</c> sono le
    /// 08:00 — e le classi li convertono con <c>TimeFromLegacyHhmm</c>. I giorni sono in convenzione
    /// pandas (0 = lunedi'), e <c>-1</c> sul giorno di ingresso <b>spegne la direzione</b>: e'
    /// l'interruttore, non un valore mancante.</para>
    /// </summary>
    public static SweepSpace BiasWeekly()
    {
        object[] hours = Enumerable.Range(0, 24).Select(h => (object)(h * 100)).ToArray();
        object[] entryDays = [-1, 0, 1, 2, 3, 4];
        object[] exitDays = [0, 1, 2, 3, 4];

        var parameters = new List<SweepParameter>
        {
            new("EntryDayLong", entryDays, Categorical: true),
            new("ExitDayLong", exitDays, Categorical: true),
            new("EntryTimeLong", hours),
            new("ExitTimeLong", hours),
            new("EntryDayShort", entryDays, Categorical: true),
            new("ExitDayShort", exitDays, Categorical: true),
            new("EntryTimeShort", hours),
            new("ExitTimeShort", hours),
            new("PtnLyYes", Fast(152), Categorical: true),
            new("PtnLyNo", Fast(153), Categorical: true),
            new("PtnSyYes", Fast(152), Categorical: true),
            new("PtnSyNo", Fast(153), Categorical: true),
            new("StopLoss", StopLossGrid),
            new("TakeProfit", TakeProfitGrid, OffSentinel: 0)
        };

        var phases = new List<SweepPhase>
        {
            new("timing long: giorni", ["EntryDayLong", "ExitDayLong"]),
            new("timing long: orari", ["EntryTimeLong", "ExitTimeLong"]),
            new("timing short: giorni", ["EntryDayShort", "ExitDayShort"]),
            new("timing short: orari", ["EntryTimeShort", "ExitTimeShort"]),
            new("pattern long", ["PtnLyYes", "PtnLyNo"]),
            new("pattern short", ["PtnSyYes", "PtnSyNo"]),
            new("stop e target", ["StopLoss", "TakeProfit"], RequiresAccurateClock: true)
        };

        // get_default_params(): entrambe le direzioni spente, e le fasi di timing ne accendono una
        // alla volta. Base senza stop ne' target: il trade e' delimitato dalla finestra giorno/ora,
        // e il risk management si scava nell'ultima fase.
        var defaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["EntryDayLong"] = -1,
            ["EntryDayShort"] = -1,
            ["EntryTimeLong"] = 100,
            ["ExitTimeLong"] = 100,
            ["EntryTimeShort"] = 100,
            ["ExitTimeShort"] = 100,
            ["StopLoss"] = 0,
            ["TakeProfit"] = 0
        };

        var sentinels = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PtnLyYes"] = 152,
            ["PtnLyNo"] = 153,
            ["PtnSyYes"] = 152,
            ["PtnSyNo"] = 153
        };

        return new SweepSpace("BIASW", parameters, phases, defaults, sentinels);
    }
}
