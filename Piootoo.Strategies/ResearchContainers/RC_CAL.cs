namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Anomalie di calendario (serie PT6EXO): turn-of-month, settimana della scadenza, vigilia di festivo,
/// contate in giorni di negoziazione del calendario del simbolo. Leve proprie: <c>Mode</c>,
/// <c>DaysBeforeMonthEnd</c>, <c>HoldSessions</c>, <c>Direction</c>. Multiday per natura: il motore non
/// applica l'uscita di sessione, quindi <c>IntradayOnly</c> parte spento e <c>ExitHour</c> acceso e' un
/// errore.
/// </summary>
public sealed class RC_CAL : PT6EXOStrategies.Engines.CalendarAnomalyEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_CAL";
    public override string Description => "Contenitore generico di ricerca: anomalie di calendario";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_CAL()
    {
        ResearchContainerSettings.Prepare(this);
        IntradayOnly = false;
        Mode = 0;
        DaysBeforeMonthEnd = 1;
        HoldSessions = 4;
        Direction = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
