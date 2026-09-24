using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>
/// Per ogni simbolo di un broker, cosa c'e' e cosa manca di cio' che serve a un run su quel broker.
/// Vedi <see cref="BrokerDataStatus"/>.
///
/// <para><b>Legge i file, non li carica.</b> Un minuto di sei anni sono centinaia di megabyte per
/// simbolo; per sapere da quando a quando va basta la testa e la coda del file. Passare dallo store
/// dei feed caricherebbe gigabyte per rispondere a una domanda da venti righe.</para>
///
/// <para>I simboli sono l'unione di quelli che la tabella di conversione del broker mappa e di quelli
/// che hanno un feed raccolto: un simbolo raccolto ma non mappato e' proprio quello che si vuole
/// vedere, perche' non si puo' ancora operare.</para>
/// </summary>
public sealed class BrokerDataStatusService
{
    private static readonly Regex DateTimeField = new("\"dateTime\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    private readonly PiootooSettings _settings;
    private readonly WorkspaceService _workspaces;
    private readonly SymbolInfoStore _symbolInfo;

    public BrokerDataStatusService(PiootooSettings settings, WorkspaceService workspaces, SymbolInfoStore symbolInfo)
    {
        _settings = settings;
        _workspaces = workspaces;
        _symbolInfo = symbolInfo;
    }

    public BrokerDataStatus Get(string broker)
    {
        var name = ExternalDatafeedStore.NormalizeBroker(broker);
        var status = new BrokerDataStatus { Broker = name, GeneratedUtc = DateTime.UtcNow };

        var conversion = ConversionOf(name);
        var mapped = conversion.Mappings
            .Where(mapping => mapping.Enabled)
            .GroupBy(mapping => Normalize(mapping.Symbol), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().AccountSymbol, StringComparer.OrdinalIgnoreCase);
        var archives = _symbolInfo.GetArchives(name);
        var reconciliation = SymbolConversionReconciler.Reconcile(name, conversion, archives).Rows
            .GroupBy(row => Normalize(row.Symbol), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var feedFolder = Path.Combine(_settings.GetExternalRepositoryPath(), name);
        var feeds = Directory.Exists(feedFolder)
            ? Directory.EnumerateFiles(feedFolder, "@*_*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Select(file => file!.Split('_'))
                .Where(parts => parts.Length == 2 && int.TryParse(parts[1], out _))
                .GroupBy(parts => parts[0], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(parts => int.Parse(parts[1], CultureInfo.InvariantCulture)).Order().ToList(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        var spread = ReadSpread(name, out var spreadFile);
        status.SpreadFile = spreadFile;
        var spreadDays = CountSpreadDays(name);
        var swap = ReadSwap(name, out var swapFile);
        status.SwapFile = swapFile;
        var seen = archives
            .Where(archive => !string.IsNullOrWhiteSpace(archive.PiootooSymbol) && archive.Snapshots.Count > 0)
            .GroupBy(archive => Normalize(archive.PiootooSymbol!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Max(archive => archive.Snapshots[^1].LastSeenUtc), StringComparer.OrdinalIgnoreCase);

        var symbols = mapped.Keys.Concat(feeds.Keys).Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in symbols)
        {
            var row = new SymbolDataStatus
            {
                Symbol = symbol,
                BrokerSymbol = mapped.TryGetValue(symbol, out var brokerSymbol) ? brokerSymbol : null,
                Registered = InstrumentRegistry.TryGet(symbol, out _) && MarketCalendarRegistry.Current.TryGet(symbol, out _)
            };

            if (feeds.TryGetValue(symbol, out var timeframes))
            {
                if (timeframes.Contains(1))
                {
                    var (from, to) = FirstAndLast(Path.Combine(feedFolder, $"{symbol}_1.json"));
                    row.MinuteFromUtc = from;
                    row.MinuteToUtc = to;
                }

                foreach (var minutes in timeframes.Where(minutes => minutes > 1))
                {
                    row.Aggregates.Add(minutes);
                    if (!HeadContains(Path.Combine(feedFolder, $"{symbol}_{minutes}.json"), "finestra"))
                        row.AggregatesWithoutWindow.Add(minutes);
                }
            }

            if (spread.TryGetValue(symbol, out var spreadRow))
            {
                row.HasSpread = true;
                row.SpreadMedian = spreadRow.Median;
                row.SpreadFromUtc = spreadRow.From;
                row.SpreadToUtc = spreadRow.To;
            }

            row.SpreadDays = spreadDays.TryGetValue(symbol, out var days) ? days : 0;
            row.Swap = swap.TryGetValue(symbol, out var swapKind) ? swapKind : null;
            row.SymbolInfoSeenUtc = seen.TryGetValue(symbol, out var lastSeen) ? lastSeen : null;

            if (reconciliation.TryGetValue(symbol, out var check))
            {
                row.Conversion = check.Unverifiable ? "non verificabile" : check.Findings.Count > 0 ? "divergente" : "confermata";
                row.ConversionFindings = check.Findings;
            }

            if (!row.Registered) row.Missing.Add("contratto o calendario");
            if (row.BrokerSymbol is null) row.Missing.Add("riga nella tabella di conversione");
            else if (row.Conversion == "divergente") row.Missing.Add("tabella di conversione divergente dalla scheda: " + string.Join(" ", row.ConversionFindings));
            else if (row.Conversion == "non verificabile") row.Missing.Add("tabella di conversione non verificabile: " + string.Join(" ", row.ConversionFindings));
            if (row.MinuteToUtc is null) row.Missing.Add("barre da un minuto");
            if (row.AggregatesWithoutWindow.Count > 0)
                row.Missing.Add("aggregati da ricostruire (" + string.Join(",", row.AggregatesWithoutWindow) + ")");
            if (!row.HasSpread) row.Missing.Add("spread");
            if (row.Swap is null) row.Missing.Add("swap");
            row.Ready = row.Missing.Count == 0;

            status.Symbols.Add(row);
        }

        return status;
    }

    /// <summary>La tabella di conversione del broker nel registro; vuota se il broker non ne ha una.</summary>
    private SymbolConversion ConversionOf(string broker)
    {
        var entry = _workspaces.ListBrokers().FirstOrDefault(candidate =>
            string.Equals(ExternalDatafeedStore.NormalizeBroker(string.IsNullOrWhiteSpace(candidate.DatafeedFolder) ? candidate.Code : candidate.DatafeedFolder),
                broker, StringComparison.OrdinalIgnoreCase));

        return _workspaces.ResolveSymbolConversion(entry?.SymbolConversionCode);
    }

    private Dictionary<string, (string Median, DateTime? From, DateTime? To)> ReadSpread(string broker, out string? fileName)
    {
        var result = new Dictionary<string, (string, DateTime?, DateTime?)>(StringComparer.OrdinalIgnoreCase);
        fileName = null;

        var folder = Path.Combine(_settings.GetSpreadPath(), broker);
        var file = Directory.Exists(folder)
            ? new DirectoryInfo(folder).EnumerateFiles(SpreadTable.FilePattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(candidate => candidate.LastWriteTimeUtc).FirstOrDefault()
            : null;
        if (file is null)
            return result;

        fileName = file.Name;
        string[]? header = null;
        foreach (var line in File.ReadLines(file.FullName))
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
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

            result[Normalize(symbol)] = (
                Cell(header, cells, "p50Spread") ?? string.Empty,
                ParseUtc(Cell(header, cells, "firstTickUtc")),
                ParseUtc(Cell(header, cells, "lastTickUtc")));
        }

        return result;
    }

    private Dictionary<string, int> CountSpreadDays(string broker)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var folder = Path.Combine(_settings.GetSpreadPath(), broker, SpreadDailyStore.DailyFolder);
        if (!Directory.Exists(folder))
            return result;

        foreach (var path in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            var symbol = Normalize(Path.GetFileNameWithoutExtension(path).Split('_')[0]);
            // Un giorno e' una chiave "yyyy-MM-dd": contarle nel testo evita di deserializzare mesi
            // di istogrammi per un numero.
            var days = Regex.Matches(File.ReadAllText(path), "\"\\d{4}-\\d{2}-\\d{2}\"\\s*:").Count;
            result[symbol] = result.GetValueOrDefault(symbol) + days;
        }

        return result;
    }

    private Dictionary<string, string> ReadSwap(string broker, out string? fileName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        fileName = null;

        var folder = Path.Combine(_settings.GetSwapPath(), broker);
        var file = Directory.Exists(folder)
            ? new DirectoryInfo(folder).EnumerateFiles(SwapTable.FilePattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(candidate => candidate.LastWriteTimeUtc).FirstOrDefault()
            : null;
        if (file is null)
            return result;

        fileName = file.Name;
        foreach (var line in File.ReadLines(file.FullName))
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line) || line.StartsWith("broker,", StringComparison.OrdinalIgnoreCase))
                continue;

            var cells = line.Split(',');
            if (cells.Length < 7)
                continue;

            var source = cells.Length > 7 ? string.Join(',', cells.Skip(7)).Trim() : string.Empty;
            result[Normalize(cells[1])] = source.StartsWith(SwapFromSymbolInfo.AutoPrefix, StringComparison.OrdinalIgnoreCase)
                ? "auto"
                : "manuale";
        }

        return result;
    }

    /// <summary>Prima e ultima barra di un feed piatto, leggendo testa e coda del file.</summary>
    private static (DateTime? From, DateTime? To) FirstAndLast(string path)
    {
        if (!File.Exists(path))
            return (null, null);

        using var stream = File.OpenRead(path);
        var head = ReadAt(stream, 0, 4096);
        var tail = ReadAt(stream, Math.Max(0, stream.Length - 4096), 4096);

        var first = DateTimeField.Match(head);
        var last = DateTimeField.Matches(tail).LastOrDefault();
        return (first.Success ? ParseUtc(first.Groups[1].Value) : null, last is null ? null : ParseUtc(last.Groups[1].Value));
    }

    private static bool HeadContains(string path, string text)
    {
        if (!File.Exists(path))
            return false;

        using var stream = File.OpenRead(path);
        return ReadAt(stream, 0, 1024).Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadAt(FileStream stream, long offset, int count)
    {
        var buffer = new byte[count];
        stream.Seek(offset, SeekOrigin.Begin);
        var read = stream.Read(buffer, 0, count);
        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static string? Cell(string[] header, string[] cells, string column)
    {
        var index = Array.IndexOf(header, column);
        return index >= 0 && index < cells.Length ? cells[index].Trim() : null;
    }

    private static DateTime? ParseUtc(string? text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value)
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : null;

    private static string Normalize(string symbol) => "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();
}
