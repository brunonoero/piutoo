using System.Globalization;
using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Core.Services;

/// <summary>
/// Lo spread misurato di un broker, per simbolo, letto dal CSV <c>spread-by-symbol</c> che produce
/// <c>PiootooSpreadDumpBot</c>.
///
/// <para><b>Perche' si legge da un file e non si scrive a mano.</b> Lo spread e' una misura, non un
/// parametro: cambia per broker, per simbolo e per periodo, e i valori giusti sono venti numeri che
/// nessuno ricopia due volte allo stesso modo. Il file porta con se' anche <i>quando</i> e su quale
/// conto e' stato misurato, che e' cio' che rende un run rifacibile mesi dopo.</para>
///
/// <para><b>Un broker per volta.</b> Vale la stessa regola del datafeed: due broker sullo stesso
/// simbolo non hanno lo stesso spread — e' il confronto per cui la misura esiste — quindi le
/// cartelle sono separate e un run ne legge una sola. La scelta e' pero' <b>indipendente</b> da
/// <see cref="BacktestingRequest.DatafeedBroker"/>: girare sul feed interno con lo spread di un
/// broker vero e' esattamente il confronto che dice quanto costa quel broker, e legare le due scelte
/// lo renderebbe impossibile.</para>
/// </summary>
public sealed class SpreadTable
{
    /// <summary>Nome della cartella di servizio sotto la radice degli spread, per i file di lavoro.</summary>
    public const string FilePattern = "*spread-by-symbol*.csv";

    /// <summary>
    /// Il token che distingue i due file della stessa misura. Il file per ora non si cerca con una
    /// glob propria: si ricava dal nome di quello per simbolo sostituendo questo token, cosi' i due
    /// vengono per costruzione dallo <b>stesso</b> run del bot. Prendere anche qui "il piu' recente"
    /// avrebbe potuto accoppiare la costante di agosto con le ore di luglio, e un run che mescola
    /// due finestre non e' confrontabile con nessuno dei due.
    /// </summary>
    private const string SymbolToken = "spread-by-symbol";

    private const string HourToken = "spread-by-hour";

    /// <summary>Quante righe ha il file per ora, per simbolo: le 24 ore UTC, anche quelle vuote.</summary>
    private const int HoursPerDay = 24;

    private SpreadTable(
        string broker,
        string filePath,
        DateTime fileUtc,
        string column,
        SpreadResolution resolution,
        IReadOnlyDictionary<string, decimal> points,
        IReadOnlyDictionary<string, decimal[]> pointsByHour,
        IReadOnlyList<string> warnings)
    {
        Broker = broker;
        FilePath = filePath;
        FileUtc = fileUtc;
        Column = column;
        Resolution = resolution;
        Points = points;
        PointsByHour = pointsByHour;
        Warnings = warnings;
    }

    public string Broker { get; }

    public string FilePath { get; }

    /// <summary>Ultima scrittura del file: e' l'eta' della misura, e una misura vecchia va vista.</summary>
    public DateTime FileUtc { get; }

    /// <summary>Colonna da cui escono i valori, dichiarata perche' p50 e media danno run diversi.</summary>
    public string Column { get; }

    /// <summary>Su quale asse si legge la misura, dichiarato perche' per simbolo e per ora danno run diversi.</summary>
    public SpreadResolution Resolution { get; }

    /// <summary>
    /// La costante per simbolo. Con <see cref="SpreadResolution.PerHour"/> non e' piu' il valore
    /// applicato ma il <b>ripiego</b>: e' cio' che paga un ingresso in un'ora che il broker non ha
    /// mai quotato, e resta il numero di riferimento del run.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> Points { get; }

    /// <summary>
    /// Le 24 ore UTC per simbolo, vuoto con <see cref="SpreadResolution.PerSymbol"/>. Ogni array ha
    /// sempre 24 caselle piene — le ore senza misura portano gia' il ripiego di
    /// <see cref="Points"/> — cosi' chi legge nel loop caldo fa una ricerca e un indice, senza un
    /// ramo per il caso mancante.
    /// </summary>
    public IReadOnlyDictionary<string, decimal[]> PointsByHour { get; }

