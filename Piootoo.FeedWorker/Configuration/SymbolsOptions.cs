using FeedWorker.Dto;
using System.Diagnostics;

namespace FeedWorker.Configuration;

/// <summary>
/// Configurazione simboli: lista usata dal worker e lista completa
/// </summary>
public class SymbolConfigOptions
{
    /// <summary>
    /// Lista simboli usata dal worker per il polling
    /// </summary>
    public List<SymbolInfo> Symbols { get; set; } = new();

    /// <summary>
    /// Lista completa dei simboli (riferimento)
    /// </summary>
    public List<SymbolInfo> SymbolsComplete { get; set; } = new();
}

/// <summary>
/// Opzioni di configurazione per i simboli da monitorare
/// </summary>
public class SymbolsOptions
{
    /// <summary>
    /// Configurazione liste simboli (completa e short)
    /// </summary>
    public SymbolConfigOptions SymbolConfig { get; set; } = new();

    /// <summary>
    /// Array di intervalli delle candele da scaricare (es. "OneMinute", "OneHour", "OneDay", "OneWeek")
    /// </summary>
    public List<string> Intervals { get; set; } = new() { "OneHour" };

    /// <summary>
    /// Cron expression per schedulare il polling dei dati.
    /// Formato: minuto ora giorno mese giorno-settimana
    /// Esempi:
    /// - "0 * * * *" - ogni ora all'inizio dell'ora
    /// - "0 */6 * * *" - ogni 6 ore
    /// - "0 0 * * *" - ogni giorno a mezzanotte
    /// - "*/15 * * * *" - ogni 15 minuti
    /// </summary>
    public string CronExpression { get; set; } = "0 * * * *"; // Default: ogni ora

    /// <summary>
    /// Numero di candele da recuperare quando si usa l'endpoint realtime.
    /// Default: 100 candele
    /// </summary>
    public int CandleLimit { get; set; } = 100;

    /// <summary>
    /// Lista simboli usata dal worker (SymbolConfig.Symbols)
    /// </summary>
    public List<SymbolInfo> SymbolsForWorker => SymbolConfig?.Symbols ?? new();

    /// <summary>
    /// Converte l'array di stringhe Intervals in lista di CandleInterval enum
    /// </summary>
    public List<CandleInterval> GetCandleIntervals()
    {
        var result = new List<CandleInterval>();

        if (Intervals == null || Intervals.Count == 0)
        {
            Debug.WriteLine($"[GetCandleIntervals] Intervals è null o vuoto, ritorno default OneHour");
            result.Add(CandleInterval.OneHour);
            return result;
        }

        foreach (var interval in Intervals)
        {
            if (string.IsNullOrWhiteSpace(interval))
                continue;

            Debug.WriteLine($"[GetCandleIntervals] Tentativo di parsing di: '{interval}'");

            if (Enum.TryParse<CandleInterval>(interval, ignoreCase: true, out var parsed))
            {
                Debug.WriteLine($"[GetCandleIntervals] Parsing riuscito: {parsed}");
                result.Add(parsed);
            }
            else
            {
                Debug.WriteLine($"[GetCandleIntervals] Parsing fallito per '{interval}', ignorato");
            }
        }

        // Se nessun intervallo valido, ritorna default
        if (result.Count == 0)
        {
            Debug.WriteLine($"[GetCandleIntervals] Nessun intervallo valido trovato, ritorno default OneHour");
            result.Add(CandleInterval.OneHour);
        }

        return result;
    }
}
