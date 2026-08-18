using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFM_001 — trend following mirrored su NQ a 60 minuti.
///
/// <para>Sessione CME, dichiarata come <c>0</c>/<c>2359</c> per la ragione spiegata sotto.
/// Finestra operativa inclusiva 16:00–03:00. Gate neutri 47/1 e direzionali mirrored 50/8.</para>
///
/// <para>Ingresso stop su <c>H_d1</c>/<c>L_d1</c>, valido solo sulla barra successiva. Un fill
/// per lato per sessione è gestito dal motore TF. <c>intraday_only = false</c>: nessuna chiusura
/// di fine sessione, posizioni multiday.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.000, target $3.000.</para>///
/// <para><b>Confine di sessione: perche' 0 / 2359 e non 1700 / 1600.</b> La riapertura CME delle
/// 17:00 di Chicago e' mezzanotte in Italia — lo stesso istante, scritto in due orologi. Il feed
/// <c>@NQ</c> e' stampato in ora locale europea nonostante la <c>Z</c> nel campo
/// <c>dateTime</c>: misurato due volte, il picco di volume dell'apertura cash di New York resta
/// alle 15:30 sia d'inverno sia d'estate (in UTC vero si sposterebbe a 14:30 e 13:30) e la pausa
/// di manutenzione CME cade alle 23:15–23:45 in entrambe le stagioni. Finche' <c>EasyLib</c>
/// confronta l'orario grezzo della barra — la migrazione a <c>SessionClock</c> non e' completa —
/// il numero corretto per questa sessione, su questo feed, e' <c>0</c>. Dopo la migrazione, con
/// gli orari letti in ora di borsa, tornera' a essere <c>1700</c>/<c>1600</c>: le due codifiche
/// descrivono la stessa sessione e vanno ribaltate insieme, mai una alla volta. Vedi
/// <c>docs/domini/mappa-strategie-pts.md</c> e <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Non verificata sul run di origine.</b> A differenza delle altre PTS, il run che ha
/// prodotto questa strategia non e' sul disco — vedi <c>mappa-strategie-pts.md</c> — quindi il
/// confine non e' stato misurato sui suoi trade. Il cambio a <c>0</c> regge comunque per entrambe
/// le provenienze possibili: se gli orari venissero da una sorgente EasyLanguage, <c>1700</c>
/// significa la riapertura di Chicago, che su questo feed e' <c>0</c>; se venissero da un run
/// Python come le altre, il confine e' la mezzanotte europea, di nuovo <c>0</c>. Resta invece
/// <b>non verificata la finestra 09:00–20:00 Chicago</b>: se fosse in ora di Chicago andrebbe spostata di
/// sette ore, e non c'e' modo di deciderlo finche' il run non salta fuori.</para>
///
/// <para><b>Gli orari sono in ora di borsa (America/Chicago), non nell'orologio del feed.</b>
/// La sessione e' la giornata CME 17:00–16:00 e la finestra operativa e' la stessa della ricerca,
/// riespressa: il motore Python lavorava su barre in ora europea e dichiarava gli orari in CET,
/// che e' Chicago piu' sette ore. Il motore converte l'istante UTC della barra in ora di Chicago
/// e confronta li', quindi il risultato non dipende piu' da come e' stampato il feed. Vedi
/// <c>docs/domini/orari-di-sessione-e-fusi.md</c> e <c>docs/domini/mappa-strategie-pts.md</c>.</para>
///
/// <para><b>Residuo noto.</b> Mezzanotte CET e le 17:00 di Chicago sono lo stesso istante tranne
/// nelle circa quattro settimane l'anno in cui l'ora legale americana ed europea non sono
/// allineate. In quelle giornate — il 6,6% dei trade delle liste di riferimento — questa classe
/// segue la sessione CME vera e diverge dalla ricerca, deliberatamente.</para>
/// </summary>
public sealed class PTS_NQ_TFM_001_60 : TfMirroredEngine
{
    public override string Name => "PTS_NQ_TFM_001_60";
    public override string Description => "TF_M NQ 60: breakout H/L d1, pattern mirrored, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 60;

    public PTS_NQ_TFM_001_60()
    {
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        Contracts = 1;

        StartHour = 9;
        EndHour = 20;
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
            StartHour = Convert.ToInt32(startHour);
        if (parameters.TryGetValue("EndHour", out var endHour))
            EndHour = Convert.ToInt32(endHour);
    }
}
