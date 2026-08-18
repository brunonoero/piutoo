using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_PCH_002 — PC long-only su NQ a 15 minuti. Genera uno stop buy, valido
/// esclusivamente sulla barra successiva, al massimo delle ultime 100 barre più
/// 2 tick. Il canale include la barra appena chiusa che produce il segnale, come
/// <c>highest(high, 100)</c> EasyLanguage e il motore Python: alla chiusura i suoi
/// OHLC sono noti, quindi non c'è look-ahead, e lo stop resta comunque sopra il
/// massimo appena formato.
///
/// <para>
/// Opera tra 13:00 e 04:00 UTC, attraversando la mezzanotte, senza filtro daily
/// né giorno escluso. Richiede <c>pattern_neutral(55)</c>, non richiede
/// <c>pattern_neutral(24)</c>, richiede <c>pattern_dir(2)</c> e rifiuta
/// <c>pattern_dir(6)</c>. Mantiene al massimo un fill per sessione.
/// </para>
///
/// <para>
/// Tutti i livelli di uscita sono USD per contratto NQ: stop $250, target
/// $5.000, breakeven $1.000 e trailing $1.000. La posizione è multiday e non
/// ha una scadenza per numero di barre.
/// </para>///
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
/// <para><b>Misura.</b> Il confine e' stato verificato sui suoi stessi trade di riferimento,
/// <c>run_20260730_0005/trades/top02_PC.csv</c>: raggruppando i 691 ingressi per confine
/// candidato, mezzanotte da' zero violazioni della regola "al massimo un ingresso per sessione e
/// per direzione", mentre il confine alle 17:00 ne da' 35. Prima del 17/08/2026 questa classe
/// dichiarava <c>1700</c>/<c>1600</c> — i numeri di Chicago confrontati con un feed europeo,
/// cioe' un taglio alle 17:00 italiane, in mezzo alla giornata.</para>
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
public sealed class PTS_NQ_PCH_002_15 : PriceChannelEngine
{
    public PTS_NQ_PCH_002_15()
    {
        SessionStartTime = 1700;   // riapertura CME, ora di Chicago
        SessionEndTime = 1600;    // chiusura CME, ora di Chicago
        ChannelBars = 100;
        EnableLong = true;
        EnableShort = false;
        Direction = 1;
        OffsetTicks = 2;
        TickSize = 0.25m;
        StartTime = 600;
        EndTime = 2100;
        TradingWindowInclusive = true;
        NeutralYes = 55;
        NeutralNo = 24;
        DirectionalYes = 2;
        DirectionalNo = 6;
        NotEntryDayLong = -1;
        IntradayOnly = false;
        StopMoney = 250;
        ProfitMoney = 5000;
        BreakEvenMoney = 1000;
        TrailingStopMoney = 1000;
        MaxBars = 0;
        MaxEntriesPerSession = 1;
        Contracts = 1;
    }

    public override string Name => "PTS_NQ_PCH_002_15";
    public override string Description =>
        "PC NQ 15 long-only: Donchian 100, 2 tick, dir 2, esclusione dir 6";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;
}
