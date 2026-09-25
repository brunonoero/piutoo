namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Regime di volatilita' (serie PT6EXO): breakout del canale quando l'ATR e' basso, ritorno alla media
/// sulle bande quando e' alto, niente in mezzo. Leve proprie: <c>AtrBars</c>, <c>RegimeBars</c>,
/// <c>LowPercentile</c>, <c>HighPercentile</c>, <c>ChannelBars</c>, <c>BandBars</c>, <c>BandDevs</c>,
/// <c>Direction</c>. <c>RegimeBars</c> fa il <c>RequiredCandles</c>: il riscaldamento deve portarlo.
/// </summary>
public sealed class RC_REG : PT6EXOStrategies.Engines.VolatilityRegimeEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_REG";
    public override string Description => "Contenitore generico di ricerca: regime di volatilita'";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_REG()
    {
        ResearchContainerSettings.Prepare(this);
        AtrBars = 14;
        RegimeBars = 500;
        LowPercentile = 20m;
        HighPercentile = 80m;
        ChannelBars = 20;
        BandBars = 20;
        BandDevs = 2m;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
