using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;

namespace Piootoo.Strategies.PT5DAVStrategies.ResearchContainers;

// I contenitori di ricerca dei motori PT5DAV: uno per motore, per ogni mercato e timeframe. Simbolo e
// timeframe arrivano dai parametri, le leve con il nome della loro colonna in strategie_224.csv
// (Pt5DavEngineBase.ApplyResearchParameter). Nati il 25/09/2026 per rifare la ricerca PT5DAV con la
// sweep, ottimizzando su una parte della storia e verificando sul resto: la consegna ha ottimizzato
// su tutta la storia 2012-2025, e il solo periodo fuori campione che resta e' l'anno broker.
//
// NON sono strategie e non vanno in nessun piano: IsResearchContainer e' vero, il server rifiuta di
// salvarli in un masterfilter. Il simbolo di default e' NQ e non FDAX, perche' la base applica
// l'ancoraggio del DAX della ricerca gia' nel costruttore e non lo toglierebbe piu'.

/// <summary>Contenitore di ricerca PT5DAV: trend following mirrored (TF_M).</summary>
public sealed class RC5_TFM : Pt5DavTfMirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_TFM";
    public override string Description => "Contenitore di ricerca PT5DAV: trend following mirrored (TF_M)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_TFM";
    public override bool IsResearchContainer => true;

    public RC5_TFM() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: trend following unmirrored (TF_U).</summary>
public sealed class RC5_TFU : Pt5DavTfUnmirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_TFU";
    public override string Description => "Contenitore di ricerca PT5DAV: trend following unmirrored (TF_U)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_TFU";
    public override bool IsResearchContainer => true;

    public RC5_TFU() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: price channel (PC).</summary>
public sealed class RC5_PCH : Pt5DavPriceChannelEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_PCH";
    public override string Description => "Contenitore di ricerca PT5DAV: price channel (PC)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_PCH";
    public override bool IsResearchContainer => true;

    public RC5_PCH() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: breakout di N sessioni chiuse (BO).</summary>
public sealed class RC5_SBO : Pt5DavSessionBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_SBO";
    public override string Description => "Contenitore di ricerca PT5DAV: breakout di N sessioni chiuse (BO)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_SBO";
    public override bool IsResearchContainer => true;

    public RC5_SBO() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: breakout della sessione in corso (BO_S).</summary>
public sealed class RC5_BOS : Pt5DavCurrentSessionBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_BOS";
    public override string Description => "Contenitore di ricerca PT5DAV: breakout della sessione in corso (BO_S)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_BOS";
    public override bool IsResearchContainer => true;

    public RC5_BOS() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: volatility breakout (VBO).</summary>
public sealed class RC5_VBO : Pt5DavVolatilityBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_VBO";
    public override string Description => "Contenitore di ricerca PT5DAV: volatility breakout (VBO)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_VBO";
    public override bool IsResearchContainer => true;

    public RC5_VBO() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: level fader sul pivot (LF).</summary>
public sealed class RC5_LFD : Pt5DavPivotFaderEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_LFD";
    public override string Description => "Contenitore di ricerca PT5DAV: level fader sul pivot (LF)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_LFD";
    public override bool IsResearchContainer => true;

    public RC5_LFD() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: level fader sugli estremi di ieri (LF_HL).</summary>
public sealed class RC5_LFH : Pt5DavHighLowFaderEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_LFH";
    public override string Description => "Contenitore di ricerca PT5DAV: level fader sugli estremi di ieri (LF_HL)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_LFH";
    public override bool IsResearchContainer => true;

    public RC5_LFH() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: reversal sugli estremi di ieri (RHL).</summary>
public sealed class RC5_RHL : Pt5DavRhlEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_RHL";
    public override string Description => "Contenitore di ricerca PT5DAV: reversal sugli estremi di ieri (RHL)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_RHL";
    public override bool IsResearchContainer => true;

    public RC5_RHL() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: reversal Bollinger mirrored (RBB_M).</summary>
public sealed class RC5_RBM : Pt5DavBollingerMirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_RBM";
    public override string Description => "Contenitore di ricerca PT5DAV: reversal Bollinger mirrored (RBB_M)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_RBM";
    public override bool IsResearchContainer => true;

    public RC5_RBM() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: reversal Bollinger unmirrored (RBB_U).</summary>
public sealed class RC5_RBU : Pt5DavBollingerUnmirroredEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_RBU";
    public override string Description => "Contenitore di ricerca PT5DAV: reversal Bollinger unmirrored (RBB_U)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_RBU";
    public override bool IsResearchContainer => true;

    public RC5_RBU() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: BIAS a mercato.</summary>
public sealed class RC5_BIA : Pt5DavBiasMarketEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_BIA";
    public override string Description => "Contenitore di ricerca PT5DAV: BIAS a mercato";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_BIA";
    public override bool IsResearchContainer => true;

    public RC5_BIA() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: BIAS in ritracciamento (BIAS_RT).</summary>
public sealed class RC5_BRT : Pt5DavBiasRetracementEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_BRT";
    public override string Description => "Contenitore di ricerca PT5DAV: BIAS in ritracciamento (BIAS_RT)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_BRT";
    public override bool IsResearchContainer => true;

    public RC5_BRT() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: BIAS in breakout (BIAS_BO).</summary>
public sealed class RC5_BBO : Pt5DavBiasBreakoutEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_BBO";
    public override string Description => "Contenitore di ricerca PT5DAV: BIAS in breakout (BIAS_BO)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_BBO";
    public override bool IsResearchContainer => true;

    public RC5_BBO() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: incrocio di medie (MAC).</summary>
public sealed class RC5_MAC : Pt5DavMovingAverageCrossoverEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_MAC";
    public override string Description => "Contenitore di ricerca PT5DAV: incrocio di medie (MAC)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_MAC";
    public override bool IsResearchContainer => true;

    public RC5_MAC() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}

/// <summary>Contenitore di ricerca PT5DAV: BIAS settimanale (BIASW).</summary>
public sealed class RC5_BSW : Pt5DavBiasWeeklyEngine
{
    private readonly ResearchContainerIdentity _identity = new() { Symbol = "@NQ", TimeframeMinutes = 15 };
    public override string Name => "RC5_BSW";
    public override string Description => "Contenitore di ricerca PT5DAV: BIAS settimanale (BIASW)";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override string ResearchCode => "RC5_BSW";
    public override bool IsResearchContainer => true;

    public RC5_BSW() => TradingWindow = ResearchWindow(-1, -1);

    public override void Initialize(Dictionary<string, object>? parameters = null) =>
        InitializeResearchContainer(_identity, parameters);
}
