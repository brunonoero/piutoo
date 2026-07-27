using FeedWorker.Dto;

namespace FeedWorker.Configuration;

/// <summary>
/// Opzioni di configurazione per il repository dei file JSON delle candele
/// </summary>
public class RepositoryOptions
{
    /// <summary>
    /// Percorso base dove salvare i file JSON
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Convenzione di naming per i file JSON.
    /// Placeholder supportati:
    /// - [basepath] - percorso base
    /// - [symbol] - simbolo (es. COMEX:GC1!)
    /// - [interval] - intervallo convertito in convenzione folder (1m, 5m, 15m, 30m, 1h, 4h, D, W)
    /// - [yyyyMMdd] - data nel formato yyyyMMdd
    /// - [yyyy-MM-dd] - data nel formato yyyy-MM-dd
    /// - [HHmmss] - ora nel formato HHmmss
    /// - [HH-mm-ss] - ora nel formato HH-mm-ss
    /// Esempio: [basepath]\datafeed\[interval]\[symbol]\[symbol]-[yyyyMMdd].json
    /// Convenzione interval: 1m (1 minuto), 5m (5 minuti), 15m (15 minuti), 30m (30 minuti), 1h (1 ora), 4h (4 ore), D (Daily), W (Weekly)
    /// </summary>
    public string NameConvention { get; set; } = string.Empty;

    /// <summary>
    /// Genera il percorso completo del file basato sulla convenzione di naming
    /// </summary>
    /// <param name="symbol">Simbolo da utilizzare</param>
    /// <param name="interval">Intervallo delle candele</param>
    /// <param name="date">Data da utilizzare per i placeholder di data</param>
    /// <returns>Percorso completo del file</returns>
    public string GetFilePath(string symbol, CandleInterval interval, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(BasePath))
        {
            throw new InvalidOperationException("BasePath non è configurato");
        }

        if (string.IsNullOrWhiteSpace(NameConvention))
        {
            throw new InvalidOperationException("NameConvention non è configurata");
        }

        var path = NameConvention
            .Replace("[basepath]", BasePath)
            .Replace("[symbol]", SanitizeSymbol(symbol))
            .Replace("[interval]", interval.ToFolderName())
            .Replace("[yyyyMMdd]", date.ToString("yyyyMMdd"))
            .Replace("[yyyy-MM-dd]", date.ToString("yyyy-MM-dd"))
            .Replace("[HHmmss]", date.ToString("HHmmss"))
            .Replace("[HH-mm-ss]", date.ToString("HH-mm-ss"));

        // Normalizza il percorso per il sistema operativo
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Sanitizza il simbolo per essere usato nel percorso del file
    /// Rimuove caratteri non validi per i nomi di file/cartelle
    /// </summary>
    private static string SanitizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return "unknown";
        }

        // Sostituisce caratteri non validi con underscore
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct();
        var sanitized = symbol;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }
}
