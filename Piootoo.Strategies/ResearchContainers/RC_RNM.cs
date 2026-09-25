namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Numeri tondi (serie PT6EXO, bizzarra): magnete verso il multiplo di <c>RoundStep</c> vicino, o muro
/// con i limit sul livello. Leve proprie: <c>RoundStep</c>, <c>Mode</c>, <c>DistanceTicks</c>,
/// <c>TargetAtLevel</c>, <c>Direction</c>; <c>TickSize</c> lo imposta il registro dello strumento.
///
/// <para>Si ricerca <b>solo sul feed di un broker</b>: sulla serie continua aggiustata del feed interno i
/// livelli tondi non sono i prezzi su cui stavano gli ordini.</para>
/// </summary>
public sealed class RC_RNM : PT6EXOStrategies.Engines.RoundNumberEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RNM";
    public override string Description => "Contenitore generico di ricerca: numeri tondi";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RNM()
    {
        ResearchContainerSettings.Prepare(this);
        RoundStep = 100m;
        Mode = 0;
        DistanceTicks = 20;
        TargetAtLevel = 1;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
