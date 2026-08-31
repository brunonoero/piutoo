using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Services;

/// <summary>
/// Elenco degli archivi di barre disponibili: il datafeed interno (<c>datafeed/</c>) e gli archivi
/// esterni, uno per broker, sotto <c>datafeed-external/{BROKER}/</c>.
///
/// <para>Le due strutture sono identiche — file piatti <c>@SYM_{minuti}.json</c> piu'
/// <c>feed-clocks.json</c> — quindi <see cref="Piootoo.Domain.Repositories.DataSourceRepository"/>
/// le legge senza sapere quale delle due sta guardando. L'unica cosa che cambia e' la radice, ed e'
/// questa classe a deciderla.</para>
///
/// <para>Non si mescolano: un run legge tutto da una sola radice. Due broker chiudono le stesse
/// candele su prezzi diversi, e un backtest a cavallo delle due non corrisponderebbe a nessun conto
/// reale.</para>
/// </summary>
public sealed class DatafeedCatalog : IDatafeedCatalog
{
    /// <summary>Etichetta della sorgente interna negli artefatti e nei log.</summary>
    public const string InternalLabel = "interno";

    private readonly PiootooSettings _settings;

    public DatafeedCatalog(PiootooSettings settings) => _settings = settings;

    public IReadOnlyList<DatafeedBrokerInfo> GetBrokers()
    {
        var root = _settings.GetExternalRepositoryPath();
        if (!Directory.Exists(root))
            return Array.Empty<DatafeedBrokerInfo>();

        var brokers = new List<DatafeedBrokerInfo>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            // Cartelle di servizio dei bot raccoglitori (.pending) e simili: non sono broker.
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith('.'))
                continue;

            // Solo il livello superiore: le sottocartelle (ticks/) non contengono feed a barre.
            var feeds = Directory.EnumerateFiles(directory, "@*_*.json", SearchOption.TopDirectoryOnly)
                .ToList();
            if (feeds.Count == 0)
                continue;

            brokers.Add(new DatafeedBrokerInfo
            {
                Broker = name,
                FeedCount = feeds.Count,
                SymbolCount = feeds
                    .Select(path => SymbolOf(Path.GetFileNameWithoutExtension(path)))
                    .Where(symbol => symbol != null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                LastWriteUtc = feeds.Max(File.GetLastWriteTimeUtc)
            });
        }

