using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Stop e target in multipli dell'ATR delle sessioni chiuse (<c>StopAtrMultiplier</c>,
/// <c>TargetAtrMultiplier</c>).
///
/// <para><b>Cosa impone.</b> Con i multipli a zero nulla cambia: il segnale porta il denaro fisso
/// di sempre. Con un multiplo, il denaro per contratto del segnale e' ATR × valore del punto ×
/// multiplo, con l'ATR misurato sulle sole sessioni <b>chiuse</b> (Legge Zero). Il segnale esce
/// comunque in denaro: chi esegue non distingue i due casi, e il cBot non cambia.</para>
/// </summary>
public sealed class AtrStopTests(ITestOutputHelper output)
{
    [Fact]
    public void WithoutMultipliersTheSignalCarriesTheFixedMoney()
    {
        var signal = Evaluate(new AtrPriceChannel(), BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(5000m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(4500m, signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// Barre piatte con range 10 punti in ogni sessione: il true range di ogni sessione chiusa e'
    /// 10 (la chiusura precedente sta dentro il range), quindi ATR = 10 punti esatti e lo stop a
    /// 2 ATR vale 20 punti × valore del punto.
    /// </summary>
    [Fact]
    public void WithMultipliersTheMoneyIsAtrTimesPointValueTimesMultiplier()
    {
        var signal = Evaluate(
            new AtrPriceChannel { StopAtr = 2m, TargetAtr = 3.5m },
            BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)));

        var pointValue = InstrumentRegistry.PointValue("@FDAX");
        output.WriteLine($"valore del punto FDAX {pointValue} · stop {signal.StopLossMoneyPerFutureContract} · target {signal.TakeProfitMoneyPerFutureContract}");

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(10m * pointValue * 2m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(10m * pointValue * 3.5m, signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// Senza 15 sessioni chiuse di storia l'ATR non esiste e si ripiega sul denaro fisso: un
    /// segnale senza stop non deve mai nascere per una questione di riscaldamento.
    /// </summary>
    [Fact]
    public void WithoutEnoughHistoryTheFixedMoneyIsTheFallback()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)).TakeLast(36).ToArray();
        var signal = Evaluate(new AtrPriceChannel { StopAtr = 2m }, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(5000m, signal.StopLossMoneyPerFutureContract);
    }

    private static TradeSignal Evaluate(PriceChannelEngine strategy, OhlcvData[] bars) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "FDAX",
                BarTimeUtc = bars[^1].DateTime
            }
        });

    private static OhlcvData[] BuildFourHourBars(DateTime lastTime)
    {
        var bars = new OhlcvData[300];
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = lastTime.AddHours((index - bars.Length + 1) * 4),
                Open = 100m,
                High = 110m,
                Low = 100m,
                Close = 105m,
                Volume = 1m
            };
        }

        return bars;
    }

    private sealed class AtrPriceChannel : PriceChannelEngine
    {
        public AtrPriceChannel()
        {
            ChannelBars = 3;
            TickSize = 0.5m;
            IntradayOnly = true;
            Direction = 1;
            StopMoney = 5000;
            ProfitMoney = 4500;
        }

        public decimal StopAtr { set => StopAtrMultiplier = value; }
        public decimal TargetAtr { set => TargetAtrMultiplier = value; }

        public override string Name => "TEST_ATR_PC_FDAX_240";
        public override string Description => "Price Channel di prova con stop in ATR";
        public override string Symbol => "@FDAX";
        public override int TimeframeMinutes => 240;
    }
}
