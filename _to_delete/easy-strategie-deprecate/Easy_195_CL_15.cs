using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_195 — breakout stop su estremi d1 con finestra overnight, CL 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_195_CL_15____1440__7.txt</c>. Mappata su
/// <see cref="TrendDeveloperEngine"/> con trigger sulla sessione precedente; i due gate propri
/// dell'originale, che non sono parametri del motore, vivono in <c>PassesExtraGates</c>.</para>
///
/// <para><b>Compressione</b> (<c>Compression_condition</c>, su <c>data1</c>): il corpo fra
/// <c>Open[2]</c> e <c>Close[1]</c> deve stare sotto il 60% dell'escursione delle due barre
/// precedenti. <c>Algoritmica_Workshop_Highest_HighS(1, 2)</c> è il massimo di <c>High[1]</c> e
/// <c>High[2]</c> — la barra corrente è esclusa.</para>
///
/// <para><b>Volatilità</b> (<c>Volatility_condition</c>, su <c>data2</c> giornaliero): il true range
/// della sessione in corso deve stare sotto l'ATR(5) delle sessioni <i>precedenti</i>. L'originale
/// scrive <c>AvgTrueRange(5)[1] of data2</c>, e quell'<c>[1]</c> è sostanziale: esclude dalla media
/// la sessione con cui la si confronta. La serie giornaliera è aggregata dal feed intraday da
/// <see cref="EasyLib.BuildSessionSeries"/>, quindi non serve un datafeed a 1440 separato.</para>
///
/// <para><b>Livelli.</b> L'originale applica <c>LevelShift</c> ai trigger; il motore usa gli
/// estremi d1 puri (<c>highd1</c>/<c>lowd1</c>). Documentato come approssimazione accettabile
/// finché il motore non espone lo shift.</para>
///
/// <para><b>Uscite.</b> Stop, breakeven, target e deadline a 3 giorni dichiarati all'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, $1.000 per punto. Stop $1.400, breakeven $1.300,
/// target $2.400.</para>
/// </summary>
public sealed class Easy_195_CL_15 : TrendDeveloperEngine
{
    private const int CompressionLength = 2;      // Compression_Lenght
    private const decimal CompressionTrigger = 0.6m;
    private const int VolatilityAtrSessions = 5;  // AvgTrueRange(5) of data2
    private const decimal VolatilityMultiplier = 1m;  // Multi_atr

    public override string Name => "Easy_195_CL_15";
    public override string Description => "Breakout d1 con finestra overnight, CL 15m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 15;

    // L'ATR su data2 consuma cinque sessioni, più quella confrontata, quella che serve al primo
    // true range e una di margine per la prima sessione troncata della finestra.
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        SessionsToCandles(VolatilityAtrSessions + 3));

    public Easy_195_CL_15()
    {
        SessionStartTime = 1800;  // StartSessionTimeC
        SessionEndTime = 1700;    // EndSessionTimeC
        Contracts = 1;            // MySize

        Trigger = TrendTrigger.PreviousSessionOhlc;

        StartTrade = 2000;  // StartTrade
        EndTrade = 1200;    // EndTrade — fine esclusa
        PauseStart = 100;   // StartPause
        PauseEnd = 400;     // EndPause

        NeutralYes = 26;  // PtnNeutYes
        NeutralNo = 1;    // PtnNeutNo
        DirectionalYes = 27;  // PtnDirYes
        DirectionalNo = 8;    // PtnDirNo

        NotEntryDayLong = 0;   // SkipDayLong(0) — domenica
        NotEntryDayShort = 2;  // SkipDayShort(2) — martedì

        MaxDaysInTrade = 3;  // MaxDaysInTrade

        StopMoney = 1400;       // SL
        BreakEvenMoney = 1300;  // BE
        ProfitMoney = 2400;     // TP
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    protected override bool PassesExtraGates(decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        PassesCompression(data) && PassesVolatility(data, barTime);

    /// <summary>
    /// <c>absvalue(opens(2) - closes(1)) &lt; (Highest_HighS(1,2) - Lowest_LowS(1,2)) * 0,6</c>.
    /// Tutti i riferimenti sono arretrati di almeno una barra: la corrente non entra.
    /// </summary>
    private static bool PassesCompression(OhlcvData[] data)
    {
        if (data.Length < CompressionLength + 1)
            return false;

        var body = Math.Abs(data[^(CompressionLength + 1)].Open - data[^2].Close);

        var highest = decimal.MinValue;
        var lowest = decimal.MaxValue;
        for (var barsAgo = 1; barsAgo <= CompressionLength; barsAgo++)
        {
            highest = Math.Max(highest, data[^(barsAgo + 1)].High);
            lowest = Math.Min(lowest, data[^(barsAgo + 1)].Low);
        }

        return body < (highest - lowest) * CompressionTrigger;
    }

    /// <summary>
    /// <c>TrueRange of data2 &lt; AvgTrueRange(5)[1] of data2 * Multi_atr</c>: la sessione in corso
    /// contro la media delle precedenti, che la esclude.
    /// </summary>
    private bool PassesVolatility(OhlcvData[] data, DateTime barTime)
    {
        var sessions = EasyLib.BuildSessionSeries(SessionStartTime, SessionEndTime, data, barTime);
        if (sessions.Length < VolatilityAtrSessions + 2)
            return false;

        var currentRange = EasyLib.TrueRange(sessions[^1], sessions[^2]);
        var averageRange = EasyLib.AvgTrueRange(sessions, VolatilityAtrSessions, barsAgo: 1);
        return currentRange < averageRange * VolatilityMultiplier;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
