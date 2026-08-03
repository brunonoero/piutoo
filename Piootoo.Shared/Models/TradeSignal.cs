using Piootoo.Shared.Enums;
using System.Text.Json.Serialization;

namespace Piootoo.Shared.Models;

/// <summary>
/// Rappresenta un segnale di trading generato da una strategia.
///
/// <para><b>Invariante — uscita autocontenuta.</b> Un segnale di ingresso deve descrivere per
/// intero come si esce dalla posizione: <see cref="StopLoss"/> /
/// <see cref="StopLossMoneyPerFutureContract"/>, <see cref="TakeProfit"/> /
/// <see cref="TakeProfitMoneyPerFutureContract"/>,
/// <see cref="TrailingStopMoneyPerFutureContract"/>, <see cref="CloseAtUtc"/> e
/// <see cref="MaxBarsInPosition"/>. <see cref="ExitOnly"/> è l'eccezione esplicita per una
/// condizione di uscita osservabile solo a runtime: chiude la posizione opposta senza aprirne una
/// nuova.</para>
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
    /// Quantità prima della conversione dell'account, cioè quella dichiarata dalla strategia dopo
    /// l'eventuale allocazione Titano. <see cref="Quantity"/> è la stessa grandezza dopo la
    /// conversione: tenerle entrambe evita che un consumatore riapplichi i fattori già applicati.
    /// </summary>
    public decimal QuantityBeforeAccountConversion { get; set; }

    /// <summary>
    /// Chiude esclusivamente la posizione esistente nel verso indicato da <see cref="Type"/>,
    /// senza aprirne una opposta. Serve alle strategie il cui exit dipende da una condizione
    /// osservabile solo a runtime, come un reverse crossover.
    /// </summary>
    public bool ExitOnly { get; set; }

    /// <summary>
    /// Ordine richiesto dalla strategia. La strategia descrive l'intent; il
    /// broker/engine decide il fill e mantiene l'eventuale ordine pendente.
    /// </summary>
    public TradeOrderType OrderType { get; set; } = TradeOrderType.Market;

    /// <summary>Da quando l'ordine può essere attivato. Null = barra corrente.</summary>
    public DateTime? ValidFromUtc { get; set; }

    /// <summary>Scadenza dell'ordine pendente. Null = policy dell'engine.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Massimo numero di fill consentiti per <see cref="EntrySessionStartUtc"/>.
    /// Il limite è applicato dall'engine al fill, non alla generazione del
    /// segnale, così un ordine stop non eseguito può essere riemesso.
    /// </summary>
    public int? MaxEntriesPerSession { get; set; }

    /// <summary>
    /// Inizio UTC della sessione a cui attribuire il fill per
    /// <see cref="MaxEntriesPerSession"/>. Deve essere valorizzato insieme al
    /// limite; non descrive una sessione del broker.
    /// </summary>
    public DateTime? EntrySessionStartUtc { get; set; }

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
    /// Profitto minimo in USD per singolo contratto futures necessario per
    /// spostare lo stop al prezzo di ingresso. Alternativa monetaria a
    /// <see cref="BreakEven"/>, che resta espressa in punti.
    /// </summary>
    public decimal? BreakEvenMoneyPerFutureContract { get; set; }

    /// <summary>
    /// Distanza del trailing stop in USD per singolo contratto futures. È
    /// relativa al massimo/minimo favorevole raggiunto dopo il fill, non al
    /// prezzo del segnale né a un valore CFD.
    /// </summary>
    public decimal? TrailingStopMoneyPerFutureContract { get; set; }

    /// <summary>
    /// Numero massimo di barre da mantenere in posizione.
    /// Se null o 0, nessun limite temporale definito.
    /// </summary>
    public int? MaxBarsInPosition { get; set; }

    /// <summary>
    /// Condiziona la chiusura di <see cref="CloseAtUtc"/> all'utile aperto: alla deadline la
    /// posizione viene chiusa <b>solo se</b> l'utile per contratto è inferiore a questa soglia,
    /// altrimenti resta aperta.
    ///
    /// <para>Esprime due forme ricorrenti nei sorgenti EasyLanguage che sembrano opposte ma sono
    /// la stessa regola: "se a quest'ora sei sotto, esci" (soglia 0) e "esci a quest'ora, a meno
    /// che l'utile non abbia già raggiunto X" (soglia X, che lascia correre il vincente).</para>
    ///
    /// <para>È in denaro per contratto di riferimento, come stop e target.</para>
    /// </summary>
    public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; set; }

    /// <summary>
    /// Da questo istante in poi la posizione viene chiusa quando l'utile aperto <b>smette di
    /// migliorare</b>: l'engine memorizza il massimo osservato dopo la deadline e chiude alla
    /// prima barra in cui l'utile corrente non lo supera.
    ///
    /// <para>Traduce l'uscita "max days in trade" con stallo dell'utile: può chiudere in perdita,
    /// in leggero utile o tenere un vincente finché continua a salire, e quindi non è
    /// riducibile a un take profit.</para>
    /// </summary>
    public DateTime? ProfitStallAfterUtc { get; set; }

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
