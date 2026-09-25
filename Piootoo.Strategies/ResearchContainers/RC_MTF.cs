namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Timeframe in disaccordo (serie PT6EXO): trend del timeframe alto, eccesso RSI contrario sul timeframe
/// della strategia, ingresso nel verso del trend. Leve proprie: <c>HigherTimeframeMinutes</c>,
/// <c>TrendBars</c>, <c>RsiPeriod</c>, <c>RsiLow</c>, <c>RsiHigh</c>, <c>Direction</c>.
///
/// <para>Gira solo in backtest: sessione live, sweep e cBot non consegnano i timeframe aggiuntivi, e il
/// backtest ne consegna 8 barre per la giornaliera. Vedi il commento di
/// <see cref="PT6EXOStrategies.Engines.TimeframeDisagreementEngine"/>.</para>
/// </summary>
public sealed class RC_MTF : PT6EXOStrategies.Engines.TimeframeDisagreementEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_MTF";
    public override string Description => "Contenitore generico di ricerca: timeframe in disaccordo";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_MTF()
    {
        ResearchContainerSettings.Prepare(this);
        HigherTimeframeMinutes = 1440;
        TrendBars = 50;
        RsiPeriod = 2;
        RsiLow = 10m;
        RsiHigh = 90m;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
