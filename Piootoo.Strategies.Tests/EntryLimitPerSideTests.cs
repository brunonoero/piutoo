using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <c>MaxEntriesPerSession</c> vale <b>per lato</b>, non per strategia.
///
/// <para><b>La fonte.</b> Il motore di ricerca dichiara <c>single_entry_per_session</c> come «al
/// massimo UNA entrata per sessione <b>per direzione</b>» (<c>easy_engine_py/base.py</c>,
/// <c>EngineSignals</c>), e il dossier del paniere lo ripete in §2.2: «Al massimo una entrata per
/// sessione e per direzione».</para>
///
/// <para><b>Cosa costava.</b> La chiave del contatore era <c>simbolo|strategia</c>, senza il lato:
/// riempito il long, lo short restava bloccato per tutto il resto della sessione. Sui motori
/// mirrored — TF_M, PC, BO, VBO, RBB_M, RHL, cioè quasi tutto il paniere — le due gambe nascono
/// sulla stessa barra e sono due segnali indipendenti, non un doppione dello stesso ordine. Il
/// gemello lato server è in <c>SessionEntryLimitTests</c>.</para>
/// </summary>
public sealed class EntryLimitPerSideTests
{
    private const string Code = "PTS_TEST_60";
    private static readonly DateTime SessionStart = new(2025, 3, 3, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BarraSegnale = SessionStart.AddHours(9);

    [Fact]
    public void RiempitoIlLong_LoShortDellaStessaSessionePuoAncoraEntrare()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m, commissionPerContract: 0m);

        // Barra 1: il motore arma entrambe le gambe, come fa ogni mirrored.
        var t1 = BarraSegnale;
        service.ProcessSignals(
            [Stop(SignalType.Buy, 105m, t1), Stop(SignalType.Sell, 95m, t1)],
            Prices(100m), Barre(t1, 100m, 101m, 99m, 100m), t1);

        // Barra 2: il prezzo rompe al rialzo, il long entra.
        var t2 = t1.AddHours(1);
        service.UpdateMarketPrices(Prices(106m), Barre(t2, 100m, 107m, 99m, 106m), t2);
        var dopoLong = service.GetExecutionSnapshot(Code, "NQ", t2).Position;
        Assert.NotNull(dopoLong);
        Assert.Equal(SignalType.Buy, dopoLong!.Direction);

        // Barra 3: il motore riemette entrambe le gambe (valide sulla barra dopo).
        var t3 = t2.AddHours(1);
        service.ProcessSignals(
            [Stop(SignalType.Buy, 110m, t3), Stop(SignalType.Sell, 94m, t3)],
            Prices(106m), Barre(t3, 106m, 107m, 105m, 106m), t3);

        // Barra 4: il prezzo rompe al ribasso. Lo short chiude il long per segnale opposto e apre:
        // e' la seconda entrata della sessione, ma la PRIMA di quel lato.
        var t4 = t3.AddHours(1);
        service.UpdateMarketPrices(Prices(93m), Barre(t4, 106m, 107m, 92m, 93m), t4);

        var dopoShort = service.GetExecutionSnapshot(Code, "NQ", t4).Position;
        Assert.NotNull(dopoShort);
        Assert.Equal(SignalType.Sell, dopoShort!.Direction);
    }

    [Fact]
    public void MaLoStessoLatoNonRientraNellaStessaSessione()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m, commissionPerContract: 0m);

        var t1 = BarraSegnale;
        service.ProcessSignals([Stop(SignalType.Buy, 105m, t1)], Prices(100m), Barre(t1, 100m, 101m, 99m, 100m), t1);

        var t2 = t1.AddHours(1);
        service.UpdateMarketPrices(Prices(106m), Barre(t2, 100m, 107m, 99m, 106m), t2);
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", t2).Position);

        // Chiusura del long con il segnale opposto a mercato, poi riarmo dello stesso lato.
        var t3 = t2.AddHours(1);
        service.ProcessSignals(
            [new TradeSignal
            {
                Date = t3, Type = SignalType.Sell, OrderType = TradeOrderType.Market, Price = 104m,
                Symbol = "NQ", StrategyCode = Code, StrategyName = Code, Quantity = 1, ExitOnly = true
            }],
            Prices(104m), Barre(t3, 106m, 106m, 103m, 104m), t3);
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", t3).Position);

        var t4 = t3.AddHours(1);
        service.ProcessSignals([Stop(SignalType.Buy, 105m, t4)], Prices(104m), Barre(t4, 104m, 104m, 103m, 104m), t4);
        var t5 = t4.AddHours(1);
        service.UpdateMarketPrices(Prices(112m), Barre(t5, 104m, 113m, 103m, 112m), t5);

        // Il long ha gia' consumato il proprio limite in questa sessione.
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", t5).Position);
    }

    [Fact]
    public void LaSessioneSuccessivaRiparteDaZeroSuEntrambiILati()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m, commissionPerContract: 0m);

        var t1 = BarraSegnale;
        service.ProcessSignals([Stop(SignalType.Buy, 105m, t1)], Prices(100m), Barre(t1, 100m, 101m, 99m, 100m), t1);
        var t2 = t1.AddHours(1);
        service.UpdateMarketPrices(Prices(106m), Barre(t2, 100m, 107m, 99m, 106m), t2);

        var t3 = t2.AddHours(1);
        service.ProcessSignals(
            [new TradeSignal
            {
                Date = t3, Type = SignalType.Sell, OrderType = TradeOrderType.Market, Price = 104m,
                Symbol = "NQ", StrategyCode = Code, StrategyName = Code, Quantity = 1, ExitOnly = true
            }],
            Prices(104m), Barre(t3, 106m, 106m, 103m, 104m), t3);

        // Giorno dopo, secchio nuovo.
        var t4 = SessionStart.AddDays(1).AddHours(9);
        service.ProcessSignals(
            [Stop(SignalType.Buy, 105m, t4, SessionStart.AddDays(1))],
            Prices(104m), Barre(t4, 104m, 104m, 103m, 104m), t4);
        var t5 = t4.AddHours(1);
        service.UpdateMarketPrices(Prices(112m), Barre(t5, 104m, 113m, 103m, 112m), t5);

        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", t5).Position);
    }

    private static TradeSignal Stop(
        SignalType side, decimal level, DateTime barTime, DateTime? sessionStart = null) => new()
    {
        Date = barTime,
        Type = side,
        Price = level,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        ValidFromUtc = barTime.AddHours(1),
        ExpiresAtUtc = barTime.AddHours(1),
        TimeframeMinutes = 60,
        MaxEntriesPerSession = 1,
        EntrySessionStartUtc = sessionStart ?? SessionStart
    };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

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
