namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Fasi lunari (serie PT6EXO, idea bizzarra): long dalla luna nuova alla piena e short dalla piena alla
/// nuova, o il contrario. Leve proprie: <c>Mode</c> (0 o 1), <c>Direction</c>. <b>Multiday per
/// natura</b>: nasce con <c>IntradayOnly = false</c>, perche' la tenuta e' mezzo mese sinodico; la
/// griglia puo' riaccenderlo, e allora vince l'uscita di sessione. Si giudica contro il controllo RAN,
/// su piu' simboli.
/// </summary>
public sealed class RC_LUN : PT6EXOStrategies.Engines.LunarPhaseEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_LUN";
    public override string Description => "Contenitore generico di ricerca: fasi lunari";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_LUN()
    {
        ResearchContainerSettings.Prepare(this);
        IntradayOnly = false;
        Mode = 0;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
