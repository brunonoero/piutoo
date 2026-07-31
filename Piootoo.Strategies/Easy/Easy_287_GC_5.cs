using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_287 — breakout sugli estremi della sessione precedente, con filtro ADX, su GC a 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_287_GC_5__7.txt</c>. La logica vive in
/// <see cref="SessionBreakoutEngine"/>; qui restano solo i valori degli <c>input</c>.</para>
///
/// <para>Con <c>nSess = 1</c> e <c>levIncludeSess0 = 1</c> il livello long parte dal massimo
/// della sessione precedente e si allarga barra dopo barra seguendo il massimo di quella in
/// corso: è un breakout su nuovi massimi assoluti, non su un livello fisso.</para>
///
/// <para><b>Uscite.</b> Solo stop, breakeven e target: <c>maxdaysintrade = 0</c> disattiva
/// interamente il blocco di uscita a giorni dell'originale, quindi non esiste uscita a tempo e
/// la posizione può restare aperta finché uno dei tre livelli non viene toccato. È interamente
/// dichiarabile all'ingresso, quindi la strategia non è close-dependent.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.500 = 25 punti,
/// breakeven $2.250 = 22,5 punti, target $5.500 = 55 punti.</para>
/// </summary>
public sealed class Easy_287_GC_5 : SessionBreakoutEngine
{
    public override string Name => "Easy_287_GC_5";
    public override string Description => "Breakout estremi di sessione + filtro ADX, GC 5m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 5;

    public Easy_287_GC_5()
    {
        // Questa strategia deriva dal sorgente EasyLanguage, non dal contratto BO Python.
        UseLegacyVariant = true;
        SessionStartTime = 1800;  // SessBegin
        SessionEndTime = 1700;    // SessEnd
        Contracts = 1;

        Sessions = 1;                  // nSess
        IncludeCurrentSession = true;  // levIncludeSess0 = 1

        AdxLength = 5;        // ADXLen
        AdxThreshold = 55m;   // ADXTH

        StartTime = 100;    // MyStartTime
        EndTime = 1700;     // MyEndTime — fine esclusiva, come tw()
        PauseStart = 600;   // MyStartPause
        PauseEnd = 800;     // MyEndPause

        NeutralYes = 4;    // PtnNeutYes
        NeutralYes2 = 55;  // PtnNeutYes2 — sentinella "sempre vero"
        NeutralNo = 45;    // PtnNeutNo
        DirectionalYes = 1;   // ptnDirYes
        DirectionalNo = 10;   // ptnDirNo

        SkipSessionLong = 0;   // SkipSessL
        SkipSessionShort = 3;  // SkipSessS

        StopMoney = 2500;       // MyStop
        BreakEvenMoney = 2250;  // MyBreakeven
        ProfitMoney = 5500;     // MyProfit
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
