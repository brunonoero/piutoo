namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Griglia giorno × ora (serie PT6EXO, idea bizzarra): una regola long/short per coppia (giorno
/// feriale, ora locale), tenuta a ore. Leve proprie: <c>Rules</c> (stringa <c>giorno-ora:L|S;…</c>,
/// giorno 0 = lunedi'), <c>HoldHours</c>, <c>ScheduleClock</c> (0 ricerca, 1 borsa). Nasce senza regole:
/// la tabella la porta la griglia. E' la famiglia con il rischio di adattamento piu' alto del catalogo:
/// solo dove HOD ha gia' mostrato qualcosa, con uno split fuori campione severo e contro il controllo RAN.
/// </summary>
public sealed class RC_DXH : PT6EXOStrategies.Engines.DayHourGridEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_DXH";
    public override string Description => "Contenitore generico di ricerca: griglia giorno per ora";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_DXH()
    {
        ResearchContainerSettings.Prepare(this);
        Rules = string.Empty;
        HoldHours = 1;
        ScheduleClock = Piootoo.Shared.Configuration.InstrumentClock.Research;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
