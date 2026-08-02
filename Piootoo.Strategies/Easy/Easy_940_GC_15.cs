using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_940 — Level Fader su pivot S1/R1 della sessione precedente, GC 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_940_GC_15__7.txt</c>. La logica vive in
/// <see cref="LevelFaderEngine"/>; qui restano solo i valori degli <c>input</c>.</para>
///
/// <para><b>Livelli.</b> <c>LevelChoice = 1</c> calcola S1 e R1 dal pivot d1
/// (<c>2×pivot − high</c> / <c>2×pivot − low</c>), con shift zero e tick 0,1.</para>
///
/// <para><b>Calendario.</b> <c>ID = 1</c> disattiva la chiusura di fine sessione; restano stop e
/// target dichiarati all'ingresso. I giorni esclusi seguono la convenzione Python del motore
/// (lunedì = 0, venerdì = 4).</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.100, target $5.000.</para>
/// </summary>
public sealed class Easy_940_GC_15 : LevelFaderEngine
{
    public override string Name => "Easy_940_GC_15";
    public override string Description => "Pivot S1/R1 reversal, GC 15m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 15;

    public Easy_940_GC_15()
    {
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1700;    // sessionEndTimeA
        Contracts = 1;

        LevelChoice = LevelFaderLevel.PreviousSessionPivot;  // S1 / R1
        LevelShift = 0;    // LevelShift
        TickSize = 0.1m;   // MyTick

        StartTrade = 23;   // MyStartTrade 23:00
        EndTrade = 16;     // MyEndTrade 16:00

        NeutralYes = 4;    // PtnNeutYes
        NeutralNo = 1;     // PtnNeutNo
        DirectionalYes = -49;  // PtnDirYes — segno incluso nel parametro

        BaseYesLong = 41;   // PtnLY
        BaseNoLong = 42;    // PtnLN
        BaseYesShort = 41;  // PtnSY
        BaseNoShort = 42;   // PtnSN

        NotEntryDayLong = 0;   // mydayNolong(1) — lunedì
        NotEntryDayShort = 4;  // mydayNoshort(5) — venerdì

        StrategyId = 1;  // ID — nessuna chiusura EOS

        StopMoney = 1100;   // MyStop
        ProfitMoney = 5000; // MyProfit
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
