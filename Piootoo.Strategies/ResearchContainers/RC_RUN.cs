namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Serie di chiusure (serie PT6EXO): ritorno alla media dopo N chiusure nello stesso verso o dopo un
/// RSI di Wilder estremo. Leve proprie: <c>Mode</c> (0 serie, 1 RSI), <c>RunBars</c>, <c>RsiPeriod</c>,
/// <c>RsiLow</c>, <c>RsiHigh</c>, <c>TrendBars</c>, <c>ExitOnFirstOppositeClose</c>, <c>Direction</c>. Il
/// motore nudo non ha uscita a segnale (<c>ExitOnFirstOppositeClose = 0</c>): la griglia la accende.
/// </summary>
public sealed class RC_RUN : PT6EXOStrategies.Engines.RunOfClosesEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RUN";
    public override string Description => "Contenitore generico di ricerca: serie di chiusure";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RUN()
    {
        ResearchContainerSettings.Prepare(this);
        Mode = 0;
        RunBars = 3;
        RsiPeriod = 2;
        RsiLow = 10m;
        RsiHigh = 90m;
        TrendBars = 0;
        ExitOnFirstOppositeClose = 0;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
