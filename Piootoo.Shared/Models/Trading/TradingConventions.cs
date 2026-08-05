namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Costanti di convenzione condivise fra backtest interno, sessioni e client.
/// </summary>
public static class TradingConventions
{
    /// <summary>
    /// Capitale rispetto al quale le strategie dichiarano le proprie quantità: un segnale da un
    /// contratto vale un contratto su un conto da un milione.
    ///
    /// <para>Serve a due posti che devono restare d'accordo: è il capitale iniziale proposto dal
    /// backtest interno (dove la size resta quella dichiarata dalla strategia, cioè 1) ed è il
    /// denominatore di <c>AccountSymbolConversion.BalanceScale</c>, che nelle sessioni riporta
    /// quella size al saldo reale del conto. Due letterali separati si sarebbero disallineati senza
    /// produrre alcun errore: solo percentuali e scale che non parlano della stessa cosa.</para>
    /// </summary>
    public const decimal StrategyReferenceBalance = 1_000_000m;
}
