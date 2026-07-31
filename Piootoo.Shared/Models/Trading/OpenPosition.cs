using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Posizione aperta nel trading emulator
/// </summary>
public class OpenPosition
{
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyCode { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public SignalType Direction { get; set; }
    public decimal EntryPrice { get; set; }
    public DateTime EntryTime { get; set; }
    /// <summary>
    /// Quantità effettivamente aperta. Può essere frazionaria per strumenti o
    /// conversioni account che consentono lotti decimali (es. 0,01).
    /// </summary>
    public decimal Contracts { get; set; } = 1m;
    public decimal ContractPointValue { get; set; } = 1m;
    
    /// <summary>
    /// Stop Loss in punti dal prezzo di entry
    /// </summary>
    public decimal? StopLoss { get; set; }
    
    /// <summary>
    /// Take Profit in punti dal prezzo di entry
    /// </summary>
    public decimal? TakeProfit { get; set; }
    
    /// <summary>
    /// Break Even level in punti dal prezzo di entry
    /// Quando il profitto raggiunge questo livello, lo stop loss viene spostato al prezzo di entry
    /// </summary>
    public decimal? BreakEven { get; set; }

    /// <summary>
    /// Numero massimo di barre da mantenere in posizione.
    /// </summary>
    public int? MaxBarsInPosition { get; set; }

    /// <summary>
    /// Orario assoluto di flat richiesto dalla strategia. L'engine chiude la
    /// posizione al primo aggiornamento di mercato uguale o successivo.
    /// </summary>
    public DateTime? CloseAtUtc { get; set; }

    /// <summary>
    /// Barre trascorse dalla barra di ingresso.
    /// </summary>
    public int BarsInPosition { get; set; }

    /// <summary>
    /// Ultima barra processata per il conteggio temporale.
    /// </summary>
    public DateTime? LastProcessedBarTime { get; set; }
    
    /// <summary>
    /// Indica se il break even è stato attivato (stop loss spostato a entry price)
    /// </summary>
    public bool BreakEvenActivated { get; set; }

    /// <summary>
    /// Soglia di utile per contratto sotto la quale la chiusura a
    /// <see cref="CloseAtUtc"/> viene eseguita. Se l'utile alla deadline è pari o superiore, la
    /// posizione resta aperta.
    /// </summary>
    public decimal? TimeExitOnlyIfProfitBelowMoneyPerContract { get; set; }

    /// <summary>
    /// Istante da cui l'engine sorveglia lo stallo dell'utile aperto.
    /// </summary>
    public DateTime? ProfitStallAfterUtc { get; set; }

    /// <summary>
    /// Massimo utile per contratto osservato dopo <see cref="ProfitStallAfterUtc"/>. Null finché
    /// la deadline non è stata superata; è la memoria che rende l'uscita per stallo eseguibile
    /// dall'engine senza interrogare la strategia.
    /// </summary>
    public decimal? PeakProfitAfterStallDeadline { get; set; }
}
