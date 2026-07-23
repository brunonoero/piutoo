using Piootoo.Shared.Enums;
using System.Text.Json.Serialization;

namespace Piootoo.Shared.Models;

/// <summary>
/// Rappresenta un segnale di trading generato da una strategia
/// </summary>
public class TradeSignal
{
    /// <summary>
    /// Timestamp del segnale in UTC (allineato al feed JSON).
    /// </summary>
    public DateTime Date { get; set; }
    public SignalType Type { get; set; }
    public decimal Price { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StrategyCode { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal Quantity { get; set; } = 1m;

    /// <summary>
    /// Ordine richiesto dalla strategia. La strategia descrive l'intent; il
    /// broker/engine decide il fill e mantiene l'eventuale ordine pendente.
    /// </summary>
    public TradeOrderType OrderType { get; set; } = TradeOrderType.Market;

    /// <summary>Da quando l'ordine può essere attivato. Null = barra corrente.</summary>
    public DateTime? ValidFromUtc { get; set; }

    /// <summary>Scadenza dell'ordine pendente. Null = policy dell'engine.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Deadline in cui l'engine deve chiudere l'eventuale posizione.</summary>
    public DateTime? CloseAtUtc { get; set; }

    /// <summary>
    /// Perdita massima in USD per singolo contratto futures. È relativa al
    /// fill effettivo, non un prezzo assoluto né un valore CFD.
    /// </summary>
    public decimal? StopLossMoneyPerFutureContract { get; set; }

    /// <summary>
    /// Profitto target in USD per singolo contratto futures. È relativo al
    /// fill effettivo, non un prezzo assoluto né un valore CFD.
    /// </summary>
    public decimal? TakeProfitMoneyPerFutureContract { get; set; }

    /// <summary>
    /// Copia del requisito dichiarato dalla strategia. Quando è true e
    /// <see cref="CloseOnly"/> è true, l'engine/cTrader esegue la chiusura solo
    /// dopo avere verificato una posizione reale per StrategyCode e Symbol.
    /// </summary>
    public bool IsPositionCloseDependent { get; set; }
    
    /// <summary>
    /// Stop Loss in punti (valore assoluto, non percentuale)
    /// Se null, nessuno stop loss definito
    /// </summary>
    public decimal? StopLoss { get; set; }
    
    /// <summary>
    /// Take Profit in punti (valore assoluto, non percentuale)
    /// Se null, nessun take profit definito
    /// </summary>
    public decimal? TakeProfit { get; set; }
    
    /// <summary>
    /// Break Even level in punti (valore assoluto)
    /// Quando il profitto raggiunge questo livello, lo stop loss viene spostato al prezzo di entry
    /// Se null, nessun break even definito
    /// </summary>
    public decimal? BreakEven { get; set; }

    /// <summary>
    /// Numero massimo di barre da mantenere in posizione.
    /// Se null o 0, nessun limite temporale definito.
    /// </summary>
    public int? MaxBarsInPosition { get; set; }

    /// <summary>
    /// Se true, il segnale serve solo a chiudere una posizione esistente e non deve aprire la direzione opposta.
    /// </summary>
    public bool CloseOnly { get; set; }

    /// <summary>
    /// Intent aggiuntivi generati sulla stessa barra (es. stop long e short
    /// contemporanei). L'engine li tratta come segnali indipendenti.
    /// </summary>
    [JsonIgnore]
    public List<TradeSignal>? CompanionSignals { get; set; }

    /// <summary>
    /// Stato tecnico restituito da adapter legacy e memorizzato dall'engine.
    /// Non viene serializzato né inviato a cTrader.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, object?>? RuntimeState { get; set; }
}
