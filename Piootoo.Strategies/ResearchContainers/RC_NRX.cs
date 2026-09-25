namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Compressione (serie PT6EXO): OCO di stop sugli estremi di una barra stretta. Leve proprie:
/// <c>CompressionKind</c> (0 NR-N, 1 inside bar), <c>LookbackBars</c>, <c>ValidBars</c>,
/// <c>OffsetTicks</c>, <c>Direction</c>. Il tick arriva dal registro strumenti.
/// </summary>
public sealed class RC_NRX : PT6EXOStrategies.Engines.CompressionEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_NRX";
    public override string Description => "Contenitore generico di ricerca: compressione";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_NRX()
    {
        ResearchContainerSettings.Prepare(this);
        CompressionKind = 0;
        LookbackBars = 7;
        ValidBars = 1;
        OffsetTicks = 0;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
