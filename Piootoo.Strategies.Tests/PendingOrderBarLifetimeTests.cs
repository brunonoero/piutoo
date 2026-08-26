using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ordine "next bar" vive la PROPRIA barra, non un tick dell'orologio di chi lo esegue.
///
/// <para><b>Perché questi test esistono.</b> Il backtest fa girare l'orologio al timeframe
/// <i>minimo</i> del portafoglio e tiene una sola barra per simbolo, quella della serie più fitta.
/// Con una strategia a 30 minuti e tre a 60 nello stesso masterfilter, l'ordine delle 60 nasceva a
/// T+60, veniva provato contro la barra da 30 minuti che copre T+60→T+90 e a T+90 risultava già
/// scaduto: metà del proprio range non veniva mai guardata. Sul broker quello stesso ordine è
/// vivo un'ora intera — <c>Create</c> a T+60, <c>Cancel</c> a T+120 — e nel confronto del
/// 26/08/2026 la differenza era 29 fill interni contro 69 esterni sulle due strategie a limite,
/// con il 54% dei fill esterni nella seconda mezz'ora.</para>
/// </summary>
public class PendingOrderBarLifetimeTests
{
    private const string Code = "PTS_TEST_60";

    // L'orologio del portafoglio è a 30 minuti; la strategia sotto esame è a 60.
    private static readonly DateTime SignalBar = new(2024, 1, 3, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FirstHalf = new(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondHalf = new(2024, 1, 3, 16, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime NextHour = new(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Il livello viene raggiunto nella SECONDA mezz'ora della barra oraria: è il fill che prima
    /// si perdeva, ed è la metà dei fill veri.
    /// </summary>
    [Fact]
    public void UnOrdineA60Minuti_SiRiempieAncheNellaSecondaMezzora()
    {
        var service = Armato(out var _);

        // Prima mezz'ora: il prezzo resta sotto lo stop, nessun fill e l'ordine deve sopravvivere.
        service.UpdateMarketPrices(
            Prices(16_010m),
            Bars(FirstHalf, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            FirstHalf);
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", FirstHalf).Position);
        Assert.Equal(1, service.PendingOrdersCount);

        // Seconda mezz'ora: il livello viene superato.
        service.UpdateMarketPrices(
            Prices(16_060m),
            Bars(SecondHalf, open: 16_012m, high: 16_070m, low: 16_010m, close: 16_060m),
            SecondHalf);

        var position = service.GetExecutionSnapshot(Code, "NQ", SecondHalf).Position;
        Assert.NotNull(position);
        Assert.Equal(16_050m, position!.EntryPrice);
    }

    /// <summary>
    /// Finita la propria ora, però, l'ordine è morto: la barra su cui era valido è passata e
    /// riempirlo dopo è il fill fantasma che <c>orologio-barre-e-fill.md</c> descrive.
    /// </summary>
    [Fact]
    public void OltreLaPropriaOra_LOrdineNonEsistePiu()
    {
        var service = Armato(out var _);

        service.UpdateMarketPrices(
            Prices(16_010m),
            Bars(FirstHalf, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            FirstHalf);
        service.UpdateMarketPrices(
            Prices(16_015m),
            Bars(SecondHalf, open: 16_012m, high: 16_030m, low: 16_005m, close: 16_015m),
            SecondHalf);
        Assert.Equal(1, service.PendingOrdersCount);

        // Ora successiva: l'ordine è scaduto anche se il livello viene ampiamente superato.
        service.UpdateMarketPrices(
            Prices(16_200m),
            Bars(NextHour, open: 16_016m, high: 16_210m, low: 16_014m, close: 16_200m),
            NextHour);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", NextHour).Position);
        Assert.Equal(0, service.PendingOrdersCount);
    }

    /// <summary>
    /// Senza timeframe dichiarato si ricade sul comportamento di prima — un solo tick — così i
    /// segnali che non lo portano non cambiano risultato.
    /// </summary>
    [Fact]
    public void SenzaTimeframeDichiarato_LOrdineViveUnTickSolo()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signal = StopEntry();
        signal.TimeframeMinutes = null;
        service.ProcessSignals([signal], Prices(15_900m), Bars(SignalBar, 15_900m, 15_910m, 15_890m, 15_900m), SignalBar);

        service.UpdateMarketPrices(
            Prices(16_010m),
            Bars(FirstHalf, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            FirstHalf);
        Assert.Equal(1, service.PendingOrdersCount);

        service.UpdateMarketPrices(
            Prices(16_060m),
            Bars(SecondHalf, open: 16_012m, high: 16_070m, low: 16_010m, close: 16_060m),
            SecondHalf);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", SecondHalf).Position);
        Assert.Equal(0, service.PendingOrdersCount);
    }

    private static PiootooTradingService Armato(out TradeSignal signal)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        signal = StopEntry();
        service.ProcessSignals(
            [signal],
            Prices(15_900m),
            Bars(SignalBar, open: 15_900m, high: 15_910m, low: 15_890m, close: 15_900m),
            SignalBar);
        Assert.Equal(1, service.PendingOrdersCount);
        return service;
    }

    private static TradeSignal StopEntry() => new()
    {
        Date = SignalBar,
        Type = SignalType.Buy,
        Price = 16_050m,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        // "Next bar" di una strategia a 60 minuti: nasce alle 16:00 e vale fino alle 17:00.
        ValidFromUtc = FirstHalf,
        ExpiresAtUtc = FirstHalf,
        TimeframeMinutes = 60
    };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1
            }
        };
}
