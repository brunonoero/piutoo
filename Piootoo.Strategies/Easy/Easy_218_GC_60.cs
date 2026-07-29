using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_218 — BIAS a conteggio barre con ingresso breakout, su GC a 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_218_GC_60__7.txt</c>. Ogni parametro qui
/// sotto corrisponde uno a uno a un <c>input</c> dell'originale; la logica vive in
/// <see cref="BiasBarCountEngine"/>.</para>
///
/// <para>Le finestre sono volutamente incrociate: <c>twBars(16, 8, ...)</c> ha inizio maggiore
/// della fine, quindi la finestra long attraversa la chiusura di sessione e resta aperta fino
/// alla barra 8 di quella successiva — dove cade anche la sua uscita a tempo. È una struttura
/// overnight, non un errore di parametrizzazione.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, 100 once troy, $100 per punto. Stop $2.000 e
/// target $4.000 per contratto valgono quindi 20 e 40 punti — ed è in punti che vengono
/// applicati a qualunque strumento di esecuzione, future o CFD.</para>
/// </summary>
public sealed class Easy_218_GC_60 : BiasBarCountEngine
{
    public override string Name => "Easy_218_GC_60";
    public override string Description => "BIAS bar-count + breakout stop, GC 60m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public Easy_218_GC_60()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;

        ArmBarLong = 16;     // MyLEBar
        ArmBarShort = 8;     // MySEBar
        ExitBarLong = 8;     // MyLXBar
        ExitBarShort = 16;   // MySXBar
        EndLong = 8;         // endlong
        EndShort = 16;       // endshort

        PatternLibrary = EasyPatternLibrary.Fast;
        PatternLongYes = 15;   // MyPtnLY — body5d < 2 * (highd5 - lowd1)
        PatternLongNo = 44;    // MyPtnLN — (opend0-lowd0) > (opend1-lowd1) * 2
        PatternShortYes = 70;  // MyPtnSY — closed1 > opend1
        PatternShortNo = 114;  // MyPtnSN — (highd1-closed1) < 0.20 * range1d

        NotEntryDayLong = 3;   // MyNotLEDay — mercoledì (0 = domenica)
        NotEntryDayShort = 3;  // MyNotSEDay

        EntryType = BiasEntryType.BreakoutStop;  // entrytype = 2
        BreakoutBarsHigh = 3;  // NHigh
        BreakoutBarsLow = 1;   // NLow

        StopMoney = 2000;      // MyStop, dollari per contratto GC
        ProfitMoney = 4000;    // MyProfit
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
