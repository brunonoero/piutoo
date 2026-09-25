namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Tra mercati (serie PT6EXO): opera sul proprio simbolo guardando <c>ReferenceSymbol</c>. Leve
/// proprie: <c>ReferenceSymbol</c>, <c>Mode</c> (0 anticipo, 1 divergenza, 2 rapporto),
/// <c>LookbackBars</c>, <c>MoveThresholdPct</c>, <c>ZEntry</c>, <c>Direction</c>. Gira solo nella sweep,
/// che e' l'unico percorso che oggi porta le serie di riferimento.
/// </summary>
public sealed class RC_XMK : PT6EXOStrategies.Engines.CrossMarketEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_XMK";
    public override string Description => "Contenitore generico di ricerca: tra mercati";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_XMK()
    {
        ResearchContainerSettings.Prepare(this);
        ReferenceSymbol = "@ES";
        Mode = 0;
        LookbackBars = 20;
        MoveThresholdPct = 1m;
        ZEntry = 2m;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