    /// <summary>
    /// Cose che non fermano il run ma che chi legge deve sapere: una finestra coperta solo in parte,
    /// un simbolo senza tick. Finiscono nel log di avvio e nel report, non in un'eccezione: la
    /// misura c'e', e' solo meno solida di quanto sembri.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Etichetta compatta per il log di avvio, il summary e il report.</summary>
    public string Describe() =>
        $"{Broker} · {Column} · {(Resolution == SpreadResolution.PerHour ? "per ora UTC" : "per simbolo")}" +
        $" · {Path.GetFileName(FilePath)} ({FileUtc:yyyy-MM-dd})";

    /// <summary>
    /// Carica la tabella di un broker da <c>{root}/{BROKER}/</c>, scegliendo il CSV
    /// <c>spread-by-symbol</c> <b>piu' recente</b>: il bot li scrive con la finestra nel nome, quindi
    /// il nome non e' fisso e indovinarlo non si puo'. Nessun file, o un file senza le colonne
    /// attese, fa fallire l'avvio: e' la stessa regola del datafeed mancante — mai proseguire in
    /// silenzio, perche' un run senza spread sembra identico a uno con lo spread e vale un'altra
    /// cosa.
    /// </summary>
    public static SpreadTable Load(
        string root,
        string broker,
        SpreadStatistic statistic,
        SpreadResolution resolution = SpreadResolution.PerSymbol)
    {
        if (string.IsNullOrWhiteSpace(broker))
            throw new ArgumentException("Il broker della tabella spread non puo' essere vuoto.", nameof(broker));

        var name = broker.Trim();

        // Il nome puo' arrivare da una richiesta HTTP: deve restare un nome di cartella, non
        // diventare un percorso. Stesso controllo di DatafeedCatalog.ResolveRoot.
        if (name.Contains(Path.DirectorySeparatorChar)
            || name.Contains(Path.AltDirectorySeparatorChar)
            || name.Contains(':')
            || name is "." or ".."
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                $"'{broker}' non e' un nome di broker valido: deve essere il nome di una cartella sotto {root}.",
                nameof(broker));
        }

        var folder = Path.Combine(root, name);
        if (!Directory.Exists(folder))
        {
            var available = Directory.Exists(root)
                ? Directory.EnumerateDirectories(root).Select(Path.GetFileName).ToList()
                : [];

            throw new DirectoryNotFoundException(
                $"Non c'e' nessuna misura di spread per il broker '{name}' in {folder}. " +
                (available.Count == 0
                    ? "Nessun broker misurato: lancia PiootooSpreadDumpBot e copia i CSV sotto questa radice."
                    : $"Disponibili: {string.Join(", ", available)}."));
        }

        var file = new DirectoryInfo(folder)
            .EnumerateFiles(FilePattern, SearchOption.TopDirectoryOnly)
            .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
            .FirstOrDefault()
            ?? throw new FileNotFoundException(
                $"In {folder} non c'e' nessun file '{FilePattern}': e' l'uscita di PiootooSpreadDumpBot, " +
                "e senza non c'e' spread da applicare.");

        var column = ColumnOf(statistic);
        var points = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        var header = (string[]?)null;
        var lineNumber = 0;
        foreach (var raw in File.ReadLines(file.FullName))
        {
            lineNumber++;
            var line = raw.Trim();

            // Le righe '#' sono l'intestazione descrittiva del bot (broker, conto, finestra): non
            // sono dati, ma sono il motivo per cui il file si spiega da solo e non vanno tolte.
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var cells = line.Split(',');
            if (header is null)
            {
                header = cells;
                continue;
            }

            var symbol = Cell(header, cells, "symbol");
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            symbol = StrategyKeys.NormalizeSymbol(symbol);

            var text = Cell(header, cells, column);
            if (string.IsNullOrWhiteSpace(text))
            {
                // Cella vuota = nessuna misura per quel simbolo (il bot non l'ha mai quotato nella
                // finestra). Non e' zero: zero sarebbe uno spread misurato e nullo.
                warnings.Add($"{symbol}: nessuna misura nel file ({column} vuoto).");
                continue;
            }

            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidDataException(
                    $"{file.Name}, riga {lineNumber}: '{column}' vale '{text}', che non e' un numero.");
            }

