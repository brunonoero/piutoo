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
