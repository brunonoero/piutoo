using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Regola operativa: nel fine settimana non devono restare né posizioni né ordini. Chiudere le
/// posizioni non basta, ed è il motivo per cui questi test esistono: uno stop emesso sull'ultima
/// barra della settimana scade sulla barra *successiva*, che è la prima della settimana dopo, e
/// senza cancellazione esplicita riempirebbe sul gap di riapertura — a un prezzo che il venerdì
/// nessuno aveva ancora visto.
/// </summary>
public class WeekEndFlatTests
{
    private const string Code = "TOP_UA_218";

    // Venerdì 5 gennaio 2024, ultima barra 15m della settimana di trading.
    private static readonly DateTime FridayLastBar = new(2024, 1, 5, 22, 45, 0, DateTimeKind.Utc);

    // Lunedì 8 gennaio: la prima barra che l'engine processa dopo il weekend.
    private static readonly DateTime MondayFirstBar = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AStopEmittedOnFriday_FillsOnMondayGapUnlessCancelled(bool cancelAtWeekEnd)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Il filtro sul lato del livello scarterebbe da solo il fill sul gap — l'apertura del
        // lunedi' e' oltre lo stop — e nasconderebbe cio' che questo test deve dimostrare: che il
        // pericolo esiste e che a difenderne e' la CANCELLAZIONE, non il filtro. Il filtro e' la
        // seconda rete, e chi porta il profilo sorgente non ce l'ha: vedi
        // TheWeekEndFlatIsTheDefenceEvenWithTheWrongSideFilterOn, sotto.
        service.RejectWrongSideLevels = false;

        service.ProcessSignals(
            [FridayStopEntry()],
            Prices(16_000m),
            Bars(FridayLastBar, open: 16_000m, high: 16_005m, low: 15_990m, close: 16_000m),
            FridayLastBar);

        // L'ordine è in attesa: il livello non è stato toccato sulla barra di emissione.
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", FridayLastBar).Position);
        Assert.Equal(1, service.PendingOrdersCount);

        if (cancelAtWeekEnd)
        {
            Assert.Equal(1, service.CancelAllPendingOrders());
            Assert.Equal(0, service.PendingOrdersCount);
        }

        // Lunedì il mercato riapre in gap oltre il livello dello stop.
        service.UpdateMarketPrices(
            Prices(16_120m),
            Bars(MondayFirstBar, open: 16_100m, high: 16_130m, low: 16_095m, close: 16_120m),
            MondayFirstBar);

        var position = service.GetExecutionSnapshot(Code, "NQ", MondayFirstBar).Position;
        if (cancelAtWeekEnd)
        {
            Assert.Null(position);
            return;
        }