            if (value < 0m)
            {
                throw new InvalidDataException(
                    $"{file.Name}, riga {lineNumber}: spread negativo su {symbol} ({value}). " +
                    "Un ask sotto il bid non e' una misura, e' un feed rotto.");
            }

            points[symbol] = value;

            if (string.Equals(Cell(header, cells, "truncated"), "true", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(
                    $"{symbol}: misurato su una finestra coperta solo in parte (truncated=true nel file).");
            }
        }

        if (header is null)
        {
            throw new InvalidDataException(
                $"{file.FullName} non ha nemmeno una riga di intestazione: non e' un file spread-by-symbol.");
        }

        if (Array.IndexOf(header, column) < 0)
        {
            throw new InvalidDataException(
                $"{file.Name} non ha la colonna '{column}'. Colonne trovate: {string.Join(", ", header)}. " +
                "Serve un file prodotto da PiootooSpreadDumpBot 2.0.0 o successivo.");
        }

        if (points.Count == 0)
        {
            throw new InvalidDataException(
                $"{file.FullName} non contiene nessuno spread utilizzabile nella colonna '{column}'.");
        }

        var pointsByHour = resolution == SpreadResolution.PerHour
            ? LoadHourly(file, column, points, warnings)
            : new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);

        return new SpreadTable(
            name, file.FullName, file.LastWriteTimeUtc, column, resolution, points, pointsByHour, warnings);
    }

    /// <summary>
    /// Il file per ora gemello di un file per simbolo: stesso nome, token sostituito. Pubblico
    /// perche' anche l'anagrafica deve poter dire se un broker ha le ore <b>senza</b> caricarle —
    /// se non le ha, la risoluzione oraria non va nemmeno proposta.
    /// </summary>
    public static string HourFilePathOf(FileInfo symbolFile)
    {
        if (!symbolFile.Name.Contains(SymbolToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"{symbolFile.Name} non ha '{SymbolToken}' nel nome: non si puo' ricavare il file per ora della stessa misura.");
        }

        return Path.Combine(
            symbolFile.DirectoryName!,
            symbolFile.Name.Replace(SymbolToken, HourToken, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Le 24 ore UTC per simbolo, dal file <c>spread-by-hour</c> <b>della stessa misura</b>: il nome
    /// si ricava da quello per simbolo, non si ricerca. I due file escono dallo stesso run del bot e
    /// devono restare accoppiati — la costante di ripiego e le ore vengono dagli stessi tick, o il
    /// run mescola due finestre senza dirlo.
    ///
    /// <para>Le ore che il broker non ha quotato (celle vuote, <c>ticks=0</c>: mercato chiuso)
    /// prendono la costante per simbolo e finiscono negli avvisi. Zero sarebbe uno spread misurato e
    /// nullo, e un ingresso gratis in un'ora vuota non e' un dato: e' un buco che somiglia a un
    /// regalo. Il ripiego rende anche l'array sempre pieno, cosi' la lettura nel loop caldo e' una
    /// ricerca piu' un indice.</para>
    /// </summary>
    private static Dictionary<string, decimal[]> LoadHourly(
        FileInfo symbolFile,
        string column,
        IReadOnlyDictionary<string, decimal> points,
        List<string> warnings)
    {
        var fileName = symbolFile.Name;
        var hourPath = HourFilePathOf(symbolFile);

        if (!File.Exists(hourPath))
        {
            throw new FileNotFoundException(
                $"Il run chiede lo spread per ora ma accanto a {fileName} non c'e' {Path.GetFileName(hourPath)}. " +
                "Rilancia PiootooSpreadDumpBot con 'Scrivi la distribuzione per ora UTC' acceso (e' il default) " +
                "e copia entrambi i file: quello per ora senza il suo per simbolo non ha ripiego, e viceversa " +
                "le ore verrebbero da un'altra finestra.",
                hourPath);
        }

        var measured = new Dictionary<string, decimal?[]>(StringComparer.OrdinalIgnoreCase);
        var header = (string[]?)null;
        var lineNumber = 0;

        foreach (var raw in File.ReadLines(hourPath))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var cells = line.Split(',');
            if (header is null)
            {
                header = cells;

                if (Array.IndexOf(header, "hourUtc") < 0 || Array.IndexOf(header, column) < 0)
                {
                    throw new InvalidDataException(
                        $"{Path.GetFileName(hourPath)} non ha le colonne 'hourUtc' e '{column}'. " +
                        $"Colonne trovate: {string.Join(", ", header)}.");
                }

                continue;
            }

            var symbol = Cell(header, cells, "symbol");
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            symbol = StrategyKeys.NormalizeSymbol(symbol);

            if (!int.TryParse(Cell(header, cells, "hourUtc"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour)
                || hour < 0
                || hour >= HoursPerDay)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(hourPath)}, riga {lineNumber}: 'hourUtc' vale " +
                    $"'{Cell(header, cells, "hourUtc")}', che non e' un'ora UTC fra 0 e 23.");
            }

            var text = Cell(header, cells, column);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(hourPath)}, riga {lineNumber}: '{column}' vale '{text}', che non e' un numero.");
            }

            if (value < 0m)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(hourPath)}, riga {lineNumber}: spread negativo su {symbol} " +
                    $"alle {hour} UTC ({value}). Un ask sotto il bid non e' una misura, e' un feed rotto.");
            }

            if (!measured.TryGetValue(symbol, out var hours))
            {
                hours = new decimal?[HoursPerDay];
                measured[symbol] = hours;
            }

            hours[hour] = value;
        }

        if (header is null)
        {
            throw new InvalidDataException(
                $"{hourPath} non ha nemmeno una riga di intestazione: non e' un file spread-by-hour.");
        }

        // Si parte dai simboli della costante e non da quelli del file per ora: senza ripiego un
        // simbolo non e' utilizzabile, e uno presente solo qui sarebbe un file spaiato — cioe'
        // proprio il caso che l'accoppiamento dei nomi esiste per escludere.
        var result = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, fallback) in points)
        {
            if (!measured.TryGetValue(symbol, out var hours))
            {
                warnings.Add(
                    $"{symbol}: nessuna riga nel file per ora, il run usa la costante per simbolo ({fallback}).");
                continue;
            }

            var filled = new decimal[HoursPerDay];
            var empty = 0;
            for (var hour = 0; hour < HoursPerDay; hour++)
            {
                if (hours[hour].HasValue)
                {
                    filled[hour] = hours[hour]!.Value;
                }
                else
                {
                    filled[hour] = fallback;
                    empty++;
                }
            }

            if (empty > 0)
            {
                warnings.Add(
                    $"{symbol}: {empty} ore su {HoursPerDay} senza misura (mercato chiuso), " +
                    $"ripiegate sulla costante per simbolo ({fallback}).");
            }

            result[symbol] = filled;
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException(
                $"{hourPath} non contiene nessuna ora utilizzabile nella colonna '{column}' " +
                "per i simboli del file per simbolo.");
        }

        return result;
    }

    /// <summary>
    /// Quale colonna del file legge ogni statistica. La mediana e' il default perche' e' lo spread
    /// che una strategia paga <i>di solito</i>: la media di un mese intero comprende la riapertura
    /// della domenica sera e le news, istanti in cui nessuna strategia sta entrando, e la p90 e'
    /// utile solo per chiedersi quanto si perde nel caso brutto.
    /// </summary>
    private static string ColumnOf(SpreadStatistic statistic) => statistic switch
    {
        SpreadStatistic.Median => "p50Spread",
        SpreadStatistic.Mean => "avgSpread",
        SpreadStatistic.P90 => "p90Spread",
        _ => throw new ArgumentOutOfRangeException(nameof(statistic), statistic, "Statistica di spread sconosciuta.")
    };

    /// <summary>Cella per nome di colonna. Una riga piu' corta dell'intestazione non fa saltare il
    /// file: la colonna manca, e chi la chiede riceve vuoto.</summary>
    private static string Cell(string[] header, string[] cells, string columnName)
    {
        var index = Array.IndexOf(header, columnName);
        return index < 0 || index >= cells.Length ? string.Empty : cells[index].Trim();
    }
}
