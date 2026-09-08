using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <c>MaxBarsInPosition</c> si conta sulle <b>barre della strategia dichiarate dal calendario</b>.
///
/// <para><b>Perché questi test esistono.</b> Prima del 08/09/2026 la stessa domanda aveva due
/// risposte sbagliate in due punti diversi. <c>ScaleSignalMaxBarsInPosition</c> moltiplicava N per
/// il rapporto fra timeframe della strategia e orologio del loop — cioè convertiva barre della
/// strategia in tick — mentre l'intent mandato ai cBot portava N non scalato: backtest e live
/// contavano su unità diverse. E il contatore avanzava su ogni barra che il feed consegnava,
/// comprese quelle che il future non ha: le 972 barre di sabato di <c>BTC/60m</c> e le 53 domeniche
/// di <c>FDAX</c>, che facevano morire una posizione multiday prima delle N barre dichiarate.</para>
///
/// <para>Il complemento sta in <see cref="PendingOrderAcrossGapTests"/>: i tick a vuoto — dove il
/// feed non ha nessuna barra — non contano già da prima. Qui si aggiunge che non contano nemmeno le
/// barre vere di un giorno in cui il calendario non dichiara sessione.</para>
/// </summary>
public sealed class MaxBarsOnTheCalendarTests
{
    /// <summary>
    /// Una strategia a 240 minuti su un orologio a 60: N è in barre <b>sue</b>, e quattro tick
    /// dell'orologio dentro lo stesso bucket valgono una barra sola. È ciò che
    /// <c>ScaleSignalMaxBarsInPosition</c> otteneva moltiplicando il numero, e che ora esce dal
    /// conteggio senza toccare il segnale.
    /// </summary>
    [Fact]
    public void MaxBars_CountsStrategyBucketsNotClockTicks()
    {
        // Griglia 240m di NQ, ancorata a mezzanotte europea: d'inverno i bucket aprono alle 23:00,
        // 03:00, 07:00, 11:00, 15:00 e 19:00 UTC.
        var entry = new DateTime(2025, 2, 10, 12, 0, 0, DateTimeKind.Utc);
        var service = OpenedPosition("NQ", entry, timeframeMinutes: 240, maxBars: 2);

        // Resto del bucket 11:00-15:00: tre barre dell'orologio, zero barre della strategia.
        foreach (var minute in new[] { 13, 14 })
        {
            var bar = entry.AddHours(minute - 12);
            service.UpdateMarketPrices(Prices("NQ", 16_000m), Bars("NQ", bar), bar);
            Assert.NotNull(PositionOf(service, "NQ", bar));
        }

        // Bucket 15:00: prima barra della strategia. Quattro tick dopo è ancora la stessa.
        for (var hour = 15; hour <= 18; hour++)
        {
            var bar = new DateTime(2025, 2, 10, hour, 0, 0, DateTimeKind.Utc);
            service.UpdateMarketPrices(Prices("NQ", 16_000m), Bars("NQ", bar), bar);
            Assert.NotNull(PositionOf(service, "NQ", bar));
        }

        // Bucket 19:00: seconda barra della strategia, MaxBars raggiunto.
        var last = new DateTime(2025, 2, 10, 19, 0, 0, DateTimeKind.Utc);
        service.UpdateMarketPrices(Prices("NQ", 16_000m), Bars("NQ", last), last);
        Assert.Null(PositionOf(service, "NQ", last));
    }

    /// <summary>
    /// Il sabato di BTC: il CFD quota, il future no. Ventiquattro barre vere non consumano nemmeno
    /// una barra di posizione, e la seconda barra della posizione è quella della domenica.
    /// </summary>
    [Fact]
    public void MaxBars_DoesNotAdvanceOnADayTheCalendarHasNoSessionFor()
    {
        // Venerdì 21:00 UTC: sessione di venerdì, perché il giorno di sessione di BTC è quello
        // europeo e la mezzanotte locale cade alle 23:00 UTC.
        var entry = new DateTime(2025, 2, 7, 21, 0, 0, DateTimeKind.Utc);
        var service = OpenedPosition("BTC", entry, timeframeMinutes: 60, maxBars: 2);

        // Ultima barra del venerdì: prima barra della posizione.
        var friday = entry.AddHours(1);
        service.UpdateMarketPrices(Prices("BTC", 96_000m), Bars("BTC", friday), friday);
        Assert.NotNull(PositionOf(service, "BTC", friday));

        // Tutto il sabato — dalle 23:00 di venerdì alle 22:00 di sabato UTC — non conta.
        for (var bar = friday.AddHours(1); bar <= new DateTime(2025, 2, 8, 22, 0, 0, DateTimeKind.Utc); bar = bar.AddHours(1))
        {
            service.UpdateMarketPrices(Prices("BTC", 96_000m), Bars("BTC", bar), bar);
            Assert.NotNull(PositionOf(service, "BTC", bar));
        }

        // Prima barra della domenica: seconda barra della posizione, MaxBars raggiunto.
        var sunday = new DateTime(2025, 2, 8, 23, 0, 0, DateTimeKind.Utc);
        service.UpdateMarketPrices(Prices("BTC", 96_000m), Bars("BTC", sunday), sunday);
        Assert.Null(PositionOf(service, "BTC", sunday));
    }

    /// <summary>
    /// Segnale senza timeframe dichiarato: non si inventa una griglia, e ogni barra consegnata vale
    /// una barra. È il ripiego, e resta il comportamento precedente al calendario.
    /// </summary>
    [Fact]
    public void MaxBars_FallsBackToDeliveredBarsWhenTheSignalDeclaresNoTimeframe()
    {
        var entry = new DateTime(2025, 2, 10, 12, 0, 0, DateTimeKind.Utc);
        var service = OpenedPosition("NQ", entry, timeframeMinutes: null, maxBars: 2);

        var first = entry.AddHours(1);
        service.UpdateMarketPrices(Prices("NQ", 16_000m), Bars("NQ", first), first);
        Assert.NotNull(PositionOf(service, "NQ", first));

        var second = entry.AddHours(2);
        service.UpdateMarketPrices(Prices("NQ", 16_000m), Bars("NQ", second), second);
        Assert.Null(PositionOf(service, "NQ", second));
    }

    private const string Code = "PTS_TEST";

    private static PiootooTradingService OpenedPosition(
        string symbol, DateTime entry, int? timeframeMinutes, int maxBars)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        service.ProcessSignals(
            [
                new TradeSignal
                {
                    Date = entry,
                    Type = SignalType.Buy,
                    Price = 0m,
                    Symbol = symbol,
                    StrategyName = Code,
                    StrategyCode = Code,
                    Quantity = 1,
                    OrderType = TradeOrderType.Market,
                    TimeframeMinutes = timeframeMinutes,
                    MaxBarsInPosition = maxBars
                }
            ],
            Prices(symbol, 100m),
            Bars(symbol, entry),
            entry);

        Assert.NotNull(PositionOf(service, symbol, entry));
        return service;
    }

    private static StrategyPositionSnapshot? PositionOf(PiootooTradingService service, string symbol, DateTime at) =>
        service.GetExecutionSnapshot(Code, symbol, at).Position;

    private static Dictionary<string, decimal> Prices(string symbol, decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { [symbol] = price };

    private static Dictionary<string, OhlcvData> Bars(string symbol, DateTime time) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [symbol] = new OhlcvData
            {
                DateTime = time, Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1
            }
        };
}