        return brokers
            .OrderBy(broker => broker.Broker, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<DatafeedFeedInfo> GetFeeds(string? broker)
    {
        var root = ResolveRoot(broker);
        if (!Directory.Exists(root))
            return Array.Empty<DatafeedFeedInfo>();

        var source = Describe(broker);

        // L'orologio del feed si legge una volta per archivio, non una per file. Un manifest
        // assente non fa fallire l'elenco: i feed compaiono comunque, col fuso vuoto e il
        // periodo etichettato come sta scritto nel file. E' un elenco, non una lettura di barre:
        // il rifiuto esplicito resta dove serve, cioe' quando un run prova a caricarle.
        FeedClockRegistry? clocks = null;
        try
        {
            clocks = FeedClockRegistry.Load(root);
        }
        catch (FeedClockNotDeclaredException)
        {
        }

        var feeds = new List<DatafeedFeedInfo>();
        foreach (var path in Directory.EnumerateFiles(root, "@*_*.json", SearchOption.TopDirectoryOnly))
        {
            var parsed = ParseFlatFileName(Path.GetFileNameWithoutExtension(path));
            if (parsed == null)
                continue;

            var (symbol, timeframeMinutes) = parsed.Value;
            var clock = clocks != null && clocks.IsDeclared(symbol) ? clocks.For(symbol) : null;
            var range = FlatFeedProbe.Read(path);
            var file = new FileInfo(path);

            feeds.Add(new DatafeedFeedInfo
            {
                Broker = string.IsNullOrWhiteSpace(broker) ? null : broker.Trim(),
                Source = source,
                Symbol = symbol,
                TimeframeMinutes = timeframeMinutes,
                FirstBarUtc = ToTrueUtc(range.First, clock),
                LastBarUtc = ToTrueUtc(range.Last, clock),
                CandleCount = range.CandleCount,
                FeedClock = clock?.TimeZoneId,
                LastWriteUtc = file.LastWriteTimeUtc,
                SizeBytes = file.Length,
                Problem = range.Problem ?? (clock == null
                    ? $"Il feed '{symbol}' non dichiara il proprio fuso in {FeedClockRegistry.ManifestFileName}: "
                      + "il periodo qui e' l'etichetta grezza del file, e un backtest si rifiuterebbe di partire."
                    : null)
            });
        }

        return feeds
            .OrderBy(feed => feed.Symbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(feed => feed.TimeframeMinutes)
            .ToList();
    }

    public IReadOnlyList<DatafeedFeedInfo> GetAllFeeds()
    {
        var feeds = new List<DatafeedFeedInfo>(GetFeeds(null));
        foreach (var broker in GetBrokers())
            feeds.AddRange(GetFeeds(broker.Broker));

        return feeds;
    }

    /// <summary>
    /// Istante vero di un timestamp stampato nel file. Senza orologio dichiarato non c'e' niente
    /// da cui convertire: il valore resta l'etichetta del file, e <c>Problem</c> lo dice.
    /// </summary>
    private static DateTime? ToTrueUtc(DateTime? feedWallClock, SessionClock? clock)
    {
        if (feedWallClock == null)
            return null;

        return clock == null
            ? DateTime.SpecifyKind(feedWallClock.Value, DateTimeKind.Utc)
            : clock.ToUtc(feedWallClock.Value);
    }

    /// <summary>
    /// Simbolo e minuti di un file piatto <c>@SYM_{minuti}</c>. Null se il nome non segue la
    /// convenzione: un file estraneo lasciato nella cartella si ignora, non diventa una riga.
    /// </summary>
    private static (string Symbol, int TimeframeMinutes)? ParseFlatFileName(string fileNameWithoutExtension)
    {
        var symbol = SymbolOf(fileNameWithoutExtension);
        if (symbol == null)
            return null;

        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        return int.TryParse(fileNameWithoutExtension[(lastUnderscore + 1)..], out var minutes)
            ? (symbol, minutes)
            : null;
    }

    public string ResolveRoot(string? broker)
    {
        if (string.IsNullOrWhiteSpace(broker))
            return _settings.GetRepositoryPath();

        var name = broker.Trim();

        // Il nome arriva da una richiesta HTTP: deve restare un nome di cartella, non diventare un
        // percorso. Senza questo controllo un "..\..\qualcosa" leggerebbe fuori dal repository.
        if (name.Length == 0
            || name.Contains(Path.DirectorySeparatorChar)
            || name.Contains(Path.AltDirectorySeparatorChar)
            || name.Contains(':')
            || name == "."
            || name == ".."
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                $"'{broker}' non e' un nome di broker valido: deve essere il nome di una cartella " +
                "sotto datafeed-external.",
                nameof(broker));
        }

        var path = Path.Combine(_settings.GetExternalRepositoryPath(), name);
        if (!Directory.Exists(path))
        {
            var available = GetBrokers().Select(item => item.Broker).ToList();
            throw new DirectoryNotFoundException(
                $"Il datafeed esterno del broker '{name}' non esiste. " +
                (available.Count == 0
                    ? $"Nessun broker disponibile in {_settings.GetExternalRepositoryPath()}."
                    : $"Disponibili: {string.Join(", ", available)}."));
        }

        return path;
    }

    public string Describe(string? broker)
        => string.IsNullOrWhiteSpace(broker) ? InternalLabel : $"esterno/{broker.Trim()}";

    /// <summary>Simbolo di un file piatto <c>@SYM_{minuti}</c>, null se il nome non segue la convenzione.</summary>
    private static string? SymbolOf(string fileNameWithoutExtension)
    {
        if (!fileNameWithoutExtension.StartsWith('@'))
            return null;

        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        return lastUnderscore > 1 ? fileNameWithoutExtension[..lastUnderscore] : null;
    }
}
