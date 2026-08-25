using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La barra successiva di un ordine <c>next bar</c> si calcola sul timeframe DICHIARATO dalla
/// strategia, non sulla distanza fra le ultime due barre della serie.
///
/// <para>Sulla prima barra dopo una chiusura — fine settimana, festività, pausa di sessione — quella
/// distanza è il buco: per l'oro alla riapertura della domenica vale circa 49 ore. Con la vecchia
/// deduzione un ordine di una strategia a 30 minuti nasceva con <c>ValidFromUtc</c> ed
/// <c>ExpiresAtUtc</c> spostati di due giorni, quindi veniva piazzato con un'attesa di 174600s (il
/// backtest GC dell'11/08/2013 lo mostra) e il template restava vivo per due giorni invece che per
/// una barra sola, riproposto a ogni claim.</para>
/// </summary>
public sealed class NextBarAcrossGapTests
{
    // Chiusura del fine settimana sull'oro: ultima barra del venerdì, prima barra della domenica.
    private static readonly DateTime UltimaDelVenerdi = new(2013, 8, 9, 20, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime PrimaDellaDomenica = new(2013, 8, 11, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PrimaBarraDopoIlFineSettimana_LOrdineNextBarValeLaBarraSeguenteNonDueGiorniDopo()
    {
        var strategy = new GapProbe();
        var bars = new[]
        {
            Bar(UltimaDelVenerdi.AddMinutes(-30), 1330m),
            Bar(UltimaDelVenerdi, 1331m),
            Bar(PrimaDellaDomenica, 1329m)
        };

        var signal = strategy.ProbeStop(bars, PrimaDellaDomenica);

        Assert.Equal(PrimaDellaDomenica.AddMinutes(30), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    [Fact]
    public void IlTimeframeDichiaratoPrevaleSuUnaSerieDiSpaziaturaDiversa()
    {
        var strategy = new GapProbe();
        // Serie oraria contigua: senza il timeframe dichiarato la deduzione direbbe 60 minuti.
        var t0 = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc);
        var bars = new[] { Bar(t0.AddHours(-1), 100m), Bar(t0, 101m) };

        Assert.Equal(t0.AddMinutes(30), strategy.ProbeStop(bars, t0).ValidFromUtc);
    }

    [Fact]
    public void LaDeduzioneDalDatoUsaLaMinimaDistanzaEnonLUltima()
    {
        var bars = new[]
        {
            Bar(UltimaDelVenerdi.AddMinutes(-30), 1330m),
            Bar(UltimaDelVenerdi, 1331m),
            Bar(PrimaDellaDomenica, 1329m)
        };

        // Overload senza timeframe dichiarato: resta per i chiamanti che non ne hanno uno, e non
        // deve più restituire il buco del fine settimana.
        Assert.Equal(
            PrimaDellaDomenica.AddMinutes(30),
            EasyLib.EstimateNextBarUtc(bars, PrimaDellaDomenica));
    }

    [Fact]
    public void SenzaAlcunaDistanzaPositiva_RestaIlDefaultOrarioEnonUnaBarraFerma()
    {
        var t0 = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc);
        var bars = new[] { Bar(t0, 100m), Bar(t0, 101m) };

        Assert.Equal(t0.AddMinutes(60), EasyLib.EstimateNextBarUtc(bars, t0));
    }

    private static OhlcvData Bar(DateTime time, decimal close) =>
        new()
        {
            DateTime = time,
            Open = close,
            High = close + 1m,
            Low = close - 1m,
            Close = close,
            Volume = 1m
        };

    private sealed class GapProbe : EasyEngineBase
    {
        public override string Name => "GAP_PROBE";
        public override string Description => "next-bar attraverso un buco della serie";
        public override string Symbol => "@GC";
        public override int TimeframeMinutes => 30;

        public TradeSignal ProbeStop(OhlcvData[] data, DateTime barTime) =>
            EntryStopNextBar(SignalType.Buy, 1340m, data, barTime, "stop");
    }
}
