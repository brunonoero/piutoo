using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_342 — ingresso a mercato su estensione ATR dall'apertura di sessione, NQ 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_342_NQ_15__7.txt</c>. Non usa livelli di
/// rottura: entra a mercato quando il close si è allontanato dall'apertura di sessione di un
/// multiplo di <c>AvgTrueRange(200)</c> — 4× verso l'alto, 9,5× verso il basso — con i gate
/// <c>PatternFast</c> per verso e un solo ingresso al giorno.</para>
///
/// <para><b>Perché non era bloccata.</b> L'originale ha un'uscita su utile aperto
/// (<c>openpositionprofit &gt; PP</c>), ma <c>PP</c> vale <b>0</b> di default e la condizione
/// richiede <c>PP &gt; 0</c>: quel ramo non viene mai eseguito. Restano stop, breakeven e il
/// limite di 7 giorni in posizione, tutti dichiarabili all'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.600 = 80 punti,
/// breakeven $1.000 = 50 punti. Nessun target (<c>MyProfit = 0</c>).</para>
/// </summary>
public sealed class Easy_342_NQ_15 : TrendDeveloperEngine
{
    public override string Name => "Easy_342_NQ_15";
    public override string Description => "Ingresso a mercato su estensione ATR di sessione, NQ 15m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    // L'ATR a 200 periodi è la finestra più lunga richiesta da questa strategia e supera le sei
    // sessioni del default: serve coprirla, altrimenti il gate lavora su una media parziale.
    public override int RequiredCandles => Math.Max(SessionsToCandles(6), 240);

    public Easy_342_NQ_15()
    {
        SessionStartTime = 1700;  // sessionStartTimeC
        SessionEndTime = 1600;    // sessionEndTimeC
        Contracts = 1;

        MarketEntry = true;  // next bar market: nessun livello di rottura

        StartTrade = 1800;  // MyStartTrade
        EndTrade = 1500;    // MyEndTrade
        PauseStart = 1200;  // MyStartPause — coppia invertita: pausa inattiva
        PauseEnd = 1100;    // MyEndPause
        MaxTradesPerDay = 1;  // MaxEntriesPerDay

        // Questa variante non usa i gate neutro/direzionale: sentinelle.
        NeutralYes = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        FastYesLong = 26;   // MyPtnLY
        FastNoLong = 134;   // MyPtnLN
        FastYesShort = 39;  // MyPtnSY
        FastNoShort = 88;   // MyPtnSN

        AtrGateLength = 200;             // AvgTrueRange(200)
        AtrGateMultiplierLong = 4m;      // PortATRpiu
        AtrGateMultiplierShort = 9.5m;   // PortATRmeno

        StopMoney = 1600;       // MyStop
        ProfitMoney = 0;        // MyProfit
        BreakEvenMoney = 1000;  // MyBreakEven
        MaxDaysInTrade = 7;     // MaxDaysInTrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
