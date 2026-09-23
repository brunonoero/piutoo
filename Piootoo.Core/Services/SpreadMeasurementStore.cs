using System.Globalization;
using System.Text;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Services;

/// <summary>
/// Riceve le misure di <c>PiootooSpreadDumpBot</c> e le scrive in <c>spread/{BROKER}/</c>, dove le
/// legge il backtest (<see cref="SpreadTable"/>).
///
/// <para><b>La cartella la decide il server</b>, dal conto su cui il bot ha misurato: e' l'etichetta
/// del broker del registro (<see cref="WorkspaceService.ResolveBrokerLabelForAccount"/>), la stessa
/// di <c>datafeed-external/</c> e <c>symbol-info/</c>. Un nome ricavato nel bot da
/// <c>Account.BrokerName</c> sarebbe un secondo nome per la stessa cosa — e per ICS e FTMO lo e'
/// stato: <c>RAWTRADINGLTD</c> e <c>FTMOPLATFORM</c>.</para>
///
/// <para><b>Unisce, non sovrascrive.</b> <see cref="SpreadTable"/> legge il solo file
/// <c>spread-by-symbol</c> piu' recente della cartella: una misura che copre quattordici simboli,
/// scritta accanto a quella che copre NQ e FDAX, toglierebbe a ogni run successivo lo spread di NQ e
/// FDAX senza un errore. I simboli misurati adesso sostituiscono le proprie righe — in tutti e due i
/// file, cosi' costante e ore di un simbolo vengono sempre dagli stessi tick — e gli altri restano.
/// E' cio' che per ICS si era fatto a mano il 22/09/2026. La coppia sostituita e la misura grezza
/// finiscono in <c>storico/</c>, che il caricatore non guarda.</para>
/// </summary>
public sealed class SpreadMeasurementStore
{
    public const string ArchiveFolder = "storico";

    /// <summary>Intestazione del file per simbolo: la scrivono il bot degli spread e
    /// <see cref="SpreadDailyStore"/>, e l'unione rifiuta file con colonne diverse.</summary>
    public const string SymbolHeader =
        "broker,symbol,brokerSymbol,ticks,firstTickUtc,lastTickUtc,tickSize,pipSize," +
        "minSpread,p50Spread,avgSpread,p90Spread,p99Spread,maxSpread," +
        "minSpreadTicks,p50SpreadTicks,avgSpreadTicks,p90SpreadTicks,p99SpreadTicks,maxSpreadTicks," +
        "nonPositiveTicks,truncated,note";

    /// <summary>Intestazione del file per ora UTC, gemello di <see cref="SymbolHeader"/>.</summary>
    public const string HourHeader =
        "broker,symbol,hourUtc,ticks,tickSize," +
        "minSpread,p50Spread,avgSpread,p90Spread,p99Spread,maxSpread," +
        "minSpreadTicks,p50SpreadTicks,avgSpreadTicks,p90SpreadTicks,p99SpreadTicks,maxSpreadTicks," +
        "nonPositiveTicks";

    private const string SymbolToken = "spread-by-symbol";
    private const string HourToken = "spread-by-hour";

    private readonly string _root;
    private readonly WorkspaceService _workspaces;
    private readonly object _gate = new();

    public SpreadMeasurementStore(PiootooSettings settings, WorkspaceService workspaces)
    {
        _root = settings.GetSpreadPath();
        _workspaces = workspaces;
    }

