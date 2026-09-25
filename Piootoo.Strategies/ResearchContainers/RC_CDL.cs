namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Candela di rifiuto su un livello (serie PT6EXO): pin bar o engulfing che toccano il massimo o minimo
/// di ieri, o l'apertura della sessione, e chiudono dalla parte opposta. Leve proprie: <c>LevelKind</c>,
/// <c>CandleKind</c>, <c>WickRatio</c>, <c>ToleranceTicks</c>, <c>StopAtExtreme</c>,
/// <c>ExtremeBufferTicks</c>, <c>Direction</c>.
/// </summary>
public sealed class RC_CDL : PT6EXOStrategies.Engines.RejectionCandleEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_CDL";
    public override string Description => "Contenitore generico di ricerca: candela di rifiuto su un livello";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_CDL()
    {
        ResearchContainerSettings.Prepare(this);
        LevelKind = 0;
        CandleKind = 0;
        WickRatio = 2m;
        ToleranceTicks = 0;
        StopAtExtreme = 0;
        ExtremeBufferTicks = 0;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
