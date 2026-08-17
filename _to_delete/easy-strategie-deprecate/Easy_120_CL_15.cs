using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_120 — breakout sugli estremi di N sessioni con filtro ADX, CL 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_120_CL_15__7.txt</c>. Variante legacy di
/// <see cref="SessionBreakoutEngine"/> con livelli fissi d1 (<c>nSess = 1</c>,
/// <c>levIncludeSess0 = 0</c>).</para>
///
/// <para><b>Finestra.</b> Opera tra le 19:00 e le 13:00 con pausa 02:00–07:00. Il filtro ADX(5)
/// deve restare sotto 50.</para>
///
/// <para><b>Uscite.</b> Stop e target monetari più deadline a 4 giorni di calendario
/// (<c>maxdaysintrade</c>). La chiusura condizionata a <c>flatTime</c> è assorbita dalla
/// deadline del motore.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, $1.000 per punto. Stop $2.200, target $3.200.</para>
/// </summary>
public sealed class Easy_120_CL_15 : SessionBreakoutEngine
{
    public override string Name => "Easy_120_CL_15";
    public override string Description => "Breakout N-sessioni + ADX, CL 15m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 15;

    public Easy_120_CL_15()
    {
        UseLegacyVariant = true;
        SessionStartTime = 1800;  // SessBegin
        SessionEndTime = 1700;    // SessEnd
        Contracts = 1;

        Sessions = 1;                    // nSess
        IncludeCurrentSession = false;   // levIncludeSess0 = 0

        AdxLength = 5;       // ADXLen
        AdxThreshold = 50m;  // ADXTH

        StartTime = 1900;   // MyStartTime
        EndTime = 1300;     // MyEndTime
        PauseStart = 200;   // MyStartPause
        PauseEnd = 700;     // MyEndPause

        NeutralYes = 26;    // PtnNeutYes
        NeutralYes2 = 55;   // PtnNeutYes2 — sentinella
        NeutralNo = 45;     // PtnNeutNo
        DirectionalYes = -9;   // ptnDirYes
        DirectionalNo = 12;    // ptnDirNo

        SkipSessionLong = 4;   // SkipSessL
        SkipSessionShort = 0;  // SkipSessS

        StopMoney = 2200;    // MyStop
        ProfitMoney = 3200;  // MyProfit
        MaxDaysInTrade = 4;  // maxdaysintrade
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