    public SpreadMeasurementResponse Ingest(SpreadMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Prima la forma, poi il conto: una misura malformata e' un errore della misura, qualunque
        // conto la porti.
        if (string.IsNullOrWhiteSpace(request.BySymbolCsv))
            throw new ArgumentException("La misura non contiene il file spread-by-symbol.");
        if (request.WindowToUtc < request.WindowFromUtc)
            throw new ArgumentException(
                $"Finestra rovesciata: {request.WindowFromUtc:yyyy-MM-dd} viene dopo {request.WindowToUtc:yyyy-MM-dd}.");

        var accountNumber = request.AccountNumber?.Trim() ?? string.Empty;
        var account = _workspaces.ListAccounts().FirstOrDefault(candidate =>
                          string.Equals(candidate.AccountNumber?.Trim(), accountNumber, StringComparison.OrdinalIgnoreCase))
                      ?? throw new InvalidOperationException(
                          $"Il conto '{accountNumber}' non e' in anagrafica: senza non si sa di quale broker sia la misura.");

        var broker = _workspaces.ResolveBrokerLabelForAccount(account)
                     ?? throw new InvalidOperationException(
                         $"Il conto '{accountNumber}' non ha un broker in anagrafica: non c'e' una cartella in cui scrivere la misura.");

        return IngestForBroker(broker, request);
    }

