namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Anomalia di volume (serie PT6EXO, declassata): attivita' della barra oltre un multiplo della mediana,
/// si segue o si sfuma. Leve proprie: <c>SpikeRatio</c>, <c>LookbackBars</c>, <c>Mode</c>,
/// <c>ActivitySource</c>, <c>Direction</c>.
///
/// <para>Ogni cella si gira <b>due volte</b>, con <c>ActivitySource</c> 0 (volume) e 1 (ampiezza della
/// barra): la cella sul volume e' ammessa solo se batte la sua gemella sull'ampiezza. E solo sul feed
/// del broker che la esegue: il tick volume di un broker non e' quello di un altro, e il volume del feed
/// interno in live non c'e'.</para>
/// </summary>
public sealed class RC_VLM : PT6EXOStrategies.Engines.VolumeSpikeEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_VLM";
    public override string Description => "Contenitore generico di ricerca: anomalia di volume";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_VLM()
    {
        ResearchContainerSettings.Prepare(this);
        SpikeRatio = 2.5m;
        LookbackBars = 20;
        Mode = 0;
        ActivitySource = 0;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
