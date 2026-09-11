using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ordine a mercato si riempie all'apertura di una barra <b>vera</b>, mai al prezzo di mark.
///
/// <para><b>Perché questi test esistono.</b> Il percorso immediato di <c>ProcessSignals</c> — un
/// market la cui barra di validità è già cominciata — apriva anche senza una barra del simbolo
/// sul tick, ripiegando sul prezzo di mark: l'ultima chiusura nota, di ore o giorni prima. Su
/// compare-0033 erano 170 ingressi interni su minuti in cui il feed non ha una barra, 71 dei 131
/// di <c>PTS_YM_BIA_001_240</c>, quasi tutti il sabato alle 00:00 UTC alla chiusura del venerdì:
/// la strategia rivalutava la barra del venerdì su un tick del fine settimana e il market, con
/// <c>ValidFromUtc</c> già passato, si eseguiva lì. Il cBot, giustamente, quei trade non li ha.</para>
/// </summary>
public sealed class MarketOrderWithoutBarTests
{
    private const string Code = "PTS_TEST_MKT_60";

    private static readonly DateTime UltimaDelVenerdi = new(2025, 2, 7, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ProiezioneNelVuoto = new(2025, 2, 7, 21, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SabatoNotte = new(2025, 2, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PrimaDellaDomenica = new(2025, 2, 9, 23, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Il caso di compare-0033: il segnale arriva su un tick senza barra e con la validità già
    /// cominciata. Non deve aprire al mark; deve aspettare la prima barra e aprire alla sua
    /// apertura, che è ciò che fa il conto vero alla riapertura della domenica.
    /// </summary>
    [Fact]
    public void SenzaBarra_IlMarketAspettaLaPrimaBarraVeraEApreAllaSuaApertura()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Il segnale arriva dentro la propria barra di validita' (21:00-22:00 del venerdi'), ma il
        // feed non ha barre: la serie si e' fermata alle 20:00.
        var dentroLaBarra = ProiezioneNelVuoto.AddMinutes(30);
        service.ProcessSignals([MarketEntry()], Prices(15_900m), SenzaBarre(), dentroLaBarra);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", dentroLaBarra).Position);
        Assert.Equal(1, service.PendingOrdersCount);

        // Tick nel vuoto, fine settimana compreso: niente da riempire e niente da far scadere,
        // perche' l'ordine non ha ancora visto una barra.
        for (var t = SabatoNotte; t < PrimaDellaDomenica; t = t.AddHours(1))
        {
            service.UpdateMarketPrices(Prices(15_900m), SenzaBarre(), t);
            Assert.Null(service.GetExecutionSnapshot(Code, "NQ", t).Position);
        }

        // Riapertura: si entra all'apertura della barra, non al mark del venerdì.
        service.UpdateMarketPrices(
            Prices(16_060m),
            Barre(PrimaDellaDomenica, open: 16_000m, high: 16_070m, low: 15_990m, close: 16_060m),
            PrimaDellaDomenica);

        var position = service.GetExecutionSnapshot(Code, "NQ", PrimaDellaDomenica).Position;
        Assert.NotNull(position);
        Assert.Equal(16_000m, position!.EntryPrice);
        Assert.Equal(0, service.PendingOrdersCount);
    }

    /// <summary>
    /// La regressione opposta: con la barra del tick presente il market resta immediato, e apre
    /// all'apertura di quella barra come ha sempre fatto.
    /// </summary>
    [Fact]
    public void ConLaBarra_IlMarketRestaImmediato()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        service.ProcessSignals(
            [MarketEntry()],
            Prices(15_910m),
            Barre(ProiezioneNelVuoto, open: 15_905m, high: 15_920m, low: 15_900m, close: 15_910m),
            ProiezioneNelVuoto);

        var position = service.GetExecutionSnapshot(Code, "NQ", ProiezioneNelVuoto).Position;
        Assert.NotNull(position);
        Assert.Equal(15_905m, position!.EntryPrice);
        Assert.Equal(0, service.PendingOrdersCount);
    }

    private static TradeSignal MarketEntry() => new()
    {
        Date = UltimaDelVenerdi,
        Type = SignalType.Buy,
        Price = 15_900m,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Market,
        ValidFromUtc = ProiezioneNelVuoto,
        ExpiresAtUtc = ProiezioneNelVuoto,
        TimeframeMinutes = 60
    };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> SenzaBarre() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, OhlcvData> Barre(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1
            }
        };
}
