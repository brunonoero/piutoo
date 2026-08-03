using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

public static class TradingPersistenceSchema
{
    public const int Version = 2;
    public const string SignalsFileName = "signals.json";
    public const string TradesFileName = "trades.json";
}

public sealed class PersistedSignal
{
    public int SchemaVersion { get; init; } = TradingPersistenceSchema.Version;
    public required string SignalId { get; init; }
    public string? IntentId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public SignalType Side { get; init; }
    public TradeOrderType OrderType { get; init; }
    public decimal TriggerPrice { get; init; }

    /// <summary>
    /// Quantità da inoltrare: la conversione dell'account (<see cref="ContractMultiplier"/> e
    /// <see cref="AccountBalanceScale"/>) è <b>già applicata</b>. Riapplicare quei fattori qui è
    /// un errore; per la grandezza a monte c'è
    /// <see cref="QuantityBeforeAccountConversion"/>.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>Quantità dichiarata dalla strategia, prima della conversione dell'account.</summary>
    public decimal QuantityBeforeAccountConversion { get; init; }

    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; } = 1m;
    public decimal MarketVolatilityMultiplier { get; init; } = 1m;
    public decimal PortfolioRiskMultiplier { get; init; } = 1m;
    public decimal FinalQuantity { get; init; }
    public string? SizingReason { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }

    /// <summary>Stop loss in punti dal fill. Null se la strategia ha dichiarato solo la forma monetaria.</summary>
    public decimal? StopLoss { get; init; }

    /// <summary>Take profit in punti dal fill. Null se la strategia ha dichiarato solo la forma monetaria.</summary>
    public decimal? TakeProfit { get; init; }

    /// <summary>
    /// Perdita massima in USD per singolo contratto futures, relativa al fill.
    /// Alternativa a <see cref="StopLoss"/>; l'engine la converte in punti con
    /// <c>DollarsPerPoint</c> del simbolo.
    /// </summary>
    public decimal? StopLossMoneyPerFutureContract { get; init; }

    /// <summary>
    /// Profitto target in USD per singolo contratto futures, relativa al fill.
    /// Alternativa a <see cref="TakeProfit"/>; l'engine la converte in punti con
    /// <c>DollarsPerPoint</c> del simbolo.
    /// </summary>
    public decimal? TakeProfitMoneyPerFutureContract { get; init; }

    /// <summary>Livello di break even in punti dal fill. Null = nessun break even.</summary>
    public decimal? BreakEven { get; init; }

    /// <summary>Profitto in USD per contratto necessario per attivare il break even.</summary>
    public decimal? BreakEvenMoneyPerFutureContract { get; init; }

    /// <summary>
    /// Distanza del trailing stop in USD per singolo contratto futures. Il
    /// consumer la converte in punti con il valore del contratto.
    /// </summary>
    public decimal? TrailingStopMoneyPerFutureContract { get; init; }

    /// <summary>
    /// Distanza del trailing stop in punti dal picco favorevole. È la forma
    /// pronta per l'esecuzione quando il server ha già risolto lo strumento.
    /// </summary>
    public decimal? TrailingStop { get; init; }

    /// <summary>Timeframe della strategia che ha generato il segnale.</summary>
    public int TimeframeMinutes { get; init; }

    public DateTime? TimeExitUtc { get; init; }

    /// <summary>Numero massimo di barre in posizione dichiarato dal segnale. Null = nessun limite.</summary>
    public int? MaxBarsInPosition { get; init; }

    /// <summary>
    /// Soglia di utile per contratto sotto la quale la chiusura a <see cref="TimeExitUtc"/> viene
    /// eseguita. Persistita perché senza di essa, rileggendo <c>signals.json</c>, non si
    /// distinguerebbe una chiusura a tempo condizionata da una incondizionata.
    /// </summary>
    public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; init; }

    /// <summary>Istante da cui sorvegliare lo stallo dell'utile aperto. Null = nessuna uscita per stallo.</summary>
    public DateTime? ProfitStallAfterUtc { get; init; }

    /// <summary>Account la cui tabella di conversione ha generato il segnale. Vuoto = nessuna conversione.</summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    /// Simbolo con cui l'engine esterno deve inoltrare l'ordine (es. <c>USDTEC</c>). Coincide con
    /// <see cref="Symbol"/> quando l'account non ha una mappatura per quel simbolo.
    /// </summary>
    public string AccountSymbol { get; init; } = string.Empty;

    /// <summary>Fattore di scala contratto applicato dalla tabella di conversione dell'account.</summary>
    public decimal ContractMultiplier { get; init; } = 1m;

    /// <summary>
    /// Rapporto fra il capitale dell'account e il milione di riferimento, l'altro fattore della
    /// conversione insieme a <see cref="ContractMultiplier"/>.
    /// </summary>
    public decimal AccountBalanceScale { get; init; } = 1m;

    public string? Reason { get; init; }

    /// <summary>
    /// Vero solo per la registrazione di una chiusura eseguita dal client (SL/TP nativi, uscita a
    /// tempo, limite barre). Le strategie non emettono segnali di chiusura.
    /// </summary>
    public bool IsClose { get; init; }
    public OrderIntentStatus? Status { get; init; }
    public decimal FilledQuantity { get; init; }
    public string? ExternalOrderId { get; init; }
    /// <summary>Account cTrader a cui è stato assegnato (multi-account/gruppi); null in modalità legacy.</summary>
    public string? AssignedAccountNumber { get; init; }
    /// <summary>Gruppo (prop firm) dell'account assegnato.</summary>
    public string? AssignedGroupId { get; init; }
}

public sealed class PersistedTrade
{
    public int SchemaVersion { get; init; } = TradingPersistenceSchema.Version;
    public required string TradeId { get; init; }
    public string? OrderId { get; init; }
    public string? IntentId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public SignalType Direction { get; init; }
    public decimal Quantity { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal ExitPrice { get; init; }
    public string? ExitReason { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal Commission { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    /// <summary>Account cTrader che ha eseguito il trade (multi-account/gruppi); null in modalità legacy.</summary>
    public string? AccountNumber { get; init; }
}
