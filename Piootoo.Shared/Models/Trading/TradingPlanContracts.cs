namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Configurazione operativa riutilizzabile, salvata nel workspace. Una sessione ne acquisisce uno
/// snapshot alla creazione: modificare il piano non cambia le sessioni già esistenti.
/// </summary>
public sealed class TradingPlan
{
    public required string WorkspaceId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string GroupId { get; init; }
    public required string AccountNumber { get; init; }
    public int MaxConcurrentTrades { get; init; }
    public string? RotationSetupId { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }
    public decimal InitialCapital { get; init; } = 100_000m;
    public decimal CommissionPerContract { get; init; } = 2m;
    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> Instruments { get; init; } = [];
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed class SaveTradingPlanRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string GroupId { get; init; }
    public required string AccountNumber { get; init; }
    public int MaxConcurrentTrades { get; init; }
    public string? RotationSetupId { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }
    public decimal InitialCapital { get; init; } = 100_000m;
    public decimal CommissionPerContract { get; init; } = 2m;
    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> Instruments { get; init; } = [];
}

/// <summary>
/// Richiesta idempotente del cBot. La chiave (piano, modalità client, execution key) identifica
/// un'esecuzione: la stessa richiesta riprende la sessione, una chiave nuova ne crea una nuova.
/// </summary>
public sealed class OpenTradingPlanSessionRequest
{
    public required string PlanCode { get; init; }
    public required ClientRunMode ClientRunMode { get; init; }
    public required string ExecutionKey { get; init; }
    public string? AccountNumber { get; init; }
}
