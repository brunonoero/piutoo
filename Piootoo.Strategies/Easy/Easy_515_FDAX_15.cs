using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_515 — Level Fader su falsi breakout degli estremi d1, FDAX 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_515_FDAX_15__7.txt</c>. La logica vive in
/// <see cref="LevelFaderEngine"/>; qui restano solo i valori degli <c>input</c>.</para>
///
/// <para><b>Livelli.</b> <c>LevelChoice = 2</c> usa massimo e minimo della sessione precedente,
/// con <c>LevelShift = 7</c> tick da <c>MyTick = 1</c>. Il recross del close arma un ingresso a
/// mercato sulla barra successiva.</para>
///
/// <para><b>Limiti del motore.</b> I gate <c>PatternFast</c> estesi (PtnLY_ext … PtnSN_ext) e la
/// pausa invertita 12:00/11:00 non sono parametri del motore: restano disattivati o assorbiti dai
/// default. Gli stop long/short differenziati sono riassunti con <c>StopMoney</c> al massimo dei
/// due lati.</para>
///
/// <para><b>Contratto di riferimento:</b> FDAX, €25 per punto. Stop €4.000, target short €5.000.</para>
/// </summary>
public sealed class Easy_515_FDAX_15 : LevelFaderEngine
{
    public override string Name => "Easy_515_FDAX_15";
    public override string Description => "Mean reversion su falsi breakout max/min d1, FDAX 15m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 15;

    public Easy_515_FDAX_15()
    {
        SessionStartTime = 800;   // sessionStartTimeA
        SessionEndTime = 2200;    // sessionEndTimeA
        Contracts = 1;

        LevelChoice = LevelFaderLevel.PreviousSessionExtremes;  // highd1 / lowd1
        LevelShift = 7;   // LevelShift
        TickSize = 1m;    // MyTick

        StartTrade = 8;   // MyStartTime 08:00 (finestra oraria per ora)
        EndTrade = 14;    // MyEndTime 14:45

        BaseYesLong = 41;   // PtnLY
        BaseNoLong = 42;    // PtnLN
        BaseYesShort = 41;  // PtnSY
        BaseNoShort = 42;   // PtnSN

        NotEntryDayLong = 4;   // SkipSessL(5) — venerdì, convenzione Python
        NotEntryDayShort = 4;  // SkipSessS(5)

        StrategyId = 1;  // nessuna chiusura EOS automatica dell'originale
        MaxEntriesPerSession = 2;  // MaxTradesPerDay

        StopMoney = 4000;    // max(MyStopL, MyStopS)
        ProfitMoney = 5000;  // MyProfitS (long senza target)
        MaxDaysInTrade = 6;  // MaxDaysLong / MaxDaysShort
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
