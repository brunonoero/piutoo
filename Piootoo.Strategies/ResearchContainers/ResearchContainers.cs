using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.ResearchContainers;

// I contenitori generici della ricerca: uno per motore, per ogni mercato e timeframe. Simbolo e
// timeframe arrivano dai parametri (vedi ResearchContainerIdentity), le leve per nome
// (ResearchContainerSettings). Nati il 24/09/2026 per girare la griglia grossa su tutti i motori della
// ricerca v5.0 senza scrivere un contenitore per ogni cella: sono sedici logiche per ventiquattro
// mercati per quattro timeframe.
//
// NON sono strategie e non vanno in nessun piano: IsResearchContainer e' vero, il server rifiuta di
// salvarli in un masterfilter. Varianti dello stesso motore (breakout di N sessioni o della sessione
// in corso, level fader sul pivot o sugli estremi di ieri) si ottengono con i parametri fissi della
// matrice, non con una classe in piu'.

/// <summary>Price Channel (Donchian).</summary>
public sealed class RC_PCH : PriceChannelEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_PCH";
    public override string Description => "Contenitore generico di ricerca: Price Channel";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_PCH()
    {
        ResearchContainerSettings.Prepare(this);
        ChannelBars = 20;
        Direction = 0;
        OffsetTicks = 0;
        DvolMin = 0m;
        SkipDay = -1;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Trend following mirrored: livello = estremo della sessione precedente.</summary>
public sealed class RC_TFM : TfMirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_TFM";
    public override string Description => "Contenitore generico di ricerca: trend following mirrored";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_TFM() => ResearchContainerSettings.Prepare(this);

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Trend following unmirrored.</summary>
public sealed class RC_TFU : TfUnmirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_TFU";
    public override string Description => "Contenitore generico di ricerca: trend following unmirrored";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_TFU() => ResearchContainerSettings.Prepare(this);

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Breakout di sessione: con <c>LevelSource = 0</c> il canale delle ultime N sessioni chiuse (BO),
/// con <c>LevelSource = 1</c> gli estremi della sessione in corso (BO_S).
/// </summary>
public sealed class RC_SBO : SessionBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_SBO";
    public override string Description => "Contenitore generico di ricerca: breakout di sessione";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_SBO()
    {
        ResearchContainerSettings.Prepare(this);
        LevelSource = 0;
        Sessions = 1;
        IncludeCurrentSession = false;
        BreakoutOffsetTicks = 0;
        SkipDay = -1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Reversal sulle bande di Bollinger, mirrored.</summary>
public sealed class RC_RBM : RbbMirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RBM";
    public override string Description => "Contenitore generico di ricerca: reversal Bollinger mirrored";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RBM()
    {
        ResearchContainerSettings.Prepare(this);
        BollingerLength = 20;
        BollingerNumDevs = 2m;
        DayToFilter = -1;
    }

    // Il motore non espone GenerateSignal: la valutazione comune e' EvaluateCore, come nelle PT3B.
    public Piootoo.Shared.Models.TradeSignal GenerateSignal(Piootoo.Shared.Models.OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Reversal sulle bande di Bollinger, unmirrored.</summary>
public sealed class RC_RBU : RbbUnmirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RBU";
    public override string Description => "Contenitore generico di ricerca: reversal Bollinger unmirrored";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RBU()
    {
        ResearchContainerSettings.Prepare(this);
        BollingerLength = 20;
        BollingerNumDevs = 2m;
        DayToFilter = -1;
    }

    // Il motore non espone GenerateSignal: la valutazione comune e' EvaluateCore, come nelle PT3B.
    public Piootoo.Shared.Models.TradeSignal GenerateSignal(Piootoo.Shared.Models.OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Reversal sul massimo e minimo di ieri.</summary>
public sealed class RC_RHL : RhlEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RHL";
    public override string Description => "Contenitore generico di ricerca: reversal sui livelli di ieri";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RHL()
    {
        ResearchContainerSettings.Prepare(this);
        Direction = 0;
        SkipDay = -1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Volatility breakout sul range della sessione precedente, ordine stop.</summary>
public sealed class RC_VBO : VolatilityBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_VBO";
    public override string Description => "Contenitore generico di ricerca: volatility breakout";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_VBO()
    {
        ResearchContainerSettings.Prepare(this);
        EntryOrderType = Piootoo.Shared.Enums.TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;
        VolatilitySource = 1;
        AtrLength = 0;
        AtrMultiplierLong = 0.5m;
        AtrMultiplierShort = -1m;
        Momentum = 0;
        Direction = 0;
        SkipDay = -1;
        MaxEntriesPerSession = 1;
    }

    // Il motore non espone GenerateSignal: la valutazione comune e' EvaluateCore, come nelle PT3B.
    public Piootoo.Shared.Models.TradeSignal GenerateSignal(Piootoo.Shared.Models.OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>Incrocio di medie mobili.</summary>
public sealed class RC_MAC : MovingAverageCrossoverEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_MAC";
    public override string Description => "Contenitore generico di ricerca: incrocio di medie";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_MAC()
    {
        ResearchContainerSettings.Prepare(this);
        FastPeriod = 10;
        SlowPeriod = 50;
        Direction = MovingAverageCrossoverDirection.Both;
        UsePatternFilter = false;
        UseDailyFilter = false;
        UseTradingWindow = false;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Level fader: falsa rottura di un livello di ieri e rientro. <c>LevelChoice = 1</c> il pivot (LF),
/// <c>2</c> massimo e minimo di ieri (LF_HL).
/// </summary>
public sealed class RC_LFD : LevelFaderEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_LFD";
    public override string Description => "Contenitore generico di ricerca: level fader";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_LFD()
    {
        ResearchContainerSettings.Prepare(this);
        LevelChoice = LevelFaderLevel.PreviousSessionPivot;
        LevelShift = 0m;
    }

    /// <summary>
    /// Filtro di regime di volatilita': sessioni chiuse su cui si misura il true range medio di lungo
    /// periodo. 0 = spento. Nato il 25/09/2026 da un'ipotesi scritta prima della misura: su FDAX 4h il
    /// level fader sugli estremi di ieri guadagna negli anni agitati (2016, 2020-2022) e perde in quelli
    /// calmi, quindi si opera solo quando la volatilita' recente e' sopra quella di lungo periodo.
    /// Blocca i soli ingressi: le uscite passano sempre.
    ///
    /// <para>Campi <b>non pubblici</b>, come ogni leva di un motore: la valutazione gira su un clone che
    /// copia i soli campi non pubblici (<c>StatelessEasyStrategyBase.GetInstanceFields</c>). Dichiarato
    /// pubblico, il filtro restava spento nel clone e la prima misura ha solo allungato il riscaldamento.</para>
    /// </summary>
    protected int VolatilityRegimeSessions;

    /// <summary>ATR a 14 sessioni chiuse diviso true range medio delle ultime <see cref="VolatilityRegimeSessions"/>: si entra da qui in su.</summary>
    protected decimal VolatilityRegimeRatio = 1m;

    public override int RequiredCandles =>
        VolatilityRegimeSessions > 0 ? Math.Max(base.RequiredCandles, SessionsToCandles(VolatilityRegimeSessions + 2)) : base.RequiredCandles;

    public new TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var signal = base.GenerateSignal(data, currentDate);
        if (VolatilityRegimeSessions <= 0 || signal.Type == SignalType.Hold || data.Length == 0 || HighVolatility(data))
            return signal;

        // Regime calmo: restano le sole uscite.
        var legs = new List<TradeSignal> { signal };
        if (signal.CompanionSignals is { } companions) legs.AddRange(companions);
        var exits = legs.Where(leg => leg.ExitOnly).ToList();
        foreach (var leg in exits) leg.CompanionSignals = null;
        return Combine(exits, Hold(data[^1].Close, data[^1].DateTime, "Regime di volatilita' calmo"));
    }

    private bool HighVolatility(OhlcvData[] data)
    {
        var barTime = data[^1].DateTime;
        var current = ClosedSessionAtrPoints(data, barTime);
        if (current is null)
            return false;

        // True range delle sessioni chiuse, con la stessa griglia dell'ATR.
        var currentSession = ResolveEntrySessionStartUtc(barTime);
        var ranges = new List<decimal>();
        DateTime? key = null;
        decimal high = 0m, low = 0m, close = 0m, previousClose = 0m;
        var hasPrevious = false;
        foreach (var candidate in data)
        {
            var candidateKey = ResolveEntrySessionStartUtc(candidate.DateTime);
            if (candidateKey >= currentSession) break;
            if (key != candidateKey)
            {
                if (key.HasValue)
                {
                    if (hasPrevious) ranges.Add(Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose))));
                    previousClose = close;
                    hasPrevious = true;
                }

                key = candidateKey;
                high = candidate.High;
                low = candidate.Low;
            }
            else
            {
                high = Math.Max(high, candidate.High);
                low = Math.Min(low, candidate.Low);
            }

            close = candidate.Close;
        }

        if (key.HasValue && hasPrevious)
            ranges.Add(Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose))));
        if (ranges.Count < VolatilityRegimeSessions)
            return false;

        var longTerm = ranges.Skip(ranges.Count - VolatilityRegimeSessions).Average();
        return longTerm > 0m && current.Value / longTerm >= VolatilityRegimeRatio;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Ingresso casuale con seme fisso (serie PT6EXO): il controllo con cui si misura quanto di una
/// strategia viene dal segnale e quanto dalle uscite. Leve proprie: <c>Seed</c>,
/// <c>EntryProbability</c>, <c>Direction</c>; le uscite sono quelle comuni.
/// </summary>
public sealed class RC_RAN : PT6EXOStrategies.Engines.RandomEntryEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_RAN";
    public override string Description => "Contenitore generico di ricerca: ingresso casuale (controllo)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_RAN()
    {
        ResearchContainerSettings.Prepare(this);
        Seed = 1;
        EntryProbability = 0.05m;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Falso breakout (serie PT6EXO): rottura del canale e rientro, ingresso contro la rottura. Leve
/// proprie: <c>ChannelBars</c>, <c>ReentryBars</c>, <c>MinBreakAtr</c>, <c>StopAtExtreme</c>,
/// <c>ExtremeBufferTicks</c>, <c>Direction</c>.
/// </summary>
public sealed class RC_FBO : PT6EXOStrategies.Engines.FailedBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_FBO";
    public override string Description => "Contenitore generico di ricerca: falso breakout";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_FBO()
    {
        ResearchContainerSettings.Prepare(this);
        ChannelBars = 20;
        ReentryBars = 1;
        MinBreakAtr = 0m;
        StopAtExtreme = 0;
        ExtremeBufferTicks = 0;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Forza interna della barra (serie PT6EXO): chiusura sul minimo compra, sul massimo vende. Leve
/// proprie: <c>LowThreshold</c>, <c>HighThreshold</c>, <c>TrendBars</c>, <c>ExitIbs</c>, <c>Direction</c>.
/// Il motore nudo non ha uscita a segnale (<c>ExitIbs = 0</c>): la griglia la accende.
/// </summary>
public sealed class RC_IBS : PT6EXOStrategies.Engines.InternalBarStrengthEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_IBS";
    public override string Description => "Contenitore generico di ricerca: forza interna della barra";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_IBS()
    {
        ResearchContainerSettings.Prepare(this);
        LowThreshold = 0.2m;
        HighThreshold = 0.8m;
        TrendBars = 0;
        ExitIbs = 0m;
        Direction = 0;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}

/// <summary>
/// Deriva oraria (serie PT6EXO): entra a un'ora fissa e tiene per un numero fisso di ore. Leve
/// proprie: <c>EntryHour</c>, <c>HoldHours</c>, <c>ScheduleClock</c> (0 ricerca, 1 borsa),
/// <c>Direction</c>, <c>MomentumMode</c>, <c>MomentumBars</c>, <c>SkipDay</c>.
/// </summary>
public sealed class RC_HOD : PT6EXOStrategies.Engines.HourOfDayEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    public override string Name => "RC_HOD";
    public override string Description => "Contenitore generico di ricerca: deriva oraria";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    public RC_HOD()
    {
        ResearchContainerSettings.Prepare(this);
        EntryTime = new TimeOnly(10, 0);
        HoldHours = 4;
        ScheduleClock = Piootoo.Shared.Configuration.InstrumentClock.Research;
        Direction = 1;
        MomentumMode = 0;
        MomentumBars = 1;
        SkipDay = -1;
        MaxEntriesPerSession = 1;
    }

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
