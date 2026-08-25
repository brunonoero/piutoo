using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Riproduzione del disallineamento fra backtest ed esecuzione distribuita sui limit "next bar".
///
/// <para>I numeri non sono inventati: vengono dal confronto fra il run di backtest 12/08–07/10/2022
/// su GC e la sessione live <c>896ba1d9…</c> sullo stesso periodo. Il segnale
/// <c>PTS_GC_RHL_001_60</c> emesso sulla barra delle 11:00 del 13/09/2022 dichiara un Buy Limit a
/// 2066,5 valido sulla barra successiva; quella barra ha minimo 2059,1, quindi il livello è
/// penetrato. Il cBot lo ha riempito, il backtest non ha aperto nulla — ed è una delle sei
/// operazioni che il live ha e il backtest no.</para>
/// </summary>
public class PendingLimitOrderTests
{
    private const string Strategy = "PTS_GC_RHL_001_60";
    private const string Symbol = "GC";

    private static readonly DateTime SignalBar = new(2022, 9, 13, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NextBar = new(2022, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>La barra 12:00 del feed: apre sopra il livello e scende a penetrarlo.</summary>
    private static OhlcvData FillBar() => new()
    {
        DateTime = NextBar,
        Open = 2086.8m,
        High = 2089.0m,
        Low = 2059.1m,
        Close = 2059.9m
    };

    private static TradeSignal Signal() => new()
    {
        Date = SignalBar,
        Type = SignalType.Buy,
        Price = 2066.5m,
        Symbol = Symbol,
        StrategyName = Strategy,
        StrategyCode = Strategy,
        Quantity = 1,
        OrderType = TradeOrderType.Limit,
        // Semantica "next bar" di EasyLanguage: nasce alla chiusura della barra di segnale, vive
        // una barra sola e scade con essa. È il motivo per cui i due estremi coincidono.
        ValidFromUtc = NextBar,
        ExpiresAtUtc = NextBar,
        StopLossMoneyPerFutureContract = 2000m
    };

    [Fact]
    public void LimitNextBar_EmessoPrimaDellaSuaBarra_SiRiempieQuandoLaBarraPenetraIlLivello()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m);

        // Il segnale arriva mentre la sua barra di validità non è ancora cominciata: va in coda.
        service.ProcessSignals(
            [Signal()],
            new Dictionary<string, decimal> { [Symbol] = 2086.8m },
            new Dictionary<string, OhlcvData>
            {
                [Symbol] = new OhlcvData
                {
                    DateTime = SignalBar, Open = 2090.0m, High = 2092.0m, Low = 2085.0m, Close = 2086.8m
                }
            },
            SignalBar);

        Assert.Null(service.GetExecutionSnapshot(Strategy, Symbol, SignalBar).Position);

        // Barra di validità: il minimo penetra il limite, quindi l'ordine deve riempirsi al livello.
        service.UpdateMarketPrices(
            new Dictionary<string, decimal> { [Symbol] = 2059.9m },
            new Dictionary<string, OhlcvData> { [Symbol] = FillBar() },
            NextBar);

        var snapshot = service.GetExecutionSnapshot(Strategy, Symbol, NextBar);
        Assert.NotNull(snapshot.Position);
        Assert.Equal(SignalType.Buy, snapshot.Position!.Direction);
        Assert.Equal(2066.5m, snapshot.Position.EntryPrice);
    }

    [Fact]
    public void LimitNextBar_ArrivatoQuandoLaSuaBarraECorrente_NonEntraAMercatoIgnorandoIlLivello()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m);

        // Stesso segnale, ma consegnato quando la sua barra di validità è già quella corrente:
        // succede ogni volta che la strategia viene valutata sulla chiusura della barra che l'ha
        // generata. Il limit deve comunque essere valutato come limit — riempito al proprio livello
        // perché la barra lo penetra — e non aperto a mercato al prezzo di apertura.
        service.ProcessSignals(
            [Signal()],
            new Dictionary<string, decimal> { [Symbol] = 2059.9m },
            new Dictionary<string, OhlcvData> { [Symbol] = FillBar() },
            NextBar);

        var snapshot = service.GetExecutionSnapshot(Strategy, Symbol, NextBar);
        Assert.NotNull(snapshot.Position);
        Assert.Equal(2066.5m, snapshot.Position!.EntryPrice);
    }

    [Fact]
    public void LimitNextBar_SullaSuaBarra_NonSiRiempieSeIlLivelloNonVieneRaggiunto()
    {
        var service = new PiootooTradingService();
        service.Initialize(1_000_000m);

        // Barra che resta tutta sopra il limite: il prezzo non ha mai toccato 2066,5, quindi
        // nessuno ha scambiato a quel livello e la posizione non può esistere. Aprirla comunque
        // sarebbe un fill fantasma, il caso che docs/domini/orologio-barre-e-fill.md descrive.
        var barSopraIlLivello = new OhlcvData
        {
            DateTime = NextBar, Open = 2086.8m, High = 2089.0m, Low = 2080.0m, Close = 2085.0m
        };

        service.ProcessSignals(
            [Signal()],
            new Dictionary<string, decimal> { [Symbol] = 2085.0m },
            new Dictionary<string, OhlcvData> { [Symbol] = barSopraIlLivello },
            NextBar);

        Assert.Null(service.GetExecutionSnapshot(Strategy, Symbol, NextBar).Position);
    }
}
