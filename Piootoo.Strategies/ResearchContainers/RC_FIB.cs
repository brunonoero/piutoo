namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Conte di Fibonacci nel tempo (serie PT6EXO, idea bizzarra): ingresso esattamente N barre dopo
/// l'ultimo pivot, contro il movimento partito da li' o a favore. Leve proprie: <c>PivotBars</c>,
/// <c>CountBars</c> (3, 5, 8, 13, 21, 34, 55), <c>Mode</c> (0 contro, 1 a favore), <c>Direction</c>.
/// Si giudica contro il controllo RAN.
/// </summary>
public sealed class RC_FIB : PT6EXOStrategies.Engines.FibonacciTimeEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_FIB";
    public override string Description => "Contenitore generico di ricerca: conte di Fibonacci nel tempo";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_FIB()
    {
        ResearchContainerSettings.Prepare(this);
        PivotBars = 3;
        CountBars = 13;
        Mode = 0;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
