namespace FeedWorker.Configuration;

/// <summary>
/// Rappresenta le informazioni di un simbolo con mappatura tra future e data source
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// Nome descrittivo del simbolo (es. "Gold", "Australian Dollar")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo del future usato per il naming dei file (es. "@GC", "@AD")
    /// </summary>
    public string FutureSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo del data source usato per le query API (es. "COMEX:GC1!", "FX:AUDUSD")
    /// </summary>
    public string DsSymbol { get; set; } = string.Empty;
}
