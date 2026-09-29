namespace Piootoo.Strategies.ResearchContainers;

// Contenitori delle figure armoniche (serie PTARM, 29/09/2026). I rapporti stanno nella classe del motore e
// non sono leve; le leve proprie sono le stesse per le quattro figure: PivotBars, Tolerance, MaxDBars,
// LookbackBars, RatioScale (il controllo con rapporti falsati), StopStructure, StopBufferAtr, AtrBars,
// TargetAd, Direction. Vedi docs/domini/catalogo-armonici.md.

/// <summary>Gartley (serie PTARM): limit su D a 0,786 di XA.</summary>
public sealed class RC_GAR : PTARMStrategies.Engines.GartleyEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_GAR";
    public override string Description => "Contenitore generico di ricerca: Gartley";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_GAR()
    {
        ResearchContainerSettings.Prepare(this);
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Bat (serie PTARM): limit su D a 0,886 di XA.</summary>
public sealed class RC_BAT : PTARMStrategies.Engines.BatEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_BAT";
    public override string Description => "Contenitore generico di ricerca: Bat";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_BAT()
    {
        ResearchContainerSettings.Prepare(this);
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Butterfly (serie PTARM): limit su D a 1,27 di XA, oltre X.</summary>
public sealed class RC_BUT : PTARMStrategies.Engines.ButterflyEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_BUT";
    public override string Description => "Contenitore generico di ricerca: Butterfly";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_BUT()
    {
        ResearchContainerSettings.Prepare(this);
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Crab (serie PTARM): limit su D a 1,618 di XA, oltre X.</summary>
public sealed class RC_CRB : PTARMStrategies.Engines.CrabEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_CRB";
    public override string Description => "Contenitore generico di ricerca: Crab";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_CRB()
    {
        ResearchContainerSettings.Prepare(this);
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
