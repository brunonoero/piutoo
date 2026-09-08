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
    /// Distanza in punti fra il massimo/minimo favorevole osservato e lo stop
    /// dinamico. Null = nessun trailing stop.
    /// </summary>
    public decimal? TrailingStop { get; set; }

    /// <summary>
    /// Massimo prezzo favorevole raggiunto dalla posizione long o minimo prezzo
    /// favorevole raggiunto dalla posizione short. È mantenuto dall'engine per
    /// calcolare il trailing stop senza richiamare la strategia.
    /// </summary>
    public decimal? PeakFavorablePrice { get; set; }

    /// <summary>
    /// Numero massimo di barre da mantenere in posizione.
    /// </summary>
    public int? MaxBarsInPosition { get; set; }

    /// <summary>
    /// Orario assoluto di flat. L'engine chiude la posizione al primo aggiornamento di mercato
    /// uguale o successivo. Puo' venire dalla strategia o dal piano: vedi
    /// <see cref="TimeExitFromAccountPolicy"/>.
    /// </summary>
    public DateTime? CloseAtUtc { get; set; }

    /// <summary>
    /// <see cref="CloseAtUtc"/> e' il flat di sessione del piano e non la deadline della strategia.
    /// L'uscita viene registrata come <c>SessionFlat</c> invece che <c>TimeExit</c>.
    /// </summary>
    public bool TimeExitFromAccountPolicy { get; set; }

    /// <summary>
    /// Timeframe in minuti della strategia che ha emesso il segnale. È l'unità in cui
    /// <see cref="MaxBarsInPosition"/> è espresso, quindi l'unità in cui
    /// <see cref="BarsInPosition"/> deve contare: l'orologio del backtest gira al timeframe
    /// <i>minimo</i> del portafoglio e non è quello della strategia. Null = sconosciuto, e il
    /// conteggio ricade sulle barre consegnate.
    /// </summary>
    public int? TimeframeMinutes { get; set; }

    /// <summary>
    /// Barre della strategia trascorse dalla barra di ingresso. Conta i <b>bucket del calendario</b>
    /// in cui il simbolo ha davvero stampato una barra e in cui il calendario dichiara una
    /// sessione: vedi <see cref="LastCountedBucketUtc"/>.
    /// </summary>
    public int BarsInPosition { get; set; }

    /// <summary>
    /// Inizio dell'ultimo bucket già contato in <see cref="BarsInPosition"/>, sulla griglia di
    /// <see cref="TimeframeMinutes"/>. È il campo che rende il conteggio idempotente rispetto ai
    /// tick dell'orologio: un bucket della strategia contiene più barre dell'orologio e va contato
    /// una volta sola.
    /// </summary>
    public DateTime? LastCountedBucketUtc { get; set; }


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
