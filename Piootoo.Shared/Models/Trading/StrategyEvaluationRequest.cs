using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Contesto immutabile fornito dall'engine per valutare una strategia.
/// Le strategie non devono conservare posizione, fill o ordini pendenti
/// nelle proprie istanze.
/// </summary>
public sealed class StrategyEvaluationRequest
{
    public required OhlcvData[] Ohlcv { get; init; }
    public required DateTime BarTimeUtc { get; init; }
    public required StrategyExecutionSnapshot Execution { get; init; }
}

/// <summary>
/// Stato di esecuzione di una strategia, di proprietà esclusiva dell'engine.
/// </summary>
public sealed class StrategyExecutionSnapshot
{
    public string StrategyCode { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public DateTime BarTimeUtc { get; init; }
    public decimal DollarsPerPoint { get; init; } = 1m;
    public StrategyPositionSnapshot? Position { get; init; }
    public int EntriesToday { get; init; }

    /// <summary>
    /// Memoria tecnica serializzabile gestita dall'engine per gli adapter di
    /// strategie EasyLanguage legacy. Non contiene mai stato broker.
    /// </summary>
    public IReadOnlyDictionary<string, object?> RuntimeState { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);
}

/// <summary>Snapshot read-only della posizione effettivamente aperta dall'engine.</summary>
public sealed class StrategyPositionSnapshot
{
    public SignalType Direction { get; init; }
    public decimal EntryPrice { get; init; }
    public DateTime EntryTimeUtc { get; init; }
    public int Contracts { get; init; }
    public int BarsInPosition { get; init; }
}
