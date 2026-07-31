namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Configurazione operativa riutilizzabile, salvata nel workspace. Una sessione ne acquisisce uno
/// snapshot alla creazione: modificare il piano non cambia le sessioni già esistenti.
///
/// <para>La collezione <see cref="Groups"/> è la fonte autorevole di gruppo/account. I campi
/// singoli (<see cref="GroupId"/>, <see cref="AccountNumber"/>, ecc.) restano come mirror della
/// prima riga per compatibilità con i <c>plans.json</c> legacy e con i client che li leggono
/// ancora in modo diretto.</para>
/// </summary>
public sealed class TradingPlan
{
    public required string WorkspaceId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }

    /// <summary>Righe gruppo/account del piano. Almeno una. È la configurazione canonica.</summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];

    /// <summary>Mirror legacy della prima riga: usato nei file vecchi e come default di sessione.</summary>
    public string GroupId { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public int MaxConcurrentTrades { get; init; }
    public string? RotationSetupId { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico
    /// (attivo ovunque tranne nel backtest senza filtro Titano, che deve produrre il campione
    /// sorgente completo). Esplicito per poter variare concorrenza e rotazione in modo indipendente:
    /// vedi <c>docs/domini/distribuzione-multi-account.md</c> §4.
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

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

    /// <summary>
    /// Righe gruppo/account da salvare. Se vuota, il server ricostruisce una riga dai campi
    /// legacy singoli (compatibilità con i client e i test già esistenti).
    /// </summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];

    public string? GroupId { get; init; }
    public string? AccountNumber { get; init; }
    public int MaxConcurrentTrades { get; init; }
    public string? RotationSetupId { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }
    public bool ApplyTitanoFilters { get; init; }

    /// <summary>
    /// Applica <c>MaxConcurrentTrades</c> nella distribuzione multi-account. Null = default storico.
    /// Vedi <see cref="TradingPlan.EnforceConcurrencyLimits"/>.
    /// </summary>
    public bool? EnforceConcurrencyLimits { get; init; }

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

    /// <summary>
    /// Account cTrader che apre la sessione. Deve appartenere alle righe del piano. Se omesso,
    /// si usa il primo account del piano.
    /// </summary>
    public string? AccountNumber { get; init; }
}
