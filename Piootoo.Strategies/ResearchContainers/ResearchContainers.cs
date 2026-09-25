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

    public void Initialize(Dictionary<string, object>? parameters = null) =>
        ResearchContainerSettings.Apply(this, _identity, parameters);
}
