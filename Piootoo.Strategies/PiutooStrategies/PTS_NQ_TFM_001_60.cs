using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFM_001 — trend following mirrored su NQ a 60 minuti.
///
/// <para>Sessione = giorno di calendario europeo (<c>0</c>/<c>2359</c> in ora della ricerca),
/// come il motore Python; non la giornata CME.
/// Finestra operativa inclusiva 16:00–03:00. Gate neutri 47/1 e direzionali mirrored 50/8.</para>
///
/// <para>Ingresso stop su <c>H_d1</c>/<c>L_d1</c>, valido solo sulla barra successiva. Un fill
/// per lato per sessione è gestito dal motore TF. <c>intraday_only = false</c>: nessuna chiusura
/// di fine sessione, posizioni multiday.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.000, target $3.000.</para>///
/// <para><b>Non verificata sul run di origine.</b> A differenza delle altre PTS, il run che ha
/// prodotto questa strategia non e' sul disco — vedi <c>mappa-strategie-pts.md</c> — quindi il
/// confine non e' stato misurato sui suoi trade. Il cambio a <c>0</c> regge comunque per entrambe
/// le provenienze possibili: se gli orari venissero da una sorgente EasyLanguage, <c>1700</c>
/// significa la riapertura di Chicago, che su questo feed e' <c>0</c>; se venissero da un run
/// Python come le altre, il confine e' la mezzanotte europea, di nuovo <c>0</c>. Resta invece
/// <b>non verificata la finestra 16:00–03:00 CET</b>: se fosse in ora di Chicago andrebbe spostata di
/// sette ore, e non c'e' modo di deciderlo finche' il run non salta fuori.</para>
/// </summary>
public sealed class PTS_NQ_TFM_001_60 : TfMirroredEngine
{
    public override string Name => "PTS_NQ_TFM_001_60";
    public override string Description => "TF_M NQ 60: breakout H/L d1, pattern mirrored, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 60;

    public PTS_NQ_TFM_001_60()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(16, 3);
        SkipDay = -1;

        NeutralYes = 47;
        NeutralNo = 1;
        DirectionalYes = 50;
        DirectionalNo = 8;

        IntradayOnly = false;

        StopMoney = 1000;
        ProfitMoney = 3000;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("StopLoss", out var stop))
            StopMoney = Convert.ToInt32(stop);
        if (parameters.TryGetValue("TakeProfit", out var profit))
            ProfitMoney = Convert.ToInt32(profit);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
