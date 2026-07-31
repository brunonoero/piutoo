using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_298 — breakout sugli estremi della sessione precedente, NQ 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_298_NQ_30__7.txt</c>.</para>
///
/// <para><b>Livello fisso, non progressivo.</b> A differenza della 287, questa variante non ha
/// <c>levIncludeSess0</c>: i livelli sono fissati all'apertura sugli estremi della sessione
/// precedente e non si allargano seguendo la sessione in corso.</para>
///
/// <para><b>Gate su PtnBaseSA2</b> per verso, più esclusioni di giorno e di mese. Nessun filtro
/// ADX. <c>MyPtnLY = 41</c> è la sentinella "sempre vero": il long è gate-ato solo dal divieto
/// <c>MyPtnLN = 36</c>.</para>
///
/// <para><b>Uscita:</b> <c>maxdaysintrade = 4</c> con <c>setexitonclose</c>, qui espresso come
/// deadline a 4 giorni sull'ingresso. <c>ID = 1</c> disattiva la chiusura di fine sessione.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.800 = 90 punti,
/// target $2.500 = 125 punti.</para>
/// </summary>
public sealed class Easy_298_NQ_30 : SessionBreakoutEngine
{
    public override string Name => "Easy_298_NQ_30";
    public override string Description => "Breakout estremi sessione precedente, NQ 30m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public Easy_298_NQ_30()
    {
        SessionStartTime = 1700;  // SessionStartTimeA
        SessionEndTime = 1600;    // sessionEndTimeA
        Contracts = 1;

        Sessions = 1;                   // NSessions
        IncludeCurrentSession = false;  // nessun levIncludeSess0 in questa variante

        AdxLength = 0;  // nessun filtro ADX

        StartTime = 1200;   // MyStartTime
        EndTime = 1600;     // MyEndTime
        PauseStart = 1200;  // MyStartPause — coppia invertita: pausa inattiva
        PauseEnd = 1100;    // MyEndPause

        // Questa variante non usa i gate neutro/direzionale: sentinelle.
        NeutralYes = 55;
        NeutralYes2 = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        BaseYesLong = 41;   // MyPtnLY — sentinella "sempre vero"
        BaseNoLong = 36;    // MyPtnLN
        BaseYesShort = 19;  // MyPtnSY
        BaseNoShort = 35;   // MyPtnSN

        NotEntryDayLong = -1;      // mydayNolong
        NotEntryDayShort = -1;     // mydayNoshort
        NotEntryMonthLong = 6;     // NoTradingMonthLong
        NotEntryMonthShort = 4;    // NoTradingMonthshort

        StopMoney = 1800;    // MyStop
        ProfitMoney = 2500;  // MyProfit
        MaxDaysInTrade = 4;  // maxdaysintrade
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
