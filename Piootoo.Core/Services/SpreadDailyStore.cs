using System.Globalization;
using System.Text;
using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Services;

/// <summary>
/// L'archivio delle <b>giornate</b> di spread raccolte dal raccoglitore, e la finestra mobile che ne
/// esce: gli ultimi <see cref="WindowDays"/> giorni di ogni simbolo, scritti in <c>spread/{BROKER}/</c>
/// negli stessi due CSV che produceva il bot degli spread e che il backtest legge
/// (<see cref="SpreadTable"/>).
///
/// <para><b>Perche' a giornate.</b> Prima la misura era un giro a mano: un mese di tick per simbolo,
/// un bot da lanciare, un CSV che invecchiava. Il raccoglitore resta acceso e ogni notte misura il
/// giorno appena chiuso; il server tiene i giorni e ricalcola la finestra. Gli istogrammi si sommano
/// esattamente, quindi il risultato e' lo stesso di una misura sull'intera finestra.</para>
///
/// <para><b>Dove.</b> <c>spread/{BROKER}/giornaliero/{SIMBOLO}_{yyyy-MM}.json</c>, un file per
/// simbolo e mese: un file per giorno sarebbero diecimila file l'anno nel repository. Il caricatore
/// guarda la sola cartella del broker, non le sottocartelle, quindi i giorni grezzi non si confondono
/// con la misura.</para>
///
/// <para><b>La scrittura passa da <see cref="SpreadMeasurementStore.IngestForBroker"/></b>: i simboli
/// ricalcolati sostituiscono le proprie righe e gli altri restano, esattamente come una misura del
/// bot. Una misura manuale di un simbolo che il raccoglitore non segue non viene toccata.</para>
/// </summary>
public sealed class SpreadDailyStore
{
    /// <summary>Ampiezza della finestra mobile, in giorni di calendario fino all'ultimo giorno raccolto.</summary>
    public const int WindowDays = 30;

    public const string DailyFolder = "giornaliero";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private readonly string _root;
    private readonly WorkspaceService _workspaces;
    private readonly SpreadMeasurementStore _measurements;
    private readonly object _gate = new();

    public SpreadDailyStore(PiootooSettings settings, WorkspaceService workspaces, SpreadMeasurementStore measurements)
    {
        _root = settings.GetSpreadPath();
        _workspaces = workspaces;
        _measurements = measurements;
    }

    /// <summary>
    /// Registra le giornate e riscrive la finestra dei simboli toccati. Un giorno gia' presente si
    /// sostituisce: rimisurarlo e' il modo di correggerlo, e l'ultima misura e' quella buona.
    /// </summary>
    public SpreadDailyResponse Ingest(SpreadDailyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Days.Count == 0)
            throw new ArgumentException("Nessuna giornata da registrare: 'days' e' vuoto.");

        var broker = ResolveBroker(request.AccountNumber);
        var response = new SpreadDailyResponse { Broker = broker };

        foreach (var day in request.Days)
            Validate(day);

