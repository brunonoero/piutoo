using Piootoo.Shared.Configuration;
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
/// </para>/// </summary>
public sealed class PTS_NQ_PCH_002_15 : PriceChannelEngine
{
    public PTS_NQ_PCH_002_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        ChannelBars = 100;
        EnableLong = true;
        EnableShort = false;
        Direction = 1;
        OffsetTicks = 2;
        TickSize = 0.25m;
        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(13, 4);
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
