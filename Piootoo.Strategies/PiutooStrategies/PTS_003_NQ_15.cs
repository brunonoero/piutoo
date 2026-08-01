using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_003 — PC long-only su NQ a 15 minuti. Genera uno stop buy, valido
/// esclusivamente sulla barra successiva, al massimo delle 100 barre precedenti
/// più 2 tick. Il canale esclude la barra di segnale, quindi non usa dati futuri.
///
/// <para>
/// Opera tra 13:00 e 04:00 UTC, attraversando la mezzanotte, senza filtro daily
/// né giorno escluso. Richiede <c>pattern_neutral(55)</c>, non richiede
/// <c>pattern_neutral(24)</c>, richiede <c>pattern_dir(2)</c> e rifiuta
/// <c>pattern_dir(6)</c>. Mantiene al massimo un fill per sessione CME
/// 17:00–16:00 UTC.
/// </para>
///
/// <para>
/// Tutti i livelli di uscita sono USD per contratto NQ: stop $250, target
/// $5.000, breakeven $1.000 e trailing $1.000. La posizione è multiday e non
/// ha una scadenza per numero di barre.
/// </para>
/// </summary>
public sealed class PTS_003_NQ_15 : PriceChannelEngine
{
    public PTS_003_NQ_15()
    {
        SessionStartTime = 1700;
        SessionEndTime = 1600;
        ChannelBars = 100;
        EnableLong = true;
        EnableShort = false;
        Direction = 1;
        OffsetTicks = 2;
        TickSize = 0.25m;
        StartTime = 1300;
        EndTime = 400;
        TradingWindowInclusive = true;
        NeutralYes = 55;
        NeutralNo = 24;
        DirectionalYes = 2;
        DirectionalNo = 6;
        NotEntryDayLong = -1;
        StopMoney = 250;
        ProfitMoney = 5000;
        BreakEvenMoney = 1000;
        TrailingStopMoney = 1000;
        MaxBars = 0;
        MaxEntriesPerSession = 1;
        Contracts = 1;
    }

    public override string Name => "PTS_003_NQ_15";
    public override string Description =>
        "PC NQ 15 long-only: Donchian 100, 2 tick, dir 2, esclusione dir 6";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;
}