        lock (_gate)
        {
            foreach (var group in request.Days.GroupBy(day => (Symbol: NormalizeSymbol(day.Symbol), Month: MonthKey(day.DayUtc))))
            {
                var path = MonthPath(broker, group.Key.Symbol, group.Key.Month);
                var file = ReadMonth(path) ?? new SpreadMonthFile { Symbol = group.Key.Symbol };

                foreach (var day in group)
                {
                    day.Symbol = group.Key.Symbol;
                    day.DayUtc = DateTime.SpecifyKind(day.DayUtc.Date, DateTimeKind.Utc);
                    file.BrokerSymbol = day.BrokerSymbol;
                    file.Days[DayKey(day.DayUtc)] = day;
                    response.Stored.Add($"{group.Key.Symbol} {DayKey(day.DayUtc)}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                AtomicFileWriter.WriteAllText(path, JsonSerializer.Serialize(file, Json));
            }

            var touched = request.Days.Select(day => NormalizeSymbol(day.Symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var window = BuildWindow(broker, touched, response.Warnings);
            if (window is not null)
            {
                window.AccountNumber = request.AccountNumber?.Trim() ?? string.Empty;
                window.BotVersion = request.BotVersion?.Trim() ?? string.Empty;
                response.SymbolFile = _measurements.IngestForBroker(broker, window).SymbolFile;
            }
        }

        return response;
    }

    /// <summary>
    /// I giorni gia' raccolti per ogni simbolo del broker del conto, dal <paramref name="sinceUtc"/>
    /// in poi. E' la domanda con cui il raccoglitore decide cosa misurare: solo quello che manca.
    /// </summary>
    public SpreadDailyStatus GetStatus(string accountNumber, DateTime sinceUtc)
    {
        var broker = ResolveBroker(accountNumber);
        var status = new SpreadDailyStatus { Broker = broker };
        var since = DayKey(sinceUtc);

        var folder = Path.Combine(_root, broker, DailyFolder);
        if (!Directory.Exists(folder))
            return status;

        lock (_gate)
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
            {
                var file = ReadMonth(path);
                if (file is null)
                    continue;

                if (!status.Days.TryGetValue(file.Symbol, out var days))
                    status.Days[file.Symbol] = days = new List<string>();

                days.AddRange(file.Days.Keys.Where(day => string.CompareOrdinal(day, since) >= 0));
            }
        }

        foreach (var days in status.Days.Values)
            days.Sort(StringComparer.Ordinal);

        return status;
    }

    /// <summary>
    /// La misura dei simboli toccati sulla loro finestra mobile, nella forma dei due CSV del bot. Null
    /// se nessun simbolo ha un solo tick nella finestra: un file di sole righe vuote cancellerebbe
    /// una misura vera.
    /// </summary>
    private SpreadMeasurementRequest? BuildWindow(string broker, IReadOnlyList<string> symbols, List<string> warnings)
    {
        var bySymbol = new StringBuilder();
        var byHour = new StringBuilder();
        bySymbol.Append("# spread = ask - bid. Le colonne *Ticks sono la stessa misura in tick di strumento (spread / tickSize).\n");
        bySymbol.Append(SpreadMeasurementStore.SymbolHeader).Append('\n');
        byHour.Append("# hourUtc = ora UTC di apertura del bucket (0..23), NON convertita nel fuso della strategia.\n");
        byHour.Append("# Un'ora con ticks=0 e celle vuote e' un'ora in cui il broker non ha quotato: mercato chiuso.\n");
        byHour.Append(SpreadMeasurementStore.HourHeader).Append('\n');

        DateTime? from = null, to = null;
        var written = 0;

        foreach (var symbol in symbols)
        {
            var days = WindowOf(broker, symbol);
            var withTicks = days.Where(day => day.Hours.Count > 0).ToList();
            if (withTicks.Count == 0)
            {
                warnings.Add($"{symbol}: nessun tick negli ultimi {WindowDays} giorni raccolti, la misura precedente resta.");
                continue;
            }

            // Tick e cifre dall'ultimo giorno: se il broker li cambia, conta come quota adesso.
            var last = withTicks[^1];
            var overall = new SpreadHistogram();
            var hours = new SpreadHistogram[24];
            for (var hour = 0; hour < 24; hour++)
                hours[hour] = new SpreadHistogram();

            foreach (var day in withTicks)
            {
                if (day.TickSize != last.TickSize)
                {
                    warnings.Add($"{symbol}: il {DayKey(day.DayUtc)} il tick era {day.TickSize}, oggi {last.TickSize}: giorno escluso.");
                    continue;
                }

                foreach (var hour in day.Hours)
                {
                    foreach (var bin in hour.Bins)
                    {
                        overall.Add((int)bin[0], bin[1]);
                        hours[hour.Hour].Add((int)bin[0], bin[1]);
                    }
                }
            }

            var firstTick = withTicks.Min(day => day.FirstTickUtc ?? day.DayUtc);
            var lastTick = withTicks.Max(day => day.LastTickUtc ?? day.DayUtc.AddDays(1));
            var windowStart = days[0].DayUtc;
            var windowEnd = days[^1].DayUtc;
            from = from is null || windowStart < from ? windowStart : from;
            to = to is null || windowEnd > to ? windowEnd : to;

            var format = new PriceFormat(last.TickSize, last.Digits);
            bySymbol.Append(broker).Append(',')
                .Append(symbol).Append(',')
                .Append(last.BrokerSymbol).Append(',')
                .Append(overall.Count.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Utc(firstTick)).Append(',')
                .Append(Utc(lastTick)).Append(',')
                .Append(Number(last.TickSize)).Append(',')
                .Append(Number(last.PipSize)).Append(',');
            AppendDistribution(bySymbol, overall, format);
            bySymbol.Append(overall.NonPositive.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append("false").Append(',')
                .Append(string.Format(CultureInfo.InvariantCulture,
                    "finestra mobile di {0} giorni raccolti ({1} con tick) dal {2} al {3}",
                    days.Count, withTicks.Count, DayKey(windowStart), DayKey(windowEnd)))
                .Append('\n');

            for (var hour = 0; hour < 24; hour++)
            {
                byHour.Append(broker).Append(',')
                    .Append(symbol).Append(',')
                    .Append(hour.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(hours[hour].Count.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Number(last.TickSize)).Append(',');
                AppendDistribution(byHour, hours[hour], format);
                byHour.Append(hours[hour].NonPositive.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            written++;
        }

        if (written == 0)
            return null;

        return new SpreadMeasurementRequest
        {
            WindowFromUtc = from!.Value,
            WindowToUtc = to!.Value,
            BySymbolCsv = bySymbol.ToString(),
            ByHourCsv = byHour.ToString()
        };
    }

    /// <summary>I giorni della finestra di un simbolo: gli ultimi <see cref="WindowDays"/> di calendario
    /// fino all'ultimo giorno raccolto, in ordine.</summary>
    private List<SpreadDayDto> WindowOf(string broker, string symbol)
    {
        var folder = Path.Combine(_root, broker, DailyFolder);
        var prefix = FilePrefix(symbol);
        var days = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, prefix + "_*.json", SearchOption.TopDirectoryOnly)
                .Select(ReadMonth)
                .Where(file => file is not null)
                .SelectMany(file => file!.Days.Values)
                .OrderBy(day => day.DayUtc)
                .ToList()
            : [];

        if (days.Count == 0)
            return days;

        var firstIncluded = days[^1].DayUtc.AddDays(-(WindowDays - 1));
        return days.Where(day => day.DayUtc >= firstIncluded).ToList();
    }

    /// <summary>Le dodici colonne della distribuzione, nello stesso ordine e formato del bot: vuote
    /// e non a zero quando non ci sono tick, perche' zero e' uno spread misurato.</summary>
    private static void AppendDistribution(StringBuilder text, SpreadHistogram stats, PriceFormat format)
    {
        if (stats.Count == 0)
        {
            text.Append(",,,,,,,,,,,,");
            return;
        }

        text.Append(format.Price(stats.Min)).Append(',')
            .Append(format.Price(stats.Percentile(0.50))).Append(',')
            .Append(format.Price(stats.Mean)).Append(',')
            .Append(format.Price(stats.Percentile(0.90))).Append(',')
            .Append(format.Price(stats.Percentile(0.99))).Append(',')
            .Append(format.Price(stats.Max)).Append(',')
            .Append(stats.Min.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(stats.Percentile(0.50).ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(stats.Mean.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
            .Append(stats.Percentile(0.90).ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(stats.Percentile(0.99).ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(stats.Max.ToString(CultureInfo.InvariantCulture)).Append(',');
    }

    private static void Validate(SpreadDayDto day)
    {
        if (string.IsNullOrWhiteSpace(day.Symbol))
            throw new ArgumentException("Una giornata senza simbolo non e' attribuibile.");
        if (day.TickSize <= 0)
            throw new ArgumentException($"{day.Symbol} {DayKey(day.DayUtc)}: tickSize {day.TickSize} non valido, gli spread sono contati in tick.");
        if (day.DayUtc.Date >= DateTime.UtcNow.Date)
            throw new ArgumentException($"{day.Symbol} {DayKey(day.DayUtc)}: il giorno non e' ancora chiuso, si misura solo un giorno finito.");

        foreach (var hour in day.Hours)
        {
            if (hour.Hour is < 0 or > 23)
                throw new ArgumentException($"{day.Symbol} {DayKey(day.DayUtc)}: ora {hour.Hour} fuori da 0..23.");
            if (hour.Bins.Any(bin => bin is not { Length: 2 } || bin[1] < 0))
                throw new ArgumentException($"{day.Symbol} {DayKey(day.DayUtc)} ora {hour.Hour}: ogni bin e' [spread in tick, conteggio >= 0].");
        }
    }

    private string ResolveBroker(string? accountNumber)
    {
        var number = accountNumber?.Trim() ?? string.Empty;
        var account = _workspaces.ListAccounts().FirstOrDefault(candidate =>
                          string.Equals(candidate.AccountNumber?.Trim(), number, StringComparison.OrdinalIgnoreCase))
                      ?? throw new InvalidOperationException(
                          $"Il conto '{number}' non e' in anagrafica: senza non si sa di quale broker sia la misura.");

        return _workspaces.ResolveBrokerLabelForAccount(account)
               ?? throw new InvalidOperationException(
                   $"Il conto '{number}' non ha un broker in anagrafica: non c'e' una cartella in cui scrivere la misura.");
    }

    private string MonthPath(string broker, string symbol, string month) =>
        Path.Combine(_root, broker, DailyFolder, $"{FilePrefix(symbol)}_{month}.json");

    private static SpreadMonthFile? ReadMonth(string path)
    {
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<SpreadMonthFile>(File.ReadAllText(path), Json)
               ?? throw new InvalidDataException($"{path} non e' un archivio di giornate leggibile.");
    }

    private static string NormalizeSymbol(string symbol) => StrategyKeys.NormalizeSymbolWithPrefix(symbol.Trim());

    /// <summary>Il simbolo senza '@', maiuscolo: <c>@FESX</c> -> <c>FESX</c>.</summary>
    private static string FilePrefix(string symbol) => symbol.Trim().TrimStart('@').ToUpperInvariant();

    private static string DayKey(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string MonthKey(DateTime day) => day.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private static string Utc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    /// <summary>Un decimale senza zeri in coda: <c>0.010</c> e <c>0.01</c> sono lo stesso tick.</summary>
    private static string Number(decimal value) => (value / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);

    /// <summary>Da tick a prezzo con le cifre del simbolo, come nel bot.</summary>
    private readonly record struct PriceFormat(decimal TickSize, int Digits)
    {
        public string Price(double ticks) =>
            ((decimal)ticks * TickSize).ToString("F" + Digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary>Un simbolo in un mese: i giorni per chiave <c>yyyy-MM-dd</c>.</summary>
    private sealed class SpreadMonthFile
    {
        public string Symbol { get; set; } = string.Empty;
        public string BrokerSymbol { get; set; } = string.Empty;
        public SortedDictionary<string, SpreadDayDto> Days { get; set; } = new(StringComparer.Ordinal);
    }
}

/// <summary>
/// Distribuzione dello spread in tick interi. Percentili per conteggio, senza interpolazione — uno
/// spread di 1,5 tick non esiste — con lo stesso algoritmo del bot degli spread, cosi' la finestra
/// ricalcolata dal server e una misura del bot sulla stessa finestra danno gli stessi numeri.
/// </summary>
public sealed class SpreadHistogram
{
    private readonly SortedDictionary<int, long> _counts = new();

    public long Count { get; private set; }
    public long NonPositive { get; private set; }
    public int Min => _counts.Count == 0 ? 0 : _counts.Keys.First();
    public int Max => _counts.Count == 0 ? 0 : _counts.Keys.Last();

    public void Add(int spreadTicks, long count)
    {
        if (count <= 0)
            return;

        _counts.TryGetValue(spreadTicks, out var current);
        _counts[spreadTicks] = current + count;
        Count += count;
        if (spreadTicks <= 0)
            NonPositive += count;
    }

    public double Mean
    {
        get
        {
            if (Count == 0)
                return 0d;

            var total = 0d;
            foreach (var (ticks, count) in _counts)
                total += (double)ticks * count;
            return total / Count;
        }
    }

    /// <summary>Il primo valore osservato che copre almeno il quantile chiesto.</summary>
    public int Percentile(double quantile)
    {
        if (Count == 0)
            return 0;

        var target = Math.Max(1L, (long)Math.Ceiling(quantile * Count));
        long cumulative = 0;
        foreach (var (ticks, count) in _counts)
        {
            cumulative += count;
            if (cumulative >= target)
                return ticks;
        }

        return Max;
    }
}
