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

    /// <summary>
    /// Serie aggiuntive richieste dalle strategie multi-timeframe, indicizzate per timeframe in
    /// minuti (es. 1440 per il giornaliero).
    ///
    /// <para><b>Perché vive qui.</b> Prima le strategie multi-timeframe venivano invocate
    /// direttamente su <c>IMultiTimeframeTradingStrategy.GenerateSignal</c>, saltando
    /// <c>Evaluate</c> e con esso tutta l'iniezione di stato: posizione corrente, memoria di
    /// sessione fra le barre e conversione del rischio in denaro. Una strategia MTF non sapeva di
    /// essere in posizione e ripartiva da zero a ogni barra. Portando le serie aggiuntive dentro
    /// la request, tutte le strategie percorrono lo stesso cammino.</para>
    /// </summary>
    public IReadOnlyDictionary<int, OhlcvData[]> AdditionalOhlcv { get; init; }
        = new Dictionary<int, OhlcvData[]>();
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
    public decimal Contracts { get; init; }
    public int BarsInPosition { get; init; }
}