    /// <summary>
    /// L'unione vera e propria, con il broker gia' risolto. La usa anche
    /// <see cref="SpreadDailyStore"/>, che scrive la finestra mobile dei giorni raccolti con le
    /// stesse regole di una misura del bot: un solo punto che sa unire, archiviare e scrivere.
    /// </summary>
    public SpreadMeasurementResponse IngestForBroker(string broker, SpreadMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureFolderName(broker);

        if (string.IsNullOrWhiteSpace(request.BySymbolCsv))
            throw new ArgumentException("La misura non contiene il file spread-by-symbol.");
        if (request.WindowToUtc < request.WindowFromUtc)
            throw new ArgumentException(
                $"Finestra rovesciata: {request.WindowFromUtc:yyyy-MM-dd} viene dopo {request.WindowToUtc:yyyy-MM-dd}.");

        var accountNumber = request.AccountNumber?.Trim() ?? string.Empty;
        var incomingSymbols = CsvTable.Parse(request.BySymbolCsv, SymbolToken);
        if (incomingSymbols.Rows.Count == 0)
            throw new ArgumentException("Il file spread-by-symbol della misura non ha righe.");
        var incomingHours = string.IsNullOrWhiteSpace(request.ByHourCsv)
            ? null
            : CsvTable.Parse(request.ByHourCsv, HourToken);

        var measured = incomingSymbols.Symbols().ToHashSet(StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            var folder = Path.Combine(_root, broker);
            Directory.CreateDirectory(folder);

            var previousSymbolFile = new DirectoryInfo(folder)
                .EnumerateFiles(SpreadTable.FilePattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
                .FirstOrDefault();
            var previousHourPath = previousSymbolFile is null ? null : SpreadTable.HourFilePathOf(previousSymbolFile);
            if (previousHourPath is not null && !File.Exists(previousHourPath))
                previousHourPath = null;

            var previousSymbols = previousSymbolFile is null
                ? null
                : CsvTable.Parse(File.ReadAllText(previousSymbolFile.FullName), SymbolToken);
            var previousHours = previousHourPath is null
                ? null
                : CsvTable.Parse(File.ReadAllText(previousHourPath), HourToken);

            EnsureSameHeader(previousSymbols, incomingSymbols, previousSymbolFile?.Name);
            if (previousHours is not null && incomingHours is not null)
                EnsureSameHeader(previousHours, incomingHours, Path.GetFileName(previousHourPath));

            var kept = previousSymbols?.Symbols()
                           .Where(symbol => !measured.Contains(symbol))
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .ToList()
                       ?? [];

            var history = string.Format(CultureInfo.InvariantCulture,
                "# {0:yyyy-MM-dd HH:mm}Z: {1} dalla misura {2:yyyy-MM-dd} -> {3:yyyy-MM-dd} (bot v{4}, conto {5}).",
                DateTime.UtcNow, string.Join(" ", measured.Order(StringComparer.OrdinalIgnoreCase)),
                request.WindowFromUtc, request.WindowToUtc, request.BotVersion?.Trim(), accountNumber);

            var mergedSymbols = Merge(broker, previousSymbols, incomingSymbols, measured, history);
            var mergedHours = previousHours is null && incomingHours is null
                ? null
                : Merge(broker, previousHours, incomingHours, measured, history);

            var symbolName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2:yyyyMMdd}-{3:yyyyMMdd}.csv",
                broker, SymbolToken, request.WindowFromUtc, request.WindowToUtc);
            var hourName = symbolName.Replace(SymbolToken, HourToken, StringComparison.Ordinal);
            var symbolPath = Path.Combine(folder, symbolName);
            var hourPath = Path.Combine(folder, hourName);

            var archive = Path.Combine(folder, ArchiveFolder);
            Directory.CreateDirectory(archive);
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var archived = new List<string>();

            // La misura grezza, cosi' com'e' arrivata: e' l'unica copia di cio' che il bot ha visto
            // prima dell'unione, e rifare l'unione a mano deve restare possibile.
            archived.Add(WriteArchive(archive, $"misura-{stamp}-{symbolName}", request.BySymbolCsv));
            if (incomingHours is not null)
                archived.Add(WriteArchive(archive, $"misura-{stamp}-{hourName}", request.ByHourCsv!));

            // La coppia sostituita si copia prima di scrivere la nuova: se la scrittura cade a meta',
            // la misura di prima esiste ancora da qualche parte.
            if (previousSymbolFile is not null)
                archived.Add(CopyToArchive(archive, stamp, previousSymbolFile.FullName));
            if (previousHourPath is not null)
                archived.Add(CopyToArchive(archive, stamp, previousHourPath));

            // Prima il file per ora, poi quello per simbolo: il caricatore parte dal per-simbolo piu'
            // recente e ne cerca il gemello, quindi scrivendo nell'altro ordine un run che parte nel
            // mezzo troverebbe il per-simbolo nuovo senza le sue ore.
            if (mergedHours is not null)
                AtomicFileWriter.WriteAllText(hourPath, mergedHours);
            AtomicFileWriter.WriteAllText(symbolPath, mergedSymbols);

            if (previousSymbolFile is not null &&
                !string.Equals(previousSymbolFile.FullName, symbolPath, StringComparison.OrdinalIgnoreCase))
                File.Delete(previousSymbolFile.FullName);
            if (previousHourPath is not null &&
                !string.Equals(Path.GetFullPath(previousHourPath), Path.GetFullPath(hourPath), StringComparison.OrdinalIgnoreCase))
                File.Delete(previousHourPath);

            return new SpreadMeasurementResponse
            {
                Broker = broker,
                SymbolFile = symbolName,
                HourFile = mergedHours is null ? null : hourName,
                Measured = measured.Order(StringComparer.OrdinalIgnoreCase).ToList(),
                Kept = kept,
                Archived = archived
            };
        }
    }

    /// <summary>
    /// Le righe di prima meno quelle dei simboli rimisurati, poi le nuove. L'ordine dei simboli
    /// gia' presenti resta quello di prima: il diff del file dice cosa e' cambiato, non che e' stato
    /// rimescolato.
    /// </summary>
    private static string Merge(
        string broker, CsvTable? previous, CsvTable? incoming, IReadOnlySet<string> measured, string history)
    {
        var header = incoming?.Header ?? previous!.Header;

        var comments = new List<string>
        {
            $"# Misure di spread del broker {broker}, unite dal server Piootoo. Ogni simbolo porta la finestra in cui",
            "# e' stato misurato (firstTickUtc/lastTickUtc): i simboli non si confrontano fra loro su periodi diversi.",
            "# Il backtest legge solo il file spread-by-symbol piu' recente: le misure non rifatte restano qui, gli originali in storico/."
        };
        foreach (var line in previous?.Comments ?? [])
            if (!comments.Contains(line, StringComparer.Ordinal))
                comments.Add(line);
        foreach (var line in incoming?.Comments ?? [])
        {
            // La prima riga del bot ("Piootoo Spread Dump vX - broker ..., finestra ...") la sostituisce
            // la riga di storia, che dice le stesse cose piu' quali simboli.
            if (line.StartsWith("# Piootoo Spread Dump", StringComparison.Ordinal))
                continue;
            if (!comments.Contains(line, StringComparer.Ordinal))
                comments.Add(line);
        }
        comments.Add(history);

        var text = new StringBuilder();
        foreach (var line in comments)
            text.Append(line).Append('\n');
        text.Append(header).Append('\n');

        foreach (var row in previous?.Rows ?? [])
            if (!measured.Contains(row.Symbol))
                text.Append(row.Line).Append('\n');
        foreach (var row in incoming?.Rows ?? [])
            text.Append(row.Line).Append('\n');

        return text.ToString();
    }

    private static void EnsureSameHeader(CsvTable? previous, CsvTable incoming, string? previousName)
    {
        if (previous is null || string.Equals(previous.Header, incoming.Header, StringComparison.Ordinal))
            return;

        throw new InvalidDataException(
            $"Le colonne della misura non sono quelle di {previousName}: unire righe con colonne diverse " +
            "darebbe un file che nessuno sa leggere. Colonne della misura: " + incoming.Header +
            ". Colonne del file: " + previous.Header + ".");
    }

    private static string WriteArchive(string archive, string name, string contents)
    {
        AtomicFileWriter.WriteAllText(Path.Combine(archive, name), contents);
        return Path.Combine(ArchiveFolder, name);
    }

    private static string CopyToArchive(string archive, string stamp, string path)
    {
        var name = $"sostituito-{stamp}-{Path.GetFileName(path)}";
        File.Copy(path, Path.Combine(archive, name), overwrite: true);
        return Path.Combine(ArchiveFolder, name);
    }

    /// <summary>Stesso controllo di <see cref="SpreadTable.Load"/>: un nome di cartella, non un percorso.</summary>
    private static void EnsureFolderName(string broker)
    {
        if (broker.Contains(Path.DirectorySeparatorChar)
            || broker.Contains(Path.AltDirectorySeparatorChar)
            || broker.Contains(':')
            || broker is "." or ".."
            || broker.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException($"'{broker}' non e' un nome di cartella utilizzabile per le misure di spread.");
        }
    }

    /// <summary>
    /// Un CSV del bot tenuto per righe: commenti, intestazione e righe intere con il loro simbolo.
    /// Le righe non si riscompongono — la colonna <c>note</c> puo' essere quotata e contenere
    /// virgole — ma del simbolo basta sapere dov'e': sta prima di ogni campo quotato.
    /// </summary>
    private sealed class CsvTable
    {
        public List<string> Comments { get; } = new();

        public string Header { get; private set; } = string.Empty;

        public List<(string Symbol, string Line)> Rows { get; } = new();

        public IEnumerable<string> Symbols() => Rows.Select(row => row.Symbol);

        public static CsvTable Parse(string text, string kind)
        {
            var table = new CsvTable();
            var symbolIndex = -1;

            foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith('#'))
                {
                    table.Comments.Add(line);
                    continue;
                }

                if (symbolIndex < 0)
                {
                    table.Header = line;
                    symbolIndex = Array.IndexOf(line.Split(','), "symbol");
                    if (symbolIndex < 0)
                        throw new InvalidDataException($"Il file {kind} non ha la colonna 'symbol': {line}");
                    continue;
                }

                var cells = line.Split(',');
                if (symbolIndex >= cells.Length || string.IsNullOrWhiteSpace(cells[symbolIndex]))
                    continue;

                table.Rows.Add((StrategyKeys.NormalizeSymbolWithPrefix(cells[symbolIndex]), line));
            }

            if (symbolIndex < 0)
                throw new InvalidDataException($"Il file {kind} non ha nemmeno una riga di intestazione.");

            return table;
        }
    }
}
