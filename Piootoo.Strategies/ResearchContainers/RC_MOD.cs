namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Modulo del tempo (serie PT6EXO, idea bizzarra): ingresso sulle barre il cui indice temporale ha
/// resto <c>Remainder</c> modulo <c>ModuloBars</c>, nella direzione della barra chiusa o contro. Leve
/// proprie: <c>ModuloBars</c>, <c>Remainder</c>, <c>Mode</c> (0 segue, 1 contro), <c>Direction</c>.
/// Si giudica contro il controllo RAN.
/// </summary>
public sealed class RC_MOD : PT6EXOStrategies.Engines.TimeModuloEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_MOD";
    public override string Description => "Contenitore generico di ricerca: modulo del tempo";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_MOD()
    {
        ResearchContainerSettings.Prepare(this);
        ModuloBars = 7;
        Remainder = 0;
        Mode = 0;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
