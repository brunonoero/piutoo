using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Core.Services;

/// <summary>
/// L'anagrafica delle misure di spread. Il caricamento vero sta in <see cref="SpreadTable"/> — qui
/// non si duplica: <see cref="GetTable"/> carica esattamente come farebbe il backtest, cosi' il
/// preventivo che la console mostra e il numero che il run applica non possono divergere. Un
/// preventivo calcolato da un secondo lettore sarebbe la cosa peggiore da avere: darebbe fiducia a
/// un numero che poi non e' quello.
/// </summary>
public sealed class SpreadCatalog : ISpreadCatalog
{
    private readonly PiootooSettings _settings;

    public SpreadCatalog(PiootooSettings settings) => _settings = settings;

    public IReadOnlyList<SpreadBrokerInfo> GetBrokers()
    {
        var root = _settings.GetSpreadPath();
        if (!Directory.Exists(root))
            return [];

        var brokers = new List<SpreadBrokerInfo>();

        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            var file = new DirectoryInfo(folder)
                .EnumerateFiles(SpreadTable.FilePattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
                .FirstOrDefault();

            // Una cartella senza il file per simbolo non e' un broker misurato: non si elenca, cosi'
            // non si puo' sceglierlo e vedersi rifiutare l'avvio.
            if (file is null)
                continue;

            brokers.Add(new SpreadBrokerInfo
            {
                Broker = Path.GetFileName(folder)!,
                SymbolCount = CountSymbols(file.FullName),
                HasHourly = File.Exists(SpreadTable.HourFilePathOf(file)),
                FileName = file.Name,
                LastWriteUtc = file.LastWriteTimeUtc
            });
        }

        return brokers
            .OrderBy(broker => broker.Broker, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public SpreadTableInfo GetTable(string broker, SpreadStatistic statistic, SpreadResolution resolution)
    {
        var table = SpreadTable.Load(_settings.GetSpreadPath(), broker, statistic, resolution);

        var symbols = new List<SpreadSymbolInfo>(table.Points.Count);
        foreach (var (symbol, points) in table.Points.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!table.PointsByHour.TryGetValue(symbol, out var hours))
            {
                symbols.Add(new SpreadSymbolInfo { Symbol = symbol, Points = points, HasHours = false });
                continue;
            }

            var minAt = 0;
            var maxAt = 0;
            var fallback = 0;
            for (var hour = 0; hour < hours.Length; hour++)
            {
                if (hours[hour] < hours[minAt])
                    minAt = hour;
                if (hours[hour] > hours[maxAt])
                    maxAt = hour;

                // Un'ora identica alla costante e' quasi sempre un'ora ripiegata. Non e' una prova —
                // un'ora puo' misurare davvero lo stesso valore — ma il conteggio serve a leggere la
                // riga, non a decidere qualcosa, e l'avviso della tabella resta la fonte esatta.
                if (hours[hour] == points)
                    fallback++;
            }

            symbols.Add(new SpreadSymbolInfo
            {
                Symbol = symbol,
                Points = points,
                HasHours = true,
                HourMin = hours[minAt],
                HourMinAt = minAt,
                HourMax = hours[maxAt],
                HourMaxAt = maxAt,
                FallbackHours = fallback
            });
        }

        return new SpreadTableInfo
        {
            Broker = table.Broker,
            Source = table.Describe(),
            Column = table.Column,
            Resolution = table.Resolution,
            Warnings = table.Warnings,
            Symbols = symbols
        };
    }

    /// <summary>
    /// Quante righe di dati ha il file, senza caricarlo: l'elenco dei broker si apre a ogni
    /// ingresso nella schermata e non deve leggere venti distribuzioni per dire "16 simboli".
    /// </summary>
    private static int CountSymbols(string path)
    {
        var rows = 0;
        var header = false;

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (!header)
            {
                header = true;
                continue;
            }

            rows++;
        }

        return rows;
    }
}