        // Il controllo che rende significativo il caso sopra: senza cancellazione l'ordine del
        // venerdì entra davvero sul gap, e al prezzo di apertura del lunedì.
        Assert.NotNull(position);
        Assert.Equal(16_100m, position!.EntryPrice);
    }

    /// <summary>
    /// Col filtro acceso — il default, allineato al cBot — il fill sul gap di riapertura non
    /// avviene comunque: l'apertura del lunedi' e' gia' oltre lo stop, e un livello scavalcato non
    /// si piazza. Non rende superflua la cancellazione, la raddoppia.
    /// </summary>
    [Fact]
    public void TheWeekEndFlatIsTheDefenceEvenWithTheWrongSideFilterOn()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        Assert.True(service.RejectWrongSideLevels);

        service.ProcessSignals(
            [FridayStopEntry()],
            Prices(16_000m),
            Bars(FridayLastBar, open: 16_000m, high: 16_005m, low: 15_990m, close: 16_000m),
            FridayLastBar);
        Assert.Equal(1, service.PendingOrdersCount);

        service.UpdateMarketPrices(
            Prices(16_120m),
            Bars(MondayFirstBar, open: 16_100m, high: 16_130m, low: 16_095m, close: 16_120m),
            MondayFirstBar);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", MondayFirstBar).Position);
        Assert.Equal(1, service.WrongSideLevelsRejected);
    }

    [Fact]
    public void TheWeekEndCloseLeavesNeitherPositionsNorOrders()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Una posizione aperta a mercato più uno stop nell'altra direzione ancora in attesa:
        // è lo stato tipico del Price Channel a fine settimana.
        service.ProcessSignals(
            [MarketEntry(SignalType.Buy), FridayStopEntry(SignalType.Sell, 15_900m)],
            Prices(16_000m),
            Bars(FridayLastBar, open: 16_000m, high: 16_005m, low: 15_990m, close: 16_000m),
            FridayLastBar);

        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", FridayLastBar).Position);
        Assert.Equal(1, service.PendingOrdersCount);

        service.CancelAllPendingOrders();
        var snapshot = service.CloseAllOpenPositions(
            Prices(16_000m),
            Bars(FridayLastBar, open: 16_000m, high: 16_005m, low: 15_990m, close: 16_000m),
            FridayLastBar,
            TradeExitReason.WeekEnd);

        Assert.Equal(0, snapshot.OpenPositionsCount);
        Assert.Equal(0, service.PendingOrdersCount);

        // Il motivo di uscita distingue in analisi la chiusura tecnica da un'uscita di strategia.
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.WeekEnd, trade.ExitReason);
    }

    /// <summary>
    /// L'orario di flat e' un NUMERO DICHIARATO, non l'ultimo slot dell'orologio prima di sabato.
    ///
    /// <para>Era proprio quello il difetto: il backtest chiudeva sull'ultima iterazione della
    /// settimana — venerdi' 23:30 con timeframe minimo a 30 minuti, tutto l'anno — mentre il conto
    /// vero e' piatto dalle 20:45. Due ore e tre quarti di venerdi' su quasi meta' dei trade.</para>
    /// </summary>
    [Theory]
    // Venerdi': prima dell'orario si opera, dall'orario in poi no.
    [InlineData(2024, 1, 5, 20, 30, false)]
    [InlineData(2024, 1, 5, 20, 45, true)]
    [InlineData(2024, 1, 5, 21, 0, true)]
    [InlineData(2024, 1, 5, 23, 30, true)]
    // Sabato sempre dentro; domenica fino alla riapertura.
    [InlineData(2024, 1, 6, 12, 0, true)]
    [InlineData(2024, 1, 7, 22, 59, true)]
    [InlineData(2024, 1, 7, 23, 0, false)]
    // Giorni feriali: mai.
    [InlineData(2024, 1, 4, 23, 30, false)]
    public void LaFinestraDiFlat_SegueLOrarioDichiarato(
        int year, int month, int day, int hour, int minute, bool dentro)
    {
        var policy = WeekEndFlatPolicy.Default;
        Assert.Equal(2045, policy.FromUtcHhmm);
        Assert.Equal(dentro, policy.IsInsideWindow(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc)));
    }

    /// <summary>
    /// Il flat scatta una volta sola, sulla prima barra dentro la finestra: e' cosi' che il
    /// backtest chiude una volta e non a ogni barra del fine settimana.
    /// </summary>
    [Fact]
    public void IlFlatScattaUnaVoltaSola_SullaPrimaBarraUtile()
    {
        var policy = WeekEndFlatPolicy.Default;
        var venerdi2030 = new DateTime(2024, 1, 5, 20, 30, 0, DateTimeKind.Utc);

        // Orologio a 30 minuti: 20:45 non e' un tick, quindi scatta alle 21:00 — la prima barra
        // che il feed offre dopo l'orario, come farebbe il cBot sullo stesso grafico.
        Assert.True(policy.IsFlatTrigger(venerdi2030.AddMinutes(30), venerdi2030));
        Assert.False(policy.IsFlatTrigger(venerdi2030.AddMinutes(60), venerdi2030.AddMinutes(30)));
        Assert.False(policy.IsFlatTrigger(venerdi2030, venerdi2030.AddMinutes(-30)));
    }

    private static TradeSignal FridayStopEntry(SignalType type = SignalType.Buy, decimal price = 16_050m) => new()
    {
        Date = FridayLastBar,
        Type = type,
        Price = price,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        // Semantica "next bar" di EasyLanguage: la barra successiva è quella di lunedì.
        ValidFromUtc = MondayFirstBar,
        ExpiresAtUtc = MondayFirstBar
    };

    private static TradeSignal MarketEntry(SignalType type) => new()
    {
        Date = FridayLastBar,
        Type = type,
        Price = 16_000m,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Market
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
