namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Schema del log diagnostico di rotazione: una traccia per barra che spiega, strategia per strategia,
/// perché Titano l'ha inclusa o esclusa dalla valutazione in quel momento. Pensato per essere letto in
/// modo automatico (anche da un agente) per individuare bug nelle strategie o nelle rotazioni: incrocia
/// lo stato Titano con l'effettivo segnale generato (o l'assenza di valutazione).
/// </summary>
public static class DiagnosticsSchema
{
    public const int Version = 1;
    public const string RotationLogFileName = "rotation-log.json";
}

/// <summary>Una riga di log per ogni barra processata da una sessione collegata a una rotazione Titano.</summary>
public sealed class RotationLogEntry
{
    public int SchemaVersion { get; init; } = DiagnosticsSchema.Version;
    public required string EntryId { get; init; }
    public required string SessionId { get; init; }
    public required DateTime BarTimeUtc { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    /// <summary>Periodo di rotazione effettivo per questa barra (null se fuori da qualsiasi periodo definito nel manifest).</summary>
    public string? PeriodId { get; init; }
    /// <summary>Strategie del masterfilter del workspace (indipendentemente da Titano).</summary>
    public IReadOnlyList<string> MasterStrategies { get; init; } = [];
    /// <summary>Strategie effettivamente passate a Evaluate() in questa barra (sottoinsieme del masterfilter, filtrato da Titano).</summary>
    public IReadOnlyList<string> EvaluatedStrategies { get; init; } = [];
    /// <summary>MasterStrategies - EvaluatedStrategies: escluse da Titano in questa barra.</summary>
    public IReadOnlyList<string> SkippedByTitano { get; init; } = [];
    /// <summary>Stato/motivo Titano per ciascuna strategia del masterfilter in questa barra.</summary>
    public IReadOnlyList<RotationStrategyState> StrategyStates { get; init; } = [];
    /// <summary>Segnali realmente generati in questa barra, come "STRATEGYCODE:SignalType" (es. "EASY_218_GC_60:Buy"). Vuoto se nessun segnale.</summary>
    public IReadOnlyList<string> SignalsEmitted { get; init; } = [];
}

/// <summary>Stato Titano di una singola strategia al momento della valutazione di una barra.</summary>
public sealed class RotationStrategyState
{
    public required string StrategyCode { get; init; }
    /// <summary>True se in questa barra la strategia è stata passata a Evaluate() (poteva quindi generare un segnale).</summary>
    public bool Included { get; init; }
    public decimal AllocationMultiplier { get; init; }
    /// <summary>Enabled / Reduced / Disabled / HardStopped (da TitanoStrategyStatus), null se la strategia non è nel run Titano.</summary>
    public string? State { get; init; }
    public bool HardStopped { get; init; }
    public int CooldownRemaining { get; init; }
    public decimal Score { get; init; }
    public int PassingFilters { get; init; }
    public int TotalFilters { get; init; }
    /// <summary>Motivo sintetico della decisione (riportato dal periodo di rotazione Titano corrente).</summary>
    public string? Reason { get; init; }
}
