namespace Piootoo.Strategies.ResearchContainers;

// Studio dell'apertura (30/09/2026): il file vive in Piootoo.Strategies/OpeningStudy/ con il motore, per
// togliere lo studio in un colpo se non porta a nulla; il namespace resta quello dei contenitori, perche'
// matrice e test li cercano li'. Vedi docs/domini/studio-apertura.md.

/// <summary>
/// Opening range dell'apertura di borsa: breakout (<c>Mode</c> 0) o fade della rottura che rientra
/// (<c>Mode</c> 1). Leve proprie: <c>OpenHhmm</c>, <c>OpenClock</c>, <c>RangeMinutes</c>,
/// <c>EntryWindowMinutes</c>, <c>Mode</c>, <c>OffsetTicks</c>, <c>MaxRangeAtr</c>, <c>StructureStop</c>,
/// <c>ExtremeBufferTicks</c>, <c>TargetRange</c>, <c>Direction</c>. L'apertura va dichiarata per mercato:
/// il default, le 09:00 in ora di borsa, e' quella europea e su NQ sarebbe le 09:00 di Chicago.
/// </summary>
public sealed class RC_ORB : OpeningStudy.Engines.OpeningRangeEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_ORB";
    public override string Description => "Contenitore generico di ricerca: opening range dell'apertura di borsa";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_ORB()
    {
        ResearchContainerSettings.Prepare(this);
        OpenTime = new TimeOnly(9, 0);
        OpenClock = Piootoo.Shared.Configuration.InstrumentClock.Exchange;
        RangeMinutes = 30;
        EntryWindowMinutes = 120;
        Mode = 0;
        OffsetTicks = 0;
        MaxRangeAtr = 0m;
        StructureStop = 0;
        ExtremeBufferTicks = 0;
        TargetRange = 0m;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
