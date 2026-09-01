using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Risultato completo di un backtesting
/// </summary>
public class BacktestingResult
{
    public string JobId { get; set; } = string.Empty;
    public string SetupName { get; set; } = string.Empty;
    public string SetupId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    
    /// <summary>
    /// Data e ora di creazione del risultato del backtesting
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Risultati globali per ora
    public List<HourlyResult> HourlyResults { get; set; } = new();
    
    // Risultati per strategia per ora
    public List<StrategyHourlyResult> StrategyResults { get; set; } = new();
    
    // Aggregati settimanali
    public List<WeeklyResult> WeeklyResults { get; set; } = new();
    
    // Metriche globali
    public decimal FinalEquity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal TotalReturn { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    
    // Lista strategie utilizzate (mantenuta per retrocompatibilità)
    public List<string> StrategiesUsed { get; set; } = new();
    
    // Lista dettagliata strategie utilizzate con symbol e timeframe
    public List<StrategyInfo> StrategiesInfo { get; set; } = new();
    
    // File path dove è salvato
    public string? ResultFilePath { get; set; }

    // File HTML con andamento equity per strategia
    public string? HtmlReportFilePath { get; set; }

    // File JSON con tutti i TradeSignal emessi dalle strategie
    public string? TradeSignalsFilePath { get; set; }

    /// <summary>
    /// Log eventi JSONL del run (segnali, ingressi, uscite, anomalie).
    /// Vedi <see cref="Trading.BacktestDiagnosticsSchema"/>.
    /// </summary>
    public string? DiagnosticsLogFilePath { get; set; }

    /// <summary>
    /// Riepilogo diagnostico del run: contatori per strategia e diagnosi automatiche.
    /// È il file da leggere per capire perché un backtest non ha prodotto trade.
    /// </summary>
    public string? DiagnosticsSummaryFilePath { get; set; }

    /// <summary>
    /// Timestamp UTC dell'ultima barra realmente presente nel datafeed, cioè il punto oltre il
    /// quale l'orologio del backtest continua a girare ma non arriva più alcun prezzo.
    /// </summary>
    /// <remarks>
    /// È il massimo fra le ultime barre dei datasource caricati: oltre quel punto NESSUN feed ha
    /// più dati, quindi l'equity resta piatta per costruzione. I resoconti annuale e mensile lo
    /// usano per non stampare periodi vuoti che sembrerebbero mesi senza operatività, quando in
    /// realtà sono mesi senza dati — la differenza è la stessa segnalata da
    /// <c>coversRequestedRange</c> nel summary diagnostico.
    /// <para><c>null</c> quando la copertura è ignota: in quel caso non si tronca nulla.</para>
    /// </remarks>
    public DateTime? DataCoverageEndUtc { get; set; }

    /// <summary>
    /// Da quale archivio di barre ha letto il run: il datafeed interno del vendor oppure i CFD di
    /// un broker sotto <c>datafeed-external/{BROKER}/</c>. E' lo stesso valore dichiarato da
    /// <c>origin.json</c> e da <c>backtest-summary.json</c>, replicato qui perche' il report HTML
    /// lo stampa sotto il titolo: una curva di equity senza il feed che l'ha prodotta non e'
    /// confrontabile con nessun'altra, e mesi dopo non c'e' altro modo di accorgersene.
    /// <para><c>null</c> nei risultati ricostruiti da cartelle precedenti al campo.</para>
    /// </summary>
    public RunPriceSource? PriceSource { get; set; }
}
