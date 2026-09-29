namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Bandiera (serie PT7CLP, figure classiche): palo, canale controtrend a rette parallele, stop sulla
/// rottura nel verso del palo. Leve proprie: <c>PoleBars</c>, <c>PoleAtr</c>, <c>AtrBars</c>,
/// <c>FlagMinBars</c>, <c>FlagMaxBars</c>, <c>MaxRetrace</c>, <c>ParallelTolerance</c>, <c>OffsetTicks</c>,
/// <c>StopAtFlag</c>, <c>ExtremeBufferTicks</c>, <c>TargetPole</c>, <c>Direction</c>.
/// </summary>
public sealed class RC_FLG : PT7CLPStrategies.Engines.FlagEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_FLG";
    public override string Description => "Contenitore generico di ricerca: bandiera (flag)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_FLG()
    {
        ResearchContainerSettings.Prepare(this);
        PoleBars = 5;
        PoleAtr = 3m;
        AtrBars = 20;
        FlagMinBars = 3;
        FlagMaxBars = 10;
        MaxRetrace = 0.5m;
        ParallelTolerance = 0.1m;
        OffsetTicks = 0;
        StopAtFlag = 0;
        ExtremeBufferTicks = 0;
        TargetPole = 0m;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
