using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b>Reversal sul minimo di ieri, solo long, sul DAX a 4 ore.</b> Compra a limite 20 punti sotto il
/// minimo della sessione precedente, solo con barre che aprono fino alle 10 dell'orologio della ricerca e
/// solo se il pattern neutro 34 e' vero (range della sessione di riferimento oltre l'1,5% del prezzo).
/// Stop e target a 1 ATR delle 14 sessioni chiuse, chiusura a fine sessione.
///
/// <para><b>Da dove viene.</b> Percorso di ricerca v4 (<c>ResearchPath</c>) sul contenitore
/// <c>RC_RHL</c>, cella FDAX 4h, ricerca sul feed FTMO 09/11/2020 → 01/09/2024, costi FTMO (spread
/// mediano 1,33 punti, swap), orologio al minuto, overnight libero: resoconto
/// <c>ricerca/percorso/fdax-240-ricerca-ftmo.md</c> (26/09/2026). Nella ricerca: 79 trade, netto 125.006,
/// net/DD 12,5.</para>
///
/// <para><b>Cosa regge, su periodi che la ricerca non ha visto</b> (stessi parametri, un contratto):</para>
/// <list type="table">
///   <item><term>prova FTMO 09/2024 → 09/2026</term><description>36 trade, +62.519, net/DD 3,20, average trade 1.737 (soglia 464), 4/4 finestre in utile</description></item>
///   <item><term>feed interno 2008 → 11/2020</term><description>206 trade, +64.716, net/DD 1,44, average trade 1,31 volte la soglia, 10 anni su 13 in utile</description></item>
///   <item><term>altri indici su FTMO dal 2020</term><description>6 su 7 con net/DD ≥ 1 (S&amp;P 4,69, Dow 5,90, EuroStoxx 5,60, Nasdaq 2,66, FTSE 1,65, CAC 1,45; Nikkei 0,19)</description></item>
///   <item><term>controllo RAN (100 long casuali con le stesse uscite)</term><description>netto sopra il 96% dei semi sulla prova e sopra il 100% sul 2008-2020, dove la mediana del caso perde 24.000</description></item>
/// </list>
///
/// <para><b>Riserve.</b> Opera poco: circa 12 trade all'anno, e in alcuni anni meno di cinque (2017: 2),
/// quindi non passa il cancello "almeno 5 trade in ogni anno". L'anno migliore (2022) vale il 32% del
/// netto sul feed interno. E' solo long su indici saliti dal 2020: il controllo su altri mercati ne e'
/// favorito, il 2008-2020 e il RAN no. E' una componente di paniere accanto a
/// <see cref="PT3B_FDAX_PCH_002_240"/> (che compra le rotture; questa compra i ritorni sui minimi), non una
/// strategia da conto da sola.</para>
///
/// <para><b>Etichetta della barra.</b> <c>ResearchLabelsBarsOnOpen = true</c>, come ogni PT3B: il percorso
/// gira sui contenitori con le barre etichettate sull'apertura, e la finestra fino alle 10 si confronta
/// con l'apertura della barra. Cambiarla sposta la finestra di quattro ore.</para>
///
/// <para><b>Cosa manca.</b> Il backtest a tick in cTrader e una sessione <c>ExternalBroker</c>: finche'
/// non sono fatti, la classe e' validata dal motore interno e basta.</para>
/// </summary>
public sealed class PT3B_FDAX_RHL_001_240 : RhlEngine
{
    public override string Name => "PT3B_FDAX_RHL_001_240";

    public override string Description =>
        "RHL FDAX 4 ore, solo long: limit 20 punti sotto il minimo di ieri fino alle 10, dopo sessioni " +
        "ampie (pattern 34), stop e target a 1 ATR, intraday";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    public PT3B_FDAX_RHL_001_240()
    {
        Contracts = 1;

        // Come il contenitore della ricerca: gli orari si confrontano con l'APERTURA della barra.
        ResearchLabelsBarsOnOpen = true;

        // start_hour -1, end_hour 10, verbatim dal percorso e nell'orologio della ricerca.
        TradingWindow = ZonedWindow.AllDay with { End = new TimeOnly(10, 0) };

        TickSize = 1m;                  // tick FDAX dal registro: 1 punto
        LongLevelOffsetTicks = 20;      // LevelOffsetTicks 20: limit 20 punti sotto il minimo di ieri
        ShortLevelOffsetTicks = 20;     // stesso valore del contenitore; lo short e' spento da Direction
        Direction = 1;                  // solo long
        SkipDay = -1;                   // nessun giorno escluso

        NeutralYes = 34;                // richiesto: highD0 > lowD0 * 1,015
        NeutralNo = 56;                 // sentinella: nessun divieto
        DirectionalYes = 52;            // sentinella sempre vera
        DirectionalNo = 53;             // sentinella: nessun divieto

        IntradayOnly = true;            // chiude a fine sessione
        SessionExitTime = null;         // ExitHour -1: nessuna uscita a ora fissa

        StopMoney = 0;                  // StopLoss 0: lo stop e' in ATR
        ProfitMoney = 0;                // TakeProfit 0: il target e' in ATR
        StopAtrMultiplier = 1.0m;       // StopAtr 1,0
        TargetAtrMultiplier = 1.0m;     // TargetAtr 1,0
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 0;                    // nessuna uscita a tempo
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("TrailingStop", out var trailing))
            TrailingStopMoney = Convert.ToInt32(trailing);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("LevelOffsetTicks", out var offset))
        {
            LongLevelOffsetTicks = Convert.ToInt32(offset);
            ShortLevelOffsetTicks = Convert.ToInt32(offset);
        }
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("Direction", out var direction))
            Direction = Convert.ToInt32(direction);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = Convert.ToInt32(exitHour) < 0 ? null : new TimeOnly(Convert.ToInt32(exitHour), 0);
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("StopAtr", out var stopAtr))
            StopAtrMultiplier = Convert.ToDecimal(stopAtr);
        if (parameters.TryGetValue("TargetAtr", out var targetAtr))
            TargetAtrMultiplier = Convert.ToDecimal(targetAtr);
    }
}
