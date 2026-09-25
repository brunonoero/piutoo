namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Gap di apertura della sessione della ricerca (serie PT6EXO): fade o go quando l'apertura dista dalla
/// chiusura di ieri piu' di una frazione dell'ATR. Leve proprie: <c>GapAtr</c>, <c>Mode</c>,
/// <c>TargetAtGapFill</c>, <c>Direction</c>. Sul feed di un broker il salto di rollover e' un gap finto
/// che il motore non filtra.
/// </summary>
public sealed class RC_GAP : PT6EXOStrategies.Engines.SessionGapEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_GAP";
    public override string Description => "Contenitore generico di ricerca: gap di apertura di sessione";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_GAP()
    {
        ResearchContainerSettings.Prepare(this);
        GapAtr = 0.3m;
        Mode = 0;
        TargetAtGapFill = 0;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
